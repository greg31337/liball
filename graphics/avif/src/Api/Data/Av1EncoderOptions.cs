namespace avif;

/// <summary>
/// Configures the low-level managed AV1 encoder.
/// </summary>
public class Av1EncoderOptions
{
    /// <summary>The encoder rate-control objective.</summary>
    public AvifEncoderMode Mode;

    /// <summary>The encoder effort and compression tradeoff.</summary>
    public AvifEncoderPreset Preset;

    /// <summary>The quality from zero through one hundred.</summary>
    public int Quality;

    /// <summary>The explicit AV1 quantizer, or negative one when the selected mode does not use it.</summary>
    public int Quantizer;

    /// <summary>The target still-frame size in bytes, or zero when unused.</summary>
    public long TargetSizeBytes;

    /// <summary>The target sequence bit rate in bits per second, or zero when unused.</summary>
    public long TargetBitrate;

    /// <summary>The maximum keyframe interval, or zero when no interval is imposed.</summary>
    public int KeyframeInterval;

    /// <summary>The still-picture versus inter-frame sequence-header policy.</summary>
    public AvifSequenceEncodingPolicy SequencePolicy;

    /// <summary>The maximum reordered-frame lag from zero through sixteen.</summary>
    public int FrameLag;

    /// <summary>Whether repeated frames may use show-existing-frame syntax.</summary>
    public bool EnableShowExistingFrames;

    /// <summary>Whether global-motion search and signaling are enabled.</summary>
    public bool EnableGlobalMotion;

    /// <summary>Whether local warped-motion search and signaling are enabled.</summary>
    public bool EnableWarpedMotion;

    /// <summary>Whether the sequence permits frame-level film-grain parameters.</summary>
    public bool FilmGrainParametersPresent;

    /// <summary>The temporal layer count from one through eight.</summary>
    public int TemporalLayerCount;

    /// <summary>Increasing-resolution authored spatial layers, or an empty array for one implicit layer.</summary>
    public Av1SpatialLayerOptions[] SpatialLayers;

    /// <summary>Explicit operating points, or an empty array for automatically generated points.</summary>
    public Av1OperatingPointOptions[] OperatingPoints;

    /// <summary>Whether intermediate spatial layers are displayable outputs.</summary>
    public bool ProgressiveOutput;

    /// <summary>Whether reduced-still-picture syntax is used for a single still frame.</summary>
    public bool ReducedStillPictureHeader;

    /// <summary>Whether tile rows and columns are selected automatically.</summary>
    public bool AutoTiling;

    /// <summary>The base-two logarithm of the tile-row count when automatic tiling is disabled.</summary>
    public int TileRowsLog2;

    /// <summary>The base-two logarithm of the tile-column count when automatic tiling is disabled.</summary>
    public int TileColumnsLog2;

    /// <summary>The requested worker count, bounded by the active security limits.</summary>
    public int WorkerCount;

    /// <summary>Whether identical inputs and settings must produce identical AV1 bytes.</summary>
    public bool Deterministic;

    /// <summary>The resource limits applied during AV1 encoding.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>
    /// Creates deterministic constant-quality AV1 still-picture options.
    /// </summary>
    public Av1EncoderOptions()
    {
        Mode = AvifEncoderMode.ConstantQuality;
        Preset = AvifEncoderPreset.Balanced;
        Quality = 75;
        Quantizer = -1;
        TargetSizeBytes = 0;
        TargetBitrate = 0;
        KeyframeInterval = 0;
        SequencePolicy = AvifSequenceEncodingPolicy.Automatic;
        FrameLag = 0;
        EnableShowExistingFrames = true;
        EnableGlobalMotion = true;
        EnableWarpedMotion = true;
        FilmGrainParametersPresent = false;
        TemporalLayerCount = 1;
        SpatialLayers = System.Array.Empty<Av1SpatialLayerOptions>();
        OperatingPoints = System.Array.Empty<Av1OperatingPointOptions>();
        ProgressiveOutput = false;
        ReducedStillPictureHeader = true;
        AutoTiling = true;
        TileRowsLog2 = 0;
        TileColumnsLog2 = 0;
        WorkerCount = 1;
        Deterministic = true;
        Limits = new AvifSecurityLimits();
    }
}
