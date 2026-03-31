using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// EmailService: Triển khai gửi email thông qua Resend API.
/// Chuyên dùng để gửi mã OTP xác thực và các thông báo hệ thống.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructor khởi tạo EmailService.
    /// </summary>
    /// <param name="configuration">Cấu hình hệ thống (ApiKey, FromEmail).</param>
    /// <param name="httpClientFactory">Factory để tạo HttpClient gửi request REST.</param>
    public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gửi mã OTP phục hồi mật khẩu qua Email.
    /// Sử dụng giao thức HTTP POST tới Resend API với nội dung HTML.
    /// </summary>
    /// <param name="toEmail">Địa chỉ nhận.</param>
    /// <param name="userName">Tên hiển thị của người dùng.</param>
    /// <param name="otp">Mã số OTP.</param>
    /// <returns>True nếu gửi thành công (HTTP 200/201).</returns>
    public async Task<bool> SendOtpEmailAsync(string toEmail, string userName, string otp)
    {
        // 1. Lấy cấu hình từ appsettings.json
        var apiKey = _configuration["ResendSettings:ApiKey"];
        var fromEmail = _configuration["ResendSettings:FromEmail"] ?? "onboarding@resend.dev";

        // 2. Tạo HttpClient và thiết lập Header Authorization (Bearer Token)
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // 3. Chuẩn bị Payload dữ liệu theo định dạng yêu cầu của Resend API
        var emailData = new
        {
            from = $"Flood Rescue <{fromEmail}>",
            to = new[] { toEmail },
            subject = "Mã xác thực phục hồi mật khẩu - Flood Rescue",
            // 4. Tạo giao diện Email bằng HTML để thân thiện với người dùng
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
            // 5. Gửi request POST tới Resend API endpoint
            var response = await client.PostAsync("https://api.resend.com/emails", content);
            
            if (!response.IsSuccessStatusCode)
            {
                // 6. Log lỗi chi tiết nếu API trả về lỗi (ví dụ: Sai API Key, Domain chưa xác thực)
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
            // 7. Xử lý các ngoại lệ kết nối mạng hoặc JSON
            Console.WriteLine($"EXCEPTION IN EMAIL SERVICE: {ex.Message}");
            return false;
        }
    }
}
