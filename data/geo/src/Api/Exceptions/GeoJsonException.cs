namespace geo;

/// <summary>Represents a GeoJSON validation, reading, or writing failure.</summary>
public class GeoJsonException : System.Exception
{
    /// <summary>Creates an exception with an actionable GeoJSON failure message.</summary>
    /// <param name="message">The GeoJSON failure message.</param>
    public GeoJsonException(string message) : base(message)
    {
    }
}
