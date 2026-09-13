using System.Collections.Concurrent;
using Admins.Bans.Contract;
using Admins.CheckCheats.Configuration;
using Admins.CheckCheats.Database;
using Admins.Core.Contract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Admins.CheckCheats.Commands;

public class CheckSession
{
    public int DbRecordId { get; set; }
    public ulong AdminSteamId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public ulong SuspectSteamId { get; set; }
    public string SuspectName { get; set; } = string.Empty;
    public string SocialType { get; set; } = "Discord / Telegram";
    public string ContactInfo { get; set; } = string.Empty;
    public int Stage { get; set; } = 0;
    public long StartTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public bool TimerStopped { get; set; } = false;
    public CancellationTokenSource? TimerCts { get; set; }
}

public class CheckCommands
{
    private readonly ISwiftlyCore Core;
    private IConfigurationManager? ConfigurationManager;
    private IBansManager? BansManager;
    private readonly IOptionsMonitor<CheckConfiguration> _config;
    private readonly CheckCheatsDb _db;

    private readonly ConcurrentDictionary<ulong, CheckSession> _activeChecksBySuspect = new();
    private readonly ConcurrentDictionary<ulong, CheckSession> _activeChecksByAdmin = new();

    public CheckCommands(ISwiftlyCore core, IOptionsMonitor<CheckConfiguration> config)
    {
        Core = core;
        _config = config;
        _db = new CheckCheatsDb(core);
        core.Registrator.Register(this);
        core.GameEvent.HookPre<EventPlayerTeam>(OnPlayerTeam);

        _ = _db.InitializeDatabaseAsync();
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        ConfigurationManager = configurationManager;
    }

    public void SetBansManager(IBansManager bansManager)
    {
        BansManager = bansManager;
    }

    private string GetPrefix()
    {
        return ConfigurationManager?.GetCurrentConfiguration()?.Prefix ?? "[Admins]";
    }

    public CheckSession? GetActiveCheckForAdmin(ulong adminSteamId)
    {
        _activeChecksByAdmin.TryGetValue(adminSteamId, out var session);
        return session;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        var player = @event.UserIdPlayer ?? Core.PlayerManager.GetPlayer(@event.UserId);
        if (player == null || !player.IsValid) return HookResult.Continue;

        if (_config.CurrentValue.AutoMove == 1 && _activeChecksBySuspect.ContainsKey(player.SteamID) && @event.Team != 1)
        {
            Core.Scheduler.NextTick(() =>
            {
                if (player.IsValid)
                {
                    player.SwitchTeam(Team.Spectator);
                }
            });
        }

        return HookResult.Continue;
    }

