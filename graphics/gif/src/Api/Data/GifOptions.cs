namespace gif;

/// <summary>Configures structural decoding, animation rendering, recovery behavior, and safety limits.</summary>
/// <remarks>Limits are checked before large allocations where possible. Set limits explicitly when processing untrusted input.</remarks>
public class GifDecodeOptions
{
    /// <summary>Controls how strictly format rules are enforced.</summary>
    public GifStrictness Strictness;
    /// <summary>Gets or sets the greatest permitted logical-screen or frame width in pixels.</summary>
    public int MaxWidth;
    /// <summary>Gets or sets the greatest permitted logical-screen or frame height in pixels.</summary>
    public int MaxHeight;
    /// <summary>Gets or sets the greatest number of image frames that may be read.</summary>
    public int MaxFrames;
    /// <summary>Gets or sets the greatest aggregate number of frame pixels that may be decoded.</summary>
    public int MaxDecodedPixels;
    /// <summary>Gets or sets the greatest number of source bytes that may be consumed.</summary>
    public int MaxBytes;
    /// <summary>Gets or sets the greatest aggregate number of compressed image payload bytes that may be consumed.</summary>
    public int MaxCompressedBytes;
    /// <summary>Gets or sets the greatest number of data sub-blocks that may be consumed.</summary>
    public int MaxSubBlocks;
    /// <summary>Gets or sets the greatest aggregate number of data sub-block payload bytes that may be consumed.</summary>
    public int MaxSubBlockBytes;
    /// <summary>Gets or sets the estimated memory ceiling used before buffers are allocated.</summary>
    public int MaxMemoryBytes;
    /// <summary>Specifies whether compressed image payloads and decoded pixels should be omitted.</summary>
    public bool MetadataOnly;
    /// <summary>Specifies whether reading should stop after the first image frame.</summary>
    public bool FirstFrameOnly;
    /// <summary>Specifies whether unrecognized GIF extensions are retained for later re-encoding.</summary>
    public bool PreserveUnknownExtensions;
    /// <summary>Selects whether plain-text extensions are ignored, preserved as metadata, or exposed as structured graphic data.</summary>
    public GifPlainTextExtensionPolicy PlainTextExtensionPolicy;
    /// <summary>Enables supported best-effort recovery paths and warning diagnostics for malformed streams.</summary>
    public bool TolerateMalformedStreams;
    /// <summary>Provides raw RGB triples to use as a global palette when the stream omits one.</summary>
    /// <remarks>The byte count must describe a legal GIF palette: 2 through 256 colors in a power-of-two count.</remarks>
    public byte[] DefaultGlobalColorTable;
    /// <summary>Selects the rendering behavior for unspecified or reserved disposal values.</summary>
    public GifDisposalMethod DisposalFallback;
    /// <summary>Gets or sets the minimum display time applied to zero-delay frames while coalescing.</summary>
    public int MinimumFrameDelayMilliseconds;
    /// <summary>Selects how cleared canvas pixels are filled while coalescing frames.</summary>
    public GifBackgroundMode BackgroundMode;
    /// <summary>Contains the canvas and disposal-clear color used when <see cref="BackgroundMode"/> is <see cref="GifBackgroundMode.SolidColor"/>.</summary>
    public GifRgbaColor SolidBackgroundColor;

    /// <summary>Creates options with normal validation and conservative built-in safety limits.</summary>
    public GifDecodeOptions()
    {
        Strictness = GifStrictness.Normal;
        MaxWidth = GifConstants.DefaultMaxWidth;
        MaxHeight = GifConstants.DefaultMaxHeight;
        MaxFrames = GifConstants.DefaultMaxFrames;
        MaxDecodedPixels = GifConstants.DefaultMaxDecodedPixels;
        MaxBytes = GifConstants.DefaultMaxBytes;
        MaxCompressedBytes = GifConstants.DefaultMaxCompressedBytes;
        MaxSubBlocks = GifConstants.DefaultMaxSubBlocks;
        MaxSubBlockBytes = GifConstants.DefaultMaxSubBlockBytes;
        MaxMemoryBytes = GifConstants.DefaultMaxMemoryBytes;
        MetadataOnly = false;
        FirstFrameOnly = false;
        PreserveUnknownExtensions = true;
        PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Preserve;
        TolerateMalformedStreams = false;
        DefaultGlobalColorTable = new byte[0];
        DisposalFallback = GifDisposalMethod.DoNotDispose;
        MinimumFrameDelayMilliseconds = 0;
        BackgroundMode = GifBackgroundMode.LogicalScreen;
        SolidBackgroundColor = new GifRgbaColor(0, 0, 0, 255);
    }
}

