namespace Admins.Core.Contract;

public interface ICoreConfiguration
{
    public string Prefix { get; set; }
    public bool UseDatabase { get; set; }
    public string TimeZone { get; set; }
    public float AdminsDatabaseSyncIntervalSeconds { get; set; }
    public float BansDatabaseSyncIntervalSeconds { get; set; }
    public float SanctionsDatabaseSyncIntervalSeconds { get; set; }

    public string DatabasePrefix { get; set; }
    public int AdminServerId { get; set; }
    public int PunishServerId { get; set; }
    public int PunishIp { get; set; }
    public int NotifyType { get; set; }
    public int ImmunityType { get; set; }
    public int BanDelay { get; set; }
    public int UnpunishType { get; set; }
    public int PunishOfflineCount { get; set; }
    public int UnpunishOfflineCount { get; set; }
    public int MessageType { get; set; }
    public int OwnReason { get; set; }

    public ImmunityMode ImmunityMode { get; set; }
}