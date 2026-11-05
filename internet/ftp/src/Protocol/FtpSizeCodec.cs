using System.Globalization;

namespace ftp;

internal static class FtpSizeCodec
{
    internal static FtpSizeValue ParseSizeValue(string? text)
    {
        FtpSizeValue size = new FtpSizeValue();
        if (text == null)
        {
            return size;
        }
        if (text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("The FTP SIZE value exceeded the control-line size limit.", null);
        }

        size.Value = FtpCommandParser.ParseNonNegativeInt64(text, "SIZE");
        size.HasValue = true;
        size.RawValue = text;
        return size;
    }

    internal static string FormatSizeValue(FtpSizeValue? size)
    {
        if (size == null)
        {
            throw new System.ArgumentNullException("size");
        }
        if (!size.HasValue)
        {
            throw new FtpProtocolException("An unavailable FTP SIZE value cannot be formatted.", null);
        }
        if (size.Value < 0)
        {
            throw new FtpProtocolException("An FTP SIZE value cannot be negative.", null);
        }
        return size.Value.ToString(CultureInfo.InvariantCulture);
    }

    internal static FtpReply CreateSizeReply(long size)
    {
        if (size < 0)
        {
            throw new FtpProtocolException("An FTP SIZE reply cannot contain a negative value.", null);
        }

        FtpReply reply = new FtpReply();
        reply.Code = FtpReplyCodes.FileStatus;
        reply.Text = size.ToString(CultureInfo.InvariantCulture);
        reply.Classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        return reply;
    }
}
