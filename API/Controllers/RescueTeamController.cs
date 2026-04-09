using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// RescueTeamController: Quản lý các hoạt động và nhiệm vụ dành riêng cho Đội cứu hộ.
/// Cho phép đội cứu hộ xem danh sách nhiệm vụ được giao, cập nhật trạng thái thực hiện và xem thông tin chi tiết.
/// </summary>
[ApiExplorerSettings(GroupName = "rescue-team")]
[ApiController]
[Route("api/rescue-team")]
[Authorize(Roles = "RESCUE_TEAM,COORDINATOR,ADMIN")]
public class RescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private const string ActionLeaderAssignedMember = "LEADER_ASSIGNED_MEMBER";
    private const string ActionMemberRequestedSupport = "MEMBER_REQUESTED_SUPPORT";
    private const string ActionMemberCompleted = "MEMBER_COMPLETED";
    private const string ActionLeaderRemovedMember = "LEADER_REMOVED_MEMBER";
    private const string ActionLeaderCompletedOperation = "LEADER_COMPLETED_OPERATION";
    private const string ActionLeaderFailedOperation = "LEADER_FAILED_OPERATION";
    private const string ActionLeaderRejectedRequest = "LEADER_REJECTED_REQUEST";

    /// <summary>
    /// Constructor khởi tạo RescueTeamController với DbContext.
    /// </summary>
    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Đội trưởng (Leader) - Cập nhật trạng thái nhiệm vụ cứu hộ từ Waiting sang Completed.
    /// </summary>
    /// <param name="operationId">ID của nhiệm vụ cần cập nhật.</param>
    /// <param name="dto">Trạng thái mới (phải là COMPLETED).</param>
    /// <returns>Kết quả cập nhật trạng thái.</returns>
    [HttpPut("operations/{operationId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        // Quy tắc nghiệp vụ mới:
        // - Chỉ Leader được cập nhật trạng thái operation.
        // - Chỉ cho phép chuyển Waiting -> Completed.
        // - Không reset RequestId của member ở bước này.
        // - Vẫn phải giải phóng phương tiện về Available.
        if (dto == null) return BadRequest(new { Success = false, Message = "Dữ liệu gửi lên không hợp lệ." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        var targetStatusKey = dto.NewStatus.Trim().ToUpperInvariant();
        if (targetStatusKey != "COMPLETED")
        {
            return BadRequest(new
            {
                Success = false,
                Message = "NewStatus chỉ chấp nhận COMPLETED."
            });
        }

        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null) return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });
        var request = operation.Request;
        if (request == null) return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ liên kết." });

        var isLeader = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive
                        && m.MemberRole == "Leader");
        if (!isLeader) return Forbid();

        if (operation.Status != "Waiting")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}', không thể chốt. Chỉ hỗ trợ Waiting -> Completed."
            });
        }

        var now = DateTime.UtcNow;
        var batchId = Guid.NewGuid();
        var strategy = _context.Database.CreateExecutionStrategy();
        IActionResult? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            operation.Status = "Completed";
            operation.StartedAt ??= now;
            operation.CompletedAt = now;

            AddDelegationActionLog(
                batchId,
                request.RequestId,
                operation.OperationId,
                userId,
                null,
                ActionLeaderCompletedOperation,
                null,
                request.Status,
                operation.Status,
                now);

            var vehicleIds = await _context.RescueOperationVehicles
                .Where(rov => rov.OperationId == operation.OperationId)
                .Select(rov => rov.VehicleId)
                .ToListAsync();
            if (vehicleIds.Count > 0)
            {
                var vehicles = await _context.Vehicles.Where(v => vehicleIds.Contains(v.VehicleId)).ToListAsync();
                foreach (var vehicle in vehicles)
                {
                    vehicle.Status = "Available";
                    vehicle.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result = Ok(new
            {
                Success = true,
                OperationId = operation.OperationId,
                RequestId = request.RequestId,
                OperationStatus = operation.Status,
                RequestStatus = request.Status,
                BatchId = batchId,
                Message = "Đã cập nhật operation Waiting -> Completed thành công."
            });
        });

        return result ?? StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi cập nhật trạng thái." });
    }

    /// <summary>
    /// Thành viên (Member) - Chuyển trạng thái nhiệm vụ từ Assigned sang Waiting.
    /// Cho phép đội cứu hộ báo cáo việc tạm dừng hoặc chờ đợi trong quá trình thực hiện nhiệm vụ.
    /// </summary>
    /// <param name="operationId">ID của nhiệm vụ cần chuyển sang trạng thái chờ.</param>
    /// <returns>Thông tin trạng thái mới của nhiệm vụ.</returns>
    [HttpPut("operations/{operationId}/waiting")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> SetOperationToWaiting(int operationId)
    {
        // Endpoint này cho phép member/leader báo trạng thái "đang chờ"
        // (ví dụ thiếu phương tiện, thời tiết xấu, chờ điều phối...).
        // 1. Xác thực định danh người dùng từ Token
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Truy vấn thông tin nhiệm vụ cứu hộ
        var operation = await _context.RescueOperations
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ cứu hộ." });
        }

        // 3. Kiểm tra quyền hạn: User phải là thành viên (Member hoặc Leader) đang hoạt động của đội được giao nhiệm vụ
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
        {
            return Forbid();
        }

        // 4. Kiểm tra logic trạng thái: Chỉ cho phép chuyển sang Waiting từ trạng thái Assigned
        if (operation.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}'. Chỉ có thể chuyển sang 'Waiting' từ trạng thái 'Assigned'."
            });
        }

        // 5. Cập nhật trạng thái nhiệm vụ
        var now = DateTime.UtcNow;
        operation.Status = "Waiting";
        operation.StartedAt ??= now; // Ghi nhận thời điểm bắt đầu hành trình nếu chưa có

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            OperationId = operation.OperationId,
            OperationStatus = operation.Status,
            StartedAt = operation.StartedAt,
            Message = "Chuyển trạng thái nhiệm vụ sang Waiting thành công."
        });
    }

    /// <summary>

    /// Đội trưởng (Leader) - Từ chối Yêu cầu cứu hộ được phân công (Gỡ bỏ gán đội).
    /// </summary>
    [HttpPut("requests/{requestId}/reject")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> RejectRequest(int requestId, [FromQuery] string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(new { Success = false, Message = "Reason là bắt buộc khi reject request." });
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        var request = await _context.RescueRequests.FindAsync(requestId);
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });
        }

        if (request.Status != "Verified" && request.Status != "Assigned")
        {
            return BadRequest(new { Success = false, Message = $"Yêu cầu đang ở trạng thái {request.Status}. Không thể từ chối." });
        }

        if (!request.TeamId.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Yêu cầu này chưa được hệ thống điều phối cho đội nào." });
        }

        var isLeader = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == request.TeamId.Value 
                        && m.UserId == userId 
                        && m.MemberRole == "Leader" 
                        && m.IsActive);
                        
        if (!isLeader)
        {
            return StatusCode(403, new { Success = false, Message = "Bạn không có quyền thực hiện. Chỉ Đội trưởng (Leader) của đội này mới có quyền từ chối." });
        }

        var now = DateTime.UtcNow;
        var batchId = Guid.NewGuid();
        var operation = await _context.RescueOperations
            .Where(o => o.RequestId == requestId && o.TeamId == request.TeamId)
            .OrderByDescending(o => o.AssignedAt)
            .FirstOrDefaultAsync();

        var strategy = _context.Database.CreateExecutionStrategy();
        IActionResult? result = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var activeMembers = await _context.RescueTeamMembers
                .Where(m => m.TeamId == request.TeamId && m.IsActive && m.RequestId == requestId)
                .ToListAsync();

            if (activeMembers.Count > 0 && operation == null)
            {
                result = Conflict(new
                {
                    Success = false,
                    Message = "Không thể reject do member đang active nhưng không tìm thấy operation để ghi log remove."
                });
                return;
            }

            foreach (var member in activeMembers)
            {
                member.RequestId = null;
                AddDelegationActionLog(
                    batchId,
                    requestId,
                    operation?.OperationId,
                    userId,
                    member.UserId,
                    ActionLeaderRemovedMember,
                    null,
                    request.Status,
                    operation?.Status,
                    now);
            }

            if (operation != null)
            {
                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(rov => rov.OperationId == operation.OperationId)
                    .Select(rov => rov.VehicleId)
                    .ToListAsync();
                if (vehicleIds.Count > 0)
                {
                    var vehicles = await _context.Vehicles.Where(v => vehicleIds.Contains(v.VehicleId)).ToListAsync();
                    foreach (var v in vehicles)
                    {
                        v.Status = "Available";
                        v.UpdatedAt = now;
                    }
                }
            }

            request.Status = "Verified";
            request.TeamId = null;
            request.UpdatedAt = now;
            request.UpdatedBy = userId;

            AddDelegationActionLog(
                batchId,
                requestId,
                null,
                userId,
                null,
                ActionLeaderRejectedRequest,
                reason.Trim(),
                request.Status,
                null,
                now);

            await UpsertRequestStatusHistoryAsync(
                requestId,
                "Verified",
                $"Đội trưởng từ chối tiếp nhận. Lý do: {reason.Trim()}",
                userId,
                now);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result = Ok(new { Success = true, BatchId = batchId, Message = "Đã từ chối và giải phóng yêu cầu cứu hộ thành công." });
        });

        return result ?? StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi reject request." });
    }

    /// <summary>
    /// Đội trưởng (Leader) - Tiếp nhận Yêu cầu cứu hộ được phân công (Chuyển trạng thái sang Assigned).
    /// </summary>
    [HttpPut("requests/{requestId}/accept")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        // 1. Trích xuất ID người dùng
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Tìm yêu cầu cứu hộ
        var request = await _context.RescueRequests.FindAsync(requestId);
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });
        }

        // 3. Kiểm tra trạng thái và cấu hình
        if (request.Status != "Verified")
        {
            return BadRequest(new { Success = false, Message = $"Yêu cầu đang ở trạng thái {request.Status}. Phải là Verified để có thể tiếp nhận." });
        }

        if (!request.TeamId.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Yêu cầu này chưa được hệ thống điều phối cho bất kỳ đội nào." });
        }

        // 4. Phân quyền: Người dùng phải tham gia Team đó và mang quyền Leader
        var isLeader = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == request.TeamId.Value 
                        && m.UserId == userId 
                        && m.MemberRole == "Leader" 
                        && m.IsActive);
                        
        if (!isLeader)
        {
            return StatusCode(403, new { Success = false, Message = "Bạn không có quyền thực hiện. Chỉ Đội trưởng (Leader) của đội này mới có quyền tiếp nhận." });
        }

        // 5. Cập nhật trạng thái yêu cầu
        var now = DateTime.UtcNow;
        request.Status = "Assigned";
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        await UpsertRequestStatusHistoryAsync(
            requestId,
            "Assigned",
            $"Đội trưởng xác nhận tiếp nhận yêu cầu.",
            userId,
            now);

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Tiếp nhận yêu cầu thành công" });
    }

    /// <summary>
    /// Đội trưởng (Leader) - Lấy danh sách các yêu cầu cứu hộ đã được điều phối cho đội của mình.
    /// Nếu có truyền status thì lọc theo status, nếu không thì lấy tất cả status của team đó
    /// </summary>
    [HttpGet("assigned-requests")]
    public async Task<IActionResult> GetAssignedRequests([FromQuery] string? status = null)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 1. Tìm danh sách TeamId mà user này là Leader đang hoạt động
        var leaderTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive && m.MemberRole == "Leader")
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!leaderTeamIds.Any())
        {
            return Ok(new { Success = true, Message = "Bạn không phải là Đội trưởng của đội cứu hộ nào.", Data = new List<object>() });
        }

        // 2. Lấy ra các yêu cầu cứu hộ được gán cho những Team đó
        var query = _context.RescueRequests
            .Include(r => r.Citizen)
            .Where(r => r.TeamId.HasValue && leaderTeamIds.Contains(r.TeamId.Value));

        // Nếu có truyền status thì lọc theo status, nếu không thì lấy tất cả status của team đó
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var requests = await query
            .OrderBy(r => r.Status) // Sắp xếp theo trạng thái để "nhóm" chúng lại
            .ThenByDescending(r => r.PriorityLevelId)
            .ThenByDescending(r => r.CreatedAt)
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
                Status = r.Status,
                AdultCount = r.AdultCount,
                ElderlyCount = r.ElderlyCount,
                ChildrenCount = r.ChildrenCount,
                NumberOfAffectedPeople = r.NumberOfAffectedPeople,
                TeamId = r.TeamId,
                TeamName = _context.RescueTeams.Where(t => t.TeamId == r.TeamId).Select(t => t.TeamName).FirstOrDefault(),
                OperationId = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId && o.TeamId == r.TeamId)
                    .Select(o => (int?)o.OperationId)
                    .FirstOrDefault(),
                OperationStatus = _context.RescueOperations
                    .Where(o => o.RequestId == r.RequestId && o.TeamId == r.TeamId)
                    .Select(o => o.Status)
                    .FirstOrDefault(),
                HasSupportRequest = _context.RescueDelegationActionLogs
                    .Any(l => l.RequestId == r.RequestId && l.ActionType == ActionMemberRequestedSupport),
                LastSupportRequestedAt = _context.RescueDelegationActionLogs
                    .Where(l => l.RequestId == r.RequestId && l.ActionType == ActionMemberRequestedSupport)
                    .OrderByDescending(l => l.ActionAt)
                    .Select(l => (DateTime?)l.ActionAt)
                    .FirstOrDefault(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = requests.Count, Data = requests });
    }

    /// <summary>
    /// Thành viên (Member) - Lấy thông tin nhiệm vụ (Rescue Request) duy nhất đang được giao cho mình.
    /// Giúp thành viên nhanh chóng xem được địa chỉ và thông tin người cần cứu nạn mà không cần tìm kiếm.
    /// </summary>
    [HttpGet("my-current-task")]
    public async Task<IActionResult> GetMyCurrentTask()
    {
        // 1. Trích xuất ID người dùng
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Tìm bản ghi thành viên đang hoạt động có RequestId (nhiệm vụ được giao)
        var memberInfo = await _context.RescueTeamMembers
            .Include(m => m.Team)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive && m.RequestId.HasValue);

        if (memberInfo == null)
        {
            return Ok(new { Success = true, Message = "Bạn hiện không được giao nhiệm vụ cụ thể nào.", Data = (object?)null });
        }

        var requestId = memberInfo.RequestId!.Value;
        var teamId = memberInfo.TeamId;

        // 3. Lấy thông tin Request và Operation liên quan
        var taskData = await _context.RescueRequests
            .Where(r => r.RequestId == requestId)
            .Select(r => new
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Description = r.Description,
                Phone = r.Phone,
                Address = r.Address,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                PriorityName = r.PriorityLevelId == 1 ? "CAO" :
                              r.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                              r.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG",
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                
                // Đính kèm thông tin Team
                TeamId = teamId,
                TeamName = memberInfo.Team != null ? memberInfo.Team.TeamName : null,

                // Tìm Operation tương ứng của Team cho Request này
                Operation = _context.RescueOperations
                    .Where(o => o.RequestId == requestId && o.TeamId == teamId)
                    .Select(o => new
                    {
                        o.OperationId,
                        o.Status,
                        o.AssignedAt,
                        o.StartedAt,
                        o.EstimatedTime,
                        Vehicles = _context.RescueOperationVehicles
                            .Where(ov => ov.OperationId == o.OperationId)
                            .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (_, v) => v.VehicleName)
                            .ToList()
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (taskData == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy dữ liệu yêu cầu cứu hộ đã giao." });
        }

        return Ok(new { Success = true, Data = taskData });
    }

    /// <summary>
    /// Đội cứu hộ - Lấy danh sách nhiệm vụ được giao cho các đội mà user hiện tại đang tham gia.
    /// Chứa các thông tin quan trọng như: Địa chỉ, Số điện thoại người dân, Mức độ ưu tiên, Phương tiện gán kèm.
    /// </summary>
    /// <returns>Danh sách các nhiệm vụ đang ở trạng thái 'Assigned'.</returns>
    [HttpGet("my-operations")]
    public async Task<IActionResult> GetMyOperations()
    {
        // 1. Trích xuất ID người dùng
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Tìm danh sách TeamId mà user này là thành viên đang hoạt động
        var myTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!myTeamIds.Any())
        {
            return Ok(new { Success = true, Message = "Bạn chưa thuộc đội cứu hộ nào.", Data = new List<object>() });
        }

        // 3. Lấy ra các nhiệm vụ 'Assigned' của những Team đó
        var operations = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .Where(o => myTeamIds.Contains(o.TeamId) && o.Status == "Assigned")
            .OrderBy(o => o.AssignedAt)
            .Select(o => new
            {
                o.OperationId,
                o.RequestId,
                RequestTitle = o.Request != null ? o.Request.Title : null,
                RequestStatus = o.Request != null ? o.Request.Status : null,
                RequestAddress = o.Request != null ? o.Request.Address : null,
                RequestDescription = o.Request != null ? o.Request.Description : null,
                RequestPhone = o.Request != null ? o.Request.Phone : null,
                // Chuyển ID mức độ ưu tiên sang nhãn văn bản để dễ đọc
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" :
                                                    o.Request.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                                    o.Request.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG") : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
                o.AssignedAt,
                o.StartedAt,
                o.CompletedAt,
                o.EstimatedTime,
                // Lấy danh sách tên phương tiện được phân bổ cho nhiệm vụ
                Vehicles = _context.RescueOperationVehicles
                    .Where(ov => ov.OperationId == o.OperationId)
                    .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (_, v) => v.VehicleName)
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = operations.Count, Data = operations });
    }

    /// <summary>
    /// Lấy toàn bộ thông tin chi tiết của một nhiệm vụ cụ thể.
    /// Có kiểm tra quyền hạn (chỉ xem được nhiệm vụ của đội mình tham gia).
    /// </summary>
    /// <param name="operationId">ID nhiệm vụ.</param>
    /// <returns>Dữ liệu chi tiết nhiệm vụ và danh sách phương tiện.</returns>
    [HttpGet("operations/{operationId:int}")]
    public async Task<IActionResult> GetMissionDetails(int operationId)
    {
        // 1. Xác thực User
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Truy vấn thông tin chi tiết nhiệm vụ
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .Where(o => o.OperationId == operationId)
            .Select(o => new
            {
                o.OperationId,
                o.RequestId,
                RequestTitle = o.Request != null ? o.Request.Title : null,
                RequestStatus = o.Request != null ? o.Request.Status : null,
                RequestAddress = o.Request != null ? o.Request.Address : null,
                RequestDescription = o.Request != null ? o.Request.Description : null,
                RequestPhone = o.Request != null ? o.Request.Phone : null,
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" :
                                                    o.Request.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                                    o.Request.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG") : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
                o.AssignedAt,
                o.StartedAt,
                o.CompletedAt,
                o.EstimatedTime,
                Vehicles = _context.RescueOperationVehicles
                    .Where(ov => ov.OperationId == o.OperationId)
                    .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (_, v) => v.VehicleName)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });
        }

        // 3. Phân quyền: Lấy TeamId gán cho nhiệm vụ này để kiểm tra tư cách thành viên của User
        var teamId = await _context.RescueOperations
            .Where(ro => ro.OperationId == operationId)
            .Select(ro => ro.TeamId)
            .FirstOrDefaultAsync();

        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == teamId
                        && m.UserId == userId
                        && m.IsActive);

        if (!isMember)
        {
            return Forbid(); // Không có quyền xem nhiệm vụ của đội khác
        }

        return Ok(new { Success = true, Data = operation });
    }

    /// <summary>
    /// Đội trưởng (Leader) - Giao nhiệm vụ cho một hoặc nhiều thành viên cùng lúc.
    /// Thành viên đang bận hoặc không thuộc đội sẽ được ghi vào danh sách skipped, không làm hỏng cả request.
    /// </summary>
    [HttpPost("members/assign-task")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> AssignTaskToMember([FromBody] MemberAssignmentDto dto)
    {
        // API giao việc của Leader:
        // - Có thể gán nhiều member trong một lần gọi.
        // - Hỗ trợ gán 1 hoặc nhiều VehicleIds.
        // - Tất cả vehicle phải AVAILABLE trước khi gán.
        var leaderIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(leaderIdStr, out var leaderId)) return Unauthorized();

        if (dto.UserIds == null || !dto.UserIds.Any())
            return BadRequest(new { Success = false, Message = "Danh sách thành viên (userIds) không được để trống." });

        var request = await _context.RescueRequests.FindAsync(dto.RequestId);
        if (request == null) 
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });

        if (!request.TeamId.HasValue)
            return BadRequest(new { Success = false, Message = "Yêu cầu này chưa được hệ thống điều phối cho đội nào." });

        var leaderMember = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId.Value 
                                   && m.UserId == leaderId 
                                   && m.MemberRole == "Leader" 
                                   && m.IsActive);

        if (leaderMember == null) return Forbid();

        var teamId = request.TeamId.Value;

        var teamMembers = await _context.RescueTeamMembers
            .Where(m => m.TeamId == teamId && dto.UserIds.Contains(m.UserId) && m.IsActive)
            .ToListAsync();

        var assignedIds = new List<int>();
        var reassignedIds = new List<int>();
        var skippedIds = new List<int>();
        var now = DateTime.UtcNow;
        var batchId = Guid.NewGuid();

        var strategy = _context.Database.CreateExecutionStrategy();
        IActionResult? result = null;
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
            var operation = await _context.RescueOperations
                .FirstOrDefaultAsync(o => o.RequestId == dto.RequestId && o.TeamId == teamId);

            if (operation == null)
            {
                operation = new RescueOperation
                {
                    RequestId = dto.RequestId,
                    TeamId = teamId,
                    AssignedBy = leaderId,
                    AssignedAt = now,
                    Status = "Assigned",
                    NumberOfAffectedPeople = request.NumberOfAffectedPeople
                };
                _context.RescueOperations.Add(operation);
                await _context.SaveChangesAsync();
            }
            else
            {
                operation.Status = "Assigned";
            }

            foreach (var uid in dto.UserIds)
            {
                var member = teamMembers.FirstOrDefault(m => m.UserId == uid);

                if (member == null)
                {
                    skippedIds.Add(uid);
                    continue;
                }

                if (member.RequestId == dto.RequestId)
                {
                    skippedIds.Add(uid);
                    continue;
                }

                if (member.RequestId.HasValue)
                {
                    var oldRequestId = member.RequestId.Value;
                    var oldOperation = await _context.RescueOperations
                        .FirstOrDefaultAsync(o => o.RequestId == oldRequestId && o.TeamId == teamId);
                    var oldRequest = await _context.RescueRequests.FindAsync(oldRequestId);
                    if (oldOperation != null && oldRequest != null)
                    {
                        AddDelegationActionLog(
                            batchId,
                            oldRequestId,
                            oldOperation.OperationId,
                            leaderId,
                            member.UserId,
                            ActionLeaderRemovedMember,
                            null,
                            oldRequest.Status,
                            oldOperation.Status,
                            now);
                    }
                    else
                    {
                        result = Conflict(new
                        {
                            Success = false,
                            Message = $"Không thể reassign user {uid} do không tìm thấy operation cũ để ghi LEADER_REMOVED_MEMBER."
                        });
                        return;
                    }

                    reassignedIds.Add(uid);
                }

                member.RequestId = dto.RequestId;
                assignedIds.Add(uid);

                AddDelegationActionLog(
                    batchId,
                    dto.RequestId,
                    operation.OperationId,
                    leaderId,
                    member.UserId,
                    ActionLeaderAssignedMember,
                    null,
                    request.Status,
                    operation.Status,
                    now);
            }

            if (!assignedIds.Any())
            {
                result = BadRequest(new
                {
                    Success = false,
                    Message = "Không có thành viên hợp lệ để giao việc.",
                    SkippedUserIds = skippedIds
                });
                return;
            }

            var assignedVehicleIds = new List<int>();
            if (dto.VehicleIds != null && dto.VehicleIds.Any())
            {
                var requestedVehicleIds = dto.VehicleIds
                    .Distinct()
                    .ToList();

                var vehicles = await _context.Vehicles
                    .Where(v => requestedVehicleIds.Contains(v.VehicleId))
                    .ToListAsync();

                var foundVehicleIds = vehicles
                    .Select(v => v.VehicleId)
                    .ToHashSet();
                var missingVehicleIds = requestedVehicleIds
                    .Where(id => !foundVehicleIds.Contains(id))
                    .ToList();
                if (missingVehicleIds.Count > 0)
                {
                    result = NotFound(new
                    {
                        Success = false,
                        Message = $"Không tìm thấy vehicle với ID: {string.Join(", ", missingVehicleIds)}"
                    });
                    return;
                }

                var unavailableVehicles = vehicles
                    .Where(v => !string.Equals(v.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
                    .Select(v => new { v.VehicleId, v.Status })
                    .ToList();
                if (unavailableVehicles.Count > 0)
                {
                    result = BadRequest(new
                    {
                        Success = false,
                        Message = $"Các phương tiện sau không ở trạng thái AVAILABLE: {string.Join(", ", unavailableVehicles.Select(v => $"ID={v.VehicleId} ({v.Status})"))}"
                    });
                    return;
                }

                foreach (var vid in requestedVehicleIds)
                {
                    var vehicle = vehicles.FirstOrDefault(v => v.VehicleId == vid);
                    if (vehicle != null)
                    {
                        bool alreadyAssigned = await _context.RescueOperationVehicles
                            .AnyAsync(ov => ov.OperationId == operation.OperationId && ov.VehicleId == vid);

                        if (!alreadyAssigned)
                        {
                            _context.RescueOperationVehicles.Add(new RescueOperationVehicle
                            {
                                OperationId = operation.OperationId,
                                VehicleId = vid,
                                AssignedBy = leaderId,
                                AssignedAt = now
                            });

                            vehicle.Status = "InUse";
                            vehicle.UpdatedAt = now;
                            assignedVehicleIds.Add(vid);
                        }
                    }
                }
            }

            request.Status = "Assigned";
            request.UpdatedAt = now;
            request.UpdatedBy = leaderId;

            await UpsertRequestStatusHistoryAsync(
                dto.RequestId,
                "Assigned",
                $"Đội trưởng giao việc cho {assignedIds.Count} thành viên và gán {assignedVehicleIds.Count} phương tiện.",
                leaderId,
                now);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

                result = Ok(new
                {
                    TeamId = teamId,
                    OperationId = operation.OperationId,
                    BatchId = batchId,
                    AssignedUserIds = assignedIds,
                    ReassignedUserIds = reassignedIds,
                    SkippedUserIds = skippedIds,
                    Message = $"Đã giao nhiệm vụ cho {assignedIds.Count} thành viên."
                });
            });
        }
        catch (DbUpdateException ex) when (IsUniqueActiveAssignmentViolation(ex))
        {
            return Conflict(new
            {
                Success = false,
                Message = "Không thể giao việc do member đã có assignment active khác (vi phạm unique active assignment)."
            });
        }

        return result ?? StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi assign task." });
    }


    /// <summary>
    /// Thành viên (Member) - Xem chi tiết nhiệm vụ (Operation) ĐANG được giao cho cá nhân mình.
    /// Khác với my-operations (xem tất cả nhiệm vụ của đội), endpoint này chỉ hiện đúng 1 task mà Thành viên bị cài RequestId.
    /// </summary>
    [HttpGet("my-assignment")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetMyAssignment()
    {
        // 1. Xác thực User
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        // 2. Tìm bản ghi thành viên có RequestId (tức là đang BẬN làm nhiệm vụ)
        var member = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.RequestId != null && m.IsActive);

        if (member == null || member.RequestId == null)
            return NotFound(new { Success = false, Message = "Bạn hiện đang rảnh. Chưa có nhiệm vụ nào được giao lúc này." });

        // 3. Tìm thông tin chi tiết của Nhiệm vụ (Operation) dựa trên RequestId của thành viên
        // Bởi vì Member bị gán bởi Leader của một Team, nên ta kèm điều kiện TeamId
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .Where(o => o.RequestId == member.RequestId.Value && o.TeamId == member.TeamId)
            .Select(o => new
            {
                o.OperationId,
                o.RequestId,
                RequestTitle = o.Request != null ? o.Request.Title : null,
                RequestStatus = o.Request != null ? o.Request.Status : null,
                RequestAddress = o.Request != null ? o.Request.Address : null,
                RequestDescription = o.Request != null ? o.Request.Description : null,
                RequestPhone = o.Request != null ? o.Request.Phone : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                AdultCount = o.Request != null ? o.Request.AdultCount : (int?)null,
                ElderlyCount = o.Request != null ? o.Request.ElderlyCount : (int?)null,
                ChildrenCount = o.Request != null ? o.Request.ChildrenCount : (int?)null,
                NumberOfAffectedPeople = o.Request != null ? o.Request.NumberOfAffectedPeople : (int?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                OperationStatus = o.Status,
                o.AssignedAt,
                o.EstimatedTime
            })
            .FirstOrDefaultAsync();

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Có lỗi xảy ra: Dữ liệu nhiệm vụ đã bị xóa hoặc không hợp lệ." });
        }

        return Ok(new { Success = true, Data = operation });
    }

    /// <summary>
    /// Thành viên (Member) - Xác nhận hoàn tất nhiệm vụ mà Leader đã giao.
    /// Không cần gửi Body. Hệ thống tự lấy UserId từ Token và kiểm tra RequestId hiện tại.
    /// Sau khi xác nhận: RequestId của Member sẽ được reset về null (thành viên trở về trạng thái Rảnh).
    /// </summary>
    [HttpPut("my-assignment/confirm")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> ConfirmMyTask()
    {
        // 1. Xác thực User từ Token
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        // 2. Tìm bản ghi thành viên trong DB (kiểm tra tư cách thành viên trước)
        var member = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive);

        if (member == null)
            return NotFound(new { Success = false, Message = "Bạn không thuộc đội cứu hộ nào đang hoạt động." });

        // 3. Chỉ cho phép Member (không phải Leader) dùng endpoint này
        if (member.MemberRole == "Leader")
            return StatusCode(403, new
            {
                Success = false,
                Message = "Đội trưởng (Leader) không sử dụng chức năng này. Endpoint này chỉ dành cho Thành viên (Member)."
            });

        // 4. Kiểm tra Member có đang được giao nhiệm vụ không (RequestId != null)
        if (member.RequestId == null)
            return NotFound(new { Success = false, Message = "Bạn hiện đang rảnh. Không có nhiệm vụ nào cần xác nhận." });

        var requestId = member.RequestId.Value;
        var teamId = member.TeamId;

        // 5. Tìm Operation liên quan để trả về thông tin phản hồi
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Where(o => o.RequestId == requestId && o.TeamId == teamId)
            .FirstOrDefaultAsync();

        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy dữ liệu nhiệm vụ liên kết. Vui lòng liên hệ Đội trưởng." });

        // 6. Kiểm tra Operation đang ở trạng thái hợp lệ để xác nhận
        if (operation.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}'. Chỉ có thể xác nhận khi trạng thái là 'Assigned'."
            });
        }

        var now = DateTime.UtcNow;
        var batchId = Guid.NewGuid();
        var strategy = _context.Database.CreateExecutionStrategy();
        IActionResult? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            member.RequestId = null;
            AddDelegationActionLog(
                batchId,
                requestId,
                operation.OperationId,
                userId,
                userId,
                ActionMemberCompleted,
                null,
                operation.Request?.Status ?? "Assigned",
                operation.Status,
                now);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result = Ok(new
            {
                Success = true,
                UserId = userId,
                OperationId = operation.OperationId,
                RequestId = requestId,
                BatchId = batchId,
                Message = "Xác nhận hoàn tất nhiệm vụ thành công."
            });
        });

        return result ?? StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi xác nhận hoàn tất nhiệm vụ." });
    }

    [HttpPost("my-assignment/support")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> RequestSupport()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        var member = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive);
        if (member == null)
            return NotFound(new { Success = false, Message = "Bạn không thuộc đội cứu hộ nào đang hoạt động." });
        if (member.MemberRole == "Leader")
            return StatusCode(403, new { Success = false, Message = "Endpoint này chỉ dành cho Member." });
        if (!member.RequestId.HasValue)
            return NotFound(new { Success = false, Message = "Bạn chưa có assignment để báo hỗ trợ." });

        var operation = await _context.RescueOperations
            .FirstOrDefaultAsync(o => o.RequestId == member.RequestId.Value && o.TeamId == member.TeamId);
        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy operation liên kết." });
        if (operation.Status != "Assigned")
            return BadRequest(new { Success = false, Message = "Chỉ được báo hỗ trợ khi operation đang Assigned." });

        var now = DateTime.UtcNow;
        AddDelegationActionLog(
            Guid.NewGuid(),
            operation.RequestId,
            operation.OperationId,
            userId,
            userId,
            ActionMemberRequestedSupport,
            null,
            "Assigned",
            operation.Status,
            now);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Success = true,
            OperationId = operation.OperationId,
            RequestId = operation.RequestId,
            Message = "Đã gửi yêu cầu hỗ trợ."
        });
    }

    /// <summary>
    /// Đội trưởng (Leader) - Xem danh sách thành viên trong đội của mình (có hỗ trợ tìm kiếm).
    /// </summary>
    [HttpGet("members")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetTeamMembers([FromQuery] string? search = null)
    {
        // 1. Xác thực Leader
        var leaderIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(leaderIdStr, out var leaderId)) return Unauthorized();

        // 2. Lấy danh sách ID các đội mà user(Leader) này đang quản lý
        var leaderTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == leaderId && m.MemberRole == "Leader" && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!leaderTeamIds.Any()) 
            return Forbid(); // Không phải leader của đội nào

        // 3. Lấy thông tin thành viên (Join bảng Users để lấy tên/SĐT)
        var query = _context.RescueTeamMembers
            .Include(m => m.User)
            .Where(m => leaderTeamIds.Contains(m.TeamId) && m.IsActive)
            .AsQueryable();

        // 4. Lọc theo chuỗi tìm kiếm (tên hoặc ID)
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(m => 
                m.UserId.ToString().Contains(search) || 
                (m.User != null && m.User.FullName != null && m.User.FullName.ToLower().Contains(search)) ||
                (m.User != null && m.User.Username != null && m.User.Username.ToLower().Contains(search)) ||
                (m.User != null && m.User.Phone != null && m.User.Phone.Contains(search))
            );
        }

        var members = await query
            .Select(m => new
            {
                m.TeamId,
                m.UserId,
                FullName = m.User != null ? m.User.FullName : null,
                Username = m.User != null ? m.User.Username : null,
                Phone = m.User != null ? m.User.Phone : null,
                m.MemberRole,
                m.JoinedAt,
                m.RequestId,
                IsBusy = m.RequestId != null,
                CurrentOperationId = m.RequestId == null
                    ? (int?)null
                    : _context.RescueOperations
                        .Where(o => o.RequestId == m.RequestId && o.TeamId == m.TeamId)
                        .Select(o => (int?)o.OperationId)
                        .FirstOrDefault(),
                LastActionType = _context.RescueDelegationActionLogs
                    .Where(l => l.MemberUserId == m.UserId)
                    .OrderByDescending(l => l.ActionAt)
                    .Select(l => l.ActionType)
                    .FirstOrDefault(),
                LastAssignedAt = _context.RescueDelegationActionLogs
                    .Where(l => l.MemberUserId == m.UserId && l.ActionType == ActionLeaderAssignedMember)
                    .OrderByDescending(l => l.ActionAt)
                    .Select(l => (DateTime?)l.ActionAt)
                    .FirstOrDefault(),
                LastSupportRequestedAt = _context.RescueDelegationActionLogs
                    .Where(l => l.MemberUserId == m.UserId && l.ActionType == ActionMemberRequestedSupport)
                    .OrderByDescending(l => l.ActionAt)
                    .Select(l => (DateTime?)l.ActionAt)
                    .FirstOrDefault(),
                LastCompletedAt = _context.RescueDelegationActionLogs
                    .Where(l => l.MemberUserId == m.UserId && l.ActionType == ActionMemberCompleted)
                    .OrderByDescending(l => l.ActionAt)
                    .Select(l => (DateTime?)l.ActionAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = members.Count, Data = members });
    }

    /// <summary>
    /// Quản lý (COORDINATOR, ADMIN, MANAGER) - Lấy danh sách thông tin và vị trí của các đội cứu hộ trên bản đồ. 
    /// Hỗ trợ lọc theo trạng thái (AVAILABLE, BUSY...).
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetTeamsWithStatus([FromQuery] string? status = null)
    {
        // Bỏ qua lọc theo status (vì logic quét chéo sang operation đã bị hủy bỏ theo yêu cầu)
        var query = _context.RescueTeams.AsQueryable();

        var teams = await query
            .OrderBy(t => t.TeamName)
            .Select(t => new
            {
                t.TeamId,
                t.TeamName,
                Status = "AVAILABLE", // Mặc định trả về rảnh rỗi như một placeholder
                t.BaseLatitude,
                t.BaseLongitude,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Count = teams.Count, Data = teams });
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Thêm mới hoặc cập nhật bản ghi lịch sử trạng thái của yêu cầu cứu hộ nhằm tránh ghi đúp (duplicate conflict).
    /// </summary>
    private async Task UpsertRequestStatusHistoryAsync(
        int requestId,
        string status,
        string notes,
        int updatedBy,
        DateTime updatedAt)
    {
        var existingHistory = await _context.RescueRequestStatusHistories
            .FirstOrDefaultAsync(history => history.RequestId == requestId && history.Status == status);

        if (existingHistory == null)
        {
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = requestId,
                Status = status,
                Notes = notes,
                UpdatedBy = updatedBy,
                UpdatedAt = updatedAt
            });
            return;
        }

        existingHistory.Notes = notes;
        existingHistory.UpdatedBy = updatedBy;
        existingHistory.UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Kiểm tra lỗi khi ghi lịch sử trạng thái bị trùng lặp thời gian hoặc record (bắt DbUpdateException).
    /// </summary>
    private static bool IsDuplicateRequestStatusHistoryError(DbUpdateException exception)
    {
        return exception.InnerException?.Message?.Contains("UX_rrsh_request_status", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsUniqueActiveAssignmentViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               && message.Contains("rescue_team_members", StringComparison.OrdinalIgnoreCase);
    }

    private void AddDelegationActionLog(
        Guid actionBatchId,
        int? requestId,
        int? operationId,
        int actorUserId,
        int? memberUserId,
        string actionType,
        string? actionReason,
        string requestStatus,
        string? operationStatus,
        DateTime actionAt)
    {
        _context.RescueDelegationActionLogs.Add(new RescueDelegationActionLog
        {
            ActionBatchId = actionBatchId,
            RequestId = requestId,
            OperationId = operationId,
            ActorUserId = actorUserId,
            MemberUserId = memberUserId,
            ActionType = actionType,
            ActionReason = actionReason,
            RequestStatus = requestStatus,
            OperationStatus = operationStatus,
            ActionAt = actionAt
        });
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Chuẩn hóa chuỗi trạng thái (in hoa, thay khoảng trắng thành gạch dưới) để so sánh.
    /// </summary>
    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }
}
