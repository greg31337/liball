namespace gif;

/// <summary>Identifies the GIF file-format version declared by a document.</summary>
public enum GifVersion
{
    /// <summary>No supported GIF version has been selected or detected.</summary>
    Unknown = 0,
    /// <summary>The GIF87a format, suitable for images that do not use GIF89a extensions.</summary>
    Gif87a = 87,
    /// <summary>The GIF89a format, which supports animation control and metadata extensions.</summary>
    Gif89a = 89
}

/// <summary>Controls what a player does with a rendered frame before drawing the next frame.</summary>
/// <remarks>Values map directly to the three-bit disposal field in a Graphic Control Extension.</remarks>
public enum GifDisposalMethod
{
    /// <summary>No disposal behavior is specified; decoders apply their configured fallback.</summary>
    Unspecified = 0,
    /// <summary>Leaves the current canvas unchanged before the next frame is drawn.</summary>
    DoNotDispose = 1,
    /// <summary>Clears the frame rectangle to the configured background before the next frame.</summary>
    RestoreToBackground = 2,
    /// <summary>Restores the canvas contents saved before the current frame was drawn.</summary>
    RestoreToPrevious = 3,
    /// <summary>Preserves reserved GIF disposal value 4 for round trips.</summary>
    Reserved4 = 4,
    /// <summary>Preserves reserved GIF disposal value 5 for round trips.</summary>
    Reserved5 = 5,
    /// <summary>Preserves reserved GIF disposal value 6 for round trips.</summary>
    Reserved6 = 6,
    /// <summary>Preserves reserved GIF disposal value 7 for round trips.</summary>
    Reserved7 = 7
}

/// <summary>Controls how strictly a decoder enforces the GIF specification.</summary>
public enum GifStrictness
{
    /// <summary>Accepts common compatible files while rejecting malformed data.</summary>
    Normal = 0,
    /// <summary>Enforces reserved bits and structural requirements strictly.</summary>
    Strict = 1,
    /// <summary>Returns partial output with warnings when supported malformed cases can be recovered.</summary>
    Tolerant = 2
}

/// <summary>Selects how GIF Plain Text Extensions are handled while decoding.</summary>
public enum GifPlainTextExtensionPolicy
{
    /// <summary>Retains plain-text extensions as round-trip metadata without rasterizing or exposing them as rendering blocks.</summary>
    Preserve = 0,
    /// <summary>Consumes and discards plain-text extensions while reporting an ignored-extension warning.</summary>
    Ignore = 1,
    /// <summary>Retains plain-text extensions and exposes their structured graphic data without rasterizing it.</summary>
    Expose = 2
}

/// <summary>Selects how millisecond delays are rounded to GIF hundredths of a second.</summary>
public enum GifDelayRoundingMode
{
    /// <summary>Rounds toward zero to the preceding 10-millisecond boundary.</summary>
    Floor = 0,
    /// <summary>Rounds to the closest 10-millisecond boundary, with 5-millisecond ties rounded upward.</summary>
    Nearest = 1,
    /// <summary>Rounds any partial hundredth upward to the next 10-millisecond boundary.</summary>
    Ceiling = 2
}

/// <summary>Selects whether encoded frames share a global palette or carry local palettes.</summary>
public enum GifColorTableMode
{
    /// <summary>Lets the encoder select global or local palettes from the supplied frames.</summary>
    Auto = 0,
    /// <summary>Uses one palette from the logical screen for every frame.</summary>
    Global = 1,
    /// <summary>Writes a separate local palette on each frame.</summary>
    Local = 2
}

/// <summary>Selects the algorithm used to reduce RGBA colors to a GIF palette.</summary>
public enum GifQuantizerMode
{
    /// <summary>Builds an exact palette and succeeds only when the color limit is sufficient.</summary>
    Exact = 0,
    /// <summary>Partitions sampled colors into representative color boxes.</summary>
    MedianCut = 1,
    /// <summary>Groups colors in an octree and reduces its least significant branches.</summary>
    Octree = 2,
    /// <summary>Maps input to an evenly spaced grayscale palette.</summary>
    Grayscale = 3,
    /// <summary>Maps input to the fixed browser-safe color cube.</summary>
    WebSafe = 4,
    /// <summary>Maps input to the palette supplied through the quantization options.</summary>
    CallerPalette = 5
}

