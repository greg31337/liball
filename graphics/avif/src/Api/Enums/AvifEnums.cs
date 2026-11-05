namespace avif;

/// <summary>
/// Identifies the outcome of an AVIF operation or the stable code attached to a diagnostic.
/// </summary>
/// <remarks>
/// <para>Values from 0 through 999 are successful or resumable statuses.</para>
/// <para>Values from 1000 through 1999 report API usage errors.</para>
/// <para>Values from 2000 through 2999 report input and I/O errors.</para>
/// <para>Values from 3000 through 3999 report malformed or nonconforming data.</para>
/// <para>Values from 4000 through 4999 report unsupported capabilities.</para>
/// <para>Values from 5000 through 5999 report resource or cancellation failures.</para>
/// <para>Values from 6000 through 6999 report operation-specific failures.</para>
/// <para>Value 9000 is reserved for an unexpected failure without a more specific code.</para>
/// </remarks>
public enum AvifResultCode
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,

    /// <summary>The operation can continue after more source bytes become available.</summary>
    NeedMoreData = 1,

    /// <summary>The source ended normally and contains no additional item, sample, frame, or OBU.</summary>
    EndOfData = 2,

    /// <summary>A caller-supplied argument is outside the documented contract.</summary>
    InvalidArgument = 1000,

    /// <summary>The requested operation is not legal in the current object state.</summary>
    InvalidState = 1001,

    /// <summary>The requested operation was made on an object that has been closed.</summary>
    Closed = 1002,

    /// <summary>A caller-provided destination cannot hold the requested output.</summary>
    BufferTooSmall = 1003,

    /// <summary>The requested item, track, frame, layer, tile, or metadata entry does not exist.</summary>
    NotFound = 1004,

    /// <summary>An input or output source failed independently of AVIF syntax.</summary>
    IoError = 2000,

    /// <summary>The source ended before all bytes required by declared syntax were available.</summary>
    TruncatedData = 2001,

    /// <summary>Input data violates a general syntax or semantic requirement.</summary>
    InvalidData = 3000,

    /// <summary>The ISO BMFF, HEIF, MIAF, or AVIF container structure is invalid.</summary>
    InvalidContainer = 3001,

    /// <summary>An image item, its properties, extents, or dependencies are invalid.</summary>
    InvalidItem = 3002,

    /// <summary>A timed-image track, sample table, timing value, or dependency is invalid.</summary>
    InvalidTrack = 3003,

    /// <summary>The AV1 bitstream syntax or required AV1 semantics are invalid.</summary>
    InvalidAv1Bitstream = 3004,

    /// <summary>Exif, XMP, ICC, HDR, or other metadata is invalid.</summary>
    InvalidMetadata = 3005,

    /// <summary>Data is bounded and understood but violates an applicable conformance rule.</summary>
    NonConformingData = 3006,

    /// <summary>The requested operation depends on a recognized feature that is not supported.</summary>
    UnsupportedFeature = 4000,

    /// <summary>An unknown essential item property prevents the requested operation.</summary>
    UnsupportedEssentialProperty = 4001,

    /// <summary>The requested operation requires access to protected cleartext or protection authoring.</summary>
    UnsupportedProtection = 4002,

    /// <summary>A configured byte, count, dimension, recursion, allocation, or work limit was exceeded.</summary>
    LimitExceeded = 5000,

    /// <summary>A required bounded allocation could not be completed.</summary>
    AllocationFailed = 5001,

    /// <summary>The caller cancelled the operation at a supported checkpoint.</summary>
    Cancelled = 5002,

    /// <summary>AV1 decoding failed after the input passed initial structural checks.</summary>
    DecodeFailed = 6000,

    /// <summary>Rendering or pixel conversion failed after samples were decoded.</summary>
    RenderFailed = 6001,

    /// <summary>AV1 encoding or AVIF authoring failed.</summary>
    EncodeFailed = 6002,

    /// <summary>Container rewrite or remuxing failed.</summary>
    RewriteFailed = 6003,

    /// <summary>An unexpected failure has no more specific stable result code.</summary>
    UnknownError = 9000
}

/// <summary>
/// Describes the effect of a diagnostic on the requested operation.
/// </summary>
public enum AvifDiagnosticSeverity
{
    /// <summary>The diagnostic supplies context and does not indicate nonconformance.</summary>
    Information = 0,

    /// <summary>The operation continued, but the caller should inspect a deviation or fallback.</summary>
    Warning = 1,

