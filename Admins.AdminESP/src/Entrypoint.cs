using Admins.AdminESP.Commands;
using Admins.AdminESP.Menus;
using Admins.Core.Contract;
using Admins.Menu.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.AdminESP;

public partial class AdminsAdminESP : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private Admins.Core.Contract.IConfigurationManager? _configurationManager;
    private IAdminMenuAPI? _adminMenuAPI;
    private EspCommands? _espCommands;
    private AdminMenu? _adminMenu;

    public AdminsAdminESP(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<EspCommands>()
            .AddSingleton<AdminMenu>();

        _serviceProvider = services.BuildServiceProvider();
        _espCommands = _serviceProvider.GetRequiredService<EspCommands>();
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
                _espCommands!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core in Admins.AdminESP.");
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
            Core.Logger.LogError(ex, "Failed to get IAdminMenuAPI from Admins.Menu in Admins.AdminESP.");
        }
    }
}
