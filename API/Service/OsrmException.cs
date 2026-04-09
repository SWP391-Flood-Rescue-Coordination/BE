namespace Flood_Rescue_Coordination.API.Services;

public class OsrmException : Exception
{
    public OsrmException(string message) : base(message)
    {
    }

    public OsrmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
