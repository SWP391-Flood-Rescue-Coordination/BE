namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeam
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Vị trí cố định của đội cứu hộ
    public decimal? BaseLatitude { get; set; }
    public decimal? BaseLongitude { get; set; }
}
