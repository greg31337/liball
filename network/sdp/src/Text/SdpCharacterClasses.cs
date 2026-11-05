namespace sdp;

internal static class SdpCharacterClasses
{
    internal static bool IsTokenCharacter(char value)
    {
        return value == '!' || value >= '#' && value <= '\'' || value >= '*' && value <= '+' || value >= '-' && value <= '.' || value >= '0' && value <= '9' || value >= 'A' && value <= 'Z' || value >= '^' && value <= '~';
    }

    internal static bool IsTokenByte(byte value)
    {
        return value == 33 || value >= 35 && value <= 39 || value >= 42 && value <= 43 || value >= 45 && value <= 46 || value >= 48 && value <= 57 || value >= 65 && value <= 90 || value >= 94 && value <= 126;
    }

    internal static bool IsNonWhitespaceCharacter(char value)
    {
        return value >= '!' && value <= '~' || value >= 128;
    }

    internal static bool IsNonWhitespaceByte(byte value)
    {
        return value >= 33 && value <= 126 || value >= 128;
    }

    internal static bool IsByteStringCharacter(char value)
    {
        return value != '\0' && value != '\r' && value != '\n';
    }

    internal static bool IsByteStringByte(byte value)
    {
        return value != 0 && value != 10 && value != 13;
    }
}
