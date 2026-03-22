using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// Request gửi OTP về số điện thoại (bước 1 – Quên mật khẩu)
/// </summary>
public class SendOtpRequest
{
    /// <summary>Số điện thoại đã đăng ký (VD: 0912345678)</summary>
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^(?:\+84|84|0)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Request đặt lại mật khẩu (bước 2 – xác thực OTP + đổi password)
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>Số điện thoại đã nhận OTP</summary>
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^(?:\+84|84|0)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Mã OTP 6 chữ số nhận qua SMS</summary>
    [Required(ErrorMessage = "OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 4, ErrorMessage = "OTP phải từ 4-6 ký tự")]
    public string Otp { get; set; } = string.Empty;

    /// <summary>Mật khẩu mới (tối thiểu 5 ký tự)</summary>
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Mật khẩu phải từ 5 đến 100 ký tự")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response chung cho các thao tác OTP
/// </summary>
public class OtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
