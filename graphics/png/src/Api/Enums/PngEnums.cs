namespace png;

/// <summary>
/// Identifies the channel layout encoded by a PNG image.
/// </summary>
public enum PngColorType
{
    /// <summary>
    /// Stores one grayscale sample per pixel.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Stores red, green, and blue samples per pixel.
    /// </summary>
    Truecolor = 2,
    /// <summary>
    /// Stores a palette index per pixel and requires a PLTE chunk.
    /// </summary>
    IndexedColor = 3,
    /// <summary>
    /// Stores grayscale followed by alpha per pixel.
    /// </summary>
    GrayscaleAlpha = 4,
    /// <summary>
    /// Stores red, green, blue, and alpha per pixel.
    /// </summary>
    TruecolorAlpha = 6
}

/// <summary>
/// Identifies the number of bits used for each PNG sample.
/// </summary>
public enum PngBitDepth
{
    /// <summary>
    /// Uses one bit per sample.
    /// </summary>
    One = 1,
    /// <summary>
    /// Uses two bits per sample.
    /// </summary>
    Two = 2,
    /// <summary>
    /// Uses four bits per sample.
    /// </summary>
    Four = 4,
    /// <summary>
    /// Uses eight bits per sample.
    /// </summary>
    Eight = 8,
    /// <summary>
    /// Uses sixteen bits per sample.
    /// </summary>
    Sixteen = 16
}

/// <summary>
/// Identifies the compression method declared in the PNG header.
/// </summary>
public enum PngCompressionMethod
{
    /// <summary>
    /// Uses the zlib-wrapped Deflate method defined by PNG.
    /// </summary>
    Deflate = 0
}

/// <summary>
/// Identifies the scanline filtering method declared in the PNG header.
/// </summary>
public enum PngFilterMethod
{
    /// <summary>
    /// Uses the five per-row reversible PNG filters.
    /// </summary>
    Adaptive = 0
}

/// <summary>
/// Identifies whether scanlines are stored directly or in seven Adam7 passes.
/// </summary>
public enum PngInterlaceMethod
{
    /// <summary>
    /// Stores rows from top to bottom without interlacing.
    /// </summary>
    None = 0,
    /// <summary>
    /// Stores pixels in seven progressively refinable Adam7 passes.
    /// </summary>
    Adam7 = 1
}

/// <summary>
/// Identifies the reversible prediction filter applied to one PNG scanline.
/// </summary>
public enum PngFilterType
{
    /// <summary>
    /// Stores each byte without prediction.
    /// </summary>
    None = 0,
    /// <summary>
    /// Predicts each byte from the corresponding byte of the previous pixel.
    /// </summary>
    Sub = 1,
    /// <summary>
    /// Predicts each byte from the byte above it.
    /// </summary>
    Up = 2,
    /// <summary>
    /// Predicts from the integer average of the left and upper bytes.
    /// </summary>
    Average = 3,
    /// <summary>
    /// Predicts from the left, upper, and upper-left bytes using the Paeth function.
    /// </summary>
    Paeth = 4
}

/// <summary>
/// Selects how the encoder chooses a reversible PNG filter for each row.
/// </summary>
public enum PngRowFilterStrategy
{
    /// <summary>
    /// Scores all five PNG filters independently for each row.
    /// </summary>
    Adaptive = 0,
    /// <summary>
    /// Uses Sub for the first row and Up for later rows without scoring alternatives.
    /// </summary>
    Fast = 1,
    /// <summary>
    /// Uses FixedFilterType for every row.
    /// </summary>
    Fixed = 2,
    /// <summary>
    /// Scores every filter for direct encoding and compares adaptive, fast, and fixed encodes during optimization.
    /// </summary>
    Exhaustive = 3
}

/// <summary>
/// Controls how preserved unknown chunks are handled during output.
/// </summary>
public enum PngChunkDisposition
{
    /// <summary>
    /// Writes the preserved unknown chunk when its copy-safety rules allow it.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Omits the unknown chunk from output.
    /// </summary>
    Drop = 1,
    /// <summary>
    /// Writes caller-supplied replacement payload data.
    /// </summary>
    Replace = 2
}

