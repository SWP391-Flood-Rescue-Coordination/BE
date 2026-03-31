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

    /// <summary>
    /// Constructor khởi tạo ReliefItemController với DbContext.
    /// </summary>
    public ReliefItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API lấy toàn bộ danh sách vật phẩm cứu trợ hiện có trong kho.
    /// Quyền truy cập: ADMIN, MANAGER, COORDINATOR.
    /// </summary>
    /// <returns>Danh sách các đối tượng ReliefItemDto.</returns>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetAllReliefItems()
    {
        // Truy vấn danh sách vật phẩm, sắp xếp theo tên và map sang DTO
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
    /// API lấy danh sách các vật phẩm đang ở mức cảnh báo (số lượng tồn kho thấp).
    /// </summary>
    /// <param name="n">Ngưỡng số lượng tối đa để coi là tồn kho thấp (mặc định = 6).</param>
    /// <returns>Danh sách vật phẩm có số lượng nhỏ hơn hoặc bằng n.</returns>
    [HttpGet("low-stock")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int n = 6)
    {
        if (n < 0)
            return BadRequest(new { Success = false, Message = "Ngưỡng số lượng (n) phải là số không âm." });

        // Lọc các vật phẩm có Quantity <= n
        var items = await _context.ReliefItems
            .Where(i => i.Quantity <= n)
            .OrderBy(i => i.Quantity) // Sắp xếp theo số lượng tăng dần (hết nhiều nhất lên trước)
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
    /// API đếm nhanh số lượng các vật phẩm đang có tồn kho thấp.
    /// Dùng để hiển thị thông báo/badge cảnh báo trên Dashboard.
    /// </summary>
    /// <param name="n">Ngưỡng số lượng (mặc định = 6).</param>
    /// <returns>Số nguyên đại diện cho số lượng vật phẩm sắp hết.</returns>
    [HttpGet("low-stock/count")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> CountLowStockItems([FromQuery] int n = 6)
    {
        if (n < 0)
            return BadRequest(new { Success = false, Message = "Ngưỡng số lượng (n) phải là số không âm." });

        var count = await _context.ReliefItems
            .CountAsync(i => i.Quantity <= n);

        return Ok(count);
    }

    /// <summary>
    /// API cập nhật thông tin cơ bản của một vật phẩm cứu trợ.
    /// Quyền truy cập: ADMIN, MANAGER.
    /// </summary>
    /// <param name="id">ID của vật phẩm cần cập nhật.</param>
    /// <param name="dto">Dữ liệu cần cập nhật (chỉ gửi trường nào cần đổi).</param>
    /// <returns>Thông báo thành công và dữ liệu mới của vật phẩm.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateReliefItem(int id, [FromBody] UpdateReliefItemDto dto)
    {
        // Tìm kiếm vật phẩm trong DB
        var item = await _context.ReliefItems.FindAsync(id);

        if (item == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy vật phẩm với ID = {id}." });

        // Cập nhật từng trường nếu có dữ liệu mới truyền vào (Patch-style update)
        if (dto.ItemName    != null) item.ItemName    = dto.ItemName;
        if (dto.CategoryId  != null) item.CategoryId  = dto.CategoryId.Value;
        if (dto.Unit        != null) item.Unit        = dto.Unit;
        if (dto.MinQuantity != null) item.MinQuantity = dto.MinQuantity.Value;
        if (dto.IsActive    != null) item.IsActive    = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = "Cập nhật thông tin vật phẩm thành công.",
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
