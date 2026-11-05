namespace svg;

/// <summary>Contains one rectangle or resource entry in an intersecting clip stack.</summary>
public class SvgClipStackEntry
{
    /// <summary>The tagged entry kind.</summary>
    public SvgClipStackEntryKind Kind;
    /// <summary>The surface-local rectangle for a rectangle entry.</summary>
    public SvgPixelRect? Rectangle;
    /// <summary>The normalized clip resource for a resource entry.</summary>
    public SvgSceneResource? Resource;
    /// <summary>The target-local to surface transform for a resource entry.</summary>
    public SvgMatrix Transform;

    /// <summary>Creates an axis-aligned rectangle clip entry.</summary>
    /// <param name="rectangle">The surface-local clip rectangle.</param>
    public SvgClipStackEntry(SvgPixelRect? rectangle)
    {
        Kind = SvgClipStackEntryKind.Rectangle;
        Rectangle = rectangle;
        Resource = null;
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
    }

    /// <summary>Creates a transformed normalized-resource clip entry.</summary>
    /// <param name="resource">The normalized clip resource.</param>
    /// <param name="transform">The target-local to surface transform.</param>
    /// <returns>A new resource clip entry.</returns>
    internal static SvgClipStackEntry CreateResource(SvgSceneResource? resource, SvgMatrix transform)
    {
        SvgClipStackEntry result = new SvgClipStackEntry(null);
        result.Kind = SvgClipStackEntryKind.Resource;
        result.Resource = resource;
        result.Transform = transform;
        return result;
    }
}

/// <summary>Configures nested clipping and optional alpha or luminance masking.</summary>
public class SvgClipMaskOptions
{
    /// <summary>The ordered clip entries intersected before masking.</summary>
    public System.Collections.Generic.List<SvgClipStackEntry> Clips;
    /// <summary>The optional normalized mask resource.</summary>
    public SvgSceneResource? Mask;
    /// <summary>The mask target-local to surface transform.</summary>
    public SvgMatrix MaskTransform;
    /// <summary>The optional surface-local subregion affected by clipping and masking.</summary>
    public SvgPixelRect? Subregion;
    /// <summary>The limits applied to every offscreen surface.</summary>
    public SvgRasterLimits RasterLimits;
    /// <summary>The scan-conversion precision and geometry-work limits.</summary>
    public SvgRasterOptions RasterOptions;
    /// <summary>The paint-sampling limits used for mask content.</summary>
    public SvgPaintSampleOptions PaintOptions;
    /// <summary>The positive maximum active nested resource depth.</summary>
    public int MaximumDepth;
    /// <summary>The positive maximum effect-content nodes visited.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum cumulative pixel-loop work.</summary>
    public long MaximumPixels;

    /// <summary>Creates an empty clip stack with no mask and bounded defaults.</summary>
    public SvgClipMaskOptions()
    {
        Clips = new System.Collections.Generic.List<SvgClipStackEntry>();
        Mask = null;
        MaskTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Subregion = null;
        RasterLimits = new SvgRasterLimits();
        RasterOptions = new SvgRasterOptions();
        PaintOptions = new SvgPaintSampleOptions();
        MaximumDepth = 16;
        MaximumNodes = 100000;
        MaximumPixels = 1000000000;
    }
}

/// <summary>Contains one completed clip-stack and mask application.</summary>
public class SvgClipMaskResult
{
    /// <summary>Whether all requested effects were applied atomically.</summary>
    public bool Success;
    /// <summary>The stable clipping or masking outcome.</summary>
    public SvgClipMaskError Error;
    /// <summary>The surface-local subregion affected by the operation.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The number of intersected clip entries.</summary>
    public int ClipsApplied;
    /// <summary>Whether a mask resource was applied.</summary>
    public bool MaskApplied;
    /// <summary>The cumulative bounded pixel-loop work.</summary>
    public long PixelsProcessed;
    /// <summary>The number of bounded offscreen surfaces allocated.</summary>
    public int OffscreenSurfaces;

    internal SvgClipMaskResult()
    {
        Success = false;
        Error = SvgClipMaskError.None;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        ClipsApplied = 0;
        MaskApplied = false;
        PixelsProcessed = 0;
        OffscreenSurfaces = 0;
    }
}