/// <summary>Selects how quantization error is distributed while pixels are mapped to a palette.</summary>
public enum GifDitherMode
{
    /// <summary>Maps each pixel independently to its nearest palette color.</summary>
    None = 0,
    /// <summary>Diffuses color error to neighboring pixels using Floyd-Steinberg weights.</summary>
    FloydSteinberg = 1,
    /// <summary>Uses a repeating Bayer threshold matrix without propagating error between pixels.</summary>
    OrderedBayer = 2
}

/// <summary>Selects one legacy animation transformation performed before encoding.</summary>
/// <remarks>Use the independent fields on <see cref="GifEncodeOptions"/> when several transformations should be combined.</remarks>
public enum GifOptimizationMode
{
    /// <summary>Preserves the supplied frames.</summary>
    None = 0,
    /// <summary>Crops coalesced frames to the rectangle that changed from the preceding frame.</summary>
    Deconstruct = 1,
    /// <summary>Removes consecutive duplicate frames and merges their delays.</summary>
    RemoveDuplicateFrames = 2,
    /// <summary>Removes zero-delay frames while retaining a usable final animation.</summary>
    RemoveZeroDelayFrames = 3
}

/// <summary>Identifies a raw content block in the ordered portion of a GIF stream.</summary>
public enum GifRawBlockKind
{
    /// <summary>Identifies a Graphic Control Extension referenced through <see cref="GifDocument.GraphicControls"/>.</summary>
    GraphicControl = 0,
    /// <summary>Identifies a comment extension referenced through <see cref="GifDocument.Comments"/>.</summary>
    Comment = 1,
    /// <summary>Identifies an application extension referenced through <see cref="GifDocument.ApplicationExtensions"/>.</summary>
    Application = 2,
    /// <summary>Identifies a Plain Text Extension referenced through <see cref="GifDocument.PlainTextExtensions"/>.</summary>
    PlainText = 3,
    /// <summary>Identifies an unrecognized extension referenced through <see cref="GifDocument.UnknownExtensions"/>.</summary>
    UnknownExtension = 4,
    /// <summary>Identifies an image block referenced through <see cref="GifDocument.Frames"/>.</summary>
    Image = 5,
    /// <summary>Identifies the stream trailer and does not reference a collection item.</summary>
    Trailer = 6
}

/// <summary>Describes the semantic scope of a raw GIF content block.</summary>
public enum GifRawBlockScope
{
    /// <summary>The block has document-level scope and is not attached to a rendering block.</summary>
    Document = 0,
    /// <summary>The block represents or controls the image at the entry's scope index.</summary>
    Frame = 1,
    /// <summary>The block represents or controls the plain-text graphic at the entry's scope index.</summary>
    PlainText = 2,
    /// <summary>The extension appears after the final rendering block immediately before the trailer region.</summary>
    Trailer = 3
}

/// <summary>Selects the color used when an animation canvas or disposed frame rectangle is cleared.</summary>
public enum GifBackgroundMode
{
    /// <summary>Uses the logical screen's background palette entry.</summary>
    LogicalScreen = 0,
    /// <summary>Clears pixels to fully transparent RGBA values.</summary>
    Transparent = 1,
    /// <summary>Uses the configured solid background behavior.</summary>
    SolidColor = 2
}

/// <summary>Controls how transparency is chosen while RGBA pixels are converted to indexed pixels.</summary>
public enum GifTransparentColorMode
{
    /// <summary>Does not reserve a transparent palette entry.</summary>
    None = 0,
    /// <summary>Preserves transparency already represented by indexed input.</summary>
    Preserve = 1,
    /// <summary>Treats RGBA pixels at or below the configured alpha threshold as transparent.</summary>
    AlphaThreshold = 2,
    /// <summary>Uses an exact caller-selected transparent palette color.</summary>
    ExactColor = 3
}

/// <summary>Controls when the GIF LZW encoder resets its code dictionary.</summary>
public enum GifLzwClearPolicy
{
    /// <summary>Keeps dictionary entries until the maximum table size is reached.</summary>
    ClearOnFullDictionary = 0,
    /// <summary>May reset the dictionary earlier to limit table growth.</summary>
    EarlyClear = 1
}

