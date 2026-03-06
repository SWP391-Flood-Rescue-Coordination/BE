using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER")]
public class ManagerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách tất cả phương tiện với khả năng lọc theo trạng thái (Chỉ Manager)
    /// </summary>
    /// <param name="status">Trạng thái lọc (Available, InUse, Maintenance, etc.)</param>
    [HttpGet("vehicles")]
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
    /// Cập nhật nhanh trạng thái của phương tiện (Chỉ Manager)
    /// </summary>
    [HttpPatch("vehicles/{id}/status")]
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
