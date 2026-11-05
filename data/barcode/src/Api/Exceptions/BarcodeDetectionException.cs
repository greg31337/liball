namespace barcode;

/// <summary>
/// Reports a failure to locate barcode candidates in an image.
/// </summary>
public class BarcodeDetectionException : global::System.Exception
{
    /// <summary>
    /// Creates a detection exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the detection failure.</param>
    public BarcodeDetectionException(string message) : base(message)
    {
    }
}
