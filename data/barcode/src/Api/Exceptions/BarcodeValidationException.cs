namespace barcode;

/// <summary>
/// Reports a failure to perform barcode validation.
/// </summary>
public class BarcodeValidationException : global::System.Exception
{
    /// <summary>
    /// Creates a validation exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the validation failure.</param>
    public BarcodeValidationException(string message) : base(message)
    {
    }
}
