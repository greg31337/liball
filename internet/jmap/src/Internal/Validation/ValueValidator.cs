using System;

namespace jmap;

internal class ValueValidator
{
    internal bool IsId(string? value)
    {
        if (value == null || value.Length < JmapProtocolLimits.MinimumIdLength || value.Length > JmapProtocolLimits.MaximumIdLength)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isUppercase = character >= 'A' && character <= 'Z';
            bool isLowercase = character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isUppercase && !isLowercase && !isDigit && character != '-' && character != '_')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsCreationId(string? value)
    {
        return IsId(value);
    }

    internal bool IsCreationIdReference(string? value)
    {
        if (value == null || value.Length < JmapProtocolLimits.MinimumIdLength + 1 || value.Length > JmapProtocolLimits.MaximumIdLength + 1 || value[0] != '#')
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            bool isUppercase = character >= 'A' && character <= 'Z';
            bool isLowercase = character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isUppercase && !isLowercase && !isDigit && character != '-' && character != '_')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsInt(long value)
    {
        return value >= JmapProtocolLimits.MinimumInt && value <= JmapProtocolLimits.MaximumInt;
    }

    internal bool IsUnsignedInt(long value)
    {
        return value >= JmapProtocolLimits.MinimumUnsignedInt && value <= JmapProtocolLimits.MaximumUnsignedInt;
    }

    internal bool IsDate(string? value)
    {
        if (value == null || value.Length < 20)
        {
            return false;
        }

        if (value[4] != '-' || value[7] != '-' || value[10] != 'T' || value[13] != ':' || value[16] != ':')
        {
            return false;
        }

        if (!AreDateDigits(value))
        {
            return false;
        }

        int year = ReadDateNumber(value, 0, 4);
        int month = ReadDateNumber(value, 5, 2);
        int day = ReadDateNumber(value, 8, 2);
        int hour = ReadDateNumber(value, 11, 2);
        int minute = ReadDateNumber(value, 14, 2);
        int second = ReadDateNumber(value, 17, 2);

        if (month < 1 || month > 12 || day < 1 || day > DaysInMonth(year, month))
        {
            return false;
        }

        if (hour > 23 || minute > 59 || second > 60)
        {
            return false;
        }

        int offsetIndex = 19;
        if (value[offsetIndex] == '.')
        {
            offsetIndex = offsetIndex + 1;
            int fractionStart = offsetIndex;
            bool nonzeroFraction = false;
            while (offsetIndex < value.Length && value[offsetIndex] >= '0' && value[offsetIndex] <= '9')
            {
                if (value[offsetIndex] != '0')
                {
                    nonzeroFraction = true;
                }

                offsetIndex = offsetIndex + 1;
            }

            if (offsetIndex == fractionStart || !nonzeroFraction)
            {
                return false;
            }
        }

        if (offsetIndex >= value.Length)
        {
            return false;
        }

        if (value[offsetIndex] == 'Z')
        {
            return offsetIndex + 1 == value.Length;
        }

        if (value[offsetIndex] != '+' && value[offsetIndex] != '-')
        {
            return false;
        }

        if (offsetIndex + 6 != value.Length || value[offsetIndex + 3] != ':')
        {
            return false;
        }

        if (!IsAsciiDigit(value[offsetIndex + 1]) || !IsAsciiDigit(value[offsetIndex + 2]) || !IsAsciiDigit(value[offsetIndex + 4]) || !IsAsciiDigit(value[offsetIndex + 5]))
        {
            return false;
        }

        int offsetHour = ((value[offsetIndex + 1] - '0') * 10) + (value[offsetIndex + 2] - '0');
        int offsetMinute = ((value[offsetIndex + 4] - '0') * 10) + (value[offsetIndex + 5] - '0');
        return offsetHour <= 23 && offsetMinute <= 59;
    }

    internal bool IsUtcDate(string? value)
    {
        return IsDate(value) && value != null && value[value.Length - 1] == 'Z';
    }

    internal bool IsCapability(string? value)
    {
        if (!IsValidUnicode(value) || value == null || value.Length == 0)
        {
            return false;
        }

        return Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }

