namespace jpeg;

/// <summary>Identifies the image format family recognized from the input signature and, for marker-based streams, its frame process.</summary>
public enum JpegFormatFamily
{
    /// <summary>The input does not have a recognized format signature.</summary>
    Unknown = 0,
    /// <summary>A classic marker-based JPEG stream described by the JPEG-1 family of standards.</summary>
    ClassicJpeg = 1,
    /// <summary>A marker-based JPEG-LS stream.</summary>
    JpegLs = 2,
    /// <summary>A JPEG 2000 codestream or JP2-family container.</summary>
    Jpeg2000 = 3,
    /// <summary>A JPEG XR codestream or file.</summary>
    JpegXr = 4,
    /// <summary>A JPEG XL codestream or container.</summary>
    JpegXl = 5,
    /// <summary>A High Efficiency Image File Format container, including recognized HEIF and AVIF brands.</summary>
    Heif = 6
}

/// <summary>Describes whether a recognized format or coding process is supported by the library.</summary>
public enum JpegSupportStatus
{
    /// <summary>Support cannot be determined because the format or coding process was not recognized.</summary>
    Unknown = 0,
    /// <summary>The format or coding process is supported by the library.</summary>
    Supported = 1,
    /// <summary>The format or coding process is recognized but is not supported by the library.</summary>
    RecognizedButUnsupported = 2
}

/// <summary>Identifies how image samples are arranged in a public buffer model.</summary>
public enum JpegBufferLayout
{
    /// <summary>No sample buffer layout has been selected.</summary>
    Unknown = 0,
    /// <summary>All component samples for each pixel are adjacent in one row-major buffer.</summary>
    Interleaved = 1,
    /// <summary>Each component has a separate row-major sample plane.</summary>
    Planar = 2
}

/// <summary>Identifies who owns the lifetime and mutation responsibility for sample-buffer arrays.</summary>
public enum JpegBufferOwnership
{
    /// <summary>The ownership relationship has not been specified.</summary>
    Unknown = 0,
    /// <summary>The library allocated or copied the buffer and the containing result owns it.</summary>
    LibraryOwned = 1,
    /// <summary>The buffer is borrowed from the caller, who retains its lifetime and mutation responsibility.</summary>
    CallerOwned = 2
}

/// <summary>Identifies the managed element type used to store image samples.</summary>
public enum JpegSampleStorage
{
    /// <summary>No sample storage has been selected.</summary>
    Unknown = 0,
    /// <summary>Samples are stored in a <c>byte</c> array.</summary>
    Byte = 1,
    /// <summary>Samples are stored in a signed 32-bit integer array capable of holding 12-bit and 16-bit values.</summary>
    Int32 = 2
}

/// <summary>Identifies the JPEG coding process selected by a start-of-frame marker.</summary>
public enum JpegProcess
{
    /// <summary>No recognized coding process has been selected.</summary>
    Unknown = 0,
    /// <summary>Baseline sequential 8-bit DCT coding with Huffman entropy data.</summary>
    BaselineDct = JpegConstants.MarkerStartOfFrame0,
    /// <summary>Extended sequential DCT coding with Huffman entropy data.</summary>
    ExtendedSequentialDct = JpegConstants.MarkerStartOfFrame1,
    /// <summary>Progressive DCT coding with Huffman entropy data.</summary>
    ProgressiveDct = JpegConstants.MarkerStartOfFrame2,
    /// <summary>Lossless predictive coding with Huffman entropy data.</summary>
    LosslessSequential = JpegConstants.MarkerStartOfFrame3,
    /// <summary>Differential sequential DCT coding with Huffman entropy data.</summary>
    DifferentialSequentialDct = JpegConstants.MarkerStartOfFrame5,
    /// <summary>Differential progressive DCT coding with Huffman entropy data.</summary>
    DifferentialProgressiveDct = JpegConstants.MarkerStartOfFrame6,
    /// <summary>Differential lossless coding with Huffman entropy data.</summary>
    DifferentialLosslessSequential = JpegConstants.MarkerStartOfFrame7,
    /// <summary>Extended sequential DCT coding with arithmetic entropy data.</summary>
    ExtendedSequentialDctArithmetic = JpegConstants.MarkerStartOfFrame9,
    /// <summary>Progressive DCT coding with arithmetic entropy data.</summary>
    ProgressiveDctArithmetic = JpegConstants.MarkerStartOfFrame10,
    /// <summary>Lossless predictive coding with arithmetic entropy data.</summary>
    LosslessSequentialArithmetic = JpegConstants.MarkerStartOfFrame11,
    /// <summary>Differential sequential DCT coding with arithmetic entropy data.</summary>
    DifferentialSequentialDctArithmetic = JpegConstants.MarkerStartOfFrame13,
    /// <summary>Differential progressive DCT coding with arithmetic entropy data.</summary>
    DifferentialProgressiveDctArithmetic = JpegConstants.MarkerStartOfFrame14,
    /// <summary>Differential lossless coding with arithmetic entropy data.</summary>
    DifferentialLosslessSequentialArithmetic = JpegConstants.MarkerStartOfFrame15,
    /// <summary>The separate JPEG-LS coding process.</summary>
    JpegLs = JpegConstants.MarkerJpegLs
}

