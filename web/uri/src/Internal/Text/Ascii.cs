namespace uri;

internal static class Ascii
{
    internal static bool IsAscii(char value)
    {
        return value <= '\u007f';
    }

    internal static bool IsAlphabetic(char value)
    {
        return IsUppercaseAlphabetic(value) || IsLowercaseAlphabetic(value);
    }

    internal static bool IsUppercaseAlphabetic(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal static bool IsLowercaseAlphabetic(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsAlphaNumeric(char value)
    {
        return IsAlphabetic(value) || IsDigit(value);
    }

    internal static bool IsHexDigit(char value)
    {
        return IsDigit(value) ||
            value >= 'A' && value <= 'F' ||
            value >= 'a' && value <= 'f';
    }

    internal static int GetHexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static int GetHexByte(char high, char low)
    {
        int highValue = GetHexValue(high);
        int lowValue = GetHexValue(low);
        if (highValue < 0 || lowValue < 0)
        {
            return -1;
        }

        return highValue * 16 + lowValue;
    }

    internal static char GetUpperHexCharacter(int value)
    {
        if (value >= 0 && value <= 9)
        {
            return (char)('0' + value);
        }

        if (value >= 10 && value <= 15)
        {
            return (char)('A' + value - 10);
        }

        throw new System.ArgumentOutOfRangeException("value");
    }

    internal static bool IsControl(char value)
    {
        return value <= '\u001f' || value == '\u007f';
    }

    internal static bool IsVisible(char value)
    {
        return value >= '\u0021' && value <= '\u007e';
    }

    internal static bool IsUnreserved(char value)
    {
        return IsAlphaNumeric(value) ||
            value == '-' ||
            value == '.' ||
            value == '_' ||
            value == '~';
    }

    internal static bool IsGeneralDelimiter(char value)
    {
        return UriCharacterSets.GeneralDelimiters.IndexOf(value) >= 0;
    }

    internal static bool IsSubDelimiter(char value)
    {
        return UriCharacterSets.SubDelimiters.IndexOf(value) >= 0;
    }

    internal static bool IsReserved(char value)
    {
        return IsGeneralDelimiter(value) || IsSubDelimiter(value);
    }
}
