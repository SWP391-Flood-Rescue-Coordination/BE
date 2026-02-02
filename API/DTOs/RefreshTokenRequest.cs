using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}