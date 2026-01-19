using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flood_Rescue_Coordination.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        
        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }



    /// <summary>
    /// Đăng xuất
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        var accessToken = Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Access token không được cung cấp"
            });
        }

        var response = await _authService.LogoutAsync(accessToken, request?.RefreshToken);
        return Ok(response);
    }

    /// <summary>
    /// Lấy thông tin user hiện tại
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var fullName = User.FindFirst("fullName")?.Value;

        return Ok(new
        {
            Success = true,
            User = new UserInfo
            {
                UserId = int.Parse(userId ?? "0"),
                Username = username ?? string.Empty,
                FullName = fullName ?? string.Empty,
                Email = email ?? string.Empty,
                Role = role ?? string.Empty
            }
        });
    }


}