/// <summary>Selects Huffman or arithmetic entropy coding independently of the JPEG process.</summary>
public enum JpegEntropyCoding
{
    /// <summary>Uses the entropy coding implied by the selected JPEG process.</summary>
    Automatic = 0,
    /// <summary>Requires Huffman entropy coding.</summary>
    Huffman = 1,
    /// <summary>Requires arithmetic entropy coding.</summary>
    Arithmetic = 2
}

/// <summary>Selects one of the seven JPEG lossless prediction formulas.</summary>
public enum JpegLosslessPredictorSelection
{
    /// <summary>Uses the left-neighbor predictor.</summary>
    Left = 1,
    /// <summary>Uses the above-neighbor predictor.</summary>
    Above = 2,
    /// <summary>Uses the upper-left-neighbor predictor.</summary>
    UpperLeft = 3,
    /// <summary>Uses left plus above minus upper-left.</summary>
    LeftPlusAboveMinusUpperLeft = 4,
    /// <summary>Uses left plus one half of above minus upper-left.</summary>
    LeftPlusHalfAboveMinusUpperLeft = 5,
    /// <summary>Uses above plus one half of left minus upper-left.</summary>
    AbovePlusHalfLeftMinusUpperLeft = 6,
    /// <summary>Uses the average of the left and above neighbors.</summary>
    AverageLeftAndAbove = 7
}

/// <summary>Selects how components are grouped into sequential JPEG scans.</summary>
public enum JpegScanLayout
{
    /// <summary>Uses an interleaved scan when the selected process permits it.</summary>
    Automatic = 0,
    /// <summary>Requests component interleaving where the selected JPEG process permits it.</summary>
    Interleaved = 1,
    /// <summary>Writes one non-interleaved scan for each component in frame order.</summary>
    NonInterleaved = 2
}

/// <summary>Selects how an encoder restart-interval value is measured.</summary>
public enum JpegRestartIntervalUnit
{
    /// <summary>Measures the interval directly in encoded MCU units.</summary>
    McuBlocks = 0,
    /// <summary>Measures the interval in complete frame MCU rows before conversion to MCU units.</summary>
    McuRows = 1
}

/// <summary>Selects a documented bundle of encoder settings for a common output goal.</summary>
public enum JpegEncoderPreset
{
    /// <summary>Uses the individually supplied encoder settings.</summary>
    None = 0,
    /// <summary>Uses progressive web-oriented output with 4:2:0 sampling and redistribution-safe metadata.</summary>
    Web = 1,
    /// <summary>Uses high-quality progressive output with full chroma resolution and complete metadata preservation.</summary>
    Archival = 2,
    /// <summary>Uses baseline output, conventional 4:2:0 sampling, and canonical Huffman tables.</summary>
    MaximumCompatibility = 3,
    /// <summary>Uses compact baseline output with 4:2:0 sampling, reduced quality, optimized Huffman tables, and no optional metadata.</summary>
    MaximumCompression = 4,
    /// <summary>Preserves all metadata and reuses retained source quantization tables and sampling when available.</summary>
    MetadataPreserving = 5
}

