namespace cookies;

internal static class CookieCompatibleDateParser
{
    internal static CookieDateParseResult ParseDateWithMissingSeconds(
        global::System.ReadOnlySpan<char> input)
    {
        int offset = 0;
        int insertionOffset = -1;
        while (offset < input.Length)
        {
            CookieDateTokenResult tokenResult = CookieDateTokenizer.ReadNextDateToken(input, offset);
            if (!tokenResult.HasToken)
            {
                break;
            }

            global::System.ReadOnlySpan<char> token = input.Slice(tokenResult.Start, tokenResult.Length);
            if (IsHourMinuteToken(token))
            {
                if (insertionOffset >= 0)
                {
                    return new CookieDateParseResult();
                }

                insertionOffset = tokenResult.Start + tokenResult.Length;
            }

            offset = tokenResult.NextOffset;
        }

        if (insertionOffset < 0)
        {
            return new CookieDateParseResult();
        }

        char[] normalized = new char[input.Length + 3];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < insertionOffset)
        {
            normalized[destinationIndex] = input[sourceIndex];
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 1;
        }

        normalized[destinationIndex] = ':';
        normalized[destinationIndex + 1] = '0';
        normalized[destinationIndex + 2] = '0';
        destinationIndex = destinationIndex + 3;
        while (sourceIndex < input.Length)
        {
            normalized[destinationIndex] = input[sourceIndex];
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 1;
        }

        return CookieDateParser.ParseCookieDate(normalized);
    }

    static bool IsHourMinuteToken(global::System.ReadOnlySpan<char> token)
    {
        int colonOffset = -1;
        int index = 0;
        while (index < token.Length)
        {
            char character = token[index];
            if (character == ':')
            {
                if (colonOffset >= 0)
                {
                    return false;
                }

                colonOffset = index;
            }
            else if (!CookieCharacterRules.IsAsciiDigit(character))
            {
                return false;
            }

            index = index + 1;
        }

        return colonOffset >= 1 && colonOffset <= 2 &&
            token.Length - colonOffset - 1 >= 1 &&
            token.Length - colonOffset - 1 <= 2;
    }
}
