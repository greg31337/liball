namespace geo;

/// <summary>Represents an invalid or unusable precision configuration.</summary>
public class PrecisionException : System.Exception
{
    /// <summary>Creates an exception with an actionable precision failure message.</summary>
    /// <param name="message">The precision failure message.</param>
    public PrecisionException(string message) : base(message)
    {
    }
}
