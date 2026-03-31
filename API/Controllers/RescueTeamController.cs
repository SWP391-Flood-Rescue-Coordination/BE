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

    /// <summary>
    /// Constructor khởi tạo RescueTeamController với DbContext.
    /// </summary>
    public RescueTeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API dành cho Đội cứu hộ: Thay đổi kết quả của một nhiệm vụ (Operation) được giao.
    /// Cho phép chuyển trạng thái sang COMPLETED hoặc FAILED (kèm lý do).
    /// Quy trình: Cập nhật trạng thái chiến dịch, giải phóng Đội và Phương tiện về trạng thái Available.
    /// Nếu thất bại, trả Request về trạng thái Verified.
    /// </summary>
    /// <param name="operationId">ID của nhiệm vụ cần cập nhật.</param>
    /// <param name="dto">Dữ liệu trạng thái mới và lý do (nếu có).</param>
    [HttpPut("operations/{operationId}/status")]
    public async Task<IActionResult> UpdateMissionStatus(
        int operationId,
        [FromBody] UpdateMissionStatusDto dto)
    {
        // 1. Kiểm tra đầu vào và định danh người dùng
        if (dto == null) return BadRequest(new { Success = false, Message = "Dữ liệu không hợp lệ." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        // 2. Chuyển đổi và kiểm tra mã trạng thái đích (Completed / Failed)
        var targetStatusKey = dto.NewStatus.Trim().ToUpperInvariant();
        var targetStatus = targetStatusKey == "COMPLETED" ? "Completed" : targetStatusKey == "FAILED" ? "Failed" : string.Empty;

        if (string.IsNullOrEmpty(targetStatus)) return BadRequest(new { Success = false, Message = "Trạng thái không hợp lệ." });
        if (targetStatus == "Failed" && string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest(new { Success = false, Message = "Phải nhập lý do thất bại." });

        // 3. Truy vấn thông tin chiến dịch kèm yêu cầu cứu hộ liên quan
        var operation = await _context.RescueOperations
            .Include(o => o.Request)
            .Include(o => o.Team)
            .FirstOrDefaultAsync(o => o.OperationId == operationId);

        if (operation == null) return NotFound(new { Success = false, Message = "Không tìm thấy nhiệm vụ." });

        var request = operation.Request;
        if (request == null) return NotFound(new { Success = false, Message = "Yêu cầu cứu hộ bị mất liên kết." });

        // 4. Kiểm tra quyền hạn: User phải là thành viên Active của đội đang thực hiện nhiệm vụ này
        var isMember = await _context.RescueTeamMembers.AnyAsync(m => m.TeamId == operation.TeamId && m.UserId == userId && m.IsActive);
        if (!isMember) return Forbid();

        // 5. Kiểm tra logic trạng thái hiện tại (Chỉ cho phép cập nhật khi đang Assigned)
        if (operation.Status != "Assigned") return BadRequest(new { Success = false, Message = "Nhiệm vụ không ở trạng thái có thể cập nhật." });

        var now = DateTime.UtcNow;

        // 6. Thực hiện cập nhật trạng thái
        operation.Status = targetStatus;
        operation.StartedAt ??= now;
        operation.CompletedAt = now;

        if (targetStatus == "Failed")
        {
            // Nếu thất bại, trả về Verified để Coordinator có thể điều phối lại cho đội khác
            request.Status = "Verified";
            await UpsertRequestStatusHistoryAsync(request.RequestId, "Verified", $"Nhiệm vụ thất bại: {dto.Reason}", userId, now);
        }

        request.UpdatedAt = now;
        request.UpdatedBy = userId;

        // 7. Giải phóng Đội cứu hộ sang trạng thái Sẵn sàng (Available)
        if (operation.Team != null) operation.Team.Status = "AVAILABLE";

        // 8. Giải phóng toàn bộ Phương tiện liên quan sang trạng thái Sẵn sàng (Available)
        var vehicleIds = await _context.RescueOperationVehicles
            .Where(ov => ov.OperationId == operation.OperationId)
            .Select(ov => ov.VehicleId)
            .ToListAsync();

        if (vehicleIds.Any())
        {
            var vehicles = await _context.Vehicles.Where(v => vehicleIds.Contains(v.VehicleId)).ToListAsync();
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
            return Conflict(new { Success = false, Message = "Dữ liệu đã bị thay đổi bởi người khác." });
        }
        catch (DbUpdateException ex) when (IsDuplicateRequestStatusHistoryError(ex))
        {
            return Conflict(new
            {
                Success = false,
                Message = "Lịch sử trạng thái của yêu cầu đã tồn tại cho trạng thái này. Vui lòng tải lại dữ liệu và thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể lưu thay đổi nhiệm vụ vào cơ sở dữ liệu."
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
                ? "Cập nhật nhiệm vụ thất bại thành công. Yêu cầu đã quay lại Verified."
                : "Đã ghi nhận đội cứu hộ hoàn tất. Yêu cầu vẫn ở Assigned và người dân có thể báo an toàn để chuyển sang Completed."
        });
    }

    /// <summary>
    /// API dành cho Đội cứu hộ: Lấy danh sách toàn bộ các nhiệm vụ đang được giao (Assigned) cho đội mà User hiện hành tham gia.
    /// Trả về kèm theo thông tin chi tiết về người gặp nạn và phương tiện được cấp.
    /// </summary>
    [HttpGet("my-operations")]
    public async Task<IActionResult> GetMyOperations()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // Lấy danh sách ID các đội mà user đang tham gia
        var myTeamIds = await _context.RescueTeamMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.TeamId)
            .ToListAsync();

        if (!myTeamIds.Any())
        {
            return Ok(new { Success = true, Message = "Bạn chưa thuộc đội cứu hộ nào.", Data = new List<object>() });
        }

        // Lấy các Operation đang Assigned
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
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" : o.Request.PriorityLevelId == 2 ? "TRUNG BÌNH" : "THẤP") : null,
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
            .ToListAsync();

        return Ok(new { Success = true, Total = operations.Count, Data = operations });
    }

    /// <summary>
    /// API cho Đội cứu hộ: Xem chi tiết một nhiệm vụ cứu hộ cụ thể dựa trên ID.
    /// Kiểm tra quyền: Chỉ cho phép xem nếu nhiệm vụ đó thuộc về đội của user.
    /// </summary>
    [HttpGet("operations/{operationId:int}")]
    public async Task<IActionResult> GetMissionDetails(int operationId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

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
                PriorityName = o.Request != null ? (o.Request.PriorityLevelId == 1 ? "CAO" : o.Request.PriorityLevelId == 2 ? "TRUNG BÌNH" : "THẤP") : null,
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

        if (operation == null) return NotFound(new { Success = false, Message = "Nhiệm vụ không tồn tại." });

        // Kiểm tra quyền truy cập (thành viên đội)
        var teamId = await _context.RescueOperations.Where(ro => ro.OperationId == operationId).Select(ro => ro.TeamId).FirstOrDefaultAsync();
        var isMember = await _context.RescueTeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.IsActive);
        if (!isMember) return Forbid();

        return Ok(new { Success = true, Data = operation });
    }

    /// <summary>
    /// API Quản lý (Coordinator / Admin): Lấy danh sách các đội cứu hộ kèm trạng thái và vị trí trụ sở.
    /// Hỗ trợ lọc theo trạng thái (AVAILABLE, BUSY...).
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "COORDINATOR,ADMIN,MANAGER")]
    public async Task<IActionResult> GetTeamsWithStatus([FromQuery] string? status = null)
    {
        var query = _context.RescueTeams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);

        var teams = await query
            .OrderBy(t => t.TeamName)
            .Select(t => new { t.TeamId, t.TeamName, t.Status, t.BaseLatitude, t.BaseLongitude, t.CreatedAt })
            .ToListAsync();

        return Ok(new { Success = true, Count = teams.Count, Data = teams });
    }

    /// <summary>
    /// Hàm hỗ trợ: Cập nhật hoặc thêm mới lịch sử trạng thái của yêu cầu cứu hộ.
    /// </summary>
    private async Task UpsertRequestStatusHistoryAsync(int requestId, string status, string notes, int updatedBy, DateTime updatedAt)
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
