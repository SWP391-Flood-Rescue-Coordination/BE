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
[ApiController]
[Route("api/rescue-team")]
[Authorize(Roles = "RESCUE_TEAM,COORDINATOR,ADMIN")]
public class RescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo RescueTeamController với DbContext.
    /// </summary>
    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Đội cứu hộ - Cập nhật trạng thái nhiệm vụ cứu hộ (COMPLETED hoặc FAILED).
    /// </summary>
    /// <param name="operationId">ID của nhiệm vụ cần cập nhật.</param>
    /// <param name="dto">Trạng thái mới và lý do (nếu thất bại).</param>
    /// <returns>Kết quả cập nhật trạng thái.</returns>
    [HttpPut("operations/{operationId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
        if (dto == null)
        {
            return BadRequest(new { Success = false, Message = "Dữ liệu gửi lên không hợp lệ." });
        }

        // 2. Xác thực định danh người dùng từ Token
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 3. Chuẩn hóa trạng thái mục tiêu
        var targetStatusKey = dto.NewStatus.Trim().ToUpperInvariant();
        var targetStatus = targetStatusKey == "COMPLETED"
            ? "Completed"
            : targetStatusKey == "FAILED"
                ? "Failed"
                : string.Empty;

        if (string.IsNullOrEmpty(targetStatus))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Trạng thái không hợp lệ. Chỉ chấp nhận: COMPLETED, FAILED."
            });
        }

        // Ràng buộc: Thất bại thì phải có lý do
        if (targetStatus == "Failed" && string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Bắt buộc phải nhập lý do khi cập nhật trạng thái FAILED."
            });
        }

        // 4. Truy vấn thông tin Operation và Request liên quan
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });
        }

        var request = operation.Request;
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ liên kết." });
        }

        // 5. Kiểm tra quyền hạn: User phải là thành viên ĐANG HOẠT ĐỘNG của Team được giao nhiệm vụ này
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
        {
            return Forbid();
        }

        // 6. Kiểm tra logic trạng thái hiện tại
        if (operation.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}', không thể chuyển sang '{targetStatus}'."
            });
        }

        if (NormalizeStatusKey(request.Status) != "ASSIGNED")
        {
            return Conflict(new
            {
                Success = false,
                Message = $"Yêu cầu liên kết đang ở trạng thái '{request.Status}', không phù hợp để đội cứu hộ hoàn tất nhiệm vụ."
            });
        }

        var now = DateTime.UtcNow;

        // 7. Cập nhật trạng thái nhiệm vụ (Operation)
        operation.Status = targetStatus;
        operation.StartedAt ??= now; // Ghi nhận lúc bắt đầu nếu chưa có
        operation.CompletedAt = now;

        // 8. Cập nhật yêu cầu cứu hộ (Request)
        // Nếu thất bại: Trả yêu cầu về trạng thái Verified để Coordinator có thể phân công đội khác
        if (targetStatus == "Failed")
        {
            request.Status = "Verified";
        }
        // Lưu ý: Nếu Thành công (Completed), Request vẫn giữ Assigned cho đến khi người dân xác nhận báo an toàn

        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        // 9. Ghi lịch sử trạng thái yêu cầu nếu thất bại
        if (targetStatus == "Failed")
        {
            await UpsertRequestStatusHistoryAsync(
                request.RequestId,
                "Verified",
                $"Nhiệm vụ thất bại. Lý do: {dto.Reason}",
                userId,
                now);
        }

        // 10. Giải phóng trạng thái cho các thành viên trong đội đang làm nhiệm vụ này
        var membersToRelease = await _context.RescueTeamMembers
            .Where(m => m.RequestId == request.RequestId && m.IsActive)
            .ToListAsync();
            
        foreach (var m in membersToRelease)
        {
            m.RequestId = null;
        }

        // 11. Giải phóng tất cả các phương tiện được gán cho nhiệm vụ này
        var vehicleIds = await _context.RescueOperationVehicles
            .Where(ov => ov.OperationId == operation.OperationId)
            .Select(ov => ov.VehicleId)
            .ToListAsync();

        if (vehicleIds.Any())
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

        // 12. Lưu các thay đổi vào DB với xử lý tranh chấp (Concurrency)
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Success = false, Message = "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại." });
        }
        catch (DbUpdateException ex) when (IsDuplicateRequestStatusHistoryError(ex))
        {
            return Conflict(new
            {
                Success = false,
                Message = "Lịch sử trạng thái của yêu cầu đã tồn tại. Vui lòng tải lại dữ liệu."
            });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Lỗi hệ thống khi lưu dữ liệu nhiệm vụ."
            });
        }

        return Ok(new
        {
            Success = true,
            OperationId = operation.OperationId,
            RequestId = request.RequestId,
            OperationStatus = operation.Status,
            RequestStatus = request.Status,
            StartedAt = operation.StartedAt,
            CompletedAt = operation.CompletedAt,
            Message = targetStatus == "Failed"
                ? "Đã ghi nhận nhiệm vụ thất bại. Yêu cầu đã quay lại trạng thái Chờ xử lý (Verified)."
                : "Xác nhận hoàn tất nhiệm vụ thành công. Chờ người dân báo an toàn để đóng yêu cầu hoàn toàn."
        });
    }

    /// <summary>
    /// Đội trưởng (Leader) - Từ chối Yêu cầu cứu hộ được phân công (Gỡ bỏ gán đội).
    /// </summary>
    [HttpPut("requests/{requestId}/reject")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> RejectRequest(int requestId, [FromQuery] string? reason = null)
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

        // 3. Kiểm tra trạng thái
        if (request.Status != "Verified" && request.Status != "Assigned")
        {
            return BadRequest(new { Success = false, Message = $"Yêu cầu đang ở trạng thái {request.Status}. Không thể từ chối." });
        }

        if (!request.TeamId.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Yêu cầu này chưa được hệ thống điều phối cho đội nào." });
        }

        // 4. Phân quyền: Người dùng phải tham gia Team đó và mang quyền Leader
        var isLeader = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == request.TeamId.Value 
                        && m.UserId == userId 
                        && m.MemberRole == "Leader" 
                        && m.IsActive);
                        
        if (!isLeader)
        {
            return StatusCode(403, new { Success = false, Message = "Bạn không có quyền thực hiện. Chỉ Đội trưởng (Leader) của đội này mới có quyền từ chối." });
        }

        // 5. Giải phóng phương tiện gán cho nhiệm vụ (nếu có)
        var operation = await _context.RescueOperations
            .Where(o => o.RequestId == requestId && o.TeamId == request.TeamId && o.Status == "Assigned")
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;

        if (operation != null)
        {
            // Giải phóng các phương tiện
            var rovList = await _context.RescueOperationVehicles
                .Where(rov => rov.OperationId == operation.OperationId)
                .ToListAsync();

            if (rovList.Any())
            {
                var vIds = rovList.Select(r => r.VehicleId).ToList();
                var vehicles = await _context.Vehicles.Where(v => vIds.Contains(v.VehicleId)).ToListAsync();
                foreach (var v in vehicles)
                {
                    v.Status = "AVAILABLE";
                }
            }
        }

        // 6. Cập nhật trạng thái yêu cầu (Quay về Verified và xóa TeamId)
        request.Status = "Verified";
        request.TeamId = null;
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        await UpsertRequestStatusHistoryAsync(
            requestId,
            "Verified",
            $"Đội trưởng từ chối tiếp nhận. Lý do: {reason ?? "Không có lý do cụ thể"}",
            userId,
            now);

        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Đã từ chối và giải phóng yêu cầu cứu hộ thành công" });
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
        // 1. Xác thực Leader
        var leaderIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(leaderIdStr, out var leaderId)) return Unauthorized();

        if (dto.UserIds == null || !dto.UserIds.Any())
            return BadRequest(new { Success = false, Message = "Danh sách thành viên (userIds) không được để trống." });

        // 2. Tìm yêu cầu cứu hộ
        var request = await _context.RescueRequests.FindAsync(dto.RequestId);
        if (request == null) 
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });

        if (!request.TeamId.HasValue)
            return BadRequest(new { Success = false, Message = "Yêu cầu này chưa được hệ thống điều phối cho đội nào." });

        // 3. Phân quyền: Kiểm tra Leader này có đang quản lý Team được gán cho Request này không
        var leaderMember = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId.Value 
                                   && m.UserId == leaderId 
                                   && m.MemberRole == "Leader" 
                                   && m.IsActive);

        if (leaderMember == null) return Forbid();

        var teamId = request.TeamId.Value;

        // 4. Lấy toàn bộ thành viên trong đội khớp với danh sách UserIds
        var teamMembers = await _context.RescueTeamMembers
            .Where(m => m.TeamId == teamId && dto.UserIds.Contains(m.UserId) && m.IsActive)
            .ToListAsync();

        var assignedIds = new List<int>();
        var skippedIds = new List<int>();

        // 5. Duyệt danh sách, gán RequestId nếu thành viên rảnh
        foreach (var uid in dto.UserIds)
        {
            var member = teamMembers.FirstOrDefault(m => m.UserId == uid);

            if (member == null || member.RequestId.HasValue)
            {
                skippedIds.Add(uid);
                continue;
            }

            member.RequestId = dto.RequestId;
            assignedIds.Add(uid);
        }

        if (!assignedIds.Any())
            return BadRequest(new { 
                Success = false, 
                Message = "Không có thành viên nào có thể được giao việc. Tất cả đang bận hoặc không thuộc đội.", 
                SkippedUserIds = skippedIds 
            });

        // 6. ĐẢM BẢO CÓ RESCUE OPERATION (Tạo mới nếu chưa có)
        var operation = await _context.RescueOperations
            .FirstOrDefaultAsync(o => o.RequestId == dto.RequestId && o.TeamId == teamId);

        var now = DateTime.UtcNow;
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
        }
        else
        {
            // Nếu đã có record, đảm bảo trạng thái là Assigned
            operation.Status = "Assigned";
        }

        // 7. Cập nhật trạng thái Request sang Assigned và ghi History
        if (request.Status != "Assigned")
        {
            request.Status = "Assigned";
            request.UpdatedAt = now;
            request.UpdatedBy = leaderId;

            await UpsertRequestStatusHistoryAsync(
                dto.RequestId,
                "Assigned",
                $"Đội trưởng phân công nhiệm vụ cho {assignedIds.Count} thành viên.",
                leaderId,
                now);
        }

        await _context.SaveChangesAsync();

        return Ok(new MemberAssignmentResponseDto
        {
            TeamId = teamId,
            OperationId = operation.OperationId,
            AssignedUserIds = assignedIds,
            SkippedUserIds = skippedIds,
            Message = $"Đã giao nhiệm vụ cho {assignedIds.Count} thành viên. Nhiệm vụ (Operation) đã được thiết lập sang 'Assigned'."
        });
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
                RequestPhone = o.Request != null ? o.Request.Phone : null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
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

        // 2. Tìm bản ghi thành viên đang được giao nhiệm vụ (RequestId != null)
        var member = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.RequestId != null && m.IsActive);

        if (member == null || member.RequestId == null)
            return NotFound(new { Success = false, Message = "Bạn hiện đang rảnh. Không có nhiệm vụ nào cần xác nhận." });

        var requestId = member.RequestId.Value;
        var teamId = member.TeamId;

        // 3. Tìm Operation liên quan để trả về thông tin phản hồi
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Where(o => o.RequestId == requestId && o.TeamId == teamId)
            .FirstOrDefaultAsync();

        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy dữ liệu nhiệm vụ liên kết. Vui lòng liên hệ Đội trưởng." });

        // 4. Kiểm tra Operation đang ở trạng thái hợp lệ để xác nhận
        if (operation.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}'. Chỉ có thể xác nhận khi trạng thái là 'Assigned'."
            });
        }

        // 5. Giải phóng thành viên: Đặt RequestId = null → trạng thái Rảnh
        member.RequestId = null;

        // 6. Lưu thay đổi
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Success = false, Message = "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại sau." });
        }

        return Ok(new
        {
            Success = true,
            UserId = userId,
            OperationId = operation.OperationId,
            RequestId = requestId,
            Message = "Xác nhận hoàn tất nhiệm vụ thành công."
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
                IsBusy = m.RequestId != null // Thuộc tính phụ trợ cho frontend dễ hiển thị rảnh/bận
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