    /// <summary>The requested result could not be produced as requested.</summary>
    Error = 2,

    /// <summary>Processing stopped because continuing could not produce trustworthy results.</summary>
    Fatal = 3
}

/// <summary>
/// Identifies the subsystem that emitted a diagnostic.
/// </summary>
public enum AvifDiagnosticCategory
{
    /// <summary>The diagnostic is not specific to another category.</summary>
    General = 0,

    /// <summary>The diagnostic concerns public API arguments, state, or lifecycle.</summary>
    Api = 1,

    /// <summary>The diagnostic concerns input bounds, availability, or source behavior.</summary>
    Input = 2,

    /// <summary>The diagnostic concerns ISO BMFF, HEIF, MIAF, or AVIF container syntax.</summary>
    Container = 3,

    /// <summary>The diagnostic concerns an image item, property, extent, or item dependency.</summary>
    Item = 4,

    /// <summary>The diagnostic concerns a track, sample, timing value, or sample dependency.</summary>
    Track = 5,

    /// <summary>The diagnostic concerns AV1 syntax, semantics, decoding, or encoding.</summary>
    Av1 = 6,

    /// <summary>The diagnostic concerns Exif, XMP, ICC, HDR, or other metadata.</summary>
    Metadata = 7,

    /// <summary>The diagnostic concerns color interpretation, conversion, or resampling.</summary>
    Color = 8,

    /// <summary>The diagnostic concerns derived images, transforms, composition, or pixel packing.</summary>
    Rendering = 9,

    /// <summary>The diagnostic concerns AV1 encoding or AVIF authoring.</summary>
    Encoding = 10,

    /// <summary>The diagnostic concerns container rewrite, preservation, or remuxing.</summary>
    Rewrite = 11,

    /// <summary>The diagnostic reports an applicable conformance requirement.</summary>
    Validation = 12,

    /// <summary>The diagnostic concerns a configured resource limit or bounded allocation.</summary>
    Resource = 13,

    /// <summary>The diagnostic concerns an input or output source failure.</summary>
    Io = 14,

    /// <summary>The diagnostic concerns encrypted or otherwise protected media.</summary>
    Protection = 15
}

/// <summary>
/// Selects how completely applicable AVIF, MIAF, HEIF, and AV1 requirements are enforced.
/// </summary>
public enum AvifValidationMode
{
    /// <summary>Enforces every applicable requirement in the frozen standards baseline.</summary>
    Strict = 0,

    /// <summary>Allows only documented interoperable deviations and reports each one.</summary>
    Compatible = 1,

    /// <summary>Continues past recoverable nonconformance while retaining all safety checks.</summary>
    Permissive = 2
}

/// <summary>
/// Identifies the meaning of a directional relationship between image items.
/// </summary>
public enum AvifItemReferenceKind
{
    /// <summary>The relationship type is preserved but not interpreted.</summary>
    Unknown = 0,

    /// <summary>The source item is auxiliary to its target item.</summary>
    Auxiliary = 1,

    /// <summary>The source item is a thumbnail of its target item.</summary>
    Thumbnail = 2,

    /// <summary>The source item is derived from its target items.</summary>
    DerivedImage = 3,

    /// <summary>The source item contains color premultiplied by its target alpha item.</summary>
    Premultiplied = 4,

    /// <summary>The source metadata item describes its target item.</summary>
    ContentDescription = 5
}

/// <summary>Identifies the interpreted purpose of an auxiliary image.</summary>
public enum AvifAuxiliaryKind
{
    /// <summary>The item is not associated as an auxiliary image.</summary>
    None = 0,

    /// <summary>The item supplies alpha samples.</summary>
    Alpha = 1,

    /// <summary>The item supplies depth samples.</summary>
    Depth = 2,

    /// <summary>The item has caller-defined auxiliary semantics.</summary>
    Other = 3
}

/// <summary>Identifies the container representation of an authored metadata item.</summary>
public enum AvifMetadataKind
{
    /// <summary>The metadata uses an Exif item payload.</summary>
    Exif = 0,

    /// <summary>The metadata uses an XMP MIME item payload.</summary>
    Xmp = 1,

    /// <summary>The metadata uses a caller-selected item type and optional MIME type.</summary>
    Custom = 2
}

/// <summary>
/// Identifies the interpreted meaning of an entity group.
/// </summary>
public enum AvifEntityGroupKind
{
    /// <summary>The group type is preserved but not interpreted.</summary>
    Unknown = 0,

