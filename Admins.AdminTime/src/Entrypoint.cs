using Admins.AdminTime.Configuration;
using Admins.AdminTime.Menus;
using Admins.AdminTime.Services;
using Admins.Core.Contract;
using Admins.Menu.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.AdminTime;

public partial class AdminsAdminTime : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private Admins.Core.Contract.IConfigurationManager? _configurationManager;
    private IAdminsManager? _adminsManager;
    private IAdminMenuAPI? _adminMenuAPI;
    private AdminTimeService? _adminTimeService;
    private AdminMenu? _adminMenu;

    public AdminsAdminTime(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<TimeConfiguration>("config.jsonc", "Main")
            .Configure(builder =>
            {
                string pluginDir = Core.PluginPath;
                string baseDir = Directory.GetCurrentDirectory();

                string[] paths = new[]
                {
                    Path.Combine(baseDir, "configs", "plugins", "Admins.AdminTime", "config.jsonc"),
                    Path.Combine(baseDir, "configs", "Admins.AdminTime", "config.jsonc"),
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
            .AddSingleton<AdminTimeService>()
            .AddSingleton<AdminMenu>()
            .AddOptionsWithValidateOnStart<TimeConfiguration>()
            .BindConfiguration("Main");

        _serviceProvider = services.BuildServiceProvider();
        _adminTimeService = _serviceProvider.GetRequiredService<AdminTimeService>();
        _adminMenu = _serviceProvider.GetRequiredService<AdminMenu>();

        _ = _adminTimeService.InitializeDatabaseAsync();
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
                _adminTimeService!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core in Admins.AdminTime.");
        }

        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Admins.V1"))
            {
                _adminsManager = interfaceManager.GetSharedInterface<IAdminsManager>("Admins.Admins.V1");
                _adminTimeService!.SetAdminsManager(_adminsManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IAdminsManager from Admins.Core in Admins.AdminTime.");
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
            Core.Logger.LogError(ex, "Failed to get IAdminMenuAPI from Admins.Menu in Admins.AdminTime.");
        }
    }
}
