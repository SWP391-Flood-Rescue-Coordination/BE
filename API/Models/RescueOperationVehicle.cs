namespace Flood_Rescue_Coordination.API.Models;

public class RescueOperationVehicle
{
    public int OperationId { get; set; }
    public int VehicleId { get; set; }
    public int AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual RescueOperation? Operation { get; set; }
    public virtual Vehicle? Vehicle { get; set; }
}
