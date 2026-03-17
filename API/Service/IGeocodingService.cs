namespace Flood_Rescue_Coordination.API.Services;

public interface IGeocodingService
{
    Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}
