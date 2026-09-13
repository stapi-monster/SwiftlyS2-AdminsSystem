using Admins.Comms.Contract;
using Admins.Comms.Database.Models;
using Admins.Comms.Manager;
using Admins.Comms.Players;
using Admins.Core.Contract;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using TimeSpanParserUtil;

namespace Admins.Comms.Commands;

public partial class ServerCommands
{
    public ISwiftlyCore Core = null!;
    public IConfigurationManager ConfigurationManager = null!;
    public IServerManager ServerManager = null!;
    public CommsManager CommsManager = null!;
    public IAdminsManager? AdminsManager = null!;
    public IGroupsManager? GroupsManager = null!;
    public GamePlayer? gamePlayer;

    public ServerCommands(ISwiftlyCore core, CommsManager commsManager)
    {
        Core = core;
        CommsManager = commsManager;

        core.Registrator.Register(this);
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        ConfigurationManager = configurationManager;
    }

    public void SetServerManager(IServerManager serverManager)
    {
        ServerManager = serverManager;
    }

    public void SetAdminsManager(IAdminsManager adminsManager)
    {
        AdminsManager = adminsManager;
    }

    public void SetGroupsManager(IGroupsManager groupsManager)
    {
        GroupsManager = groupsManager;
    }

    public void SetGamePlayer(GamePlayer gp)
    {
        gamePlayer = gp;
    }

    public void SendSyntax(ICommandContext context, string cmdname, string[] arguments)
    {
        var localizer = GetPlayerLocalizer(context);
        var syntax = localizer[
            "command.syntax",
            ConfigurationManager.GetCurrentConfiguration()!.Prefix,
            context.Prefix,
            cmdname,
            string.Join(" ", arguments)
        ];
        context.Reply(syntax);
    }

    public ILocalizer GetPlayerLocalizer(ICommandContext context)
    {
        return context.IsSentByPlayer
            ? Core.Translation.GetPlayerLocalizer(context.Sender!)
            : Core.Localizer;
    }

