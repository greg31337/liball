namespace xml;

/// <summary>
/// Carries one XSLT execution trace event.
/// </summary>
public class XsltTraceEvent
{
    /// <summary>
    /// The trace event kind.
    /// </summary>
    public XsltTraceEventKind Kind;
    /// <summary>
    /// The related template, function, mode, or component name.
    /// </summary>
    public XmlName? ComponentName;
    /// <summary>
    /// The instruction display name.
    /// </summary>
    public string? InstructionName;
    /// <summary>
    /// The trace message.
    /// </summary>
    public string? Message;
    /// <summary>
    /// The stylesheet source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The execution stack depth.
    /// </summary>
    public int Depth;
    /// <summary>
    /// The context item represented as an empty or singleton sequence.
    /// </summary>
    public XdmSequence? ContextItem;
}

