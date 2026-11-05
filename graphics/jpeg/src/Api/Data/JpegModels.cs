using System.Collections.Generic;

namespace jpeg;

/// <summary>Contains decoded pixels together with the JPEG characteristics and metadata needed for later processing or encoding.</summary>
/// <remarks><c>Pixels</c> remains the compact eight-bit interleaved convenience buffer. <c>InterleavedBuffer</c> and <c>PlanarBuffer</c> describe strided 8-bit, 12-bit, and 16-bit storage.</remarks>
public class JpegImage
{
    /// <summary>Creates an empty image with initialized pixel, metadata, and diagnostic containers.</summary>
    public JpegImage()
    {
    }

    /// <summary>The image width in pixels.</summary>
    public int Width;
    /// <summary>The image height in pixels.</summary>
    public int Height;
    /// <summary>The original encoded image width before IDCT scaling or region selection.</summary>
    public int SourceWidth;
    /// <summary>The original encoded image height before IDCT scaling or region selection.</summary>
    public int SourceHeight;
    /// <summary>The number of meaningful bits in each source sample, normally 8.</summary>
    public int Precision;
    /// <summary>The number of color components represented by each pixel.</summary>
    public int ComponentCount;
    /// <summary>The byte layout used by <c>Pixels</c>.</summary>
    public JpegPixelFormat PixelFormat;
    /// <summary>The interpreted color space of the decoded pixels.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>The sampling relationship recorded in the JPEG frame.</summary>
    public JpegSubsampling Subsampling;
    /// <summary>The active sample-buffer arrangement.</summary>
    public JpegBufferLayout BufferLayout;
    /// <summary>The ownership relationship for the active sample buffers.</summary>
    public JpegBufferOwnership BufferOwnership;
    /// <summary>The number of storage elements between adjacent interleaved rows, or zero for planar storage.</summary>
    public int RowStride;
    /// <summary>The supported inverse-DCT scale ratio actually applied.</summary>
    public JpegIdctScaleRatio AppliedIdctScaleRatio;
    /// <summary>The canonical numerator of the inverse-DCT scale actually applied.</summary>
    public int AppliedIdctScaleNumerator;
    /// <summary>The canonical denominator of the inverse-DCT scale actually applied.</summary>
    public int AppliedIdctScaleDenominator;
    /// <summary>The exact source-coordinate region decoded after optional MCU alignment.</summary>
    public JpegRectangle DecodedRegion = new JpegRectangle();
    /// <summary>The exact rectangle produced in the coordinate system of the scaled full image.</summary>
    public JpegRectangle OutputRectangle = new JpegRectangle();
    /// <summary>Whether MCU-aligned fast cropping was applied to a requested region.</summary>
    public bool FastMcuCropApplied;
    /// <summary>The tightly packed decoded pixel bytes in top-to-bottom row order.</summary>
    public byte[] Pixels = new byte[0];
    /// <summary>The full interleaved buffer description, including wide samples and row padding.</summary>
    public JpegInterleavedPixelBuffer InterleavedBuffer = new JpegInterleavedPixelBuffer();
    /// <summary>The planar component-buffer description when samples are stored in separate planes.</summary>
    public JpegPlanarImageBuffer PlanarBuffer = new JpegPlanarImageBuffer();
    /// <summary>The quantization tables retained from a decoded JPEG for optional reuse during encoding.</summary>
    public List<JpegQuantizationTable> QuantizationTables = new List<JpegQuantizationTable>();
    /// <summary>The retained quantization-table identifier for each decoded source component.</summary>
    public List<int> ComponentQuantizationTableIdentifiers = new List<int>();
    /// <summary>The metadata retained or produced for the image.</summary>
    public JpegMetadata Metadata = new JpegMetadata();
    /// <summary>Recoverable parsing and decoding observations, in the order they occurred.</summary>
    public List<JpegDiagnostic> Diagnostics = new List<JpegDiagnostic>();
    internal JpegDiagnosticCollection DiagnosticCollection = new JpegDiagnosticCollection();
}

/// <summary>Describes a JPEG stream from its headers and metadata without requiring pixel decoding.</summary>
public class JpegInfo
{
    /// <summary>Creates an empty information result with initialized metadata and diagnostic containers.</summary>
    public JpegInfo()
    {
    }

    /// <summary>The image format family recognized without decoding pixels.</summary>
    public JpegFormatFamily FormatFamily;
    /// <summary>Whether the recognized format family is supported by this library.</summary>
    public JpegSupportStatus FormatSupport;
    /// <summary>Whether the coding process selected by the frame is supported, or unknown when no process was found.</summary>
    public JpegSupportStatus CodingProcessSupport;
    /// <summary>The image width declared by the frame header.</summary>
    public int Width;
    /// <summary>The resolved image height, including a height supplied later by a DNL marker.</summary>
    public int Height;
    /// <summary>Indicates whether a DNL marker explicitly supplied the number of image lines.</summary>
    public bool HasDefinedNumberOfLines;
    /// <summary>The height carried by the DNL marker, or zero when none was present.</summary>
    public int DefinedNumberOfLines;
    /// <summary>The number of bits used for each encoded sample.</summary>
    public int Precision;
    /// <summary>The number of components declared by the frame.</summary>
    public int ComponentCount;
    /// <summary>The JPEG coding process selected by the start-of-frame marker.</summary>
    public JpegProcess Process;
    /// <summary>Indicates whether the stream uses progressive scans.</summary>
    public bool Progressive;
    /// <summary>Indicates whether entropy data uses arithmetic coding instead of Huffman coding.</summary>
    public bool ArithmeticCoding;
    /// <summary>Indicates that the stream uses the separate JPEG-LS coding process.</summary>
    public bool IsJpegLs;
    /// <summary>Indicates that markers suggest a nonstandard SmartScale stream.</summary>
    public bool HasSmartScale;
    /// <summary>The color interpretation inferred from component identifiers and application markers.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>The evidence selected for <c>ColorSpace</c> after applying JPEG color-hint precedence.</summary>
    public JpegColorHintSource ColorSpaceSource;
    /// <summary>The individual color hints and their resolved precedence outcome.</summary>
    public JpegColorHintMetadata ColorHints = new JpegColorHintMetadata();
    /// <summary>The standard sampling preset inferred from component sampling factors.</summary>
    public JpegSubsampling Subsampling;
    /// <summary>The total number of bytes consumed by metadata marker payloads.</summary>
    public long MetadataBytes;
    /// <summary>The estimated number of pixels in a full decoded output.</summary>
    public long EstimatedOutputPixels;
    /// <summary>The estimated byte size of a full decoded output buffer.</summary>
    public long EstimatedOutputBytes;
    /// <summary>The parsed frame header and its component definitions.</summary>
    public JpegFrameHeader Frame = new JpegFrameHeader();
    /// <summary>The frame components in encoded order.</summary>
    public List<JpegFrameComponent> Components = new List<JpegFrameComponent>();
    /// <summary>The final parsed scan header, or an empty header when no scan was found.</summary>
    public JpegScanHeader LastScanHeader = new JpegScanHeader();
    /// <summary>All parsed scan headers in stream order.</summary>
    public List<JpegScanHeader> ScanHeaders = new List<JpegScanHeader>();
    /// <summary>All parsed or supplied quantization tables in stream order.</summary>
    public List<JpegQuantizationTable> QuantizationTables = new List<JpegQuantizationTable>();
    /// <summary>All parsed or supplied Huffman tables in stream order.</summary>
    public List<JpegHuffmanTable> HuffmanTables = new List<JpegHuffmanTable>();
    /// <summary>All parsed or supplied arithmetic-conditioning tables in stream order.</summary>
    public List<JpegArithmeticConditioningTable> ArithmeticConditioningTables = new List<JpegArithmeticConditioningTable>();
    internal bool AdobeInvertsCmyk;
    /// <summary>The number of quantization tables defined by the stream.</summary>
    public int QuantizationTableCount;
    /// <summary>The number of Huffman tables defined by the stream.</summary>
    public int HuffmanTableCount;
    /// <summary>The number of arithmetic conditioning tables defined by the stream.</summary>
    public int ArithmeticConditioningTableCount;
    /// <summary>Indicates that the stream declares hierarchical progression.</summary>
    public bool HasHierarchicalProgression;
    /// <summary>The number of hierarchical-progression markers encountered.</summary>
    public int HierarchicalProgressionMarkerCount;
    /// <summary>The number of expand-reference-component markers encountered.</summary>
    public int ExpandReferenceComponentMarkerCount;
    /// <summary>The number of JPEG-LS markers encountered.</summary>
    public int JpegLsMarkerCount;
    /// <summary>The number of markers interpreted as SmartScale indicators.</summary>
    public int SmartScaleMarkerCount;
    /// <summary>Indicates that the stream declares a restart interval.</summary>
    public bool HasRestartInterval;
    /// <summary>The declared number of MCU units between restart markers.</summary>
    public int RestartInterval;
    /// <summary>The restart interval resolved to the MCU-block count encoded by the DRI marker.</summary>
    public int ResolvedRestartInterval;
    /// <summary>The number of restart markers encountered in entropy data.</summary>
    public int RestartMarkerCount;
    internal int LastRestartMarkerIndex = -1;
    internal int ExpectedRestartMarkerIndex;
    /// <summary>The number of APP0 through APP15 metadata segments.</summary>
    public int AppSegmentCount;
    /// <summary>The number of COM marker segments.</summary>
    public int CommentCount;
    /// <summary>The number of scan headers in the stream.</summary>
    public int ScanCount;
    /// <summary>The number of components in the final scan header.</summary>
    public int LastScanComponentCount;
    /// <summary>The first spectral coefficient selected by the final scan.</summary>
    public int LastScanSpectralStart;
    /// <summary>The last spectral coefficient selected by the final scan.</summary>
    public int LastScanSpectralEnd;
    /// <summary>The previous successive-approximation bit position in the final scan.</summary>
    public int LastScanSuccessiveHigh;
    /// <summary>The current successive-approximation bit position in the final scan.</summary>
    public int LastScanSuccessiveLow;
    /// <summary>Indicates that a start-of-image marker was found.</summary>
    public bool SawStartOfImage;
    /// <summary>Indicates that a supported or recognized start-of-frame marker was found.</summary>
    public bool SawStartOfFrame;
    /// <summary>Indicates that at least one start-of-scan marker was found.</summary>
    public bool SawStartOfScan;
    /// <summary>Indicates that an end-of-image marker was found.</summary>
    public bool SawEndOfImage;
    /// <summary>Indicates that the stream contains reusable tables but no image frame.</summary>
    public bool IsAbbreviatedTableStream;
    /// <summary>Indicates that the image depends on tables supplied separately.</summary>
    public bool IsAbbreviatedImageStream;
    /// <summary>Indicates that separately supplied tables were used while parsing.</summary>
    public bool UsedSuppliedTables;
    /// <summary>The parsed metadata and raw payloads retained from the stream.</summary>
    public JpegMetadata Metadata = new JpegMetadata();
    /// <summary>Recoverable parsing observations, warnings, and errors in encounter order.</summary>
    public List<JpegDiagnostic> Diagnostics = new List<JpegDiagnostic>();
    internal JpegDiagnosticCollection DiagnosticCollection = new JpegDiagnosticCollection();
}

