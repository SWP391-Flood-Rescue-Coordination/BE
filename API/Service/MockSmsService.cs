using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// SMS Service dùng cho môi trường Test/Deploy (Vercel).
/// Sử dụng mã OTP cố định để tránh lỗi mất bộ nhớ trên Serverless.
/// </summary>
public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;
    
    // Mã OTP cố định để test mọi lúc mọi nơi, không cần lưu Cache
    private const string MagicOtp = "123456"; 

    /// <summary>
    /// Khởi tạo MockSmsService.
    /// </summary>
    /// <param name="logger">Sử dụng ILogger để ghi nhật ký ra Console thay vì gửi tin nhắn thật.</param>
    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Giả lập việc gửi mã OTP tới một số điện thoại.
    /// Thay vì gửi SMS thực qua mạng viễn thông, hệ thống sẽ log mã OTP ra màn hình console để lập trình viên sử dụng.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại người nhận.</param>
    /// <returns>Luôn luôn trả về True trong môi trường Mock.</returns>
    public Task<bool> SendOtpAsync(string phoneNumber)
    {
        // Ghi lại thông báo nổi bật trong log Console để dễ dàng lấy mã OTP khi test
        _logger.LogWarning("------------------------------------------");
        _logger.LogWarning("Đang giả lập gửi OTP cho SĐT: {Phone}", phoneNumber);
        _logger.LogWarning("MÃ OTP DÙNG ĐỂ TEST LÀ: {Otp}", MagicOtp);
        _logger.LogWarning("------------------------------------------");

        return Task.FromResult(true);
    }

    /// <summary>
    /// Giả lập việc xác thực mã OTP do người dùng nhập vào.
    /// Trong môi trường Test, hệ thống chấp nhận mã mặc định '123456' cho tất cả mọi số điện thoại.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại cần xác thực.</param>
    /// <param name="otp">Mã OTP mà người dùng nhập vào Form.</param>
    /// <returns>True nếu trùng với mã MagicOtp (123456), ngược lại False.</returns>
    public Task<bool> VerifyOtpAsync(string phoneNumber, string otp)
    {
        // Kiểm tra xem mã nhập vào có khớp với mã '123456' cố định không
        if (otp.Trim() == MagicOtp)
        {
            _logger.LogInformation("Xác thực THÀNH CÔNG với mã OTP TEST cho {Phone}", phoneNumber);
            return Task.FromResult(true);
        }

        // Nếu mã nhập sai, ghi lại cảnh báo trong log
        _logger.LogWarning("Xác thực THẤT BẠI cho {Phone}: Mã nhập vào '{Input}' không khớp mã mặc định {Magic}", phoneNumber, otp, MagicOtp);
        return Task.FromResult(false);
    }
}
