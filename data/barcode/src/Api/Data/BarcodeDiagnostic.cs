namespace barcode;

/// <summary>
/// Holds a bounded diagnostic entry produced during barcode processing.
/// </summary>
public class BarcodeDiagnostic
{
    /// <summary>
    /// The processing stage that produced the diagnostic.
    /// </summary>
    public string? Stage;

    /// <summary>
    /// The stable machine-readable diagnostic code.
    /// </summary>
    public string? Code;

    /// <summary>
    /// The human-readable diagnostic message.
    /// </summary>
    public string? Message;

    /// <summary>
    /// Indicates whether numeric evidence is present.
    /// </summary>
    public bool HasNumericEvidence;

    /// <summary>
    /// The observed numeric value when numeric evidence is present.
    /// </summary>
    public double Value;

    /// <summary>
    /// The comparison threshold when one applies.
    /// </summary>
    public double Threshold;

    /// <summary>
    /// The image-space point associated with the diagnostic when available.
    /// </summary>
    public BarcodePoint? Location;

    /// <summary>
    /// The image-space region associated with the diagnostic when available.
    /// </summary>
    public BarcodeRectangle? Region;
}
