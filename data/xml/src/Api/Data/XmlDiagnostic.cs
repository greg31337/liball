namespace xml;

/// <summary>
/// Carries a diagnostic produced while processing XML.
/// </summary>
public class XmlDiagnostic
{
    /// <summary>
    /// The stable library or standard error code.
    /// </summary>
    public string? Code;
    /// <summary>
    /// The error category.
    /// </summary>
    public XmlErrorCategory Category;
    /// <summary>
    /// The diagnostic severity.
    /// </summary>
    public XmlDiagnosticSeverity Severity;
    /// <summary>
    /// The diagnostic message.
    /// </summary>
    public string? Message;
    /// <summary>
    /// The primary source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// A related source location.
    /// </summary>
    public SourceLocation? RelatedLocation;
    /// <summary>
    /// The outermost-to-innermost entity stack.
    /// </summary>
    public string[]? EntityStack;
    /// <summary>
    /// The outermost-to-innermost include stack.
    /// </summary>
    public string[]? IncludeStack;
    /// <summary>
    /// The outermost-to-innermost template stack.
    /// </summary>
    public string[]? TemplateStack;
    /// <summary>
    /// The outermost-to-innermost stylesheet function stack.
    /// </summary>
    public string[]? FunctionStack;
    /// <summary>
    /// The outermost-to-innermost XSLT component stack.
    /// </summary>
    public string[]? ComponentStack;
}
