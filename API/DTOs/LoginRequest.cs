using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;
public class LoginRequest
{
    [Required(ErrorMessage = "Username là bắt buộc")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password là bắt buộc")]
    public string Password { get; set; } = string.Empty;
}