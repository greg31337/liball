namespace geo;

/// <summary>Represents a general geometry operation failure.</summary>
public class GeometryException : System.Exception
{
    /// <summary>Creates an exception with an actionable failure message.</summary>
    /// <param name="message">The failure message.</param>
    public GeometryException(string message) : base(message)
    {
    }
}
