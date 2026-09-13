using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Admins.Core.Contract;
using Dommel;

namespace Admins.Core.Database.Models;

[Table("as_groups")]
public class Group : IGroup
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("flags")]
    public string Flags { get; set; } = string.Empty;

    [Column("immunity")]
    public int Immunity { get; set; }

    [Ignore]
    public List<string> Permissions { get; set; } = new();

    [Ignore]
    public List<string> Servers { get; set; } = new();
}