namespace xml;

/// <summary>
/// Carries one immutable XPath trace observation.
/// </summary>
public class XPathTraceEvent
{
    /// <summary>
    /// The trace label.
    /// </summary>
    public string? Label;
    /// <summary>
    /// The immutable traced value.
    /// </summary>
    public XdmSequence? Value;
    /// <summary>
    /// The source range associated with the trace operation.
    /// </summary>
    public SourceRange? Range;
}
