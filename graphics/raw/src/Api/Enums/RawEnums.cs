namespace raw;

/// <summary>Identifies the raw file or container family.</summary>
public enum RawFormat
{
    /// <summary>The format has not been identified.</summary>
    Unknown = 0,
    /// <summary>A descriptor-driven headerless raw sample stream.</summary>
    GenericRaw = 1,
    /// <summary>A TIFF or BigTIFF container without a more specific raw identity.</summary>
    Tiff = 2,
    /// <summary>A Canon CR2 container.</summary>
    Cr2 = 3,
    /// <summary>A Nikon NEF container.</summary>
    Nef = 4
}

/// <summary>Describes the highest verified operation available for a format or profile.</summary>
public enum RawSupportLevel
{
    /// <summary>No safe operation is available.</summary>
    Unsupported = 0,
    /// <summary>The format and basic identity can be recognized.</summary>
    Recognized = 1,
    /// <summary>Container structure, metadata, previews, and payload descriptions can be inspected.</summary>
    Inspected = 2,
    /// <summary>Stored raw samples can be reconstructed.</summary>
    Decoded = 3,
    /// <summary>A documented displayable or linear image can be rendered.</summary>
    Rendered = 4,
    /// <summary>Supported edits can preserve unknown data and undecoded payloads.</summary>
    PreservingRewrite = 5,
    /// <summary>A new payload and container can be emitted for a validated profile.</summary>
    Encoded = 6,
    /// <summary>Encoded output has passed the compatibility checks named by its profile.</summary>
    VendorConfirmed = 7
}

/// <summary>Specifies the importance of a diagnostic or validation message.</summary>
public enum RawSeverity
{
    /// <summary>Informational context that does not indicate a problem.</summary>
    Information = 0,
    /// <summary>A recoverable or compatibility-relevant condition.</summary>
    Warning = 1,
    /// <summary>A condition that prevents the requested operation.</summary>
    Error = 2
}

/// <summary>Specifies byte order for multi-byte values.</summary>
public enum RawByteOrder
{
    /// <summary>The least significant byte is stored first.</summary>
    LittleEndian = 0,
    /// <summary>The most significant byte is stored first.</summary>
    BigEndian = 1
}

/// <summary>Identifies the public typed representation of a TIFF or Exif field.</summary>
public enum RawTagType
{
    /// <summary>An unsigned 8-bit integer.</summary>
    Byte = 1,
    /// <summary>A null-terminated 7-bit ASCII string.</summary>
    Ascii = 2,
    /// <summary>An unsigned 16-bit integer.</summary>
    Short = 3,
    /// <summary>An unsigned 32-bit integer.</summary>
    Long = 4,
    /// <summary>A pair of unsigned 32-bit integers.</summary>
    Rational = 5,
    /// <summary>A signed 8-bit integer.</summary>
    SignedByte = 6,
    /// <summary>An unconstrained 8-bit value.</summary>
    Undefined = 7,
    /// <summary>A signed 16-bit integer.</summary>
    SignedShort = 8,
    /// <summary>A signed 32-bit integer.</summary>
    SignedLong = 9,
    /// <summary>A pair of signed 32-bit integers.</summary>
    SignedRational = 10,
    /// <summary>An IEEE 754 single-precision value.</summary>
    Float = 11,
    /// <summary>An IEEE 754 double-precision value.</summary>
    Double = 12,
    /// <summary>A classic TIFF IFD offset.</summary>
    Ifd = 13,
    /// <summary>An unsigned 64-bit BigTIFF integer.</summary>
    Long8 = 16,
    /// <summary>A signed 64-bit BigTIFF integer.</summary>
    SignedLong8 = 17,
    /// <summary>A BigTIFF IFD offset.</summary>
    Ifd8 = 18,
    /// <summary>An Exif 3 UTF-8 string terminated by a null byte.</summary>
    Utf8 = 129
}

/// <summary>Identifies the semantic role of a public TIFF directory.</summary>
public enum RawIfdKind
{
    /// <summary>The directory role is not known.</summary>
    Unknown = 0,
    /// <summary>A primary or chained TIFF image directory.</summary>
    Image = 1,
    /// <summary>A TIFF SubIFD.</summary>
    SubIfd = 2,
    /// <summary>An Exif attribute directory.</summary>
    Exif = 3,
    /// <summary>A GPS attribute directory.</summary>
    Gps = 4,
    /// <summary>An Exif interoperability directory.</summary>
    Interoperability = 5
}

/// <summary>Identifies a character encoding used by an Exif textual value.</summary>
public enum RawTextEncoding
{
    /// <summary>The encoding prefix or field type is not recognized.</summary>
    Unknown = 0,
    /// <summary>Seven-bit ASCII.</summary>
    Ascii = 1,
    /// <summary>Japanese JIS X 0208 represented by the platform ISO-2022-JP codec when available.</summary>
    Jis = 2,
    /// <summary>The Exif Unicode designation, encoded as UTF-8 by Exif 3 and read compatibly from legacy UTF-16.</summary>
    Unicode = 3,
    /// <summary>Unicode UTF-8 without a byte-order mark.</summary>
    Utf8 = 4
}

