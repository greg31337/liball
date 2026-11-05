namespace webp;

/// <summary>Describes how seriously a diagnostic affects a WebP operation.</summary>
public enum WebpDiagnosticSeverity
{
    /// <summary>Provides informational context without indicating a problem.</summary>
    Info = 0,
    /// <summary>Reports a non-fatal issue that permissive processing can continue past.</summary>
    Warning = 1,
    /// <summary>Reports an issue that makes the requested operation invalid.</summary>
    Error = 2
}

/// <summary>Controls whether questionable but recoverable WebP data is accepted.</summary>
public enum WebpStrictness
{
    /// <summary>Continues past recoverable format issues and reports them as warnings.</summary>
    Permissive = 0,
    /// <summary>Applies the container rules exactly and reports recoverable deviations as errors.</summary>
    Strict = 1
}

/// <summary>Identifies the RIFF container layout used by a WebP image.</summary>
public enum WebpContainerKind
{
    /// <summary>Indicates that the container layout could not be identified.</summary>
    Unknown = 0,
    /// <summary>Indicates a basic container containing one lossy VP8 image.</summary>
    SimpleLossy = 1,
    /// <summary>Indicates a basic container containing one lossless VP8L image.</summary>
    SimpleLossless = 2,
    /// <summary>Indicates a VP8X container containing a still image and optional extended features.</summary>
    ExtendedStill = 3,
    /// <summary>Indicates a VP8X container containing animation frames.</summary>
    ExtendedAnimation = 4
}

/// <summary>Identifies feature bits declared by a VP8X extended-container header.</summary>
/// <remarks>Values may be combined. Reserved bits are reported separately by <see cref="WebpInfo"/>.</remarks>
public enum WebpVp8XFeatureFlags
{
    /// <summary>No known extended feature is declared.</summary>
    None = 0,
    /// <summary>The container declares animation chunks.</summary>
    Animation = 2,
    /// <summary>The container declares XMP metadata.</summary>
    Xmp = 4,
    /// <summary>The container declares Exif metadata.</summary>
    Exif = 8,
    /// <summary>The container declares alpha information.</summary>
    Alpha = 16,
    /// <summary>The container declares an ICC color profile.</summary>
    IccProfile = 32
}

/// <summary>Identifies the compression bitstream stored for an image or frame.</summary>
public enum WebpBitstreamKind
{
    /// <summary>Indicates that no supported image bitstream was identified.</summary>
    Unknown = 0,
    /// <summary>Indicates a lossy VP8 bitstream.</summary>
    Lossy = 1,
    /// <summary>Indicates a lossless VP8L bitstream.</summary>
    Lossless = 2
}

/// <summary>Selects the main compression strategy used by the encoder.</summary>
public enum WebpEncodeMode
{
    /// <summary>Lets the encoder choose a mode from the image and encoding options.</summary>
    Automatic = 0,
    /// <summary>Uses lossy VP8 compression, with a separate alpha stream when needed.</summary>
    Lossy = 1,
    /// <summary>Uses exact VP8L lossless compression.</summary>
    Lossless = 2,
    /// <summary>Preprocesses colors according to near-lossless quality and then uses VP8L compression.</summary>
    NearLossless = 3
}

/// <summary>Provides starting settings suited to common kinds of lossy source material.</summary>
public enum WebpLossyPreset
{
    /// <summary>Uses general-purpose lossy settings.</summary>
    Default = 0,
    /// <summary>Favors photographic content with natural detail.</summary>
    Photo = 1,
    /// <summary>Favors pictures that mix photographic and synthetic content.</summary>
    Picture = 2,
    /// <summary>Favors drawings with broader areas of similar color.</summary>
    Drawing = 3,
    /// <summary>Favors small, sharp icon graphics.</summary>
    Icon = 4,
    /// <summary>Favors text and other high-contrast edges.</summary>
    Text = 5
}

/// <summary>Selects optional pixel preparation performed before lossy encoding.</summary>
public enum WebpLossyPreprocessing
{
    /// <summary>Encodes the supplied RGB values without preprocessing.</summary>
    None = 0,
    /// <summary>Smooths nearby RGB samples before encoding to reduce local noise.</summary>
    SegmentSmooth = 1,
    /// <summary>Applies deterministic small RGB adjustments to reduce visible banding.</summary>
    Dithering = 2,
    /// <summary>Applies smoothing followed by dithering.</summary>
    SegmentSmoothAndDithering = 3
}

