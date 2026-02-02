using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    /// Xem toàn bộ các yêu cầu cứu hộ (đã đăng nhập và vãng lai)
    /// Hỗ trợ lọc theo trạng thái và mức độ ưu tiên
    /// </summary>
    [HttpGet("all-requests")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, [FromQuery] int? priorityId = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
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
                // Ưu tiên FullName từ User table, nếu không có thì dùng ContactName từ Request table
                CitizenName = r.Citizen != null ? r.Citizen.FullName : r.ContactName,
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title = r.Title,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status,
                NumberOfPeople = r.NumberOfPeople,
                HasChildren = r.HasChildren,
                HasElderly = r.HasElderly,
                HasDisabled = r.HasDisabled,
                SpecialNotes = r.SpecialNotes,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = requests.Count, Data = requests });
    }

    /// <summary>
    /// Cập nhật trạng thái và mức độ ưu tiên cho yêu cầu cứu hộ
    /// </summary>
    [HttpPut("update-request/{id}")]
    public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateRequestFromCoordinatorDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);
        
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            request.Status = dto.Status;
        }

        if (dto.PriorityLevelId.HasValue)
        {
            request.PriorityLevelId = dto.PriorityLevelId.Value;
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }
}
