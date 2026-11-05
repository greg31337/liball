using System;
using System.Collections.Generic;

namespace webp;

/// <summary>Describes a format, validation, or processing issue discovered while handling WebP data.</summary>
public class WebpDiagnostic
{
    /// <summary>The severity that determines whether the issue prevents a valid result.</summary>
    public WebpDiagnosticSeverity Severity;
    /// <summary>The zero-based byte offset associated with the issue, or <c>-1</c> when no precise offset is available.</summary>
    public long Offset;
    /// <summary>The four-character RIFF chunk name associated with the issue, or an empty string for a container-wide issue.</summary>
    public string FourCc;
    /// <summary>A plain-English description of the issue.</summary>
    public string Message;

    /// <summary>Creates a diagnostic with complete source context.</summary>
    /// <param name="severity">The seriousness of the issue.</param>
    /// <param name="offset">The zero-based byte offset, or <c>-1</c> when it is not known.</param>
    /// <param name="fourCc">The related four-character chunk name, or an empty string.</param>
    /// <param name="message">The developer-facing explanation of the issue.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>fourCc</c> or <c>message</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>severity</c> is invalid or <c>offset</c> is less than <c>-1</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when a non-empty <c>fourCc</c> is not exactly four bytes.</exception>
    public WebpDiagnostic(WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        ArgumentNullException.ThrowIfNull(fourCc);
        ArgumentNullException.ThrowIfNull(message);
        if (severity != WebpDiagnosticSeverity.Info
            && severity != WebpDiagnosticSeverity.Warning
            && severity != WebpDiagnosticSeverity.Error)
        {
            throw new ArgumentOutOfRangeException("severity");
        }

        if (offset < -1)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (fourCc != "")
        {
            WebpUnknownChunkMux.ValidateFourCc(fourCc, "fourCc");
        }

        Severity = severity;
        Offset = offset;
        FourCc = fourCc;
        Message = message;
    }
}

/// <summary>Contains the outcome and diagnostics produced by container validation.</summary>
public class WebpValidationResult
{
    /// <summary>Indicates whether validation completed without error-severity diagnostics.</summary>
    public bool IsValid;
    /// <summary>The warnings and errors found while validating the input.</summary>
    public List<WebpDiagnostic> Diagnostics;

    /// <summary>Creates an initially valid result with an empty diagnostic collection.</summary>
    public WebpValidationResult()
    {
        IsValid = true;
        Diagnostics = new List<WebpDiagnostic>();
    }

    /// <summary>Checks the current diagnostic collection for at least one error.</summary>
    /// <returns><c>true</c> when an error-severity diagnostic is present; otherwise <c>false</c>.</returns>
    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].Severity == WebpDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

/// <summary>Contains the output and diagnostics from a container normalization attempt.</summary>
public class WebpNormalizationResult
{
    /// <summary>The normalized WebP bytes, or an independent copy of the input when normalization was unsafe.</summary>
    public byte[] Data;
    /// <summary>Indicates that every requested structural repair was completed safely.</summary>
    public bool IsNormalized;
    /// <summary>Indicates that the normalized bytes differ from the supplied input.</summary>
    public bool WasChanged;
    /// <summary>Issues that prevented a safe repair.</summary>
    public List<WebpDiagnostic> Diagnostics;

    /// <summary>Creates an incomplete result with empty data and diagnostics.</summary>
    public WebpNormalizationResult()
    {
        Data = Array.Empty<byte>();
        IsNormalized = false;
        WasChanged = false;
        Diagnostics = new List<WebpDiagnostic>();
    }
}

/// <summary>Describes the location, size, padding, and recognition state of one top-level RIFF chunk.</summary>
public class WebpChunkInfo
{
    /// <summary>The four-character RIFF chunk name.</summary>
    public string FourCc;
    /// <summary>The zero-based offset of the chunk header in the WebP file.</summary>
    public long Offset;
    /// <summary>The zero-based offset of the first payload byte.</summary>
    public long PayloadOffset;
    /// <summary>The payload length recorded in the RIFF chunk header, excluding padding.</summary>
    public uint PayloadLength;
    /// <summary>Indicates whether the odd-sized payload requires a RIFF padding byte.</summary>
    public bool HasPadding;
    /// <summary>The zero-based padding-byte offset, or the payload end when no padding is required.</summary>
    public long PaddingOffset;
    /// <summary>The first byte offset after the payload and any RIFF padding.</summary>
    public long PaddedEndOffset;
    /// <summary>Indicates whether a required padding byte exists and is zero.</summary>
    public bool IsPaddingValid;
    /// <summary>Indicates whether the library recognizes the chunk name as part of the WebP format.</summary>
    public bool IsKnown;