/// <summary>Selects how a lossy image stores its separate alpha plane.</summary>
public enum WebpAlphaCompressionMethod
{
    /// <summary>Stores filtered alpha samples directly.</summary>
    None = 0,
    /// <summary>Compresses alpha samples with a lossless WebP stream.</summary>
    Lossless = 1
}

/// <summary>Selects how the encoder searches for an alpha-plane prediction filter.</summary>
public enum WebpAlphaFilterMode
{
    /// <summary>Does not filter alpha samples before storage.</summary>
    None = 0,
    /// <summary>Uses horizontal alpha prediction without searching every supported filter.</summary>
    Fast = 1,
    /// <summary>Searches the supported filters for the smallest encoded representation.</summary>
    Best = 2
}

/// <summary>Controls how lossy encoding handles non-opaque source pixels.</summary>
public enum WebpLossyAlphaMode
{
    /// <summary>Stores non-opaque alpha in a separate ALPH payload.</summary>
    Preserve = 0,
    /// <summary>Omits alpha while retaining the source RGB channel values.</summary>
    Discard = 1,
    /// <summary>Composites source RGB against a caller-supplied opaque background and omits alpha.</summary>
    BlendWithBackground = 2
}

/// <summary>Controls which metadata payloads are copied while encoding an image model.</summary>
public enum WebpMetadataCopyPolicy
{
    /// <summary>Omits all ICC, Exif, and XMP metadata.</summary>
    None = 0,
    /// <summary>Copies every metadata payload present on the image model.</summary>
    All = 1,
    /// <summary>Copies only the ICC color profile.</summary>
    Icc = 2,
    /// <summary>Copies only the Exif payload.</summary>
    Exif = 3,
    /// <summary>Copies only the XMP payload.</summary>
    Xmp = 4,
    /// <summary>Copies every non-null metadata field, allowing callers to select fields by clearing the others.</summary>
    Selected = 5
}

/// <summary>Controls how unrecognized RIFF chunks are handled during processing.</summary>
public enum WebpUnknownChunkPolicy
{
    /// <summary>Copies unknown chunks when the operation can preserve them.</summary>
    Preserve = 0,
    /// <summary>Omits unknown chunks from the produced image or container.</summary>
    Strip = 1
}

/// <summary>Controls where replacement top-level unknown chunks are placed relative to recognized WebP chunks.</summary>
public enum WebpUnknownChunkPlacement
{
    /// <summary>Places replacement unknown chunks before the first recognized chunk.</summary>
    BeforeRecognizedChunks = 0,
    /// <summary>Places replacement unknown chunks after the last recognized chunk.</summary>
    AfterRecognizedChunks = 1
}

/// <summary>Controls how an animation frame is drawn over the current canvas.</summary>
public enum WebpFrameBlendMode
{
    /// <summary>Alpha-composites the frame over the pixels already on the canvas.</summary>
    AlphaBlend = 0,
    /// <summary>Replaces the covered canvas rectangle with the frame pixels.</summary>
    Replace = 1
}

/// <summary>Controls what happens to an animation frame rectangle after its display time.</summary>
public enum WebpFrameDisposeMode
{
    /// <summary>Leaves the rendered pixels in place for the next frame.</summary>
    DoNotDispose = 0,
    /// <summary>Clears the frame rectangle to the animation background before drawing the next frame.</summary>
    DisposeToBackground = 1
}

/// <summary>Selects the pixel layout returned when decoding animation frames.</summary>
public enum WebpAnimationDecodeMode
{
    /// <summary>Returns each frame as its encoded rectangle, clipped and translated when a source crop is requested; non-intersecting frames remain as zero-area timing entries.</summary>
    IndependentFrames = 0,
    /// <summary>Returns a full-canvas display snapshot after each frame is blended and before that frame is disposed.</summary>
    CompositedFrames = 1
}

/// <summary>Controls how decoded pixels are scaled to requested target dimensions.</summary>
public enum WebpAspectRatioMode
{
    /// <summary>Uses the exact target width and height, allowing the source aspect ratio to change.</summary>
    Stretch = 0,
    /// <summary>Preserves the source aspect ratio and fits the output within the requested target dimensions.</summary>
    Preserve = 1
}
