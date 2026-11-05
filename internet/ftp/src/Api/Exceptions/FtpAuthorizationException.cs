namespace ftp;

/// <summary>
/// Represents an FTP operation denied by authorization policy.
/// </summary>
public class FtpAuthorizationException : System.Exception
{
    /// <summary>
    /// Creates an authorization exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The authorization failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpAuthorizationException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
