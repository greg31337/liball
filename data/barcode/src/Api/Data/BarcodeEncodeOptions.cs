namespace barcode;

/// <summary>
/// Holds format-independent barcode encoding options.
/// </summary>
public class BarcodeEncodeOptions
{
    /// <summary>
    /// The requested logical, raster, and physical sizing constraints.
    /// </summary>
    public BarcodeSizing? Sizing;

    /// <summary>
    /// The policy used to determine quiet-zone dimensions.
    /// </summary>
    public BarcodeQuietZonePolicy QuietZonePolicy;

    /// <summary>
    /// The custom quiet-zone dimensions when supplied.
    /// </summary>
    public BarcodeQuietZone? QuietZone;

    /// <summary>
    /// The check-character policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// The human-readable text mode.
    /// </summary>
    public BarcodeTextMode TextMode;

    /// <summary>
    /// The caller-supplied human-readable text when custom text is selected.
    /// </summary>
    public string? CustomText;

    /// <summary>
    /// The requested character-encoding name when one is explicitly selected.
    /// </summary>
    public string? CharacterEncoding;

    /// <summary>
    /// The requested ECI assignment number, or zero when no explicit assignment is requested.
    /// </summary>
    public int EciAssignmentNumber;

    /// <summary>
    /// The structured-append metadata when encoding one segment of a sequence.
    /// </summary>
    public StructuredAppendInfo? StructuredAppend;
}
