namespace cbor;

/// <summary>Reports an exceptional failure while validating data with CDDL.</summary>
public class CddlValidationException : System.Exception
{
    /// <summary>The stable validation failure code.</summary>
    public string ErrorCode;
    /// <summary>The validation issue category.</summary>
    public CddlValidationCode ValidationCode;
    /// <summary>The path to the affected instance value.</summary>
    public string? InstancePath;
    /// <summary>The exact schema source range associated with the failure.</summary>
    public CddlSourceSpan? Span;
    /// <summary>The containing rule name, or null when unavailable.</summary>
    public string? RuleName;

    /// <summary>Creates a CDDL validation exception with structured instance and schema metadata.</summary>
    /// <param name="errorCode">The stable validation failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="validationCode">The validation issue category.</param>
    /// <param name="instancePath">The instance path, or null when unavailable.</param>
    /// <param name="span">The schema source range, or null when unavailable.</param>
    /// <param name="ruleName">The containing rule name, or null when unavailable.</param>
    public CddlValidationException(
        string errorCode,
        string message,
        CddlValidationCode validationCode,
        string? instancePath,
        CddlSourceSpan? span,
        string? ruleName) : base(message)
    {
        ErrorCode = errorCode;
        ValidationCode = validationCode;
        InstancePath = instancePath;
        Span = span;
        RuleName = ruleName;
    }
}
