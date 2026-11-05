namespace xml;

/// <summary>
/// Carries an immutable compiled XPath expression handle.
/// </summary>
public class XPathCompiledExpression
{
    internal XPathPlan? CompiledPlan;

    /// <summary>
    /// The XPath source text.
    /// </summary>
    public string? Source;
    /// <summary>
    /// The static context snapshot.
    /// </summary>
    public XPathStaticContext? StaticContext;
    /// <summary>
    /// The internal immutable plan handle.
    /// </summary>
    public object? Plan;
    /// <summary>
    /// The source range occupied by the expression.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The static diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// Whether the compiled expression supports concurrent evaluation.
    /// </summary>
    public bool IsThreadSafe;
}
