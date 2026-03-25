namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cung cấp dịch vụ giải mã địa lý ngược (Reverse Geocoding).
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Chuyển đổi từ tọa độ (vĩ độ, kinh độ) sang địa chỉ cụ thể dạng chuỗi (DisplayName).
    /// </summary>
    Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}
