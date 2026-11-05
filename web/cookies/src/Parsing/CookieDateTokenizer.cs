namespace cookies;

internal static class CookieDateTokenizer
{
    internal static CookieDateTokenResult ReadNextDateToken(global::System.ReadOnlySpan<char> input, int start)
    {
        CookieDateTokenResult result = new CookieDateTokenResult();
        if (start < 0 || start > input.Length)
        {
            result.InvalidBounds = true;
            result.NextOffset = start;
            return result;
        }

        int index = start;
        while (index < input.Length && CookieCharacterRules.IsDateDelimiter(input[index]))
        {
            index = index + 1;
        }

        if (index == input.Length)
        {
            result.NextOffset = input.Length;
            return result;
        }

        int tokenStart = index;
        while (index < input.Length && !CookieCharacterRules.IsDateDelimiter(input[index]))
        {
            index = index + 1;
        }

        result.HasToken = true;
        result.Start = tokenStart;
        result.Length = index - tokenStart;
        result.NextOffset = index;
        return result;
    }
}
