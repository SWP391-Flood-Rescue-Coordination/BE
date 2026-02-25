namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeam
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamCode { get; set; } = string.Empty;
    public int? LeaderId { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public int CurrentCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
