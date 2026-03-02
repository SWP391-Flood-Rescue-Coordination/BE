namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeam
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Status { get; set; } = "AVAILABLE";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
