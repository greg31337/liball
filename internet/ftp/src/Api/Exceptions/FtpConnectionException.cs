namespace ftp;

/// <summary>
/// Represents an FTP control or data connection failure.
/// </summary>
public class FtpConnectionException : System.Exception
{
    /// <summary>
    /// Creates a connection exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The connection failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpConnectionException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
