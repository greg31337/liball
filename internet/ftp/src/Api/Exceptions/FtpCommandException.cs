namespace ftp;

/// <summary>
/// Represents an FTP command that did not reach an accepted completion.
/// </summary>
public class FtpCommandException : System.Exception
{
    /// <summary>
    /// Creates a command exception with diagnostic text and an optional underlying failure.
    /// </summary>
    /// <param name="message">The command failure description.</param>
    /// <param name="innerException">The optional underlying failure.</param>
    public FtpCommandException(string message, System.Exception? innerException)
        : base(message, innerException)
    {
    }
}
