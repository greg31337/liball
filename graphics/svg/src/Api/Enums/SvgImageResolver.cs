namespace svg;

/// <summary>Identifies a stable PNG header-probing outcome.</summary>
public enum SvgPngProbeError
{
    /// <summary>The PNG header was accepted.</summary>
    None = 0,
    /// <summary>The probe arguments or limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The byte source ended before the complete PNG header.</summary>
    TruncatedHeader = 2,
    /// <summary>The PNG signature was invalid.</summary>
    InvalidSignature = 3,
    /// <summary>The first chunk was not a conforming IHDR chunk.</summary>
    InvalidHeader = 4,
    /// <summary>The dimensions exceeded a configured raster bound.</summary>
    DimensionLimitExceeded = 5
}

/// <summary>Identifies a stable PNG pixel-decoding outcome.</summary>
public enum SvgPngDecodeError
{
    /// <summary>The PNG pixels were decoded.</summary>
    None = 0,
    /// <summary>The bytes or raster limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The PNG signature was invalid.</summary>
    InvalidSignature = 2,
    /// <summary>The IHDR fields or required chunk order were invalid.</summary>
    InvalidHeader = 3,
    /// <summary>A chunk length, type, placement, or payload was invalid.</summary>
    InvalidChunk = 4,
    /// <summary>A chunk CRC did not match its type and payload.</summary>
    CrcMismatch = 5,
    /// <summary>The chunk count exceeded MaxPngChunks.</summary>
    ChunkLimitExceeded = 6,
    /// <summary>The dimensions or RGBA output exceeded raster limits.</summary>
    DimensionLimitExceeded = 7,
    /// <summary>The required inflated scanlines exceeded MaxPngDecodedBytes.</summary>
    DecodedSizeLimitExceeded = 8,
    /// <summary>The zlib stream was malformed, truncated, or had an unexpected size.</summary>
    DecompressionFailed = 9,
    /// <summary>A scanline filter or reconstructed sample stream was invalid.</summary>
    InvalidScanline = 10,
    /// <summary>An indexed image omitted a valid palette or referenced a missing entry.</summary>
    InvalidPalette = 11
}

/// <summary>Selects the PNG scanline filter applied by the encoder.</summary>
public enum SvgPngFilterMode
{
    /// <summary>Selects the lowest-cost filter independently for each row.</summary>
    Adaptive = 0,
    /// <summary>Writes every row without prediction.</summary>
    None = 1,
    /// <summary>Predicts every byte from the corresponding byte of the preceding pixel.</summary>
    Sub = 2,
    /// <summary>Predicts every byte from the byte directly above it.</summary>
    Up = 3,
    /// <summary>Predicts every byte from the average of its left and upper neighbors.</summary>
    Average = 4,
    /// <summary>Predicts every byte with the Paeth function.</summary>
    Paeth = 5
}

/// <summary>Selects the deterministic zlib compression effort used by the PNG encoder.</summary>
public enum SvgPngCompressionMode
{
    /// <summary>Prioritizes encoding speed.</summary>
    Fastest = 0,
    /// <summary>Balances encoding time and output size.</summary>
    Balanced = 1,
    /// <summary>Prioritizes the smallest output available from the platform zlib encoder.</summary>
    Smallest = 2
}

/// <summary>Identifies a stable PNG encoding outcome.</summary>
public enum SvgPngEncodeError
{
    /// <summary>The PNG was encoded.</summary>
    None = 0,
    /// <summary>The pixels, destination, options, or mutable buffer state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The dimensions or pixel count exceeded configured raster limits.</summary>
    DimensionLimitExceeded = 2,
    /// <summary>The input, filtered scanlines, or encoded PNG exceeded a configured byte limit.</summary>
    ByteLimitExceeded = 3,
    /// <summary>The platform zlib encoder failed.</summary>
    CompressionFailed = 4,
    /// <summary>The destination stream failed while receiving the complete encoded PNG.</summary>
    StreamWriteFailed = 5,
    /// <summary>The encoded PNG could not be represented by the bounded data URL encoder.</summary>
    DataUrlFailed = 6
}
