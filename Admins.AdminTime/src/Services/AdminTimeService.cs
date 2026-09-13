using System.Collections.Concurrent;
using Admins.AdminTime.Configuration;
using Admins.Core.Contract;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace Admins.AdminTime.Services;

public class AdminSessionData
{
    public long ConnectTime { get; set; }
    public long ActiveTimeSeconds { get; set; }
    public long ActiveTeamJoinedTime { get; set; }
}

public class AdminTimeService
{
    private readonly ISwiftlyCore Core;
    private readonly IOptionsMonitor<TimeConfiguration> _config;
    private Admins.Core.Contract.IConfigurationManager? _configurationManager;
    private readonly ConcurrentDictionary<ulong, AdminSessionData> _adminSessions = new();

    private IAdminsManager? _adminsManager;

    public AdminTimeService(ISwiftlyCore core, IOptionsMonitor<TimeConfiguration> config)
    {
        Core = core;
        _config = config;
        core.Registrator.Register(this);
    }

    public void SetConfigurationManager(Admins.Core.Contract.IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public void SetAdminsManager(IAdminsManager adminsManager)
    {
        _adminsManager = adminsManager;
    }

    public string GetSessionFormattedTime(IPlayer player)
    {
        if (_adminSessions.TryGetValue(player.SteamID, out var session))
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long currentSessionSeconds = now - session.ConnectTime;
            TimeSpan span = TimeSpan.FromSeconds(currentSessionSeconds);
            return $"Текущая сессия: {span.Hours}ч {span.Minutes}мин {span.Seconds}сек";
        }
        return "Сессия не найдена";
    }

    private string GetPrefix()
    {
        return _configurationManager?.GetCurrentConfiguration()?.Prefix ?? "[Admins]";
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS `as_admin_time` (
                    `id` INT PRIMARY KEY AUTO_INCREMENT,
                    `admin_id` VARCHAR(32) NOT NULL,
                    `admin_name` VARCHAR(64) NOT NULL,
                    `connect_time` INT NOT NULL,
                    `disconnect_time` INT NOT NULL DEFAULT -1 COMMENT '-1 = Admin online',
                    `played_time` INT NOT NULL DEFAULT 0,
                    `server_id` VARCHAR(32) NOT NULL
                );";

            await db.ExecuteAsync(createTableSql);

            if (_config.CurrentValue.ResetUnfinishedSessions)
            {
                int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int serverId = _config.CurrentValue.ServerId;

                string resetSql = @"
                    UPDATE `as_admin_time` 
                    SET `disconnect_time` = @Now, `played_time` = @Now - `connect_time` 
                    WHERE `disconnect_time` = -1 AND `server_id` = @ServerId;";

                await db.ExecuteAsync(resetSql, new { Now = now, ServerId = serverId.ToString() });
            }

            Core.Logger.LogInformation("[Admins.AdminTime] Database table 'as_admin_time' initialized.");
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Admins.AdminTime] Error initializing database table 'as_admin_time'.");
        }
    }

    [EventListener<EventDelegates.OnClientSteamAuthorize>]
    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player == null || !player.IsValid) return;

        if (Core.Permission.PlayerHasPermission(player.SteamID, "admins.commands.admin"))
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _adminSessions[player.SteamID] = new AdminSessionData
            {
                ConnectTime = now,
                ActiveTimeSeconds = 0,
                ActiveTeamJoinedTime = now
            };

            _ = RecordAdminConnectAsync(player.SteamID, player.Controller.PlayerName, now);
        }
    }

    private async Task RecordAdminConnectAsync(ulong steamId, string playerName, long connectTime)
    {
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            int serverId = _config.CurrentValue.ServerId;

            string sql = @"
                INSERT INTO `as_admin_time` (`admin_id`, `admin_name`, `connect_time`, `server_id`) 
                VALUES (@AdminId, @AdminName, @ConnectTime, @ServerId);";

            await db.ExecuteAsync(sql, new
            {
                AdminId = steamId.ToString(),
                AdminName = playerName,
                ConnectTime = connectTime,
                ServerId = serverId.ToString()
            });
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, $"[Admins.AdminTime] Error recording connect time for admin {steamId}.");
        }
    }

    [EventListener<EventDelegates.OnClientDisconnected>]
    public void OnClientDisconnected(IOnClientDisconnectedEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player == null || !player.IsValid) return;

        if (_adminSessions.TryRemove(player.SteamID, out var session))
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long playedTimeSeconds;

            if (_config.CurrentValue.CountSpectatorTime)
            {
                playedTimeSeconds = now - session.ConnectTime;
            }
            else
            {
                if (session.ActiveTeamJoinedTime > 0)
                {
                    session.ActiveTimeSeconds += now - session.ActiveTeamJoinedTime;
                }
                playedTimeSeconds = session.ActiveTimeSeconds;
            }

            _ = RecordAdminDisconnectAsync(player.SteamID, now, playedTimeSeconds);
        }
    }

    private async Task RecordAdminDisconnectAsync(ulong steamId, long disconnectTime, long playedTime)
    {
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            int serverId = _config.CurrentValue.ServerId;

            string sql = @"
                UPDATE `as_admin_time` 
                SET `disconnect_time` = @DisconnectTime, `played_time` = @PlayedTime 
                WHERE `admin_id` = @AdminId AND `disconnect_time` = -1 AND `server_id` = @ServerId;";

            await db.ExecuteAsync(sql, new
            {
                DisconnectTime = disconnectTime,
                PlayedTime = playedTime,
                AdminId = steamId.ToString(),
                ServerId = serverId.ToString()
            });
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, $"[Admins.AdminTime] Error recording disconnect time for admin {steamId}.");
        }
    }

    [Command("admintime", permission: "admins.commands.admin")]
    [CommandAlias("atime")]
    public void Command_AdminTime(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;

        var player = context.Sender!;
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        if (_adminSessions.TryGetValue(player.SteamID, out var session))
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long currentSessionSeconds = now - session.ConnectTime;
            TimeSpan span = TimeSpan.FromSeconds(currentSessionSeconds);

            context.Reply(localizer["atime.session_info", GetPrefix(), span.Hours, span.Minutes, span.Seconds]);
        }
        else
        {
            context.Reply(localizer["atime.not_found", GetPrefix()]);
        }
    }
}
