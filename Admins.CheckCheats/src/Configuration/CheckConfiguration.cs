namespace Admins.CheckCheats.Configuration;

public class CheckConfiguration
{
    public int ServerId { get; set; } = 1;
    public int DbUse { get; set; } = 1;
    public int AutoMove { get; set; } = 1;
    public string Commands { get; set; } = "!check|!cheats";
    public string ContactCommand { get; set; } = "!contact";
    public TimerConfig Timer { get; set; } = new();
    public LeaveConfig Leave { get; set; } = new();
    public Dictionary<string, ReasonConfig> Reasons { get; set; } = new();
    public Dictionary<string, SocialConfig> Socials { get; set; } = new();
}

public class TimerConfig
{
    public int Time { get; set; } = 180;
    public int Min { get; set; } = 0;
    public int Max { get; set; } = 2;
    public int AutoBanStatus { get; set; } = 1;
    public string AutoBanReason { get; set; } = "3";
}

public class LeaveConfig
{
    public int PlayerBanStatus { get; set; } = 1;
    public string Reason { get; set; } = "Покинул игру во время проверки";
    public string Reason2 { get; set; } = "Выход из игры во время проверки на читы";
    public int Time { get; set; } = 0;
}

public class ReasonConfig
{
    public string Reason { get; set; } = string.Empty;
    public string Reason2 { get; set; } = string.Empty;
    public int Time { get; set; } = -1;
    public int Show { get; set; } = 1;
}

public class SocialConfig
{
    public int Min { get; set; } = 2;
    public int Max { get; set; } = 64;
    public string Example { get; set; } = string.Empty;
}
