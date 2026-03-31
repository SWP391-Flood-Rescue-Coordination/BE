namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cho dịch vụ gửi Email (sử dụng cho OTP, thông báo...)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi mã xác thực OTP qua Email cho người dùng
    /// </summary>
    /// <param name="toEmail">Địa chỉ email nhận</param>
    /// <param name="userName">Tên người dùng (hiển thị trong mail)</param>
    /// <param name="otp">Mã OTP</param>
    /// <returns>True nếu gửi thành công qua Resend API</returns>
    Task<bool> SendOtpEmailAsync(string toEmail, string userName, string otp);
}
