namespace svg;

/// <summary>Configures one root or nested SVG viewport resolution.</summary>
public class SvgViewportResolveOptions
{
    /// <summary>The typed attribute accessor used for SVG attribute values and defaults.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The optional computed style for the SVG element.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The optional resolved containing viewport for a nested SVG element.</summary>
    public SvgViewportResult? ParentViewport;
    /// <summary>The containing viewport and font context used when no parent result is supplied.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The fallback width for a root viewport without a host-provided size.</summary>
    public double DefaultRootWidth;
    /// <summary>The fallback height for a root viewport without a host-provided size.</summary>
    public double DefaultRootHeight;
    /// <summary>The optional fragment identifier, with or without a leading number sign.</summary>
    public string FragmentIdentifier;

    /// <summary>Creates deterministic standalone viewport defaults.</summary>
    public SvgViewportResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ComputedStyle = null;
        ParentViewport = null;
        LengthContext = new SvgLengthContext();
        DefaultRootWidth = 300;
        DefaultRootHeight = 150;
        FragmentIdentifier = "";
    }
}

/// <summary>Contains one established SVG viewport and its child user-space mapping.</summary>
public class SvgViewportResult
{
    /// <summary>The SVG element that establishes the viewport.</summary>
    public SvgNode? Node;
    /// <summary>The element version used during resolution.</summary>
    public long NodeVersion;
    /// <summary>Whether this is the document's root SVG viewport.</summary>
    public bool IsRoot;
    /// <summary>The horizontal viewport origin in containing user units.</summary>
    public double X;
    /// <summary>The vertical viewport origin in containing user units.</summary>
    public double Y;
    /// <summary>The viewport width in containing user units.</summary>
    public double Width;
    /// <summary>The viewport height in containing user units.</summary>
    public double Height;
    /// <summary>The physical viewport bounds in containing user units.</summary>
    public SvgRect? Bounds;
    /// <summary>The effective view box, or <c>null</c> when none applies.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The effective preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>The transform from child user coordinates to containing user coordinates.</summary>
    public SvgMatrix Transform;
    /// <summary>The physical viewport expressed as axis-aligned child user-space bounds.</summary>
    public SvgRect? UserBounds;
    /// <summary>The viewport and inherited font inputs used to evaluate child percentages and lengths.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>Whether overflow establishes a viewport clip.</summary>
    public bool ClipsOverflow;
    /// <summary>The clip rectangle in containing user units, or <c>null</c> when overflow is visible.</summary>
    public SvgRect? ClipRect;
    /// <summary>Whether nonzero viewport and view-box dimensions permit descendant rendering.</summary>
    public bool Renderable;
    /// <summary>The supplied fragment identifier.</summary>
    public string FragmentIdentifier;
    /// <summary>The viewport interpretation of the fragment identifier.</summary>
    public SvgViewportFragmentKind FragmentKind;
    /// <summary>The element selected by a bare fragment identifier, or <c>null</c>.</summary>
    public SvgNode? FragmentTarget;
    /// <summary>The <c>view</c> element supplying overrides, or <c>null</c>.</summary>
    public SvgNode? ViewSpecificationNode;
    /// <summary>The optional transform supplied by an <c>svgView(...)</c> fragment.</summary>
    public SvgTransformList? FragmentTransform;
    /// <summary>Resolution diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether viewport establishment completed without error diagnostics.</summary>
    public bool Success;

    internal SvgViewportResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        IsRoot = false;
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        Bounds = null;
        ViewBox = null;
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        UserBounds = null;
        LengthContext = new SvgLengthContext();
        ClipsOverflow = false;
        ClipRect = null;
        Renderable = false;
        FragmentIdentifier = "";
        FragmentKind = SvgViewportFragmentKind.None;
        FragmentTarget = null;
        ViewSpecificationNode = null;
        FragmentTransform = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }
}
