using Flood_Rescue_Coordination.API.Models;
using Flood_Rescue_Coordination.API.DTOs;
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
            Role = "CITIZEN",
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
}