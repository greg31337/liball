namespace cbor;

/// <summary>Describes one CDDL schema mismatch or validation resource failure.</summary>
public class CddlValidationIssue
{
    /// <summary>The validation issue category.</summary>
    public CddlValidationCode Code = CddlValidationCode.None;
    /// <summary>The issue severity.</summary>
    public CddlDiagnosticSeverity Severity = CddlDiagnosticSeverity.Error;
    /// <summary>The explanatory issue message.</summary>
    public string Message = "";
    /// <summary>The path to the mismatching instance value.</summary>
    public string InstancePath = "$";
    /// <summary>The encoded byte offset of the mismatching value, or negative one when unavailable.</summary>
    public long ByteOffset = -1;
    /// <summary>The path through the selected schema rules and nodes.</summary>
    public string SchemaPath = "";
    /// <summary>The source range of the failing schema expression.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
    /// <summary>The containing rule name, or null when unavailable.</summary>
    public string? RuleName;
    /// <summary>A concise description of the expected value.</summary>
    public string? Expected;
    /// <summary>A concise description of the actual value.</summary>
    public string? Actual;
    /// <summary>Bounded nested issues from rejected alternatives.</summary>
    public CddlValidationIssue[] Details = [];
}