/// <summary>Identifies the byte encoding of an XMP packet.</summary>
public enum RawXmpEncoding
{
    /// <summary>UTF-8.</summary>
    Utf8 = 0,
    /// <summary>Big-endian UTF-16.</summary>
    Utf16BigEndian = 1,
    /// <summary>Little-endian UTF-16.</summary>
    Utf16LittleEndian = 2,
    /// <summary>Big-endian UTF-32.</summary>
    Utf32BigEndian = 3,
    /// <summary>Little-endian UTF-32.</summary>
    Utf32LittleEndian = 4
}

/// <summary>Identifies the semantic value form of an IPTC IIM DataSet.</summary>
public enum RawIptcValueKind
{
    /// <summary>The value is retained as uninterpreted binary bytes.</summary>
    Binary = 0,
    /// <summary>The value is character text.</summary>
    Text = 1,
    /// <summary>The value is an unsigned big-endian integer.</summary>
    UnsignedInteger = 2,
    /// <summary>The value is an eight-digit IPTC date.</summary>
    Date = 3,
    /// <summary>The value is an IPTC time with an optional UTC offset.</summary>
    Time = 4
}

/// <summary>Identifies the IPTC photo metadata schema represented by an XMP field.</summary>
public enum RawIptcSchema
{
    /// <summary>The field belongs to the IPTC Core schema.</summary>
    Core = 0,
    /// <summary>The field belongs to the IPTC Extension schema.</summary>
    Extension = 1
}

/// <summary>Identifies the representation used by an extracted ICC tone reproduction curve.</summary>
public enum RawIccCurveKind
{
    /// <summary>The curve is the identity function.</summary>
    Identity = 0,
    /// <summary>The curve is represented by one gamma exponent.</summary>
    Gamma = 1,
    /// <summary>The curve is represented by uniformly spaced samples.</summary>
    Sampled = 2,
    /// <summary>The curve uses one of the ICC parametric function forms.</summary>
    Parametric = 3
}

/// <summary>Selects an ICC rendering intent for profile transforms.</summary>
public enum RawIccRenderingIntent
{
    /// <summary>Use the profile's perceptual transform.</summary>
    Perceptual = 0,
    /// <summary>Preserve colors relative to the destination media white.</summary>
    MediaRelativeColorimetric = 1,
    /// <summary>Use the profile's saturation-preserving transform.</summary>
    Saturation = 2,
    /// <summary>Preserve colors relative to the ICC profile connection-space illuminant.</summary>
    IccAbsoluteColorimetric = 3
}

/// <summary>Specifies the order in which bits are consumed within a byte stream.</summary>
public enum RawBitOrder
{
    /// <summary>The most significant bit is consumed first.</summary>
    MostSignificantBitFirst = 0,
    /// <summary>The least significant bit is consumed first.</summary>
    LeastSignificantBitFirst = 1
}

/// <summary>Identifies the numeric representation of a stored sample.</summary>
public enum RawSampleKind
{
    /// <summary>An unsigned integer sample.</summary>
    UnsignedInteger = 0,
    /// <summary>A signed two's-complement integer sample.</summary>
    SignedInteger = 1,
    /// <summary>An IEEE 754 half-precision floating-point sample.</summary>
    Half = 2,
    /// <summary>An IEEE 754 single-precision floating-point sample.</summary>
    Single = 3
}

/// <summary>Identifies how raw samples are packed into bytes or words.</summary>
public enum RawPacking
{
    /// <summary>Each sample occupies a complete stored word.</summary>
    ByteAligned = 0,
    /// <summary>Samples form a continuous caller-specified bit stream.</summary>
    PackedBits = 1,
    /// <summary>The common four-pixel, five-byte RAW10 group layout.</summary>
    Raw10 = 2,
    /// <summary>The common two-pixel, three-byte RAW12 group layout.</summary>
    Raw12 = 3,
    /// <summary>The common four-pixel, seven-byte RAW14 group layout.</summary>
    Raw14 = 4
}

/// <summary>Identifies how multiple channels are arranged in a generic raw payload.</summary>
public enum RawSampleLayout
{
    /// <summary>All channel samples for each pixel are stored together.</summary>
    Interleaved = 0,
    /// <summary>Each channel is stored in a separate plane.</summary>
    Planar = 1
}

/// <summary>Identifies the role of an image exposed by a raw document.</summary>
public enum RawImageKind
{
    /// <summary>The image role is not known.</summary>
    Unknown = 0,
    /// <summary>The primary or secondary sensor-domain raw image.</summary>
    Raw = 1,
    /// <summary>A full-size or reduced display preview.</summary>
    Preview = 2,
    /// <summary>A small embedded thumbnail.</summary>
    Thumbnail = 3,
    /// <summary>An auxiliary image such as depth or dual-pixel data.</summary>
    Auxiliary = 4,
    /// <summary>A transparency or diagnostic mask image.</summary>
    Mask = 5
}

