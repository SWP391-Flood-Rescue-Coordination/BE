using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RescueOperationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistanceService _distanceService;

    public RescueOperationController(ApplicationDbContext context, IDistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }

    /// <summary>
    /// COORDINATOR - Phân công đội cứu hộ và phương tiện cho một rescue request.
    /// rescue_requests.status phải = Verified, rescue_teams.status phải = Available,
    /// vehicles.status (nếu có) phải = Available.
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> AssignRescue([FromBody] AssignRescueDto dto)
    {
        var coordinatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // --- Parse vehicle IDs từ chuỗi ---
        var vehicleIds = new List<int>();
        if (!string.IsNullOrWhiteSpace(dto.VehicleIds))
        {
            foreach (var part in dto.VehicleIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int vid))
                    vehicleIds.Add(vid);
                else
                    return BadRequest(new { Success = false, Message = $"Vehicle ID không hợp lệ: '{part.Trim()}'" });
            }
        }

        // --- Validate rescue_requests.status = Verified ---
        var rescueRequest = await _context.RescueRequests.FindAsync(dto.RequestId);
        if (rescueRequest == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy rescue request với ID = {dto.RequestId}" });

        if (rescueRequest.Status != "Verified")
            return BadRequest(new { Success = false, Message = $"Rescue request phải có status = Verified. Status hiện tại: {rescueRequest.Status}" });

        // --- Validate rescue_teams.status = Available ---
        var rescueTeam = await _context.RescueTeams.FindAsync(dto.TeamId);
        if (rescueTeam == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy rescue team với ID = {dto.TeamId}" });

        if (!string.Equals(rescueTeam.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Success = false, Message = $"Rescue team phải có status = AVAILABLE. Status hiện tại: {rescueTeam.Status}" });

        // --- Validate vehicles.status = Available (nếu có) ---
        var vehicles = new List<Vehicle>();
        if (vehicleIds.Count > 0)
        {
            vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync();

            // Kiểm tra tất cả ID có tồn tại không
            var foundIds = vehicles.Select(v => v.VehicleId).ToHashSet();
            var missingIds = vehicleIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Any())
                return NotFound(new { Success = false, Message = $"Không tìm thấy vehicle với ID: {string.Join(", ", missingIds)}" });

            // Kiểm tra status
            var unavailable = vehicles
                .Where(v => !string.Equals(v.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (unavailable.Any())
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Các phương tiện sau không có status = AVAILABLE: {string.Join(", ", unavailable.Select(v => $"ID={v.VehicleId} ({v.Status})"))}",
                });
        }

        // --- Thực hiện tất cả DB writes trong một transaction ---
        // SqlServerRetryingExecutionStrategy yêu cầu dùng CreateExecutionStrategy()
        // để bao bọc manual transaction thành một đơn vị có thể retry.
        var strategy = _context.Database.CreateExecutionStrategy();

        AssignRescueResponseDto? responseData = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            // 1. Tạo rescue_operation
            var operation = new RescueOperation
            {
                RequestId  = dto.RequestId,
                TeamId     = dto.TeamId,
                AssignedBy = coordinatorId,
                AssignedAt = now,
                Status     = "Assigned",
                EstimatedTime = dto.EstimatedTime
            };
            _context.RescueOperations.Add(operation);
            await _context.SaveChangesAsync(); // Cần SaveChanges để có OperationId

            // 2. Tạo rescue_operation_vehicles (nếu có vehicle)
            foreach (var vid in vehicleIds)
            {
                _context.RescueOperationVehicles.Add(new RescueOperationVehicle
                {
                    OperationId = operation.OperationId,
                    VehicleId   = vid,
                    AssignedBy  = coordinatorId,
                    AssignedAt  = now
                });
            }

            // 3. Cập nhật rescue_requests.status → Assigned
            rescueRequest.Status    = "Assigned";
            rescueRequest.UpdatedAt = now;
            rescueRequest.UpdatedBy = coordinatorId;

            // 4. Thêm record vào rescue_request_status_history
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = dto.RequestId,
                Status    = "Assigned",
                Notes     = $"Phân công cho team {rescueTeam.TeamName} (ID={dto.TeamId})",
                UpdatedBy = coordinatorId,
                UpdatedAt = now
            });

            // 5. Cập nhật rescue_teams.status → BUSY
            rescueTeam.Status = "BUSY";

            // 6. Cập nhật vehicles.status → InUse
            foreach (var v in vehicles)
            {
                v.Status    = "InUse";
                v.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            responseData = new AssignRescueResponseDto
            {
                OperationId        = operation.OperationId,
                RequestId          = dto.RequestId,
                TeamId             = dto.TeamId,
                AssignedVehicleIds = vehicleIds,
                AssignedAt         = now,
                Status             = operation.Status,
                NumberOfAffectedPeople = rescueRequest.NumberOfAffectedPeople,
                EstimatedTime      = operation.EstimatedTime
            };
        });

        return Ok(new
        {
            Success = true,
            Message = "Phân công cứu hộ thành công",
            Data    = responseData
        });
    }

    /// <summary>
    /// RESCUE_TEAM - Lấy danh sách các operation đã được giao cho một team,    
    /// sắp xếp theo thời gian phân công mới nhất trước.
    /// Chỉ thành viên của team đó mới được gọi.
    /// </summary>
    [HttpGet("team/{teamId:int}")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetOperationsByTeam(int teamId)
    {
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdText, out var currentUserId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        var now = DateTime.UtcNow;

        // Kiểm tra user có thuộc teamId này không
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == currentUserId && m.IsActive);

        if (!isMember)
            return Forbid(); // 403 - không phải thành viên của team này

        // Lấy danh sách operations của team (Chỉ lấy những cái đang thực hiện: Assigned hoặc In Progress)
        var operations = await _context.RescueOperations
            .Where(op => op.TeamId == teamId && (op.Status == "Assigned" || op.Status == "In Progress"))
            .OrderByDescending(op => op.AssignedAt)
            .Select(op => new TeamOperationDto
            {
                OperationId        = op.OperationId,
                RequestId          = op.RequestId,
                TeamId             = op.TeamId,
                RequestTitle       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Title)
                                        .FirstOrDefault(),
                RequestAddress     = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Address)
                                        .FirstOrDefault(),
                RequestDescription = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Description)
                                        .FirstOrDefault(),
                RequestPhone       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Phone)
                                        .FirstOrDefault(),
                PriorityName       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => (r.PriorityLevelId == 1 ? "CAO" : 
                                                     r.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                                     r.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG"))
                                        .FirstOrDefault(),
                Latitude           = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Latitude)
                                        .FirstOrDefault(),
                Longitude          = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Longitude)
                                        .FirstOrDefault(),
                OperationStatus    = op.Status,
                AssignedAt         = op.AssignedAt,
                StartedAt          = op.StartedAt,
                CompletedAt        = op.CompletedAt,
                NumberOfAffectedPeople = op.NumberOfAffectedPeople,
                EstimatedTime      = op.EstimatedTime,
                VehicleIds         = _context.RescueOperationVehicles
                                        .Where(v => v.OperationId == op.OperationId)
                                        .Select(v => v.VehicleId)
                                        .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            TeamId  = teamId,
            Total   = operations.Count,
            Data    = operations
        });
    }

    /// <summary>
    /// ADMIN / COORDINATOR / MANAGER - Tìm kiếm rescue operation theo operation_id.
    /// </summary>
    [HttpGet("{operationId:int}")]
    [Authorize(Roles = "ADMIN,COORDINATOR,MANAGER,RESCUE_TEAM")]
    public async Task<IActionResult> GetOperationById(int operationId)
    {
        var operation = await _context.RescueOperations
            .Where(op => op.OperationId == operationId)
            .Select(op => new TeamOperationDto
            {
                OperationId        = op.OperationId,
                RequestId          = op.RequestId,
                TeamId             = op.TeamId,
                RequestTitle       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Title)
                                        .FirstOrDefault(),
                RequestAddress     = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Address)
                                        .FirstOrDefault(),
                RequestDescription = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Description)
                                        .FirstOrDefault(),
                RequestPhone       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Phone)
                                        .FirstOrDefault(),
                PriorityName       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => (r.PriorityLevelId == 1 ? "CAO" : 
                                                     r.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                                     r.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG"))
                                        .FirstOrDefault(),
                Latitude           = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Latitude)
                                        .FirstOrDefault(),
                Longitude          = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => r.Longitude)
                                        .FirstOrDefault(),
                OperationStatus    = op.Status,
                AssignedAt         = op.AssignedAt,
                StartedAt          = op.StartedAt,
                CompletedAt        = op.CompletedAt,
                NumberOfAffectedPeople = op.NumberOfAffectedPeople,
                EstimatedTime      = op.EstimatedTime,
                VehicleIds         = _context.RescueOperationVehicles
                                        .Where(v => v.OperationId == op.OperationId)
                                        .Select(v => v.VehicleId)
                                        .ToList()
            })
            .FirstOrDefaultAsync();

        if (operation == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy operation với ID = {operationId}" });

        return Ok(new { Success = true, Data = operation });
    }

    /// <summary>
    /// RESCUE_TEAM - Cập nhật trạng thái của nhiệm vụ (In Progress hoặc Completed)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> UpdateOperationStatus(int id, [FromBody] UpdateOperationStatusDto dto)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var now = DateTime.UtcNow;

        var strategy = _context.Database.CreateExecutionStrategy();
        IActionResult? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. Load operation, request, team
            var operation = await _context.RescueOperations
                .Include(op => op.Request)
                .Include(op => op.Team)
                .FirstOrDefaultAsync(op => op.OperationId == id);

            if (operation == null)
            {
                result = NotFound(new { Success = false, Message = $"Không tìm thấy operation với ID {id}" });
                return;
            }

            // 2. Kiểm tra user là thành viên active của team
            var isMember = await _context.RescueTeamMembers
                .AnyAsync(m => m.TeamId == operation.TeamId && m.UserId == currentUserId && m.IsActive);

            if (!isMember)
            {
                result = Forbid(); // 403 - không phải thành viên của team này
                return;
            }

            // 3. Quy trình chuyển đổi và Conditional Update (Database level concept via transaction check)
            var currentOpStatus = operation.Status;

            // Normalize input - chuyển đổi mapping từ prompt sang PascalCase dùng trong DB
            string targetStatus = dto.NewStatus;
            if (targetStatus.Equals("IN_PROGRESS", StringComparison.OrdinalIgnoreCase)) targetStatus = "In Progress";
            if (targetStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)) targetStatus = "Completed";
            if (targetStatus.Equals("FAILED", StringComparison.OrdinalIgnoreCase)) targetStatus = "Failed";

            if (targetStatus == "In Progress")
            {
                // Kiểm tra status hiện tại (Assigned)
                if (currentOpStatus != "Assigned")
                {
                    result = Conflict(new { Success = false, Message = $"Không thể bắt đầu nhiệm vụ. Trạng thái hiện tại: {currentOpStatus}" });
                    return;
                }

                operation.Status = "In Progress";
                operation.StartedAt = now;
                
                if (operation.Request != null)
                {
                    operation.Request.Status = "In Progress";
                    operation.Request.UpdatedAt = now;
                    operation.Request.UpdatedBy = currentUserId;
                }
            }
            else if (targetStatus == "Completed")
            {
                // Kiểm tra status hiện tại (In Progress)
                if (currentOpStatus != "In Progress")
                {
                    result = Conflict(new { Success = false, Message = $"Không thể hoàn tất nhiệm vụ. Trạng thái hiện tại: {currentOpStatus}" });
                    return;
                }

                operation.Status = "Completed";
                operation.CompletedAt = now;

                if (operation.Request != null)
                {
                    operation.Request.Status = "Completed";
                    operation.Request.UpdatedAt = now;
                    operation.Request.UpdatedBy = currentUserId;
                }

                // Cập nhật đội và phương tiện sang Available
                if (operation.Team != null)
                {
                    operation.Team.Status = "AVAILABLE";
                }

                // Giải phóng phương tiện
                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(rov => rov.OperationId == operation.OperationId)
                    .Select(rov => rov.VehicleId)
                    .ToListAsync();

                if (vehicleIds.Any())
                {
                    var vehiclesToRelease = await _context.Vehicles
                        .Where(v => vehicleIds.Contains(v.VehicleId))
                        .ToListAsync();

                    foreach (var v in vehiclesToRelease)
                    {
                        v.Status = "AVAILABLE";
                        v.UpdatedAt = now;
                    }
                }
            }
            else if (targetStatus == "Failed")
            {
                // Bắt buộc phải có lý do
                if (string.IsNullOrWhiteSpace(dto.Reason))
                {
                    result = BadRequest(new { Success = false, Message = "Bắt buộc phải nhập lý do" });
                    return;
                }

                // Kiểm tra status hiện tại (Chỉ cho phép fail khi đang Assigned hoặc In Progress)
                if (currentOpStatus != "Assigned" && currentOpStatus != "In Progress")
                {
                    result = Conflict(new { Success = false, Message = $"Không thể cập nhật thất bại. Trạng thái hiện tại: {currentOpStatus}" });
                    return;
                }

                operation.Status = "Failed";
                operation.CompletedAt = now; 

                if (operation.Request != null)
                {
                    // Khi thất bại, đưa Request về lại trạng thái Verified để có thể phân công đội khác
                    operation.Request.Status = "Verified";
                    operation.Request.UpdatedAt = now;
                    operation.Request.UpdatedBy = currentUserId;
                }

                // Cập nhật đội và phương tiện sang Available
                if (operation.Team != null)
                {
                    operation.Team.Status = "AVAILABLE";
                }

                // Giải phóng phương tiện
                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(rov => rov.OperationId == operation.OperationId)
                    .Select(rov => rov.VehicleId)
                    .ToListAsync();

                if (vehicleIds.Any())
                {
                    var vehiclesToRelease = await _context.Vehicles
                        .Where(v => vehicleIds.Contains(v.VehicleId))
                        .ToListAsync();

                    foreach (var v in vehiclesToRelease)
                    {
                        v.Status = "AVAILABLE";
                        v.UpdatedAt = now;
                    }
                }
            }
            else
            {
                result = BadRequest(new { Success = false, Message = "Trạng thái mới không hợp lệ. Chỉ chấp nhận IN_PROGRESS, COMPLETED hoặc FAILED." });
                return;
            }

            // 4. Lưu lịch sử cho Request
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = operation.RequestId,
                Status = targetStatus,
                Notes = targetStatus == "Failed" 
                        ? $"Nhiệm vụ thất bại. Lý do: {dto.Reason}" 
                        : $"Đội cứu hộ cập nhật trạng thái nhiệm vụ sang {targetStatus}",
                UpdatedBy = currentUserId,
                UpdatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result = Ok(new { Success = true, Message = $"Cập nhật trạng thái sang {targetStatus} thành công" });
        });

        return result ?? StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi cập nhật trạng thái" });
    }

    [HttpGet("requests/{requestId:int}/nearest-teams")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> GetNearestTeamsForRequest(int requestId, CancellationToken cancellationToken)
    {
        var request = await _context.RescueRequests
            .AsNoTracking()
            .Where(r => r.RequestId == requestId)
            .Select(r => new
            {
                r.RequestId,
                r.Latitude,
                r.Longitude
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });
        }

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Yêu cầu cứu hộ chưa có tọa độ hợp lệ." });
        }

        var teams = await _context.RescueTeams
            .AsNoTracking()
            .Where(t => t.Status == "AVAILABLE" && t.BaseLatitude.HasValue && t.BaseLongitude.HasValue)
            .Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.Status,
                t.BaseLatitude,
                t.BaseLongitude
            })
            .ToListAsync(cancellationToken);

        var result = new List<RescueTeamDistanceDto>();

        foreach (var team in teams)
        {
            var distanceKm = await _distanceService.GetRoadDistanceKmAsync(
                (double)team.BaseLatitude!.Value,
                (double)team.BaseLongitude!.Value,
                (double)request.Latitude.Value,
                (double)request.Longitude.Value,
                cancellationToken);

            result.Add(new RescueTeamDistanceDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Status = team.Status,
                BaseLatitude = team.BaseLatitude.Value,
                BaseLongitude = team.BaseLongitude.Value,
                RequestLatitude = request.Latitude.Value,
                RequestLongitude = request.Longitude.Value,
                DistanceKm = Math.Round(distanceKm, 2)
            });
        }

        var ordered = result
            .OrderBy(x => x.DistanceKm)
            .ToList();

        return Ok(new
        {
            Success = true,
            RequestId = requestId,
            Count = ordered.Count,
            Data = ordered
        });
    }
}
