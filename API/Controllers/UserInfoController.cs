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
    /// Admin - Lấy danh sách người dùng.
    /// Hỗ trợ chuẩn hóa search: ?searchBy=userId&keyword=123
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? searchBy = null, [FromQuery] string? keyword = null)
    {
        var query = _context.Users.AsQueryable();

        // Chuẩn hóa search backend: Mỗi trang chỉ tìm theo 1 field đúng mục đích nghiệp vụ
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Whitelist các trường được phép search cho endpoint này
            var allowedFields = new[] { "userId", "username", "fullName", "email", "phone" };
            if (!allowedFields.Contains(searchBy))
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Trường tìm kiếm '{searchBy}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedFields)}" 
                });
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                switch (searchBy)
                {
                    case "userId":
                        if (int.TryParse(keyword, out int id))
                            query = query.Where(u => u.UserId == id);
                        else
                            return BadRequest(new { Success = false, Message = "Id người dùng phải là số." });
                        break;
                    case "username":
                        query = query.Where(u => u.Username.Contains(keyword));
                        break;
                    case "fullName":
                        query = query.Where(u => u.FullName != null && u.FullName.Contains(keyword));
                        break;
                    case "email":
                        query = query.Where(u => u.Email != null && u.Email.Contains(keyword));
                        break;
                    case "phone":
                        query = query.Where(u => u.Phone != null && u.Phone.Contains(keyword));
                        break;
                }
            }
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

        // 1. Không cho phép admin tự thay đổi role của chính mình
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
        {
            return BadRequest(new { Success = false, Message = "Admin không thể tự thay đổi role của chính mình" });
        }

        // 2. Không cho phép thay đổi role của người đang là Admin hoặc Manager
        if (user.Role == "ADMIN" || user.Role == "MANAGER")
        {
            return BadRequest(new { Success = false, Message = "Admin không thể thay đổi role của người dùng có quyền Admin hoặc Manager" });
        }

        // 3. Không cho phép thay đổi role thành Admin hoặc Manager
        string newRole = request.Role.ToUpper();
        if (newRole == "ADMIN" || newRole == "MANAGER")
        {
            return BadRequest(new { Success = false, Message = "Admin không thể cấp quyền Admin hoặc Manager cho người dùng" });
        }

        var validRoles = new List<string> { "ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN" };
        if (!validRoles.Contains(newRole))
        {
            return BadRequest(new { Success = false, Message = "Role không hợp lệ" });
        }

        user.Role = newRole;
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
