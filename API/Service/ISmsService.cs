namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface xác thực OTP qua SMS.
/// Dùng mã OTP cố định 123456 cho môi trường test/deploy.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Gửi OTP tới số điện thoại thông qua SMS.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại VN dạng +84xxxxxxxxx</param>
    /// <returns>true nếu gửi thành công</returns>
    Task<bool> SendOtpAsync(string phoneNumber);

    /// <summary>
    /// Xác thực OTP người dùng nhập.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại VN dạng +84xxxxxxxxx</param>
    /// <param name="otp">Mã OTP người dùng nhập</param>
    /// <returns>true nếu OTP đúng và còn hiệu lực</returns>
    Task<bool> VerifyOtpAsync(string phoneNumber, string otp);
}
