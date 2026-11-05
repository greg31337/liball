namespace ftp;

internal class FtpAuthTlsState
{
    internal bool AuthorizationPending;
    internal bool AuthorizationAccepted;
    internal bool TlsRejected;
    internal bool HandshakePending;
    internal bool ControlProtected;
    internal bool PlaintextFallback;
    internal bool FeatureRefreshRequired;
    internal bool FeatureRefreshPending;
    internal bool FeatureRefreshComplete;
    internal bool FeatureRefreshSucceeded;
    internal bool ApplicationCommandsAllowed;
    internal bool Failed;
    internal bool ConnectionMustClose;
    internal string? Mechanism;
    internal FtpCommand? AuthorizationCommand;
    internal FtpReply? AuthorizationReply;
    internal FtpCommand? FeatureCommand;
    internal FtpReply? FeatureReply;

    internal FtpAuthTlsState()
    {
        AuthorizationPending = false;
        AuthorizationAccepted = false;
        TlsRejected = false;
        HandshakePending = false;
        ControlProtected = false;
        PlaintextFallback = false;
        FeatureRefreshRequired = false;
        FeatureRefreshPending = false;
        FeatureRefreshComplete = false;
        FeatureRefreshSucceeded = false;
        ApplicationCommandsAllowed = false;
        Failed = false;
        ConnectionMustClose = false;
        Mechanism = null;
        AuthorizationCommand = null;
        AuthorizationReply = null;
        FeatureCommand = null;
        FeatureReply = null;
    }
}
