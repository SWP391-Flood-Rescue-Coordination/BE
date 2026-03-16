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

        var importedItemsDetail = new List<string>();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var itemInput in request.Items)
            {
                if (itemInput.Quantity <= 0)
                {
                    return BadRequest(new { Success = false, Message = $"Số lượng vật tư (ItemId: {itemInput.ItemId}) phải lớn hơn 0." });
                }

                var reliefItem = await _context.ReliefItems.FindAsync(itemInput.ItemId);
                if (reliefItem == null)
                {
                    return BadRequest(new { Success = false, Message = $"Vật tư với ID {itemInput.ItemId} không tồn tại trong hệ thống." });
                }

                reliefItem.Quantity += itemInput.Quantity;
                importedItemsDetail.Add($"{reliefItem.ItemName} (+{itemInput.Quantity} {reliefItem.Unit})");
            }

            var noteContent = $"Địa chỉ tiếp nhận: {request.Location}";
            var bodyContent = "Nhập: " + string.Join(", ", importedItemsDetail);

            // Limit body width to max 500 characters
            if (bodyContent.Length > 500)
            {
                bodyContent = bodyContent.Substring(0, 497) + "...";
            }

            var history = new StockHistory
            {
                Type = "IN",
                Date = DateTime.UtcNow,
                FromTo = request.Source,
                Note = noteContent.Length > 500 ? noteContent.Substring(0, 497) + "..." : noteContent,
                Body = bodyContent
            };

            _context.StockHistories.Add(history);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                Success = true,
                Message = "Tạo phiếu nhập kho thành công.",
                HistoryId = history.Id
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Success = false, Message = "Lỗi khi tạo phiếu nhập kho: " + ex.Message });
        }
    }
}
