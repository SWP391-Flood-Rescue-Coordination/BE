using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Dịch vụ gửi Email sử dụng Resend.com API
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gửi OTP thông qua Email dùng Resend REST API (không cần thêm SDK)
    /// </summary>
    public async Task<bool> SendOtpEmailAsync(string toEmail, string userName, string otp)
    {
        var apiKey = _configuration["ResendSettings:ApiKey"];
        var fromEmail = _configuration["ResendSettings:FromEmail"] ?? "onboarding@resend.dev";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Chuẩn bị payload dữ liệu gửi tới Resend API
        var emailData = new
        {
            from = $"Flood Rescue <{fromEmail}>",
            to = new[] { toEmail },
            subject = "Mã xác thực phục hồi mật khẩu - Flood Rescue",
            // Tạo template HTML cho Email (thân thiện hơn SMS)
            html = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6;'>
                    <h2 style='color: #007bff;'>Xác minh phục hồi mật khẩu</h2>
                    <p>Chào <strong>{userName}</strong>,</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản trên hệ thống Flood Rescue.</p>
                    <p>Mã xác thực (OTP) của bạn là:</p>
                    <div style='background: #f4f4f4; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 5px; font-weight: bold; color: #333; margin: 20px 0;'>
                        {otp}
                    </div>
                    <p>Mã này có hiệu lực trong <strong>10 phút</strong>. Nếu không yêu cầu, vui lòng bỏ qua thư này.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #888;'>Hệ thống Điều phối Cứu hộ Lũ lụt (Flood Rescue Coordination)</p>
                </div>"
        };

        var json = JsonSerializer.Serialize(emailData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("https://api.resend.com/emails", content);
            
            if (!response.IsSuccessStatusCode)
            {
                // Đọc lỗi chi tiết từ Resend để báo lại cho Console
                var errorInfo = await response.Content.ReadAsStringAsync();
                Console.WriteLine("==========================================");
                Console.WriteLine($"RESEND API ERROR: {response.StatusCode}");
                Console.WriteLine($"Body: {errorInfo}");
                Console.WriteLine("==========================================");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION IN EMAIL SERVICE: {ex.Message}");
            return false;
        }
    }
}
