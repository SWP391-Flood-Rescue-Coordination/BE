namespace Flood_Rescue_Coordination.API.DTOs;

public class VehicleResponseDto
{
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string? VehicleName { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentLocation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateVehicleStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateVehicleDto
{
    public int? VehicleTypeId { get; set; }
    public string? VehicleName { get; set; }
    public int? Capacity { get; set; }
    public string? Status { get; set; }
    public string? CurrentLocation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastMaintenance { get; set; }
}

public class CreateVehicleDto
{
    public string VehicleCode { get; set; } = string.Empty;
    public string? VehicleName { get; set; }
    public int VehicleTypeId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public string? Status { get; set; }
    public string? CurrentLocation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastMaintenance { get; set; }
}
