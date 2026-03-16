using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

public class ImportStockRequest
{
    [Required(ErrorMessage = "Nguồn gốc hàng (Source) không được rỗng.")]
    public string Source { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ tiếp nhận hàng (Location) không được rỗng.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Danh sách vật tư không được rỗng.")]
    [MinLength(1, ErrorMessage = "Danh sách vật tư không được rỗng.")]
    public List<ImportStockItem> Items { get; set; } = new();
}

public class ImportStockItem
{
    [Required(ErrorMessage = "Mã vật tư (ItemId) không được rỗng.")]
    public int ItemId { get; set; }

    [Required(ErrorMessage = "Số lượng nhập không được rỗng.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng vật tư nhập phải lớn hơn 0.")]
    public int Quantity { get; set; }
}
