namespace Flood_Rescue_Coordination.API.DTOs;
public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiration { get; set; }
    public UserInfo? User { get; set; }
}