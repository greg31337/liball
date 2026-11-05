namespace barcode;

/// <summary>
/// Holds format, region, mode, effort, and count controls for localization-only detection.
/// </summary>
public class BarcodeDetectionOptions
{
    /// <summary>
    /// The formats permitted during detection, or null to permit every supported format.
    /// </summary>
    public BarcodeFormat[]? AllowedFormats;

    /// <summary>
    /// The image region to inspect, or null to inspect the complete image.
    /// </summary>
    public BarcodeRectangle? RegionOfInterest;

    /// <summary>
    /// The maximum number of candidates to return.
    /// </summary>
    public int MaximumCandidates;

    /// <summary>
    /// The expected number of candidates, or zero when unknown.
    /// </summary>
    public int ExpectedCandidateCount;

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
    /// The barcode polarity attempts to perform.
    /// </summary>
    public BarcodePolarity Polarity;

    /// <summary>
    /// The mirrored orientation attempts to perform.
    /// </summary>
    public BarcodeMirrorMode MirrorMode;

    /// <summary>
    /// Indicates whether right-angle rotation attempts are enabled.
    /// </summary>
    public bool TryRotations;

    /// <summary>
    /// The maximum number of diagnostic entries to retain, or zero for the implementation default.
    /// </summary>
    public int MaximumDiagnostics;
}