/// <summary>Identifies the semantic purpose of a rectangular image area.</summary>
public enum RawAreaKind
{
    /// <summary>The area purpose is not known.</summary>
    Unknown = 0,
    /// <summary>The complete stored image bounds.</summary>
    StoredImage = 1,
    /// <summary>The sensor area containing usable image samples.</summary>
    Active = 2,
    /// <summary>The camera-recommended default crop.</summary>
    DefaultCrop = 3,
    /// <summary>A caller-selected non-destructive crop.</summary>
    UserCrop = 4,
    /// <summary>A masked area excluded from visible output.</summary>
    Masked = 5,
    /// <summary>A masked area intended for optical-black estimation.</summary>
    OpticalBlack = 6
}

/// <summary>Identifies a sensor defect repaired before demosaic.</summary>
public enum RawDefectKind
{
    /// <summary>The defect category is not defined.</summary>
    Unknown = 0,
    /// <summary>One explicitly listed bad sample.</summary>
    Pixel = 1,
    /// <summary>One conservatively detected isolated high sample.</summary>
    HotPixel = 2,
    /// <summary>One conservatively detected isolated low sample.</summary>
    DeadPixel = 3,
    /// <summary>One explicitly listed defective row.</summary>
    Row = 4,
    /// <summary>One explicitly listed defective column.</summary>
    Column = 5
}

/// <summary>Identifies where a sensor defect declaration originated.</summary>
public enum RawDefectSource
{
    /// <summary>The defect source is not defined.</summary>
    Unknown = 0,
    /// <summary>The input metadata declared the defect.</summary>
    Metadata = 1,
    /// <summary>The caller declared the defect.</summary>
    Caller = 2,
    /// <summary>The conservative detector declared the defect.</summary>
    Detected = 3
}

/// <summary>Identifies the values stored in a raw diagnostic plane.</summary>
public enum RawDiagnosticPlaneKind
{
    /// <summary>The diagnostic plane kind is not defined.</summary>
    Unknown = 0,
    /// <summary>Normalized sensor sample values for one logical channel.</summary>
    NormalizedSample = 1,
    /// <summary>Black-clipping mask values for one logical channel.</summary>
    BlackClipped = 2,
    /// <summary>White-clipping mask values for one logical channel.</summary>
    WhiteClipped = 3,
    /// <summary>Defect-repair mask values for one logical channel.</summary>
    Repaired = 4,
    /// <summary>Zero-based repeating CFA phase identifiers.</summary>
    CfaPhase = 5
}

/// <summary>Identifies the encoded representation of a thumbnail.</summary>
public enum RawThumbnailKind
{
    /// <summary>The thumbnail representation is not known.</summary>
    Unknown = 0,
    /// <summary>A baseline JPEG byte stream.</summary>
    Jpeg = 1,
    /// <summary>A TIFF-encoded thumbnail.</summary>
    Tiff = 2,
    /// <summary>Uncompressed pixel-interleaved RGB bytes.</summary>
    Rgb8 = 3,
    /// <summary>Uncompressed grayscale bytes.</summary>
    Gray8 = 4,
    /// <summary>An encoded representation retained without a decoder.</summary>
    Opaque = 5
}

/// <summary>Identifies the semantic content of a mapped input payload.</summary>
public enum RawPayloadKind
{
    /// <summary>The payload purpose is not known.</summary>
    Unknown = 0,
    /// <summary>A sensor-domain raw image payload.</summary>
    RawImage = 1,
    /// <summary>A display preview payload.</summary>
    Preview = 2,
    /// <summary>A thumbnail payload.</summary>
    Thumbnail = 3,
    /// <summary>A standard metadata payload.</summary>
    Metadata = 4,
    /// <summary>A camera maker-note payload.</summary>
    MakerNote = 5,
    /// <summary>An XMP packet.</summary>
    Xmp = 6,
    /// <summary>An IPTC payload.</summary>
    Iptc = 7,
    /// <summary>An embedded ICC profile.</summary>
    IccProfile = 8,
    /// <summary>An unknown block retained as opaque bytes.</summary>
    Opaque = 9,
    /// <summary>A vendor authenticity, signature, digest, or original-decision payload retained without re-signing.</summary>
    Authenticity = 10
}

/// <summary>Identifies a standard calibration illuminant for a color profile.</summary>
public enum RawColorIlluminant
{
    /// <summary>The calibration illuminant is not known.</summary>
    Unknown = 0,
    /// <summary>CIE standard illuminant A.</summary>
    StandardLightA = 1,
    /// <summary>CIE daylight illuminant D50.</summary>
    D50 = 2,
    /// <summary>CIE daylight illuminant D55.</summary>
    D55 = 3,
    /// <summary>CIE daylight illuminant D65.</summary>
    D65 = 4,
    /// <summary>CIE daylight illuminant D75.</summary>
    D75 = 5,
    /// <summary>A caller- or profile-specified correlated color temperature.</summary>
    Custom = 6,
    /// <summary>CIE daylight illuminant D60 used by ACES working spaces.</summary>
    D60 = 7
}

