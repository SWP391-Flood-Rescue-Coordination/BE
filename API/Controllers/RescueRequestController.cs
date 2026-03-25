using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        // =============================================
        // KIỂM TRA DUPLICATE: cùng SĐT + cùng địa chỉ trong vòng 15 phút
        // Áp dụng cho cả Guest và Citizen đã đăng nhập
        // =============================================
        bool isDuplicate = false;
        string? checkPhone = dto.ContactPhone;

        if (!string.IsNullOrWhiteSpace(checkPhone) && !string.IsNullOrWhiteSpace(dto.Address))
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-15);
            var normalizedAddress = dto.Address.Trim().ToLower();

            isDuplicate = await _context.RescueRequests
                .AnyAsync(r =>
                    r.CreatedAt >= windowStart &&
                    r.Status != "Completed" &&
                    r.Status != "Cancelled" &&
                    r.Status != "Duplicate" &&
                    (r.Phone == checkPhone || r.ContactPhone == checkPhone) &&
                    r.Address != null &&
                    r.Address.Trim().ToLower() == normalizedAddress);
        }

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
            Status                = isDuplicate ? "Duplicate" : "Pending",
            CreatedAt             = DateTime.UtcNow
        };

        var elderly = dto.ElderlyCount ?? 0;
        var children = dto.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        // Cộng điểm thêm dựa trên từ khóa trong Description
        var desc = (dto.Description ?? string.Empty).ToLower();
        var keywordScores = new (string Keyword, double Score)[]
        {
            ("hết nhu yếu phẩm", 1.0),
            ("sập nhà",          3.0),
            ("cần điều trị y tế", 3.5),
            ("ngập dưới 1m",     1.5),
            ("ngập trên 1m",     2.5),
        };
        foreach (var (keyword, score) in keywordScores)
        {
            if (desc.Contains(keyword))
                priorityScore += score;
        }

        if (priorityScore >= 8)
        {
            request.PriorityLevelId = 1; // High
        }
        else if (priorityScore >= 4)
        {
            request.PriorityLevelId = 2; // Medium
        }
        else
        {
            request.PriorityLevelId = 3; // Low
        }

        _context.RescueRequests.Add(request);
        await _context.SaveChangesAsync();

        if (isDuplicate)
        {
            return Ok(new
            {
                Success   = true,
                Duplicate = true,
                Message   = "Yêu cầu cứu hộ đã được ghi nhận nhưng có thể trùng với yêu cầu trước đó tại cùng địa chỉ (trong vòng 15 phút). Yêu cầu được đánh dấu là Duplicate.",
                RequestId = request.RequestId
            });
        }

        return Ok(new
        {
            Success = true,
            Message = "Tạo yêu cầu cứu hộ thành công",
            RequestId = request.RequestId
        });
    }

    [HttpGet("my-requests")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

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

        await ApplyCanReportSafeAsync(requests);
        return Ok(new { Success = true, Data = requests });
    }

    [HttpGet("my-latest-request")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyLatestRequest()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = _context.RescueRequests.AsQueryable();

        if (!string.IsNullOrEmpty(userIdString))
        {
            var userId = int.Parse(userIdString);
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
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ nào." });
        }

        await ApplyCanReportSafeAsync(latestRequest);
        return Ok(new { Success = true, Data = latestRequest });
    }

    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
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

        requests = requests
            .OrderByDescending(r => r.Status == "Pending" || r.Status == "Verified")
            .ThenBy(r => GetWardFromAddress(r.Address))
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

        return Ok(new { Success = true, Data = requests });
    }

    private string GetWardFromAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        var parts = address.Split(',');
        if (parts.Length >= 2)
        {
            foreach (var part in parts)
            {
                var p = part.Trim().ToLower();
                if (p.StartsWith("phường") || p.StartsWith("p.") || p.StartsWith("xã") || p.StartsWith("thị trấn"))
                {
                    return p;
                }
            }
            return parts.Length > 1 ? parts[1].Trim().ToLower() : address.ToLower();
        }
        return address.ToLower();
    }

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
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        await ApplyCanReportSafeAsync(request);
        return Ok(new { Success = true, Data = request });
    }

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
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        await ApplyCanReportSafeAsync(request);
        return Ok(new { Success = true, Data = request });
    }

    [HttpPut("guest/update/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRequestByGuest(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        if (request.Status != "Pending" && request.Status != "Verified")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });
        }

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
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    [HttpPut("{id}/update")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> UpdateRequestByCitizen(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var userId = int.Parse(userIdString);

        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == userId);

        if (request == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ hoặc bạn không có quyền chỉnh sửa yêu cầu này."
            });
        }

        if (request.Status != "Pending" && request.Status != "Verified")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });
        }

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
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }
    [HttpGet("citizen-dashboard-statistics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCitizenDashboardStatistics()
    {
        var supportedStatuses = new[] { "Confirmed", "Completed", "CitizenConfirmed" };
        var safeStatuses = new[] { "Completed", "CitizenConfirmed" };

        var receivedRequests = await _context.RescueRequests.CountAsync();
        var supportedRequests = await _context.RescueRequests.CountAsync(r => supportedStatuses.Contains(r.Status));
        var safeReports = await _context.RescueRequests.CountAsync(r => safeStatuses.Contains(r.Status));
        var rescuedPeople =
            await _context.RescueRequests
                .Where(r => supportedStatuses.Contains(r.Status))
                .Select(r => (int?)r.NumberOfAffectedPeople)
                .SumAsync() ?? 0;

        return Ok(new
        {
            Success = true,
            Data = new CitizenDashboardStatisticsDto
            {
                ReceivedRequests = receivedRequests,
                RescuedPeople = rescuedPeople,
                SupportedRequests = supportedRequests,
                SafeReports = safeReports
            }
        });
    }


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
        var userId = userIdString != null ? int.Parse(userIdString) : -1;

        request.Status = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.Status,
            Notes = "Trạng thái được cập nhật bởi hệ thống quản lý",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    [HttpPut("{id}/verify")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> VerifyRequest(int id)
    {
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        if (request.Status != "Pending")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Yêu cầu cứu hộ phải ở trạng thái Pending (hiện tại: {request.Status})"
            });
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdString != null ? int.Parse(userIdString) : -1;

        request.Status = "Verified";
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Verified",
            Notes = $"Điều phối viên xác minh yêu cầu (ưu tiên hiện tại: {request.PriorityLevelId})",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Xác minh yêu cầu thành công" });
    }

    [HttpPut("{id}/confirm-rescued")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> ConfirmRescued(int id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var userId = int.Parse(userIdString);

        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.CitizenId == userId);

        if (request == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ hoặc bạn không có quyền báo an toàn cho yêu cầu này."
            });
        }

        if (request.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể báo an toàn khi yêu cầu đang ở trạng thái 'Assigned'. Trạng thái hiện tại: '{request.Status}'."
            });
        }

        var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
        if (!canReportSafe)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Đội cứu hộ chưa xác nhận hoàn tất, bạn chưa thể báo an toàn."
            });
        }

        var now = DateTime.UtcNow;
        request.Status = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Completed",
            Notes = "Công dân đã báo an toàn sau khi đội cứu hộ xác nhận hoàn tất.",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Báo an toàn thành công. Yêu cầu đã được chuyển sang Completed."
        });
    }

    [HttpPut("guest/{id}/confirm-rescued")]
    [AllowAnonymous]
    public async Task<IActionResult> GuestConfirmRescued(int id, [FromBody] GuestConfirmRescuedDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Phone))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc để báo an toàn."
            });
        }

        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Không tìm thấy yêu cầu cứu hộ."
            });
        }

        if (!PhoneMatches(request.Phone, dto.Phone) && !PhoneMatches(request.ContactPhone, dto.Phone))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Số điện thoại không khớp với yêu cầu đã đăng ký."
            });
        }

        if (request.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể báo an toàn khi yêu cầu đang ở trạng thái 'Assigned'. Trạng thái hiện tại: '{request.Status}'."
            });
        }

        var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
        if (!canReportSafe)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Đội cứu hộ chưa xác nhận hoàn tất, bạn chưa thể báo an toàn."
            });
        }

        var now = DateTime.UtcNow;
        request.Status = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = -1; // -1 = Guest không có tài khoản

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Completed",
            Notes = "Khách vãng lai đã báo an toàn sau khi đội cứu hộ xác nhận hoàn tất.",
            UpdatedBy = -1, // -1 = Guest không có tài khoản
            UpdatedAt = now
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUpdatedByForeignKeyError(ex))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Success = false,
                Message = "updated_by đang được gán = -1 cho Guest/System nhưng database hiện tại vẫn còn FK tới users. Hãy chạy script drop_updated_by_fk.sql trên đúng database đang kết nối."
            });
        }

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Báo an toàn thành công. Yêu cầu đã được chuyển sang Completed."
        });
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalRequests = await _context.RescueRequests.CountAsync();
        var pending = await _context.RescueRequests.CountAsync(r => r.Status == "Pending");
        var verified = await _context.RescueRequests.CountAsync(r => r.Status == "Verified");
        var inProgress = await _context.RescueRequests.CountAsync(r => r.Status == "In Progress");
        var completed = await _context.RescueRequests.CountAsync(r => r.Status == "Completed");
        var citizenConfirmed = await _context.RescueRequests.CountAsync(r => r.Status == "CitizenConfirmed");
        var cancelled = await _context.RescueRequests.CountAsync(r => r.Status == "Cancelled");
        var duplicate = await _context.RescueRequests.CountAsync(r => r.Status == "Duplicate");
        var today = DateTime.UtcNow.Date;
        var todayRequests = await _context.RescueRequests.CountAsync(r => r.CreatedAt >= today);

        return Ok(new
        {
            Success = true,
            Data = new DashboardStatisticsDto
            {
                TotalRequests = totalRequests,
                PendingRequests = pending,
                VerifiedRequests = verified,
                InProgressRequests = inProgress,
                CompletedRequests = completed,
                CitizenConfirmedRequests = citizenConfirmed,
                CancelledRequests = cancelled,
                DuplicateRequests = duplicate,
                TodayRequests = todayRequests
            }
        });
    }

    private async Task ApplyCanReportSafeAsync(List<RescueRequestResponseDto> requests)
    {
        if (requests.Count == 0)
        {
            return;
        }

        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(requests.Select(r => r.RequestId));
        foreach (var request in requests)
        {
            request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
                && completedRequestIds.Contains(request.RequestId);
        }
    }

    private async Task ApplyCanReportSafeAsync(RescueRequestResponseDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    private async Task ApplyCanReportSafeAsync(LatestRescueRequestDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    private async Task<HashSet<int>> GetCompletedOperationRequestIdsAsync(IEnumerable<int> requestIds)
    {
        var ids = requestIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var completedIds = await _context.RescueOperations
            .Where(o => ids.Contains(o.RequestId) && o.Status == "Completed")
            .Select(o => o.RequestId)
            .Distinct()
            .ToListAsync();

        return completedIds.ToHashSet();
    }

    private async Task<bool> RequestHasCompletedOperationAsync(int requestId)
    {
        return await _context.RescueOperations
            .AnyAsync(o => o.RequestId == requestId && o.Status == "Completed");
    }

    private static bool IsUpdatedByForeignKeyError(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("FK_rrsh_updated_by", StringComparison.OrdinalIgnoreCase)
            || message.Contains("FK_rescue_requests_updated_by", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }

    private static bool PhoneMatches(string? left, string? right)
    {
        return NormalizePhone(left) == NormalizePhone(right);
    }

    private static string NormalizePhone(string? phone)
    {
        var value = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (value.StartsWith("84") && value.Length > 9)
        {
            value = $"0{value[2..]}";
        }

        return value;
    }
}
