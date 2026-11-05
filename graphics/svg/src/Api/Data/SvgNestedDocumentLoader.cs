namespace svg;

/// <summary>Describes one resolved external SVG document request and fragment.</summary>
public class SvgNestedDocumentRequest
{
    /// <summary>The URI text authored by the containing SVG resource.</summary>
    public string AuthoredUri;
    /// <summary>The effective base URI of the containing SVG resource.</summary>
    public string BaseUri;
    /// <summary>The resolved fragment-free resource URI.</summary>
    public string ResolvedUri;
    /// <summary>The preserved fragment text without the number sign.</summary>
    public string Fragment;
    /// <summary>The exact source span that requested the nested document.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The stable zero-based resource ordinal within the containing resource.</summary>
    public int ResourceIndex;
    /// <summary>The nested document depth, where a direct child has depth one.</summary>
    public int ReferenceDepth;

    /// <summary>Creates one nested SVG document request.</summary>
    /// <param name="authoredUri">The authored URI text.</param>
    /// <param name="baseUri">The containing resource base URI.</param>
    /// <param name="resolvedUri">The resolved fragment-free resource URI.</param>
    /// <param name="fragment">The preserved fragment without the number sign.</param>
    public SvgNestedDocumentRequest(string authoredUri, string baseUri, string resolvedUri, string fragment)
    {
        if (authoredUri == null || authoredUri.Length == 0) throw new System.ArgumentOutOfRangeException("authoredUri");
        if (baseUri == null) throw new System.ArgumentNullException("baseUri");
        if (resolvedUri == null || resolvedUri.Length == 0) throw new System.ArgumentOutOfRangeException("resolvedUri");
        if (fragment == null) throw new System.ArgumentNullException("fragment");
        AuthoredUri = authoredUri;
        BaseUri = baseUri;
        ResolvedUri = resolvedUri;
        Fragment = fragment;
        Source = null;
        ResourceIndex = 0;
        ReferenceDepth = 1;
    }
}

/// <summary>Shares bounded cache and accounting state across nested document loads.</summary>
public class SvgNestedDocumentContext
{
    /// <summary>The bounded normalized resource cache.</summary>
    public SvgResourceCache Cache;
    /// <summary>The caller requests, successful unique loads, and unique encoded bytes.</summary>
    public SvgResourceSummary Resources;
    /// <summary>The resource attempts charged against the total count budget.</summary>
    public int AccountedCount;
    /// <summary>The encoded resource bytes charged before this nested context began.</summary>
    public long InheritedEncodedBytes;

    /// <summary>Creates a context bounded by one resource-limit group.</summary>
    /// <param name="limits">The positive resource capacities.</param>
    public SvgNestedDocumentContext(SvgResourceLimits limits)
    {
        if (limits == null) throw new System.ArgumentNullException("limits");
        if (limits.MaxCount < 1 || limits.MaxTotalBytes < 1) throw new System.ArgumentOutOfRangeException("limits");
        Cache = new SvgResourceCache(limits.MaxCount, limits.MaxTotalBytes);
        Resources = new SvgResourceSummary();
        AccountedCount = 0;
        InheritedEncodedBytes = 0;
    }

    internal void Add(SvgResourceAcquisitionResult acquisition)
    {
        AccountedCount = AccountedCount + acquisition.BudgetCount;
        Resources.RequestCount = Resources.RequestCount + acquisition.RequestCount;
        Resources.LoadedCount = Resources.LoadedCount + acquisition.LoadedCount;
        if (acquisition.LoadedCount != 0) Resources.EncodedBytes = Resources.EncodedBytes + acquisition.Bytes.LongLength;
    }
}

/// <summary>Configures one nested SVG acquisition and parse operation.</summary>
public class SvgNestedDocumentOptions
{
    /// <summary>The inherited processing, policy, callback, cancellation, and parser limits.</summary>
    public SvgLoadOptions LoadOptions;
    /// <summary>The shared cache and total resource accounting context.</summary>
    public SvgNestedDocumentContext Context;

    /// <summary>Creates secure nested document defaults.</summary>
    public SvgNestedDocumentOptions()
    {
        LoadOptions = new SvgLoadOptions();
        Context = new SvgNestedDocumentContext(LoadOptions.Limits.Resources);
    }
}

/// <summary>Contains one loaded nested SVG document and selected fragment.</summary>
public class SvgNestedDocumentResult
{
    /// <summary>Whether acquisition, parsing, and fragment selection succeeded.</summary>
    public bool Success;
    /// <summary>The parsed nested document, or <c>null</c>.</summary>
    public SvgDocument? Document;
    /// <summary>The selected fragment element or document element, or <c>null</c>.</summary>
    public SvgNode? Target;
    /// <summary>The caller-mediated acquisition result, or <c>null</c> before acquisition.</summary>
    public SvgResourceAcquisitionResult? Acquisition;
    /// <summary>The pre-acquisition resource-policy decision.</summary>
    public SvgResourcePolicyDecision? InitialPolicyDecision;
    /// <summary>The post-acquisition final-URI and media policy decision.</summary>
    public SvgResourcePolicyDecision? FinalPolicyDecision;
    /// <summary>The complete nested load result, or <c>null</c> before parsing.</summary>
    public SvgLoadResult? LoadResult;
    /// <summary>The ordered request, acquisition, parse, and fragment diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgNestedDocumentResult()
    {
        Success = false;
        Document = null;
        Target = null;
        Acquisition = null;
        InitialPolicyDecision = null;
        FinalPolicyDecision = null;
        LoadResult = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}
