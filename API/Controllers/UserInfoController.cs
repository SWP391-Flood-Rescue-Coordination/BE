using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// UserInfoController: Cung cấp các chức năng quản trị người dùng dành riêng cho ADMIN.
/// Bao gồm: Liệt kê, tìm kiếm người dùng, thay đổi phân quyền (Role) và quản lý trạng thái tài khoản.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UserInfoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructor khởi tạo UserInfoController với DbContext.
    /// </summary>
    public UserInfoController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// ADMIN - Lấy danh sách toàn bộ người dùng trong hệ thống.
    /// Hỗ trợ tìm kiếm linh hoạt theo UserId, Username, Họ tên, Email hoặc Số điện thoại.
    /// </summary>
    /// <param name="searchBy">Trường cần tìm kiếm (userId, username, fullName, email, phone).</param>
    /// <param name="keyword">Từ khóa tìm kiếm tương ứng.</param>
    /// <returns>Danh sách người dùng được sắp xếp theo thời gian tạo mới nhất.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? searchBy = null, [FromQuery] string? keyword = null)
    {
        var query = _context.Users.AsQueryable();

        // 1. Xử lý Logic tìm kiếm (Search Backend)
        if (!string.IsNullOrWhiteSpace(searchBy))
        {
            // Kiểm tra tính hợp lệ của trường tìm kiếm (Whitelist)
            var allowedFields = new[] { "userId", "username", "fullName", "email", "phone" };
            if (!allowedFields.Contains(searchBy))
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Trường tìm kiếm '{searchBy}' không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", allowedFields)}" 
                });
            }

            // Nếu có từ khóa, bắt đầu lọc Query
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                switch (searchBy)
                {
                    case "userId":
                        if (int.TryParse(keyword, out int id))
                            query = query.Where(u => u.UserId == id);
                        else
                            return BadRequest(new { Success = false, Message = "Id người dùng phải là một số nguyên hợp lệ." });
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

        // 2. Thực thi truy vấn, sắp xếp và Map sang định dạng kết quả (UserInfo DTO)
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
                TeamId = _context.RescueTeamMembers
                    .Where(m => m.UserId == u.UserId && m.IsActive)
                    .Select(m => (int?)m.TeamId)
                    .FirstOrDefault(),
                TeamName = _context.RescueTeamMembers
                    .Where(m => m.UserId == u.UserId && m.IsActive)
                    .Select(m => m.Team != null ? m.Team.TeamName : null)
                    .FirstOrDefault(),
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
    /// ADMIN - Thay đổi quyền hạn (Role) của một người dùng cụ thể.
    /// Có các ràng buộc bảo mật nghiêm ngặt để tránh việc lạm quyền hoặc tự hạ quyền.
    /// </summary>
    /// <param name="id">ID người dùng cần thay đổi.</param>
    /// <param name="request">Role mới cần gán.</param>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        // 1. Tìm người dùng
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng trong hệ thống." });
        }

        // 2. Bảo mật: Không cho phép Admin tự thay đổi Role của chính mình thông qua API này
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
        {
            return BadRequest(new { Success = false, Message = "Admin không thể tự thay đổi quyền hạn của chính mình." });
        }

        // 3. Ràng buộc: Không cho phép thay đổi Role của những tài khoản đang có quyền Admin hoặc Manager
        // (Để bảo vệ tầng quản trị cao nhất, việc thay đổi các role này cần can thiệp trực tiếp DB hoặc quy trình khác)
        if (user.Role == "ADMIN" || user.Role == "MANAGER")
        {
            return BadRequest(new { Success = false, Message = "Không thể thay đổi quyền hạn của tài khoản Admin hoặc Manager khác." });
        }

        // 4. Ràng buộc: Không cho phép Admin cấp quyền Admin hoặc Manager cho người khác thông qua API này
        string newRole = request.Role.ToUpper();
        if (newRole == "ADMIN" || newRole == "MANAGER")
        {
            return BadRequest(new { Success = false, Message = "Admin không có quyền cấp Role Admin hoặc Manager cho người dùng thông qua chức năng này." });
        }

        // 5. Kiểm tra tính hợp lệ của Role mới
        var validRoles = new List<string> { "ADMIN", "COORDINATOR", "MANAGER", "RESCUE_TEAM", "CITIZEN" };
        if (!validRoles.Contains(newRole))
        {
            return BadRequest(new { Success = false, Message = "Tên quyền (Role) không hợp lệ." });
        }

        // 6. Cập nhật và lưu
        user.Role = newRole;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = $"Đã cập nhật quyền hạn cho người dùng '{user.Username}' thành '{user.Role}'." });
    }

    /// <summary>
    /// ADMIN - Kích hoạt hoặc Vô hiệu hóa tài khoản người dùng.
    /// Tài khoản bị vô hiệu hóa sẽ không thể đăng nhập vào hệ thống.
    /// </summary>
    /// <param name="id">ID người dùng.</param>
    /// <param name="request">Trạng thái IsActive mới.</param>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        // 1. Tìm người dùng
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Không tìm thấy người dùng." });
        }

        // 2. Bảo mật: Admin không được phép tự vô hiệu hóa tài khoản của chính mình
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id && !request.IsActive)
        {
            return BadRequest(new { Success = false, Message = "Admin không thể tự vô hiệu hóa tài khoản của chính mình." });
        }

        // 3. Cập nhật và lưu
        user.IsActive = request.IsActive;
        await _context.SaveChangesAsync();

        string statusLabel = request.IsActive ? "Kích hoạt" : "Vô hiệu hóa";
        return Ok(new { Success = true, Message = $"Đã {statusLabel} tài khoản '{user.Username}' thành công." });
    }
}
