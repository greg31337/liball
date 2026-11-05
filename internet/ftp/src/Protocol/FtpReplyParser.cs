using System.Collections.Generic;
using System.Text;

namespace ftp;

internal class FtpReplyParser
{
    internal int MaximumReplyBytes;
    internal int MaximumReplyLines;
    internal bool Strict;

    internal FtpReplyParser(int maximumReplyBytes, int maximumReplyLines, bool strict)
    {
        if (maximumReplyBytes <= 0 || maximumReplyLines <= 0)
        {
            throw new FtpConfigurationException("Reply parser size and line limits must be positive.", null);
        }

        MaximumReplyBytes = maximumReplyBytes;
        MaximumReplyLines = maximumReplyLines;
        Strict = strict;
    }

    internal FtpReply ParseEncodedReply(byte[] replyBytes, Encoding? encoding)
    {
        if (replyBytes == null)
        {
            throw new System.ArgumentNullException("replyBytes");
        }

        if (encoding == null)
        {
            throw new FtpConfigurationException("A negotiated reply encoding is required.", null);
        }

        if (replyBytes.Length > MaximumReplyBytes)
        {
            throw new FtpProtocolException("The FTP reply exceeded its configured byte limit.", null);
        }

        if (encoding.CodePage == Encoding.ASCII.CodePage)
        {
            FtpCommandParser.ValidateAsciiBytes(replyBytes);
        }
        else if (encoding.CodePage == Encoding.UTF8.CodePage)
        {
            FtpCommandParser.ValidateUtf8Bytes(replyBytes);
        }
        else
        {
            throw new FtpConfigurationException("FTP reply decoding supports negotiated ASCII or UTF-8.", null);
        }

        return ParseReplyText(encoding.GetString(replyBytes));
    }

    internal FtpReply ParseReplyText(string? replyText)
    {
        if (string.IsNullOrEmpty(replyText))
        {
            throw new FtpProtocolException("An FTP reply cannot be empty.", null);
        }

        if (Encoding.UTF8.GetByteCount(replyText) > MaximumReplyBytes)
        {
            throw new FtpProtocolException("The FTP reply exceeded its configured byte limit.", null);
        }

        List<string> physicalLines = SplitPhysicalLines(replyText);
        if (physicalLines.Count == 0)
        {
            throw new FtpProtocolException("An FTP reply must contain a terminated physical line.", null);
        }

        FtpReplyLine first = ParseInitialLine(physicalLines[0]);
        List<FtpReplyLine> parsedLines = new List<FtpReplyLine>(physicalLines.Count);
        parsedLines.Add(first);

        bool multiline = physicalLines[0].Length >= 4 && physicalLines[0][3] == '-';
        FtpReplyLine terminal = first;
        if (multiline)
        {
            bool terminated = false;
            int index = 1;
            while (index < physicalLines.Count)
            {
                FtpReplyLine line = ParseMultilineLine(physicalLines[index], first.Code);
                parsedLines.Add(line);
                if (line.TerminatesMultiline)
                {
                    terminal = line;
                    terminated = true;
                    if (index != physicalLines.Count - 1)
                    {
                        throw new FtpProtocolException("An FTP reply contained data after its multiline terminator.", null);
                    }
                }

                index++;
            }

            if (!terminated)
            {
                throw new FtpProtocolException("An FTP multiline reply did not contain a matching terminator.", null);
            }
        }
        else if (physicalLines.Count != 1)
        {
            throw new FtpProtocolException("An FTP single-line reply contained trailing physical lines.", null);
        }

        FtpReply reply = new FtpReply();
        reply.Code = terminal.Code;
        reply.Text = terminal.Text;
        reply.Lines = parsedLines.ToArray();
        reply.Multiline = multiline;
        reply.RawText = replyText;
        reply.Classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        return reply;
    }

