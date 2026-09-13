using System.Collections.Concurrent;
using Admins.Core.Contract;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace Admins.AdminESP.Commands;

public class EspCommands
{
    private readonly ISwiftlyCore Core;
    private IConfigurationManager? ConfigurationManager;
    private readonly ConcurrentDictionary<ulong, bool> _espAdmins = new();

    public EspCommands(ISwiftlyCore core)
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

    // ESP Glow / Highlight Color strictly RED (R:255, G:0, B:0, A:255)
    public static readonly (byte R, byte G, byte B, byte A) EspColorRed = (255, 0, 0, 255);

    [EventListener<EventDelegates.OnClientDisconnected>]
    public void OnClientDisconnected(IOnClientDisconnectedEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player != null && player.IsValid)
        {
            _espAdmins.TryRemove(player.SteamID, out _);
        }
    }

    [EventListener<EventDelegates.OnClientSteamAuthorize>]
    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player != null && player.IsValid)
        {
            _espAdmins.TryRemove(player.SteamID, out _);
        }
    }

    public bool IsAdminEspEnabled(ulong steamId)
    {
        var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p != null && p.IsValid && p.SteamID == steamId);
        if (player != null)
        {
            return IsAdminEspEnabled(player);
        }
        return _espAdmins.TryGetValue(steamId, out var enabled) && enabled;
    }

    public bool IsAdminEspEnabled(IPlayer player)
    {
        if (player == null || !player.IsValid) return false;

        // TeamNum 1 = Spectator
        if (player.Controller == null || player.Controller.TeamNum != 1)
        {
            if (_espAdmins.TryRemove(player.SteamID, out _))
            {
                var localizer = Core.Translation.GetPlayerLocalizer(player);
                player.SendChat(localizer["ESP_Disabled_TeamChange", GetPrefix()]);
            }
            return false;
        }

        return _espAdmins.TryGetValue(player.SteamID, out var enabled) && enabled;
    }

    public bool ToggleEsp(IPlayer admin)
    {
        bool isSpectator = admin.Controller != null && admin.Controller.TeamNum == 1;
        if (!isSpectator)
        {
            _espAdmins.TryRemove(admin.SteamID, out _);
            return false;
        }

        bool newStatus = !IsAdminEspEnabled(admin);
        _espAdmins[admin.SteamID] = newStatus;
        return newStatus;
    }

    [Command("esp", permission: "@admin/esp")]
    [CommandAlias("adminesp")]
    [CommandAlias("sw_esp")]
    public void Command_Esp(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("[Admins.AdminESP] This command can only be used by players.");
            return;
        }

        var admin = context.Sender!;
        var localizer = Core.Translation.GetPlayerLocalizer(admin);

        bool isSpectator = admin.Controller != null && admin.Controller.TeamNum == 1;
        if (!isSpectator)
        {
            context.Reply(localizer["ESP_OnlySpectator", GetPrefix()]);
            return;
        }

        bool enabled = ToggleEsp(admin);

        if (enabled)
        {
            context.Reply(localizer["ESP_Enabled", GetPrefix()]);
        }
        else
        {
            context.Reply(localizer["ESP_Disabled", GetPrefix()]);
        }

        Core.Logger.LogInformation($"[Admins.AdminESP] Spectator Admin '{admin.Controller?.PlayerName}' toggled Admin ESP: {enabled}");
    }
}
