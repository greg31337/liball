using System.Collections.Generic;
using System.Text;

namespace ftp;

internal static class FtpPathCodec
{
    internal static string ResolveWorkingPath(
        string currentDirectory,
        string path)
    {
        string combined;
        if (path.Length > 0 && path[0] == '/')
        {
            combined = path;
        }
        else if (currentDirectory == "/")
        {
            combined = "/" + path;
        }
        else
        {
            combined = currentDirectory + "/" + path;
        }

        string[] parts = combined.Split('/');
        List<string> resolved = new List<string>();
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index];
            if (part.Length != 0 && part != ".")
            {
                if (part == "..")
                {
                    if (resolved.Count > 0)
                    {
                        resolved.RemoveAt(resolved.Count - 1);
                    }
                }
                else
                {
                    resolved.Add(part);
                }
            }
            index++;
        }

        if (resolved.Count == 0)
        {
            return "/";
        }
        return "/" + string.Join("/", resolved);
    }

    internal static string ParseQuotedPathname(string? text)
    {
        if (string.IsNullOrEmpty(text) || text[0] != '"')
        {
            throw new FtpProtocolException(
                "An FTP PWD or MKD reply must begin with a quoted pathname.", null);
        }

        StringBuilder path = new StringBuilder();
        int index = 1;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '"')
            {
                if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    path.Append('"');
                    index += 2;
                    continue;
                }

                index++;
                if (index < text.Length && text[index] != ' ')
                {
                    throw new FtpProtocolException(
                        "Text following an FTP quoted pathname must be separated by a space.", null);
                }
                ValidateReplySuffix(text, index);
                return path.ToString();
            }

            if (char.IsControl(character))
            {
                throw new FtpProtocolException(
                    "An FTP PWD or MKD pathname cannot contain control characters.", null);
            }

            path.Append(character);
            index++;
        }

        throw new FtpProtocolException(
            "An FTP PWD or MKD reply contains an unterminated quoted pathname.", null);
    }

    internal static string FormatQuotedPathname(string? pathname)
    {
        if (pathname == null)
        {
            throw new System.ArgumentNullException("pathname");
        }

        StringBuilder text = new StringBuilder(pathname.Length + 2);
        text.Append('"');
        int index = 0;
        while (index < pathname.Length)
        {
            char character = pathname[index];
            if (char.IsControl(character))
            {
                throw new FtpProtocolException(
                    "An FTP PWD or MKD pathname cannot contain control characters.", null);
            }
            if (character == '"')
            {
                text.Append('"');
            }
            text.Append(character);
            index++;
        }
        text.Append('"');
        return text.ToString();
    }

    internal static void ValidateReplySuffix(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (char.IsControl(text[index]))
            {
                throw new FtpProtocolException(
                    "An FTP PWD or MKD reply suffix cannot contain control characters.", null);
            }
            index++;
        }
    }
}