/// <summary>Identifies fatal validation, decoding, and encoding failures.</summary>
/// <remarks><see cref="GifErrorDiagnostic"/> supplies the message and location associated with a code.</remarks>
public enum GifErrorCode
{
    /// <summary>No error occurred.</summary>
    None = 0,
    /// <summary>The input does not begin with the GIF signature.</summary>
    InvalidSignature = 1,
    /// <summary>The input declares a GIF version that the library does not support.</summary>
    UnsupportedVersion = 2,
    /// <summary>The input ended before the current block was complete.</summary>
    UnexpectedEndOfStream = 3,
    /// <summary>A required logical screen descriptor was not available.</summary>
    MissingLogicalScreenDescriptor = 4,
    /// <summary>A palette does not contain a legal power-of-two count from 2 through 256.</summary>
    InvalidColorTableSize = 5,
    /// <summary>The logical-screen background index is outside its palette.</summary>
    InvalidBackgroundColorIndex = 6,
    /// <summary>A top-level block marker is not defined by the GIF format.</summary>
    UnknownBlockType = 7,
    /// <summary>An extension has an invalid header, fixed size, or payload.</summary>
    MalformedExtension = 8,
    /// <summary>A chain of GIF data sub-blocks has no zero-length terminator.</summary>
    UnterminatedSubBlocks = 9,
    /// <summary>A frame rectangle is empty, overflows, or is outside the supported screen bounds.</summary>
    InvalidImageRectangle = 10,
    /// <summary>A frame has neither a usable local palette nor a usable global palette.</summary>
    MissingColorTable = 11,
    /// <summary>The LZW minimum code size is outside the GIF-supported range.</summary>
    InvalidLzwMinimumCodeSize = 12,
    /// <summary>The LZW stream contains an invalid code sequence.</summary>
    InvalidLzwCodeStream = 13,
    /// <summary>LZW output contains a different number of pixels than the frame rectangle requires.</summary>
    DecodedPixelCountMismatch = 14,
    /// <summary>Decoding would exceed the configured aggregate pixel limit.</summary>
    DecodedPixelLimitExceeded = 15,
    /// <summary>The input contains more frames than the configured limit.</summary>
    FrameLimitExceeded = 16,
    /// <summary>An image dimension exceeds the configured limit.</summary>
    DimensionLimitExceeded = 17,
    /// <summary>Reading or writing would exceed the configured byte limit.</summary>
    ByteLimitExceeded = 18,
    /// <summary>An operation would exceed the configured memory limit.</summary>
    MemoryLimitExceeded = 19,
    /// <summary>An indexed pixel or transparency index does not exist in the active palette.</summary>
    InvalidPaletteIndex = 20,
    /// <summary>Packed-field flags or dependent values form an invalid combination.</summary>
    MalformedPackedField = 21,
    /// <summary>A reserved packed-field bit or value is nonzero in strict mode.</summary>
    ReservedFieldValue = 22,
    /// <summary>A strict GIF decode reached the end of input without the required trailer.</summary>
    MissingTrailer = 23,
    /// <summary>The requested operation cannot run in the current object or stream state.</summary>
    InvalidOperationState = 24,
    /// <summary>Encoded or decoded data could not be written to the caller's destination.</summary>
    DestinationWriteFailed = 25,
    /// <summary>Decoding would consume more source bytes than the configured input limit.</summary>
    InputByteLimitExceeded = 26,
    /// <summary>Decoding would consume more compressed image payload bytes than the configured limit.</summary>
    CompressedByteLimitExceeded = 27,
    /// <summary>Decoding would consume more data sub-blocks than the configured count limit.</summary>
    SubBlockCountLimitExceeded = 28,
    /// <summary>Decoding would consume more data sub-block payload bytes than the configured limit.</summary>
    SubBlockByteLimitExceeded = 29
}

/// <summary>Identifies recoverable conditions reported while processing a GIF.</summary>
public enum GifWarningCode
{
    /// <summary>No warning occurred.</summary>
    None = 0,
    /// <summary>A frame used a reserved disposal value and was rendered with a fallback.</summary>
    ReservedDisposalMethod = 1,
    /// <summary>The stream ended without the required trailer marker.</summary>
    MissingTrailer = 2,
    /// <summary>Bytes were present after the trailer marker.</summary>
    ExtraDataAfterTrailer = 3,
    /// <summary>A plain-text extension was intentionally ignored by the selected behavior.</summary>
    IgnoredPlainTextExtension = 4,
    /// <summary>Tolerant decoding recovered from malformed block data.</summary>
    ToleratedMalformedBlock = 5,
    /// <summary>A zero frame delay was replaced by the configured minimum delay.</summary>
    ZeroDelayNormalized = 6,
    /// <summary>A frame rectangle was clipped to the logical screen while rendering.</summary>
    FrameClippedToLogicalScreen = 7
}
