using System;

namespace ftp;

internal class FtpClientAbortOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;
    internal FtpControlConnection Connection;

    internal FtpClientAbortOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required to abort a transfer.",
                null);
        }

        ClientState = clientState;
        Options = clientState.Options;
        Connection = clientState.Connection;
    }

    internal FtpCommandResult AbortCurrentTransfer()
    {
        ClientState.RequireOpenSession("AbortTransfer");
        FtpClientCommandExecutor executor =
            new FtpClientCommandExecutor(ClientState);
        FtpCommand command = executor.CreateSimpleCommand(
            FtpCommandKind.Abor, null);
        FtpSequenceValidator.RequireAllowedCommand(
            command, ClientState.Session.State);
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ExecuteAbortExchange(command);
        });
        if (operation.Succeeded)
        {
            FtpCommandResult? result = operation.Value as FtpCommandResult;
            if (result != null)
            {
                return result;
            }
        }

        FtpFaultIsolationRunner.RunOperation(() =>
        {
            ClientState.CloseSession();
            return null;
        });
        if (operation.Failure != null)
        {
            throw operation.Failure;
        }
        throw new FtpConnectionException(
            "The FTP abort exchange did not reach a terminal result.", null);
    }

    internal FtpCommandResult ExecuteAbortExchange(FtpCommand command)
    {
        ReleaseDataConnection();

        FtpReplyCoordinator coordinator = new FtpReplyCoordinator();
        coordinator.BeginAbortCommand(command);
        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(Options);
        exchange.WriteCommand(Connection, command);

        FtpDeadline deadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ControlReadTimeoutMilliseconds);
        FtpReply firstReply = ReadAbortReply(deadline);
        FtpReplyCoordinationResult coordination =
            ApplyAbortReply(coordinator, command, firstReply);
        if (coordination.CompletedAbortCommand == null &&
            coordination.AbortCommandPending)
        {
            FtpReply secondReply = ReadAbortReply(deadline);
            coordination = ApplyAbortReply(
                coordinator, command, secondReply);
        }

        FtpCommandResult? completed = coordination.CompletedAbortCommand;
        if (completed == null)
        {
            throw new FtpProtocolException(
                "The FTP ABOR exchange did not return a terminal reply.", null);
        }

        if (completed.ConnectionClosed)
        {
            ClientState.CloseSession();
        }
        else
        {
            ClientState.UpdateReconnectState();
        }
        return completed;
    }

    internal void ReleaseDataConnection()
    {
        FtpClientDataConnectionStrategy? strategy = ClientState.DataStrategy;
        ClientState.DataStrategy = null;
        if (strategy != null)
        {
            strategy.AbortDataConnection();
        }
    }

    internal FtpReply ReadAbortReply(FtpDeadline deadline)
    {
        return FtpControlReplyReader.ReadReply(
            Connection.Stream,
            Connection.Socket,
            deadline,
            Options.Timeouts.CancellationToken,
            Options.StrictReplyParsing);
    }

    internal FtpReplyCoordinationResult ApplyAbortReply(
        FtpReplyCoordinator coordinator,
        FtpCommand command,
        FtpReply reply)
    {
        FtpReplyCoordinationResult result =
            coordinator.ReceiveControlReply(reply);
        ClientState.Session.ApplyCommandReply(command, reply);
        return result;
    }
}
