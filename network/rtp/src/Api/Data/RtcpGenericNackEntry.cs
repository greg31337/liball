namespace rtp;

/// <summary>
/// Contains one RFC 4585 Generic NACK PID and bitmask pair.
/// </summary>
public class RtcpGenericNackEntry
{
    /// <summary>The first lost RTP sequence number represented by the entry.</summary>
    public ushort PacketIdentifier;

    /// <summary>The bitmask representing losses in the next sixteen sequence numbers.</summary>
    public ushort LostPacketBitmask;

    internal RtcpGenericNackEntry Copy()
    {
        RtcpGenericNackEntry copy = new RtcpGenericNackEntry();
        copy.PacketIdentifier = PacketIdentifier;
        copy.LostPacketBitmask = LostPacketBitmask;
        return copy;
    }
}