/// <summary>Identifies the semantic role of a generic raw channel.</summary>
public enum RawChannelKind
{
    /// <summary>The channel role is not known.</summary>
    Unknown = 0,
    /// <summary>A grayscale or luminance channel.</summary>
    Gray = 1,
    /// <summary>A red channel.</summary>
    Red = 2,
    /// <summary>A green channel.</summary>
    Green = 3,
    /// <summary>A blue channel.</summary>
    Blue = 4,
    /// <summary>An alpha channel.</summary>
    Alpha = 5,
    /// <summary>A cyan channel.</summary>
    Cyan = 6,
    /// <summary>A magenta channel.</summary>
    Magenta = 7,
    /// <summary>A yellow channel.</summary>
    Yellow = 8,
    /// <summary>A clear or white channel.</summary>
    White = 9,
    /// <summary>A caller-defined channel whose name carries its meaning.</summary>
    Custom = 10
}

/// <summary>Identifies a known payload compression family.</summary>
public enum RawCompression
{
    /// <summary>The compression identifier is absent or unknown.</summary>
    Unknown = 0,
    /// <summary>The payload is uncompressed.</summary>
    None = 1,
    /// <summary>TIFF PackBits compression.</summary>
    PackBits = 2,
    /// <summary>TIFF LZW compression.</summary>
    Lzw = 3,
    /// <summary>TIFF Deflate compression.</summary>
    Deflate = 4,
    /// <summary>TIFF Adobe Deflate compression.</summary>
    AdobeDeflate = 5,
    /// <summary>Baseline sequential Huffman JPEG compression.</summary>
    BaselineJpeg = 6,
    /// <summary>Predictive lossless JPEG compression.</summary>
    LosslessJpeg = 7,
    /// <summary>A supported Nikon lossless Huffman and delta stream.</summary>
    NikonLossless = 8,
    /// <summary>A supported Nikon curve-quantized lossy stream.</summary>
    NikonLossy = 9,
    /// <summary>An unsupported Nikon HE stream.</summary>
    NikonHe = 10,
    /// <summary>An unsupported Nikon HE Star stream.</summary>
    NikonHeStar = 11
}

/// <summary>Identifies the interpretation of stored image samples.</summary>
public enum RawPhotometricInterpretation
{
    /// <summary>The interpretation is unknown or profile-specific.</summary>
    Unknown = 0,
    /// <summary>Zero represents white in a grayscale image.</summary>
    WhiteIsZero = 1,
    /// <summary>Zero represents black in a grayscale image.</summary>
    BlackIsZero = 2,
    /// <summary>Samples represent red, green, and blue channels.</summary>
    Rgb = 3,
    /// <summary>Samples index a color palette.</summary>
    Palette = 4,
    /// <summary>Samples represent luminance and chroma components.</summary>
    YCbCr = 5,
    /// <summary>Samples represent a color filter array mosaic.</summary>
    ColorFilterArray = 6,
    /// <summary>Samples are already linear raw channels.</summary>
    LinearRaw = 7
}

/// <summary>Identifies a color at a color filter array position.</summary>
public enum RawCfaColor
{
    /// <summary>The color is unknown.</summary>
    Unknown = 0,
    /// <summary>A red filter position.</summary>
    Red = 1,
    /// <summary>The first or only green filter position.</summary>
    Green = 2,
    /// <summary>A blue filter position.</summary>
    Blue = 3,
    /// <summary>A second independently calibrated green filter position.</summary>
    Green2 = 4,
    /// <summary>A cyan filter position.</summary>
    Cyan = 5,
    /// <summary>A magenta filter position.</summary>
    Magenta = 6,
    /// <summary>A yellow filter position.</summary>
    Yellow = 7,
    /// <summary>A clear or white filter position.</summary>
    White = 8
}

/// <summary>Identifies a standard color filter array layout.</summary>
public enum RawCfaLayout
{
    /// <summary>The image does not use a color filter array.</summary>
    None = 0,
    /// <summary>A Bayer layout whose first row is red and green.</summary>
    Rggb = 1,
    /// <summary>A Bayer layout whose first row is blue and green.</summary>
    Bggr = 2,
    /// <summary>A Bayer layout whose first row is green and red.</summary>
    Grbg = 3,
    /// <summary>A Bayer layout whose first row is green and blue.</summary>
    Gbrg = 4,
    /// <summary>A caller-supplied arbitrary repeating layout.</summary>
    Arbitrary = 5
}