    internal List<string> SplitPhysicalLines(string replyText)
    {
        List<string> lines = new List<string>();
        int lineStart = 0;
        int index = 0;
        while (index < replyText.Length)
        {
            char character = replyText[index];
            if (character == '\0')
            {
                throw new FtpProtocolException("FTP reply text cannot contain NUL characters.", null);
            }

            if (character == '\r')
            {
                if (Strict)
                {
                    if (index + 1 >= replyText.Length || replyText[index + 1] != '\n')
                    {
                        throw new FtpProtocolException("Strict FTP reply parsing requires CRLF line endings.", null);
                    }

                    AddPhysicalLine(lines, replyText.Substring(lineStart, index - lineStart));
                    index = index + 2;
                    lineStart = index;
                }
                else
                {
                    AddPhysicalLine(lines, replyText.Substring(lineStart, index - lineStart));
                    if (index + 1 < replyText.Length && replyText[index + 1] == '\n')
                    {
                        index = index + 2;
                    }
                    else
                    {
                        index++;
                    }

                    lineStart = index;
                }
            }
            else if (character == '\n')
            {
                if (Strict)
                {
                    throw new FtpProtocolException("Strict FTP reply parsing requires CRLF line endings.", null);
                }

                AddPhysicalLine(lines, replyText.Substring(lineStart, index - lineStart));
                index++;
                lineStart = index;
            }
            else
            {
                index++;
            }
        }

        if (lineStart != replyText.Length)
        {
            throw new FtpProtocolException("The FTP reply ended before its final line terminator.", null);
        }

        return lines;
    }

    internal void AddPhysicalLine(List<string> lines, string line)
    {
        if (lines.Count >= MaximumReplyLines)
        {
            throw new FtpProtocolException("The FTP reply exceeded its configured line limit.", null);
        }

        if (Encoding.UTF8.GetByteCount(line) + 2 > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP reply line exceeded the control-line byte limit.", null);
        }

        ValidateLineCharacters(line);
        lines.Add(line);
    }

    internal FtpReplyLine ParseInitialLine(string line)
    {
        if (line.Length < 3 || !HasThreeDigitCode(line))
        {
            throw new FtpProtocolException("An FTP reply must begin with a three-digit code.", null);
        }

        FtpReplyLine parsed = new FtpReplyLine();
        parsed.HasCode = true;
        parsed.Code = ParseReplyCode(line);
        parsed.RawText = line;
        if (line.Length == 3)
        {
            if (Strict)
            {
                throw new FtpProtocolException("A strict FTP reply code requires a space or hyphen separator.", null);
            }

            parsed.Text = "";
            return parsed;
        }

        char separator = line[3];
        if (separator == ' ' || separator == '-')
        {
            parsed.Text = line.Substring(4);
            return parsed;
        }

        if (Strict)
        {
            throw new FtpProtocolException("A strict FTP reply code requires a space or hyphen separator.", null);
        }

        parsed.Text = line.Substring(3);
        return parsed;
    }

    internal FtpReplyLine ParseMultilineLine(string line, int openingCode)
    {
        FtpReplyLine parsed = new FtpReplyLine();
        parsed.RawText = line;
        parsed.Text = line;
        if (!HasThreeDigitCode(line))
        {
            return parsed;
        }

        int code = ParseReplyCode(line);
        bool recognizedSeparator = line.Length == 3 || line[3] == ' ' || line[3] == '-';
        if (!recognizedSeparator)
        {
            return parsed;
        }

        parsed.HasCode = true;
        parsed.Code = code;
        if (line.Length == 3)
        {
            parsed.Text = "";
            if (!Strict && code == openingCode)
            {
                parsed.TerminatesMultiline = true;
            }

            return parsed;
        }

        parsed.Text = line.Substring(4);
        if (code == openingCode && line[3] == ' ')
        {
            parsed.TerminatesMultiline = true;
        }

        return parsed;
    }

    internal bool HasThreeDigitCode(string line)
    {
        return line.Length >= 3 &&
            line[0] >= '0' && line[0] <= '9' &&
            line[1] >= '0' && line[1] <= '9' &&
            line[2] >= '0' && line[2] <= '9';
    }

    internal int ParseReplyCode(string line)
    {
        return ((line[0] - '0') * 100) + ((line[1] - '0') * 10) + (line[2] - '0');
    }

    internal void ValidateLineCharacters(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (char.IsControl(line[index]) && line[index] != '\t')
            {
                throw new FtpProtocolException("FTP reply lines cannot contain embedded control characters.", null);
            }

            index++;
        }
    }
}
