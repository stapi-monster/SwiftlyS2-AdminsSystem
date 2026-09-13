using Admins.Bans.Contract;
using Admins.CheckCheats.Commands;
using Admins.CheckCheats.Configuration;
using Admins.CheckCheats.Menus;
using Admins.Core.Contract;
using Admins.Menu.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.CheckCheats;

public partial class AdminsCheckCheats : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private Admins.Core.Contract.IConfigurationManager? _configurationManager;
    private IBansManager? _bansManager;
    private IAdminMenuAPI? _adminMenuAPI;
    private CheckCommands? _checkCommands;
    private AdminMenu? _adminMenu;

    public AdminsCheckCheats(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<CheckConfiguration>("config.jsonc", "Main")
            .Configure(builder =>
            {
                string pluginDir = Core.PluginPath;
                string baseDir = Directory.GetCurrentDirectory();

                string[] paths = new[]
                {
                    Path.Combine(baseDir, "configs", "plugins", "Admins.CheckCheats", "config.jsonc"),
                    Path.Combine(baseDir, "configs", "Admins.CheckCheats", "config.jsonc"),
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
            .AddSingleton<CheckCommands>()
            .AddSingleton<AdminMenu>()
            .AddOptionsWithValidateOnStart<CheckConfiguration>()
            .BindConfiguration("Main");

        _serviceProvider = services.BuildServiceProvider();
        _checkCommands = _serviceProvider.GetRequiredService<CheckCommands>();
        _adminMenu = _serviceProvider.GetRequiredService<AdminMenu>();
    }

    public override void Unload()
    {
        _adminMenu?.UnloadAdminMenu();
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Configuration.V1"))
            {
                _configurationManager = interfaceManager.GetSharedInterface<Admins.Core.Contract.IConfigurationManager>("Admins.Configuration.V1");
                _checkCommands!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core in Admins.CheckCheats.");
        }

        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Bans.V1"))
            {
                _bansManager = interfaceManager.GetSharedInterface<IBansManager>("Admins.Bans.V1");
                _checkCommands!.SetBansManager(_bansManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IBansManager from Admins.Bans in Admins.CheckCheats.");
        }

        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Menu.V1"))
            {
                _adminMenuAPI = interfaceManager.GetSharedInterface<IAdminMenuAPI>("Admins.Menu.V1");
                _adminMenu!.SetAdminMenuAPI(_adminMenuAPI);
                _adminMenu.LoadAdminMenu();
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IAdminMenuAPI from Admins.Menu in Admins.CheckCheats.");
        }
    }
}