/// <summary>Describes one informational message, warning, or recoverable error associated with JPEG processing.</summary>
public class JpegDiagnostic
{
    /// <summary>Creates an empty diagnostic with an initialized message.</summary>
    public JpegDiagnostic()
    {
    }

    /// <summary>The importance of the diagnostic.</summary>
    public JpegDiagnosticSeverity Severity;
    /// <summary>The stable category of the condition.</summary>
    public JpegDiagnosticCode Code;
    /// <summary>A plain-language explanation of the condition.</summary>
    public string Message = "";
    /// <summary>The zero-based byte offset associated with the condition, or a negative value when unavailable.</summary>
    public long Offset;
    /// <summary>The marker being processed when the condition was found.</summary>
    public JpegMarker Marker;
}

/// <summary>Collects standard JPEG application metadata, raw payloads, and comments.</summary>
/// <remarks>Presence flags distinguish absent metadata from a present marker whose value is empty or zero.</remarks>
public class JpegMetadata
{
    /// <summary>Creates an empty metadata container with initialized format objects, payloads, and collections.</summary>
    public JpegMetadata()
    {
    }

    /// <summary>The parsed JFIF APP0 metadata.</summary>
    public JpegJfifMetadata Jfif = new JpegJfifMetadata();
    /// <summary>The parsed JFXX APP0 extensions in marker order.</summary>
    public List<JpegJfxxMetadata> Jfxx = new List<JpegJfxxMetadata>();
    /// <summary>The parsed Adobe APP14 metadata.</summary>
    public JpegAdobeMetadata Adobe = new JpegAdobeMetadata();
    /// <summary>The commonly used fields parsed from the Exif TIFF structure.</summary>
    public JpegExifMetadata ExifMetadata = new JpegExifMetadata();
    /// <summary>The parsed standard XMP packet.</summary>
    public JpegXmpMetadata XmpMetadata = new JpegXmpMetadata();
    /// <summary>The parsed and reassembled Extended XMP packet.</summary>
    public JpegExtendedXmpMetadata ExtendedXmpMetadata = new JpegExtendedXmpMetadata();
    /// <summary>The parsed MPF container header.</summary>
    public JpegMpfMetadata MpfMetadata = new JpegMpfMetadata();
    /// <summary>The parsed Photoshop APP13 resource blocks and IPTC payload.</summary>
    public JpegPhotoshopMetadata Photoshop = new JpegPhotoshopMetadata();
    /// <summary>The parsed ICC profile header and tag table.</summary>
    public JpegIccProfile Icc = new JpegIccProfile();
    /// <summary>The CICP values parsed from an ICC <c>cicp</c> tag.</summary>
    public JpegCicpMetadata Cicp = new JpegCicpMetadata();
    internal List<JpegIccProfileChunk> IccProfileChunks = new List<JpegIccProfileChunk>();
    /// <summary>The Extended XMP APP1 chunks in marker order.</summary>
    public List<JpegExtendedXmpChunk> ExtendedXmpChunks = new List<JpegExtendedXmpChunk>();
    /// <summary>Indicates that a JFIF APP0 segment was present.</summary>
    public bool HasJfif;
    /// <summary>Indicates that at least one JFXX extension was present.</summary>
    public bool HasJfxx;
    /// <summary>Indicates that ICC_PROFILE APP2 chunks were present, even if they could not be assembled.</summary>
    public bool HasIccProfileChunks;
    /// <summary>Indicates that the ICC chunks formed a complete profile.</summary>
    public bool HasIccProfile;
    /// <summary>Indicates that an ICC <c>cicp</c> tag was present.</summary>
    public bool HasCicp;
    /// <summary>The major JFIF version, duplicated here for convenient inspection.</summary>
    public int JfifMajorVersion;
    /// <summary>The minor JFIF version, duplicated here for convenient inspection.</summary>
    public int JfifMinorVersion;
    /// <summary>The JFIF density unit: zero for aspect ratio, one for dots per inch, or two for dots per centimetre.</summary>
    public int DensityUnit;
    /// <summary>The horizontal JFIF density in the declared density unit.</summary>
    public int XDensity;
    /// <summary>The vertical JFIF density in the declared density unit.</summary>
    public int YDensity;
    /// <summary>Indicates that density values could be converted to dots per inch.</summary>
    public bool HasDensityDpi;
    /// <summary>The horizontal density normalized to dots per inch.</summary>
    public double XDpi;
    /// <summary>The vertical density normalized to dots per inch.</summary>
    public double YDpi;
    /// <summary>Indicates that an Adobe APP14 segment was present.</summary>
    public bool HasAdobe;
    /// <summary>Indicates that an Exif APP1 segment was present.</summary>
    public bool HasExif;
    /// <summary>Indicates that a standard XMP APP1 segment was present.</summary>
    public bool HasXmp;
    /// <summary>Indicates that extended XMP chunks were present.</summary>
    public bool HasExtendedXmpChunks;
    /// <summary>Indicates that extended XMP chunks formed a complete payload.</summary>
    public bool HasExtendedXmp;
    /// <summary>Indicates that an MPF APP2 segment was present.</summary>
    public bool HasMpf;
    /// <summary>Indicates that a Photoshop APP13 segment was present.</summary>
    public bool HasPhotoshop;
    /// <summary>Indicates that a Photoshop resource contained IPTC data.</summary>
    public bool HasIptc;
    /// <summary>The Adobe APP14 version, duplicated here for convenient inspection.</summary>
    public int AdobeVersion;
    /// <summary>The Adobe color-transform code, duplicated here for convenient inspection.</summary>
    public int AdobeTransform;
    /// <summary>The raw Exif APP1 payload, including its identifier.</summary>
    public byte[] Exif = new byte[0];
    /// <summary>The raw standard XMP APP1 payload, including its namespace identifier.</summary>
    public byte[] Xmp = new byte[0];
    /// <summary>The reassembled extended XMP packet bytes.</summary>
    public byte[] ExtendedXmp = new byte[0];
    /// <summary>The raw MPF APP2 payload.</summary>
    public byte[] Mpf = new byte[0];
    /// <summary>The extracted IPTC payload bytes.</summary>
    public byte[] Iptc = new byte[0];
    /// <summary>The reassembled raw ICC profile bytes.</summary>
    public byte[] IccProfile = new byte[0];
    /// <summary>The decoded COM marker strings in file order.</summary>
    public List<string> Comments = new List<string>();
    internal List<JpegMetadataSegment> Segments = new List<JpegMetadataSegment>();
    /// <summary>Preserved APPn, COM, and unknown markers in original stream order; collection order controls write order.</summary>
    public List<JpegRawMarker> RawMarkers = new List<JpegRawMarker>();
    /// <summary>Indicates that <c>RawMarkers</c> is a caller-supplied complete marker sequence, including an intentionally empty sequence, that is eligible for <see cref="JpegMetadataPolicy.ReplaceSelected"/>.</summary>
    public bool RawMarkersAreExplicit;
}

