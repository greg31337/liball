namespace cbor;

/// <summary>Holds a generated CBOR value or a structured generation failure.</summary>
public class CddlGenerationResult
{
    /// <summary>Indicates whether a valid value was generated.</summary>
    public bool IsSuccess;
    /// <summary>The generated CBOR value.</summary>
    public CborValue? Value;
    /// <summary>The selected root rule name.</summary>
    public string? RuleName;
    /// <summary>The stable failure code, or null on success.</summary>
    public string? ErrorCode;
    /// <summary>The explanatory failure message, or null on success.</summary>
    public string? ErrorMessage;
    /// <summary>The schema source range associated with a failure.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
    /// <summary>The last custom control result, when relevant.</summary>
    public CddlControlResult? ControlResult;
    /// <summary>Validation issues explaining why a generated candidate was rejected.</summary>
    public CddlValidationIssue[] ValidationIssues = [];
    /// <summary>The options used for the operation.</summary>
    public CddlGenerationOptions Options = new CddlGenerationOptions();
}
