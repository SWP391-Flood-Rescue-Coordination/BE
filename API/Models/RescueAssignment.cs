namespace Flood_Rescue_Coordination.API.Models;

public class RescueAssignment
{
    public int AssignmentId { get; set; }
    public int RequestId { get; set; }
    public int TeamId { get; set; }
    public int AssignedBy { get; set; }
    public int? VehicleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "ASSIGNED";
    public string? Notes { get; set; }

    // Navigation properties
    public virtual RescueRequest? Request { get; set; }
    public virtual RescueTeam? Team { get; set; }
    public virtual Vehicle? Vehicle { get; set; }
}