/// <summary>Describes a JFIF APP0 header and its optional uncompressed RGB thumbnail.</summary>
public class JpegJfifMetadata
{
    /// <summary>Creates empty JFIF metadata with initialized thumbnail buffers.</summary>
    public JpegJfifMetadata()
    {
    }

    /// <summary>Indicates that a valid JFIF identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The major JFIF version number.</summary>
    public int MajorVersion;
    /// <summary>The minor JFIF version number.</summary>
    public int MinorVersion;
    /// <summary>The density unit code defined by JFIF.</summary>
    public int DensityUnit;
    /// <summary>The horizontal density in the declared unit.</summary>
    public int XDensity;
    /// <summary>The vertical density in the declared unit.</summary>
    public int YDensity;
    /// <summary>Indicates that the density could be normalized to dots per inch.</summary>
    public bool HasDensityDpi;
    /// <summary>The horizontal density in dots per inch.</summary>
    public double XDpi;
    /// <summary>The vertical density in dots per inch.</summary>
    public double YDpi;
    /// <summary>The thumbnail width in pixels.</summary>
    public int ThumbnailWidth;
    /// <summary>The thumbnail height in pixels.</summary>
    public int ThumbnailHeight;
    /// <summary>The raw three-byte RGB thumbnail data stored in APP0.</summary>
    public byte[] ThumbnailData = new byte[0];
    /// <summary>Indicates that the thumbnail length matches its declared dimensions.</summary>
    public bool HasRgbThumbnail;
    /// <summary>The validated RGB thumbnail pixels.</summary>
    public byte[] RgbThumbnailPixels = new byte[0];
}

/// <summary>Contains coding-independent color code points from an ICC <c>cicp</c> tag.</summary>
public class JpegCicpMetadata
{
    /// <summary>Creates empty CICP metadata with initialized payload storage.</summary>
    public JpegCicpMetadata()
    {
    }

    /// <summary>Indicates that an ICC tag-directory entry named <c>cicp</c> was present.</summary>
    public bool IsPresent;
    /// <summary>Indicates that the tag used the required <c>cicp</c> payload type, length, and range flag.</summary>
    public bool IsValid;
    /// <summary>The H.273 color-primaries code point.</summary>
    public int ColorPrimaries;
    /// <summary>The H.273 transfer-characteristics code point.</summary>
    public int TransferCharacteristics;
    /// <summary>The H.273 matrix-coefficients code point.</summary>
    public int MatrixCoefficients;
    /// <summary>The numeric H.273 video-full-range flag.</summary>
    public int VideoFullRangeFlag;
    /// <summary>Indicates full-range component values when the CICP tag is valid.</summary>
    public bool FullRange;
    /// <summary>An independent copy of the complete ICC tag payload.</summary>
    public byte[] RawPayload = new byte[0];
}

/// <summary>Exposes the JPEG color hints and the result of applying their precedence.</summary>
/// <remarks>Encoded component layout is resolved in this order: Adobe APP14, CICP matrix coefficients, JFIF, component identifiers, then component count. Color characteristics are resolved separately in this order: ICC profile, CICP, Exif, then JFIF.</remarks>
public class JpegColorHintMetadata
{
    /// <summary>Creates empty color-hint metadata with initialized component identifiers.</summary>
    public JpegColorHintMetadata()
    {
    }

    /// <summary>The resolved encoded component color space.</summary>
    public JpegColorSpace EncodedColorSpace;
    /// <summary>The source selected for the encoded component color space.</summary>
    public JpegColorHintSource EncodedColorSpaceSource;
    /// <summary>The highest-precedence source of color primaries and transfer characteristics.</summary>
    public JpegColorHintSource ColorCharacteristicsSource;
    /// <summary>Indicates that two available encoded-layout hints disagree.</summary>
    public bool HasConflict;
    /// <summary>Indicates that JFIF supplied an encoded-layout hint.</summary>
    public bool HasJfifColorSpace;
    /// <summary>The encoded-layout hint derived from JFIF.</summary>
    public JpegColorSpace JfifColorSpace;
    /// <summary>Indicates that Exif supplied a rendered color-space hint.</summary>
    public bool HasExifColorSpace;
    /// <summary>The rendered color-space hint derived from Exif.</summary>
    public JpegColorSpace ExifColorSpace;
    /// <summary>Indicates that Adobe APP14 supplied an encoded-layout hint.</summary>
    public bool HasAdobeColorSpace;
    /// <summary>The encoded-layout hint derived from Adobe APP14.</summary>
    public JpegColorSpace AdobeColorSpace;
    /// <summary>Indicates that frame component identifiers supplied an encoded-layout hint.</summary>
    public bool HasComponentIdentifierColorSpace;
    /// <summary>The encoded-layout hint derived from frame component identifiers.</summary>
    public JpegColorSpace ComponentIdentifierColorSpace;
    /// <summary>The frame component identifiers in encoded order.</summary>
    public List<int> ComponentIdentifiers = new List<int>();
    /// <summary>Indicates that an ICC profile supplied a rendered color-space hint.</summary>
    public bool HasIccColorSpace;
    /// <summary>The rendered color-space hint derived from the ICC profile header.</summary>
    public JpegColorSpace IccColorSpace;
    /// <summary>Indicates that valid CICP matrix coefficients supplied an encoded-layout hint.</summary>
    public bool HasCicpColorSpace;
    /// <summary>The encoded-layout hint derived from CICP matrix coefficients.</summary>
    public JpegColorSpace CicpColorSpace;
}

/// <summary>Describes one JFXX APP0 thumbnail extension.</summary>
public class JpegJfxxMetadata
{
    /// <summary>Creates empty JFXX metadata with initialized thumbnail and extension buffers.</summary>
    public JpegJfxxMetadata()
    {
    }

    /// <summary>Indicates that a valid JFXX identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The JFXX extension code that selects the thumbnail encoding.</summary>
    public int ExtensionCode;
    /// <summary>Indicates that the extension contains an embedded JPEG stream.</summary>
    public bool IsJpegThumbnail;
    /// <summary>Indicates that the extension contains a palette and indexed pixels.</summary>
    public bool IsIndexedThumbnail;
    /// <summary>Indicates that the extension contains packed RGB pixels.</summary>
    public bool IsRgbThumbnail;
    /// <summary>The thumbnail width when it is stored outside an embedded JPEG header.</summary>
    public int ThumbnailWidth;
    /// <summary>The thumbnail height when it is stored outside an embedded JPEG header.</summary>
    public int ThumbnailHeight;
    /// <summary>The embedded JPEG thumbnail bytes.</summary>
    public byte[] JpegThumbnailData = new byte[0];
    /// <summary>The packed RGB thumbnail pixels.</summary>
    public byte[] RgbThumbnailPixels = new byte[0];
    /// <summary>The 256-entry RGB palette for an indexed thumbnail.</summary>
    public byte[] IndexedThumbnailPalette = new byte[0];
    /// <summary>The palette indices for an indexed thumbnail.</summary>
    public byte[] IndexedThumbnailPixels = new byte[0];
    /// <summary>The raw bytes following the JFXX extension code.</summary>
    public byte[] ExtensionData = new byte[0];
}

