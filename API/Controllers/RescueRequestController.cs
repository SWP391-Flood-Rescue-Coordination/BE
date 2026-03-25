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
            Message = "Tao yeu cau cuu ho thanh cong",
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
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho nao." });
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
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
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
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
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
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
        }

        if (request.Status != "Pending" && request.Status != "Verified" && request.Status != "Duplicate")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Khong the chinh sua yeu cau khi dang o trang thai: {request.Status}"
            });
        }

        // Apply fields (dùng giá trị mới nếu có, giữ cũ nếu không)
        request.Title       = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone       = dto.ContactPhone ?? request.Phone;
        request.Address     = dto.Address ?? request.Address;
        request.Latitude    = dto.Latitude ?? request.Latitude;
        request.Longitude   = dto.Longitude ?? request.Longitude;

        bool hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount             = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount           = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount          = dto.ChildrenCount ?? request.ChildrenCount;
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        // =============================================
        // KIỂM TRA DUPLICATE: cùng SĐT + cùng địa chỉ trong vòng 15 phút
        // Loại trừ chính request đang được chỉnh sửa
        // =============================================
        bool isDuplicate = false;
        string? checkPhone = request.Phone ?? request.ContactPhone;

        if (!string.IsNullOrWhiteSpace(checkPhone) && !string.IsNullOrWhiteSpace(request.Address))
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-15);
            var normalizedAddress = request.Address.Trim().ToLower();

            isDuplicate = await _context.RescueRequests
                .AnyAsync(r =>
                    r.RequestId != id &&
                    r.CreatedAt >= windowStart &&
                    r.Status != "Completed" &&
                    r.Status != "Cancelled" &&
                    r.Status != "Duplicate" &&
                    (r.Phone == checkPhone || r.ContactPhone == checkPhone) &&
                    r.Address != null &&
                    r.Address.Trim().ToLower() == normalizedAddress);
        }

        request.Status = isDuplicate ? "Duplicate" : (request.Status == "Duplicate" ? "Pending" : request.Status);

        // =============================================
        // TÁI TÍNH PRIORITY dựa trên count + keyword description
        // =============================================
        var elderly = request.ElderlyCount ?? 0;
        var children = request.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        var desc = (request.Description ?? string.Empty).ToLower();
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

        request.PriorityLevelId = priorityScore >= 8 ? 1 : priorityScore >= 4 ? 2 : 3;

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (isDuplicate)
        {
            return Ok(new
            {
                Success   = true,
                Duplicate = true,
                Message   = "Cap nhat thanh cong nhung yeu cau co the trung voi yeu cau khac tai cung dia chi (trong vong 15 phut). Yeu cau duoc danh dau la Duplicate.",
            });
        }

        return Ok(new { Success = true, Message = "Cap nhat yeu cau thanh cong" });
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
                Message = "Khong tim thay yeu cau cuu ho hoac ban khong co quyen chinh sua yeu cau nay."
            });
        }

        if (request.Status != "Pending" && request.Status != "Verified" && request.Status != "Duplicate")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Khong the chinh sua yeu cau khi dang o trang thai: {request.Status}"
            });
        }

        // Apply fields (dùng giá trị mới nếu có, giữ cũ nếu không)
        request.Title       = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone       = dto.ContactPhone ?? request.Phone;
        request.ContactPhone = dto.ContactPhone ?? request.ContactPhone;
        request.Address     = dto.Address ?? request.Address;
        request.Latitude    = dto.Latitude ?? request.Latitude;
        request.Longitude   = dto.Longitude ?? request.Longitude;

        bool hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount             = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount           = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount          = dto.ChildrenCount ?? request.ChildrenCount;
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        // =============================================
        // KIỂM TRA DUPLICATE: cùng SĐT + cùng địa chỉ trong vòng 15 phút
        // Loại trừ chính request đang được chỉnh sửa
        // =============================================
        bool isDuplicate = false;
        string? checkPhone = request.ContactPhone ?? request.Phone;

        if (!string.IsNullOrWhiteSpace(checkPhone) && !string.IsNullOrWhiteSpace(request.Address))
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-15);
            var normalizedAddress = request.Address.Trim().ToLower();

            isDuplicate = await _context.RescueRequests
                .AnyAsync(r =>
                    r.RequestId != id &&
                    r.CreatedAt >= windowStart &&
                    r.Status != "Completed" &&
                    r.Status != "Cancelled" &&
                    r.Status != "Duplicate" &&
                    (r.Phone == checkPhone || r.ContactPhone == checkPhone) &&
                    r.Address != null &&
                    r.Address.Trim().ToLower() == normalizedAddress);
        }

        request.Status = isDuplicate ? "Duplicate" : (request.Status == "Duplicate" ? "Pending" : request.Status);

        // =============================================
        // TÁI TÍNH PRIORITY dựa trên count + keyword description
        // =============================================
        var elderly = request.ElderlyCount ?? 0;
        var children = request.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        var desc = (request.Description ?? string.Empty).ToLower();
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

        request.PriorityLevelId = priorityScore >= 8 ? 1 : priorityScore >= 4 ? 2 : 3;

        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        if (isDuplicate)
        {
            return Ok(new
            {
                Success   = true,
                Duplicate = true,
                Message   = "Cap nhat thanh cong nhung yeu cau co the trung voi yeu cau khac tai cung dia chi (trong vong 15 phut). Yeu cau duoc danh dau la Duplicate.",
            });
        }

        return Ok(new { Success = true, Message = "Cap nhat yeu cau thanh cong" });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
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
            Notes = "Trang thai cap nhat boi he thong quan ly",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cap nhat trang thai thanh cong" });
    }

    [HttpPut("{id}/verify")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> VerifyRequest(int id)
    {
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
        }

        if (request.Status != "Pending")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Yeu cau cuu ho phai o trang thai Pending (hien tai: {request.Status})"
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
            Notes = $"Coordinator xac minh yeu cau (uu tien hien tai: {request.PriorityLevelId})",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Xac minh yeu cau thanh cong" });
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
                Message = "Khong tim thay yeu cau cuu ho hoac ban khong co quyen bao an toan cho yeu cau nay."
            });
        }

        if (request.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chi co the bao an toan khi yeu cau dang o trang thai 'Assigned'. Trang thai hien tai: '{request.Status}'."
            });
        }

        var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
        if (!canReportSafe)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Doi cuu ho chua xac nhan hoan tat, ban chua the bao an toan."
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
            Notes = "Cong dan da bao an toan sau khi doi cuu ho xac nhan hoan tat.",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Bao an toan thanh cong. Yeu cau da duoc chuyen sang Completed."
        });
    }

    [HttpPut("guest/{id}/confirm-rescued")]
    [AllowAnonymous]
    public async Task<IActionResult> GuestConfirmRescued(int id, [FromBody] GuestConfirmRescuedDto dto)
    {
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Khong tim thay yeu cau cuu ho."
            });
        }

        if (!PhoneMatches(request.Phone, dto.Phone) && !PhoneMatches(request.ContactPhone, dto.Phone))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "So dien thoai khong khop voi yeu cau da dang ky."
            });
        }

        if (request.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chi co the bao an toan khi yeu cau dang o trang thai 'Assigned'. Trang thai hien tai: '{request.Status}'."
            });
        }

        var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
        if (!canReportSafe)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Doi cuu ho chua xac nhan hoan tat, ban chua the bao an toan."
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
            Notes = "Khach vang lai da bao an toan sau khi doi cuu ho xac nhan hoan tat.",
            UpdatedBy = -1, // -1 = Guest không có tài khoản
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Bao an toan thanh cong. Yeu cau da duoc chuyen sang Completed."
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
