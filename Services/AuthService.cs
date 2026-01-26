using Flood_Rescue_Coordination.Models;
using Flood_Rescue_Coordination.Models.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Flood_Rescue_Coordination.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private static readonly List<User> _users = new()
        {
            // Tài kho?n m?u: username = "user", password = "123"
            new User
            {
                Id = 1,
                Username = "user",
                PasswordHash = HashPassword("123"),
                Email = "user@example.com",
                FullName = "Sample User",
                CreatedAt = DateTime.UtcNow
            }
        };

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AuthResponse Login(LoginRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Username == request.Username);

            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tên ??ng nh?p không t?n t?i"
                };
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "M?t kh?u không chính xác"
                };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "??ng nh?p thành công",
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName
                }
            };
        }

        public AuthResponse Register(RegisterRequest request)
        {
            if (_users.Any(u => u.Username == request.Username))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tên ??ng nh?p ?ã t?n t?i"
                };
            }

            if (_users.Any(u => u.Email == request.Email))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email ?ã ???c s? d?ng"
                };
            }

            var newUser = new User
            {
                Id = _users.Max(u => u.Id) + 1,
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                Email = request.Email,
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(newUser);

            var token = GenerateJwtToken(newUser);

            return new AuthResponse
            {
                Success = true,
                Message = "??ng ký thành công",
                Token = token,
                User = new UserInfo
                {
                    Id = newUser.Id,
                    Username = newUser.Username,
                    Email = newUser.Email,
                    FullName = newUser.FullName
                }
            };
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForFloodRescueCoordination2024!@#$";
            var issuer = jwtSettings["Issuer"] ?? "FloodRescueAPI";
            var audience = jwtSettings["Audience"] ?? "FloodRescueClient";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    }
}       