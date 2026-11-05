namespace barcode;

/// <summary>
/// Reports a failure to decode a barcode symbol or image.
/// </summary>
public class BarcodeDecodingException : global::System.Exception
{
    /// <summary>
    /// Creates a decoding exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the decoding failure.</param>
    public BarcodeDecodingException(string message) : base(message)
    {
    }
}
