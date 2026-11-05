namespace avif;

/// <summary>
/// Configures AV1 encoding and AVIF container authoring.
/// </summary>
public class AvifEncoderOptions
{
    /// <summary>The encoder rate-control objective.</summary>
    public AvifEncoderMode Mode;

    /// <summary>The encoder effort and compression tradeoff.</summary>
    public AvifEncoderPreset Preset;

    /// <summary>The requested container header representation.</summary>
    public AvifHeaderFormat HeaderFormat;

    /// <summary>The color quality from zero through one hundred.</summary>
    public int Quality;

    /// <summary>The alpha quality from zero through one hundred.</summary>
    public int AlphaQuality;

    /// <summary>The gain-map quality from zero through one hundred.</summary>
    public int GainMapQuality;

    /// <summary>The arbitrary auxiliary-image quality from zero through one hundred.</summary>
    public int AuxiliaryQuality;

    /// <summary>The default grid-cell quality from zero through one hundred.</summary>
    public int GridCellQuality;

    /// <summary>The default progressive-layer quality from zero through one hundred.</summary>
    public int LayerQuality;

    /// <summary>The explicit AV1 quantizer, or negative one when the selected mode does not use it.</summary>
    public int Quantizer;

    /// <summary>The target still-image size in bytes, or zero when unused.</summary>
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

    /// <summary>Whether authored AV1 sequence headers permit film-grain parameters.</summary>
    public bool FilmGrainParametersPresent;

    /// <summary>The temporal layer count from one through eight.</summary>
    public int TemporalLayerCount;

    /// <summary>Increasing-resolution authored spatial layers, or an empty array for one implicit layer.</summary>
    public Av1SpatialLayerOptions[] SpatialLayers;

    /// <summary>Explicit operating points, or an empty array for automatically generated points.</summary>
    public Av1OperatingPointOptions[] OperatingPoints;

    /// <summary>Whether intermediate spatial layers are displayable outputs.</summary>
    public bool ProgressiveOutput;

    /// <summary>Whether tile rows and columns are selected automatically.</summary>
    public bool AutoTiling;

    /// <summary>The base-two logarithm of the tile-row count when automatic tiling is disabled.</summary>
    public int TileRowsLog2;

    /// <summary>The base-two logarithm of the tile-column count when automatic tiling is disabled.</summary>
    public int TileColumnsLog2;

    /// <summary>The requested worker count, bounded by the active security limits.</summary>
    public int WorkerCount;

    /// <summary>Whether identical inputs and settings must produce identical output bytes.</summary>
    public bool Deterministic;

    /// <summary>Whether authored output is validated before it is returned.</summary>
    public bool ValidateOutput;

    /// <summary>The resource limits applied while encoding and authoring.</summary>
    public AvifSecurityLimits? Limits;

    /// <summary>
    /// Creates deterministic constant-quality encoder options with lossless alpha and validated output.
    /// </summary>
    public AvifEncoderOptions()
    {
        Mode = AvifEncoderMode.ConstantQuality;
        Preset = AvifEncoderPreset.Balanced;
        HeaderFormat = AvifHeaderFormat.Automatic;
        Quality = 75;
        AlphaQuality = 100;
        GainMapQuality = 75;
        AuxiliaryQuality = 75;
        GridCellQuality = 75;
        LayerQuality = 75;
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
        AutoTiling = true;
        TileRowsLog2 = 0;
        TileColumnsLog2 = 0;
        WorkerCount = 1;
        Deterministic = true;
        ValidateOutput = true;
        Limits = new AvifSecurityLimits();
    }
}
