namespace tftp;

/// <summary>
/// Represents malformed local or peer TFTP protocol data.
/// </summary>
public class TftpProtocolException : System.Exception
{
    /// <summary>
    /// Indicates whether <see cref="PacketOffset"/> identifies the failure position.
    /// </summary>
    public bool HasPacketOffset;

    /// <summary>
    /// The zero-based packet offset when <see cref="HasPacketOffset"/> is true.
    /// </summary>
    public int PacketOffset;

    /// <summary>
    /// Initializes a protocol exception with validation details.
    /// </summary>
    /// <param name="message">The protocol validation failure message.</param>
    /// <param name="hasPacketOffset">Whether <c>packetOffset</c> is available.</param>
    /// <param name="packetOffset">The zero-based packet offset when available.</param>
    public TftpProtocolException(string message, bool hasPacketOffset, int packetOffset)
        : base(message)
    {
        HasPacketOffset = hasPacketOffset;
        PacketOffset = packetOffset;
    }
}
