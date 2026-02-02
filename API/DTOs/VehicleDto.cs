namespace Flood_Rescue_Coordination.API.DTOs;

public class VehicleResponseDto
{
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentLocation { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public DateTime CreatedAt { get; set; }
}
