using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ftp;

internal static class FtpServerListingCommandHandler
{
    internal static FtpCommandResult HandleCommand(
        FtpServerCommandContext context)
    {
        if (context.Command.Kind != FtpCommandKind.List &&
            context.Command.Kind != FtpCommandKind.Nlst &&
            context.Command.Kind != FtpCommandKind.Stat &&
            context.Command.Kind != FtpCommandKind.Mlst &&
            context.Command.Kind != FtpCommandKind.Mlsd)
        {
            return FtpServerServiceCommandHandler.HandleCommand(context);
        }
        if (context.FileSystem == null)
        {
            DiscardDataSetup(context);
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        if (IsDataListing(context.Command.Kind) &&
            !context.Session.HasDataConnectionSetup())
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CannotOpenDataConnection,
                "Use PORT, EPRT, PASV, or EPSV before a directory listing.",
                false);
        }

        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreatePathRequest(context);
        FtpAuthorizationOperation operation =
            context.Command.Kind == FtpCommandKind.Mlst ?
                FtpAuthorizationOperation.ReadMetadata :
                FtpAuthorizationOperation.List;
        if (!FtpServerFileSystemCommandHandler.Authorize(
            context, operation, request, null))
        {
            DiscardDataSetup(context);
            return CreateListingFailure(context);
        }

        FtpFileSystemResult fileSystemResult =
            ResolveListing(context, request);
        FtpEntry[] entries = SelectEntries(
            context, fileSystemResult);
        if (!fileSystemResult.Succeeded)
        {
            DiscardDataSetup(context);
            return CreateListingFailure(context);
        }
        entries = FilterAuthorizedEntries(
            context, request, entries, operation);
        if (ExceedsListingEntryCap(context, entries.Length))
        {
            DiscardDataSetup(context);
            return CreateListingLimitFailure(context);
        }

