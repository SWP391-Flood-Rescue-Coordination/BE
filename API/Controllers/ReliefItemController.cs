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
}
