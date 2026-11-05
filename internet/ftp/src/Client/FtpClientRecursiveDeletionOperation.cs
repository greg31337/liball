using System;
using System.Collections.Generic;

namespace ftp;

internal class FtpClientRecursiveDeletionOperation
{
    internal FtpClientState ClientState;

    internal FtpClientRecursiveDeletionOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for recursive deletion.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpDirectoryTransferResult DeleteTree(
        FtpDirectoryTransferRequest? request)
    {
        FtpDirectoryTransferRequest checkedRequest = ValidateRequest(request);
        ClientState.RequireOpenSession("DeleteDirectoryTree");
        string rootPath = FtpPathCodec.ResolveWorkingPath(
            ClientState.Session.State.CurrentDirectory,
            checkedRequest.RemotePath);
        if (rootPath == "/")
        {
            throw new FtpConfigurationException(
                "Recursive deletion of the FTP root directory is prohibited.", null);
        }

        FtpRecursiveDeletionState state = new FtpRecursiveDeletionState();
        state.Request = checkedRequest;
        state.RootPath = rootPath;
        bool traversed = DeleteDirectory(
            state, rootPath, 0, rootPath, true);
        return CreateResult(state, traversed);
    }

    internal bool DeleteDirectory(
        FtpRecursiveDeletionState state,
        string path,
        int depth,
        string identity,
        bool removeAsDirectory)
    {
        FtpEntryType containerType = removeAsDirectory ?
            FtpEntryType.Directory : FtpEntryType.SymbolicLink;
        if (state.Stopped)
        {
            return false;
        }
        if (depth > state.Request.Options.MaximumDepth)
        {
            AddFailureItem(
                state, path, containerType,
                "The recursive deletion maximum depth was exceeded.", null);
            return false;
        }
        if (state.ActiveDirectories.Contains(identity))
        {
            AddFailureItem(
                state, path, containerType,
                "A symbolic-link or directory cycle was detected.", null);
            return false;
        }
        state.ActiveDirectories.Add(identity);

        FtpListing listing =
            new FtpClientListingOperation(ClientState).RetrieveAdaptive(path);
        if (!listing.Complete)
        {
            AddFailureItem(
                state, path, containerType,
                listing.FailureMessage == null ?
                    "The remote directory could not be listed." :
                    listing.FailureMessage,
                listing.CommandResult);
            state.ActiveDirectories.Remove(identity);
            return false;
        }

        FtpEntry[] entries = SortEntries(listing.Entries);
        bool childrenSucceeded = true;
        int index = 0;
        while (index < entries.Length && !state.Stopped)
        {
            FtpEntry entry = entries[index];
            string childPath = ResolveChildPath(path, entry.Name);
            if (!IsContainedPath(childPath, state.RootPath) || childPath == path)
            {
                AddFailureItem(
                    state, childPath, entry.Type,
                    "A listed entry escaped or repeated its deletion directory.",
                    listing.CommandResult);
                childrenSucceeded = false;
            }
            else
            {
                bool childSucceeded = DeleteEntry(
                    state, entry, childPath, depth + 1);
                if (!childSucceeded)
                {
                    childrenSucceeded = false;
                }
            }
            index++;
        }

        state.ActiveDirectories.Remove(identity);
        if (state.Stopped || !childrenSucceeded)
        {
            AddBlockedDirectoryItem(state, path, removeAsDirectory);
            return false;
        }

        if (state.Request.Options.DryRun)
        {
            AddDryRunItem(state, path, containerType);
            return true;
        }

        FtpClientFileSystemOperation fileSystem =
            new FtpClientFileSystemOperation(ClientState);
        FtpCommandResult command = removeAsDirectory ?
            fileSystem.RemoveDirectory(path) : fileSystem.DeleteFile(path);
        return AddCommandItem(state, path, containerType, command);
    }

