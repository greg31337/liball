namespace barcode;

/// <summary>
/// Decodes logical symbols and raw images into barcode results.
/// </summary>
public interface IBarcodeDecoder
{
    /// <summary>
    /// Decodes the requested input or reports failure with an exception.
    /// </summary>
    /// <param name="request">The image or logical symbol and decoding options.</param>
    /// <returns>The decoded results in stable reading order.</returns>
    BarcodeDecodeResult[] DecodeBarcode(BarcodeDecodeRequest request);

    /// <summary>
    /// Attempts to decode the requested input without using an exception for an expected decode failure.
    /// </summary>
    /// <param name="request">The image or logical symbol and decoding options.</param>
    /// <returns>The exception-less decode outcome.</returns>
    BarcodeDecodeAttempt TryDecodeBarcode(BarcodeDecodeRequest request);
}
