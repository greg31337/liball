namespace tiff;

/// <summary>Reports metadata rewrite output and encoded image-data pass-through compatibility.</summary>
public class TiffMetadataRewriteResult
{
    /// <summary>Indicates whether the rewrite completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the destination image index, or <c>-1</c> when no image was added.</summary>
    public int DestinationImageIndex;
    /// <summary>Indicates whether source metadata was copied.</summary>
    public bool MetadataCopied;
    /// <summary>Indicates whether the requested unknown-metadata preservation policy was applied.</summary>
    public bool UnknownMetadataPreserved;
    /// <summary>Indicates whether encoded image-data pass-through was requested.</summary>
    public bool RawImageDataPassThroughRequested;
    /// <summary>Indicates whether the source and destination are compatible with encoded image-data pass-through.</summary>
    public bool CanPassThroughRawImageData;
    /// <summary>Indicates whether encoded strips or tiles were copied without decoding.</summary>
    public bool RawImageDataPassedThrough;
    /// <summary>Explains why encoded image data could not be passed through, or is empty when it could.</summary>
    public string RawImageDataPassThroughReason;
    /// <summary>Contains diagnostics produced by validation and copying.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty unsuccessful rewrite result.</summary>
    public TiffMetadataRewriteResult()
    {
        Success = false;
        DestinationImageIndex = -1;
        MetadataCopied = false;
        UnknownMetadataPreserved = false;
        RawImageDataPassThroughRequested = false;
        CanPassThroughRawImageData = false;
        RawImageDataPassedThrough = false;
        RawImageDataPassThroughReason = "";
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffMetadataRewriteResult Create(int destinationImageIndex, bool metadataCopied, bool unknownMetadataPreserved, bool passThroughRequested, bool canPassThrough, bool passedThrough, string reason, TiffDiagnostics diagnostics)
    {
        TiffMetadataRewriteResult result = new TiffMetadataRewriteResult();
        result.DestinationImageIndex = destinationImageIndex;
        result.MetadataCopied = metadataCopied;
        result.UnknownMetadataPreserved = unknownMetadataPreserved;
        result.RawImageDataPassThroughRequested = passThroughRequested;
        result.CanPassThroughRawImageData = canPassThrough;
        result.RawImageDataPassedThrough = passedThrough;
        result.RawImageDataPassThroughReason = reason == null ? "" : reason;
        result.Diagnostics = diagnostics;
        result.Success = !diagnostics.HasErrors() && destinationImageIndex >= 0;
        return result;
    }
}
