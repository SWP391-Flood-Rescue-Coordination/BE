using System.Globalization;
using System.Net.Http.Json;

namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Dịch vụ tính toán khoảng cách đường bộ thông qua OSRM (Open Source Routing Machine l).
/// </summary>
public class OsrmDistanceService : IDistanceService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Khởi tạo OsrmDistanceService.
    /// </summary>
    /// <param name="httpClient">HttpClient dùng để gọi API của OSRM.</param>
    public OsrmDistanceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Tính toán khoảng cách di chuyển bằng đường bộ (đường ô tô) giữa hai tọa độ.
    /// Sử dụng API OSRM (Open Source Routing Machine).
    /// </summary>
    /// <param name="lat1">Vĩ độ điểm đi.</param>
    /// <param name="lon1">Kinh độ điểm đi.</param>
    /// <param name="lat2">Vĩ độ điểm đến.</param>
    /// <param name="lon2">Kinh độ điểm đến.</param>
    /// <param name="cancellationToken">Token để hủy bỏ yêu cầu.</param>
    /// <returns>Khoảng cách tính bằng Kilômét (KM).</returns>
    public async Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default)
    {
        // 1. Xây dựng URL yêu cầu định tuyến (dạng: long,lat;long,lat)
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false");

        // 2. Thực hiện gọi API và nhận dữ liệu JSON
        var response = await _httpClient.GetFromJsonAsync<OsrmRouteResponse>(url, cancellationToken);

        // 3. Kiểm tra mã phản hồi từ server (Phải là 'Ok')
        if (response == null || !string.Equals(response.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OSRM không trả về kết quả hợp lệ.");
        }

        // 4. Lấy tuyến đường (Route) đầu tiên tìm được
        var route = response.Routes.FirstOrDefault();
        if (route == null)
        {
            throw new InvalidOperationException("OSRM không có route phù hợp.");
        }

        // 5. OSRM trả về khoảng cách theo đơn vị Mét (m), cần chia cho 1000 để đổi sang KM
        return route.Distance / 1000.0;
    }

    private sealed class OsrmRouteResponse
    {
        public string Code { get; set; } = string.Empty;
        public List<OsrmRoute> Routes { get; set; } = [];
    }

    private sealed class OsrmRoute
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
    }
}