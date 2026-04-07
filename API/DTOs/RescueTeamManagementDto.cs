using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

/// <summary>
/// DTO hiển thị thông tin một thành viên team.
/// </summary>
public class RescueTeamMemberInfoDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MemberRole { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? RequestId { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// DTO tóm tắt team.
/// </summary>
public class RescueTeamSummaryDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? BaseLatitude { get; set; }
    public decimal? BaseLongitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public RescueTeamMemberInfoDto? Leader { get; set; }
}

/// <summary>
/// DTO chi tiết team.
/// </summary>
public class RescueTeamDetailDto : RescueTeamSummaryDto
{
    public List<RescueTeamMemberInfoDto> Members { get; set; } = new();
}

/// <summary>
/// Request tạo team mới.
/// </summary>
public class CreateRescueTeamRequest
{
    [Required]
    public string TeamName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public decimal? BaseLatitude { get; set; }
    public decimal? BaseLongitude { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "LeaderUserId phải lớn hơn 0.")]
    public int? LeaderUserId { get; set; }

    public List<int> MemberUserIds { get; set; } = new();
}

/// <summary>
/// Request thêm thành viên vào team.
/// </summary>
public class AddRescueTeamMemberRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "UserId phải lớn hơn 0.")]
    public int UserId { get; set; }
}

/// <summary>
/// Request đổi leader của team.
/// </summary>
public class ChangeRescueTeamLeaderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "LeaderUserId phải lớn hơn 0.")]
    public int LeaderUserId { get; set; }
}