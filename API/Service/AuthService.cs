using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Caching.Memory;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// AuthService: Triển khai các dịch vụ xác thực người dùng.
/// Bao gồm xử lý đăng nhập, đăng ký, cấp phát JWT, làm mới token và khôi phục mật khẩu.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Constructor khởi tạo AuthService với các phụ thuộc cần thiết.
    /// </summary>
    public AuthService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        IEmailService emailService,
        IMemoryCache cache)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _emailService = emailService;
        _cache = cache;
    }

    /// <summary>
    /// Thực hiện đăng nhập dựa trên số điện thoại và mật khẩu.
    /// </summary>
    /// <param name="request">Chứa thông tin số điện thoại và mật khẩu.</param>
    /// <returns>AuthResponse chứa kết quả xác thực và JWT tokens.</returns>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // 1. Chuẩn hóa số điện thoại người dùng nhập vào
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc và phải đúng định dạng"
            };
        }

        // 2. Tạo danh sách các biến thể số điện thoại để tìm kiếm trong DB (+84, 84, 0)
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);

        // 3. Tìm người dùng trong DB dựa trên số điện thoại
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

        // 4. Kiểm tra mật khẩu (sử dụng BCrypt để verify)
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Mật khẩu không chính xác"
            };
        }

        // 5. Khởi tạo Access Token và Refresh Token
        var accessToken = _jwtService.GenerateAccessToken(user);
        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!);
        
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

        // 6. Lưu Refresh Token vào cơ sở dữ liệu để quản lý phiên
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
    /// Đăng ký tài khoản người dùng mới (vai trò CITIZEN).
    /// </summary>
    /// <param name="request">Thông tin đăng ký (Họ tên, SĐT, Email, Mật khẩu).</param>
    /// <returns>AuthResponse chứa trạng thái đăng ký và token sau khi tự động đăng nhập.</returns>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Kiểm tra Email xem đã có ai dùng chưa
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email đã được sử dụng"
            };
        }

        // 2. Kiểm tra và chuẩn hóa số điện thoại
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại là bắt buộc và phải đúng định dạng"
            };
        }

        // 3. Kiểm tra số điện thoại xem đã tồn tại chưa
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        if (await _context.Users.AnyAsync(u => u.Phone != null && phoneCandidates.Contains(u.Phone)))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Số điện thoại đã được sử dụng"
            };
        }

        // 4. Tự động sinh Username từ Họ tên (không dấu, viết liền)
        string baseUsername = GenerateUsername(request.FullName.ToLower());
        string generatedUsername = baseUsername;
        int counter = 1;
        
        // Đảm bảo username là duy nhất, nếu trùng thì thêm số phía sau
        while (await _context.Users.AnyAsync(u => u.Username == generatedUsername))
        {
            generatedUsername = $"{baseUsername}{counter}";
            counter++;
        }

        // 5. Khởi tạo User mới
        var user = new User
        {
            Username = generatedUsername,
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

        // 6. Thực hiện tự động đăng nhập sau khi đăng ký thành công
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
    /// Làm mới Access Token bằng Refresh Token.
    /// Thu hồi Refresh Token cũ và cung cấp cặp token mới.
    /// </summary>
    /// <param name="refreshToken">Refresh Token đang sử dụng.</param>
    /// <returns>AuthResponse chứa cặp token mới.</returns>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        // 1. Tìm Refresh Token trong DB
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

        // 2. Kiểm tra tính khả dụng (không bị hết hạn, không bị thu hồi trước đó)
        if (!storedToken.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token đã hết hạn hoặc đã bị thu hồi"
            };
        }

        // 3. Kiểm tra xem User đính kèm có còn hoạt động không
        if (storedToken.User == null || !storedToken.User.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Tài khoản đã bị vô hiệu hóa"
            };
        }

        // 4. Thu hồi Refresh Token hiện tại (Đánh dấu thời điểm Revoked)
        storedToken.RevokedAt = DateTime.UtcNow;

        // 5. Sinh cặp Access Token và Refresh Token mới
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
    /// Đăng xuất người dùng.
    /// Đưa Access Token vào danh sách đen (Blacklist) và thu hồi Refresh Token.
    /// </summary>
    /// <param name="accessToken">Token hiện tại đang đăng nhập.</param>
    /// <param name="refreshToken">Refresh Token gửi kèm (nếu có) để vô hiệu hóa.</param>
    /// <returns>AuthResponse thông báo kết quả.</returns>
    public async Task<AuthResponse> LogoutAsync(string accessToken, string? refreshToken)
    {
        // 1. Xác định thời điểm hết hạn của Access Token hiện tại
        var tokenExpiration = _jwtService.GetTokenExpiration(accessToken);
        
        // 2. Thêm vào bảng BlacklistedTokens (Hệ thống sẽ từ chối các request mang token này)
        var blacklistedToken = new BlacklistedToken
        {
            Token = accessToken,
            ExpiresAt = tokenExpiration,
            BlacklistedAt = DateTime.UtcNow
        };

        _context.BlacklistedTokens.Add(blacklistedToken);

        // 3. Nếu người dùng gửi kèm Refresh Token, thu hồi nó luôn để không thể "Refresh" lại được
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
    /// Bước 1: Gửi mã OTP khôi phục mật khẩu.
    /// Kiểm tra tài khoản qua SĐT và gửi mã xác nhận qua Email.
    /// </summary>
    public async Task<AuthResponse> SendForgotPasswordOtpAsync(SendOtpRequest request)
    {
        // 1. Chuẩn hóa số điện thoại của yêu cầu
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ" };
        }

        // 2. Tìm người dùng dựa trên số điện thoại (tài khoản phải đang hoạt động)
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không tồn tại trong hệ thống" };
        }

        // 3. Tiến hành gửi mã OTP kèm theo quy tắc giới hạn gửi lại (Cooldown)
        return await SendOtpWithCooldownAsync(user, normalizedPhone);
    }

    /// <summary>
    /// Xử lý việc sinh mã OTP ngẫu nhiên, lưu vào bộ nhớ đệm và gửi email.
    /// Có cơ chế chống spam (60 giây mới được gửi lại).
    /// </summary>
    private async Task<AuthResponse> SendOtpWithCooldownAsync(User user, string phone)
    {
        string cacheKey = "OTP_RESET_" + phone;
        string cooldownKey = "COOLDOWN_OTP_" + phone;

        // 1. Kiểm tra Cooldown để chống spam tin nhắn liên tục
        if (_cache.TryGetValue(cooldownKey, out _))
        {
            return new AuthResponse { Success = false, Message = "Vui lòng đợi 60 giây trước khi yêu cầu gửi lại mã mới." };
        }

        // 2. Sinh mã OTP ngẫu nhiên gồm 6 chữ số
        string otp = new Random().Next(100000, 999999).ToString();

        // 3. Lưu OTP vào Memory Cache (hiệu lực 10 phút) và thiết lập Cooldown (60 giây)
        _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));
        _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(60));

        // 4. Gửi nội dung OTP qua Email thông qua EmailService (Resend API)
        var sent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName ?? user.Username, otp);

        if (!sent) return new AuthResponse { Success = false, Message = "Lỗi kỹ thuật khi gửi mail. Thử lại sau." };

        // 5. Làm mờ địa chỉ email khi phản hồi về Client để tăng tính bảo mật
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
    /// Bước 2: Xác thực mã OTP và tiến hành thay đổi mật khẩu.
    /// </summary>
    public async Task<AuthResponse> ResetPasswordWithOtpAsync(ResetPasswordRequest request)
    {
        // 1. Chuẩn hóa lại số điện thoại để khớp với Cache Key
        if (!TryNormalizeVietnamPhone(request.Phone, out var normalizedPhone))
        {
            return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ" };
        }

        // 2. Lấy mã OTP trong Cache ra để so khớp
        if (!_cache.TryGetValue($"OTP_RESET_{normalizedPhone}", out string? storedOtp) || storedOtp != request.Otp)
        {
            return new AuthResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." };
        }

        // 3. Xóa OTP khỏi cache sau khi đã xác thực xong (để tránh dùng lại)
        _cache.Remove($"OTP_RESET_{normalizedPhone}");

        // 4. Kiểm tra sự tồn tại của người dùng
        var phoneCandidates = BuildPhoneCandidates(normalizedPhone);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.IsActive && u.Phone != null && phoneCandidates.Contains(u.Phone));

        if (user == null) return new AuthResponse { Success = false, Message = "Tài khoản không tồn tại." };

        // 5. Cập nhật mật khẩu mới (có hash bảo mật)
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return new AuthResponse { Success = true, Message = "Mật khẩu đã được cập nhật thành công. Vui lòng đăng nhập lại." };
    }

    /// <summary>
    /// Tạo mảng các định dạng số điện thoại biến thể (+84, 84, 0) dùng cho việc tìm kiếm chính xác trong DB.
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
    /// Hàm chuẩn hóa số điện thoại Việt Nam về dạng chuỗi 10 chữ số bắt đầu bằng '0'.
    /// </summary>
    /// <param name="phone">Số điện thoại đầu vào.</param>
    /// <param name="normalizedPhone">Số điện thoại sau khi chuẩn hóa.</param>
    /// <returns>True nếu số điện thoại hợp lệ; ngược lại là False.</returns>
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

    /// <summary>
    /// Tự động sinh Username từ Họ và Tên.
    /// Chuyển tên về dạng không dấu, loại bỏ khoảng trắng và chuyển sang chữ thường.
    /// </summary>
    private static string GenerateUsername(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "user";
        
        var normalizedString = fullName.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        string result = stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        
        result = result.Replace("Đ", "D").Replace("đ", "d");

        return result.Replace(" ", "").ToLower();
    }
}