    internal bool DeleteEntry(
        FtpRecursiveDeletionState state,
        FtpEntry entry,
        string path,
        int depth)
    {
        if (entry.Type == FtpEntryType.Directory)
        {
            string identity = entry.Unique == null ?
                path : "unique:" + entry.Unique;
            return DeleteDirectory(state, path, depth, identity, true);
        }
        if (entry.Type == FtpEntryType.SymbolicLink)
        {
            return DeleteSymbolicLink(state, entry, path, depth);
        }
        if (entry.Type != FtpEntryType.File)
        {
            AddFailureItem(
                state, path, entry.Type,
                "The listed entry type is unsafe for recursive deletion.", null);
            return false;
        }
        if (state.Request.Options.DryRun)
        {
            AddDryRunItem(state, path, FtpEntryType.File);
            return true;
        }

        FtpCommandResult command =
            new FtpClientFileSystemOperation(ClientState).DeleteFile(path);
        return AddCommandItem(state, path, FtpEntryType.File, command);
    }

    internal bool DeleteSymbolicLink(
        FtpRecursiveDeletionState state,
        FtpEntry entry,
        string path,
        int depth)
    {
        FtpSymlinkPolicy policy = state.Request.Options.SymlinkPolicy;
        if (policy == FtpSymlinkPolicy.Skip)
        {
            AddSkippedItem(state, path, "The symbolic link was skipped by policy.");
            return false;
        }
        if (policy == FtpSymlinkPolicy.Fail)
        {
            AddFailureItem(
                state, path, FtpEntryType.SymbolicLink,
                "The symbolic-link policy rejects recursive deletion.", null);
            return false;
        }
        if (policy == FtpSymlinkPolicy.Follow)
        {
            if (string.IsNullOrWhiteSpace(entry.LinkTarget))
            {
                AddFailureItem(
                    state, path, FtpEntryType.SymbolicLink,
                    "The symbolic-link target is unavailable.", null);
                return false;
            }
            string target = FtpPathCodec.ResolveWorkingPath(
                GetParentPath(path), entry.LinkTarget);
            if (!IsContainedPath(target, state.RootPath))
            {
                AddFailureItem(
                    state, path, FtpEntryType.SymbolicLink,
                    "The symbolic-link target escapes the deletion root.", null);
                return false;
            }
            return DeleteDirectory(
                state, path, depth, target, false);
        }

        if (state.Request.Options.DryRun)
        {
            AddDryRunItem(state, path, FtpEntryType.SymbolicLink);
            return true;
        }
        FtpCommandResult command =
            new FtpClientFileSystemOperation(ClientState).DeleteFile(path);
        return AddCommandItem(state, path, FtpEntryType.SymbolicLink, command);
    }

    internal bool AddCommandItem(
        FtpRecursiveDeletionState state,
        string path,
        FtpEntryType type,
        FtpCommandResult command)
    {
        FtpDirectoryItemResult? item = CreateItem(state, path, type);
        if (item == null)
        {
            return false;
        }
        item.Executed = true;
        item.CommandResult = command;
        item.Succeeded = command.Succeeded;
        item.Deleted = command.Succeeded;
        if (!command.Succeeded)
        {
            item.Message = FtpClientListingOperation.CreateFailureMessage(
                command, command.Command.Verb);
            RecordFailure(state, item.Message);
        }
        state.Items.Add(item);
        return command.Succeeded;
    }

    internal void AddFailureItem(
        FtpRecursiveDeletionState state,
        string path,
        FtpEntryType type,
        string message,
        FtpCommandResult? command)
    {
        FtpDirectoryItemResult? item = CreateItem(state, path, type);
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
        FtpRecursiveDeletionState state,
        string path,
        string message)
    {
        FtpDirectoryItemResult? item =
            CreateItem(state, path, FtpEntryType.SymbolicLink);
        if (item == null)
        {
            return;
        }
        item.Skipped = true;
        item.Message = message;
        state.Items.Add(item);
        if (state.FailureMessage == null)
        {
            state.FailureMessage = message;
        }
    }

    internal void AddDryRunItem(
        FtpRecursiveDeletionState state,
        string path,
        FtpEntryType type)
    {
        FtpDirectoryItemResult? item = CreateItem(state, path, type);
        if (item != null)
        {
            item.Message = "Deletion was planned without execution.";
            state.Items.Add(item);
        }
    }