/// <summary>Describes the Adobe APP14 header used to interpret RGB, YCbCr, CMYK, and YCCK component data.</summary>
public class JpegAdobeMetadata
{
    /// <summary>Creates empty Adobe metadata with no transform selected.</summary>
    public JpegAdobeMetadata()
    {
    }

    /// <summary>Indicates that a valid Adobe identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The Adobe header version.</summary>
    public int Version;
    /// <summary>The first Adobe flag word, preserved without interpretation.</summary>
    public int Flags0;
    /// <summary>The second Adobe flag word, preserved without interpretation.</summary>
    public int Flags1;
    /// <summary>The Adobe transform code: zero for no transform, one for YCbCr, or two for YCCK.</summary>
    public int Transform;
    /// <summary>Indicates that the transform code is not one of the standard values.</summary>
    public bool IsUnknownTransform;
    /// <summary>Indicates that the marker declares a YCbCr transform.</summary>
    public bool IsYCbCrTransform;
    /// <summary>Indicates that the marker declares a YCCK transform.</summary>
    public bool IsYCCKTransform;
    /// <summary>The color space implied by the transform code alone.</summary>
    public JpegColorSpace TransformColorSpace;
    /// <summary>The color space after also considering the image component count.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>Indicates that Adobe-style inverted CMYK sample interpretation applies.</summary>
    public bool InvertsCmyk;
}

/// <summary>Contains commonly used values parsed from an Exif TIFF structure and its optional thumbnail.</summary>
/// <remarks>Each optional value has a matching <c>Has...</c> field because zero can be a valid TIFF value.</remarks>
public class JpegExifMetadata
{
    /// <summary>Creates empty Exif metadata with initialized payload, text, header, and thumbnail fields.</summary>
    public JpegExifMetadata()
    {
    }

    /// <summary>Indicates that a valid Exif identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The complete Exif APP1 payload, including the Exif identifier.</summary>
    public byte[] RawPayload = new byte[0];
    /// <summary>The TIFF bytes after the Exif identifier.</summary>
    public byte[] TiffData = new byte[0];
    /// <summary>The byte offset at which TIFF data begins in <c>RawPayload</c>.</summary>
    public int TiffOffset;
    /// <summary>The parsed TIFF byte order and first-directory header.</summary>
    public JpegExifTiffHeader TiffHeader = new JpegExifTiffHeader();
    /// <summary>The primary, Exif, GPS, and thumbnail directories that were traversed.</summary>
    public List<JpegExifIfd> Ifds = new List<JpegExifIfd>();
    /// <summary>Indicates that the primary IFD contains an image-width tag.</summary>
    public bool HasImageWidth;
    /// <summary>The primary image width reported by Exif.</summary>
    public long ImageWidth;
    /// <summary>Indicates that the primary IFD contains an image-height tag.</summary>
    public bool HasImageHeight;
    /// <summary>The primary image height reported by Exif.</summary>
    public long ImageHeight;
    /// <summary>Indicates that an orientation tag was parsed.</summary>
    public bool HasOrientation;
    /// <summary>The TIFF orientation value from one through eight.</summary>
    public int Orientation;
    /// <summary>Indicates that horizontal resolution was parsed.</summary>
    public bool HasXResolution;
    /// <summary>The horizontal resolution in the unit selected by <c>ResolutionUnit</c>.</summary>
    public double XResolution;
    /// <summary>Indicates that vertical resolution was parsed.</summary>
    public bool HasYResolution;
    /// <summary>The vertical resolution in the unit selected by <c>ResolutionUnit</c>.</summary>
    public double YResolution;
    /// <summary>Indicates that the resolution-unit tag was parsed.</summary>
    public bool HasResolutionUnit;
    /// <summary>The TIFF resolution-unit code.</summary>
    public int ResolutionUnit;
    /// <summary>Indicates that the Exif color-space tag was parsed.</summary>
    public bool HasColorSpace;
    /// <summary>The numeric Exif color-space value.</summary>
    public int ColorSpace;
    /// <summary>Indicates that the Exif pixel-width tag was parsed.</summary>
    public bool HasPixelWidth;
    /// <summary>The valid image width reported by the Exif sub-IFD.</summary>
    public long PixelWidth;
    /// <summary>Indicates that the Exif pixel-height tag was parsed.</summary>
    public bool HasPixelHeight;
    /// <summary>The valid image height reported by the Exif sub-IFD.</summary>
    public long PixelHeight;
    /// <summary>Indicates that the general date and time string was parsed.</summary>
    public bool HasDateTime;
    /// <summary>The general Exif date and time string without format conversion.</summary>
    public string DateTime = "";
    /// <summary>Indicates that the original-capture date and time was parsed.</summary>
    public bool HasDateTimeOriginal;
    /// <summary>The original-capture date and time string without format conversion.</summary>
    public string DateTimeOriginal = "";
    /// <summary>Indicates that a camera manufacturer string was parsed.</summary>
    public bool HasMake;
    /// <summary>The camera manufacturer string.</summary>
    public string Make = "";
    /// <summary>Indicates that a camera model string was parsed.</summary>
    public bool HasModel;
    /// <summary>The camera model string.</summary>
    public string Model = "";
    /// <summary>Indicates that a software string was parsed.</summary>
    public bool HasSoftware;
    /// <summary>The software name recorded by Exif.</summary>
    public string Software = "";
    /// <summary>Indicates that a lens model string was parsed.</summary>
    public bool HasLensModel;
    /// <summary>The recorded lens model.</summary>
    public string LensModel = "";
    /// <summary>Indicates that the primary IFD points to an Exif sub-IFD.</summary>
    public bool HasExifIfd;
    /// <summary>The Exif sub-IFD offset relative to the TIFF header.</summary>
    public long ExifIfdOffset;
    /// <summary>Indicates that the primary IFD points to a GPS sub-IFD.</summary>
    public bool HasGpsIfd;
    /// <summary>The GPS sub-IFD offset relative to the TIFF header.</summary>
    public long GpsIfdOffset;
    /// <summary>Indicates that a complete GPS latitude was parsed.</summary>
    public bool HasGpsLatitude;
    /// <summary>The GPS latitude in decimal degrees before applying the latitude reference sign.</summary>
    public double GpsLatitude;
    /// <summary>The latitude reference, normally <c>N</c> or <c>S</c>.</summary>
    public string GpsLatitudeRef = "";
    /// <summary>Indicates that a complete GPS longitude was parsed.</summary>
    public bool HasGpsLongitude;
    /// <summary>The GPS longitude in decimal degrees before applying the longitude reference sign.</summary>
    public double GpsLongitude;
    /// <summary>The longitude reference, normally <c>E</c> or <c>W</c>.</summary>
    public string GpsLongitudeRef = "";
    /// <summary>Indicates that a thumbnail IFD was found.</summary>
    public bool HasThumbnailIfd;
    /// <summary>The thumbnail IFD offset relative to the TIFF header.</summary>
    public long ThumbnailIfdOffset;
    /// <summary>Indicates that thumbnail compression was parsed.</summary>
    public bool HasThumbnailCompression;
    /// <summary>The TIFF compression code used by the thumbnail.</summary>
    public int ThumbnailCompression;
    /// <summary>Indicates that the embedded JPEG thumbnail offset was parsed.</summary>
    public bool HasJpegThumbnailOffset;
    /// <summary>The embedded JPEG thumbnail offset relative to the TIFF header.</summary>
    public long JpegThumbnailOffset;
    /// <summary>Indicates that the embedded JPEG thumbnail length was parsed.</summary>
    public bool HasJpegThumbnailLength;
    /// <summary>The declared embedded JPEG thumbnail byte length.</summary>
    public long JpegThumbnailLength;
    /// <summary>Indicates that a complete embedded JPEG thumbnail was extracted.</summary>
    public bool HasJpegThumbnail;
    /// <summary>The extracted embedded JPEG thumbnail data.</summary>
    public byte[] JpegThumbnailData = new byte[0];
    /// <summary>Indicates that an uncompressed thumbnail width was parsed.</summary>
    public bool HasThumbnailWidth;
    /// <summary>The uncompressed thumbnail width.</summary>
    public long ThumbnailWidth;
    /// <summary>Indicates that an uncompressed thumbnail height was parsed.</summary>
    public bool HasThumbnailHeight;
    /// <summary>The uncompressed thumbnail height.</summary>
    public long ThumbnailHeight;
    /// <summary>Indicates that thumbnail photometric interpretation was parsed.</summary>
    public bool HasThumbnailPhotometricInterpretation;
    /// <summary>The TIFF photometric-interpretation code for the thumbnail.</summary>
    public int ThumbnailPhotometricInterpretation;
    /// <summary>Indicates that the thumbnail strip offset was parsed.</summary>
    public bool HasThumbnailStripOffset;
    /// <summary>The thumbnail strip offset relative to the TIFF header.</summary>
    public long ThumbnailStripOffset;
    /// <summary>Indicates that the thumbnail strip length was parsed.</summary>
    public bool HasThumbnailStripByteCount;
    /// <summary>The thumbnail strip byte length.</summary>
    public long ThumbnailStripByteCount;
    /// <summary>Indicates that the thumbnail sample count was parsed.</summary>
    public bool HasThumbnailSamplesPerPixel;
    /// <summary>The number of samples in each uncompressed thumbnail pixel.</summary>
    public int ThumbnailSamplesPerPixel;
    /// <summary>Indicates that a complete packed RGB thumbnail was extracted.</summary>
    public bool HasRgbThumbnail;
    /// <summary>The extracted packed RGB thumbnail pixels.</summary>
    public byte[] RgbThumbnailPixels = new byte[0];
    /// <summary>Indicates that a complete packed YCbCr thumbnail was extracted.</summary>
    public bool HasYCbCrThumbnail;
    /// <summary>The extracted packed YCbCr thumbnail pixels.</summary>
    public byte[] YCbCrThumbnailPixels = new byte[0];
}

