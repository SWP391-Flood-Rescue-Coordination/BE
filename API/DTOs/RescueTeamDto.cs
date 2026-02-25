namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// DTO để Rescue Team cập nhật trạng thái nhiệm vụ
/// newStatus chấp nhận: "IN_PROGRESS" hoặc "COMPLETED"
/// </summary>
public class UpdateMissionStatusDto
{
    /// <summary>
    /// Trạng thái mới của nhiệm vụ: "IN_PROGRESS" hoặc "COMPLETED"
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú thêm (không bắt buộc)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Trạng thái hiện tại mà client đang thấy (dùng để kiểm tra concurrency).
    /// Client phải gửi đúng trạng thái assignment đang có trong DB trước khi update.
    /// </summary>
    public string ExpectedCurrentStatus { get; set; } = string.Empty;
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
