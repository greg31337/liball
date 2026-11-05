namespace geo;

/// <summary>Represents an invalid spatial-index operation or lifecycle transition.</summary>
public class SpatialIndexException : System.Exception
{
    /// <summary>Creates an exception with an actionable spatial-index failure message.</summary>
    /// <param name="message">The spatial-index failure message.</param>
    public SpatialIndexException(string message) : base(message)
    {
    }
}
