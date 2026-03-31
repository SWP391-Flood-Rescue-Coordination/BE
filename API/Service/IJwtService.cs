using Flood_Rescue_Coordination.API.Models;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface chuyên quản lý khởi tạo, parse và validate thông tin JSON Web Token (JWT).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Tạo ra một chuỗi JWT Access Token mới dựa trên thông tin của User.
    /// Token này sẽ chứa các Claims quan trọng như UserId, Role, FullName...
    /// </summary>
    /// <param name="user">Đối tượng người dùng cần cấp token</param>
    /// <returns>Chuỗi JWT Token được ký bảo mật</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Tạo ra một chuỗi ngẫu nhiên duy nhất dùng làm Refresh Token.
    /// Refresh Token dùng để yêu cầu cấp Access Token mới mà không cần đăng nhập lại.
    /// </summary>
    /// <returns>Chuỗi ký tự Refresh Token</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Đọc thông tin từ một chuỗi JWT để lấy ra thời gian hết hạn của nó.
    /// </summary>
    /// <param name="token">Chuỗi JWT Token cần kiểm tra</param>
    /// <returns>Thời điểm hết hạn (DateTime)</returns>
    DateTime GetTokenExpiration(string token);

    /// <summary>
    /// Trích xuất các thông tin định danh (ClaimsPrincipal) từ một Token đã hết hạn.
    /// Được sử dụng trong luồng làm mới Access Token (Token Refresh).
    /// </summary>
    /// <param name="token">Chuỗi Access Token đã hết hạn</param>
    /// <returns>Các thông tin định danh của người dùng hoặc null nếu token không hợp lệ</returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}