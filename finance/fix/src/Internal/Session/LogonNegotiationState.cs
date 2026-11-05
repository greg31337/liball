namespace fix;

internal class LogonNegotiationState
{
    internal bool IsLoggedOn;
    internal bool IsAuthenticated;
    internal int HeartbeatIntervalSeconds;
    internal string DefaultApplicationVersion = "";
    internal FixDictionaryIdentity ApplicationIdentity = new FixDictionaryIdentity();
    internal FixDictionary? ApplicationDictionary;
    internal bool ResetSequenceNumbers;
    internal int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    internal int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    internal bool HasPeerNextExpectedMsgSeqNum;
    internal int PeerNextExpectedMsgSeqNum;
}