/// <summary>Specifies the TIFF/Exif orientation of an image.</summary>
public enum RawOrientation
{
    /// <summary>The orientation is not known.</summary>
    Unknown = 0,
    /// <summary>Rows run top to bottom and columns run left to right.</summary>
    Normal = 1,
    /// <summary>The image is mirrored horizontally.</summary>
    MirrorHorizontal = 2,
    /// <summary>The image is rotated 180 degrees.</summary>
    Rotate180 = 3,
    /// <summary>The image is mirrored vertically.</summary>
    MirrorVertical = 4,
    /// <summary>The image is transposed across the top-left to bottom-right axis.</summary>
    Transpose = 5,
    /// <summary>The image is rotated 90 degrees clockwise.</summary>
    Rotate90Clockwise = 6,
    /// <summary>The image is transposed across the top-right to bottom-left axis.</summary>
    Transverse = 7,
    /// <summary>The image is rotated 270 degrees clockwise.</summary>
    Rotate270Clockwise = 8
}

/// <summary>Selects a demosaic algorithm.</summary>
public enum RawDemosaicMode
{
    /// <summary>Return mosaic or already-linear samples without demosaicing.</summary>
    None = 0,
    /// <summary>Use nearest-neighbor reconstruction.</summary>
    NearestNeighbor = 1,
    /// <summary>Use bilinear reconstruction.</summary>
    Bilinear = 2,
    /// <summary>Use edge-aware variable-number-of-gradients reconstruction.</summary>
    Vng = 3,
    /// <summary>Use adaptive homogeneity-directed reconstruction.</summary>
    Ahd = 4
}

/// <summary>Selects a managed post-demosaic noise-reduction method.</summary>
public enum RawDenoiseMode
{
    /// <summary>Do not apply post-demosaic noise reduction.</summary>
    None = 0,
    /// <summary>Replace isolated channel spikes using a local median.</summary>
    HotPixel = 1,
    /// <summary>Median-filter chroma while preserving scene-linear luminance.</summary>
    MedianChroma = 2,
    /// <summary>Soft-threshold luminance and chroma detail using an edge-aware wavelet-like pass.</summary>
    WaveletLike = 3
}

/// <summary>Selects how normalized RGB values outside the output gamut are handled.</summary>
public enum RawGamutMode
{
    /// <summary>Preserve finite out-of-gamut floating-point values.</summary>
    None = 0,
    /// <summary>Clamp every color channel independently to the normalized output gamut.</summary>
    Clip = 1,
    /// <summary>Scale color differences toward luminance until every channel enters the normalized output gamut.</summary>
    Compress = 2
}

/// <summary>Selects the source of white-balance multipliers.</summary>
public enum RawWhiteBalanceMode
{
    /// <summary>Do not apply white balance.</summary>
    None = 0,
    /// <summary>Use the camera's as-shot values.</summary>
    Camera = 1,
    /// <summary>Use profile daylight values.</summary>
    Daylight = 2,
    /// <summary>Estimate white balance from image content.</summary>
    Auto = 3,
    /// <summary>Use caller-supplied channel multipliers.</summary>
    UserMultipliers = 4,
    /// <summary>Use a caller-supplied temperature and tint.</summary>
    TemperatureAndTint = 5
}

/// <summary>Selects highlight handling during rendering.</summary>
public enum RawHighlightMode
{
    /// <summary>Clip values at the output boundary.</summary>
    Clip = 0,
    /// <summary>Blend clipped channels with available channel detail.</summary>
    Blend = 1,
    /// <summary>Attempt to reconstruct clipped color detail.</summary>
    Reconstruct = 2
}

/// <summary>Identifies a working or output color space.</summary>
public enum RawColorSpace
{
    /// <summary>The camera's untransformed native color space.</summary>
    RawCamera = 0,
    /// <summary>CIE XYZ with a D50 white point.</summary>
    XyzD50 = 1,
    /// <summary>CIE XYZ with a D65 white point.</summary>
    XyzD65 = 2,
    /// <summary>The sRGB color primaries.</summary>
    Srgb = 3,
    /// <summary>The Display P3 color primaries.</summary>
    DisplayP3 = 4,
    /// <summary>The Adobe RGB color primaries.</summary>
    AdobeRgb = 5,
    /// <summary>The ProPhoto RGB color primaries.</summary>
    ProPhotoRgb = 6,
    /// <summary>The Rec. 2020 color primaries.</summary>
    Rec2020 = 7,
    /// <summary>The ACEScg color primaries.</summary>
    AcesCg = 8,
    /// <summary>RGB values encoded by an accompanying ICC profile.</summary>
    IccRgb = 9
}

/// <summary>Selects the chromatic adaptation transform used between XYZ white points.</summary>
public enum RawChromaticAdaptationMode
{
    /// <summary>Do not adapt XYZ values between source and destination white points.</summary>
    None = 0,
    /// <summary>Use the Bradford cone-response transform.</summary>
    Bradford = 1,
    /// <summary>Use the CIECAT02 cone-response transform.</summary>
    Cat02 = 2
}

/// <summary>Identifies the transfer function applied to color values.</summary>
public enum RawTransferFunction
{
    /// <summary>No nonlinear transfer function is applied.</summary>
    Linear = 0,
    /// <summary>The standard sRGB transfer function.</summary>
    Srgb = 1,
    /// <summary>A caller-configured power-law gamma function.</summary>
    Gamma = 2,
    /// <summary>The Rec. 2020 transfer function.</summary>
    Rec2020 = 3,
    /// <summary>The ProPhoto RGB transfer function.</summary>
    ProPhotoRgb = 4,
    /// <summary>The tone reproduction curves from an accompanying ICC profile.</summary>
    Icc = 5
}

