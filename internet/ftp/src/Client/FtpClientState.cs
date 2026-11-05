namespace ftp;

internal class FtpClientState
{
    internal FtpSessionStateMachine Session;
    internal IFtpCertificateValidator? CertificateValidator;
    internal FtpClientOptions? Options;
    internal FtpFeatureSet Features;
    internal FtpClientFeatureState FeatureState;
    internal FtpControlConnection? Connection;
    internal FtpClientAuthTlsCoordinator? TlsCoordinator;
    internal FtpAuthenticationResult? LastAuthentication;
    internal FtpCredentials? ActiveCredentials;
    internal FtpClientDataConnectionStrategy? DataStrategy;
    internal FtpClientReconnectState ReconnectState;
    internal string? LatestRestartMarker;
    internal string? LatestLocalRestartMarker;
    internal string LastOperation;
    internal int DispatchCount;
    internal bool ConnectRequested;
    internal bool ReconnectInProgress;

    internal FtpClientState(IFtpCertificateValidator? certificateValidator)
    {
        Session = new FtpSessionStateMachine();
        CertificateValidator = certificateValidator;
        Options = null;
        Features = new FtpFeatureSet();
        FeatureState = new FtpClientFeatureState();
        Connection = null;
        TlsCoordinator = null;
        LastAuthentication = null;
        ActiveCredentials = null;
        DataStrategy = null;
        ReconnectState = new FtpClientReconnectState();
        LatestRestartMarker = null;
        LatestLocalRestartMarker = null;
        LastOperation = "";
        DispatchCount = 0;
        ConnectRequested = false;
        ReconnectInProgress = false;
    }

    internal void RecordOperation(string operation)
    {
        LastOperation = operation;
        DispatchCount++;
    }

    internal void RequireClosedSession()
    {
        FtpSessionState sessionState = Session.State.SessionState;
        if (sessionState != FtpSessionState.Disconnected &&
            sessionState != FtpSessionState.Closed)
        {
            throw new FtpProtocolException(
                "A new FTP connection cannot replace an open session.", null);
        }
    }

    internal void RequireOpenSession(string operation)
    {
        FtpSessionState sessionState = Session.State.SessionState;
        if (sessionState == FtpSessionState.Disconnected ||
            sessionState == FtpSessionState.Closed)
        {
            throw new FtpProtocolException(
                $"The FTP client operation '{operation}' requires an open session.", null);
        }
    }

    internal FtpProtocolException CreateUnavailableOperation(string operation)
    {
        RequireOpenSession(operation);
        return new FtpProtocolException(
            $"The FTP client operation '{operation}' is not available yet.", null);
    }

    internal void CloseSession()
    {
        UpdateReconnectState();
        FtpFaultIsolationResult cleanup = new FtpFaultIsolationResult();
        cleanup.Succeeded = true;
        FtpFaultIsolationResult dataCleanup = new FtpFaultIsolationResult();
        dataCleanup.Succeeded = true;
        if (DataStrategy != null)
        {
            FtpClientDataConnectionStrategy dataStrategy = DataStrategy;
            DataStrategy = null;
            dataCleanup = FtpFaultIsolationRunner.RunOperation(() =>
            {
                dataStrategy.AbortDataConnection();
                return null;
            });
        }
        if (Connection != null)
        {
            cleanup = Connection.Close();
            Connection = null;
        }
        TlsCoordinator = null;
        LastAuthentication = null;
        ActiveCredentials = null;

        FtpSessionState sessionState = Session.State.SessionState;
        if (sessionState != FtpSessionState.Disconnected &&
            sessionState != FtpSessionState.Closed)
        {
            Session.CloseConnection();
        }

        if (!dataCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The FTP data connection could not be closed cleanly.",
                dataCleanup.Failure);
        }
        if (!cleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The FTP control connection could not be closed cleanly.",
                cleanup.Failure);
        }
    }

    internal void ResetReconnectState()
    {
        ReconnectState = new FtpClientReconnectState();
        ActiveCredentials = null;
        ReconnectInProgress = false;
    }

    internal void UpdateReconnectState()
    {
        if (ReconnectInProgress)
        {
            return;
        }
        FtpSessionState sessionState = Session.State.SessionState;
        if (sessionState == FtpSessionState.Disconnected ||
            sessionState == FtpSessionState.Closed)
        {
            return;
        }
        ReconnectState = FtpClientReconnectState.Capture(this);
    }
}
