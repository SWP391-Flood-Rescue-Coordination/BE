using Flood_Rescue_Coordination.Models.DTOs;
using Flood_Rescue_Coordination.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Flood_Rescue_Coordination.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        /// <param name="request">Thông tin đăng nhập (Username: user, Password: 123)</param>
        /// <returns>Thông tin user và JWT token nếu thành công</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {           
            var response = _authService.Login(request);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="request">Thông tin đăng ký (Username tối thiểu 6 ký tự, Password tối thiểu 6 ký tự bao gồm chữ và số)</param>
        /// <returns>Thông tin user và JWT token nếu thành công</returns>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // Trim và kiểm tra các trường bắt buộc
            var username = request.Username?.Trim() ?? string.Empty;
            var password = request.Password?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Vui lòng điền đầy đủ Username và Password"
                });
            }

            // Kiểm tra Username không chứa khoảng trắng
            if (username.Contains(' '))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Username không được chứa khoảng trắng"
                });
            }

            // Kiểm tra độ dài Username
            if (username.Length < 6)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Username phải có ít nhất 6 ký tự"
                });
            }

            // Kiểm tra Password không chứa khoảng trắng
            if (password.Contains(' '))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Password không được chứa khoảng trắng"
                });
            }

            // Kiểm tra độ dài Password
            if (password.Length < 6)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Password phải có ít nhất 6 ký tự"
                });
            }

            // Kiểm tra Password phải bao gồm cả chữ và số
            bool hasLetter = Regex.IsMatch(password, @"[a-zA-Z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");

            if (!hasLetter || !hasDigit)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Password phải bao gồm cả chữ và số"
                });
            }

            // Cập nhật lại request sau khi trim
            request.Username = username;
            request.Password = password;

            var response = _authService.Register(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Test endpoint để kiểm tra API hoạt động
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Auth API is working!",
                timestamp = DateTime.UtcNow,
                sampleAccount = new
                {
                    username = "user",
                    password = "123"
                },
                registerRules = new
                {
                    usernameMinLength = 6,
                    passwordMinLength = 6,
                    passwordRequirement = "Phải bao gồm cả chữ và số",
                    noSpaces = "Username và Password không được chứa khoảng trắng"
                }
            });
        }
    }
}