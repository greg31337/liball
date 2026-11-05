namespace geo;

/// <summary>Represents a failure to parse or decode a geometry representation.</summary>
public class GeometryParseException : System.Exception
{
    /// <summary>Creates an exception with an actionable parsing failure message.</summary>
    /// <param name="message">The parsing failure message.</param>
    public GeometryParseException(string message) : base(message)
    {
    }
}
