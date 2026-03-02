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
    /// Tạo yêu cầu cứu hộ mới (Hỗ trợ cả Guest và Citizen)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        // Check for authenticated user
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        

        var request = new RescueRequest
        {
            CitizenId = userId,
            ContactName = userId == null ? dto.ContactName : null,
            ContactPhone = dto.ContactPhone,
            Title = dto.Title,
            Phone = dto.ContactPhone,
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address,
            NumberOfAffectedPeople = dto.NumberOfPeople, // Using NumberOfPeople from DTO
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            AccessCode = userId == null ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() : null
        };

        _context.RescueRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Tạo yêu cầu cứu hộ thành công", RequestId = request.RequestId, AccessCode = request.AccessCode });
    }

    /// <summary>
    /// Lấy danh sách yêu cầu cứu hộ của citizen đang đăng nhập
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        
        var userId = int.Parse(userIdString);
        
        var requests = await _context.RescueRequests
            .Where(r => r.CitizenId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                CitizenName = r.Citizen != null ? r.Citizen.FullName : "",
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : "",
                Title = r.Title,
                Phone = r.Phone,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                NumberOfPeople = r.NumberOfAffectedPeople,
                NumberOfAffectedPeople = r.NumberOfAffectedPeople,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Lấy yêu cầu cứu hộ mới nhất của citizen đang đăng nhập
    /// </summary>
    [HttpGet("my-latest-request")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyLatestRequest()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var query = _context.RescueRequests.AsQueryable();

        if (!string.IsNullOrEmpty(userIdString))
        {
            // Nếu đã đăng nhập, lấy theo UserId
            int userId = int.Parse(userIdString);
            query = query.Where(r => r.CitizenId == userId);
        }
        else
        {
            // Nếu là khách vãng lai, lấy yêu cầu mới nhất chưa có CitizenId gắn vào
            query = query.Where(r => r.CitizenId == null);
        }

        var latestRequest = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LatestRescueRequestDto
            {
                Title = r.Title,
                Description = r.Description,
                Address = r.Address,
                Status = r.Status ?? "Pending",
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (latestRequest == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ nào" });
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
                CitizenName = r.Citizen != null ? (r.Citizen.FullName ?? "") : (r.ContactName ?? ""),
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title = r.Title,
                Phone = r.Phone,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                NumberOfPeople = r.NumberOfAffectedPeople,
                NumberOfAffectedPeople = r.NumberOfAffectedPeople,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
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
                CitizenName = r.Citizen != null ? r.Citizen.FullName : r.ContactName,
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title = r.Title,
                Phone = r.Phone,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                NumberOfPeople = r.NumberOfAffectedPeople,
                NumberOfAffectedPeople = r.NumberOfAffectedPeople,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Khách vãng lai xem trạng thái yêu cầu qua Access Code
    /// </summary>
    [HttpGet("guest/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRequestByAccessCode([FromQuery] int requestId, [FromQuery] string accessCode)
    {
        var request = await _context.RescueRequests
            .Where(r => r.RequestId == requestId && r.AccessCode == accessCode)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status ?? "Pending",
                Address = r.Address,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                NumberOfPeople = r.NumberOfAffectedPeople
            })
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu hoặc mã truy cập không đúng" });
        }

        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Khách vãng lai chỉnh sửa yêu cầu qua Access Code
    /// </summary>
    [HttpPut("guest/update/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRequestByGuest(int id, [FromQuery] string accessCode, [FromBody] UpdateRescueRequestDto dto)
    {
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.AccessCode == accessCode);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu hoặc mã truy cập không đúng" });
        }

        // Chỉ cho phép chỉnh sửa khi đang ở trạng thái Pending hoặc Verified (trước khi thực hiện cứu hộ)
        // Tuy nhiên người dùng yêu cầu "chỉnh sửa lại cái request đó và request đó sẽ được lưu vào trong database, nếu chỉnh sửa thì cái request đó sẽ lưu thay đó request chưa chỉnh trước đó"
        // Nên nếu request đã 'Completed' hay 'Cancelled' thì có lẽ không nên cho sửa.
        if (request.Status != "Pending" && request.Status != "Verified")
        {
            return BadRequest(new { Success = false, Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}" });
        }

        request.Title = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone = dto.ContactPhone ?? request.Phone;
        request.Address = dto.Address ?? request.Address;
        request.Latitude = dto.Latitude ?? request.Latitude;
        request.Longitude = dto.Longitude ?? request.Longitude;
        request.NumberOfAffectedPeople = dto.NumberOfPeople ?? request.NumberOfAffectedPeople;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    /// <summary>
    /// Coordinator/Admin - Cập nhật trạng thái yêu cầu
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

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.Status = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;
        
        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.Status,
            Notes = $"Trạng thái cập nhật bởi hệ thống quản lý",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// Coordinator - Thiết lập priority level và xác minh yêu cầu
    /// </summary>
    [HttpPut("{id}/set-priority-and-verify")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> SetPriorityAndVerify(int id, [FromBody] SetPriorityAndVerifyDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        if (request.Status != "Pending")
        {
            return BadRequest(new { Success = false, Message = $"Yêu cầu cứu hộ phải ở trạng thái Pending (hiện tại: {request.Status})" });
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.PriorityLevelId = dto.PriorityLevelId;
        request.Status = "Verified";
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Verified",
            Notes = $"Coordinator thiết lập mức độ ưu tiên {dto.PriorityLevelId} và xác minh yêu cầu",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Thiết lập mức độ ưu tiên và xác minh yêu cầu thành công" });
    }

    /// <summary>
    /// Manager/Admin/Coordinator - Cập nhật mức độ ưu tiên của yêu cầu
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

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.PriorityLevelId = dto.PriorityLevelId;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;
        
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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return Unauthorized();
        var userId = int.Parse(userIdString);
        
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == userId);
        
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        request.Status = "Completed";
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;
        
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
        
        var pending = await _context.RescueRequests.CountAsync(r => r.Status == "Pending");
        var verified = await _context.RescueRequests.CountAsync(r => r.Status == "Verified");
        var inProgress = await _context.RescueRequests.CountAsync(r => r.Status == "In Progress");
        var completed = await _context.RescueRequests.CountAsync(r => r.Status == "Completed");
        var cancelled = await _context.RescueRequests.CountAsync(r => r.Status == "Cancelled");
        var duplicate = await _context.RescueRequests.CountAsync(r => r.Status == "Duplicate");

        var today = DateTime.UtcNow.Date;
        var todayRequests = await _context.RescueRequests.CountAsync(r => r.CreatedAt >= today);

        var stats = new DashboardStatisticsDto
        {
            TotalRequests = totalRequests,
            PendingRequests = pending,
            VerifiedRequests = verified,
            InProgressRequests = inProgress,
            CompletedRequests = completed,
            CancelledRequests = cancelled,
            DuplicateRequests = duplicate,
            TodayRequests = todayRequests
        };

        return Ok(new { Success = true, Data = stats });
    }
}