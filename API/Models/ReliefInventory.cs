using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flood_Rescue_Coordination.API.Models;

public class ReliefItem
{
    [Key]
    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("item_code")]
    [Required]
    [MaxLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Column("item_name")]
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("unit")]
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Warehouse
{
    [Key]
    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("warehouse_name")]
    [Required]
    [MaxLength(100)]
    public string WarehouseName { get; set; } = string.Empty;

    [Column("location")]
    public string? Location { get; set; }

    [Column("region_id")]
    public int? RegionId { get; set; }
}

public class Region
{
    [Key]
    [Column("region_id")]
    public int RegionId { get; set; }

    [Column("region_name")]
    [Required]
    [MaxLength(100)]
    public string RegionName { get; set; } = string.Empty;

    [Column("region_type")]
    [Required]
    [MaxLength(20)]
    public string RegionType { get; set; } = "Province"; // Province, District, Village
}

public class Inventory
{
    [Key]
    [Column("inventory_id")]
    public int InventoryId { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [ForeignKey("WarehouseId")]
    public virtual Warehouse? Warehouse { get; set; }

    [ForeignKey("ItemId")]
    public virtual ReliefItem? Item { get; set; }
}

public class ManagerScope
{
    [Key]
    [Column("scope_id")]
    public int ScopeId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("region_id")]
    public int RegionId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("RegionId")]
    public virtual Region? Region { get; set; }
}

public class ReliefExportOrder
{
    [Key]
    [Column("export_id")]
    public int ExportId { get; set; }

    [Column("manager_id")]
    public int ManagerId { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("destination_region_id")]
    public int DestinationRegionId { get; set; }

    [Column("export_date")]
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("notes")]
    public string? Notes { get; set; }

    [ForeignKey("ManagerId")]
    public virtual User? Manager { get; set; }

    [ForeignKey("WarehouseId")]
    public virtual Warehouse? Warehouse { get; set; }

    [ForeignKey("DestinationRegionId")]
    public virtual Region? Destination { get; set; }
}

public class ReliefExportItem
{
    [Key]
    [Column("export_item_id")]
    public int ExportItemId { get; set; }

    [Column("export_id")]
    public int ExportId { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [ForeignKey("ExportId")]
    public virtual ReliefExportOrder? ExportOrder { get; set; }

    [ForeignKey("ItemId")]
    public virtual ReliefItem? Item { get; set; }
}

public class ReliefExportVehicle
{
    [Key]
    [Column("export_vehicle_id")]
    public int ExportVehicleId { get; set; }

    [Column("export_id")]
    public int ExportId { get; set; }

    [Column("vehicle_id")]
    public int VehicleId { get; set; }

    [ForeignKey("ExportId")]
    public virtual ReliefExportOrder? ExportOrder { get; set; }

    [ForeignKey("VehicleId")]
    public virtual Vehicle? Vehicle { get; set; }
}

public class StockHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("type")]
    [Required]
    [MaxLength(3)]
    public string Type { get; set; } = "OUT"; // "IN" or "OUT"

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("body")]
    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty; // format: "item_id-quantity,item_id-quantity"

    [Column("from_to")]
    [MaxLength(200)]
    public string? FromTo { get; set; }

    [Column("note")]
    [MaxLength(500)]
    public string? Note { get; set; }
}
