using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// RescueOperationController: Quản lý các hoạt động cứu hộ thực tế.
/// Bao gồm việc phân công đội cứu hộ, cập nhật trạng thái nhiệm vụ và tìm kiếm các đội cứu hộ gần điểm xảy ra sự cố.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RescueOperationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistanceService _distanceService;
    private readonly ILogger<RescueOperationController> _logger;

    /// <summary>
    /// Constructor khởi tạo RescueOperationController.
    /// </summary>
    /// <param name="context">DbContext để thao tác dữ liệu.</param>
    public RescueOperationController(
        ApplicationDbContext context,
        IDistanceService distanceService,
        ILogger<RescueOperationController> logger)
    {
        _context = context;
        _distanceService = distanceService;
        _logger = logger;
    }

    /// <summary>
    /// API Phân công đội cứu hộ (Rescue Team) và phương tiện (Vehicles) cho một yêu cầu cứu hộ cụ thể.
    /// Chỉ dành cho vai trò COORDINATOR.
    /// </summary>
    /// <param name="dto">Dữ liệu phân công mẫu: ID yêu cầu, ID Team, danh sách ID phương tiện.</param>
    /// <returns>Thông tin chi tiết về nhiệm vụ vừa được khởi tạo.</returns>
    [HttpPost("assign")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> AssignRescue([FromBody] AssignRescueDto dto)
    {
        // 1. Lấy ID người điều phối từ Token JWT
        var coordinatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // 2. Phân tách danh sách ID phương tiện từ chuỗi (dấu phẩy ngăn cách)
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

        // 3. Kiểm tra tính hợp lệ của Rescue Request (Yêu cầu phải tồn tại và có trạng thái Verified)
        var rescueRequest = await _context.RescueRequests.FindAsync(dto.RequestId);
        if (rescueRequest == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy rescue request với ID = {dto.RequestId}" });

        if (rescueRequest.Status != "Verified")
            return BadRequest(new { Success = false, Message = $"Rescue request phải có status = Verified. Status hiện tại: {rescueRequest.Status}" });

        // 4. Kiểm tra tính hợp lệ của Rescue Team (Team phải tồn tại và đang rảnh - Available)
        var rescueTeam = await _context.RescueTeams.FindAsync(dto.TeamId);
        if (rescueTeam == null)
            return NotFound(new { Success = false, Message = $"Không tìm thấy rescue team với ID = {dto.TeamId}" });

        // (Đã loại bỏ check trạng thái bận của Đội cứu hộ theo yêu cầu)

        // 5. Kiểm tra tính hợp lệ của các phương tiện được chọn (Phương tiện phải rảnh - Available)
        var vehicles = new List<Vehicle>();
        if (vehicleIds.Count > 0)
        {
            vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync();

            // Đảm bảo tìm thấy tất cả ID đã cung cấp
            var foundIds = vehicles.Select(v => v.VehicleId).ToHashSet();
            var missingIds = vehicleIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Any())
                return NotFound(new { Success = false, Message = $"Không tìm thấy vehicle với ID: {string.Join(", ", missingIds)}" });

            // Kiểm tra trạng thái từng phương tiện
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

        // 6. Thực thi quy trình phân công bằng Database Transaction (Để đảm bảo tính nhất quán dữ liệu)
        var strategy = _context.Database.CreateExecutionStrategy();
        AssignRescueResponseDto? responseData = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            // (1) Tạo bản ghi Rescue Operation (Nhiệm vụ cứu hộ)
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
            await _context.SaveChangesAsync(); // Lưu để phát sinh OperationId tự tăng

            // (2) Liên kết các phương tiện với nhiệm vụ cứu hộ này
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

            // (3) Cập nhật trạng thái của Yêu cầu (Request) sang 'Assigned'
            rescueRequest.Status    = "Assigned";
            rescueRequest.UpdatedAt = now;
            rescueRequest.UpdatedBy = coordinatorId;

            // (4) Lưu vết lịch sử thay đổi trạng thái của Yêu cầu
            _context.RescueRequestStatusHistories.Add(new RescueRequestStatusHistory
            {
                RequestId = dto.RequestId,
                Status    = "Assigned",
                Notes     = $"Phân công cho team {rescueTeam.TeamName} (ID={dto.TeamId})",
                UpdatedBy = coordinatorId,
                UpdatedAt = now
            });

            // (5) Đội cứu hộ (trạng thái bận được tính toán động dựa trên các operation đang chạy)

            // (6) Chuyển trạng thái các Phương tiện sang 'InUse'
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
                NumberOfAffectedPeople = operation.NumberOfAffectedPeople,
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
    /// API lấy danh sách các nhiệm vụ cứu hộ được phân công cho một Đội (Rescue Team).
    /// Hỗ trợ đội cứu hộ xem các công việc đang chờ xử lý.
    /// </summary>
    /// <param name="teamId">ID của đội cứu hộ cần tra cứu.</param>
    /// <returns>Danh sách các nhiệm vụ cụ thể và thông tin đi kèm từ yêu cầu gốc.</returns>
    [HttpGet("team/{teamId:int}")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> GetOperationsByTeam(int teamId)
    {
        // 1. Xác thực ID người dùng từ Token
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdText, out var currentUserId))
        {
            return Unauthorized(new { Success = false, Message = "Token không hợp lệ." });
        }

        // 2. Bảo mật: Đảm bảo người dùng gọi API là thành viên chính thức của Team đó
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == currentUserId && m.IsActive);

        if (!isMember)
            return Forbid();

        // 3. Truy vấn các nhiệm vụ đang ở trạng thái 'Assigned' của Team
        var operations = await _context.RescueOperations
            .Where(op => op.TeamId == teamId && op.Status == "Assigned")
            .OrderByDescending(op => op.AssignedAt)
            .Select(op => new TeamOperationDto
            {
                OperationId = op.OperationId,
                RequestId = op.RequestId,
                TeamId = op.TeamId,
                // Lấy thông tin tóm tắt từ Yêu cầu cứu hộ gốc để Team nắm bắt hiện trạng
                RequestTitle = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Title).FirstOrDefault(),
                RequestAddress = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Address).FirstOrDefault(),
                RequestDescription = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Description).FirstOrDefault(),
                RequestPhone = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Phone).FirstOrDefault(),
                // Phân loại mức độ ưu tiên sang tiếng Việt dễ hiểu
                PriorityName = _context.RescueRequests
                    .Where(r => r.RequestId == op.RequestId)
                    .Select(r => (r.PriorityLevelId == 1 ? "CAO" :
                                 r.PriorityLevelId == 2 ? "TRUNG BÌNH" :
                                 r.PriorityLevelId == 3 ? "THẤP" : "THÔNG THƯỜNG"))
                    .FirstOrDefault(),
                Latitude = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Latitude).FirstOrDefault(),
                Longitude = _context.RescueRequests.Where(r => r.RequestId == op.RequestId).Select(r => r.Longitude).FirstOrDefault(),
                OperationStatus = op.Status ?? string.Empty,
                AssignedAt = op.AssignedAt,
                StartedAt = op.StartedAt,
                CompletedAt = op.CompletedAt,
                NumberOfAffectedPeople = op.NumberOfAffectedPeople,
                EstimatedTime = op.EstimatedTime,
                // Lấy danh sách phương tiện được cấp cho nhiệm vụ này
                VehicleIds = _context.RescueOperationVehicles
                    .Where(v => v.OperationId == op.OperationId)
                    .Select(v => v.VehicleId)
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            TeamId = teamId,
            Total = operations.Count,
            Data = operations
        });
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một nhiệm vụ cứu hộ thông qua ID.
    /// </summary>
    /// <param name="operationId">Mã định danh của nhiệm vụ.</param>
    /// <returns>Dữ liệu DTO của nhiệm vụ (TeamOperationDto).</returns>
    [HttpGet("{operationId:int}")]
    [Authorize(Roles = "ADMIN,COORDINATOR,MANAGER,RESCUE_TEAM")]
    public async Task<IActionResult> GetOperationById(int operationId)
    {
        // Thực hiện truy vấn và Map nhanh sang DTO
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
                OperationStatus    = op.Status ?? string.Empty,
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
    /// API cho phép đội cứu hộ (Rescue Team) báo cáo hoàn thành nhiệm vụ hoặc báo cáo từ chối/thất bại.
    /// Hệ thống sẽ tự động giải phóng đội cứu hộ và phương tiện sau khi hoàn tất.
    /// </summary>
    /// <param name="id">ID của nhiệm vụ cứu hộ (OperationId).</param>
    /// <param name="dto">Dữ liệu trạng thái mới (COMPLETED).</param>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "RESCUE_TEAM")]
    public async Task<IActionResult> UpdateOperationStatus(int id, [FromBody] UpdateOperationStatusDto dto)
    {
        await Task.CompletedTask;
        return StatusCode(410, new
        {
            Success = false,
            Message = "Endpoint này đã ngừng dùng trong luồng rescue team. Hãy dùng PUT /api/rescue-team/operations/{operationId}/status để đảm bảo đúng audit log."
        });
    }

    /// <summary>
    /// API tìm kiếm các đội cứu hộ đang rảnh (AVAILABLE) và ở gần vị trí yêu cầu cứu hộ nhất.
    /// Tính toán khoảng cách di chuyển thực tế theo đường bộ thông qua dịch vụ DistanceService (OSRM API).
    /// Hỗ trợ COORDINATOR ra quyết định phân công tối ưu nhất về thời gian di chuyển.
    /// </summary>
    /// <param name="requestId">ID của yêu cầu cứu hộ cần hỗ trợ.</param>
    [HttpGet("requests/{requestId:int}/nearest-teams")]
    [Authorize(Roles = "COORDINATOR")]
    public async Task<IActionResult> GetNearestTeamsForRequest(int requestId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin tọa độ của yêu cầu cứu hộ từ DB
        var request = await _context.RescueRequests
            .AsNoTracking()
            .Where(r => r.RequestId == requestId)
            .Select(r => new
            {
                r.RequestId,
                r.Status,
                r.Latitude,
                r.Longitude
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy yêu cầu cứu hộ." });
        }

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                Success = false,
                Message = $"Chỉ có thể lấy danh sách team gần nhất cho yêu cầu có trạng thái Pending. Trạng thái hiện tại: '{request.Status}'."
            });
        }

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Yêu cầu cứu hộ chưa có tọa độ hợp lệ." });
        }

        // 2. Lấy danh sách tất cả các Đội cứu hộ có tọa độ đóng quân
        var teams = await _context.RescueTeams
            .AsNoTracking()
            .Where(t => t.BaseLatitude.HasValue && t.BaseLongitude.HasValue)
            .Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.BaseLatitude,
                t.BaseLongitude,
                FreeMemberCount = _context.RescueTeamMembers.Count(m =>
                    m.TeamId == t.TeamId &&
                    m.IsActive &&
                    m.RequestId == null &&
                    m.MemberRole != "Leader")
            })
            .ToListAsync(cancellationToken);

        var result = new List<RescueTeamDistanceDto>();
        var warnings = new List<string>();

        // 3. Với mỗi đội cứu hộ, ưu tiên tính khoảng cách theo đường bộ qua OSRM;
        // nếu OSRM lỗi thì fallback sang khoảng cách thẳng.
        foreach (var team in teams)
        {
            double distanceKm;
            string? distanceNote = null;

            try
            {
                distanceKm = await _distanceService.GetRoadDistanceKmAsync(
                    (double)team.BaseLatitude!.Value,
                    (double)team.BaseLongitude!.Value,
                    (double)request.Latitude.Value,
                    (double)request.Longitude.Value,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                distanceKm = CalculateStraightLineDistanceKm(
                    (double)team.BaseLatitude!.Value,
                    (double)team.BaseLongitude!.Value,
                    (double)request.Latitude.Value,
                    (double)request.Longitude.Value);

                distanceNote = $"OSRM network lỗi khi tính khoảng cách cho team {team.TeamId}: {ex.Message}. Đã dùng khoảng cách thẳng thay thế.";
                warnings.Add(distanceNote);
                _logger.LogWarning(ex, "OSRM network failure for team {TeamId}, request {RequestId}", team.TeamId, requestId);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                distanceKm = CalculateStraightLineDistanceKm(
                    (double)team.BaseLatitude!.Value,
                    (double)team.BaseLongitude!.Value,
                    (double)request.Latitude.Value,
                    (double)request.Longitude.Value);

                distanceNote = $"OSRM timeout khi tính khoảng cách cho team {team.TeamId}: {ex.Message}. Đã dùng khoảng cách thẳng thay thế.";
                warnings.Add(distanceNote);
                _logger.LogWarning(ex, "OSRM timeout for team {TeamId}, request {RequestId}", team.TeamId, requestId);
            }
            catch (OsrmException ex)
            {
                distanceKm = CalculateStraightLineDistanceKm(
                    (double)team.BaseLatitude!.Value,
                    (double)team.BaseLongitude!.Value,
                    (double)request.Latitude.Value,
                    (double)request.Longitude.Value);

                distanceNote = $"OSRM invalid response cho team {team.TeamId}: {ex.Message}. Đã dùng khoảng cách thẳng thay thế.";
                warnings.Add(distanceNote);
                _logger.LogWarning(ex, "OSRM invalid response for team {TeamId}, request {RequestId}", team.TeamId, requestId);
            }

            result.Add(new RescueTeamDistanceDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                BaseLatitude = team.BaseLatitude.Value,
                BaseLongitude = team.BaseLongitude.Value,
                RequestLatitude = request.Latitude.Value,
                RequestLongitude = request.Longitude.Value,
                DistanceKm = Math.Round(distanceKm, 2),
                FreeMemberCount = team.FreeMemberCount,
                DistanceNote = distanceNote
            });
        }

        // 4. Sắp xếp danh sách đội cứu hộ từ gần nhất đến xa nhất và trả về
        var ordered = result
            .OrderBy(x => x.DistanceKm)
            .ToList();

        return Ok(new
        {
            Success = true,
            RequestId = requestId,
            Count = ordered.Count,
            Warnings = warnings.Any() ? warnings.Distinct().ToList() : null,
            Data = ordered
        });
    }

    private static double CalculateStraightLineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }
}
