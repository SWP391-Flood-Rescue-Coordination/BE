namespace Flood_Rescue_Coordination.API.DTOs;

public class RescueTeamDistanceDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;


    public decimal BaseLatitude { get; set; }
    public decimal BaseLongitude { get; set; }

    public decimal RequestLatitude { get; set; }
    public decimal RequestLongitude { get; set; }

    public double DistanceKm { get; set; }
}