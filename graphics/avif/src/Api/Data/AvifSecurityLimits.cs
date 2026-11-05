namespace avif;

/// <summary>
/// Defines hard resource bounds applied while parsing, decoding, rendering, encoding, and rewriting.
/// </summary>
/// <remarks>
/// A zero or negative value is not interpreted as unlimited. Operations validate configured values
/// before use and return <c>LimitExceeded</c> when valid input exceeds a bound.
/// </remarks>
public class AvifSecurityLimits
{
    /// <summary>The default maximum complete input size in bytes.</summary>
    public const long DefaultMaxSourceBytes = 1073741824;

    /// <summary>The default maximum nesting depth of ISO boxes.</summary>
    public const int DefaultMaxBoxDepth = 32;

    /// <summary>The default maximum number of ISO boxes.</summary>
    public const int DefaultMaxBoxCount = 100000;

    /// <summary>The default maximum number of HEIF items.</summary>
    public const int DefaultMaxItemCount = 65536;

    /// <summary>The default maximum number of item properties.</summary>
    public const int DefaultMaxPropertyCount = 65536;

    /// <summary>The default maximum number of item and track references.</summary>
    public const int DefaultMaxReferenceCount = 262144;

    /// <summary>The default maximum number of media tracks.</summary>
    public const int DefaultMaxTrackCount = 1024;

    /// <summary>The default maximum number of media samples.</summary>
    public const int DefaultMaxSampleCount = 2592000;

    /// <summary>The default maximum number of decoded or encoded frames.</summary>
    public const int DefaultMaxFrameCount = 2592000;

    /// <summary>The default maximum coded or rendered width.</summary>
    public const int DefaultMaxWidth = 32768;

    /// <summary>The default maximum coded or rendered height.</summary>
    public const int DefaultMaxHeight = 32768;

    /// <summary>The default maximum number of pixels in one image.</summary>
    public const long DefaultMaxPixels = 268435456;

    /// <summary>The default maximum number of AV1 tiles in one frame.</summary>
    public const int DefaultMaxTileCount = 512;

    /// <summary>The default maximum combined metadata payload size in bytes.</summary>
    public const long DefaultMaxMetadataBytes = 67108864;

    /// <summary>The default maximum ICC profile size in bytes.</summary>
    public const long DefaultMaxIccBytes = 16777216;

    /// <summary>The default maximum Exif payload size in bytes.</summary>
    public const long DefaultMaxExifBytes = 67108864;

    /// <summary>The default maximum XMP packet size in bytes.</summary>
    public const long DefaultMaxXmpBytes = 67108864;

    /// <summary>The default maximum recursion depth outside ISO box nesting.</summary>
    public const int DefaultMaxRecursionDepth = 64;

    /// <summary>The default maximum item dependency depth.</summary>
    public const int DefaultMaxDependencyDepth = 64;

    /// <summary>The default maximum retained decoded sample storage in bytes.</summary>
    public const long DefaultMaxDecodedBytes = 1073741824;

    /// <summary>The default maximum temporary working storage in bytes.</summary>
    public const long DefaultMaxTemporaryBytes = 536870912;

    /// <summary>The default maximum number of workers used concurrently.</summary>
    public const int DefaultMaxWorkerCount = 1;

    /// <summary>The maximum complete input size in bytes.</summary>
    public long MaxSourceBytes;

    /// <summary>The maximum nesting depth of ISO boxes.</summary>
    public int MaxBoxDepth;

    /// <summary>The maximum number of ISO boxes.</summary>
    public int MaxBoxCount;

    /// <summary>The maximum number of HEIF items.</summary>
    public int MaxItemCount;

    /// <summary>The maximum number of item properties.</summary>
    public int MaxPropertyCount;

    /// <summary>The maximum number of item and track references.</summary>
    public int MaxReferenceCount;

    /// <summary>The maximum number of media tracks.</summary>
    public int MaxTrackCount;

    /// <summary>The maximum number of media samples.</summary>
    public int MaxSampleCount;

    /// <summary>The maximum number of decoded or encoded frames.</summary>
    public int MaxFrameCount;

    /// <summary>The maximum coded or rendered width.</summary>
    public int MaxWidth;

    /// <summary>The maximum coded or rendered height.</summary>
    public int MaxHeight;

    /// <summary>The maximum number of pixels in one image.</summary>
    public long MaxPixels;

    /// <summary>The maximum number of AV1 tiles in one frame.</summary>
    public int MaxTileCount;

    /// <summary>The maximum combined metadata payload size in bytes.</summary>
    public long MaxMetadataBytes;

    /// <summary>The maximum ICC profile size in bytes.</summary>
    public long MaxIccBytes;

    /// <summary>The maximum Exif payload size in bytes.</summary>
    public long MaxExifBytes;

    /// <summary>The maximum XMP packet size in bytes.</summary>
    public long MaxXmpBytes;

    /// <summary>The maximum recursion depth outside ISO box nesting.</summary>
    public int MaxRecursionDepth;

    /// <summary>The maximum item dependency depth.</summary>
    public int MaxDependencyDepth;

    /// <summary>The maximum retained decoded sample storage in bytes.</summary>
    public long MaxDecodedBytes;

    /// <summary>The maximum temporary working storage in bytes.</summary>
    public long MaxTemporaryBytes;

    /// <summary>The maximum number of workers used concurrently.</summary>
    public int MaxWorkerCount;

    /// <summary>
    /// Creates a resource-limit set with conservative bounded defaults.
    /// </summary>
    public AvifSecurityLimits()
    {
        MaxSourceBytes = DefaultMaxSourceBytes;
        MaxBoxDepth = DefaultMaxBoxDepth;
        MaxBoxCount = DefaultMaxBoxCount;
        MaxItemCount = DefaultMaxItemCount;
        MaxPropertyCount = DefaultMaxPropertyCount;
        MaxReferenceCount = DefaultMaxReferenceCount;
        MaxTrackCount = DefaultMaxTrackCount;
        MaxSampleCount = DefaultMaxSampleCount;
        MaxFrameCount = DefaultMaxFrameCount;
        MaxWidth = DefaultMaxWidth;
        MaxHeight = DefaultMaxHeight;
        MaxPixels = DefaultMaxPixels;
        MaxTileCount = DefaultMaxTileCount;
        MaxMetadataBytes = DefaultMaxMetadataBytes;
        MaxIccBytes = DefaultMaxIccBytes;
        MaxExifBytes = DefaultMaxExifBytes;
        MaxXmpBytes = DefaultMaxXmpBytes;
        MaxRecursionDepth = DefaultMaxRecursionDepth;
        MaxDependencyDepth = DefaultMaxDependencyDepth;
        MaxDecodedBytes = DefaultMaxDecodedBytes;
        MaxTemporaryBytes = DefaultMaxTemporaryBytes;
        MaxWorkerCount = DefaultMaxWorkerCount;
    }
}
