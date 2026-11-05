namespace geo;

/// <summary>Represents an operation attempted with a structurally or topologically invalid geometry.</summary>
public class InvalidGeometryException : System.Exception
{
    /// <summary>Creates an exception with an actionable invalid-geometry message.</summary>
    /// <param name="message">The invalid-geometry message.</param>
    public InvalidGeometryException(string message) : base(message)
    {
    }
}
