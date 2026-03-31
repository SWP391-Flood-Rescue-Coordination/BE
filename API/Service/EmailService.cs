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

    /// <summary>
    /// Khởi tạo EmailService với các cấu hình cần thiết.
    /// </summary>
    /// <param name="configuration">Cấu hình hệ thống để lấy API Key của Resend.</param>
    /// <param name="httpClientFactory">Factory để tạo HttpClient gửi request REST.</param>
    public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gửi mã OTP phục hồi mật khẩu qua Email sử dụng Resend.com API.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận.</param>
    /// <param name="userName">Tên hiển thị của người dùng (để cá nhân hóa nội dung).</param>
    /// <param name="otp">Mã xác thực (6 chữ số).</param>
    /// <returns>True nếu gửi thành công, False nếu thất bại hoặc có lỗi API.</returns>
    public async Task<bool> SendOtpEmailAsync(string toEmail, string userName, string otp)
    {
        // 1. Lấy thông tin cấu hình từ appsettings.json
        var apiKey = _configuration["ResendSettings:ApiKey"];
        var fromEmail = _configuration["ResendSettings:FromEmail"] ?? "onboarding@resend.dev";

        // 2. Tạo client HTTP và thiết lập Header Authorization (Bearer Token)
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // 3. Chuẩn bị payload (dữ liệu) cho Resend API theo định dạng JSON
        var emailData = new
        {
            from = $"Flood Rescue <{fromEmail}>",
            to = new[] { toEmail },
            subject = "Mã xác thực phục hồi mật khẩu - Flood Rescue",
            // Tạo nội dung Email dạng HTML thân thiện với người dùng
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

        // 4. Serialize dữ liệu sang chuỗi JSON
        var json = JsonSerializer.Serialize(emailData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            // 5. Gửi request POST tới endpoint của Resend
            var response = await client.PostAsync("https://api.resend.com/emails", content);
            
            // 6. Kiểm tra mã trạng thái trả về (200-299)
            if (!response.IsSuccessStatusCode)
            {
                // Nếu lỗi, đọc nội dung lỗi để ghi log phục vụ việc debug
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
            // Bắt lỗi ngoại lệ (mất kết nối, timeout...)
            Console.WriteLine($"EXCEPTION IN EMAIL SERVICE: {ex.Message}");
            return false;
        }
    }
}
