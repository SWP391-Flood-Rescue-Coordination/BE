using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RescueRequestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RescueRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo yêu cầu cứu hộ mới (Cho phép cả khách vãng lai)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        // Kiểm tra xem user có đăng nhập không
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        
        var request = new RescueRequest
        {
            CitizenId = userId,
            ContactName = userId == null ? dto.ContactName : null,
            ContactPhone = userId == null ? dto.ContactPhone : null,
            Title = dto.Title,
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address,
            NumberOfPeople = dto.NumberOfPeople,
            HasChildren = dto.HasChildren,
            HasElderly = dto.HasElderly,
            HasDisabled = dto.HasDisabled,
            SpecialNotes = dto.SpecialNotes,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.RescueRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Tạo yêu cầu cứu hộ thành công", RequestId = request.RequestId });
    }

    /// <summary>
    /// Lấy danh sách yêu cầu cứu hộ của citizen đang đăng nhập
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var requests = await _context.RescueRequests
            .Where(r => r.CitizenId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                CitizenName = r.Citizen != null ? r.Citizen.FullName : "", // Chắc chắn có FullName vì đã login
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : "",
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

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Xem chi tiết một yêu cầu
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequestById(int id)
    {
        var request = await _context.RescueRequests
            .Include(r => r.Citizen)
            .Where(r => r.RequestId == id)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                // Xử lý cả trường hợp có và không có Citizen
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
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Coordinator - Cập nhật trạng thái yêu cầu (xác minh, phân loại)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "COORDINATOR,ADMIN")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);
        
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        request.Status = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// Citizen - Xác nhận đã được cứu hộ
    /// </summary>
    [HttpPut("{id}/confirm-rescued")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> ConfirmRescued(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == userId);
        
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        request.Status = "COMPLETED";
        request.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Xác nhận đã được cứu hộ thành công" });
    }
}