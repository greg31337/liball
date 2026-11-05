namespace ftp;

internal class FtpClientMetadataMutationOperation
{
    internal FtpClientState ClientState;

    internal FtpClientMetadataMutationOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for metadata mutation.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpCommandResult SetModificationTime(
        string path,
        FtpTimestamp? timestamp)
    {
        FtpTransferValidator.ValidateRequiredPath(path, "remote metadata");
        string value = FtpTimestampCodec.FormatTimestampValue(timestamp);
        RequireFeature(
            ClientState.FeatureState.ModificationTime,
            FtpFeatureNames.Mfmt);
        FtpCommandResult result = Execute(
            FtpCommandKind.Mfmt, value + " " + path);
        result.Succeeded = result.FinalReply != null &&
            result.FinalReply.Code == FtpReplyCodes.FileStatus;
        if (!result.Succeeded && result.FinalReply != null)
        {
            result.FailureMessage =
                "FTP MFMT failed with reply " +
                result.FinalReply.Code.ToString() + ": " +
                result.FinalReply.Text;
        }
        return result;
    }

    internal FtpCommandResult SetPermissions(string path, uint mode)
    {
        FtpTransferValidator.ValidateRequiredPath(path, "remote permission");
        if (mode > 4095)
        {
            throw new FtpConfigurationException(
                "SITE CHMOD permission bits must be between 0000 and 07777.", null);
        }
        RequireFeature(
            ClientState.FeatureState.SiteChmod,
            FtpFeatureNames.SiteChmod);
        return Execute(
            FtpCommandKind.Site,
            "CHMOD " + FormatMode(mode, 3) + " " + path);
    }

    internal FtpCommandResult SetUmask(uint mode)
    {
        if (mode > 511)
        {
            throw new FtpConfigurationException(
                "SITE UMASK bits must be between 000 and 0777.", null);
        }
        RequireFeature(
            ClientState.FeatureState.SiteUmask,
            FtpFeatureNames.SiteUmask);
        return Execute(
            FtpCommandKind.Site,
            "UMASK " + FormatMode(mode, 3));
    }

    internal void RequireFeature(bool supported, string featureName)
    {
        if (ClientState.Features.Negotiated && !supported)
        {
            throw new FtpConfigurationException(
                "The negotiated FTP feature set does not advertise " +
                featureName + ".",
                null);
        }
    }

    internal FtpCommandResult Execute(FtpCommandKind kind, string argument)
    {
        ClientState.RequireOpenSession(kind.ToString());
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(kind, argument));
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        return result;
    }

    internal static string FormatMode(uint mode, int minimumDigits)
    {
        string value = System.Convert.ToString((int)mode, 8);
        return value.PadLeft(minimumDigits, '0');
    }
}
