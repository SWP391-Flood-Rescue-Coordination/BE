using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// MockSmsService: Dịch vụ giả lập gửi tin nhắn SMS để kiểm thử hoặc chạy trên môi trường Serverless (Vercel).
/// Thay vì gửi tin nhắn thật tốn phí, nó sẽ log mã OTP cố định ra Console/Log hệ thống.
/// </summary>
public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;
    
    // Mã OTP cố định để test mọi lúc mọi nơi, không cần lưu Cache
    private const string MagicOtp = "123456"; 

    /// <summary>
    /// Constructor khởi tạo MockSmsService.
    /// </summary>
    /// <param name="logger">Logger để ghi lại thông tin mã OTP giả lập.</param>
    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Giả lập việc gửi mã OTP. Trong thực tế, mã này sẽ được gửi tới điện thoại người dùng.
    /// Ở bản Mock này, chúng ta chỉ in mã "123456" ra màn hình Log.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại nhận tin.</param>
    /// <returns>Luôn trả về True.</returns>
    public Task<bool> SendOtpAsync(string phoneNumber)
    {
        // 1. Ghi log cảnh báo nổi bật để nhà phát triển biết mã OTP đang dùng là gì
        _logger.LogWarning("------------------------------------------");
        _logger.LogWarning("API Quên mật khẩu được gọi cho số: {Phone}", phoneNumber);
        _logger.LogWarning("Mã OTP GIẢ LẬP (Dùng để nhập): {Otp}", MagicOtp);
        _logger.LogWarning("------------------------------------------");

        // 2. Trả về kết quả hoàn thành ngay lập tức
        return Task.FromResult(true);
    }

    /// <summary>
    /// Kiểm tra xem mã OTP người dùng nhập vào có khớp với mã giả lập (123456) hay không.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại thực hiện xác thực.</param>
    /// <param name="otp">Mã OTP đầu vào từ người dùng.</param>
    /// <returns>True nếu mã là "123456"; ngược lại là False.</returns>
    public Task<bool> VerifyOtpAsync(string phoneNumber, string otp)
    {
        // 1. So sánh mã nhập vào với mã MagicOtp sau khi đã loại bỏ khoảng trắng thừa
        if (otp.Trim() == MagicOtp)
        {
            _logger.LogInformation("Xác thực THÀNH CÔNG với mã OTP GIẢ LẬP cho số {Phone}", phoneNumber);
            return Task.FromResult(true);
        }

        // 2. Log ra nếu người dùng nhập sai mã để hỗ trợ debug
        _logger.LogWarning("Xác thực THẤT BẠI cho {Phone}: Mã nhập vào '{Input}' không khớp mã GIẢ LẬP", phoneNumber, otp);
        return Task.FromResult(false);
    }
}
