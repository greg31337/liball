namespace uri;

internal static class UrnGrammar
{
    internal static bool IsNamespaceIdentifierCharacter(char value)
    {
        return Ascii.IsAlphaNumeric(value) || value == '-';
    }

    internal static bool StartsWithIgnoreCase(
        System.ReadOnlySpan<char> text,
        string prefix)
    {
        if (text.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (AsciiCase.FoldToLower(text[index]) !=
                AsciiCase.FoldToLower(prefix[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsInformalNamespaceIdentifier(
        System.ReadOnlySpan<char> namespaceIdentifier)
    {
        if (!StartsWithIgnoreCase(namespaceIdentifier, "urn-") ||
            namespaceIdentifier.Length == 4 ||
            namespaceIdentifier[4] == '0')
        {
            return false;
        }

        int index = 4;
        while (index < namespaceIdentifier.Length)
        {
            if (!Ascii.IsDigit(namespaceIdentifier[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
