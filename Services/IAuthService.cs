using Flood_Rescue_Coordination.Models;
using Flood_Rescue_Coordination.Models.DTOs;

namespace Flood_Rescue_Coordination.Services
{
    public interface IAuthService
    {
        AuthResponse Login(LoginRequest request);
        AuthResponse Register(RegisterRequest request);
        string GenerateJwtToken(User user);
    }
}   