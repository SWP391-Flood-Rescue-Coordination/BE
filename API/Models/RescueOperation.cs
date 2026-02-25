namespace Flood_Rescue_Coordination.API.Models;

public class RescueOperation
{
    public int OperationId { get; set; }
    public int RequestId { get; set; }
    public int TeamId { get; set; }
    public int AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Assigned";
}