/// <summary>
/// Identifies the datastream region occupied by a raw PNG chunk.
/// </summary>
public enum PngChunkPlacement
{
    /// <summary>
    /// Leaves placement to the chunk codec when the chunk has not been read from a datastream.
    /// </summary>
    Unspecified = 0,
    /// <summary>
    /// Places the chunk after IHDR and before PLTE when a palette is present.
    /// </summary>
    BeforePalette = 1,
    /// <summary>
    /// Places the chunk after PLTE when present and before the first IDAT chunk.
    /// </summary>
    BeforeImageData = 2,
    /// <summary>
    /// Identifies an IDAT chunk in the contiguous image-data region.
    /// </summary>
    ImageData = 3,
    /// <summary>
    /// Places the chunk after the contiguous IDAT region and before IEND.
    /// </summary>
    AfterImageData = 4
}

/// <summary>
/// Identifies whether a raw chunk was created by the caller or preserved from an input datastream.
/// </summary>
public enum PngChunkPreservationState
{
    /// <summary>
    /// Identifies a new chunk that was not read from an input datastream.
    /// </summary>
    Created = 0,
    /// <summary>
    /// Identifies a chunk preserved from an input datastream.
    /// </summary>
    Preserved = 1
}

/// <summary>
/// Controls how the raw chunk codec handles chunk placement while writing.
/// </summary>
public enum PngChunkPlacementPolicy
{
    /// <summary>
    /// Writes chunks in caller-supplied order without changing their placement.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Writes chunks in canonical pre-PLTE, pre-IDAT, contiguous-IDAT, and post-IDAT regions.
    /// </summary>
    Canonicalize = 1
}

/// <summary>
/// Controls whether ancillary metadata is retained during encoding.
/// </summary>
public enum PngMetadataPolicy
{
    /// <summary>
    /// Retains available ancillary metadata.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Removes ancillary metadata that is not required to represent the pixels.
    /// </summary>
    Strip = 1,
    /// <summary>
    /// Retains only metadata selected by the caller's workflow.
    /// </summary>
    KeepSelected = 2,
    /// <summary>
    /// Retains metadata except for entries selected for removal.
    /// </summary>
    RemoveSelected = 3
}

/// <summary>
/// Selects how standard PNG text payloads are stored while encoding metadata.
/// </summary>
public enum PngTextCompressionMode
{
    /// <summary>
    /// Retains the source text chunk family and compression flag.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Stores Latin-1 text as tEXt and international text as uncompressed iTXt.
    /// </summary>
    Uncompressed = 1,
    /// <summary>
    /// Stores Latin-1 text as zTXt and international text as compressed iTXt.
    /// </summary>
    Compressed = 2,
    /// <summary>
    /// Compresses text whose encoded value length meets TextCompressionThreshold.
    /// </summary>
    Automatic = 3
}

/// <summary>
/// Identifies which standard PNG text chunk supplied a text entry.
/// </summary>
public enum PngTextKind
{
    /// <summary>
    /// Represents an uncompressed Latin-1 tEXt entry.
    /// </summary>
    Latin1 = 0,
    /// <summary>
    /// Represents a compressed Latin-1 zTXt entry.
    /// </summary>
    CompressedLatin1 = 1,
    /// <summary>
    /// Represents an international UTF-8 iTXt entry.
    /// </summary>
    InternationalUtf8 = 2
}

/// <summary>
/// Controls how strictly malformed PNG data is handled.
/// </summary>
public enum PngErrorPolicy
{
    /// <summary>
    /// Treats every specification violation as an error.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Continues past selected malformed ancillary metadata and reports warnings.
    /// </summary>
    TolerantMetadata = 1,
    /// <summary>
    /// Allows ancillary ordering and CRC issues supported by the repair workflow.
    /// </summary>
    TolerantRepair = 2,
    /// <summary>
    /// Collects as much structural information as possible from malformed data.
    /// </summary>
    BestEffortInspection = 3
}

