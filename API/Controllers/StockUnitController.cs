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
    /// API cho FE màn Nhập kho:
    /// chỉ trả về các đơn vị còn hiệu lực (is_active = true) và hỗ trợ nhập (supports_import = true).
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
    /// API cho FE màn Xuất kho:
    /// chỉ trả về các đơn vị còn hiệu lực (is_active = true) và hỗ trợ xuất (supports_export = true).
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
    /// API quản lý: thêm mới đơn vị nhập/xuất.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUnit([FromBody] CreateStockUnitRequest request)
    {
        var code = request.UnitCode.Trim();
        var name = request.UnitName.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { Success = false, Message = "UnitCode và UnitName là bắt buộc." });
        }

        if (!request.SupportsImport && !request.SupportsExport)
        {
            return BadRequest(new { Success = false, Message = "Đơn vị phải hỗ trợ ít nhất một nghiệp vụ: nhập hoặc xuất." });
        }

        var codeUpper = code.ToUpperInvariant();
        var duplicatedCode = await _context.StockUnits
            .AnyAsync(u => u.UnitCode.ToUpper() == codeUpper);

        if (duplicatedCode)
        {
            return BadRequest(new { Success = false, Message = "UnitCode đã tồn tại." });
        }

        var entity = new StockUnit
        {
            UnitCode = code,
            UnitName = name,
            UnitType = string.IsNullOrWhiteSpace(request.UnitType) ? null : request.UnitType.Trim(),
            Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            SupportsImport = request.SupportsImport,
            SupportsExport = request.SupportsExport,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockUnits.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Tạo đơn vị thành công.", Data = new { entity.StockUnitId, entity.UnitCode, entity.UnitName } });
    }

    /// <summary>
    /// API quản lý: cập nhật thông tin đơn vị và cờ phân loại nhập/xuất.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateStockUnitRequest request)
    {
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị." });
        }

        if (request.UnitCode != null)
        {
            var code = request.UnitCode.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { Success = false, Message = "UnitCode không được rỗng." });
            }

            var codeUpper = code.ToUpperInvariant();
            var duplicatedCode = await _context.StockUnits
                .AnyAsync(u => u.StockUnitId != id && u.UnitCode.ToUpper() == codeUpper);

            if (duplicatedCode)
            {
                return BadRequest(new { Success = false, Message = "UnitCode đã tồn tại." });
            }

            entity.UnitCode = code;
        }

        if (request.UnitName != null)
        {
            var name = request.UnitName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { Success = false, Message = "UnitName không được rỗng." });
            }

            entity.UnitName = name;
        }

        if (request.UnitType != null)
        {
            entity.UnitType = string.IsNullOrWhiteSpace(request.UnitType) ? null : request.UnitType.Trim();
        }

        if (request.Region != null)
        {
            entity.Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim();
        }

        if (request.Address != null)
        {
            entity.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        }

        if (request.SupportsImport.HasValue)
        {
            entity.SupportsImport = request.SupportsImport.Value;
        }

        if (request.SupportsExport.HasValue)
        {
            entity.SupportsExport = request.SupportsExport.Value;
        }

        if (!entity.SupportsImport && !entity.SupportsExport)
        {
            return BadRequest(new { Success = false, Message = "Đơn vị phải hỗ trợ ít nhất một nghiệp vụ: nhập hoặc xuất." });
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật đơn vị thành công." });
    }

    /// <summary>
    /// API quản lý: ngưng sử dụng đơn vị (soft delete).
    /// FE sẽ không còn nhìn thấy đơn vị này trong import-options/export-options.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUnit(int id)
    {
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị." });
        }

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Đã ngưng sử dụng đơn vị." });
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