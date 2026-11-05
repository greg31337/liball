namespace ftp;

internal static class FtpMfmtExtension
{
    internal static FtpMfmtRequest ParseRequest(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException(
                "MFMT requires a timestamp and pathname.", null);
        }
        int separator = argument.IndexOf(' ');
        if (separator <= 0 || separator >= argument.Length - 1 ||
            argument[separator + 1] == ' ')
        {
            throw new FtpProtocolException(
                "MFMT requires one timestamp followed by one pathname.", null);
        }
        string timestampText = argument.Substring(0, separator);
        string path = argument.Substring(separator + 1);
        if (string.IsNullOrWhiteSpace(path) ||
            FtpConfigurationValidator.HasInvalidText(path))
        {
            throw new FtpProtocolException(
                "MFMT requires a valid pathname.", null);
        }
        FtpMfmtRequest request = new FtpMfmtRequest();
        request.Timestamp = FtpTimestampCodec.ParseTimestampValue(timestampText);
        request.Path = path;
        return request;
    }

    internal static string FormatReply(
        FtpTimestamp timestamp,
        string path)
    {
        return "Modify=" + FtpTimestampCodec.FormatTimestampValue(timestamp) +
            "; " + path;
    }
}

internal class FtpMfmtRequest
{
    internal FtpTimestamp Timestamp;
    internal string Path;

    internal FtpMfmtRequest()
    {
        Timestamp = new FtpTimestamp();
        Path = "";
    }
}
