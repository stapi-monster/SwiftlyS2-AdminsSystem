using Admins.Core.Contract;
using Admins.Menu.Contract;
using Admins.SuperCommands.Commands;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Admins.SuperCommands.Menus;

public class AdminMenu
{
    private ISwiftlyCore Core = null!;
    private IAdminMenuAPI? _adminMenuAPI;
    private ServerCommands _serverCommands;

    public AdminMenu(ISwiftlyCore core, ServerCommands serverCommands)
    {
        Core = core;
        _serverCommands = serverCommands;
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

        _adminMenuAPI.RegisterSubmenu("menu.supercommands.players.title", ["admins.commands.admin"], TranslateString, (player) =>
        {
            var menuBuilder = Core.MenusAPI.CreateBuilder();

            menuBuilder
                .Design.SetMenuTitle(TranslateString(player, "menu.supercommands.players.title"))
                .Design.SetMenuFooterColor(_adminMenuAPI.GetMenuColor())
                .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
                .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor())
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.slay"),
                    () => BuildPlayerTargetMenu(player, "slay", (admin, target) => ExecuteSlay(admin, target))
                ))
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.slap"),
                    () => BuildPlayerTargetMenu(player, "slap", (admin, target) => ExecuteSlap(admin, target))
                ))
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.kick"),
                    () => BuildPlayerTargetMenu(player, "kick", (admin, target) => ExecuteKick(admin, target))
                ))
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.respawn"),
                    () => BuildPlayerTargetMenu(player, "respawn", (admin, target) => ExecuteRespawn(admin, target))
                ))
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.noclip"),
                    () => BuildPlayerTargetMenu(player, "noclip", (admin, target) => ExecuteNoclip(admin, target))
                ))
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.supercommands.god"),
                    () => BuildPlayerTargetMenu(player, "god", (admin, target) => ExecuteGod(admin, target))
                ));

            return menuBuilder.Build();
        });
    }

    public void UnloadAdminMenu()
    {
        _adminMenuAPI?.UnregisterSubmenu("menu.supercommands.players.title");
    }

    private IMenuAPI BuildPlayerTargetMenu(IPlayer admin, string actionName, Action<IPlayer, IPlayer> onSelect)
    {
        var menuBuilder = Core.MenusAPI.CreateBuilder();
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        menuBuilder
            .Design.SetMenuTitle($"{localizer["menu.supercommands.players.title"]} -> {actionName}")
            .Design.SetMenuFooterColor(_adminMenuAPI!.GetMenuColor())
            .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
            .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor());

        var players = Core.PlayerManager.GetAllValidPlayers().Where(p => p.IsValid && !p.IsFakeClient);

        foreach (var target in players)
        {
            var button = new ButtonMenuOption(target.Controller.PlayerName) { CloseAfterClick = true };
            button.Click += (_, args) =>
            {
                Core.Scheduler.NextTick(() =>
                {
                    onSelect(admin, target);
                });
                return ValueTask.CompletedTask;
            };
            menuBuilder.AddOption(button);
        }

        return menuBuilder.Build();
    }

    private void ExecuteSlay(IPlayer admin, IPlayer target)
    {
        if (target.PlayerPawn != null && target.PlayerPawn.IsValid && target.PlayerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            target.PlayerPawn.CommitSuicide(false, true);
        }
    }

    private void ExecuteSlap(IPlayer admin, IPlayer target)
    {
        if (target.PlayerPawn != null && target.PlayerPawn.IsValid && target.PlayerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            target.PlayerPawn.Health = Math.Max(1, target.PlayerPawn.Health - 5);
        }
    }

    private void ExecuteKick(IPlayer admin, IPlayer target)
    {
        target.KickAsync("Kicked by admin", ENetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED);
    }

    private void ExecuteRespawn(IPlayer admin, IPlayer target)
    {
        if (target.Controller != null && target.Controller.IsValid)
        {
            target.Controller.Respawn();
        }
    }

    private void ExecuteNoclip(IPlayer admin, IPlayer target)
    {
        if (target.PlayerPawn != null && target.PlayerPawn.IsValid)
        {
            var moveType = target.PlayerPawn.MoveType == MoveType_t.MOVETYPE_NOCLIP 
                ? MoveType_t.MOVETYPE_WALK 
                : MoveType_t.MOVETYPE_NOCLIP;
            target.PlayerPawn.ActualMoveType = moveType;
            target.PlayerPawn.MoveType = moveType;
            target.PlayerPawn.MoveTypeUpdated();
        }
    }

    private void ExecuteGod(IPlayer admin, IPlayer target)
    {
        if (target.PlayerPawn != null && target.PlayerPawn.IsValid)
        {
            target.PlayerPawn.TakesDamage = !target.PlayerPawn.TakesDamage;
            target.PlayerPawn.TakesDamageUpdated();
        }
    }
}
