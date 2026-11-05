using System;

namespace ftp;

internal static class FtpChecksumExtensions
{
    internal const string DefaultHashAlgorithm = "SHA-256";
    internal const int Md5ReplyCode = 251;
    internal const int HashAuthorizationDeniedReplyCode = 552;

    internal static string FormatHashFeature(string selectedAlgorithm)
    {
        string selected = TransferHash.NormalizeAlgorithm(selectedAlgorithm);
        string[] algorithms = new string[]
        {
            "SHA-256", "SHA-512", "SHA-1", "MD5", "CRC32"
        };
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < algorithms.Length)
        {
            text.Append(algorithms[index]);
            if (algorithms[index] == selected)
            {
                text.Append('*');
            }
            text.Append(';');
            index++;
        }
        return text.ToString();
    }

    internal static string ParseHashOption(
        string? value,
        string currentAlgorithm)
    {
        if (value == null)
        {
            return TransferHash.NormalizeAlgorithm(currentAlgorithm);
        }
        if (value.Length == 0 || value.IndexOf(' ') >= 0)
        {
            throw new FtpProtocolException(
                "OPTS HASH accepts at most one algorithm name.", null);
        }
        return TransferHash.NormalizeAlgorithm(value);
    }

    internal static string GetCommandAlgorithm(FtpCommandKind kind)
    {
        if (kind == FtpCommandKind.Md5 || kind == FtpCommandKind.Xmd5)
        {
            return "MD5";
        }
        if (kind == FtpCommandKind.Xsha1)
        {
            return "SHA-1";
        }
        if (kind == FtpCommandKind.Xsha256)
        {
            return "SHA-256";
        }
        if (kind == FtpCommandKind.Xsha512)
        {
            return "SHA-512";
        }
        if (kind == FtpCommandKind.Xcrc)
        {
            return "CRC32";
        }
        throw new FtpConfigurationException(
            "A recognized checksum command is required.", null);
    }

    internal static string FormatHashReply(
        string algorithm,
        long length,
        string checksum,
        string path)
    {
        string normalizedAlgorithm = TransferHash.NormalizeAlgorithm(algorithm);
        string? normalizedChecksum = TransferHash.NormalizeChecksum(
            checksum, normalizedAlgorithm);
        if (normalizedChecksum == null || length < 0)
        {
            throw new FtpProtocolException(
                "A valid HASH result is required.", null);
        }
        long end = length == 0 ? 0 : length - 1;
        return normalizedAlgorithm + " 0-" + end.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + " " +
            normalizedChecksum.ToLowerInvariant() + " " + path;
    }

    internal static string FormatVendorReply(
        string algorithm,
        string checksum,
        string path)
    {
        string normalizedAlgorithm = TransferHash.NormalizeAlgorithm(algorithm);
        string? normalizedChecksum = TransferHash.NormalizeChecksum(
            checksum, normalizedAlgorithm);
        if (normalizedChecksum == null)
        {
            throw new FtpProtocolException(
                "A valid compatibility checksum result is required.", null);
        }
        return normalizedChecksum.ToLowerInvariant() + " " + path;
    }

    internal static string? ExtractReplyChecksum(
        FtpCommandKind kind,
        FtpReply? reply,
        string expectedAlgorithm)
    {
        if (reply == null || reply.Code < 200 || reply.Code >= 300)
        {
            return null;
        }
        string algorithm = TransferHash.NormalizeAlgorithm(expectedAlgorithm);
        if (kind == FtpCommandKind.Hash)
        {
            return ExtractHashReplyChecksum(reply, algorithm);
        }
        return ExtractVendorReplyChecksum(reply.Text, algorithm);
    }

    internal static string? ExtractHashReplyChecksum(
        FtpReply reply,
        string expectedAlgorithm)
    {
        if (reply.Code != FtpReplyCodes.FileStatus)
        {
            return null;
        }
        string[] fields = reply.Text.Split(
            new char[] { ' ' },
            4,
            StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 4)
        {
            return null;
        }
        string? algorithm = TransferHash.TryNormalizeAlgorithm(fields[0]);
        if (algorithm == null || algorithm != expectedAlgorithm ||
            !IsValidHashRange(fields[1]))
        {
            return null;
        }
        return TransferHash.NormalizeChecksum(fields[2], algorithm);
    }

    internal static string? ExtractVendorReplyChecksum(
        string text,
        string algorithm)
    {
        string[] fields = text.Split(
            new char[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        string? found = null;
        int index = 0;
        while (index < fields.Length)
        {
            string? checksum = TransferHash.NormalizeChecksum(
                fields[index], algorithm);
            if (checksum != null)
            {
                if (found != null)
                {
                    return null;
                }
                found = checksum;
            }
            index++;
        }
        return found;
    }

    internal static bool IsValidHashRange(string text)
    {
        int separator = text.IndexOf('-');
        if (separator <= 0 || separator >= text.Length - 1 ||
            text.IndexOf('-', separator + 1) >= 0)
        {
            return false;
        }
        string start = text.Substring(0, separator);
        string end = text.Substring(separator + 1);
        if (!IsDecimalRangeValue(start) || !IsDecimalRangeValue(end))
        {
            return false;
        }
        return IsHashRangeEndValid(start, end);
    }

    internal static bool IsDecimalRangeValue(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
            index++;
        }
        return text.Length > 0;
    }

    internal static bool IsHashRangeEndValid(string start, string end)
    {
        int startIndex = 0;
        while (startIndex < start.Length - 1 && start[startIndex] == '0')
        {
            startIndex++;
        }
        int endIndex = 0;
        while (endIndex < end.Length - 1 && end[endIndex] == '0')
        {
            endIndex++;
        }
        int startLength = start.Length - startIndex;
        int endLength = end.Length - endIndex;
        if (endLength != startLength)
        {
            return endLength > startLength;
        }
        return string.CompareOrdinal(
            end, endIndex, start, startIndex, endLength) >= 0;
    }
}
