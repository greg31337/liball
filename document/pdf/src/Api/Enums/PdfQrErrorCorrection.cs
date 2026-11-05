namespace pdf;

/// <summary>
/// Specifies the QR Code error-correction capacity.
/// </summary>
public enum PdfQrErrorCorrection
{
    /// <summary>
    /// Uses low error correction with approximately seven percent recovery.
    /// </summary>
    Low = 0,
    /// <summary>
    /// Uses medium error correction with approximately fifteen percent recovery.
    /// </summary>
    Medium = 1,
    /// <summary>
    /// Uses quartile error correction with approximately twenty-five percent recovery.
    /// </summary>
    Quartile = 2,
    /// <summary>
    /// Uses high error correction with approximately thirty percent recovery.
    /// </summary>
    High = 3
}