    /// <summary>Creates an empty chunk description with valid padding state.</summary>
    public WebpChunkInfo()
    {
        FourCc = "";
        Offset = 0;
        PayloadOffset = 0;
        PayloadLength = 0;
        HasPadding = false;
        PaddingOffset = 0;
        PaddedEndOffset = 0;
        IsPaddingValid = true;
        IsKnown = false;
    }
}

/// <summary>Stores an unrecognized RIFF chunk so it can be inspected or preserved.</summary>
public class WebpUnknownChunk
{
    /// <summary>The four-character RIFF chunk name.</summary>
    public string FourCc;
    /// <summary>A caller-owned copy of the chunk payload without its RIFF header or padding byte.</summary>
    public byte[] Data;

    /// <summary>Creates an unknown chunk and copies its payload so later changes to the input array do not affect it.</summary>
    /// <param name="fourCc">The four-character RIFF chunk name.</param>
    /// <param name="data">The payload bytes to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>fourCc</c> or <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>fourCc</c> is not a four-byte unrecognized WebP chunk name.</exception>
    public WebpUnknownChunk(string fourCc, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(fourCc);
        ArgumentNullException.ThrowIfNull(data);
        WebpUnknownChunkMux.ValidateFourCc(fourCc, "fourCc");
        if (WebpUnknownChunkMux.IsKnownWebpChunk(fourCc))
        {
            throw new ArgumentException("unknown chunks must not use known WebP FourCC values", "fourCc");
        }

        FourCc = fourCc;
        Data = CopyBytes(data);
    }

    internal static byte[] CopyBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] copy = new byte[data.Length];
        Array.Copy(data, copy, data.Length);
        return copy;
    }
}

/// <summary>Holds raw WebP metadata payloads without interpreting their internal formats.</summary>
/// <remarks>Each field contains only the chunk payload. A null field means that metadata kind is absent.</remarks>
public class WebpMetadata
{
    /// <summary>The raw ICC color-profile payload, or null when absent.</summary>
    public byte[]? IccProfile;
    /// <summary>The raw Exif payload, or null when absent.</summary>
    public byte[]? Exif;
    /// <summary>The raw XMP payload, or null when absent.</summary>
    public byte[]? Xmp;

    /// <summary>Creates a metadata container with all payloads absent.</summary>
    public WebpMetadata()
    {
        IccProfile = null;
        Exif = null;
        Xmp = null;
    }
}

/// <summary>Represents an 8-bit straight-alpha RGBA image stored row by row.</summary>
/// <remarks>Each pixel occupies four bytes in red, green, blue, alpha order. Before an operation consumes this model, dimensions must be positive, <c>Stride</c> must be at least <c>Width * 4</c>, and <c>Rgba</c> must contain at least <c>Stride * Height</c> bytes.</remarks>
public class WebpPixelBuffer
{
    /// <summary>The number of pixels in each row.</summary>
    public int Width;
    /// <summary>The number of pixel rows.</summary>
    public int Height;
    /// <summary>The number of bytes from the start of one row to the start of the next.</summary>
    public int Stride;
    /// <summary>The row-major RGBA bytes, including any unused bytes at the end of each row.</summary>
    public byte[] Rgba;

    /// <summary>Creates an empty pixel buffer; set a valid shape and byte array before encoding it.</summary>
    public WebpPixelBuffer()
    {
        Width = 0;
        Height = 0;
        Stride = 0;
        Rgba = Array.Empty<byte>();
    }
}

