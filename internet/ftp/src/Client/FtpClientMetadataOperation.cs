using System;
using System.Collections.Generic;

namespace ftp;

internal class FtpClientMetadataOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;

    internal FtpClientMetadataOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for metadata operations.",
                null);
        }
        ClientState = clientState;
        Options = clientState.Options;
    }

    internal FtpPathProbeResult RetrievePreferred(string path)
    {
        ValidatePath(path);
        FtpMetadataProbeState state = new FtpMetadataProbeState();
        TryMlst(state, path);
        if (!state.ExistsKnown)
        {
            ContinueExistenceProbe(state, path);
        }
        return CreateProbeResult(state);
    }

    internal FtpPathProbeResult RetrieveSize(string path)
    {
        ValidatePath(path);
        FtpMetadataProbeState state = new FtpMetadataProbeState();
        bool attempted = CanUseSize();
        if (attempted)
        {
            TrySize(state, path);
            if (state.Entry != null && state.Entry.HasSize)
            {
                return CreateProbeResult(state);
            }
        }

        FtpClientQuirkPolicy policy = new FtpClientQuirkPolicy(Options.Quirks);
        FtpReply? failedReply = GetLatestReply(state);
        if ((!attempted || failedReply != null) &&
            (!Options.Quirks.AllowSizeFallback ||
                (attempted && !policy.ShouldFallbackFromSize(failedReply))))
        {
            return CreateUnavailableResult(
                state, "FTP SIZE fallback is disabled or unavailable by feature policy.");
        }

        TryMlst(state, path);
        if (state.Entry != null &&
            (state.Entry.HasSize || state.Entry.Type == FtpEntryType.Directory))
        {
            return CreateProbeResult(state);
        }
        TryList(state, path);
        if (state.Entry != null && state.Entry.HasSize)
        {
            return CreateProbeResult(state);
        }
        if (!state.ExistsKnown)
        {
            TryMdtm(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryDirectory(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryNames(state, path);
        }
        return CreateProbeResult(state);
    }

    internal FtpPathProbeResult RetrieveModification(string path)
    {
        ValidatePath(path);
        FtpMetadataProbeState state = new FtpMetadataProbeState();
        bool attempted = CanUseMdtm();
        if (attempted)
        {
            TryMdtm(state, path);
            if (state.Entry != null && state.Entry.Modified.HasValue)
            {
                return CreateProbeResult(state);
            }
        }

        FtpClientQuirkPolicy policy = new FtpClientQuirkPolicy(Options.Quirks);
        FtpReply? failedReply = GetLatestReply(state);
        if ((!attempted || failedReply != null) &&
            (!Options.Quirks.AllowMdtmFallback ||
                (attempted && !policy.ShouldFallbackFromMdtm(failedReply))))
        {
            return CreateUnavailableResult(
                state, "FTP MDTM fallback is disabled or unavailable by feature policy.");
        }

        TryMlst(state, path);
        if (state.Entry != null &&
            (state.Entry.Modified.HasValue ||
                state.Entry.Type == FtpEntryType.Directory))
        {
            return CreateProbeResult(state);
        }
        TryList(state, path);
        if (state.Entry != null && state.Entry.Modified.HasValue)
        {
            return CreateProbeResult(state);
        }
        if (!state.ExistsKnown)
        {
            TrySize(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryDirectory(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryNames(state, path);
        }
        return CreateProbeResult(state);
    }

    internal FtpPathProbeResult Probe(string path)
    {
        ValidatePath(path);
        FtpMetadataProbeState state = new FtpMetadataProbeState();
        TryMlst(state, path);
        if (!state.ExistsKnown)
        {
            ContinueExistenceProbe(state, path);
        }
        return CreateProbeResult(state);
    }

    internal void ContinueExistenceProbe(
        FtpMetadataProbeState state,
        string path)
    {
        TrySize(state, path);
        if (!state.ExistsKnown)
        {
            TryMdtm(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryDirectory(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryList(state, path);
        }
        if (!state.ExistsKnown)
        {
            TryNames(state, path);
        }
    }

    internal void TryMlst(FtpMetadataProbeState state, string path)
    {
        if (state.ExistsKnown || !CanUseMlst())
        {
            return;
        }
        FtpCommandResult result = ExecuteCommand(FtpCommandKind.Mlst, path);
        RecordCommand(state, result, "MLST");
        if (!result.Succeeded || result.FinalReply == null)
        {
            return;
        }

        FtpEntry entry = FtpMlsxCodec.ParseControlReply(result.FinalReply);
        entry.FullPath = path;
        MergeEntry(state, entry, path);
    }

    internal void TrySize(FtpMetadataProbeState state, string path)
    {
        if (state.ExistsKnown || !CanUseSize())
        {
            return;
        }
        FtpCommandResult result = ExecuteCommand(FtpCommandKind.Size, path);
        RecordCommand(state, result, "SIZE");
        if (!result.Succeeded || result.FinalReply == null)
        {
            return;
        }

        FtpEntry entry = CreatePathEntry(path);
        entry.Type = FtpEntryType.File;
        entry.HasSize = true;
        entry.Size = FtpReplyExtractor.ExtractSize(result.FinalReply);
        MergeEntry(state, entry, path);
    }

    internal void TryMdtm(FtpMetadataProbeState state, string path)
    {
        if (state.ExistsKnown || !CanUseMdtm())
        {
            return;
        }
        FtpCommandResult result = ExecuteCommand(FtpCommandKind.Mdtm, path);
        RecordCommand(state, result, "MDTM");
        if (!result.Succeeded || result.FinalReply == null)
        {
            return;
        }

        FtpEntry entry = CreatePathEntry(path);
        entry.Type = FtpEntryType.File;
        entry.Modified = FtpReplyExtractor.ExtractModificationTime(result.FinalReply);
        MergeEntry(state, entry, path);
    }

    internal void TryDirectory(FtpMetadataProbeState state, string path)
    {
        if (state.ExistsKnown)
        {
            return;
        }

        FtpCommandResult pwd = ExecuteCommand(FtpCommandKind.Pwd, null);
        RecordCommand(state, pwd, "PWD");
        if (!pwd.Succeeded || pwd.FinalReply == null)
        {
            return;
        }
        string originalDirectory = FtpReplyExtractor.ExtractCreatedPath(pwd.FinalReply);

        FtpCommandResult change = ExecuteCommand(FtpCommandKind.Cwd, path);
        RecordCommand(state, change, "CWD");
        if (!change.Succeeded)
        {
            return;
        }

        FtpFaultIsolationResult restoration = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ExecuteCommand(FtpCommandKind.Cwd, originalDirectory);
        });
        FtpCommandResult? restored = restoration.Value as FtpCommandResult;
        if (restored != null)
        {
            RecordCommand(state, restored, "CWD restore");
        }
        if (!restoration.Succeeded || restored == null || !restored.Succeeded)
        {
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                ClientState.CloseSession();
                return null;
            });
            if (restoration.Failure != null)
            {
                throw restoration.Failure;
            }
            throw new FtpConnectionException(
                "The FTP directory probe could not restore its working directory.",
                null);
        }

        FtpEntry entry = CreatePathEntry(path);
        entry.Type = FtpEntryType.Directory;
        MergeEntry(state, entry, path);
    }

    internal void TryList(FtpMetadataProbeState state, string path)
    {
        FtpListing listing =
            new FtpClientListingOperation(ClientState).RetrieveHuman(path);
        if (listing.CommandResult != null)
        {
            RecordCommand(state, listing.CommandResult, "LIST");
        }
        if (!listing.Complete)
        {
            return;
        }
        FtpEntry? match = FindMatchingEntry(listing, path);
        if (match != null)
        {
            match.FullPath = path;
            MergeEntry(state, match, path);
        }
    }

    internal void TryNames(FtpMetadataProbeState state, string path)
    {
        if (state.ExistsKnown)
        {
            return;
        }
        FtpListing listing =
            new FtpClientListingOperation(ClientState).RetrieveNames(path);
        if (listing.CommandResult != null)
        {
            RecordCommand(state, listing.CommandResult, "NLST");
        }
        if (!listing.Complete)
        {
            return;
        }
        FtpEntry? match = FindMatchingEntry(listing, path);
        if (match != null)
        {
            match.FullPath = path;
            MergeEntry(state, match, path);
        }
    }

    internal FtpCommandResult ExecuteCommand(
        FtpCommandKind commandKind,
        string? argument)
    {
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(commandKind, argument));
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        return result;
    }

    internal void RecordCommand(
        FtpMetadataProbeState state,
        FtpCommandResult result,
        string commandName)
    {
        state.Commands.Add(result);
        if (result.Succeeded)
        {
            return;
        }
        if (result.FinalReply != null &&
            result.FinalReply.Code == FtpReplyCodes.RequestedActionNotTaken)
        {
            state.SawMissingReply = true;
        }
        state.FailureMessage = FtpClientListingOperation.CreateFailureMessage(
            result, commandName);
    }

    internal void MergeEntry(
        FtpMetadataProbeState state,
        FtpEntry source,
        string path)
    {
        FtpEntry? current = state.Entry;
        if (current != null)
        {
            if (!source.HasSize && current.HasSize)
            {
                source.HasSize = true;
                source.Size = current.Size;
            }
            if (!source.Modified.HasValue && current.Modified.HasValue)
            {
                source.Modified = current.Modified;
            }
            if (source.Type == FtpEntryType.Unknown)
            {
                source.Type = current.Type;
            }
        }
        source.FullPath = path;
        state.Entry = source;
        state.ExistsKnown = true;
    }

    internal FtpEntry? FindMatchingEntry(FtpListing listing, string path)
    {
        string leaf = GetLeafName(path);
        int index = 0;
        while (index < listing.Entries.Length)
        {
            FtpEntry entry = listing.Entries[index];
            if (string.Equals(entry.Name, path, StringComparison.Ordinal) ||
                string.Equals(entry.Name, leaf, StringComparison.Ordinal))
            {
                return entry;
            }
            index++;
        }
        return null;
    }

    internal FtpPathProbeResult CreateProbeResult(FtpMetadataProbeState state)
    {
        FtpPathProbeResult result = new FtpPathProbeResult();
        result.Commands = state.Commands.ToArray();
        if (state.ExistsKnown && state.Entry != null)
        {
            result.Succeeded = true;
            result.Exists = true;
            result.Entry = state.Entry;
            return result;
        }
        if (state.SawMissingReply)
        {
            result.Succeeded = true;
            result.Exists = false;
            return result;
        }
        result.FailureMessage = state.FailureMessage == null ?
            "No safe FTP metadata probe established the path state." :
            state.FailureMessage;
        return result;
    }

    internal FtpPathProbeResult CreateUnavailableResult(
        FtpMetadataProbeState state,
        string failureMessage)
    {
        FtpPathProbeResult result = new FtpPathProbeResult();
        result.Commands = state.Commands.ToArray();
        result.FailureMessage = state.FailureMessage == null ?
            failureMessage : state.FailureMessage;
        return result;
    }

    internal bool CanUseMlst()
    {
        return !ClientState.Features.Negotiated ||
            ClientState.FeatureState.MlstAdvertised;
    }

    internal bool CanUseSize()
    {
        return !ClientState.Features.Negotiated ||
            ClientState.FeatureState.FileSize;
    }

    internal bool CanUseMdtm()
    {
        return !ClientState.Features.Negotiated ||
            ClientState.FeatureState.ModificationTimeQuery;
    }

    internal static FtpReply? GetLatestReply(FtpMetadataProbeState state)
    {
        if (state.Commands.Count == 0)
        {
            return null;
        }
        return state.Commands[state.Commands.Count - 1].FinalReply;
    }

    internal static FtpEntry CreatePathEntry(string path)
    {
        FtpEntry entry = new FtpEntry();
        entry.Name = GetLeafName(path);
        entry.FullPath = path;
        entry.Hidden = FtpListingParsing.IsHiddenName(entry.Name);
        return entry;
    }

    internal static string GetLeafName(string path)
    {
        int end = path.Length;
        while (end > 1 && path[end - 1] == '/')
        {
            end--;
        }
        int separator = path.LastIndexOf('/', end - 1);
        if (separator < 0)
        {
            return path.Substring(0, end);
        }
        if (separator == 0 && end == 1)
        {
            return "/";
        }
        return path.Substring(separator + 1, end - separator - 1);
    }

    internal static void ValidatePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpConfigurationException(
                "A valid remote metadata path is required.", null);
        }
    }
}

internal class FtpMetadataProbeState
{
    internal List<FtpCommandResult> Commands;
    internal FtpEntry? Entry;
    internal bool ExistsKnown;
    internal bool SawMissingReply;
    internal string? FailureMessage;

    internal FtpMetadataProbeState()
    {
        Commands = new List<FtpCommandResult>();
        Entry = null;
        ExistsKnown = false;
        SawMissingReply = false;
        FailureMessage = null;
    }
}
