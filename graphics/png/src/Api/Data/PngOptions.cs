namespace png;

/// <summary>
/// Defines allocation and complexity limits used to reject unreasonably large or hostile PNG inputs.
/// </summary>
public class PngSecurityLimits
{
    /// <summary>
    /// Initializes a new instance of the PngSecurityLimits class with its documented default values.
    /// </summary>
    public PngSecurityLimits()
    {
    }

    /// <summary>
    /// Defines the default maximum image width in pixels.
    /// </summary>
    public const int DefaultMaxWidth = 100000;
    /// <summary>
    /// Defines the default maximum image height in pixels.
    /// </summary>
    public const int DefaultMaxHeight = 100000;
    /// <summary>
    /// Defines the default maximum product of image width and height.
    /// </summary>
    public const long DefaultMaxPixelCount = 268435456;
    /// <summary>
    /// Defines the default maximum decoded bytes in one image or frame row.
    /// </summary>
    public const long DefaultMaxRowBytes = 1073741824;
    /// <summary>
    /// Defines the default maximum number of chunks in one datastream.
    /// </summary>
    public const int DefaultMaxChunkCount = 100000;
    /// <summary>
    /// Defines the default maximum combined ancillary metadata size in bytes.
    /// </summary>
    public const long DefaultMaxMetadataBytes = 67108864;
    /// <summary>
    /// Defines the default maximum decompressed text size in bytes.
    /// </summary>
    public const long DefaultMaxTextBytes = 16777216;
    /// <summary>
    /// Defines the default maximum number of text entries.
    /// </summary>
    public const int DefaultMaxTextEntries = 10000;
    /// <summary>
    /// Defines the default maximum combined compressed image-data size in bytes.
    /// </summary>
    public const long DefaultMaxIdatBytes = 1073741824;
    /// <summary>
    /// Defines the default maximum decompressed image and profile data size in bytes.
    /// </summary>
    public const long DefaultMaxDecompressedBytes = 2147483648;
    /// <summary>
    /// Defines the default maximum number of animation frames.
    /// </summary>
    public const int DefaultMaxAnimationFrames = 10000;
    /// <summary>
    /// Defines the default maximum decoded bytes in one image or animation frame.
    /// </summary>
    public const long DefaultMaxFrameBytes = 2147483648;
    /// <summary>
    /// Defines the default maximum combined decoded bytes across animation frames and composed frames.
    /// </summary>
    public const long DefaultMaxAnimationBytes = 2147483648;

    /// <summary>
    /// Stores the maximum accepted image width in pixels.
    /// </summary>
    public int MaxWidth = DefaultMaxWidth;
    /// <summary>
    /// Stores the maximum accepted image height in pixels.
    /// </summary>
    public int MaxHeight = DefaultMaxHeight;
    /// <summary>
    /// Stores the maximum accepted product of image width and height.
    /// </summary>
    public long MaxPixelCount = DefaultMaxPixelCount;
    /// <summary>
    /// Stores the maximum accepted decoded bytes in one image or frame row.
    /// </summary>
    public long MaxRowBytes = DefaultMaxRowBytes;
    /// <summary>
    /// Stores the maximum accepted number of chunks in one datastream.
    /// </summary>
    public int MaxChunkCount = DefaultMaxChunkCount;
    /// <summary>
    /// Stores the maximum accepted combined ancillary metadata size in bytes.
    /// </summary>
    public long MaxMetadataBytes = DefaultMaxMetadataBytes;
    /// <summary>
    /// Stores the maximum accepted decompressed text size in bytes.
    /// </summary>
    public long MaxTextBytes = DefaultMaxTextBytes;
    /// <summary>
    /// Stores the maximum accepted number of text entries.
    /// </summary>
    public int MaxTextEntries = DefaultMaxTextEntries;
    /// <summary>
    /// Stores the maximum accepted combined compressed image-data size in bytes.
    /// </summary>
    public long MaxIdatBytes = DefaultMaxIdatBytes;
    /// <summary>
    /// Stores the maximum accepted decompressed image and profile data size in bytes.
    /// </summary>
    public long MaxDecompressedBytes = DefaultMaxDecompressedBytes;
    /// <summary>
    /// Stores the maximum accepted number of APNG frames.
    /// </summary>
    public int MaxAnimationFrames = DefaultMaxAnimationFrames;
    /// <summary>
    /// Stores the maximum accepted decoded bytes in one image or animation frame.
    /// </summary>
    public long MaxFrameBytes = DefaultMaxFrameBytes;
    /// <summary>
    /// Stores the maximum accepted combined decoded bytes across animation frames and composed frames.
    /// </summary>
    public long MaxAnimationBytes = DefaultMaxAnimationBytes;
}

