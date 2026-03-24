using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/rescue-team")]
[Authorize(Roles = "RESCUE_TEAM,COORDINATOR,ADMIN")]
public class RescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPut("operations/{operationId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { Success = false, Message = "Du lieu gui len khong hop le." });
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token khong hop le." });
        }

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
                Message = "Trang thai khong hop le. Chi chap nhan: COMPLETED, FAILED."
            });
        }

        if (targetStatus == "Failed" && string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Bat buoc phai nhap ly do khi cap nhat trang thai FAILED."
            });
        }

        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay nhiem vu." });
        }

        var request = operation.Request;
        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay yeu cau cuu ho lien ket." });
        }

        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == operation.TeamId
                        && m.UserId == userId
                        && m.IsActive);
        if (!isMember)
        {
            return Forbid();
        }

        if (operation.Status != "Assigned")
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Nhiem vu dang o trang thai '{operation.Status}', khong the chuyen sang '{targetStatus}'."
            });
        }

        if (NormalizeStatusKey(request.Status) != "ASSIGNED")
        {
            return Conflict(new
            {
                Success = false,
                Message = $"Yeu cau lien ket dang o trang thai '{request.Status}', khong phu hop de doi cuu ho hoan tat nhiem vu."
            });
        }

        var now = DateTime.UtcNow;

        operation.Status = targetStatus;
        operation.StartedAt ??= now;
        operation.CompletedAt = now;

        if (targetStatus == "Failed")
        {
            request.Status = "Verified";
        }

        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        if (targetStatus == "Failed")
        {
            await UpsertRequestStatusHistoryAsync(
                request.RequestId,
                "Verified",
                $"Nhiem vu that bai. Ly do: {dto.Reason}",
                userId,
                now);
        }

        if (operation.Team != null)
        {
            operation.Team.Status = "AVAILABLE";
        }

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

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Success = false, Message = "Du lieu da bi thay doi boi nguoi khac." });
        }
        catch (DbUpdateException ex) when (IsDuplicateRequestStatusHistoryError(ex))
        {
            return Conflict(new
            {
                Success = false,
                Message = "Lich su trang thai cua yeu cau da ton tai cho trang thai nay. Vui long tai lai du lieu va thu lai."
            });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Khong the luu thay doi nhiem vu vao co so du lieu."
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
                ? "Cap nhat nhiem vu that bai thanh cong. Yeu cau da quay lai Verified."
                : "Da ghi nhan doi cuu ho hoan tat. Yeu cau van o Assigned va nguoi dan co the bao an toan de chuyen sang Completed."
        });
    }

    [HttpGet("my-operations")]
    public async Task<IActionResult> GetMyOperations()
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

        if (!myTeamIds.Any())
        {
            return Ok(new { Success = true, Message = "Ban chua thuoc doi cuu ho nao.", Data = new List<object>() });
        }

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
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" :
                                                    o.Request.PriorityLevelId == 2 ? "TRUNG BINH" :
                                                    o.Request.PriorityLevelId == 3 ? "THAP" : "THONG THUONG") : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
                o.AssignedAt,
                o.StartedAt,
                o.CompletedAt,
                Vehicles = _context.RescueOperationVehicles
                    .Where(ov => ov.OperationId == o.OperationId)
                    .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (_, v) => v.VehicleName)
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = operations.Count, Data = operations });
    }

    [HttpGet("operations/{operationId:int}")]
    public async Task<IActionResult> GetMissionDetails(int operationId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token khong hop le." });
        }

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
                                                    o.Request.PriorityLevelId == 2 ? "TRUNG BINH" :
                                                    o.Request.PriorityLevelId == 3 ? "THAP" : "THONG THUONG") : null,
                RequestLatitude = o.Request != null ? o.Request.Latitude : (decimal?)null,
                RequestLongitude = o.Request != null ? o.Request.Longitude : (decimal?)null,
                TeamName = o.Team != null ? o.Team.TeamName : null,
                o.Status,
                o.AssignedAt,
                o.StartedAt,
                o.CompletedAt,
                Vehicles = _context.RescueOperationVehicles
                    .Where(ov => ov.OperationId == o.OperationId)
                    .Join(_context.Vehicles, ov => ov.VehicleId, v => v.VehicleId, (_, v) => v.VehicleName)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (operation == null)
        {
            return NotFound(new { Success = false, Message = "Khong tim thay nhiem vu." });
        }

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
            return Forbid();
        }

        return Ok(new { Success = true, Data = operation });
    }

    [HttpGet("status")]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
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
                t.BaseLatitude,
                t.BaseLongitude,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Count = teams.Count, Data = teams });
    }

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

    private static bool IsDuplicateRequestStatusHistoryError(DbUpdateException exception)
    {
        return exception.InnerException?.Message?.Contains("UX_rrsh_request_status", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string NormalizeStatusKey(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim().ToUpperInvariant().Replace(" ", "_");
    }
}
