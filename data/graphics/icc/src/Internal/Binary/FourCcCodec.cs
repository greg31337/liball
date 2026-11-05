namespace icc;

internal static class FourCcCodec
{
    internal static uint Parse(string value)
    {
        if (value == null)
        {
            throw new IccProfileFormatException("A FourCC value cannot be null.");
        }

        if (value.Length == 4)
        {
            return ParseAscii(value);
        }

        if (value.Length == 10 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X'))
        {
            return ParseHex(value);
        }

        throw new IccProfileFormatException("A FourCC value must contain four ASCII characters or 0x followed by eight hexadecimal digits.");
    }

    internal static string Format(uint value)
    {
        int first = (int)((value >> 24) & 255);
        int second = (int)((value >> 16) & 255);
        int third = (int)((value >> 8) & 255);
        int fourth = (int)(value & 255);

        if (IsPrintableAscii(first) && IsPrintableAscii(second) && IsPrintableAscii(third) && IsPrintableAscii(fourth))
        {
            char[] characters =
            {
                (char)first,
                (char)second,
                (char)third,
                (char)fourth
            };
            return new string(characters);
        }

        return FormatHex(value);
    }

    internal static string FormatHex(uint value)
    {
        return $"0x{value:X8}";
    }

    internal static uint ParseAscii(string value)
    {
        int index = 0;
        uint result = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character > 127)
            {
                throw new IccProfileFormatException("A FourCC text value must contain only ASCII characters.");
            }

            result = (result << 8) | (uint)character;
            index = index + 1;
        }

        return result;
    }

    internal static uint ParseHex(string value)
    {
        int index = 2;
        uint result = 0;
        while (index < value.Length)
        {
            result = (result << 4) | HexValue(value[index]);
            index = index + 1;
        }

        return result;
    }

    internal static uint HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return (uint)(character - '0');
        }

        if (character >= 'A' && character <= 'F')
        {
            return (uint)(character - 'A' + 10);
        }

        if (character >= 'a' && character <= 'f')
        {
            return (uint)(character - 'a' + 10);
        }

        throw new IccProfileFormatException("A hexadecimal FourCC value contains a non-hexadecimal character.");
    }

    internal static bool IsPrintableAscii(int value)
    {
        return value >= 32 && value <= 126;
    }
}
