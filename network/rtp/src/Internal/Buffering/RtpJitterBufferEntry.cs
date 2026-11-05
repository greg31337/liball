namespace rtp;

internal class RtpJitterBufferEntry
{
    internal RtpPacket Packet = new RtpPacket();
    internal RtpPacketContext Context = new RtpPacketContext();
    internal RtpSequenceUpdate SequenceUpdate = new RtpSequenceUpdate();
}
