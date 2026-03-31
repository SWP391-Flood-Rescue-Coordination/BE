using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

public class StockUnitOptionDto
{
    public int StockUnitId { get; set; }
    public string Id { get; set; } = string.Empty; // để FE map với id cũ
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Region { get; set; }
    public string? Address { get; set; }
    public bool SupportsImport { get; set; }
    public bool SupportsExport { get; set; }
}

public class CreateStockUnitRequest
{
    [Required]
    public string UnitCode { get; set; } = string.Empty;

    [Required]
    public string UnitName { get; set; } = string.Empty;

    public string? UnitType { get; set; }
    public string? Region { get; set; }
    public string? Address { get; set; }

    public bool SupportsImport { get; set; } = true;
    public bool SupportsExport { get; set; } = true;
}

public class UpdateStockUnitRequest
{
    public string? UnitCode { get; set; }
    public string? UnitName { get; set; }
    public string? UnitType { get; set; }
    public string? Region { get; set; }
    public string? Address { get; set; }
    public bool? SupportsImport { get; set; }
    public bool? SupportsExport { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateStockUnitStatusRequest
{
    /// <summary>
    /// Trạng thái hoạt động mong muốn của đơn vị:
    /// true = Active, false = Inactive.
    /// </summary>
    public bool? IsActive { get; set; }
}