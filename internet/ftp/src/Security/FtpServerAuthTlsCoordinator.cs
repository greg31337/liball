using System;

namespace ftp;

internal class FtpServerAuthTlsCoordinator
{
    internal FtpAuthTlsState State;
    internal bool TlsAvailable;
    internal FtpSessionStateMachine Session;

    internal FtpServerAuthTlsCoordinator(
        bool tlsAvailable, FtpSessionStateMachine? session)
    {
        if (session == null)
        {
            throw new ArgumentNullException("session");
        }

        State = new FtpAuthTlsState();
        TlsAvailable = tlsAvailable;
        Session = session;
    }

    internal FtpReply HandleAuthorizationCommand(FtpCommand? command)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (State.ConnectionMustClose)
        {
            return CreateReply(
                FtpReplyCodes.ServiceUnavailable,
                "The FTP control connection must close after TLS failure.");
        }

        FtpSequenceValidationResult validation =
            FtpSequenceValidator.InspectCommand(command, Session.State);
        if (!validation.Allowed)
        {
            return CreateReply(validation.ReplyCode, validation.Message);
        }
        if (command.Kind != FtpCommandKind.Auth)
        {
            return CreateReply(
                FtpReplyCodes.SyntaxError, "An AUTH command is required.");
        }
        if (!IsTlsMechanism(command.Argument))
        {
            State.TlsRejected = true;
            return CreateReply(
                FtpReplyCodes.CommandParameterNotImplemented,
                "Only the TLS and TLS-C security mechanisms are supported.");
        }
        if (!TlsAvailable)
        {
            State.TlsRejected = true;
            return CreateReply(
                FtpReplyCodes.RequestDeniedByPolicy,
                "TLS is not available for this FTP listener.");
        }

        FtpReply reply = CreateReply(
            FtpReplyCodes.SecurityExchangeComplete,
            "AUTH TLS accepted; proceed with TLS negotiation.");
        Session.ApplyCommandReply(command, reply);
        State.AuthorizationAccepted = true;
        State.TlsRejected = false;
        State.HandshakePending = true;
        State.ControlProtected = false;
        State.ApplicationCommandsAllowed = false;
        State.Mechanism = NormalizeMechanism(command.Argument);
        State.AuthorizationCommand = command;
        State.AuthorizationReply = reply;
        return reply;
    }

    internal void CompleteHandshake()
    {
        if (!State.HandshakePending || !State.AuthorizationAccepted)
        {
            throw new FtpProtocolException(
                "No accepted server AUTH TLS exchange is awaiting a handshake.", null);
        }

        Session.CompleteSecurityExchange(true);
        State.HandshakePending = false;
        State.ControlProtected = true;
        State.ApplicationCommandsAllowed = true;
    }

    internal void FailHandshake()
    {
        if (!State.HandshakePending)
        {
            throw new FtpProtocolException(
                "No server AUTH TLS handshake is in progress.", null);
        }

        State.HandshakePending = false;
        State.ControlProtected = false;
        State.ApplicationCommandsAllowed = false;
        State.Failed = true;
        State.ConnectionMustClose = true;
        Session.CloseConnection();
    }

    internal bool IsTlsMechanism(string? mechanism)
    {
        return string.Equals(mechanism, "TLS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase);
    }

    internal string NormalizeMechanism(string? mechanism)
    {
        return string.Equals(mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase) ?
            "TLS-C" : "TLS";
    }

    internal FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
