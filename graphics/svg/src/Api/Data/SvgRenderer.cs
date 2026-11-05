namespace svg;

/// <summary>Configures a bounded whole-document or extracted-target raster render.</summary>
public class SvgRenderOptions
{
    /// <summary>The world-space horizontal origin mapped to the output's left edge.</summary>
    public double ViewportX;
    /// <summary>The world-space vertical origin mapped to the output's top edge.</summary>
    public double ViewportY;
    /// <summary>The positive requested viewport width in CSS pixels.</summary>
    public double ViewportWidth;
    /// <summary>The positive requested viewport height in CSS pixels.</summary>
    public double ViewportHeight;
    /// <summary>The positive output pixels per CSS pixel.</summary>
    public double Scale;
    /// <summary>The optional premultiplied output background, or <c>null</c> for transparency.</summary>
    public SvgPremultipliedRgba8? Background;
    /// <summary>The caller-controlled animation sample time in seconds.</summary>
    public double AnimationTime;
    /// <summary>Whether successful pixels are also encoded as PNG bytes.</summary>
    public bool EncodePng;
    /// <summary>The deterministic PNG encoding options.</summary>
    public SvgPngEncodeOptions PngOptions;
    /// <summary>The base document-resolution environment used by document entry points.</summary>
    public SvgResolveOptions ResolveOptions;
    /// <summary>The complete processing and raster security limits.</summary>
    public SvgSecurityLimits Limits;
    /// <summary>The path coverage samples along each output-pixel axis.</summary>
    public int SamplesPerAxis;
    /// <summary>The positive maximum scene nodes visited.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum cumulative raster and pixel work.</summary>
    public long MaximumPixels;
    /// <summary>An optional callback returning local-user-space text outline geometry.</summary>
    public System.Func<SvgSceneNode, SvgPathData>? TextPathCallback;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates transparent 300 by 150 CSS-pixel rendering defaults.</summary>
    public SvgRenderOptions()
    {
        ViewportX = 0;
        ViewportY = 0;
        ViewportWidth = 300;
        ViewportHeight = 150;
        Scale = 1;
        Background = null;
        AnimationTime = 0;
        EncodePng = false;
        PngOptions = new SvgPngEncodeOptions();
        ResolveOptions = new SvgResolveOptions();
        Limits = new SvgSecurityLimits();
        SamplesPerAxis = 4;
        MaximumNodes = 100000;
        MaximumPixels = 1000000000;
        TextPathCallback = null;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains complete pixels, optional PNG bytes, provenance, commands, and diagnostics.</summary>
public class SvgRenderResult
{
    /// <summary>Whether the requested target rendered completely.</summary>
    public bool Success;
    /// <summary>The stable rendering outcome.</summary>
    public SvgRenderError Error;
    /// <summary>The complete premultiplied sRGB output, or <c>null</c> after failure.</summary>
    public SvgPixelBuffer? Pixels;
    /// <summary>The deterministic PNG output, or an empty array when it was not requested.</summary>
    public byte[] PngBytes;
    /// <summary>The selected scene's world-space drawing bounds, or <c>null</c>.</summary>
    public SvgRect? DrawingBounds;
    /// <summary>The normalized scene selected for rendering, or <c>null</c>.</summary>
    public SvgScene? Scene;
    /// <summary>The selected scene's deterministic command list, or <c>null</c>.</summary>
    public SvgRenderCommandList? Commands;
    /// <summary>The resolved document used by document and target entry points, or <c>null</c>.</summary>
    public SvgResolvedDocument? ResolvedDocument;
    /// <summary>The ordered resolution, scene, and rendering diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The resource activity associated with the resolved document.</summary>
    public SvgResourceSummary Resources;
    /// <summary>The feature report associated with the resolved document or library.</summary>
    public SvgFeatureReport FeatureReport;
    /// <summary>The requested animation sample time in seconds.</summary>
    public double AnimationTime;
    /// <summary>The number of visited scene nodes.</summary>
    public int NodesVisited;
    /// <summary>The cumulative bounded raster and pixel work.</summary>
    public long PixelsProcessed;

    internal SvgRenderResult()
    {
        Success = false;
        Error = SvgRenderError.InvalidArgument;
        Pixels = null;
        PngBytes = System.Array.Empty<byte>();
        DrawingBounds = null;
        Scene = null;
        Commands = null;
        ResolvedDocument = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Resources = new SvgResourceSummary();
        FeatureReport = new SvgCapabilities().GetLibraryReport();
        AnimationTime = 0;
        NodesVisited = 0;
        PixelsProcessed = 0;
    }
}
