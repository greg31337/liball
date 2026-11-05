namespace svg;

/// <summary>Configures bounded focus-order and activation planning.</summary>
public class SvgNavigationOptions
{
    /// <summary>The positive maximum resolved nodes visited.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum URI character count.</summary>
    public int MaximumUriCharacters;

    /// <summary>Creates secure navigation-planning defaults.</summary>
    public SvgNavigationOptions()
    {
        MaximumNodes = 1000000;
        MaximumUriCharacters = SvgUri.DefaultMaxCharacters;
    }
}

/// <summary>Contains one focusable source or use-instance identity.</summary>
public class SvgFocusEntry
{
    /// <summary>The focusable resolved node.</summary>
    public SvgResolvedNode Node;
    /// <summary>The corresponding authored source element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>Whether this entry belongs to a generated use instance.</summary>
    public bool IsInstance;
    /// <summary>Whether an explicit valid <c>tabindex</c> was present.</summary>
    public bool HasTabIndex;
    /// <summary>The explicit tab index, or zero for implicit focusability.</summary>
    public int TabIndex;
    /// <summary>Whether the node participates in sequential focus navigation.</summary>
    public bool Sequential;
    /// <summary>Whether a usable link supplied implicit focusability.</summary>
    public bool ImplicitLink;

    internal SvgFocusEntry(SvgResolvedNode node, bool hasTabIndex, int tabIndex, bool sequential, bool implicitLink)
    {
        Node = node;
        SourceNode = node.SourceNode;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        IsInstance = node.InstancePath != null && node.InstancePath.Length != 0;
        HasTabIndex = hasTabIndex;
        TabIndex = tabIndex;
        Sequential = sequential;
        ImplicitLink = implicitLink;
    }
}

/// <summary>Contains focusable entries and their deterministic sequential order.</summary>
public class SvgFocusOrderResult
{
    /// <summary>Whether bounded focus-order computation completed.</summary>
    public bool Success;
    /// <summary>The stable computation failure.</summary>
    public SvgNavigationError Error;
    /// <summary>Every focusable entry in resolved document order.</summary>
    public System.Collections.Generic.List<SvgFocusEntry> Entries;
    /// <summary>Sequential entries with positive indices first and zero or implicit entries afterward.</summary>
    public System.Collections.Generic.List<SvgFocusEntry> TabOrder;
    /// <summary>The number of resolved nodes visited.</summary>
    public int VisitedNodeCount;
    /// <summary>Diagnostics for invalid authored tab indices.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgFocusOrderResult()
    {
        Success = false;
        Error = SvgNavigationError.None;
        Entries = new System.Collections.Generic.List<SvgFocusEntry>();
        TabOrder = new System.Collections.Generic.List<SvgFocusEntry>();
        VisitedNodeCount = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}

/// <summary>Describes navigation the host may choose to perform.</summary>
public class SvgNavigationRequest
{
    /// <summary>The requested host action kind.</summary>
    public SvgNavigationRequestKind Kind;
    /// <summary>The activated authored link element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The activated resolved link node.</summary>
    public SvgResolvedNode Node;
    /// <summary>The activated link's authored source path.</summary>
    public string SourcePath;
    /// <summary>The activated link's use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>The selected authored href text.</summary>
    public string Href;
    /// <summary>The normalized Unicode target IRI.</summary>
    public string NormalizedIri;
    /// <summary>The normalized ASCII target URI.</summary>
    public string NormalizedUri;
    /// <summary>The normalized fragment-free resource IRI.</summary>
    public string ResourceIri;
    /// <summary>The decoded fragment identifier, or an empty string.</summary>
    public string Fragment;
    /// <summary>Whether the target includes a fragment delimiter.</summary>
    public bool HasFragment;
    /// <summary>The requested browsing-context name.</summary>
    public string Target;
    /// <summary>Whether download behavior was requested.</summary>
    public bool HasDownload;
    /// <summary>The suggested download filename, or an empty string.</summary>
    public string Download;
    /// <summary>The normalized linked-resource language, or an empty string.</summary>
    public string HrefLanguage;
    /// <summary>The linked-resource media type, or <c>null</c>.</summary>
    public SvgMediaType? MediaType;
    /// <summary>The normalized link relation tokens.</summary>
    public System.Collections.Generic.List<string> Relations;

    internal SvgNavigationRequest(SvgResolvedNode node)
    {
        Kind = SvgNavigationRequestKind.None;
        SourceNode = node.SourceNode;
        Node = node;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        Href = "";
        NormalizedIri = "";
        NormalizedUri = "";
        ResourceIri = "";
        Fragment = "";
        HasFragment = false;
        Target = "";
        HasDownload = false;
        Download = "";
        HrefLanguage = "";
        MediaType = null;
        Relations = new System.Collections.Generic.List<string>();
    }
}

/// <summary>Contains one side-effect-free link or fragment activation record.</summary>
public class SvgActivationRecord
{
    /// <summary>Whether activation planning completed successfully.</summary>
    public bool Success;
    /// <summary>The stable activation failure.</summary>
    public SvgNavigationError Error;
    /// <summary>The activation kind.</summary>
    public SvgActivationKind Kind;
    /// <summary>The originally activated resolved target or descendant.</summary>
    public SvgResolvedNode? ActivatedNode;
    /// <summary>The resolved link ancestor selected for activation.</summary>
    public SvgResolvedNode? LinkNode;
    /// <summary>The same-document fragment target, or <c>null</c>.</summary>
    public SvgNode? FragmentTarget;
    /// <summary>The host navigation request, or <c>null</c> after failure.</summary>
    public SvgNavigationRequest? Request;
    /// <summary>Located activation diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgActivationRecord(SvgResolvedNode? activated)
    {
        Success = false;
        Error = SvgNavigationError.None;
        Kind = SvgActivationKind.None;
        ActivatedNode = activated;
        LinkNode = null;
        FragmentTarget = null;
        Request = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}