/// <summary>Selects an image resampling kernel.</summary>
public enum RawResampler
{
    /// <summary>Use the nearest source sample.</summary>
    NearestNeighbor = 0,
    /// <summary>Use bilinear interpolation.</summary>
    Bilinear = 1,
    /// <summary>Use bicubic interpolation.</summary>
    Bicubic = 2,
    /// <summary>Use Lanczos interpolation.</summary>
    Lanczos = 3
}

/// <summary>Selects the kind of write operation.</summary>
public enum RawWriteMode
{
    /// <summary>Create a new descriptor-driven generic raw payload.</summary>
    GenericRaw = 0,
    /// <summary>Create a new profile-backed container.</summary>
    NewContainer = 1,
    /// <summary>Rewrite a container while preserving supported opaque data.</summary>
    PreservingRewrite = 2,
    /// <summary>Apply an eligible fixed-size metadata patch.</summary>
    MetadataPatch = 3
}

/// <summary>Specifies how unknown or private data is handled during writing.</summary>
public enum RawUnknownDataPolicy
{
    /// <summary>Preserve unknown data and reject the write when preservation is impossible.</summary>
    Preserve = 0,
    /// <summary>Reject input containing unknown data relevant to the requested write.</summary>
    Reject = 1,
    /// <summary>Allow unknown data to be dropped after explicit data-loss consent.</summary>
    DropWithConsent = 2
}

/// <summary>Specifies how normalized generic raw values are quantized to integers.</summary>
public enum RawSampleRoundingMode
{
    /// <summary>Round to the nearest integer, with exact midpoints rounded upward in the encoded domain.</summary>
    Nearest = 0,
    /// <summary>Discard the fractional encoded-domain component.</summary>
    TowardZero = 1
}

/// <summary>Specifies how normalized values outside their permitted range are handled.</summary>
public enum RawSampleRangePolicy
{
    /// <summary>Reject values outside the permitted range.</summary>
    Reject = 0,
    /// <summary>Clamp values to the nearest permitted endpoint.</summary>
    Clamp = 1
}

/// <summary>Specifies how normalized values map to a signed integer descriptor.</summary>
public enum RawSignedEncodingPolicy
{
    /// <summary>Map zero through one across the complete declared signed range.</summary>
    FullRange = 0,
    /// <summary>Map zero through one from zero through the declared positive maximum.</summary>
    NonNegative = 1
}

/// <summary>Specifies how NaN and infinity are handled while encoding floating samples.</summary>
public enum RawNonFinitePolicy
{
    /// <summary>Preserve non-finite values when the destination is floating point.</summary>
    Preserve = 0,
    /// <summary>Reject every non-finite value.</summary>
    Reject = 1,
    /// <summary>Map NaN to zero and infinities to finite range endpoints.</summary>
    Clamp = 2
}

/// <summary>Specifies how declared generic raw trailing bytes are written.</summary>
public enum RawTrailingBytePolicy
{
    /// <summary>Reject descriptors that declare bytes after required sample storage.</summary>
    Reject = 0,
    /// <summary>Write declared trailing bytes as zero.</summary>
    ZeroFill = 1,
    /// <summary>Copy trailing and other nonsample bytes from an exact preserved template.</summary>
    Preserve = 2
}

/// <summary>Specifies which metadata representation wins during reconciliation.</summary>
public enum RawMetadataPrecedence
{
    /// <summary>Do not reconcile conflicting metadata representations.</summary>
    None = 0,
    /// <summary>Prefer Exif metadata.</summary>
    Exif = 1,
    /// <summary>Prefer XMP metadata.</summary>
    Xmp = 2,
    /// <summary>Prefer IPTC metadata.</summary>
    Iptc = 3
}

/// <summary>Identifies the representation that supplied a unified metadata value.</summary>
public enum RawMetadataSource
{
    /// <summary>No representation was selected.</summary>
    None = 0,
    /// <summary>The value came from TIFF or Exif metadata.</summary>
    Exif = 1,
    /// <summary>The value came from an XMP packet.</summary>
    Xmp = 2,
    /// <summary>The value came from an IPTC IIM record.</summary>
    Iptc = 3
}

/// <summary>Identifies one staged metadata edit operation.</summary>
public enum RawMetadataEditKind
{
    /// <summary>Add a field that is not already present in the target representation.</summary>
    Add = 0,
    /// <summary>Replace an existing field in the target representation.</summary>
    Replace = 1,
    /// <summary>Remove matching fields from the target representation.</summary>
    Remove = 2,
    /// <summary>Update declared preview geometry or orientation.</summary>
    PreviewUpdate = 3
}

