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

    /// <summary>
    /// API cho phép tạo yêu cầu cứu hộ mới. Tự động tính toán điểm khẩn cấp (priorityScore) và check Duplicate.
    /// Áp dụng cho cả công dân có tài khoản lẫn Khách (Guest).
    /// </summary>
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

    /// <summary>
    /// Hiển thị danh sách các yêu cầu cấp cứu do chính công dân (đã đăng nhập) này tạo ra.
    /// </summary>
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

    /// <summary>
    /// Lấy về thông tin tóm tắt của một yêu cầu cứu hộ mới nhất (dành cho popup giao diện).
    /// </summary>
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

    /// <summary>
    /// Quản lý (Admin/Coordinator/Manager) - Lấy toàn bộ danh sách yêu cầu trên hệ thống. 
    /// Có chức năng Full-text filter thông qua searchTerm.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, [FromQuery] int? priorityId = null, [FromQuery] string? searchTerm = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(r => 
                r.RequestId.ToString() == term || 
                r.Title.ToLower().Contains(term) || 
                r.Description.ToLower().Contains(term) ||
                r.Address.ToLower().Contains(term) ||
                r.Phone.Contains(term) ||
                r.ContactPhone.Contains(term) ||
                (r.Citizen != null && r.Citizen.FullName.ToLower().Contains(term)) ||
                (r.ContactName != null && r.ContactName.ToLower().Contains(term))
            );
        }

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

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Lọc cụm từ Xã/Phường từ địa chỉ để sort các yêu cầu đứng gần nhau lại với nhau.
    /// </summary>
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

    /// <summary>
    /// Quản lý - Xem thông tin chi tiết đầy đủ của một yêu cầu cứu hộ bằng ID.
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
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
        }

        await ApplyCanReportSafeAsync(request);
        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Guest - Kiểm tra trạng thái của yêu cầu bằng ID (bôi che các thông tin cá nhân của người khác nếu tình cờ gõ trúng ID lạ).
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
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho" });
        }

        await ApplyCanReportSafeAsync(request);
        return Ok(new { Success = true, Data = request });
    }

    /// <summary>
    /// Guest - Thay đổi thông tin yêu cầu cứu hộ (chỉ được thực hiện khi quy trình cứu hộ chưa lăn bánh).
    /// </summary>
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

    /// <summary>
    /// Trang chủ (Guest/Toàn Dân) - Lấy các con số vĩ mô cho banner hệ thống (Tổng cứu hộ, số lượng người đã an toàn...)
    /// </summary>
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


    /// <summary>
    /// Citizen - Thay đổi thông tin yêu cầu của chính bản thân (đi kèm Validation trạng thái đang treo).
    /// </summary>
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

    /// <summary>
    /// Quản lý - Cập nhật can thiệp thẳng trạng thái vào một đơn xin cứu hộ bất kỳ. Tự động ghi Log qua thẻ Audit.
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "COORDINATOR,RESCUE_TEAM,ADMIN,MANAGER")]
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

    /// <summary>
    /// Coordinator - Duyệt đơn đăng ký cứu hộ, thông qua tính chất hợp lệ của đơn này. Đơn này sẽ chuẩn bị được điều xe tới.
    /// </summary>
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

    /// <summary>
    /// Citizen - Công dân chủ động báo cáo rằng mình "Đã được cứu thành công/An toàn", 
    /// chỉ khi đội quản lý phía sau đã chốt sổ chiến dịch. Đẩy status thành Completed.
    /// </summary>
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

    /// <summary>
    /// Chức năng cho Guest xác nhận đã được cứu an toàn.
    /// Yêu cầu số điện thoại truyền vào phải khớp với số điện thoại trong hệ thống để xác minh vì Guest không có mật khẩu.
    /// </summary>
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

    /// <summary>
    /// Lấy các thông số thống kê tổng quan về yêu cầu cứu hộ để hiển thị cho trang Dashboard của nội bộ Quản lý.
    /// Bao gồm số lượng yêu cầu theo các trạng thái khác nhau.
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

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Áp dụng cờ trạng thái "được phép báo an toàn" (CanReportSafe) cho một danh sách yêu cầu.
    /// </summary>
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

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Áp dụng cờ trạng thái "được phép báo an toàn" (CanReportSafe) cho một yêu cầu duy nhất.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(RescueRequestResponseDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Áp dụng cờ trạng thái "được phép báo an toàn" cho khối dữ liệu Front-end LatestRescueRequest.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(LatestRescueRequestDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Lấy về danh sách các ID của yêu cầu cứu hộ đã có chiến dịch thực tế hoàn tất (Completed).
    /// </summary>
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

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Kiểm tra xem một yêu cầu cứu hộ riêng lẻ đã có chiến dịch hoàn thành tương ứng hay chưa.
    /// </summary>
    private async Task<bool> RequestHasCompletedOperationAsync(int requestId)
    {
        return await _context.RescueOperations
            .AnyAsync(o => o.RequestId == requestId && o.Status == "Completed");
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Chuẩn hóa trạng thái bằng cách viết hoa và thay khoảng trắng thành dấu gạch dưới. 
    /// (Ví dụ: "In Progress" -> "IN_PROGRESS")
    /// </summary>
    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): So sánh sự trùng khớp giữa hai số điện thoại thông qua Regex / Chuẩn hóa độ dài biến tấu "84".
    /// </summary>
    private static bool PhoneMatches(string? left, string? right)
    {
        return NormalizePhone(left) == NormalizePhone(right);
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Chuẩn hóa chuỗi số điện thoại. Xóa các ký tự linh tinh và điều chỉnh "84" mã quốc gia thành "0".
    /// </summary>
    private static string NormalizePhone(string? phone)
    {
        var value = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (value.StartsWith("84") && value.Length > 9)
        {
            value = $"0{value[2..]}";
        }

        return value;
    }
    /// <summary>
    /// Thống kê (View Only cho Admin): Xem dòng thời gian Log các luồng đổi trạng thái của công dân hoặc lính cứu hộ. Giải tỏa tranh chấp đổi status láo.
    /// </summary>
    [HttpGet("status-history")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllRequestStatusHistories()
    {
        var histories = await _context.RescueRequestStatusHistories
            .OrderByDescending(h => h.UpdatedAt)
            .Select(h => new RescueRequestStatusHistoryDto
            {
                StatusId = h.StatusId,
                RequestId = h.RequestId,
                RequestTitle = _context.RescueRequests
                    .Where(r => r.RequestId == h.RequestId)
                    .Select(r => r.Title)
                    .FirstOrDefault(),
                Status = h.Status,
                Notes = h.Notes,
                UpdatedBy = h.UpdatedBy,
                UpdatedByName = h.UpdatedBy == -1
                    ? "GUEST"
                    : _context.Users
                        .Where(u => u.UserId == h.UpdatedBy)
                        .Select(u => u.FullName ?? u.Username)
                        .FirstOrDefault(),
                UpdatedAt = h.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Total = histories.Count,
            Data = histories
        });
    }
}
