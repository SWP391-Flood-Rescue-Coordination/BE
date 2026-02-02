namespace Flood_Rescue_Coordination.API.DTOs;

public class CreateRescueRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Thông tin người gửi (bắt buộc nếu không đăng nhập)
    public string? ContactName { get; set; } 
    public string? ContactPhone { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public int NumberOfPeople { get; set; } = 1;
    public bool HasChildren { get; set; } = false;
    public bool HasElderly { get; set; } = false;
    public bool HasDisabled { get; set; } = false;
    public string? SpecialNotes { get; set; }
}

public class UpdateRescueRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public int? NumberOfPeople { get; set; }
    public bool? HasChildren { get; set; }
    public bool? HasElderly { get; set; }
    public bool? HasDisabled { get; set; }
    public string? SpecialNotes { get; set; }
}

public class RescueRequestResponseDto
{
    public int RequestId { get; set; }
    public int? CitizenId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public string CitizenPhone { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public int NumberOfPeople { get; set; }
    public bool HasChildren { get; set; }
    public bool HasElderly { get; set; }
    public bool HasDisabled { get; set; }
    public string? SpecialNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdatePriorityDto
{
    public int PriorityLevelId { get; set; }
}

public class DashboardStatisticsDto
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; } // Chờ xác nhận
    public int InProgressRequests { get; set; } // Đang cứu hộ
    public int CompletedRequests { get; set; } // Đã cứu hộ
    public int CancelledRequests { get; set; } // Đã hủy
    
    // Thống kê theo độ ưu tiên (nếu có priority system sau này)
    public int HighPriorityRequests { get; set; }
    public int MediumPriorityRequests { get; set; }
    public int LowPriorityRequests { get; set; }

    public int TodayRequests { get; set; }
}

public class LatestRescueRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasElderly { get; set; }
    public bool HasChildren { get; set; }
    public bool HasDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

