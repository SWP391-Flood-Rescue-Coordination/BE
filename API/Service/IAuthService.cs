using Flood_Rescue_Coordination.API.DTOs;

namespace Flood_Rescue_Coordination.API.Services;

public interface IAuthService
{
    /// <summary>
    /// Thực hiện đăng nhập, xác thực thông tin và cấp phát JWT Token.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Đăng ký tài khoản mới cho công dân (Citizen), cấp JWT Token nếu hợp lệ.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Làm mới (Refresh) Access Token dựa vào Refresh Token hợp lệ.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Đăng xuất người dùng, vô hiệu hóa (Blacklist) Access Token hiện tại và Refresh Token.
    /// </summary>
    Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken);

    /// <summary>Gửi OTP về số điện thoại để xác thực quên mật khẩu.</summary>
    Task<OtpResponse> SendForgotPasswordOtpAsync(SendOtpRequest request);

    /// <summary>Xác thực OTP và đặt lại mật khẩu mới.</summary>
    Task<OtpResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request);
}