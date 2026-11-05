using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ftp;

internal class FtpClientUploadOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions ClientOptions;
    internal FtpControlConnection Connection;
    internal string? ObservedRestartMarker;

    internal FtpClientUploadOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for uploads.", null);
        }
        ClientState = clientState;
        ClientOptions = clientState.Options;
        Connection = clientState.Connection;
        ObservedRestartMarker = null;
    }

    internal FtpTransferResult Upload(FtpTransferRequest? request)
    {
        FtpTransferValidator.ValidateTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException("A transfer request is required.", null);
        }
        if (request.Direction != FtpTransferDirection.Upload)
        {
            throw new FtpConfigurationException(
                "A storage operation requires upload direction.", null);
        }
        bool streamFile = request.Options.Mode == FtpTransferMode.Stream &&
            request.Options.Structure == FtpFileStructure.File;
        bool deflateStructure =
            request.Options.Mode == FtpTransferMode.Deflate;
        bool markerMode = FtpClientMarkerRestart.IsMarkerMode(request.Options);
        if (!streamFile && !deflateStructure && !markerMode)
        {
            throw new FtpConfigurationException(
                "Storage requires stream/file, MODE Z, or block/compressed transfer framing.",
                null);
        }
        if (request.Options.Mode == FtpTransferMode.Stream &&
            IsResumeRequested(request) &&
            request.Options.Type != FtpTransferType.Image)
        {
            throw new FtpConfigurationException(
                "REST STREAM upload resume requires image representation.", null);
        }
        if (IsResumeRequested(request) &&
            request.Options.CollisionPolicy == FtpCollisionPolicy.Unique)
        {
            throw new FtpConfigurationException(
                "A resumed upload cannot use unique storage.", null);
        }
        if (request.Options.AtomicUpload &&
            request.Options.CollisionPolicy == FtpCollisionPolicy.Unique)
        {
            throw new FtpConfigurationException(
                "An atomic upload cannot use unique destination naming.", null);
        }

        ClientState.RequireOpenSession("Transfer");
        FtpUploadSource source = OpenSource(request);
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ExecuteUpload(request, source);
        });
        FtpFaultIsolationResult sourceCleanup = CloseSource(source);
        if (!operation.Succeeded || !sourceCleanup.Succeeded)
        {
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                ClientState.CloseSession();
                return null;
            });
            if (!operation.Succeeded)
            {
                if (FtpCancellation.IsCancellationRequested(
                    request.Options.Timeouts.CancellationToken))
                {
                    throw new OperationCanceledException(
                        "The FTP upload was canceled.", operation.Failure,
                        request.Options.Timeouts.CancellationToken);
                }
                throw new FtpTransferException(
                    "The FTP upload did not complete.", operation.Failure);
            }
            throw new FtpTransferException(
                "The local upload source could not be closed.",
                sourceCleanup.Failure);
        }

        FtpTransferResult? completed = operation.Value as FtpTransferResult;
        if (completed == null)
        {
            throw new FtpTransferException(
                "The FTP upload did not produce a result.", null);
        }
        return completed;
    }

    internal FtpTransferResult ExecuteUpload(
        FtpTransferRequest request,
        FtpUploadSource source)
    {
        if (source.Stream == null)
        {
            throw new FtpConfigurationException(
                "A readable upload source is required.", null);
        }

        List<FtpCommandResult> commands = new List<FtpCommandResult>();
        FtpClientTransferMonitor monitor = new FtpClientTransferMonitor(
            ClientState, request.Observer, request.Options);
        string? atomicPath = request.Options.AtomicUpload ?
            CreateAtomicPath(request.RemotePath) : null;
        FtpTransferResult? collision = ApplyCollisionPolicy(request, commands);
        if (collision != null)
        {
            return collision;
        }
        FtpCommandResult? parentFailure = CreateParents(request, commands);
        if (parentFailure != null)
        {
            return CreateTerminalResult(
                request, source, commands, parentFailure, null, null);
        }
        FtpCommandResult? parameterFailure =
            ApplyTransferParameters(request.Options, commands);
        if (parameterFailure != null)
        {
            return CreateTerminalResult(
                request, source, commands, parameterFailure, null, null);
        }

        if (IsStreamResumeRequested(request))
        {
            FtpUploadResumeValidation validation =
                ValidateRemoteResume(request, source, commands);
            if (!validation.Safe)
            {
                if (MustResume(request))
                {
                    return CreateResumeFailureResult(
                        request, commands, validation.FailureMessage);
                }
                ResetSourceForFullUpload(source);
            }
        }

        FtpCommandKind commandKind = source.MarkerResuming ?
            FtpCommandKind.Stor : source.Resuming ?
            ResolveResumeStorageCommand(source) :
            request.Options.AtomicUpload || IsResumeRequested(request) ?
            FtpCommandKind.Stor :
            ResolveStorageCommand(request.Options.CollisionPolicy);
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        string? argument = commandKind == FtpCommandKind.Stou ?
            null : atomicPath == null ? request.RemotePath : atomicPath;
        FtpCommand command = executor.CreateSimpleCommand(commandKind, argument);
        command = FtpCommandParser.ParseDecodedCommand(
            FtpCommandWriter.BuildCommandLine(command));
        FtpSequenceValidator.RequireAllowedCommand(command, ClientState.Session.State);
        FtpCommandResult? pret =
            new FtpClientPretOperation(ClientState).SendPret(command);
        if (pret != null)
        {
            commands.Add(pret);
            if (!pret.Succeeded)
            {
                FtpTransferResult rejected = CreateTerminalResult(
                    request, source, commands, pret, null, null);
                CleanupAtomicUpload(request, atomicPath, commands);
                rejected.Commands = commands.ToArray();
                return rejected;
            }
        }
        FtpClientDataConnectionStrategy strategy =
            new FtpClientDataConnectionStrategy(ClientState);
        strategy.PrepareDataConnection();
        ClientState.DataStrategy = strategy;

        if ((source.Resuming && source.UseRest) || source.MarkerResuming)
        {
            string restartArgument = source.MarkerResuming ?
                FtpClientMarkerRestart.RequireServerMarker(ClientState) :
                FtpArgumentCodec.FormatRestartMarker(source.ResumeOffset);
            FtpCommandResult restart = ExecuteSimple(
                FtpCommandKind.Rest, restartArgument);
            commands.Add(restart);
            if (restart.FinalReply == null ||
                restart.FinalReply.Code != FtpReplyCodes.RequestedFileActionPending)
            {
                if (source.MarkerResuming)
                {
                    strategy.AbortDataConnection();
                    ClientState.DataStrategy = null;
                    return CreateResumeFailureResult(
                        request, commands,
                        "The FTP server did not accept the restart marker.");
                }
                else
                {
                    if (pret != null)
                    {
                        strategy.AbortDataConnection();
                        ClientState.DataStrategy = null;
                        return CreateResumeFailureResult(
                            request, commands,
                            "The FTP server rejected the restart marker after PRET.");
                    }
                    source.UseRest = false;
                    commandKind = FtpCommandKind.Appe;
                }
            }
            else
            {
                FtpReplyExtractor.ExtractRestartReadiness(restart.FinalReply);
                source.RestartAccepted = true;
                source.AcceptedRestartMarker = restartArgument;
                if (source.MarkerResuming)
                {
                    ClientState.LatestRestartMarker = restartArgument;
                }
            }
        }

        if (commandKind == FtpCommandKind.Appe)
        {
            command = executor.CreateSimpleCommand(commandKind, argument);
            command = FtpCommandParser.ParseDecodedCommand(
                FtpCommandWriter.BuildCommandLine(command));
            FtpSequenceValidator.RequireAllowedCommand(
                command, ClientState.Session.State);
        }

        FtpReplyCoordinator replies = new FtpReplyCoordinator();
        replies.BeginDataCommand(command);
        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(ClientOptions);
        FtpReply firstReply = exchange.ExecuteCommand(Connection, command);
        ClientState.Session.ApplyCommandReply(command, firstReply);
        FtpReplyCoordinationResult firstCoordination =
            replies.ReceiveControlReply(firstReply);
        string? uniqueName = commandKind == FtpCommandKind.Stou ?
            ExtractUniqueName(firstReply.Text) : null;
        if (!FtpReplyCoordinator.IsTransferPreliminaryReply(firstReply.Code))
        {
            strategy.AbortDataConnection();
            ClientState.DataStrategy = null;
            FtpCommandResult terminal =
                FtpClientListingDataTransfer.RequireCompletedCommand(
                    firstCoordination, "initial upload");
            commands.Add(terminal);
            CompleteControlResult(terminal);
            FtpTransferResult rejected = CreateTerminalResult(
                request, source, commands, terminal, null, uniqueName);
            CleanupAtomicUpload(request, atomicPath, commands);
            rejected.Commands = commands.ToArray();
            return rejected;
        }

        FtpClearDataStream transfer = SendData(
            strategy, source, request, commandKind, monitor);
        FtpTransferPumpResult? pump = transfer.Result.PumpResult;
        FtpReply finalReply = ReadTerminalReply(request.Options);
        FtpReplyCoordinationResult finalCoordination =
            replies.ReceiveControlReply(finalReply);
        ClientState.Session.ApplyTransferReply(finalReply);
        transfer.ApplyFinalReply(finalReply);
        ClientState.DataStrategy = null;

        FtpCommandResult completed =
            FtpClientListingDataTransfer.RequireCompletedCommand(
                finalCoordination, "terminal upload");
        commands.Add(completed);
        monitor.AppendKeepAliveCommands(commands);
        CompleteControlResult(completed);
        if (commandKind == FtpCommandKind.Stou && uniqueName == null)
        {
            uniqueName = ExtractUniqueName(finalReply.Text);
        }
        FtpTransferResult result = CreateTerminalResult(
            request, source, commands, completed, pump, uniqueName);
        if (atomicPath == null)
        {
            if (result.Succeeded &&
                request.Options.VerificationMode == FtpVerificationMode.Checksum)
            {
                new FtpClientChecksumOperation(ClientState).VerifyChecksum(
                    request, request.RemotePath, source.Stream, result, commands);
                result.Commands = commands.ToArray();
            }
            return result;
        }
        if (!result.Succeeded)
        {
            CleanupAtomicUpload(request, atomicPath, commands);
            result.Commands = commands.ToArray();
            return result;
        }
        if (!VerifyAtomicUpload(
            request, atomicPath, source.Stream, result, commands))
        {
            CleanupAtomicUpload(request, atomicPath, commands);
            result.Commands = commands.ToArray();
            return result;
        }
        CommitAtomicUpload(request, atomicPath, result, commands);
        if (!result.Succeeded)
        {
            CleanupAtomicUpload(request, atomicPath, commands);
        }
        result.Commands = commands.ToArray();
        return result;
    }

    internal bool VerifyAtomicUpload(
        FtpTransferRequest request,
        string atomicPath,
        Stream? source,
        FtpTransferResult result,
        List<FtpCommandResult> commands)
    {
        FtpVerificationMode mode = request.Options.VerificationMode;
        if (mode == FtpVerificationMode.None)
        {
            return true;
        }
        result.VerificationAttempted = true;
        if (mode == FtpVerificationMode.Checksum)
        {
            return new FtpClientChecksumOperation(ClientState).VerifyChecksum(
                request, atomicPath, source, result, commands);
        }

        bool verifySize = mode == FtpVerificationMode.Size ||
            mode == FtpVerificationMode.SizeAndModifiedTime;
        if (verifySize)
        {
            FtpPathProbeResult size =
                new FtpClientMetadataOperation(ClientState).RetrieveSize(atomicPath);
            AppendCommands(commands, size.Commands);
            if (!size.Succeeded || !size.Exists || size.Entry == null ||
                !size.Entry.HasSize || !result.HasActualSize ||
                size.Entry.Size != result.ActualSize)
            {
                result.Succeeded = false;
                result.FailureMessage =
                    "The temporary atomic upload failed exact size verification.";
                return false;
            }
        }

        bool verifyModified = mode == FtpVerificationMode.ModifiedTime ||
            mode == FtpVerificationMode.SizeAndModifiedTime;
        if (verifyModified)
        {
            if (request.LocalPath == null)
            {
                result.Succeeded = false;
                result.FailureMessage =
                    "Atomic modification-time verification requires a local file path.";
                return false;
            }
            FtpPathProbeResult modification =
                new FtpClientMetadataOperation(ClientState)
                    .RetrieveModification(atomicPath);
            AppendCommands(commands, modification.Commands);
            System.DateTime localModified = File.GetLastWriteTimeUtc(request.LocalPath);
            if (!modification.Succeeded || !modification.Exists ||
                modification.Entry == null ||
                !modification.Entry.Modified.HasValue ||
                modification.Entry.Modified.ValueUtc != localModified)
            {
                result.Succeeded = false;
                result.FailureMessage =
                    "The temporary atomic upload failed modification-time verification.";
                return false;
            }
        }

        result.Verified = true;
        return true;
    }

    internal void CommitAtomicUpload(
        FtpTransferRequest request,
        string atomicPath,
        FtpTransferResult result,
        List<FtpCommandResult> commands)
    {
        FtpCommandResult[] rename =
            new FtpClientFileSystemOperation(ClientState)
                .RenamePath(atomicPath, request.RemotePath);
        AppendCommands(commands, rename);
        if (rename.Length == 2 && rename[1].Succeeded)
        {
            result.Succeeded = true;
            return;
        }

        result.Succeeded = false;
        if (rename.Length == 0)
        {
            result.FailureMessage =
                "The temporary atomic upload could not begin its rename commit.";
            return;
        }
        FtpCommandResult failed = rename[rename.Length - 1];
        result.FailureMessage = FtpClientListingOperation.CreateFailureMessage(
            failed, failed.Command.Verb);
    }

    internal void CleanupAtomicUpload(
        FtpTransferRequest request,
        string? atomicPath,
        List<FtpCommandResult> commands)
    {
        if (atomicPath == null || !request.Options.CleanupPartialOnFailure ||
            ClientState.Session.State.SessionState == FtpSessionState.Disconnected ||
            ClientState.Session.State.SessionState == FtpSessionState.Closed)
        {
            return;
        }
        FtpCommandResult cleanup =
            new FtpClientFileSystemOperation(ClientState).DeleteFile(atomicPath);
        commands.Add(cleanup);
    }

    internal string CreateAtomicPath(string destinationPath)
    {
        string? parent = GetParentPath(destinationPath);
        string leaf = FtpClientMetadataOperation.GetLeafName(destinationPath);
        string token = System.Convert.ToHexString(
            RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        string temporaryName = "." + leaf + ".upload-" + token + ".tmp";
        if (parent == null)
        {
            return temporaryName;
        }
        if (parent == "/")
        {
            return "/" + temporaryName;
        }
        return parent + "/" + temporaryName;
    }

    internal FtpTransferResult? ApplyCollisionPolicy(
        FtpTransferRequest request,
        List<FtpCommandResult> commands)
    {
        if (IsResumeRequested(request))
        {
            return null;
        }
        FtpCollisionPolicy policy = request.Options.CollisionPolicy;
        if (policy != FtpCollisionPolicy.Skip && policy != FtpCollisionPolicy.Fail)
        {
            return null;
        }

        FtpPathProbeResult probe =
            new FtpClientMetadataOperation(ClientState).Probe(request.RemotePath);
        AppendCommands(commands, probe.Commands);
        if (!probe.Succeeded)
        {
            FtpTransferResult unknown = CreateLocalResult(
                request, false,
                probe.FailureMessage == null ?
                    "The remote collision state could not be established." :
                    probe.FailureMessage);
            unknown.Commands = commands.ToArray();
            return unknown;
        }
        if (!probe.Exists)
        {
            return null;
        }

        bool skipped = policy == FtpCollisionPolicy.Skip;
        FtpTransferResult collision = CreateLocalResult(
            request,
            skipped,
            skipped ? null : "The remote upload destination already exists.");
        collision.Commands = commands.ToArray();
        return collision;
    }

    internal FtpCommandResult? CreateParents(
        FtpTransferRequest request,
        List<FtpCommandResult> commands)
    {
        if (!request.Options.CreateParentDirectories ||
            request.Options.CollisionPolicy == FtpCollisionPolicy.Unique)
        {
            return null;
        }
        string? parent = GetParentPath(request.RemotePath);
        if (parent == null || parent == "/")
        {
            return null;
        }

        FtpCommandResult[] results =
            new FtpClientFileSystemOperation(ClientState)
                .CreateParentDirectories(parent);
        AppendCommands(commands, results);
        if (results.Length != 0 && !results[results.Length - 1].Succeeded)
        {
            return results[results.Length - 1];
        }
        return null;
    }

    internal FtpCommandResult? ApplyTransferParameters(
        FtpTransferOptions options,
        List<FtpCommandResult> commands)
    {
        if (options.Mode == FtpTransferMode.Deflate &&
            ClientState.Features.Negotiated &&
            !ClientState.FeatureState.ModeZ)
        {
            throw new FtpConfigurationException(
                "MODE Z was not advertised by the negotiated FTP feature set.",
                null);
        }
        FtpSequenceState state = ClientState.Session.State;
        bool representationChanged = state.TransferType != options.Type ||
            state.TextFormat != options.TextFormat ||
            state.HasLocalByteSize != options.HasLocalByteSize ||
            state.LocalByteSize != options.LocalByteSize;
        bool representationFirst = representationChanged &&
            FtpTransferParameterCodec.RepresentationMustPrecedeStructure(
                options, state);
        if (representationFirst)
        {
            FtpCommandResult type = ExecuteSimple(
                FtpCommandKind.Type,
                FtpTransferParameterCodec.FormatRepresentation(options));
            commands.Add(type);
            if (!type.Succeeded)
            {
                return type;
            }
        }
        if (state.FileStructure != options.Structure)
        {
            FtpCommandResult structure = ExecuteSimple(
                FtpCommandKind.Stru,
                FtpTransferParameterCodec.FormatStructure(options.Structure));
            commands.Add(structure);
            if (!structure.Succeeded)
            {
                return structure;
            }
        }
        if (state.TransferMode != options.Mode)
        {
            FtpCommandResult mode = ExecuteSimple(
                FtpCommandKind.Mode,
                FtpTransferParameterCodec.FormatMode(options.Mode));
            commands.Add(mode);
            if (!mode.Succeeded)
            {
                return mode;
            }
        }
        if (representationChanged && !representationFirst)
        {
            FtpCommandResult type = ExecuteSimple(
                FtpCommandKind.Type,
                FtpTransferParameterCodec.FormatRepresentation(options));
            commands.Add(type);
            if (!type.Succeeded)
            {
                return type;
            }
        }
        return null;
    }

    internal FtpClearDataStream SendData(
        FtpClientDataConnectionStrategy strategy,
        FtpUploadSource source,
        FtpTransferRequest request,
        FtpCommandKind commandKind,
        FtpClientTransferMonitor monitor)
    {
        if (source.Stream == null)
        {
            throw new FtpConfigurationException(
                "A readable upload source is required.", null);
        }
        FtpClearDataStreamRequest transferRequest =
            CreateDataRequest(
                source.Stream, request, commandKind, source.ResumeOffset,
                monitor);
        if (ClientState.Session.State.DataProtectionLevel ==
            FtpDataProtectionLevel.Private)
        {
            FtpProtectedDataStreamRequest protectedRequest =
                new FtpProtectedDataStreamRequest();
            protectedRequest.Transfer = transferRequest;
            protectedRequest.Role = FtpDataChannelRole.Client;
            protectedRequest.TlsOptions = ClientOptions.Tls;
            protectedRequest.TargetHost = ClientOptions.Endpoint.Host;
            protectedRequest.HandshakeTimeoutMilliseconds =
                ClientOptions.Timeouts.TlsHandshakeTimeoutMilliseconds;
            protectedRequest.CertificateValidator = ClientState.CertificateValidator;
            protectedRequest.RemoteEndpoint = ClientOptions.Endpoint;
            FtpProtectedDataStream protectedTransfer =
                new FtpProtectedDataStream(strategy.Coordinator);
            protectedTransfer.BeginSend(protectedRequest);
            protectedTransfer.ExecuteSendData();
            return protectedTransfer.Transfer;
        }

        FtpClearDataStream clearTransfer =
            new FtpClearDataStream(strategy.Coordinator);
        clearTransfer.BeginSend(transferRequest);
        clearTransfer.ExecuteSendData();
        return clearTransfer;
    }

    internal FtpClearDataStreamRequest CreateDataRequest(
        Stream source,
        FtpTransferRequest request,
        FtpCommandKind commandKind,
        long resumeOffset,
        FtpClientTransferMonitor monitor)
    {
        FtpClearDataStreamRequest transferRequest =
            new FtpClearDataStreamRequest();
        transferRequest.CommandKind = commandKind;
        transferRequest.LocalStream = source;
        transferRequest.Options = request.Options;
        transferRequest.Transform = new FtpTransferPipelineTransform(
            request.Options, true, null, resumeOffset);
        transferRequest.Direction = FtpTransferDirection.Upload;
        transferRequest.Path = request.LocalPath == null ?
            request.RemotePath : request.LocalPath;
        transferRequest.HasExpectedBytes = request.HasExpectedSize;
        transferRequest.ExpectedBytes = request.ExpectedSize;
        transferRequest.InitialLogicalBytes = resumeOffset;
        transferRequest.ProgressCallback = monitor.ReportProgress;
        transferRequest.ConnectionTimeoutMilliseconds =
            request.Options.Timeouts.DataConnectionTimeoutMilliseconds;
        transferRequest.TransferTimeoutMilliseconds =
            request.Options.Timeouts.IdleTimeoutMilliseconds;
        transferRequest.LeaveLocalStreamOpen = true;
        return transferRequest;
    }

    internal FtpTransferResult CreateTerminalResult(
        FtpTransferRequest request,
        FtpUploadSource source,
        List<FtpCommandResult> commands,
        FtpCommandResult terminal,
        FtpTransferPumpResult? pump,
        string? uniqueName)
    {
        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Commands = commands.ToArray();
        result.Succeeded = terminal.Succeeded;
        result.VerificationMode = request.Options.VerificationMode;
        result.UniqueRemoteName = uniqueName;
        if (pump != null)
        {
            result.Progress = pump.Progress;
            result.HasActualSize = true;
            result.ActualSize = source.ResumeOffset + pump.SourceBytes;
        }
        if (!terminal.Succeeded)
        {
            result.FailureMessage = FtpClientListingOperation.CreateFailureMessage(
                terminal, terminal.Command.Verb);
        }
        if (ObservedRestartMarker != null)
        {
            result.RestartMarker = ObservedRestartMarker;
        }
        else if (source.AcceptedRestartMarker != null)
        {
            result.RestartMarker = source.AcceptedRestartMarker;
        }
        else if (source.RestartAccepted)
        {
            result.RestartMarker =
                FtpArgumentCodec.FormatRestartMarker(source.ResumeOffset);
        }
        return result;
    }

    internal FtpUploadResumeValidation ValidateRemoteResume(
        FtpTransferRequest request,
        FtpUploadSource source,
        List<FtpCommandResult> commands)
    {
        FtpUploadResumeValidation validation = new FtpUploadResumeValidation();
        if (source.Stream == null || !source.Stream.CanSeek)
        {
            validation.FailureMessage =
                "A resumed upload source must be seekable.";
            return validation;
        }
        if (ClientState.Features.Negotiated &&
            !ClientState.FeatureState.FileSize)
        {
            validation.FailureMessage =
                "The negotiated FTP features do not provide SIZE for safe upload resume.";
            return validation;
        }

        FtpCommandResult size = ExecuteSimple(FtpCommandKind.Size, request.RemotePath);
        commands.Add(size);
        if (!size.Succeeded || size.FinalReply == null)
        {
            validation.FailureMessage =
                "The remote upload length could not be established with SIZE.";
            return validation;
        }

        long remoteLength = FtpReplyExtractor.ExtractSize(size.FinalReply);
        if (remoteLength > source.Length ||
            (request.HasRestartOffset && request.RestartOffset != remoteLength) ||
            (request.HasExpectedSize && request.ExpectedSize != source.Length))
        {
            validation.FailureMessage =
                "The remote upload length is inconsistent with the local source or requested offset.";
            return validation;
        }

        FtpFaultIsolationResult seek = FtpFaultIsolationRunner.RunOperation(() =>
        {
            source.Stream.Seek(remoteLength, SeekOrigin.Begin);
            return null;
        });
        if (!seek.Succeeded)
        {
            validation.FailureMessage =
                "The local upload source could not seek to the remote length.";
            return validation;
        }

        source.ResumeOffset = remoteLength;
        source.Resuming = remoteLength != 0;
        source.UseRest = source.Resuming &&
            (!ClientState.Features.Negotiated ||
                ClientState.FeatureState.RestStream);
        validation.Safe = true;
        validation.RemoteLength = remoteLength;
        return validation;
    }

    internal void ResetSourceForFullUpload(FtpUploadSource source)
    {
        if (source.Stream == null || !source.Stream.CanSeek)
        {
            throw new FtpConfigurationException(
                "A fallback upload source must be seekable.", null);
        }
        source.Stream.Seek(0, SeekOrigin.Begin);
        source.Resuming = false;
        source.UseRest = false;
        source.RestartAccepted = false;
        source.ResumeOffset = 0;
    }

    internal FtpTransferResult CreateResumeFailureResult(
        FtpTransferRequest request,
        List<FtpCommandResult> commands,
        string? failureMessage)
    {
        FtpTransferResult result = CreateLocalResult(
            request,
            false,
            failureMessage == null ?
                "The FTP upload could not be resumed safely." : failureMessage);
        result.Commands = commands.ToArray();
        return result;
    }

    internal bool IsResumeRequested(FtpTransferRequest request)
    {
        return request.HasRestartOffset ||
            request.Options.ResumePolicy != FtpResumePolicy.Disabled ||
            request.Options.CollisionPolicy == FtpCollisionPolicy.Resume;
    }

    internal bool IsStreamResumeRequested(FtpTransferRequest request)
    {
        return IsResumeRequested(request) &&
            (request.Options.Mode == FtpTransferMode.Stream ||
             request.Options.Mode == FtpTransferMode.Deflate);
    }

    internal bool IsMarkerResumeRequested(FtpTransferRequest request)
    {
        return IsResumeRequested(request) &&
            FtpClientMarkerRestart.IsMarkerMode(request.Options);
    }

    internal long ResolveMarkerOffset(FtpTransferRequest request)
    {
        FtpRestartCheckpoint checkpoint =
            FtpClientMarkerRestart.RequireLocalCheckpoint(ClientState);
        if (request.HasRestartOffset &&
            request.RestartOffset != checkpoint.LogicalByteOffset)
        {
            throw new FtpConfigurationException(
                "The requested restart offset does not match the local marker checkpoint.",
                null);
        }
        FtpClientMarkerRestart.RequireServerMarker(ClientState);
        return checkpoint.LogicalByteOffset;
    }

    internal bool MustResume(FtpTransferRequest request)
    {
        return request.HasRestartOffset ||
            request.Options.ResumePolicy == FtpResumePolicy.Required ||
            request.Options.CollisionPolicy == FtpCollisionPolicy.Resume;
    }

    internal FtpCommandKind ResolveResumeStorageCommand(FtpUploadSource source)
    {
        return source.UseRest ? FtpCommandKind.Stor : FtpCommandKind.Appe;
    }

    internal FtpTransferResult CreateLocalResult(
        FtpTransferRequest request,
        bool succeeded,
        string? failureMessage)
    {
        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Succeeded = succeeded;
        result.FailureMessage = failureMessage;
        result.VerificationMode = request.Options.VerificationMode;
        result.Progress.Direction = FtpTransferDirection.Upload;
        result.Progress.Path = request.RemotePath;
        result.Progress.Completed = true;
        return result;
    }

    internal FtpUploadSource OpenSource(FtpTransferRequest request)
    {
        FtpUploadSource source = new FtpUploadSource();
        if (request.Content != null)
        {
            if (!request.Content.CanRead)
            {
                throw new FtpConfigurationException(
                    "An upload content stream must be readable.", null);
            }
            source.Stream = request.Content;
            source.CloseStream = !request.LeaveContentOpen;
            if (IsResumeRequested(request))
            {
                if (!request.Content.CanSeek)
                {
                    throw new FtpConfigurationException(
                        "A resumed upload content stream must be seekable.", null);
                }
                source.Length = request.Content.Length;
                if (IsMarkerResumeRequested(request))
                {
                    long markerOffset = ResolveMarkerOffset(request);
                    if (source.Length < markerOffset)
                    {
                        throw new FtpConfigurationException(
                            "A marker-resumed upload stream is shorter than its checkpoint.",
                            null);
                    }
                    request.Content.Seek(markerOffset, SeekOrigin.Begin);
                    source.MarkerResuming = true;
                    source.ResumeOffset = markerOffset;
                }
            }
            return source;
        }
        if (request.LocalPath == null)
        {
            throw new FtpConfigurationException(
                "A local upload path is required.", null);
        }

        FtpFaultIsolationResult opened = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return new FileStream(
                request.LocalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        });
        source.Stream = opened.Value as Stream;
        source.CloseStream = true;
        if (!opened.Succeeded || source.Stream == null)
        {
            throw new FtpTransferException(
                "The local upload source could not be opened.", opened.Failure);
        }
        if (IsResumeRequested(request))
        {
            source.Length = source.Stream.Length;
            if (IsMarkerResumeRequested(request))
            {
                long markerOffset = ResolveMarkerOffset(request);
                if (source.Length < markerOffset)
                {
                    throw new FtpConfigurationException(
                        "A marker-resumed local file is shorter than its checkpoint.",
                        null);
                }
                source.Stream.Seek(markerOffset, SeekOrigin.Begin);
                source.MarkerResuming = true;
                source.ResumeOffset = markerOffset;
            }
        }
        return source;
    }

    internal FtpFaultIsolationResult CloseSource(FtpUploadSource source)
    {
        if (!source.CloseStream || source.Stream == null)
        {
            FtpFaultIsolationResult unchanged = new FtpFaultIsolationResult();
            unchanged.Succeeded = true;
            return unchanged;
        }
        return new FtpResourceLifetime(source.Stream).DisposeResource();
    }

    internal FtpCommandResult ExecuteSimple(
        FtpCommandKind kind,
        string argument)
    {
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(kind, argument));
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        return result;
    }

    internal FtpReply ReadTerminalReply(FtpTransferOptions options)
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            options.Timeouts.ControlReadTimeoutMilliseconds);
        FtpReply reply = FtpControlReplyReader.ReadReply(
            Connection.Stream, Connection.Socket, deadline,
            options.Timeouts.CancellationToken, ClientOptions.StrictReplyParsing);
        while (reply.Code == FtpReplyCodes.RestartMarker)
        {
            ObservedRestartMarker =
                FtpClientMarkerRestart.RecordMarkerReply(ClientState, reply);
            reply = FtpControlReplyReader.ReadReply(
                Connection.Stream, Connection.Socket, deadline,
                options.Timeouts.CancellationToken, ClientOptions.StrictReplyParsing);
        }
        return reply;
    }

    internal void CompleteControlResult(FtpCommandResult result)
    {
        if (result.ConnectionClosed)
        {
            ClientState.CloseSession();
        }
        else
        {
            ClientState.UpdateReconnectState();
        }
    }

    internal static FtpCommandKind ResolveStorageCommand(
        FtpCollisionPolicy policy)
    {
        if (policy == FtpCollisionPolicy.Append)
        {
            return FtpCommandKind.Appe;
        }
        if (policy == FtpCollisionPolicy.Unique)
        {
            return FtpCommandKind.Stou;
        }
        return FtpCommandKind.Stor;
    }

    internal static string? GetParentPath(string path)
    {
        int separator = path.LastIndexOf('/');
        if (separator < 0)
        {
            return null;
        }
        if (separator == 0)
        {
            return "/";
        }
        return path.Substring(0, separator);
    }

    internal static string? ExtractUniqueName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        string value = text.Trim();
        int colon = value.LastIndexOf(':');
        if (colon >= 0 && colon + 1 < value.Length)
        {
            value = value.Substring(colon + 1).Trim();
        }
        else
        {
            int separator = value.LastIndexOf(' ');
            if (separator >= 0 && separator + 1 < value.Length)
            {
                value = value.Substring(separator + 1);
            }
        }
        value = value.Trim('"', '\'', '(', ')');
        return value.Length == 0 ? null : value;
    }

    internal static void AppendCommands(
        List<FtpCommandResult> target,
        FtpCommandResult[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index++;
        }
    }
}

internal class FtpUploadSource
{
    internal Stream? Stream;
    internal bool CloseStream;
    internal long Length;
    internal bool Resuming;
    internal long ResumeOffset;
    internal bool UseRest;
    internal bool RestartAccepted;
    internal bool MarkerResuming;
    internal string? AcceptedRestartMarker;

    internal FtpUploadSource()
    {
        Stream = null;
        CloseStream = false;
        Length = 0;
        Resuming = false;
        ResumeOffset = 0;
        UseRest = false;
        RestartAccepted = false;
        MarkerResuming = false;
        AcceptedRestartMarker = null;
    }
}

internal class FtpUploadResumeValidation
{
    internal bool Safe;
    internal long RemoteLength;
    internal string? FailureMessage;

    internal FtpUploadResumeValidation()
    {
        Safe = false;
        RemoteLength = 0;
        FailureMessage = null;
    }
}
