namespace xml;

/// <summary>
/// Carries the principal and secondary outcomes of an XSLT transformation.
/// </summary>
public class XsltTransformResult
{
    /// <summary>
    /// The executable that was invoked.
    /// </summary>
    public XsltExecutable? Executable;
    /// <summary>
    /// The transformation options snapshot.
    /// </summary>
    public XsltTransformOptions? Options;
    /// <summary>
    /// The principal result sequence.
    /// </summary>
    public XdmSequence? PrincipalValue;
    /// <summary>
    /// The principal result document when one was constructed.
    /// </summary>
    public XmlDocument? PrincipalDocument;
    /// <summary>
    /// The secondary results.
    /// </summary>
    public XsltSecondaryResult[]? SecondaryResults;
    /// <summary>
    /// The emitted messages.
    /// </summary>
    public XsltMessage[]? Messages;
    /// <summary>
    /// The collected trace events.
    /// </summary>
    public XsltTraceEvent[]? TraceEvents;
    /// <summary>
    /// The transformation diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// The principal result disposition.
    /// </summary>
    public XsltResultDisposition Disposition;
    /// <summary>
    /// Whether the transformation completed successfully.
    /// </summary>
    public bool Succeeded;
}