/// <summary>Configures GIF version selection, palettes, metadata, compression, optimization, and output limits.</summary>
public class GifEncodeOptions
{
    /// <summary>Selects a required output version, or <see cref="GifVersion.Unknown"/> for automatic selection.</summary>
    public GifVersion Version;
    /// <summary>Selects global, local, or automatically chosen color tables.</summary>
    public GifColorTableMode ColorTableMode;
    /// <summary>Selects the default RGBA quantization algorithm.</summary>
    public GifQuantizerMode QuantizerMode;
    /// <summary>Selects the default palette-mapping dither.</summary>
    public GifDitherMode DitherMode;
    /// <summary>Selects one legacy animation transformation to perform before writing.</summary>
    /// <remarks>The independently selectable optimization fields are applied in addition to this value.</remarks>
    public GifOptimizationMode OptimizationMode;
    /// <summary>Specifies whether zero-delay frames are removed before other optimization steps.</summary>
    public bool RemoveZeroDelayFrames;
    /// <summary>Specifies whether consecutive duplicate frames are removed and their delays are merged.</summary>
    public bool RemoveDuplicateFrames;
    /// <summary>Specifies whether zero-delay frames that remain after removals are assigned <see cref="MinimumFrameDelayMilliseconds"/>.</summary>
    public bool NormalizeFrameDelays;
    /// <summary>Gets or sets the delay assigned to remaining zero-delay frames when <see cref="NormalizeFrameDelays"/> is enabled.</summary>
    public int MinimumFrameDelayMilliseconds;
    /// <summary>Specifies whether frames are cropped to their changed rectangles after removal and delay steps.</summary>
    public bool DeconstructFrames;
    /// <summary>Specifies whether the animation's total duration is recalculated after optimization.</summary>
    public bool RecalculateTimeline;
    /// <summary>Controls when the LZW code dictionary is reset.</summary>
    public GifLzwClearPolicy LzwClearPolicy;
    /// <summary>Gets or sets the repeat count: <c>-1</c> omits the loop extension, <c>0</c> loops forever, and positive values are finite repeats.</summary>
    public int LoopCount;
    /// <summary>Gets or sets comments to add to the encoded document.</summary>
    public GifCommentExtension[] Comments;
    /// <summary>Gets or sets application extensions to add to the encoded document.</summary>
    public GifApplicationExtension[] ApplicationExtensions;
    /// <summary>Gets or sets plain-text extensions to add to the encoded document.</summary>
    public GifPlainTextExtension[] PlainTextExtensions;
    /// <summary>Gets or sets unrecognized extension payloads to add to the encoded document.</summary>
    public GifUnknownExtension[] UnknownExtensions;
    /// <summary>Gets or sets the logical screen's background palette index.</summary>
    public int BackgroundColorIndex;
    /// <summary>Selects how transparent pixels are represented during RGBA quantization.</summary>
    public GifTransparentColorMode TransparentColorMode;
    /// <summary>Specifies whether encoded image rows use GIF's four-pass interlace order.</summary>
    public bool Interlace;
    /// <summary>Specifies whether metadata already present on a document is re-emitted.</summary>
    public bool PreserveMetadata;
    /// <summary>Specifies whether the complete document is validated before bytes are written.</summary>
    public bool ValidateBeforeWrite;
    /// <summary>Gets or sets the greatest permitted encoded byte count.</summary>
    public int MaxBytes;
    /// <summary>Gets or sets the estimated memory ceiling used by encoding operations.</summary>
    public int MaxMemoryBytes;

    /// <summary>Creates options that choose the earliest valid version, preserve metadata, and validate before writing.</summary>
    public GifEncodeOptions()
    {
        Version = GifVersion.Unknown;
        ColorTableMode = GifColorTableMode.Auto;
        QuantizerMode = GifQuantizerMode.Exact;
        DitherMode = GifDitherMode.None;
        OptimizationMode = GifOptimizationMode.None;
        RemoveZeroDelayFrames = false;
        RemoveDuplicateFrames = false;
        NormalizeFrameDelays = false;
        MinimumFrameDelayMilliseconds = 0;
        DeconstructFrames = false;
        RecalculateTimeline = true;
        LzwClearPolicy = GifLzwClearPolicy.ClearOnFullDictionary;
        LoopCount = -1;
        Comments = new GifCommentExtension[0];
        ApplicationExtensions = new GifApplicationExtension[0];
        PlainTextExtensions = new GifPlainTextExtension[0];
        UnknownExtensions = new GifUnknownExtension[0];
        BackgroundColorIndex = 0;
        TransparentColorMode = GifTransparentColorMode.None;
        Interlace = false;
        PreserveMetadata = true;
        ValidateBeforeWrite = true;
        MaxBytes = GifConstants.DefaultMaxBytes;
        MaxMemoryBytes = GifConstants.DefaultMaxMemoryBytes;
    }
}

