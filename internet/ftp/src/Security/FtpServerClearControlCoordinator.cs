using System;

namespace ftp;

internal class FtpServerClearControlCoordinator
{
    internal FtpClearControlState State;
    internal FtpTlsOptions Options;
    internal FtpSessionStateMachine Session;
    internal FtpTlsServerStream TlsStream;
    internal IFtpEventSink? EventSink;

    internal FtpServerClearControlCoordinator(
        FtpTlsOptions? options, FtpSessionStateMachine? session,
        FtpTlsServerStream? tlsStream, IFtpEventSink? eventSink)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        if (session == null)
        {
            throw new ArgumentNullException("session");
        }
        if (tlsStream == null)
        {
            throw new ArgumentNullException("tlsStream");
        }
        if (options.AllowClearCommandChannel && !tlsStream.Request.LeaveTransportOpen)
        {
            throw new FtpConfigurationException(
                "CCC requires the server TLS stream to leave the control transport open.", null);
        }

        State = new FtpClearControlState();
        Options = options;
        Session = session;
        TlsStream = tlsStream;
        EventSink = eventSink;
    }

    internal FtpReply HandleClearControlCommand(FtpCommand? command)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (State.ConnectionMustClose)
        {
            return CreateReply(
                FtpReplyCodes.ServiceUnavailable,
                "The FTP control connection must close after CCC failure.");
        }

        FtpSequenceValidationResult validation =
            FtpSequenceValidator.InspectCommand(command, Session.State);
        if (!validation.Allowed)
        {
            return CreateReply(validation.ReplyCode, validation.Message);
        }
        if (command.Kind != FtpCommandKind.Ccc)
        {
            return CreateReply(FtpReplyCodes.SyntaxError, "A CCC command is required.");
        }
        if (!Options.AllowClearCommandChannel)
        {
            FtpReply rejectedReply = CreateReply(
                FtpReplyCodes.RequestDeniedByPolicy,
                "CCC is disabled by server policy.");
            State.Rejected = true;
            State.Command = command;
            State.Reply = rejectedReply;
            RecordPolicyRejection(command, rejectedReply);
            return rejectedReply;
        }
        if (State.ReplyWritePending || State.TlsShutdownPending ||
            State.ClearControlRestored || State.Failed)
        {
            return CreateReply(
                FtpReplyCodes.BadCommandSequence,
                "CCC cannot begin in the current server downgrade state.");
        }

        FtpReply reply = CreateReply(
            FtpReplyCodes.CommandOkay,
            "CCC accepted; the control channel will continue without TLS.");
        State.Command = command;
        State.Reply = reply;
        State.ReplyWritePending = true;
        State.Rejected = false;
        return reply;
    }

    internal void CompleteClearControlAfterReplyWrite()
    {
        if (!State.ReplyWritePending || State.Command == null || State.Reply == null)
        {
            throw new FtpProtocolException(
                "No accepted server CCC reply is awaiting protected transmission.", null);
        }

        State.ReplyWritePending = false;
        State.TlsShutdownPending = true;
        FtpFaultIsolationResult shutdown = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TlsStream.ShutdownServerTlsForClearControl();
            return null;
        });
        if (!shutdown.Succeeded)
        {
            TerminateConnection();
            throw new FtpTlsException(
                "The server control connection could not complete the CCC TLS shutdown.",
                shutdown.Failure);
        }

        Session.CompleteClearCommandChannel(State.Command, State.Reply);
        State.TlsShutdownPending = false;
        State.TlsShutdownComplete = true;
        State.ClearControlRestored = true;
        FtpEvent eventData = FtpClearControlAudit.CreateDowngradeEvent(
            State.Command, State.Reply, Session.State);
        State.AuditEvent = eventData;
        State.AuditRecorded = FtpClearControlAudit.RecordEvent(EventSink, eventData);
    }

    internal void FailReplyWrite()
    {
        if (!State.ReplyWritePending)
        {
            throw new FtpProtocolException(
                "No accepted server CCC reply write is in progress.", null);
        }
        TerminateConnection();
    }

    internal void RecordPolicyRejection(FtpCommand command, FtpReply reply)
    {
        FtpEvent eventData = FtpClearControlAudit.CreatePolicyRejectionEvent(
            command, reply, Session.State);
        State.AuditEvent = eventData;
        State.AuditRecorded = FtpClearControlAudit.RecordEvent(EventSink, eventData);
    }

    internal void TerminateConnection()
    {
        State.CommandPending = false;
        State.ReplyWritePending = false;
        State.TlsShutdownPending = false;
        State.Failed = true;
        State.ConnectionMustClose = true;
        Session.CloseConnection();
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