/// <summary>Represents an 8-bit premultiplied-alpha RGBA image stored row by row.</summary>
/// <remarks>Each color channel has already been multiplied by its pixel's alpha. <c>Stride</c> may be larger than <c>Width * 4</c>.</remarks>
public class WebpPremultipliedPixelBuffer
{
    /// <summary>The number of pixels in each row.</summary>
    public int Width;
    /// <summary>The number of pixel rows.</summary>
    public int Height;
    /// <summary>The number of bytes from the start of one row to the start of the next.</summary>
    public int Stride;
    /// <summary>The row-major premultiplied RGBA bytes, including any unused bytes at the end of each row.</summary>
    public byte[] Rgba;

    /// <summary>Creates an empty premultiplied pixel buffer; set a valid shape and byte array before converting it.</summary>
    public WebpPremultipliedPixelBuffer()
    {
        Width = 0;
        Height = 0;
        Stride = 0;
        Rgba = Array.Empty<byte>();
    }
}

/// <summary>Represents one still image or one rectangular frame in a WebP animation.</summary>
/// <remarks>Decoded frames use <c>Pixels</c>. Frames extracted without decoding can instead retain <c>EncodedBitstream</c> and an optional lossy <c>AlphaData</c> payload. A frame supplied for encoding must have one of those image sources, valid enum values, matching pixel dimensions, and nonnegative geometry. Animation positions must be even and the rectangle must fit the canvas.</remarks>
public class WebpFrame
{
    /// <summary>The horizontal frame offset on the animation canvas, in pixels.</summary>
    public int X;
    /// <summary>The vertical frame offset on the animation canvas, in pixels.</summary>
    public int Y;
    /// <summary>The frame rectangle width in pixels.</summary>
    public int Width;
    /// <summary>The frame rectangle height in pixels.</summary>
    public int Height;
    /// <summary>How long the animation frame is displayed, in milliseconds.</summary>
    public int DurationMilliseconds;
    /// <summary>How this frame combines with pixels already on the animation canvas.</summary>
    public WebpFrameBlendMode BlendMode;
    /// <summary>How the frame rectangle is prepared before the following frame is drawn.</summary>
    public WebpFrameDisposeMode DisposeMode;
    /// <summary>The compression format of <c>EncodedBitstream</c>, or <c>Unknown</c> when pixels still need encoding.</summary>
    public WebpBitstreamKind BitstreamKind;
    /// <summary>The decoded RGBA pixels, or null when pixels were not decoded or a preserved bitstream should be used.</summary>
    public WebpPixelBuffer? Pixels;
    /// <summary>The raw VP8 or VP8L payload without a RIFF chunk header, or null when no encoded payload is retained.</summary>
    public byte[]? EncodedBitstream;
    /// <summary>The raw ALPH chunk payload for a lossy frame, or null when no separate alpha data is present.</summary>
    public byte[]? AlphaData;
    /// <summary>Unrecognized subchunks retained inside this animation frame.</summary>
    public List<WebpUnknownChunk> UnknownChunks;
    /// <summary>Optional compression and alpha settings for encoding this frame's pixels, or null to use the global encode options.</summary>
    public WebpFrameEncodeOptions? EncodeOptions;

    /// <summary>Creates an empty frame using alpha blending and no disposal.</summary>
    public WebpFrame()
    {
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
        DurationMilliseconds = 0;
        BlendMode = WebpFrameBlendMode.AlphaBlend;
        DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        BitstreamKind = WebpBitstreamKind.Unknown;
        Pixels = null;
        EncodedBitstream = null;
        AlphaData = null;
        UnknownChunks = new List<WebpUnknownChunk>();
        EncodeOptions = null;
    }
}

