namespace svg;

/// <summary>Describes intrinsic dimensions supplied by an image decoder or host layout callback.</summary>
public class SvgIntrinsicSize
{
    /// <summary>Whether an intrinsic width is available.</summary>
    public bool HasWidth;
    /// <summary>The positive intrinsic width when available.</summary>
    public double Width;
    /// <summary>Whether an intrinsic height is available.</summary>
    public bool HasHeight;
    /// <summary>The positive intrinsic height when available.</summary>
    public double Height;
    /// <summary>Whether an intrinsic width-to-height ratio is available.</summary>
    public bool HasAspectRatio;
    /// <summary>The positive intrinsic width-to-height ratio when available.</summary>
    public double AspectRatio;

    /// <summary>Creates a placeholder with no known intrinsic dimensions.</summary>
    public SvgIntrinsicSize()
    {
        HasWidth = false;
        Width = 0;
        HasHeight = false;
        Height = 0;
        HasAspectRatio = false;
        AspectRatio = 0;
    }

    /// <summary>Creates a complete positive intrinsic size.</summary>
    /// <param name="width">The positive intrinsic width.</param>
    /// <param name="height">The positive intrinsic height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a dimension is not finite and positive.</exception>
    /// <returns>A new complete intrinsic size.</returns>
    internal static SvgIntrinsicSize Create(double width, double height)
    {
        if (!SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height) || width <= 0 || height <= 0) throw new System.ArgumentOutOfRangeException("width");
        SvgIntrinsicSize result = new SvgIntrinsicSize();
        result.HasWidth = true;
        result.Width = width;
        result.HasHeight = true;
        result.Height = height;
        result.HasAspectRatio = true;
        result.AspectRatio = width / height;
        return result;
    }
}

