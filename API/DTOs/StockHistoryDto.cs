namespace Flood_Rescue_Coordination.API.DTOs;

public class StockHistoryDto
{
    public int Id { get; set; }
    public string Type { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Body { get; set; }
    public string? FromTo { get; set; }
    public string? Note { get; set; }
}
