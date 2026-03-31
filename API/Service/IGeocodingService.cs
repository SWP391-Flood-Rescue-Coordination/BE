namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cung cấp dịch vụ giải mã địa lý ngược (Reverse Geocoding).
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Thực hiện chuyển đổi tọa độ địa lý (Latitude, Longitude) thành một địa chỉ văn bản dễ hiểu.
    /// Dựa trên dịch vụ bản đồ (ví dụ: Nominatim).
    /// </summary>
    /// <param name="latitude">Vĩ độ của vị trí cần giải mã</param>
    /// <param name="longitude">Kinh độ của vị trí cần giải mã</param>
    /// <param name="cancellationToken">Token hỗ trợ hủy tác vụ bất đồng bộ</param>
    /// <returns>Chuỗi địa chỉ đầy đủ (DisplayName) hoặc null nếu không tìm thấy.</returns>
    Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}
