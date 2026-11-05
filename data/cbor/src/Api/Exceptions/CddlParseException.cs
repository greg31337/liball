namespace cbor;

/// <summary>Reports an exceptional failure while parsing CDDL source text.</summary>
public class CddlParseException : System.Exception
{
    /// <summary>The stable parser failure code.</summary>
    public string ErrorCode;
    /// <summary>The exact source range associated with the failure.</summary>
    public CddlSourceSpan? Span;

    /// <summary>Creates a CDDL parse exception with structured source metadata.</summary>
    /// <param name="errorCode">The stable parser failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="span">The source range, or null when unavailable.</param>
    public CddlParseException(
        string errorCode,
        string message,
        CddlSourceSpan? span) : base(message)
    {
        ErrorCode = errorCode;
        Span = span;
    }
}
