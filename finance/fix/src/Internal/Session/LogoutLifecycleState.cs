using System;

namespace fix;

internal class LogoutLifecycleState
{
    internal bool InProgress;
    internal bool LocalLogoutInitiated;
    internal bool RemoteLogoutReceived;
    internal bool ResponsePending;
    internal bool HasDeadline;
    internal DateTime DeadlineUtc;
    internal bool LogoutNotified;
    internal string LocalText = "";
    internal string PeerText = "";
    internal FixMessage? PeerMessage;
    internal int PeerSequenceNumber;
    internal FixMessage? PendingResponse;
}
