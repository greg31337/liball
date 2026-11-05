namespace svg;

/// <summary>Configures bounded SVG href and fragment resolution.</summary>
public class SvgReferenceResolveOptions
{
    /// <summary>Whether absence of both reference attributes is an error.</summary>
    public bool RequireReference;
    /// <summary>An optional caller-loaded document for an external resource reference.</summary>
    public SvgDocument? ExternalDocument;
    /// <summary>The maximum authored, base, and resolved URI character count.</summary>
    public int MaxUriCharacters;
    /// <summary>The maximum element ancestry depth inspected for xml:base values.</summary>
    public int MaxBaseDepth;
    /// <summary>Optional inherited callback loading used when an external document was not pre-supplied.</summary>
    public SvgNestedDocumentOptions? NestedDocumentOptions;

    /// <summary>Creates secure reference-resolution defaults.</summary>
    public SvgReferenceResolveOptions()
    {
        RequireReference = false;
        ExternalDocument = null;
        MaxUriCharacters = 8192;
        MaxBaseDepth = 128;
        NestedDocumentOptions = null;
    }
}

/// <summary>Contains a selected SVG reference, resolved URI, target, diagnostics, and source provenance.</summary>
public class SvgReferenceResult
{
    /// <summary>The element containing the reference attributes.</summary>
    public SvgNode? Node;
    /// <summary>The source element version used during resolution.</summary>
    public long NodeVersion;
    /// <summary>The document containing the reference element, or <c>null</c>.</summary>
    public SvgDocument? SourceDocument;
    /// <summary>The authored unnamespaced href attribute, or <c>null</c>.</summary>
    public SvgAttribute? HrefAttribute;
    /// <summary>The authored legacy XLink href attribute, or <c>null</c>.</summary>
    public SvgAttribute? XLinkHrefAttribute;
    /// <summary>The selected attribute, or <c>null</c>.</summary>
    public SvgAttribute? SelectedAttribute;
    /// <summary>The selected attribute kind.</summary>
    public SvgReferenceAttributeKind AttributeKind;
    /// <summary>Whether an authored XLink href was ignored because an unnamespaced href was present.</summary>
    public bool XLinkHrefIgnored;
    /// <summary>The parsed selected IRI, or <c>null</c>.</summary>
    public SvgIriReference? Reference;
    /// <summary>The trimmed selected URI text, or an empty string.</summary>
    public string AuthoredUri;
    /// <summary>The effective base URI after applying the complete xml:base chain.</summary>
    public string BaseUri;
    /// <summary>The resolved URI including any query and fragment.</summary>
    public string ResolvedUri;
    /// <summary>The resolved resource URI without a fragment.</summary>
    public string ResourceUri;
    /// <summary>Whether the selected URI contains a fragment delimiter.</summary>
    public bool HasFragment;
    /// <summary>The preserved fragment text without the number sign.</summary>
    public string Fragment;
    /// <summary>The percent-decoded fragment identifier used for ID lookup.</summary>
    public string FragmentIdentifier;
    /// <summary>Whether the resolved resource URI identifies the source document.</summary>
    public bool IsSameDocument;
    /// <summary>The available target document, or <c>null</c> for an unloaded external resource.</summary>
    public SvgDocument? TargetDocument;
    /// <summary>The callback-loaded nested document result, or <c>null</c>.</summary>
    public SvgNestedDocumentResult? NestedDocument;
    /// <summary>The resolved fragment element or referenced document root, or <c>null</c>.</summary>
    public SvgNode? Target;
    /// <summary>The exact selected attribute source span, or <c>null</c>.</summary>
    public SvgSourceSpan? ReferenceSource;
    /// <summary>The nearest xml:base source or document source span, or <c>null</c>.</summary>
    public SvgSourceSpan? BaseSource;
    /// <summary>The resolved target source span, or <c>null</c>.</summary>
    public SvgSourceSpan? TargetSource;
    /// <summary>The selected attribute's nested-resource provenance from inner to outer source.</summary>
    public System.Collections.Generic.List<SvgSourceSpan> ReferenceProvenance;
    /// <summary>The authored xml:base spans in outer-to-inner application order.</summary>
    public System.Collections.Generic.List<SvgSourceSpan> BaseProvenance;
    /// <summary>The resolved target's nested-resource provenance from inner to outer source.</summary>
    public System.Collections.Generic.List<SvgSourceSpan> TargetProvenance;
    /// <summary>The stable reference outcome.</summary>
    public SvgReferenceStatus Status;
    /// <summary>Located diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether processing completed without an error diagnostic.</summary>
    public bool Success;

    internal SvgReferenceResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        SourceDocument = node == null ? null : node.OwnerDocument;
        HrefAttribute = null;
        XLinkHrefAttribute = null;
        SelectedAttribute = null;
        AttributeKind = SvgReferenceAttributeKind.None;
        XLinkHrefIgnored = false;
        Reference = null;
        AuthoredUri = "";
        BaseUri = "";
        ResolvedUri = "";
        ResourceUri = "";
        HasFragment = false;
        Fragment = "";
        FragmentIdentifier = "";
        IsSameDocument = false;
        TargetDocument = null;
        NestedDocument = null;
        Target = null;
        ReferenceSource = null;
        BaseSource = null;
        TargetSource = null;
        ReferenceProvenance = new System.Collections.Generic.List<SvgSourceSpan>();
        BaseProvenance = new System.Collections.Generic.List<SvgSourceSpan>();
        TargetProvenance = new System.Collections.Generic.List<SvgSourceSpan>();
        Status = SvgReferenceStatus.Missing;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }
}
