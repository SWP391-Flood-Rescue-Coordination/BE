using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// RescueRequestController: Quản lý các yêu cầu cứu hộ từ người dân (Citizen) và khách vãng lai (Guest).
/// Cung cấp các chức năng tạo mới, cập nhật, xác minh và báo cáo an toàn cho các yêu cầu cứu nạn.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RescueRequestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo RescueRequestController.
    /// </summary>
    /// <param name="context">DbContext để thao tác dữ liệu.</param>
    public RescueRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API tạo một yêu cầu cứu hộ mới. 
    /// Chấp nhận cả người dùng đã đăng nhập (Citizen) và người dùng chưa đăng nhập (Guest).
    /// </summary>
    /// <param name="dto">Thông tin yêu cầu cứu hộ (Tiêu đề, Mô tả, Tọa độ, Số lượng người...).</param>
    /// <returns>ID của yêu cầu vừa tạo thành công.</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRescueRequestDto dto)
    {
        // 1. Kiểm tra xem người dùng có đang đăng nhập hay không thông qua Claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        // 2. Khởi tạo đối tượng RescueRequest từ DTO
        var request = new RescueRequest
        {
            CitizenId = userId, // Nếu là Guest thì CitizenId sẽ là null
            ContactName = userId == null ? dto.ContactName : null,
            ContactPhone = dto.ContactPhone,
            Title = dto.Title,
            Phone = dto.ContactPhone,
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address,
            AdultCount = dto.AdultCount,
            ElderlyCount = dto.ElderlyCount,
            ChildrenCount = dto.ChildrenCount,
            Status = "Pending", // Trạng thái mặc định khi mới tạo
            CreatedAt = DateTime.UtcNow
        };

        // 3. Tính toán mức độ ưu tiên (Priority Score) dựa trên thành phần người bị nạn
        // Người già (Elderly) hệ số 1.5, Trẻ em (Children) hệ số 1.8
        var elderly = dto.ElderlyCount ?? 0;
        var children = dto.ChildrenCount ?? 0;
        var priorityScore = 1.5 * elderly + 1.8 * children;

        // Phân loại Level dựa trên Score
        if (priorityScore >= 6)
        {
            request.PriorityLevelId = 1; // CAO
        }
        else if (priorityScore >= 3)
        {
            request.PriorityLevelId = 2; // TRUNG BÌNH
        }
        else
        {
            request.PriorityLevelId = 3; // THẤP/THÔNG THƯỜNG
        }

        // 4. Lưu vào Database
        _context.RescueRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            Message = "Tạo yêu cầu cứu hộ thành công",
            RequestId = request.RequestId
        });
    }

    /// <summary>
    /// CITIZEN - Lấy danh sách các yêu cầu cứu hộ do chính người dùng hiện tại tạo ra.
    /// </summary>
    /// <returns>Danh sách các yêu cầu cứu hộ của cá nhân.</returns>
    [HttpGet("my-requests")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> GetMyRequests()
    {
        // 1. Trích xuất ID người dùng từ Token
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var userId = int.Parse(userIdString);

        // 2. Truy vấn danh sách yêu cầu thuộc về User này
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
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        // 3. Kiểm tra xem người dân đã có thể báo an toàn (Confirm Rescued) hay chưa
        await ApplyCanReportSafeAsync(requests);
        
        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// API lấy yêu cầu cứu hộ gần đây nhất. 
    /// Dùng để hiển thị trạng thái hiện thời cho người dân ngay khi vào trang chủ.
    /// </summary>
    [HttpGet("my-latest-request")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyLatestRequest()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = _context.RescueRequests.AsQueryable();

        // Nếu đã đăng nhập, lọc theo ID. Nếu là khách, lọc các yêu cầu không có ID (Guest requests)
        if (!string.IsNullOrEmpty(userIdString))
        {
            var userId = int.Parse(userIdString);
            query = query.Where(r => r.CitizenId == userId);
        }
        else
        {
            query = query.Where(r => r.CitizenId == null);
        }

        // Lấy bản ghi mới nhất dựa trên thời gian tạo
        var latestRequest = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LatestRescueRequestDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Address = r.Address,
                Status = r.Status ?? "Pending",
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (latestRequest == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ nào." });
        }

        // Gán trạng thái có thể báo an toàn hay không
        await ApplyCanReportSafeAsync(latestRequest);
        
        return Ok(new { Success = true, Data = latestRequest });
    }

    /// <summary>
    /// ADMIN / COORDINATOR / MANAGER - Truy vấn toàn bộ danh sách yêu cầu cứu hộ trong hệ thống.
    /// Hỗ trợ lọc theo trạng thái, mức độ ưu tiên và tìm kiếm đa năng.
    /// </summary>
    /// <param name="status">Trạng thái cần lọc (Pending, Verified, v.v.).</param>
    /// <param name="priorityId">ID mức độ ưu tiên cần lọc.</param>
    /// <param name="searchBy">Trường cần tìm kiếm (requestId, phone, address, title, citizenName, contactName).</param>
    /// <param name="keyword">Từ khóa tìm kiếm.</param>
    /// <returns>Danh sách các yêu cầu khớp với bộ lọc.</returns>
    [HttpGet]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetAllRequests(
        [FromQuery] string? status = null, 
        [FromQuery] int? priorityId = null, 
        [FromQuery] string? searchBy = null, 
        [FromQuery] string? keyword = null)
    {
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .AsQueryable();

        // 1. Phân tích logic tìm kiếm (Search)
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Kiểm tra tính hợp lệ của trường tìm kiếm (Whitelist)
            var allowedFields = new[] { "requestId", "phone", "contactPhone", "address", "title", "citizenName", "contactName" };
            if (!allowedFields.Contains(searchBy))
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Trường tìm kiếm '{searchBy}' không hợp lệ. Chỉ chấp nhận các trường: {string.Join(", ", allowedFields)}" 
                });
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                switch (searchBy)
                {
                    case "requestId":
                        query = query.Where(r => r.RequestId.ToString() == keyword);
                        break;
                    case "phone":
                        query = query.Where(r => r.Phone.Contains(keyword));
                        break;
                    case "contactPhone":
                        query = query.Where(r => r.ContactPhone.Contains(keyword));
                        break;
                    case "address":
                        query = query.Where(r => r.Address.ToLower().Contains(keyword));
                        break;
                    case "title":
                        query = query.Where(r => r.Title.ToLower().Contains(keyword));
                        break;
                    case "citizenName":
                        query = query.Where(r => r.Citizen != null && r.Citizen.FullName.ToLower().Contains(keyword));
                        break;
                    case "contactName":
                        query = query.Where(r => r.ContactName != null && r.ContactName.ToLower().Contains(keyword));
                        break;
                }
            }
        }

        // 2. Lọc theo trạng thái và độ ưu tiên
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (priorityId.HasValue)
        {
            query = query.Where(r => r.PriorityLevelId == priorityId.Value);
        }

        // 3. Thực thi truy vấn, sắp xếp theo thời gian tạo mới nhất và Map sang DTO
        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                CitizenId = r.CitizenId,
                // Định danh người gửi: Ưu tiên FullName nếu là Citizen, ngược lại dùng ContactName
                CitizenName = r.Citizen != null ? (r.Citizen.FullName ?? "") : (r.ContactName ?? ""),
                CitizenPhone = r.Citizen != null ? r.Citizen.Phone : r.ContactPhone,
                Title = r.Title,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Data = requests });
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một yêu cầu cứu hộ qua ID.
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
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Address = r.Address,
                PriorityLevelId = r.PriorityLevelId,
                Status = r.Status ?? "Pending",
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
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
    /// API kiểm tra trạng thái yêu cầu dành cho Khách vãng lai (không cần đăng nhập).
    /// </summary>
    /// <param name="requestId">ID yêu cầu đã được cấp sau khi gửi yêu cứu trợ.</param>
    [HttpGet("guest/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRequestByIdForGuest([FromQuery] int requestId)
    {
        var request = await _context.RescueRequests
            .Where(r => r.RequestId == requestId)
            .Select(r => new RescueRequestResponseDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status ?? "Pending",
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                Address = r.Address,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
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
    /// API cho phép Khách vãng lai cập nhật thông tin yêu cầu cứu hộ.
    /// Chỉ cho phép sửa khi trạng thái vẫn đang là 'Pending' hoặc 'Verified'.
    /// </summary>
    [HttpPut("guest/update/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRequestByGuest(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        // 1. Tìm yêu cầu cứu hộ
        var request = await _context.RescueRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        // 2. Kiểm tra trạng thái: Không cho phép sửa nếu đã phân công (Assigned) hoặc đang giải cứu
        if (request.Status != "Pending" && request.Status != "Verified")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });
        }

        // 3. Cập nhật các trường thông tin (Nếu DTO có giá trị mới)
        request.Title = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone = dto.ContactPhone ?? request.Phone;
        request.Address = dto.Address ?? request.Address;
        request.Latitude = dto.Latitude ?? request.Latitude;
        request.Longitude = dto.Longitude ?? request.Longitude;

        var hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount = dto.ChildrenCount ?? request.ChildrenCount;
        }

        request.UpdatedAt = DateTime.UtcNow;

        // 4. Lưu thay đổi
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật yêu cầu thành công" });
    }

    /// <summary>
    /// API cho phép người dùng (Citizen) cập nhật yêu cầu cứu hộ của chính họ.
    /// </summary>
    [HttpPut("{id}/update")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> UpdateRequestByCitizen(int id, [FromBody] UpdateRescueRequestDto dto)
    {
        // 1. Xác thực quyền sở hữu yêu cầu
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

        // 2. Kiểm tra trạng thái
        if (request.Status != "Pending" && request.Status != "Verified")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Không thể chỉnh sửa yêu cầu khi đang ở trạng thái: {request.Status}"
            });
        }

        // 3. Cập nhật dữ liệu
        request.Title = dto.Title ?? request.Title;
        request.Description = dto.Description ?? request.Description;
        request.Phone = dto.ContactPhone ?? request.Phone;
        request.ContactPhone = dto.ContactPhone ?? request.ContactPhone;
        request.Address = dto.Address ?? request.Address;
        request.Latitude = dto.Latitude ?? request.Latitude;
        request.Longitude = dto.Longitude ?? request.Longitude;

        var hasAnyCountUpdate = dto.AdultCount.HasValue || dto.ElderlyCount.HasValue || dto.ChildrenCount.HasValue;
        if (hasAnyCountUpdate)
        {
            request.AdultCount = dto.AdultCount ?? request.AdultCount;
            request.ElderlyCount = dto.ElderlyCount ?? request.ElderlyCount;
            request.ChildrenCount = dto.ChildrenCount ?? request.ChildrenCount;
        }

        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        // 4. Lưu thay đổi
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



    /// <summary>
    /// ADMIN / COORDINATOR - Cập nhật trực tiếp trạng thái của yêu cầu cứu hộ.
    /// Đồng thời ghi lại lịch sử thay đổi trạng thái.
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "RESCUE_TEAM,COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        // 1. Tìm yêu cầu
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        // 2. Lấy định danh người thực hiện cập nhật
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdString != null ? int.Parse(userIdString) : 0;
        var now = DateTime.UtcNow;
        var nextStatusKey = NormalizeStatusKey(dto.Status);

        // 3. Nếu chuyển sang Hủy thì giải phóng operation, đội và xe liên quan.
        if (nextStatusKey == "CANCELLED" || nextStatusKey == "CANCELED")
        {
            var activeOperations = (await _context.RescueOperations
                .Include(o => o.Team)
                .Where(o => o.RequestId == request.RequestId)
                .ToListAsync())
                .Where(o =>
                {
                    var operationStatusKey = NormalizeStatusKey(o.Status);
                    return operationStatusKey == "ASSIGNED"
                        || operationStatusKey == "IN_PROGRESS"
                        || operationStatusKey == "SCHEDULED";
                })
                .ToList();

            if (activeOperations.Count > 0)
            {
                foreach (var operation in activeOperations)
                {
                    operation.Status = "Cancelled";
                    operation.CompletedAt ??= now;

                    if (operation.Team != null)
                    {
                        operation.Team.Status = "AVAILABLE";
                    }
                }

                var activeOperationIds = activeOperations
                    .Select(o => o.OperationId)
                    .Distinct()
                    .ToList();

                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(ov => activeOperationIds.Contains(ov.OperationId))
                    .Select(ov => ov.VehicleId)
                    .Distinct()
                    .ToListAsync();

                if (vehicleIds.Count > 0)
                {
                    var vehicles = await _context.Vehicles
                        .Where(v => vehicleIds.Contains(v.VehicleId))
                        .ToListAsync();

                    foreach (var vehicle in vehicles)
                    {
                        vehicle.Status = "AVAILABLE";
                        vehicle.UpdatedAt = now;
                    }
                }
            }
        }
        if (nextStatusKey == "COMPLETED")
        {
            var activeOperations = (await _context.RescueOperations
                .Include(o => o.Team)
                .Where(o => o.RequestId == request.RequestId)
                .ToListAsync())
                .Where(o =>
                {
                    var operationStatusKey = NormalizeStatusKey(o.Status);
                    return operationStatusKey == "ASSIGNED"
                        || operationStatusKey == "IN_PROGRESS"
                        || operationStatusKey == "SCHEDULED";
                })
                .ToList();

            if (activeOperations.Count > 0)
            {
                foreach (var operation in activeOperations)
                {
                    operation.Status = "Completed";
                    operation.CompletedAt ??= now;

                    if (operation.Team != null)
                    {
                        operation.Team.Status = "AVAILABLE";
                    }
                }

                var activeOperationIds = activeOperations
                    .Select(o => o.OperationId)
                    .Distinct()
                    .ToList();

                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(ov => activeOperationIds.Contains(ov.OperationId))
                    .Select(ov => ov.VehicleId)
                    .Distinct()
                    .ToListAsync();

                if (vehicleIds.Count > 0)
                {
                    var vehicles = await _context.Vehicles
                        .Where(v => vehicleIds.Contains(v.VehicleId))
                        .ToListAsync();

                    foreach (var vehicle in vehicles)
                    {
                        vehicle.Status = "AVAILABLE";
                        vehicle.UpdatedAt = now;
                    }
                }
            }
        }

        // 4. Cập nhật trạng thái request
        request.Status = dto.Status;
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        // 5. Lưu vết lịch sử (History)
        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.Status,
            Notes = "Trạng thái cập nhật bởi hệ thống quản lý",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// COORDINATOR - Xác minh yêu cầu cứu hộ (Chuyển từ Pending sang Verified).
    /// Đây là bước quan trọng để yêu cầu có thể được phân công cho các đội cứu hộ.
    /// </summary>
    [HttpPut("{id}/verify")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> VerifyRequest(int id)
    {
        // 1. Tìm yêu cầu
        var request = await _context.RescueRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ" });
        }

        // 2. Chỉ cho phép xác minh các yêu cầu chưa được xử lý (Pending)
        if (request.Status != "Pending")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Yêu cầu cứu hộ phải ở trạng thái Pending (hiện tại: {request.Status})"
            });
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdString != null ? int.Parse(userIdString) : 0;

        // 3. Cập nhật sang trạng thái 'Verified'
        request.Status = "Verified";
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;

        // 4. Lưu vết lịch sử xác minh
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

    /// <summary>
    /// CITIZEN - Người dân xác nhận đã được cứu an toàn (Confirm Rescued).
    /// Giúp đóng yêu cầu cứu hộ sau khi đội cứu hộ đã hoàn tất công việc.
    /// </summary>
    [HttpPut("{id}/confirm-rescued")]
    [Authorize(Roles = "CITIZEN")]
    public async Task<IActionResult> ConfirmRescued(int id)
    {
        // 1. Xác thực ID người dùng
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var userId = int.Parse(userIdString);

        // 2. Tìm yêu cầu thuộc sở hữu của User
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

        // 3. Chỉ cho phép báo an toàn khi yêu cầu đã được phân công (Assigned)
        if (request.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể báo an toàn khi yêu cầu đang ở trạng thái 'Assigned'. Trạng thái hiện tại: '{request.Status}'."
            });
        }

        // 4. Ràng buộc: Đội cứu hộ phải xác nhận HOÀN TẤT trước thì người dân mới có thể xác nhận AN TOÀN
        var canReportSafe = await RequestHasCompletedOperationAsync(request.RequestId);
        if (!canReportSafe)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Đội cứu hộ chưa xác nhận hoàn tất nhiệm vụ, bạn chưa thể báo an toàn vào lúc này."
            });
        }

        // 5. Cập nhật yêu cầu sang trạng thái 'Completed'
        var now = DateTime.UtcNow;
        request.Status = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        // 6. Ghi lịch sử báo an toàn
        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Completed",
            Notes = "Công dân đã báo an toàn sau khi đội cứu hộ xác nhận hoàn tất thực địa.",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Báo an toàn thành công. Yêu cầu đã được đóng chính thức."
        });
    }

    /// <summary>
    /// GUEST - Khách vãng lai báo an toàn. Cần cung cấp Số điện thoại để xác thực quyền sở hữu yêu cầu.
    /// </summary>
    [HttpPut("guest/{id}/confirm-rescued")]
    [AllowAnonymous]
    public async Task<IActionResult> GuestConfirmRescued(int id, [FromBody] GuestConfirmRescuedDto dto)
    {
        // 1. Tìm yêu cầu
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

        // 2. Xác thực bằng số điện thoại đăng ký (Phone hoặc ContactPhone)
        if (!PhoneMatches(request.Phone, dto.Phone) && !PhoneMatches(request.ContactPhone, dto.Phone))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Số điện thoại không khớp với số đã dùng để gửi yêu cầu."
            });
        }

        // 3. Kiểm tra trạng thái và xác nhận từ đội cứu hộ
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
                Message = "Đội cứu hộ chưa xác nhận hoàn tất, khách chưa thể tự báo an toàn."
            });
        }

        // 4. Hoàn tất yêu cầu
        var now = DateTime.UtcNow;
        request.Status = "Completed";
        request.UpdatedAt = now;
        request.UpdatedBy = 0; // Hệ thống/Guest

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = "Completed",
            Notes = "Khách vãng lai đã báo an toàn thông qua số điện thoại xác thực.",
            UpdatedBy = 0,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            RequestId = request.RequestId,
            Status = request.Status,
            Message = "Báo an toàn thành công."
        });
    }

    /// <summary>
    /// API cung cấp dữ liệu thống kê tổng hợp cho Dashboard quản lý.
    /// Bao gồm số lượng yêu cầu theo từng loại trạng thái và số yêu cầu mới trong ngày.
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "MANAGER,ADMIN,COORDINATOR")]
    public async Task<IActionResult> GetStatistics()
    {
        // Thực hiện đếm trực tiếp trên Database cho các trạng thái quan trọng
        var totalRequests = await _context.RescueRequests.CountAsync();
        var pending = await _context.RescueRequests.CountAsync(r => r.Status == "Pending");
        var verified = await _context.RescueRequests.CountAsync(r => r.Status == "Verified");
        var inProgress = await _context.RescueRequests.CountAsync(r => r.Status == "In Progress");
        var completed = await _context.RescueRequests.CountAsync(r => r.Status == "Completed");
        var citizenConfirmed = await _context.RescueRequests.CountAsync(r => r.Status == "CitizenConfirmed");
        var cancelled = await _context.RescueRequests.CountAsync(r => r.Status == "Cancelled");
        var duplicate = await _context.RescueRequests.CountAsync(r => r.Status == "Duplicate");
        
        // Thống kê số yêu cầu phát sinh trong ngày hôm nay
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
    /// Kiểm tra và gán giá trị CanReportSafe cho danh sách DTO.
    /// Giúp Front-end biết có nên hiển thị nút "Báo an toàn" hay không.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(List<RescueRequestResponseDto> requests)
    {
        if (requests.Count == 0) return;

        // Lấy danh sách RequestId có các Operation đã hoàn tất (Completed)
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(requests.Select(r => r.RequestId));
        
        foreach (var request in requests)
        {
            // Điều kiện: Đang bị gán nhiệm vụ (Assigned) VÀ Đội cứu hộ đã báo xong (Completed Operation)
            request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
                && completedRequestIds.Contains(request.RequestId);
        }
    }

    /// <summary>
    /// Kiểm tra và gán giá trị CanReportSafe cho một DTO duy nhất.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(RescueRequestResponseDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Kiểm tra và gán giá trị CanReportSafe cho LatestRescueRequestDto.
    /// </summary>
    private async Task ApplyCanReportSafeAsync(LatestRescueRequestDto request)
    {
        var completedRequestIds = await GetCompletedOperationRequestIdsAsync(new[] { request.RequestId });
        request.CanReportSafe = NormalizeStatusKey(request.Status) == "ASSIGNED"
            && completedRequestIds.Contains(request.RequestId);
    }

    /// <summary>
    /// Truy vấn Database để tìm những RequestId nào có ít nhất một Rescue Operation đã ở trạng thái 'Completed'.
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
    /// Kiểm tra xem một yêu cầu cứu hộ cụ thể đã có báo cáo hoàn tất từ Đội cứu hộ hay chưa.
    /// </summary>
    private async Task<bool> RequestHasCompletedOperationAsync(int requestId)
    {
        return await _context.RescueOperations
            .AnyAsync(o => o.RequestId == requestId && o.Status == "Completed");
    }

    /// <summary>
    /// Chuẩn hóa chuỗi trạng thái thành IN_UPPER_CASE_SNAKE để dễ so sánh logic.
    /// </summary>
    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }

    /// <summary>
    /// So sánh hai số điện thoại xem có khớp nhau không sau khi đã chuẩn hóa.
    /// </summary>
    private static bool PhoneMatches(string? left, string? right)
    {
        return NormalizePhone(left) == NormalizePhone(right);
    }

    /// <summary>
    /// Chuẩn hóa số điện thoại: Loại bỏ ký tự không phải số, chuyển '84' về '0'.
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
}
