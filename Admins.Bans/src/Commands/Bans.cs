using Admins.Bans.Contract;
using Admins.Bans.Database.Models;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Admins.Bans.Commands;

public partial class ServerCommands
{
    [Command("ban", permission: "admins.commands.ban")]
    public void Command_Ban(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "ban", ["<player|steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = Core.PlayerManager.FindTargettedPlayers(context.Sender!, context.Args[0], TargetSearchMode.IncludeSelf);

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));

        if (players != null && players.Any())
        {
            ApplyBan(players.ToList(), context, BanType.SteamID, duration, reason, isGlobal: false);
            KickBannedPlayers(players.ToList());
        }
        else if (TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            var onlinePlayer = Core.PlayerManager.GetPlayerFromSteamId(steamId64);
            if (onlinePlayer != null)
            {
                ApplyBan([onlinePlayer], context, BanType.SteamID, duration, reason, isGlobal: false);
                KickBannedPlayers([onlinePlayer]);
            }
            else
            {
                ApplyOfflineBan(context, (long)steamId64, null, BanType.SteamID, duration, reason, isGlobal: false);
            }
        }
        else
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer["command.invalid_target", ConfigurationManager.GetCurrentConfiguration()!.Prefix, context.Args[0]]);
        }
    }

    [Command("globalban", permission: "admins.commands.globalban")]
    public void Command_GlobalBan(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalban", ["<player|steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = Core.PlayerManager.FindTargettedPlayers(context.Sender!, context.Args[0], TargetSearchMode.IncludeSelf);

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));

        if (players != null && players.Any())
        {
            ApplyBan(players.ToList(), context, BanType.SteamID, duration, reason, isGlobal: true);
            KickBannedPlayers(players.ToList());
        }
        else if (TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            var onlinePlayer = Core.PlayerManager.GetPlayerFromSteamId(steamId64);
            if (onlinePlayer != null)
            {
                ApplyBan([onlinePlayer], context, BanType.SteamID, duration, reason, isGlobal: true);
                KickBannedPlayers([onlinePlayer]);
            }
            else
            {
                ApplyOfflineBan(context, (long)steamId64, null, BanType.SteamID, duration, reason, isGlobal: true);
            }
        }
        else
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer["command.invalid_target", ConfigurationManager.GetCurrentConfiguration()!.Prefix, context.Args[0]]);
        }
    }

    [Command("banip", permission: "admins.commands.ban")]
    public void Command_BanIp(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "banip", ["<player|ip_address>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = Core.PlayerManager.FindTargettedPlayers(context.Sender!, context.Args[0], TargetSearchMode.IncludeSelf);

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));

        if (players != null && players.Any())
        {
            ApplyBan(players.ToList(), context, BanType.IP, duration, reason, isGlobal: false);
            KickBannedPlayers(players.ToList());
        }
        else
        {
            var ipAddress = context.Args[0];
            ApplyOfflineBan(context, 0, ipAddress, BanType.IP, duration, reason, isGlobal: false);
        }
    }

    [Command("unban", permission: "admins.commands.unban")]
    public void Command_Unban(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "unban", ["<steamid64>"]))
        {
            return;
        }

        if (TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            RemoveBanBySteamID(context, (long)steamId64);
        }
        else
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer["command.invalid_steamid", ConfigurationManager.GetCurrentConfiguration()!.Prefix, context.Args[0]]);
        }
    }

    [Command("unbanip", permission: "admins.commands.unban")]
    public void Command_UnbanIp(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "unbanip", ["<ip_address>"]))
        {
            return;
        }

        var ipAddress = context.Args[0];
        RemoveBanByIP(context, ipAddress);
    }

    private void ApplyBan(
        List<IPlayer> players,
        ICommandContext context,
        BanType banType,
        TimeSpan duration,
        string reason,
        bool isGlobal)
    {
        var applicablePlayers = new List<IPlayer>();

        foreach (var player in players)
        {
            if (context.IsSentByPlayer && !CanApplyActionToPlayer(context, player))
            {
                NotifyImmunityProtection(context, player);
            }
            else
            {
                applicablePlayers.Add(player);
            }
        }

        if (!applicablePlayers.Any())
        {
            return;
        }

        var expiresAt = CalculateExpiresAt(duration);
        var adminName = GetAdminName(context);

        foreach (var player in applicablePlayers)
        {
            var ban = new Ban
            {
                SteamId64 = (long)player.SteamID,
                BanType = banType,
                Reason = reason,
                PlayerName = player.Controller.PlayerName,
                PlayerIp = player.IPAddress ?? "",
                ExpiresAt = expiresAt,
                Length = (long)duration.TotalSeconds,
                AdminSteamId64 = context.IsSentByPlayer ? (long)context.Sender!.SteamID : 0,
                AdminName = adminName,
                Server = ConfigurationManager.GetCurrentConfiguration()!.PunishServerId.ToString(),
                GlobalBan = isGlobal,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            BanManager.AddBan(ban);
        }

        NotifyBanApplied(applicablePlayers, context.Sender, expiresAt, adminName, reason);
    }

    private void ApplyOfflineBan(
        ICommandContext context,
        long steamId64,
        string? ipAddress,
        BanType banType,
        TimeSpan duration,
        string reason,
        bool isGlobal)
    {
        var expiresAt = CalculateExpiresAt(duration);
        var adminName = GetAdminName(context);

        var ban = new Ban
        {
            SteamId64 = steamId64,
            BanType = banType,
            Reason = reason,
            PlayerName = steamId64 != 0 ? $"Offline ({steamId64})" : (ipAddress ?? "Offline IP"),
            PlayerIp = ipAddress ?? "",
            ExpiresAt = expiresAt,
            Length = (long)duration.TotalSeconds,
            AdminSteamId64 = context.IsSentByPlayer ? (long)context.Sender!.SteamID : 0,
            AdminName = adminName,
            Server = ConfigurationManager.GetCurrentConfiguration()!.PunishServerId.ToString(),
            GlobalBan = isGlobal,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        BanManager.AddBan(ban);

        var localizer = GetPlayerLocalizer(context);
        var message = localizer["command.bano_success", ConfigurationManager.GetCurrentConfiguration()!.Prefix, adminName, steamId64 != 0 ? steamId64.ToString() : (ipAddress ?? ""), expiresAt == 0 ? localizer["never"] : FormatTimestampInTimeZone(expiresAt), isGlobal ? "Global" : "Local", reason];
        context.Reply(message);
    }

    public void NotifyBanApplied(
        List<IPlayer> players,
        IPlayer? sender,
        long expiresAt,
        string adminName,
        string reason)
    {
        SendMessageToPlayers(players, sender, (player, localizer) =>
        {
            var expiryText = expiresAt == 0
                ? localizer["never"]
                : FormatTimestampInTimeZone(expiresAt);

            var message = localizer[
                "ban.kick_message",
                reason,
                expiryText,
                adminName,
                sender?.SteamID.ToString() ?? "0"
            ];

            return (message, MessageType.Console);
        });
    }

    public void KickBannedPlayers(List<IPlayer> players)
    {
        int banDelay = ConfigurationManager.GetCurrentConfiguration()!.BanDelay;

        Core.Scheduler.NextTick(() =>
        {
            foreach (var player in players)
            {
                if (banDelay > 0)
                {
                    Core.Scheduler.DelayBySeconds(banDelay, () =>
                    {
                        if (player.IsValid)
                        {
                            player.Kick("Banned.", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
                        }
                    });
                }
                else
                {
                    player.Kick("Banned.", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
                }
            }
        });
    }

    private void RemoveBanBySteamID(ICommandContext context, long steamId64)
    {
        var adminName = GetAdminName(context);
        var bans = BanManager.FindBans(steamId64);

        int unpunishType = ConfigurationManager.GetCurrentConfiguration()!.UnpunishType;
        long adminSteamId = context.IsSentByPlayer ? (long)context.Sender!.SteamID : 0;

        int removedCount = 0;
        foreach (var ban in bans)
        {
            if (unpunishType == 0 && context.IsSentByPlayer && ban.AdminSteamId64 != adminSteamId)
            {
                continue;
            }
            BanManager.RemoveBan(ban);
            removedCount++;
        }

        var localizer = GetPlayerLocalizer(context);
        var messageKey = removedCount > 0 ? "command.unban_success" : "command.unban_none";
        var message = removedCount > 0
            ? localizer[messageKey, ConfigurationManager.GetCurrentConfiguration()!.Prefix, adminName, removedCount, steamId64]
            : localizer[messageKey, ConfigurationManager.GetCurrentConfiguration()!.Prefix, steamId64];
        context.Reply(message);
    }

    private void RemoveBanByIP(ICommandContext context, string ipAddress)
    {
        var adminName = GetAdminName(context);
        var bans = BanManager.FindBans(null, ipAddress);

        int unpunishType = ConfigurationManager.GetCurrentConfiguration()!.UnpunishType;
        long adminSteamId = context.IsSentByPlayer ? (long)context.Sender!.SteamID : 0;

        int removedCount = 0;
        foreach (var ban in bans)
        {
            if (unpunishType == 0 && context.IsSentByPlayer && ban.AdminSteamId64 != adminSteamId)
            {
                continue;
            }
            BanManager.RemoveBan(ban);
            removedCount++;
        }

        var localizer = GetPlayerLocalizer(context);
        var messageKey = removedCount > 0 ? "command.unbanip_success" : "command.unbanip_none";
        var message = removedCount > 0
            ? localizer[messageKey, ConfigurationManager.GetCurrentConfiguration()!.Prefix, adminName, removedCount, ipAddress]
            : localizer[messageKey, ConfigurationManager.GetCurrentConfiguration()!.Prefix, ipAddress];
        context.Reply(message);
    }
}