    internal bool IsMethodName(string? value)
    {
        if (value == null || value.Length < 3 || value.Length > 255)
        {
            return false;
        }

        int slashIndex = -1;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '/')
            {
                if (slashIndex >= 0 || index == 0 || index == value.Length - 1)
                {
                    return false;
                }

                slashIndex = index;
            }
            else if (character < '!' || character > '~')
            {
                return false;
            }

            index = index + 1;
        }

        return slashIndex > 0;
    }

    internal bool IsKeyword(string? value)
    {
        if (value == null || value.Length < JmapProtocolLimits.MinimumKeywordLength || value.Length > JmapProtocolLimits.MaximumKeywordLength)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '!' || character > '~' || character == '(' || character == ')' || character == '{' || character == ']' || character == '%' || character == '*' || character == '"' || character == '\\')
            {
                return false;
            }

            if (character >= 'A' && character <= 'Z')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsMailboxRole(string? value)
    {
        if (value == null || value.Length < 1 || value.Length > 255)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '!' || character > '~' || character == '(' || character == ')' || character == '{' || character == ']' || character == '%' || character == '*' || character == '"' || character == '\\')
            {
                return false;
            }

            if (character >= 'A' && character <= 'Z')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsMediaType(string? value)
    {
        if (value == null || value.Length < 3 || value.Length > 255)
        {
            return false;
        }

        int slashIndex = -1;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '/')
            {
                if (slashIndex >= 0)
                {
                    return false;
                }

                slashIndex = index;
            }

            index = index + 1;
        }

        if (slashIndex < 1 || slashIndex > 127 || value.Length - slashIndex - 1 < 1 || value.Length - slashIndex - 1 > 127)
        {
            return false;
        }

        if (!IsRestrictedNameFirst(value[0]) || !IsRestrictedNameFirst(value[slashIndex + 1]))
        {
            return false;
        }

        index = 1;
        while (index < value.Length)
        {
            if (index == slashIndex)
            {
                index = index + 2;
                continue;
            }

            if (!IsRestrictedNameCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal long GetUtf8Size(string? value)
    {
        if (value == null)
        {
            return -1L;
        }

        long size = 0L;
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = value[index];
            if (codePoint >= 55296 && codePoint <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 || value[index + 1] > 57343)
                {
                    return -1L;
                }

                codePoint = 65536 + ((codePoint - 55296) * 1024) + (value[index + 1] - 56320);
                index = index + 1;
            }
            else if (codePoint >= 56320 && codePoint <= 57343)
            {
                return -1L;
            }

            if (IsUnicodeNoncharacter(codePoint))
            {
                return -1L;
            }

            if (codePoint <= 127)
            {
                size = size + 1L;
            }
            else if (codePoint <= 2047)
            {
                size = size + 2L;
            }
            else if (codePoint <= 65535)
            {
                size = size + 3L;
            }
            else
            {
                size = size + 4L;
            }

            index = index + 1;
        }

        return size;
    }

    internal bool IsUtf8WithinSize(string? value, long maximumBytes)
    {
        if (maximumBytes < 0L)
        {
            return false;
        }

        long size = GetUtf8Size(value);
        return size >= 0L && size <= maximumBytes;
    }

    internal bool AreDateDigits(string value)
    {
        int[] positions = new int[] { 0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, 17, 18 };
        int index = 0;
        while (index < positions.Length)
        {
            if (!IsAsciiDigit(value[positions[index]]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int ReadDateNumber(string value, int offset, int length)
    {
        int number = 0;
        int index = offset;
        while (index < offset + length)
        {
            number = (number * 10) + (value[index] - '0');
            index = index + 1;
        }

        return number;
    }

    internal int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool leapYear = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            if (leapYear)
            {
                return 29;
            }

            return 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    internal bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal bool IsRestrictedNameFirst(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9');
    }

    internal bool IsRestrictedNameCharacter(char character)
    {
        return IsRestrictedNameFirst(character) || character == '!' || character == '#' || character == '$' || character == '&' || character == '-' || character == '^' || character == '_' || character == '.' || character == '+';
    }

    internal bool IsValidUnicode(string? value)
    {
        return GetUtf8Size(value) >= 0L;
    }

    internal bool IsUnicodeNoncharacter(int codePoint)
    {
        return (codePoint >= 64976 && codePoint <= 65007) || (codePoint & 65535) == 65534 || (codePoint & 65535) == 65535;
    }
}
