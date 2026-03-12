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
[Authorize]
public class RescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách đội cứu hộ kèm trạng thái (Admin/Coordinator/Manager)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,COORDINATOR,MANAGER")]
    public async Task<IActionResult> GetTeamsWithStatus([FromQuery] string? status = null)
    {
        var query = _context.RescueTeams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var teams = await query
            .OrderBy(t => t.TeamName)
            .Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.Status,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Count = teams.Count, Data = teams });
    }

    /// <summary>
    /// Cập nhật trạng thái nhiệm vụ (operation) của đội cứu hộ.
    /// </summary>
    [HttpPut("operations/{operationId}/status")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

        string targetStatus = dto.NewStatus;
        if (targetStatus.Equals("FAILED", StringComparison.OrdinalIgnoreCase)) targetStatus = "Failed";

        var allowedStatuses = new[] { "In Progress", "Completed", "Failed" };
        if (!allowedStatuses.Contains(targetStatus))
            return BadRequest(new
            {
                Success = false,
                Message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedStatuses)}"
            });

        if (targetStatus == "Failed" && string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { Success = false, Message = "Bắt buộc phải nhập lý do khi cập nhật trạng thái FAILED." });
        }

        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });

        var request = operation.Request;
        if (request == null)
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ liên kết." });

        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
            return Forbid();

        var allowedFrom = targetStatus == "In Progress"
            ? new[] { "Assigned" }
            : (targetStatus == "Failed" 
                ? new[] { "Assigned", "In Progress" } 
                : new[] { "In Progress" });

        if (!allowedFrom.Contains(operation.Status))
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiệm vụ đang ở trạng thái '{operation.Status}', không thể chuyển sang '{targetStatus}'."
            });

        var now = DateTime.UtcNow;

        operation.Status = targetStatus;
        if (targetStatus == "In Progress")
        {
            operation.StartedAt = now;
        }
        else
        {
            operation.CompletedAt = now;
            if (operation.StartedAt == null)
                operation.StartedAt = now;
        }

        if (targetStatus == "Failed")
        {
            request.Status = "Verified";
        }
        else
        {
            request.Status = targetStatus;
        }
        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = targetStatus,
            Notes = targetStatus == "Failed" 
                ? $"Nhiệm vụ thất bại. Lý do: {dto.Reason}" 
                : $"Đội cứu hộ cập nhật trạng thái nhiệm vụ sang {targetStatus}",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        if (targetStatus == "Completed" || targetStatus == "Failed")
        {
            var team = operation.Team;
            if (team != null)
                team.Status = "AVAILABLE";

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

        await _context.SaveChangesAsync();
        return Ok(new { Success = true, Message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// Xem danh sách các nhiệm vụ được phân công cho đội của mình.
    /// </summary>
    [HttpGet("my-operations")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetMyOperations()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });

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
    [Authorize(Roles = "RESCUE_TEAM")]
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

        return Ok(new { Success = true, Data = operation });
    }
}
