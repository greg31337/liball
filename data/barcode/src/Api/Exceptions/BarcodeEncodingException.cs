namespace barcode;

/// <summary>
/// Reports a failure to encode barcode data into a logical symbol.
/// </summary>
public class BarcodeEncodingException : global::System.Exception
{
    /// <summary>
    /// Creates an encoding exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the encoding failure.</param>
    public BarcodeEncodingException(string message) : base(message)
    {
    }
}
