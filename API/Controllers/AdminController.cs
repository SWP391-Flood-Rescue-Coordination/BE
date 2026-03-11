using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách người dùng hoặc tìm kiếm theo ID
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int? userId = null)
    {
        var query = _context.Users.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(u => u.UserId == userId.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Phone = u.Phone,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Success = true, Total = users.Count, Data = users });
    }

    /// <summary>
    /// Lấy danh sách các role hiện có trong hệ thống
    /// </summary>
    [HttpGet("roles")]
    public IActionResult GetAvailableRoles()
    {
        var roles = new List<string> { "ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN" };
        return Ok(new { Success = true, Data = roles });
    }

    /// <summary>
    /// Cập nhật role cho người dùng
    /// </summary>
    [HttpPut("users/{id}/role")]
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
    /// Kích hoạt hoặc vô hiệu hóa tài khoản người dùng
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng" });
        }

        // Không cho phép admin tự vô hiệu hóa chính mình (để tránh mất quyền truy cập hệ thống)
        // Lưu ý: Cần lấy UserId của người đang đăng nhập từ Token
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
