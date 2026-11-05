namespace fake;

/// <summary>
/// Represents a geographic point in decimal degrees.
/// </summary>
public class GeoCoordinate
{
    /// <summary>
    /// The latitude in degrees from negative 90 through positive 90.
    /// </summary>
    public double Latitude;

    /// <summary>
    /// The longitude in degrees from negative 180 through positive 180.
    /// </summary>
    public double Longitude;

    internal GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