/// <summary>
/// Controls raw PNG chunk placement and security limits while writing a complete datastream.
/// </summary>
public class PngChunkWriteOptions
{
    /// <summary>
    /// Initializes a new instance of the PngChunkWriteOptions class with its documented default values.
    /// </summary>
    public PngChunkWriteOptions()
    {
    }

    /// <summary>
    /// Selects whether caller-supplied placement is preserved or canonicalized.
    /// </summary>
    public PngChunkPlacementPolicy PlacementPolicy = PngChunkPlacementPolicy.Preserve;
    /// <summary>
    /// Stores the limits used to bound output chunks and allocations.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
}

/// <summary>
/// Selects a pixel-format conversion and supplies palette or transparency metadata when required.
/// </summary>
public class PngPixelConversionOptions
{
    /// <summary>
    /// Initializes a new instance of the PngPixelConversionOptions class with its documented default values.
    /// </summary>
    public PngPixelConversionOptions()
    {
    }

    /// <summary>
    /// Selects the destination channel layout and sample depth.
    /// </summary>
    public PngPixelFormat TargetFormat = PngPixelFormat.Rgba8;
    /// <summary>
    /// Stores the palette required when indexed pixels are expanded or created.
    /// </summary>
    public PngPalette? Palette;
    /// <summary>
    /// Stores optional per-entry alpha used when indexed pixels are expanded or created.
    /// </summary>
    public PngIndexedTransparency? IndexedTransparency;
    /// <summary>
    /// Stores optional grayscale transparency expanded when the destination contains alpha.
    /// </summary>
    public PngGrayscaleTransparency? GrayscaleTransparency;
    /// <summary>
    /// Stores optional truecolor transparency expanded when the destination contains alpha.
    /// </summary>
    public PngTruecolorTransparency? TruecolorTransparency;
}

/// <summary>
/// Controls validation, output format, metadata preservation, animation decoding, and safety limits while reading PNG data.
/// </summary>
public class PngDecoderOptions
{
    /// <summary>
    /// Initializes a new instance of the PngDecoderOptions class with its documented default values.
    /// </summary>
    public PngDecoderOptions()
    {
    }

    /// <summary>
    /// Selects strict validation, tolerant metadata handling, repair-oriented validation, or best-effort inspection.
    /// </summary>
    public PngErrorPolicy ErrorPolicy = PngErrorPolicy.Strict;
    /// <summary>
    /// Selects the requested decoded pixel format; conversion occurs after scanline reconstruction.
    /// </summary>
    public PngPixelFormat PreferredPixelFormat = PngPixelFormat.Rgba8;
    /// <summary>
    /// Stores the limits used to bound parsing, decompression, and allocations.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
    /// <summary>
    /// Controls whether every chunk CRC is checked while reading.
    /// </summary>
    public bool ValidateCrc = true;
    /// <summary>
    /// Controls whether unrecognized ancillary chunks are retained for later writing.
    /// </summary>
    public bool PreserveUnknownChunks = true;
    /// <summary>
    /// Controls whether APNG frames are decoded in addition to the default image.
    /// </summary>
    public bool DecodeAnimation = true;
    /// <summary>
    /// Controls whether bytes after the IEND chunk are tolerated.
    /// </summary>
    public bool IgnoreTrailingBytes;
}

/// <summary>
/// Controls PNG color layout, filtering, compression, interlacing, metadata handling, animation, and chunk sizing.
/// </summary>
public class PngEncoderOptions
{
    /// <summary>
    /// Initializes a new instance of the PngEncoderOptions class with its documented default values.
    /// </summary>
    public PngEncoderOptions()
    {
    }

