using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flood_Rescue_Coordination.API.Models;

[Table("blacklisted_tokens")]
public class BlacklistedToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("blacklisted_at")]
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
