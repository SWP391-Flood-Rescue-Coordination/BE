using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// API Quản lý (Admin/Manager): Truy vấn lịch sử biến động kho vật tư cứu trợ.
    /// Hỗ trợ lọc theo loại giao dịch: IN (Nhập), OUT (Xuất).
    /// </summary>
    public StockHistoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách lịch sử kho theo thứ tự mới nhất đến cũ nhất.
    /// Nếu truyền type = "IN" hoặc "OUT" thì chỉ lấy các dòng tương ứng.
    /// </summary>
    /// <param name="type">Loại giao dịch: IN hoặc OUT (tuỳ chọn)</param>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetStockHistory([FromQuery] string? type)
    {
        if (type != null)
        {
            var upper = type.ToUpper();
            if (upper != "IN" && upper != "OUT")
                return BadRequest(new { Success = false, Message = "type phải là 'IN' hoặc 'OUT'." });
            type = upper;
        }

        var query = _context.StockHistories.AsQueryable();
        if (!string.IsNullOrEmpty(type)) query = query.Where(s => s.Type == type);

        var items = await query
            .OrderByDescending(s => s.Date)
            .Select(s => new StockHistoryDto { Id = s.Id, Type = s.Type, Date = s.Date, Body = s.Body, FromTo = s.FromTo, Note = s.Note })
            .ToListAsync();

        return Ok(new { Success = true, Type = type ?? "ALL", Count = items.Count, Data = items });
    }

    /// <summary>
    /// API Quản lý (Manager): Thực hiện nhập kho vật tư cứu trợ (Import).
    /// Quy trình: Cập nhật tăng số lượng tồn kho (Quantity) cho từng vật phẩm và ghi lại lịch sử giao dịch.
    /// Sử dụng Transaction để đảm bảo tính nhất quán của dữ liệu kho.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ImportStock([FromBody] ImportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { Success = false, Message = "Danh sách vật tư không được rỗng." });
        }

        // Kiểm tra nguồn gốc nhập hàng (Source) bám theo danh sách đơn vị đăng ký trong hệ thống
        var sourceUnitName = await ResolveActiveStockUnitNameAsync(request.Source, forImport: true);
        if (sourceUnitName == null)
            return BadRequest(new { Success = false, Message = "Nguồn nhập không tồn tại hoặc đã bị vô hiệu hoá." });

        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var importedItemsBodyList = new List<string>();

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null || !reliefItem.IsActive)
                        throw new Exception($"Vật tư (ID: {itemInput.ItemId}) không tồn tại hoặc đã ngừng cung cấp.");

                    if (itemInput.Quantity <= 0)
                        throw new Exception($"Số lượng nhập vào phải lớn hơn 0.");

                    reliefItem.Quantity += itemInput.Quantity;
                    importedItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                var bodyContent = string.Join(",", importedItemsBodyList);

                var history = new StockHistory
                {
                    Type = "IN",
                    Date = DateTime.UtcNow,
                    FromTo = sourceUnitName,
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = request.Note
                };

                _context.StockHistories.Add(history);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Success = true, Message = "Nhập kho thành công." });
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = "Lỗi nghiệp vụ: " + ex.Message });
        }
    }

    /// <summary>
    /// API Quản lý (Manager): Thực hiện xuất kho vật tư (Export) để điều phối cứu trợ.
    /// Quy trình: Kiểm tra tồn kho hiện tại, trừ số lượng và ghi nhận đơn vị nhận hàng (Destination).
    /// Sử dụng Transaction để đảm bảo không bị xuất quá số lượng hiện có.
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ExportStock([FromBody] ExportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { Success = false, Message = "Danh sách vật tư xuất không được rỗng." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var manager = await _context.Users.FindAsync(userId);
                if (manager == null)
                    return Unauthorized(new { Success = false, Message = "Không tìm thấy thông tin Manager." });

                // 1. Địa điểm nhận (Destination)
                if (string.IsNullOrWhiteSpace(request.Destination))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Đơn vị nhận hàng là bắt buộc. Vui lòng chọn từ danh sách đơn vị xuất."
                    });
                }

                var destinationUnitName = await ResolveActiveStockUnitNameAsync(request.Destination, forImport: false);
                if (destinationUnitName == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Đơn vị xuất không tồn tại hoặc đã ngưng sử dụng. Vui lòng chọn từ danh sách đơn vị hợp lệ."
                    });
                }

                string finalDestination = destinationUnitName;

                // 2. Kiểm tra tồn kho và chuẩn bị Body
                var exportItemsBodyList = new List<string>();
                int totalExportQuantity = 0;

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null) throw new Exception($"Vật tư (ID: {itemInput.ItemId}) không còn trong kho.");

                    if (reliefItem.Quantity < itemInput.Quantity)
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' không đủ tồn kho (Hiện có: {reliefItem.Quantity}).");

                    reliefItem.Quantity -= itemInput.Quantity;
                    totalExportQuantity += itemInput.Quantity;
                    exportItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                // 3. Tạo lịch sử
                var bodyContent = string.Join(",", exportItemsBodyList);
                var finalNote = $"Địa điểm nhận: {finalDestination}";
                if (!string.IsNullOrEmpty(request.Note))
                    finalNote += $" | Ghi chú: {request.Note}";

                var history = new StockHistory
                {
                    Type = "OUT",
                    Date = DateTime.UtcNow,
                    FromTo = finalDestination,
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = finalNote.Length > 500 ? finalNote.Substring(0, 497) + "..." : finalNote
                };

                _context.StockHistories.Add(history);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Xuất kho thành công.",
                    HistoryId = history.Id,
                    Data = new
                    {
                        history.Type,
                        history.FromTo,
                        history.Body,
                        history.Note
                    }
                });
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = "Lỗi nghiệp vụ xuất kho: " + ex.Message });
        }
    }

    /// <summary>
    /// Hàm hỗ trợ: Xác thực và chuẩn hoá tên đơn vị (Source/Destination) từ bảng stock_units.
    /// Đảm bảo dữ liệu đầu vào khớp với danh sách đơn vị tham chiếu của hệ thống.
    /// </summary>
    private async Task<string?> ResolveActiveStockUnitNameAsync(string rawValue, bool forImport)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return null;

        var normalizedUpper = rawValue.Trim().ToUpperInvariant();

        return await _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && (forImport ? u.SupportsImport : u.SupportsExport))
            .Where(u => u.UnitCode.ToUpper() == normalizedUpper || u.UnitName.ToUpper() == normalizedUpper)
            .Select(u => u.UnitName)
            .FirstOrDefaultAsync();
    }
}
