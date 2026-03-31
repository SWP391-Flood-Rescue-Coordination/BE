namespace Flood_Rescue_Coordination.API.Services;

/// <summary>
/// Interface cung cấp dịch vụ tính toán khoảng cách.
/// </summary>
public interface IDistanceService
{
    /// <summary>
    /// Tính toán khoảng cách di chuyển thực tế theo đường bộ (đơn vị km) giữa hai tọa độ địa lý.
    /// Thường sử dụng kết nối tới API OSRM (Open Source Routing Machine).
    /// </summary>
    /// <param name="lat1">Vĩ độ điểm xuất phát</param>
    /// <param name="lon1">Kinh độ điểm xuất phát</param>
    /// <param name="lat2">Vĩ độ điểm đến</param>
    /// <param name="lon2">Kinh độ điểm đến</param>
    /// <param name="cancellationToken">Token thông báo dừng xử lý nếu có</param>
    /// <returns>Khoảng cách tính theo KM (double)</returns>
    Task<double> GetRoadDistanceKmAsync(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        CancellationToken cancellationToken = default);
}