/// <summary>Describes the channel order and byte count of a packed pixel buffer.</summary>
public enum JpegPixelFormat
{
    /// <summary>No pixel layout has been selected.</summary>
    Unknown = 0,
    /// <summary>One 8-bit luminance sample per pixel.</summary>
    L8 = 1,
    /// <summary>Three bytes per pixel in red, green, blue order.</summary>
    Rgb24 = 2,
    /// <summary>Four bytes per pixel in red, green, blue, alpha order.</summary>
    Rgba32 = 3,
    /// <summary>Three bytes per pixel in blue, green, red order.</summary>
    Bgr24 = 4,
    /// <summary>Four bytes per pixel in blue, green, red, alpha order.</summary>
    Bgra32 = 5,
    /// <summary>Four bytes per pixel in cyan, magenta, yellow, black order.</summary>
    Cmyk32 = 6,
    /// <summary>Three bytes per pixel in luma, blue-difference, red-difference order.</summary>
    YCbCr24 = 7,
    /// <summary>Four bytes per pixel in luma, blue-difference, red-difference, black order.</summary>
    Ycck32 = 8,
    /// <summary>Samples retain the stream's component order without a standard packed interpretation.</summary>
    RawComponents = 9
}

/// <summary>Identifies the interpreted color model of JPEG components or output pixels.</summary>
public enum JpegColorSpace
{
    /// <summary>The color model could not be determined.</summary>
    Unknown = 0,
    /// <summary>A single luminance or gray component.</summary>
    Grayscale = 1,
    /// <summary>Luma with blue-difference and red-difference chroma components.</summary>
    YCbCr = 2,
    /// <summary>Red, green, and blue components.</summary>
    RGB = 3,
    /// <summary>Cyan, magenta, yellow, and black components.</summary>
    CMYK = 4,
    /// <summary>YCbCr components plus a black component.</summary>
    YCCK = 5
}

/// <summary>Identifies the evidence selected while resolving JPEG color metadata.</summary>
public enum JpegColorHintSource
{
    /// <summary>No usable color evidence was available.</summary>
    Unknown = 0,
    /// <summary>The component count supplied the fallback interpretation.</summary>
    ComponentCount = 1,
    /// <summary>Recognized frame-component identifiers supplied the interpretation.</summary>
    ComponentIdentifiers = 2,
    /// <summary>A JFIF APP0 marker supplied the interpretation.</summary>
    Jfif = 3,
    /// <summary>An Exif color-space tag supplied the color characteristics.</summary>
    Exif = 4,
    /// <summary>An Adobe APP14 transform supplied the interpretation.</summary>
    Adobe = 5,
    /// <summary>Coding-independent code points supplied the interpretation or characteristics.</summary>
    Cicp = 6,
    /// <summary>An embedded ICC profile supplied the color characteristics.</summary>
    IccProfile = 7
}

/// <summary>Identifies common relationships between luma and chroma sampling density.</summary>
public enum JpegSubsampling
{
    /// <summary>The sampling relationship is unknown.</summary>
    Unknown = 0,
    /// <summary>A one-component image with no chroma sampling.</summary>
    Grayscale = 1,
    /// <summary>Full horizontal and vertical chroma resolution.</summary>
    Ratio444 = 2,
    /// <summary>Half horizontal and full vertical chroma resolution.</summary>
    Ratio422 = 3,
    /// <summary>Half horizontal and half vertical chroma resolution.</summary>
    Ratio420 = 4,
    /// <summary>Full horizontal and half vertical chroma resolution.</summary>
    Ratio440 = 5,
    /// <summary>Quarter horizontal and full vertical chroma resolution.</summary>
    Ratio411 = 6,
    /// <summary>Legal sampling factors that do not match a named preset.</summary>
    Custom = 7
}