/// <summary>Selects a conservative privacy metadata removal preset.</summary>
public enum RawPrivacyScrubPreset
{
    /// <summary>Remove GPS and textual location fields.</summary>
    Location = 0,
    /// <summary>Remove creator, contact, rights, and free-text identity fields.</summary>
    Personal = 1,
    /// <summary>Remove device and software identity fields.</summary>
    Device = 2,
    /// <summary>Remove location, personal, device, and capture-time identity fields.</summary>
    All = 3
}

/// <summary>Identifies the origin used by offsets stored inside a maker note.</summary>
public enum RawMakerNoteOffsetBase
{
    /// <summary>The vendor-specific offset origin is not recognized.</summary>
    Unknown = 0,
    /// <summary>Stored offsets are relative to the containing TIFF header.</summary>
    TiffHeader = 1,
    /// <summary>Stored offsets are relative to the first maker-note byte.</summary>
    MakerNote = 2,
    /// <summary>Stored offsets are relative to an embedded TIFF header inside the maker note.</summary>
    EmbeddedTiffHeader = 3,
    /// <summary>Stored offsets are absolute file positions.</summary>
    AbsoluteFile = 4
}

/// <summary>Identifies the safe writer action selected for a maker note.</summary>
public enum RawMakerNoteWriteStrategy
{
    /// <summary>The rewrite is rejected because the required offset semantics are not proven.</summary>
    Reject = 0,
    /// <summary>The maker note is copied byte for byte at its original absolute offset.</summary>
    FixedAnchor = 1,
    /// <summary>The maker note is relocated byte for byte because its offsets remain relative to the relocated note.</summary>
    RelativeRelocation = 2,
    /// <summary>The maker note is relocated after every structurally proven internal offset is adjusted.</summary>
    PatchedRelocation = 3
}

/// <summary>Selects a version of the library-reviewed Canon main-IFD encoding schema.</summary>
/// <remarks>This version identifies reviewed tag definitions and is not a camera firmware version.</remarks>
public enum RawCanonMakerNoteVersion
{
    /// <summary>No reviewed Canon encoding schema was selected.</summary>
    Unknown = 0,
    /// <summary>The baseline schema for stable identity, file, firmware, and owner fields.</summary>
    Baseline = 1,
    /// <summary>The extended schema adding stable model, serial-format, revision, and lens identity fields.</summary>
    Extended = 2
}

/// <summary>Selects a version of the library-reviewed Nikon type-3 main-IFD encoding schema.</summary>
/// <remarks>This version identifies reviewed tag definitions and is distinct from the four-byte Nikon maker-note and color-balance versions.</remarks>
public enum RawNikonMakerNoteVersion
{
    /// <summary>No reviewed Nikon encoding schema was selected.</summary>
    Unknown = 0,
    /// <summary>The baseline schema for stable ISO, color-mode, quality, white-balance, sharpness, and focus-mode fields.</summary>
    Baseline = 1,
    /// <summary>The extended schema adding stable hue, compression, saturation, and noise-reduction fields.</summary>
    Extended = 2
}

/// <summary>Identifies the independent source category supporting a camera profile value.</summary>
public enum RawProfileProvenanceKind
{
    /// <summary>The source category is not identified.</summary>
    Unknown = 0,
    /// <summary>The value comes from a published standard or specification.</summary>
    Standard = 1,
    /// <summary>The value comes from camera-manufacturer material.</summary>
    Manufacturer = 2,
    /// <summary>The value was measured or inspected from a redistributable sample.</summary>
    Sample = 3,
    /// <summary>The value was confirmed by an independent software tool.</summary>
    Tool = 4,
    /// <summary>The value comes from independently reviewed technical research.</summary>
    Research = 5
}

/// <summary>Reports the outcome of one write-profile compatibility check.</summary>
public enum RawProfileEvidenceResult
{
    /// <summary>The compatibility outcome is not recorded.</summary>
    Unknown = 0,
    /// <summary>The named check passed.</summary>
    Passed = 1,
    /// <summary>The named check failed.</summary>
    Failed = 2
}

/// <summary>Identifies the independently recorded check represented by write-profile evidence.</summary>
public enum RawProfileEvidenceKind
{
    /// <summary>The evidence category is not recorded.</summary>
    Unknown = 0,
    /// <summary>The generated container passed structural reopen and conformance checks.</summary>
    Structural = 1,
    /// <summary>The lossless generated payload passed decoded pixel-identity checks.</summary>
    PixelIdentity = 2,
    /// <summary>The generated metadata passed preservation and semantic checks.</summary>
    Metadata = 3,
    /// <summary>A named independent application and version accepted the generated file.</summary>
    Application = 4,
    /// <summary>An explicitly lossy generated payload passed its declared numeric error bound.</summary>
    ErrorBound = 5
}

/// <summary>Identifies the numeric guarantee applied to pixels after reopening writer output.</summary>
public enum RawPixelValidationKind
{
    /// <summary>No decoded pixel validation is requested.</summary>
    None = 0,
    /// <summary>Every decoded stored sample must equal its source sample.</summary>
    Identity = 1,
    /// <summary>Every decoded stored sample must remain within an explicitly reported absolute-error bound.</summary>
    ErrorBound = 2
}

