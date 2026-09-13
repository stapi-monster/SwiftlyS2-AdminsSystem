using Admins.Core.Contract;
using Admins.Rcon.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.Rcon;

public partial class AdminsRcon : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private IConfigurationManager? _configurationManager;
    private RconCommands? _rconCommands;

    public AdminsRcon(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<RconCommands>();

        _serviceProvider = services.BuildServiceProvider();
        _rconCommands = _serviceProvider.GetRequiredService<RconCommands>();
    }

    public override void Unload()
    {
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        try
        {
            if (interfaceManager.HasSharedInterface("Admins.Configuration.V1"))
            {
                _configurationManager = interfaceManager.GetSharedInterface<IConfigurationManager>("Admins.Configuration.V1");
                _rconCommands!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core in Admins.Rcon.");
        }
    }
}
