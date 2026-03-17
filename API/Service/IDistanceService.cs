namespace Flood_Rescue_Coordination.API.Services;

public interface IDistanceService
{
    Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default);
}