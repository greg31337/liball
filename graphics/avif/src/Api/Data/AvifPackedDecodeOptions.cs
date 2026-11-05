namespace avif;

/// <summary>Configures one-call decoding directly into packed pixels.</summary>
public class AvifPackedDecodeOptions
{
    /// <summary>The requested packed component order.</summary>
    public AvifPixelFormat PixelFormat;

    /// <summary>The integer component depth, or 16 for linear half float.</summary>
    public int BitDepth;

    /// <summary>Whether RGB or RGBA components contain linear half-float values.</summary>
    public bool FloatingPoint;

    /// <summary>The rendering and color-conversion settings.</summary>
    public AvifRenderOptions RenderOptions;

    /// <summary>The parsing, decoded-storage, rendering, and temporary-storage limits.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>The cancellation signal checked between bounded workflow stages.</summary>
    public AvifCancellation Cancellation;

    /// <summary>Creates eight-bit RGBA output with normative transforms and strict limits.</summary>
    public AvifPackedDecodeOptions()
    {
        PixelFormat = AvifPixelFormat.Rgba;
        BitDepth = 8;
        FloatingPoint = false;
        RenderOptions = new AvifRenderOptions();
        Limits = new AvifSecurityLimits();
        Cancellation = new AvifCancellation();
    }
}
