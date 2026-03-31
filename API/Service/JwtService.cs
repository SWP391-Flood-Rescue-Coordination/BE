using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// JwtService: Quản lý việc tạo, phân tích và kiểm tra tính hợp lệ của JSON Web Tokens (JWT).
/// Sử dụng các thiết lập từ cấu hình hệ thống (JwtSettings).
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor khởi tạo JwtService.
    /// </summary>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Tạo Access Token chứa các thông tin định danh (Claims) của người dùng.
    /// </summary>
    /// <param name="user">Thông tin người dùng cần mã hóa vào token.</param>
    /// <returns>Chuỗi JWT Token đã được ký số.</returns>
    public string GenerateAccessToken(User user)
    {
        // 1. Đọc các tham số cấu hình từ JwtSettings
        var secretKey = _configuration["JwtSettings:SecretKey"]!;
        var issuer = _configuration["JwtSettings:Issuer"]!;
        var audience = _configuration["JwtSettings:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);

        // 2. Thiết lập khóa ký bảo mật (Symmetric Security Key)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Định nghĩa danh sách các quyền hạn và thông tin (Claims) đính kèm trong Token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),        // ID người dùng
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),          // Tên đăng nhập
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),           // Email
            new Claim(ClaimTypes.Role, user.Role),                                // Vai trò (Admin, Citizen, v.v.)
            new Claim("fullName", user.FullName ?? ""),                           // Họ tên đầy đủ
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),     // ID định danh duy nhất cho Token (chống Replay Attack)
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Thời điểm phát hành
        };

        // 4. Khởi tạo cấu trúc Token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        // 5. Chuyển đổi đối tượng Token thành chuỗi văn bản (JWT string)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Sinh chuỗi Refresh Token ngẫu nhiên có độ bảo mật cao.
    /// Sử dụng RandomNumberGenerator để đảm bảo tính không thể dự đoán.
    /// </summary>
    /// <returns>Chuỗi Refresh Token dưới dạng Base64.</returns>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Đọc thông tin từ chuỗi JWT đã cấp để lấy thời gian hết hạn (ValidTo).
    /// </summary>
    public DateTime GetTokenExpiration(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo;
    }

    /// <summary>
    /// Trích xuất thông tin người dùng (Principal) từ một Token đã hết hạn.
    /// Dùng trong quá trình Refresh Token để xác định ai đang yêu cầu cấp mới.
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]!;

        // Thiết lập các tham số kiểm tra Token
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            // Quan trọng: Tắt kiểm tra thời gian hết hạn để có thể đọc thông tin từ Token cũ
            ValidateLifetime = false 
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Tiến hành giải mã và xác thực chữ ký (chỉ bỏ qua phần thời gian)
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        // Kiểm tra xem thuật toán mã hóa (Algorithm) có đúng là HS256 không
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}