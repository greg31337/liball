using System;
using System.IO;
using System.Threading;

namespace ftp;

internal static class FtpServerFileTransferCommandHandler
{
    internal static FtpCommandResult HandleCommand(
        FtpServerCommandContext context)
    {
        FtpAllocationArgument? pendingAllocation =
            context.Session.PendingAllocation;
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(
                context.Command, context.State.State);
        if (!sequence.Allowed)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                sequence.ReplyCode,
                sequence.Message,
                sequence.ReplyCode == FtpReplyCodes.ServiceUnavailable);
        }
        if (context.Command.Kind != FtpCommandKind.Retr &&
            context.Command.Kind != FtpCommandKind.Stor &&
            context.Command.Kind != FtpCommandKind.Stou &&
            context.Command.Kind != FtpCommandKind.Appe)
        {
            return FtpServerListingCommandHandler.HandleCommand(context);
        }
        context.Session.PendingAllocation = null;
        if (context.FileSystem == null)
        {
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "The FTP file system is unavailable.",
                false);
        }
        if (!context.Session.HasDataConnectionSetup())
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CannotOpenDataConnection,
                "Use PORT, EPRT, PASV, or EPSV before a file transfer.",
                false);
        }
        if (context.Session.HasPendingTransfer())
        {
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedFileActionNotTaken,
                "The previous data transfer is still being released.",
                false);
        }

        FtpTransferOptions options = CreateTransferOptions(context);
        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpTransferValidator.ValidateTransferOptions(options);
                return null;
            });
        if (!validation.Succeeded)
        {
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The selected transfer parameters are not supported.",
                false);
        }

        FtpServerFileTransferState transferState =
            new FtpServerFileTransferState();
        if (!PrepareRequest(context, transferState))
        {
            context.Session.DiscardDataConnectionSetup();
            return CreateFileFailure(context);
        }
        if (!AuthorizeRequest(context, transferState.Request))
        {
            context.Session.DiscardDataConnectionSetup();
            return CreateFileFailure(context);
        }
        if (!context.Connection.TryBeginTransfer())
        {
            context.Session.RecordPolicyEvent(
                "The simultaneous FTP transfer limit was exceeded.");
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedFileActionNotTaken,
                "No server transfer slot is available.",
                false);
        }
        transferState.TransferLimitAcquired = true;

        if (!ApplyRestartState(context, transferState) ||
            !CreateUniquePath(context, transferState))
        {
            CleanupTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return CreateFileFailure(context);
        }
        if (!ApplyResourceLimits(context, transferState))
        {
            CleanupTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return CreateResourceFailure(
                context, transferState.ResourceFailureCode);
        }
        if (!ApplyPendingAllocation(
            context, transferState, pendingAllocation))
        {
            CleanupTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return CreateResourceFailure(
                context,
                FtpReplyCodes.RequestedActionNotTakenInsufficientStorage);
        }
        if (!OpenFile(context, transferState) ||
            !ValidateKnownTransferLength(context, transferState))
        {
            CleanupTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return transferState.ResourceFailureCode == 0 ?
                CreateFileFailure(context) : CreateResourceFailure(
                    context, transferState.ResourceFailureCode);
        }

        CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                context.Session.Settings.Timeouts.CancellationToken);
        options.Timeouts.CancellationToken = cancellation.Token;
        FtpFaultIsolationResult beginning =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return BeginTransfer(context, transferState, options);
            });
        FtpReply? preliminary = beginning.Value as FtpReply;
        if (!beginning.Succeeded || preliminary == null)
        {
            FtpServerSessionContext.CloseResource(cancellation);
            CleanupTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CannotOpenDataConnection,
                "The FTP data connection could not be opened.",
                false);
        }

        context.Session.RegisterPendingTransfer(
            context.Command,
            () => CompletePendingTransfer(context, transferState),
            cancellation);
        FtpCommandResult result = new FtpCommandResult();
        result.Command = context.Command;
        result.PreliminaryReply = preliminary;
        result.FinalReply = preliminary;
        result.Succeeded = true;
        return result;
    }

    internal static FtpTransferOptions CreateTransferOptions(
        FtpServerCommandContext context)
    {
        FtpSequenceState state = context.State.State;
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = state.TransferType;
        options.TextFormat = state.TextFormat;
        options.Structure = state.FileStructure;
        options.Mode = state.TransferMode;
        options.HasLocalByteSize = state.HasLocalByteSize;
        options.LocalByteSize = state.LocalByteSize;
        options.Timeouts = FtpClientOptionsSnapshot.CopyTimeouts(
            context.Session.Settings.Timeouts);
        options.VerificationMode = FtpVerificationMode.None;
        return options;
    }

    internal static bool PrepareRequest(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        FtpCommandKind kind = context.Command.Kind;
        string path;
        if (kind == FtpCommandKind.Stou)
        {
            path = context.State.State.CurrentDirectory;
        }
        else
        {
            string argument = context.Command.Argument == null ?
                "" : context.Command.Argument;
            path = FtpPathCodec.ResolveWorkingPath(
                context.State.State.CurrentDirectory, argument);
        }
        transferState.Request =
            FtpServerFileSystemCommandHandler.CreateRequest(
                context, path, null);
        if (context.Session.Settings.EnableCompatibilityExtensions &&
            kind != FtpCommandKind.Retr)
        {
            transferState.Request.HasUnixMode = true;
            transferState.Request.UnixMode =
                438 & ~context.Session.OptionState.UnixUmask;
        }
        if (kind == FtpCommandKind.Retr)
        {
            transferState.Request.OpenMode = FtpFileOpenMode.Read;
        }
        else if (kind == FtpCommandKind.Appe)
        {
            transferState.Request.OpenMode = FtpFileOpenMode.Append;
        }
        else
        {
            transferState.Request.OpenMode = FtpFileOpenMode.Overwrite;
        }
        return true;
    }

    internal static bool AuthorizeRequest(
        FtpServerCommandContext context,
        FtpFileSystemRequest request)
    {
        FtpAuthorizationOperation operation =
            context.Command.Kind == FtpCommandKind.Retr ?
                FtpAuthorizationOperation.Read :
            context.Command.Kind == FtpCommandKind.Appe ?
                FtpAuthorizationOperation.Append :
                FtpAuthorizationOperation.Write;
        return FtpServerFileSystemCommandHandler.Authorize(
            context, operation, request, null);
    }

    internal static bool CreateUniquePath(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        if (context.Command.Kind != FtpCommandKind.Stou)
        {
            return true;
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return false;
        }
        FtpFileSystemResult unique =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () =>
                {
                    return fileSystem.CreateUniqueEntry(
                        transferState.Request);
                });
        if (!unique.Succeeded || string.IsNullOrEmpty(unique.Path) ||
            FtpConfigurationValidator.HasInvalidText(unique.Path))
        {
            return false;
        }
        string uniquePath = FtpPathCodec.ResolveWorkingPath(
            context.State.State.CurrentDirectory, unique.Path);
        transferState.UniquePath = uniquePath;
        transferState.Request.Path = uniquePath;
        transferState.Request.OpenMode = FtpFileOpenMode.Overwrite;
        return true;
    }

    internal static bool ApplyPendingAllocation(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState,
        FtpAllocationArgument? allocation)
    {
        if (allocation == null ||
            context.Command.Kind == FtpCommandKind.Retr)
        {
            return true;
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return false;
        }
        FtpFileSystemRequest request = transferState.Request;
        request.HasLength = true;
        request.Length = allocation.ByteCount;
        FtpFileSystemResult result =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () => fileSystem.AllocateEntry(request));
        request.HasLength = false;
        request.Length = 0;
        return result.Succeeded && result.HasAllocatedBytes &&
            result.AllocatedBytes >= allocation.ByteCount;
    }

    internal static bool ApplyRestartState(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        FtpSequenceState state = context.State.State;
        if (!state.HasRestartMarker)
        {
            return true;
        }
        if (context.Command.Kind == FtpCommandKind.Appe ||
            context.Command.Kind == FtpCommandKind.Stou)
        {
            return false;
        }

        long offset;
        if (state.TransferMode == FtpTransferMode.Stream ||
            state.TransferMode == FtpTransferMode.Deflate)
        {
            if (state.FileStructure != FtpFileStructure.File ||
                (state.TransferMode == FtpTransferMode.Stream &&
                 state.TransferType != FtpTransferType.Image))
            {
                return false;
            }
            offset = state.RestartMarker;
        }
        else
        {
            FtpFaultIsolationResult parsing =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return new RestartMarkerCodec().ParseMarker(
                        state.RestartMarkerText);
                });
            FtpRestartCheckpoint? checkpoint =
                parsing.Value as FtpRestartCheckpoint;
            if (!parsing.Succeeded || checkpoint == null)
            {
                return false;
            }
            offset = checkpoint.LogicalByteOffset;
        }
        transferState.Request.HasOffset = true;
        transferState.Request.Offset = offset;
        if (context.Command.Kind == FtpCommandKind.Stor)
        {
            transferState.Request.OpenMode = FtpFileOpenMode.Resume;
        }
        return true;
    }

    internal static bool OpenFile(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return false;
        }
        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                if (context.Command.Kind == FtpCommandKind.Retr)
                {
                    return fileSystem.OpenFileRead(transferState.Request);
                }
                return fileSystem.OpenFileWrite(transferState.Request);
            });
        FtpFileOpenResult? openResult =
            invocation.Value as FtpFileOpenResult;
        if (!invocation.Succeeded)
        {
            context.Session.RecordFaultEvent("file-system provider");
        }
        if (!invocation.Succeeded || openResult == null ||
            !openResult.Succeeded || openResult.Stream == null)
        {
            return false;
        }
        transferState.OpenResult = openResult;
        if (!openResult.LeaveStreamOpen)
        {
            context.Connection.RegisterFileResource(openResult.Stream);
        }
        return true;
    }

    internal static FtpReply BeginTransfer(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState,
        FtpTransferOptions options)
    {
        FtpFileOpenResult? openResult = transferState.OpenResult;
        if (openResult == null || openResult.Stream == null)
        {
            throw new FtpConfigurationException(
                "An open FTP file stream is required.", null);
        }
        FtpClearDataStreamRequest request = CreateDataRequest(
            context, transferState, options, openResult.Stream);
        FtpDataConnectionCoordinator coordinator =
            context.Session.CreateDataConnectionCoordinator();
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        transferState.Transfer = transfer;
        FtpReply preliminary;
        bool sending = context.Command.Kind == FtpCommandKind.Retr;
        if (context.State.State.DataProtectionLevel ==
            FtpDataProtectionLevel.Private)
        {
            FtpProtectedDataStream protectedTransfer =
                new FtpProtectedDataStream(coordinator);
            transferState.ProtectedTransfer = protectedTransfer;
            FtpProtectedDataStreamRequest protectedRequest =
                CreateProtectedRequest(context, request);
            preliminary = sending ?
                protectedTransfer.BeginSend(protectedRequest) :
                protectedTransfer.BeginReceive(protectedRequest);
        }
        else
        {
            preliminary = sending ?
                transfer.BeginSend(request) : transfer.BeginReceive(request);
        }
        if (transferState.UniquePath != null)
        {
            preliminary = FtpServerSession.CreateReply(
                preliminary.Code,
                "File status okay; opening data connection; unique file: " +
                    transferState.UniquePath);
        }
        transferState.PreliminarySent = true;
        return preliminary;
    }

    internal static void ExecuteTransferData(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        bool sending = context.Command.Kind == FtpCommandKind.Retr;
        FtpProtectedDataStream? protectedValue =
            transferState.ProtectedTransfer;
        if (protectedValue != null)
        {
            if (sending)
            {
                protectedValue.ExecuteSendData();
            }
            else
            {
                protectedValue.ExecuteReceiveData();
            }
            transferState.FinalReply = protectedValue.CreateFinalReply();
        }
        else
        {
            FtpClearDataStream? transfer = transferState.Transfer;
            if (transfer == null)
            {
                throw new FtpConfigurationException(
                    "A file data transfer is required.", null);
            }
            if (sending)
            {
                transfer.ExecuteSendData();
            }
            else
            {
                transfer.ExecuteReceiveData();
            }
            transferState.FinalReply = transfer.CreateFinalReply();
        }
    }

    internal static FtpReply CompletePendingTransfer(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                ExecuteTransferData(context, transferState);
                return null;
            });
        FtpFaultIsolationResult cleanup =
            CleanupTransfer(context, transferState);
        if (!operation.Succeeded || !cleanup.Succeeded ||
            transferState.FinalReply == null)
        {
            if (!operation.Succeeded)
            {
                context.Session.RecordFaultEvent("file transfer");
            }
            if (FtpResourceLimitPolicy.IsLimitFailure(operation.Failure))
            {
                context.Session.RecordPolicyEvent(
                    "The FTP transfer resource ceiling was exceeded.");
                return FtpServerSession.CreateReply(
                    FtpReplyCodes.FileActionAbortedStorageExceeded,
                    "Transfer aborted; configured resource limit exceeded.");
            }
            return FtpServerSession.CreateReply(
                FtpReplyCodes.ConnectionClosedTransferAborted,
                "Data connection closed; transfer aborted.");
        }
        return transferState.FinalReply;
    }

    internal static FtpClearDataStreamRequest CreateDataRequest(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState,
        FtpTransferOptions options,
        Stream fileStream)
    {
        FtpClearDataStreamRequest request =
            new FtpClearDataStreamRequest();
        request.CommandKind = context.Command.Kind;
        request.LocalStream = fileStream;
        request.Options = options;
        request.Direction = context.Command.Kind == FtpCommandKind.Retr ?
            FtpTransferDirection.Download : FtpTransferDirection.Upload;
        request.Transform = new FtpTransferPipelineTransform(
            options,
            context.Command.Kind == FtpCommandKind.Retr,
            marker => context.Session.WriteTransferMarker(
                context.Command, marker),
            transferState.Request.HasOffset ?
                transferState.Request.Offset : 0);
        request.Path = transferState.Request.Path;
        FtpFileOpenResult? openResult = transferState.OpenResult;
        if (context.Command.Kind == FtpCommandKind.Retr &&
            openResult != null && openResult.HasLength)
        {
            long offset = transferState.Request.HasOffset ?
                transferState.Request.Offset : 0;
            request.HasExpectedBytes = true;
            request.ExpectedBytes = openResult.Length;
            request.InitialLogicalBytes = offset;
        }
        request.HasMaximumBytes = transferState.HasMaximumBytes;
        request.MaximumBytes = transferState.MaximumBytes;
        request.HasDestinationMaximumBytes =
            transferState.HasStorageMaximumBytes;
        request.DestinationMaximumBytes =
            transferState.StorageMaximumBytes;
        request.HasBytesPerSecond = transferState.HasBytesPerSecond;
        request.BytesPerSecond = transferState.BytesPerSecond;
        request.ConnectionTimeoutMilliseconds =
            options.Timeouts.DataConnectionTimeoutMilliseconds;
        request.TransferTimeoutMilliseconds =
            options.Timeouts.IdleTimeoutMilliseconds;
        request.LeaveLocalStreamOpen =
            openResult != null && openResult.LeaveStreamOpen;
        return request;
    }

    internal static FtpProtectedDataStreamRequest CreateProtectedRequest(
        FtpServerCommandContext context,
        FtpClearDataStreamRequest transferRequest)
    {
        FtpTlsOptions tls = FtpClientOptionsSnapshot.CopyTls(
            FtpServerLoginCommandHandler.ResolveTlsOptions(context.Session));
        tls.DataProtectionLevel = FtpDataProtectionLevel.Private;
        FtpProtectedDataStreamRequest request =
            new FtpProtectedDataStreamRequest();
        request.Transfer = transferRequest;
        request.Role = FtpDataChannelRole.Server;
        request.TlsOptions = tls;
        request.HandshakeTimeoutMilliseconds =
            context.Session.Settings.Timeouts.TlsHandshakeTimeoutMilliseconds;
        request.CertificateProvider = context.CertificateProvider;
        request.HostName = context.Host == null ? "" : context.Host.HostName;
        return request;
    }

    internal static FtpFaultIsolationResult CleanupTransfer(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        FtpFaultIsolationResult result =
            FtpServerSessionContext.CreateSuccessfulResult();
        FtpFileOpenResult? openResult = transferState.OpenResult;
        if (openResult != null && !openResult.LeaveStreamOpen)
        {
            result = FtpServerSessionContext.CloseResource(
                openResult.Stream);
        }
        context.Connection.ClearFileResource();
        context.Connection.ClearDataSocket();
        if (transferState.TransferLimitAcquired)
        {
            context.Connection.EndTransfer();
            transferState.TransferLimitAcquired = false;
        }
        return result;
    }

    internal static FtpCommandResult CreateFileFailure(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedActionNotTaken,
            "The requested file action was not taken.",
            false);
    }

    internal static bool ApplyResourceLimits(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal == null)
        {
            return true;
        }
        FtpResourceLimits limits = principal.ResourceLimits;
        if (limits.HasTransferByteCeiling)
        {
            transferState.HasMaximumBytes = true;
            transferState.MaximumBytes = limits.TransferByteCeiling;
        }
        bool reading = context.Command.Kind == FtpCommandKind.Retr;
        if (reading && limits.HasReadBytesPerSecond)
        {
            transferState.HasBytesPerSecond = true;
            transferState.BytesPerSecond = limits.ReadBytesPerSecond;
        }
        if (!reading && limits.HasWriteBytesPerSecond)
        {
            transferState.HasBytesPerSecond = true;
            transferState.BytesPerSecond = limits.WriteBytesPerSecond;
        }
        if (reading || !limits.HasStorageQuotaBytes)
        {
            return true;
        }
        return ApplyStorageQuota(context, transferState, limits.StorageQuotaBytes);
    }

    internal static bool ApplyStorageQuota(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState,
        long quotaBytes)
    {
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            transferState.ResourceFailureCode =
                FtpReplyCodes.RequestedActionNotTakenInsufficientStorage;
            return false;
        }
        FtpFileSystemResult spaceResult =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () => fileSystem.QuerySpace(transferState.Request));
        if (!spaceResult.Succeeded || spaceResult.Space == null ||
            !spaceResult.Space.HasUsedBytes)
        {
            transferState.ResourceFailureCode =
                FtpReplyCodes.RequestedActionNotTakenInsufficientStorage;
            return false;
        }
        long reclaimableBytes = ResolveReclaimableBytes(
            context, transferState, fileSystem);
        long usedBytes = spaceResult.Space.UsedBytes;
        if (usedBytes < 0)
        {
            transferState.ResourceFailureCode =
                FtpReplyCodes.RequestedActionNotTakenInsufficientStorage;
            return false;
        }
        long allowance = CalculateStorageAllowance(
            quotaBytes, usedBytes, reclaimableBytes);
        if (spaceResult.Space.HasAvailableBytes)
        {
            long availableBytes = spaceResult.Space.AvailableBytes;
            if (availableBytes < 0)
            {
                transferState.ResourceFailureCode =
                    FtpReplyCodes.RequestedActionNotTakenInsufficientStorage;
                return false;
            }
            long physicalAllowance = availableBytes >
                long.MaxValue - reclaimableBytes ?
                    long.MaxValue : availableBytes + reclaimableBytes;
            if (physicalAllowance < allowance)
            {
                allowance = physicalAllowance;
            }
        }
        transferState.HasStorageMaximumBytes = true;
        transferState.StorageMaximumBytes = allowance;
        return true;
    }

    internal static long ResolveReclaimableBytes(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState,
        IFtpFileSystem fileSystem)
    {
        if (context.Command.Kind == FtpCommandKind.Appe ||
            context.Command.Kind == FtpCommandKind.Stou)
        {
            return 0;
        }
        FtpFileSystemResult existing =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () => fileSystem.ResolveEntry(transferState.Request));
        if (!existing.Succeeded || existing.Entry == null ||
            !existing.Entry.HasSize || existing.Entry.Size < 0)
        {
            return 0;
        }
        if (!transferState.Request.HasOffset)
        {
            return existing.Entry.Size;
        }
        if (transferState.Request.Offset >= existing.Entry.Size)
        {
            return 0;
        }
        return existing.Entry.Size - transferState.Request.Offset;
    }

    internal static long CalculateStorageAllowance(
        long quotaBytes,
        long usedBytes,
        long reclaimableBytes)
    {
        if (usedBytes >= quotaBytes)
        {
            long excess = usedBytes - quotaBytes;
            return reclaimableBytes > excess ?
                reclaimableBytes - excess : 0;
        }
        long remaining = quotaBytes - usedBytes;
        return remaining > long.MaxValue - reclaimableBytes ?
            long.MaxValue : remaining + reclaimableBytes;
    }

    internal static bool ValidateKnownTransferLength(
        FtpServerCommandContext context,
        FtpServerFileTransferState transferState)
    {
        if (context.Command.Kind != FtpCommandKind.Retr ||
            !transferState.HasMaximumBytes ||
            transferState.OpenResult == null ||
            !transferState.OpenResult.HasLength)
        {
            return true;
        }
        long offset = transferState.Request.HasOffset ?
            transferState.Request.Offset : 0;
        if (transferState.OpenResult.Length - offset <=
            transferState.MaximumBytes)
        {
            return true;
        }
        transferState.ResourceFailureCode =
            FtpReplyCodes.FileActionAbortedStorageExceeded;
        return false;
    }

    internal static FtpCommandResult CreateResourceFailure(
        FtpServerCommandContext context,
        int replyCode)
    {
        int code = replyCode == 0 ?
            FtpReplyCodes.FileActionAbortedStorageExceeded : replyCode;
        string text = code ==
            FtpReplyCodes.RequestedActionNotTakenInsufficientStorage ?
                "Insufficient storage information is available." :
                "The configured storage or transfer limit was exceeded.";
        context.Session.RecordPolicyEvent(text);
        return FtpServerSession.CreateCommandResult(
            context.Command, code, text, false);
    }
}
