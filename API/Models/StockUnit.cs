namespace Flood_Rescue_Coordination.API.Models;

public class StockUnit
{
    public int StockUnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? UnitType { get; set; }
    public string? Region { get; set; }
    public string? Address { get; set; }
    public bool SupportsImport { get; set; } = true;
    public bool SupportsExport { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}