/// <summary>Overrides compression and alpha settings for one pixel-backed animation frame.</summary>
/// <remarks>Settings are ignored for frames that already contain an encoded bitstream. Encoder controls not represented here continue to come from the global <see cref="WebpEncodeOptions"/>.</remarks>
public class WebpFrameEncodeOptions
{
    /// <summary>The automatic, lossy, lossless, or near-lossless strategy for this frame.</summary>
    public WebpEncodeMode Mode;
    /// <summary>The lossy quality from <c>0</c> through <c>100</c>.</summary>
    public int Quality;
    /// <summary>The near-lossless preprocessing quality from <c>0</c> through <c>100</c>.</summary>
    public int NearLosslessQuality;
    /// <summary>When preserving lossy alpha, keeps RGB values beneath fully transparent pixels.</summary>
    public bool ExactTransparentRgb;
    /// <summary>When <c>true</c>, preserves the separate alpha plane exactly.</summary>
    public bool LosslessAlpha;
    /// <summary>The separate alpha-plane quality from <c>0</c> through <c>100</c>.</summary>
    public int AlphaQuality;
    /// <summary>The storage method for a lossy frame's separate alpha plane.</summary>
    public WebpAlphaCompressionMethod AlphaCompression;
    /// <summary>The filter-search mode for a lossy frame's separate alpha plane.</summary>
    public WebpAlphaFilterMode AlphaFilter;
    /// <summary>Whether lossy encoding preserves, discards, or blends this frame's alpha.</summary>
    public WebpLossyAlphaMode LossyAlphaMode;
    /// <summary>The opaque background used when <c>LossyAlphaMode</c> is <c>BlendWithBackground</c>, packed as blue, green, red, alpha bytes.</summary>
    public uint AlphaBackgroundColorBgra;

    /// <summary>Creates automatic per-frame settings using the library's general-purpose quality and alpha defaults.</summary>
    public WebpFrameEncodeOptions()
    {
        Mode = WebpConstants.DefaultEncodeMode;
        Quality = WebpConstants.DefaultQuality;
        NearLosslessQuality = WebpConstants.DefaultNearLosslessQuality;
        ExactTransparentRgb = false;
        LosslessAlpha = true;
        AlphaQuality = WebpConstants.DefaultAlphaQuality;
        AlphaCompression = WebpConstants.DefaultAlphaCompression;
        AlphaFilter = WebpConstants.DefaultAlphaFilter;
        LossyAlphaMode = WebpConstants.DefaultLossyAlphaMode;
        AlphaBackgroundColorBgra = WebpConstants.DefaultAlphaBackgroundColorBgra;
    }
}

/// <summary>Represents a complete still or animated WebP image, including pixels, metadata, and preserved chunks.</summary>
/// <remarks>Encoding requires initialized metadata and chunk collections and at least one valid frame. Animated models require positive canvas dimensions. A zero width and height on a still model lets the encoder derive its canvas from the frame; only one zero dimension is invalid.</remarks>
public class WebpImage
{
    /// <summary>The full canvas width in pixels.</summary>
    public int CanvasWidth;
    /// <summary>The full canvas height in pixels.</summary>
    public int CanvasHeight;
    /// <summary>Indicates that the image declares or contains non-opaque alpha information.</summary>
    public bool HasAlpha;
    /// <summary>Indicates that the model should be written or was read as an animation.</summary>
    public bool HasAnimation;
    /// <summary>The number of animation repetitions, where <c>0</c> means repeat forever.</summary>
    public ushort LoopCount;
    /// <summary>The animation background color packed as blue, green, red, alpha bytes in a 32-bit value.</summary>
    public uint BackgroundColorBgra;
    /// <summary>The raw ICC, Exif, and XMP payloads associated with the image.</summary>
    public WebpMetadata Metadata;
    /// <summary>The still-image frame or ordered animation frames.</summary>
    public List<WebpFrame> Frames;
    /// <summary>Unrecognized top-level RIFF chunks retained with the image.</summary>
    public List<WebpUnknownChunk> UnknownChunks;
    /// <summary>The source rectangle and output dimensions applied while decoding this image.</summary>
    public WebpDecodeTransform Transform;

    /// <summary>Creates an empty still-image model with initialized metadata and frame collections.</summary>
    public WebpImage()
    {
        CanvasWidth = 0;
        CanvasHeight = 0;
        HasAlpha = false;
        HasAnimation = false;
        LoopCount = WebpConstants.AnimDefaultLoopCount;
        BackgroundColorBgra = WebpConstants.AnimDefaultBackgroundColorBgra;
        Metadata = new WebpMetadata();
        Frames = new List<WebpFrame>();
        UnknownChunks = new List<WebpUnknownChunk>();
        Transform = new WebpDecodeTransform();
    }
}

