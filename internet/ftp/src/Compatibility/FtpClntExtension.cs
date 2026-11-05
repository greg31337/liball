namespace ftp;

internal static class FtpClntExtension
{
    internal static string ParseClientName(string? argument)
    {
        if (argument == null)
        {
            throw new FtpProtocolException(
                "CLNT requires a client identifier.", null);
        }
        ValidateClientName(argument);
        return argument;
    }

    internal static void ValidateClientName(string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName) ||
            FtpConfigurationValidator.HasInvalidText(clientName) ||
            clientName[0] == ' ' ||
            clientName[clientName.Length - 1] == ' ')
        {
            throw new FtpConfigurationException(
                "A CLNT identifier must contain bounded non-control text without surrounding spaces.",
                null);
        }
        string commandLine = FtpCommandNames.Clnt + " " + clientName;
        int index = 0;
        while (index < clientName.Length)
        {
            if (clientName[index] < ' ' || clientName[index] > '~')
            {
                throw new FtpConfigurationException(
                    "A CLNT identifier must contain printable ASCII characters.",
                    null);
            }
            index++;
        }
        if (System.Text.Encoding.UTF8.GetByteCount(commandLine) >
            FtpDefaults.MaximumControlLineBytes - 2)
        {
            throw new FtpConfigurationException(
                "The CLNT identifier exceeds the control-line limit.", null);
        }
    }
}
