namespace rtp;

/// <summary>
/// Contains the fields shared by every RTCP packet header.
/// </summary>
public class RtcpHeader
{
    /// <summary>The RTCP version.</summary>
    public byte Version = RtcpProtocolConstants.Version;

    /// <summary>Whether the RTCP padding flag is set.</summary>
    public bool HasPadding;

    /// <summary>The five-bit report count, source count, subtype, or feedback format.</summary>
    public byte Count;

    /// <summary>The eight-bit RTCP packet type.</summary>
    public byte PacketType;

    /// <summary>The packet length in 32-bit words minus one.</summary>
    public ushort LengthInWordsMinusOne;

    internal RtcpHeader Copy()
    {
        RtcpHeader copy = new RtcpHeader();
        copy.Version = Version;
        copy.HasPadding = HasPadding;
        copy.Count = Count;
        copy.PacketType = PacketType;
        copy.LengthInWordsMinusOne = LengthInWordsMinusOne;
        return copy;
    }
}
