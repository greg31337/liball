namespace rtp;

/// <summary>
/// Represents malformed or unsupported RTCP packet data.
/// </summary>
public class RtcpPacketFormatException : System.Exception
{
    /// <summary>
    /// Initializes an RTCP packet format exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the packet format error.</param>
    public RtcpPacketFormatException(string message) : base(message)
    {
    }
}
