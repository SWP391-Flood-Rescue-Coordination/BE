using System;

namespace Flood_Rescue_Coordination.API.Models;

public class RescueRequest
{
    public int RequestId { get; set; }
    public int CitizenId { get; set; }
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public int? PriorityLevelId { get; set; }
    public string Status { get; set; } = "Pending";
    public int? NumberOfAffectedPeople { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual User? Citizen { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}
