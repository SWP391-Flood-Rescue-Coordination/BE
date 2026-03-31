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

    /// <summary>
    /// Constructor khởi tạo AuthController với dịch vụ xác thực.
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// API Đăng nhập cho người dùng.
    /// Trả về Access Token và Refresh Token nếu thông tin chính xác.
    /// </summary>
    /// <param name="request">Request chứa Phone và Password.</param>
    /// <returns>AuthResponse với thông tin mã Token.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        
        if (!response.Success)
        {
            // Trả về 401 Unauthorized nếu đăng nhập thất bại
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API Đăng ký tài khoản mới cho công dân (CITIZEN).
    /// </summary>
    /// <param name="request">Thông tin đăng ký (FullName, Email, Phone, Password).</param>
    /// <returns>AuthResponse chứa thông tin tài khoản vừa tạo và Token đăng nhập tự động.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        
        if (!response.Success)
        {
            // Trả về 400 Bad Request nếu dữ liệu không hợp lệ hoặc bị trùng (Email/Phone)
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API làm mới Access Token khi nó đã hết hạn.
    /// </summary>
    /// <param name="refreshToken">Chuỗi Refresh Token do phía Frontend lưu trữ.</param>
    /// <returns>Cặp Access Token và Refresh Token mới.</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        var response = await _authService.RefreshTokenAsync(refreshToken);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API Đăng xuất tài khoản.
    /// Vô hiệu hóa Access Token và thu hồi Refresh Token.
    /// </summary>
    /// <param name="refreshToken">Refresh Token cần thu hồi (tùy chọn).</param>
    /// <returns>Thông báo đăng xuất thành công.</returns>
    [Authorize] // Yêu cầu người dùng phải đang đăng nhập
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string? refreshToken)
    {
        // Trích xuất Access Token từ Header Authorization
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var response = await _authService.LogoutAsync(accessToken, refreshToken);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API Lấy thông tin chi tiết của người dùng đang đăng nhập dựa trên Token.
    /// </summary>
    /// <returns>Đối tượng UserInfo chứa thông tin cơ bản của người dùng.</returns>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Đọc các Claims đã được JwtMiddleware/Authentication trích xuất từ Token
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
                Phone = User.FindFirst(System.Security.Claims.ClaimTypes.MobilePhone)?.Value ?? User.FindFirst("phone")?.Value ?? string.Empty,
                Role = role ?? string.Empty,
                IsActive = true 
            }
        });
    }

    // =============================================
    // QUÊN MẬT KHẨU – OTP
    // =============================================

    /// <summary>
    /// Bước 1 trong Quên mật khẩu: Yêu cầu gửi mã OTP tới Số điện thoại.
    /// </summary>
    /// <remarks>
    /// Flow: Nhập số điện thoại → Backend kiểm tra tồn tại → Giả định gửi OTP
    /// Mã OTP test mặc định: 123456
    /// </remarks>
    [HttpPost("forgot-password/send-otp")]
    public async Task<IActionResult> SendForgotPasswordOtp([FromBody] SendOtpRequest request)
    {
        var response = await _authService.SendForgotPasswordOtpAsync(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Bước 2 trong Quên mật khẩu: Xác nhận mã OTP và đặt lại mật khẩu mới.
    /// </summary>
    /// <remarks>
    /// Flow: Nhập số điện thoại + OTP + mật khẩu mới → Xác thực OTP → Cập nhật mật khẩu
    /// </remarks>
    [HttpPost("forgot-password/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordWithOtpAsync(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}