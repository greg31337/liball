namespace uri;

/// <summary>
/// Describes a validation, parsing, conversion, or processing condition.
/// </summary>
public class UriDiagnostic
{
    /// <summary>The operation family that produced the diagnostic.</summary>
    public DiagnosticDomain Domain;

    /// <summary>The stable programmatic diagnostic code.</summary>
    public UriDiagnosticCode Code;

    /// <summary>The diagnostic severity.</summary>
    public DiagnosticSeverity Severity;

    /// <summary>The component associated with the diagnostic.</summary>
    public UriComponent Component;

    /// <summary>The exact input or output range associated with the diagnostic.</summary>
    public TextRange Range;

    /// <summary>A stable, human-readable description that does not include input text.</summary>
    public string Message;

    /// <summary>
    /// Initializes a diagnostic.
    /// </summary>
    /// <param name="domain">The operation family.</param>
    /// <param name="code">The programmatic code.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="component">The associated component.</param>
    /// <param name="range">The associated text range.</param>
    /// <param name="message">The human-readable message.</param>
    public UriDiagnostic(
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        DiagnosticSeverity severity,
        UriComponent component,
        TextRange range,
        string message)
    {
        Domain = domain;
        Code = code;
        Severity = severity;
        Component = component;
        Range = range;
        Message = message;
    }
}
