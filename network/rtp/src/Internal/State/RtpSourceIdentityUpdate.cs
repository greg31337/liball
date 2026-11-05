namespace rtp;

internal class RtpSourceIdentityUpdate
{
    internal bool collisionDetected;
    internal bool loopDetected;
    internal uint previousSourceIdentifier;
    internal uint currentSourceIdentifier;
    internal RtcpGoodbye? goodbye;
    internal RtpSourceSnapshot? sourceSnapshot;
}
