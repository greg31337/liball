namespace sip;

internal class SipTransactionLayerEntry
{
    internal string? Id;
    internal string? CorrelationId;
    internal int MaximumEventCount = SipDefaults.MaxQueueCount;
    internal bool AllowLegacyMatching;
    internal SipInviteClientTransaction? InviteClient;
    internal SipInviteServerTransaction? InviteServer;
    internal SipNonInviteClientTransaction? NonInviteClient;
    internal SipNonInviteServerTransaction? NonInviteServer;

    internal SipTransactionSnapshot GetSnapshot()
    {
        SipTransactionSnapshot snapshot;
        if (InviteClient != null)
        {
            snapshot = InviteClient.GetSnapshot();
        }
        else if (InviteServer != null)
        {
            snapshot = InviteServer.GetSnapshot();
        }
        else if (NonInviteClient != null)
        {
            snapshot = NonInviteClient.GetSnapshot();
        }
        else if (NonInviteServer != null)
        {
            snapshot = NonInviteServer.GetSnapshot();
        }
        else
        {
            throw new System.InvalidOperationException(
                "The transaction layer entry has no state machine.");
        }
        snapshot.CorrelationId = CorrelationId;
        return snapshot;
    }
}
