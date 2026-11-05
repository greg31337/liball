using System;
using System.Collections.Generic;
using System.IO;

namespace ftp;

internal class FtpClientDirectorySynchronizationOperation
{
    internal FtpClientState ClientState;

    internal FtpClientDirectorySynchronizationOperation(
        FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for directory synchronization.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpDirectoryTransferResult Synchronize(
        FtpDirectoryTransferRequest? request)
    {
        FtpTransferValidator.ValidateDirectoryTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A directory synchronization request is required.", null);
        }
        if (request.Options.DryRun)
        {
            throw new FtpConfigurationException(
                "Directory synchronization execution cannot use dry-run mode.",
                null);
        }
        if (request.Options.SyncMode == FtpDirectorySyncMode.Copy)
        {
            throw new FtpConfigurationException(
                "Directory synchronization execution requires update or mirror mode.",
                null);
        }
        ClientState.RequireOpenSession("TransferDirectory");

        string localRoot = Path.GetFullPath(request.LocalPath);
        string remoteRoot = FtpPathCodec.ResolveWorkingPath(
            ClientState.Session.State.CurrentDirectory, request.RemotePath);
        ValidateDeletionRoot(request, localRoot, remoteRoot);

        FtpClientDirectoryPlanningOperation planning =
            new FtpClientDirectoryPlanningOperation(ClientState);
        FtpDirectoryPlan plan = planning.Plan(request);
        FtpDirectorySynchronizationState state =
            new FtpDirectorySynchronizationState();
        state.Request = request;
        state.Plan = plan;
        state.PlanningState = planning.LatestState;
        state.LocalRoot = localRoot;
        state.RemoteRoot = remoteRoot;
        state.FailureMessage = plan.FailureMessage;

        int index = 0;
        while (index < plan.Items.Length && !state.Stopped)
        {
            CheckCancellation(state);
            ExecutePlanItem(state, plan.Items[index]);
            index++;
        }
        return CreateResult(state);
    }

    internal void ExecutePlanItem(
        FtpDirectorySynchronizationState state,
        FtpDirectoryItemResult planned)
    {
        if (planned.Skipped)
        {
            state.Items.Add(CopyItem(planned));
            return;
        }
        if (!planned.Planned)
        {
            FtpDirectoryItemResult failure = CopyItem(planned);
            state.Items.Add(failure);
            RecordFailure(
                state, failure.Message == null ?
                    "A synchronization plan item could not be executed." :
                    failure.Message);
            return;
        }
        if (planned.Deleted)
        {
            ExecuteDeletion(state, planned);
            return;
        }

        FtpDirectorySnapshotEntry? source = FindSourceEntry(state, planned);
        if (source == null)
        {
            FtpDirectoryItemResult missing = CopyItem(planned);
            missing.Planned = false;
            missing.Message =
                "The planned synchronization source entry was unavailable.";
            state.Items.Add(missing);
            RecordFailure(state, missing.Message);
            return;
        }
        FtpDirectorySnapshotEntry? destination =
            FindDestinationEntry(state, source.RelativePath);
        if (destination != null &&
            destination.Comparison.Type != source.Comparison.Type)
        {
            if (!ReplaceConflictingDestination(state, source, destination))
            {
                return;
            }
        }

        if (source.Original.Type == FtpEntryType.SymbolicLink &&
            state.Request.Options.SymlinkPolicy == FtpSymlinkPolicy.Preserve)
        {
            ExecutePreservedLink(state, source);
            return;
        }
        if (source.Comparison.Type == FtpEntryType.Directory)
        {
            ExecuteDirectory(state, source);
            return;
        }
        if (source.Comparison.Type == FtpEntryType.File)
        {
            ExecuteFile(state, source);
            return;
        }

        FtpDirectoryItemResult unsupported = CopyItem(planned);
        unsupported.Planned = false;
        unsupported.Message =
            "The planned synchronization entry type is unsupported.";
        state.Items.Add(unsupported);
        RecordFailure(state, unsupported.Message);
    }

