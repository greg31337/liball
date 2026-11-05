namespace xml;

/// <summary>
/// Carries the result of guaranteed-streamability analysis.
/// </summary>
public class XsltStreamabilityReport
{
    /// <summary>
    /// The overall streamability outcome.
    /// </summary>
    public XsltStreamability Streamability;
    /// <summary>
    /// The reasons for a non-streamable or unknown result.
    /// </summary>
    public string[]? Reasons;
    /// <summary>
    /// The source ranges associated with the reasons.
    /// </summary>
    public SourceRange[]? Locations;
    /// <summary>
    /// The analyzed modes.
    /// </summary>
    public XmlName[]? Modes;
    /// <summary>
    /// The per-construct streamability classifications.
    /// </summary>
    public XsltStreamabilityConstruct[]? Constructs;
    /// <summary>
    /// The conformance level used for the analysis.
    /// </summary>
    public XsltConformanceLevel ConformanceLevel;
    /// <summary>
    /// Whether at least one analyzed construct requires grounded fallback.
    /// </summary>
    public bool RequiresFallback;
    /// <summary>
    /// Whether analysis used schema-aware type information.
    /// </summary>
    public bool SchemaAware;
}
