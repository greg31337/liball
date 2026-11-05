namespace xml;

/// <summary>
/// Carries an XPath evaluation value and diagnostics.
/// </summary>
public class XPathEvaluationResult
{
    /// <summary>
    /// The expression that was evaluated.
    /// </summary>
    public XPathCompiledExpression? Expression;
    /// <summary>
    /// The dynamic context snapshot.
    /// </summary>
    public XPathDynamicContext? DynamicContext;
    /// <summary>
    /// The resulting XDM sequence.
    /// </summary>
    public XdmSequence? Value;
    /// <summary>
    /// The evaluation diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// The trace events collected during evaluation when the baseline trace collector is used.
    /// </summary>
    public XPathTraceEvent[]? TraceEvents;
    /// <summary>
    /// The standard error code when evaluation failed.
    /// </summary>
    public string? ErrorCode;
    /// <summary>
    /// Whether evaluation completed successfully.
    /// </summary>
    public bool Succeeded;
}
