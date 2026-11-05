namespace svg;

/// <summary>Contains dimensions and format facts obtained without decoding PNG pixel data.</summary>
public class SvgPngProbeResult
{
    /// <summary>Whether a complete valid bounded PNG header was found.</summary>
    public bool Success;
    /// <summary>The stable probe outcome.</summary>
    public SvgPngProbeError Error;
    /// <summary>The positive PNG pixel width after success.</summary>
    public int Width;
    /// <summary>The positive PNG pixel height after success.</summary>
    public int Height;
    /// <summary>The PNG sample bit depth after success.</summary>
    public int BitDepth;
    /// <summary>The PNG color-type value after success.</summary>
    public int ColorType;
    /// <summary>The PNG interlace-method value after success.</summary>
    public int InterlaceMethod;
    /// <summary>Whether the PNG color type has an explicit alpha channel.</summary>
    public bool HasAlpha;
    /// <summary>The complete intrinsic dimensions after success, or <c>null</c>.</summary>
    public SvgIntrinsicSize? IntrinsicSize;

    internal SvgPngProbeResult()
    {
        Success = false;
        Error = SvgPngProbeError.InvalidArgument;
        Width = 0;
        Height = 0;
        BitDepth = 0;
        ColorType = 0;
        InterlaceMethod = 0;
        HasAlpha = false;
        IntrinsicSize = null;
    }
}

/// <summary>Contains one bounded PNG decode and its premultiplied sRGB pixels.</summary>
public class SvgPngDecodeResult
{
    /// <summary>Whether complete pixels were decoded.</summary>
    public bool Success;
    /// <summary>The stable decode outcome.</summary>
    public SvgPngDecodeError Error;
    /// <summary>The decoded premultiplied sRGB pixels, or <c>null</c>.</summary>
    public SvgPixelBuffer? Pixels;
    /// <summary>The decoded pixel width.</summary>
    public int Width;
    /// <summary>The decoded pixel height.</summary>
    public int Height;
    /// <summary>The source PNG sample bit depth.</summary>
    public int BitDepth;
    /// <summary>The source PNG color type.</summary>
    public int ColorType;
    /// <summary>The source PNG interlace method.</summary>
    public int InterlaceMethod;
    /// <summary>The number of validated PNG chunks.</summary>
    public int ChunksRead;
    /// <summary>The number of inflated filter and sample bytes.</summary>
    public long BytesInflated;

    internal SvgPngDecodeResult()
    {
        Success = false;
        Error = SvgPngDecodeError.InvalidArgument;
        Pixels = null;
        Width = 0;
        Height = 0;
        BitDepth = 0;
        ColorType = 0;
        InterlaceMethod = 0;
        ChunksRead = 0;
        BytesInflated = 0;
    }
}

/// <summary>Configures one bounded deterministic PNG encoding operation.</summary>
public class SvgPngEncodeOptions
{
    /// <summary>The raster dimensions, pixels, and intermediate-byte limits.</summary>
    public SvgRasterLimits RasterLimits;
    /// <summary>The adaptive or fixed scanline filter selection.</summary>
    public SvgPngFilterMode FilterMode;
    /// <summary>The zlib compression effort.</summary>
    public SvgPngCompressionMode CompressionMode;
    /// <summary>Whether to write an sRGB color-space chunk.</summary>
    public bool IncludeSrgbChunk;
    /// <summary>The horizontal physical pixel density in pixels per meter, or zero to omit physical metadata.</summary>
    public int PixelsPerMeterX;
    /// <summary>The vertical physical pixel density in pixels per meter, or zero to omit physical metadata.</summary>
    public int PixelsPerMeterY;
    /// <summary>The maximum complete encoded PNG byte count.</summary>
    public long MaximumEncodedBytes;

    /// <summary>Creates bounded adaptive-filter, balanced-compression, sRGB defaults.</summary>
    public SvgPngEncodeOptions()
    {
        RasterLimits = new SvgRasterLimits();
        FilterMode = SvgPngFilterMode.Adaptive;
        CompressionMode = SvgPngCompressionMode.Balanced;
        IncludeSrgbChunk = true;
        PixelsPerMeterX = 0;
        PixelsPerMeterY = 0;
        MaximumEncodedBytes = 400000000;
    }
}

