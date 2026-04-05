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

        // 10. Giải phóng trạng thái cho Đội cứu hộ (trạng thái được tính toán động)

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
