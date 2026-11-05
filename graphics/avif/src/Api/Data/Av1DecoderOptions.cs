namespace avif;

/// <summary>
/// Configures the low-level managed AV1 decoder.
/// </summary>
public class Av1DecoderOptions
{
    /// <summary>The operating-point index, or negative one for automatic selection.</summary>
    public int OperatingPoint;

    /// <summary>The maximum spatial layer to output, or negative one for all selected layers.</summary>
    public int SpatialLayer;

    /// <summary>The maximum temporal layer to output, or negative one for all selected layers.</summary>
    public int TemporalLayer;

    /// <summary>Whether each selected spatial layer may be emitted as progressive output.</summary>
    public bool AllowProgressive;

    /// <summary>Whether the optional AV1 large-scale tile decoding mode is enabled.</summary>
    public bool DecodeLargeScaleTiles;

    /// <summary>Whether decoded tile rows may be exposed before a frame is complete.</summary>
    public bool AllowIncremental;

    /// <summary>Whether normative film grain is applied to displayed output.</summary>
    public bool ApplyFilmGrain;

    /// <summary>Whether all applicable AV1 semantic conformance checks are enabled.</summary>
    public bool Strict;

    /// <summary>The requested worker count, bounded by the active security limits.</summary>
    public int WorkerCount;

    /// <summary>The resource limits applied during AV1 parsing and decoding.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>The cancellation signal checked at bounded decode checkpoints.</summary>
    public AvifCancellation Cancellation;

    /// <summary>
    /// Creates strict, deterministic AV1 decoder options with automatic layer selection.
    /// </summary>
    public Av1DecoderOptions()
    {
        OperatingPoint = -1;
        SpatialLayer = -1;
        TemporalLayer = -1;
        AllowProgressive = false;
        DecodeLargeScaleTiles = false;
        AllowIncremental = false;
        ApplyFilmGrain = true;
        Strict = true;
        WorkerCount = 1;
        Limits = new AvifSecurityLimits();
        Cancellation = new AvifCancellation();
    }
}
