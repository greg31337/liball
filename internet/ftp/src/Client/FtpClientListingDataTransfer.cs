using System;
using System.IO;
using System.Text;

namespace ftp;

internal class FtpClientListingDataTransfer
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;
    internal FtpControlConnection Connection;

    internal FtpClientListingDataTransfer(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for data listings.",
                null);
        }
        ClientState = clientState;
        Options = clientState.Options;
        Connection = clientState.Connection;
    }

    internal FtpClientListingTransferResult ReceiveListing(
        FtpCommandKind commandKind,
        string path)
    {
        ValidateRequest(commandKind, path);
        ClientState.RequireOpenSession("ReceiveListing");
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ExecuteListing(commandKind, path);
        });
        if (operation.Succeeded)
        {
            FtpClientListingTransferResult? result =
                operation.Value as FtpClientListingTransferResult;
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
            "The FTP listing transfer did not reach a terminal result.", null);
    }

    internal FtpClientListingTransferResult ExecuteListing(
        FtpCommandKind commandKind,
        string path)
    {
        string? argument = path.Length == 0 ? null : path;
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommand command = executor.CreateSimpleCommand(commandKind, argument);
        command = FtpCommandParser.ParseDecodedCommand(
            FtpCommandWriter.BuildCommandLine(command));
        FtpSequenceValidator.RequireAllowedCommand(command, ClientState.Session.State);
        FtpCommandResult? pret =
            new FtpClientPretOperation(ClientState).SendPret(command);
        if (pret != null && !pret.Succeeded)
        {
            return CreateTerminalResult(pret);
        }

        FtpClientDataConnectionStrategy strategy =
            new FtpClientDataConnectionStrategy(ClientState);
        strategy.PrepareDataConnection();
        ClientState.DataStrategy = strategy;

        FtpReplyCoordinator replies = new FtpReplyCoordinator();
        replies.BeginDataCommand(command);
        FtpControlCommandExchange exchange = new FtpControlCommandExchange(Options);
        FtpReply firstReply = exchange.ExecuteCommand(Connection, command);
        ClientState.Session.ApplyCommandReply(command, firstReply);
        FtpReplyCoordinationResult firstCoordination =
            replies.ReceiveControlReply(firstReply);
        if (!FtpReplyCoordinator.IsTransferPreliminaryReply(firstReply.Code))
        {
            ReleaseUnusedStrategy(strategy);
            FtpCommandResult completed = RequireCompletedCommand(
                firstCoordination, "initial listing");
            return CreateTerminalResult(completed);
        }

        using (MemoryStream buffer = new MemoryStream())
        {
            FtpClearDataStream transfer = ReceiveData(
                strategy, buffer, commandKind, path);
            FtpReply finalReply = ReadTerminalReply();
            FtpReplyCoordinationResult finalCoordination =
                replies.ReceiveControlReply(finalReply);
            ClientState.Session.ApplyTransferReply(finalReply);
            transfer.ApplyFinalReply(finalReply);
            ClientState.DataStrategy = null;

            FtpCommandResult completed = RequireCompletedCommand(
                finalCoordination, "terminal listing");
            FtpClientListingTransferResult result = new FtpClientListingTransferResult();
            result.CommandResult = completed;
            result.DataText = DecodeData(buffer);
            CompleteControlResult(completed);
            return result;
        }
    }

    internal FtpClearDataStream ReceiveData(
        FtpClientDataConnectionStrategy strategy,
        MemoryStream buffer,
        FtpCommandKind commandKind,
        string path)
    {
        FtpClearDataStreamRequest transferRequest = CreateTransferRequest(
            buffer, commandKind, path);
        if (ClientState.Session.State.DataProtectionLevel ==
            FtpDataProtectionLevel.Private)
        {
            FtpProtectedDataStreamRequest protectedRequest =
                new FtpProtectedDataStreamRequest();
            protectedRequest.Transfer = transferRequest;
            protectedRequest.Role = FtpDataChannelRole.Client;
            protectedRequest.TlsOptions = Options.Tls;
            protectedRequest.TargetHost = Options.Endpoint.Host;
            protectedRequest.HandshakeTimeoutMilliseconds =
                Options.Timeouts.TlsHandshakeTimeoutMilliseconds;
            protectedRequest.CertificateValidator = ClientState.CertificateValidator;
            protectedRequest.RemoteEndpoint = Options.Endpoint;
            FtpProtectedDataStream protectedTransfer =
                new FtpProtectedDataStream(strategy.Coordinator);
            protectedTransfer.BeginReceive(protectedRequest);
            protectedTransfer.ExecuteReceiveData();
            return protectedTransfer.Transfer;
        }

        FtpClearDataStream clearTransfer =
            new FtpClearDataStream(strategy.Coordinator);
        clearTransfer.BeginReceive(transferRequest);
        clearTransfer.ExecuteReceiveData();
        return clearTransfer;
    }

    internal FtpClearDataStreamRequest CreateTransferRequest(
        MemoryStream buffer,
        FtpCommandKind commandKind,
        string path)
    {
        FtpTransferOptions transferOptions = new FtpTransferOptions();
        if (ClientState.Session.State.TransferMode ==
            FtpTransferMode.Deflate)
        {
            transferOptions.Mode = FtpTransferMode.Deflate;
        }
        transferOptions.Timeouts = FtpClientOptionsSnapshot.CopyTimeouts(
            Options.Timeouts);
        FtpClearDataStreamRequest request = new FtpClearDataStreamRequest();
        request.CommandKind = commandKind;
        request.LocalStream = buffer;
        request.Options = transferOptions;
        request.Direction = FtpTransferDirection.Download;
        request.Path = path;
        request.ConnectionTimeoutMilliseconds =
            Options.Timeouts.DataConnectionTimeoutMilliseconds;
        request.TransferTimeoutMilliseconds = Options.Timeouts.IdleTimeoutMilliseconds;
        request.LeaveLocalStreamOpen = true;
        return request;
    }

    internal FtpReply ReadTerminalReply()
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ControlReadTimeoutMilliseconds);
        return FtpControlReplyReader.ReadReply(
            Connection.Stream,
            Connection.Socket,
            deadline,
            Options.Timeouts.CancellationToken,
            Options.StrictReplyParsing);
    }

    internal string DecodeData(MemoryStream buffer)
    {
        byte[] bytes = buffer.ToArray();
        Encoding encoding;
        if (Connection.Utf8Enabled)
        {
            FtpCommandParser.ValidateUtf8Bytes(bytes);
            encoding = Encoding.UTF8;
        }
        else
        {
            FtpCommandParser.ValidateAsciiBytes(bytes);
            encoding = Encoding.ASCII;
        }
        return encoding.GetString(bytes);
    }

    internal FtpClientListingTransferResult CreateTerminalResult(
        FtpCommandResult commandResult)
    {
        FtpClientListingTransferResult result = new FtpClientListingTransferResult();
        result.CommandResult = commandResult;
        CompleteControlResult(commandResult);
        return result;
    }

    internal void ReleaseUnusedStrategy(FtpClientDataConnectionStrategy strategy)
    {
        strategy.AbortDataConnection();
        ClientState.DataStrategy = null;
    }

    internal void CompleteControlResult(FtpCommandResult commandResult)
    {
        if (commandResult.ConnectionClosed)
        {
            ClientState.CloseSession();
        }
        else
        {
            ClientState.UpdateReconnectState();
        }
    }

    internal static FtpCommandResult RequireCompletedCommand(
        FtpReplyCoordinationResult coordination,
        string stage)
    {
        if (coordination.CompletedDataCommand == null)
        {
            throw new FtpProtocolException(
                $"The FTP {stage} reply did not complete its data command.", null);
        }
        return coordination.CompletedDataCommand;
    }

    internal static void ValidateRequest(
        FtpCommandKind commandKind,
        string? path)
    {
        bool listingCommand = commandKind == FtpCommandKind.List ||
            commandKind == FtpCommandKind.Mlsd ||
            commandKind == FtpCommandKind.Nlst;
        if (!listingCommand)
        {
            throw new FtpConfigurationException(
                "A LIST, MLSD, or NLST command is required for listing data.", null);
        }
        if (path == null || FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpConfigurationException(
                "A valid remote listing path is required.", null);
        }
    }
}

internal class FtpClientListingTransferResult
{
    internal FtpCommandResult CommandResult;
    internal string DataText;

    internal FtpClientListingTransferResult()
    {
        CommandResult = new FtpCommandResult();
        DataText = "";
    }
}