    /// <summary>
    /// Selects the encoded PNG color type when automatic color-type selection is disabled.
    /// </summary>
    public PngColorType ColorType = PngColorType.TruecolorAlpha;
    /// <summary>
    /// Selects the encoded sample depth when automatic bit-depth selection is disabled.
    /// </summary>
    public PngBitDepth BitDepth = PngBitDepth.Eight;
    /// <summary>
    /// Selects the PNG compression method; PNG currently defines only Deflate.
    /// </summary>
    public PngCompressionMethod CompressionMethod = PngCompressionMethod.Deflate;
    /// <summary>
    /// Selects the PNG scanline filter method; PNG currently defines only Adaptive.
    /// </summary>
    public PngFilterMethod FilterMethod = PngFilterMethod.Adaptive;
    /// <summary>
    /// Selects direct scanlines or seven-pass Adam7 interlacing.
    /// </summary>
    public PngInterlaceMethod InterlaceMethod = PngInterlaceMethod.None;
    /// <summary>
    /// Selects the row filter used for every scanline when fixed filtering is enabled.
    /// </summary>
    public PngFilterType FixedFilterType = PngFilterType.None;
    /// <summary>
    /// Selects how a filter is chosen for each encoded row.
    /// </summary>
    public PngRowFilterStrategy RowFilterStrategy = PngRowFilterStrategy.Adaptive;
    /// <summary>
    /// Selects which ancillary metadata is copied into the encoded datastream.
    /// </summary>
    public PngMetadataPolicy MetadataPolicy = PngMetadataPolicy.Preserve;
    /// <summary>
    /// Selects whether text compression is retained, forced, disabled, or chosen by size.
    /// </summary>
    public PngTextCompressionMode TextCompressionMode = PngTextCompressionMode.Preserve;
    /// <summary>
    /// Stores the encoded text length at which Automatic text compression begins.
    /// </summary>
    public int TextCompressionThreshold = 1024;
    /// <summary>
    /// Selects whether preserved unknown ancillary chunks are copied, dropped, or replaced.
    /// </summary>
    public PngChunkDisposition UnknownChunkDisposition = PngChunkDisposition.Preserve;
    /// <summary>
    /// Stores ancillary chunk types retained when MetadataPolicy is KeepSelected.
    /// </summary>
    public string[] MetadataChunkTypesToKeep = new string[0];
    /// <summary>
    /// Stores ancillary chunk types omitted when MetadataPolicy is RemoveSelected.
    /// </summary>
    public string[] MetadataChunkTypesToRemove = new string[0];
    /// <summary>
    /// Stores replacement payloads used when UnknownChunkDisposition is Replace.
    /// </summary>
    public PngUnknownChunkReplacement[] UnknownChunkReplacements = new PngUnknownChunkReplacement[0];
    /// <summary>
    /// Indicates that pixel data changed, requiring unknown chunks marked unsafe-to-copy to be omitted unless explicitly replaced.
    /// </summary>
    public bool PixelDataChanged = true;
    /// <summary>
    /// Stores the limits used to bound encoding work, output chunks, and animation frames.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
    /// <summary>
    /// Stores the Deflate compression level from 0 through 9.
    /// </summary>
    public int CompressionLevel = PngCompressionLevelMapper.MaximumCompressionLevel;
    /// <summary>
    /// Stores the maximum IDAT payload size; zero writes image data without an explicit split limit.
    /// </summary>
    public int MaxIdatChunkBytes;
    /// <summary>
    /// Stores the maximum compressed frame-data bytes in each fdAT payload, excluding its sequence number; zero disables splitting.
    /// </summary>
    public int MaxFdatChunkBytes;
    /// <summary>
    /// Controls whether the encoder derives the smallest lossless color type from the pixels.
    /// </summary>
    public bool UseAutomaticColorType = true;
    /// <summary>
    /// Controls whether the encoder derives the smallest lossless sample depth from the pixels.
    /// </summary>
    public bool UseAutomaticBitDepth = true;
    /// <summary>
    /// Controls whether every row uses FixedFilterType, overriding RowFilterStrategy for compatibility.
    /// </summary>
    public bool UseFixedFilter;
    /// <summary>
    /// Controls whether animation frames are emitted as APNG chunks.
    /// </summary>
    public bool EncodeAnimation;
    /// <summary>
    /// Controls whether unused trailing palette entries may be removed during content analysis.
    /// </summary>
    public bool AllowPaletteReduction = true;
    /// <summary>
    /// Stores an output byte threshold used to stop encoding early; zero disables early termination.
    /// </summary>
    public long EarlyBailoutByteCount;
    /// <summary>
    /// Stores the output bytes produced or predicted when early termination occurred.
    /// </summary>
    public long ProducedByteCount;
    /// <summary>
    /// Indicates that encoding stopped because output exceeded EarlyBailoutByteCount.
    /// </summary>
    public bool WasEarlyBailedOut;
}

/// <summary>
/// Controls lossless encoding trials, reductions, metadata handling, limits, and early bailout.
/// </summary>
public class PngOptimizerOptions
{
    /// <summary>
    /// Initializes a new instance of the PngOptimizerOptions class with its documented default values.
    /// </summary>
    public PngOptimizerOptions()
    {
    }

