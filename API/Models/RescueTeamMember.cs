namespace Flood_Rescue_Coordination.API.Models;

public class RescueTeamMember
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string MemberRole { get; set; } = "Member"; // Leader | Member
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
