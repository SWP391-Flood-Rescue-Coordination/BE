using Flood_Rescue_Coordination.API.DTOs;
namespace Flood_Rescue_Coordination.API.Services;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken);
    Task<bool> IsTokenBlacklistedAsync(string token);
}