/// <summary>Describes the source canvas region and output dimensions used for decoded pixels.</summary>
public class WebpDecodeTransform
{
    /// <summary>The original WebP canvas width before cropping or scaling.</summary>
    public int SourceCanvasWidth;
    /// <summary>The original WebP canvas height before cropping or scaling.</summary>
    public int SourceCanvasHeight;
    /// <summary>The horizontal offset of the decoded source rectangle on the original canvas.</summary>
    public int SourceX;
    /// <summary>The vertical offset of the decoded source rectangle on the original canvas.</summary>
    public int SourceY;
    /// <summary>The width of the decoded source rectangle before scaling.</summary>
    public int SourceWidth;
    /// <summary>The height of the decoded source rectangle before scaling.</summary>
    public int SourceHeight;
    /// <summary>The final decoded canvas width after scaling.</summary>
    public int OutputWidth;
    /// <summary>The final decoded canvas height after scaling.</summary>
    public int OutputHeight;
    /// <summary>The aspect-ratio behavior used to calculate the output dimensions.</summary>
    public WebpAspectRatioMode AspectRatioMode;

    /// <summary>Creates an empty transform description using aspect-ratio preservation.</summary>
    public WebpDecodeTransform()
    {
        SourceCanvasWidth = 0;
        SourceCanvasHeight = 0;
        SourceX = 0;
        SourceY = 0;
        SourceWidth = 0;
        SourceHeight = 0;
        OutputWidth = 0;
        OutputHeight = 0;
        AspectRatioMode = WebpAspectRatioMode.Preserve;
    }
}

/// <summary>Contains container and frame metadata read without decoding pixel buffers.</summary>
public class WebpInfo
{
    /// <summary>The image canvas width in pixels, or <c>0</c> when it could not be read.</summary>
    public int Width;
    /// <summary>The image canvas height in pixels, or <c>0</c> when it could not be read.</summary>
    public int Height;
    /// <summary>Indicates that alpha is declared or present.</summary>
    public bool HasAlpha;
    /// <summary>Indicates that animation chunks or animation flags are present.</summary>
    public bool HasAnimation;
    /// <summary>Indicates that an ICC profile chunk is present.</summary>
    public bool HasIccProfile;
    /// <summary>Indicates that an Exif chunk is present.</summary>
    public bool HasExif;
    /// <summary>Indicates that an XMP chunk is present.</summary>
    public bool HasXmp;
    /// <summary>The animation repetition count from <c>ANIM</c>, where <c>0</c> means repeat forever.</summary>
    public ushort LoopCount;
    /// <summary>The animation background color from <c>ANIM</c>, packed as blue, green, red, alpha bytes.</summary>
    public uint BackgroundColorBgra;
    /// <summary>The recognized feature bits declared by the <c>VP8X</c> header, independent of whether matching chunks are present.</summary>
    public WebpVp8XFeatureFlags DeclaredFeatureFlags;
    /// <summary>The reserved bits that were set in the <c>VP8X</c> feature byte.</summary>
    public byte ReservedFeatureBits;
    /// <summary>Indicates that at least one reserved bit was set in the <c>VP8X</c> feature byte.</summary>
    public bool HasReservedFeatureBits;
    /// <summary>Indicates that at least one of the three reserved <c>VP8X</c> bytes was non-zero.</summary>
    public bool HasReservedBytes;
    /// <summary>The detected basic or extended container layout.</summary>
    public WebpContainerKind ContainerKind;
    /// <summary>The detected image bitstream format; mixed animation formats are reported as <c>Unknown</c>.</summary>
    public WebpBitstreamKind BitstreamKind;
    /// <summary>The top-level RIFF chunks in source order.</summary>
    public List<WebpChunkInfo> Chunks;
    /// <summary>Animation frame geometry and timing without pixels or encoded payloads.</summary>
    public List<WebpFrame> Frames;
    /// <summary>Format issues encountered while reading the information.</summary>
    public List<WebpDiagnostic> Diagnostics;

