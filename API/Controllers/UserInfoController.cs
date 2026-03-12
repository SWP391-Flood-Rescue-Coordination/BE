using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UserInfoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserInfoController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Admin - Lấy danh sách người dùng hoặc tìm kiếm theo ID
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] int? userId = null)
    {
        var query = _context.Users.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(u => u.UserId == userId.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserInfo
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Phone = u.Phone ?? string.Empty,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = users.Count, Data = users });
    }

    /// <summary>
    /// Admin - Lấy danh sách các role hiện có trong hệ thống
    /// </summary>
    [HttpGet("roles")]
    public IActionResult GetAvailableRoles()
    {
        var roles = new List<string> { "ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN" };
        return Ok(new { Success = true, Data = roles });
    }

    /// <summary>
    /// Admin - Cập nhật role cho người dùng
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng" });
        }

        var validRoles = new List<string> { "ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN" };
        if (!validRoles.Contains(request.Role.ToUpper()))
        {
            return BadRequest(new { Success = false, Message = "Role không hợp lệ" });
        }

        user.Role = request.Role.ToUpper();
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = $"Đã cập nhật role cho người dùng {user.Username} thành {user.Role}" });
    }

    /// <summary>
    /// Admin - Kích hoạt hoặc vô hiệu hóa tài khoản người dùng
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng" });
        }

        // Không cho phép admin tự vô hiệu hóa chính mình
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id && !request.IsActive)
        {
            return BadRequest(new { Success = false, Message = "Admin không thể tự vô hiệu hóa tài khoản của chính mình" });
        }

        user.IsActive = request.IsActive;
        await _context.SaveChangesAsync();

        string statusLabel = request.IsActive ? "kích hoạt" : "vô hiệu hóa";
        return Ok(new { Success = true, Message = $"Đã {statusLabel} tài khoản {user.Username}" });
    }
}
