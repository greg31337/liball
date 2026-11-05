using System.IO;

namespace ftp;

internal class FtpReplyStreamReader
{
    internal int MaximumReplyBytes;
    internal int MaximumReplyLines;
    internal int MaximumLineBytes;
    internal bool Strict;

    internal FtpReplyStreamReader(
        int maximumReplyBytes,
        int maximumReplyLines,
        int maximumLineBytes,
        bool strict)
    {
        if (maximumReplyBytes <= 0 || maximumReplyLines <= 0 || maximumLineBytes < 2)
        {
            throw new FtpConfigurationException(
                "Reply stream limits must be positive and reserve CRLF space.", null);
        }

        MaximumReplyBytes = maximumReplyBytes;
        MaximumReplyLines = maximumReplyLines;
        MaximumLineBytes = maximumLineBytes;
        Strict = strict;
    }

    internal byte[] ReadReplyBytes(Stream? source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        using MemoryStream replyBytes = new MemoryStream();
        byte[] prefix = new byte[4];
        int lineLength = 0;
        int lineCount = 0;
        int openingCode = 0;
        bool firstLine = true;
        bool multiline = false;
        bool sawCarriageReturn = false;
        bool complete = false;

        while (!complete)
        {
            int value = source.ReadByte();
            if (value < 0)
            {
                throw new FtpProtocolException(
                    "The FTP reply stream ended before a complete reply was received.", null);
            }
            if (replyBytes.Length >= MaximumReplyBytes)
            {
                throw new FtpProtocolException(
                    "The FTP reply exceeded its configured byte limit.", null);
            }
            replyBytes.WriteByte((byte)value);

            if (sawCarriageReturn)
            {
                if (value != 10)
                {
                    throw new FtpProtocolException(
                        "A carriage return in an FTP reply must be followed by a line feed.", null);
                }

                lineCount++;
                if (lineCount > MaximumReplyLines)
                {
                    throw new FtpProtocolException(
                        "The FTP reply exceeded its configured line limit.", null);
                }

                if (firstLine)
                {
                    firstLine = false;
                    multiline = IsMultilineOpening(prefix, lineLength);
                    if (multiline)
                    {
                        openingCode = ParsePrefixCode(prefix);
                    }
                    else
                    {
                        complete = true;
                    }
                }
                else if (multiline && IsMultilineTerminator(prefix, lineLength, openingCode))
                {
                    complete = true;
                }

                if (!complete && lineCount >= MaximumReplyLines)
                {
                    throw new FtpProtocolException(
                        "The FTP multiline reply exceeded its configured line limit.", null);
                }

                sawCarriageReturn = false;
                lineLength = 0;
                prefix = new byte[4];
            }
            else if (value == 13)
            {
                sawCarriageReturn = true;
            }
            else if (value == 10)
            {
                if (Strict)
                {
                    throw new FtpProtocolException(
                        "A bare line feed is not a valid FTP reply terminator.", null);
                }

                lineCount++;
                if (lineCount > MaximumReplyLines)
                {
                    throw new FtpProtocolException(
                        "The FTP reply exceeded its configured line limit.", null);
                }
                if (firstLine)
                {
                    firstLine = false;
                    multiline = IsMultilineOpening(prefix, lineLength);
                    if (multiline)
                    {
                        openingCode = ParsePrefixCode(prefix);
                    }
                    else
                    {
                        complete = true;
                    }
                }
                else if (multiline &&
                    IsMultilineTerminator(prefix, lineLength, openingCode))
                {
                    complete = true;
                }
                if (!complete && lineCount >= MaximumReplyLines)
                {
                    throw new FtpProtocolException(
                        "The FTP multiline reply exceeded its configured line limit.", null);
                }
                lineLength = 0;
                prefix = new byte[4];
            }
            else
            {
                if (lineLength >= MaximumLineBytes - 2)
                {
                    throw new FtpProtocolException(
                        "The FTP reply line exceeded its configured byte limit.", null);
                }
                if (lineLength < prefix.Length)
                {
                    prefix[lineLength] = (byte)value;
                }
                lineLength++;
            }
        }

        return replyBytes.ToArray();
    }

    internal bool IsMultilineOpening(byte[] prefix, int lineLength)
    {
        return lineLength >= 4 && HasCodePrefix(prefix) && prefix[3] == 45;
    }

    internal bool IsMultilineTerminator(byte[] prefix, int lineLength, int openingCode)
    {
        return lineLength >= 4 && HasCodePrefix(prefix) && prefix[3] == 32 &&
            ParsePrefixCode(prefix) == openingCode;
    }

    internal bool HasCodePrefix(byte[] prefix)
    {
        return prefix[0] >= 48 && prefix[0] <= 57 &&
            prefix[1] >= 48 && prefix[1] <= 57 &&
            prefix[2] >= 48 && prefix[2] <= 57;
    }

    internal int ParsePrefixCode(byte[] prefix)
    {
        return ((prefix[0] - 48) * 100) + ((prefix[1] - 48) * 10) + (prefix[2] - 48);
    }
}
