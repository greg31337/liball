namespace xml;

/// <summary>
/// Carries the streamability classification of one compiled XSLT construct.
/// </summary>
public class XsltStreamabilityConstruct
{
    /// <summary>
    /// The construct family, such as expression, instruction, template, mode, or package.
    /// </summary>
    public string? Kind;
    /// <summary>
    /// The compiled construct name or descriptive token.
    /// </summary>
    public string? Name;
    /// <summary>
    /// The guaranteed-streamability outcome for this construct.
    /// </summary>
    public XsltStreamability Streamability;
    /// <summary>
    /// The posture inferred for this construct.
    /// </summary>
    public XsltPosture Posture;
    /// <summary>
    /// The sweep inferred for this construct.
    /// </summary>
    public XsltSweep Sweep;
    /// <summary>
    /// The number of consuming operands combined by this construct.
    /// </summary>
    public int ConsumingOperandCount;
    /// <summary>
    /// The reason guaranteed streamability was not established, or null when it was established.
    /// </summary>
    public string? Reason;
    /// <summary>
    /// The source range of the construct, or null when unavailable.
    /// </summary>
    public SourceRange? Location;
    /// <summary>
    /// Whether guaranteed streamability is required for this construct by its declaration or containing mode.
    /// </summary>
    public bool Required;
    /// <summary>
    /// Whether this construct requires grounded fallback when it is evaluated.
    /// </summary>
    public bool RequiresFallback;
}
