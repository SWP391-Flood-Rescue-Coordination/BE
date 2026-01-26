using Flood_Rescue_Coordination.Models.DTOs;
using Flood_Rescue_Coordination.Services;
using Microsoft.AspNetCore.Mvc;

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
        /// <param name="request">Thông tin đăng ký</param>
        /// <returns>Thông tin user và JWT token nếu thành công</returns>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Vui lòng điền đầy đủ thông tin"
                });
            }

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
                }
            });
        }
    }
}