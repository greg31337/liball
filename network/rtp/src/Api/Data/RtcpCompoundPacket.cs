namespace rtp;

/// <summary>
/// Contains an ordered RTCP compound packet.
/// </summary>
public class RtcpCompoundPacket
{
    /// <summary>The RTCP packets in wire order.</summary>
    public RtcpPacket[] Packets = [];

    internal RtcpCompoundPacket Copy()
    {
        RtcpCompoundPacket copy = new RtcpCompoundPacket();
        copy.Packets = new RtcpPacket[Packets.Length];
        int index = 0;
        while (index < Packets.Length)
        {
            copy.Packets[index] = Packets[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
