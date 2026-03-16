using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReliefItemController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReliefItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách tất cả vật phẩm cứu trợ. (Cho Front-end)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetAllReliefItems()
    {
        var items = await _context.ReliefItems
            .OrderBy(i => i.ItemName)
            .Select(i => new ReliefItemDto
            {
                ItemId    = i.ItemId,
                ItemCode  = i.ItemCode,
                ItemName  = i.ItemName,
                CategoryId = i.CategoryId,
                Unit      = i.Unit,
                Quantity  = i.Quantity,
                MinQuantity = i.MinQuantity,
                IsActive  = i.IsActive,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Count = items.Count,
            Data = items
        });
    }

    /// <summary>
    /// Lấy danh sách vật phẩm có quantity nhỏ hơn hoặc bằng n.
    /// </summary>
    /// <param name="n">Ngưỡng số lượng tối đa (mặc định = 6)</param>
    [HttpGet("low-stock")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int n = 6)
    {
        if (n < 0)
            return BadRequest(new { Success = false, Message = "n phải là số không âm." });

        var items = await _context.ReliefItems
            .Where(i => i.Quantity <= n)
            .OrderBy(i => i.Quantity)
            .Select(i => new ReliefItemDto
            {
                ItemId    = i.ItemId,
                ItemCode  = i.ItemCode,
                ItemName  = i.ItemName,
                CategoryId = i.CategoryId,
                Unit      = i.Unit,
                Quantity  = i.Quantity,
                MinQuantity = i.MinQuantity,
                IsActive  = i.IsActive,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Threshold = n,
            Count = items.Count,
            Items = items
        });
    }

    /// <summary>
    /// Đếm số lượng vật phẩm có quantity nhỏ hơn hoặc bằng n.
    /// </summary>
    /// <param name="n">Ngưỡng số lượng tối đa (mặc định = 6)</param>
    [HttpGet("low-stock/count")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> CountLowStockItems([FromQuery] int n = 6)
    {
        if (n < 0)
            return BadRequest(new { Success = false, Message = "n phải là số không âm." });

        var count = await _context.ReliefItems
            .CountAsync(i => i.Quantity <= n);

        return Ok(count);
    }

    /// <summary>
    /// Admin/Manager - Cập nhật thông tin vật phẩm cứu trợ.
    /// Chỉ cập nhật các trường được cung cấp trong request body.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateReliefItem(int id, [FromBody] UpdateReliefItemDto dto)
    {
        var item = await _context.ReliefItems.FindAsync(id);

        if (item == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy vật phẩm với ID = {id}." });

        if (dto.ItemName    != null) item.ItemName    = dto.ItemName;
        if (dto.CategoryId  != null) item.CategoryId  = dto.CategoryId.Value;
        if (dto.Unit        != null) item.Unit        = dto.Unit;
        if (dto.MinQuantity != null) item.MinQuantity = dto.MinQuantity.Value;
        if (dto.IsActive    != null) item.IsActive    = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = "Cập nhật vật phẩm thành công.",
            Data = new ReliefItemDto
            {
                ItemId      = item.ItemId,
                ItemCode    = item.ItemCode,
                ItemName    = item.ItemName,
                CategoryId  = item.CategoryId,
                Unit        = item.Unit,
                Quantity    = item.Quantity,
                MinQuantity = item.MinQuantity,
                IsActive    = item.IsActive,
                CreatedAt   = item.CreatedAt
            }
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
