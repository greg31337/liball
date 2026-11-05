namespace svg;

/// <summary>Configures structural element resolution and bounded text extraction.</summary>
public class SvgStructuralResolveOptions
{
    /// <summary>The element catalog used to classify descendant render modes.</summary>
    public SvgElementCatalog ElementCatalog;
    /// <summary>The typed attribute accessor used for structural values and defaults.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The optional computed style for the element being resolved.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The tree and descriptive-text limits.</summary>
    public SvgTreeLimits TreeLimits;
    /// <summary>Whether a reference has instantiated the element.</summary>
    public bool Referenced;
    /// <summary>Whether the containing rendered subtree remains active.</summary>
    public bool AncestorRendered;
    /// <summary>The maximum relation-token count on one link.</summary>
    public int MaxRelationTokens;
    /// <summary>The maximum characters in one relation, language, or media-type token.</summary>
    public int MaxTokenCharacters;
    /// <summary>The maximum parameter count in a link media type.</summary>
    public int MaxMediaTypeParameters;

    /// <summary>Creates secure structural-resolution defaults.</summary>
    public SvgStructuralResolveOptions()
    {
        ElementCatalog = new SvgCatalogFactory().CreateElementCatalog();
        Attributes = new SvgTypedAttributeAccessor();
        ComputedStyle = null;
        TreeLimits = new SvgTreeLimits();
        Referenced = false;
        AncestorRendered = true;
        MaxRelationTokens = 64;
        MaxTokenCharacters = 4096;
        MaxMediaTypeParameters = 64;
    }
}

/// <summary>Contains resolved runtime semantics for one structural SVG element.</summary>
public class SvgStructuralResult
{
    /// <summary>The source structural element.</summary>
    public SvgNode? Node;
    /// <summary>The element version used during resolution.</summary>
    public long NodeVersion;
    /// <summary>The supported structural role.</summary>
    public SvgStructuralElementKind Kind;
    /// <summary>The catalog render mode for the element.</summary>
    public SvgElementRenderMode RenderMode;
    /// <summary>Whether the element was resolved in a referencing context.</summary>
    public bool Referenced;
    /// <summary>Whether the element participates in the active rendered subtree.</summary>
    public bool Rendered;
    /// <summary>Whether eligible children must be visited for rendering.</summary>
    public bool RendersChildren;
    /// <summary>Whether this is a non-rendered definition container.</summary>
    public bool DefinitionContainer;
    /// <summary>Whether this element can render only through a reference.</summary>
    public bool ReferenceOnly;
    /// <summary>Whether the element supplies descriptive rather than rendered content.</summary>
    public bool Descriptive;
    /// <summary>All direct element children in source order.</summary>
    public System.Collections.Generic.List<SvgNode> ElementChildren;
    /// <summary>Direct children eligible for ordinary rendered traversal.</summary>
    public System.Collections.Generic.List<SvgNode> RenderingChildren;
    /// <summary>Direct title children in source order.</summary>
    public System.Collections.Generic.List<SvgNode> TitleElements;
    /// <summary>Direct description children in source order.</summary>
    public System.Collections.Generic.List<SvgNode> DescriptionElements;
    /// <summary>Direct metadata children in source order.</summary>
    public System.Collections.Generic.List<SvgNode> MetadataElements;
    /// <summary>The bounded descendant text for a descriptive element.</summary>
    public string TextContent;
    /// <summary>The bounded text from the first direct title child, or an empty string.</summary>
    public string TitleText;
    /// <summary>The bounded text from the first direct description child, or an empty string.</summary>
    public string DescriptionText;
    /// <summary>The authored transform attribute, or <c>null</c>.</summary>
    public SvgTransformList? Transform;
    /// <summary>The computed CSS transform, or <c>null</c>.</summary>
    public SvgCssTransformList? ComputedTransform;
    /// <summary>The symbol or view view box, or <c>null</c>.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The symbol or view aspect-ratio rule, or <c>null</c> for other roles.</summary>
    public SvgAspectRatio? AspectRatio;
    /// <summary>The symbol reference-point horizontal length, or <c>null</c> for other roles.</summary>
    public SvgLength? RefX;
    /// <summary>The symbol reference-point vertical length, or <c>null</c> for other roles.</summary>
    public SvgLength? RefY;
    /// <summary>The unnamespaced link IRI, or <c>null</c> when absent or invalid.</summary>
    public SvgIriReference? Href;
    /// <summary>The legacy XLink IRI, or <c>null</c> when absent or invalid.</summary>
    public SvgIriReference? XLinkHref;
    /// <summary>The XML-normalized unnamespaced link text, or an empty string.</summary>
    public string HrefText;
    /// <summary>The XML-normalized legacy XLink text, or an empty string.</summary>
    public string XLinkHrefText;
    /// <summary>The effective link target browsing-context name.</summary>
    public string Target;
    /// <summary>Whether the link has a download attribute, including an empty one.</summary>
    public bool HasDownload;
    /// <summary>The link download filename suggestion, or an empty string.</summary>
    public string Download;
    /// <summary>The normalized link-language tag, or an empty string.</summary>
    public string HrefLanguage;
    /// <summary>The parsed link media type, or <c>null</c>.</summary>
    public SvgMediaType? MediaType;
    /// <summary>The ordered lowercase link relation tokens.</summary>
    public System.Collections.Generic.List<SvgTextToken> Relations;
    /// <summary>Located diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether structural resolution completed without errors.</summary>
    public bool Success;

    internal SvgStructuralResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        Kind = SvgStructuralElementKind.None;
        RenderMode = SvgElementRenderMode.Never;
        Referenced = false;
        Rendered = false;
        RendersChildren = false;
        DefinitionContainer = false;
        ReferenceOnly = false;
        Descriptive = false;
        ElementChildren = new System.Collections.Generic.List<SvgNode>();
        RenderingChildren = new System.Collections.Generic.List<SvgNode>();
        TitleElements = new System.Collections.Generic.List<SvgNode>();
        DescriptionElements = new System.Collections.Generic.List<SvgNode>();
        MetadataElements = new System.Collections.Generic.List<SvgNode>();
        TextContent = "";
        TitleText = "";
        DescriptionText = "";
        Transform = null;
        ComputedTransform = null;
        ViewBox = null;
        AspectRatio = null;
        RefX = null;
        RefY = null;
        Href = null;
        XLinkHref = null;
        HrefText = "";
        XLinkHrefText = "";
        Target = "";
        HasDownload = false;
        Download = "";
        HrefLanguage = "";
        MediaType = null;
        Relations = new System.Collections.Generic.List<SvgTextToken>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }
}