    /// <summary>The group contains alternative entities.</summary>
    Alternate = 1,

    /// <summary>The group contains a stereoscopic pair.</summary>
    Stereo = 2
}

/// <summary>
/// Selects the image source used when both a primary item and a timed sequence are present.
/// </summary>
public enum AvifSourceSelection
{
    /// <summary>Selects the most appropriate source for the requested operation.</summary>
    Automatic = 0,

    /// <summary>Selects the primary image item.</summary>
    PrimaryItem = 1,

    /// <summary>Selects the timed image sequence.</summary>
    ImageSequence = 2
}

/// <summary>
/// Selects which image planes and associated images are decoded.
/// </summary>
public enum AvifContentSelection
{
    /// <summary>Decodes the color image and its associated alpha image when present.</summary>
    ColorAndAlpha = 0,

    /// <summary>Decodes only the color image.</summary>
    Color = 1,

    /// <summary>Decodes only the associated alpha image.</summary>
    Alpha = 2,

    /// <summary>Decodes color, alpha, gain-map, depth, and other requested auxiliary images.</summary>
    All = 3
}

/// <summary>
/// Selects the AV1 encoder rate-control objective.
/// </summary>
public enum AvifEncoderMode
{
    /// <summary>Uses a quality target chosen on a zero-through-one-hundred scale.</summary>
    ConstantQuality = 0,

    /// <summary>Uses an explicit AV1 base quantizer.</summary>
    Quantizer = 1,

    /// <summary>Requires mathematically lossless coding.</summary>
    Lossless = 2,

    /// <summary>Targets an encoded byte size for a still image.</summary>
    TargetSize = 3,

    /// <summary>Targets a bit rate for a timed image sequence.</summary>
    TargetBitrate = 4
}

/// <summary>
/// Selects the encoder effort and compression tradeoff.
/// </summary>
public enum AvifEncoderPreset
{
    /// <summary>Minimizes encoder work.</summary>
    Fastest = 0,

    /// <summary>Favors encoding speed over compression efficiency.</summary>
    Fast = 1,

    /// <summary>Balances encoding time and compression efficiency.</summary>
    Balanced = 2,

    /// <summary>Favors compression efficiency over encoding speed.</summary>
    Quality = 3,

    /// <summary>Uses the highest available bounded search effort.</summary>
    Best = 4
}

/// <summary>
/// Selects the container header representation used for authored output.
/// </summary>
public enum AvifHeaderFormat
{
    /// <summary>Selects a conforming representation based on output features and size.</summary>
    Automatic = 0,

    /// <summary>Uses the regular ISO BMFF <c>meta</c> and media-data representation.</summary>
    Full = 1,

    /// <summary>Uses the standardized low-overhead <c>mini</c> representation.</summary>
    Minimized = 2,

    /// <summary>Retains the input representation during rewrite when it remains valid.</summary>
    PreserveExisting = 3
}

/// <summary>Identifies a high-level AVIF container or image feature.</summary>
public enum AvifFeature
{
    /// <summary>One or more image items are present or declared.</summary>
    ImageItem = 0,

    /// <summary>Multiple visual image items form an image collection.</summary>
    ImageCollection = 1,

    /// <summary>A grid derived-image item is present.</summary>
    Grid = 2,

    /// <summary>Layer selection or a layered-image index is present.</summary>
    LayeredImage = 3,

    /// <summary>A progressively renderable layered image is present.</summary>
    ProgressiveImage = 4,

    /// <summary>An auxiliary image item or track is present.</summary>
    AuxiliaryImage = 5,

    /// <summary>A gain-map tone-map derived image is present.</summary>
    GainMap = 6,

    /// <summary>A sample-transform derived image is present.</summary>
    SampleTransform = 7,

    /// <summary>A timed image sequence is present or declared.</summary>
    ImageSequence = 8,

    /// <summary>The standardized low-overhead minimized representation is present or declared.</summary>
    LowOverhead = 9,

    /// <summary>Movie fragments or fragment declarations are present.</summary>
    FragmentedMedia = 10,

    /// <summary>Protected media signaling or protected items are present.</summary>
    ProtectedMedia = 11
}

/// <summary>
/// Selects the chroma resampling filter used by color conversion.
/// </summary>
public enum AvifResamplingFilter
{
    /// <summary>Uses the nearest source chroma sample.</summary>
    Nearest = 0,

    /// <summary>Uses linear interpolation between adjacent chroma samples.</summary>
    Bilinear = 1,

