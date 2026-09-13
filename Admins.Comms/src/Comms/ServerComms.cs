using System.Collections.Concurrent;
using Admins.Comms.Contract;
using Admins.Comms.Database.Models;
using Admins.Core.Contract;
using Dommel;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Admins.Comms.Manager;

public class ServerComms
{
    private ISwiftlyCore Core = null!;
    private IConfigurationManager _configurationManager = null!;
    private IServerManager _serverManager = null!;

    public static ConcurrentDictionary<ulong, List<ISanction>> OnlinePlayerSanctions { get; set; } = [];

    public ServerComms(ISwiftlyCore core)
    {
        Core = core;
    }

    public void SetServerManager(IServerManager serverManager)
    {
        _serverManager = serverManager;
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public async Task LoadPlayerSanctionsAsync(ulong steamId, string playerIp)
    {
        var config = _configurationManager.GetConfigurationMonitor()!.CurrentValue;
        if (config.UseDatabase == false)
            return;

        try
        {
            var db = Core.Database.GetConnection("admin_system");
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int punishServerId = config.PunishServerId;
            var steamId64 = (long)steamId;

            var steamIdSanctions = await db.SelectAsync<Sanction>(s =>
                s.SteamId64 == steamId64 &&
                s.SanctionType == SanctionType.SteamID
            );

            var ipSanctions = (config.PunishIp == 1 && !string.IsNullOrEmpty(playerIp))
                ? await db.SelectAsync<Sanction>(s =>
                    s.PlayerIp == playerIp &&
                    s.SanctionType == SanctionType.IP
                )
                : [];

            var activeSanctions = steamIdSanctions.Concat(ipSanctions)
                .Where(s =>
                    (s.ExpiresAt == 0 || s.ExpiresAt > currentTime) &&
                    (punishServerId == -1 || s.Server == punishServerId.ToString() || s.GlobalSanction)
                )
                .Cast<ISanction>()
                .ToList();

            OnlinePlayerSanctions[steamId] = activeSanctions;
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"[Comms] Error loading sanctions for SteamID {steamId}: {ex.Message}");
        }
    }

    public void UnloadPlayer(ulong steamId)
    {
        OnlinePlayerSanctions.TryRemove(steamId, out _);
    }

    public void AddToOnlineCache(ulong steamId, ISanction sanction)
    {
        OnlinePlayerSanctions.AddOrUpdate(
            steamId,
            [sanction],
            (_, existing) =>
            {
                existing.Add(sanction);
                return existing;
            }
        );
    }

    public void RemoveFromOnlineCache(ulong steamId, long sanctionId)
    {
        if (OnlinePlayerSanctions.TryGetValue(steamId, out var sanctions))
        {
            sanctions.RemoveAll(s => s.Id == sanctionId);
        }
    }

    public async Task RefreshOnlinePlayerSanctionsAsync()
    {
        var players = Core.PlayerManager.GetAllValidPlayers();
        foreach (var player in players)
        {
            if (player.IsFakeClient)
                continue;

            await LoadPlayerSanctionsAsync(player.SteamID, player.IPAddress ?? "");
        }
    }

    public ISanction? FindActiveSanction(long steamId64, string playerIp, SanctionKind sanctionKind)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (OnlinePlayerSanctions.TryGetValue((ulong)steamId64, out var sanctions))
        {
            return sanctions.FirstOrDefault(s =>
                s.SanctionKind == sanctionKind &&
                (s.ExpiresAt == 0 || s.ExpiresAt > currentTime)
            );
        }

        return null;
    }
}
