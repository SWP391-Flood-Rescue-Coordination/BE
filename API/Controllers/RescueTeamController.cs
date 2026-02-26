using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

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
    /// Compatible endpoint from Tuan branch:
    /// PUT /api/rescue-team/assignments/{assignmentId}/status
    /// assignmentId is mapped to rescue_operations.operation_id.
    /// </summary>
    [HttpPut("assignments/{assignmentId:int}/status")]
    public async Task<IActionResult> UpdateMissionStatus(int assignmentId, [FromBody] UpdateMissionStatusDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token khong hop le." });
        }

        var newStatus = (dto.NewStatus ?? string.Empty).Trim().ToUpperInvariant();
        if (newStatus != "IN_PROGRESS" && newStatus != "COMPLETED")
        {
            return BadRequest(new { Success = false, Message = "Trang thai khong hop le. Chi chap nhan IN_PROGRESS hoac COMPLETED." });
        }

        var operation = await _context.RescueOperations.FirstOrDefaultAsync(o => o.OperationId == assignmentId);
        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay nhiem vu." });
        }

        var request = await _context.RescueRequests.FirstOrDefaultAsync(r => r.RequestId == operation.RequestId);
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho lien ket." });
        }

        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId && m.UserId == userId && m.IsActive);
        if (!isMember)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(dto.ExpectedCurrentStatus))
        {
            var expected = NormalizeStatus(dto.ExpectedCurrentStatus);
            var current = NormalizeStatus(operation.Status);
            if (expected != current)
            {
                return Conflict(new
                {
                    Success = false,
                    Message = $"Xung dot trang thai. Hien tai la '{operation.Status}', FE dang ky vong '{dto.ExpectedCurrentStatus}'.",
                    CurrentStatus = operation.Status
                });
            }
        }

        var currentNormalized = NormalizeStatus(operation.Status);
        if (newStatus == "IN_PROGRESS" && currentNormalized != "ASSIGNED")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiem vu dang o trang thai '{operation.Status}', khong the chuyen sang IN_PROGRESS."
            });
        }

        if (newStatus == "COMPLETED" && currentNormalized != "ASSIGNED" && currentNormalized != "IN_PROGRESS")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiem vu dang o trang thai '{operation.Status}', khong the chuyen sang COMPLETED."
            });
        }

        var now = DateTime.UtcNow;
        if (newStatus == "IN_PROGRESS")
        {
            operation.Status = "In Progress";
            operation.StartedAt ??= now;
            request.Status = "In Progress";
        }
        else
        {
            operation.Status = "Completed";
            operation.StartedAt ??= now;
            operation.CompletedAt = now;
            request.Status = "Completed";
        }

        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
        {
            RequestId = request.RequestId,
            Status = request.Status,
            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? $"Updated by Rescue Team (UserId={userId})"
                : $"Updated by Rescue Team (UserId={userId}): {dto.Notes}",
            UpdatedBy = userId,
            UpdatedAt = now
        });

        if (newStatus == "COMPLETED")
        {
            var team = await _context.RescueTeams.FindAsync(operation.TeamId);
            if (team != null)
            {
                team.Status = "AVAILABLE";
            }

            var vehicleIds = await _context.RescueOperationVehicles
                .Where(v => v.OperationId == operation.OperationId)
                .Select(v => v.VehicleId)
                .ToListAsync();

            if (vehicleIds.Count > 0)
            {
                var vehicles = await _context.Vehicles
                    .Where(v => vehicleIds.Contains(v.VehicleId))
                    .ToListAsync();

                foreach (var vehicle in vehicles)
                {
                    vehicle.Status = "Available";
                    vehicle.UpdatedAt = now;
                }
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Success = false, Message = "Du lieu da thay doi boi nguoi khac. Vui long tai lai va thu lai." });
        }

        return Ok(new MissionStatusResponseDto
        {
            AssignmentId = operation.OperationId,
            RequestId = request.RequestId,
            AssignmentStatus = operation.Status,
            RequestStatus = request.Status,
            StartedAt = operation.StartedAt,
            CompletedAt = operation.CompletedAt,
            Message = newStatus == "COMPLETED"
                ? "Hoan thanh nhiem vu thanh cong. Doi va phuong tien da duoc giai phong."
                : "Bat dau thuc hien nhiem vu thanh cong."
        });
    }

    /// <summary>
    /// Compatible endpoint from Tuan branch:
    /// GET /api/rescue-team/my-assignments
    /// </summary>
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token khong hop le." });
        }

        var myTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (myTeamIds.Count == 0)
        {
            return Ok(new { Success = true, Message = "Ban chua thuoc doi cuu ho nao.", Data = new List<object>() });
        }

        var assignments = await _context.RescueOperations
            .Where(o => myTeamIds.Contains(o.TeamId)
                        && (o.Status == "Assigned" || o.Status == "In Progress" || o.Status == "ASSIGNED" || o.Status == "IN_PROGRESS"))
            .OrderBy(o => o.AssignedAt)
            .Select(o => new
            {
                AssignmentId = o.OperationId,
                RequestId = o.RequestId,
                RequestTitle = _context.RescueRequests.Where(r => r.RequestId == o.RequestId).Select(r => r.Title).FirstOrDefault(),
                RequestStatus = _context.RescueRequests.Where(r => r.RequestId == o.RequestId).Select(r => r.Status).FirstOrDefault(),
                RequestAddress = _context.RescueRequests.Where(r => r.RequestId == o.RequestId).Select(r => r.Address).FirstOrDefault(),
                RequestLatitude = _context.RescueRequests.Where(r => r.RequestId == o.RequestId).Select(r => r.Latitude).FirstOrDefault(),
                RequestLongitude = _context.RescueRequests.Where(r => r.RequestId == o.RequestId).Select(r => r.Longitude).FirstOrDefault(),
                TeamName = _context.RescueTeams.Where(t => t.TeamId == o.TeamId).Select(t => t.TeamName).FirstOrDefault(),
                VehicleIds = _context.RescueOperationVehicles.Where(v => v.OperationId == o.OperationId).Select(v => v.VehicleId).ToList(),
                Status = o.Status,
                o.AssignedAt,
                o.StartedAt,
                o.CompletedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = assignments.Count, Data = assignments });
    }

    private static string NormalizeStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "_");
    }
}