    /// <summary>Uses a separable cubic reconstruction filter.</summary>
    Bicubic = 2,

    /// <summary>Uses a separable three-lobe Lanczos reconstruction filter.</summary>
    Lanczos3 = 3
}

/// <summary>
/// Selects the dithering strategy used during bit-depth reduction.
/// </summary>
public enum AvifDitherMode
{
    /// <summary>Disables dithering for exact deterministic quantization.</summary>
    None = 0,

    /// <summary>Uses a deterministic ordered dither pattern.</summary>
    Ordered = 1,

    /// <summary>Uses deterministic error-diffusion dithering.</summary>
    ErrorDiffusion = 2
}

/// <summary>Selects an ICC profile rendering intent.</summary>
public enum AvifRenderingIntent
{
    /// <summary>Uses the profile's perceptual color rendering transform.</summary>
    Perceptual = 0,

    /// <summary>Preserves colors relative to the profile media white point.</summary>
    RelativeColorimetric = 1,

    /// <summary>Uses the profile's saturation color rendering transform.</summary>
    Saturation = 2,

    /// <summary>Preserves colors relative to an absolute illuminant.</summary>
    AbsoluteColorimetric = 3
}

/// <summary>Identifies an AVIF sample-transform postfix operator.</summary>
public enum AvifSampleTransformOperator
{
    /// <summary>Negates one operand.</summary>
    Negate = 64,

    /// <summary>Computes the absolute value of one operand.</summary>
    AbsoluteValue = 65,

    /// <summary>Computes the bitwise complement of one operand.</summary>
    BitwiseNot = 66,

    /// <summary>Finds the zero-based most-significant set-bit index.</summary>
    BitScanReverse = 67,

    /// <summary>Adds two operands.</summary>
    Add = 128,

    /// <summary>Subtracts the right operand from the left operand.</summary>
    Subtract = 129,

    /// <summary>Multiplies two operands.</summary>
    Multiply = 130,

    /// <summary>Divides the left operand by the right operand.</summary>
    Divide = 131,

    /// <summary>Computes the bitwise conjunction of two operands.</summary>
    BitwiseAnd = 132,

    /// <summary>Computes the bitwise inclusive disjunction of two operands.</summary>
    BitwiseOr = 133,

    /// <summary>Computes the bitwise exclusive disjunction of two operands.</summary>
    BitwiseXor = 134,

    /// <summary>Raises the left operand to the power of the right operand.</summary>
    Power = 135,

    /// <summary>Selects the lesser operand.</summary>
    Minimum = 136,

    /// <summary>Selects the greater operand.</summary>
    Maximum = 137
}

/// <summary>Identifies H.273 color-primary chromaticities.</summary>
public enum AvifColorPrimaries
{
    /// <summary>The value is reserved by H.273.</summary>
    Reserved = 0,

    /// <summary>Uses ITU-R BT.709 and IEC 61966-2-1 primaries with a D65 white point.</summary>
    Bt709 = 1,

    /// <summary>The color primaries are not specified.</summary>
    Unspecified = 2,

    /// <summary>The value is reserved by H.273.</summary>
    ReservedThree = 3,

    /// <summary>Uses the historical ITU-R BT.470 System M primaries.</summary>
    Bt470M = 4,

    /// <summary>Uses the ITU-R BT.470 System B/G primaries.</summary>
    Bt470Bg = 5,

    /// <summary>Uses the SMPTE 170M and ITU-R BT.601 525-line primaries.</summary>
    Bt601 = 6,

    /// <summary>Uses the SMPTE 240M primaries.</summary>
    Smpte240 = 7,

    /// <summary>Uses generic film scanner primaries with an illuminant C white point.</summary>
    GenericFilm = 8,

    /// <summary>Uses ITU-R BT.2020 wide-gamut primaries with a D65 white point.</summary>
    Bt2020 = 9,

    /// <summary>Uses SMPTE ST 428-1 equal-energy XYZ primaries.</summary>
    Smpte428 = 10,

    /// <summary>Uses SMPTE RP 431-2 DCI-P3 primaries and white point.</summary>
    Smpte431 = 11,

    /// <summary>Uses SMPTE EG 432-1 Display-P3 primaries with a D65 white point.</summary>
    Smpte432 = 12,

    /// <summary>Uses the EBU Tech. 3213-E primaries with a D65 white point.</summary>
    EbuTech3213 = 22
}

