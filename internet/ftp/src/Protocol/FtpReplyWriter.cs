using System.Globalization;
using System.Text;

namespace ftp;

internal class FtpReplyWriter
{
    internal int MaximumReplyBytes;
    internal int MaximumReplyLines;

    internal FtpReplyWriter(int maximumReplyBytes, int maximumReplyLines)
    {
        if (maximumReplyBytes <= 0 || maximumReplyLines <= 0)
        {
            throw new FtpConfigurationException("Reply writer size and line limits must be positive.", null);
        }

        MaximumReplyBytes = maximumReplyBytes;
        MaximumReplyLines = maximumReplyLines;
    }

    internal string FormatReply(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new System.ArgumentNullException("reply");
        }

        ValidateReplyCode(reply.Code);
        if (reply.Lines == null)
        {
            throw new FtpProtocolException("The FTP reply line collection is required.", null);
        }

        string codeText = reply.Code.ToString("D3", CultureInfo.InvariantCulture);
        StringBuilder builder = new StringBuilder();
        if (!reply.Multiline)
        {
            if (reply.Lines.Length > 1)
            {
                throw new FtpProtocolException("A single-line FTP reply cannot contain multiple physical lines.", null);
            }

            ValidateReplyText(reply.Text);
            AppendBoundedLine(builder, codeText + " " + reply.Text);
        }
        else
        {
            FormatMultilineReply(builder, reply, codeText);
        }

        string formatted = builder.ToString();
        if (Encoding.UTF8.GetByteCount(formatted) > MaximumReplyBytes)
        {
            throw new FtpProtocolException("The formatted FTP reply exceeded its configured byte limit.", null);
        }

        return formatted;
    }

    internal byte[] FormatEncodedReply(FtpReply? reply, Encoding? encoding)
    {
        if (encoding == null)
        {
            throw new FtpConfigurationException("A negotiated reply encoding is required.", null);
        }

        FtpCommandWriter.ValidateEncoding(encoding);
        string formatted = FormatReply(reply);
        if (encoding.CodePage == Encoding.ASCII.CodePage)
        {
            FtpCommandWriter.ValidateAsciiText(formatted);
        }

        byte[] bytes = encoding.GetBytes(formatted);
        if (bytes.Length > MaximumReplyBytes)
        {
            throw new FtpProtocolException("The encoded FTP reply exceeded its configured byte limit.", null);
        }

        if (encoding.CodePage == Encoding.UTF8.CodePage)
        {
            FtpCommandParser.ValidateUtf8Bytes(bytes);
        }

        return bytes;
    }

    internal void FormatMultilineReply(StringBuilder builder, FtpReply reply, string codeText)
    {
        if (reply.Lines.Length < 2)
        {
            throw new FtpProtocolException("A multiline FTP reply requires opening and terminating physical lines.", null);
        }

        if (reply.Lines.Length > MaximumReplyLines)
        {
            throw new FtpProtocolException("The formatted FTP reply exceeded its configured line limit.", null);
        }

        FtpReplyLine opening = reply.Lines[0];
        FtpReplyLine terminal = reply.Lines[reply.Lines.Length - 1];
        ValidateMatchingLineCode(opening, reply.Code, "opening");
        ValidateMatchingLineCode(terminal, reply.Code, "terminating");
        ValidateReplyText(opening.Text);
        ValidateReplyText(reply.Text);
        AppendBoundedLine(builder, codeText + "-" + opening.Text);

        int index = 1;
        while (index < reply.Lines.Length - 1)
        {
            string middle = ResolveMiddleLine(reply.Lines[index]);
            ValidateReplyText(middle);
            if (middle.StartsWith(codeText + " ", System.StringComparison.Ordinal))
            {
                throw new FtpProtocolException("A multiline body line cannot imitate the matching terminator.", null);
            }

            AppendBoundedLine(builder, middle);
            index++;
        }

        AppendBoundedLine(builder, codeText + " " + reply.Text);
    }

    internal string ResolveMiddleLine(FtpReplyLine line)
    {
        if (line == null)
        {
            throw new FtpProtocolException("A multiline FTP reply cannot contain a null physical line.", null);
        }

        if (!string.IsNullOrEmpty(line.RawText))
        {
            return line.RawText;
        }

        if (line.HasCode)
        {
            ValidateReplyCode(line.Code);
            return line.Code.ToString("D3", CultureInfo.InvariantCulture) + " " + line.Text;
        }

        return line.Text;
    }

    internal void AppendBoundedLine(StringBuilder builder, string line)
    {
        if (Encoding.UTF8.GetByteCount(line) + 2 > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("A formatted FTP reply line exceeded the control-line byte limit.", null);
        }

        builder.Append(line);
        builder.Append("\r\n");
    }

    internal void ValidateMatchingLineCode(FtpReplyLine line, int replyCode, string lineName)
    {
        if (line == null)
        {
            throw new FtpProtocolException($"The multiline FTP {lineName} line is required.", null);
        }

        if (line.HasCode && line.Code != replyCode)
        {
            throw new FtpProtocolException($"The multiline FTP {lineName} line code must match the reply code.", null);
        }
    }

    internal void ValidateReplyCode(int code)
    {
        if (code < 100 || code > 599)
        {
            throw new FtpProtocolException("An outbound FTP reply code must be between 100 and 599.", null);
        }
    }

    internal void ValidateReplyText(string? text)
    {
        if (text == null)
        {
            throw new FtpProtocolException("FTP reply text cannot be null.", null);
        }

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (char.IsControl(character) && character != '\t')
            {
                throw new FtpProtocolException("FTP reply text cannot contain embedded control characters.", null);
            }
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    throw new FtpProtocolException("FTP reply text contains an unpaired Unicode surrogate.", null);
                }
                index = index + 2;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw new FtpProtocolException("FTP reply text contains an unpaired Unicode surrogate.", null);
            }
            else
            {
                index++;
            }
        }
    }
}
