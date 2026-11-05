namespace ftp;

/// <summary>
/// Represents an FTP data-transfer or verification failure.
/// </summary>
public class FtpTransferException : System.Exception
{
    /// <summary>
    /// Creates a transfer exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The transfer failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpTransferException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
