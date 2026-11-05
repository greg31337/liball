using System;

namespace ftp;

internal class FtpClientAuthTlsCoordinator
{
    internal FtpAuthTlsState State;
    internal FtpSecurityMode SecurityMode;
    internal bool AllowPlaintextFallback;
    internal FtpSessionStateMachine Session;

    internal FtpClientAuthTlsCoordinator(
        FtpSecurityMode securityMode, bool allowPlaintextFallback,
        FtpSessionStateMachine? session)
    {
        if (session == null)
        {
            throw new ArgumentNullException("session");
        }
        if (securityMode != FtpSecurityMode.ExplicitTlsRequired &&
            securityMode != FtpSecurityMode.ExplicitTlsOpportunistic)
        {
            throw new FtpConfigurationException(
                "AUTH TLS negotiation requires an explicit TLS security mode.", null);
        }
        if (securityMode == FtpSecurityMode.ExplicitTlsOpportunistic &&
            !allowPlaintextFallback)
        {
            throw new FtpConfigurationException(
                "Opportunistic TLS requires explicit plaintext fallback opt-in.", null);
        }

        State = new FtpAuthTlsState();
        SecurityMode = securityMode;
        AllowPlaintextFallback = allowPlaintextFallback;
        Session = session;
    }

    internal FtpCommand BeginAuthorization(string? mechanism)
    {
        if (State.AuthorizationPending || State.HandshakePending ||
            State.ControlProtected || State.PlaintextFallback || State.Failed)
        {
            throw new FtpProtocolException(
                "AUTH TLS cannot begin in the current negotiation state.", null);
        }

        string normalizedMechanism = NormalizeMechanism(mechanism);
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Auth;
        command.Verb = FtpCommandNames.Auth;
        command.Argument = normalizedMechanism;
        command.RawText = FtpCommandNames.Auth + " " + normalizedMechanism;
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);

        State.AuthorizationPending = true;
        State.AuthorizationAccepted = false;
        State.TlsRejected = false;
        State.Mechanism = normalizedMechanism;
        State.AuthorizationCommand = command;
        State.AuthorizationReply = null;
        State.ApplicationCommandsAllowed = false;
        return command;
    }

    internal void ApplyAuthorizationReply(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (!State.AuthorizationPending || State.AuthorizationCommand == null)
        {
            throw new FtpProtocolException(
                "No client AUTH TLS command is awaiting a reply.", null);
        }

        State.AuthorizationPending = false;
        State.AuthorizationReply = reply;
        if (reply.Code == FtpReplyCodes.SecurityExchangeComplete)
        {
            Session.ApplyCommandReply(State.AuthorizationCommand, reply);
            State.AuthorizationAccepted = true;
            State.HandshakePending = true;
            return;
        }

        if (reply.Code >= 400 && reply.Code <= 599)
        {
            State.TlsRejected = true;
            if (SecurityMode == FtpSecurityMode.ExplicitTlsOpportunistic &&
                AllowPlaintextFallback && reply.Code != FtpReplyCodes.ServiceUnavailable)
            {
                State.PlaintextFallback = true;
                State.ApplicationCommandsAllowed = true;
                return;
            }

            TerminateConnection();
            return;
        }

        TerminateConnection();
        throw new FtpProtocolException(
            "AUTH TLS must receive reply 234 or a negative completion reply.", null);
    }

    internal void CompleteHandshake()
    {
        if (!State.HandshakePending || !State.AuthorizationAccepted)
        {
            throw new FtpProtocolException(
                "No accepted client AUTH TLS exchange is awaiting a handshake.", null);
        }

        Session.CompleteSecurityExchange(true);
        State.HandshakePending = false;
        State.ControlProtected = true;
        State.FeatureRefreshRequired = true;
        State.FeatureRefreshPending = false;
        State.FeatureRefreshComplete = false;
        State.FeatureRefreshSucceeded = false;
        State.FeatureCommand = null;
        State.FeatureReply = null;
        State.ApplicationCommandsAllowed = false;
    }

    internal void FailAuthorizationExchange()
    {
        if (!State.AuthorizationPending)
        {
            throw new FtpProtocolException(
                "No client AUTH TLS reply exchange is in progress.", null);
        }

        TerminateConnection();
    }

    internal void FailHandshake()
    {
        if (!State.HandshakePending)
        {
            throw new FtpProtocolException(
                "No client AUTH TLS handshake is in progress.", null);
        }

        TerminateConnection();
    }

    internal FtpCommand BeginFeatureRefresh()
    {
        if (!State.ControlProtected || !State.FeatureRefreshRequired ||
            State.FeatureRefreshPending || State.FeatureRefreshComplete || State.Failed)
        {
            throw new FtpProtocolException(
                "FEAT refresh cannot begin in the current TLS negotiation state.", null);
        }

        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Feat;
        command.Verb = FtpCommandNames.Feat;
        command.RawText = FtpCommandNames.Feat;
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        State.FeatureRefreshPending = true;
        State.FeatureCommand = command;
        return command;
    }

    internal void ApplyFeatureRefreshReply(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (!State.FeatureRefreshPending)
        {
            throw new FtpProtocolException(
                "No post-upgrade FEAT command is awaiting a reply.", null);
        }

        State.FeatureReply = reply;
        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            TerminateConnection();
            return;
        }
        if (reply.Code != FtpReplyCodes.SystemStatus &&
            (reply.Code < 400 || reply.Code > 599))
        {
            TerminateConnection();
            throw new FtpProtocolException(
                "A post-upgrade FEAT exchange requires reply 211 or a negative completion reply.", null);
        }

        State.FeatureRefreshPending = false;
        State.FeatureRefreshRequired = false;
        State.FeatureRefreshComplete = true;
        State.FeatureRefreshSucceeded = reply.Code == FtpReplyCodes.SystemStatus;
        State.ApplicationCommandsAllowed = true;
    }

    internal void FailFeatureRefresh()
    {
        if (!State.FeatureRefreshPending)
        {
            throw new FtpProtocolException(
                "No post-upgrade FEAT exchange is in progress.", null);
        }

        TerminateConnection();
    }

    internal string NormalizeMechanism(string? mechanism)
    {
        if (string.Equals(mechanism, "TLS", StringComparison.OrdinalIgnoreCase))
        {
            return "TLS";
        }
        if (string.Equals(mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase))
        {
            return "TLS-C";
        }
        throw new FtpProtocolException(
            "The built-in FTP TLS coordinator accepts only AUTH TLS or AUTH TLS-C.", null);
    }

    internal void TerminateConnection()
    {
        State.AuthorizationPending = false;
        State.HandshakePending = false;
        State.ControlProtected = false;
        State.FeatureRefreshRequired = false;
        State.FeatureRefreshPending = false;
        State.ApplicationCommandsAllowed = false;
        State.Failed = true;
        State.ConnectionMustClose = true;
        Session.CloseConnection();
    }
}
