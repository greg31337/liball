namespace barcode;

/// <summary>
/// Holds the common format and processing selections used by barcode operations.
/// </summary>
public class BarcodeConfiguration
{
    /// <summary>
    /// The checksum request associated with the configuration when available.
    /// </summary>
    public BarcodeChecksumRequest? ChecksumRequest;

    /// <summary>
    /// The checksum result associated with the configuration when available.
    /// </summary>
    public BarcodeChecksumResult? ChecksumResult;

    /// <summary>
    /// The validation request associated with the configuration when available.
    /// </summary>
    public BarcodeValidationRequest? ValidationRequest;

    /// <summary>
    /// The validation report associated with the configuration when available.
    /// </summary>
    public BarcodeValidationReport? ValidationReport;

    /// <summary>
    /// The GS1 processing request associated with the configuration when available.
    /// </summary>
    public Gs1ProcessRequest? Gs1Request;

    /// <summary>
    /// The GS1 processing result associated with the configuration when available.
    /// </summary>
    public Gs1ProcessResult? Gs1Result;

    /// <summary>
    /// The structured-append request associated with the configuration when available.
    /// </summary>
    public StructuredAppendRequest? StructuredAppendRequest;

    /// <summary>
    /// The structured-append result associated with the configuration when available.
    /// </summary>
    public StructuredAppendResult? StructuredAppendResult;

    /// <summary>
    /// The render request associated with the configuration when available.
    /// </summary>
    public BarcodeRenderRequest? RenderRequest;

    /// <summary>
    /// The rendered output associated with the configuration when available.
    /// </summary>
    public BarcodeRenderedImage? RenderedImage;

    /// <summary>
    /// The decode request associated with the configuration when available.
    /// </summary>
    public BarcodeDecodeRequest? DecodeRequest;

    /// <summary>
    /// The exception-less decode outcome associated with the configuration when available.
    /// </summary>
    public BarcodeDecodeAttempt? DecodeAttempt;

    /// <summary>
    /// The detection request associated with the configuration when available.
    /// </summary>
    public BarcodeDetectionRequest? DetectionRequest;

    /// <summary>
    /// The detection result associated with the configuration when available.
    /// </summary>
    public BarcodeDetectionResult? DetectionResult;

    /// <summary>
    /// The encode request associated with the configuration when available.
    /// </summary>
    public BarcodeEncodeRequest? EncodeRequest;

    /// <summary>
    /// The encoded symbol associated with the configuration when available.
    /// </summary>
    public BarcodeSymbol? EncodedSymbol;

    /// <summary>
    /// The payload associated with the configuration when available.
    /// </summary>
    public BarcodePayload? Payload;

    /// <summary>
    /// The requested logical, raster, and physical sizing constraints.
    /// </summary>
    public BarcodeSizing? Sizing;

    /// <summary>
    /// The resolved size associated with the configuration when available.
    /// </summary>
    public BarcodeSize? ResolvedSize;

    /// <summary>
    /// The human-readable text layout associated with the configuration.
    /// </summary>
    public BarcodeTextLayout? TextLayout;

    /// <summary>
    /// The image-space region associated with the configuration.
    /// </summary>
    public BarcodeQuadrilateral? Region;

    /// <summary>
    /// The binarization strategy used for image processing.
    /// </summary>
    public BarcodeBinarizationMode BinarizationMode;

    /// <summary>
    /// The policy used to calculate or verify check characters.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// The dimensional organization of the barcode.
    /// </summary>
    public BarcodeDimension Dimension;

    /// <summary>
    /// The amount of image-processing effort to use.
    /// </summary>
    public BarcodeEffort Effort;

    /// <summary>
    /// The barcode format to process.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The strategy used to locate barcode candidates.
    /// </summary>
    public BarcodeLocalizationMode LocalizationMode;

    /// <summary>
    /// The mirrored orientations to consider.
    /// </summary>
    public BarcodeMirrorMode MirrorMode;

    /// <summary>
    /// The byte layout of image pixels.
    /// </summary>
    public BarcodePixelFormat PixelFormat;

    /// <summary>
    /// The expected barcode polarity.
    /// </summary>
    public BarcodePolarity Polarity;

    /// <summary>
    /// The policy used to determine quiet-zone dimensions.
    /// </summary>
    public BarcodeQuietZonePolicy QuietZonePolicy;

    /// <summary>
    /// The rendered output format.
    /// </summary>
    public BarcodeRenderFormat RenderFormat;

    /// <summary>
    /// The right-angle rotation applied during processing.
    /// </summary>
    public BarcodeRotation Rotation;

    /// <summary>
    /// The human-readable text selection.
    /// </summary>
    public BarcodeTextMode TextMode;

    /// <summary>
    /// The validation behavior to use.
    /// </summary>
    public BarcodeValidationMode ValidationMode;

    /// <summary>
    /// The minimum validation severity of interest.
    /// </summary>
    public BarcodeValidationSeverity ValidationSeverity;

    /// <summary>
    /// The selected Code 128 code set.
    /// </summary>
    public Code128CodeSet Code128CodeSet;

    /// <summary>
    /// The selected Code 39 character repertoire.
    /// </summary>
    public Code39CharacterMode Code39CharacterMode;

    /// <summary>
    /// The selected Data Matrix encodation mode.
    /// </summary>
    public DataMatrixEncodingMode DataMatrixEncodingMode;

    /// <summary>
    /// The permitted Data Matrix symbol shape.
    /// </summary>
    public DataMatrixShape DataMatrixShape;

    /// <summary>
    /// The GS1 source or target syntax.
    /// </summary>
    public Gs1Syntax Gs1Syntax;

    /// <summary>
    /// The selected PDF417 compaction mode.
    /// </summary>
    public Pdf417CompactionMode Pdf417CompactionMode;

    /// <summary>
    /// The selected PDF417 row-layout variant.
    /// </summary>
    public Pdf417Variant Pdf417Variant;
}
