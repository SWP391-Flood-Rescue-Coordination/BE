using System.ComponentModel.DataAnnotations;

    namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// DTO để Rescue Team cập nhật trạng thái nhiệm vụ
/// newStatus chấp nhận: "COMPLETED" hoặc "FAILED"
/// </summary>
public class UpdateMissionStatusDto : IValidatableObject
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NewStatus))
        {
            yield return new ValidationResult("NewStatus là bắt buộc.", [nameof(NewStatus)]);
            yield break;
        }

        var key = NewStatus.Trim().ToUpperInvariant();
        if (key != "COMPLETED" && key != "FAILED")
        {
            yield return new ValidationResult(
                "NewStatus không hợp lệ. Chỉ chấp nhận: COMPLETED hoặc FAILED.",
                [nameof(NewStatus)]);
        }

        if (key == "FAILED" && string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult(
                "Reason là bắt buộc khi NewStatus = FAILED.",
                [nameof(Reason)]);
        }
    }
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

/// <summary>
/// DTO để Đội trưởng (Leader) giao việc cho thành viên (Member)
/// </summary>
public class MemberAssignmentDto
{
    public int UserId { get; set; }
    public int OperationId { get; set; }
}

/// <summary>
/// Phản hồi sau khi Đội trưởng giao việc
/// </summary>
public class MemberAssignmentResponseDto
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public int OperationId { get; set; }
    public string MemberStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO để Member xác nhận nhiệm vụ được Leader giao
/// </summary>
public class ConfirmTaskDto
{
    public int OperationId { get; set; }
    public string? Notes { get; set; }
}
