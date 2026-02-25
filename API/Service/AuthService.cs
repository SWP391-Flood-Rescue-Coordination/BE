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

    public AuthService(ApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
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

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng nhập thành công",
            AccessToken = accessToken,
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
