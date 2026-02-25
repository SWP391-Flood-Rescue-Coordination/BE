using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
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

    public RescueOperationController(ApplicationDbContext context)
    {
        _context = context;
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

        if (rescueTeam.Status != "AVAILABLE")
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
            var unavailable = vehicles.Where(v => v.Status != "Available").ToList();
            if (unavailable.Any())
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Các phương tiện sau không có status = Available: {string.Join(", ", unavailable.Select(v => $"ID={v.VehicleId} ({v.Status})"))}",
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
                Status     = "Assigned"
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
                Status             = operation.Status
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
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Kiểm tra user có thuộc teamId này không
        var isMember = await _context.RescueTeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == currentUserId && m.IsActive);

        if (!isMember)
            return Forbid(); // 403 - không phải thành viên của team này

        // Lấy danh sách operations của team, join với rescue_requests và vehicles
        var operations = await _context.RescueOperations
            .Where(op => op.TeamId == teamId)
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
    [Authorize(Roles = "ADMIN,COORDINATOR,MANAGER")]
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
}
