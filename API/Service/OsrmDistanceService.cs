using System.Globalization;
using System.Net.Http.Json;

namespace Flood_Rescue_Coordination.API.Services;

public class OsrmDistanceService : IDistanceService
{
    private readonly HttpClient _httpClient;

    public OsrmDistanceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false");

        var response = await _httpClient.GetFromJsonAsync<OsrmRouteResponse>(url, cancellationToken);

        if (response == null || !string.Equals(response.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OSRM không trả về kết quả hợp lệ.");
        }

        var route = response.Routes.FirstOrDefault();
        if (route == null)
        {
            throw new InvalidOperationException("OSRM không có route phù hợp.");
        }

        // OSRM distance trả về theo mét
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