using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ftp;

internal class FtpClientDirectoryUploadOperation
{
    internal FtpClientState ClientState;

    internal FtpClientDirectoryUploadOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for directory uploads.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpDirectoryTransferResult Upload(
        FtpDirectoryTransferRequest? request)
    {
        FtpTransferValidator.ValidateDirectoryTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A directory upload request is required.", null);
        }
        if (request.Direction != FtpTransferDirection.Upload)
        {
            throw new FtpConfigurationException(
                "The recursive upload operation requires upload direction.",
                null);
        }
        if (request.Options.DryRun)
        {
            throw new FtpConfigurationException(
                "Recursive upload execution cannot use dry-run mode.", null);
        }
        if (request.Options.SyncMode != FtpDirectorySyncMode.Copy)
        {
            throw new FtpConfigurationException(
                "Recursive upload execution currently requires copy synchronization mode.",
                null);
        }

        ClientState.RequireOpenSession("TransferDirectory");
        string localRoot = Path.GetFullPath(request.LocalPath);
        if (!Directory.Exists(localRoot) ||
            HasFileAttribute(localRoot, FileAttributes.ReparsePoint))
        {
            throw new FtpConfigurationException(
                "A recursive upload requires a non-symbolic local directory root.",
                null);
        }

        FtpDirectoryUploadState state = new FtpDirectoryUploadState();
        state.Request = request;
        state.LocalRoot = localRoot;
        state.RemoteRoot = FtpPathCodec.ResolveWorkingPath(
            ClientState.Session.State.CurrentDirectory, request.RemotePath);
        FtpEntry root = CreateLocalEntry(localRoot);
        UploadDirectory(
            state, root, localRoot, state.RemoteRoot, 0);
        return CreateResult(state);
    }

    internal bool UploadDirectory(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string localPath,
        string remotePath,
        int depth)
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
                "The recursive upload maximum depth was exceeded.", null);
            return false;
        }
        string identity = Path.GetFullPath(localPath);
        if (state.ActiveDirectories.Contains(identity))
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.Directory,
                "A symbolic-link or directory cycle was detected.", null);
            return false;
        }
        state.ActiveDirectories.Add(identity);

        FtpDirectoryItemResult? directoryItem = CreateItem(
            state, localPath, remotePath, FtpEntryType.Directory);
        if (directoryItem == null)
        {
            state.ActiveDirectories.Remove(identity);
            return false;
        }
        state.Items.Add(directoryItem);
        bool directoryReady = false;
        if (state.Request.Options.PreserveEmptyDirectories)
        {
            directoryReady = CreateRemoteDirectory(
                state, directoryItem, remotePath);
        }

        FtpFaultIsolationResult enumeration = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return Directory.GetFileSystemEntries(localPath);
        });
        string[]? paths = enumeration.Value as string[];
        if (!enumeration.Succeeded || paths == null)
        {
            FailItem(
                state, directoryItem,
                "The local upload directory could not be enumerated.");
            state.ActiveDirectories.Remove(identity);
            return false;
        }
        SortPaths(paths);

        int firstChild = state.Items.Count;
        bool childrenSucceeded = true;
        int index = 0;
        while (index < paths.Length && !state.Stopped)
        {
            string childPath = Path.GetFullPath(paths[index]);
            string name = Path.GetFileName(childPath);
            string remoteChild = FtpClientRecursiveDeletionOperation.ResolveChildPath(
                remotePath, name);
            if (!IsContainedLocalPath(childPath, state.LocalRoot) ||
                !IsSafeName(name) ||
                !FtpClientRecursiveDeletionOperation.IsContainedPath(
                    remoteChild, state.RemoteRoot) || remoteChild == remotePath)
            {
                AddFailureItem(
                    state, childPath, remoteChild, FtpEntryType.Unknown,
                    "A local entry escaped or repeated its upload directory.", null);
                childrenSucceeded = false;
            }
            else
            {
                FtpFaultIsolationResult inspection =
                    FtpFaultIsolationRunner.RunOperation(() =>
                    {
                        return CreateLocalEntry(childPath);
                    });
                FtpEntry? child = inspection.Value as FtpEntry;
                if (!inspection.Succeeded || child == null)
                {
                    AddFailureItem(
                        state, childPath, remoteChild, FtpEntryType.Unknown,
                        "The local upload entry could not be inspected.", null);
                    childrenSucceeded = false;
                }
                else if (!IncludeEntry(
                    state, child, childPath, remoteChild, depth + 1))
                {
                    if (state.FilterFailed)
                    {
                        childrenSucceeded = false;
                        state.FilterFailed = false;
                    }
                }
                else
                {
                    bool childSucceeded = UploadEntry(
                        state, child, childPath, remoteChild, depth + 1);
                    if (!childSucceeded)
                    {
                        childrenSucceeded = false;
                    }
                }
            }
            index++;
        }

        state.ActiveDirectories.Remove(identity);
        if (!directoryReady && HasMaterializedChild(state, firstChild))
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
            state, entry, remotePath, directoryItem))
        {
            childrenSucceeded = false;
        }
        return childrenSucceeded && !state.Stopped;
    }

    internal bool UploadEntry(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string localPath,
        string remotePath,
        int depth)
    {
        if (entry.Type == FtpEntryType.Directory)
        {
            return UploadDirectory(
                state, entry, localPath, remotePath, depth);
        }
        if (entry.Type == FtpEntryType.File)
        {
            return UploadFile(state, entry, localPath, remotePath);
        }
        if (entry.Type == FtpEntryType.SymbolicLink)
        {
            return UploadSymbolicLink(
                state, entry, localPath, remotePath, depth);
        }
        AddFailureItem(
            state, localPath, remotePath, entry.Type,
            "The local entry type cannot be uploaded recursively.", null);
        return false;
    }

    internal bool UploadFile(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string localPath,
        string remotePath)
    {
        FtpDirectoryItemResult? item = CreateItem(
            state, localPath, remotePath, FtpEntryType.File);
        if (item == null)
        {
            return false;
        }
        state.Items.Add(item);
        FtpTransferRequest fileRequest = new FtpTransferRequest();
        fileRequest.Direction = FtpTransferDirection.Upload;
        fileRequest.RemotePath = remotePath;
        fileRequest.LocalPath = localPath;
        fileRequest.HasExpectedSize = entry.HasSize;
        fileRequest.ExpectedSize = entry.Size;
        fileRequest.Options = FtpClientDirectoryDownloadOperation
            .CopyTransferOptions(state.Request.Options.Transfer);
        fileRequest.Options.CreateParentDirectories =
            !state.Request.Options.PreserveEmptyDirectories ||
            state.Request.Options.Transfer.CreateParentDirectories;
        item.Executed = true;
        FtpFaultIsolationResult transferOperation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return new FtpClientUploadOperation(ClientState)
                    .Upload(fileRequest);
            });
        CheckCancellation(state);
        FtpTransferResult? transfer =
            transferOperation.Value as FtpTransferResult;
        if (!transferOperation.Succeeded || transfer == null)
        {
            FailItem(
                state, item,
                "The recursive file upload did not complete.");
            return false;
        }
        item.TransferResult = transfer;
        if (transfer.UniqueRemoteName != null)
        {
            item.RemotePath = ResolveUniquePath(
                remotePath, transfer.UniqueRemoteName);
        }
        if (!transfer.Succeeded)
        {
            FailItem(
                state, item,
                transfer.FailureMessage == null ?
                    "The recursive file upload failed." :
                    transfer.FailureMessage);
            return false;
        }
        if (state.Request.Options.Transfer.CollisionPolicy ==
            FtpCollisionPolicy.Skip && !transfer.HasActualSize)
        {
            item.Skipped = true;
            item.Message = "The remote file was skipped by collision policy.";
            return true;
        }
        if (!ApplyTimestamp(
            state, entry, item.RemotePath, item))
        {
            transfer.Succeeded = false;
            return false;
        }
        if (!ApplyVerification(
            state, entry, item.RemotePath, transfer))
        {
            FailItem(
                state, item,
                transfer.FailureMessage == null ?
                    "The uploaded file failed verification." :
                    transfer.FailureMessage);
            return false;
        }
        item.Succeeded = true;
        state.LogicalBytes += transfer.Progress.LogicalBytes;
        return true;
    }

    internal bool UploadSymbolicLink(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string localPath,
        string remotePath,
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
                "The symbolic-link policy rejects recursive upload.", null);
            return false;
        }
        if (policy == FtpSymlinkPolicy.Preserve)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "FTP has no negotiated portable operation for preserving a symbolic link.",
                null);
            return false;
        }

        FtpFaultIsolationResult resolution =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return File.ResolveLinkTarget(localPath, true);
            });
        FileSystemInfo? target = resolution.Value as FileSystemInfo;
        if (!resolution.Succeeded || target == null)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target is unavailable.", null);
            return false;
        }
        string targetPath = Path.GetFullPath(target.FullName);
        if (!IsContainedLocalPath(targetPath, state.LocalRoot))
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target escapes the upload root.", null);
            return false;
        }
        FtpFaultIsolationResult inspection =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return CreateLocalEntry(targetPath);
            });
        FtpEntry? targetEntry = inspection.Value as FtpEntry;
        if (!inspection.Succeeded || targetEntry == null)
        {
            AddFailureItem(
                state, localPath, remotePath, FtpEntryType.SymbolicLink,
                "The symbolic-link target could not be inspected.", null);
            return false;
        }
        if (targetEntry.Type == FtpEntryType.Directory)
        {
            return UploadDirectory(
                state, targetEntry, targetPath, remotePath, depth);
        }
        if (targetEntry.Type == FtpEntryType.File)
        {
            return UploadFile(
                state, targetEntry, targetPath, remotePath);
        }
        AddFailureItem(
            state, localPath, remotePath, FtpEntryType.SymbolicLink,
            "The symbolic-link target type cannot be uploaded.", null);
        return false;
    }

    internal bool CreateRemoteDirectory(
        FtpDirectoryUploadState state,
        FtpDirectoryItemResult item,
        string remotePath)
    {
        item.Executed = true;
        if (remotePath == "/")
        {
            item.Succeeded = true;
            return true;
        }
        FtpFaultIsolationResult creation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return new FtpClientFileSystemOperation(ClientState)
                .CreateParentDirectories(remotePath);
        });
        FtpCommandResult[]? commands =
            creation.Value as FtpCommandResult[];
        if (!creation.Succeeded || commands == null || commands.Length == 0)
        {
            FailItem(
                state, item,
                "The remote upload directory could not be created.");
            return false;
        }
        item.CommandResult = commands[commands.Length - 1];
        if (!item.CommandResult.Succeeded)
        {
            FailItem(
                state, item,
                FtpClientListingOperation.CreateFailureMessage(
                    item.CommandResult, item.CommandResult.Command.Verb));
            return false;
        }
        item.Succeeded = true;
        return true;
    }

    internal bool ApplyTimestamp(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string remotePath,
        FtpDirectoryItemResult item)
    {
        if (!state.Request.Options.Transfer.PreserveModifiedTime ||
            !entry.Modified.HasValue)
        {
            return true;
        }
        FtpFaultIsolationResult mutation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return new FtpClientMetadataMutationOperation(ClientState)
                .SetModificationTime(remotePath, entry.Modified);
        });
        FtpCommandResult? command = mutation.Value as FtpCommandResult;
        if (!mutation.Succeeded || command == null || !command.Succeeded)
        {
            FailItem(
                state, item,
                "The local modification timestamp could not be preserved remotely.");
            return false;
        }
        item.CommandResult = command;
        return true;
    }

    internal bool ApplyVerification(
        FtpDirectoryUploadState state,
        FtpEntry entry,
        string remotePath,
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
        bool sizeMatches = true;
        bool timeMatches = true;
        List<FtpCommandResult> commands = new List<FtpCommandResult>();
        FtpClientUploadOperation.AppendCommands(commands, transfer.Commands);
        bool verifySize = mode == FtpVerificationMode.Size ||
            mode == FtpVerificationMode.SizeAndModifiedTime;
        if (verifySize)
        {
            FtpPathProbeResult size =
                new FtpClientMetadataOperation(ClientState)
                    .RetrieveSize(remotePath);
            FtpClientUploadOperation.AppendCommands(commands, size.Commands);
            sizeMatches = size.Succeeded && size.Exists &&
                size.Entry != null && size.Entry.HasSize && entry.HasSize &&
                size.Entry.Size == entry.Size;
        }
        bool verifyTime = mode == FtpVerificationMode.ModifiedTime ||
            mode == FtpVerificationMode.SizeAndModifiedTime;
        if (verifyTime)
        {
            FtpPathProbeResult modification =
                new FtpClientMetadataOperation(ClientState)
                    .RetrieveModification(remotePath);
            FtpClientUploadOperation.AppendCommands(
                commands, modification.Commands);
            timeMatches = modification.Succeeded && modification.Exists &&
                modification.Entry != null &&
                modification.Entry.Modified.HasValue &&
                entry.Modified.HasValue && SameTimestamp(
                    modification.Entry.Modified, entry.Modified);
        }
        transfer.Commands = commands.ToArray();
        transfer.Verified = sizeMatches && timeMatches;
        if (!transfer.Verified)
        {
            transfer.Succeeded = false;
            transfer.FailureMessage =
                "The uploaded file failed its configured metadata verification.";
        }
        return transfer.Verified;
    }

    internal bool IncludeEntry(
        FtpDirectoryUploadState state,
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
        filterRequest.Direction = FtpTransferDirection.Upload;
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

    internal FtpDirectoryItemResult? CreateItem(
        FtpDirectoryUploadState state,
        string localPath,
        string remotePath,
        FtpEntryType type)
    {
        if (state.Items.Count >= state.Request.Options.MaximumEntryCount)
        {
            RecordFailure(
                state,
                "The recursive upload maximum entry count was exceeded.");
            state.Stopped = true;
            return null;
        }
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();
        item.LocalPath = localPath;
        item.RemotePath = remotePath;
        item.Direction = FtpTransferDirection.Upload;
        item.EntryType = type;
        item.Planned = true;
        return item;
    }

    internal void AddFailureItem(
        FtpDirectoryUploadState state,
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
        FtpDirectoryUploadState state,
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
        FtpDirectoryUploadState state,
        FtpDirectoryItemResult item,
        string message)
    {
        item.Succeeded = false;
        item.Message = message;
        RecordFailure(state, message);
    }

    internal void RecordFailure(
        FtpDirectoryUploadState state,
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

    internal void CheckCancellation(FtpDirectoryUploadState state)
    {
        FtpCancellation.ThrowIfCancellationRequested(
            state.Request.Options.Transfer.Timeouts.CancellationToken);
    }

    internal FtpDirectoryTransferResult CreateResult(
        FtpDirectoryUploadState state)
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

    internal static FtpEntry CreateLocalEntry(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        FtpEntry entry = new FtpEntry();
        entry.Name = Path.GetFileName(path);
        entry.FullPath = path;
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            entry.Type = FtpEntryType.SymbolicLink;
            entry.LinkTarget = new FileInfo(path).LinkTarget;
        }
        else if ((attributes & FileAttributes.Directory) != 0)
        {
            entry.Type = FtpEntryType.Directory;
        }
        else
        {
            entry.Type = FtpEntryType.File;
            entry.HasSize = true;
            entry.Size = new FileInfo(path).Length;
        }
        DateTime modified = (attributes & FileAttributes.Directory) != 0 ?
            Directory.GetLastWriteTimeUtc(path) :
            File.GetLastWriteTimeUtc(path);
        entry.Modified = CreateTimestamp(modified);
        return entry;
    }

    internal static FtpTimestamp CreateTimestamp(DateTime valueUtc)
    {
        DateTime value = valueUtc.Kind == DateTimeKind.Utc ?
            valueUtc : valueUtc.ToUniversalTime();
        FtpTimestamp timestamp = new FtpTimestamp();
        timestamp.HasValue = true;
        timestamp.ValueUtc = value;
        long fractionalTicks = value.Ticks % TimeSpan.TicksPerSecond;
        timestamp.FractionalSeconds = fractionalTicks == 0 ?
            "" : fractionalTicks.ToString(
                "D7", CultureInfo.InvariantCulture).TrimEnd('0');
        timestamp.RawValue = value.ToString(
            "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        if (timestamp.FractionalSeconds.Length != 0)
        {
            timestamp.RawValue = timestamp.RawValue + "." +
                timestamp.FractionalSeconds;
        }
        return timestamp;
    }

    internal static void SortPaths(string[] paths)
    {
        int index = 1;
        while (index < paths.Length)
        {
            string value = paths[index];
            int target = index;
            while (target > 0 && string.CompareOrdinal(
                Path.GetFileName(paths[target - 1]),
                Path.GetFileName(value)) > 0)
            {
                paths[target] = paths[target - 1];
                target--;
            }
            paths[target] = value;
            index++;
        }
    }

    internal static bool IsContainedLocalPath(string path, string root)
    {
        if (path == root)
        {
            return true;
        }
        string prefix = root.EndsWith(
            Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ?
            root : root + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.Ordinal);
    }

    internal static bool IsSafeName(string name)
    {
        return !string.IsNullOrEmpty(name) && name != "." && name != ".." &&
            name.IndexOf('/') < 0 && name.IndexOf('\\') < 0;
    }

    internal static bool HasFileAttribute(
        string path,
        FileAttributes attribute)
    {
        return (File.GetAttributes(path) & attribute) != 0;
    }

    internal static bool HasMaterializedChild(
        FtpDirectoryUploadState state,
        int startIndex)
    {
        int index = startIndex;
        while (index < state.Items.Count)
        {
            FtpDirectoryItemResult item = state.Items[index];
            if (item.Succeeded ||
                (item.Skipped && item.TransferResult != null))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool SameTimestamp(
        FtpTimestamp first,
        FtpTimestamp second)
    {
        return first.HasValue && second.HasValue &&
            first.ValueUtc == second.ValueUtc &&
            TransferVerifier.NormalizeFraction(first.FractionalSeconds) ==
                TransferVerifier.NormalizeFraction(second.FractionalSeconds) &&
            first.LeapSecond == second.LeapSecond;
    }

    internal static string ResolveUniquePath(
        string requestedPath,
        string uniqueName)
    {
        if (uniqueName.StartsWith("/", StringComparison.Ordinal))
        {
            return uniqueName;
        }
        string parent = FtpClientRecursiveDeletionOperation.GetParentPath(
            requestedPath);
        return FtpPathCodec.ResolveWorkingPath(parent, uniqueName);
    }
}

internal class FtpDirectoryUploadState
{
    internal FtpDirectoryTransferRequest Request;
    internal string LocalRoot;
    internal string RemoteRoot;
    internal List<FtpDirectoryItemResult> Items;
    internal HashSet<string> ActiveDirectories;
    internal bool Stopped;
    internal bool FilterFailed;
    internal string? FailureMessage;
    internal long LogicalBytes;

    internal FtpDirectoryUploadState()
    {
        Request = new FtpDirectoryTransferRequest();
        LocalRoot = "";
        RemoteRoot = "";
        Items = new List<FtpDirectoryItemResult>();
        ActiveDirectories = new HashSet<string>(StringComparer.Ordinal);
        Stopped = false;
        FilterFailed = false;
        FailureMessage = null;
        LogicalBytes = 0;
    }
}
