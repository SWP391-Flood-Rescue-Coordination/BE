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

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Lấy định danh user, tạo claims bảo mật, dùng mã hóa ký Symmetric key để sinh Access Token mới.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]!;
        var issuer = _configuration["JwtSettings:Issuer"]!;
        var audience = _configuration["JwtSettings:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Sinh chuỗi bytes ngẫu nhiên chuẩn bảo mật làm Refresh Token (Mã hóa Base64).
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Phân tích Jwt Token mà không cần validate chữ ký, chỉ để lấy về thuộc tính ValidTo (thời hạn).
    /// </summary>
    public DateTime GetTokenExpiration(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo;
    }

    /// <summary>
    /// Tắt ValidateLifetime trong param cấu hình để có thể decode lấy dữ liệu ClaimsPrincipal từ một Token dù nó đã hết hạn.
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]!;
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false // Quan trọng: không validate lifetime để lấy principal từ token đã hết hạn
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}