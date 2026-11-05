namespace barcode;

/// <summary>
/// Holds localized barcode candidates and pipeline diagnostics.
/// </summary>
public class BarcodeDetectionResult
{
    /// <summary>
    /// The localized candidates in stable reading order.
    /// </summary>
    public BarcodeCandidate[]? Candidates;

    /// <summary>
    /// The bounded diagnostics collected during detection.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;
}
