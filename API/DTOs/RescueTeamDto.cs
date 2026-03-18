namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// DTO để Rescue Team cập nhật trạng thái nhiệm vụ
/// newStatus chấp nhận: "IN_PROGRESS", "COMPLETED" hoặc "FAILED"
/// </summary>
public class UpdateMissionStatusDto
{
    /// <summary>
    /// Trạng thái mới của nhiệm vụ: "IN_PROGRESS", "COMPLETED" hoặc "FAILED"
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Lý do (Bắt buộc nếu trạng thái là FAILED)</summary>
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
