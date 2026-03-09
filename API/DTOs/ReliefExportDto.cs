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

public class StockHistoryResponseDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? FromTo { get; set; }
    public string? Note { get; set; }
}

public class SupplyItemResponseDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int MinQuantity { get; set; }
}