/// <summary>Selects how source chroma samples are reduced for subsampled JPEG output.</summary>
public enum JpegChromaDownsamplingFilter
{
    /// <summary>Averages the source area covered by each output chroma sample.</summary>
    Box = 0,
    /// <summary>Uses the source sample nearest the center of each output chroma sample.</summary>
    Center = 1
}

/// <summary>Selects which metadata is retained when JPEG data is read, rewritten, or encoded.</summary>
public enum JpegMetadataPolicy
{
    /// <summary>Retains recognized and unrecognized metadata.</summary>
    PreserveAll = 0,
    /// <summary>Retains metadata formats understood by the library.</summary>
    PreserveKnown = 1,
    /// <summary>Omits all optional metadata.</summary>
    StripAll = 2,
    /// <summary>Retains metadata considered safe for ordinary redistribution.</summary>
    StripUnsafe = 3,
    /// <summary>Writes only metadata explicitly supplied for replacement.</summary>
    ReplaceSelected = 4
}

/// <summary>Selects how a requested lossless crop rectangle is resolved to JPEG MCU boundaries.</summary>
public enum JpegMcuCropMode
{
    /// <summary>Requires the requested origin and dimensions to be exactly MCU-aligned.</summary>
    Exact = 0,
    /// <summary>Moves edges inward to select only complete MCUs contained by the requested rectangle.</summary>
    Trim = 1,
    /// <summary>Moves edges outward, clamped to the image bounds, to contain the requested rectangle.</summary>
    Expand = 2
}

/// <summary>Selects how source color components are interpreted or converted.</summary>
public enum JpegColorPolicy
{
    /// <summary>Keeps the source color interpretation whenever the operation supports it.</summary>
    PreserveSource = 0,
    /// <summary>Converts decoded color data to RGB.</summary>
    ConvertToRgb = 1,
    /// <summary>Converts decoded four-component color data to CMYK.</summary>
    ConvertToCmyk = 2,
    /// <summary>Uses a supported embedded ICC profile for color conversion.</summary>
    UseIccWhenSupported = 3,
    /// <summary>Rejects data whose color interpretation cannot be handled exactly.</summary>
    FailIfUnsupported = 4
}

/// <summary>Selects the reconstruction filter used to expand subsampled chroma while reading.</summary>
public enum JpegChromaUpsamplingMode
{
    /// <summary>Replicates each chroma sample across its covered output area.</summary>
    Box = 0,
    /// <summary>Interpolates adjacent chroma samples for smoother transitions.</summary>
    Fancy = 1
}

/// <summary>Selects how a JPEG with an unknown color interpretation is returned.</summary>
public enum JpegUnknownColorHandling
{
    /// <summary>Uses the component count when a conventional interpretation is available and otherwise returns raw components.</summary>
    BestEffort = 0,
    /// <summary>Rejects a JPEG unless its color interpretation is known exactly.</summary>
    Exact = 1
}

/// <summary>Controls inversion of component samples in Adobe CMYK JPEG images.</summary>
public enum JpegAdobeInversionMode
{
    /// <summary>Uses Adobe marker metadata to determine whether CMYK samples are inverted.</summary>
    Automatic = 0,
    /// <summary>Always treats CMYK component samples as inverted.</summary>
    ForceInverted = 1,
    /// <summary>Always treats CMYK component samples as non-inverted.</summary>
    ForceNonInverted = 2
}

/// <summary>Selects the numeric range used to interpret decoded YCbCr samples.</summary>
public enum JpegYCbCrRange
{
    /// <summary>Interprets Y, Cb, and Cr over their complete eight-bit ranges.</summary>
    Full = 0,
    /// <summary>Expands studio-range YCbCr values to full range before producing output pixels.</summary>
    Limited = 1
}

