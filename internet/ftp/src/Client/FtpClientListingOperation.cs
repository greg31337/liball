using System;

namespace ftp;

internal class FtpClientListingOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;

    internal FtpClientListingOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for client listings.",
                null);
        }
        ClientState = clientState;
        Options = clientState.Options;
    }

    internal FtpListing RetrieveAdaptive(string path)
    {
        ValidatePath(path);
        bool tryMachine = !ClientState.Features.Negotiated ||
            ClientState.FeatureState.MachineListing;
        if (tryMachine)
        {
            FtpListing machine =
                new FtpClientMachineListingOperation(ClientState).RetrieveDirectory(path);
            if (machine.Complete || machine.CommandResult == null ||
                !new FtpClientQuirkPolicy(Options.Quirks).ShouldFallbackFromMlsd(
                    machine.CommandResult.FinalReply))
            {
                return machine;
            }
        }
        return RetrieveHuman(path);
    }

    internal FtpListing RetrieveHuman(string path)
    {
        ValidatePath(path);
        FtpClientListingTransferResult transfer =
            new FtpClientListingDataTransfer(ClientState).ReceiveListing(
                FtpCommandKind.List, path);
        if (!transfer.CommandResult.Succeeded)
        {
            return CreateFailedListing(transfer.CommandResult, "LIST", "Raw");
        }

        string[] lines = FtpListingTextCodec.ParseLines(
            transfer.DataText, Options.StrictReplyParsing);
        FtpListing listing = FtpHumanListingParser.ParseListing(lines);
        listing.CommandResult = transfer.CommandResult;
        FilterPseudoEntries(listing);
        return listing;
    }

    internal FtpListing RetrieveNames(string path)
    {
        ValidatePath(path);
        FtpClientListingTransferResult transfer =
            new FtpClientListingDataTransfer(ClientState).ReceiveListing(
                FtpCommandKind.Nlst, path);
        if (!transfer.CommandResult.Succeeded)
        {
            return CreateFailedListing(transfer.CommandResult, "NLST", "Names");
        }

        string[] lines = FtpListingTextCodec.ParseLines(
            transfer.DataText, Options.StrictReplyParsing);
        FtpListing listing = FtpNameListingParser.ParseListing(lines);
        listing.CommandResult = transfer.CommandResult;
        return listing;
    }

    internal FtpListing RetrieveStatus(string path)
    {
        ValidatePath(path);
        ClientState.RequireOpenSession("ListStatus");
        string? argument = path.Length == 0 ? null : path;
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult commandResult = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(FtpCommandKind.Stat, argument));
        if (!commandResult.Succeeded)
        {
            return CreateFailedListing(commandResult, "STAT", "Raw");
        }
        if (commandResult.FinalReply == null)
        {
            throw new FtpProtocolException(
                "A successful FTP STAT listing requires a terminal reply.", null);
        }

        string[] lines = ExtractStatusLines(commandResult.FinalReply);
        FtpListing listing = FtpHumanListingParser.ParseListing(lines);
        listing.CommandResult = commandResult;
        FilterPseudoEntries(listing);
        ClientState.UpdateReconnectState();
        return listing;
    }

    internal FtpListing CreateFailedListing(
        FtpCommandResult commandResult,
        string commandName,
        string formatName)
    {
        FtpListing listing = new FtpListing();
        listing.FormatName = formatName;
        listing.CommandResult = commandResult;
        listing.FailureMessage = CreateFailureMessage(commandResult, commandName);
        return listing;
    }

    internal static string[] ExtractStatusLines(FtpReply reply)
    {
        if (!reply.Multiline)
        {
            return new string[0];
        }
        if (reply.Lines.Length < 2)
        {
            throw new FtpProtocolException(
                "A multiline FTP STAT reply requires boundary lines.", null);
        }

        string[] lines = new string[reply.Lines.Length - 2];
        int index = 0;
        while (index < lines.Length)
        {
            lines[index] = reply.Lines[index + 1].RawText;
            index++;
        }
        return lines;
    }

    internal static void FilterPseudoEntries(FtpListing listing)
    {
        int count = 0;
        int index = 0;
        while (index < listing.Entries.Length)
        {
            FtpEntryType type = listing.Entries[index].Type;
            if (type != FtpEntryType.CurrentDirectory &&
                type != FtpEntryType.ParentDirectory)
            {
                count++;
            }
            index++;
        }
        if (count == listing.Entries.Length)
        {
            return;
        }

        FtpEntry[] filtered = new FtpEntry[count];
        int target = 0;
        index = 0;
        while (index < listing.Entries.Length)
        {
            FtpEntry entry = listing.Entries[index];
            if (entry.Type != FtpEntryType.CurrentDirectory &&
                entry.Type != FtpEntryType.ParentDirectory)
            {
                filtered[target] = entry;
                target++;
            }
            index++;
        }
        listing.Entries = filtered;
    }

    internal static string CreateFailureMessage(
        FtpCommandResult commandResult,
        string commandName)
    {
        if (commandResult.FinalReply == null)
        {
            return "The FTP " + commandName +
                " command did not return a terminal reply.";
        }
        return "FTP " + commandName + " failed with reply " +
            commandResult.FinalReply.Code + ": " +
            commandResult.FinalReply.Text;
    }

    internal static void ValidatePath(string? path)
    {
        if (path == null || FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpConfigurationException(
                "A valid remote listing path is required.", null);
        }
    }
}
