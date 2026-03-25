using Flood_Rescue_Coordination.API.Models;
using System.Security.Claims;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface chuyên quản lý khởi tạo, parse và validate thông tin JSON Web Token (JWT).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Tạo Access Token mới chứa các Claims dựa trên thông tin định danh của User.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Tạo ra một chuỗi ngẫu nhiên bảo mật dùng làm Refresh Token kéo dài phiên đăng nhập.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Phân tích Jwt Token lấy về thời gian hết hạn (Expiration Time).
    /// </summary>
    DateTime GetTokenExpiration(string token);

    /// <summary>
    /// Phân tích Token đã hết hạn, trả về cấu trúc xác thực cơ bản (ClaimsPrincipal) bỏ qua lỗi thời hạn, thường áp dụng cho Refreshing token phase.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}