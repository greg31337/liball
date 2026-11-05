namespace svg;

/// <summary>Configures clipped bounded Porter-Duff compositing.</summary>
public class SvgCompositeOptions
{
    /// <summary>The Porter-Duff operation.</summary>
    public SvgCompositeOperator Operator;
    /// <summary>The source opacity from zero through one, applied before compositing.</summary>
    public double Opacity;
    /// <summary>The source-over blend mode.</summary>
    public SvgBlendMode BlendMode;
    /// <summary>The color space used for blend and compositing arithmetic.</summary>
    public SvgBlendColorSpace BlendColorSpace;
    /// <summary>Whether the source is isolated from backdrop color blending.</summary>
    public bool Isolated;
    /// <summary>The optional source-local rectangle; <c>null</c> selects the complete source.</summary>
    public SvgPixelRect? SourceRectangle;
    /// <summary>The destination x coordinate receiving the source rectangle origin.</summary>
    public int DestinationX;
    /// <summary>The destination y coordinate receiving the source rectangle origin.</summary>
    public int DestinationY;
    /// <summary>The optional destination-local clip.</summary>
    public SvgPixelRect? Clip;
    /// <summary>The positive maximum clipped pixels processed.</summary>
    public long MaximumPixels;
    /// <summary>The allocation limits used only for overlap snapshots.</summary>
    public SvgRasterLimits RasterLimits;

    /// <summary>Creates opaque source-over compositing at the destination origin.</summary>
    public SvgCompositeOptions()
    {
        Operator = SvgCompositeOperator.SourceOver;
        Opacity = 1;
        BlendMode = SvgBlendMode.Normal;
        BlendColorSpace = SvgBlendColorSpace.Auto;
        Isolated = false;
        SourceRectangle = null;
        DestinationX = 0;
        DestinationY = 0;
        Clip = null;
        MaximumPixels = 100000000;
        RasterLimits = new SvgRasterLimits();
    }
}

/// <summary>Contains one completed compositing operation or stable failure.</summary>
public class SvgCompositeResult
{
    /// <summary>Whether compositing completed.</summary>
    public bool Success;
    /// <summary>The stable compositing outcome.</summary>
    public SvgCompositeError Error;
    /// <summary>The clipped destination rectangle.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The number of pixels composited.</summary>
    public long PixelsProcessed;
    /// <summary>Whether an overlap-safe source snapshot was allocated.</summary>
    public bool UsedSnapshot;

    internal SvgCompositeResult()
    {
        Success = false;
        Error = SvgCompositeError.None;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        PixelsProcessed = 0;
        UsedSnapshot = false;
    }
}

/// <summary>Contains a bounded offscreen group surface and its destination placement.</summary>
public class SvgOpacityGroup
{
    /// <summary>The transparent offscreen surface painted at full child opacity.</summary>
    public SvgPixelBuffer? Surface;
    /// <summary>The destination-local bounds represented by the surface.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The group opacity applied once when the surface is composited.</summary>
    public double Opacity;
    /// <summary>The blend mode applied when the group is composited.</summary>
    public SvgBlendMode BlendMode;
    /// <summary>The color space used when the group is composited.</summary>
    public SvgBlendColorSpace BlendColorSpace;
    /// <summary>Whether the group is isolated from backdrop color blending.</summary>
    public bool Isolated;
    internal SvgRasterLimits RasterLimits;

    internal SvgOpacityGroup(SvgPixelBuffer? surface, SvgPixelRect bounds, double opacity, SvgRasterLimits limits)
    {
        Surface = surface;
        Bounds = bounds;
        Opacity = opacity;
        BlendMode = SvgBlendMode.Normal;
        BlendColorSpace = SvgBlendColorSpace.Auto;
        Isolated = false;
        RasterLimits = limits;
    }

    /// <summary>Composites the complete group over its destination bounds exactly once.</summary>
    /// <param name="destination">The premultiplied destination surface.</param>
    /// <returns>The source-over group compositing result.</returns>
    internal SvgCompositeResult CompositeTo(SvgPixelBuffer destination)
    {
        SvgCompositeOptions options = new SvgCompositeOptions();
        options.Opacity = Opacity;
        options.BlendMode = BlendMode;
        options.BlendColorSpace = BlendColorSpace;
        options.Isolated = Isolated;
        options.DestinationX = Bounds.X;
        options.DestinationY = Bounds.Y;
        options.MaximumPixels = RasterLimits.MaxSurfacePixels;
        options.RasterLimits = RasterLimits;
        return new SvgCompositor().CompositeWithSourceAndDestinationAndOptions((Surface ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), destination, options);
    }
}

/// <summary>Contains a created bounded opacity group or a stable failure.</summary>
public class SvgOpacityGroupResult
{
    /// <summary>Whether the group surface was created.</summary>
    public bool Success;
    /// <summary>The stable creation outcome.</summary>
    public SvgCompositeError Error;
    /// <summary>The created transparent group, or <c>null</c>.</summary>
    public SvgOpacityGroup? Group;

    internal SvgOpacityGroupResult(bool success, SvgCompositeError error, SvgOpacityGroup? group)
    {
        Success = success;
        Error = error;
        Group = group;
    }
}
