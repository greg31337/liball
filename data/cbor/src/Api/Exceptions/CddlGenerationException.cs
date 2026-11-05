namespace cbor;

/// <summary>Reports an exceptional failure while generating data from CDDL.</summary>
public class CddlGenerationException : System.Exception
{
    /// <summary>The stable generation failure code.</summary>
    public string ErrorCode;
    /// <summary>The exact schema source range associated with the failure.</summary>
    public CddlSourceSpan? Span;
    /// <summary>The containing rule name, or null when unavailable.</summary>
    public string? RuleName;

    /// <summary>Creates a CDDL generation exception with structured schema metadata.</summary>
    /// <param name="errorCode">The stable generation failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="span">The schema source range, or null when unavailable.</param>
    /// <param name="ruleName">The containing rule name, or null when unavailable.</param>
    public CddlGenerationException(
        string errorCode,
        string message,
        CddlSourceSpan? span,
        string? ruleName) : base(message)
    {
        ErrorCode = errorCode;
        Span = span;
        RuleName = ruleName;
    }
}
