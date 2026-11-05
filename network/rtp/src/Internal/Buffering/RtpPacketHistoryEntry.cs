namespace rtp;

internal class RtpPacketHistoryEntry
{
    internal RtpPacket Packet = new RtpPacket();
    internal RtpPacketContext Context = new RtpPacketContext();
    internal int EncodedSize;
}
