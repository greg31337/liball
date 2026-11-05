using System;
using System.Collections.Generic;
using System.IO;

namespace ftp;

internal class FtpClientDownloadOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions ClientOptions;
    internal FtpControlConnection Connection;
    internal string? ObservedRestartMarker;

    internal FtpClientDownloadOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for downloads.", null);
        }
        ClientState = clientState;
        ClientOptions = clientState.Options;
        Connection = clientState.Connection;
        ObservedRestartMarker = null;
    }

    internal FtpTransferResult Download(FtpTransferRequest? request)
    {
        FtpTransferValidator.ValidateTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException("A transfer request is required.", null);
        }
        if (request.Direction != FtpTransferDirection.Download)
        {
            throw new FtpConfigurationException(
                "The RETR operation requires download direction.", null);
        }
        bool streamFile = request.Options.Mode == FtpTransferMode.Stream &&
            request.Options.Structure == FtpFileStructure.File;
        bool deflateStructure =
            request.Options.Mode == FtpTransferMode.Deflate;
        bool markerMode = FtpClientMarkerRestart.IsMarkerMode(request.Options);
        if (!streamFile && !deflateStructure && !markerMode)
        {
            throw new FtpConfigurationException(
                "RETR requires stream/file, MODE Z, or block/compressed transfer framing.",
                null);
        }
        if (request.Options.Mode == FtpTransferMode.Stream &&
            IsResumeRequested(request) &&
            request.Options.Type != FtpTransferType.Image)
        {
            throw new FtpConfigurationException(
                "REST STREAM download resume requires image representation.", null);
        }

        ClientState.RequireOpenSession("Transfer");
        FtpDownloadDestination destination = OpenDestination(request);
        if (destination.CollisionResult != null)
        {
            return destination.CollisionResult;
        }

        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ExecuteDownload(request, destination);
        });
        FtpFaultIsolationResult destinationCleanup = CloseDestination(destination);
        if (!operation.Succeeded || !destinationCleanup.Succeeded)
        {
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                ClientState.CloseSession();
                return null;
            });
            CleanupPartial(request, destination);
            if (!operation.Succeeded)
            {
                if (FtpCancellation.IsCancellationRequested(
                    request.Options.Timeouts.CancellationToken))
                {
                    throw new OperationCanceledException(
                        "The FTP download was canceled.", operation.Failure,
                        request.Options.Timeouts.CancellationToken);
                }
                throw new FtpTransferException(
                    "The FTP download did not complete.", operation.Failure);
            }
            throw new FtpTransferException(
                "The local download destination could not be closed.",
                destinationCleanup.Failure);
        }

        FtpTransferResult? completed = operation.Value as FtpTransferResult;
        if (completed == null)
        {
            CleanupPartial(request, destination);
            throw new FtpTransferException(
                "The FTP download did not produce a result.", null);
        }
        if (!completed.Succeeded)
        {
            CleanupPartial(request, destination);
        }
        return completed;
    }

    internal FtpTransferResult ExecuteDownload(
        FtpTransferRequest request,
        FtpDownloadDestination destination)
    {
        if (destination.Stream == null)
        {
            throw new FtpConfigurationException(
                "A writable download destination is required.", null);
        }

        List<FtpCommandResult> commands = new List<FtpCommandResult>();
        FtpClientTransferMonitor monitor = new FtpClientTransferMonitor(
            ClientState, request.Observer, request.Options);
        FtpCommandResult? parameterFailure =
            ApplyTransferParameters(request.Options, commands);
        if (parameterFailure != null)
        {
            return CreateTerminalResult(
                request, destination, commands, parameterFailure, null);
        }

        if (destination.Resuming)
        {
            FtpDownloadResumeValidation validation =
                ValidateRemoteResume(request, destination.ResumeOffset, commands);
            if (!validation.Safe)
            {
                if (MustResume(request))
                {
                    return CreateResumeFailureResult(
                        request, destination, commands, validation.FailureMessage);
                }
                ResetDestinationForFullDownload(destination);
            }
        }

        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommand command = executor.CreateSimpleCommand(
            FtpCommandKind.Retr, request.RemotePath);
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
                return CreateTerminalResult(
                    request, destination, commands, pret, null);
            }
        }

        FtpClientDataConnectionStrategy strategy =
            new FtpClientDataConnectionStrategy(ClientState);
        strategy.PrepareDataConnection();
        ClientState.DataStrategy = strategy;

        if (destination.Resuming || destination.MarkerResuming)
        {
            string restartArgument = destination.MarkerResuming ?
                FtpClientMarkerRestart.RequireServerMarker(ClientState) :
                FtpArgumentCodec.FormatRestartMarker(destination.ResumeOffset);
            FtpCommandResult restart = ExecuteSimple(
                FtpCommandKind.Rest, restartArgument);
            commands.Add(restart);
            if (restart.FinalReply == null ||
                restart.FinalReply.Code != FtpReplyCodes.RequestedFileActionPending)
            {
                strategy.AbortDataConnection();
                ClientState.DataStrategy = null;
                return CreateResumeFailureResult(
                    request, destination, commands,
                    "The FTP server did not accept the restart marker.");
            }
            FtpReplyExtractor.ExtractRestartReadiness(restart.FinalReply);
            destination.AcceptedRestartMarker = restartArgument;
            if (destination.MarkerResuming)
            {
                ClientState.LatestRestartMarker = restartArgument;
            }
        }

        FtpReplyCoordinator replies = new FtpReplyCoordinator();
        replies.BeginDataCommand(command);
        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(ClientOptions);
        FtpReply firstReply = exchange.ExecuteCommand(Connection, command);
        ClientState.Session.ApplyCommandReply(command, firstReply);
        FtpReplyCoordinationResult firstCoordination =
            replies.ReceiveControlReply(firstReply);
        if (!FtpReplyCoordinator.IsTransferPreliminaryReply(firstReply.Code))
        {
            strategy.AbortDataConnection();
            ClientState.DataStrategy = null;
            FtpCommandResult terminal =
                FtpClientListingDataTransfer.RequireCompletedCommand(
                    firstCoordination, "initial download");
            commands.Add(terminal);
            CompleteControlResult(terminal);
            return CreateTerminalResult(
                request, destination, commands, terminal, null);
        }

        FtpClearDataStream transfer = ReceiveData(
            strategy, destination, request, monitor);
        FtpTransferPumpResult? pump = transfer.Result.PumpResult;
        FtpReply finalReply = ReadTerminalReply(request.Options);
        FtpReplyCoordinationResult finalCoordination =
            replies.ReceiveControlReply(finalReply);
        ClientState.Session.ApplyTransferReply(finalReply);
        transfer.ApplyFinalReply(finalReply);
        ClientState.DataStrategy = null;

        FtpCommandResult completed =
            FtpClientListingDataTransfer.RequireCompletedCommand(
                finalCoordination, "terminal download");
        commands.Add(completed);
        monitor.AppendKeepAliveCommands(commands);
        CompleteControlResult(completed);
        FtpTransferResult result = CreateTerminalResult(
            request, destination, commands, completed, pump);
        if (result.Succeeded &&
            request.Options.VerificationMode == FtpVerificationMode.Checksum)
        {
            new FtpClientChecksumOperation(ClientState).VerifyChecksum(
                request, request.RemotePath, destination.Stream, result, commands);
            result.Commands = commands.ToArray();
        }
        return result;
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

    internal FtpClearDataStream ReceiveData(
        FtpClientDataConnectionStrategy strategy,
        FtpDownloadDestination destination,
        FtpTransferRequest request,
        FtpClientTransferMonitor monitor)
    {
        if (destination.Stream == null)
        {
            throw new FtpConfigurationException(
                "A writable download destination is required.", null);
        }
        FtpClearDataStreamRequest transferRequest =
            CreateDataRequest(
                destination.Stream, request, destination.ResumeOffset,
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

    internal FtpClearDataStreamRequest CreateDataRequest(
        Stream destination,
        FtpTransferRequest request,
        long resumeOffset,
        FtpClientTransferMonitor monitor)
    {
        FtpClearDataStreamRequest transferRequest =
            new FtpClearDataStreamRequest();
        transferRequest.CommandKind = FtpCommandKind.Retr;
        transferRequest.LocalStream = destination;
        transferRequest.Options = request.Options;
        transferRequest.Transform = new FtpTransferPipelineTransform(
            request.Options, false, null, resumeOffset);
        transferRequest.Direction = FtpTransferDirection.Download;
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
        FtpDownloadDestination destination,
        List<FtpCommandResult> commands,
        FtpCommandResult terminal,
        FtpTransferPumpResult? pump)
    {
        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Commands = commands.ToArray();
        result.Succeeded = terminal.Succeeded;
        result.VerificationMode = request.Options.VerificationMode;
        if (pump != null)
        {
            result.Progress = pump.Progress;
            result.HasActualSize = true;
            result.ActualSize = destination.ResumeOffset + pump.DestinationBytes;
        }
        if (!terminal.Succeeded)
        {
            result.FailureMessage = FtpClientListingOperation.CreateFailureMessage(
                terminal, terminal.Command.Verb);
        }
        if (destination.ActualPath != null)
        {
            result.Progress.Path = destination.ActualPath;
        }
        if (destination.Resuming)
        {
            result.RestartMarker =
                FtpArgumentCodec.FormatRestartMarker(destination.ResumeOffset);
        }
        else if (ObservedRestartMarker != null)
        {
            result.RestartMarker = ObservedRestartMarker;
        }
        else if (destination.AcceptedRestartMarker != null)
        {
            result.RestartMarker = destination.AcceptedRestartMarker;
        }
        return result;
    }

    internal FtpDownloadDestination OpenDestination(FtpTransferRequest request)
    {
        FtpDownloadDestination destination = new FtpDownloadDestination();
        if (request.Content != null)
        {
            if (!request.Content.CanWrite)
            {
                throw new FtpConfigurationException(
                    "A download content stream must be writable.", null);
            }
            destination.Stream = request.Content;
            destination.CloseStream = !request.LeaveContentOpen;
            if (IsResumeRequested(request))
            {
                if (!request.Content.CanSeek)
                {
                    throw new FtpConfigurationException(
                        "A resumed download content stream must be seekable.", null);
                }
                long contentLength = request.Content.Length;
                if (IsMarkerResumeRequested(request))
                {
                    long markerOffset = ResolveMarkerOffset(request);
                    if (contentLength < markerOffset)
                    {
                        throw new FtpConfigurationException(
                            "A marker-resumed download stream is shorter than its local checkpoint.",
                            null);
                    }
                    request.Content.SetLength(markerOffset);
                    request.Content.Seek(markerOffset, SeekOrigin.Begin);
                    destination.MarkerResuming = true;
                    destination.ResumeOffset = markerOffset;
                    destination.OriginalLength = contentLength;
                    return destination;
                }
                long restartOffset = request.HasRestartOffset ?
                    request.RestartOffset : contentLength;
                if (contentLength != restartOffset)
                {
                    throw new FtpConfigurationException(
                        "A resumed download stream length must equal its restart offset.",
                        null);
                }
                request.Content.Seek(restartOffset, SeekOrigin.Begin);
                destination.Resuming = true;
                destination.ResumeOffset = restartOffset;
                destination.OriginalLength = contentLength;
            }
            return destination;
        }
        if (request.LocalPath == null)
        {
            throw new FtpConfigurationException(
                "A local download path is required.", null);
        }

        string path = request.LocalPath;
        bool exists = File.Exists(path);
        FtpCollisionPolicy policy = request.Options.CollisionPolicy;
        bool resumeRequested = IsResumeRequested(request);
        if (resumeRequested && !exists && MustResume(request))
        {
            throw new FtpConfigurationException(
                "A required resumed download needs an existing local file.", null);
        }
        if (exists && !resumeRequested && policy == FtpCollisionPolicy.Skip)
        {
            destination.CollisionResult = CreateCollisionResult(request, true, null);
            return destination;
        }
        if (exists && !resumeRequested && policy == FtpCollisionPolicy.Fail)
        {
            destination.CollisionResult = CreateCollisionResult(
                request, false, "The local download destination already exists.");
            return destination;
        }

        FileMode mode = FileMode.Create;
        if (resumeRequested && exists)
        {
            long localLength = new FileInfo(path).Length;
            if (IsMarkerResumeRequested(request))
            {
                long markerOffset = ResolveMarkerOffset(request);
                if (localLength < markerOffset)
                {
                    throw new FtpConfigurationException(
                        "A marker-resumed local file is shorter than its checkpoint.",
                        null);
                }
                mode = FileMode.Open;
                destination.MarkerResuming = true;
                destination.ResumeOffset = markerOffset;
                destination.RestoreLengthOnFailure = true;
                destination.OriginalLength = localLength;
            }
            else
            {
            long restartOffset = request.HasRestartOffset ?
                request.RestartOffset : localLength;
            if (localLength != restartOffset)
            {
                throw new FtpConfigurationException(
                    "A resumed local file length must equal its restart offset.", null);
            }
            mode = FileMode.Open;
            destination.Resuming = true;
            destination.ResumeOffset = restartOffset;
            destination.RestoreLengthOnFailure = true;
            destination.OriginalLength = localLength;
            }
        }
        else if (policy == FtpCollisionPolicy.Append)
        {
            mode = FileMode.OpenOrCreate;
            destination.RestoreLengthOnFailure = exists;
        }
        else if (policy == FtpCollisionPolicy.Unique)
        {
            if (exists)
            {
                path = FindUniquePath(path);
            }
            mode = FileMode.CreateNew;
        }
        else if (policy == FtpCollisionPolicy.Fail ||
            policy == FtpCollisionPolicy.Skip)
        {
            mode = FileMode.CreateNew;
        }
        destination.DeleteOnFailure = !destination.RestoreLengthOnFailure;
        destination.ActualPath = path;
        if (destination.RestoreLengthOnFailure)
        {
            destination.OriginalLength = new FileInfo(path).Length;
        }

        FtpFaultIsolationResult opened = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return new FileStream(path, mode, FileAccess.ReadWrite, FileShare.None);
        });
        destination.Stream = opened.Value as Stream;
        destination.CloseStream = true;
        if (!opened.Succeeded || destination.Stream == null)
        {
            throw new FtpTransferException(
                "The local download destination could not be opened.", opened.Failure);
        }
        if (destination.MarkerResuming)
        {
            destination.Stream.SetLength(destination.ResumeOffset);
            destination.Stream.Seek(destination.ResumeOffset, SeekOrigin.Begin);
        }
        else if (destination.Resuming)
        {
            destination.Stream.Seek(destination.ResumeOffset, SeekOrigin.Begin);
        }
        else if (policy == FtpCollisionPolicy.Append)
        {
            destination.Stream.Seek(0, SeekOrigin.End);
        }
        return destination;
    }

    internal FtpDownloadResumeValidation ValidateRemoteResume(
        FtpTransferRequest request,
        long restartOffset,
        List<FtpCommandResult> commands)
    {
        FtpDownloadResumeValidation validation = new FtpDownloadResumeValidation();
        if (ClientState.Features.Negotiated &&
            (!ClientState.FeatureState.RestStream ||
                !ClientState.FeatureState.MlstAdvertised ||
                !ClientState.FeatureState.FileSize ||
                !ClientState.FeatureState.ModificationTimeQuery))
        {
            validation.FailureMessage =
                "The negotiated FTP features do not provide REST STREAM, MLST, SIZE, and MDTM resume guards.";
            return validation;
        }

        FtpCommandResult firstMlst = ExecuteSimple(
            FtpCommandKind.Mlst, request.RemotePath);
        commands.Add(firstMlst);
        if (!firstMlst.Succeeded || firstMlst.FinalReply == null)
        {
            validation.FailureMessage =
                "The remote file could not be identified with MLST before resume.";
            return validation;
        }
        FtpEntry firstEntry = FtpMlsxCodec.ParseControlReply(firstMlst.FinalReply);
        if (!HasResumeFacts(firstEntry))
        {
            validation.FailureMessage =
                "The remote MLST result lacks file size, modification time, or a unique identifier.";
            return validation;
        }

        FtpCommandResult size = ExecuteSimple(FtpCommandKind.Size, request.RemotePath);
        commands.Add(size);
        if (!size.Succeeded || size.FinalReply == null)
        {
            validation.FailureMessage =
                "The remote file size could not be validated before resume.";
            return validation;
        }
        long remoteSize = FtpReplyExtractor.ExtractSize(size.FinalReply);
        if (remoteSize != firstEntry.Size || remoteSize < restartOffset ||
            (request.HasExpectedSize && request.ExpectedSize != remoteSize))
        {
            validation.FailureMessage =
                "The remote file size is inconsistent with the requested resume offset.";
            return validation;
        }

        FtpCommandResult modification = ExecuteSimple(
            FtpCommandKind.Mdtm, request.RemotePath);
        commands.Add(modification);
        if (!modification.Succeeded || modification.FinalReply == null)
        {
            validation.FailureMessage =
                "The remote modification time could not be validated before resume.";
            return validation;
        }
        FtpTimestamp remoteModification =
            FtpReplyExtractor.ExtractModificationTime(modification.FinalReply);
        if (!SameTimestamp(firstEntry.Modified, remoteModification))
        {
            validation.FailureMessage =
                "The remote modification time changed during resume validation.";
            return validation;
        }

        FtpCommandResult secondMlst = ExecuteSimple(
            FtpCommandKind.Mlst, request.RemotePath);
        commands.Add(secondMlst);
        if (!secondMlst.Succeeded || secondMlst.FinalReply == null)
        {
            validation.FailureMessage =
                "The remote file identity could not be confirmed before resume.";
            return validation;
        }
        FtpEntry secondEntry = FtpMlsxCodec.ParseControlReply(secondMlst.FinalReply);
        if (!HasResumeFacts(secondEntry) ||
            firstEntry.Size != secondEntry.Size ||
            !SameTimestamp(firstEntry.Modified, secondEntry.Modified) ||
            !string.Equals(firstEntry.Unique, secondEntry.Unique,
                StringComparison.Ordinal))
        {
            validation.FailureMessage =
                "The remote file identity changed during resume validation.";
            return validation;
        }

        validation.Safe = true;
        validation.RemoteSize = remoteSize;
        return validation;
    }

    internal void ResetDestinationForFullDownload(
        FtpDownloadDestination destination)
    {
        if (destination.Stream == null || !destination.Stream.CanSeek)
        {
            throw new FtpConfigurationException(
                "A fallback download destination must be seekable.", null);
        }
        destination.Stream.SetLength(0);
        destination.Stream.Seek(0, SeekOrigin.Begin);
        destination.Resuming = false;
        destination.ResumeOffset = 0;
        destination.RestoreLengthOnFailure = false;
        destination.DeleteOnFailure = destination.ActualPath != null;
        destination.OriginalLength = 0;
    }

    internal FtpTransferResult CreateResumeFailureResult(
        FtpTransferRequest request,
        FtpDownloadDestination destination,
        List<FtpCommandResult> commands,
        string? failureMessage)
    {
        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Commands = commands.ToArray();
        result.Succeeded = false;
        result.VerificationMode = request.Options.VerificationMode;
        result.FailureMessage = failureMessage == null ?
            "The FTP download could not be resumed safely." : failureMessage;
        result.Progress.Direction = FtpTransferDirection.Download;
        result.Progress.Path = destination.ActualPath == null ?
            request.RemotePath : destination.ActualPath;
        result.Progress.Completed = true;
        return result;
    }

    internal bool IsResumeRequested(FtpTransferRequest request)
    {
        return request.HasRestartOffset ||
            request.Options.ResumePolicy != FtpResumePolicy.Disabled ||
            request.Options.CollisionPolicy == FtpCollisionPolicy.Resume;
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

    internal bool HasResumeFacts(FtpEntry entry)
    {
        return entry.Type == FtpEntryType.File && entry.HasSize &&
            entry.Modified.HasValue && !string.IsNullOrEmpty(entry.Unique);
    }

    internal bool SameTimestamp(FtpTimestamp first, FtpTimestamp second)
    {
        return first.HasValue && second.HasValue &&
            first.ValueUtc == second.ValueUtc &&
            string.Equals(first.FractionalSeconds, second.FractionalSeconds,
                StringComparison.Ordinal) &&
            first.LeapSecond == second.LeapSecond;
    }

    internal FtpTransferResult CreateCollisionResult(
        FtpTransferRequest request,
        bool succeeded,
        string? failureMessage)
    {
        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Succeeded = succeeded;
        result.FailureMessage = failureMessage;
        result.VerificationMode = request.Options.VerificationMode;
        result.Progress.Direction = FtpTransferDirection.Download;
        result.Progress.Path = request.LocalPath;
        result.Progress.Completed = true;
        return result;
    }

    internal FtpFaultIsolationResult CloseDestination(
        FtpDownloadDestination destination)
    {
        if (!destination.CloseStream || destination.Stream == null)
        {
            FtpFaultIsolationResult unchanged = new FtpFaultIsolationResult();
            unchanged.Succeeded = true;
            return unchanged;
        }
        return new FtpResourceLifetime(destination.Stream).DisposeResource();
    }

    internal void CleanupPartial(
        FtpTransferRequest request,
        FtpDownloadDestination destination)
    {
        if (!request.Options.CleanupPartialOnFailure ||
            destination.ActualPath == null)
        {
            return;
        }
        FtpFaultIsolationResult cleanup = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (destination.RestoreLengthOnFailure)
            {
                using (FileStream file = new FileStream(
                    destination.ActualPath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None))
                {
                    file.SetLength(destination.OriginalLength);
                }
            }
            else if (destination.DeleteOnFailure &&
                File.Exists(destination.ActualPath))
            {
                File.Delete(destination.ActualPath);
            }
            return null;
        });
        if (!cleanup.Succeeded)
        {
            throw new FtpTransferException(
                "The partial local download could not be cleaned up.",
                cleanup.Failure);
        }
    }

    internal string FindUniquePath(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int suffix = 1;
        while (suffix <= 10000)
        {
            string candidateName = name + " (" + suffix.ToString() + ")" + extension;
            string candidate = string.IsNullOrEmpty(directory) ?
                candidateName : Path.Combine(directory, candidateName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }
            suffix++;
        }
        throw new FtpTransferException(
            "A unique local download destination could not be selected.", null);
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
}

internal class FtpDownloadDestination
{
    internal Stream? Stream;
    internal string? ActualPath;
    internal bool CloseStream;
    internal bool DeleteOnFailure;
    internal bool RestoreLengthOnFailure;
    internal long OriginalLength;
    internal bool Resuming;
    internal long ResumeOffset;
    internal bool MarkerResuming;
    internal string? AcceptedRestartMarker;
    internal FtpTransferResult? CollisionResult;

    internal FtpDownloadDestination()
    {
        Stream = null;
        ActualPath = null;
        CloseStream = false;
        DeleteOnFailure = false;
        RestoreLengthOnFailure = false;
        OriginalLength = 0;
        Resuming = false;
        ResumeOffset = 0;
        MarkerResuming = false;
        AcceptedRestartMarker = null;
        CollisionResult = null;
    }
}

internal class FtpDownloadResumeValidation
{
    internal bool Safe;
    internal long RemoteSize;
    internal string? FailureMessage;

    internal FtpDownloadResumeValidation()
    {
        Safe = false;
        RemoteSize = 0;
        FailureMessage = null;
    }
}