/// <summary>Supplies timing, disposal, placement, transparency, and looping values when creating an animation.</summary>
public class GifAnimationOptions
{
    /// <summary>Gets or sets per-frame delays in milliseconds; missing entries use <see cref="DefaultDelayMilliseconds"/>.</summary>
    public int[] DelaysMilliseconds;
    /// <summary>Gets or sets the delay used when a frame has no matching entry in <see cref="DelaysMilliseconds"/>.</summary>
    public int DefaultDelayMilliseconds;
    /// <summary>Gets or sets per-frame disposal methods; missing entries use <see cref="DefaultDisposalMethod"/>.</summary>
    public GifDisposalMethod[] DisposalMethods;
    /// <summary>Gets or sets the disposal method used when no per-frame value is supplied.</summary>
    public GifDisposalMethod DefaultDisposalMethod;
    /// <summary>Gets or sets the repeat count: <c>-1</c> omits looping, <c>0</c> loops forever, and positive values are finite repeats.</summary>
    public int LoopCount;
    /// <summary>Gets or sets per-frame horizontal offsets from the logical screen's left edge.</summary>
    public int[] FrameLefts;
    /// <summary>Gets or sets per-frame vertical offsets from the logical screen's top edge.</summary>
    public int[] FrameTops;
    /// <summary>Specifies whether created indexed frames use a transparent palette entry.</summary>
    public bool UseTransparency;
    /// <summary>Gets or sets the palette index treated as transparent when <see cref="UseTransparency"/> is enabled.</summary>
    public int TransparentColorIndex;
    /// <summary>Selects how animation background pixels are interpreted.</summary>
    public GifBackgroundMode BackgroundMode;

    /// <summary>Creates animation options with a 100 ms default delay, no loop extension, and no transparency.</summary>
    public GifAnimationOptions()
    {
        DelaysMilliseconds = new int[0];
        DefaultDelayMilliseconds = 100;
        DisposalMethods = new GifDisposalMethod[0];
        DefaultDisposalMethod = GifDisposalMethod.DoNotDispose;
        LoopCount = -1;
        FrameLefts = new int[0];
        FrameTops = new int[0];
        UseTransparency = false;
        TransparentColorIndex = -1;
        BackgroundMode = GifBackgroundMode.LogicalScreen;
    }
}

/// <summary>Configures conversion of RGBA pixels to a GIF-compatible indexed palette.</summary>
public class GifQuantizeOptions
{
    /// <summary>Gets or sets the maximum number of palette colors, from 2 through 256.</summary>
    public int MaxColors;
    /// <summary>Selects the palette-building algorithm.</summary>
    public GifQuantizerMode QuantizerMode;
    /// <summary>Selects how color-mapping error is distributed.</summary>
    public GifDitherMode DitherMode;
    /// <summary>Gets or sets the interval used to sample source pixels; larger values trade fidelity for speed.</summary>
    public int SamplingStep;
    /// <summary>Gets or sets the inclusive alpha threshold from 0 through 255 used to classify transparent pixels.</summary>
    public int TransparentAlphaThreshold;
    /// <summary>Selects whether an animation should use a shared palette or per-frame palettes.</summary>
    public GifColorTableMode PaletteMode;
    /// <summary>Specifies whether palette entry order is preserved; <c>false</c> sorts entries by red, green, and blue channel values.</summary>
    public bool PreservePaletteOrder;
    /// <summary>Contains the palette used when <see cref="QuantizerMode"/> is <see cref="GifQuantizerMode.CallerPalette"/>.</summary>
    public GifPalette CallerPalette;
    /// <summary>Selects how RGBA pixels are classified as transparent during quantization.</summary>
    public GifTransparentColorMode TransparentColorMode;
    /// <summary>Contains the exact RGBA value classified as transparent when <see cref="TransparentColorMode"/> is <see cref="GifTransparentColorMode.ExactColor"/>.</summary>
    public GifRgbaColor ExactTransparentColor;
    /// <summary>Contains the caller-palette index reserved for transparency, or <c>-1</c> for automatic exact-color lookup.</summary>
    public int TransparentColorIndex;

    /// <summary>Creates options for a 256-color exact palette without dithering.</summary>
    public GifQuantizeOptions()
    {
        MaxColors = GifConstants.MaximumColorTableSize;
        QuantizerMode = GifQuantizerMode.Exact;
        DitherMode = GifDitherMode.None;
        SamplingStep = 1;
        TransparentAlphaThreshold = 0;
        PaletteMode = GifColorTableMode.Auto;
        PreservePaletteOrder = true;
        CallerPalette = new GifPalette(null);
        TransparentColorMode = GifTransparentColorMode.AlphaThreshold;
        ExactTransparentColor = new GifRgbaColor(0, 0, 0, 0);
        TransparentColorIndex = -1;
    }
}
