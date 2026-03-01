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
    /// Cập nhật trạng thái nhiệm vụ (operation) của đội cứu hộ.
    /// 
    /// Trạng thái hợp lệ (Operation Status):
    ///   - "In Progress"  : Đội đã bắt đầu thực hiện (từ Assigned → In Progress)
    ///   - "Completed"    : Đội đã hoàn thành (từ In Progress → Completed)
    /// 
    /// Kèm theo đó rescue_request cũng được cập nhật status tương ứng.
    /// Nếu Completed, team và vehicle sẽ được trả về trạng thái AVAILABLE/Available.
    /// </summary>
    [HttpPut("operations/{operationId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        // ── 1. Lấy userId từ JWT token ────────────────────────────────────────
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        // ── 2. Validate newStatus ─────────────────────────────────────────────
        var allowedStatuses = new[] { "In Progress", "Completed" };
        if (!allowedStatuses.Contains(dto.NewStatus))
            return BadRequest(new
            {
                Success = false,
                Message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedStatuses)}"
            });

        // ── 3. Tải operation + request ───────────────────────────────────────
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });

        var request = operation.Request;
        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ liên kết." });

        // ── 4. Kiểm tra user là thành viên active của team ────────────────────
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
            return Forbid(); // 403

        // ── 5. Kiểm tra rescue_operation.status hợp lệ ────────────────────────
        var allowedFrom = dto.NewStatus == "In Progress"
            ? new[] { "Assigned" }
            : new[] { "Assigned", "In Progress" };

        if (!allowedFrom.Contains(operation.Status))
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}', không thể chuyển sang '{dto.NewStatus}'."
            });

        var now = DateTime.UtcNow;

        // ── 7. Cập nhật rescue_operations ───────────────────────────────────
        operation.Status = dto.NewStatus;
        if (dto.NewStatus == "In Progress")
        {
            operation.StartedAt = now;
        }
        else // Completed
        {
            operation.CompletedAt = now;
            if (operation.StartedAt == null)
                operation.StartedAt = now;
        }

        // ── 8. Cập nhật rescue_requests.status ───────────────────────────────
        request.Status = dto.NewStatus; // "In Progress" hoặc "Completed"
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = dto.NewStatus,
            Notes = $"Đội cứu hộ cập nhật trạng thái nhiệm vụ sang {dto.NewStatus}",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        // ── 10. Nếu Completed: trả team và vehicle về AVAILABLE/Available ────
        if (dto.NewStatus == "Completed")
        {
            var team = operation.Team;
            if (team != null)
                team.Status = "Available";

            // Cập nhật tất cả vehicles trong operation này về Available
            var operationVehicles = await _context.RescueOperationVehicles
                .Where(ov => ov.OperationId == operationId)
                .Select(ov => ov.VehicleId)
                .ToListAsync();

            if (operationVehicles.Any())
            {
                var vehicles = await _context.Vehicles
                    .Where(v => operationVehicles.Contains(v.VehicleId))
                    .ToListAsync();
                foreach (var v in vehicles)
                {
                    v.Status = "Available";
                    v.UpdatedAt = now;
                }
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Success = false, Message = "Dữ liệu đã bị thay đổi bởi người khác." });
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
            Message = dto.NewStatus == "Completed"
                ? "Hoàn thành nhiệm vụ thành công."
                : "Bắt đầu thực hiện nhiệm vụ thành công."
        });
    }

    /// <summary>
    /// Xem danh sách các nhiệm vụ được phân công cho đội của mình.
    /// Chỉ hiển thị nhiệm vụ Assigned hoặc In Progress.
    /// </summary>
    [HttpGet("my-operations")]
    public async Task<IActionResult> GetMyOperations()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        // Tìm team(s) mà user đang là thành viên active
        var myTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!myTeamIds.Any())
            return Ok(new { Success = true, Message = "Bạn chưa thuộc đội cứu hộ nào.", Data = new List<object>() });

        var operations = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .Where(o => myTeamIds.Contains(o.TeamId)
                     && (o.Status == "Assigned" || o.Status == "In Progress"))
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
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" :
                                                    o.Request.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                                    o.Request.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG") : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
                o.AssignedAt,
                o.StartedAt,
                Vehicles = _context.RescueOperationVehicles
                            .Where(ov => ov.OperationId == o.OperationId)
                            .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (ov, v) => v.VehicleName)
                            .ToList()
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = operations.Count, Data = operations });
    }

    /// <summary>
    /// Xem chi tiết một nhiệm vụ (operation) theo ID.
    /// </summary>
    [HttpGet("operations/{operationId:int}")]
    public async Task<IActionResult> GetMissionDetails(int operationId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

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
                Vehicles = _context.RescueOperationVehicles
                            .Where(ov => ov.OperationId == o.OperationId)
                            .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (ov, v) => v.VehicleName)
                            .ToList()
            })
            .FirstOrDefaultAsync();

        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });

        // Kiểm tra quyền xem: Phải thuộc team đang được gán nhiệm vụ
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == (int)_context.RescueOperations
                                        .Where(ro => ro.OperationId == operationId)
                                        .Select(ro => ro.TeamId).FirstOrDefault()
                        && m.UserId == userId
                        && m.IsActive);

        if (!isMember)
            return Forbid();

        return Ok(new { Success = true, Data = operation });
    }
}
