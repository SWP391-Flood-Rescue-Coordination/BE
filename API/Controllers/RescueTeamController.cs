using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// API cho Rescue Team cập nhật trạng thái thực hiện nhiệm vụ.
/// Chỉ dành cho vai trò RESCUE_TEAM.
/// </summary>
[ApiController]
[Route("api/rescue-team")]
[Authorize(Roles = "RESCUE_TEAM")]
public class RescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cập nhật trạng thái nhiệm vụ (assignment) của đội cứu hộ.
    /// 
    /// Trạng thái hợp lệ:
    ///   - "IN_PROGRESS"  : Đội đã bắt đầu thực hiện (từ ASSIGNED → IN_PROGRESS)
    ///   - "COMPLETED"    : Đội đã hoàn thành (từ IN_PROGRESS → COMPLETED)
    /// 
    /// Kèm theo đó rescue_request cũng được cập nhật status tương ứng.
    /// Nếu COMPLETED, team và vehicle sẽ được trả về trạng thái AVAILABLE.
    /// 
    /// Concurrency: Client phải gửi 'expectedCurrentStatus' khớp với DB.
    /// Nếu không khớp → 409 Conflict.
    /// </summary>
    [HttpPut("assignments/{assignmentId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int assignmentId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        // ── 1. Lấy userId từ JWT token ────────────────────────────────────────
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        // ── 2. Validate newStatus ─────────────────────────────────────────────
        var allowedStatuses = new[] { "IN_PROGRESS", "COMPLETED" };
        if (!allowedStatuses.Contains(dto.NewStatus))
            return BadRequest(new
            {
                Success = false,
                Message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedStatuses)}"
            });

        // ── 3. Tải assignment + request ───────────────────────────────────────
        var assignment = await _context.RescueAssignments
            .Include(a => a.Request)
            .Include(a => a.Team)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

        if (assignment == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });

        var request = assignment.Request;
        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ liên kết." });

        // ── 4. Kiểm tra user là thành viên active của team ────────────────────
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == assignment.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
            return Forbid(); // 403

        // ── 5. Kiểm tra rescue_request.status hợp lệ ─────────────────────────
        var validRequestStatuses = new[] { "ASSIGNED", "IN_PROGRESS" };
        if (!validRequestStatuses.Contains(request.Status))
            return BadRequest(new
            {
                Success = false,
                Message = $"Yêu cầu cứu hộ phải ở trạng thái ASSIGNED hoặc IN_PROGRESS. Hiện tại: {request.Status}"
            });

        // ── 6. Kiểm tra rescue_assignment.status hợp lệ theo đích đến ────────
        var validTransitions = new Dictionary<string, string>
        {
            { "IN_PROGRESS", "ASSIGNED" },    // ASSIGNED → IN_PROGRESS
            { "COMPLETED",   "EN_ROUTE" }     // EN_ROUTE/ARRIVED → COMPLETED (dùng trạng thái cuối)
        };

        // Từ bất kỳ trạng thái nào (ASSIGNED, EN_ROUTE, ARRIVED) đều có thể chuyển sang COMPLETED
        var allowedAssignmentFrom = dto.NewStatus == "IN_PROGRESS"
            ? new[] { "ASSIGNED" }
            : new[] { "ASSIGNED", "EN_ROUTE", "ARRIVED", "IN_PROGRESS" };

        if (!allowedAssignmentFrom.Contains(assignment.Status))
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{assignment.Status}', không thể chuyển sang '{dto.NewStatus}'."
            });

        // ── 7. Concurrency check: so sánh expectedCurrentStatus với DB ────────
        if (!string.IsNullOrEmpty(dto.ExpectedCurrentStatus)
            && assignment.Status != dto.ExpectedCurrentStatus)
        {
            return Conflict(new
            {
                Success = false,
                Message = $"Xung đột trạng thái (Concurrency): Trạng thái hiện tại là '{assignment.Status}', bạn đang kỳ vọng '{dto.ExpectedCurrentStatus}'. Vui lòng tải lại và thử lại.",
                CurrentStatus = assignment.Status
            });
        }

        var now = DateTime.UtcNow;

        // ── 8. Cập nhật rescue_assignments ───────────────────────────────────
        string oldAssignmentStatus = assignment.Status;
        assignment.Status = dto.NewStatus == "IN_PROGRESS" ? "EN_ROUTE" : "COMPLETED";
        assignment.Notes = dto.Notes ?? assignment.Notes;

        if (dto.NewStatus == "IN_PROGRESS")
        {
            assignment.StartedAt = now;
        }
        else // COMPLETED
        {
            assignment.CompletedAt = now;
            if (assignment.StartedAt == null)
                assignment.StartedAt = now; // phòng trường hợp bỏ qua bước IN_PROGRESS
        }

        // ── 9. Cập nhật rescue_requests.status ───────────────────────────────
        string oldRequestStatus = request.Status;
        request.Status = dto.NewStatus; // "IN_PROGRESS" hoặc "COMPLETED"
        request.UpdatedAt = now;

        // ── 10. Ghi lịch sử vào rescue_request_status_history ────────────────
        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.NewStatus,
            Notes = $"Cập nhật bởi Rescue Team (UserId={userId}). {dto.Notes}".Trim(),
            UpdatedBy = userId,
            UpdatedAt = now
        });

        // ── 11. Nếu COMPLETED: trả team và vehicle về AVAILABLE ───────────────
        if (dto.NewStatus == "COMPLETED")
        {
            // Cập nhật rescue_teams.status
            var team = await _context.RescueTeams.FindAsync(assignment.TeamId);
            if (team != null)
                team.Status = "AVAILABLE";

            // Cập nhật vehicles.status (nếu có vehicle được gán)
            if (assignment.VehicleId.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(assignment.VehicleId.Value);
                if (vehicle != null)
                    vehicle.Status = "AVAILABLE";
            }
        }

        // ── 12. Lưu tất cả trong một transaction ─────────────────────────────
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                Success = false,
                Message = "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại và thử lại."
            });
        }

        return Ok(new MissionStatusResponseDto
        {
            AssignmentId = assignment.AssignmentId,
            RequestId = request.RequestId,
            AssignmentStatus = assignment.Status,
            RequestStatus = request.Status,
            StartedAt = assignment.StartedAt,
            CompletedAt = assignment.CompletedAt,
            Message = dto.NewStatus == "COMPLETED"
                ? "Hoàn thành nhiệm vụ thành công. Đội và phương tiện đã được giải phóng."
                : "Bắt đầu thực hiện nhiệm vụ thành công."
        });
    }

    /// <summary>
    /// Xem danh sách các nhiệm vụ được phân công cho đội của mình.
    /// Chỉ hiển thị nhiệm vụ ASSIGNED hoặc IN_PROGRESS (đang chờ xử lý).
    /// </summary>
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        // Tìm team mà user đang là thành viên active
        var myTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!myTeamIds.Any())
            return Ok(new { Success = true, Message = "Bạn chưa thuộc đội cứu hộ nào.", Data = new List<object>() });

        var assignments = await _context.RescueAssignments
            .Include(a => a.Request)
            .Include(a => a.Team)
            .Include(a => a.Vehicle)
            .Where(a => myTeamIds.Contains(a.TeamId)
                     && (a.Status == "ASSIGNED" || a.Status == "EN_ROUTE" || a.Status == "ARRIVED"))
            .OrderBy(a => a.AssignedAt)
            .Select(a => new
            {
                a.AssignmentId,
                a.RequestId,
                RequestTitle = a.Request != null ? a.Request.Title : null,
                RequestStatus = a.Request != null ? a.Request.Status : null,
                RequestAddress = a.Request != null ? a.Request.Address : null,
                RequestLatitude = a.Request != null ? a.Request.Latitude : (decimal?)null,
                RequestLongitude = a.Request != null ? a.Request.Longitude : (decimal?)null,
                TeamName = a.Team != null ? a.Team.TeamName : null,
                VehicleName = a.Vehicle != null ? a.Vehicle.VehicleName : null,
                a.Status,
                a.AssignedAt,
                a.StartedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = assignments.Count, Data = assignments });
    }
}
