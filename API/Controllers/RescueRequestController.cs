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
    /// API Tạo yêu cầu cứu hộ mới.
    /// Cho phép cả người dùng đã đăng nhập và khách (Guest) gửi yêu cầu.
    /// Tự động kiểm tra trùng lặp và tính toán mức độ ưu tiên.
    /// </summary>
    /// <param name="dto">Dữ liệu yêu cầu cứu hộ.</param>
    /// <returns>Thông tin kết quả tạo yêu cầu.</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        // 1. Xác định ID người dùng nếu đã đăng nhập
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        // 2. Kiểm tra yêu cầu trùng lặp (Duplicate Check)
        // Tiêu chí: Cùng số điện thoại + Cùng địa chỉ trong vòng 15 phút gần nhất
        bool isDuplicate = false;
        string? checkPhone = dto.ContactPhone;

        if (!string.IsNullOrWhiteSpace(checkPhone) && !string.IsNullOrWhiteSpace(dto.Address))
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-15);
            var normalizedAddress = dto.Address.Trim().ToLower();

            // Tìm xem có yêu cầu nào tương tự đang chờ xử lý không
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

        // 3. Khởi tạo đối tượng yêu cầu cứu hộ
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
            Status                = isDuplicate ? "Duplicate" : "Pending", // Đánh dấu Duplicate nếu bị trùng
            CreatedAt             = DateTime.UtcNow
        };

        // 4. Tính toán điểm ưu tiên (Priority Score)
        // Công thức: 1.5 * người già + 1.8 * trẻ em
        var elderly = dto.ElderlyCount ?? 0;
        var children = dto.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        // 5. Cộng thêm điểm dựa trên các từ khóa khẩn cấp trong phần mô tả
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

        // 6. Phân cấp mức độ ưu tiên dựa trên tổng điểm
        if (priorityScore >= 8)
        {
            request.PriorityLevelId = 1; // Cao (High)
        }
        else if (priorityScore >= 4)
        {
            request.PriorityLevelId = 2; // Trung bình (Medium)
        }
        else
        {
            request.PriorityLevelId = 3; // Thấp (Low)
        }

        _context.RescueRequests.Add(request);
        await _context.SaveChangesAsync();

        // 7. Trả về phản hồi cho người dùng
        if (isDuplicate)
        {
            return Ok(new
            {
                Success   = true,
                Duplicate = true,
                Message   = "Yêu cầu cứu hộ đã được ghi nhận nhưng bị trùng với yêu cầu trước đó tại cùng địa chỉ (trong vòng 15 phút).",
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

    /// <summary>
    /// API Lấy danh sách các yêu cầu cứu hộ của cá nhân người dùng đang đăng nhập.
    /// Quyền: CITIZEN.
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

        // Lấy danh sách yêu cầu của user, kèm theo thông tin thời gian dự kiến (nếu đã được phân công)
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
                EstimatedTime          = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId)
                    .OrderByDescending(o => o.AssignedAt)
                    .Select(o => o.EstimatedTime)
                    .FirstOrDefault(),
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .ToListAsync();

        // Gắn thêm thông tin liệu người dùng có thể báo cáo "An toàn" ngay lúc này không
        await ApplyCanReportSafeAsync(requests);
        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// API lấy yêu cầu cứu hộ mới nhất của người dùng (bao gồm cả khách truy cập theo phiên).
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
            // Nếu là khách, tìm yêu cầu mới nhất không gắn CitizenId (theo logic lưu trữ)
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
                EstimatedTime          = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId)
                    .OrderByDescending(o => o.AssignedAt)
                    .Select(o => o.EstimatedTime)
                    .FirstOrDefault(),
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

    /// <summary>
    /// API Quản lý: Lấy toàn bộ danh sách yêu cầu cứu hộ trên hệ thống.
    /// Hỗ trợ lọc theo trạng thái, mức độ ưu tiên và tìm kiếm từ khóa.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null, [FromQuery] int? priorityId = null, [FromQuery] string? searchTerm = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        // 1. Phân tích từ khóa tìm kiếm (Search Term)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(r => 
                r.RequestId.ToString() == term || 
                (r.Title != null && r.Title.ToLower().Contains(term)) || 
                (r.Description != null && r.Description.ToLower().Contains(term)) ||
                (r.Address != null && r.Address.ToLower().Contains(term)) ||
                (r.Phone != null && r.Phone.Contains(term)) ||
                (r.ContactPhone != null && r.ContactPhone.Contains(term)) ||
                (r.Citizen != null && r.Citizen.FullName != null && r.Citizen.FullName.ToLower().Contains(term)) ||
                (r.ContactName != null && r.ContactName.ToLower().Contains(term))
            );
        }

        // 2. Lọc theo trạng thái
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        // 3. Lọc theo mức độ ưu tiên
        if (priorityId.HasValue)
        {
            query = query.Where(r => r.PriorityLevelId == priorityId.Value);
        }

        // 4. Map dữ liệu sang DTO
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
                EstimatedTime          = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId)
                    .OrderByDescending(o => o.AssignedAt)
                    .Select(o => o.EstimatedTime)
                    .FirstOrDefault(),
                CreatedAt              = r.CreatedAt,
                UpdatedAt              = r.UpdatedAt
            })
            .ToListAsync();

        // 5. Sắp xếp kết quả trả về: Ưu tiên đơn mới/đang chờ, gom nhóm theo Phường/Xã
        requests = requests
            .OrderByDescending(r => r.Status == "Pending" || r.Status == "Verified")
            .ThenBy(r => GetWardFromAddress(r.Address))
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Hàm hỗ trợ: Trích xuất thông tin Phường/Xã từ địa chỉ thô để phục vụ gom nhóm địa lý.
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
    /// API xem chi tiết một yêu cầu cứu hộ bằng ID.
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
                EstimatedTime          = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId)
                    .OrderByDescending(o => o.AssignedAt)
                    .Select(o => o.EstimatedTime)
                    .FirstOrDefault(),
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

    /// <summary>
    /// API dành cho khách (Guest): Kiểm tra trạng thái cứu hộ dựa trên mã yêu cầu.
    /// Bảo mật: Không trả về các thông tin nhạy cảm của người dùng khác.
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
                EstimatedTime          = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId)
                    .OrderByDescending(o => o.AssignedAt)
                    .Select(o => o.EstimatedTime)
                    .FirstOrDefault(),
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

    /// <summary>
    /// API cho phép khách cập nhật/chỉnh sửa thông tin yêu cầu cứu hộ.
    /// Chỉ được phép sửa khi chưa có đội nào được phân công.
    /// </summary>
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

        // Chặn chỉnh sửa nếu trạng thái không còn là đang chờ
        if (request.Status != "Pending" && request.Status != "Verified" && request.Status != "Duplicate")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });
        }

        // Cập nhật các trường thông tin
        request.Title       = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone       = dto.ContactPhone ?? request.Phone;
        request.Address     = dto.Address ?? request.Address;
        request.Latitude    = dto.Latitude ?? request.Latitude;
        request.Longitude   = dto.Longitude ?? request.Longitude;

        if (dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue)
        {
            request.AdultCount             = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount           = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount          = dto.ChildrenCount ?? request.ChildrenCount;
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        // Kiểm tra lại tính trùng lặp sau khi cập nhật địa chỉ/SĐT
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

        // Tái tính toán điểm ưu tiên
        var elderly = request.ElderlyCount ?? 0;
        var children = request.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        var desc = (request.Description ?? string.Empty).ToLower();
        var keywordScores = new (string Keyword, double Score)[]
        {
            ("hết nhu yếu phẩm", 1.0), ("sập nhà", 3.0), ("cần điều trị y tế", 3.5), ("ngập dưới 1m", 1.5), ("ngập trên 1m", 2.5),
        };
        foreach (var (keyword, score) in keywordScores)
        {
            if (desc.Contains(keyword)) priorityScore += score;
        }

        request.PriorityLevelId = priorityScore >= 8 ? 1 : priorityScore >= 4 ? 2 : 3;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (isDuplicate)
        {
            return Ok(new { Success = true, Duplicate = true, Message = "Cập nhật thành công nhưng yêu cầu bị đánh dấu là trùng lặp (Duplicate)." });
        }

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    /// <summary>
    /// API lấy thông số thống kê tổng quan (Dashboard) cho công dân.
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
    /// API cho phép công dân (Citizen) cập nhật thông tin yêu cầu của chính mình.
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
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu hoặc bạn không có quyền sửa." });
        }

        if (request.Status != "Pending" && request.Status != "Verified" && request.Status != "Duplicate")
        {
            return BadRequest(new { Success = false, Message = $"Không thể sửa yêu cầu ở trạng thái: {request.Status}" });
        }

        // Cập nhật thông tin
        request.Title       = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone       = dto.ContactPhone ?? request.Phone;
        request.ContactPhone = dto.ContactPhone ?? request.ContactPhone;
        request.Address     = dto.Address ?? request.Address;
        request.Latitude    = dto.Latitude ?? request.Latitude;
        request.Longitude   = dto.Longitude ?? request.Longitude;

        if (dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue)
        {
            request.AdultCount             = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount           = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount          = dto.ChildrenCount ?? request.ChildrenCount;
            request.NumberOfAffectedPeople = dto.NumberOfAffectedPeople ?? request.NumberOfAffectedPeople;
        }

        // Kiểm tra trùng lặp
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
                    r.Status != "Completed" && r.Status != "Cancelled" && r.Status != "Duplicate" &&
                    (r.Phone == checkPhone || r.ContactPhone == checkPhone) &&
                    r.Address != null && r.Address.Trim().ToLower() == normalizedAddress);
        }

        request.Status = isDuplicate ? "Duplicate" : (request.Status == "Duplicate" ? "Pending" : request.Status);

        // Tái tính điểm ưu tiên
        var elderly = request.ElderlyCount ?? 0;
        var children = request.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;
        var desc = (request.Description ?? string.Empty).ToLower();
        var keywordScores = new (string Keyword, double Score)[]
        {
            ("hết nhu yếu phẩm", 1.0), ("sập nhà", 3.0), ("cần điều trị y tế", 3.5), ("ngập dưới 1m", 1.5), ("ngập trên 1m", 2.5),
        };
        foreach (var (keyword, score) in keywordScores)
        {
            if (desc.Contains(keyword)) priorityScore += score;
        }
        request.PriorityLevelId = priorityScore >= 8 ? 1 : priorityScore >= 4 ? 2 : 3;

        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        if (isDuplicate)
        {
            return Ok(new { Success = true, Duplicate = true, Message = "Cập nhật thành công nhưng bị trùng lặp." });
        }

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    /// <summary>
    /// API Quản lý: Cập nhật trực tiếp trạng thái của yêu cầu cứu hộ.
    /// Ghi lại lịch sử cập nhật (Audit Log).
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "COORDINATOR,RESCUE_TEAM,ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var request = await _context.RescueRequests.FindAsync(id);
        if (request == null) return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu" });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdString != null ? int.Parse(userIdString) : -1;

        request.Status = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        // Ghi lại lịch sử thay đổi trạng thái
        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.Status,
            Notes = "Trạng thái cập nhật bởi hệ thống quản lý",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// API Điều phối (Coordinator): Xác minh tính chính xác của một yêu cầu cứu hộ (Verify).
    /// Chuyển trạng thái từ Pending sang Verified để chuẩn bị phân công đội cứu hộ.
    /// </summary>
    [HttpPut("{id}/verify")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> VerifyRequest(int id)
    {
        var request = await _context.RescueRequests.FindAsync(id);
        if (request == null) return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu" });

        if (request.Status != "Pending")
        {
            return BadRequest(new { Success = false, Message = $"Yêu cầu phải ở trạng thái Pending (hiện tại: {request.Status})" });
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
            Notes = $"Xác minh yêu cầu (độ ưu tiên: {request.PriorityLevelId})",
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { Success = true, Message = "Xác minh yêu cầu thành công" });
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
    /// API lấy các con số thống kê Dashboard cho bộ phận quản lý.
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> GetStatistics()
    {
        // Thực hiện đếm số lượng yêu cầu cứu hộ theo từng nhóm trạng thái
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
    /// Hàm hỗ trợ: Gắn cờ CanReportSafe cho danh sách DTO dựa trên trạng thái chiến dịch thực tế.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(List<RescueRequestResponseDto> requests)
    {
        if (requests.Count == 0) return;

        // Tìm các yêu cầu đã có Operation status là 'Completed'
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(requests.Select(r => r.RequestId));
        foreach (var request in requests)
        {
            // Được báo an toàn nếu: Yêu cầu đang Assigned + Chiến dịch đã xong
            request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
                && completedRequestIds.Contains(request.RequestId);
        }
    }

    /// <summary>
    /// Hàm hỗ trợ: Kiểm tra quyền báo an toàn cho một yêu cầu duy nhất.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(RescueRequestResponseDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Hàm hỗ trợ: Kiểm tra quyền báo an toàn cho DTO dạng tóm tắt.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(LatestRescueRequestDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Hàm hỗ trợ: Truy vấn danh sách ID các yêu cầu cứu hộ mà đội cứu hộ đã xác nhận hoàn thành công việc.
    /// </summary>
    private async Task<HashSet<int>> GetCompletedOperationRequestIdsAsync(IEnumerable<int> requestIds)
    {
        var ids = requestIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0) return new HashSet<int>();

        var completedIds = await _context.RescueOperations
            .Where(o => ids.Contains(o.RequestId) && o.Status == "Completed")
            .Select(o => o.RequestId)
            .Distinct()
            .ToListAsync();

        return completedIds.ToHashSet();
    }

    /// <summary>
    /// Hàm hỗ trợ: Kiểm tra xem một Request cụ thể đã có Operation hoàn tất hay chưa.
    /// </summary>
    private async Task<bool> RequestHasCompletedOperationAsync(int requestId)
    {
        return await _context.RescueOperations
            .AnyAsync(o => o.RequestId == requestId && o.Status == "Completed");
    }

    /// <summary>
    /// Hàm hỗ trợ: Chuẩn hóa Key trạng thái (Viết hoa, thay dấu cách bằng gạch dưới).
    /// </summary>
    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }

    /// <summary>
    /// Hàm hỗ trợ: So sánh hai số điện thoại sau khi đã chuẩn hóa.
    /// </summary>
    private static bool PhoneMatches(string? left, string? right)
    {
        return NormalizePhone(left) == NormalizePhone(right);
    }

    /// <summary>
    /// Hàm hỗ trợ: Chuẩn hóa số điện thoại về dạng chuỗi số nguyên bản (Xử lý đầu số 84 -> 0).
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
    /// API Quản trị (Admin): Lấy toàn bộ lịch sử thay đổi trạng thái của các yêu cầu cứu hộ trên hệ thống.
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
