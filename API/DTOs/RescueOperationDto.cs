using System.ComponentModel.DataAnnotations;

    namespace Flood_Rescue_Coordination.API.DTOs;

public class AssignRescueDto
{
    /// <summary>ID của rescue request cần phân công</summary>
    public int RequestId { get; set; }

    /// <summary>ID của đội cứu hộ được phân công</summary>
    public int TeamId { get; set; }

    /// <summary>Danh sách vehicle ID, cách nhau bằng dấu phẩy. Ví dụ: "1,2,3". Để trống nếu không cần phương tiện.</summary>
    public string? VehicleIds { get; set; }

    /// <summary>Thời gian ước tính hoàn thành (phút). Có thể để trống.</summary>
    public int? EstimatedTime { get; set; }
}

public class AssignRescueResponseDto
{
    public int OperationId { get; set; }
    public int RequestId { get; set; }
    public int TeamId { get; set; }
    public List<int> AssignedVehicleIds { get; set; } = new();
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? NumberOfAffectedPeople { get; set; }
    public int? EstimatedTime { get; set; }
}

/// <summary>Thông tin một operation trả về cho rescue team member</summary>
public class TeamOperationDto
{
    public int OperationId { get; set; }
    public int RequestId { get; set; }
    public int TeamId { get; set; }

    // Thông tin rescue request
    public string? RequestTitle { get; set; }
    public string? RequestAddress { get; set; }
    public string? RequestDescription { get; set; }
    public string? RequestPhone { get; set; }
    public string? PriorityName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string OperationStatus { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? NumberOfAffectedPeople { get; set; }
    public int? EstimatedTime { get; set; }

    /// <summary>Danh sách vehicle_id được gán vào operation này</summary>
    public List<int> VehicleIds { get; set; } = new();
}

public class UpdateOperationStatusDto : IValidatableObject
{
    /// <summary>Trạng thái mới: COMPLETED hoặc FAILED</summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Lý do (bắt buộc nếu trạng thái là FAILED)</summary>
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

