using System.Collections.Concurrent;
using Admins.Core.Admins;
using Admins.Core.Config;
using Admins.Core.Database.Models;
using Dommel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace Admins.Core.Groups;

public partial class ServerGroups
{
    private ISwiftlyCore Core = null!;

    private ServerAdmins _admins = null!;

    private IOptionsMonitor<CoreConfiguration>? _config;

    public static ConcurrentDictionary<long, Group> AllGroups { get; set; } = [];

    public ServerGroups(ServerAdmins admins, ISwiftlyCore core, IOptionsMonitor<CoreConfiguration> config)
    {
        core.Registrator.Register(this);
        _admins = admins;
        _config = config;
        Core = core;
    }

    public void Load()
    {
        Task.Run(async () =>
        {
            foreach (var (player, admin) in ServerAdmins.OnlineAdmins)
            {
                _admins.UnassignAdmin(player, admin);
            }

            if (_config!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admin_system");
                var groups = await db.GetAllAsync<Group>();
                AllGroups = new ConcurrentDictionary<long, Group>(groups.ToDictionary(g => g.Id, g => g));
            }

            _admins.Load();
        });
    }
}