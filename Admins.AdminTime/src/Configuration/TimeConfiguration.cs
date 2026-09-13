namespace Admins.AdminTime.Configuration;

public class TimeConfiguration
{
    public int ServerId { get; set; } = 1;
    public bool ResetUnfinishedSessions { get; set; } = true;
    public bool CountSpectatorTime { get; set; } = true;
}
