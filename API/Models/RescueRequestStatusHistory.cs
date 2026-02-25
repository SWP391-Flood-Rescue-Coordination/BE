namespace Flood_Rescue_Coordination.API.Models;

public class RescueRequestStatusHistory
{
    public int StatusId { get; set; }
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
