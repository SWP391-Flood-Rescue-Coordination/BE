namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface xác thực OTP qua SMS.
/// Dùng mã OTP cố định 123456 cho môi trường test/deploy.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Gửi mã xác thực OTP (One Time Password) tới số điện thoại người dùng thông qua hệ thống SMS.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại nhận tin nhắn (định dạng chuẩn quốc tế, ví dụ: +849...).</param>
    /// <returns>True nếu hệ thống SMS gửi tin nhắn đi thành công; ngược lại là False.</returns>
    Task<bool> SendOtpAsync(string phoneNumber);

    /// <summary>
    /// Kiểm tra tính hợp lệ của mã OTP mà người dùng cung cấp so với mã đã gửi qua SMS.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại gắn liền với mã OTP cần kiểm tra.</param>
    /// <param name="otp">Chuỗi mã OTP mà người dùng nhập vào.</param>
    /// <returns>True nếu mã khớp và vẫn còn trong thời gian hiệu lực; ngược lại là False.</returns>
    Task<bool> VerifyOtpAsync(string phoneNumber, string otp);
}
