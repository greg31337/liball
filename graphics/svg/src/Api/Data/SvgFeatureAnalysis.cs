namespace svg;

/// <summary>Configures bounded policy-aware per-document feature analysis.</summary>
public class SvgFeatureAnalysisOptions
{
    /// <summary>The positive maximum number of document nodes inspected.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum number of located feature entries emitted.</summary>
    public int MaximumEntries;
    /// <summary>The CSS parser limits used to inspect authored styles.</summary>
    public SvgCssLimits CssLimits;
    /// <summary>The resource policy used to classify authored acquisition requests.</summary>
    public SvgResourcePolicy ResourcePolicy;
    /// <summary>Host-supplied feature identifiers available through callbacks.</summary>
    public SvgCapabilitySet HostCapabilities;
    /// <summary>Whether external navigation requests may be returned to the host.</summary>
    public bool AllowExternalLinks;
    /// <summary>The cancellation token observed during bounded document traversal.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates bounded defaults with no host callbacks and caller-mediated resource policy.</summary>
    public SvgFeatureAnalysisOptions()
    {
        MaximumNodes = 1000000;
        MaximumEntries = 1000000;
        CssLimits = new SvgCssLimits();
        ResourcePolicy = new SvgResourcePolicy();
        HostCapabilities = new SvgCapabilitySet();
        AllowExternalLinks = false;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains a deterministic dynamic feature report for one document version.</summary>
public class SvgFeatureAnalysisResult
{
    /// <summary>The analyzed document, or <c>null</c> for invalid input.</summary>
    public SvgDocument? Document;
    /// <summary>The document version captured at analysis time.</summary>
    public long SourceVersion;
    /// <summary>The located feature occurrences and related diagnostics.</summary>
    public SvgFeatureReport FeatureReport;
    /// <summary>Unique dynamically used feature identifiers in first-use document order.</summary>
    public System.Collections.Generic.List<string> DynamicallyUsedFeatures;
    /// <summary>The number of nodes inspected.</summary>
    public int InspectedNodes;
    /// <summary>The number of implemented occurrences.</summary>
    public int ImplementedCount;
    /// <summary>The number of callback-required occurrences.</summary>
    public int CallbackRequiredCount;
    /// <summary>The number of preserve-only occurrences.</summary>
    public int PreserveOnlyCount;
    /// <summary>The number of policy-disabled occurrences.</summary>
    public int PolicyDisabledCount;
    /// <summary>The number of unsupported occurrences.</summary>
    public int UnsupportedCount;
    /// <summary>The stable operational failure.</summary>
    public SvgFeatureAnalysisError Error;
    /// <summary>Whether bounded analysis completed.</summary>
    public bool Success;

    internal SvgFeatureAnalysisResult(SvgDocument? document)
    {
        Document = document;
        SourceVersion = document == null ? 0 : document.Version;
        FeatureReport = new SvgFeatureReport();
        DynamicallyUsedFeatures = new System.Collections.Generic.List<string>();
        InspectedNodes = 0;
        ImplementedCount = 0;
        CallbackRequiredCount = 0;
        PreserveOnlyCount = 0;
        PolicyDisabledCount = 0;
        UnsupportedCount = 0;
        Error = SvgFeatureAnalysisError.None;
        Success = false;
    }

    /// <summary>Checks whether the analyzed document retains its captured version.</summary>
    /// <returns><c>true</c> when the analysis still describes the current document.</returns>
    internal bool IsCurrent()
    {
        return Document != null && Document.Version == SourceVersion;
    }
}