/// <summary>Describes the byte order and first-directory pointer at the start of Exif TIFF data.</summary>
public class JpegExifTiffHeader
{
    /// <summary>Creates an empty TIFF header with unknown byte order.</summary>
    public JpegExifTiffHeader()
    {
    }

    /// <summary>Indicates that a complete valid TIFF header was parsed.</summary>
    public bool IsPresent;
    /// <summary>The byte order used for all multibyte TIFF values.</summary>
    public JpegTiffByteOrder ByteOrder;
    /// <summary>Indicates Intel-style least-significant-byte-first order.</summary>
    public bool IsLittleEndian;
    /// <summary>Indicates Motorola-style most-significant-byte-first order.</summary>
    public bool IsBigEndian;
    /// <summary>The TIFF magic value, normally 42.</summary>
    public int Magic;
    /// <summary>The first IFD offset relative to the TIFF header.</summary>
    public long FirstIfdOffset;
}

/// <summary>Contains one traversed Exif TIFF image-file directory and its tag entries.</summary>
public class JpegExifIfd
{
    /// <summary>Creates an empty directory with an initialized tag collection.</summary>
    public JpegExifIfd()
    {
    }

    /// <summary>The semantic role assigned while traversing the directory graph.</summary>
    public JpegExifIfdKind Kind;
    /// <summary>The directory offset relative to the TIFF header.</summary>
    public long Offset;
    /// <summary>The next-directory offset stored after the entries, or zero.</summary>
    public long NextIfdOffset;
    /// <summary>The tags in their encoded directory order.</summary>
    public List<JpegExifTag> Tags = new List<JpegExifTag>();
}

/// <summary>Describes one Exif TIFF tag entry and its decoded or raw value.</summary>
public class JpegExifTag
{
    /// <summary>Creates an empty tag with an initialized value.</summary>
    public JpegExifTag()
    {
    }

    /// <summary>The TIFF tag identifier.</summary>
    public int Identifier;
    /// <summary>The TIFF field type.</summary>
    public JpegExifTagType Type;
    /// <summary>The number of typed values.</summary>
    public long Count;
    /// <summary>The entry offset relative to the TIFF header.</summary>
    public long EntryOffset;
    /// <summary>The external value offset, or the encoded inline value word for values no larger than four bytes.</summary>
    public long ValueOffset;
    /// <summary>The exact value bytes and decoded convenience representations.</summary>
    public JpegExifTagValue Value = new JpegExifTagValue();
}

/// <summary>Contains exact Exif tag bytes together with decoded text, integer, and rational values when applicable.</summary>
public class JpegExifTagValue
{
    /// <summary>Creates an empty value with initialized byte and numeric collections.</summary>
    public JpegExifTagValue()
    {
    }

    /// <summary>The exact value bytes in TIFF byte order.</summary>
    public byte[] RawBytes = new byte[0];
    /// <summary>The decoded ASCII text before the first null byte, or an empty string for non-ASCII values.</summary>
    public string Text = "";
    /// <summary>Decoded unsigned integer values for BYTE, SHORT, and LONG fields.</summary>
    public List<long> UnsignedValues = new List<long>();
    /// <summary>Decoded signed integer values for SBYTE, SSHORT, and SLONG fields.</summary>
    public List<long> SignedValues = new List<long>();
    /// <summary>Rational numerators in encoded order.</summary>
    public List<long> RationalNumerators = new List<long>();
    /// <summary>Rational denominators in encoded order.</summary>
    public List<long> RationalDenominators = new List<long>();
    /// <summary>Decoded rational values, with zero used when an encoded denominator is zero.</summary>
    public List<double> RationalValues = new List<double>();
}

/// <summary>Contains a standard XMP APP1 payload in both byte and text form.</summary>
public class JpegXmpMetadata
{
    /// <summary>Creates empty XMP metadata with initialized byte and text representations.</summary>
    public JpegXmpMetadata()
    {
    }

    /// <summary>Indicates that a valid standard XMP namespace identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The complete APP1 payload, including the XMP namespace identifier.</summary>
    public byte[] RawPayload = new byte[0];
    /// <summary>The XMP packet bytes after the namespace identifier.</summary>
    public byte[] PacketData = new byte[0];
    /// <summary>The XMP packet decoded as UTF-8 text.</summary>
    public string Packet = "";
}

/// <summary>Contains one Extended XMP APP1 chunk.</summary>
public class JpegExtendedXmpChunk
{
    /// <summary>Creates an empty Extended XMP chunk with initialized buffers and identifiers.</summary>
    public JpegExtendedXmpChunk()
    {
    }

    /// <summary>Indicates that an Extended XMP namespace identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The complete APP1 payload, including the Extended XMP namespace identifier and chunk header.</summary>
    public byte[] RawPayload = new byte[0];
    /// <summary>The 32-character hexadecimal identifier shared by every chunk in the packet.</summary>
    public string Guid = "";
    /// <summary>The declared size of the complete Extended XMP packet.</summary>
    public long FullLength;
    /// <summary>The zero-based destination offset of this chunk in the complete packet.</summary>
    public long Offset;
    /// <summary>The packet bytes carried by this chunk.</summary>
    public byte[] Data = new byte[0];
}

/// <summary>Describes one Extended XMP validation or reassembly problem.</summary>
public class JpegXmpReassemblyDiagnostic
{
    /// <summary>Creates an empty Extended XMP diagnostic with an initialized message.</summary>
    public JpegXmpReassemblyDiagnostic()
    {
    }

    /// <summary>The stable category of the problem.</summary>
    public JpegXmpReassemblyDiagnosticCode Code;
    /// <summary>A plain-language explanation of the problem.</summary>
    public string Message = "";
    /// <summary>The zero-based chunk index associated with the problem, or a negative value for a packet-wide problem.</summary>
    public int ChunkIndex;
    /// <summary>The packet offset associated with the problem, or a negative value when unavailable.</summary>
    public long Offset;
    /// <summary>The number of packet bytes associated with the problem.</summary>
    public long Length;
}

