namespace Flood_Rescue_Coordination.API.Models;

public class VehicleType
{
    public int VehicleTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
