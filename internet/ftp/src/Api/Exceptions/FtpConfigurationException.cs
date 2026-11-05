namespace ftp;

/// <summary>
/// Represents an invalid or incomplete FTP configuration.
/// </summary>
public class FtpConfigurationException : System.Exception
{
    /// <summary>
    /// Creates a configuration exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The configuration failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpConfigurationException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
