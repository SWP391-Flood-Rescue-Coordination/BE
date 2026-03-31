using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Caching.Memory;

namespace Flood_Rescue_Coordination.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;

    public AuthService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        ISmsService smsService,
        IEmailService emailService,
        IMemoryCache cache)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _smsService = smsService;
        _emailService = emailService;
        _cache = cache;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc và phải đúng định dạng"
            };
        }

        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại không tồn tại hoặc tài khoản đã bị vô hiệu hóa"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Mật khẩu không chính xác"
            };
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);
        
        // Generate Refresh Token
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

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
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tên đăng nhập đã được sử dụng"
            };
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email đã được sử dụng"
            };
        }

        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc và phải đúng định dạng"
            };
        }

        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        if (await _context.Users.AnyAsync(u => u.Phone != null && phoneCandidates.Contains(u.Phone)))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại đã được sử dụng"
            };
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = normalizedPhone,
            Email = request.Email,
            Role = "CITIZEN",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginResponse = await LoginAsync(new LoginRequest
        {
            Phone = normalizedPhone,
            Password = request.Password
        });

        if (loginResponse.Success)
        {
            loginResponse.Message = "Đăng ký thành công";
        }

        return loginResponse;
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

        if (storedToken.User == null || !storedToken.User.IsActive)
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
                FullName = storedToken.User.FullName ?? "",
                Email = storedToken.User.Email ?? "",
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
            ExpiresAt = tokenExpiration,
            BlacklistedAt = DateTime.UtcNow
        };

        _context.BlacklistedTokens.Add(blacklistedToken);

        // Thu hồi refresh token nếu có
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            
            if (storedToken != null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng xuất thành công"
        };
    }

    // =============================================
    // QUÊN MẬT KHẨU – OTP
    // =============================================

    /// <summary>
    /// Bước 1: Kiểm tra số điện thoại tồn tại, lấy email và gửi OTP qua Email (Resend.com).
    /// </summary>
    public async Task<AuthResponse> SendForgotPasswordOtpAsync(SendOtpRequest request)
    {
        // 1. Normalize số điện thoại
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ" };
        }

        // 2. Tìm User dựa trên SĐT
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không tồn tại trong hệ thống" };
        }

        return await SendOtpWithCooldownAsync(user, normalizedPhone);
    }

    /// <summary>
    /// Hàm dùng chung xử lý sinh OTP, kiểm tra thời gian chờ (Cooldown) và gửi Email.
    /// </summary>
    private async Task<AuthResponse> SendOtpWithCooldownAsync(User user, string phone)
    {
        string cacheKey = "OTP_RESET_" + phone;
        string cooldownKey = "COOLDOWN_OTP_" + phone;

        // 1. Kiểm tra Cooldown để chống spam (60s)
        if (_cache.TryGetValue(cooldownKey, out _))
        {
            return new AuthResponse { Success = false, Message = "Vui lòng đợi 60 giây trước khi yêu cầu gửi lại mã mới." };
        }

        // 2. Sinh mã mới
        string otp = new Random().Next(100000, 999999).ToString();

        // 3. Lưu vào Cache (10 phút) và Set Cooldown (60s)
        _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));
        _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(60));

        // 4. Gửi Mail qua Resend
        var sent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName ?? user.Username, otp);

        if (!sent) return new AuthResponse { Success = false, Message = "Lỗi kỹ thuật khi gửi mail. Thử lại sau." };

        var obfuscatedEmail = HideEmail(user.Email!);
        return new AuthResponse 
        { 
            Success = true, 
            Message = $"Mã OTP đã được gửi về {obfuscatedEmail}. Vui lòng kiểm tra hộp thư." 
        };
    }

    private static string HideEmail(string email)
    {
        var parts = email.Split('@');
        if (parts[0].Length <= 3) return email;
        return parts[0][..Part0Length(parts[0])] + "****@" + parts[1];
    }

    private static int Part0Length(string s) => s.Length > 3 ? 3 : s.Length;

    /// <summary>
    /// Bước 2: Xác thực mã OTP từ Memory Cache và cập nhật mật khẩu mới.
    /// </summary>
    public async Task<AuthResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request)
    {
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ" };
        }

        if (!_cache.TryGetValue($"OTP_RESET_{normalizedPhone}", out string? storedOtp) || storedOtp != request.Otp)
        {
            return new AuthResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." };
        }

        _cache.Remove($"OTP_RESET_{normalizedPhone}");

        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null) return new AuthResponse { Success = false, Message = "Tài khoản không tồn tại." };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return new AuthResponse { Success = true, Message = "Mật khẩu đã được cập nhật thành công. Vui lòng đăng nhập lại." };
    }

    private static string[] BuildPhoneCandidates(string normalizedPhone)
    {
        return
        [
            normalizedPhone,
            $"+84{normalizedPhone[1..]}",
            $"84{normalizedPhone[1..]}"
        ];
    }

    private static bool TryNormalizeVietnamPhone(string? phone, out string normalizedPhone)
    {
        normalizedPhone = string.Empty;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var sanitizedPhone = phone.Trim();
        if (!Regex.IsMatch(sanitizedPhone, @"^\+?[0-9\s\-.()]+$"))
        {
            return false;
        }

        var digits = new string(sanitizedPhone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84") && digits.Length == 11)
        {
            digits = $"0{digits[2..]}";
        }

        if (digits.Length != 10 || !digits.StartsWith('0') || !digits.All(char.IsDigit))
        {
            return false;
        }

        normalizedPhone = digits;
        return true;
    }
}
