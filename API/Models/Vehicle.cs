using System;

namespace Flood_Rescue_Coordination.API.Models;

public class Vehicle
{
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string? VehicleName { get; set; }
    public int VehicleTypeId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public string? CurrentLocation { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual VehicleType? VehicleType { get; set; }
}
