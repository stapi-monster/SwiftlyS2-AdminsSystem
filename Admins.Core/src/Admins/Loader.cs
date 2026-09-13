using System.Collections.Concurrent;
using Admins.Core.Config;
using Admins.Core.Database.Models;
using Admins.Core.Flags;
using Admins.Core.Groups;
using Admins.Core.Server;
using Dapper;
using Dommel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admins.Core.Admins;

public partial class ServerAdmins
{
    private ISwiftlyCore Core = null!;
    public static ConcurrentDictionary<ulong, Admin> AllAdmins { get; set; } = [];
    public static ConcurrentDictionary<IPlayer, Admin> OnlineAdmins { get; set; } = [];
    private IOptionsMonitor<CoreConfiguration>? _config;
    private AdminsManager? _adminsManager;
    private FlagsManager? _flagsManager;

    public ServerAdmins(IOptionsMonitor<CoreConfiguration> config, ISwiftlyCore core, FlagsManager flagsManager)
    {
        core.Registrator.Register(this);
        _config = config;
        Core = core;
        _flagsManager = flagsManager;
    }

    public void SetAdminsManager(AdminsManager adminsManager)
    {
        _adminsManager = adminsManager;
    }

    public void Load()
    {
        Task.Run(async () =>
        {
            foreach (var (adminPlayer, adminObject) in OnlineAdmins)
            {
                if (!adminPlayer.IsValid) continue;

                UnassignAdmin(adminPlayer, adminObject);
            }

            OnlineAdmins.Clear();

            if (_config!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admin_system");
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int targetServerId = _config.CurrentValue.AdminServerId;

                try
                {
                    string sql = @"
                        SELECT a.id AS Id, a.steamid AS SteamIdStr, a.name AS Username,
                               s.flags AS ServerFlags, s.immunity AS ServerImmunity, s.expires AS EndTime, s.server_id AS ServerId,
                               g.flags AS GroupFlags, g.immunity AS GroupImmunity
                        FROM as_admins a
                        JOIN as_admins_servers s ON a.id = s.admin_id
                        LEFT JOIN as_groups g ON s.group_id = g.id
                        WHERE (s.server_id = @ServerId OR s.server_id = -1 OR @ServerId = -1)
                          AND (s.expires = 0 OR s.expires > @Now)";

                    var rows = await db.QueryAsync<dynamic>(sql, new { ServerId = targetServerId, Now = now });
                    var loadedAdmins = new Dictionary<ulong, Admin>();
                    int totalRows = 0;

                    foreach (var row in rows)
                    {
                        totalRows++;
                        string steamIdStr = Convert.ToString(row.SteamIdStr) ?? "";
                        if (!ulong.TryParse(steamIdStr, out var steamId64) || steamId64 == 0) continue;

                        int serverId = row.ServerId != null ? Convert.ToInt32(row.ServerId) : -1;
                        if (targetServerId != -1 && serverId != -1 && serverId != targetServerId) continue;

                        long endTime = row.EndTime != null ? Convert.ToInt64(row.EndTime) : 0;
                        if (endTime != 0 && endTime < now) continue;

                        int serverImmunity = row.ServerImmunity != null ? Convert.ToInt32(row.ServerImmunity) : 0;
                        int groupImmunity = row.GroupImmunity != null ? Convert.ToInt32(row.GroupImmunity) : 0;
                        int finalImmunity = Math.Max(serverImmunity, groupImmunity);

                        string serverFlags = Convert.ToString(row.ServerFlags) ?? "";
                        string groupFlags = Convert.ToString(row.GroupFlags) ?? "";
                        string combinedFlags = $"{serverFlags},{groupFlags}".Trim(',', ' ');

                        if (!loadedAdmins.TryGetValue(steamId64, out var admin))
                        {
                            admin = new Admin
                            {
                                Id = Convert.ToInt64(row.Id),
                                SteamId64 = (long)steamId64,
                                Username = Convert.ToString(row.Username) ?? "Admin",
                                Immunity = finalImmunity,
                                EndTime = endTime,
                                Flags = combinedFlags
                            };
                            loadedAdmins[steamId64] = admin;
                        }
                        else
                        {
                            if (finalImmunity > admin.Immunity) admin.Immunity = finalImmunity;
                            if (!string.IsNullOrWhiteSpace(combinedFlags))
                            {
                                admin.Flags = $"{admin.Flags},{combinedFlags}".Trim(',', ' ');
                            }
                        }
                    }

                    AllAdmins = new ConcurrentDictionary<ulong, Admin>(loadedAdmins);
                }
                catch
                {
                    var admins = await db.GetAllAsync<Admin>();
                    var validAdmins = admins.Where(a => (a.EndTime == 0 || a.EndTime > now));
                    AllAdmins = new ConcurrentDictionary<ulong, Admin>(validAdmins.ToDictionary(a => (ulong)a.SteamId64, a => a));
                }
            }

            AssignAdmins();
        });
    }

