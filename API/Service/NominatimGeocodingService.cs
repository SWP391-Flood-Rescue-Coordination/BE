using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;


namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// NominatimGeocodingService: Triển khai dịch vụ giải mã địa lý ngược (Reverse Geocoding).
/// Sử dụng API công khai của Nominatim (OpenStreetMap) để chuyển tọa độ thành địa chỉ.
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor khởi tạo NominatimGeocodingService.
    /// </summary>
    /// <param name="httpClient">HttpClient được cấu hình sẵn Base Address tới Nominatim API.</param>
    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Thực hiện gọi API Nominatim để lấy địa chỉ từ tọa độ.
    /// </summary>
    /// <param name="latitude">Vĩ độ.</param>
    /// <param name="longitude">Kinh độ.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Địa chỉ dưới dạng chuỗi (DisplayName) hoặc null.</returns>
    public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        // 1. Tạo URL truy vấn với tham số format=jsonv2 và tọa độ tương ứng
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"reverse?format=jsonv2&lat={latitude}&lon={longitude}");

        // 2. Gửi request GET và giải mã kết quả JSON trả về
        var response = await _httpClient.GetFromJsonAsync<NominatimReverseResponse>(url, cancellationToken);
        
        // 3. Trả về thuộc tính DisplayName chứa địa chỉ đầy đủ
        return response?.DisplayName;
    }

    private sealed class NominatimReverseResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