    /// <summary>Creates an empty, unidentified information result.</summary>
    public WebpInfo()
    {
        Width = 0;
        Height = 0;
        HasAlpha = false;
        HasAnimation = false;
        HasIccProfile = false;
        HasExif = false;
        HasXmp = false;
        LoopCount = WebpConstants.AnimDefaultLoopCount;
        BackgroundColorBgra = WebpConstants.AnimDefaultBackgroundColorBgra;
        DeclaredFeatureFlags = WebpVp8XFeatureFlags.None;
        ReservedFeatureBits = 0;
        HasReservedFeatureBits = false;
        HasReservedBytes = false;
        ContainerKind = WebpContainerKind.Unknown;
        BitstreamKind = WebpBitstreamKind.Unknown;
        Chunks = new List<WebpChunkInfo>();
        Frames = new List<WebpFrame>();
        Diagnostics = new List<WebpDiagnostic>();
    }
}

/// <summary>Sets allocation and complexity ceilings used to reject unexpectedly expensive input or output.</summary>
/// <remarks>Limits are checked before the relevant buffers or collections are created. Lower them when processing untrusted data under a tighter memory or work budget.</remarks>
public class WebpSecurityLimits
{
    /// <summary>The largest accepted canvas width in pixels.</summary>
    public int MaxCanvasWidth;
    /// <summary>The largest accepted canvas height in pixels.</summary>
    public int MaxCanvasHeight;
    /// <summary>The largest accepted width-times-height value for one canvas or frame.</summary>
    public long MaxCanvasPixels;
    /// <summary>The largest combined pixel count accepted across all animation frames.</summary>
    public long MaxTotalPixels;
    /// <summary>The largest accepted or produced RIFF file size in bytes.</summary>
    public long MaxRiffSize;
    /// <summary>The largest number of top-level chunks that may be processed.</summary>
    public int MaxChunkCount;
    /// <summary>The largest number of animation frames that may be processed.</summary>
    public int MaxFrameCount;
    /// <summary>The largest recursion depth permitted while decoding nested VP8L image streams.</summary>
    public int MaxRecursionDepth;
    /// <summary>The largest combined number of ICC, Exif, and XMP payload bytes that may be retained or written.</summary>
    public long MaxMetadataBytes;
    /// <summary>The largest combined number of unrecognized chunk payload bytes that may be retained or written.</summary>
    public long MaxUnknownChunkBytes;
    /// <summary>The largest combined byte allocation permitted for retained payloads or one operation's output buffers.</summary>
    public long MaxAggregateAllocationBytes;

    /// <summary>Creates limits using the library's general-purpose safety defaults.</summary>
    public WebpSecurityLimits()
    {
        MaxCanvasWidth = WebpConstants.Vp8XMaxCanvasDimension;
        MaxCanvasHeight = WebpConstants.Vp8XMaxCanvasDimension;
        MaxCanvasPixels = WebpConstants.DefaultMaxCanvasPixels;
        MaxTotalPixels = WebpConstants.DefaultMaxTotalPixels;
        MaxRiffSize = WebpConstants.MaxRiffFileSize;
        MaxChunkCount = WebpConstants.DefaultMaxChunkCount;
        MaxFrameCount = WebpConstants.DefaultMaxFrameCount;
        MaxRecursionDepth = WebpConstants.DefaultMaxRecursionDepth;
        MaxMetadataBytes = WebpConstants.DefaultMaxMetadataBytes;
        MaxUnknownChunkBytes = WebpConstants.DefaultMaxUnknownChunkBytes;
        MaxAggregateAllocationBytes = WebpConstants.DefaultMaxAggregateAllocationBytes;
    }
}

