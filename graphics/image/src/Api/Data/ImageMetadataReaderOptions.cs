namespace image;

/// <summary>Controls parser safety limits, raw-data retention, optional decoding, and diagnostic detail.</summary>
/// <remarks>Limits are applied independently. A limit reached while parsing is normally recorded as a diagnostic and extraction continues where safe.</remarks>
public class ImageMetadataReaderOptions
{
    /// <summary>The maximum number of source bytes the reader will process.</summary>
    public long MaxFileBytes;
    /// <summary>The maximum declared size accepted for one container segment or chunk.</summary>
    public long MaxSegmentBytes;
    /// <summary>The maximum number of bytes accepted for one preserved raw block.</summary>
    public long MaxRawBlockBytes;
    /// <summary>The maximum number of bytes accepted for one embedded metadata profile.</summary>
    public long MaxProfileBytes;
    /// <summary>The maximum number of XML bytes accepted for XMP or XML-based metadata.</summary>
    public long MaxXmlBytes;
    /// <summary>The maximum number of bytes produced by one decompression operation.</summary>
    public long MaxDecompressedBytes;
    /// <summary>The maximum number of TIFF image file directories traversed.</summary>
    public int MaxIfdCount;
    /// <summary>The maximum depth followed through nested metadata containers.</summary>
    public int MaxNestingDepth;
    /// <summary>The maximum number of metadata tags decoded.</summary>
    public int MaxTagCount;
    /// <summary>The maximum number of frames or image items added to a document.</summary>
    public int MaxFrameCount;
    /// <summary>Indicates whether recognized metadata byte ranges are listed in <see cref="ImageMetadataDocument.RawBlocks"/>.</summary>
    public bool CaptureRawBlocks;
    /// <summary>Indicates whether captured raw blocks also retain their byte payloads.</summary>
    /// <remarks>Enabling this can substantially increase memory use. <see cref="MaxRawBlockBytes"/> still applies.</remarks>
    public bool CaptureRawBlockData;
    /// <summary>Indicates whether XMP packets are parsed and preserved.</summary>
    public bool ParseXmp;
    /// <summary>Indicates whether ICC profiles are parsed and preserved.</summary>
    public bool ParseIcc;
    /// <summary>Indicates whether camera maker-note payloads are preserved.</summary>
    public bool PreserveMakerNotes;
    /// <summary>Indicates whether selected vendor maker-note tags are decoded.</summary>
    /// <remarks>Unknown maker-note layouts remain available as raw data when preservation is enabled.</remarks>
    public bool DecodeMakerNotes;
    /// <summary>The amount of non-fatal parser detail recorded in diagnostics.</summary>
    public MetadataDiagnosticsVerbosity DiagnosticsVerbosity;

    /// <summary>Creates the balanced default configuration used by reader overloads without options.</summary>
    public ImageMetadataReaderOptions()
    {
        MaxFileBytes = 536870912;
        MaxSegmentBytes = 67108864;
        MaxRawBlockBytes = 16777216;
        MaxProfileBytes = 33554432;
        MaxXmlBytes = 16777216;
        MaxDecompressedBytes = 33554432;
        MaxIfdCount = 4096;
        MaxNestingDepth = 64;
        MaxTagCount = 65536;
        MaxFrameCount = 10000;
        CaptureRawBlocks = true;
        CaptureRawBlockData = false;
        ParseXmp = true;
        ParseIcc = true;
        PreserveMakerNotes = true;
        DecodeMakerNotes = false;
        DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Normal;
    }

    /// <summary>Creates a new options object containing the default settings.</summary>
    /// <returns>An independent default options instance.</returns>
    internal static ImageMetadataReaderOptions CreateDefault()
    {
        return new ImageMetadataReaderOptions();
    }

    /// <summary>Creates an options object with smaller limits for untrusted or resource-constrained workloads.</summary>
    /// <returns>An independent options instance configured with conservative limits.</returns>
    internal static ImageMetadataReaderOptions CreateConservative()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFileBytes = 134217728;
        options.MaxSegmentBytes = 8388608;
        options.MaxRawBlockBytes = 1048576;
        options.MaxProfileBytes = 4194304;
        options.MaxXmlBytes = 2097152;
        options.MaxDecompressedBytes = 4194304;
        options.MaxIfdCount = 512;
        options.MaxNestingDepth = 16;
        options.MaxTagCount = 8192;
        options.MaxFrameCount = 1024;
        options.CaptureRawBlocks = true;
        options.CaptureRawBlockData = false;
        options.ParseXmp = true;
        options.ParseIcc = true;
        options.PreserveMakerNotes = true;
        options.DecodeMakerNotes = false;
        options.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Normal;
        return options;
    }

    /// <summary>Creates an options object that records raw block ranges but omits their byte payloads.</summary>
    /// <returns>An independent metadata-focused options instance.</returns>
    internal static ImageMetadataReaderOptions CreateMetadataOnly()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlocks = true;
        options.CaptureRawBlockData = false;
        options.ParseXmp = true;
        options.ParseIcc = true;
        options.PreserveMakerNotes = true;
        options.DecodeMakerNotes = false;
        options.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Normal;
        return options;
    }
}
