using Flood_Rescue_Coordination.API.Models;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
}