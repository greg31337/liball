namespace svg;

/// <summary>Configures one mapped decoded-image point sample.</summary>
public class SvgImageSampleOptions
{
    /// <summary>The automatic or explicit interpolation mode.</summary>
    public SvgImageSamplingMode SamplingMode;
    /// <summary>The additional opacity from zero through one.</summary>
    public double Opacity;

    /// <summary>Creates an automatic opaque image sample.</summary>
    public SvgImageSampleOptions()
    {
        SamplingMode = SvgImageSamplingMode.Auto;
        Opacity = 1;
    }
}

/// <summary>Contains one mapped premultiplied image sample.</summary>
public class SvgImageSampleResult
{
    /// <summary>Whether mapping and sampling completed.</summary>
    public bool Success;
    /// <summary>The stable sampling outcome.</summary>
    public SvgImageSampleError Error;
    /// <summary>Whether the point lies on decoded image content.</summary>
    public bool HasPixel;
    /// <summary>The sampled premultiplied pixel, transparent when uncovered or failed.</summary>
    public SvgPremultipliedRgba8 Pixel;
    /// <summary>The decoded source x coordinate before filtering.</summary>
    public double SourceX;
    /// <summary>The decoded source y coordinate before filtering.</summary>
    public double SourceY;
    /// <summary>The concrete color space of the returned pixel.</summary>
    public SvgPixelColorSpace ColorSpace;

    internal SvgImageSampleResult()
    {
        Success = false;
        Error = SvgImageSampleError.None;
        HasPixel = false;
        Pixel = new SvgPremultipliedRgba8(0, 0, 0, 0);
        SourceX = 0;
        SourceY = 0;
        ColorSpace = SvgPixelColorSpace.Srgb;
    }
}

/// <summary>Configures direct clipped image drawing without an intermediate scaled surface.</summary>
public class SvgImageDrawOptions
{
    /// <summary>The automatic or explicit interpolation mode.</summary>
    public SvgImageSamplingMode SamplingMode;
    /// <summary>The additional opacity from zero through one.</summary>
    public double Opacity;
    /// <summary>The optional destination-local integer clip.</summary>
    public SvgPixelRect? Clip;
    /// <summary>The positive maximum destination pixels tested after clipping.</summary>
    public long MaximumPixels;

    /// <summary>Creates automatic opaque drawing with bounded destination work.</summary>
    public SvgImageDrawOptions()
    {
        SamplingMode = SvgImageSamplingMode.Auto;
        Opacity = 1;
        Clip = null;
        MaximumPixels = 100000000;
    }
}

/// <summary>Contains one completed direct image draw or a stable failure.</summary>
public class SvgImageDrawResult
{
    /// <summary>Whether image drawing completed.</summary>
    public bool Success;
    /// <summary>The stable drawing outcome.</summary>
    public SvgImageSampleError Error;
    /// <summary>The clipped candidate destination bounds.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The number of destination pixels tested.</summary>
    public long PixelsTested;
    /// <summary>The number of destination pixels replaced by image samples.</summary>
    public long PixelsWritten;

    internal SvgImageDrawResult()
    {
        Success = false;
        Error = SvgImageSampleError.None;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        PixelsTested = 0;
        PixelsWritten = 0;
    }
}
