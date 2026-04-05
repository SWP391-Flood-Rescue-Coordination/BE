namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeamMember
{
    // The database has composite key on {team_id, user_id}
    public int TeamId { get; set; }
    public int UserId { get; set; }
    
    public string MemberRole { get; set; } = "Member"; // Leader | Member
    
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public int? RequestId { get; set; }

    // Navigation properties
    public virtual RescueTeam? Team { get; set; }
    public virtual User? User { get; set; }
    public virtual RescueRequest? Request { get; set; }
}
