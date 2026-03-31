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

    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<bool> SendOtpAsync(string phoneNumber)
    {
        // Log ra console để biết API đã chạy
        _logger.LogWarning("------------------------------------------");
        _logger.LogWarning("API Quên mật khẩu được gọi cho: {Phone}", phoneNumber);
        _logger.LogWarning("Mã OTP TEST (Dùng để nhập): {Otp}", MagicOtp);
        _logger.LogWarning("------------------------------------------");

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> VerifyOtpAsync(string phoneNumber, string otp)
    {
        // Chấp nhận mã "123456" cho tất cả các số điện thoại
        if (otp.Trim() == MagicOtp)
        {
            _logger.LogInformation("Xác thực THÀNH CÔNG với mã OTP TEST cho {Phone}", phoneNumber);
            return Task.FromResult(true);
        }

        _logger.LogWarning("Xác thực THẤT BẠI cho {Phone}: Mã nhập vào '{Input}' không khớp mã TEST", phoneNumber, otp);
        return Task.FromResult(false);
    }
}
