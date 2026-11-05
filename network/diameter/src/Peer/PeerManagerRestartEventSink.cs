namespace diameter;

internal class PeerManagerRestartEventSink : IPeerRestartNotificationSink
{
    internal DiameterPeerManager? Manager;

    internal PeerManagerRestartEventSink(DiameterPeerManager? manager)
    {
        Manager = manager;
    }

    public void HandlePeerRestart(DiameterPeerEvent peerEvent)
    {
        if (Manager != null)
        {
            Manager.DispatchPeerManagerEvent(peerEvent);
        }
    }
}
