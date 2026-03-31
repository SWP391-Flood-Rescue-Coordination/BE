using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
public class VehicleController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo VehicleController với DbContext.
    /// </summary>
    public VehicleController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Map dữ liệu từ Entity Vehicle sang VehicleResponseDto.
    /// Giúp code đảm bảo Null-safety, phân giải các biến rỗng thành chuỗi trống, chống văng màn hình giao diện.
    /// </summary>
    private static VehicleResponseDto ToVehicleResponseDto(Vehicle vehicle)
    {
        return new VehicleResponseDto
        {
            VehicleId = vehicle.VehicleId,
            VehicleCode = vehicle.VehicleCode ?? string.Empty,
            VehicleName = vehicle.VehicleName,
            VehicleTypeName = vehicle.VehicleType?.TypeName ?? string.Empty,
            LicensePlate = vehicle.LicensePlate ?? string.Empty,
            Capacity = vehicle.Capacity,
            Status = vehicle.Status ?? string.Empty,
            CurrentLocation = vehicle.CurrentLocation,
            Latitude = vehicle.Latitude,
            Longitude = vehicle.Longitude,
            LastMaintenance = vehicle.LastMaintenance,
            UpdatedAt = vehicle.UpdatedAt
        };
    }

    /// <summary>
    /// Chuẩn hóa tiền tố mã xe để mã sinh ra ngắn gọn và đồng nhất hơn.
    /// </summary>
    private static string NormalizeVehicleCodePrefix(string? rawPrefix)
    {
        var prefix = (rawPrefix ?? string.Empty).Trim().ToUpperInvariant();

        return prefix switch
        {
            "HELICOPTER" => "HELI",
            "AMPHIBIOUS" => "AMPH",
            _ => prefix
        };
    }

    /// <summary>
    /// Tách phần số thứ tự ở cuối các mã dạng BOAT-001 hoặc HELI-004.
    /// </summary>
    private static int? TryExtractVehicleCodeSequence(string? vehicleCode, string prefix)
    {
        if (string.IsNullOrWhiteSpace(vehicleCode))
        {
            return null;
        }

        var expectedPrefix = $"{prefix}-";
        if (!vehicleCode.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = vehicleCode[expectedPrefix.Length..];
        return int.TryParse(suffix, out var sequence) ? sequence : null;
    }

    /// <summary>
    /// Sinh VehicleCode ở backend để người dùng không phải nhập mã nội bộ của hệ thống.
    /// </summary>
    private async Task<string> GenerateVehicleCodeAsync(int vehicleTypeId, string typeCode)
    {
        // Ưu tiên dùng lại tiền tố hiện có của cùng loại xe để dữ liệu cũ và mới đồng nhất.
        var existingCodes = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.VehicleTypeId == vehicleTypeId && !string.IsNullOrWhiteSpace(v.VehicleCode))
            .Select(v => v.VehicleCode)
            .ToListAsync();

        // Chỉ fallback về TypeCode khi loại xe này chưa có bản ghi nào trước đó.
        var existingPrefix = existingCodes
            .Select(code => code.Split('-', 2)[0].Trim().ToUpperInvariant())
            .FirstOrDefault(prefix => !string.IsNullOrWhiteSpace(prefix));

        var prefix = !string.IsNullOrWhiteSpace(existingPrefix)
            ? existingPrefix
            : NormalizeVehicleCodePrefix(typeCode);

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "VEH";
        }

        var nextSequence = existingCodes
            .Select(code => TryExtractVehicleCodeSequence(code, prefix))
            .Where(sequence => sequence.HasValue)
            .Select(sequence => sequence!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}-{nextSequence:D3}";
    }

    /// <summary>
    /// API Quản lý (Manager/Admin/Coordinator): Lấy danh sách toàn bộ phương tiện cứu hộ.
    /// Hỗ trợ lọc theo trạng thái: AVAILABLE, INUSE, MAINTENANCE.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllVehicles([FromQuery] string? status = null)
    {
        var validStatuses = new[] { "AVAILABLE", "INUSE", "MAINTENANCE" };
        var query = _context.Vehicles.AsNoTracking().Include(v => v.VehicleType).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusUpper = status.Trim().ToUpperInvariant();
            if (!validStatuses.Contains(statusUpper))
                return BadRequest(new { Success = false, Message = "Trạng thái không hợp lệ." });
            
            query = query.Where(v => (v.Status ?? string.Empty).ToUpper() == statusUpper);
        }

        var vehicles = await query.OrderByDescending(v => v.UpdatedAt).ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = vehicles.Select(ToVehicleResponseDto).ToList(),
            Count = vehicles.Count
        });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Lấy thông tin chi tiết của một phương tiện.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicleById(int id)
    {
        var vehicle = await _context.Vehicles.AsNoTracking().Include(v => v.VehicleType).FirstOrDefaultAsync(v => v.VehicleId == id);
        if (vehicle == null) return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });

        return Ok(new { Success = true, Data = ToVehicleResponseDto(vehicle) });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Cập nhật thông tin phương tiện.
    /// Hạn chế: Không thể đổi trạng thái thủ công nếu xe đang bận nhiệm vụ (INUSE).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto request)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);
        if (vehicle == null) return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });

        // Cập nhật biển số (kiểm tra trùng lặp bản ghi khác)
        if (request.LicensePlate is not null)
        {
            var licensePlate = request.LicensePlate.Trim();
            if (await _context.Vehicles.AnyAsync(v => v.VehicleId != id && v.LicensePlate == licensePlate))
                return BadRequest(new { Success = false, Message = "Biển số đã tồn tại trên hệ thống." });
            vehicle.LicensePlate = licensePlate;
        }

        // Cập nhật loại xe
        if (request.VehicleTypeId.HasValue)
        {
            if (!await _context.VehicleTypes.AnyAsync(vt => vt.VehicleTypeId == request.VehicleTypeId.Value))
                return BadRequest(new { Success = false, Message = "Loại phương tiện không hợp lệ." });
            vehicle.VehicleTypeId = request.VehicleTypeId.Value;
        }

        if (request.VehicleName is not null) vehicle.VehicleName = request.VehicleName.Trim();
        if (request.Capacity.HasValue) vehicle.Capacity = request.Capacity.Value;

        // Cập nhật trạng thái thủ công (Chỉ dành cho Available hoặc Maintenance)
        if (request.Status is not null)
        {
            var currentStatus = (vehicle.Status ?? string.Empty).Trim().ToUpperInvariant();
            var newStatus = request.Status.Trim().ToUpperInvariant();
            var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };

            if (!manualValidStatuses.Contains(newStatus))
                return BadRequest(new { Success = false, Message = "Chỉ có thể cập nhật thủ công thành AVAILABLE hoặc MAINTENANCE." });

            if (currentStatus == "INUSE" && currentStatus != newStatus)
                return BadRequest(new { Success = false, Message = "Phương tiện đang làm nhiệm vụ, không thể đổi trạng thái." });

            if (currentStatus != "MAINTENANCE" && newStatus == "MAINTENANCE" && !request.LastMaintenance.HasValue)
                vehicle.LastMaintenance = DateTime.UtcNow;

            vehicle.Status = newStatus;
        }

        // Cập nhật tọa độ định vị thực tế của xe
        if (request.CurrentLocation is not null) vehicle.CurrentLocation = request.CurrentLocation.Trim();
        if (request.Latitude.HasValue) vehicle.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) vehicle.Longitude = request.Longitude.Value;
        if (request.LastMaintenance.HasValue) vehicle.LastMaintenance = request.LastMaintenance.Value;

        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật thành công." });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Thêm mới phương tiện vào đội.
    /// Hệ thống tự động sinh mã quản lý (VehicleCode) dựa trên loại xe.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto request)
    {
        var licensePlate = (request.LicensePlate ?? "").Trim();
        if (await _context.Vehicles.AnyAsync(v => v.LicensePlate == licensePlate))
            return BadRequest(new { Success = false, Message = "Biển số đã tồn tại." });

        var vehicleType = await _context.VehicleTypes.AsNoTracking().FirstOrDefaultAsync(vt => vt.VehicleTypeId == request.VehicleTypeId);
        if (vehicleType == null) return BadRequest(new { Success = false, Message = "Loại phương tiện không tồn tại." });

        // Tự động sinh mã xe (Ví dụ: BOAT-001)
        var generatedVehicleCode = await GenerateVehicleCodeAsync(request.VehicleTypeId, vehicleType.TypeCode);

        var vehicle = new Vehicle
        {
            VehicleCode = generatedVehicleCode,
            VehicleName = request.VehicleName?.Trim(),
            VehicleTypeId = request.VehicleTypeId,
            LicensePlate = licensePlate,
            Capacity = request.Capacity,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "AVAILABLE" : request.Status.Trim().ToUpperInvariant(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Thêm mới phương tiện thành công!" });
    }

    /// <summary>
    /// API Quản lý (Manager/Admin): Xoá phương tiện khỏi hệ thống.
    /// Lưu ý: Chỉ có thể xoá nếu xe KHÔNG đang thực hiện nhiệm vụ.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);
        if (vehicle == null) return NotFound(new { Success = false });

        if ((vehicle.Status ?? "").Trim().ToUpperInvariant() == "INUSE")
            return BadRequest(new { Success = false, Message = "Phương tiện đang làm nhiệm vụ, không thể xoá." });

        // Xoá lịch sử gán xe để tránh lỗi khoá ngoại
        var history = _context.RescueOperationVehicles.Where(rov => rov.VehicleId == id);
        _context.RescueOperationVehicles.RemoveRange(history);

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Đã xoá phương tiện." });
    }
}
