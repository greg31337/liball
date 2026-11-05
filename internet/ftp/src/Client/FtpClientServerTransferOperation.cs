using System;
using System.Collections.Generic;
using System.Net;

namespace ftp;

internal class FtpClientServerTransferOperation
{
    internal FtpServerTransferRequest Request;
    internal FtpClientState SourceState;
    internal FtpClientState TargetState;
    internal FtpClientOptions SourceOptions;
    internal FtpClientOptions TargetOptions;
    internal FtpControlConnection SourceConnection;
    internal FtpControlConnection TargetConnection;
    internal FtpThirdPartyTransferCoordinator Coordinator;
    internal List<FtpCommandResult> SourceCommands;
    internal List<FtpCommandResult> TargetCommands;
    internal string? UniqueTargetName;

    internal FtpClientServerTransferOperation(FtpServerTransferRequest? request)
    {
        FtpTransferValidator.ValidateServerTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A server-to-server transfer request is required.", null);
        }
        FtpClient? sourceClient = request.SourceClient as FtpClient;
        FtpClient? targetClient = request.TargetClient as FtpClient;
        if (sourceClient == null || targetClient == null)
        {
            throw new FtpConfigurationException(
                "Server-to-server transfer requires concrete FTP client sessions.",
                null);
        }
        sourceClient.State.RequireOpenSession("TransferServerToServer");
        targetClient.State.RequireOpenSession("TransferServerToServer");
        if (sourceClient.State.Options == null ||
            sourceClient.State.Connection == null ||
            targetClient.State.Options == null ||
            targetClient.State.Connection == null)
        {
            throw new FtpConnectionException(
                "Both server-to-server sessions must be configured and open.",
                null);
        }

        Request = request;
        SourceState = sourceClient.State;
        TargetState = targetClient.State;
        SourceOptions = sourceClient.State.Options;
        TargetOptions = targetClient.State.Options;
        SourceConnection = sourceClient.State.Connection;
        TargetConnection = targetClient.State.Connection;
        SourceCommands = new List<FtpCommandResult>();
        TargetCommands = new List<FtpCommandResult>();
        UniqueTargetName = null;

