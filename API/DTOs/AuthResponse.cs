using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiration { get; set; }
    public UserInfo? User { get; set; }
}

/// <summary>
/// Request gửi OTP (Dùng cho cả Quên mật khẩu & Đổi mật khẩu)
/// </summary>
public class SendOtpRequest
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^(?:\+84|84|0)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Request đặt lại mật khẩu (Quên mật khẩu)
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP là bắt buộc")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Mật khẩu phải từ 5 đến 100 ký tự")]
    public string NewPassword { get; set; } = string.Empty;
}