/// <summary>Selects how an embedded ICC profile participates in JPEG processing.</summary>
public enum JpegIccBehaviorPolicy
{
    /// <summary>Retains profile bytes without using them to alter pixels.</summary>
    Preserve = 0,
    /// <summary>Ignores the profile for conversion and preservation decisions.</summary>
    Ignore = 1,
    /// <summary>Applies profiles supported by the built-in color converter.</summary>
    ConvertWhenSupported = 2,
    /// <summary>Rejects an embedded profile when its conversion is unsupported.</summary>
    FailIfUnsupported = 3
}

/// <summary>Identifies the rendering intent declared by an ICC profile.</summary>
public enum JpegIccRenderingIntent
{
    /// <summary>Preserves the overall visual relationship between source colors.</summary>
    Perceptual = 0,
    /// <summary>Maps source colors relative to the destination media white point.</summary>
    MediaRelativeColorimetric = 1,
    /// <summary>Prioritizes vividness and saturation over exact color relationships.</summary>
    Saturation = 2,
    /// <summary>Maps colors relative to the ICC profile-connection-space illuminant.</summary>
    IccAbsoluteColorimetric = 3,
    /// <summary>The encoded rendering-intent value is outside the ICC-defined range.</summary>
    Unknown = 4
}

/// <summary>Identifies the destination color space produced by a supported ICC conversion description.</summary>
public enum JpegIccDestinationColorSpace
{
    /// <summary>No destination color space is available.</summary>
    Unknown = 0,
    /// <summary>CIE XYZ profile-connection-space values.</summary>
    Xyz = 1
}

/// <summary>Summarizes whether the built-in ICC conversion path can use a profile.</summary>
public enum JpegIccConversionStatus
{
    /// <summary>No conversion assessment was performed.</summary>
    NotAssessed = 0,
    /// <summary>The profile has a supported matrix/TRC conversion description.</summary>
    Supported = 1,
    /// <summary>The profile is valid but the built-in conversion path cannot use it.</summary>
    Unsupported = 2
}

/// <summary>Identifies a reason an ICC profile cannot use the built-in conversion path.</summary>
public enum JpegIccConversionDiagnosticCode
{
    /// <summary>No ICC conversion problem is assigned.</summary>
    None = 0,
    /// <summary>The profile input color space is not RGB, Gray, or CMYK.</summary>
    UnsupportedColorSpace = 1,
    /// <summary>A required matrix, white-point, or tone-curve tag is absent.</summary>
    MissingRequiredTag = 2,
    /// <summary>A matrix or white-point tag does not use a supported XYZ payload.</summary>
    UnsupportedTagType = 3,
    /// <summary>A tone-curve tag does not use an identity or single-gamma curve.</summary>
    UnsupportedToneCurve = 4,
    /// <summary>The rendering-intent value is outside the ICC-defined range.</summary>
    InvalidRenderingIntent = 5
}

/// <summary>Identifies the byte order used by an Exif or MPF TIFF structure.</summary>
public enum JpegTiffByteOrder
{
    /// <summary>No valid TIFF byte-order marker was found.</summary>
    Unknown = 0,
    /// <summary>Least-significant bytes appear first.</summary>
    LittleEndian = 1,
    /// <summary>Most-significant bytes appear first.</summary>
    BigEndian = 2
}

/// <summary>Identifies the role of an Exif TIFF image-file directory.</summary>
public enum JpegExifIfdKind
{
    /// <summary>The directory role is not recognized.</summary>
    Unknown = 0,
    /// <summary>The primary image directory.</summary>
    Primary = 1,
    /// <summary>The Exif-specific subdirectory.</summary>
    Exif = 2,
    /// <summary>The GPS subdirectory.</summary>
    Gps = 3,
    /// <summary>The next-IFD thumbnail directory.</summary>
    Thumbnail = 4
}

