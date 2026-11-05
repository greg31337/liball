namespace fix;

/// <summary>Describes one model or schema generation issue.</summary>
public class GenerationDiagnostic
{
    /// <summary>The issue severity.</summary>
    public ValidationSeverity Severity;
    /// <summary>A stable machine-readable diagnostic code.</summary>
    public string Code = "";
    /// <summary>A human-readable diagnostic description.</summary>
    public string Message = "";
    /// <summary>The source definition path or identifier.</summary>
    public string Source = "";
}
