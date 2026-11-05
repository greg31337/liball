namespace rtp;

/// <summary>
/// Represents malformed or unsupported RTP packet data.
/// </summary>
public class RtpPacketFormatException : System.Exception
{
    /// <summary>
    /// Initializes an RTP packet format exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the packet format error.</param>
    public RtpPacketFormatException(string message) : base(message)
    {
    }
}
