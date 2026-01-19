using Flood_Rescue_Coordination.API.Data;
using Flood_Rescue_Coordination.API.DTOs;
using Flood_Rescue_Coordination.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Flood_Rescue_Coordination.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tên đăng nhập không tồn tại hoặc tài khoản đã bị vô hiệu hóa"
            };
        }

        // Debug: Log password verification details
        var passwordToVerify = request.Password;
        var storedHash = user.PasswordHash;
        var hashLength = storedHash?.Length ?? 0;
        var isBcryptFormat = !string.IsNullOrEmpty(storedHash) && 
                            (storedHash.StartsWith("$2a$") || 
                             storedHash.StartsWith("$2b$") || 
                             storedHash.StartsWith("$2y$"));
        
        var verifyResult = BCrypt.Net.BCrypt.Verify(passwordToVerify, storedHash);
        
        if (!verifyResult)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Mật khẩu không chính xác. Hash length: {hashLength}, Is BCrypt format: {isBcryptFormat}"
            };
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

        // Lưu refresh token vào database
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng nhập thành công",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!)),
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra username đã tồn tại
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tên đăng nhập đã được sử dụng"
            };
        }

        // Kiểm tra email đã tồn tại
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email đã được sử dụng"
            };
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Role = "CITIZEN", // Mặc định role là CITIZEN
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Tự động đăng nhập sau khi đăng ký
        return await LoginAsync(new LoginRequest
        {
            Username = request.Username,
            Password = request.Password
        });
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token không hợp lệ"
            };
        }

        if (!storedToken.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token đã hết hạn hoặc đã bị thu hồi"
            };
        }

        if (!storedToken.User.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản đã bị vô hiệu hóa"
            };
        }

        // Thu hồi refresh token cũ
        storedToken.RevokedAt = DateTime.UtcNow;

        // Tạo token mới
        var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Token đã được làm mới",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!)),
            User = new UserInfo
            {
                UserId = storedToken.User.UserId,
                Username = storedToken.User.Username,
                FullName = storedToken.User.FullName,
                Email = storedToken.User.Email,
                Role = storedToken.User.Role
            }
        };
    }

    public async Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken)
    {
        // Blacklist access token
        var tokenExpiration = _jwtService.GetTokenExpiration(accessToken);
        
        var blacklistedToken = new BlacklistedToken
        {
            Token = accessToken,
            BlacklistedAt = DateTime.UtcNow,
            ExpiresAt = tokenExpiration
        };

        _context.BlacklistedTokens.Add(blacklistedToken);

        // Thu hồi refresh token nếu có
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedRefreshToken != null && storedRefreshToken.IsActive)
            {
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        // Dọn dẹp các blacklisted token đã hết hạn
        await CleanupExpiredBlacklistedTokensAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng xuất thành công"
        };
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        return await _context.BlacklistedTokens
            .AnyAsync(bt => bt.Token == token && bt.ExpiresAt > DateTime.UtcNow);
    }

    private async Task CleanupExpiredBlacklistedTokensAsync()
    {
        var expiredTokens = await _context.BlacklistedTokens
            .Where(bt => bt.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.BlacklistedTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}