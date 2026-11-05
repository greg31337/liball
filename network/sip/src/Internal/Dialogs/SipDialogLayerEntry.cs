namespace sip;

internal class SipDialogLayerEntry
{
    internal SipDialogSnapshot Snapshot = new SipDialogSnapshot();
    internal int InitialInviteCSeq = -1;
    internal string? InviteUsageId;
    internal int MaximumUsageCount = 32;
    internal int MaximumTransactionCount = 64;
    internal int MaximumEventCount = SipDefaults.MaxQueueCount;
    internal int LocalByeCSeq = -1;
    internal SipUri? PendingLocalTarget;
    internal int LocalTargetRefreshCSeq = -1;
    internal SipMethod LocalTargetRefreshMethod;
    internal SipUri? PendingRemoteTarget;
    internal int RemoteTargetRefreshCSeq = -1;
    internal SipMethod RemoteTargetRefreshMethod;
    internal bool StrictRemoteCSeq;
    internal bool PreventSecureDowngrade;
}
