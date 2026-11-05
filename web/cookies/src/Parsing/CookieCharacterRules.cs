namespace cookies;

internal static class CookieCharacterRules
{
    internal static bool IsAscii(char character)
    {
        return character <= (char)0x7f;
    }

    internal static bool IsAsciiAlpha(char character)
    {
        return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
    }

    internal static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal static bool IsAsciiWhitespace(char character)
    {
        return character == ' ' || character == '\t';
    }

    internal static bool IsControl(char character)
    {
        return character <= (char)0x1f ||
            character >= (char)0x7f && character <= (char)0x9f;
    }

    internal static bool IsHeaderInjection(char character)
    {
        return character == '\0' || character == '\r' || character == '\n';
    }

    internal static bool IsDateDelimiter(char character)
    {
        return character == (char)0x09 ||
            character >= (char)0x20 && character <= (char)0x2f ||
            character >= (char)0x3b && character <= (char)0x40 ||
            character >= (char)0x5b && character <= (char)0x60 ||
            character >= (char)0x7b && character <= (char)0x7e;
    }

    internal static bool IsTokenCharacter(char character)
    {
        if (IsAsciiAlpha(character) || IsAsciiDigit(character))
        {
            return true;
        }

        return character == '!' || character == '#' || character == '$' || character == '%' ||
            character == '&' || character == '\'' || character == '*' || character == '+' ||
            character == '-' || character == '.' || character == '^' || character == '_' ||
            character == '`' || character == '|' || character == '~';
    }

    internal static bool IsCookieOctet(char character)
    {
        return character == (char)0x21 ||
            character >= (char)0x23 && character <= (char)0x2b ||
            character >= (char)0x2d && character <= (char)0x3a ||
            character >= (char)0x3c && character <= (char)0x5b ||
            character >= (char)0x5d && character <= (char)0x7e;
    }

    internal static bool IsAttributeValueOctet(char character)
    {
        return character >= (char)0x20 && character <= (char)0x3a ||
            character >= (char)0x3c && character <= (char)0x7e;
    }
}
