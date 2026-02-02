namespace Flood_Rescue_Coordination.API.Models;

public class BlacklistedToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}