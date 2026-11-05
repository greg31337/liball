using System;

namespace ftp;

internal class FtpReplyCoordinator
{
    internal FtpCommand? PendingDataCommand;
    internal FtpReply? PendingPreliminaryReply;
    internal FtpCommand? PendingAbortCommand;
    internal FtpReply[] PendingAbortReplies;
    internal bool ConnectionClosed;

    internal FtpReplyCoordinator()
    {
        PendingDataCommand = null;
        PendingPreliminaryReply = null;
        PendingAbortCommand = null;
        PendingAbortReplies = new FtpReply[0];
        ConnectionClosed = false;
    }

    internal void BeginDataCommand(FtpCommand? command)
    {
        FtpCommand checkedCommand = RequireCommand(command, "data");
        if (ConnectionClosed)
        {
            throw new FtpProtocolException("An FTP data command cannot begin after the control connection has closed.", null);
        }
        if (PendingDataCommand != null)
        {
            throw new FtpProtocolException("Only one FTP data command can await completion at a time.", null);
        }
        if (PendingAbortCommand != null)
        {
            throw new FtpProtocolException("An FTP data command cannot begin while ABOR awaits completion.", null);
        }
        if (!IsDataCommand(checkedCommand.Kind))
        {
            throw new FtpProtocolException("Only an FTP transfer or listing command can use data-command coordination.", null);
        }

        PendingDataCommand = checkedCommand;
        PendingPreliminaryReply = null;
    }

    internal void BeginAbortCommand(FtpCommand? command)
    {
        FtpCommand checkedCommand = RequireCommand(command, "ABOR");
        if (ConnectionClosed)
        {
            throw new FtpProtocolException("FTP ABOR cannot begin after the control connection has closed.", null);
        }
        if (PendingAbortCommand != null)
        {
            throw new FtpProtocolException("An FTP ABOR command already awaits completion.", null);
        }
        if (checkedCommand.Kind != FtpCommandKind.Abor)
        {
            throw new FtpProtocolException("The abort coordinator requires an ABOR command.", null);
        }

        PendingAbortCommand = checkedCommand;
        PendingAbortReplies = new FtpReply[0];
    }

    internal FtpReplyCoordinationResult ReceiveControlReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireReply(reply, "coordinated control");
        if (ConnectionClosed)
        {
            throw new FtpProtocolException("An FTP reply cannot be coordinated after the control connection has closed.", null);
        }

        if (checkedReply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            return HandleServiceClosure(checkedReply);
        }
        if (PendingAbortCommand != null)
        {
            return HandleAbortReply(checkedReply);
        }
        if (PendingDataCommand != null)
        {
            return HandleDataReply(checkedReply);
        }

