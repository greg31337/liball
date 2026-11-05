namespace fix;

/// <summary>Describes one dictionary loading, normalization, or composition issue.</summary>
public class DictionaryDiagnostic
{
    /// <summary>The issue severity.</summary>
    public ValidationSeverity Severity;
    /// <summary>A stable machine-readable diagnostic code.</summary>
    public string Code = "";
    /// <summary>A human-readable diagnostic description.</summary>
    public string Message = "";
    /// <summary>The source name or path.</summary>
    public string Source = "";
    /// <summary>The one-based source line, or zero when unavailable.</summary>
    public int Line;
    /// <summary>The one-based source column, or zero when unavailable.</summary>
    public int Column;
    /// <summary>The affected definition name.</summary>
    public string DefinitionName = "";
    /// <summary>Whether the diagnostic identifies a field tag.</summary>
    public bool HasTag;
    /// <summary>The affected field tag.</summary>
    public int Tag;
}
