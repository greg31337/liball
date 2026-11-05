namespace http;

internal static class HttpGrammar
{
    internal static bool IsTokenCharacter(byte value)
    {
        if (value >= 0x30 && value <= 0x39)
        {
            return true;
        }

        if (value >= 0x41 && value <= 0x5a)
        {
            return true;
        }

        if (value >= 0x61 && value <= 0x7a)
        {
            return true;
        }

        return value == 0x21 || value == 0x23 || value == 0x24 || value == 0x25 ||
            value == 0x26 || value == 0x27 || value == 0x2a || value == 0x2b ||
            value == 0x2d || value == 0x2e || value == 0x5e || value == 0x5f ||
            value == 0x60 || value == 0x7c || value == 0x7e;
    }

    internal static bool IsQuotedTextCharacter(byte value)
    {
        if (value == 0x09 || value == 0x20 || value == 0x21)
        {
            return true;
        }

        if (value >= 0x23 && value <= 0x5b)
        {
            return true;
        }

        if (value >= 0x5d && value <= 0x7e)
        {
            return true;
        }

        return value >= 0x80;
    }

    internal static bool IsQuotedPairCharacter(byte value)
    {
        return value == 0x09 || value >= 0x20 && value <= 0x7e || value >= 0x80;
    }
}