/// <summary>Identifies a TIFF field type used by an Exif tag value.</summary>
public enum JpegExifTagType
{
    /// <summary>The TIFF field type is not recognized.</summary>
    Unknown = 0,
    /// <summary>An unsigned eight-bit integer.</summary>
    Byte = 1,
    /// <summary>A null-terminated ASCII byte sequence.</summary>
    Ascii = 2,
    /// <summary>An unsigned sixteen-bit integer.</summary>
    Short = 3,
    /// <summary>An unsigned thirty-two-bit integer.</summary>
    Long = 4,
    /// <summary>An unsigned rational value.</summary>
    Rational = 5,
    /// <summary>A signed eight-bit integer.</summary>
    SignedByte = 6,
    /// <summary>An uninterpreted byte sequence.</summary>
    Undefined = 7,
    /// <summary>A signed sixteen-bit integer.</summary>
    SignedShort = 8,
    /// <summary>A signed thirty-two-bit integer.</summary>
    SignedLong = 9,
    /// <summary>A signed rational value.</summary>
    SignedRational = 10,
    /// <summary>An IEEE 754 single-precision value retained as raw bytes.</summary>
    Float = 11,
    /// <summary>An IEEE 754 double-precision value retained as raw bytes.</summary>
    Double = 12
}

/// <summary>Selects how recoverable structural problems are handled.</summary>
public enum JpegErrorPolicy
{
    /// <summary>Rejects malformed data at the first standards violation.</summary>
    Strict = 0,
    /// <summary>Continues through supported recoverable problems and records diagnostics.</summary>
    Tolerant = 1
}

/// <summary>Selects the integer inverse-DCT implementation used to reconstruct samples.</summary>
public enum JpegInverseDctMethod
{
    /// <summary>Uses the higher-accuracy integer transform.</summary>
    Accurate = 0,
    /// <summary>Uses a faster approximation with potentially different rounding.</summary>
    Fast = 1
}

/// <summary>Selects one of the reduced inverse-DCT output sizes supported for each eight-by-eight block.</summary>
public enum JpegIdctScaleRatio
{
    /// <summary>Selects a ratio from target dimensions or the explicit numerator and denominator.</summary>
    Automatic = 0,
    /// <summary>Produces one output sample per block axis.</summary>
    OneEighth = 1,
    /// <summary>Produces two output samples per block axis.</summary>
    OneQuarter = 2,
    /// <summary>Produces three output samples per block axis.</summary>
    ThreeEighths = 3,
    /// <summary>Produces four output samples per block axis.</summary>
    OneHalf = 4,
    /// <summary>Produces five output samples per block axis.</summary>
    FiveEighths = 5,
    /// <summary>Produces six output samples per block axis.</summary>
    ThreeQuarters = 6,
    /// <summary>Produces seven output samples per block axis.</summary>
    SevenEighths = 7,
    /// <summary>Produces all eight output samples per block axis.</summary>
    Full = 8
}

/// <summary>Ranks the effect of a JPEG diagnostic.</summary>
public enum JpegDiagnosticSeverity
{
    /// <summary>Describes a condition that does not imply damage.</summary>
    Info = 0,
    /// <summary>Describes a recoverable or suspicious condition.</summary>
    Warning = 1,
    /// <summary>Describes a condition that prevents the requested result.</summary>
    Error = 2
}

/// <summary>Provides stable categories for diagnostics independent of their human-readable messages.</summary>
public enum JpegDiagnosticCode
{
    /// <summary>No diagnostic category is assigned.</summary>
    None = 0,
    /// <summary>A marker prefix or marker code is invalid.</summary>
    InvalidMarker = 1,
    /// <summary>A marker segment declares an invalid length.</summary>
    InvalidMarkerLength = 2,
    /// <summary>A recognized marker appears in an invalid order.</summary>
    InvalidMarkerOrder = 3,
    /// <summary>The JPEG coding process is recognized but unsupported.</summary>
    UnsupportedProcess = 4,
    /// <summary>The component color interpretation is unsupported.</summary>
    UnsupportedColorSpace = 5,
    /// <summary>A configured allocation or parsing limit was exceeded.</summary>
    ResourceLimitExceeded = 6,
    /// <summary>The input ended before the current structure was complete.</summary>
    TruncatedData = 7,
    /// <summary>A metadata payload is malformed or incomplete.</summary>
    CorruptMetadata = 8
}

