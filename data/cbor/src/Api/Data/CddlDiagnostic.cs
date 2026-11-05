namespace cbor;

/// <summary>Holds a parser or compiler diagnostic with exact source context.</summary>
public class CddlDiagnostic
{
    /// <summary>The diagnostic severity.</summary>
    public CddlDiagnosticSeverity Severity = CddlDiagnosticSeverity.Error;
    /// <summary>The stable diagnostic code.</summary>
    public string Code = "";
    /// <summary>The explanatory diagnostic message.</summary>
    public string Message = "";
    /// <summary>The source range associated with the diagnostic.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
    /// <summary>The containing rule name, or null when unavailable.</summary>
    public string? RuleName;
}
