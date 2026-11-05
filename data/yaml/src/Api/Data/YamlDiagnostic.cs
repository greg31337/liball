namespace yaml;

/// <summary>
/// Describes one informational message, warning, or error with its source location.
/// </summary>
public class YamlDiagnostic
{
    /// <summary>
    /// The diagnostic severity.
    /// </summary>
    public YamlDiagnosticSeverity Severity;
    /// <summary>
    /// The stable diagnostic code intended for programmatic filtering.
    /// </summary>
    public string Code;
    /// <summary>
    /// The plain-English diagnostic message.
    /// </summary>
    public string Message;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a new YAML diagnostic with empty or default state.
    /// </summary>
    public YamlDiagnostic()
    {
        Severity = YamlDiagnosticSeverity.Error;
        Code = "";
        Message = "";
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates a diagnostic with the supplied severity, stable code, message, and source range.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML diagnostic.</returns>
    internal static YamlDiagnostic Create(YamlDiagnosticSeverity severity, string? code, string? message, YamlSourceSpan? span)
    {
        YamlDiagnostic diagnostic = new YamlDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code == null ? "" : code;
        diagnostic.Message = message == null ? "" : message;
        diagnostic.Span = span == null ? YamlSourceSpan.Empty() : span;
        return diagnostic;
    }

    /// <summary>
    /// Creates an error diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML diagnostic.</returns>
    internal static YamlDiagnostic Error(string? code, string? message, YamlSourceSpan? span)
    {
        return Create(YamlDiagnosticSeverity.Error, code, message, span);
    }

    /// <summary>
    /// Creates a warning diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML diagnostic.</returns>
    internal static YamlDiagnostic Warning(string? code, string? message, YamlSourceSpan? span)
    {
        return Create(YamlDiagnosticSeverity.Warning, code, message, span);
    }

    /// <summary>
    /// Creates an informational diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML diagnostic.</returns>
    internal static YamlDiagnostic Info(string? code, string? message, YamlSourceSpan? span)
    {
        return Create(YamlDiagnosticSeverity.Info, code, message, span);
    }

    /// <summary>
    /// Determines whether this diagnostic has error severity.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsError()
    {
        return Severity == YamlDiagnosticSeverity.Error;
    }

    /// <summary>
    /// Determines whether this diagnostic has warning severity.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsWarning()
    {
        return Severity == YamlDiagnosticSeverity.Warning;
    }

    /// <summary>
    /// Determines whether this diagnostic has informational severity.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsInfo()
    {
        return Severity == YamlDiagnosticSeverity.Info;
    }

    /// <summary>
    /// Formats the severity, code, message, and source position as one readable line.
    /// </summary>
    /// <returns>The emitted or formatted text.</returns>
    internal string Format()
    {
        return Severity.ToString()
            + " "
            + Code
            + " at "
            + Span.Format()
            + ": "
            + Message;
    }
}
