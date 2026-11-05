namespace barcode;

/// <summary>
/// Reports that a requested barcode format is unsupported by an operation.
/// </summary>
public class UnsupportedBarcodeFormatException : global::System.Exception
{
    /// <summary>
    /// Creates an unsupported-format exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the unsupported format.</param>
    public UnsupportedBarcodeFormatException(string message) : base(message)
    {
    }
}
