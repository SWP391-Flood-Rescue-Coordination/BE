using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;
public class RegisterRequest
{
    
    [Required(ErrorMessage = "Password là bắt buộc")]
    [MinLength(5, ErrorMessage = "Password phải có ít nhất 5 ký tự")]
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