/// <summary>Contains an Extended XMP packet, its source chunks, and reassembly diagnostics.</summary>
public class JpegExtendedXmpMetadata
{
    /// <summary>Creates empty Extended XMP metadata with initialized packet, chunk, and diagnostic containers.</summary>
    public JpegExtendedXmpMetadata()
    {
    }

    /// <summary>Indicates that at least one Extended XMP chunk was present.</summary>
    public bool IsPresent;
    /// <summary>Indicates that every chunk was valid and the packet was reassembled without overlaps or gaps.</summary>
    public bool IsComplete;
    /// <summary>The validated 32-character hexadecimal identifier shared by the chunks.</summary>
    public string Guid = "";
    /// <summary>The declared complete packet length.</summary>
    public long FullLength;
    /// <summary>The reassembled packet bytes, or an empty array when reassembly was incomplete.</summary>
    public byte[] PacketData = new byte[0];
    /// <summary>The complete packet decoded as UTF-8 text, or an empty string when reassembly was incomplete.</summary>
    public string Packet = "";
    /// <summary>The source chunks in marker order.</summary>
    public List<JpegExtendedXmpChunk> Chunks = new List<JpegExtendedXmpChunk>();
    /// <summary>Validation and reassembly problems in discovery order.</summary>
    public List<JpegXmpReassemblyDiagnostic> Diagnostics = new List<JpegXmpReassemblyDiagnostic>();
}

/// <summary>Contains the MPF APP2 payload and its embedded TIFF data.</summary>
public class JpegMpfMetadata
{
    /// <summary>Creates empty MPF metadata with initialized payload buffers.</summary>
    public JpegMpfMetadata()
    {
    }

    /// <summary>Indicates that a valid MPF identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The complete MPF APP2 payload, including its identifier.</summary>
    public byte[] RawPayload = new byte[0];
    /// <summary>The TIFF bytes following the MPF identifier.</summary>
    public byte[] TiffData = new byte[0];
}

/// <summary>Contains Photoshop APP13 image-resource blocks and an extracted IPTC resource when present.</summary>
public class JpegPhotoshopMetadata
{
    /// <summary>Creates empty Photoshop metadata with initialized resource and payload containers.</summary>
    public JpegPhotoshopMetadata()
    {
    }

    /// <summary>Indicates that a valid Photoshop APP13 identifier was found.</summary>
    public bool IsPresent;
    /// <summary>The complete Photoshop APP13 payload.</summary>
    public byte[] RawPayload = new byte[0];
    /// <summary>The parsed image-resource blocks in payload order.</summary>
    public List<JpegPhotoshopResourceBlock> ResourceBlocks = new List<JpegPhotoshopResourceBlock>();
    /// <summary>Indicates that an IPTC resource block was found.</summary>
    public bool HasIptc;
    /// <summary>The raw data from the IPTC resource block.</summary>
    public byte[] IptcData = new byte[0];
}

/// <summary>Describes one Photoshop image-resource block from an APP13 payload.</summary>
public class JpegPhotoshopResourceBlock
{
    /// <summary>Creates an empty Photoshop resource block with initialized text and data fields.</summary>
    public JpegPhotoshopResourceBlock()
    {
    }

    /// <summary>Indicates that the resource block was structurally complete.</summary>
    public bool IsPresent;
    /// <summary>The block's byte offset within the Photoshop payload.</summary>
    public long Offset;
    /// <summary>The four-character resource signature, normally <c>8BIM</c>.</summary>
    public string Signature = "";
    /// <summary>The numeric Photoshop resource identifier.</summary>
    public int ResourceId;
    /// <summary>The Pascal-style resource name decoded as text.</summary>
    public string Name = "";
    /// <summary>The resource-data byte offset within the Photoshop payload.</summary>
    public long DataOffset;
    /// <summary>The resource-data byte length before padding.</summary>
    public long Size;
    /// <summary>A copy of the resource data without alignment padding.</summary>
    public byte[] Data = new byte[0];
    /// <summary>Indicates that this is the IPTC-NAA resource.</summary>
    public bool IsIptc;
}

/// <summary>Describes the header, tags, and built-in conversion assessment of a reassembled ICC color profile.</summary>
public class JpegIccProfile
{
    /// <summary>Creates an empty ICC profile description with initialized signatures, identifier, and tag collection.</summary>
    public JpegIccProfile()
    {
    }

    /// <summary>Indicates that a complete valid ICC header was parsed.</summary>
    public bool IsPresent;
    /// <summary>The profile size declared by the ICC header.</summary>
    public long ProfileSize;
    /// <summary>The four-character preferred color-management module signature.</summary>
    public string CmmType = "";
    /// <summary>The major ICC format version.</summary>
    public int VersionMajor;
    /// <summary>The minor ICC format version.</summary>
    public int VersionMinor;
    /// <summary>The ICC format bug-fix version.</summary>
    public int VersionBugFix;
    /// <summary>The four-character profile or device class signature.</summary>
    public string ProfileClass = "";
    /// <summary>The four-character input color-space signature.</summary>
    public string ColorSpace = "";
    /// <summary>The four-character profile-connection-space signature.</summary>
    public string Pcs = "";
    /// <summary>The profile creation year.</summary>
    public int Year;
    /// <summary>The profile creation month.</summary>
    public int Month;
    /// <summary>The profile creation day.</summary>
    public int Day;
    /// <summary>The profile creation hour.</summary>
    public int Hour;
    /// <summary>The profile creation minute.</summary>
    public int Minute;
    /// <summary>The profile creation second.</summary>
    public int Second;
    /// <summary>The four-character ICC magic signature, normally <c>acsp</c>.</summary>
    public string Signature = "";
    /// <summary>The four-character primary-platform signature.</summary>
    public string Platform = "";
    /// <summary>The raw ICC header flags.</summary>
    public long Flags;
    /// <summary>The four-character device-manufacturer signature.</summary>
    public string DeviceManufacturer = "";
    /// <summary>The four-character device-model signature.</summary>
    public string DeviceModel = "";
    /// <summary>The numeric ICC rendering intent.</summary>
    public long RenderingIntent;
    /// <summary>The typed ICC rendering intent, or <see cref="JpegIccRenderingIntent.Unknown"/> for an invalid encoded value.</summary>
    public JpegIccRenderingIntent RenderingIntentKind;
    /// <summary>The X coordinate of the profile illuminant.</summary>
    public double IlluminantX;
    /// <summary>The Y coordinate of the profile illuminant.</summary>
    public double IlluminantY;
    /// <summary>The Z coordinate of the profile illuminant.</summary>
    public double IlluminantZ;
    /// <summary>The four-character profile-creator signature.</summary>
    public string Creator = "";
    /// <summary>The 16-byte ICC profile identifier.</summary>
    public byte[] ProfileId = new byte[16];
    /// <summary>The number of entries declared by the ICC tag table.</summary>
    public int TagCount;
    /// <summary>The parsed ICC tag-directory entries in table order.</summary>
    public List<JpegIccTag> Tags = new List<JpegIccTag>();
    /// <summary>Indicates that the tag directory contains a <c>cicp</c> entry.</summary>
    public bool HasCicp;
    /// <summary>The coding-independent color code points parsed from the ICC profile.</summary>
    public JpegCicpMetadata Cicp = new JpegCicpMetadata();
    /// <summary>The built-in matrix/TRC conversion assessment and diagnostics.</summary>
    public JpegIccConversionResult Conversion = new JpegIccConversionResult();
}

/// <summary>Identifies and exposes one tagged data element in an ICC profile.</summary>
public class JpegIccTag
{
    /// <summary>Creates an empty ICC tag-directory entry.</summary>
    public JpegIccTag()
    {
    }

    /// <summary>The four-character tag signature.</summary>
    public string Signature = "";
    /// <summary>The tag-data offset from the start of the profile.</summary>
    public long Offset;
    /// <summary>The tag-data byte length.</summary>
    public long Size;
    /// <summary>The four-character tag payload type signature, or an empty string when the payload is shorter than four bytes.</summary>
    public string TypeSignature = "";
    /// <summary>An independent copy of the complete tag payload.</summary>
    public byte[] Payload = new byte[0];
}

