using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER,ADMIN")]
public class StockUnitController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Controller quản lý danh mục đơn vị nhập/xuất kho.
    /// Luồng chính:
    /// 1) FE lấy danh sách đơn vị nhập bằng endpoint import-options
    /// 2) FE lấy danh sách đơn vị xuất bằng endpoint export-options
    /// 3) Quản trị viên/manager thêm-sửa-ngưng dùng đơn vị tại đây
    /// </summary>
    public StockUnitController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API cho FE: Lấy danh sách các đơn vị Hợp lệ để hiển thị trong màn hình Nhập kho (Import).
    /// Lọc: IsActive = true và SupportsImport = true.
    /// </summary>
    [HttpGet("import-options")]
    public async Task<IActionResult> GetImportOptions()
    {
        var units = await _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && u.SupportsImport)
            .OrderBy(u => u.UnitName)
            .Select(ToOptionDto())
            .ToListAsync();

        return Ok(new { Success = true, Count = units.Count, Data = units });
    }

    /// <summary>
    /// API cho FE: Lấy danh sách các đơn vị Hợp lệ để hiển thị trong màn hình Xuất kho (Export).
    /// Lọc: IsActive = true và SupportsExport = true.
    /// </summary>
    [HttpGet("export-options")]
    public async Task<IActionResult> GetExportOptions()
    {
        var units = await _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && u.SupportsExport)
            .OrderBy(u => u.UnitName)
            .Select(ToOptionDto())
            .ToListAsync();

        return Ok(new { Success = true, Count = units.Count, Data = units });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Thêm mới một đơn vị nhập/xuất vào danh mục hệ thống.
    /// Ví dụ: Thêm một nhà tài trợ mới hoặc một xã/huyện mới nhận hàng cứu trợ.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUnit([FromBody] CreateStockUnitRequest request)
    {
        var code = (request.UnitCode ?? "").Trim();
        var name = (request.UnitName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest(new { Success = false, Message = "Mã và Tên đơn vị là bắt buộc." });

        if (!request.SupportsImport && !request.SupportsExport)
            return BadRequest(new { Success = false, Message = "Đơn vị phải hỗ trợ nhập hoặc xuất hàng." });

        if (await _context.StockUnits.AnyAsync(u => u.UnitCode.ToUpper() == code.ToUpperInvariant()))
            return BadRequest(new { Success = false, Message = "Mã đơn vị (UnitCode) đã tồn tại." });

        var entity = new StockUnit
        {
            UnitCode = code,
            UnitName = name,
            UnitType = request.UnitType?.Trim(),
            Region = request.Region?.Trim(),
            Address = request.Address?.Trim(),
            SupportsImport = request.SupportsImport,
            SupportsExport = request.SupportsExport,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockUnits.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Tạo đơn vị thành công." });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Cập nhật thông tin chi tiết của một đơn vị.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateStockUnitRequest request)
    {
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null) return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị." });

        // Cập nhật UnitCode (kiểm tra trùng lặp)
        if (request.UnitCode != null)
        {
            var code = request.UnitCode.Trim();
            if (await _context.StockUnits.AnyAsync(u => u.StockUnitId != id && u.UnitCode.ToUpper() == code.ToUpperInvariant()))
                return BadRequest(new { Success = false, Message = "Mã đơn vị mới đã tồn tại." });
            entity.UnitCode = code;
        }

        if (request.UnitName != null) entity.UnitName = request.UnitName.Trim();
        if (request.UnitType != null) entity.UnitType = request.UnitType.Trim();
        if (request.Region != null) entity.Region = request.Region.Trim();
        if (request.Address != null) entity.Address = request.Address.Trim();

        if (request.SupportsImport.HasValue) entity.SupportsImport = request.SupportsImport.Value;
        if (request.SupportsExport.HasValue) entity.SupportsExport = request.SupportsExport.Value;

        if (!entity.SupportsImport && !entity.SupportsExport)
            return BadRequest(new { Success = false, Message = "Đơn vị phải hỗ trợ ít nhất một nghiệp vụ." });

        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật thành công." });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Ngưng sử dụng đơn vị (Soft-delete).
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUnit(int id)
    {
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null) return NotFound(new { Success = false });

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Đã ngưng sử dụng đơn vị này." });
    }

    private static System.Linq.Expressions.Expression<Func<StockUnit, StockUnitOptionDto>> ToOptionDto()
    {
        return u => new StockUnitOptionDto
        {
            StockUnitId = u.StockUnitId,
            Id = u.UnitCode,
            Name = u.UnitName,
            Type = u.UnitType,
            Region = u.Region,
            Address = u.Address,
            SupportsImport = u.SupportsImport,
            SupportsExport = u.SupportsExport
        };
    }
}