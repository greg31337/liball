namespace barcode;

/// <summary>
/// Decodes logical symbols and raw images into barcode results.
/// </summary>
public class BarcodeDecoder : IBarcodeDecoder
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a barcode decoder backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeDecoder()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Decodes the requested input or reports failure with an exception.
    /// </summary>
    /// <param name="request">The image or logical symbol and decoding options.</param>
    /// <returns>The decoded results in stable reading order.</returns>
    /// <exception cref="BarcodeDecodingException">Decoding is unavailable or the input cannot be decoded.</exception>
    public BarcodeDecodeResult[] DecodeBarcode(BarcodeDecodeRequest request)
    {
        return Dispatcher.DecodeBarcode(request);
    }

    /// <summary>
    /// Attempts to decode the requested input without using an exception for an expected decode failure.
    /// </summary>
    /// <param name="request">The image or logical symbol and decoding options.</param>
    /// <returns>The exception-less decode outcome.</returns>
    public BarcodeDecodeAttempt TryDecodeBarcode(BarcodeDecodeRequest request)
    {
        return Dispatcher.TryDecodeBarcode(request);
    }
}
