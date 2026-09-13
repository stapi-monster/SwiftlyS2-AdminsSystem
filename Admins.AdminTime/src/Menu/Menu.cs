using Admins.AdminTime.Services;
using Admins.Menu.Contract;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admins.AdminTime.Menus;

public class AdminMenu
{
    private readonly ISwiftlyCore Core;
    private IAdminMenuAPI? _adminMenuAPI;
    private readonly AdminTimeService _adminTimeService;

    public AdminMenu(ISwiftlyCore core, AdminTimeService adminTimeService)
    {
        Core = core;
        _adminTimeService = adminTimeService;
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

        _adminMenuAPI.RegisterSubmenu("menu.admintime.title", ["admins.commands.admin"], TranslateString, (player) =>
        {
            var menuBuilder = Core.MenusAPI.CreateBuilder();

            string sessionText = _adminTimeService.GetSessionFormattedTime(player);

            menuBuilder
                .Design.SetMenuTitle(TranslateString(player, "menu.admintime.title"))
                .Design.SetMenuFooterColor(_adminMenuAPI.GetMenuColor())
                .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
                .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor())
                .AddOption(new TextMenuOption(sessionText));

            return menuBuilder.Build();
        });
    }

    public void UnloadAdminMenu()
    {
        _adminMenuAPI?.UnregisterSubmenu("menu.admintime.title");
    }
}
