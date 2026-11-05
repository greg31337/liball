namespace barcode;

/// <summary>
/// Holds format, region, transform, validation, effort, and result controls for decoding.
/// </summary>
public class BarcodeDecodeOptions
{
    /// <summary>
    /// The formats permitted during decoding, or null to permit every supported format.
    /// </summary>
    public BarcodeFormat[]? AllowedFormats;

    /// <summary>
    /// The decoded data lengths permitted for formats with length filtering.
    /// </summary>
    public int[]? AllowedLengths;

    /// <summary>
    /// The image region to inspect, or null to inspect the complete image.
    /// </summary>
    public BarcodeRectangle? RegionOfInterest;

    /// <summary>
    /// The maximum number of results to return.
    /// </summary>
    public int MaximumResults;

    /// <summary>
    /// The expected number of results, or zero when unknown.
    /// </summary>
    public int ExpectedResultCount;

    /// <summary>
    /// Indicates whether the input is an already sampled pure symbol.
    /// </summary>
    public bool PureSymbol;

    /// <summary>
    /// The checksum validation and transmission policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// Indicates whether decoded GS1 data is parsed into semantic elements.
    /// </summary>
    public bool ParseGs1;

    /// <summary>
    /// The fallback character-encoding name when the symbol does not select one.
    /// </summary>
    public string? CharacterEncoding;

    /// <summary>
    /// The barcode polarity attempts to perform.
    /// </summary>
    public BarcodePolarity Polarity;

    /// <summary>
    /// The mirrored orientation attempts to perform.
    /// </summary>
    public BarcodeMirrorMode MirrorMode;

    /// <summary>
    /// The right-angle rotation attempts to perform.
    /// </summary>
    public BarcodeRotation[]? Rotations;

    /// <summary>
    /// The amount of image-processing effort to use.
    /// </summary>
    public BarcodeEffort Effort;

    /// <summary>
    /// The binarization strategy to use.
    /// </summary>
    public BarcodeBinarizationMode BinarizationMode;

    /// <summary>
    /// The localization strategy to use.
    /// </summary>
    public BarcodeLocalizationMode LocalizationMode;

    /// <summary>
    /// The maximum number of diagnostic entries to retain, or zero for the implementation default.
    /// </summary>
    public int MaximumDiagnostics;
}
