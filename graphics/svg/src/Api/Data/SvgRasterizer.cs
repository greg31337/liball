namespace svg;

/// <summary>Configures bounded deterministic fill scan conversion.</summary>
public class SvgRasterOptions
{
    /// <summary>The positive curve-flattening tolerance in output pixels.</summary>
    public double Tolerance;
    /// <summary>The optional transform from path coordinates to surface coordinates.</summary>
    public SvgMatrix? Transform;
    /// <summary>The optional surface-local rectangular clip.</summary>
    public SvgPixelRect? Clip;
    /// <summary>The positive number of evenly spaced coverage samples along each pixel axis.</summary>
    public int SamplesPerAxis;
    /// <summary>The nonnegative maximum adaptive curve subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum flattened and implicitly closing edge count.</summary>
    public int MaximumEdges;
    /// <summary>The positive maximum edge-sample tests for one operation.</summary>
    public long MaximumWork;

    /// <summary>Creates bounded deterministic rasterization defaults.</summary>
    public SvgRasterOptions()
    {
        Tolerance = 0.25;
        Transform = null;
        Clip = null;
        SamplesPerAxis = 4;
        MaximumSubdivisionDepth = 32;
        MaximumEdges = 2000000;
        MaximumWork = 1000000000;
    }
}

/// <summary>Contains one completed fill scan conversion or a stable failure.</summary>
public class SvgRasterResult
{
    /// <summary>Whether scan conversion succeeded.</summary>
    public bool Success;
    /// <summary>The stable scan-conversion outcome.</summary>
    public SvgRasterError Error;
    /// <summary>The surface-local candidate bounds after geometry and clip intersection.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The flattened and implicitly closing edge count.</summary>
    public int EdgeCount;
    /// <summary>The preflighted edge-sample test count.</summary>
    public long WorkUnits;
    /// <summary>The number of pixels receiving nonzero coverage.</summary>
    public long PixelsWritten;

    internal SvgRasterResult(bool success, SvgRasterError error, SvgPixelRect bounds, int edgeCount, long workUnits, long pixelsWritten)
    {
        Success = success;
        Error = error;
        Bounds = bounds;
        EdgeCount = edgeCount;
        WorkUnits = workUnits;
        PixelsWritten = pixelsWritten;
    }
}
