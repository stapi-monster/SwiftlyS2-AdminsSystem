using System.ComponentModel.DataAnnotations.Schema;
using Dommel;

namespace Admins.Core.Database.Models;

[Table("as_admins_servers")]
public class AdminServerLink
{
    [Column("admin_id")]
    public long AdminId { get; set; }

    [Column("server_id")]
    public int ServerId { get; set; }
}
