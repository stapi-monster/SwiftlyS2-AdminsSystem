using System.Collections.Concurrent;
using Admins.Core.Contract;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Admins.Hide.Commands;

public class HideCommands
{
    private readonly ISwiftlyCore Core;
    private IConfigurationManager? ConfigurationManager;
    private readonly ConcurrentDictionary<ulong, bool> _hiddenAdmins = new();

    public HideCommands(ISwiftlyCore core)
    {
        Core = core;
        core.Registrator.Register(this);
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        ConfigurationManager = configurationManager;
    }

    private string GetPrefix()
    {
        return ConfigurationManager?.GetCurrentConfiguration()?.Prefix ?? "[Admins]";
    }

    [EventListener<EventDelegates.OnClientDisconnected>]
    public void OnClientDisconnected(IOnClientDisconnectedEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player != null && player.IsValid)
        {
            _hiddenAdmins.TryRemove(player.SteamID, out _);
        }
    }

    [EventListener<EventDelegates.OnClientSteamAuthorize>]
    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player != null && player.IsValid)
        {
            _hiddenAdmins.TryRemove(player.SteamID, out _);
        }
    }

    [Command("jointeam")]
    public void Command_JoinTeam(ICommandContext context)
    {
        if (context.IsSentByPlayer && context.Sender != null)
        {
            var admin = context.Sender;
            if (_hiddenAdmins.TryGetValue(admin.SteamID, out var isHidden) && isHidden)
            {
                _hiddenAdmins[admin.SteamID] = false;
                var localizer = Core.Translation.GetPlayerLocalizer(admin);
                context.Reply(localizer["Hide_Off", GetPrefix()]);
            }
        }
    }

    [Command("hide", permission: "@admin/hide")]
    [CommandAlias("mm_hide")]
    public void Command_Hide(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("[Admins.Hide] This command can only be used by players.");
            return;
        }

        var admin = context.Sender!;
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        bool isCurrentlyHidden = _hiddenAdmins.TryGetValue(admin.SteamID, out var val) && val;

        if (isCurrentlyHidden)
        {
            _hiddenAdmins[admin.SteamID] = false;

            Core.Scheduler.NextTick(() =>
            {
                admin.SwitchTeam(Team.Spectator);
                context.Reply(localizer["Hide_Off", GetPrefix()]);
            });
        }
        else
        {
            _hiddenAdmins[admin.SteamID] = true;

            Core.Scheduler.NextTick(() =>
            {
                Core.Engine.ExecuteCommand("sv_disable_teamselect_menu 1");

                if (admin.PlayerPawn != null && admin.PlayerPawn.IsValid && admin.PlayerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    admin.PlayerPawn.CommitSuicide(false, true);
                }

                admin.SwitchTeam(Team.Spectator);

                Core.Scheduler.DelayBySeconds(0.1f, () =>
                {
                    Core.Engine.ExecuteCommand("sv_disable_teamselect_menu 0");
                });

                context.Reply(localizer["Hide_On", GetPrefix()]);
            });
        }

        Core.Logger.LogInformation($"[Admins.Hide] Admin '{admin.Controller.PlayerName}' toggled hide mode: {!isCurrentlyHidden}");
    }
}
