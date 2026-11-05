using System;

namespace ftp;

internal class FtpClientClearControlCoordinator
{
    internal FtpClearControlState State;
    internal FtpTlsOptions Options;
    internal FtpSessionStateMachine Session;
    internal FtpTlsClientStream TlsStream;
    internal IFtpEventSink? EventSink;

    internal FtpClientClearControlCoordinator(
        FtpTlsOptions? options, FtpSessionStateMachine? session,
        FtpTlsClientStream? tlsStream, IFtpEventSink? eventSink)
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
                "CCC requires the client TLS stream to leave the control transport open.", null);
        }

        State = new FtpClearControlState();
        Options = options;
        Session = session;
        TlsStream = tlsStream;
        EventSink = eventSink;
    }

    internal FtpCommand BeginClearControl()
    {
        if (State.CommandPending || State.TlsShutdownPending ||
            State.ClearControlRestored || State.Failed)
        {
            throw new FtpProtocolException(
                "CCC cannot begin in the current client downgrade state.", null);
        }

        FtpCommand command = CreateCommand();
        if (!Options.AllowClearCommandChannel)
        {
            State.Rejected = true;
            State.Command = command;
            RecordPolicyRejection(command, null);
            throw new FtpConfigurationException(
                "CCC requires explicit client opt-in.", null);
        }

        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        State.CommandPending = true;
        State.Rejected = false;
        State.Command = command;
        State.Reply = null;
        return command;
    }

    internal void ApplyClearControlReply(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (!State.CommandPending || State.Command == null)
        {
            throw new FtpProtocolException(
                "No client CCC command is awaiting a reply.", null);
        }

        State.CommandPending = false;
        State.Reply = reply;
        if (reply.Code == FtpReplyCodes.CommandOkay)
        {
            State.TlsShutdownPending = true;
            return;
        }
        if (reply.Code >= 400 && reply.Code <= 599)
        {
            State.Rejected = true;
            if (reply.Code == FtpReplyCodes.ServiceUnavailable)
            {
                TerminateConnection();
            }
            return;
        }

        TerminateConnection();
        throw new FtpProtocolException(
            "CCC must receive reply 200 or a negative completion reply.", null);
    }

    internal void CompleteClearControl()
    {
        if (!State.TlsShutdownPending || State.Command == null || State.Reply == null)
        {
            throw new FtpProtocolException(
                "No accepted client CCC exchange is awaiting TLS shutdown.", null);
        }

        FtpFaultIsolationResult shutdown = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TlsStream.ShutdownClientTlsForClearControl();
            return null;
        });
        if (!shutdown.Succeeded)
        {
            TerminateConnection();
            throw new FtpTlsException(
                "The client control connection could not complete the CCC TLS shutdown.",
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

    internal void FailReplyExchange()
    {
        if (!State.CommandPending)
        {
            throw new FtpProtocolException(
                "No client CCC reply exchange is in progress.", null);
        }
        TerminateConnection();
    }

    internal FtpCommand CreateCommand()
    {
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Ccc;
        command.Verb = FtpCommandNames.Ccc;
        command.RawText = FtpCommandNames.Ccc;
        return command;
    }

    internal void RecordPolicyRejection(FtpCommand command, FtpReply? reply)
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
}
