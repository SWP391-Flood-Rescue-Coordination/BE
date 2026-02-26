using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "COORDINATOR,ADMIN")]
public class CoordinatorController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CoordinatorController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Compatible endpoint from Tuan branch:
    /// GET /api/Coordinator/all-requests
    /// </summary>
    [HttpGet("all-requests")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, [FromQuery] int? priorityId = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (priorityId.HasValue)
        {
            query = query.Where(r => r.PriorityLevelId == priorityId.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                CitizenName = r.Citizen != null ? (r.Citizen.FullName ?? string.Empty) : string.Empty,
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : null,
                Title = r.Title,
                Phone = r.Phone,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                Status = r.Status ?? "Pending",
                NumberOfAffectedPeople = r.NumberOfAffectedPeople,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = requests.Count, Data = requests });
    }

    /// <summary>
    /// Compatible endpoint from Tuan branch:
    /// PUT /api/Coordinator/update-request/{id}
    /// </summary>
    [HttpPut("update-request/{id:int}")]
    public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateRequestFromCoordinatorDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            request.Status = dto.Status;
        }

        if (dto.PriorityLevelId.HasValue)
        {
            request.PriorityLevelId = dto.PriorityLevelId.Value;
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cap nhat yeu cau thanh cong" });
    }
}
