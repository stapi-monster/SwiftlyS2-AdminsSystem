using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Admins.Comms.Contract;

namespace Admins.Comms.Database.Models;

[Table("as_mutes")]
public class Sanction : ISanction
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("steamid")]
    public long SteamId64 { get; set; }

    [Column("name")]
    public string PlayerName { get; set; } = string.Empty;

    [Column("ip")]
    public string PlayerIp { get; set; } = string.Empty;

    [Column("sanction_type")]
    public SanctionType SanctionType { get; set; }

    [Column("type")]
    public SanctionKind SanctionKind { get; set; }

    [Column("ends")]
    public long ExpiresAt { get; set; }

    [Column("duration")]
    public long Length { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("admin_steamid")]
    public long AdminSteamId64 { get; set; }

    [Column("admin_name")]
    public string AdminName { get; set; } = string.Empty;

    [Column("server")]
    public string Server { get; set; } = string.Empty;

    [Column("global_sanction")]
    public bool GlobalSanction { get; set; }

    [Column("created")]
    public long CreatedAt { get; set; }

    [Column("updated")]
    public long UpdatedAt { get; set; }
}