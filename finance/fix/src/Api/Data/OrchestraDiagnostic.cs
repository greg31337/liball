namespace fix;

/// <summary>Describes one Orchestra parsing, schema, or semantic issue.</summary>
public class OrchestraDiagnostic
{
    /// <summary>The issue severity.</summary>
    public ValidationSeverity Severity;
    /// <summary>A stable machine-readable diagnostic code.</summary>
    public string Code = "";
    /// <summary>A human-readable diagnostic description.</summary>
    public string Message = "";
    /// <summary>The source name or URI.</summary>
    public string Source = "";
    /// <summary>The one-based source line, or zero when unavailable.</summary>
    public int Line;
    /// <summary>The one-based source column, or zero when unavailable.</summary>
    public int Column;
    /// <summary>The canonical element path.</summary>
    public string ElementPath = "";
}
