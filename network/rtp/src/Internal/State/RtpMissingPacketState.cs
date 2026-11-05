namespace rtp;

internal class RtpMissingPacketState
{
    internal uint ExtendedSequenceNumber;
    internal long MissingSinceTicks;
    internal int AttemptCount;
    internal long LastRequestTimeTicks;
}
