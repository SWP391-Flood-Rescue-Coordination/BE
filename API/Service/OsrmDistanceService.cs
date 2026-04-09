using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// OsrmDistanceService: Triển khai dịch vụ tính toán khoảng cách đường bộ.
/// Sử dụng công cụ OSRM (Open Source Routing Machine) để tìm tuyến đường tối ưu và trả về khoảng cách.
/// </summary>
public class OsrmDistanceService : IDistanceService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor khởi tạo OsrmDistanceService.
    /// </summary>
    /// <param name="httpClient">HttpClient được cấu hình sẵn Base Address tới máy chủ OSRM.</param>
    public OsrmDistanceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Tính toán và lấy về khoảng cách thực tế di chuyển bằng ô tô giữa hai tọa độ.
    /// Chuyển đổi kết quả từ Mét sang Kilômét.
    /// </summary>
    /// <param name="lat1">Vĩ độ điểm 1.</param>
    /// <param name="lon1">Kinh độ điểm 1.</param>
    /// <param name="lat2">Vĩ độ điểm 2.</param>
    /// <param name="lon2">Kinh độ điểm 2.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Khoảng cách tính theo KM (double).</returns>
    public async Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default)
    {
        ValidateCoordinates(lat1, lon1);
        ValidateCoordinates(lat2, lon2);

        try
        {
            // 1. Tạo URL truy vấn OSRM theo định dạng: lon,lat;lon,lat
            var url = string.Create(
                CultureInfo.InvariantCulture,
                $"route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false");

            // 2. Gửi request GET tới OSRM server và nhận phản hồi JSON
            using var responseMessage = await _httpClient.GetAsync(url, cancellationToken);

            if (!responseMessage.IsSuccessStatusCode)
            {
                var errorBody = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                throw new OsrmException(
                    $"OSRM trả HTTP {(int)responseMessage.StatusCode} ({responseMessage.ReasonPhrase}). {errorBody}");
            }

            var response = await responseMessage.Content.ReadFromJsonAsync<OsrmRouteResponse>(cancellationToken);

            // 3. Kiểm tra mã phản hồi (Code phải là Ok)
            if (response == null)
            {
                throw new OsrmException("OSRM không trả về dữ liệu.");
            }

            if (!string.Equals(response.Code, "Ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new OsrmException($"OSRM code không hợp lệ: {response.Code}");
            }

            // 4. Lấy tuyến đường (Route) đầu tiên từ danh sách kết quả
            if (response.Routes == null || response.Routes.Count == 0)
            {
                throw new OsrmException("OSRM không có route phù hợp.");
            }

            var route = response.Routes.First();

            // 5. OSRM trả về khoảng cách (Distance) tính bằng Mét, thực hiện chia cho 1000 để đổi sang KM
            return route.Distance / 1000.0;
        }
        catch (JsonException ex)
        {
            throw new OsrmException("OSRM trả JSON không hợp lệ.", ex);
        }
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude) || latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude phải nằm trong khoảng [-90, 90].");
        }

        if (double.IsNaN(longitude) || double.IsInfinity(longitude) || longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude phải nằm trong khoảng [-180, 180].");
        }
    }

    private sealed class OsrmRouteResponse
    {
        public string Code { get; set; } = string.Empty;
        public List<OsrmRoute>? Routes { get; set; }
    }

    private sealed class OsrmRoute
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
    }
}