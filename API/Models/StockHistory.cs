using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flood_Rescue_Coordination.API.Models;

public class StockHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("type")]
    [Required]
    [MaxLength(3)]
    public string Type { get; set; } = "OUT"; // "IN" or "OUT"

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("body")]
    [MaxLength(500)]
    public string? Body { get; set; }

    [Column("from_to")]
    [MaxLength(200)]
    public string? FromTo { get; set; }

    [Column("note")]
    [MaxLength(500)]
    public string? Note { get; set; }
}
