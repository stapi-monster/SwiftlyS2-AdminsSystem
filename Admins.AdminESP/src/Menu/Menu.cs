using Admins.AdminESP.Commands;
using Admins.Menu.Contract;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admins.AdminESP.Menus;

public class AdminMenu
{
    private readonly ISwiftlyCore Core;
    private IAdminMenuAPI? _adminMenuAPI;
    private readonly EspCommands _espCommands;

    public AdminMenu(ISwiftlyCore core, EspCommands espCommands)
    {
        Core = core;
        _espCommands = espCommands;
        core.Registrator.Register(this);
    }

    public void SetAdminMenuAPI(IAdminMenuAPI adminMenuAPI)
    {
        _adminMenuAPI = adminMenuAPI;
    }

    public string TranslateString(IPlayer player, string key)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);
        return localizer[key];
    }

    public void LoadAdminMenu()
    {
        if (_adminMenuAPI == null) return;

        _adminMenuAPI.RegisterSubmenu("menu.adminesp.title", ["@admin/esp", "admins.commands.esp"], TranslateString, (player) =>
        {
            var menuBuilder = Core.MenusAPI.CreateBuilder();
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            bool isEnabled = _espCommands.IsAdminEspEnabled(player.SteamID);

            string statusText = isEnabled ? "Включен" : "Выключен";
            var button = new ButtonMenuOption($"Режим ESP: {statusText}") { CloseAfterClick = true };
            button.Click += (_, args) =>
            {
                Core.Scheduler.NextTick(() =>
                {
                    _espCommands.Command_Esp(new FakeCommandContext(player));
                });
                return ValueTask.CompletedTask;
            };

            menuBuilder
                .Design.SetMenuTitle(TranslateString(player, "menu.adminesp.title"))
                .Design.SetMenuFooterColor(_adminMenuAPI.GetMenuColor())
                .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
                .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor())
                .AddOption(button);

            return menuBuilder.Build();
        });
    }

    public void UnloadAdminMenu()
    {
        _adminMenuAPI?.UnregisterSubmenu("menu.adminesp.title");
    }
}

public class FakeCommandContext : SwiftlyS2.Shared.Commands.ICommandContext
{
    public IPlayer Sender { get; }
    public bool IsSentByPlayer => true;
    public string CommandName => "esp";
    public string Prefix => "!";
    public bool IsSlient => false;
    public string[] Args => Array.Empty<string>();

    public FakeCommandContext(IPlayer sender)
    {
        Sender = sender;
    }

    public void Reply(string message)
    {
        Sender.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, message);
    }

    public Task ReplyAsync(string message)
    {
        Reply(message);
        return Task.CompletedTask;
    }
}
