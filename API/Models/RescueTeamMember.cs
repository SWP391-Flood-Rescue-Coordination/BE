namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeamMember
{
    public int MemberId { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "MEMBER";
    public string? Specialization { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual RescueTeam? Team { get; set; }
    public virtual User? User { get; set; }
}
