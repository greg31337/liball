namespace barcode;

/// <summary>
/// Holds the exception-less outcome of a barcode decode attempt.
/// </summary>
public class BarcodeDecodeAttempt
{
    /// <summary>
    /// Indicates whether at least one requested barcode was decoded successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The decoded results in stable reading order.
    /// </summary>
    public BarcodeDecodeResult[]? Results;

    /// <summary>
    /// The bounded diagnostics collected across the attempt.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;

    /// <summary>
    /// The stable machine-readable failure code when the attempt did not succeed.
    /// </summary>
    public string? FailureCode;

    /// <summary>
    /// The human-readable failure message when the attempt did not succeed.
    /// </summary>
    public string? FailureMessage;
}