/// <summary>Identifies an H.273 transfer characteristic.</summary>
public enum AvifTransferCharacteristics
{
    /// <summary>The value is reserved by H.273.</summary>
    Reserved = 0,

    /// <summary>Uses the ITU-R BT.709 transfer function.</summary>
    Bt709 = 1,

    /// <summary>The transfer characteristic is not specified.</summary>
    Unspecified = 2,

    /// <summary>The value is reserved by H.273.</summary>
    ReservedThree = 3,

    /// <summary>Uses a pure power function with gamma 2.2.</summary>
    Gamma22 = 4,

    /// <summary>Uses a pure power function with gamma 2.8.</summary>
    Gamma28 = 5,

    /// <summary>Uses the SMPTE 170M and ITU-R BT.601 transfer function.</summary>
    Bt601 = 6,

    /// <summary>Uses the SMPTE 240M transfer function.</summary>
    Smpte240 = 7,

    /// <summary>Uses a linear transfer function.</summary>
    Linear = 8,

    /// <summary>Uses a logarithmic transfer function with a 100-to-1 range.</summary>
    Logarithmic100 = 9,

    /// <summary>Uses a logarithmic transfer function with a 316-to-1 range.</summary>
    Logarithmic316 = 10,

    /// <summary>Uses the extended-gamut IEC 61966-2-4 transfer function.</summary>
    Iec61966_2_4 = 11,

    /// <summary>Uses the extended-gamut ITU-R BT.1361 transfer function.</summary>
    Bt1361 = 12,

    /// <summary>Uses the IEC 61966-2-1 sRGB transfer function.</summary>
    Srgb = 13,

    /// <summary>Uses the ten-bit ITU-R BT.2020 transfer function.</summary>
    Bt2020TenBit = 14,

    /// <summary>Uses the twelve-bit ITU-R BT.2020 transfer function.</summary>
    Bt2020TwelveBit = 15,

    /// <summary>Uses the SMPTE ST 2084 perceptual quantizer transfer function.</summary>
    Smpte2084 = 16,

    /// <summary>Uses the SMPTE ST 428-1 transfer function.</summary>
    Smpte428 = 17,

    /// <summary>Uses the ARIB STD-B67 hybrid log-gamma transfer function.</summary>
    Hlg = 18
}

/// <summary>Identifies H.273 matrix coefficients.</summary>
public enum AvifMatrixCoefficients
{
    /// <summary>Uses the identity mapping between G, B, R and Y, U, V components.</summary>
    Identity = 0,

    /// <summary>Uses ITU-R BT.709 non-constant-luminance coefficients.</summary>
    Bt709 = 1,

    /// <summary>The matrix coefficients are not specified.</summary>
    Unspecified = 2,

    /// <summary>The value is reserved by H.273.</summary>
    ReservedThree = 3,

    /// <summary>Uses the United States FCC coefficients.</summary>
    Fcc = 4,

    /// <summary>Uses ITU-R BT.470 System B/G coefficients.</summary>
    Bt470Bg = 5,

    /// <summary>Uses SMPTE 170M and ITU-R BT.601 coefficients.</summary>
    Bt601 = 6,

    /// <summary>Uses SMPTE 240M coefficients.</summary>
    Smpte240 = 7,

    /// <summary>Uses the reversible YCgCo component transform.</summary>
    YCgCo = 8,

    /// <summary>Uses ITU-R BT.2020 non-constant-luminance coefficients.</summary>
    Bt2020NonConstantLuminance = 9,

    /// <summary>Uses ITU-R BT.2020 constant-luminance coefficients.</summary>
    Bt2020ConstantLuminance = 10,

    /// <summary>Uses SMPTE ST 2085 Y'D'zD'x coefficients.</summary>
    Smpte2085 = 11,

    /// <summary>Derives non-constant-luminance coefficients from the signaled primaries.</summary>
    ChromaDerivedNonConstantLuminance = 12,

    /// <summary>Derives constant-luminance coefficients from the signaled primaries.</summary>
    ChromaDerivedConstantLuminance = 13,

    /// <summary>Uses the ITU-R BT.2100 ICtCp opponent-color transform.</summary>
    ICtCp = 14,

    /// <summary>Uses the SMPTE ST 2128 IPT-C2 opponent-color transform.</summary>
    IptC2 = 15,

    /// <summary>Uses the reversible YCgCo transform with two additional RGB bits.</summary>
    YCgCoRe = 16,

