namespace barcode;

/// <summary>
/// Calculates and verifies barcode check values.
/// </summary>
public interface IBarcodeChecksumCalculator
{
    /// <summary>
    /// Calculates a barcode check value.
    /// </summary>
    /// <param name="request">The format, algorithm, and source data.</param>
    /// <returns>The calculated checksum result.</returns>
    BarcodeChecksumResult CalculateBarcodeChecksum(BarcodeChecksumRequest request);

    /// <summary>
    /// Verifies an observed barcode check value.
    /// </summary>
    /// <param name="request">The format, algorithm, source data, and observed value.</param>
    /// <returns>The checksum verification result.</returns>
    BarcodeChecksumResult VerifyBarcodeChecksum(BarcodeChecksumRequest request);
}
