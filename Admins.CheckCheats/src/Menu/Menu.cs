using Admins.CheckCheats.Commands;
using Admins.Core.Contract;
using Admins.Menu.Contract;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admins.CheckCheats.Menus;

public class AdminMenu
{
    private readonly ISwiftlyCore Core;
    private IAdminMenuAPI? _adminMenuAPI;
    private readonly CheckCommands _checkCommands;

    public AdminMenu(ISwiftlyCore core, CheckCommands checkCommands)
    {
        Core = core;
        _checkCommands = checkCommands;
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

        _adminMenuAPI.RegisterSubmenu("menu.checkcheats.title", ["@admin/check", "admins.commands.check"], TranslateString, (player) =>
        {
            var menuBuilder = Core.MenusAPI.CreateBuilder();
            var activeSession = _checkCommands.GetActiveCheckForAdmin(player.SteamID);

            menuBuilder
                .Design.SetMenuTitle(TranslateString(player, "menu.checkcheats.title"))
                .Design.SetMenuFooterColor(_adminMenuAPI.GetMenuColor())
                .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
                .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor());

            if (activeSession != null)
            {
                menuBuilder.AddOption(new SubmenuMenuOption(
                    $"{TranslateString(player, "CC_Active_Check")}: {activeSession.SuspectName}",
                    () => BuildActiveCheckMenu(player, activeSession)
                ));
            }

            menuBuilder.AddOption(new SubmenuMenuOption(
                TranslateString(player, "CC_Select_Player"),
                () => BuildPlayerTargetMenu(player)
            ));

            return menuBuilder.Build();
        });
    }

    public void UnloadAdminMenu()
    {
        _adminMenuAPI?.UnregisterSubmenu("menu.checkcheats.title");
    }

    private IMenuAPI BuildActiveCheckMenu(IPlayer admin, CheckSession session)
    {
        var menuBuilder = Core.MenusAPI.CreateBuilder();
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        string contactText = string.IsNullOrWhiteSpace(session.ContactInfo) ? "Не указан" : session.ContactInfo;

        menuBuilder
            .Design.SetMenuTitle($"Проверка: {session.SuspectName} ({contactText})")
            .Design.SetMenuFooterColor(_adminMenuAPI!.GetMenuColor())
            .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
            .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor());

        if (session.Stage < 3)
        {
            var btnStart = new ButtonMenuOption(localizer["CC_Menu_StartCheck"]) { CloseAfterClick = true };
            btnStart.Click += (_, args) =>
            {
                Core.Scheduler.NextTick(() => _checkCommands.MarkStartCheck(admin));
                return ValueTask.CompletedTask;
            };
            menuBuilder.AddOption(btnStart);
        }

        var btnClean = new ButtonMenuOption(localizer["CC_Menu_Clean"]) { CloseAfterClick = true };
        btnClean.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(() => _checkCommands.EndCheck(admin, "2"));
            return ValueTask.CompletedTask;
        };
        menuBuilder.AddOption(btnClean);

        var btnCheats = new ButtonMenuOption(localizer["CC_Menu_Cheats"]) { CloseAfterClick = true };
        btnCheats.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(() => _checkCommands.EndCheck(admin, "1"));
            return ValueTask.CompletedTask;
        };
        menuBuilder.AddOption(btnCheats);

        var btnRefusal = new ButtonMenuOption(localizer["CC_Menu_Refusal"]) { CloseAfterClick = true };
        btnRefusal.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(() => _checkCommands.EndCheck(admin, "0"));
            return ValueTask.CompletedTask;
        };
        menuBuilder.AddOption(btnRefusal);

        var btnMissclick = new ButtonMenuOption(localizer["CC_Menu_Missclick"]) { CloseAfterClick = true };
        btnMissclick.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(() => _checkCommands.EndCheck(admin, "4"));
            return ValueTask.CompletedTask;
        };
        menuBuilder.AddOption(btnMissclick);

        return menuBuilder.Build();
    }

    private IMenuAPI BuildPlayerTargetMenu(IPlayer admin)
    {
        var menuBuilder = Core.MenusAPI.CreateBuilder();
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        menuBuilder
            .Design.SetMenuTitle($"{localizer["menu.checkcheats.title"]}")
            .Design.SetMenuFooterColor(_adminMenuAPI!.GetMenuColor())
            .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
            .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor());

        var players = Core.PlayerManager.GetAllValidPlayers().Where(p => p.IsValid && !p.IsFakeClient && p.SteamID != admin.SteamID);

        foreach (var target in players)
        {
            var button = new ButtonMenuOption(target.Controller.PlayerName) { CloseAfterClick = true };
            button.Click += (_, args) =>
            {
                Core.Scheduler.NextTick(() =>
                {
                    _checkCommands.StartCheck(admin, target);
                });
                return ValueTask.CompletedTask;
            };
            menuBuilder.AddOption(button);
        }

        return menuBuilder.Build();
    }
}
