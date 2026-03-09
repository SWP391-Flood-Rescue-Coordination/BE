using System.Collections.Generic;

namespace Flood_Rescue_Coordination.API.DTOs;

public class CreateReliefExportDto
{
    public int WarehouseId { get; set; }
    public int DestinationRegionId { get; set; }
    public string? Notes { get; set; }
    public List<ExportItemDto> Items { get; set; } = new();
    public List<int> VehicleIds { get; set; } = new();
}

public class ExportItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class ReliefExportResponseDto
{
    public int ExportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
