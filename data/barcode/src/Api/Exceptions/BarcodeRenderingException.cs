namespace barcode;

/// <summary>
/// Reports a failure to render a logical barcode symbol.
/// </summary>
public class BarcodeRenderingException : global::System.Exception
{
    /// <summary>
    /// Creates a rendering exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the rendering failure.</param>
    public BarcodeRenderingException(string message) : base(message)
    {
    }
}