    /// <summary>
    /// Stores the limits applied to every trial encode.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
    /// <summary>
    /// Selects which metadata is retained in trial outputs.
    /// </summary>
    public PngMetadataPolicy MetadataPolicy = PngMetadataPolicy.Preserve;
    /// <summary>
    /// Selects whether text compression is retained, forced, disabled, or chosen by size.
    /// </summary>
    public PngTextCompressionMode TextCompressionMode = PngTextCompressionMode.Preserve;
    /// <summary>
    /// Stores the encoded text length at which Automatic text compression begins.
    /// </summary>
    public int TextCompressionThreshold = 1024;
    /// <summary>
    /// Selects how unknown ancillary chunks are handled in trial outputs.
    /// </summary>
    public PngChunkDisposition UnknownChunkDisposition = PngChunkDisposition.Preserve;
    /// <summary>
    /// Stores ancillary chunk types retained when MetadataPolicy is KeepSelected.
    /// </summary>
    public string[] MetadataChunkTypesToKeep = new string[0];
    /// <summary>
    /// Stores ancillary chunk types omitted when MetadataPolicy is RemoveSelected.
    /// </summary>
    public string[] MetadataChunkTypesToRemove = new string[0];
    /// <summary>
    /// Stores replacement payloads used when UnknownChunkDisposition is Replace.
    /// </summary>
    public PngUnknownChunkReplacement[] UnknownChunkReplacements = new PngUnknownChunkReplacement[0];
    /// <summary>
    /// Indicates that trial pixels differ from the source represented by preserved metadata.
    /// </summary>
    public bool PixelDataChanged = true;
    /// <summary>
    /// Selects the filter used when a trial forces one filter for every row.
    /// </summary>
    public PngFilterType FixedFilterType = PngFilterType.None;
    /// <summary>
    /// Selects one row-filter strategy or exhaustive comparison of all available strategies.
    /// </summary>
    public PngRowFilterStrategy RowFilterStrategy = PngRowFilterStrategy.Exhaustive;
    /// <summary>
    /// Stores the first Deflate compression level to try, from 0 through 9.
    /// </summary>
    public int MinimumCompressionLevel;
    /// <summary>
    /// Stores the last Deflate compression level to try, from 0 through 9.
    /// </summary>
    public int MaximumCompressionLevel = PngCompressionLevelMapper.MaximumCompressionLevel;
    /// <summary>
    /// Controls whether alternative row-filter strategies are encoded and compared; false forces FixedFilterType for compatibility.
    /// </summary>
    public bool TryFilters = true;
    /// <summary>
    /// Controls whether the configured compression-level range is encoded and compared.
    /// </summary>
    public bool TryCompressionLevels = true;
    /// <summary>
    /// Controls whether the baseline encode forces FixedFilterType, overriding RowFilterStrategy for compatibility.
    /// </summary>
    public bool UseFixedFilter;
    /// <summary>
    /// Controls whether lossless color-type reduction may be considered.
    /// </summary>
    public bool AllowColorTypeReduction = true;
    /// <summary>
    /// Controls whether lossless sample-depth reduction may be considered.
    /// </summary>
    public bool AllowBitDepthReduction = true;
    /// <summary>
    /// Controls whether unused trailing palette entries may be removed.
    /// </summary>
    public bool AllowPaletteReduction = true;
    /// <summary>
    /// Controls whether a trial stops once it cannot beat the current best byte count.
    /// </summary>
    public bool UseEarlyBailout = true;
    /// <summary>
    /// Stores an existing best output size used as the initial bailout threshold; zero means no known result.
    /// </summary>
    public long BestKnownByteCount;
}

/// <summary>
/// Selects the recoverable changes performed by the report-producing PNG repair workflow.
/// </summary>
public class PngRepairOptions
{
    /// <summary>
    /// Initializes a new instance of the PngRepairOptions class with every repair disabled.
    /// </summary>
    public PngRepairOptions()
    {
    }

    /// <summary>
    /// Controls whether bytes after IEND are removed.
    /// </summary>
    public bool RemoveTrailingData;
    /// <summary>
    /// Controls whether recognized malformed ancillary metadata chunks are removed.
    /// </summary>
    public bool RemoveMalformedAncillaryChunks;
    /// <summary>
    /// Controls whether duplicate ancillary chunks whose specification permits at most one occurrence are removed after the first.
    /// </summary>
    public bool RemoveDuplicateAncillaryChunks;
    /// <summary>
    /// Controls whether misplaced ancillary chunks are moved to valid positions.
    /// </summary>
    public bool RepairAncillaryOrdering;
    /// <summary>
    /// Controls whether ancillary CRC values are recalculated.
    /// </summary>
    public bool RepairAncillaryCrc;
    /// <summary>
    /// Stores the limits applied while reading and writing the repaired datastream.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
}
