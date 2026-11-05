using System.Globalization;
using System.Text;

namespace ftp;

internal static class FtpArgumentCodec
{
    internal static string ParseLanguageTag(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException("An FTP language tag is required.", null);
        }

        StringBuilder normalized = new StringBuilder(argument.Length);
        int subtagLength = 0;
        int index = 0;
        while (index < argument.Length)
        {
            char character = argument[index];
            bool letter = (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z');
            if (letter)
            {
                subtagLength++;
                if (subtagLength > 8)
                {
                    throw new FtpProtocolException(
                        "An FTP language-tag subtag cannot exceed eight letters.", null);
                }

                normalized.Append(char.ToLowerInvariant(character));
            }
            else if (character == '-' && subtagLength > 0 && index + 1 < argument.Length)
            {
                normalized.Append('-');
                subtagLength = 0;
            }
            else
            {
                throw new FtpProtocolException(
                    "An FTP language tag must contain one-to-eight-letter subtags.", null);
            }

            index++;
        }

        if (subtagLength == 0)
        {
            throw new FtpProtocolException(
                "An FTP language tag cannot contain an empty subtag.", null);
        }

        return normalized.ToString();
    }

    internal static string FormatLanguageTag(string? languageTag)
    {
        return ParseLanguageTag(languageTag);
    }

    internal static FtpAllocationArgument ParseAllocateArgument(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException("The FTP ALLO byte count is required.", null);
        }

        FtpAllocationArgument allocation = new FtpAllocationArgument();
        int separator = argument.IndexOf(' ');
        if (separator < 0)
        {
            allocation.ByteCount = FtpCommandParser.ParseNonNegativeInt64(
                argument, "ALLO byte count");
            return allocation;
        }

        string byteCount = argument.Substring(0, separator);
        string remainder = argument.Substring(separator + 1);
        if (remainder.Length < 3 ||
            (remainder[0] != 'R' && remainder[0] != 'r') || remainder[1] != ' ')
        {
            throw new FtpProtocolException("The FTP ALLO record allocation syntax is invalid.", null);
        }

        allocation.ByteCount = FtpCommandParser.ParseNonNegativeInt64(
            byteCount, "ALLO byte count");
        allocation.RecordSize = FtpCommandParser.ParseNonNegativeInt64(
            remainder.Substring(2), "ALLO record size");
        allocation.HasRecordSize = true;
        return allocation;
    }

    internal static string FormatAllocateArgument(FtpAllocationArgument? allocation)
    {
        if (allocation == null)
        {
            throw new System.ArgumentNullException("allocation");
        }
        if (allocation.ByteCount < 0)
        {
            throw new FtpProtocolException("The FTP ALLO byte count cannot be negative.", null);
        }
        if (!allocation.HasRecordSize && allocation.RecordSize != 0)
        {
            throw new FtpProtocolException(
                "An FTP ALLO record size requires its availability flag.", null);
        }
        if (allocation.HasRecordSize && allocation.RecordSize < 0)
        {
            throw new FtpProtocolException("The FTP ALLO record size cannot be negative.", null);
        }

        string byteCount = allocation.ByteCount.ToString(CultureInfo.InvariantCulture);
        if (!allocation.HasRecordSize)
        {
            return byteCount;
        }

        return byteCount + " R " + allocation.RecordSize.ToString(CultureInfo.InvariantCulture);
    }

    internal static long ParseRestartMarker(string? argument)
    {
        return FtpCommandParser.ParseNonNegativeInt64(argument, "REST marker");
    }

    internal static string ValidateRestartMarker(string? argument)
    {
        if (string.IsNullOrEmpty(argument) || argument.Length > 127)
        {
            throw new FtpProtocolException(
                "An FTP REST marker is empty or exceeds its command limit.", null);
        }
        string marker = argument;
        int index = 0;
        while (index < marker.Length)
        {
            char value = marker[index];
            if (value < 33 || value > 126)
            {
                throw new FtpProtocolException(
                    "An FTP REST marker must be printable ASCII without spaces.",
                    null);
            }
            index++;
        }
        char first = marker[0];
        if (first >= '0' && first <= '9')
        {
            ParseRestartMarker(marker);
        }
        else if (first == '+' || first == '-')
        {
            throw new FtpProtocolException(
                "An FTP REST marker cannot begin with a numeric sign.", null);
        }
        return marker;
    }

    internal static string FormatRestartMarker(long marker)
    {
        if (marker < 0)
        {
            throw new FtpProtocolException("The FTP REST marker cannot be negative.", null);
        }

        return marker.ToString(CultureInfo.InvariantCulture);
    }

    internal static int ParseTypeLocalArgument(string? argument)
    {
        if (argument == null || argument.Length < 3 ||
            (argument[0] != 'L' && argument[0] != 'l') || argument[1] != ' ')
        {
            throw new FtpProtocolException("An FTP TYPE L argument must use 'L <byte-size>'.", null);
        }

        return FtpCommandParser.ParseInt32InRange(
            argument.Substring(2), 1, 255, "TYPE L byte size");
    }

    internal static string FormatTypeLocalArgument(int byteSize)
    {
        if (byteSize < 1 || byteSize > 255)
        {
            throw new FtpProtocolException(
                "The FTP TYPE L byte size is outside its permitted range.", null);
        }

        return "L " + byteSize.ToString(CultureInfo.InvariantCulture);
    }
}