/// <summary>
/// Identifies the in-memory channel order and sample depth of a pixel buffer.
/// </summary>
/// <remarks>Packed grayscale and indexed formats place the first pixel in the high bits of each byte. Sixteen-bit samples use big-endian byte order.</remarks>
public enum PngPixelFormat
{
    /// <summary>
    /// Does not describe a usable pixel layout.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Stores packed 1-bit grayscale samples.
    /// </summary>
    Grayscale1 = 1,
    /// <summary>
    /// Stores packed 2-bit grayscale samples.
    /// </summary>
    Grayscale2 = 2,
    /// <summary>
    /// Stores packed 4-bit grayscale samples.
    /// </summary>
    Grayscale4 = 3,
    /// <summary>
    /// Stores one 8-bit grayscale sample per pixel.
    /// </summary>
    Grayscale8 = 4,
    /// <summary>
    /// Stores one 16-bit grayscale sample per pixel.
    /// </summary>
    Grayscale16 = 5,
    /// <summary>
    /// Stores packed 1-bit palette indices.
    /// </summary>
    Indexed1 = 6,
    /// <summary>
    /// Stores packed 2-bit palette indices.
    /// </summary>
    Indexed2 = 7,
    /// <summary>
    /// Stores packed 4-bit palette indices.
    /// </summary>
    Indexed4 = 8,
    /// <summary>
    /// Stores one 8-bit palette index per pixel.
    /// </summary>
    Indexed8 = 9,
    /// <summary>
    /// Stores 8-bit red, green, and blue samples per pixel.
    /// </summary>
    Rgb8 = 10,
    /// <summary>
    /// Stores 16-bit red, green, and blue samples per pixel.
    /// </summary>
    Rgb16 = 11,
    /// <summary>
    /// Stores 8-bit grayscale and alpha samples per pixel.
    /// </summary>
    GrayscaleAlpha8 = 12,
    /// <summary>
    /// Stores 16-bit grayscale and alpha samples per pixel.
    /// </summary>
    GrayscaleAlpha16 = 13,
    /// <summary>
    /// Stores 8-bit red, green, blue, and alpha samples per pixel.
    /// </summary>
    Rgba8 = 14,
    /// <summary>
    /// Stores 16-bit red, green, blue, and alpha samples per pixel.
    /// </summary>
    Rgba16 = 15,
    /// <summary>
    /// Stores 8-bit blue, green, and red samples per pixel.
    /// </summary>
    Bgr8 = 16,
    /// <summary>
    /// Stores 16-bit blue, green, and red samples per pixel in big-endian sample order.
    /// </summary>
    Bgr16 = 17,
    /// <summary>
    /// Stores 8-bit blue, green, red, and alpha samples per pixel.
    /// </summary>
    Bgra8 = 18,
    /// <summary>
    /// Stores 16-bit blue, green, red, and alpha samples per pixel in big-endian sample order.
    /// </summary>
    Bgra16 = 19,
    /// <summary>
    /// Stores 8-bit alpha, red, green, and blue samples per pixel.
    /// </summary>
    Argb8 = 20,
    /// <summary>
    /// Stores 16-bit alpha, red, green, and blue samples per pixel in big-endian sample order.
    /// </summary>
    Argb16 = 21
}

/// <summary>
/// Identifies how an APNG frame is combined with the current canvas.
/// </summary>
public enum PngApngBlendOperation
{
    /// <summary>
    /// Replaces the frame rectangle with source pixels.
    /// </summary>
    Source = 0,
    /// <summary>
    /// Composites source pixels over the existing canvas using straight alpha.
    /// </summary>
    Over = 1
}

/// <summary>
/// Identifies how the APNG canvas is changed after displaying a frame.
/// </summary>
public enum PngApngDisposeOperation
{
    /// <summary>
    /// Leaves the displayed canvas unchanged for the next frame.
    /// </summary>
    None = 0,
    /// <summary>
    /// Clears the displayed frame rectangle to transparent black.
    /// </summary>
    Background = 1,
    /// <summary>
    /// Restores the canvas state from before the frame was rendered.
    /// </summary>
    Previous = 2
}
