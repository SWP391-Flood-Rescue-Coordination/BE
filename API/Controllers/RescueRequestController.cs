using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
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
    /// Tạo yêu cầu cứu hộ mới (Hỗ trợ cả người dùng đăng nhập và khách vãng lai)
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Cho phép không cần đăng nhập
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        int? userId = null;
        string contactName = dto.ContactName ?? "Ẩn danh";
        string contactPhone = dto.ContactPhone ?? "  ";

        // Nếu người dùng đã đăng nhập, lấy thông tin từ token
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int id))
            {
                userId = id;
                // Nếu user đã đăng nhập mà không nhập contact info, có thể lấy từ profile user (tùy chọn)
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    contactName = user.FullName;
                    contactPhone = user.Phone;
                }
            }
        }

        var request = new RescueRequest
        {
            CitizenId = userId,
            ContactName = contactName,
            ContactPhone = contactPhone,
            Title = dto.Title,
            Description = dto.Description ?? "",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address ?? "",
            NumberOfPeople = dto.NumberOfPeople,
            HasChildren = dto.HasChildren,
            HasElderly = dto.HasElderly,
            HasDisabled = dto.HasDisabled,
            SpecialNotes = dto.SpecialNotes ?? "",
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
                Title = r.Title ?? "",
                Description = r.Description ?? "",
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address ?? "",
                Status = r.Status ?? "PENDING",
                NumberOfPeople = r.NumberOfPeople,
                HasChildren = r.HasChildren,
                HasElderly = r.HasElderly,
                HasDisabled = r.HasDisabled,
                SpecialNotes = r.SpecialNotes ?? "",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Lấy yêu cầu cứu hộ mới nhất của citizen đang đăng nhập
    /// </summary>
    [HttpGet("my-latest-request")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> GetMyLatestRequest()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var latestRequest = await _context.RescueRequests
            .Where(r => r.CitizenId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LatestRescueRequestDto
            {
                Title = r.Title ?? "",
                Description = r.Description ?? "",
                Address = r.Address ?? "",
                Status = r.Status ?? "PENDING",
                HasElderly = r.HasElderly,
                HasChildren = r.HasChildren,
                HasDisabled = r.HasDisabled,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (latestRequest == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        return Ok(new { Success = true, Data = latestRequest });
    }


    /// <summary>
    /// Coordinator/Admin/Manager - Lấy tất cả yêu cầu cứu hộ
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                CitizenName = r.Citizen != null ? r.Citizen.FullName : r.ContactName ?? "",
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone ?? "",
                Title = r.Title ?? "",
                Description = r.Description ?? "",
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address ?? "",
                Status = r.Status ?? "PENDING",
                NumberOfPeople = r.NumberOfPeople,
                HasChildren = r.HasChildren,
                HasElderly = r.HasElderly,
                HasDisabled = r.HasDisabled,
                SpecialNotes = r.SpecialNotes ?? "",
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
                CitizenName = r.Citizen != null ? r.Citizen.FullName : r.ContactName ?? "",
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone ?? "",
                Title = r.Title ?? "",
                Description = r.Description ?? "",
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address ?? "",
                Status = r.Status ?? "PENDING",
                NumberOfPeople = r.NumberOfPeople,
                HasChildren = r.HasChildren,
                HasElderly = r.HasElderly,
                HasDisabled = r.HasDisabled,
                SpecialNotes = r.SpecialNotes ?? "",
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
    /// Coordinator/Admin/Manager - Cập nhật trạng thái yêu cầu (xác minh, phân loại)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
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
    /// Manager/Admin - Cập nhật mức độ ưu tiên của yêu cầu
    /// </summary>
    [HttpPut("{id}/priority")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> UpdatePriority(int id, [FromBody] UpdatePriorityDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        request.PriorityLevelId = dto.PriorityLevelId;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật mức độ ưu tiên thành công" });
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

    /// <summary>
    /// Manager/Admin/Coordinator - Xem thống kê tổng quan (Dashboard)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalRequests = await _context.RescueRequests.CountAsync();

        // Thống kê theo trạng thái
        var pending = await _context.RescueRequests.CountAsync(r => r.Status == "PENDING");
        var inProgress = await _context.RescueRequests.CountAsync(r => r.Status == "IN_PROGRESS" || r.Status == "VERIFIED"); // Giả sử VERIFIED cũng đang xử lý
        var completed = await _context.RescueRequests.CountAsync(r => r.Status == "COMPLETED" || r.Status == "RESCUED");
        var cancelled = await _context.RescueRequests.CountAsync(r => r.Status == "CANCELLED");

        // Thống kê theo mức độ ưu tiên (Giả sử ID 3=High, 2=Medium, 1=Low)
        // Bạn cần map đúng ID với logic Priority của bạn sau này
        var highPriority = await _context.RescueRequests.CountAsync(r => r.PriorityLevelId == 3);
        var mediumPriority = await _context.RescueRequests.CountAsync(r => r.PriorityLevelId == 2);
        var lowPriority = await _context.RescueRequests.CountAsync(r => r.PriorityLevelId == 1);

        // Thống kê hôm nay
        var today = DateTime.UtcNow.Date;
        var todayRequests = await _context.RescueRequests.CountAsync(r => r.CreatedAt >= today);

        var stats = new DashboardStatisticsDto
        {
            TotalRequests = totalRequests,
            PendingRequests = pending,
            InProgressRequests = inProgress,
            CompletedRequests = completed,
            CancelledRequests = cancelled,
            HighPriorityRequests = highPriority,
            MediumPriorityRequests = mediumPriority,
            LowPriorityRequests = lowPriority,
            TodayRequests = todayRequests
        };

        return Ok(new { Success = true, Data = stats });
    }
}