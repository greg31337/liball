using System;
using System.Collections.Generic;
using System.IO;

namespace ftp;

internal class FtpClientDirectoryPlanningOperation
{
    internal FtpClientState ClientState;
    internal FtpDirectoryPlanningState LatestState;

    internal FtpClientDirectoryPlanningOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for directory planning.",
                null);
        }
        ClientState = clientState;
        LatestState = new FtpDirectoryPlanningState();
    }

    internal FtpDirectoryPlan Plan(FtpDirectoryTransferRequest? request)
    {
        FtpTransferValidator.ValidateDirectoryTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A directory-transfer planning request is required.", null);
        }
        ClientState.RequireOpenSession("PlanDirectoryTransfer");

        FtpDirectoryPlanningState state = new FtpDirectoryPlanningState();
        LatestState = state;
        state.Request = request;
        state.LocalRoot = Path.GetFullPath(request.LocalPath);
        state.RemoteRoot = FtpPathCodec.ResolveWorkingPath(
            ClientState.Session.State.CurrentDirectory, request.RemotePath);

        FtpDirectorySnapshot local = BuildLocalSnapshot(state);
        FtpDirectorySnapshot remote = BuildRemoteSnapshot(state);
        state.LocalSnapshot = local;
        state.RemoteSnapshot = remote;
        if (!state.Stopped)
        {
            CompareSnapshots(state, local, remote);
        }

        FtpDirectoryPlan plan = new FtpDirectoryPlan();
        plan.Request = request;
        plan.Items = state.Items.ToArray();
        plan.Complete = state.Complete && !state.Stopped;
        plan.FailureMessage = state.FailureMessage;
        return plan;
    }

    internal FtpDirectoryTransferResult CreateDryRunResult(
        FtpDirectoryPlan plan)
    {
        FtpDirectoryTransferResult result = new FtpDirectoryTransferResult();
        result.Request = plan.Request;
        result.Plan = plan;
        result.Items = plan.Items;
        result.TotalItems = plan.Items.Length;
        result.Succeeded = plan.Complete;
        result.FailureMessage = plan.FailureMessage;
        int index = 0;
        while (index < plan.Items.Length)
        {
            FtpDirectoryItemResult item = plan.Items[index];
            if (item.Skipped)
            {
                result.SkippedItems++;
            }
            else if (!item.Planned)
            {
                result.FailedItems++;
            }
            index++;
        }
        return result;
    }

    internal FtpDirectorySnapshot BuildLocalSnapshot(
        FtpDirectoryPlanningState state)
    {
        FtpDirectorySnapshot snapshot = new FtpDirectorySnapshot();
        bool exists = Directory.Exists(state.LocalRoot) ||
            File.Exists(state.LocalRoot);
        if (!exists)
        {
            if (state.Request.Direction == FtpTransferDirection.Upload)
            {
                throw new FtpConfigurationException(
                    "A directory upload plan requires an existing local source directory.",
                    null);
            }
            return snapshot;
        }

        FtpFaultIsolationResult inspection =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpClientDirectoryUploadOperation.CreateLocalEntry(
                    state.LocalRoot);
            });
        FtpEntry? root = inspection.Value as FtpEntry;
        if (!inspection.Succeeded || root == null)
        {
            RecordFailure(
                state, "The local directory root could not be inspected.");
            return snapshot;
        }
        if (state.Request.Direction == FtpTransferDirection.Upload &&
            root.Type != FtpEntryType.Directory)
        {
            throw new FtpConfigurationException(
                "A directory upload plan requires a non-symbolic local directory root.",
                null);
        }

        AddSnapshotEntry(
            state, snapshot, "", state.LocalRoot, state.RemoteRoot,
            root, root, 0);
        if (root.Type == FtpEntryType.Directory && !state.Stopped)
        {
            ReadLocalChildren(
                state, snapshot, state.LocalRoot, "", 0,
                Path.GetFullPath(state.LocalRoot));
        }
        return snapshot;
    }

    internal FtpDirectorySnapshot BuildRemoteSnapshot(
        FtpDirectoryPlanningState state)
    {
        FtpDirectorySnapshot snapshot = new FtpDirectorySnapshot();
        FtpFaultIsolationResult retrieval =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return new FtpClientListingOperation(ClientState)
                    .RetrieveAdaptive(state.RemoteRoot);
            });
        FtpListing? listing = retrieval.Value as FtpListing;
        if (!retrieval.Succeeded || listing == null)
        {
            RecordFailure(
                state, "The remote directory root could not be listed.");
            return snapshot;
        }
        if (!listing.Complete)
        {
            if (state.Request.Direction == FtpTransferDirection.Upload &&
                IsMissingListing(listing))
            {
                return snapshot;
            }
            RecordFailure(
                state, listing.FailureMessage == null ?
                    "The remote directory root could not be listed." :
                    listing.FailureMessage);
            return snapshot;
        }

        FtpEntry root = CreateDirectoryEntry("", state.RemoteRoot);
        AddSnapshotEntry(
            state, snapshot, "", state.LocalRoot, state.RemoteRoot,
            root, root, 0);
        if (!state.Stopped)
        {
            ReadRemoteListing(
                state, snapshot, listing, state.RemoteRoot, "", 0);
        }
        return snapshot;
    }

    internal void ReadLocalChildren(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        string actualDirectory,
        string relativeDirectory,
        int depth,
        string identity)
    {
        if (state.Stopped)
        {
            return;
        }
        if (snapshot.ActiveDirectories.Contains(identity))
        {
            RecordFailure(
                state, "A local symbolic-link or directory cycle was detected.");
            return;
        }
        snapshot.ActiveDirectories.Add(identity);
        CheckCancellation(state);
        FtpFaultIsolationResult enumeration =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Directory.GetFileSystemEntries(actualDirectory);
            });
        string[]? paths = enumeration.Value as string[];
        if (!enumeration.Succeeded || paths == null)
        {
            snapshot.ActiveDirectories.Remove(identity);
            RecordFailure(
                state, "A local directory could not be enumerated for planning.");
            return;
        }
        FtpClientDirectoryUploadOperation.SortPaths(paths);

        int index = 0;
        while (index < paths.Length && !state.Stopped)
        {
            string path = Path.GetFullPath(paths[index]);
            string name = Path.GetFileName(path);
            string relative = ResolveRelativeChild(relativeDirectory, name);
            int childDepth = depth + 1;
            if (childDepth > state.Request.Options.MaximumDepth ||
                !FtpClientDirectoryUploadOperation.IsSafeName(name) ||
                !FtpClientDirectoryUploadOperation.IsContainedLocalPath(
                    path, state.LocalRoot))
            {
                RecordFailure(
                    state, "A local planning entry exceeded its traversal bounds.");
            }
            else
            {
                AddLocalSnapshotChild(
                    state, snapshot, path, relative, childDepth);
            }
            index++;
        }
        snapshot.ActiveDirectories.Remove(identity);
    }

    internal void AddLocalSnapshotChild(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        string path,
        string relative,
        int depth)
    {
        FtpFaultIsolationResult inspection =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpClientDirectoryUploadOperation.CreateLocalEntry(path);
            });
        FtpEntry? original = inspection.Value as FtpEntry;
        if (!inspection.Succeeded || original == null)
        {
            RecordFailure(
                state, "A local planning entry could not be inspected.");
            return;
        }

        FtpEntry comparison = original;
        string traversalPath = path;
        if (original.Type == FtpEntryType.SymbolicLink &&
            state.Request.Options.SymlinkPolicy == FtpSymlinkPolicy.Follow)
        {
            FtpFaultIsolationResult resolution =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return File.ResolveLinkTarget(path, true);
                });
            FileSystemInfo? target = resolution.Value as FileSystemInfo;
            if (!resolution.Succeeded || target == null)
            {
                RecordFailure(
                    state, "A local symbolic-link target could not be resolved.");
                return;
            }
            traversalPath = Path.GetFullPath(target.FullName);
            if (!FtpClientDirectoryUploadOperation.IsContainedLocalPath(
                traversalPath, state.LocalRoot))
            {
                RecordFailure(
                    state, "A local symbolic-link target escaped the planning root.");
                return;
            }
            FtpFaultIsolationResult targetInspection =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return FtpClientDirectoryUploadOperation.CreateLocalEntry(
                        traversalPath);
                });
            FtpEntry? targetEntry = targetInspection.Value as FtpEntry;
            if (!targetInspection.Succeeded || targetEntry == null)
            {
                RecordFailure(
                    state, "A local symbolic-link target could not be inspected.");
                return;
            }
            comparison = targetEntry;
        }

        string localPath = ResolveLocalPath(state.LocalRoot, relative);
        string remotePath = ResolveRemotePath(state.RemoteRoot, relative);
        AddSnapshotEntry(
            state, snapshot, relative, localPath, remotePath,
            original, comparison, depth);
        if (comparison.Type == FtpEntryType.Directory && !state.Stopped)
        {
            ReadLocalChildren(
                state, snapshot, traversalPath, relative, depth,
                Path.GetFullPath(traversalPath));
        }
    }

    internal void ReadRemoteChildren(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        string actualDirectory,
        string relativeDirectory,
        int depth)
    {
        if (state.Stopped)
        {
            return;
        }
        FtpFaultIsolationResult retrieval =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return new FtpClientListingOperation(ClientState)
                    .RetrieveAdaptive(actualDirectory);
            });
        FtpListing? listing = retrieval.Value as FtpListing;
        if (!retrieval.Succeeded || listing == null || !listing.Complete)
        {
            RecordFailure(
                state, listing == null || listing.FailureMessage == null ?
                    "A remote directory could not be listed for planning." :
                    listing.FailureMessage);
            return;
        }
        ReadRemoteListing(
            state, snapshot, listing, actualDirectory,
            relativeDirectory, depth);
    }

    internal void ReadRemoteListing(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        FtpListing listing,
        string actualDirectory,
        string relativeDirectory,
        int depth)
    {
        if (snapshot.ActiveDirectories.Contains(actualDirectory))
        {
            RecordFailure(
                state, "A remote symbolic-link or directory cycle was detected.");
            return;
        }
        snapshot.ActiveDirectories.Add(actualDirectory);
        FtpEntry[] entries = listing.Entries;
        FtpClientRecursiveDeletionOperation.SortEntries(entries);
        int index = 0;
        while (index < entries.Length && !state.Stopped)
        {
            CheckCancellation(state);
            FtpEntry original = entries[index];
            string relative = ResolveRelativeChild(
                relativeDirectory, original.Name);
            string remotePath =
                FtpClientRecursiveDeletionOperation.ResolveChildPath(
                    actualDirectory, original.Name);
            int childDepth = depth + 1;
            if (childDepth > state.Request.Options.MaximumDepth ||
                !FtpClientDirectoryUploadOperation.IsSafeName(original.Name) ||
                !FtpClientRecursiveDeletionOperation.IsContainedPath(
                    remotePath, state.RemoteRoot))
            {
                RecordFailure(
                    state, "A remote planning entry exceeded its traversal bounds.");
            }
            else
            {
                AddRemoteSnapshotChild(
                    state, snapshot, original, remotePath,
                    relative, childDepth);
            }
            index++;
        }
        snapshot.ActiveDirectories.Remove(actualDirectory);
    }

    internal void AddRemoteSnapshotChild(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        FtpEntry original,
        string remotePath,
        string relative,
        int depth)
    {
        FtpEntry comparison = original;
        string traversalPath = remotePath;
        if (original.Type == FtpEntryType.SymbolicLink &&
            state.Request.Options.SymlinkPolicy == FtpSymlinkPolicy.Follow)
        {
            if (string.IsNullOrWhiteSpace(original.LinkTarget))
            {
                RecordFailure(
                    state, "A remote symbolic-link target was unavailable.");
                return;
            }
            traversalPath = FtpPathCodec.ResolveWorkingPath(
                FtpClientRecursiveDeletionOperation.GetParentPath(remotePath),
                original.LinkTarget);
            if (!FtpClientRecursiveDeletionOperation.IsContainedPath(
                traversalPath, state.RemoteRoot))
            {
                RecordFailure(
                    state, "A remote symbolic-link target escaped the planning root.");
                return;
            }
            FtpFaultIsolationResult probeOperation =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return new FtpClientMetadataOperation(ClientState)
                        .Probe(traversalPath);
                });
            FtpPathProbeResult? probe =
                probeOperation.Value as FtpPathProbeResult;
            if (!probeOperation.Succeeded || probe == null ||
                !probe.Succeeded || !probe.Exists || probe.Entry == null)
            {
                RecordFailure(
                    state, "A remote symbolic-link target could not be inspected.");
                return;
            }
            comparison = probe.Entry;
        }

        string localPath = ResolveLocalPath(state.LocalRoot, relative);
        string logicalRemotePath = ResolveRemotePath(
            state.RemoteRoot, relative);
        AddSnapshotEntry(
            state, snapshot, relative, localPath, logicalRemotePath,
            original, comparison, depth);
        if (comparison.Type == FtpEntryType.Directory && !state.Stopped)
        {
            ReadRemoteChildren(
                state, snapshot, traversalPath, relative, depth);
        }
    }

    internal void CompareSnapshots(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot local,
        FtpDirectorySnapshot remote)
    {
        FtpDirectorySnapshot source =
            state.Request.Direction == FtpTransferDirection.Upload ?
                local : remote;
        FtpDirectorySnapshot destination =
            state.Request.Direction == FtpTransferDirection.Upload ?
                remote : local;
        string? excludedDirectory = null;
        int index = 0;
        while (index < source.Entries.Count && !state.Stopped)
        {
            FtpDirectorySnapshotEntry sourceEntry = source.Entries[index];
            if (excludedDirectory != null && IsRelativeChild(
                sourceEntry.RelativePath, excludedDirectory))
            {
                index++;
                continue;
            }
            excludedDirectory = null;
            FtpDirectorySnapshotEntry? destinationEntry = null;
            if (destination.Index.ContainsKey(sourceEntry.RelativePath))
            {
                destinationEntry = destination.Index[sourceEntry.RelativePath];
            }
            bool included = sourceEntry.RelativePath.Length == 0 ||
                IncludeEntry(state, sourceEntry, false);
            if (!included)
            {
                if (state.FilterFailed)
                {
                    state.FilterFailed = false;
                }
                else
                {
                    AddSkippedPlanItem(
                        state, sourceEntry,
                        "The entry was excluded by the recursive transfer filter.");
                    if (sourceEntry.Comparison.Type == FtpEntryType.Directory)
                    {
                        excludedDirectory = sourceEntry.RelativePath;
                    }
                }
            }
            else
            {
                CompareSourceEntry(
                    state, sourceEntry, destinationEntry);
            }
            index++;
        }

        if (state.Request.Options.SyncMode == FtpDirectorySyncMode.Mirror &&
            state.Request.Options.DeleteExtraneous && !state.Stopped)
        {
            AddDeletionPlans(state, source, destination);
        }
    }

    internal void CompareSourceEntry(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry source,
        FtpDirectorySnapshotEntry? destination)
    {
        FtpEntryType type = source.Comparison.Type;
        if (source.Original.Type == FtpEntryType.SymbolicLink)
        {
            FtpSymlinkPolicy policy = state.Request.Options.SymlinkPolicy;
            if (policy == FtpSymlinkPolicy.Skip)
            {
                AddSkippedPlanItem(
                    state, source,
                    "The symbolic link would be skipped by policy.");
                return;
            }
            if (policy == FtpSymlinkPolicy.Fail ||
                (policy == FtpSymlinkPolicy.Preserve &&
                    state.Request.Direction == FtpTransferDirection.Upload))
            {
                AddFailedPlanItem(
                    state, source,
                    policy == FtpSymlinkPolicy.Fail ?
                        "The symbolic-link policy rejects this planned transfer." :
                        "FTP has no negotiated portable operation for preserving a symbolic link.");
                return;
            }
        }

        if (type != FtpEntryType.Directory &&
            type != FtpEntryType.File &&
            type != FtpEntryType.SymbolicLink)
        {
            AddFailedPlanItem(
                state, source,
                "The entry type cannot be included in a directory plan.");
            return;
        }

        if (type == FtpEntryType.Directory)
        {
            if (!state.Request.Options.PreserveEmptyDirectories &&
                !HasSnapshotChild(source, state))
            {
                AddSkippedPlanItem(
                    state, source,
                    "The empty directory would not be created by policy.");
                return;
            }
            if (destination != null &&
                destination.Comparison.Type == FtpEntryType.Directory)
            {
                AddSkippedPlanItem(
                    state, source,
                    "The destination directory already exists.");
                return;
            }
            if (destination != null &&
                state.Request.Options.Transfer.CollisionPolicy ==
                    FtpCollisionPolicy.Skip)
            {
                AddSkippedPlanItem(
                    state, source,
                    "The destination entry would be skipped by collision policy.");
                return;
            }
            AddActionPlanItem(
                state, source,
                "The destination directory would be created.");
            return;
        }

        bool destinationExists = destination != null;
        bool same = destination != null && EntriesMatch(source, destination);
        if (destinationExists &&
            state.Request.Options.Transfer.CollisionPolicy ==
                FtpCollisionPolicy.Skip)
        {
            AddSkippedPlanItem(
                state, source,
                "The destination entry would be skipped by collision policy.");
            return;
        }
        if (same && state.Request.Options.SyncMode != FtpDirectorySyncMode.Copy)
        {
            AddSkippedPlanItem(
                state, source,
                "The source and destination entries are unchanged.");
            return;
        }
        AddActionPlanItem(
            state, source,
            destinationExists ?
                "The changed destination entry would be replaced." :
                state.Request.Direction == FtpTransferDirection.Upload ?
                    "The file would be uploaded." :
                    "The file would be downloaded.");
    }

    internal void AddDeletionPlans(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot source,
        FtpDirectorySnapshot destination)
    {
        int index = destination.Entries.Count - 1;
        while (index >= 0 && !state.Stopped)
        {
            FtpDirectorySnapshotEntry entry = destination.Entries[index];
            if (entry.RelativePath.Length != 0 &&
                !source.Index.ContainsKey(entry.RelativePath))
            {
                if (IncludeEntry(state, entry, true))
                {
                    FtpDirectoryItemResult? item = CreatePlanItem(state, entry);
                    if (item != null)
                    {
                        if (entry.Original.Type ==
                            FtpEntryType.SymbolicLink)
                        {
                            item.EntryType = FtpEntryType.SymbolicLink;
                        }
                        item.Deleted = true;
                        item.Message =
                            "The destination-only entry would be deleted.";
                        state.Items.Add(item);
                    }
                }
                else
                {
                    if (state.FilterFailed)
                    {
                        state.FilterFailed = false;
                    }
                    else
                    {
                        AddSkippedPlanItem(
                            state, entry,
                            "The destination-only entry was excluded from deletion by the filter.");
                    }
                }
            }
            index--;
        }
    }

    internal bool IncludeEntry(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry entry,
        bool deletion)
    {
        IFtpTransferFilter? filter = state.Request.Options.Filter;
        if (filter == null)
        {
            return true;
        }
        FtpTransferFilterRequest request = new FtpTransferFilterRequest();
        request.Direction = state.Request.Direction;
        request.Entry = entry.Original;
        request.LocalPath = entry.LocalPath;
        request.RemotePath = entry.RemotePath;
        request.Depth = entry.Depth;
        request.Deletion = deletion;
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return filter.IncludeTransferEntry(request);
            });
        if (!operation.Succeeded || operation.Value == null)
        {
            AddFailedPlanItem(
                state, entry, "The recursive transfer filter failed.");
            state.FilterFailed = true;
            return false;
        }
        return (bool)operation.Value;
    }

    internal FtpDirectoryItemResult? CreatePlanItem(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry entry)
    {
        if (state.Items.Count >= state.Request.Options.MaximumEntryCount)
        {
            RecordFailure(
                state, "The directory plan maximum entry count was exceeded.");
            state.Stopped = true;
            return null;
        }
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();
        item.LocalPath = entry.LocalPath;
        item.RemotePath = entry.RemotePath;
        item.Direction = state.Request.Direction;
        item.EntryType = entry.Comparison.Type;
        item.Planned = true;
        return item;
    }

    internal void AddActionPlanItem(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry entry,
        string message)
    {
        FtpDirectoryItemResult? item = CreatePlanItem(state, entry);
        if (item == null)
        {
            return;
        }
        item.Message = message;
        state.Items.Add(item);
    }

    internal void AddSkippedPlanItem(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry entry,
        string message)
    {
        FtpDirectoryItemResult? item = CreatePlanItem(state, entry);
        if (item == null)
        {
            return;
        }
        item.Skipped = true;
        item.Message = message;
        state.Items.Add(item);
    }

    internal void AddFailedPlanItem(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshotEntry entry,
        string message)
    {
        FtpDirectoryItemResult? item = CreatePlanItem(state, entry);
        if (item != null)
        {
            item.Planned = false;
            item.Message = message;
            state.Items.Add(item);
        }
        RecordFailure(state, message);
    }

    internal void AddSnapshotEntry(
        FtpDirectoryPlanningState state,
        FtpDirectorySnapshot snapshot,
        string relative,
        string localPath,
        string remotePath,
        FtpEntry original,
        FtpEntry comparison,
        int depth)
    {
        if (snapshot.Entries.Count >=
            state.Request.Options.MaximumEntryCount)
        {
            RecordFailure(
                state, "A directory snapshot exceeded the maximum entry count.");
            state.Stopped = true;
            return;
        }
        if (snapshot.Index.ContainsKey(relative))
        {
            RecordFailure(
                state, "A directory snapshot contained a duplicate relative path.");
            return;
        }
        FtpDirectorySnapshotEntry item = new FtpDirectorySnapshotEntry();
        item.RelativePath = relative;
        item.LocalPath = localPath;
        item.RemotePath = remotePath;
        item.Original = original;
        item.Comparison = comparison;
        item.Depth = depth;
        snapshot.Entries.Add(item);
        snapshot.Index[relative] = item;
    }

    internal void RecordFailure(
        FtpDirectoryPlanningState state,
        string message)
    {
        state.Complete = false;
        if (state.FailureMessage == null)
        {
            state.FailureMessage = message;
        }
        if (state.Request.Options.FailurePolicy == FtpFailurePolicy.Stop)
        {
            state.Stopped = true;
        }
    }

    internal void CheckCancellation(FtpDirectoryPlanningState state)
    {
        FtpCancellation.ThrowIfCancellationRequested(
            state.Request.Options.Transfer.Timeouts.CancellationToken);
    }

    internal static bool EntriesMatch(
        FtpDirectorySnapshotEntry source,
        FtpDirectorySnapshotEntry destination)
    {
        FtpEntry first = source.Comparison;
        FtpEntry second = destination.Comparison;
        if (first.Type != second.Type)
        {
            return false;
        }
        if (first.Type == FtpEntryType.SymbolicLink)
        {
            return first.LinkTarget != null && second.LinkTarget != null &&
                first.LinkTarget == second.LinkTarget;
        }
        bool compared = false;
        bool same = true;
        if (first.HasSize && second.HasSize)
        {
            compared = true;
            same = first.Size == second.Size;
        }
        if (first.Modified.HasValue && second.Modified.HasValue)
        {
            compared = true;
            same = same && FtpClientDirectoryUploadOperation.SameTimestamp(
                first.Modified, second.Modified);
        }
        return compared && same;
    }

    internal static bool HasSnapshotChild(
        FtpDirectorySnapshotEntry directory,
        FtpDirectoryPlanningState state)
    {
        string prefix = directory.RelativePath.Length == 0 ?
            "" : directory.RelativePath + "/";
        FtpDirectorySnapshot snapshot =
            state.Request.Direction == FtpTransferDirection.Upload ?
                state.LocalSnapshot : state.RemoteSnapshot;
        int index = 0;
        while (index < snapshot.Entries.Count)
        {
            string relative = snapshot.Entries[index].RelativePath;
            if (relative.Length > prefix.Length &&
                relative.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool IsRelativeChild(string path, string parent)
    {
        if (parent.Length == 0)
        {
            return path.Length != 0;
        }
        return path.StartsWith(
            parent + "/", StringComparison.Ordinal);
    }

    internal static string ResolveRelativeChild(
        string parent,
        string name)
    {
        return parent.Length == 0 ? name : parent + "/" + name;
    }

    internal static string ResolveLocalPath(string root, string relative)
    {
        if (relative.Length == 0)
        {
            return root;
        }
        return Path.Combine(
            root, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    internal static string ResolveRemotePath(string root, string relative)
    {
        if (relative.Length == 0)
        {
            return root;
        }
        return FtpPathCodec.ResolveWorkingPath(root, relative);
    }

    internal static FtpEntry CreateDirectoryEntry(
        string name,
        string fullPath)
    {
        FtpEntry entry = new FtpEntry();
        entry.Name = name;
        entry.FullPath = fullPath;
        entry.Type = FtpEntryType.Directory;
        return entry;
    }

    internal static bool IsMissingListing(FtpListing listing)
    {
        if (listing.CommandResult == null ||
            listing.CommandResult.FinalReply == null)
        {
            return false;
        }
        int code = listing.CommandResult.FinalReply.Code;
        return code == 450 || code == 550;
    }
}

internal class FtpDirectoryPlanningState
{
    internal FtpDirectoryTransferRequest Request;
    internal string LocalRoot;
    internal string RemoteRoot;
    internal List<FtpDirectoryItemResult> Items;
    internal FtpDirectorySnapshot LocalSnapshot;
    internal FtpDirectorySnapshot RemoteSnapshot;
    internal bool Complete;
    internal bool Stopped;
    internal bool FilterFailed;
    internal string? FailureMessage;

    internal FtpDirectoryPlanningState()
    {
        Request = new FtpDirectoryTransferRequest();
        LocalRoot = "";
        RemoteRoot = "";
        Items = new List<FtpDirectoryItemResult>();
        LocalSnapshot = new FtpDirectorySnapshot();
        RemoteSnapshot = new FtpDirectorySnapshot();
        Complete = true;
        Stopped = false;
        FilterFailed = false;
        FailureMessage = null;
    }
}

internal class FtpDirectorySnapshot
{
    internal List<FtpDirectorySnapshotEntry> Entries;
    internal Dictionary<string, FtpDirectorySnapshotEntry> Index;
    internal HashSet<string> ActiveDirectories;

    internal FtpDirectorySnapshot()
    {
        Entries = new List<FtpDirectorySnapshotEntry>();
        Index = new Dictionary<string, FtpDirectorySnapshotEntry>(
            StringComparer.Ordinal);
        ActiveDirectories = new HashSet<string>(StringComparer.Ordinal);
    }
}

internal class FtpDirectorySnapshotEntry
{
    internal string RelativePath;
    internal string LocalPath;
    internal string RemotePath;
    internal FtpEntry Original;
    internal FtpEntry Comparison;
    internal int Depth;

    internal FtpDirectorySnapshotEntry()
    {
        RelativePath = "";
        LocalPath = "";
        RemotePath = "";
        Original = new FtpEntry();
        Comparison = new FtpEntry();
        Depth = 0;
    }
}
