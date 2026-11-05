namespace uri;

internal static class UriGrammar
{
    internal static bool IsSchemeStartCharacter(char value)
    {
        return Ascii.IsAlphabetic(value);
    }

    internal static bool IsSchemeCharacter(char value)
    {
        return Ascii.IsAlphaNumeric(value) || value == '+' || value == '-' || value == '.';
    }

    internal static bool IsIpFutureAddressCharacter(char value)
    {
        return Ascii.IsUnreserved(value) || Ascii.IsSubDelimiter(value) || value == ':';
    }
}
