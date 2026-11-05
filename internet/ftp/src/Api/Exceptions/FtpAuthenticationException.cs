namespace ftp;

/// <summary>
/// Represents an FTP authentication or accounting failure.
/// </summary>
public class FtpAuthenticationException : System.Exception
{
    /// <summary>
    /// Creates an authentication exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The authentication failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpAuthenticationException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