        FtpDataConnectionOptions sourceData =
            FtpClientOptionsSnapshot.CopyDataConnection(
                SourceOptions.DataConnection);
        FtpDataConnectionOptions targetData =
            FtpClientOptionsSnapshot.CopyDataConnection(
                TargetOptions.DataConnection);
        ApplyFeaturePolicy(sourceData, targetData);
        IPAddress targetPeer =
            FtpClientDataConnectionStrategy.ParseControlAddress(
                TargetConnection.RemoteEndpoint, "target remote");
        Coordinator = new FtpThirdPartyTransferCoordinator(
            targetPeer, sourceData, targetData,
            SourceState.Session.State.DataProtectionLevel,
            TargetState.Session.State.DataProtectionLevel);
    }

    internal FtpServerTransferResult Transfer()
    {
        FtpTransferOptions sourceOriginal = CaptureTransferParameters(
            SourceState.Session.State);
        FtpTransferOptions targetOriginal = CaptureTransferParameters(
            TargetState.Session.State);
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return ExecuteTransfer();
            });
        FtpFaultIsolationResult sourceRestoration =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                RestoreSourceParameters(sourceOriginal);
                return null;
            });
        FtpFaultIsolationResult targetRestoration =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                RestoreTargetParameters(targetOriginal);
                return null;
            });
        if (!operation.Succeeded ||
            !sourceRestoration.Succeeded || !targetRestoration.Succeeded)
        {
            CloseBothSessions();
            if (!operation.Succeeded && operation.Failure != null)
            {
                throw new FtpTransferException(
                    "The FTP server-to-server transfer did not complete.",
                    operation.Failure);
            }
            Exception? restorationFailure = !sourceRestoration.Succeeded ?
                sourceRestoration.Failure : targetRestoration.Failure;
            throw new FtpConnectionException(
                "The FTP server-to-server transfer could not restore both sessions.",
                restorationFailure);
        }

        FtpServerTransferResult? completed =
            operation.Value as FtpServerTransferResult;
        if (completed == null)
        {
            CloseBothSessions();
            throw new FtpTransferException(
                "The FTP server-to-server transfer produced no result.", null);
        }
        completed.SourceCommands = SourceCommands.ToArray();
        completed.TargetCommands = TargetCommands.ToArray();
        SourceState.UpdateReconnectState();
        TargetState.UpdateReconnectState();
        return completed;
    }

    internal FtpServerTransferResult ExecuteTransfer()
    {
        FtpCommandResult? sourceParameterFailure =
            new FtpClientDownloadOperation(SourceState)
                .ApplyTransferParameters(Request.Options, SourceCommands);
        if (sourceParameterFailure != null)
        {
            return CreateFailureResult(
                FtpClientListingOperation.CreateFailureMessage(
                    sourceParameterFailure,
                    sourceParameterFailure.Command.Verb));
        }
        FtpCommandResult? targetParameterFailure =
            new FtpClientUploadOperation(TargetState)
                .ApplyTransferParameters(Request.Options, TargetCommands);
        if (targetParameterFailure != null)
        {
            return CreateFailureResult(
                FtpClientListingOperation.CreateFailureMessage(
                    targetParameterFailure,
                    targetParameterFailure.Command.Verb));
        }
        FtpCommand sourceCommand = CreateTransferCommand(
            SourceState, FtpCommandKind.Retr, Request.SourcePath);
        FtpCommandKind targetKind = Request.Append ?
            FtpCommandKind.Appe : Request.Unique ?
                FtpCommandKind.Stou : FtpCommandKind.Stor;
        string? targetArgument = targetKind == FtpCommandKind.Stou ?
            null : Request.TargetPath;
        FtpCommand targetCommand = CreateTransferCommand(
            TargetState, targetKind, targetArgument);
        FtpCommandResult? targetPret =
            new FtpClientPretOperation(TargetState).SendPret(targetCommand);
        if (targetPret != null)
        {
            TargetCommands.Add(targetPret);
            if (!targetPret.Succeeded)
            {
                return CreateFailureResult(
                    FtpClientListingOperation.CreateFailureMessage(
                        targetPret, targetPret.Command.Verb));
            }
        }
        FtpCommandResult? sourcePret =
            new FtpClientPretOperation(SourceState).SendPret(sourceCommand);
        if (sourcePret != null)
        {
            SourceCommands.Add(sourcePret);
            if (!sourcePret.Succeeded)
            {
                return CreateFailureResult(
                    FtpClientListingOperation.CreateFailureMessage(
                        sourcePret, sourcePret.Command.Verb));
            }
        }
        if (!ConfigureTargetPassive())
        {
            return CreateFailureResult(
                "The target FTP session rejected passive FXP setup.");
        }
        if (!ConfigureSourceActive())
        {
            TargetState.Session.ClearDataConnectionState();
            return CreateFailureResult(
                "The source FTP session rejected active FXP setup.");
        }
        return ExecutePairedCommands(
            sourceCommand, targetCommand, targetKind);
    }

    internal bool ConfigureTargetPassive()
    {
        FtpCommand command = Coordinator.TargetUseExtendedPassive ?
            Coordinator.BeginTargetExtendedPassiveSetup(
                FtpAddressFamily.Unspecified) :
            Coordinator.BeginTargetPassiveSetup();
        FtpCommandResult result = ExecuteSetupCommand(
            TargetState, command);
        TargetCommands.Add(result);
        if (result.Succeeded && result.FinalReply != null)
        {
            Coordinator.ApplyTargetPassiveSetupReply(result.FinalReply);
            return true;
        }
        if (command.Kind != FtpCommandKind.Epsv ||
            result.FinalReply == null ||
            !Coordinator.TargetAllowPassiveFallback ||
            !IsUnsupportedReply(result.FinalReply))
        {
            return false;
        }

        FtpCommand fallback =
            Coordinator.BeginTargetPassiveFallback(result.FinalReply);
        FtpCommandResult fallbackResult = ExecuteSetupCommand(
            TargetState, fallback);
        TargetCommands.Add(fallbackResult);
        if (!fallbackResult.Succeeded || fallbackResult.FinalReply == null)
        {
            return false;
        }
        Coordinator.ApplyTargetPassiveSetupReply(
            fallbackResult.FinalReply);
        return true;
    }

    internal bool ConfigureSourceActive()
    {
        FtpCommand command = Coordinator.SourceUseExtendedActive ?
            Coordinator.BeginSourceExtendedActiveSetup() :
            Coordinator.BeginSourceActiveSetup();
        FtpCommandResult result = ExecuteSetupCommand(
            SourceState, command);
        SourceCommands.Add(result);
        if (result.Succeeded && result.FinalReply != null)
        {
            Coordinator.ApplySourceActiveSetupReply(result.FinalReply);
            return true;
        }
        if (command.Kind != FtpCommandKind.Eprt ||
            result.FinalReply == null ||
            !Coordinator.SourceAllowActiveFallback ||
            !IsUnsupportedReply(result.FinalReply))
        {
            return false;
        }

        FtpCommand fallback =
            Coordinator.BeginSourceActiveFallback(result.FinalReply);
        FtpCommandResult fallbackResult = ExecuteSetupCommand(
            SourceState, fallback);
        SourceCommands.Add(fallbackResult);
        if (!fallbackResult.Succeeded || fallbackResult.FinalReply == null)
        {
            return false;
        }
        Coordinator.ApplySourceActiveSetupReply(
            fallbackResult.FinalReply);
        return true;
    }

    internal FtpServerTransferResult ExecutePairedCommands(
        FtpCommand sourceCommand,
        FtpCommand targetCommand,
        FtpCommandKind targetKind)
    {
        Coordinator.BeginPairedTransfer(sourceCommand, targetCommand);

        FtpReply targetFirst = ExchangeTransferCommand(
            TargetState, TargetOptions, TargetConnection, targetCommand);
        Coordinator.ApplyTargetTransferReply(targetFirst);
        CaptureUniqueTargetName(targetKind, targetFirst);
        if (!FtpReplyCoordinator.IsTransferPreliminaryReply(
            targetFirst.Code))
        {
            AppendCompletedTransferResults();
            SourceState.Session.ClearDataConnectionState();
            return CreateTransferResult();
        }

        FtpReply sourceFirst = ExchangeTransferCommand(
            SourceState, SourceOptions, SourceConnection, sourceCommand);
        Coordinator.ApplySourceTransferReply(sourceFirst);
        if (FtpReplyCoordinator.IsTransferPreliminaryReply(sourceFirst.Code))
        {
            FtpReply sourceFinal = ReadTransferReply(
                SourceState, SourceOptions, SourceConnection);
            Coordinator.ApplySourceTransferReply(sourceFinal);
        }
        FtpReply targetFinal = ReadTransferReply(
            TargetState, TargetOptions, TargetConnection);
        Coordinator.ApplyTargetTransferReply(targetFinal);
        CaptureUniqueTargetName(targetKind, targetFinal);
        AppendCompletedTransferResults();
        return CreateTransferResult();
    }

    internal FtpCommandResult ExecuteSetupCommand(
        FtpClientState state,
        FtpCommand command)
    {
        return new FtpClientCommandExecutor(state)
            .ExecuteRawCommand(command);
    }

    internal FtpReply ExchangeTransferCommand(
        FtpClientState state,
        FtpClientOptions options,
        FtpControlConnection connection,
        FtpCommand command)
    {
        FtpSequenceValidator.RequireAllowedCommand(
            command, state.Session.State);
        FtpReply reply = new FtpControlCommandExchange(options)
            .ExecuteCommand(connection, command);
        state.Session.ApplyCommandReply(command, reply);
        return reply;
    }

    internal FtpReply ReadTransferReply(
        FtpClientState state,
        FtpClientOptions options,
        FtpControlConnection connection)
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            options.Timeouts.ControlReadTimeoutMilliseconds);
        FtpReply reply = FtpControlReplyReader.ReadReply(
            connection.Stream, connection.Socket, deadline,
            options.Timeouts.CancellationToken,
            options.StrictReplyParsing);
        while (reply.Code == FtpReplyCodes.RestartMarker)
        {
            FtpClientMarkerRestart.RecordMarkerReply(state, reply);
            reply = FtpControlReplyReader.ReadReply(
                connection.Stream, connection.Socket, deadline,
                options.Timeouts.CancellationToken,
                options.StrictReplyParsing);
        }
        state.Session.ApplyTransferReply(reply);
        return reply;
    }

    internal void AppendCompletedTransferResults()
    {
        FtpThirdPartyTransferState current = Coordinator.GetCurrentState();
        if (current.SourceTransferResult != null)
        {
            SourceCommands.Add(current.SourceTransferResult);
        }
        if (current.TargetTransferResult != null)
        {
            TargetCommands.Add(current.TargetTransferResult);
        }
    }

    internal FtpServerTransferResult CreateTransferResult()
    {
        FtpThirdPartyTransferState current = Coordinator.GetCurrentState();
        FtpServerTransferResult result = new FtpServerTransferResult();
        result.Request = Request;
        result.SourceCommands = SourceCommands.ToArray();
        result.TargetCommands = TargetCommands.ToArray();
        result.Succeeded = current.Complete && current.Succeeded;
        result.UniqueTargetName = UniqueTargetName;
        if (!result.Succeeded)
        {
            result.FailureMessage = CreateTransferFailureMessage(current);
        }
        return result;
    }

    internal FtpServerTransferResult CreateFailureResult(string message)
    {
        FtpServerTransferResult result = new FtpServerTransferResult();
        result.Request = Request;
        result.SourceCommands = SourceCommands.ToArray();
        result.TargetCommands = TargetCommands.ToArray();
        result.FailureMessage = message;
        result.UniqueTargetName = UniqueTargetName;
        return result;
    }

    internal string CreateTransferFailureMessage(
        FtpThirdPartyTransferState state)
    {
        if (state.SourceTransferResult != null &&
            !state.SourceTransferResult.Succeeded)
        {
            return FtpClientListingOperation.CreateFailureMessage(
                state.SourceTransferResult,
                state.SourceTransferResult.Command.Verb);
        }
        if (state.TargetTransferResult != null &&
            !state.TargetTransferResult.Succeeded)
        {
            return FtpClientListingOperation.CreateFailureMessage(
                state.TargetTransferResult,
                state.TargetTransferResult.Command.Verb);
        }
        return "The paired FTP transfer did not reach completion on both sessions.";
    }

    internal void CaptureUniqueTargetName(
        FtpCommandKind targetKind,
        FtpReply reply)
    {
        if (targetKind == FtpCommandKind.Stou &&
            UniqueTargetName == null)
        {
            UniqueTargetName =
                FtpClientUploadOperation.ExtractUniqueName(reply.Text);
        }
    }

    internal void RestoreSourceParameters(FtpTransferOptions original)
    {
        SourceState.Session.ClearDataConnectionState();
        FtpCommandResult? failure =
            new FtpClientDownloadOperation(SourceState)
            .ApplyTransferParameters(original, SourceCommands);
        if (failure != null)
        {
            throw new FtpConnectionException(
                "The source FTP session rejected transfer-parameter restoration.",
                null);
        }
    }

    internal void RestoreTargetParameters(FtpTransferOptions original)
    {
        TargetState.Session.ClearDataConnectionState();
        FtpCommandResult? failure =
            new FtpClientUploadOperation(TargetState)
            .ApplyTransferParameters(original, TargetCommands);
        if (failure != null)
        {
            throw new FtpConnectionException(
                "The target FTP session rejected transfer-parameter restoration.",
                null);
        }
    }

    internal void CloseBothSessions()
    {
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            SourceState.CloseSession();
            return null;
        });
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            TargetState.CloseSession();
            return null;
        });
    }

    internal void ApplyFeaturePolicy(
        FtpDataConnectionOptions sourceData,
        FtpDataConnectionOptions targetData)
    {
        if (SourceState.Features.Negotiated &&
            !SourceState.FeatureState.ExtendedActive)
        {
            sourceData.UseExtendedActive = false;
        }
        if (TargetState.Features.Negotiated &&
            !TargetState.FeatureState.ExtendedPassive &&
            TargetConnection.RemoteEndpoint.AddressFamily ==
                FtpAddressFamily.IPv4)
        {
            targetData.UseExtendedPassive = false;
        }
    }

    internal static FtpTransferOptions CaptureTransferParameters(
        FtpSequenceState state)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = state.TransferType;
        options.TextFormat = state.TextFormat;
        options.Structure = state.FileStructure;
        options.Mode = state.TransferMode;
        options.HasLocalByteSize = state.HasLocalByteSize;
        options.LocalByteSize = state.LocalByteSize;
        return options;
    }

    internal static FtpCommand CreateTransferCommand(
        FtpClientState state,
        FtpCommandKind kind,
        string? argument)
    {
        FtpCommand command = new FtpClientCommandExecutor(state)
            .CreateSimpleCommand(kind, argument);
        return FtpCommandParser.ParseDecodedCommand(
            FtpCommandWriter.BuildCommandLine(command));
    }

    internal static bool IsUnsupportedReply(FtpReply reply)
    {
        return reply.Code == FtpReplyCodes.SyntaxError ||
            reply.Code == FtpReplyCodes.SyntaxErrorInParameters ||
            reply.Code == FtpReplyCodes.CommandNotImplemented ||
            reply.Code == FtpReplyCodes.CommandParameterNotImplemented ||
            reply.Code == FtpReplyCodes.NetworkProtocolNotSupported;
    }
}