/// <summary>Describes the matrix and tone curves used by a supported RGB, Gray, or CMYK ICC conversion.</summary>
public class JpegIccMatrixTrcProfile
{
    /// <summary>Creates an empty matrix/TRC description.</summary>
    public JpegIccMatrixTrcProfile()
    {
    }

    /// <summary>Indicates that a supported matrix/TRC description was parsed.</summary>
    public bool IsPresent;
    /// <summary>Indicates an RGB source profile.</summary>
    public bool IsRgb;
    /// <summary>Indicates a Gray source profile.</summary>
    public bool IsGray;
    /// <summary>Indicates a CMYK source profile.</summary>
    public bool IsCmyk;
    /// <summary>The source color space represented by this description.</summary>
    public JpegColorSpace SourceColorSpace;
    /// <summary>The explicit destination color space produced by this description.</summary>
    public JpegIccDestinationColorSpace DestinationColorSpace;
    /// <summary>The rendering intent declared by the source profile.</summary>
    public JpegIccRenderingIntent RenderingIntent;
    /// <summary>The red-primary X coordinate.</summary>
    public double RedX;
    /// <summary>The red-primary Y coordinate.</summary>
    public double RedY;
    /// <summary>The red-primary Z coordinate.</summary>
    public double RedZ;
    /// <summary>The green-primary X coordinate.</summary>
    public double GreenX;
    /// <summary>The green-primary Y coordinate.</summary>
    public double GreenY;
    /// <summary>The green-primary Z coordinate.</summary>
    public double GreenZ;
    /// <summary>The blue-primary X coordinate.</summary>
    public double BlueX;
    /// <summary>The blue-primary Y coordinate.</summary>
    public double BlueY;
    /// <summary>The blue-primary Z coordinate.</summary>
    public double BlueZ;
    /// <summary>The media-white X coordinate.</summary>
    public double WhiteX;
    /// <summary>The media-white Y coordinate.</summary>
    public double WhiteY;
    /// <summary>The media-white Z coordinate.</summary>
    public double WhiteZ;
    /// <summary>The red-channel gamma exponent.</summary>
    public double RedGamma;
    /// <summary>The green-channel gamma exponent.</summary>
    public double GreenGamma;
    /// <summary>The blue-channel gamma exponent.</summary>
    public double BlueGamma;
    /// <summary>The Gray-channel gamma exponent.</summary>
    public double GrayGamma;
    /// <summary>The cyan-channel gamma exponent.</summary>
    public double CyanGamma;
    /// <summary>The magenta-channel gamma exponent.</summary>
    public double MagentaGamma;
    /// <summary>The yellow-channel gamma exponent.</summary>
    public double YellowGamma;
    /// <summary>The black-channel gamma exponent.</summary>
    public double BlackGamma;
}

/// <summary>Describes one ICC conversion-assessment problem.</summary>
public class JpegIccConversionDiagnostic
{
    /// <summary>Creates an empty ICC conversion diagnostic with initialized strings.</summary>
    public JpegIccConversionDiagnostic()
    {
    }

    /// <summary>The stable category of the problem.</summary>
    public JpegIccConversionDiagnosticCode Code;
    /// <summary>A plain-language explanation of the problem.</summary>
    public string Message = "";
    /// <summary>The related four-character ICC tag signature, or an empty string for a profile-wide problem.</summary>
    public string TagSignature = "";
}

/// <summary>Reports whether and how the built-in ICC matrix/TRC conversion path can use a profile.</summary>
public class JpegIccConversionResult
{
    /// <summary>Creates an unassessed ICC conversion result with initialized descriptions and diagnostics.</summary>
    public JpegIccConversionResult()
    {
    }

    /// <summary>The conversion assessment outcome.</summary>
    public JpegIccConversionStatus Status;
    /// <summary>The profile input color space recognized by the conversion path.</summary>
    public JpegColorSpace SourceColorSpace;
    /// <summary>The explicit destination color space for supported conversion.</summary>
    public JpegIccDestinationColorSpace DestinationColorSpace;
    /// <summary>The rendering intent used by the conversion description.</summary>
    public JpegIccRenderingIntent RenderingIntent;
    /// <summary>A summary of the conversion outcome.</summary>
    public string Message = "";
    /// <summary>The parsed matrix and tone curves when the profile uses supported tag forms.</summary>
    public JpegIccMatrixTrcProfile MatrixTrcProfile = new JpegIccMatrixTrcProfile();
    /// <summary>Reasons the built-in conversion path cannot use the profile.</summary>
    public List<JpegIccConversionDiagnostic> Diagnostics = new List<JpegIccConversionDiagnostic>();
}

/// <summary>Controls JPEG parsing, pixel conversion, inverse transforms, metadata handling, and resource limits.</summary>
/// <remarks>Zero-valued policy fields select the documented conservative defaults. Resource limits are initialized to practical nonzero defaults.</remarks>
public class JpegReadOptions
{
    /// <summary>Creates read options with conservative policies, unit IDCT scaling, and default resource limits.</summary>
    public JpegReadOptions()
    {
    }

    /// <summary>Selects which metadata is retained in returned objects.</summary>
    public JpegMetadataPolicy MetadataPolicy = JpegMetadataPolicy.PreserveAll;
    /// <summary>Selects whether decoded samples remain in their source color space or are converted.</summary>
    public JpegColorPolicy ColorPolicy = JpegColorPolicy.PreserveSource;
    /// <summary>Selects how an embedded ICC profile is preserved or used.</summary>
    public JpegIccBehaviorPolicy IccBehaviorPolicy = JpegIccBehaviorPolicy.Preserve;
    /// <summary>Selects strict rejection or tolerant diagnostics for recoverable malformed data.</summary>
    public JpegErrorPolicy ErrorPolicy = JpegErrorPolicy.Strict;
    /// <summary>The requested packed pixel layout, or <see cref="JpegPixelFormat.Unknown"/> to choose from the source.</summary>
    public JpegPixelFormat OutputPixelFormat = JpegPixelFormat.Unknown;
    /// <summary>Selects box replication or interpolated reconstruction for subsampled chroma.</summary>
    public JpegChromaUpsamplingMode ChromaUpsamplingMode = JpegChromaUpsamplingMode.Box;
    /// <summary>Returns the decoded component planes without chroma upsampling or color conversion.</summary>
    public bool RawComponentOutput = false;
    /// <summary>Selects best-effort raw output or exact rejection when the source color interpretation is unknown.</summary>
    public JpegUnknownColorHandling UnknownColorHandling = JpegUnknownColorHandling.BestEffort;
    /// <summary>Selects automatic or forced handling of inverted Adobe CMYK component samples.</summary>
    public JpegAdobeInversionMode AdobeInversionMode = JpegAdobeInversionMode.Automatic;
    /// <summary>Selects full-range or limited-range interpretation of YCbCr samples.</summary>
    public JpegYCbCrRange YCbCrRange = JpegYCbCrRange.Full;
    /// <summary>Selects the accuracy and speed tradeoff for inverse DCT calculations.</summary>
    public JpegInverseDctMethod InverseDctMethod = JpegInverseDctMethod.Accurate;
    /// <summary>The numerator of the requested IDCT output scale.</summary>
    public int IdctScaleNumerator = 1;
    /// <summary>The denominator of the requested IDCT output scale.</summary>
    public int IdctScaleDenominator = 1;
    /// <summary>Selects a named supported IDCT ratio, or automatic selection from target dimensions and explicit ratio fields.</summary>
    public JpegIdctScaleRatio IdctScaleRatio = JpegIdctScaleRatio.Automatic;
    /// <summary>The preferred maximum output width, or zero when width does not select the IDCT ratio.</summary>
    public int TargetWidth = 0;
    /// <summary>The preferred maximum output height, or zero when height does not select the IDCT ratio.</summary>
    public int TargetHeight = 0;
    /// <summary>The source-coordinate region to decode. A zero width and height select the complete image.</summary>
    public JpegRectangle DecodeRegion = new JpegRectangle();
    /// <summary>Expands a requested region outward to source MCU boundaries before producing pixels.</summary>
    public bool FastMcuCrop = false;
    /// <summary>The largest accepted image width; larger headers cause a resource-limit exception.</summary>
    public int MaxWidth = JpegConstants.DefaultMaxWidth;
    /// <summary>The largest accepted image height; larger headers cause a resource-limit exception.</summary>
    public int MaxHeight = JpegConstants.DefaultMaxHeight;
    /// <summary>The largest accepted component count.</summary>
    public int MaxComponents = JpegConstants.DefaultMaxComponents;
    /// <summary>The largest accepted number of scan headers.</summary>
    public int MaxScans = JpegConstants.DefaultMaxScans;
    /// <summary>The largest accepted payload in one length-bearing marker, or a negative value to disable this limit.</summary>
    public int MaxMarkerPayloadBytes = JpegConstants.DefaultMaxMarkerPayloadBytes;
    /// <summary>The largest number of APPn, COM, and unknown marker payloads retained in metadata, or a negative value to disable this limit.</summary>
    public int MaxRetainedMarkers = JpegConstants.DefaultMaxRetainedMarkers;
    /// <summary>The largest combined number of parsed or supplied quantization, Huffman, and arithmetic-conditioning tables, or a negative value to disable this limit.</summary>
    public int MaxTables = JpegConstants.DefaultMaxTables;
    /// <summary>The largest total metadata payload size in bytes.</summary>
    public long MaxMetadataBytes = JpegConstants.DefaultMaxMetadataBytes;
    /// <summary>The largest accepted entropy-coded payload size in bytes.</summary>
    public long MaxEntropyBytes = JpegConstants.DefaultMaxEntropyBytes;
    /// <summary>The largest decoded output pixel count.</summary>
    public long MaxOutputPixels = JpegConstants.DefaultMaxOutputPixels;
    /// <summary>The largest decoded output allocation in bytes.</summary>
    public long MaxOutputBytes = JpegConstants.DefaultMaxOutputBytes;
    /// <summary>The largest combined decoded planar component allocation in bytes, or a negative value to disable this limit.</summary>
    public long MaxComponentBufferBytes = JpegConstants.DefaultMaxComponentBufferBytes;
    /// <summary>The largest progressive coefficient-state allocation in bytes, or a negative value to disable this limit.</summary>
    public long MaxProgressiveStateBytes = JpegConstants.DefaultMaxProgressiveStateBytes;
    /// <summary>The largest sample payload exposed in one row or progressive-preview callback, or a negative value to disable this limit.</summary>
    public long MaxCallbackOutputBytes = JpegConstants.DefaultMaxCallbackOutputBytes;
    internal JpegTableStore TableStore = new JpegTableStore();
    /// <summary>Indicates whether Exif orientation should be applied to decoded pixels.</summary>
    public bool AutoOrient = false;
    /// <summary>Indicates whether metadata payload parsing should be skipped when the operation permits it.</summary>
    public bool SkipMetadata = false;
}

