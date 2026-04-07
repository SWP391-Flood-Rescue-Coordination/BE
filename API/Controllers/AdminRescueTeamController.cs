using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// API quản lý `rescue team` dành riêng cho Admin.
/// 
/// Luồng chính:
/// - `GET /api/admin/rescue-teams` : lấy danh sách team
/// - `GET /api/admin/rescue-teams/{id}` : xem chi tiết team
/// - `POST /api/admin/rescue-teams` : tạo team mới
/// - `POST /api/admin/rescue-teams/{id}/members` : thêm thành viên
/// - `DELETE /api/admin/rescue-teams/{id}/members/{userId}` : bớt thành viên
/// - `PUT /api/admin/rescue-teams/{id}/leader` : đổi leader
/// - `DELETE /api/admin/rescue-teams/{id}` : xóa team
/// </summary>
[ApiExplorerSettings(GroupName = "rescue-team")]
[ApiController]
[Route("api/admin/rescue-teams")]
[Authorize(Roles = "ADMIN")]
public class AdminRescueTeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IGeocodingService _geocodingService;

    public AdminRescueTeamController(ApplicationDbContext context, IGeocodingService geocodingService)
    {
        _context = context;
        _geocodingService = geocodingService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ team trong hệ thống.
    /// </summary>
    /// <remarks>
    /// Route: `GET /api/admin/rescue-teams`
    /// 
    /// Công dụng:
    /// - Admin xem toàn bộ team.
    /// - Có cả thông tin leader, số lượng thành viên active/inactive.
    /// 
    /// Nơi FE gọi tới:
    /// - Màn quản trị danh sách rescue team.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAllTeams()
    {
        var teams = await _context.RescueTeams
            .AsNoTracking()
            .OrderBy(t => t.TeamName)
            .Select(t => new RescueTeamSummaryDto
            {
                TeamId = t.TeamId,
                TeamName = t.TeamName,
                Address = t.Address,
                BaseLatitude = t.BaseLatitude,
                BaseLongitude = t.BaseLongitude,
                CreatedAt = t.CreatedAt,
                TotalMembers = _context.RescueTeamMembers.Count(m => m.TeamId == t.TeamId),
                ActiveMembers = _context.RescueTeamMembers.Count(m => m.TeamId == t.TeamId && m.IsActive),
                Leader = _context.RescueTeamMembers
                    .Where(m => m.TeamId == t.TeamId && m.IsActive && m.MemberRole == "Leader")
                    .Join(
                        _context.Users,
                        m => m.UserId,
                        u => u.UserId,
                        (m, u) => new RescueTeamMemberInfoDto
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            FullName = u.FullName ?? string.Empty,
                            Email = u.Email ?? string.Empty,
                            Phone = u.Phone ?? string.Empty,
                            Role = u.Role,
                            MemberRole = m.MemberRole,
                            IsActive = m.IsActive,
                            RequestId = m.RequestId,
                            JoinedAt = m.JoinedAt
                        })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Count = teams.Count,
            Data = teams
        });
    }

    /// <summary>
    /// Lấy chi tiết một team, bao gồm danh sách thành viên.
    /// </summary>
    /// <remarks>
    /// Route: `GET /api/admin/rescue-teams/{teamId}`
    /// 
    /// Công dụng:
    /// - Xem full thông tin team.
    /// - Xem toàn bộ member trong team.
    /// - Có đánh dấu `MemberRole`, `IsActive`, `RequestId`.
    /// </remarks>
    [HttpGet("{teamId:int}")]
    public async Task<IActionResult> GetTeamById(int teamId)
    {
        var team = await BuildTeamDetailAsync(teamId);

        if (team == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy team." });
        }

        return Ok(new
        {
            Success = true,
            Data = team
        });
    }   
    /// <summary>
    /// Tạo team mới và gán leader ban đầu.
    /// </summary>
    /// <remarks>
    /// Route: `POST /api/admin/rescue-teams`
    /// 
    /// Công dụng:
    /// - Admin tạo team mới.
    /// - Leader ban đầu phải là `CITIZEN`.
    /// - Khi được thêm vào team, user sẽ đổi role thành `RESCUE_TEAM`.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateRescueTeamRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Message = "Dữ liệu gửi lên không hợp lệ." });
        }

        var teamName = request.TeamName.Trim();
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return BadRequest(new { Success = false, Message = "TeamName không được để trống." });
        }

        if (request.BaseLatitude.HasValue ^ request.BaseLongitude.HasValue)
        {
            return BadRequest(new { Success = false, Message = "Nếu nhập tọa độ thì phải nhập cả BaseLatitude và BaseLongitude." });
        }

        if (request.BaseLatitude.HasValue &&
            (request.BaseLatitude.Value < -90 || request.BaseLatitude.Value > 90))
        {
            return BadRequest(new { Success = false, Message = "BaseLatitude không hợp lệ." });
        }

        if (request.BaseLongitude.HasValue &&
            (request.BaseLongitude.Value < -180 || request.BaseLongitude.Value > 180))
        {
            return BadRequest(new { Success = false, Message = "BaseLongitude không hợp lệ." });
        }

        var leader = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.LeaderUserId);
        if (leader == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người được chọn làm leader." });
        }

        if (!leader.IsActive)
        {
            return BadRequest(new { Success = false, Message = "Người được chọn làm leader đang bị vô hiệu hóa." });
        }

        if (!string.Equals(leader.Role, "CITIZEN", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Success = false, Message = "Leader ban đầu phải là tài khoản CITIZEN." });
        }

        var hasActiveMembership = await _context.RescueTeamMembers
            .AnyAsync(m => m.UserId == leader.UserId && m.IsActive);

        if (hasActiveMembership)
        {
            return BadRequest(new { Success = false, Message = "Người được chọn đang thuộc một team khác." });
        }

        var memberUserIds = (request.MemberUserIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .Where(id => id != request.LeaderUserId)
            .ToList();

        var memberUsers = new List<User>();
        if (memberUserIds.Count > 0)
        {
            memberUsers = await _context.Users
                .Where(u => memberUserIds.Contains(u.UserId))
                .ToListAsync();

            if (memberUsers.Count != memberUserIds.Count)
            {
                return BadRequest(new { Success = false, Message = "Một hoặc nhiều thành viên được chọn không tồn tại." });
            }

            var invalidMember = memberUsers.FirstOrDefault(u => !u.IsActive || !string.Equals(u.Role, "CITIZEN", StringComparison.OrdinalIgnoreCase));
            if (invalidMember != null)
            {
                return BadRequest(new { Success = false, Message = "Chỉ có thể thêm các tài khoản CITIZEN đang hoạt động vào team." });
            }

            var memberIdsWithActiveTeam = await _context.RescueTeamMembers
                .Where(m => memberUserIds.Contains(m.UserId) && m.IsActive)
                .Select(m => new { m.UserId, m.TeamId })
                .ToListAsync();

            if (memberIdsWithActiveTeam.Any())
            {
                return BadRequest(new { Success = false, Message = "Một hoặc nhiều thành viên đang thuộc team khác." });
            }
        }

        string? resolvedAddress = !string.IsNullOrWhiteSpace(request.Address)
            ? request.Address.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(resolvedAddress) && request.BaseLatitude.HasValue && request.BaseLongitude.HasValue)
        {
            resolvedAddress = await _geocodingService.ReverseGeocodeAsync(
                request.BaseLatitude.Value,
                request.BaseLongitude.Value);
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var now = DateTime.UtcNow;

                    var team = new RescueTeam
                    {
                        TeamName = teamName,
                        Address = resolvedAddress,
                        BaseLatitude = request.BaseLatitude,
                        BaseLongitude = request.BaseLongitude,
                        CreatedAt = now
                    };

                    _context.RescueTeams.Add(team);
                    await _context.SaveChangesAsync();

                    _context.RescueTeamMembers.Add(new RescueTeamMember
                    {
                        TeamId = team.TeamId,
                        UserId = leader.UserId,
                        MemberRole = "Leader",
                        IsActive = true,
                        JoinedAt = now,
                        RequestId = null
                    });

                    leader.Role = "RESCUE_TEAM";

                    foreach (var memberUser in memberUsers)
                    {
                        _context.RescueTeamMembers.Add(new RescueTeamMember
                        {
                            TeamId = team.TeamId,
                            UserId = memberUser.UserId,
                            MemberRole = "Member",
                            IsActive = true,
                            JoinedAt = now,
                            RequestId = null
                        });

                        memberUser.Role = "RESCUE_TEAM";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var createdTeam = await BuildTeamDetailAsync(team.TeamId);

                    return Ok(new
                    {
                        Success = true,
                        Message = "Tạo team mới thành công.",
                        Data = createdTeam
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Không thể tạo team mới.",
                        Detail = ex.Message
                    });
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể tạo team mới.",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Thêm một thành viên vào team.
    /// </summary>
    /// <remarks>
    /// Route: `POST /api/admin/rescue-teams/{teamId}/members`
    /// 
    /// Công dụng:
    /// - Thêm citizen vào team.
    /// - Tự động đổi role user thành `RESCUE_TEAM`.
    /// - Thành viên đã inactive có thể được kích hoạt lại.
    /// </remarks>
    [HttpPost("{teamId:int}/members")]
    public async Task<IActionResult> AddMember(int teamId, [FromBody] AddRescueTeamMemberRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Message = "Dữ liệu gửi lên không hợp lệ." });
        }

        var teamExists = await _context.RescueTeams.AnyAsync(t => t.TeamId == teamId);
        if (!teamExists)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy team." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng." });
        }

        if (!user.IsActive)
        {
            return BadRequest(new { Success = false, Message = "Người dùng đang bị vô hiệu hóa." });
        }

        if (!string.Equals(user.Role, "CITIZEN", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Success = false, Message = "Chỉ có thể thêm tài khoản CITIZEN vào team." });
        }

        var activeMembership = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.UserId == user.UserId && m.IsActive);

        if (activeMembership != null)
        {
            if (activeMembership.TeamId == teamId)
            {
                return BadRequest(new { Success = false, Message = "Người dùng đã là thành viên active của team này." });
            }

            return BadRequest(new { Success = false, Message = "Người dùng đang thuộc một team khác." });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var now = DateTime.UtcNow;

                    var existingMembership = await _context.RescueTeamMembers
                        .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == user.UserId);

                    if (existingMembership == null)
                    {
                        _context.RescueTeamMembers.Add(new RescueTeamMember
                        {
                            TeamId = teamId,
                            UserId = user.UserId,
                            MemberRole = "Member",
                            IsActive = true,
                            JoinedAt = now,
                            RequestId = null
                        });
                    }
                    else
                    {
                        existingMembership.IsActive = true;
                        existingMembership.MemberRole = "Member";
                        existingMembership.JoinedAt = now;
                        existingMembership.RequestId = null;
                    }

                    user.Role = "RESCUE_TEAM";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedTeam = await BuildTeamDetailAsync(teamId);

                    return Ok(new
                    {
                        Success = true,
                        Message = "Thêm thành viên vào team thành công.",
                        Data = updatedTeam
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Không thể thêm thành viên vào team.",
                        Detail = ex.Message
                    });
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể thêm thành viên vào team.",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Bớt một thành viên khỏi team.
    /// </summary>
    /// <remarks>
    /// Route: `DELETE /api/admin/rescue-teams/{teamId}/members/{userId}`
    /// 
    /// Công dụng:
    /// - Xóa hẳn bản ghi thành viên khỏi bảng `rescue_team_members`.
    /// - Leader không được rời team.
    /// - Nếu user không còn active membership nào, role sẽ trở lại `CITIZEN`.
    /// </remarks>
    [HttpDelete("{teamId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int teamId, int userId)
    {
        var membership = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);

        if (membership == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy thành viên trong team." });
        }

        if (string.Equals(membership.MemberRole, "Leader", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Success = false, Message = "Leader không thể rời team." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng." });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.RescueTeamMembers.Remove(membership);

                    var hasOtherActiveMembership = await _context.RescueTeamMembers
                        .AnyAsync(m => m.UserId == userId && m.TeamId != teamId && m.IsActive);

                    if (!hasOtherActiveMembership && string.Equals(user.Role, "RESCUE_TEAM", StringComparison.OrdinalIgnoreCase))
                    {
                        user.Role = "CITIZEN";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedTeam = await BuildTeamDetailAsync(teamId);

                    return Ok(new
                    {
                        Success = true,
                        Message = "Đã bớt thành viên khỏi team.",
                        Data = updatedTeam
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Không thể bớt thành viên khỏi team.",
                        Detail = ex.Message
                    });
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể bớt thành viên khỏi team.",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Đổi leader của team sang một thành viên khác trong cùng team.
    /// </summary>
    /// <remarks>
    /// Route: `PUT /api/admin/rescue-teams/{teamId}/leader`
    /// 
    /// Công dụng:
    /// - Mỗi team chỉ giữ 1 leader.
    /// - Leader mới phải là thành viên active của team.
    /// - Leader cũ sẽ được hạ xuống Member.
    /// </remarks>
    [HttpPut("{teamId:int}/leader")]
    public async Task<IActionResult> ChangeLeader(int teamId, [FromBody] ChangeRescueTeamLeaderRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Message = "Dữ liệu gửi lên không hợp lệ." });
        }

        var teamExists = await _context.RescueTeams.AnyAsync(t => t.TeamId == teamId);
        if (!teamExists)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy team." });
        }

        var newLeader = await _context.RescueTeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == request.LeaderUserId);

        if (newLeader == null || !newLeader.IsActive)
        {
            return BadRequest(new { Success = false, Message = "Người được chọn phải là thành viên active của team." });
        }

        if (string.Equals(newLeader.MemberRole, "Leader", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Success = false, Message = "Người này đã là leader của team." });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var currentLeaders = await _context.RescueTeamMembers
                        .Where(m => m.TeamId == teamId && m.IsActive && m.MemberRole == "Leader")
                        .ToListAsync();

                    foreach (var leader in currentLeaders)
                    {
                        leader.MemberRole = "Member";
                    }

                    newLeader.MemberRole = "Leader";

                    var leaderUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.LeaderUserId);
                    if (leaderUser != null && !string.Equals(leaderUser.Role, "RESCUE_TEAM", StringComparison.OrdinalIgnoreCase))
                    {
                        leaderUser.Role = "RESCUE_TEAM";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedTeam = await BuildTeamDetailAsync(teamId);

                    return Ok(new
                    {
                        Success = true,
                        Message = "Đổi leader thành công.",
                        Data = updatedTeam
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Không thể đổi leader.",
                        Detail = ex.Message
                    });
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể đổi leader.",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Xóa hẳn một rescue team khỏi hệ thống.
    /// </summary>
    /// <remarks>
    /// Route: `DELETE /api/admin/rescue-teams/{teamId}`
    /// 
    /// Công dụng:
    /// - Xóa team cùng toàn bộ membership của team đó.
    /// - Chỉ cho xóa khi team không còn liên kết với rescue request hoặc rescue operation.
    /// - Nếu user không còn active membership nào khác, role sẽ trở lại `CITIZEN`.
    /// </remarks>
    [HttpDelete("{teamId:int}")]
    public async Task<IActionResult> DeleteTeam(int teamId)
    {
        var team = await _context.RescueTeams
            .FirstOrDefaultAsync(t => t.TeamId == teamId);

        if (team == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy team." });
        }

        var hasLinkedRequests = await _context.RescueRequests.AnyAsync(r => r.TeamId == teamId);
        if (hasLinkedRequests)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Không thể xóa team vì vẫn còn rescue request đang liên kết với team này. Hãy gỡ phân công trước."
            });
        }

        var hasLinkedOperations = await _context.RescueOperations.AnyAsync(o => o.TeamId == teamId);
        if (hasLinkedOperations)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Không thể xóa team vì vẫn còn rescue operation liên kết với team này. Hãy xử lý hoặc gỡ liên kết trước."
            });
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var memberships = await _context.RescueTeamMembers
                        .Where(m => m.TeamId == teamId)
                        .ToListAsync();

                    foreach (var membership in memberships)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == membership.UserId);
                        if (user != null)
                        {
                            var hasOtherActiveMembership = await _context.RescueTeamMembers
                                .AnyAsync(m => m.UserId == membership.UserId && m.TeamId != teamId && m.IsActive);

                            if (!hasOtherActiveMembership && string.Equals(user.Role, "RESCUE_TEAM", StringComparison.OrdinalIgnoreCase))
                            {
                                user.Role = "CITIZEN";
                            }
                        }
                    }

                    _context.RescueTeamMembers.RemoveRange(memberships);
                    _context.RescueTeams.Remove(team);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Success = true,
                        Message = "Xóa team thành công."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Không thể xóa team.",
                        Detail = ex.Message
                    });
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Không thể xóa team.",
                Detail = ex.Message
            });
        }
    }

    private async Task<RescueTeamDetailDto?> BuildTeamDetailAsync(int teamId)
    {
        var team = await _context.RescueTeams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TeamId == teamId);

        if (team == null)
        {
            return null;
        }

        var members = await _context.RescueTeamMembers
            .AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Join(
                _context.Users.AsNoTracking(),
                m => m.UserId,
                u => u.UserId,
                (m, u) => new RescueTeamMemberInfoDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Phone = u.Phone ?? string.Empty,
                    Role = u.Role,
                    MemberRole = m.MemberRole,
                    IsActive = m.IsActive,
                    RequestId = m.RequestId,
                    JoinedAt = m.JoinedAt
                })
            .OrderByDescending(m => m.MemberRole == "Leader" ? 1 : 0)
            .ThenBy(m => m.FullName)
            .ToListAsync();

        return new RescueTeamDetailDto
        {
            TeamId = team.TeamId,
            TeamName = team.TeamName,
            Address = team.Address,
            BaseLatitude = team.BaseLatitude,
            BaseLongitude = team.BaseLongitude,
            CreatedAt = team.CreatedAt,
            TotalMembers = members.Count,
            ActiveMembers = members.Count(m => m.IsActive),
            Leader = members.FirstOrDefault(m => m.IsActive && m.MemberRole == "Leader"),
            Members = members
        };
    }
}