    internal void ExecuteFile(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source)
    {
        if (state.Request.Direction == FtpTransferDirection.Download)
        {
            FtpDirectoryDownloadState download = CreateDownloadState(state);
            new FtpClientDirectoryDownloadOperation(ClientState).DownloadFile(
                download, source.Comparison,
                source.RemotePath, source.LocalPath);
            AppendOperationItem(state, download.Items, download.FailureMessage);
        }
        else
        {
            FtpDirectoryUploadState upload = CreateUploadState(state);
            new FtpClientDirectoryUploadOperation(ClientState).UploadFile(
                upload, source.Comparison,
                source.LocalPath, source.RemotePath);
            AppendOperationItem(state, upload.Items, upload.FailureMessage);
        }
    }

    internal void ExecuteDirectory(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source)
    {
        FtpDirectoryItemResult item = CreateExecutionItem(
            state, source, FtpEntryType.Directory);
        bool succeeded;
        if (state.Request.Direction == FtpTransferDirection.Download)
        {
            FtpDirectoryDownloadState download = CreateDownloadState(state);
            FtpClientDirectoryDownloadOperation operation =
                new FtpClientDirectoryDownloadOperation(ClientState);
            succeeded = operation.CreateLocalDirectory(
                download, item, source.LocalPath);
            if (succeeded)
            {
                succeeded = operation.ApplyTimestamp(
                    download, source.Comparison, source.LocalPath,
                    true, item);
            }
        }
        else
        {
            FtpDirectoryUploadState upload = CreateUploadState(state);
            FtpClientDirectoryUploadOperation operation =
                new FtpClientDirectoryUploadOperation(ClientState);
            succeeded = operation.CreateRemoteDirectory(
                upload, item, source.RemotePath);
            if (succeeded)
            {
                succeeded = operation.ApplyTimestamp(
                    upload, source.Comparison, source.RemotePath, item);
            }
        }
        state.Items.Add(item);
        if (!succeeded)
        {
            RecordFailure(
                state, item.Message == null ?
                    "A synchronization directory could not be created." :
                    item.Message);
        }
    }

    internal void ExecutePreservedLink(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source)
    {
        if (state.Request.Direction == FtpTransferDirection.Upload)
        {
            FtpDirectoryItemResult unsupported = CreateExecutionItem(
                state, source, FtpEntryType.SymbolicLink);
            unsupported.Planned = false;
            unsupported.Message =
                "FTP has no negotiated portable operation for preserving a symbolic link.";
            state.Items.Add(unsupported);
            RecordFailure(state, unsupported.Message);
            return;
        }

        FtpDirectoryDownloadState download = CreateDownloadState(state);
        new FtpClientDirectoryDownloadOperation(ClientState)
            .DownloadSymbolicLink(
                download, source.Original,
                source.RemotePath, source.LocalPath, source.Depth);
        AppendOperationItem(state, download.Items, download.FailureMessage);
    }

