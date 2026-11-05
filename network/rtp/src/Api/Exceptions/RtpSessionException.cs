namespace rtp;

/// <summary>
/// Represents an invalid or unrecoverable RTP session operation.
/// </summary>
public class RtpSessionException : System.Exception
{
    /// <summary>
    /// Initializes an RTP session exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the session error.</param>
    public RtpSessionException(string message) : base(message)
    {
    }
}
