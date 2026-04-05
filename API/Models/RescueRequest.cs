using System;

namespace Flood_Rescue_Coordination.API.Models;

public class RescueRequest
{
    public int RequestId { get; set; }
    
    // Supporting both logged-in citizens and anonymous guests
    // Program.cs handles ALTER COLUMN citizen_id INT NULL
    public int? CitizenId { get; set; } 
    
    public string? Title { get; set; } = string.Empty;
    public string? Phone { get; set; } = string.Empty; // Normal contact phone
    public string? Description { get; set; } = string.Empty;
    
    // Guest contact info - Program.cs handles ALTER TABLE ADD these columns
    public string? ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; } = string.Empty;
    public int? PriorityLevelId { get; set; }
    public string Status { get; set; } = "Pending";
    
    // In SQL script
    
    public int? AdultCount { get; set; }
    public int? ElderlyCount { get; set; }
    public int? ChildrenCount { get; set; }
    public int? NumberOfAffectedPeople { get; set; }
    
    public int? TeamId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }


    // Navigation properties
    public virtual User? Citizen { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}
