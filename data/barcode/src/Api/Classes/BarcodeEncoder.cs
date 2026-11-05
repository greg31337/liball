namespace barcode;

/// <summary>
/// Encodes payloads into logical barcode symbols.
/// </summary>
public class BarcodeEncoder : IBarcodeEncoder
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a barcode encoder backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeEncoder()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Encodes a barcode or reports failure with an exception.
    /// </summary>
    /// <param name="request">The payload, format, and options to encode.</param>
    /// <returns>The encoded logical symbol.</returns>
    /// <exception cref="BarcodeEncodingException">Encoding is unavailable or the request cannot be encoded.</exception>
    public BarcodeSymbol EncodeBarcode(BarcodeEncodeRequest request)
    {
        return Dispatcher.EncodeBarcode(request);
    }

    /// <summary>
    /// Attempts to encode a barcode without using an exception for an expected data failure.
    /// </summary>
    /// <param name="request">The payload, format, and options to encode.</param>
    /// <returns>The encoded logical symbol, or null when the request cannot be encoded.</returns>
    public BarcodeSymbol? TryEncodeBarcode(BarcodeEncodeRequest request)
    {
        return Dispatcher.TryEncodeBarcode(request);
    }
}
