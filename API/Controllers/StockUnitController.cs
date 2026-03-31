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
    /// Constructor khởi tạo StockUnitController.
    /// </summary>
    /// <param name="context">DbContext thao tác dữ liệu bảng stock_units.</param>
    public StockUnitController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hợp lệ để hiển thị trong tùy chọn Nhập kho.
    /// Chỉ lấy các đơn vị đang hoạt động (IsActive) và có hỗ trợ nghiệp vụ Nhập (SupportsImport).
    /// </summary>
    /// <returns>Danh sách các đơn vị phù hợp cho form Nhập kho.</returns>
    [HttpGet("import-options")]
    public async Task<IActionResult> GetImportOptions()
    {
        // Truy vấn danh mục đơn vị, lọc theo điều kiện Nhập và Active
        var units = await _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && u.SupportsImport)
            .OrderBy(u => u.UnitName)
            .Select(ToOptionDto()) // Chuyển đổi sang định dạng Option cho FE
            .ToListAsync();

        return Ok(new { Success = true, Count = units.Count, Data = units });
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hợp lệ để hiển thị trong tùy chọn Xuất kho.
    /// Chỉ lấy các đơn vị đang hoạt động (IsActive) và có hỗ trợ nghiệp vụ Xuất (SupportsExport).
    /// </summary>
    /// <returns>Danh sách các đơn vị phù hợp cho form Xuất kho.</returns>
    [HttpGet("export-options")]
    public async Task<IActionResult> GetExportOptions()
    {
        // Truy vấn danh mục đơn vị, lọc theo điều kiện Xuất và Active
        var units = await _context.StockUnits
            .AsNoTracking()
            .Where(u => u.IsActive && u.SupportsExport)
            .OrderBy(u => u.UnitName)
            .Select(ToOptionDto()) // Chuyển đổi sang định dạng Option cho FE
            .ToListAsync();

        return Ok(new { Success = true, Count = units.Count, Data = units });
    }

    /// <summary>
    /// ADMIN - Tạo mới một đơn vị quản lý kho (Đơn vị cung cấp hoặc Đơn vị tiếp nhận).
    /// </summary>
    /// <param name="request">Thông tin đơn vị: Mã, Tên, Địa chỉ, Loại hình, Khả năng Nhập/Xuất.</param>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateStockUnitRequest request)
    {
        // 1. Chuẩn hóa và kiểm tra các trường bắt buộc
        var code = request.UnitCode.Trim();
        var name = request.UnitName.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { Success = false, Message = "Mã đơn vị (UnitCode) và Tên đơn vị (UnitName) không được để trống." });
        }

        // 2. Ràng buộc nghiệp vụ: Một đơn vị phải có ít nhất một chức năng (Nhập hoặc Xuất)
        if (!request.SupportsImport && !request.SupportsExport)
        {
            return BadRequest(new { Success = false, Message = "Đơn vị phải hỗ trợ ít nhất một nghiệp vụ: Nhập hoặc Xuất." });
        }

        // 3. Kiểm tra trùng lặp Mã đơn vị (UnitCode)
        var codeUpper = code.ToUpperInvariant();
        var duplicatedCode = await _context.StockUnits
            .AnyAsync(u => u.UnitCode.ToUpper() == codeUpper);

        if (duplicatedCode)
        {
            return BadRequest(new { Success = false, Message = "Mã đơn vị (UnitCode) này đã tồn tại trong hệ thống." });
        }

        // 4. Khởi tạo và lưu thực thể mới
        var entity = new StockUnit
        {
            UnitCode = code,
            UnitName = name,
            UnitType = string.IsNullOrWhiteSpace(request.UnitType) ? null : request.UnitType.Trim(),
            Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            SupportsImport = request.SupportsImport,
            SupportsExport = request.SupportsExport,
            IsActive = true, // Mặc định là Active khi tạo mới
            CreatedAt = DateTime.UtcNow
        };

        _context.StockUnits.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = "Tạo đơn vị mới thành công.",
            Data = new { entity.StockUnitId, entity.UnitCode, entity.UnitName }
        });
    }

    /// <summary>
    /// ADMIN - Cập nhật thông tin của một đơn vị quản lý kho hiện có.
    /// Cho phép cập nhật từng phần (Partial Update).
    /// </summary>
    /// <param name="id">ID của đơn vị cần sửa.</param>
    /// <param name="request">Các trường thông tin cần cập nhật.</param>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateStockUnitRequest request)
    {
        // 1. Tìm đơn vị trong DB
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị cần cập nhật." });
        }

        // 2. Cập nhật Mã đơn vị (Nếu có thay đổi)
        if (request.UnitCode != null)
        {
            var code = request.UnitCode.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { Success = false, Message = "Mã đơn vị (UnitCode) không được để trống." });
            }

            var codeUpper = code.ToUpperInvariant();
            // Kiểm tra xem mã mới có trùng với đơn vị khác không
            var duplicatedCode = await _context.StockUnits
                .AnyAsync(u => u.StockUnitId != id && u.UnitCode.ToUpper() == codeUpper);

            if (duplicatedCode)
            {
                return BadRequest(new { Success = false, Message = "Mã đơn vị mới đã tồn tại trong hệ thống." });
            }

            entity.UnitCode = code;
        }

        // 3. Cập nhật các trường thông tin khác nếu có giá trị trong Request
        if (request.UnitName != null)
        {
            var name = request.UnitName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { Success = false, Message = "Tên đơn vị không được để trống." });
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

        // 4. Ràng buộc: Sau khi sửa, đơn vị vẫn phải giữ ít nhất một nghiệp vụ
        if (!entity.SupportsImport && !entity.SupportsExport)
        {
            return BadRequest(new { Success = false, Message = "Đơn vị phải có ít nhất một nghiệp vụ: Nhập hoặc Xuất." });
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        // 5. Cập nhật thời gian và lưu
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật thông tin đơn vị thành công." });
    }

    /// <summary>
    /// ADMIN - Thay đổi trạng thái hoạt động của một đơn vị (Kích hoạt hoặc Vô hiệu hóa).
    /// </summary>
    /// <param name="id">ID đơn vị.</param>
    /// <param name="request">Trạng thái IsActive mới.</param>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateUnitStatus(int id, [FromBody] UpdateStockUnitStatusRequest request)
    {
        // 1. Kiểm tra đầu vào
        if (request.IsActive is null)
        {
            return BadRequest(new { Success = false, Message = "vui lòng cung cấp trạng thái isActive (true/false)." });
        }

        // 2. Kiểm tra tồn tại
        var entity = await _context.StockUnits.FirstOrDefaultAsync(u => u.StockUnitId == id);
        if (entity == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy đơn vị." });
        }

        // 3. Cập nhật và lưu thay đổi
        entity.IsActive = request.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = entity.IsActive
                ? "Đã kích hoạt đơn vị thành công."
                : "Đã vô hiệu hóa đơn vị thành công."
        });
    }

    /// <summary>
    /// Helper: Ánh xạ từ thực thể cơ sở dữ liệu sang định dạng DTO gọn nhẹ để trả về cho Front-end.
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