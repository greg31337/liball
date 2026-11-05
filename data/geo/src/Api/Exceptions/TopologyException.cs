namespace geo;

/// <summary>Represents a failure to construct or evaluate a robust topology.</summary>
public class TopologyException : System.Exception
{
    /// <summary>Creates an exception with an actionable topology failure message.</summary>
    /// <param name="message">The topology failure message.</param>
    public TopologyException(string message) : base(message)
    {
    }
}
