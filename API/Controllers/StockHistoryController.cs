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
    /// Constructor khởi tạo StockHistoryController với DbContext.
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

        if (!string.IsNullOrEmpty(type))
            query = query.Where(s => s.Type == type);

        var items = await query
            .OrderByDescending(s => s.Date)
            .Select(s => new StockHistoryDto
            {
                Id     = s.Id,
                Type   = s.Type,
                Date   = s.Date,
                Body   = s.Body,
                FromTo = s.FromTo,
                Note   = s.Note
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Type    = type ?? "ALL",
            Count   = items.Count,
            Data    = items
        });
    }

    /// <summary>
    /// Tạo phiếu nhập kho vật tư cứu trợ khi tiếp nhận hàng từ các nguồn hỗ trợ.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ImportStock([FromBody] ImportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { Success = false, Message = "Danh sách vật tư không được rỗng." });
        }

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
                    if (reliefItem == null)
                    {
                        throw new Exception($"Vật tư với ID {itemInput.ItemId} không tồn tại trong hệ thống.");
                    }

                    if (!reliefItem.IsActive)
                    {
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' (ID: {itemInput.ItemId}) đang trong trạng thái ngưng hoạt động.");
                    }

                    if (itemInput.Quantity <= 0)
                    {
                        throw new Exception($"Số lượng vật tư '{reliefItem.ItemName}' phải lớn hơn 0.");
                    }

                    reliefItem.Quantity += itemInput.Quantity;
                    importedItemsBodyList.Add($"{itemInput.ItemId}-{itemInput.Quantity}");
                }

                var bodyContent = string.Join(",", importedItemsBodyList);

                var history = new StockHistory
                {
                    Type = "IN",
                    Date = DateTime.UtcNow,
                    FromTo = request.Source,
                    Body = bodyContent.Length > 500 ? bodyContent.Substring(0, 497) + "..." : bodyContent,
                    Note = request.Note?.Length > 500 ? request.Note.Substring(0, 497) + "..." : request.Note
                };

                _context.StockHistories.Add(history);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Tạo phiếu nhập kho thành công.",
                    HistoryId = history.Id,
                    Data = new
                    {
                        history.Type,
                        history.Date,
                        history.FromTo,
                        history.Body,
                        history.Note
                    }
                });
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = "Lỗi khi tạo phiếu nhập kho: " + ex.Message });
        }
    }

    /// <summary>
    /// Tạo phiếu xuất kho vật tư cứu trợ để điều phối đến đơn vị nhận (tỉnh/xóm).
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ExportStock([FromBody] ExportStockRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { Success = false, Message = "Danh sách vật tư xuất không được rỗng." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

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
                string finalDestination = !string.IsNullOrEmpty(request.Destination) 
                    ? request.Destination 
                    : "Chưa xác định";

                // 2. Kiểm tra tồn kho và chuẩn bị Body
                var exportItemsBodyList = new List<string>();
                int totalExportQuantity = 0;

                foreach (var itemInput in request.Items)
                {
                    var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                    if (reliefItem == null)
                        throw new Exception($"Vật tư với ID {itemInput.ItemId} không tồn tại.");

                    if (reliefItem.Quantity < itemInput.Quantity)
                        throw new Exception($"Vật tư '{reliefItem.ItemName}' không đủ tồn kho (Cần: {itemInput.Quantity}, Hiện có: {reliefItem.Quantity}).");

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
            return BadRequest(new { Success = false, Message = "Lỗi khi xuất kho: " + ex.Message });
        }
    }
}
