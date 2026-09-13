using Admins.Menu.API;
using Admins.Menu.Contract;
using Admins.Menu.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Plugins;

namespace Admins.Menu;

public partial class AdminsMenu : BasePlugin
{
    private ServiceProvider? _serviceProvider;

    public AdminsMenu(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<CoreMenuConfiguration>("config.jsonc", "Main")
            .Configure(builder =>
            {
                string pluginDir = Core.PluginPath;
                string baseDir = Directory.GetCurrentDirectory();

                string[] paths = new[]
                {
                    Path.Combine(baseDir, "configs", "plugins", "Admins.Menu", "config.jsonc"),
                    Path.Combine(baseDir, "configs", "Admins.Menu", "config.jsonc"),
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
            .AddSingleton<AdminMenu>()
            .AddSingleton<AdminMenuAPI>()
            .AddOptionsWithValidateOnStart<CoreMenuConfiguration>()
            .BindConfiguration("Main");

        _serviceProvider = services.BuildServiceProvider();

        var adminMenu = _serviceProvider.GetRequiredService<AdminMenu>();
        var adminMenuAPI = _serviceProvider.GetRequiredService<AdminMenuAPI>();
    }

    public override void Unload()
    {
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IAdminMenuAPI, AdminMenuAPI>("Admins.Menu.V1", _serviceProvider!.GetRequiredService<AdminMenuAPI>());
    }

    [Command("admin", permission: "admins.commands.admin")]
    public void OpenAdminMenuCommand(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var sender = context.Sender!;

        var adminMenu = _serviceProvider!.GetRequiredService<AdminMenu>();
        var menu = adminMenu.CreateAdminMenu(sender);

        Core.MenusAPI.OpenMenuForPlayer(sender, menu);
    }
}