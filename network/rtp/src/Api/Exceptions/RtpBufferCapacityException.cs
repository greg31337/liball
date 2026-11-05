namespace rtp;

/// <summary>
/// Represents a bounded RTP buffer or output-capacity failure.
/// </summary>
public class RtpBufferCapacityException : System.Exception
{
    /// <summary>
    /// Initializes an RTP buffer capacity exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the capacity failure.</param>
    public RtpBufferCapacityException(string message) : base(message)
    {
    }
}
