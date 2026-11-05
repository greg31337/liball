using System;
using System.Collections.Generic;

namespace ftp;

internal static class FtpListingTextCodec
{
    internal static string[] ParseLines(
        string? listingText,
        bool strictLineEndings)
    {
        if (listingText == null)
        {
            throw new ArgumentNullException("listingText");
        }

        List<string> lines = new List<string>();
        int lineStart = 0;
        int index = 0;
        while (index < listingText.Length)
        {
            char character = listingText[index];
            if (character == '\r' || character == '\n')
            {
                int terminatorLength = 1;
                if (character == '\r' && index + 1 < listingText.Length &&
                    listingText[index + 1] == '\n')
                {
                    terminatorLength = 2;
                }
                else if (strictLineEndings)
                {
                    throw new FtpProtocolException(
                        "Strict FTP listing parsing requires CRLF line endings.", null);
                }

                AddLine(listingText.Substring(lineStart, index - lineStart), lines);
                index = index + terminatorLength;
                lineStart = index;
            }
            else
            {
                index++;
            }
        }

        if (lineStart < listingText.Length)
        {
            AddLine(listingText.Substring(lineStart), lines);
        }
        return lines.ToArray();
    }

    internal static void AddLine(string line, List<string> lines)
    {
        if (line.Length == 0)
        {
            throw new FtpProtocolException(
                "An FTP listing cannot contain an empty entry line.", null);
        }
        if (lines.Count >= FtpDefaults.MaximumListingEntryCount)
        {
            throw new FtpProtocolException(
                "The FTP listing exceeded the configured entry limit.", null);
        }
        lines.Add(line);
    }
}
