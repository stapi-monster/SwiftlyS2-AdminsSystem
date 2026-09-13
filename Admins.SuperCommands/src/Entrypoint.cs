using Admins.Core.Contract;
using Admins.Menu.Contract;
using Admins.SuperCommands.Commands;
using Admins.SuperCommands.Menus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.SuperCommands;

public partial class AdminsSuperCommands : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private IConfigurationManager? _configurationManager;
    private IAdminsManager? _adminsManager;
    private IGroupsManager? _groupsManager;
    private IAdminMenuAPI? _adminMenuAPI;
    private ServerCommands? _serverCommands;
    private AdminMenu? _adminMenu;

    public AdminsSuperCommands(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<ServerCommands>()
            .AddSingleton<AdminMenu>();

        _serviceProvider = services.BuildServiceProvider();

        _serverCommands = _serviceProvider.GetRequiredService<ServerCommands>();
        _adminMenu = _serviceProvider.GetRequiredService<AdminMenu>();
    }

    public override void Unload()
    {
        _adminMenu?.UnloadAdminMenu();
        _serverCommands?._beaconPlayers.Clear();
        foreach (var token in _serverCommands?._beaconEffectTimerToken.Values ?? Enumerable.Empty<CancellationTokenSource>())
        {
            token.Cancel();
        }
        _serverCommands?._beaconEffectTimerToken.Clear();
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Configuration.V1"))
            {
                _configurationManager = interfaceManager.GetSharedInterface<Core.Contract.IConfigurationManager>("Admins.Configuration.V1");
                _serverCommands!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core.");
        }

        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Admins.V1"))
            {
                _adminsManager = interfaceManager.GetSharedInterface<IAdminsManager>("Admins.Admins.V1");
                _serverCommands!.SetAdminsManager(_adminsManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IAdminsManager from Admins.Core.");
        }

        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Groups.V1"))
            {
                _groupsManager = interfaceManager.GetSharedInterface<IGroupsManager>("Admins.Groups.V1");
                _serverCommands!.SetGroupsManager(_groupsManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IGroupsManager from Admins.Core.");
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
            Core.Logger.LogError(ex, "Failed to get IAdminMenuAPI from Admins.Menu.");
        }
    }
}