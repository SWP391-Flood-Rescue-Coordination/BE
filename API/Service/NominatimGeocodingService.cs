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

    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Truy vấn API format JSON để diễn dịch địa chỉ cụ thể từ tọa độ tương đối.
    /// </summary>
    public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"reverse?format=jsonv2&lat={latitude}&lon={longitude}");

        var response = await _httpClient.GetFromJsonAsync<NominatimReverseResponse>(url, cancellationToken);
        return response?.DisplayName;
    }

    private sealed class NominatimReverseResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
