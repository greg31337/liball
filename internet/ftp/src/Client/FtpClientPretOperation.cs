namespace ftp;

internal class FtpClientPretOperation
{
    internal FtpClientState ClientState;
    internal FtpClientOptions Options;

    internal FtpClientPretOperation(FtpClientState clientState)
    {
        if (clientState == null || clientState.Options == null)
        {
            throw new FtpConfigurationException(
                "An open configured FTP client is required for PRET.", null);
        }
        ClientState = clientState;
        Options = clientState.Options;
    }

    internal FtpCommandResult? SendPret(FtpCommand upcomingCommand)
    {
        bool advertised = !ClientState.Features.Negotiated ||
            ClientState.FeatureState.Pret;
        if (!Options.EnablePret || !advertised)
        {
            return null;
        }
        if (!FtpReplyCoordinator.IsDataCommand(upcomingCommand.Kind))
        {
            throw new FtpConfigurationException(
                "PRET requires an upcoming FTP data-transfer command.",
                null);
        }
        FtpClientCommandExecutor executor =
            new FtpClientCommandExecutor(ClientState);
        string intent = FtpCommandWriter.BuildCommandLine(upcomingCommand);
        FtpCommand pret = executor.CreateSimpleCommand(
            FtpCommandKind.Pret, intent);
        FtpCommandResult result = executor.ExecuteRawCommand(pret);
        result.Succeeded = result.FinalReply != null &&
            result.FinalReply.Code == FtpReplyCodes.CommandOkay;
        return result;
    }
}
