namespace ftp;

/// <summary>
/// Represents an FTP-over-TLS negotiation, validation, or shutdown failure.
/// </summary>
public class FtpTlsException : System.Exception
{
    /// <summary>
    /// Creates a TLS exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The TLS failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpTlsException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
