using Admins.Core.Contract;
using Admins.Hide.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Admins.Hide;

public partial class AdminsHide : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private IConfigurationManager? _configurationManager;
    private HideCommands? _hideCommands;

    public AdminsHide(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<HideCommands>();

        _serviceProvider = services.BuildServiceProvider();
        _hideCommands = _serviceProvider.GetRequiredService<HideCommands>();
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
                _hideCommands!.SetConfigurationManager(_configurationManager);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "Failed to get IConfigurationManager from Admins.Core in Admins.Hide.");
        }
    }
}
