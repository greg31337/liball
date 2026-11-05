namespace cbor;

/// <summary>Reports an exceptional failure while compiling a CDDL schema.</summary>
public class CddlCompileException : System.Exception
{
    /// <summary>The stable compiler failure code.</summary>
    public string ErrorCode;
    /// <summary>The exact schema source range associated with the failure.</summary>
    public CddlSourceSpan? Span;
    /// <summary>The containing rule name, or null when unavailable.</summary>
    public string? RuleName;

    /// <summary>Creates a CDDL compile exception with structured source metadata.</summary>
    /// <param name="errorCode">The stable compiler failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="span">The source range, or null when unavailable.</param>
    /// <param name="ruleName">The containing rule name, or null when unavailable.</param>
    public CddlCompileException(
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
