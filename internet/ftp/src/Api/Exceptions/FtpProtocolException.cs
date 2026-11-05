namespace ftp;

/// <summary>
/// Represents malformed or invalid FTP protocol data or sequencing.
/// </summary>
public class FtpProtocolException : System.Exception
{
    /// <summary>
    /// Creates a protocol exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The protocol failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpProtocolException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