    public void SendMessageToPlayers(
        IEnumerable<IPlayer> players,
        IPlayer? sender,
        Func<IPlayer, ILocalizer, (string message, MessageType type)> messageBuilder)
    {
        foreach (var player in players)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            var (message, type) = messageBuilder(player, localizer);

            player.SendMessage(type, message);

            if (sender != null && sender != player)
            {
                sender.SendMessage(type, message);
            }
        }
    }

    public bool ValidateArgsCount(
        ICommandContext context,
        int requiredArgsCount,
        string commandName,
        string[] argNames)
    {
        if (context.Args.Length < requiredArgsCount)
        {
            SendSyntax(context, commandName, argNames);
            return false;
        }

        return true;
    }

    public bool TryParseDuration(
        ICommandContext context,
        string timeArg,
        out TimeSpan duration)
    {
        try
        {
            duration = TimeSpanParser.Parse(timeArg);
            return true;
        }
        catch
        {
            var localizer = GetPlayerLocalizer(context);
            var invalidTime = localizer["command.invalid_time", ConfigurationManager.GetCurrentConfiguration()!.Prefix, timeArg];
            context.Reply(invalidTime);
            duration = TimeSpan.Zero;
            return false;
        }
    }

    public bool TryParseSteamID(
        ICommandContext context,
        string steamIdArg,
        out ulong steamId64)
    {
        if (ulong.TryParse(steamIdArg, out steamId64) && steamId64 > 76561197960265728)
        {
            return true;
        }

        steamId64 = 0;
        return false;
    }

    public IEnumerable<IPlayer>? FindTargetPlayers(
        ICommandContext context,
        string targetArg)
    {
        var players = Core.PlayerManager.FindTargettedPlayers(
            context.Sender!,
            targetArg,
            TargetSearchMode.IncludeSelf
        );

        if (players == null || !players.Any())
        {
            var localizer = GetPlayerLocalizer(context);
            var invalidTarget = localizer["command.invalid_target", ConfigurationManager.GetCurrentConfiguration()!.Prefix, targetArg];
            context.Reply(invalidTarget);
            return null;
        }

        return players;
    }

    public long CalculateExpiresAt(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
        {
            return 0;
        }

        return DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds();
    }

    public string GetAdminName(ICommandContext context)
    {
        return context.IsSentByPlayer
            ? context.Sender!.Controller.PlayerName
            : "Console";
    }

    public TimeZoneInfo GetConfiguredTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.GetCurrentConfiguration()!.TimeZone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    public string FormatTimestampInTimeZone(long unixTimeSeconds)
    {
        var utcTime = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        var timeZone = GetConfiguredTimeZone();
        var localTime = TimeZoneInfo.ConvertTime(utcTime, timeZone);
        return localTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public int GetPlayerImmunityLevel(IPlayer player)
    {
        if (AdminsManager == null) return 0;

        var admin = AdminsManager.GetAdmin(player);
        if (admin != null)
        {
            return admin.Immunity;
        }

        if (GroupsManager == null) return 0;

        var groups = GroupsManager.GetAllGroups();
        int maxImmunity = 0;

        foreach (var group in groups)
        {
            if (Core.Permission.PlayerHasPermissions(player.SteamID, [$"group.{group.Name}"]))
            {
                if (group.Immunity > maxImmunity)
                {
                    maxImmunity = group.Immunity;
                }
            }
        }

        return maxImmunity;
    }

    public bool CanApplyActionToPlayer(ICommandContext context, IPlayer targetPlayer)
    {
        if (!context.IsSentByPlayer)
        {
            return true;
        }

        return CanAdminApplyActionToPlayer(context.Sender!, targetPlayer);
    }

    public bool CanAdminApplyActionToPlayer(IPlayer adminPlayer, IPlayer targetPlayer)
    {
        int senderImmunity = GetPlayerImmunityLevel(adminPlayer);
        int targetImmunity = GetPlayerImmunityLevel(targetPlayer);

        var config = ConfigurationManager.GetCurrentConfiguration();
        var immunityMode = config!.ImmunityMode;

        return immunityMode switch
        {
            ImmunityMode.IgnoreImmunity => true,
            ImmunityMode.ProtectFromLowerAccess => senderImmunity >= targetImmunity,
            ImmunityMode.ProtectFromEqualOrLowerAccess => senderImmunity > targetImmunity,
            ImmunityMode.ProtectWithNoImmunityBypass => targetImmunity == 0,

            _ => false,
        };
    }

    public bool CanAdminApplyActionToSteamId(IPlayer adminPlayer, ulong targetSteamId)
    {
        var targetPlayer = Core.PlayerManager.GetPlayerFromSteamId(targetSteamId);
        if (targetPlayer != null && targetPlayer.IsValid)
        {
            return CanAdminApplyActionToPlayer(adminPlayer, targetPlayer);
        }

        if (AdminsManager != null)
        {
            var targetAdmin = AdminsManager.GetAdmin(targetSteamId);
            if (targetAdmin != null)
            {
                int adminImmunity = GetPlayerImmunityLevel(adminPlayer);
                var config = ConfigurationManager.GetCurrentConfiguration();
                var immunityMode = config!.ImmunityMode;

                return immunityMode switch
                {
                    ImmunityMode.IgnoreImmunity => true,
                    ImmunityMode.ProtectFromLowerAccess => adminImmunity >= targetAdmin.Immunity,
                    ImmunityMode.ProtectFromEqualOrLowerAccess => adminImmunity > targetAdmin.Immunity,
                    ImmunityMode.ProtectWithNoImmunityBypass => targetAdmin.Immunity == 0,
                    _ => false,
                };
            }
        }

        return true;
    }

    public void NotifyImmunityProtection(ICommandContext context, IPlayer targetPlayer)
    {
        var localizer = GetPlayerLocalizer(context);
        var message = localizer[
            "command.target_has_immunity",
            targetPlayer.Controller.PlayerName,
            GetPlayerImmunityLevel(targetPlayer)
        ];
        context.Reply(message);
    }

    public void NotifyAdminOfImmunityProtection(IPlayer adminPlayer, IPlayer targetPlayer)
    {
        NotifyAdminOfImmunityProtection(adminPlayer, targetPlayer.Controller.PlayerName, GetPlayerImmunityLevel(targetPlayer));
    }

    public void NotifyAdminOfImmunityProtection(IPlayer adminPlayer, string targetName, int targetImmunity)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(adminPlayer);
        var message = localizer[
            "command.target_has_immunity",
            targetName,
            targetImmunity
        ];
        adminPlayer.SendChat(message);
    }
}