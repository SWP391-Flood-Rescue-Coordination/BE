namespace Flood_Rescue_Coordination.API.DTOs;

public class CreateRescueRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public int? NumberOfPeople { get; set; }
}

public class UpdateRescueRequestDto
{
    public string? Title { get; set; }
    public string? ContactPhone { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public int? NumberOfPeople { get; set; }
}

public class RescueRequestResponseDto
{
    public int RequestId { get; set; }
    public int? CitizenId { get; set; }
    public string? CitizenName { get; set; } = string.Empty;
    public string? CitizenPhone { get; set; } = string.Empty;
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public int? PriorityLevelId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? NumberOfAffectedPeople { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdatePriorityDto
{
    public int PriorityLevelId { get; set; }
}

public class UpdateRequestFromCoordinatorDto
{
    public string? Status { get; set; }
    public int? PriorityLevelId { get; set; }
}

public class SetPriorityAndVerifyDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "PriorityLevelId là bắt buộc")]
    public int PriorityLevelId { get; set; }
}

public class DashboardStatisticsDto
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int VerifiedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CitizenConfirmedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int DuplicateRequests { get; set; }
    public int TodayRequests { get; set; }
}

public class LatestRescueRequestDto
{
    public int RequestId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? NumberOfAffectedPeople { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO cho Guest xác nhận đã được cứu hộ (không cần đăng nhập).
/// Dùng số điện thoại đã đăng ký khi tạo yêu cầu để xác thực danh tính.
/// </summary>
public class GuestConfirmRescuedDto
{
    /// <summary>Số điện thoại đã nhập khi tạo yêu cầu cứu hộ</summary>
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    public string Phone { get; set; } = string.Empty;
}