        throw new FtpProtocolException("An FTP reply arrived without a command awaiting coordination.", null);
    }

    internal bool HasPendingDataCommand()
    {
        return PendingDataCommand != null;
    }

    internal bool HasPendingAbortCommand()
    {
        return PendingAbortCommand != null;
    }

    internal bool IsConnectionClosed()
    {
        return ConnectionClosed;
    }

    internal FtpReplyCoordinationResult HandleDataReply(FtpReply reply)
    {
        FtpCommand command = RequirePendingDataCommand();
        bool preliminary = IsTransferPreliminaryReply(reply.Code);
        if (PendingPreliminaryReply == null && preliminary)
        {
            PendingPreliminaryReply = reply;
            return CreateCurrentStateResult();
        }
        if (PendingPreliminaryReply != null && preliminary)
        {
            throw new FtpProtocolException("An FTP data command received more than one preliminary reply.", null);
        }

        FtpReply[] replies;
        if (PendingPreliminaryReply == null)
        {
            replies = new[] { reply };
        }
        else
        {
            replies = new[] { PendingPreliminaryReply, reply };
        }

        FtpCommandResult completed = FtpReplyExtractor.ExtractTransferCompletion(command, replies);
        ClearPendingDataCommand();
        FtpReplyCoordinationResult result = CreateCurrentStateResult();
        result.CompletedDataCommand = completed;
        return result;
    }

    internal FtpReplyCoordinationResult HandleAbortReply(FtpReply reply)
    {
        FtpCommand abortCommand = RequirePendingAbortCommand();
        bool transferAborted = reply.Code == FtpReplyCodes.ConnectionClosedTransferAborted;
        bool abortSucceeded = reply.Code == FtpReplyCodes.DataConnectionOpenNoTransfer ||
            reply.Code == FtpReplyCodes.ClosingDataConnection;
        FtpReplyClassification classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        bool abortFailed = classification.TransientFailure || classification.PermanentFailure;
        if (!transferAborted && !abortSucceeded && !abortFailed)
        {
            throw new FtpProtocolException("An FTP ABOR sequence did not end with a valid completion reply.", null);
        }
        if (transferAborted && PendingAbortReplies.Length != 0)
        {
            throw new FtpProtocolException("An FTP ABOR sequence contains an unexpected 426 reply.", null);
        }

        PendingAbortReplies = AppendReply(PendingAbortReplies, reply);
        FtpReplyCoordinationResult result;
        if (transferAborted)
        {
            result = CreateCurrentStateResult();
            if (PendingDataCommand != null)
            {
                result.CompletedDataCommand = CompleteDataCommandWithReply(reply);
            }
            result.DataCommandPending = PendingDataCommand != null;
            return result;
        }

        result = CreateCurrentStateResult();
        if (abortSucceeded && PendingDataCommand != null)
        {
            result.CompletedDataCommand = CompleteAbortedDataCommand(reply);
        }
        result.CompletedAbortCommand = CreateCommandResult(
            abortCommand, PendingAbortReplies, reply, abortSucceeded, false, null);
        ClearPendingAbortCommand();
        result.DataCommandPending = PendingDataCommand != null;
        result.AbortCommandPending = false;
        return result;
    }

    internal FtpReplyCoordinationResult HandleServiceClosure(FtpReply reply)
    {
        ConnectionClosed = true;
        FtpReplyCoordinationResult result = CreateCurrentStateResult();
        result.ConnectionClosed = true;
        result.AsynchronousReply = reply;
        if (PendingDataCommand != null)
        {
            result.CompletedDataCommand = CompleteDataCommandWithReply(reply);
        }
        if (PendingAbortCommand != null)
        {
            FtpCommand abortCommand = RequirePendingAbortCommand();
            PendingAbortReplies = AppendReply(PendingAbortReplies, reply);
            result.CompletedAbortCommand = CreateCommandResult(
                abortCommand, PendingAbortReplies, reply, false, true, null);
            ClearPendingAbortCommand();
        }
        result.DataCommandPending = false;
        result.AbortCommandPending = false;
        return result;
    }

    internal FtpCommandResult CompleteDataCommandWithReply(FtpReply reply)
    {
        FtpCommand command = RequirePendingDataCommand();
        FtpReply[] replies;
        if (PendingPreliminaryReply == null)
        {
            replies = new[] { reply };
        }
        else
        {
            replies = new[] { PendingPreliminaryReply, reply };
        }

        FtpCommandResult completed = FtpReplyExtractor.ExtractTransferCompletion(command, replies);
        ClearPendingDataCommand();
        return completed;
    }

    internal FtpCommandResult CompleteAbortedDataCommand(FtpReply reply)
    {
        FtpCommand command = RequirePendingDataCommand();
        FtpReply[] replies;
        if (PendingPreliminaryReply == null)
        {
            replies = new[] { reply };
        }
        else
        {
            replies = new[] { PendingPreliminaryReply, reply };
        }

        FtpCommandResult completed = CreateCommandResult(
            command, replies, reply, false, false, "The FTP data command was aborted.");
        completed.PreliminaryReply = PendingPreliminaryReply;
        ClearPendingDataCommand();
        return completed;
    }

    internal FtpReplyCoordinationResult CreateCurrentStateResult()
    {
        FtpReplyCoordinationResult result = new FtpReplyCoordinationResult();
        result.DataCommandPending = PendingDataCommand != null;
        result.AbortCommandPending = PendingAbortCommand != null;
        result.ConnectionClosed = ConnectionClosed;
        return result;
    }

    internal static FtpCommandResult CreateCommandResult(
        FtpCommand command, FtpReply[] replies, FtpReply finalReply,
        bool succeeded, bool connectionClosed, string? failureMessage)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = replies;
        result.FinalReply = finalReply;
        result.Succeeded = succeeded;
        result.ConnectionClosed = connectionClosed;
        result.FailureMessage = failureMessage;
        return result;
    }

    internal static FtpReply[] AppendReply(FtpReply[] replies, FtpReply reply)
    {
        FtpReply[] appended = new FtpReply[replies.Length + 1];
        int index = 0;
        while (index < replies.Length)
        {
            appended[index] = replies[index];
            index++;
        }
        appended[replies.Length] = reply;
        return appended;
    }

    internal static bool IsDataCommand(FtpCommandKind kind)
    {
        return kind == FtpCommandKind.Appe || kind == FtpCommandKind.List ||
            kind == FtpCommandKind.Mlsd || kind == FtpCommandKind.Nlst ||
            kind == FtpCommandKind.Retr || kind == FtpCommandKind.Stor ||
            kind == FtpCommandKind.Stou;
    }

    internal static bool IsTransferPreliminaryReply(int code)
    {
        return code == FtpReplyCodes.DataConnectionAlreadyOpen ||
            code == FtpReplyCodes.FileStatusOkayOpeningDataConnection;
    }

    internal static FtpCommand RequireCommand(FtpCommand? command, string commandName)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (command.Verb == null)
        {
            throw new FtpProtocolException($"The FTP {commandName} command verb is unavailable.", null);
        }
        return command;
    }

    internal FtpCommand RequirePendingDataCommand()
    {
        if (PendingDataCommand == null)
        {
            throw new FtpProtocolException("No FTP data command awaits completion.", null);
        }
        return PendingDataCommand;
    }

    internal FtpCommand RequirePendingAbortCommand()
    {
        if (PendingAbortCommand == null)
        {
            throw new FtpProtocolException("No FTP ABOR command awaits completion.", null);
        }
        return PendingAbortCommand;
    }

    internal void ClearPendingDataCommand()
    {
        PendingDataCommand = null;
        PendingPreliminaryReply = null;
    }

    internal void ClearPendingAbortCommand()
    {
        PendingAbortCommand = null;
        PendingAbortReplies = new FtpReply[0];
    }
}
