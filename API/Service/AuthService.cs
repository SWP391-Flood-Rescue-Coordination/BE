using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Flood_Rescue_Coordination.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;

    public AuthService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        ISmsService smsService)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _smsService = smsService;
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
    /// Bước 1: Kiểm tra số điện thoại tồn tại, sau đó giả định gửi mã xác thực.
    /// </summary>
    public async Task<OtpResponse> SendForgotPasswordOtpAsync(SendOtpRequest request)
    {
        // 1. Normalize số điện thoại
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new OtpResponse
            {
                Success = false,
                Message = "Số điện thoại không hợp lệ"
            };
        }

        // 2. Kiểm tra số điện thoại có tồn tại trong hệ thống không
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var userExists = await _context.Users
            .AnyAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (!userExists)
        {
            // Trả lời mơ hồ để tránh lộ thông tin tồn tại của SĐT
            return new OtpResponse
            {
                Success = false,
                Message = "Số điện thoại không tồn tại trong hệ thống"
            };
        }

        // 3. Giả định gửi OTP (Log ra console hoặc dùng mã cố định)
        var sent = await _smsService.SendOtpAsync(normalizedPhone);

        if (!sent)
        {
            return new OtpResponse
            {
                Success = false,
                Message = "Không thể gửi OTP. Vui lòng thử lại sau."
            };
        }

        return new OtpResponse
        {
            Success = true,
            Message = "OTP đã được gửi tới số điện thoại của bạn. Mã có hiệu lực trong 10 phút."
        };
    }

    /// <summary>
    /// Bước 2: Xác thực mã OTP và cập nhật mật khẩu mới.
    /// </summary>
    public async Task<OtpResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request)
    {
        // 1. Normalize số điện thoại
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new OtpResponse
            {
                Success = false,
                Message = "Số điện thoại không hợp lệ"
            };
        }

        // 2. Xác thực OTP
        // Chấp nhận mã test cố định 123456
        var isValid = await _smsService.VerifyOtpAsync(normalizedPhone, request.Otp);

        if (!isValid)
        {
            return new OtpResponse
            {
                Success = false,
                Message = "OTP không hợp lệ hoặc đã hết hạn"
            };
        }

        // 3. Tìm user theo số điện thoại
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null)
        {
            return new OtpResponse
            {
                Success = false,
                Message = "Không tìm thấy tài khoản với số điện thoại này"
            };
        }

        // 4. Hash và lưu mật khẩu mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return new OtpResponse
        {
            Success = true,
            Message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập lại."
        };
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
