namespace barcode;

/// <summary>
/// Calculates and verifies barcode check values.
/// </summary>
public class BarcodeChecksumCalculator : IBarcodeChecksumCalculator
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a checksum calculator backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeChecksumCalculator()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Calculates a barcode check value.
    /// </summary>
    /// <param name="request">The format, algorithm, and source data.</param>
    /// <returns>The calculated checksum result.</returns>
    /// <exception cref="BarcodeValidationException">Checksum calculation is unavailable or cannot be performed.</exception>
    public BarcodeChecksumResult CalculateBarcodeChecksum(BarcodeChecksumRequest request)
    {
        return Dispatcher.CalculateBarcodeChecksum(request);
    }

    /// <summary>
    /// Verifies an observed barcode check value.
    /// </summary>
    /// <param name="request">The format, algorithm, source data, and observed value.</param>
    /// <returns>The checksum verification result.</returns>
    /// <exception cref="BarcodeValidationException">Checksum verification is unavailable or cannot be performed.</exception>
    public BarcodeChecksumResult VerifyBarcodeChecksum(BarcodeChecksumRequest request)
    {
        return Dispatcher.VerifyBarcodeChecksum(request);
    }
}
