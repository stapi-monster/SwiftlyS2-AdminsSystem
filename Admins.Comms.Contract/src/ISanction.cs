namespace Admins.Comms.Contract;

public enum SanctionType
{
    SteamID = 1,
    IP = 2
}

public enum SanctionKind
{
    Gag = 1,
    Mute = 2
}

public interface ISanction
{
    long Id { get; set; }
    long SteamId64 { get; set; }
    string PlayerName { get; set; }
    string PlayerIp { get; set; }
    SanctionType SanctionType { get; set; }
    SanctionKind SanctionKind { get; set; }
    long ExpiresAt { get; set; }
    long Length { get; set; }
    string Reason { get; set; }
    long AdminSteamId64 { get; set; }
    string AdminName { get; set; }
    string Server { get; set; }
    bool GlobalSanction { get; set; }
    long CreatedAt { get; set; }
    long UpdatedAt { get; set; }
}