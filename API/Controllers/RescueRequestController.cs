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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        var request = new RescueRequest
        {
            CitizenId             = userId,
            ContactName           = userId == null ? dto.ContactName : null,
            ContactPhone          = dto.ContactPhone,
            Title                 = dto.Title,
            Phone                 = dto.ContactPhone,
            Description           = dto.Description,
            Latitude              = dto.Latitude,
            Longitude             = dto.Longitude,
            Address               = dto.Address,
            AdultCount            = dto.AdultCount,
            ElderlyCount          = dto.ElderlyCount,
            ChildrenCount         = dto.ChildrenCount,
            Status                = "Pending",
            CreatedAt             = DateTime.UtcNow
        };

        // Auto-calculate Priority Level: E = ElderlyCount, C = ChildrenCount
        // V = 1.5 * E + 1.8 * C
        int elderly = dto.ElderlyCount ?? 0;
        int children = dto.ChildrenCount ?? 0;
        double v = 1.5 * elderly + 1.8 * children;

        if (v >= 6)
        {
            request.PriorityLevelId = 1; // High
        }
        else if (v >= 3)
        {
            request.PriorityLevelId = 2; // Medium
        }
        else
        {
            request.PriorityLevelId = 3; // Low
        }

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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = int.Parse(userIdString);

        var requests = await _context.RescueRequests
            .Where(r => r.CitizenId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId              = r.RequestId,
                CitizenId              = r.CitizenId,
                CitizenName            = r.Citizen != null ? r.Citizen.FullName : "",
                CitizenPhone           = r.Citizen != null ? r.Citizen.Phone : "",
                Title                  = r.Title,
                Description            = r.Description,
                Latitude               = r.Latitude,
                Longitude              = r.Longitude,
                Address                = r.Address,
                PriorityLevelId        = r.PriorityLevelId,
                Status                 = r.Status ?? "Pending",
                AdultCount             = r.AdultCount,
                ElderlyCount           = r.ElderlyCount,
                ChildrenCount          = r.ChildrenCount,
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Lấy yêu cầu cứu hộ mới nhất.
    /// - Citizen đã đăng nhập: lấy theo UserId từ JWT.
    /// - Guest: lấy yêu cầu mới nhất chưa có CitizenId.
    /// </summary>
    [HttpGet("my-latest-request")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyLatestRequest()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var query = _context.RescueRequests.AsQueryable();

        if (!string.IsNullOrEmpty(userIdString))
        {
            int userId = int.Parse(userIdString);
            query = query.Where(r => r.CitizenId == userId);
        }
        else
        {
            query = query.Where(r => r.CitizenId == null);
        }

        var latestRequest = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LatestRescueRequestDto
            {
                RequestId              = r.RequestId,
                Title                  = r.Title,
                Description            = r.Description,
                Address                = r.Address,
                Status                 = r.Status ?? "Pending",
                AdultCount             = r.AdultCount,
                ElderlyCount           = r.ElderlyCount,
                ChildrenCount          = r.ChildrenCount,
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (latestRequest == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ nào." });

        return Ok(new { Success = true, Data = latestRequest });
    }

    /// <summary>
    /// Coordinator/Admin/Manager - Lấy tất cả yêu cầu cứu hộ
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, [FromQuery] int? priorityId = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (priorityId.HasValue)
            query = query.Where(r => r.PriorityLevelId == priorityId.Value);


        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId              = r.RequestId,
                CitizenId              = r.CitizenId,
                CitizenName            = r.Citizen != null ? (r.Citizen.FullName ?? "") : (r.ContactName ?? ""),
                CitizenPhone           = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title                  = r.Title,
                Description            = r.Description,
                Latitude               = r.Latitude,
                Longitude              = r.Longitude,
                Address                = r.Address,
                PriorityLevelId        = r.PriorityLevelId,
                Status                 = r.Status ?? "Pending",
                AdultCount             = r.AdultCount,
                ElderlyCount           = r.ElderlyCount,
                ChildrenCount          = r.ChildrenCount,
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
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
                RequestId              = r.RequestId,
                CitizenId              = r.CitizenId,
                CitizenName            = r.Citizen != null ? r.Citizen.FullName : r.ContactName,
                CitizenPhone           = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title                  = r.Title,
                Description            = r.Description,
                Latitude               = r.Latitude,
                Longitude              = r.Longitude,
                Address                = r.Address,
                PriorityLevelId        = r.PriorityLevelId,
                Status                 = r.Status ?? "Pending",
                AdultCount             = r.AdultCount,
                ElderlyCount           = r.ElderlyCount,
                ChildrenCount          = r.ChildrenCount,
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Khách vãng lai xem trạng thái yêu cầu qua ID
    /// </summary>
    [HttpGet("guest/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRequestByIdForGuest([FromQuery] int requestId)
    {
        var request = await _context.RescueRequests
            .Where(r => r.RequestId == requestId)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId              = r.RequestId,
                Title                  = r.Title,
                Description            = r.Description,
                Status                 = r.Status ?? "Pending",
                AdultCount             = r.AdultCount,
                ElderlyCount           = r.ElderlyCount,
                ChildrenCount          = r.ChildrenCount,
                Address                = r.Address,
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Khách vãng lai chỉnh sửa yêu cầu qua ID
    /// </summary>
    [HttpPut("guest/update/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRequestByGuest(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        if (request.Status != "Pending" && request.Status != "Verified")
            return BadRequest(new { Success = false, Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}" });

        request.Title                  = dto.Title ?? request.Title;
        request.Description            = dto.Description ?? request.Description;
        request.Phone                  = dto.ContactPhone ?? request.Phone;
        request.Address                = dto.Address ?? request.Address;
        request.Latitude               = dto.Latitude ?? request.Latitude;
        request.Longitude              = dto.Longitude ?? request.Longitude;
        
        bool hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount = dto.ChildrenCount ?? request.ChildrenCount;
        }

        request.UpdatedAt              = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    /// <summary>
    /// CITIZEN (đã đăng nhập) - Chỉnh sửa yêu cầu cứu hộ của chính mình.
    /// Chỉ cho phép khi status = "Pending" hoặc "Verified".
    /// </summary>
    [HttpPut("{id}/update")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> UpdateRequestByCitizen(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = int.Parse(userIdString);

        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == userId);

        if (request == null)
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ hoặc bạn không có quyền chỉnh sửa yêu cầu này."
            });

        if (request.Status != "Pending" && request.Status != "Verified")
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });

        request.Title                  = dto.Title ?? request.Title;
        request.Description            = dto.Description ?? request.Description;
        request.Phone                  = dto.ContactPhone ?? request.Phone;
        request.ContactPhone           = dto.ContactPhone ?? request.ContactPhone;
        request.Address                = dto.Address ?? request.Address;
        request.Latitude               = dto.Latitude ?? request.Latitude;
        request.Longitude              = dto.Longitude ?? request.Longitude;
        
        bool hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount = dto.ChildrenCount ?? request.ChildrenCount;
        }

        request.UpdatedAt              = DateTime.UtcNow;
        request.UpdatedBy              = userId;

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
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.Status    = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status    = dto.Status,
            Notes     = "Trạng thái cập nhật bởi hệ thống quản lý",
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
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        if (request.Status != "Pending")
            return BadRequest(new { Success = false, Message = $"Yêu cầu cứu hộ phải ở trạng thái Pending (hiện tại: {request.Status})" });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.PriorityLevelId = dto.PriorityLevelId;
        request.Status          = "Verified";
        request.UpdatedAt       = DateTime.UtcNow;
        request.UpdatedBy       = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status    = "Verified",
            Notes     = $"Coordinator thiết lập mức độ ưu tiên {dto.PriorityLevelId} và xác minh yêu cầu",
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
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = userIdString != null ? int.Parse(userIdString) : 0;

        request.PriorityLevelId = dto.PriorityLevelId;
        request.UpdatedAt       = DateTime.UtcNow;
        request.UpdatedBy       = userId;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật mức độ ưu tiên thành công" });
    }

    /// <summary>
    /// CITIZEN (đã đăng nhập) - Xác nhận đã được cứu hộ.
    /// Chỉ gọi được khi status = "Confirmed".
    /// Sau khi xác nhận, status request chuyển thành "Completed" và rescue operation cùng RequestId cũng chuyển thành "Completed".
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
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ hoặc bạn không có quyền xác nhận yêu cầu này."
            });

        if (request.Status != "Confirmed")
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể xác nhận khi yêu cầu đang ở trạng thái 'Confirmed'. Trạng thái hiện tại: '{request.Status}'."
            });

        var now = DateTime.UtcNow;

        // Cập nhật trạng thái request
        request.Status    = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status    = "Completed",
            Notes     = "Công dân xác nhận đã được cứu hộ thành công.",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        // Cập nhật trạng thái các rescue operation cùng RequestId
        var operations = await _context.RescueOperations
            .Where(o => o.RequestId == id)
            .ToListAsync();

        foreach (var operation in operations)
        {
            operation.Status      = "Completed";
            operation.CompletedAt = now;
        }

        // Trả vehicle về AVAILABLE khi user xác nhận Completed
        var operationIds = operations.Select(o => o.OperationId).ToList();
        var vehicleIds = await _context.RescueOperationVehicles
            .Where(ov => operationIds.Contains(ov.OperationId))
            .Select(ov => ov.VehicleId)
            .Distinct()
            .ToListAsync();

        if (vehicleIds.Any())
        {
            var vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync();
            foreach (var v in vehicles)
            {
                v.Status    = "AVAILABLE";
                v.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success            = true,
            Message            = "Cảm ơn bạn đã xác nhận! Yêu cầu cứu hộ đã được đóng lại.",
            RequestId          = request.RequestId,
            Status             = request.Status,
            OperationsUpdated  = operations.Count,
            CompletedAt        = now
        });
    }

    /// <summary>
    /// GUEST (không cần đăng nhập) - Xác nhận đã được cứu hộ bằng RequestId + Phone.
    /// Phone phải khớp với ContactPhone hoặc Phone đã lưu khi tạo yêu cầu.
    /// Chỉ gọi được khi status = "Confirmed".
    /// Sau khi xác nhận, status request chuyển thành "Completed" và rescue operation cùng RequestId cũng chuyển thành "Completed".
    /// </summary>
    [HttpPut("guest/{id}/confirm-rescued")]
    [AllowAnonymous]
    public async Task<IActionResult> GuestConfirmRescued(int id, [FromBody] GuestConfirmRescuedDto dto)
    {
        var inputPhone = dto.Phone?.Trim();
        if (string.IsNullOrEmpty(inputPhone))
            return BadRequest(new
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc để xác nhận."
            });

        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == null);

        if (request == null)
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ. Yêu cầu không tồn tại hoặc thuộc về tài khoản đã đăng nhập."
            });

        var savedPhone = (request.ContactPhone ?? request.Phone ?? "").Trim();
        if (!string.Equals(inputPhone, savedPhone, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new
            {
                Success = false,
                Message = "Số điện thoại không khớp với yêu cầu cứu hộ này."
            });

        if (request.Status != "Confirmed")
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể xác nhận khi yêu cầu đang ở trạng thái 'Confirmed'. Trạng thái hiện tại: '{request.Status}'."
            });

        var now = DateTime.UtcNow;

        // Cập nhật trạng thái request
        request.Status    = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = null; // Guest không có userId

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status    = "Completed",
            Notes     = $"Khách vãng lai (SĐT: {inputPhone}) xác nhận đã được cứu hộ thành công.",
            UpdatedBy = request.CitizenId ?? 1, // fallback: dùng citizen nếu có, không thì admin id=1
            UpdatedAt = now
        });

        // Cập nhật trạng thái các rescue operation cùng RequestId
        var operations = await _context.RescueOperations
            .Where(o => o.RequestId == id)
            .ToListAsync();

        foreach (var operation in operations)
        {
            operation.Status      = "Completed";
            operation.CompletedAt = now;
        }

        // Trả vehicle về AVAILABLE khi guest xác nhận Completed
        var operationIds = operations.Select(o => o.OperationId).ToList();
        var vehicleIds = await _context.RescueOperationVehicles
            .Where(ov => operationIds.Contains(ov.OperationId))
            .Select(ov => ov.VehicleId)
            .Distinct()
            .ToListAsync();

        if (vehicleIds.Any())
        {
            var vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync();
            foreach (var v in vehicles)
            {
                v.Status    = "AVAILABLE";
                v.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success           = true,
            Message           = "Cảm ơn bạn đã xác nhận! Yêu cầu cứu hộ đã được đóng lại.",
            RequestId         = request.RequestId,
            Status            = request.Status,
            OperationsUpdated = operations.Count,
            CompletedAt       = now
        });
    }

    /// <summary>
    /// Manager/Admin/Coordinator - Xem thống kê tổng quan (Dashboard)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalRequests    = await _context.RescueRequests.CountAsync();
        var pending          = await _context.RescueRequests.CountAsync(r => r.Status == "Pending");
        var verified         = await _context.RescueRequests.CountAsync(r => r.Status == "Verified");
        var inProgress       = await _context.RescueRequests.CountAsync(r => r.Status == "In Progress");
        var completed        = await _context.RescueRequests.CountAsync(r => r.Status == "Completed");
        var citizenConfirmed = await _context.RescueRequests.CountAsync(r => r.Status == "CitizenConfirmed");
        var cancelled        = await _context.RescueRequests.CountAsync(r => r.Status == "Cancelled");
        var duplicate        = await _context.RescueRequests.CountAsync(r => r.Status == "Duplicate");
        var today            = DateTime.UtcNow.Date;
        var todayRequests    = await _context.RescueRequests.CountAsync(r => r.CreatedAt >= today);

        return Ok(new
        {
            Success = true,
            Data = new DashboardStatisticsDto
            {
                TotalRequests            = totalRequests,
                PendingRequests          = pending,
                VerifiedRequests         = verified,
                InProgressRequests       = inProgress,
                CompletedRequests        = completed,
                CitizenConfirmedRequests = citizenConfirmed,
                CancelledRequests        = cancelled,
                DuplicateRequests        = duplicate,
                TodayRequests            = todayRequests
            }
        });
    }
}
