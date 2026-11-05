namespace svg;

/// <summary>Configures deterministic paint sampling, pattern bounds, context paints, and output encoding.</summary>
public class SvgPaintSampleOptions
{
    /// <summary>The additional clamped paint opacity.</summary>
    public double Opacity;
    /// <summary>The concrete output pixel encoding.</summary>
    public SvgPixelColorSpace OutputColorSpace;
    /// <summary>The positive maximum normalized gradient stop count.</summary>
    public int MaximumStops;
    /// <summary>The raster allocation limits applied to each offscreen pattern tile.</summary>
    public SvgRasterLimits RasterLimits;
    /// <summary>The positive maximum number of active nested patterns.</summary>
    public int MaximumPatternDepth;
    /// <summary>The positive maximum number of pattern content nodes visited by one sample.</summary>
    public int MaximumPatternNodes;
    /// <summary>The pattern path-coverage samples along each pixel axis.</summary>
    public int PatternSamplesPerAxis;
    /// <summary>The optional paint substituted for <c>context-fill</c> in pattern content.</summary>
    public SvgSceneResource? ContextFill;
    /// <summary>The optional paint substituted for <c>context-stroke</c> in pattern content.</summary>
    public SvgSceneResource? ContextStroke;

    /// <summary>Creates opaque sRGB sampling defaults with a secure stop limit.</summary>
    public SvgPaintSampleOptions()
    {
        Opacity = 1;
        OutputColorSpace = SvgPixelColorSpace.Srgb;
        MaximumStops = 100000;
        RasterLimits = new SvgRasterLimits();
        MaximumPatternDepth = 16;
        MaximumPatternNodes = 100000;
        PatternSamplesPerAxis = 4;
        ContextFill = null;
        ContextStroke = null;
    }
}

/// <summary>Contains one premultiplied sampled paint value.</summary>
public class SvgPaintSampleResult
{
    /// <summary>Whether sampling completed without an invalid resource or arithmetic failure.</summary>
    public bool Success;
    /// <summary>The stable sampling outcome.</summary>
    public SvgPaintSampleError Error;
    /// <summary>Whether the paint covers the sample point.</summary>
    public bool HasPaint;
    /// <summary>The premultiplied output pixel, transparent after no paint or failure.</summary>
    public SvgPremultipliedRgba8 Pixel;
    /// <summary>The spread-adjusted gradient offset, or zero for solid and uncovered samples.</summary>
    public double Offset;
    /// <summary>The lower normalized stop index, or <c>-1</c>.</summary>
    public int LowerStopIndex;
    /// <summary>The upper normalized stop index, or <c>-1</c>.</summary>
    public int UpperStopIndex;
    /// <summary>The normalized stop count used by the sample.</summary>
    public int StopCount;

    internal SvgPaintSampleResult()
    {
        Success = false;
        Error = SvgPaintSampleError.None;
        HasPaint = false;
        Pixel = new SvgPremultipliedRgba8(0, 0, 0, 0);
        Offset = 0;
        LowerStopIndex = -1;
        UpperStopIndex = -1;
        StopCount = 0;
    }
}
