namespace Flood_Rescue_Coordination.API.DTOs;

public class CreateRescueRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Thông tin liên hệ cho khách vãng lai (không cần đăng nhập)
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
    public int? CitizenId { get; set; } // Nullable để hỗ trợ khách vãng lai
    public string CitizenName { get; set; } = string.Empty;
    public string CitizenPhone { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public int? PriorityLevelId { get; set; } // Thêm field này
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

public class UpdateRequestFromCoordinatorDto
{
    public string? Status { get; set; }
    public int? PriorityLevelId { get; set; }
}
