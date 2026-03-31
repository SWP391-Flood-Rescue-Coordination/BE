namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cho dịch vụ gửi Email (sử dụng cho OTP, thông báo...)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi mã xác thực OTP qua Email cho người dùng để phục hồi mật khẩu hoặc xác minh tài khoản.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email đích của người nhận</param>
    /// <param name="userName">Tên hiển thị của người dùng trong nội dung email</param>
    /// <param name="otp">Chuỗi mã OTP gồm các chữ số</param>
    /// <returns>True nếu email được gửi đi thành công; ngược lại là False.</returns>
    Task<bool> SendOtpEmailAsync(string toEmail, string userName, string otp);
}
