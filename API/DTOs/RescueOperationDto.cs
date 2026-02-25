namespace Flood_Rescue_Coordination.API.DTOs;

public class AssignRescueDto
{
    /// <summary>ID của rescue request cần phân công</summary>
    public int RequestId { get; set; }

    /// <summary>ID của đội cứu hộ được phân công</summary>
    public int TeamId { get; set; }

    /// <summary>Danh sách vehicle ID, cách nhau bằng dấu phẩy. Ví dụ: "1,2,3". Để trống nếu không cần phương tiện.</summary>
    public string? VehicleIds { get; set; }
}

public class AssignRescueResponseDto
{
    public int OperationId { get; set; }
    public int RequestId { get; set; }
    public int TeamId { get; set; }
    public List<int> AssignedVehicleIds { get; set; } = new();
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
