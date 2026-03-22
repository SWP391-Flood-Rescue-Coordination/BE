using Flood_Rescue_Coordination.API.DTOs;

namespace Flood_Rescue_Coordination.API.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken);

    /// <summary>Gửi OTP về số điện thoại để xác thực quên mật khẩu.</summary>
    Task<OtpResponse> SendForgotPasswordOtpAsync(SendOtpRequest request);

    /// <summary>Xác thực OTP và đặt lại mật khẩu mới.</summary>
    Task<OtpResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request);
}