/// <summary>Identifies where a profile catalogue entry was registered.</summary>
public enum RawProfileRegistrationSource
{
    /// <summary>The entry source is not identified.</summary>
    Unknown = 0,
    /// <summary>The entry came from the library's reviewed built-in catalogue.</summary>
    BuiltIn = 1,
    /// <summary>The entry came from caller-supplied custom profile data.</summary>
    Custom = 2
}

/// <summary>Identifies the validation subsystem that produced a message.</summary>
public enum RawValidationCategory
{
    /// <summary>The category is not known.</summary>
    Unknown = 0,
    /// <summary>Container structure and byte ranges.</summary>
    Structural = 1,
    /// <summary>Compressed or packed payload rules.</summary>
    Codec = 2,
    /// <summary>Image and metadata meaning or consistency.</summary>
    Semantic = 3,
    /// <summary>Camera or writer profile conformance.</summary>
    Profile = 4,
    /// <summary>Exif, XMP, IPTC, ICC, or maker-note data.</summary>
    Metadata = 5,
    /// <summary>Output layout and write compatibility.</summary>
    Writer = 6
}

/// <summary>Identifies how a block changes in a planned write.</summary>
public enum RawWriteChangeKind
{
    /// <summary>The block is copied byte for byte.</summary>
    Copied = 0,
    /// <summary>The block is changed without re-encoding its payload.</summary>
    Changed = 1,
    /// <summary>The block is decoded and encoded again.</summary>
    Reencoded = 2,
    /// <summary>The block is moved while retaining its bytes.</summary>
    Relocated = 3,
    /// <summary>The block is omitted with explicit consent.</summary>
    Dropped = 4,
    /// <summary>A new block is added.</summary>
    Added = 5
}

/// <summary>Provides stable machine-readable diagnostic identifiers.</summary>
public enum RawDiagnosticCode
{
    /// <summary>No diagnostic condition is present.</summary>
    None = 0,
    /// <summary>An otherwise unclassified failure occurred.</summary>
    Unknown = 1,
    /// <summary>A public API argument is invalid.</summary>
    InvalidArgument = 100,
    /// <summary>The operation was cancelled.</summary>
    Cancelled = 101,
    /// <summary>The input format could not be identified.</summary>
    UnknownFormat = 200,
    /// <summary>The content and caller-provided filename or format disagree.</summary>
    FormatMismatch = 201,
    /// <summary>A required file or container signature is invalid.</summary>
    InvalidSignature = 202,
    /// <summary>The input ends before a required value or payload is complete.</summary>
    TruncatedInput = 203,
    /// <summary>An offset points outside its permitted region.</summary>
    InvalidOffset = 204,
    /// <summary>A declared length or count is invalid.</summary>
    InvalidLength = 205,
    /// <summary>Checked size or offset arithmetic overflowed.</summary>
    ArithmeticOverflow = 206,
    /// <summary>A caller-configured resource limit was exceeded.</summary>
    ResourceLimitExceeded = 207,
    /// <summary>Content evidence supports more than one format without an authoritative selection.</summary>
    AmbiguousFormat = 208,
    /// <summary>A generic raw descriptor is missing or inconsistent.</summary>
    InvalidDescriptor = 300,
    /// <summary>The container structure is invalid.</summary>
    InvalidContainer = 301,
    /// <summary>Compressed or packed data violates its codec rules.</summary>
    InvalidCodecData = 302,
    /// <summary>Metadata is malformed or inconsistent.</summary>
    InvalidMetadata = 303,
    /// <summary>A camera or writer profile is invalid.</summary>
    InvalidProfile = 304,
    /// <summary>A TIFF rational value has a zero denominator.</summary>
    InvalidRationalDenominator = 305,
    /// <summary>Parallel metadata representations contain different values for one unified field.</summary>
    MetadataConflict = 306,
    /// <summary>The requested format operation is not supported.</summary>
    UnsupportedFormat = 400,
    /// <summary>The payload codec is recognized but unavailable.</summary>
    UnsupportedCodec = 401,
    /// <summary>No enabled profile supports the requested operation.</summary>
    UnsupportedProfile = 402,
    /// <summary>Multiple profiles match without an authoritative selection.</summary>
    AmbiguousProfile = 403,
    /// <summary>Validation found a conformance error.</summary>
    ValidationFailed = 500,
    /// <summary>A named tolerant-mode recovery was applied.</summary>
    RecoveryApplied = 501,
    /// <summary>A write would discard unknown or private data.</summary>
    UnknownDataDropped = 600,
    /// <summary>A write requires explicit data-loss consent.</summary>
    DataLossConsentRequired = 601,
    /// <summary>A write plan could not be produced.</summary>
    WritePlanningFailed = 602,
    /// <summary>The requested write failed.</summary>
    WriteFailed = 603,
    /// <summary>Reopening or validating written output failed.</summary>
    OutputValidationFailed = 604,
    /// <summary>An edit invalidates preserved authenticity information.</summary>
    SignatureInvalidated = 605
}
