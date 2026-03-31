using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flood_Rescue_Coordination.API.Controllers;

/// <summary>
/// AuthController: Quản lý các tiến trình xác thực người dùng.
/// Cung cấp các Endpoint cho Đăng nhập, Đăng ký, Quản lý Token và Quên mật khẩu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Constructor khởi tạo AuthController.
    /// Inject IAuthService để xử lý logic nghiệp vụ liên quan đến xác thực.
    /// </summary>
    /// <param name="authService">Dịch vụ xác thực được đăng ký trong hệ thống DI.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// API Đăng nhập người dùng.
    /// Kiểm tra thông tin tài khoản và mật khẩu, trả về Access Token và Refresh Token nếu thành công.
    /// </summary>
    /// <param name="request">Thông tin đăng nhập gồm username/email và password.</param>
    /// <returns>AuthResponse chứa Token hoặc thông báo lỗi.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Gọi nghiệp vụ đăng nhập từ AuthService
        var response = await _authService.LoginAsync(request);
        
        // Nếu đăng nhập thất bại (sai tài khoản, mật khẩu, hoặc tài khoản bị khóa)
        if (!response.Success)
        {
            return Unauthorized(response);
        }

        // Đăng nhập thành công, trả về thông tin token
        return Ok(response);
    }

    /// <summary>
    /// API Đăng ký tài khoản người dùng mới.
    /// Tiếp nhận thông tin, kiểm tra tính hợp lệ và lưu vào cơ sở dữ liệu.
    /// </summary>
    /// <param name="request">Thông tin đăng ký của người dùng.</param>
    /// <returns>AuthResponse xác nhận trạng thái đăng ký.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Gọi AuthService để xử lý logic đăng ký (validate, hash pass, lưu DB)
        var response = await _authService.RegisterAsync(request);
        
        // Nếu có lỗi (email/username đã tồn tại, dữ liệu không hợp lệ)
        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Đăng ký thành công
        return Ok(response);
    }

    /// <summary>
    /// API Làm mới Access Token khi token cũ hết hạn bằng cách sử dụng Refresh Token.
    /// Giúp duy trì phiên đăng nhập của người dùng mà không cần nhập lại mật khẩu.
    /// </summary>
    /// <param name="refreshToken">Refresh Token hợp lệ đã được cấp trước đó.</param>
    /// <returns>Cặp Access Token và Refresh Token mới.</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        // Kiểm tra và cấp lại token mới từ AuthService
        var response = await _authService.RefreshTokenAsync(refreshToken);
        
        // Nếu Refresh Token không hợp lệ hoặc đã hết hạn
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API Đăng xuất người dùng. 
    /// Thu hồi Access Token hiện tại và vô hiệu hóa Refresh Token (nếu có).
    /// </summary>
    /// <param name="refreshToken">Refresh Token gửi kèm để xóa khỏi hệ thống.</param>
    /// <returns>Trạng thái đăng xuất thành công hay thất bại.</returns>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string? refreshToken)
    {
        // Trích xuất Access Token từ Header Authorization
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        
        // Gọi dịch vụ để xử lý việc đăng xuất và vô hiệu hóa token
        var response = await _authService.LogoutAsync(accessToken, refreshToken);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// API Lấy thông tin chi tiết của người dùng đang đăng nhập thông qua Identity Token.
    /// Sử dụng các Claims được đính kèm trong JWT để xác định danh tính.
    /// </summary>
    /// <returns>Thông tin cơ bản của user hiện tại.</returns>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Lấy thông tin từ các Claims trong Identity User
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var fullName = User.FindFirst("fullName")?.Value;

        // Trả về DTO UserInfo chứa các thông tin cần thiết cho Frontend
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
                IsActive = true // Mặc định là true nếu token còn hiệu lực
            }
        });
    }

    // =============================================
    // QUÊN MẬT KHẨU (FORGOT PASSWORD)
    // =============================================

    /// <summary>
    /// QUÊN MẬT KHẨU - Bước 1: Gửi mã OTP.
    /// Kiểm tra email có tồn tại trong hệ thống không và gửi mã xác nhận.
    /// </summary>
    /// <param name="request">Chứa Email cần lấy lại mật khẩu.</param>
    /// <returns>Thông báo trạng thái gửi OTP.</returns>
    [AllowAnonymous]
    [HttpPost("forgot-password/send-otp")]
    public async Task<IActionResult> SendForgotPasswordOtp([FromBody] SendOtpRequest request)
    {
        // Xử lý logic gửi email OTP
        var response = await _authService.SendForgotPasswordOtpAsync(request);
        
        // Nếu email không tồn tại hoặc lỗi gửi mail
        if (!response.Success) return BadRequest(response);
        
        return Ok(response);
    }

    /// <summary>
    /// QUÊN MẬT KHẨU - Bước 2: Đặt lại mật khẩu.
    /// Xác thực mã OTP người dùng cung cấp và cập nhật mật khẩu mới vào DB.
    /// </summary>
    /// <param name="request">Chứa Email, mã OTP và Mật khẩu mới.</param>
    /// <returns>Thông báo cập nhật mật khẩu thành công hay thất bại.</returns>
    [AllowAnonymous]
    [HttpPost("forgot-password/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // Xác minh OTP và đổi mật khẩu
        var response = await _authService.ResetPasswordWithOtpAsync(request);
        
        // Nếu OTP sai, hết hạn hoặc lỗi database
        if (!response.Success) return BadRequest(response);
        
        return Ok(response);
    }
}