        FtpFaultIsolationResult formatting =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FormatResult(context, request, entries);
            });
        if (!formatting.Succeeded)
        {
            DiscardDataSetup(context);
            return CreateListingFailure(context);
        }
        if (context.Command.Kind == FtpCommandKind.Mlst ||
            context.Command.Kind == FtpCommandKind.Stat)
        {
            FtpReply? controlReply = formatting.Value as FtpReply;
            if (controlReply == null)
            {
                return CreateListingFailure(context);
            }
            if (ExceedsListingByteCap(
                context,
                Encoding.UTF8.GetByteCount(
                    context.Session.ReplyWriter.FormatReply(controlReply))))
            {
                return CreateListingLimitFailure(context);
            }
            return CreateControlResult(context, controlReply);
        }

        byte[]? listingBytes = formatting.Value as byte[];
        if (listingBytes == null)
        {
            DiscardDataSetup(context);
            return CreateListingFailure(context);
        }
        if (ExceedsListingByteCap(context, listingBytes.Length))
        {
            DiscardDataSetup(context);
            return CreateListingLimitFailure(context);
        }
        return TransferListing(context, request.Path, listingBytes);
    }

    internal static FtpFileSystemResult ResolveListing(
        FtpServerCommandContext context,
        FtpFileSystemRequest request)
    {
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateFileSystemFailure();
        }
        if (context.Command.Kind == FtpCommandKind.Mlsd)
        {
            return FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () =>
                {
                    return fileSystem.EnumerateDirectory(request);
                });
        }

        FtpFileSystemResult resolved =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () =>
                {
                    return fileSystem.ResolveEntry(request);
                });
        if (!resolved.Succeeded || resolved.Entry == null ||
            context.Command.Kind == FtpCommandKind.Mlst ||
            !IsDirectory(resolved.Entry))
        {
            return resolved;
        }
        return FtpServerFileSystemCommandHandler.InvokeFileSystem(
            context, () =>
            {
                return fileSystem.EnumerateDirectory(request);
            });
    }

    internal static FtpEntry[] SelectEntries(
        FtpServerCommandContext context,
        FtpFileSystemResult result)
    {
        if (!result.Succeeded)
        {
            return new FtpEntry[0];
        }
        if (context.Command.Kind == FtpCommandKind.Mlst)
        {
            if (result.Entry == null)
            {
                result.Succeeded = false;
                return new FtpEntry[0];
            }
            return new FtpEntry[] { result.Entry };
        }
        if (result.Entry != null && !IsDirectory(result.Entry))
        {
            return new FtpEntry[] { result.Entry };
        }
        if (result.Entries == null)
        {
            result.Succeeded = false;
            return new FtpEntry[0];
        }
        return result.Entries;
    }

    internal static FtpEntry[] FilterAuthorizedEntries(
        FtpServerCommandContext context,
        FtpFileSystemRequest request,
        FtpEntry[] entries,
        FtpAuthorizationOperation operation)
    {
        if (context.Command.Kind == FtpCommandKind.Mlst)
        {
            return entries;
        }
        List<FtpEntry> allowed = new List<FtpEntry>();
        int index = 0;
        while (index < entries.Length)
        {
            FtpEntry entry = entries[index];
            if (FtpServerFileSystemCommandHandler.Authorize(
                context, operation, request, entry))
            {
                allowed.Add(entry);
            }
            index++;
        }
        return allowed.ToArray();
    }

    internal static object FormatResult(
        FtpServerCommandContext context,
        FtpFileSystemRequest request,
        FtpEntry[] entries)
    {
        FtpOptionSessionState options = context.Session.OptionState;
        if (context.Command.Kind == FtpCommandKind.Mlst)
        {
            string entryLine = FtpServerListingFormatter.FormatMachineEntry(
                entries[0], request.Path, options.MlstFacts,
                options.MlstFactsConfigured);
            FtpReply reply = CreateMultilineReply(
                FtpReplyCodes.RequestedFileActionOkay,
                "File status follows",
                new string[] { " " + entryLine },
                "End");
            context.Session.ReplyWriter.FormatReply(reply);
            return reply;
        }
        if (context.Command.Kind == FtpCommandKind.Stat)
        {
            if (entries.Length + 2 > FtpDefaults.MaximumReplyLines)
            {
                throw new FtpProtocolException(
                    "The FTP STAT listing exceeded the reply-line limit.",
                    null);
            }
            string[] lines = new string[entries.Length];
            int index = 0;
            while (index < entries.Length)
            {
                lines[index] =
                    FtpServerListingFormatter.FormatHumanEntry(entries[index]);
                index++;
            }
            FtpReply reply = CreateMultilineReply(
                FtpReplyCodes.DirectoryStatus,
                "Directory status follows",
                lines,
                "End of status");
            context.Session.ReplyWriter.FormatReply(reply);
            return reply;
        }

        string listing = FtpServerListingFormatter.FormatDataListing(
            entries, context.Command.Kind, options.MlstFacts,
            options.MlstFactsConfigured);
        return new UTF8Encoding(false, true).GetBytes(listing);
    }

    internal static FtpCommandResult TransferListing(
        FtpServerCommandContext context,
        string path,
        byte[] listingBytes)
    {
        if (context.Session.HasPendingTransfer())
        {
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedFileActionNotTaken,
                "The previous data transfer is still being released.",
                false);
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

        MemoryStream listingStream = new MemoryStream(listingBytes, false);
        FtpServerListingTransferState transferState =
            new FtpServerListingTransferState(listingStream);
        CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                context.Session.Settings.Timeouts.CancellationToken);
        FtpFaultIsolationResult beginning =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return BeginListingTransfer(
                    context,
                    transferState,
                    path,
                    listingBytes.Length,
                    cancellation.Token);
            });
        FtpReply? preliminary = beginning.Value as FtpReply;
        if (!beginning.Succeeded || preliminary == null)
        {
            FtpServerSessionContext.CloseResource(cancellation);
            CleanupListingTransfer(context, transferState);
            context.Session.DiscardDataConnectionSetup();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CannotOpenDataConnection,
                "The FTP data connection could not be opened.",
                false);
        }

        context.Session.RegisterPendingTransfer(
            context.Command,
            () => CompleteListingTransfer(context, transferState),
            cancellation);
        FtpCommandResult result = new FtpCommandResult();
        result.Command = context.Command;
        result.PreliminaryReply = preliminary;
        result.FinalReply = preliminary;
        result.Succeeded = true;
        return result;
    }

    internal static FtpReply BeginListingTransfer(
        FtpServerCommandContext context,
        FtpServerListingTransferState transferState,
        string path,
        int byteCount,
        CancellationToken cancellationToken)
    {
        FtpClearDataStreamRequest request = CreateDataRequest(
            context,
            path,
            transferState.Stream,
            byteCount,
            cancellationToken);
        FtpDataConnectionCoordinator coordinator =
            context.Session.CreateDataConnectionCoordinator();
        if (context.State.State.DataProtectionLevel ==
            FtpDataProtectionLevel.Private)
        {
            FtpProtectedDataStream protectedTransfer =
                new FtpProtectedDataStream(coordinator);
            transferState.ProtectedTransfer = protectedTransfer;
            transferState.ClearTransfer = protectedTransfer.Transfer;
            FtpProtectedDataStreamRequest protectedRequest =
                FtpServerFileTransferCommandHandler.CreateProtectedRequest(
                    context, request);
            return protectedTransfer.BeginSend(protectedRequest);
        }

        FtpClearDataStream clearTransfer =
            new FtpClearDataStream(coordinator);
        transferState.ClearTransfer = clearTransfer;
        return clearTransfer.BeginSend(request);
    }

    internal static FtpReply CompleteListingTransfer(
        FtpServerCommandContext context,
        FtpServerListingTransferState transferState)
    {
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpProtectedDataStream? protectedTransfer =
                    transferState.ProtectedTransfer;
                if (protectedTransfer != null)
                {
                    protectedTransfer.ExecuteSendData();
                    transferState.FinalReply =
                        protectedTransfer.CreateFinalReply();
                }
                else
                {
                    FtpClearDataStream? clearTransfer =
                        transferState.ClearTransfer;
                    if (clearTransfer == null)
                    {
                        throw new FtpConfigurationException(
                            "A directory listing transfer is required.", null);
                    }
                    clearTransfer.ExecuteSendData();
                    transferState.FinalReply =
                        clearTransfer.CreateFinalReply();
                }
                return null;
            });
        FtpFaultIsolationResult cleanup =
            CleanupListingTransfer(context, transferState);
        if (!operation.Succeeded || !cleanup.Succeeded ||
            transferState.FinalReply == null)
        {
            if (!operation.Succeeded)
            {
                context.Session.RecordFaultEvent(
                    "directory listing transfer");
            }
            if (FtpResourceLimitPolicy.IsLimitFailure(operation.Failure))
            {
                context.Session.RecordPolicyEvent(
                    "The FTP listing resource ceiling was exceeded.");
                return FtpServerSession.CreateReply(
                    FtpReplyCodes.FileActionAbortedStorageExceeded,
                    "Listing aborted; configured resource limit exceeded.");
            }
            return FtpServerSession.CreateReply(
                FtpReplyCodes.ConnectionClosedTransferAborted,
                "Data connection closed; transfer aborted.");
        }
        return transferState.FinalReply;
    }

    internal static FtpFaultIsolationResult CleanupListingTransfer(
        FtpServerCommandContext context,
        FtpServerListingTransferState transferState)
    {
        FtpFaultIsolationResult cleanup =
            FtpServerSessionContext.CloseResource(transferState.Stream);
        context.Connection.ClearDataSocket();
        if (transferState.TransferLimitAcquired)
        {
            context.Connection.EndTransfer();
            transferState.TransferLimitAcquired = false;
        }
        return cleanup;
    }

    internal static FtpClearDataStreamRequest CreateDataRequest(
        FtpServerCommandContext context,
        string path,
        Stream stream,
        int byteCount,
        CancellationToken cancellationToken)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = FtpTransferType.Image;
        options.TextFormat = FtpTextFormat.NonPrint;
        options.Structure = FtpFileStructure.File;
        options.Mode = context.State.State.TransferMode ==
            FtpTransferMode.Deflate ?
            FtpTransferMode.Deflate : FtpTransferMode.Stream;
        options.Timeouts = FtpClientOptionsSnapshot.CopyTimeouts(
            context.Session.Settings.Timeouts);
        options.Timeouts.CancellationToken = cancellationToken;
        options.VerificationMode = FtpVerificationMode.None;

        FtpClearDataStreamRequest request =
            new FtpClearDataStreamRequest();
        request.CommandKind = context.Command.Kind;
        request.LocalStream = stream;
        request.Options = options;
        request.Direction = FtpTransferDirection.Download;
        request.Path = path;
        request.HasExpectedBytes = true;
        request.ExpectedBytes = byteCount;
        ApplyListingTransferLimits(context, request);
        request.ConnectionTimeoutMilliseconds =
            options.Timeouts.DataConnectionTimeoutMilliseconds;
        request.TransferTimeoutMilliseconds =
            options.Timeouts.IdleTimeoutMilliseconds;
        request.LeaveLocalStreamOpen = true;
        return request;
    }

    internal static FtpReply CreateMultilineReply(
        int code,
        string openingText,
        string[] bodyLines,
        string terminalText)
    {
        FtpReply reply = FtpServerSession.CreateReply(code, terminalText);
        reply.Multiline = true;
        reply.Lines = new FtpReplyLine[bodyLines.Length + 2];

        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = code;
        opening.Text = openingText;
        reply.Lines[0] = opening;
        int index = 0;
        while (index < bodyLines.Length)
        {
            FtpReplyLine line = new FtpReplyLine();
            line.Text = bodyLines[index];
            line.RawText = bodyLines[index];
            reply.Lines[index + 1] = line;
            index++;
        }
        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = code;
        terminal.Text = terminalText;
        terminal.TerminatesMultiline = true;
        reply.Lines[reply.Lines.Length - 1] = terminal;
        return reply;
    }

    internal static FtpCommandResult CreateControlResult(
        FtpServerCommandContext context,
        FtpReply reply)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = context.Command;
        result.FinalReply = reply;
        result.Succeeded = true;
        return result;
    }

    internal static FtpCommandResult CreateListingFailure(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedActionNotTaken,
            "The requested listing was not available.",
            false);
    }

    internal static FtpCommandResult CreateListingLimitFailure(
        FtpServerCommandContext context)
    {
        string text = "The configured directory listing limit was exceeded.";
        context.Session.RecordPolicyEvent(text);
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.FileActionAbortedStorageExceeded,
            text,
            false);
    }

    internal static bool ExceedsListingEntryCap(
        FtpServerCommandContext context,
        int entryCount)
    {
        int cap = context.Session.Settings.MaximumListingEntryCount;
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal != null &&
            principal.ResourceLimits.HasListingEntryCap &&
            principal.ResourceLimits.ListingEntryCap < cap)
        {
            cap = principal.ResourceLimits.ListingEntryCap;
        }
        return entryCount > cap;
    }

    internal static bool ExceedsListingByteCap(
        FtpServerCommandContext context,
        long byteCount)
    {
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal == null)
        {
            return false;
        }
        FtpResourceLimits limits = principal.ResourceLimits;
        long cap = long.MaxValue;
        if (limits.HasListingByteCap)
        {
            cap = limits.ListingByteCap;
        }
        if (limits.HasTransferByteCeiling &&
            limits.TransferByteCeiling < cap)
        {
            cap = limits.TransferByteCeiling;
        }
        return byteCount > cap;
    }

    internal static void ApplyListingTransferLimits(
        FtpServerCommandContext context,
        FtpClearDataStreamRequest request)
    {
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal == null)
        {
            return;
        }
        FtpResourceLimits limits = principal.ResourceLimits;
        long cap = long.MaxValue;
        if (limits.HasListingByteCap)
        {
            cap = limits.ListingByteCap;
        }
        if (limits.HasTransferByteCeiling &&
            limits.TransferByteCeiling < cap)
        {
            cap = limits.TransferByteCeiling;
        }
        if (cap != long.MaxValue)
        {
            request.HasMaximumBytes = true;
            request.MaximumBytes = cap;
        }
        if (limits.HasReadBytesPerSecond)
        {
            request.HasBytesPerSecond = true;
            request.BytesPerSecond = limits.ReadBytesPerSecond;
        }
    }

    internal static bool IsDataListing(FtpCommandKind kind)
    {
        return kind == FtpCommandKind.List ||
            kind == FtpCommandKind.Nlst ||
            kind == FtpCommandKind.Mlsd;
    }

    internal static bool IsDirectory(FtpEntry entry)
    {
        return entry.Type == FtpEntryType.Directory ||
            entry.Type == FtpEntryType.CurrentDirectory ||
            entry.Type == FtpEntryType.ParentDirectory;
    }

    internal static void DiscardDataSetup(
        FtpServerCommandContext context)
    {
        if (IsDataListing(context.Command.Kind))
        {
            context.Session.DiscardDataConnectionSetup();
        }
    }
}
