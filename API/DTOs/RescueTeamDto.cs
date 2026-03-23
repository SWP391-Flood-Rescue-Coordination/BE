namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// DTO để Rescue Team cập nhật trạng thái nhiệm vụ
/// newStatus chấp nhận: "COMPLETED", "CANCELLED" hoặc "CANCELED"
/// </summary>
public class UpdateMissionStatusDto
{
    /// <summary>
    /// Trạng thái mới của nhiệm vụ: "COMPLETED", "CANCELLED" hoặc "CANCELED"
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Lý do hủy nhiệm vụ (không bắt buộc)</summary>
    public string? Reason { get; set; }
}

public class MissionStatusResponseDto
{
    public int AssignmentId { get; set; }
    public int RequestId { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public string RequestStatus { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
