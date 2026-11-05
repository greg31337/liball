namespace barcode;

/// <summary>
/// Encodes payloads into logical barcode symbols.
/// </summary>
public interface IBarcodeEncoder
{
    /// <summary>
    /// Encodes a barcode or reports failure with an exception.
    /// </summary>
    /// <param name="request">The payload, format, and options to encode.</param>
    /// <returns>The encoded logical symbol.</returns>
    BarcodeSymbol EncodeBarcode(BarcodeEncodeRequest request);

    /// <summary>
    /// Attempts to encode a barcode without using an exception for an expected data failure.
    /// </summary>
    /// <param name="request">The payload, format, and options to encode.</param>
    /// <returns>The encoded logical symbol, or null when the request cannot be encoded.</returns>
    BarcodeSymbol? TryEncodeBarcode(BarcodeEncodeRequest request);
}
