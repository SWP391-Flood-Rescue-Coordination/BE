using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;
public class LoginRequest
{
    [Required(ErrorMessage = "Hãy nhập số điện thoại")]
    [RegularExpression(@"^(?:\+84|84|0)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Password là bắt buộc")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Password phải từ 6 đến 100 ký tự")]
    public string Password { get; set; } = string.Empty;
}