/// <summary>Controls validation, frame selection, retained data, and safety limits during decoding.</summary>
/// <remarks>Invalid enum values, ranges, and unavailable frame indexes produce <see cref="ArgumentOutOfRangeException"/>. Mutually incompatible requests, including pixel transforms during identification and explicit frame selection during composited animation decoding, produce <see cref="ArgumentException"/>.</remarks>
public class WebpDecodeOptions
{
    /// <summary>Whether recoverable format deviations produce warnings or errors.</summary>
    public WebpStrictness Strictness;
    /// <summary>Whether unrecognized RIFF chunks are retained in the decoded model.</summary>
    public WebpUnknownChunkPolicy UnknownChunkPolicy;
    /// <summary>Whether animated output contains frame rectangles or composited full-canvas snapshots.</summary>
    public WebpAnimationDecodeMode AnimationDecodeMode;
    /// <summary>When <c>true</c>, parses image and frame information but leaves pixel buffers null.</summary>
    public bool IdentifyOnly;
    /// <summary>When <c>true</c>, returns only the first animation frame unless <c>FrameIndex</c> selects another frame.</summary>
    public bool FirstFrameOnly;
    /// <summary>The zero-based animation frame to return, or <c>-1</c> to use the normal frame-selection behavior.</summary>
    public int FrameIndex;
    /// <summary>When <c>true</c>, copies ICC, Exif, and XMP payloads into the decoded image.</summary>
    public bool DecodeMetadata;
    /// <summary>The horizontal offset of the requested source rectangle; used only when <c>CropWidth</c> and <c>CropHeight</c> are non-zero.</summary>
    public int CropX;
    /// <summary>The vertical offset of the requested source rectangle; used only when <c>CropWidth</c> and <c>CropHeight</c> are non-zero.</summary>
    public int CropY;
    /// <summary>The requested source-rectangle width, or <c>0</c> together with <c>CropHeight</c> to use the full canvas.</summary>
    public int CropWidth;
    /// <summary>The requested source-rectangle height, or <c>0</c> together with <c>CropWidth</c> to use the full canvas.</summary>
    public int CropHeight;
    /// <summary>The requested output width, or <c>0</c> to derive or retain the width according to <c>AspectRatioMode</c>; scaling uses nearest-neighbor sampling.</summary>
    public int TargetWidth;
    /// <summary>The requested output height, or <c>0</c> to derive or retain the height according to <c>AspectRatioMode</c>.</summary>
    public int TargetHeight;
    /// <summary>Whether scaling stretches to exact dimensions or preserves the cropped source aspect ratio.</summary>
    public WebpAspectRatioMode AspectRatioMode;
    /// <summary>The allocation and complexity ceilings applied before decoding data.</summary>
    public WebpSecurityLimits Limits;

    /// <summary>Creates permissive decode options that retain metadata, unknown chunks, and independent frames.</summary>
    public WebpDecodeOptions()
    {
        Strictness = WebpStrictness.Permissive;
        UnknownChunkPolicy = WebpConstants.DefaultUnknownChunkPolicy;
        AnimationDecodeMode = WebpAnimationDecodeMode.IndependentFrames;
        IdentifyOnly = false;
        FirstFrameOnly = false;
        FrameIndex = -1;
        DecodeMetadata = true;
        CropX = 0;
        CropY = 0;
        CropWidth = 0;
        CropHeight = 0;
        TargetWidth = 0;
        TargetHeight = 0;
        AspectRatioMode = WebpAspectRatioMode.Preserve;
        Limits = new WebpSecurityLimits();
    }
}