    /// <summary>Uses the reversible YCgCo transform with one additional RGB bit.</summary>
    YCgCoRo = 17
}

/// <summary>
/// Selects how a gain map or tone-map derived image is rendered.
/// </summary>
public enum AvifToneMapMode
{
    /// <summary>Selects base or alternate rendering from metadata and display headroom.</summary>
    Automatic = 0,

    /// <summary>Renders the base image without applying its gain map.</summary>
    BaseImage = 1,

    /// <summary>Applies the gain map for the configured display headroom.</summary>
    ApplyGainMap = 2
}

/// <summary>Identifies the component order of a packed pixel buffer.</summary>
public enum AvifPixelFormat
{
    /// <summary>Stores one gray component.</summary>
    Gray = 0,

    /// <summary>Stores gray followed by alpha.</summary>
    GrayAlpha = 1,

    /// <summary>Stores red, green, and blue.</summary>
    Rgb = 2,

    /// <summary>Stores red, green, blue, and alpha.</summary>
    Rgba = 3,

    /// <summary>Stores blue, green, and red.</summary>
    Bgr = 4,

    /// <summary>Stores blue, green, red, and alpha.</summary>
    Bgra = 5,

    /// <summary>Stores alpha, red, green, and blue.</summary>
    Argb = 6,

    /// <summary>Stores alpha, blue, green, and red.</summary>
    Abgr = 7,

    /// <summary>Stores five red bits, six green bits, and five blue bits in one little-endian 16-bit pixel.</summary>
    Rgb565 = 8
}

/// <summary>Identifies the planar chroma layout of an image.</summary>
public enum AvifChromaFormat
{
    /// <summary>The image contains only a full-resolution luma plane.</summary>
    Monochrome = 0,

    /// <summary>Both chroma axes are subsampled by two.</summary>
    Yuv420 = 1,

    /// <summary>The chroma horizontal axis is subsampled by two.</summary>
    Yuv422 = 2,

    /// <summary>Both chroma planes have full luma resolution.</summary>
    Yuv444 = 3
}

/// <summary>Identifies chroma sample placement relative to luma samples.</summary>
public enum AvifChromaSamplePosition
{
    /// <summary>The sample placement is unknown or not applicable.</summary>
    Unknown = 0,

    /// <summary>Chroma samples are vertically centered between luma samples.</summary>
    Vertical = 1,

    /// <summary>Chroma samples are colocated with the top-left luma sample.</summary>
    Colocated = 2
}

/// <summary>Identifies whether integer image samples use full or studio range.</summary>
public enum AvifRange
{
    /// <summary>Samples use the limited studio range for their bit depth.</summary>
    Limited = 0,

    /// <summary>Samples use the complete integer range for their bit depth.</summary>
    Full = 1
}

/// <summary>Identifies the representation of an image alpha plane.</summary>
public enum AvifAlphaMode
{
    /// <summary>The image has no alpha plane.</summary>
    None = 0,

    /// <summary>Color samples are independent of straight alpha samples.</summary>
    Straight = 1,

    /// <summary>Color samples are premultiplied by alpha.</summary>
    Premultiplied = 2
}

/// <summary>Identifies one plane in a planar image.</summary>
public enum AvifPlaneKind
{
    /// <summary>The luma or monochrome plane.</summary>
    Y = 0,

    /// <summary>The first chroma plane.</summary>
    U = 1,

    /// <summary>The second chroma plane.</summary>
    V = 2,

    /// <summary>The full-resolution alpha plane.</summary>
    Alpha = 3
}

/// <summary>Selects optional luminance fields in content-colour-volume metadata.</summary>
public enum AvifContentColorVolumeFields
{
    /// <summary>No luminance field is present.</summary>
    None = 0,

    /// <summary>Minimum luminance is present.</summary>
    MinimumLuminance = 1,

    /// <summary>Maximum luminance is present.</summary>
    MaximumLuminance = 2,

    /// <summary>Average luminance is present.</summary>
    AverageLuminance = 4
}

/// <summary>Selects still-picture or inter-frame AV1 sequence-header policy.</summary>
public enum AvifSequenceEncodingPolicy
{
    /// <summary>Uses still syntax for one frame and inter-frame syntax for sequences.</summary>
    Automatic = 0,

    /// <summary>Requires reduced still-picture sequence syntax.</summary>
    StillPicture = 1,

    /// <summary>Uses non-reduced sequence syntax suitable for inter-frame coding.</summary>
    InterFrame = 2
}