    internal void ExecuteDeletion(
        FtpDirectorySynchronizationState state,
        FtpDirectoryItemResult planned)
    {
        string deletionKey = GetDeletionKey(state, planned);
        if (state.RemovedDestinations.Contains(deletionKey))
        {
            FtpDirectoryItemResult redundant = CopyItem(planned);
            redundant.Deleted = false;
            redundant.Skipped = true;
            redundant.Message =
                "The destination entry was already removed for type replacement.";
            state.Items.Add(redundant);
            return;
        }
        FtpDirectoryItemResult item = CopyItem(planned);
        item.Deleted = false;
        if (!IsSafeDeletion(state, item))
        {
            item.Planned = false;
            item.Message =
                "The planned deletion escaped or selected its synchronization root.";
            state.Items.Add(item);
            RecordFailure(state, item.Message);
            return;
        }

        item.Executed = true;
        if (state.Request.Direction == FtpTransferDirection.Download)
        {
            FtpFaultIsolationResult deletion =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    if (item.EntryType == FtpEntryType.Directory)
                    {
                        Directory.Delete(item.LocalPath);
                    }
                    else
                    {
                        File.Delete(item.LocalPath);
                    }
                    return null;
                });
            item.Succeeded = deletion.Succeeded;
            item.Deleted = deletion.Succeeded;
            if (!deletion.Succeeded)
            {
                item.Message =
                    "The destination-only local entry could not be deleted.";
            }
        }
        else
        {
            FtpFaultIsolationResult deletion =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    FtpClientFileSystemOperation fileSystem =
                        new FtpClientFileSystemOperation(ClientState);
                    return item.EntryType == FtpEntryType.Directory ?
                        fileSystem.RemoveDirectory(item.RemotePath) :
                        fileSystem.DeleteFile(item.RemotePath);
                });
            FtpCommandResult? command =
                deletion.Value as FtpCommandResult;
            item.CommandResult = command;
            item.Succeeded = deletion.Succeeded && command != null &&
                command.Succeeded;
            item.Deleted = item.Succeeded;
            if (!item.Succeeded)
            {
                item.Message = command == null ?
                    "The destination-only remote entry could not be deleted." :
                    FtpClientListingOperation.CreateFailureMessage(
                        command, command.Command.Verb);
            }
        }
        state.Items.Add(item);
        if (!item.Succeeded)
        {
            RecordFailure(
                state, item.Message == null ?
                    "A synchronization deletion failed." : item.Message);
        }
    }

    internal bool ReplaceConflictingDestination(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source,
        FtpDirectorySnapshotEntry destination)
    {
        if (state.Request.Options.Transfer.CollisionPolicy !=
            FtpCollisionPolicy.Overwrite)
        {
            AddTypeConflictFailure(state, source,
                "The destination type conflict requires overwrite collision policy.");
            return false;
        }
        if (destination.RelativePath.Length == 0)
        {
            AddTypeConflictFailure(state, source,
                "Replacement of a synchronization root is prohibited.");
            return false;
        }
        if (destination.Comparison.Type == FtpEntryType.Directory &&
            !state.Request.Options.AllowRecursiveDeletion)
        {
            AddTypeConflictFailure(state, source,
                "Replacing a destination directory requires recursive-deletion opt-in.");
            return false;
        }

        FtpDirectorySnapshot snapshot =
            state.Request.Direction == FtpTransferDirection.Upload ?
                state.PlanningState.RemoteSnapshot :
                state.PlanningState.LocalSnapshot;
        bool succeeded = true;
        int index = snapshot.Entries.Count - 1;
        while (index >= 0 && !state.Stopped)
        {
            FtpDirectorySnapshotEntry entry = snapshot.Entries[index];
            if (entry.RelativePath == destination.RelativePath ||
                FtpClientDirectoryPlanningOperation.IsRelativeChild(
                    entry.RelativePath, destination.RelativePath))
            {
                FtpDirectoryItemResult deletion = CreateDeletionItem(
                    state, entry);
                ExecuteDeletion(state, deletion);
                if (state.Items.Count == 0 ||
                    !state.Items[state.Items.Count - 1].Succeeded)
                {
                    succeeded = false;
                }
                else
                {
                    state.RemovedDestinations.Add(
                        GetDeletionKey(state, deletion));
                }
            }
            index--;
        }
        return succeeded;
    }

    internal void AddTypeConflictFailure(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source,
        string message)
    {
        FtpDirectoryItemResult conflict = CreateExecutionItem(
            state, source, source.Comparison.Type);
        conflict.Planned = false;
        conflict.Message = message;
        state.Items.Add(conflict);
        RecordFailure(state, message);
    }

    internal void AppendOperationItem(
        FtpDirectorySynchronizationState state,
        List<FtpDirectoryItemResult> items,
        string? failureMessage)
    {
        if (items.Count == 0)
        {
            RecordFailure(
                state, failureMessage == null ?
                    "A synchronization transfer produced no item result." :
                    failureMessage);
            return;
        }
        FtpDirectoryItemResult item = items[items.Count - 1];
        state.Items.Add(item);
        if (item.TransferResult != null && item.Succeeded)
        {
            state.LogicalBytes += item.TransferResult.Progress.LogicalBytes;
        }
        if (!item.Succeeded && !item.Skipped)
        {
            RecordFailure(
                state, item.Message == null ?
                    failureMessage == null ?
                        "A synchronization transfer failed." : failureMessage :
                    item.Message);
        }
    }

    internal FtpDirectoryDownloadState CreateDownloadState(
        FtpDirectorySynchronizationState state)
    {
        FtpDirectoryDownloadState download =
            new FtpDirectoryDownloadState();
        download.Request = state.Request;
        download.LocalRoot = state.LocalRoot;
        download.RemoteRoot = state.RemoteRoot;
        return download;
    }

    internal FtpDirectoryUploadState CreateUploadState(
        FtpDirectorySynchronizationState state)
    {
        FtpDirectoryUploadState upload = new FtpDirectoryUploadState();
        upload.Request = state.Request;
        upload.LocalRoot = state.LocalRoot;
        upload.RemoteRoot = state.RemoteRoot;
        return upload;
    }

    internal FtpDirectorySnapshotEntry? FindSourceEntry(
        FtpDirectorySynchronizationState state,
        FtpDirectoryItemResult item)
    {
        FtpDirectorySnapshot snapshot =
            state.Request.Direction == FtpTransferDirection.Upload ?
                state.PlanningState.LocalSnapshot :
                state.PlanningState.RemoteSnapshot;
        int index = 0;
        while (index < snapshot.Entries.Count)
        {
            FtpDirectorySnapshotEntry entry = snapshot.Entries[index];
            if (entry.LocalPath == item.LocalPath &&
                entry.RemotePath == item.RemotePath)
            {
                return entry;
            }
            index++;
        }
        return null;
    }

    internal FtpDirectorySnapshotEntry? FindDestinationEntry(
        FtpDirectorySynchronizationState state,
        string relativePath)
    {
        FtpDirectorySnapshot snapshot =
            state.Request.Direction == FtpTransferDirection.Upload ?
                state.PlanningState.RemoteSnapshot :
                state.PlanningState.LocalSnapshot;
        if (snapshot.Index.ContainsKey(relativePath))
        {
            return snapshot.Index[relativePath];
        }
        return null;
    }

    internal bool IsSafeDeletion(
        FtpDirectorySynchronizationState state,
        FtpDirectoryItemResult item)
    {
        if (state.Request.Direction == FtpTransferDirection.Download)
        {
            string path = Path.GetFullPath(item.LocalPath);
            return path != state.LocalRoot &&
                FtpClientDirectoryUploadOperation.IsContainedLocalPath(
                    path, state.LocalRoot);
        }
        string remote = FtpPathCodec.ResolveWorkingPath(
            state.RemoteRoot, item.RemotePath);
        return remote != state.RemoteRoot &&
            FtpClientRecursiveDeletionOperation.IsContainedPath(
                remote, state.RemoteRoot);
    }

    internal void RecordFailure(
        FtpDirectorySynchronizationState state,
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

    internal void CheckCancellation(
        FtpDirectorySynchronizationState state)
    {
        FtpCancellation.ThrowIfCancellationRequested(
            state.Request.Options.Transfer.Timeouts.CancellationToken);
    }

    internal FtpDirectoryTransferResult CreateResult(
        FtpDirectorySynchronizationState state)
    {
        FtpDirectoryTransferResult result = new FtpDirectoryTransferResult();
        result.Request = state.Request;
        result.Plan = state.Plan;
        result.Items = state.Items.ToArray();
        result.TotalItems = result.Items.Length;
        result.LogicalBytes = state.LogicalBytes;
        result.FailureMessage = state.FailureMessage;
        int index = 0;
        while (index < result.Items.Length)
        {
            FtpDirectoryItemResult item = result.Items[index];
            if (item.Succeeded)
            {
                result.SucceededItems++;
                if (item.Deleted)
                {
                    result.DeletedItems++;
                }
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
        result.Succeeded = state.Plan.Complete &&
            result.FailedItems == 0 && state.FailureMessage == null;
        return result;
    }

    internal static FtpDirectoryItemResult CreateExecutionItem(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry source,
        FtpEntryType type)
    {
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();
        item.LocalPath = source.LocalPath;
        item.RemotePath = source.RemotePath;
        item.Direction = state.Request.Direction;
        item.EntryType = type;
        item.Planned = true;
        return item;
    }

    internal static FtpDirectoryItemResult CopyItem(
        FtpDirectoryItemResult source)
    {
        FtpDirectoryItemResult copy = new FtpDirectoryItemResult();
        copy.LocalPath = source.LocalPath;
        copy.RemotePath = source.RemotePath;
        copy.Direction = source.Direction;
        copy.EntryType = source.EntryType;
        copy.Planned = source.Planned;
        copy.Executed = source.Executed;
        copy.Succeeded = source.Succeeded;
        copy.Skipped = source.Skipped;
        copy.Deleted = source.Deleted;
        copy.TransferResult = source.TransferResult;
        copy.CommandResult = source.CommandResult;
        copy.Message = source.Message;
        return copy;
    }

    internal static FtpDirectoryItemResult CreateDeletionItem(
        FtpDirectorySynchronizationState state,
        FtpDirectorySnapshotEntry entry)
    {
        FtpDirectoryItemResult item = CreateExecutionItem(
            state, entry, entry.Original.Type == FtpEntryType.SymbolicLink ?
                FtpEntryType.SymbolicLink : entry.Comparison.Type);
        item.Deleted = true;
        item.Message = "The conflicting destination entry would be deleted.";
        return item;
    }

    internal static string GetDeletionKey(
        FtpDirectorySynchronizationState state,
        FtpDirectoryItemResult item)
    {
        return state.Request.Direction == FtpTransferDirection.Upload ?
            item.RemotePath : item.LocalPath;
    }

    internal static void ValidateDeletionRoot(
        FtpDirectoryTransferRequest request,
        string localRoot,
        string remoteRoot)
    {
        if (!request.Options.DeleteExtraneous)
        {
            return;
        }
        if (request.Direction == FtpTransferDirection.Upload &&
            remoteRoot == "/")
        {
            throw new FtpConfigurationException(
                "Mirror deletion at the FTP root is prohibited.", null);
        }
        string? fileSystemRoot = Path.GetPathRoot(localRoot);
        if (request.Direction == FtpTransferDirection.Download &&
            fileSystemRoot != null && localRoot == fileSystemRoot)
        {
            throw new FtpConfigurationException(
                "Mirror deletion at the local filesystem root is prohibited.",
                null);
        }
    }
}

internal class FtpDirectorySynchronizationState
{
    internal FtpDirectoryTransferRequest Request;
    internal FtpDirectoryPlan Plan;
    internal FtpDirectoryPlanningState PlanningState;
    internal string LocalRoot;
    internal string RemoteRoot;
    internal List<FtpDirectoryItemResult> Items;
    internal bool Stopped;
    internal string? FailureMessage;
    internal long LogicalBytes;
    internal HashSet<string> RemovedDestinations;

    internal FtpDirectorySynchronizationState()
    {
        Request = new FtpDirectoryTransferRequest();
        Plan = new FtpDirectoryPlan();
        PlanningState = new FtpDirectoryPlanningState();
        LocalRoot = "";
        RemoteRoot = "";
        Items = new List<FtpDirectoryItemResult>();
        Stopped = false;
        FailureMessage = null;
        LogicalBytes = 0;
        RemovedDestinations = new HashSet<string>(StringComparer.Ordinal);
    }
}
