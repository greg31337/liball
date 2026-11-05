namespace avif;

/// <summary>
/// Configures color conversion, chroma resampling, bit-depth reduction, and alpha output.
/// </summary>
public class AvifColorConversionOptions
{
    /// <summary>The chroma resampling filter.</summary>
    public AvifResamplingFilter ResamplingFilter;

    /// <summary>The dithering strategy used during bit-depth reduction.</summary>
    public AvifDitherMode DitherMode;

    /// <summary>Whether an applicable embedded ICC profile takes precedence over CICP signaling.</summary>
    public bool UseEmbeddedIccProfile;

    /// <summary>Whether values outside the destination gamut are clamped to representable endpoints.</summary>
    public bool ClampOutOfGamut;

    /// <summary>Whether an available alpha channel is retained in packed output.</summary>
    public bool PreserveAlpha;

    /// <summary>Whether packed color output is premultiplied by alpha.</summary>
    public bool OutputPremultipliedAlpha;

    /// <summary>The requested worker count, bounded by the active security limits.</summary>
    public int WorkerCount;

    /// <summary>
    /// Creates deterministic color-conversion options with bilinear resampling and no dithering.
    /// </summary>
    public AvifColorConversionOptions()
    {
        ResamplingFilter = AvifResamplingFilter.Bilinear;
        DitherMode = AvifDitherMode.None;
        UseEmbeddedIccProfile = true;
        ClampOutOfGamut = true;
        PreserveAlpha = true;
        OutputPremultipliedAlpha = false;
        WorkerCount = 1;
    }
}
