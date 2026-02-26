namespace Flood_Rescue_Coordination.API.DTOs;

public class UpdateMissionStatusDto
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ExpectedCurrentStatus { get; set; }
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
