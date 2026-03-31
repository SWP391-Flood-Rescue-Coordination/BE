using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// VehicleController: Quản lý danh mục và trạng thái các phương tiện cứu hộ (Thuyền, Xe lội nước, Trực thăng...).
/// Cho phép điều phối viên và quản lý theo dõi vị trí, khả năng tải và lịch sử bảo trì của đội xe.
/// </summary>
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
    /// Tự động sinh Mã phương tiện (VehicleCode) dựa trên loại xe và số thứ tự tăng dần.
    /// Ví dụ: BOAT-001, BOAT-002, HELI-001...
    /// </summary>
    /// <param name="vehicleTypeId">ID loại phương tiện.</param>
    /// <param name="typeCode">Mã loại phương tiện (dùng làm tiền tố mặc định).</param>
    /// <returns>Chuỗi mã phương tiện duy nhất.</returns>
    private async Task<string> GenerateVehicleCodeAsync(int vehicleTypeId, string typeCode)
    {
        // 1. Lấy danh sách các mã hiện có của cùng loại xe này
        var existingCodes = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.VehicleTypeId == vehicleTypeId && !string.IsNullOrWhiteSpace(v.VehicleCode))
            .Select(v => v.VehicleCode)
            .ToListAsync();

        // 2. Xác định tiền tố (Prefix): Ưu tiên dùng tiền tố của các xe cùng loại đã có
        var existingPrefix = existingCodes
            .Select(code => code.Split('-', 2)[0].Trim().ToUpperInvariant())
            .FirstOrDefault(prefix => !string.IsNullOrWhiteSpace(prefix));

        var prefix = !string.IsNullOrWhiteSpace(existingPrefix)
            ? existingPrefix
            : NormalizeVehicleCodePrefix(typeCode); // Nếu chưa có xe nào thì chuẩn hóa từ TypeCode

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "VEH"; // Fallback cuối cùng
        }

        // 3. Tìm số thứ tự lớn nhất hiện tại để cộng thêm 1
        var nextSequence = existingCodes
            .Select(code => TryExtractVehicleCodeSequence(code, prefix))
            .Where(sequence => sequence.HasValue)
            .Select(sequence => sequence!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        // 4. Trả về mã định dạng Prefix-NNN (ví dụ BOAT-005)
        return $"{prefix}-{nextSequence:D3}";
    }

    /// <summary>
    /// Lấy danh sách toàn bộ phương tiện cứu hộ.
    /// Hỗ trợ lọc theo trạng thái hoạt động.
    /// </summary>
    /// <param name="status">Trạng thái cần lọc: AVAILABLE, INUSE, MAINTENANCE.</param>
    /// <returns>Danh sách phương tiện kèm thông tin chi tiết loại xe và tọa độ.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllVehicles([FromQuery] string? status = null)
    {
        var validStatuses = new[] { "AVAILABLE", "INUSE", "MAINTENANCE" };

        var query = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .AsQueryable();

        // 1. Lọc theo trạng thái (nếu có)
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusUpper = status.Trim().ToUpperInvariant();
            if (!validStatuses.Contains(statusUpper))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Trạng thái '{status}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}"
                });
            }
            query = query.Where(v => (v.Status ?? string.Empty).ToUpper() == statusUpper);
        }

        // 2. Thực thi truy vấn và sắp xếp theo ngày cập nhật mới nhất
        var vehicles = await query
            .OrderByDescending(v => v.UpdatedAt)
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = vehicles.Select(ToVehicleResponseDto).ToList(),
            Count = vehicles.Count
        });
    }

    /// <summary>
    /// Manager/Admin - Xem chi tiết một phương tiện
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicleById(int id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        return Ok(new { Success = true, Data = ToVehicleResponseDto(vehicle) });
    }

    /// <summary>
    /// MANAGER / ADMIN - Cập nhật thông tin chi tiết cho một phương tiện.
    /// Có các ràng buộc nghiệp vụ về biển số và trạng thái hoạt động.
    /// </summary>
    /// <param name="id">ID phương tiện cần sửa.</param>
    /// <param name="request">Dữ liệu cập nhật mới.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto request)
    {
        // 1. Tìm phương tiện
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện này trong hệ thống." });
        }

        // 2. Kiểm tra trùng lặp Biển số (LicensePlate)
        if (request.LicensePlate is not null)
        {
            var licensePlate = request.LicensePlate.Trim();
            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                return BadRequest(new { Success = false, Message = "Biển số xe không được để trống." });
            }

            var duplicatedPlate = await _context.Vehicles
                .AnyAsync(v => v.VehicleId != id && v.LicensePlate == licensePlate);

            if (duplicatedPlate)
            {
                return BadRequest(new { Success = false, Message = "Biển số xe này đã được đăng ký cho phương tiện khác." });
            }

            vehicle.LicensePlate = licensePlate;
        }

        // 3. Cập nhật Loại phương tiện (nếu có)
        if (request.VehicleTypeId.HasValue)
        {
            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == request.VehicleTypeId.Value);

            if (!vehicleTypeExists)
            {
                return BadRequest(new { Success = false, Message = "Loại phương tiện được chọn không tồn tại." });
            }

            vehicle.VehicleTypeId = request.VehicleTypeId.Value;
        }

        // 4. Cập nhật các trường thông tin cơ bản
        if (request.VehicleName is not null)
        {
            vehicle.VehicleName = string.IsNullOrWhiteSpace(request.VehicleName) ? null : request.VehicleName.Trim();
        }

        if (request.Capacity.HasValue)
        {
            vehicle.Capacity = request.Capacity.Value;
        }

        // 5. Logic chuyển đổi trạng thái (Status Transition)
        if (request.Status is not null)
        {
            var currentStatus = (vehicle.Status ?? string.Empty).Trim().ToUpperInvariant();
            var newStatus = request.Status.Trim().ToUpperInvariant();
            var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };

            // Ràng buộc: Chỉ cho phép Manager cập nhật thủ công sang AVAILABLE hoặc MAINTENANCE
            if (!manualValidStatuses.Contains(newStatus))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Không thể cập nhật thủ công thành trạng thái '{request.Status}'. Chỉ có thể đặt thành: {string.Join(", ", manualValidStatuses)}"
                });
            }

            // Ràng buộc bảo vệ: Không cho phép đổi trạng thái nếu xe đang bận làm nhiệm vụ (INUSE)
            if (currentStatus == "INUSE" && currentStatus != newStatus)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Phương tiện hiện đang trong nhiệm vụ (INUSE), không thể thay đổi trạng thái."
                });
            }

            // Tự động chốt ngày bảo trì nếu chuyển sang trạng thái MAINTENANCE
            if (currentStatus != "MAINTENANCE" && newStatus == "MAINTENANCE" && !request.LastMaintenance.HasValue)
            {
                vehicle.LastMaintenance = DateTime.UtcNow;
            }

            vehicle.Status = newStatus;
        }

        // 6. Cập nhật Vị trí và Tọa độ
        if (request.CurrentLocation is not null)
        {
            vehicle.CurrentLocation = string.IsNullOrWhiteSpace(request.CurrentLocation) ? null : request.CurrentLocation.Trim();
        }

        if (request.Latitude.HasValue)
        {
            vehicle.Latitude = request.Latitude.Value;
        }

        if (request.Longitude.HasValue)
        {
            vehicle.Longitude = request.Longitude.Value;
        }

        if (request.LastMaintenance.HasValue)
        {
            vehicle.LastMaintenance = request.LastMaintenance.Value;
        }

        // 7. Lưu thay đổi
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Truy vấn lại để lấy đầy đủ thông tin (bao gồm cả quan hệ VehicleType) để trả về cho FE cập nhật UI
        var updatedVehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstAsync(v => v.VehicleId == id);

        return Ok(new
        {
            Success = true,
            Message = "Cập nhật thông tin phương tiện thành công.",
            Data = ToVehicleResponseDto(updatedVehicle)
        });
    }

    /// <summary>
    /// MANAGER / ADMIN - Thêm một phương tiện cứu hộ mới vào hệ thống.
    /// Mã phương tiện (VehicleCode) sẽ được tự động sinh dựa trên loại xe.
    /// </summary>
    /// <param name="request">Thông tin xe mới cần thêm.</param>
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto request)
    {
        // 1. Kiểm tra biển số không được trống và không được trùng
        var licensePlate = request.LicensePlate.Trim();
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            return BadRequest(new { Success = false, Message = "Biển số xe không được để trống." });
        }

        if (await _context.Vehicles.AnyAsync(v => v.LicensePlate == licensePlate))
        {
            return BadRequest(new { Success = false, Message = "Biển số xe này đã tồn tại trong hệ thống." });
        }

        // 2. Kiểm tra loại phương tiện
        var vehicleType = await _context.VehicleTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(vt => vt.VehicleTypeId == request.VehicleTypeId);

        if (vehicleType == null)
        {
            return BadRequest(new { Success = false, Message = "Loại phương tiện không tồn tại." });
        }

        // 3. Chuẩn hóa trạng thái ban đầu
        var statusToSet = string.IsNullOrWhiteSpace(request.Status)
            ? "AVAILABLE"
            : request.Status.Trim().ToUpperInvariant();

        var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };
        if (!manualValidStatuses.Contains(statusToSet))
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Trạng thái '{request.Status}' không hợp lệ cho phương tiện mới. Chỉ chấp nhận: {string.Join(", ", manualValidStatuses)}"
            });
        }

        // 4. Tự động sinh Mã phương tiện (Ví dụ: BOAT-003)
        var generatedVehicleCode = await GenerateVehicleCodeAsync(request.VehicleTypeId, vehicleType.TypeCode);

        // 5. Khởi tạo thực thể và lưu
        var vehicle = new Vehicle
        {
            VehicleCode = generatedVehicleCode,
            VehicleName = string.IsNullOrWhiteSpace(request.VehicleName) ? null : request.VehicleName.Trim(),
            VehicleTypeId = request.VehicleTypeId,
            LicensePlate = licensePlate,
            Capacity = request.Capacity,
            Status = statusToSet,
            CurrentLocation = string.IsNullOrWhiteSpace(request.CurrentLocation) ? null : request.CurrentLocation.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LastMaintenance = request.LastMaintenance,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        // 6. Truy vấn thông tin đầy đủ để trả về kết quả
        var createdVehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstAsync(v => v.VehicleId == vehicle.VehicleId);

        return Ok(new
        {
            Success = true,
            Message = "Thêm phương tiện mới vào đội cứu hộ thành công!",
            Data = ToVehicleResponseDto(createdVehicle)
        });
    }

    /// <summary>
    /// MANAGER / ADMIN - Xóa phương tiện khỏi hệ thống.
    /// Chỉ cho phép xóa nếu phương tiện không bận nhiệm vụ.
    /// Đồng thời tự động dọn dẹp các bản ghi liên quan trong lịch sử nhiệm vụ.
    /// </summary>
    /// <param name="id">ID phương tiện cần xóa.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        // 1. Tìm phương tiện
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện." });
        }

        // 2. Ràng buộc: Chặn xóa nếu xe đang trong nhiệm vụ (Status = INUSE)
        if ((vehicle.Status ?? string.Empty).Trim().ToUpperInvariant() == "INUSE")
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Không thể xóa phương tiện khi đang trong nhiệm vụ (INUSE). Vui lòng đợi nhiệm vụ hoàn tất trước khi thực hiện xóa."
            });
        }

        // 3. Dọn dẹp ràng buộc dữ liệu: Xóa các bản ghi tham chiếu trong lịch sử phân công (RescueOperationVehicles)
        // để tránh lỗi vi phạm khóa ngoại (Foreign Key Constraint) khi xóa thực thể chính.
        var historyRecords = _context.RescueOperationVehicles.Where(rov => rov.VehicleId == id);
        if (await historyRecords.AnyAsync())
        {
            _context.RescueOperationVehicles.RemoveRange(historyRecords);
        }

        // 4. Xóa chính thực thể phương tiện và lưu thay đổi
        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Đã xóa phương tiện và dữ liệu liên quan thành công khỏi hệ thống." });
    }
}