/// <summary>Identifies a validation or assembly problem in an Extended XMP chunk set.</summary>
public enum JpegXmpReassemblyDiagnosticCode
{
    /// <summary>No Extended XMP reassembly problem is assigned.</summary>
    None = 0,
    /// <summary>A chunk GUID is not a 32-character hexadecimal identifier.</summary>
    InvalidGuid = 1,
    /// <summary>Chunks do not share one GUID.</summary>
    GuidMismatch = 2,
    /// <summary>A declared full packet length is invalid.</summary>
    InvalidFullLength = 3,
    /// <summary>Chunks do not share one declared full packet length.</summary>
    FullLengthMismatch = 4,
    /// <summary>A chunk offset or extent falls outside the declared packet.</summary>
    InvalidOffset = 5,
    /// <summary>Two or more chunks cover at least one common packet byte.</summary>
    Overlap = 6,
    /// <summary>The chunks leave at least one packet byte uncovered.</summary>
    Gap = 7
}

/// <summary>Identifies the two-byte marker codes used by classic JPEG streams.</summary>
public enum JpegMarker
{
    /// <summary>No recognized JPEG marker.</summary>
    Unknown = 0,
    /// <summary>The temporary marker reserved by the JPEG specification.</summary>
    Temporary = JpegConstants.MarkerTemporary,
    /// <summary>The baseline DCT start-of-frame marker.</summary>
    StartOfFrame0 = JpegConstants.MarkerStartOfFrame0,
    /// <summary>The extended sequential DCT start-of-frame marker.</summary>
    StartOfFrame1 = JpegConstants.MarkerStartOfFrame1,
    /// <summary>The progressive DCT start-of-frame marker.</summary>
    StartOfFrame2 = JpegConstants.MarkerStartOfFrame2,
    /// <summary>The lossless Huffman start-of-frame marker.</summary>
    StartOfFrame3 = JpegConstants.MarkerStartOfFrame3,
    /// <summary>The marker that defines one or more Huffman tables.</summary>
    DefineHuffmanTable = JpegConstants.MarkerDefineHuffmanTable,
    /// <summary>The differential sequential DCT start-of-frame marker.</summary>
    StartOfFrame5 = JpegConstants.MarkerStartOfFrame5,
    /// <summary>The differential progressive DCT start-of-frame marker.</summary>
    StartOfFrame6 = JpegConstants.MarkerStartOfFrame6,
    /// <summary>The differential lossless start-of-frame marker.</summary>
    StartOfFrame7 = JpegConstants.MarkerStartOfFrame7,
    /// <summary>The JPEG extension marker.</summary>
    JpegExtension = JpegConstants.MarkerJpegExtension,
    /// <summary>The arithmetic extended sequential DCT start-of-frame marker.</summary>
    StartOfFrame9 = JpegConstants.MarkerStartOfFrame9,
    /// <summary>The arithmetic progressive DCT start-of-frame marker.</summary>
    StartOfFrame10 = JpegConstants.MarkerStartOfFrame10,
    /// <summary>The arithmetic lossless start-of-frame marker.</summary>
    StartOfFrame11 = JpegConstants.MarkerStartOfFrame11,
    /// <summary>The marker that defines arithmetic conditioning tables.</summary>
    DefineArithmeticConditioning = JpegConstants.MarkerDefineArithmeticConditioning,
    /// <summary>The arithmetic differential sequential DCT start-of-frame marker.</summary>
    StartOfFrame13 = JpegConstants.MarkerStartOfFrame13,
    /// <summary>The arithmetic differential progressive DCT start-of-frame marker.</summary>
    StartOfFrame14 = JpegConstants.MarkerStartOfFrame14,
    /// <summary>The arithmetic differential lossless start-of-frame marker.</summary>
    StartOfFrame15 = JpegConstants.MarkerStartOfFrame15,
    /// <summary>Restart marker number zero.</summary>
    Restart0 = JpegConstants.MarkerRestart0,
    /// <summary>Restart marker number one.</summary>
    Restart1 = JpegConstants.MarkerRestart1,
    /// <summary>Restart marker number two.</summary>
    Restart2 = JpegConstants.MarkerRestart2,
    /// <summary>Restart marker number three.</summary>
    Restart3 = JpegConstants.MarkerRestart3,
    /// <summary>Restart marker number four.</summary>
    Restart4 = JpegConstants.MarkerRestart4,
    /// <summary>Restart marker number five.</summary>
    Restart5 = JpegConstants.MarkerRestart5,
    /// <summary>Restart marker number six.</summary>
    Restart6 = JpegConstants.MarkerRestart6,
    /// <summary>Restart marker number seven.</summary>
    Restart7 = JpegConstants.MarkerRestart7,
    /// <summary>The marker that begins a JPEG datastream.</summary>
    StartOfImage = JpegConstants.MarkerStartOfImage,
    /// <summary>The marker that ends a JPEG datastream.</summary>
    EndOfImage = JpegConstants.MarkerEndOfImage,
    /// <summary>The marker that defines scan components and begins entropy-coded data.</summary>
    StartOfScan = JpegConstants.MarkerStartOfScan,
    /// <summary>The marker that defines one or more quantization tables.</summary>
    DefineQuantizationTable = JpegConstants.MarkerDefineQuantizationTable,
    /// <summary>The marker that supplies a frame height omitted from the frame header.</summary>
    DefineNumberOfLines = JpegConstants.MarkerDefineNumberOfLines,
    /// <summary>The marker that defines the interval between restart markers.</summary>
    DefineRestartInterval = JpegConstants.MarkerDefineRestartInterval,
    /// <summary>The marker that begins hierarchical progression.</summary>
    DefineHierarchicalProgression = JpegConstants.MarkerDefineHierarchicalProgression,
    /// <summary>The marker that expands reference components in hierarchical coding.</summary>
    ExpandReferenceComponents = JpegConstants.MarkerExpandReferenceComponents,
    /// <summary>Application marker zero, commonly used for JFIF and JFXX.</summary>
    App0 = JpegConstants.MarkerApp0,
    /// <summary>Application marker one, commonly used for Exif and XMP.</summary>
    App1 = JpegConstants.MarkerApp1,
    /// <summary>Application marker two, commonly used for ICC profiles and MPF.</summary>
    App2 = JpegConstants.MarkerApp2,
    /// <summary>Application marker three.</summary>
    App3 = JpegConstants.MarkerApp3,
    /// <summary>Application marker four.</summary>
    App4 = JpegConstants.MarkerApp4,
    /// <summary>Application marker five.</summary>
    App5 = JpegConstants.MarkerApp5,
    /// <summary>Application marker six.</summary>
    App6 = JpegConstants.MarkerApp6,
    /// <summary>Application marker seven.</summary>
    App7 = JpegConstants.MarkerApp7,
    /// <summary>Application marker eight.</summary>
    App8 = JpegConstants.MarkerApp8,
    /// <summary>Application marker nine.</summary>
    App9 = JpegConstants.MarkerApp9,
    /// <summary>Application marker ten.</summary>
    App10 = JpegConstants.MarkerApp10,
    /// <summary>Application marker eleven.</summary>
    App11 = JpegConstants.MarkerApp11,
    /// <summary>Application marker twelve.</summary>
    App12 = JpegConstants.MarkerApp12,
    /// <summary>Application marker thirteen, commonly used for Photoshop resources.</summary>
    App13 = JpegConstants.MarkerApp13,
    /// <summary>Application marker fourteen, commonly used for Adobe color-transform information.</summary>
    App14 = JpegConstants.MarkerApp14,
    /// <summary>Application marker fifteen.</summary>
    App15 = JpegConstants.MarkerApp15,
    /// <summary>The JPEG-LS start-of-frame marker.</summary>
    JpegLs = JpegConstants.MarkerJpegLs,
    /// <summary>The marker containing a textual JPEG comment.</summary>
    Comment = JpegConstants.MarkerComment
}
