using Admins.Menu.Contract;
using Admins.Menu.Menu;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Admins.Menu.API;

public class AdminMenuAPI : IAdminMenuAPI
{
    public AdminMenu? _menuManager;
    public IOptionsMonitor<CoreMenuConfiguration>? _config;

    public AdminMenuAPI(AdminMenu menuManager, IOptionsMonitor<CoreMenuConfiguration> config)
    {
        _menuManager = menuManager;
        _config = config;
    }

    public void RegisterSubmenu(string translationKey, string[] permission, Func<IPlayer, string, string> getPlayerTranslationFromConsumer, Func<IPlayer, IMenuAPI> submenu)
    {
        _menuManager!.RegisterSubmenu(translationKey, permission, getPlayerTranslationFromConsumer, submenu);
    }

    public void UnregisterSubmenu(string translationKey)
    {
        _menuManager!.UnregisterSubmenu(translationKey);
    }

    public IMenuAPI CreateAdminMenu(IPlayer player)
    {
        return _menuManager!.CreateAdminMenu(player);
    }

    public Color GetMenuColor()
    {
        return _config?.CurrentValue.MenuColor ?? new Color(0, 186, 105);
    }
}