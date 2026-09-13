using Admins.Core.Admins;
using Admins.Core.Commands;
using Admins.Core.Config;
using Admins.Core.Contract;
using Admins.Core.Database;
using Admins.Core.Flags;
using Admins.Core.Groups;
using Admins.Core.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.Core;

public partial class AdminsCore : BasePlugin
{
    private ServiceProvider? _serviceProvider;

    public AdminsCore(ISwiftlyCore core) : base(core)
    {
        var connection = Core.Database.GetConnection("admin_system");
        MigrationRunner.RunMigrations(connection);
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<CoreConfiguration>("config.jsonc", "Main")
            .Configure(builder =>
            {
                string pluginDir = Core.PluginPath;
                string baseDir = Directory.GetCurrentDirectory();

                string[] paths = new[]
                {
                    Path.Combine(baseDir, "configs", "plugins", "Admins.Core", "config.jsonc"),
                    Path.Combine(baseDir, "configs", "Admins.Core", "config.jsonc"),
                    Path.Combine(pluginDir, "configs", "config.jsonc"),
                    Path.Combine(pluginDir, "resources", "config.jsonc"),
                    Path.Combine(pluginDir, "config.jsonc")
                };

                string? validPath = paths.FirstOrDefault(File.Exists);
                if (validPath != null)
                {
                    builder.AddJsonFile(validPath, optional: false, reloadOnChange: true);
                }
                else
                {
                    builder.AddJsonFile("config.jsonc", optional: true, reloadOnChange: true);
                }
            });

        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<FlagsManager>()
            .AddSingleton<ServerLoader>()
            .AddSingleton<ServerAdmins>()
            .AddSingleton<ServerGroups>()
            .AddSingleton<ServerManager>()
            .AddSingleton<GroupsManager>()
            .AddSingleton<AdminsManager>()
            .AddSingleton<ServerCommands>()
            .AddSingleton<Config.ConfigurationManager>()
            .AddSingleton<Contract.IConfigurationManager>(sp => sp.GetRequiredService<Config.ConfigurationManager>())
            .AddSingleton<GamePlayer.GamePlayer>()
            .AddOptionsWithValidateOnStart<CoreConfiguration>()
            .BindConfiguration("Main");

        _serviceProvider = services.BuildServiceProvider();
        var serverLoader = _serviceProvider.GetRequiredService<ServerLoader>();
        var serverAdmins = _serviceProvider.GetRequiredService<ServerAdmins>();
        var serverGroups = _serviceProvider.GetRequiredService<ServerGroups>();
        var serverManager = _serviceProvider.GetRequiredService<ServerManager>();
        var groupsManager = _serviceProvider.GetRequiredService<GroupsManager>();
        var _adminsManager = _serviceProvider.GetRequiredService<AdminsManager>();
        _ = _serviceProvider.GetRequiredService<ServerCommands>();
        var gamePlayer = _serviceProvider.GetRequiredService<GamePlayer.GamePlayer>();
        var configurationManager = _serviceProvider.GetRequiredService<Config.ConfigurationManager>();

        serverAdmins.SetAdminsManager(_adminsManager);
        _adminsManager.SetServerAdmins(serverAdmins);
        gamePlayer.SetServerAdmins(serverAdmins);

        serverGroups.Load();
        _adminsManager.StartSyncTimer();
    }

    public override void Unload()
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IAdminsManager, AdminsManager>("Admins.Admins.V1", _serviceProvider!.GetRequiredService<AdminsManager>());
        interfaceManager.AddSharedInterface<IGroupsManager, GroupsManager>("Admins.Groups.V1", _serviceProvider!.GetRequiredService<GroupsManager>());
        interfaceManager.AddSharedInterface<IServerManager, ServerManager>("Admins.Server.V1", _serviceProvider!.GetRequiredService<ServerManager>());
        interfaceManager.AddSharedInterface<Contract.IConfigurationManager, Config.ConfigurationManager>("Admins.Configuration.V1", _serviceProvider!.GetRequiredService<Config.ConfigurationManager>());
        interfaceManager.AddSharedInterface<IGamePlayer, GamePlayer.GamePlayer>("Admins.GamePlayer.V1", _serviceProvider!.GetRequiredService<GamePlayer.GamePlayer>());
    }
}