namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cung cấp dịch vụ tính toán khoảng cách.
/// </summary>
public interface IDistanceService
{
    /// <summary>
    /// Tính khoảng cách đường bộ (km) từ điểm (lat1, lon1) đến điểm (lat2, lon2).
    /// </summary>
    Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default);
}