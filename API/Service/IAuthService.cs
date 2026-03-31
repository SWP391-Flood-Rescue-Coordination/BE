using Flood_Rescue_Coordination.API.DTOs;

namespace Flood_Rescue_Coordination.API.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken);

    /// <summary>Gửi mã OTP về email để phục hồi mật khẩu.</summary>
    Task<AuthResponse> SendForgotPasswordOtpAsync(SendOtpRequest request);

    /// <summary>Xác thực OTP và đặt lại mật khẩu mới khi quên.</summary>
    Task<AuthResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request);
}