using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

public class ExportStockRequest
{
    public string? Destination { get; set; }

    public string? Note { get; set; }

    [Required(ErrorMessage = "Danh sách vật tư không được rỗng.")]
    [MinLength(1, ErrorMessage = "Danh sách vật tư không được rỗng.")]
    public List<ExportStockItem> Items { get; set; } = new();
}

public class ExportStockItem
{
    [Required(ErrorMessage = "Mã vật tư (ItemId) không được rỗng.")]
    public int ItemId { get; set; }

    [Required(ErrorMessage = "Số lượng xuất không được rỗng.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng vật tư xuất phải lớn hơn 0.")]
    public int Quantity { get; set; }
}
