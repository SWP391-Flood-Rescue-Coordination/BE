using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Service triển khai nghiệp vụ bảo mật và xác thực người dùng.
/// Xử lý Login, Register (đăng ký), Refresh Token, đăng xuất và tích hợp SMS phục vụ khôi phục mật khẩu.
/// </summary>
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

    /// <summary>
    /// Nghiệp vụ Đăng nhập: Chuẩn hóa SĐT, kiểm tra trong DB, so khớp hash mật khẩu bằng BCrypt, trả về JWT Token cấp quyền.
    /// </summary>
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

    /// <summary>
    /// Nghiệp vụ Đăng ký tài khoản cho công dân. Tự động sinh username từ FullName (bỏ dấu, bỏ khoảng cách, lowercase).
    /// Đảm bảo email, số điện thoại không trùng, tạo record và tự động đăng nhập.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var baseUsername = GenerateUsernameFromFullName(request.FullName);
        var username = baseUsername;
        var suffix = 2;
        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{baseUsername}{suffix}";
            suffix++;
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
            Username = username,
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

    /// <summary>
    /// Nghiệp vụ Cấp mới Token: Nếu RefreshToken còn hạn và chưa bị thu hồi, hệ thống sinh ra bộ Token mới cho user và thu hồi mã cũ.
    /// </summary>
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

    /// <summary>
    /// Nghiệp vụ Đăng xuất: Blacklist Access Token hiện tại (hết hiệu lực ép buộc), và thu hồi (Revoke) Refresh Token để chặn tạo Token mới.
    /// </summary>
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

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Sinh username từ họ tên đầy đủ — bỏ dấu tiếng Việt, bỏ khoảng cách, chuyển thành chữ thường.
    /// Ví dụ: "Nguyễn Văn A" → "nguyenvana".
    /// </summary>
    private static string GenerateUsernameFromFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "user";

        // Bảng ánh xạ ký tự có dấu → không dấu (bao gồm cả chữ hoa)
        var map = new Dictionary<char, char>
        {
            {'à','a'},{'á','a'},{'ả','a'},{'ã','a'},{'ạ','a'},
            {'ă','a'},{'ắ','a'},{'ặ','a'},{'ằ','a'},{'ẳ','a'},{'ẵ','a'},
            {'â','a'},{'ấ','a'},{'ầ','a'},{'ẩ','a'},{'ẫ','a'},{'ậ','a'},
            {'đ','d'},
            {'è','e'},{'é','e'},{'ẻ','e'},{'ẽ','e'},{'ẹ','e'},
            {'ê','e'},{'ế','e'},{'ề','e'},{'ể','e'},{'ễ','e'},{'ệ','e'},
            {'ì','i'},{'í','i'},{'ỉ','i'},{'ĩ','i'},{'ị','i'},
            {'ò','o'},{'ó','o'},{'ỏ','o'},{'õ','o'},{'ọ','o'},
            {'ô','o'},{'ố','o'},{'ồ','o'},{'ổ','o'},{'ỗ','o'},{'ộ','o'},
            {'ơ','o'},{'ớ','o'},{'ờ','o'},{'ở','o'},{'ỡ','o'},{'ợ','o'},
            {'ù','u'},{'ú','u'},{'ủ','u'},{'ũ','u'},{'ụ','u'},
            {'ư','u'},{'ứ','u'},{'ừ','u'},{'ử','u'},{'ữ','u'},{'ự','u'},
            {'ỳ','y'},{'ý','y'},{'ỷ','y'},{'ỹ','y'},{'ỵ','y'},
            // Chữ hoa
            {'À','a'},{'Á','a'},{'Ả','a'},{'Ã','a'},{'Ạ','a'},
            {'Ă','a'},{'Ắ','a'},{'Ặ','a'},{'Ằ','a'},{'Ẳ','a'},{'Ẵ','a'},
            {'Â','a'},{'Ấ','a'},{'Ầ','a'},{'Ẩ','a'},{'Ẫ','a'},{'Ậ','a'},
            {'Đ','d'},
            {'È','e'},{'É','e'},{'Ẻ','e'},{'Ẽ','e'},{'Ẹ','e'},
            {'Ê','e'},{'Ế','e'},{'Ề','e'},{'Ể','e'},{'Ễ','e'},{'Ệ','e'},
            {'Ì','i'},{'Í','i'},{'Ỉ','i'},{'Ĩ','i'},{'Ị','i'},
            {'Ò','o'},{'Ó','o'},{'Ỏ','o'},{'Õ','o'},{'Ọ','o'},
            {'Ô','o'},{'Ố','o'},{'Ồ','o'},{'Ổ','o'},{'Ỗ','o'},{'Ộ','o'},
            {'Ơ','o'},{'Ớ','o'},{'Ờ','o'},{'Ở','o'},{'Ỡ','o'},{'Ợ','o'},
            {'Ù','u'},{'Ú','u'},{'Ủ','u'},{'Ũ','u'},{'Ụ','u'},
            {'Ư','u'},{'Ứ','u'},{'Ừ','u'},{'Ử','u'},{'Ữ','u'},{'Ự','u'},
            {'Ỳ','y'},{'Ý','y'},{'Ỷ','y'},{'Ỹ','y'},{'Ỵ','y'}
        };

        var sb = new System.Text.StringBuilder();
        foreach (var c in fullName)
        {
            if (map.TryGetValue(c, out var mapped))
                sb.Append(mapped);
            else if (char.IsLetter(c))
                sb.Append(char.ToLowerInvariant(c));
            // bỏ qua khoảng cách và ký tự đặc biệt
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "user" : result;
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Đưa ra nhiều biến thể của SĐT Việt Nam (+84, 84, 0x) để có dữ liệu so khớp bao phủ trong CSDL.
    /// </summary>
    private static string[] BuildPhoneCandidates(string normalizedPhone)
    {
        return
        [
            normalizedPhone,
            $"+84{normalizedPhone[1..]}",
            $"84{normalizedPhone[1..]}"
        ];
    }

    /// <summary>
    /// Phương thức hỗ trợ (Helper): Rà soát và chuẩn hóa SĐT Việt Nam đầu vào (chuyển đầu số 84 thành 0, xóa ký hiệu lạ).
    /// </summary>
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
