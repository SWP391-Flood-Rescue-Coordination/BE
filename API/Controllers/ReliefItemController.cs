using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// ReliefItemController: Quản lý danh mục các vật phẩm cứu trợ (thực phẩm, thuốc men, trang thiết bị...).
/// Cung cấp các thao tác xem danh sách, lọc hàng tồn kho thấp và cập nhật thông tin vật phẩm.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReliefItemController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo ReliefItemController.
    /// </summary>
    /// <param name="context">DbContext để truy xuất cơ sở dữ liệu.</param>
    public ReliefItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Truy vấn danh sách toàn bộ các vật phẩm cứu trợ trong hệ thống.
    /// Có hỗ trợ tìm kiếm theo tên vật phẩm (ItemName).
    /// </summary>
    /// <param name="searchBy">Trường cần tìm kiếm (Hiện tại chỉ hỗ trợ 'itemName').</param>
    /// <param name="keyword">Từ khóa tìm kiếm.</param>
    /// <returns>Danh sách vật phẩm đã được định dạng qua DTO.</returns>
    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetAllReliefItems([FromQuery] string? searchBy = null, [FromQuery] string? keyword = null)
    {
        // 1. Khởi tạo truy vấn từ bảng ReliefItems
        var query = _context.ReliefItems.AsQueryable();

        // 2. Xử lý logic tìm kiếm nếu có tham số được gửi lên
        // Chuẩn hóa search backend: Mỗi trang chỉ tìm theo 1 field đúng mục đích nghiệp vụ
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Kiểm tra tính hợp lệ của trường tìm kiếm (Whitelist)
            var allowedFields = new[] { "itemName" };
            if (!allowedFields.Contains(searchBy))
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Trường tìm kiếm '{searchBy}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedFields)}" 
                });
            }

            // Thực hiện lọc theo từ khóa (không phân biệt hoa thường trong SQL)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                if (searchBy == "itemName")
                {
                    query = query.Where(i => i.ItemName.Contains(keyword));
                }
            }
        }

        // 3. Thực thi truy vấn, sắp xếp theo tên và map sang DTO
        var items = await query
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
    /// <param name="n">Ngưỡng số lượng tối đa để coi là 'sắp hết' (Mặc định là 6).</param>
    /// <returns>Danh sách các vật phẩm vi phạm ngưỡng tồn kho.</returns>
    [HttpGet("low-stock")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int n = 6)
    {
        // 1. Kiểm tra tham số đầu vào
        if (n < 0)
            return BadRequest(new { Success = false, Message = "n phải là số không âm." });

        // 2. Truy vấn các vật phẩm có số lượng (Quantity) <= n
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
    /// Trả về số lượng (Count) các vật phẩm đang ở trạng thái sắp hết hàng.
    /// Dùng để hiển thị thông báo/Badge trên Dashboard.
    /// </summary>
    /// <param name="n">Ngưỡng số lượng (Mặc định là 6).</param>
    /// <returns>Con số tổng số vật phẩm sắp hết.</returns>
    [HttpGet("low-stock/count")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> CountLowStockItems([FromQuery] int n = 6)
    {
        if (n < 0)
            return BadRequest(new { Success = false, Message = "n phải là số không âm." });

        // Thực hiện đếm nhanh trên Server DB
        var count = await _context.ReliefItems
            .CountAsync(i => i.Quantity <= n);

        return Ok(count);
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết một vật phẩm cứu trợ (Tên, Loại, Đơn vị, Ngưỡng tối thiểu...).
    /// Chỉ dành cho vai trò ADMIN hoặc MANAGER.
    /// </summary>
    /// <param name="id">ID của vật phẩm cần sửa.</param>
    /// <param name="dto">Dữ liệu cập nhật mới.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateReliefItem(int id, [FromBody] UpdateReliefItemDto dto)
    {
        // 1. Tìm vật phẩm trong cơ sở dữ liệu
        var item = await _context.ReliefItems.FindAsync(id);

        if (item == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy vật phẩm với ID = {id}." });

        // 2. Cập nhật các trường thông tin (Chỉ cập nhật nếu giá trị gửi lên không null)
        if (dto.ItemName    != null) item.ItemName    = dto.ItemName;
        if (dto.CategoryId  != null) item.CategoryId  = dto.CategoryId.Value;
        if (dto.Unit        != null) item.Unit        = dto.Unit;
        if (dto.MinQuantity != null) item.MinQuantity = dto.MinQuantity.Value;
        if (dto.IsActive    != null) item.IsActive    = dto.IsActive.Value;

        // 3. Lưu thay đổi vào DB
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
