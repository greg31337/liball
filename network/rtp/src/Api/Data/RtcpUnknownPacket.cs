namespace rtp;

/// <summary>
/// Contains the opaque body of an unrecognized RTCP packet.
/// </summary>
public class RtcpUnknownPacket
{
    /// <summary>The unrecognized eight-bit packet type.</summary>
    public byte PacketType;

    /// <summary>The original five-bit count or subtype field.</summary>
    public byte Count;

    /// <summary>The packet body bytes excluding the common header and RTCP padding.</summary>
    public byte[] Data = [];

    internal RtcpUnknownPacket Copy()
    {
        RtcpUnknownPacket copy = new RtcpUnknownPacket();
        copy.PacketType = PacketType;
        copy.Count = Count;
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
