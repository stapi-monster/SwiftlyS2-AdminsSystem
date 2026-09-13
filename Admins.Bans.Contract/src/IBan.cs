namespace Admins.Bans.Contract;

public enum BanType
{
    SteamID = 1,
    IP = 2
}

public interface IBan
{
    long Id { get; set; }
    long SteamId64 { get; set; }
    string PlayerName { get; set; }
    string PlayerIp { get; set; }
    BanType BanType { get; set; }
    long ExpiresAt { get; set; }
    long Length { get; set; }
    string Reason { get; set; }
    long AdminSteamId64 { get; set; }
    string AdminName { get; set; }
    string Server { get; set; }
    bool GlobalBan { get; set; }
    long CreatedAt { get; set; }
    long UpdatedAt { get; set; }
}