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

    public VehicleController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Manager/Admin - Xem danh sách tất cả phương tiện
    /// </summary>
    /// <param name="status">Lọc theo trạng thái (Available, InUse, Maintenance, Disabled)</param>
    [HttpGet]
    public async Task<IActionResult> GetAllVehicles([FromQuery] string? status = null)
    {
        var validStatuses = new[] { "AVAILABLE", "INUSE", "MAINTENANCE" };

        var query = _context.Vehicles
            .Include(v => v.VehicleType)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            var statusUpper = status.Trim().ToUpper();
            if (!validStatuses.Contains(statusUpper))
            {
                return BadRequest(new 
                { 
                    Success = false, 
                    Message = $"Trạng thái '{status}' không hợp lệ. Các trạng thái hợp lệ là: {string.Join(", ", validStatuses)}" 
                });
            }
            query = query.Where(v => v.Status.ToUpper() == statusUpper);
        }

        var vehicles = await query
            .OrderByDescending(v => v.UpdatedAt)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                VehicleCode = v.VehicleCode,
                VehicleName = v.VehicleName,
                VehicleTypeName = v.VehicleType != null ? v.VehicleType.TypeName : "",
                LicensePlate = v.LicensePlate,
                Capacity = v.Capacity,
                Status = v.Status,
                CurrentLocation = v.CurrentLocation,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                LastMaintenance = v.LastMaintenance,
                UpdatedAt = v.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = vehicles, Count = vehicles.Count });
    }

    /// <summary>
    /// Manager/Admin - Xem chi tiết một phương tiện
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicleById(int id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Where(v => v.VehicleId == id)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                VehicleCode = v.VehicleCode,
                VehicleName = v.VehicleName,
                VehicleTypeName = v.VehicleType != null ? v.VehicleType.TypeName : "",
                LicensePlate = v.LicensePlate,
                Capacity = v.Capacity,
                Status = v.Status,
                CurrentLocation = v.CurrentLocation,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                LastMaintenance = v.LastMaintenance,
                UpdatedAt = v.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        return Ok(new { Success = true, Data = vehicle });
    }

    /// <summary>
    /// Manager/Admin - Cập nhật thông tin phương tiện
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto request)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        // Kiểm tra VehicleTypeId hợp lệ nếu có gửi lên
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
            vehicle.VehicleName = request.VehicleName;

        if (request.Capacity.HasValue)
            vehicle.Capacity = request.Capacity.Value;

        if (request.Status is not null)
        {
            var currentStatus = vehicle.Status.Trim().ToUpper();
            var newStatus = request.Status.Trim().ToUpper();
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

            vehicle.Status = newStatus;
        }

        if (request.CurrentLocation is not null)
            vehicle.CurrentLocation = request.CurrentLocation;

        if (request.Latitude.HasValue)
            vehicle.Latitude = request.Latitude.Value;

        if (request.Longitude.HasValue)
            vehicle.Longitude = request.Longitude.Value;

        if (request.LastMaintenance.HasValue)
            vehicle.LastMaintenance = request.LastMaintenance.Value;

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Trả về thông tin mới nhất sau khi cập nhật
        var updated = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Where(v => v.VehicleId == id)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                VehicleCode = v.VehicleCode,
                VehicleName = v.VehicleName,
                VehicleTypeName = v.VehicleType != null ? v.VehicleType.TypeName : "",
                LicensePlate = v.LicensePlate,
                Capacity = v.Capacity,
                Status = v.Status,
                CurrentLocation = v.CurrentLocation,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                LastMaintenance = v.LastMaintenance,
                UpdatedAt = v.UpdatedAt
            })
            .FirstAsync();

        return Ok(new { Success = true, Message = "Cập nhật phương tiện thành công", Data = updated });
    }

    /// <summary>
    /// Manager/Admin - Thêm mới phương tiện
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto request)
    {
        // Kiểm tra VehicleCode đã tồn tại chưa
        if (await _context.Vehicles.AnyAsync(v => v.VehicleCode == request.VehicleCode))
        {
            return BadRequest(new { Success = false, Message = "Mã phương tiện đã tồn tại" });
        }

        // Kiểm tra VehicleTypeId hợp lệ
        var vehicleTypeExists = await _context.VehicleTypes
            .AnyAsync(vt => vt.VehicleTypeId == request.VehicleTypeId);

        if (!vehicleTypeExists)
        {
            return BadRequest(new { Success = false, Message = "Loại phương tiện không tồn tại" });
        }

        var statusToSet = string.IsNullOrWhiteSpace(request.Status) ? "AVAILABLE" : request.Status.Trim().ToUpper();
        var manualValidStatuses = new[] { "AVAILABLE", "MAINTENANCE" };
        if (!manualValidStatuses.Contains(statusToSet))
        {
            return BadRequest(new 
            { 
                Success = false, 
                Message = $"Trạng thái '{request.Status}' không hợp lệ cho phương tiện mới. Vui lòng chọn: {string.Join(", ", manualValidStatuses)}" 
            });
        }

        var vehicle = new Vehicle
        {
            VehicleCode = request.VehicleCode,
            VehicleName = request.VehicleName,
            VehicleTypeId = request.VehicleTypeId,
            LicensePlate = request.LicensePlate,
            Capacity = request.Capacity,
            Status = statusToSet,
            CurrentLocation = request.CurrentLocation,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LastMaintenance = request.LastMaintenance,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var response = await _context.Vehicles
            .Include(v => v.VehicleType)
            .Where(v => v.VehicleId == vehicle.VehicleId)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                VehicleCode = v.VehicleCode,
                VehicleName = v.VehicleName,
                VehicleTypeName = v.VehicleType != null ? v.VehicleType.TypeName : "",
                LicensePlate = v.LicensePlate,
                Capacity = v.Capacity,
                Status = v.Status,
                CurrentLocation = v.CurrentLocation,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                LastMaintenance = v.LastMaintenance,
                UpdatedAt = v.UpdatedAt
            })
            .FirstAsync();

        return Ok(new { Success = true, Message = "Thành công thêm phương tiện", Data = response });
    }

    /// <summary>
    /// Manager/Admin - Xóa phương tiện
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);

        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        // 1. Chỉ chặn xóa nếu phương tiện ĐANG trong nhiệm vụ (Status = INUSE)
        if (vehicle.Status.ToUpper() == "INUSE")
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