/// <summary>Controls compression, alpha handling, preservation behavior, and output safety limits.</summary>
/// <remarks>All enum values and numeric ranges are validated regardless of the selected encoding mode. Invalid ranges produce <see cref="ArgumentOutOfRangeException"/>; incompatible settings, such as simultaneous target size and target PSNR requests, produce <see cref="ArgumentException"/>.</remarks>
public class WebpEncodeOptions
{
    /// <summary>The requested automatic, lossy, lossless, or near-lossless strategy.</summary>
    public WebpEncodeMode Mode;
    /// <summary>The content preset used to tune lossy settings before explicit option values are applied.</summary>
    public WebpLossyPreset Preset;
    /// <summary>The requested lossy quality from <c>0</c> through <c>100</c>; larger values retain more detail.</summary>
    public int Quality;
    /// <summary>The requested lossy search effort from <c>0</c> through <c>6</c>; larger values may take more work.</summary>
    public int Method;
    /// <summary>The requested deblocking-filter strength from <c>0</c> through <c>100</c>.</summary>
    public int FilterStrength;
    /// <summary>The filter sharpness from <c>0</c> through <c>7</c>; larger values preserve sharper edges.</summary>
    public int FilterSharpness;
    /// <summary>When <c>true</c>, allows the encoder to strengthen weak filtering based on the image.</summary>
    public bool AutoFilter;
    /// <summary>The spatial noise-shaping amount from <c>0</c> through <c>100</c>.</summary>
    public int SpatialNoiseShaping;
    /// <summary>The optional RGB preprocessing applied before lossy encoding.</summary>
    public WebpLossyPreprocessing Preprocessing;
    /// <summary>The desired encoded size in bytes, or <c>0</c> when no size target is requested.</summary>
    public int TargetSize;
    /// <summary>The desired peak signal-to-noise ratio in decibels, or <c>0</c> when no PSNR target is requested.</summary>
    public double TargetPsnr;
    /// <summary>The requested planning-pass count; when a size or PSNR target is active, a value below <c>2</c> selects the library's default multi-pass count.</summary>
    public int PassCount;
    /// <summary>The near-lossless preprocessing quality from <c>0</c> through <c>100</c>, where <c>100</c> preserves exact samples.</summary>
    public int NearLosslessQuality;
    /// <summary>When preserving lossy alpha, keeps RGB values beneath fully transparent pixels.</summary>
    public bool ExactTransparentRgb;
    /// <summary>When <c>true</c>, uses lossless coding for the separate alpha plane of lossy images.</summary>
    public bool LosslessAlpha;
    /// <summary>The separate alpha-plane quality from <c>0</c> through <c>100</c>.</summary>
    public int AlphaQuality;
    /// <summary>The storage method for the separate alpha plane of lossy images.</summary>
    public WebpAlphaCompressionMethod AlphaCompression;
    /// <summary>The filter-search mode for the separate alpha plane.</summary>
    public WebpAlphaFilterMode AlphaFilter;
    /// <summary>Whether lossy encoding preserves, discards, or blends source alpha.</summary>
    public WebpLossyAlphaMode LossyAlphaMode;
    /// <summary>The opaque background used when <c>LossyAlphaMode</c> is <c>BlendWithBackground</c>, packed as blue, green, red, alpha bytes.</summary>
    public uint AlphaBackgroundColorBgra;
    /// <summary>When <c>true</c>, requires stable bitstream decisions and canonical RIFF serialization for identical inputs and settings.</summary>
    public bool Deterministic;
    /// <summary>Which metadata payloads from a <see cref="WebpImage"/> model are copied to the output.</summary>
    public WebpMetadataCopyPolicy MetadataCopyPolicy;
    /// <summary>Whether unrecognized chunks from a <see cref="WebpImage"/> model are preserved.</summary>
    public WebpUnknownChunkPolicy UnknownChunkPolicy;
    /// <summary>The allocation and file-size ceilings applied while encoding.</summary>
    public WebpSecurityLimits Limits;

    /// <summary>Creates general-purpose automatic encoding options with quality <c>75</c>.</summary>
    public WebpEncodeOptions()
    {
        Mode = WebpConstants.DefaultEncodeMode;
        Preset = WebpConstants.DefaultLossyPreset;
        Quality = WebpConstants.DefaultQuality;
        Method = WebpConstants.DefaultMethod;
        FilterStrength = WebpConstants.DefaultFilterStrength;
        FilterSharpness = WebpConstants.DefaultFilterSharpness;
        AutoFilter = WebpConstants.DefaultAutoFilter;
        SpatialNoiseShaping = WebpConstants.DefaultSpatialNoiseShaping;
        Preprocessing = WebpConstants.DefaultLossyPreprocessing;
        TargetSize = WebpConstants.DefaultTargetSize;
        TargetPsnr = WebpConstants.DefaultTargetPsnr;
        PassCount = WebpConstants.DefaultPassCount;
        NearLosslessQuality = WebpConstants.DefaultNearLosslessQuality;
        ExactTransparentRgb = false;
        LosslessAlpha = true;
        AlphaQuality = WebpConstants.DefaultAlphaQuality;
        AlphaCompression = WebpConstants.DefaultAlphaCompression;
        AlphaFilter = WebpConstants.DefaultAlphaFilter;
        LossyAlphaMode = WebpConstants.DefaultLossyAlphaMode;
        AlphaBackgroundColorBgra = WebpConstants.DefaultAlphaBackgroundColorBgra;
        Deterministic = WebpConstants.DefaultDeterministic;
        MetadataCopyPolicy = WebpConstants.DefaultMetadataCopyPolicy;
        UnknownChunkPolicy = WebpConstants.DefaultUnknownChunkPolicy;
        Limits = new WebpSecurityLimits();
    }
}
