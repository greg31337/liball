using System;

namespace ftp;

internal class FtpClientMachineListingOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;
    internal FtpControlConnection Connection;

    internal FtpClientMachineListingOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for machine listings.",
                null);
        }

        ClientState = clientState;
        Options = clientState.Options;
        Connection = clientState.Connection;
    }

    internal FtpPathProbeResult RetrieveMetadata(string path)
    {
        FtpTransferValidator.ValidateRequiredPath(path, "remote metadata");
        ClientState.RequireOpenSession("GetMetadata");
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult commandResult = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(FtpCommandKind.Mlst, path));
        FtpPathProbeResult result = new FtpPathProbeResult();
        result.Commands = new[] { commandResult };
        if (!commandResult.Succeeded)
        {
            result.FailureMessage = CreateCommandFailure(commandResult, "MLST");
            return result;
        }
        if (commandResult.FinalReply == null)
        {
            throw new FtpProtocolException(
                "A successful FTP MLST command requires a terminal reply.", null);
        }

        FtpEntry entry = FtpMlsxCodec.ParseControlReply(commandResult.FinalReply);
        entry.FullPath = entry.Name;
        result.Succeeded = true;
        result.Exists = true;
        result.Entry = entry;
        ClientState.UpdateReconnectState();
        return result;
    }

    internal FtpListing RetrieveDirectory(string path)
    {
        ValidateDirectoryPath(path);
        ClientState.RequireOpenSession("ListMachineDirectory");
        FtpClientListingTransferResult transfer =
            new FtpClientListingDataTransfer(ClientState).ReceiveListing(
                FtpCommandKind.Mlsd, path);
        FtpListing listing;
        if (transfer.CommandResult.Succeeded)
        {
            listing = FtpMlsxCodec.ParseDirectoryText(
                transfer.DataText, Options.StrictReplyParsing);
        }
        else
        {
            listing = new FtpListing();
            listing.MachineReadable = true;
            listing.FormatName = FtpCommandNames.Mlsd;
            listing.FailureMessage = CreateCommandFailure(
                transfer.CommandResult, "MLSD");
            ApplyDirectoryFallback(transfer.CommandResult.FinalReply);
        }
        listing.CommandResult = transfer.CommandResult;
        listing.Complete = transfer.CommandResult.Succeeded;
        FtpClientListingOperation.FilterPseudoEntries(listing);
        return listing;
    }

    internal void ApplyDirectoryFallback(FtpReply? reply)
    {
        FtpClientQuirkPolicy policy = new FtpClientQuirkPolicy(Options.Quirks);
        if (policy.ShouldFallbackFromMlsd(reply))
        {
            ClientState.FeatureState.MachineListing = false;
        }
    }

    internal static string CreateCommandFailure(
        FtpCommandResult commandResult,
        string commandName)
    {
        if (commandResult.FinalReply == null)
        {
            return "The FTP " + commandName + " command did not return a terminal reply.";
        }
        return "FTP " + commandName + " failed with reply " +
            commandResult.FinalReply.Code + ": " + commandResult.FinalReply.Text;
    }

    internal static void ValidateDirectoryPath(string? path)
    {
        if (path == null || FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpConfigurationException(
                "A valid remote listing path is required.", null);
        }
    }
}
