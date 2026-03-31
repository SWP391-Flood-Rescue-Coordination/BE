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

    /// <summary>
    /// Constructor khởi tạo RescueOperationController với DbContext và dịch vụ tính toán khoảng cách.
    /// </summary>
    public RescueOperationController(ApplicationDbContext context, IDistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }

    /// <summary>
    /// API Điều phối (Coordinator): Phân công đội cứu hộ và phương tiện cho một yêu cầu cứu hộ.
    /// Quy trình: Kiểm tra trạng thái Request (phải là Verified), Đội (Available), Phương tiện (Available). 
    /// Sau đó thực hiện cập nhật đồng thời trong một Transaction.
    /// </summary>
    /// <param name="dto">Dữ liệu phân công (RequestId, TeamId, VehicleIds, EstimatedTime).</param>
    /// <returns>Thông tin chi tiết về Operation vừa tạo.</returns>
    [HttpPost("assign")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> AssignRescue([FromBody] AssignRescueDto dto)
    {
        var coordinatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // 1. Phân tách danh sách ID phương tiện từ chuỗi (ví dụ: "1,2,3")
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

        // 2. Kiểm tra tính hợp lệ của Yêu cầu cứu hộ
        var rescueRequest = await _context.RescueRequests.FindAsync(dto.RequestId);
        if (rescueRequest == null) return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });
        if (rescueRequest.Status != "Verified") return BadRequest(new { Success = false, Message = "Yêu cầu chưa được xác minh." });

        // 3. Kiểm tra tính hợp lệ của Đội cứu hộ
        var rescueTeam = await _context.RescueTeams.FindAsync(dto.TeamId);
        if (rescueTeam == null) return NotFound(new { Success = false, Message = "Không tìm thấy đội cứu hộ." });
        if (!string.Equals(rescueTeam.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { Success = false, Message = "Đội cứu hộ đang bận hoặc không sẵn sàng." });

        // 4. Kiểm tra tính hợp lệ của các Phương tiện
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

        // 5. Thực thi nghiệp vụ phân công trong một Transaction để đảm bảo tính toàn vẹn dữ liệu
        var strategy = _context.Database.CreateExecutionStrategy();
        AssignRescueResponseDto? responseData = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            // - Tạo bản ghi chiến dịch cốt lõi (RescueOperation)
            var operation = new RescueOperation
            {
                RequestId  = dto.RequestId,
                TeamId     = dto.TeamId,
                AssignedBy = coordinatorId,
                AssignedAt = now,
                Status     = "Assigned",
                NumberOfAffectedPeople = rescueRequest.NumberOfAffectedPeople,
                EstimatedTime = dto.EstimatedTime
            };
            _context.RescueOperations.Add(operation);
            await _context.SaveChangesAsync();

            // - Gán phương tiện vào chiến dịch
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

            // - Cập nhật trạng thái Request sang 'Assigned'
            rescueRequest.Status    = "Assigned";
            rescueRequest.UpdatedAt = now;
            rescueRequest.UpdatedBy = coordinatorId;

            // - Ghi Log lịch sử Request
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = dto.RequestId,
                Status    = "Assigned",
                Notes     = $"Đã phân công cho đội {rescueTeam.TeamName}",
                UpdatedBy = coordinatorId,
                UpdatedAt = now
            });

            // - Đánh dấu Đội và Phương tiện là 'BUSY'/'InUse'
            rescueTeam.Status = "BUSY";

            // 6. Cập nhật vehicles.status → InUse
            foreach (var v in vehicles)
            {
                v.Status = "InUse";
                v.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            responseData = new AssignRescueResponseDto
            {
                OperationId = operation.OperationId,
                RequestId = dto.RequestId,
                TeamId = dto.TeamId,
                AssignedVehicleIds = vehicleIds,
                AssignedAt = now,
                Status = operation.Status,
                NumberOfAffectedPeople = operation.NumberOfAffectedPeople,
                EstimatedTime = operation.EstimatedTime
            };
        });

        return Ok(new { Success = true, Message = "Phân công cứu hộ thành công", Data = responseData });
    }

    /// <summary>
    /// API cho Đội cứu hộ: Lấy danh sách các nhiệm vụ (Operation) đang được giao cho đội của mình.
    /// Yêu cầu: Người dùng phải là thành viên đang hoạt động của đội đó.
    /// </summary>
    /// <param name="teamId">ID của đội cứu hộ.</param>
    /// <returns>Danh sách các nhiệm vụ kèm theo thông tin chi tiết địa chỉ, tọa độ cứu hộ.</returns>
    [HttpGet("team/{teamId:int}")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetOperationsByTeam(int teamId)
    {
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdText, out var currentUserId)) return Unauthorized();

        // Kiểm tra tư cách thành viên
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == currentUserId && m.IsActive);

        if (!isMember) return Forbid();

        // Truy vấn các Operation đang ở trạng thái 'Assigned' của đội
        var operations = await _context.RescueOperations
            .Where(op => op.TeamId == teamId && op.Status == "Assigned")
            .OrderByDescending(op => op.AssignedAt)
            .Select(op => new TeamOperationDto
            {
                OperationId = op.OperationId,
                RequestId = op.RequestId,
                TeamId = op.TeamId,
                RequestTitle = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Title).FirstOrDefault(),
                RequestAddress = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Address).FirstOrDefault(),
                RequestDescription = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Description).FirstOrDefault(),
                RequestPhone = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Phone).FirstOrDefault(),
                PriorityName = _context.RescueRequests
                    .Where(r => r.RequestId == op.RequestId)
                    .Select(r => (r.PriorityLevelId == 1 ? "CAO" : r.PriorityLevelId == 2 ? "TRUNG BÌNH" : "THẤP"))
                    .FirstOrDefault(),
                Latitude = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Latitude).FirstOrDefault(),
                Longitude = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Longitude).FirstOrDefault(),
                OperationStatus = op.Status ?? string.Empty,
                AssignedAt = op.AssignedAt,
                StartedAt = op.StartedAt,
                CompletedAt = op.CompletedAt,
                NumberOfAffectedPeople = op.NumberOfAffectedPeople,
                EstimatedTime = op.EstimatedTime,
                VehicleIds = _context.RescueOperationVehicles
                    .Where(v => v.OperationId == op.OperationId)
                    .Select(v => v.VehicleId)
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { Success = true, TeamId = teamId, Total = operations.Count, Data = operations });
    }

    /// <summary>
    /// API lấy thông tin chi tiết của một nhiệm vụ cứu hộ cụ thể bằng ID.
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
                RequestTitle       = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Title).FirstOrDefault(),
                RequestAddress     = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Address).FirstOrDefault(),
                RequestDescription = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Description).FirstOrDefault(),
                RequestPhone       = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Phone).FirstOrDefault(),
                PriorityName       = _context.RescueRequests
                                        .Where(r => r.RequestId == op.RequestId)
                                        .Select(r => (r.PriorityLevelId == 1 ? "CAO" : r.PriorityLevelId == 2 ? "TRUNG BÌNH" : "THẤP"))
                                        .FirstOrDefault(),
                Latitude           = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Latitude).FirstOrDefault(),
                Longitude          = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Longitude).FirstOrDefault(),
                OperationStatus    = op.Status ?? string.Empty,
                AssignedAt         = op.AssignedAt,
                StartedAt          = op.StartedAt,
                CompletedAt        = op.CompletedAt,
                NumberOfAffectedPeople = op.NumberOfAffectedPeople,
                EstimatedTime      = op.EstimatedTime,
                VehicleIds         = _context.RescueOperationVehicles.Where(v => v.OperationId == op.OperationId).Select(v => v.VehicleId).ToList()
            })
            .FirstOrDefaultAsync();

        if (operation == null) return NotFound(new { Success = false, Message = "Nhiệm vụ không tồn tại." });

        return Ok(new { Success = true, Data = operation });
    }

    /// <summary>
    /// API dành cho Đội cứu hộ: Cập nhật kết quả của nhiệm vụ (Thành công / Thất bại).
    /// Quy trình: Cập nhật trạng thái Operation, giải phóng Đội và Phương tiện về trạng thái Available.
    /// Nếu thất đuổi, trả Request về trạng thái Verified để Coordinator điều xe khác.
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

            var operation = await _context.RescueOperations
                .Include(op => op.Request)
                .Include(op => op.Team)
                .FirstOrDefaultAsync(op => op.OperationId == id);

            if (operation == null) { result = NotFound(new { Success = false }); return; }

            // Kiểm tra thành viên đội
            var isMember = await _context.RescueTeamMembers
                .AnyAsync(m => m.TeamId == operation.TeamId && m.UserId == currentUserId && m.IsActive);
            if (!isMember) { result = Forbid(); return; }

            var currentOpStatus = operation.Status;
            var targetStatusKey = dto.NewStatus.Trim().ToUpperInvariant();

            if (targetStatusKey == "COMPLETED")
            {
                // Khi đội cứu hộ xác nhận hoàn tất thành công
                operation.Status = "Completed";
                operation.StartedAt ??= now;
                operation.CompletedAt = now;

                if (operation.Request != null)
                {
                    // Chuyển status của Request (lúc này công dân có thể bấm báo an toàn nốt)
                    operation.Request.Status = "Completed";
                    operation.Request.UpdatedAt = now;
                    operation.Request.UpdatedBy = currentUserId;
                }

                if (operation.Team != null) operation.Team.Status = "AVAILABLE"; // Giải phóng đội

                // Giải phóng phương tiện
                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(rov => rov.OperationId == operation.OperationId)
                    .Select(rov => rov.VehicleId)
                    .ToListAsync();
                if (vehicleIds.Any())
                {
                    var vehicles = await _context.Vehicles.Where(v => vehicleIds.Contains(v.VehicleId)).ToListAsync();
                    foreach (var v in vehicles) { v.Status = "AVAILABLE"; v.UpdatedAt = now; }
                }
            }
            else if (targetStatusKey == "FAILED")
            {
                // Khi đội cứu hộ xác nhận không thể tiếp cận/cứu hộ thất bại
                operation.Status = "Failed";
                operation.StartedAt ??= now;
                operation.CompletedAt = now;

                if (operation.Request != null)
                {
                    // Trả yêu cầu về trạng thái Verified để lập chiến dịch mới
                    operation.Request.Status = "Verified";
                    operation.Request.UpdatedAt = now;
                    operation.Request.UpdatedBy = currentUserId;
                }

                if (operation.Team != null) operation.Team.Status = "AVAILABLE"; // Giải phóng đội

                // Giải phóng phương tiện
                var vehicleIds = await _context.RescueOperationVehicles
                    .Where(rov => rov.OperationId == operation.OperationId)
                    .Select(rov => rov.VehicleId)
                    .ToListAsync();
                if (vehicleIds.Any())
                {
                    var vehicles = await _context.Vehicles.Where(v => vehicleIds.Contains(v.VehicleId)).ToListAsync();
                    foreach (var v in vehicles) { v.Status = "AVAILABLE"; v.UpdatedAt = now; }
                }
            }
            else
            {
                result = BadRequest(new { Success = false, Message = "Trạng thái không hợp lệ." });
                return;
            }

            // Ghi Log lịch sử thay đổi của Request
            var requestStatusForHistory = operation.Request?.Status ?? (targetStatusKey == "FAILED" ? "Verified" : "Completed");
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = operation.RequestId,
                Status = requestStatusForHistory,
                Notes = targetStatusKey == "FAILED" ? $"Thất bại: {dto.Reason}" : "Hoàn tất nhiệm vụ.",
                UpdatedBy = currentUserId,
                UpdatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result = Ok(new { Success = true, Message = "Cập nhật thành công." });
        });

        return result ?? StatusCode(500);
    }

    /// <summary>
    /// API Điều phối (Coordinator): Tìm kiếm các đội cứu hộ đang sẵn sàng và ở gần vị trí yêu cầu cứu hộ nhất.
    /// Sử dụng Nominatim/OSRM để tính toán khoảng cách đường bộ thực tế thay vì chim bay.
    /// </summary>
    /// <param name="requestId">ID của yêu cầu cứu hộ cần điều phối.</param>
    /// <param name="cancellationToken">Token hủy yêu cầu.</param>
    /// <returns>Danh sách các đội cứu hộ kèm theo số KM khoảng cách đường bộ, sắp xếp từ gần tới xa.</returns>
    [HttpGet("requests/{requestId:int}/nearest-teams")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> GetNearestTeamsForRequest(int requestId, CancellationToken cancellationToken)
    {
        // 1. Lấy tọa độ của yêu cầu cứu hộ
        var request = await _context.RescueRequests
            .AsNoTracking()
            .Where(r => r.RequestId == requestId)
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null) return NotFound(new { Success = false, Message = "Yêu cầu không tồn tại." });
        if (!request.Latitude.HasValue || !request.Longitude.HasValue) 
            return BadRequest(new { Success = false, Message = "Yêu cầu chưa có tọa độ vị trí." });

        // 2. Lấy danh sách các đội đang Available và có định vị trụ sở
        var teams = await _context.RescueTeams
            .AsNoTracking()
            .Where(t => t.Status == "AVAILABLE" && t.BaseLatitude.HasValue && t.BaseLongitude.HasValue)
            .ToListAsync(cancellationToken);

        var result = new List<RescueTeamDistanceDto>();

        // 3. Gọi dịch vụ OSRM để tính toán khoảng cách di chuyển thực tế cho từng đội
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

        // 4. Sắp xếp theo khoảng cách KM tăng dần
        var ordered = result.OrderBy(x => x.DistanceKm).ToList();

        return Ok(new { Success = true, RequestId = requestId, Count = ordered.Count, Data = ordered });
    }
}
