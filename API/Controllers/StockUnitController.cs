using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// API quản lý danh mục đơn vị nhập/xuất kho (`stock_units`).
/// 
/// Phân quyền:
/// - `MANAGER, ADMIN`: chỉ xem danh sách đơn vị cho màn hình nhập/xuất.
/// - `ADMIN`: được tạo/cập nhật và đổi trạng thái hoạt động đơn vị.
/// 
/// Luồng FE:
/// 1) FE màn Nhập kho gọi `GET /api/StockUnit/import-options`.
/// 2) FE màn Xuất kho gọi `GET /api/StockUnit/export-options`.
/// 3) FE màn Quản trị (Admin) gọi các API quản trị dữ liệu đơn vị.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER,ADMIN")]
public class StockUnitController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Khởi tạo controller danh mục đơn vị kho.
    /// </summary>
    /// <param name="context">DbContext thao tác dữ liệu `stock_units`.</param>
    public StockUnitController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách đơn vị hợp lệ cho nghiệp vụ Nhập kho.
    /// </summary>
    /// <remarks>
    /// Route: `GET /api/StockUnit/import-options`
    /// 
    /// Công dụng:
    /// - Trả về các đơn vị còn hiệu lực (`is_active = true`)
    /// - Và có hỗ trợ nhập (`supports_import = true`)
    /// 
    /// Nơi FE gọi tới:
    /// - Form tạo phiếu nhập kho (`POST /api/StockHistory/import`)
    /// </remarks>
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
    /// Lấy danh sách đơn vị hợp lệ cho nghiệp vụ Xuất kho.
    /// </summary>
    /// <remarks>
    /// Route: `GET /api/StockUnit/export-options`
    /// 
    /// Công dụng:
    /// - Trả về các đơn vị còn hiệu lực (`is_active = true`)
    /// - Và có hỗ trợ xuất (`supports_export = true`)
    /// 
    /// Nơi FE gọi tới:
    /// - Form tạo phiếu xuất kho (`POST /api/StockHistory/export`)
    /// </remarks>
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
    /// Tạo mới một đơn vị trong danh mục `stock_units`.
    /// </summary>
    /// <remarks>
    /// Route: `POST /api/StockUnit`
    /// Phân quyền: `ADMIN`
    /// 
    /// Công dụng:
    /// - Admin thêm đơn vị mới để FE có thể chọn trong import/export.
    /// - Validate không cho tạo đơn vị có cả 2 cờ `supports_import` và `supports_export` cùng `false`.
    /// - Validate `unit_code` không trùng.
    /// 
    /// Nơi chuyển tiếp:
    /// - Dữ liệu mới sẽ xuất hiện tại:
    ///   - `GET /api/StockUnit/import-options` (nếu supports_import = true)
    ///   - `GET /api/StockUnit/export-options` (nếu supports_export = true)
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
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

        return Ok(new
        {
            Success = true,
            Message = "Tạo đơn vị thành công.",
            Data = new { entity.StockUnitId, entity.UnitCode, entity.UnitName }
        });
    }

    /// <summary>
    /// Cập nhật thông tin đơn vị đã tồn tại.
    /// </summary>
    /// <remarks>
    /// Route: `PUT /api/StockUnit/{id}`
    /// Phân quyền: `ADMIN`
    /// 
    /// Công dụng:
    /// - Cho phép Admin sửa thông tin đơn vị (partial update).
    /// - Có thể đổi cờ phân loại nhập/xuất.
    /// - Không cho lưu trạng thái cả 2 cờ đều `false`.
    /// 
    /// Nơi ảnh hưởng:
    /// - Kết quả lọc danh sách ở:
    ///   - `GET /api/StockUnit/import-options`
    ///   - `GET /api/StockUnit/export-options`
    /// - Validate của:
    ///   - `POST /api/StockHistory/import`
    ///   - `POST /api/StockHistory/export`
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
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
    /// Cập nhật trạng thái hoạt động của đơn vị (Active/Inactive).
    /// </summary>
    /// <remarks>
    /// Route: `PUT /api/StockUnit/{id}/status`
    /// Phân quyền: `ADMIN`
    ///
    /// Công dụng:
    /// - Cho phép chuyển qua lại giữa Active và Inactive bằng API chuyên biệt.
    /// - Tạo thêm cách quản lý trạng thái ngoài cách cập nhật qua `PUT /api/StockUnit/{id}`.
    ///
    /// Lưu ý:
    /// - `isActive = true`  => đơn vị có thể xuất hiện lại trong danh sách options (nếu thỏa supports_import/export).
    /// - `isActive = false` => đơn vị bị ẩn khỏi danh sách options.
    /// </remarks>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateUnitStatus(int id, [FromBody] UpdateStockUnitStatusRequest request)
    {
        if (request.IsActive is null)
        {
            return BadRequest(new { Success = false, Message = "isActive là bắt buộc." });
        }

        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị." });
        }

        entity.IsActive = request.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = entity.IsActive
                ? "Đã chuyển đơn vị sang trạng thái Active."
                : "Đã chuyển đơn vị sang trạng thái Inactive."
        });
    }

    /// <summary>
    /// Helper map entity `StockUnit` sang DTO trả cho FE.
    /// </summary>
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