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
        var query = _context.Vehicles
            .Include(v => v.VehicleType)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(v => v.Status == status);
        }

        var vehicles = await query
            .OrderBy(v => v.VehicleCode)
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
    /// Manager/Admin - Cập nhật nhanh trạng thái của phương tiện
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateVehicleStatus(int id, [FromBody] UpdateVehicleStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { Success = false, Message = "Trạng thái không được để trống" });
        }

        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy phương tiện" });
        }

        vehicle.Status = dto.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }
}
