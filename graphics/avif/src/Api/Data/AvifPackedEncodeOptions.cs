namespace avif;

/// <summary>Configures one-call conversion from packed pixels and AVIF encoding.</summary>
public class AvifPackedEncodeOptions
{
    /// <summary>The encoded planar integer sample depth: 8, 10, or 12.</summary>
    public int BitDepth;

    /// <summary>The encoded monochrome or YUV chroma layout.</summary>
    public AvifChromaFormat ChromaFormat;

    /// <summary>The encoded chroma position relative to luma samples.</summary>
    public AvifChromaSamplePosition ChromaSamplePosition;

    /// <summary>The encoded H.273 color interpretation and integer range.</summary>
    public AvifColorInfo ColorInfo;

    /// <summary>The encoded alpha association, or <c>None</c> to omit alpha.</summary>
    public AvifAlphaMode AlphaMode;

    /// <summary>The packed-to-planar conversion settings.</summary>
    public AvifColorConversionOptions ColorConversion;

    /// <summary>The AV1 encoding and AVIF authoring settings.</summary>
    public AvifEncoderOptions EncoderOptions;

    /// <summary>The conversion, encoded-storage, and temporary-storage limits.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>The cancellation signal checked between bounded workflow stages.</summary>
    public AvifCancellation Cancellation;

    /// <summary>Creates eight-bit 4:4:4 sRGB encoding with straight alpha when available.</summary>
    public AvifPackedEncodeOptions()
    {
        BitDepth = 8;
        ChromaFormat = AvifChromaFormat.Yuv444;
        ChromaSamplePosition = AvifChromaSamplePosition.Unknown;
        ColorInfo = new AvifColorInfo(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Bt709,
            AvifRange.Full);
        AlphaMode = AvifAlphaMode.Straight;
        ColorConversion = new AvifColorConversionOptions();
        EncoderOptions = new AvifEncoderOptions();
        Limits = new AvifSecurityLimits();
        Cancellation = new AvifCancellation();
    }
}
