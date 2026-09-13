using Admins.Core.Contract;

namespace Admins.Core.Config;

public class CoreConfiguration : ICoreConfiguration
{
    public string Prefix { get; set; } = "[RED][Admins][DEFAULT]";
    public bool UseDatabase { get; set; } = true;
    public string TimeZone { get; set; } = "UTC";
    public float AdminsDatabaseSyncIntervalSeconds { get; set; } = 60f;
    public float BansDatabaseSyncIntervalSeconds { get; set; } = 30f;
    public float SanctionsDatabaseSyncIntervalSeconds { get; set; } = 30f;

    public string DatabasePrefix { get; set; } = "as_";
    public int AdminServerId { get; set; } = 1;
    public int PunishServerId { get; set; } = 1;
    public int PunishIp { get; set; } = 1;
    public int NotifyType { get; set; } = 1;
    public int ImmunityType { get; set; } = 1;
    public int BanDelay { get; set; } = 5;
    public int UnpunishType { get; set; } = 0;
    public int PunishOfflineCount { get; set; } = 30;
    public int UnpunishOfflineCount { get; set; } = 30;
    public int MessageType { get; set; } = 1;
    public int OwnReason { get; set; } = 1;

    public ImmunityMode ImmunityMode
    {
        get => (ImmunityMode)ImmunityType;
        set => ImmunityType = (int)value;
    }
}