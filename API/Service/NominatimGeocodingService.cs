using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;


namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Dịch vụ Reverse Geocoding giao tiếp với API công khai của Nominatim (OpenStreetMap).
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Khởi tạo NominatimGeocodingService.
    /// </summary>
    /// <param name="httpClient">HttpClient được cấu hình sẵn (BaseAddress, User-Agent) qua Dependency Injection.</param>
    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Chuyển đổi tọa độ (Kinh độ, Vĩ độ) thành địa chỉ thực tế (Reverse Geocoding).
    /// Sử dụng API của Nominatim (OpenStreetMap).
    /// </summary>
    /// <param name="latitude">Vĩ độ của vị trí.</param>
    /// <param name="longitude">Kinh độ của vị trí.</param>
    /// <param name="cancellationToken">Token để hủy bỏ yêu cầu nếu cần thiết.</param>
    /// <returns>Chuỗi địa chỉ đầy đủ gắn với tọa độ đó, hoặc null nếu không tìm thấy.</returns>
    public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        // 1. Xây dựng URL truy vấn với định dạng JSON
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"reverse?format=jsonv2&lat={latitude}&lon={longitude}");

        // 2. Gửi request và nhận kết quả trả về, tự động map vào đối tượng NominatimReverseResponse
        var response = await _httpClient.GetFromJsonAsync<NominatimReverseResponse>(url, cancellationToken);
        
        // 3. Trả về thuộc tính DisplayName chứa địa chỉ thô từ OSM
        return response?.DisplayName;
    }

    private sealed class NominatimReverseResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