    internal void AddBlockedDirectoryItem(
        FtpRecursiveDeletionState state,
        string path,
        bool directory)
    {
        FtpEntryType type = directory ?
            FtpEntryType.Directory : FtpEntryType.SymbolicLink;
        FtpDirectoryItemResult? item = CreateItem(state, path, type);
        if (item == null)
        {
            return;
        }
        item.Skipped = true;
        item.Message = "Deletion was not attempted because a child was not deleted.";
        state.Items.Add(item);
    }

    internal FtpDirectoryItemResult? CreateItem(
        FtpRecursiveDeletionState state,
        string path,
        FtpEntryType type)
    {
        if (state.Items.Count >= state.Request.Options.MaximumEntryCount)
        {
            RecordFailure(
                state, "The recursive deletion maximum entry count was exceeded.");
            state.Stopped = true;
            return null;
        }
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();
        item.RemotePath = path;
        item.Direction = state.Request.Direction;
        item.EntryType = type;
        item.Planned = true;
        return item;
    }

    internal void RecordFailure(
        FtpRecursiveDeletionState state,
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

    internal FtpDirectoryTransferResult CreateResult(
        FtpRecursiveDeletionState state,
        bool traversed)
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

        int index = 0;
        while (index < result.Items.Length)
        {
            FtpDirectoryItemResult item = result.Items[index];
            if (item.Succeeded)
            {
                result.SucceededItems++;
            }
            if (item.Executed && !item.Succeeded)
            {
                result.FailedItems++;
            }
            else if (!item.Succeeded && !item.Skipped &&
                item.Message != null && !state.Request.Options.DryRun)
            {
                result.FailedItems++;
            }
            if (item.Skipped)
            {
                result.SkippedItems++;
            }
            if (item.Deleted)
            {
                result.DeletedItems++;
            }
            index++;
        }
        result.Succeeded = state.Request.Options.DryRun ?
            traversed && state.FailureMessage == null :
            traversed && result.DeletedItems == result.TotalItems;
        return result;
    }

    internal static FtpEntry[] SortEntries(FtpEntry[] entries)
    {
        FtpEntry[] sorted = new FtpEntry[entries.Length];
        Array.Copy(entries, sorted, entries.Length);
        int index = 1;
        while (index < sorted.Length)
        {
            FtpEntry value = sorted[index];
            int target = index;
            while (target > 0 && string.CompareOrdinal(
                sorted[target - 1].Name, value.Name) > 0)
            {
                sorted[target] = sorted[target - 1];
                target--;
            }
            sorted[target] = value;
            index++;
        }
        return sorted;
    }

    internal static string ResolveChildPath(string parent, string name)
    {
        return FtpPathCodec.ResolveWorkingPath(parent, name);
    }

    internal static string GetParentPath(string path)
    {
        int separator = path.LastIndexOf('/');
        if (separator <= 0)
        {
            return "/";
        }
        return path.Substring(0, separator);
    }

    internal static bool IsContainedPath(string path, string root)
    {
        if (path == root)
        {
            return true;
        }
        if (root == "/")
        {
            return path.StartsWith("/", StringComparison.Ordinal);
        }
        return path.StartsWith(root + "/", StringComparison.Ordinal);
    }

    internal static FtpDirectoryTransferRequest ValidateRequest(
        FtpDirectoryTransferRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A recursive deletion request is required.", null);
        }
        FtpTransferValidator.ValidateRequiredPath(
            request.RemotePath, "remote deletion root");
        FtpTransferValidator.ValidateDirection(request.Direction);
        FtpTransferValidator.ValidateDirectoryTransferOptions(request.Options);
        if (!request.Options.AllowRecursiveDeletion)
        {
            throw new FtpConfigurationException(
                "Recursive deletion requires explicit enablement.", null);
        }
        return request;
    }
}

internal class FtpRecursiveDeletionState
{
    internal FtpDirectoryTransferRequest Request;
    internal string RootPath;
    internal List<FtpDirectoryItemResult> Items;
    internal HashSet<string> ActiveDirectories;
    internal bool Stopped;
    internal string? FailureMessage;

    internal FtpRecursiveDeletionState()
    {
        Request = new FtpDirectoryTransferRequest();
        RootPath = "";
        Items = new List<FtpDirectoryItemResult>();
        ActiveDirectories = new HashSet<string>(StringComparer.Ordinal);
        Stopped = false;
        FailureMessage = null;
    }
}
