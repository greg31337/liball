using System;

namespace ftp;

internal class FtpClientCommandExecutor
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;
    internal FtpControlConnection Connection;
    internal FtpControlCommandExchange Exchange;

    internal FtpClientCommandExecutor(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required to execute commands.",
                null);
        }

        ClientState = clientState;
        Options = clientState.Options;
        Connection = clientState.Connection;
        Exchange = new FtpControlCommandExchange(Options);
    }

    internal FtpCommandResult ExecuteRawCommand(FtpCommand? command)
    {
        FtpCommand prepared = PrepareRawCommand(command);
        FtpCommandResult primary = ExecutePreparedCommand(prepared);
        return ExecuteHistoricFallback(prepared, primary);
    }

    internal FtpCommandResult ExecuteHistoricFallback(
        FtpCommand primaryCommand,
        FtpCommandResult primaryResult)
    {
        FtpClientQuirkPolicy policy =
            new FtpClientQuirkPolicy(Options.Quirks);
        if (!policy.ShouldFallbackToHistoricAlias(
            primaryCommand.Kind, primaryResult.FinalReply) ||
            primaryResult.ConnectionClosed)
        {
            return primaryResult;
        }
        FtpCommandDefinition? alias =
            FtpCommandCatalog.FindHistoricAliasForTarget(
                primaryCommand.Kind);
        if (alias == null)
        {
            return primaryResult;
        }
        FtpCommand fallback = CreateSimpleCommand(
            alias.Kind, primaryCommand.Argument);
        return ExecutePreparedCommand(fallback);
    }

    internal FtpCommandResult[] ExecuteRawBatch(FtpCommand[]? commands)
    {
        FtpCommand[] prepared = PrepareRawBatch(commands);
        FtpCommandResult[] results = new FtpCommandResult[prepared.Length];
        int count = 0;
        while (count < prepared.Length)
        {
            FtpCommandResult result = ExecutePreparedCommand(prepared[count]);
            results[count] = result;
            count++;
            if (result.ConnectionClosed)
            {
                break;
            }
        }

        if (count == results.Length)
        {
            return results;
        }

        FtpCommandResult[] completed = new FtpCommandResult[count];
        Array.Copy(results, completed, count);
        return completed;
    }

    internal FtpCommand CreateSimpleCommand(
        FtpCommandKind kind,
        string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
        command.RawText = argument == null ?
            command.Verb : command.Verb + " " + argument;
        return command;
    }

    internal FtpCommand PrepareRawCommand(FtpCommand? command)
    {
        string commandLine = FtpCommandWriter.BuildCommandLine(command);
        FtpCommand prepared = FtpCommandParser.ParseDecodedCommand(commandLine);
        RequireSingleReplyCommand(prepared);
        return prepared;
    }

    internal FtpCommand[] PrepareRawBatch(FtpCommand[]? commands)
    {
        if (commands == null)
        {
            throw new ArgumentNullException("commands");
        }

        FtpCommand[] prepared = new FtpCommand[commands.Length];
        int index = 0;
        while (index < commands.Length)
        {
            prepared[index] = PrepareRawCommand(commands[index]);
            index++;
        }
        return prepared;
    }

    internal FtpCommandResult ExecutePreparedCommand(FtpCommand command)
    {
        FtpSequenceValidator.RequireAllowedCommand(
            command, ClientState.Session.State);
        FtpReply reply = ExchangeCommand(command);
        FtpReplyClassification classification =
            FtpReplyClassifier.ClassifyCode(reply.Code);
        if (classification.Category == FtpReplyCategory.PositivePreliminary)
        {
            CloseAfterFault();
            throw new FtpProtocolException(
                "A raw single-reply FTP command cannot continue after a preliminary reply.",
                null);
        }
        if (command.Kind == FtpCommandKind.Rein &&
            classification.Succeeded && reply.Code != FtpReplyCodes.ServiceReady)
        {
            CloseAfterFault();
            throw new FtpProtocolException(
                "A successful FTP REIN reply must use code 220.", null);
        }
        if (command.Kind == FtpCommandKind.Quit && classification.Succeeded &&
            reply.Code != FtpReplyCodes.ServiceClosingControlConnection)
        {
            CloseAfterFault();
            throw new FtpProtocolException(
                "A successful FTP QUIT reply must use code 221.", null);
        }

        ClientState.Session.ApplyCommandReply(command, reply);
        FtpCommandResult result = CreateCommandResult(
            command, reply, classification.Succeeded);
        if (command.Kind == FtpCommandKind.Rein &&
            reply.Code == FtpReplyCodes.ServiceReady)
        {
            CompleteReinitialize();
        }
        if (reply.Code == FtpReplyCodes.ServiceUnavailable ||
            (command.Kind == FtpCommandKind.Quit &&
                reply.Code == FtpReplyCodes.ServiceClosingControlConnection))
        {
            result.ConnectionClosed = true;
            ClientState.CloseSession();
        }
        return result;
    }

    internal FtpReply ExchangeCommand(FtpCommand command)
    {
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Exchange.ExecuteCommand(Connection, command);
            });
        if (operation.Succeeded)
        {
            FtpReply? reply = operation.Value as FtpReply;
            if (reply != null)
            {
                return reply;
            }
        }

        CloseAfterFault();
        if (operation.Failure != null)
        {
            throw operation.Failure;
        }
        throw new FtpConnectionException(
            "The FTP command exchange did not reach a terminal result.", null);
    }

    internal FtpCommandResult CreateCommandResult(
        FtpCommand command,
        FtpReply reply,
        bool succeeded)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = new[] { reply };
        result.FinalReply = reply;
        result.Succeeded = succeeded;
        result.ConnectionClosed = reply.Code == FtpReplyCodes.ServiceUnavailable;
        return result;
    }

    internal void CompleteReinitialize()
    {
        if (Connection.TlsClient != null)
        {
            FtpTlsClientStream tlsClient = Connection.TlsClient;
            FtpFaultIsolationResult shutdown =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    tlsClient.ShutdownClientTlsForClearControl();
                    return null;
                });
            if (!shutdown.Succeeded)
            {
                CloseAfterFault();
                throw new FtpTlsException(
                    "The FTP client could not clear TLS after REIN.",
                    shutdown.Failure);
            }

            Connection.TlsClient = null;
            Connection.TlsResult = null;
            Connection.Stream = Connection.TransportStream;
        }

        Connection.ControlProtected = false;
        Connection.PlaintextFallback = false;
        Connection.Utf8Enabled = false;
        ClientState.TlsCoordinator = null;
        ClientState.LastAuthentication = null;
        ClientState.LatestRestartMarker = null;
        ClientState.LatestLocalRestartMarker = null;
        ClientState.Features = new FtpFeatureSet();
        ClientState.FeatureState = new FtpClientFeatureState();
    }

    internal void RequireSingleReplyCommand(FtpCommand command)
    {
        bool credentialCommand = command.Kind == FtpCommandKind.User ||
            command.Kind == FtpCommandKind.Pass ||
            command.Kind == FtpCommandKind.Acct;
        if (credentialCommand && !Connection.ControlProtected &&
            !Options.AllowPlaintextCredentials)
        {
            throw new FtpConfigurationException(
                "Raw plaintext authentication commands require explicit opt-in.",
                null);
        }

        bool dataCommand = FtpReplyCoordinator.IsDataCommand(command.Kind);
        bool coordinatedCommand = command.Kind == FtpCommandKind.Abor ||
            command.Kind == FtpCommandKind.Auth ||
            command.Kind == FtpCommandKind.Adat ||
            command.Kind == FtpCommandKind.Ccc ||
            command.Kind == FtpCommandKind.Mic ||
            command.Kind == FtpCommandKind.Conf ||
            command.Kind == FtpCommandKind.Enc;
        if (dataCommand || coordinatedCommand)
        {
            throw new FtpProtocolException(
                $"FTP command {command.Verb} requires a dedicated data or security operation.",
                null);
        }
    }

    internal void CloseAfterFault()
    {
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            ClientState.CloseSession();
            return null;
        });
    }
}
