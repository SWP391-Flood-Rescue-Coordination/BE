using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Service triển khai chức năng liên quan đến Token định danh JWT.
/// Đảm nhiệm đọc config (secret key, issuer...) để khởi tạo và kiểm chứng Token.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Khởi tạo JwtService.
    /// </summary>
    /// <param name="configuration">Cấu hình hệ thống để lấy các cài đặt JWT (SecretKey, Issuer, Audience...).</param>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Tạo Access Token (JWT) cho người dùng.
    /// Access Token chứa các thông tin (Claims) về định danh và quyền hạn của người dùng.
    /// </summary>
    /// <param name="user">Đối tượng người dùng cần tạo token.</param>
    /// <returns>Chuỗi JWT Token đã được ký số.</returns>
    public string GenerateAccessToken(User user)
    {
        // 1. Đọc các tham số cấu hình bảo mật
        var secretKey = _configuration["JwtSettings:SecretKey"]!;
        var issuer = _configuration["JwtSettings:Issuer"]!;
        var audience = _configuration["JwtSettings:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);

        // 2. Tạo khóa bảo mật và thông tin ký số (SymmetricSecurityKey + HMAC SHA256)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Định nghĩa các yêu cầu (Claims) gắn vào Token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // ID người dùng
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),   // Tên đăng nhập
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),    // Email
            new Claim(ClaimTypes.Role, user.Role),                         // Vai trò (Admin/Citizen/...)
            new Claim("fullName", user.FullName ?? ""),                    // Họ tên đầy đủ
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID duy nhất của Token (ngăn replay attack)
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Thời điểm tạo
        };

        // 4. Khởi tạo đối tượng JwtSecurityToken với đầy đủ thông tin
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes), // Thời gian hết hạn (thường là ngắn: 15-60 phút)
            signingCredentials: credentials
        );

        // 5. Sử dụng JwtSecurityTokenHandler để chuyển đối tượng token thành chuỗi string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Sinh một chuỗi Refresh Token ngẫu nhiên có độ bảo mật cao.
    /// Refresh Token không chứa thông tin claims mà chỉ là một mã định danh ngẫu nhiên dùng để làm mới Access Token.
    /// </summary>
    /// <returns>Chuỗi Refresh Token dưới dạng Base64.</returns>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber); // Sinh 32 bytes ngẫu nhiên chuẩn mã hóa
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Giải mã và lấy thời điểm hết hạn (ValidTo) của một Token.
    /// Phương thức này không thực hiện kiểm tra chữ ký (Signature) mà chỉ đọc dữ liệu thô từ Token.
    /// </summary>
    /// <param name="token">Chuỗi JWT Token.</param>
    /// <returns>Thời điểm hết hạn của Token.</returns>
    public DateTime GetTokenExpiration(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo;
    }

    /// <summary>
    /// Lấy thông tin Claims (Principal) từ một Token đã hết hạn.
    /// Phương thức này cực kỳ quan trọng trong luồng Refresh Token: Chúng ta cần biết Token cũ thuộc về ai 
    /// nhưng không thể dùng ValidateToken thông thường vì nó sẽ báo lỗi hết hạn.
    /// </summary>
    /// <param name="token">Chuỗi JWT Token đã hết hạn.</param>
    /// <returns>ClaimsPrincipal nếu token hợp lệ cấu trúc, ngược lại là null.</returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]!;
        
        // Thiết lập các thông số kiểm chứng nhưng BỎ QUA việc kiểm tra thời hạn (ValidateLifetime = false)
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false 
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Giải mã token dựa trên các tham số trên
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        // Kiểm tra xem thuật toán mã hóa (Algorithm) có đúng là HMAC SHA256 không để tránh các cuộc tấn công thay đổi thuật toán
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}