/// <summary>Contains one encoded PNG, stream write, or data URL result.</summary>
public class SvgPngEncodeResult
{
    /// <summary>Whether encoding and any requested destination write completed.</summary>
    public bool Success;
    /// <summary>The stable encoding outcome.</summary>
    public SvgPngEncodeError Error;
    /// <summary>The complete encoded PNG bytes, or an empty array after failure.</summary>
    public byte[] Bytes;
    /// <summary>The number of complete PNG bytes encoded or written.</summary>
    public long BytesWritten;
    /// <summary>The base64 PNG data URL, or an empty string when data URL output was not requested.</summary>
    public string DataUrl;

    internal SvgPngEncodeResult()
    {
        Success = false;
        Error = SvgPngEncodeError.InvalidArgument;
        Bytes = System.Array.Empty<byte>();
        BytesWritten = 0;
        DataUrl = "";
    }
}

/// <summary>Contains optional image facts supplied by a resource provider.</summary>
public class SvgImageMetadata
{
    /// <summary>The image intrinsic dimensions, or <c>null</c> when unknown.</summary>
    public SvgIntrinsicSize? IntrinsicSize;
    /// <summary>Whether the callback also made decoded image content available to the host.</summary>
    public bool DecodedContentAvailable;

    /// <summary>Creates image metadata with no known dimensions or decoded content.</summary>
    public SvgImageMetadata()
    {
        IntrinsicSize = null;
        DecodedContentAvailable = false;
    }
}

/// <summary>Configures one callback-mediated image resource resolution.</summary>
public class SvgImageResolveOptions
{
    /// <summary>The inherited resource provider, policy, cancellation, and limits.</summary>
    public SvgLoadOptions LoadOptions;
    /// <summary>The cache and total-resource accounting shared by the containing operation.</summary>
    public SvgNestedDocumentContext Context;

    /// <summary>Creates secure image-resolution defaults.</summary>
    public SvgImageResolveOptions()
    {
        LoadOptions = new SvgLoadOptions();
        Context = new SvgNestedDocumentContext(LoadOptions.Limits.Resources);
    }
}

/// <summary>Contains acquired image bytes and metadata suitable for deferred decoding.</summary>
public class SvgImageResolveResult
{
    /// <summary>Whether image acquisition and metadata resolution succeeded.</summary>
    public bool Success;
    /// <summary>The source reference resolution result.</summary>
    public SvgReferenceResult Reference;
    /// <summary>The caller-mediated acquisition, or <c>null</c> for same-document content.</summary>
    public SvgResourceAcquisitionResult? Acquisition;
    /// <summary>The pre-acquisition resource-policy decision, or <c>null</c>.</summary>
    public SvgResourcePolicyDecision? InitialPolicyDecision;
    /// <summary>The post-acquisition media and redirect policy decision, or <c>null</c>.</summary>
    public SvgResourcePolicyDecision? FinalPolicyDecision;
    /// <summary>The intrinsic dimensions from callback metadata or PNG probing, or <c>null</c>.</summary>
    public SvgIntrinsicSize? IntrinsicSize;
    /// <summary>Whether the callback supplied the intrinsic dimensions.</summary>
    public bool UsedCallbackMetadata;
    /// <summary>Whether PNG header probing supplied the intrinsic dimensions.</summary>
    public bool UsedPngHeader;
    /// <summary>Whether decoded image content is already available to the host.</summary>
    public bool DecodedContentAvailable;
    /// <summary>Ordered policy, acquisition, and probe diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgImageResolveResult(SvgReferenceResult reference)
    {
        Success = false;
        Reference = reference;
        Acquisition = null;
        InitialPolicyDecision = null;
        FinalPolicyDecision = null;
        IntrinsicSize = null;
        UsedCallbackMetadata = false;
        UsedPngHeader = false;
        DecodedContentAvailable = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}
