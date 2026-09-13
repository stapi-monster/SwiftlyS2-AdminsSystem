using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Admins.Core.Contract;
using Dommel;

namespace Admins.Core.Database.Models;

[Table("as_admins")]
public class Admin : IAdmin
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("steamid")]
    public long SteamId64 { get; set; }

    [Column("name")]
    public string Username { get; set; } = string.Empty;

    [Ignore]
    public string Flags
    {
        get => string.Join(",", Permissions);
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Permissions = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
            else
            {
                Permissions = new List<string>();
            }
        }
    }

    [Ignore]
    public int Immunity { get; set; }

    [Ignore]
    public long EndTime { get; set; }

    [Ignore]
    public List<string> Permissions { get; set; } = [];

    [Ignore]
    public List<string> Groups { get; set; } = [];

    [Ignore]
    public List<string> Servers { get; set; } = [];
}