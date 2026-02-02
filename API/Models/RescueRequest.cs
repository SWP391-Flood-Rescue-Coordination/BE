using System;

namespace Flood_Rescue_Coordination.API.Models;

public class RescueRequest
{
    public int RequestId { get; set; }
    public int? CitizenId { get; set; } // Cho phép null (khách vãng lai)
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Thông tin liên hệ cho khách vãng lai
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public int? PriorityLevelId { get; set; }
    public string Status { get; set; } = "PENDING";
    public int NumberOfPeople { get; set; }
    public bool HasChildren { get; set; }
    public bool HasElderly { get; set; }
    public bool HasDisabled { get; set; }
    public string SpecialNotes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual User? Citizen { get; set; }
}