    [EventListener<EventDelegates.OnClientDisconnected>]
    public void OnClientDisconnected(IOnClientDisconnectedEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player != null && player.IsValid)
        {
            if (_activeChecksBySuspect.TryRemove(player.SteamID, out var session))
            {
                _activeChecksByAdmin.TryRemove(session.AdminSteamId, out _);
                session.TimerCts?.Cancel();

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var leaveConfig = _config.CurrentValue.Leave;

                string verdict = leaveConfig.Reason;
                string banReason = string.IsNullOrWhiteSpace(leaveConfig.Reason2) ? leaveConfig.Reason : leaveConfig.Reason2;
                int duration = leaveConfig.Time;

                _ = _db.UpdateVerdictAsync(session.DbRecordId, verdict, now);

                var localizer = Core.Translation.GetPlayerLocalizer(player);
                Core.PlayerManager.SendChat(localizer["CC_Banned_Left", GetPrefix(), session.SuspectName]);

                if (leaveConfig.PlayerBanStatus == 1 && duration >= 0)
                {
                    BanSuspect(session, duration, banReason);
                }
            }
        }
    }

    [Command("check", permission: "@admin/check")]
    [CommandAlias("checkcheats")]
    [CommandAlias("sw_check")]
    public void Command_Check(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("[Admins.CheckCheats] This command can only be used by players.");
            return;
        }

        var admin = context.Sender!;

        if (context.Args.Length < 1)
        {
            context.Reply($"{GetPrefix()} Синтаксис: [lime]!check <player>[default]");
            return;
        }

        var targets = Core.PlayerManager.FindTargettedPlayers(admin, context.Args[0], TargetSearchMode.IncludeSelf);
        var suspect = targets.FirstOrDefault(p => p != null && p.IsValid && !p.IsFakeClient && p.SteamID != admin.SteamID);

        if (suspect == null)
        {
            context.Reply($"{GetPrefix()} Игрок не найден.");
            return;
        }

        StartCheck(admin, suspect);
    }

    public void StartCheck(IPlayer admin, IPlayer suspect)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        if (_activeChecksBySuspect.ContainsKey(suspect.SteamID))
        {
            admin.SendMessage(MessageType.Chat, $"{GetPrefix()} Игрок {suspect.Controller.PlayerName} уже находится на проверке.");
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var session = new CheckSession
        {
            AdminSteamId = admin.SteamID,
            AdminName = admin.Controller.PlayerName,
            SuspectSteamId = suspect.SteamID,
            SuspectName = suspect.Controller.PlayerName,
            StartTimestamp = now,
            Stage = 0
        };

        _activeChecksBySuspect[suspect.SteamID] = session;
        _activeChecksByAdmin[admin.SteamID] = session;

        if (_config.CurrentValue.AutoMove == 1)
        {
            suspect.SwitchTeam(Team.Spectator);
        }

        int serverId = _config.CurrentValue.ServerId;
        if (_config.CurrentValue.DbUse == 1)
        {
            _ = Task.Run(async () =>
            {
                int dbId = await _db.InsertCheckStartAsync(serverId, suspect.SteamID.ToString(), suspect.Controller.PlayerName, admin.SteamID.ToString(), admin.Controller.PlayerName, now);
                session.DbRecordId = dbId;
            });
        }

        StartTimer(session);

        Core.PlayerManager.SendChat(localizer["CC_Start_Notice", GetPrefix(), admin.Controller.PlayerName, suspect.Controller.PlayerName]);
        suspect.SendMessage(MessageType.Chat, localizer["CC_Input_Contact", GetPrefix(), session.SocialType]);
    }

    private void StartTimer(CheckSession session)
    {
        session.TimerCts = new CancellationTokenSource();
        var token = session.TimerCts.Token;
        int delayMs = Math.Max(5, _config.CurrentValue.Timer.Time) * 1000;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested || session.TimerStopped || session.Stage >= 3) return;

                if (_activeChecksBySuspect.TryRemove(session.SuspectSteamId, out _))
                {
                    _activeChecksByAdmin.TryRemove(session.AdminSteamId, out _);

                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var timerConfig = _config.CurrentValue.Timer;
                    string reasonKey = timerConfig.AutoBanReason;

                    string verdict = "Не указал социальную сеть";
                    string banReason = "Игрок не указал социальную сеть";
                    int duration = 86400;

                    if (_config.CurrentValue.Reasons.TryGetValue(reasonKey, out var rConfig))
                    {
                        verdict = rConfig.Reason;
                        banReason = string.IsNullOrWhiteSpace(rConfig.Reason2) ? rConfig.Reason : rConfig.Reason2;
                        duration = rConfig.Time;
                    }

                    if (_config.CurrentValue.DbUse == 1)
                    {
                        await _db.UpdateVerdictAsync(session.DbRecordId, verdict, now);
                    }

                    Core.Scheduler.NextTick(() =>
                    {
                        var player = Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(p => p.SteamID == session.SuspectSteamId);
                        var localizer = player != null ? Core.Translation.GetPlayerLocalizer(player) : Core.Translation.GetPlayerLocalizer(Core.PlayerManager.GetAllValidPlayers().First());
                        Core.PlayerManager.SendChat(localizer["CC_Banned_NoContact", GetPrefix(), session.SuspectName]);

                        if (timerConfig.AutoBanStatus == 1 && duration >= 0)
                        {
                            BanSuspect(session, duration, banReason);
                        }
                    });
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Admins.CheckCheats] Error in timer task.");
            }
        });
    }

    private void BanSuspect(CheckSession session, long durationSec, string reason)
    {
        Core.Scheduler.NextTick(() =>
        {
            Core.Engine.ExecuteCommand($"sw_bano {session.SuspectSteamId} {durationSec} \"{reason}\"");
        });
    }

    [Command("contact")]
    [CommandAlias("co")]
    public void Command_Contact(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;

        var suspect = context.Sender!;
        if (!_activeChecksBySuspect.TryGetValue(suspect.SteamID, out var session))
        {
            return;
        }

        if (context.Args.Length < 1)
        {
            context.Reply($"{GetPrefix()} Синтаксис: [lime]!contact <ваш_дискорд_или_телеграм>[default]");
            return;
        }

        string contactInfo = string.Join(" ", context.Args);
        session.ContactInfo = contactInfo;
        session.Stage = 2;

        if (_config.CurrentValue.DbUse == 1)
        {
            _ = _db.UpdateContactAsync(session.DbRecordId, contactInfo);
        }

        var localizer = Core.Translation.GetPlayerLocalizer(suspect);
        var admin = Core.PlayerManager.GetAllValidPlayers().FirstOrDefault(p => p.SteamID == session.AdminSteamId);

        if (admin != null && admin.IsValid)
        {
            admin.SendMessage(MessageType.Chat, localizer["CC_Contact_Received", GetPrefix(), suspect.Controller.PlayerName, contactInfo]);
        }

        suspect.SendMessage(MessageType.Chat, $"{GetPrefix()} Ваш контакт отправлен администратору: [lime]{contactInfo}[default]");
    }

    public void MarkStartCheck(IPlayer admin)
    {
        if (!_activeChecksByAdmin.TryGetValue(admin.SteamID, out var session)) return;

        session.Stage = 3;
        session.TimerStopped = true;
        session.TimerCts?.Cancel();

        var localizer = Core.Translation.GetPlayerLocalizer(admin);
        Core.PlayerManager.SendChat(localizer["CC_Stage3_Notice", GetPrefix(), admin.Controller.PlayerName]);
    }

    public void EndCheck(IPlayer admin, string reasonKey)
    {
        if (!_activeChecksByAdmin.TryRemove(admin.SteamID, out var session)) return;
        _activeChecksBySuspect.TryRemove(session.SuspectSteamId, out _);

        session.TimerCts?.Cancel();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        string verdict = "Отмена проверки";
        string banReason = "Отмена проверки";
        int duration = -1;

        if (_config.CurrentValue.Reasons.TryGetValue(reasonKey, out var rConfig))
        {
            verdict = rConfig.Reason;
            banReason = string.IsNullOrWhiteSpace(rConfig.Reason2) ? rConfig.Reason : rConfig.Reason2;
            duration = rConfig.Time;
        }

        if (_config.CurrentValue.DbUse == 1)
        {
            _ = _db.UpdateVerdictAsync(session.DbRecordId, verdict, now);
        }

        if (reasonKey == "4") // Миссклик
        {
            Core.PlayerManager.SendChat(localizer["CC_Missclick", GetPrefix(), session.SuspectName]);
        }
        else if (duration < 0) // Чистый / Без бана
        {
            Core.PlayerManager.SendChat(localizer["CC_Passed", GetPrefix(), session.SuspectName]);
        }
        else // Забанить
        {
            Core.PlayerManager.SendChat(localizer["CC_Banned_Cheats", GetPrefix(), session.SuspectName]);
            BanSuspect(session, duration, banReason);
        }
    }

    [Command("stopcheck", permission: "@admin/check")]
    public void Command_StopCheck(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;

        var admin = context.Sender!;
        EndCheck(admin, "4"); // Default stop as Missclick/Cancel
    }
}
