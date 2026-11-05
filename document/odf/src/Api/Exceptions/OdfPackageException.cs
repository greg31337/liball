namespace odf;

/// <summary>
/// Represents a failure while reading, validating, or writing an OpenDocument package.
/// </summary>
public class OdfPackageException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the package failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfPackageException(string message) : base(message)
    {
    }
}