/// <summary>Contains a bounded synchronous request for host-owned embedded content.</summary>
public class SvgHostContentCallbackRequest
{
    /// <summary>The source embedded-content element.</summary>
    public SvgNode Node;
    /// <summary>The source element version captured for this request.</summary>
    public long NodeVersion;
    /// <summary>The stable use-instance path, or an empty string for authored content.</summary>
    public string InstancePath;
    /// <summary>The embedded-content kind.</summary>
    public SvgEmbeddedContentKind Kind;
    /// <summary>The provisional positioning rectangle before callback intrinsic sizing.</summary>
    public SvgRect Bounds;
    /// <summary>The selected resolved image reference, or <c>null</c>.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The acquired encoded image and metadata, or <c>null</c>.</summary>
    public SvgImageResolveResult? ImageResource;
    /// <summary>The bounded direct child-node snapshot.</summary>
    public System.Collections.Generic.List<SvgNode> Children;
    /// <summary>The maximum accepted raster width.</summary>
    public int MaxRasterWidth;
    /// <summary>The maximum accepted raster height.</summary>
    public int MaxRasterHeight;
    /// <summary>The maximum accepted raster pixel count.</summary>
    public long MaxRasterPixels;
    /// <summary>The maximum accepted raster byte footprint.</summary>
    public long MaxRasterBytes;
    /// <summary>The maximum accepted callback diagnostic count.</summary>
    public int MaxDiagnostics;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    internal SvgHostContentCallbackRequest(SvgNode node, SvgEmbeddedContentKind kind, SvgRect bounds)
    {
        Node = node;
        NodeVersion = node.Version;
        InstancePath = "";
        Kind = kind;
        Bounds = bounds;
        Reference = null;
        ImageResource = null;
        Children = new System.Collections.Generic.List<SvgNode>();
        MaxRasterWidth = 32768;
        MaxRasterHeight = 32768;
        MaxRasterPixels = 268435456;
        MaxRasterBytes = 400000000;
        MaxDiagnostics = 10000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains intrinsic, layout, raster, and diagnostic output from a host-content callback.</summary>
public class SvgHostContentCallbackResult
{
    /// <summary>Whether host processing completed successfully.</summary>
    public bool Success;
    /// <summary>Optional intrinsic dimensions used to resolve automatic sizing.</summary>
    public SvgIntrinsicSize? IntrinsicSize;
    /// <summary>Optional content bounds in current SVG user space.</summary>
    public SvgRect? ContentBounds;
    /// <summary>Optional mapping from raster or host-local coordinates into current SVG user space.</summary>
    public SvgMatrix? ContentTransform;
    /// <summary>Optional decoded premultiplied raster output.</summary>
    public SvgPixelBuffer? RasterPixels;
    /// <summary>Deterministic caller diagnostics appended in list order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates a declined result with no layout or raster output.</summary>
    public SvgHostContentCallbackResult()
    {
        Success = false;
        IntrinsicSize = null;
        ContentBounds = null;
        ContentTransform = null;
        RasterPixels = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}

/// <summary>Configures embedded-content geometry and host availability resolution.</summary>
public class SvgEmbeddedContentResolveOptions
{
    /// <summary>The typed accessor used for geometry, references, and aspect ratio.</summary>
    public SvgTypedAttributeAccessor Attributes;
    /// <summary>The optional computed style whose geometry properties take precedence.</summary>
    public SvgComputedStyle? ComputedStyle;
    /// <summary>The stable use-instance path supplied by resolved-document traversal.</summary>
    public string InstancePath;
    /// <summary>The viewport and font context used to resolve lengths and percentages.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>Optional intrinsic dimensions supplied by an image decoder or host.</summary>
    public SvgIntrinsicSize? IntrinsicSize;
    /// <summary>The href, base URI, external document, and URI-bound options used for images.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>Optional caller-mediated encoded-image acquisition and metadata options.</summary>
    public SvgImageResolveOptions? ImageOptions;
    /// <summary>Whether decoded image content is currently available.</summary>
    public bool ImageContentAvailable;
    /// <summary>Whether a host callback is available to lay out foreign content.</summary>
    public bool ForeignContentCallbackAvailable;
    /// <summary>The optional synchronous host-content callback. It is disabled by default.</summary>
    public System.Func<SvgHostContentCallbackRequest, SvgHostContentCallbackResult>? HostContentCallback;
    /// <summary>Whether conditional-processing attributes permit this element.</summary>
    public bool ConditionsSatisfied;
    /// <summary>Whether the containing rendered subtree remains active.</summary>
    public bool AncestorRendered;
    /// <summary>The maximum direct child nodes copied into a host-content record.</summary>
    public int MaxHostContentNodes;
    /// <summary>The maximum accepted callback raster width.</summary>
    public int MaxHostRasterWidth;
    /// <summary>The maximum accepted callback raster height.</summary>
    public int MaxHostRasterHeight;
    /// <summary>The maximum accepted callback raster pixel count.</summary>
    public long MaxHostRasterPixels;
    /// <summary>The maximum accepted callback raster byte footprint.</summary>
    public long MaxHostRasterBytes;
    /// <summary>The maximum retained callback diagnostic count.</summary>
    public int MaxHostDiagnostics;
    /// <summary>The maximum elapsed synchronous callback time in milliseconds.</summary>
    public int MaxHostCallbackMilliseconds;
    /// <summary>The synchronous cancellation token passed to host processing.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure deterministic embedded-content defaults.</summary>
    public SvgEmbeddedContentResolveOptions()
    {
        Attributes = new SvgTypedAttributeAccessor();
        ComputedStyle = null;
        InstancePath = "";
        LengthContext = new SvgLengthContext();
        IntrinsicSize = null;
        ReferenceOptions = new SvgReferenceResolveOptions();
        ReferenceOptions.RequireReference = true;
        ImageOptions = null;
        ImageContentAvailable = false;
        ForeignContentCallbackAvailable = false;
        HostContentCallback = null;
        ConditionsSatisfied = true;
        AncestorRendered = true;
        MaxHostContentNodes = 250000;
        MaxHostRasterWidth = 32768;
        MaxHostRasterHeight = 32768;
        MaxHostRasterPixels = 268435456;
        MaxHostRasterBytes = 400000000;
        MaxHostDiagnostics = 10000;
        MaxHostCallbackMilliseconds = 30000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains a bounded handoff from SVG semantics to image or foreign-content host code.</summary>
public class SvgHostContentRecord
{
    /// <summary>The source embedded-content element.</summary>
    public SvgNode? Node;
    /// <summary>The source element version used to create this record.</summary>
    public long NodeVersion;
    /// <summary>The stable use-instance path, or an empty string for authored content.</summary>
    public string InstancePath;
    /// <summary>The embedded-content kind.</summary>
    public SvgEmbeddedContentKind Kind;
    /// <summary>The positioning rectangle in current SVG user space.</summary>
    public SvgRect? Bounds;
    /// <summary>The fitted image rectangle or foreign-content containing block in SVG user space.</summary>
    public SvgRect? ContentBounds;
    /// <summary>The mapping from intrinsic or host-local coordinates into current SVG user space.</summary>
    public SvgMatrix? ContentTransform;
    /// <summary>The overflow clip rectangle, or <c>null</c> when content is not clipped.</summary>
    public SvgRect? ClipBounds;
    /// <summary>Whether content is clipped to the positioning rectangle.</summary>
    public bool ClipsOverflow;
    /// <summary>Whether rendering this record requires host-provided content processing.</summary>
    public bool RequiresHostProcessing;
    /// <summary>Whether the required decoded content or callback is available.</summary>
    public bool ContentAvailable;
    /// <summary>Whether conditional processing permits the host content.</summary>
    public bool ConditionsSatisfied;
    /// <summary>The independently retained unqualified image reference, or <c>null</c>.</summary>
    public SvgIriReference? Href;
    /// <summary>The independently retained legacy XLink image reference, or <c>null</c>.</summary>
    public SvgIriReference? XLinkHref;
    /// <summary>The requested image CORS mode.</summary>
    public SvgCrossOriginMode CrossOrigin;
    /// <summary>The selected and resolved image reference, or <c>null</c> for foreign content.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The acquired encoded image and deferred-decode metadata, or <c>null</c>.</summary>
    public SvgImageResolveResult? ImageResource;
    /// <summary>The completed host callback result, or <c>null</c> when callbacks were disabled.</summary>
    public SvgHostContentCallbackResult? CallbackResult;
    /// <summary>The bounded direct child-node snapshot for foreign content.</summary>
    public System.Collections.Generic.List<SvgNode> Children;

    internal SvgHostContentRecord(SvgNode? node, SvgEmbeddedContentKind kind)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        InstancePath = "";
        Kind = kind;
        Bounds = null;
        ContentBounds = null;
        ContentTransform = null;
        ClipBounds = null;
        ClipsOverflow = false;
        RequiresHostProcessing = true;
        ContentAvailable = false;
        ConditionsSatisfied = true;
        Href = null;
        XLinkHref = null;
        CrossOrigin = SvgCrossOriginMode.Unspecified;
        Reference = null;
        ImageResource = null;
        CallbackResult = null;
        Children = new System.Collections.Generic.List<SvgNode>();
    }
}

/// <summary>Contains used geometry, intrinsic sizing, and conditional renderability for embedded content.</summary>
public class SvgEmbeddedContentResult
{
    /// <summary>The source embedded-content element.</summary>
    public SvgNode? Node;
    /// <summary>The source element version used during resolution.</summary>
    public long NodeVersion;
    /// <summary>The stable use-instance path, or an empty string for authored content.</summary>
    public string InstancePath;
    /// <summary>The supported embedded-content kind.</summary>
    public SvgEmbeddedContentKind Kind;
    /// <summary>Whether SVG 2 computed geometry properties supplied applicable values.</summary>
    public bool UsesComputedGeometry;
    /// <summary>Whether the width was resolved from <c>auto</c>.</summary>
    public bool WidthAuto;
    /// <summary>Whether the height was resolved from <c>auto</c>.</summary>
    public bool HeightAuto;
    /// <summary>Whether the CSS default object size contributed a missing dimension.</summary>
    public bool UsedDefaultObjectSize;
    /// <summary>The resolved horizontal positioning coordinate.</summary>
    public double X;
    /// <summary>The resolved vertical positioning coordinate.</summary>
    public double Y;
    /// <summary>The resolved nonnegative positioning width.</summary>
    public double Width;
    /// <summary>The resolved nonnegative positioning height.</summary>
    public double Height;
    /// <summary>The positioning rectangle in current SVG user space.</summary>
    public SvgRect? Bounds;
    /// <summary>The concrete intrinsic coordinate width used for host mapping.</summary>
    public double SourceWidth;
    /// <summary>The concrete intrinsic coordinate height used for host mapping.</summary>
    public double SourceHeight;
    /// <summary>Whether an intrinsic aspect ratio controls image fitting.</summary>
    public bool HasIntrinsicAspectRatio;
    /// <summary>The positive intrinsic width-to-height ratio when available.</summary>
    public double IntrinsicAspectRatio;
    /// <summary>The image preserveAspectRatio value, or <c>null</c> for foreign content.</summary>
    public SvgAspectRatio? AspectRatio;
    /// <summary>Whether preserveAspectRatio was applied to intrinsic image content.</summary>
    public bool AspectRatioApplied;
    /// <summary>The fitted image rectangle or foreign-content containing block.</summary>
    public SvgRect? ContentBounds;
    /// <summary>The intrinsic or host-local coordinate transform.</summary>
    public SvgMatrix? ContentTransform;
    /// <summary>Whether overflow is clipped to the positioning rectangle.</summary>
    public bool ClipsOverflow;
    /// <summary>The clip rectangle, or <c>null</c> when overflow remains visible.</summary>
    public SvgRect? ClipBounds;
    /// <summary>Whether the element and all resolved ancestors have non-none display.</summary>
    public bool Displayed;
    /// <summary>Whether computed visibility permits painting.</summary>
    public bool Visible;
    /// <summary>Whether conditional-processing attributes permit rendering.</summary>
    public bool ConditionsSatisfied;
    /// <summary>Whether the positioning rectangle has positive dimensions.</summary>
    public bool HasGeometry;
    /// <summary>Whether a reference or child host payload is present.</summary>
    public bool HasContent;
    /// <summary>Whether decoded content or the required host callback is available.</summary>
    public bool ContentAvailable;
    /// <summary>Whether all current conditions permit a rendered contribution.</summary>
    public bool Renderable;
    /// <summary>The independently retained unqualified image reference, or <c>null</c>.</summary>
    public SvgIriReference? Href;
    /// <summary>The independently retained legacy XLink image reference, or <c>null</c>.</summary>
    public SvgIriReference? XLinkHref;
    /// <summary>The requested image CORS mode.</summary>
    public SvgCrossOriginMode CrossOrigin;
    /// <summary>The selected and resolved image reference, or <c>null</c> for foreign content.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The acquired encoded image and deferred-decode metadata, or <c>null</c>.</summary>
    public SvgImageResolveResult? ImageResource;
    /// <summary>The completed host callback result, or <c>null</c> when callbacks were disabled.</summary>
    public SvgHostContentCallbackResult? CallbackResult;
    /// <summary>The bounded host handoff record.</summary>
    public SvgHostContentRecord? HostContent;
    /// <summary>Located diagnostics in discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether resolution completed without error diagnostics.</summary>
    public bool Success;

    internal SvgEmbeddedContentResult(SvgNode? node)
    {
        Node = node;
        NodeVersion = node == null ? 0 : node.Version;
        InstancePath = "";
        Kind = SvgEmbeddedContentKind.None;
        UsesComputedGeometry = false;
        WidthAuto = false;
        HeightAuto = false;
        UsedDefaultObjectSize = false;
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        Bounds = null;
        SourceWidth = 0;
        SourceHeight = 0;
        HasIntrinsicAspectRatio = false;
        IntrinsicAspectRatio = 0;
        AspectRatio = null;
        AspectRatioApplied = false;
        ContentBounds = null;
        ContentTransform = null;
        ClipsOverflow = false;
        ClipBounds = null;
        Displayed = false;
        Visible = false;
        ConditionsSatisfied = true;
        HasGeometry = false;
        HasContent = false;
        ContentAvailable = false;
        Renderable = false;
        Href = null;
        XLinkHref = null;
        CrossOrigin = SvgCrossOriginMode.Unspecified;
        Reference = null;
        ImageResource = null;
        CallbackResult = null;
        HostContent = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }
}
