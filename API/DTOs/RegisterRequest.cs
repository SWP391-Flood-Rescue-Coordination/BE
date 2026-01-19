using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;
public class RegisterRequest
{
    [Required(ErrorMessage = "Username là bắt buộc")]
    [MinLength(3, ErrorMessage = "Username phải có ít nhất 3 ký tự")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password là bắt buộc")]
    [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    public string FullName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}