/// <summary>Controls JPEG quality, component sampling, entropy optimization, restart markers, and metadata output.</summary>
public class JpegWriteOptions
{
    /// <summary>Creates write options with metadata preservation, box downsampling, and the default quality.</summary>
    public JpegWriteOptions()
    {
    }

    /// <summary>Selects a fixed encoder-settings bundle; preset-controlled fields take precedence over their individual values.</summary>
    public JpegEncoderPreset Preset = JpegEncoderPreset.None;
    /// <summary>Requests canonical encoder choices that produce byte-identical output for identical image data, metadata, and options.</summary>
    public bool Deterministic = false;
    /// <summary>Selects which image metadata is written or preserved.</summary>
    public JpegMetadataPolicy MetadataPolicy = JpegMetadataPolicy.PreserveAll;
    /// <summary>Selects source color preservation or conversion before encoding.</summary>
    public JpegColorPolicy ColorPolicy = JpegColorPolicy.PreserveSource;
    /// <summary>Selects how an embedded ICC profile is preserved or used.</summary>
    public JpegIccBehaviorPolicy IccBehaviorPolicy = JpegIccBehaviorPolicy.Preserve;
    /// <summary>Selects strict rejection or tolerant handling of recoverable input issues.</summary>
    public JpegErrorPolicy ErrorPolicy = JpegErrorPolicy.Strict;
    /// <summary>The JPEG coding process, or <see cref="JpegProcess.Unknown"/> to select baseline sequential Huffman coding.</summary>
    public JpegProcess Process = JpegProcess.Unknown;
    /// <summary>The encoded sample precision, or zero to use the image precision and then the process default.</summary>
    public int SamplePrecision = 0;
    /// <summary>Requires a particular entropy coder, or uses the coder implied by <c>Process</c>.</summary>
    public JpegEntropyCoding EntropyCoding = JpegEntropyCoding.Automatic;
    /// <summary>The prediction formula used by lossless processes.</summary>
    public JpegLosslessPredictorSelection LosslessPredictor = JpegLosslessPredictorSelection.Left;
    /// <summary>The number of low sample bits discarded by a lossless point transform.</summary>
    public int PointTransform = 0;
    /// <summary>Requests hierarchical JPEG framing; unsupported encoder paths report <see cref="JpegUnsupportedException"/>.</summary>
    public bool Hierarchical = false;
    /// <summary>Explicit component mappings in encoded frame order, or an empty list to derive the conventional order and sampling.</summary>
    public List<JpegEncoderComponent> Components = new List<JpegEncoderComponent>();
    /// <summary>User-supplied quantization, Huffman, and arithmetic-conditioning tables; non-empty collections replace generated tables of the same kind.</summary>
    public JpegTableSet Tables = new JpegTableSet();
    /// <summary>Reuses quantization tables and component assignments retained on the source image unless explicit quantization tables are supplied.</summary>
    public bool PreserveQuantizationTables = false;
    /// <summary>Explicit entropy-table assignments addressed by generated scan index and component identifier.</summary>
    public List<JpegEncoderScanTableAssignment> ScanTableAssignments = new List<JpegEncoderScanTableAssignment>();
    /// <summary>Controls whether components are grouped when the selected JPEG process permits interleaving.</summary>
    public JpegScanLayout ScanLayout = JpegScanLayout.Automatic;
    /// <summary>The desired chroma sampling preset, or <see cref="JpegSubsampling.Unknown"/> for the encoder default.</summary>
    public JpegSubsampling Subsampling = JpegSubsampling.Unknown;
    /// <summary>The filter used to combine source samples when reducing chroma resolution.</summary>
    public JpegChromaDownsamplingFilter ChromaDownsamplingFilter = JpegChromaDownsamplingFilter.Box;
    /// <summary>The general quality from 1 through 100 used when component-specific values are zero.</summary>
    public int Quality = JpegConstants.DefaultQuality;
    /// <summary>The luminance quality override from 1 through 100, or zero to use <c>Quality</c>.</summary>
    public int LuminanceQuality = 0;
    /// <summary>The chrominance quality override from 1 through 100, or zero to use <c>Quality</c>.</summary>
    public int ChrominanceQuality = 0;
    /// <summary>Requests progressive Huffman DCT output when <c>Process</c> is unspecified.</summary>
    public bool Progressive = false;
    /// <summary>The progressive scan script, or an empty script to use the standard DC-first and per-component AC sequence; a non-empty script selects progressive encoding when <c>Process</c> is unspecified.</summary>
    public JpegProgressiveScanScript ProgressiveScanScript = new JpegProgressiveScanScript();
    /// <summary>Requests Huffman tables derived from the image's observed coefficient frequencies.</summary>
    public bool OptimizeHuffmanTables = false;
    /// <summary>The number of units selected by <c>RestartIntervalUnit</c> between restart markers, or zero to omit restart markers.</summary>
    public int RestartInterval = 0;
    /// <summary>Whether <c>RestartInterval</c> is measured in MCU blocks or complete frame MCU rows.</summary>
    public JpegRestartIntervalUnit RestartIntervalUnit = JpegRestartIntervalUnit.McuBlocks;
}

/// <summary>Contains encoded JPEG bytes together with the concrete options used to produce them.</summary>
public class JpegWriteResult
{
    /// <summary>Creates an empty result with initialized byte and option values.</summary>
    public JpegWriteResult()
    {
    }

    /// <summary>The complete encoded JPEG byte sequence.</summary>
    public byte[] Data = new byte[0];
    /// <summary>The resolved options, including concrete process, precision, sampling, scan layout, entropy, and restart values.</summary>
    public JpegWriteOptions ResolvedOptions = new JpegWriteOptions();
}
