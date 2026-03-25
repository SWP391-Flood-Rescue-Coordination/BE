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
    /// Manager/Admin/Coordinator - Xem danh sách tất cả phương tiện.
    /// </summary>
    /// <param name="status">Lọc theo trạng thái (Available, InUse, Maintenance).</param>
    [HttpGet]
    public async Task<IActionResult> GetAllVehicles([FromQuery] string? status = null)
    {
        var validStatuses = new[] { "AVAILABLE", "INUSE", "MAINTENANCE" };

        var query = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusUpper = status.Trim().ToUpperInvariant();
            if (!validStatuses.Contains(statusUpper))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Trạng thái '{status}' không hợp lệ. Các trạng thái hợp lệ là: {string.Join(", ", validStatuses)}"
                });
            }
            // Null-safe để việc lọc trạng thái không vỡ nếu có bản ghi thiếu status.
            query = query.Where(v => (v.Status ?? string.Empty).ToUpper() == statusUpper);
        }

        // Danh sách xe hiện trả cả tọa độ để màn quản lý có thể dùng lại trực tiếp cho bảng và popup bản đồ.
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
    /// Manager/Admin - Cập nhật thông tin phương tiện
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto request)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        // Kiểm tra sớm biển số để trả lỗi nghiệp vụ rõ ràng thay vì để DB ném lỗi unique index.
        if (request.LicensePlate is not null)
        {
            var licensePlate = request.LicensePlate.Trim();
            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                return BadRequest(new { Success = false, Message = "Biển số không được để trống" });
            }

            var duplicatedPlate = await _context.Vehicles
                .AnyAsync(v => v.VehicleId != id && v.LicensePlate == licensePlate);

            if (duplicatedPlate)
            {
                return BadRequest(new { Success = false, Message = "Biển số đã tồn tại" });
            }

            vehicle.LicensePlate = licensePlate;
        }

        if (request.VehicleTypeId.HasValue)
        {
            var vehicleTypeExists = await _context.VehicleTypes
                .AnyAsync(vt => vt.VehicleTypeId == request.VehicleTypeId.Value);

            if (!vehicleTypeExists)
            {
                return BadRequest(new { Success = false, Message = "Loại phương tiện không tồn tại" });
            }

            vehicle.VehicleTypeId = request.VehicleTypeId.Value;
        }

        if (request.VehicleName is not null)
        {
            vehicle.VehicleName = string.IsNullOrWhiteSpace(request.VehicleName) ? null : request.VehicleName.Trim();
        }

        if (request.Capacity.HasValue)
        {
            vehicle.Capacity = request.Capacity.Value;
        }

        if (request.Status is not null)
        {
            // Chuẩn hóa trạng thái trước khi so sánh để luồng cập nhật thủ công bám đúng nghiệp vụ.
            var currentStatus = (vehicle.Status ?? string.Empty).Trim().ToUpperInvariant();
            var newStatus = request.Status.Trim().ToUpperInvariant();
            var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };

            // 1. Kiểm tra nếu trạng thái mới không nằm trong danh sách cho phép cập nhật thủ công
            if (!manualValidStatuses.Contains(newStatus))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Không thể cập nhật thủ công thành trạng thái '{request.Status}'. Chỉ có thể đặt thành: {string.Join(", ", manualValidStatuses)}"
                });
            }
            // 2. Không cho phép đổi trạng thái nếu hiện tại đang INUSE
            if (currentStatus == "INUSE" && currentStatus != newStatus)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Không thể thay đổi trạng thái khi phương tiện đang trong nhiệm vụ (INUSE)."
                });
            }

            // Nếu vừa chuyển từ trạng thái khác sang Bảo trì thì backend tự chốt ngày bảo trì gần nhất.
            if (currentStatus != "MAINTENANCE" && newStatus == "MAINTENANCE" && !request.LastMaintenance.HasValue)
            {
                vehicle.LastMaintenance = DateTime.UtcNow;
            }

            vehicle.Status = newStatus;
        }

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

        // Mọi lần cập nhật thành công đều ghi nhận lại thời điểm cập nhật gần nhất.
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        // Trả về thông tin mới nhất sau khi cập nhật
        var updatedVehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstAsync(v => v.VehicleId == id);

        return Ok(new
        {
            Success = true,
            Message = "Cập nhật phương tiện thành công",
            Data = ToVehicleResponseDto(updatedVehicle)
        });
    }

    /// <summary>
    /// Manager/Admin - Thêm mới phương tiện
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto request)
    {
        var licensePlate = request.LicensePlate.Trim();
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            return BadRequest(new { Success = false, Message = "Biển số không được để trống" });
        }

        // Kiểm tra trùng biển số trước khi thêm để tránh phát sinh lỗi ràng buộc từ DB.
        if (await _context.Vehicles.AnyAsync(v => v.LicensePlate == licensePlate))
        {
            return BadRequest(new { Success = false, Message = "Biển số đã tồn tại" });
        }

        // Tải sẵn loại xe để vừa validate vừa dùng luôn cho bước sinh VehicleCode.
        var vehicleType = await _context.VehicleTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(vt => vt.VehicleTypeId == request.VehicleTypeId);

        if (vehicleType == null)
        {
            return BadRequest(new { Success = false, Message = "Loại phương tiện không tồn tại" });
        }

        var statusToSet = string.IsNullOrWhiteSpace(request.Status)
            ? "AVAILABLE"
            : request.Status.Trim().ToUpperInvariant();

        var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };
        if (!manualValidStatuses.Contains(statusToSet))
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Trạng thái '{request.Status}' không hợp lệ cho phương tiện mới. Vui lòng chọn: {string.Join(", ", manualValidStatuses)}"
            });
        }

        // VehicleCode được sinh ở backend vì đây là mã nội bộ, không phải dữ liệu người dùng cần nhập.
        var generatedVehicleCode = await GenerateVehicleCodeAsync(request.VehicleTypeId, vehicleType.TypeCode);

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
            // Ngày bảo trì gần nhất chỉ lưu khi FE gửi lên; xe mới có thể để trống nếu chưa bảo trì lần nào.
            LastMaintenance = request.LastMaintenance,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var createdVehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstAsync(v => v.VehicleId == vehicle.VehicleId);

        return Ok(new
        {
            Success = true,
            Message = "Thêm phương tiện thành công!",
            Data = ToVehicleResponseDto(createdVehicle)
        });
    }

    /// <summary>
    /// Manager/Admin - Xóa phương tiện
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        // 1. Chỉ chặn xóa nếu phương tiện ĐANG trong nhiệm vụ (Status = INUSE)
        if ((vehicle.Status ?? string.Empty).Trim().ToUpperInvariant() == "INUSE")
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Không thể xóa phương tiện khi đang trong nhiệm vụ (INUSE). Vui lòng đợi nhiệm vụ hoàn tất hoặc chuyển sang trạng thái khác."
            });
        }

        // 2. Xóa các bản ghi liên quan trong lịch sử nhiệm vụ (RescueOperationVehicles) để tránh lỗi khóa ngoại
        var historyRecords = _context.RescueOperationVehicles.Where(rov => rov.VehicleId == id);
        if (await historyRecords.AnyAsync())
        {
            _context.RescueOperationVehicles.RemoveRange(historyRecords);
        }

        // 3. Xóa phương tiện
        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Xóa phương tiện và các dữ liệu liên quan thành công" });
    }
}