    public void AssignAdmins()
    {
        var players = Core.PlayerManager.GetAllValidPlayers();

        foreach (var player in players)
        {
            if (!player.IsValid || player.IsFakeClient) continue;

            if (AllAdmins.TryGetValue(player.SteamID, out var adminObject))
            {
                AssignAdmin(player, adminObject);
            }
        }
    }

    public void AssignAdmin(IPlayer player, Admin admin)
    {
        OnlineAdmins.TryAdd(player, admin);

        var resolvedPermissions = _flagsManager != null
            ? _flagsManager.GetPermissionsForFlags(admin.Flags)
            : admin.Permissions;

        foreach (var permission in resolvedPermissions)
        {
            Core.Permission.AddPermission(player.SteamID, permission);
        }

        foreach (var groupName in admin.Groups)
        {
            var groupObject = ServerGroups.AllGroups.FirstOrDefault(p => p.Value.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (groupObject.Value != null)
            {
                var groupPermissions = _flagsManager != null
                    ? _flagsManager.GetPermissionsForFlags(groupObject.Value.Flags)
                    : groupObject.Value.Permissions;

                foreach (var permission in groupPermissions)
                {
                    Core.Permission.AddPermission(player.SteamID, permission);
                }

                if (groupObject.Value.Immunity > admin.Immunity)
                {
                    admin.Immunity = groupObject.Value.Immunity;
                }
            }
        }

        var directGroup = ServerGroups.AllGroups.FirstOrDefault(p => p.Value.Name.Equals(admin.Flags, StringComparison.OrdinalIgnoreCase));
        if (directGroup.Value != null)
        {
            var groupPermissions = _flagsManager != null
                ? _flagsManager.GetPermissionsForFlags(directGroup.Value.Flags)
                : directGroup.Value.Permissions;

            foreach (var permission in groupPermissions)
            {
                Core.Permission.AddPermission(player.SteamID, permission);
            }

            if (directGroup.Value.Immunity > admin.Immunity)
            {
                admin.Immunity = directGroup.Value.Immunity;
            }
        }

        _adminsManager?.TriggerOnAdminLoad(player, admin);
    }

    public void UnassignAdmin(IPlayer player, Admin admin)
    {
        var resolvedPermissions = _flagsManager != null
            ? _flagsManager.GetPermissionsForFlags(admin.Flags)
            : admin.Permissions;

        foreach (var permission in resolvedPermissions)
        {
            Core.Permission.RemovePermission(player.SteamID, permission);
        }

        foreach (var groupName in admin.Groups)
        {
            var groupObject = ServerGroups.AllGroups.FirstOrDefault(p => p.Value.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (groupObject.Value != null)
            {
                var groupPermissions = _flagsManager != null
                    ? _flagsManager.GetPermissionsForFlags(groupObject.Value.Flags)
                    : groupObject.Value.Permissions;

                foreach (var permission in groupPermissions)
                {
                    Core.Permission.RemovePermission(player.SteamID, permission);
                }
            }
        }

        var directGroup = ServerGroups.AllGroups.FirstOrDefault(p => p.Value.Name.Equals(admin.Flags, StringComparison.OrdinalIgnoreCase));
        if (directGroup.Value != null)
        {
            var groupPermissions = _flagsManager != null
                ? _flagsManager.GetPermissionsForFlags(directGroup.Value.Flags)
                : directGroup.Value.Permissions;

            foreach (var permission in groupPermissions)
            {
                Core.Permission.RemovePermission(player.SteamID, permission);
            }
        }

        OnlineAdmins.TryRemove(player, out _);
    }
}