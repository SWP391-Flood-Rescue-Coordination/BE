using Flood_Rescue_Coordination.API.Models;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetTokenExpiration(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}