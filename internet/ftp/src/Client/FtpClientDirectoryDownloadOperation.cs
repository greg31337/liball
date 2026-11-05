using System;
using System.Collections.Generic;
using System.IO;

namespace ftp;

internal class FtpClientDirectoryDownloadOperation
{
    internal FtpClientState ClientState;

    internal FtpClientDirectoryDownloadOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for directory downloads.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpDirectoryTransferResult Download(
        FtpDirectoryTransferRequest? request)
    {
        FtpTransferValidator.ValidateDirectoryTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A directory download request is required.", null);
        }
        if (request.Direction != FtpTransferDirection.Download)
        {
            throw new FtpConfigurationException(
                "The recursive download operation requires download direction.",
                null);
        }
        if (request.Options.DryRun)
        {
            throw new FtpConfigurationException(
                "Recursive download execution cannot use dry-run mode.", null);
        }
        if (request.Options.SyncMode != FtpDirectorySyncMode.Copy)
        {
            throw new FtpConfigurationException(
                "Recursive download execution currently requires copy synchronization mode.",
                null);
        }

        ClientState.RequireOpenSession("TransferDirectory");
        FtpDirectoryDownloadState state = new FtpDirectoryDownloadState();
        state.Request = request;
        state.RemoteRoot = FtpPathCodec.ResolveWorkingPath(
            ClientState.Session.State.CurrentDirectory, request.RemotePath);
        state.LocalRoot = Path.GetFullPath(request.LocalPath);
        FtpEntry root = new FtpEntry();
        root.Name = FtpClientMetadataOperation.GetLeafName(state.RemoteRoot);
        root.FullPath = state.RemoteRoot;
        root.Type = FtpEntryType.Directory;
        DownloadDirectory(
            state, root, state.RemoteRoot, state.LocalRoot, 0, true);
        return CreateResult(state);
    }

    internal bool DownloadDirectory(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string remotePath,
        string localPath,
        int depth,
        bool root)
    {
        if (state.Stopped)
        {
            return false;
        }
        CheckCancellation(state);
        if (depth > state.Request.Options.MaximumDepth)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.Directory,
                "The recursive download maximum depth was exceeded.", null);
            return false;
        }

        string identity = entry.Unique == null ?
            remotePath : "unique:" + entry.Unique;
        if (state.ActiveDirectories.Contains(identity))
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.Directory,
                "A symbolic-link or directory cycle was detected.", null);
            return false;
        }
        state.ActiveDirectories.Add(identity);

        FtpListing listing =
            new FtpClientListingOperation(ClientState).RetrieveAdaptive(remotePath);
        if (!listing.Complete)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.Directory,
                listing.FailureMessage == null ?
                    "The remote download directory could not be listed." :
                    listing.FailureMessage,
                listing.CommandResult);
            state.ActiveDirectories.Remove(identity);
            return false;
        }

        FtpDirectoryItemResult? directoryItem = CreateItem(
            state, localPath, remotePath, FtpEntryType.Directory);
        if (directoryItem == null)
        {
            state.ActiveDirectories.Remove(identity);
            return false;
        }
        directoryItem.CommandResult = listing.CommandResult;
        state.Items.Add(directoryItem);

        bool directoryReady = false;
        if (state.Request.Options.PreserveEmptyDirectories)
        {
            directoryReady = CreateLocalDirectory(
                state, directoryItem, localPath);
        }

        bool childrenSucceeded = true;
        FtpEntry[] entries = FtpClientRecursiveDeletionOperation.SortEntries(
            listing.Entries);
        int index = 0;
        while (index < entries.Length && !state.Stopped)
        {
            FtpEntry child = entries[index];
            string remoteChild = FtpClientRecursiveDeletionOperation.ResolveChildPath(
                remotePath, child.Name);
            string? localChild = ResolveLocalChild(
                state.LocalRoot, localPath, child.Name);
            if (localChild == null ||
                !FtpClientRecursiveDeletionOperation.IsContainedPath(
                    remoteChild, state.RemoteRoot) || remoteChild == remotePath)
            {
                AddFailureItem(
                    state,
                    localChild == null ? localPath : localChild,
                    remoteChild,
                    child.Type,
                    "A listed entry escaped or repeated its download directory.",
                    listing.CommandResult);
                childrenSucceeded = false;
            }
            else if (!IncludeEntry(
                state, child, localChild, remoteChild, depth + 1))
            {
                if (state.FilterFailed)
                {
                    childrenSucceeded = false;
                    state.FilterFailed = false;
                }
            }
            else
            {
                bool childSucceeded = DownloadEntry(
                    state, child, remoteChild, localChild, depth + 1);
                if (!childSucceeded)
                {
                    childrenSucceeded = false;
                }
            }
            index++;
        }

        state.ActiveDirectories.Remove(identity);
        if (!directoryReady && Directory.Exists(localPath))
        {
            directoryReady = true;
            directoryItem.Executed = true;
            directoryItem.Succeeded = true;
        }
        if (!directoryReady && !state.Stopped && childrenSucceeded)
        {
            directoryItem.Skipped = true;
            directoryItem.Message =
                "The empty directory was not created by policy.";
        }
        if (directoryReady && !ApplyTimestamp(
            state, entry, localPath, true, directoryItem))
        {
            childrenSucceeded = false;
        }
        return childrenSucceeded && !state.Stopped;
    }

    internal bool DownloadEntry(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string remotePath,
        string localPath,
        int depth)
    {
        if (entry.Type == FtpEntryType.Directory)
        {
            return DownloadDirectory(
                state, entry, remotePath, localPath, depth, false);
        }
        if (entry.Type == FtpEntryType.File)
        {
            return DownloadFile(state, entry, remotePath, localPath);
        }
        if (entry.Type == FtpEntryType.SymbolicLink)
        {
            return DownloadSymbolicLink(
                state, entry, remotePath, localPath, depth);
        }

        AddFailureItem(
            state, localPath, remotePath, entry.Type,
            "The listed entry type cannot be downloaded recursively.", null);
        return false;
    }

    internal bool DownloadFile(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string remotePath,
        string localPath)
    {
        FtpCollisionPolicy collision =
            state.Request.Options.Transfer.CollisionPolicy;
        if (File.Exists(localPath) && collision == FtpCollisionPolicy.Skip)
        {
            AddSkippedItem(
                state, localPath, remotePath, FtpEntryType.File,
                "The local file was skipped by collision policy.");
            return true;
        }

        FtpDirectoryItemResult? item = CreateItem(
            state, localPath, remotePath, FtpEntryType.File);
        if (item == null)
        {
            return false;
        }
        state.Items.Add(item);
        FtpFaultIsolationResult parent = FtpFaultIsolationRunner.RunOperation(() =>
        {
            string? directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return null;
        });
        if (!parent.Succeeded)
        {
            FailItem(
                state, item,
                "The local parent directory could not be created.");
            return false;
        }

        FtpTransferRequest fileRequest = new FtpTransferRequest();
        fileRequest.Direction = FtpTransferDirection.Download;
        fileRequest.RemotePath = remotePath;
        fileRequest.LocalPath = localPath;
        fileRequest.HasExpectedSize = entry.HasSize;
        fileRequest.ExpectedSize = entry.Size;
        fileRequest.Options = CopyTransferOptions(
            state.Request.Options.Transfer);
        item.Executed = true;
        FtpFaultIsolationResult transferOperation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return new FtpClientDownloadOperation(ClientState)
                    .Download(fileRequest);
            });
        CheckCancellation(state);
        FtpTransferResult? transfer =
            transferOperation.Value as FtpTransferResult;
        if (!transferOperation.Succeeded || transfer == null)
        {
            FailItem(
                state, item,
                "The recursive file download did not complete.");
            return false;
        }

        item.TransferResult = transfer;
        if (transfer.Progress.Path != null)
        {
            item.LocalPath = transfer.Progress.Path;
        }
        if (!transfer.Succeeded)
        {
            FailItem(
                state, item,
                transfer.FailureMessage == null ?
                    "The recursive file download failed." :
                    transfer.FailureMessage);
            return false;
        }
        if (!ApplyTimestamp(
            state, entry, item.LocalPath, false, item))
        {
            transfer.Succeeded = false;
            return false;
        }
        if (!ApplyVerification(state, entry, item.LocalPath, transfer))
        {
            FailItem(
                state, item,
                transfer.FailureMessage == null ?
                    "The downloaded file failed verification." :
                    transfer.FailureMessage);
            return false;
        }

        item.Succeeded = true;
        state.LogicalBytes += transfer.Progress.LogicalBytes;
        return true;
    }

    internal bool DownloadSymbolicLink(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string remotePath,
        string localPath,
        int depth)
    {
        FtpSymlinkPolicy policy = state.Request.Options.SymlinkPolicy;
        if (policy == FtpSymlinkPolicy.Skip)
        {
            AddSkippedItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic link was skipped by policy.");
            return true;
        }
        if (policy == FtpSymlinkPolicy.Fail)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link policy rejects recursive download.", null);
            return false;
        }
        if (string.IsNullOrWhiteSpace(entry.LinkTarget))
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target is unavailable.", null);
            return false;
        }

        string target = FtpPathCodec.ResolveWorkingPath(
            FtpClientRecursiveDeletionOperation.GetParentPath(remotePath),
            entry.LinkTarget);
        if (!FtpClientRecursiveDeletionOperation.IsContainedPath(
            target, state.RemoteRoot))
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target escapes the download root.", null);
            return false;
        }

        FtpPathProbeResult probe =
            new FtpClientMetadataOperation(ClientState).Probe(target);
        if (!probe.Succeeded || !probe.Exists || probe.Entry == null)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                probe.FailureMessage == null ?
                    "The symbolic-link target could not be identified." :
                    probe.FailureMessage,
                probe.Commands.Length == 0 ? null :
                    probe.Commands[probe.Commands.Length - 1]);
            return false;
        }
        if (policy == FtpSymlinkPolicy.Follow)
        {
            if (probe.Entry.Type == FtpEntryType.Directory)
            {
                return DownloadDirectory(
                    state, probe.Entry, target, localPath, depth, false);
            }
            if (probe.Entry.Type == FtpEntryType.File)
            {
                return DownloadFile(
                    state, probe.Entry, target, localPath);
            }
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target type cannot be downloaded.", null);
            return false;
        }
        return PreserveSymbolicLink(
            state, probe.Entry, localPath, remotePath, target);
    }

    internal bool PreserveSymbolicLink(
        FtpDirectoryDownloadState state,
        FtpEntry targetEntry,
        string localPath,
        string remotePath,
        string targetRemotePath)
    {
        FtpDirectoryItemResult? item = CreateItem(
            state, localPath, remotePath, FtpEntryType.SymbolicLink);
        if (item == null)
        {
            return false;
        }
        state.Items.Add(item);
        FtpCollisionPolicy collision =
            state.Request.Options.Transfer.CollisionPolicy;
        bool exists = File.Exists(localPath) || Directory.Exists(localPath);
        if (exists && collision == FtpCollisionPolicy.Skip)
        {
            item.Skipped = true;
            item.Message = "The local symbolic link was skipped by collision policy.";
            return true;
        }
        if (exists && collision != FtpCollisionPolicy.Overwrite)
        {
            FailItem(
                state, item,
                "The local symbolic-link destination already exists.");
            return false;
        }

        string suffix = targetRemotePath == state.RemoteRoot ?
            "" : targetRemotePath.Substring(state.RemoteRoot.Length + 1);
        string targetLocal = suffix.Length == 0 ?
            state.LocalRoot : Path.Combine(
                state.LocalRoot,
                suffix.Replace('/', Path.DirectorySeparatorChar));
        string? parentPath = Path.GetDirectoryName(localPath);
        if (parentPath == null)
        {
            FailItem(state, item, "The local symbolic-link parent is unavailable.");
            return false;
        }
        string relativeTarget = Path.GetRelativePath(parentPath, targetLocal);
        item.Executed = true;
        FtpFaultIsolationResult creation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            Directory.CreateDirectory(parentPath);
            if (exists && Directory.Exists(localPath))
            {
                Directory.Delete(localPath);
            }
            else if (exists)
            {
                File.Delete(localPath);
            }
            if (targetEntry.Type == FtpEntryType.Directory)
            {
                Directory.CreateSymbolicLink(localPath, relativeTarget);
            }
            else
            {
                File.CreateSymbolicLink(localPath, relativeTarget);
            }
            return null;
        });
        if (!creation.Succeeded)
        {
            FailItem(state, item, "The local symbolic link could not be created.");
            return false;
        }
        item.Succeeded = true;
        return true;
    }

    internal bool IncludeEntry(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string localPath,
        string remotePath,
        int depth)
    {
        IFtpTransferFilter? filter = state.Request.Options.Filter;
        if (filter == null)
        {
            return true;
        }
        FtpTransferFilterRequest filterRequest = new FtpTransferFilterRequest();
        filterRequest.Direction = FtpTransferDirection.Download;
        filterRequest.Entry = entry;
        filterRequest.LocalPath = localPath;
        filterRequest.RemotePath = remotePath;
        filterRequest.Depth = depth;
        FtpFaultIsolationResult inclusion = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return filter.IncludeTransferEntry(filterRequest);
        });
        if (!inclusion.Succeeded || inclusion.Value == null)
        {
            AddFailureItem(
                state, localPath, remotePath, entry.Type,
                "The recursive transfer filter failed.", null);
            state.FilterFailed = true;
            return false;
        }
        bool included = (bool)inclusion.Value;
        if (!included)
        {
            AddSkippedItem(
                state, localPath, remotePath, entry.Type,
                "The entry was excluded by the recursive transfer filter.");
        }
        return included;
    }

    internal bool CreateLocalDirectory(
        FtpDirectoryDownloadState state,
        FtpDirectoryItemResult item,
        string localPath)
    {
        item.Executed = true;
        FtpFaultIsolationResult creation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            Directory.CreateDirectory(localPath);
            return null;
        });
        if (!creation.Succeeded)
        {
            FailItem(state, item, "The local download directory could not be created.");
            return false;
        }
        item.Succeeded = true;
        return true;
    }

    internal bool ApplyTimestamp(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string localPath,
        bool directory,
        FtpDirectoryItemResult item)
    {
        if (!state.Request.Options.Transfer.PreserveModifiedTime ||
            !entry.Modified.HasValue)
        {
            return true;
        }
        FtpFaultIsolationResult timestamp = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (directory)
            {
                Directory.SetLastWriteTimeUtc(
                    localPath, entry.Modified.ValueUtc);
            }
            else
            {
                File.SetLastWriteTimeUtc(
                    localPath, entry.Modified.ValueUtc);
            }
            return null;
        });
        if (!timestamp.Succeeded)
        {
            FailItem(
                state, item,
                "The remote modification timestamp could not be preserved locally.");
            return false;
        }
        return true;
    }

    internal bool ApplyVerification(
        FtpDirectoryDownloadState state,
        FtpEntry entry,
        string localPath,
        FtpTransferResult transfer)
    {
        FtpVerificationMode mode =
            state.Request.Options.Transfer.VerificationMode;
        if (mode == FtpVerificationMode.None ||
            mode == FtpVerificationMode.Checksum)
        {
            return transfer.Succeeded;
        }

        transfer.VerificationAttempted = true;
        bool sizeMatches = entry.HasSize && transfer.HasActualSize &&
            entry.Size == transfer.ActualSize;
        bool timeMatches = false;
        if (entry.Modified.HasValue && File.Exists(localPath))
        {
            timeMatches = File.GetLastWriteTimeUtc(localPath) ==
                entry.Modified.ValueUtc;
        }
        bool verified = mode == FtpVerificationMode.Size ?
            sizeMatches : mode == FtpVerificationMode.ModifiedTime ?
            timeMatches : sizeMatches && timeMatches;
        transfer.Verified = verified;
        if (!verified)
        {
            transfer.Succeeded = false;
            transfer.FailureMessage =
                "The downloaded file failed its configured metadata verification.";
        }
        return verified;
    }

    internal FtpDirectoryItemResult? CreateItem(
        FtpDirectoryDownloadState state,
        string localPath,
        string remotePath,
        FtpEntryType type)
    {
        if (state.Items.Count >= state.Request.Options.MaximumEntryCount)
        {
            RecordFailure(
                state,
                "The recursive download maximum entry count was exceeded.");
            state.Stopped = true;
            return null;
        }
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();
        item.LocalPath = localPath;
        item.RemotePath = remotePath;
        item.Direction = FtpTransferDirection.Download;
        item.EntryType = type;
        item.Planned = true;
        return item;
    }

    internal void AddFailureItem(
        FtpDirectoryDownloadState state,
        string localPath,
        string remotePath,
        FtpEntryType type,
        string message,
        FtpCommandResult? command)
    {
        FtpDirectoryItemResult? item = CreateItem(
            state, localPath, remotePath, type);
        if (item == null)
        {
            return;
        }
        item.Executed = command != null;
        item.CommandResult = command;
        item.Message = message;
        state.Items.Add(item);
        RecordFailure(state, message);
    }

    internal void AddSkippedItem(
        FtpDirectoryDownloadState state,
        string localPath,
        string remotePath,
        FtpEntryType type,
        string message)
    {
        FtpDirectoryItemResult? item = CreateItem(
            state, localPath, remotePath, type);
        if (item == null)
        {
            return;
        }
        item.Skipped = true;
        item.Message = message;
        state.Items.Add(item);
    }

    internal void FailItem(
        FtpDirectoryDownloadState state,
        FtpDirectoryItemResult item,
        string message)
    {
        item.Succeeded = false;
        item.Message = message;
        RecordFailure(state, message);
    }

    internal void RecordFailure(
        FtpDirectoryDownloadState state,
        string message)
    {
        if (state.FailureMessage == null)
        {
            state.FailureMessage = message;
        }
        if (state.Request.Options.FailurePolicy == FtpFailurePolicy.Stop)
        {
            state.Stopped = true;
        }
    }

    internal void CheckCancellation(FtpDirectoryDownloadState state)
    {
        FtpCancellation.ThrowIfCancellationRequested(
            state.Request.Options.Transfer.Timeouts.CancellationToken);
    }

    internal FtpDirectoryTransferResult CreateResult(
        FtpDirectoryDownloadState state)
    {
        FtpDirectoryTransferResult result = new FtpDirectoryTransferResult();
        result.Request = state.Request;
        result.Items = state.Items.ToArray();
        result.Plan.Request = state.Request;
        result.Plan.Items = result.Items;
        result.Plan.Complete = !state.Stopped;
        result.Plan.FailureMessage = state.FailureMessage;
        result.FailureMessage = state.FailureMessage;
        result.TotalItems = result.Items.Length;
        result.LogicalBytes = state.LogicalBytes;
        int index = 0;
        while (index < result.Items.Length)
        {
            FtpDirectoryItemResult item = result.Items[index];
            if (item.Succeeded)
            {
                result.SucceededItems++;
            }
            else if (item.Skipped)
            {
                result.SkippedItems++;
            }
            else
            {
                result.FailedItems++;
            }
            index++;
        }
        result.Succeeded = result.FailedItems == 0 &&
            state.FailureMessage == null;
        return result;
    }

    internal static string? ResolveLocalChild(
        string localRoot,
        string localParent,
        string name)
    {
        if (string.IsNullOrEmpty(name) || name == "." || name == ".." ||
            name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 ||
            Path.IsPathRooted(name))
        {
            return null;
        }
        string child = Path.GetFullPath(Path.Combine(localParent, name));
        string prefix = localRoot.EndsWith(
            Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ?
            localRoot : localRoot + Path.DirectorySeparatorChar;
        if (child == localRoot || !child.StartsWith(
            prefix, StringComparison.Ordinal))
        {
            return null;
        }
        return child;
    }

    internal static FtpTransferOptions CopyTransferOptions(
        FtpTransferOptions source)
    {
        FtpTransferOptions copy = new FtpTransferOptions();
        copy.Type = source.Type;
        copy.TextFormat = source.TextFormat;
        copy.Structure = source.Structure;
        copy.Mode = source.Mode;
        copy.HasLocalByteSize = source.HasLocalByteSize;
        copy.LocalByteSize = source.LocalByteSize;
        copy.CollisionPolicy = source.CollisionPolicy;
        copy.ResumePolicy = source.ResumePolicy;
        copy.VerificationMode = source.VerificationMode;
        copy.Timeouts = FtpClientOptionsSnapshot.CopyTimeouts(source.Timeouts);
        copy.CreateParentDirectories = source.CreateParentDirectories;
        copy.AtomicUpload = source.AtomicUpload;
        copy.CleanupPartialOnFailure = source.CleanupPartialOnFailure;
        copy.PreserveModifiedTime = source.PreserveModifiedTime;
        copy.EnableKeepAlive = source.EnableKeepAlive;
        copy.KeepAliveIntervalMilliseconds =
            source.KeepAliveIntervalMilliseconds;
        copy.BufferBytes = source.BufferBytes;
        copy.ExpectedChecksumAlgorithm = source.ExpectedChecksumAlgorithm;
        copy.ExpectedChecksum = source.ExpectedChecksum;
        return copy;
    }
}

internal class FtpDirectoryDownloadState
{
    internal FtpDirectoryTransferRequest Request;
    internal string RemoteRoot;
    internal string LocalRoot;
    internal List<FtpDirectoryItemResult> Items;
    internal HashSet<string> ActiveDirectories;
    internal bool Stopped;
    internal bool FilterFailed;
    internal string? FailureMessage;
    internal long LogicalBytes;

    internal FtpDirectoryDownloadState()
    {
        Request = new FtpDirectoryTransferRequest();
        RemoteRoot = "";
        LocalRoot = "";
        Items = new List<FtpDirectoryItemResult>();
        ActiveDirectories = new HashSet<string>(StringComparer.Ordinal);
        Stopped = false;
        FilterFailed = false;
        FailureMessage = null;
        LogicalBytes = 0;
    }
}
