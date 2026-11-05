namespace cookies;

internal static class CookieSpanScanner
{
    internal static CookieScanResult ScanCookiePairSegment(global::System.ReadOnlySpan<char> input, int start, int maximumCharacters)
    {
        return ScanSegment(input, start, maximumCharacters);
    }

    internal static CookieScanResult ScanAttributeSegment(global::System.ReadOnlySpan<char> input, int start, int maximumCharacters)
    {
        return ScanSegment(input, start, maximumCharacters);
    }

    static CookieScanResult ScanSegment(global::System.ReadOnlySpan<char> input, int start, int maximumCharacters)
    {
        CookieScanResult result = new CookieScanResult();
        if (start < 0 || start > input.Length || maximumCharacters < 0)
        {
            result.InvalidBounds = true;
            result.NextOffset = start;
            return result;
        }

        int available = input.Length - start;
        int scanEnd = input.Length;
        if (maximumCharacters < available)
        {
            scanEnd = start + maximumCharacters;
        }

        int index = start;
        int equalsOffset = -1;
        while (index < scanEnd && input[index] != ';')
        {
            if (equalsOffset < 0 && input[index] == '=')
            {
                equalsOffset = index;
            }

            index = index + 1;
        }

        bool hasDelimiter = false;
        if (index < input.Length && input[index] == ';')
        {
            hasDelimiter = true;
        }
        else if (index < input.Length)
        {
            result.LimitExceeded = true;
        }

        int segmentStart = start;
        int segmentEnd = index;
        while (segmentStart < segmentEnd && CookieCharacterRules.IsAsciiWhitespace(input[segmentStart]))
        {
            segmentStart = segmentStart + 1;
        }

        while (segmentEnd > segmentStart && CookieCharacterRules.IsAsciiWhitespace(input[segmentEnd - 1]))
        {
            segmentEnd = segmentEnd - 1;
        }

        result.SegmentStart = segmentStart;
        result.SegmentLength = segmentEnd - segmentStart;
        if (equalsOffset >= 0)
        {
            int nameStart = start;
            int nameEnd = equalsOffset;
            while (nameStart < nameEnd && CookieCharacterRules.IsAsciiWhitespace(input[nameStart]))
            {
                nameStart = nameStart + 1;
            }

            while (nameEnd > nameStart && CookieCharacterRules.IsAsciiWhitespace(input[nameEnd - 1]))
            {
                nameEnd = nameEnd - 1;
            }

            int valueStart = equalsOffset + 1;
            int valueEnd = index;
            while (valueStart < valueEnd && CookieCharacterRules.IsAsciiWhitespace(input[valueStart]))
            {
                valueStart = valueStart + 1;
            }

            while (valueEnd > valueStart && CookieCharacterRules.IsAsciiWhitespace(input[valueEnd - 1]))
            {
                valueEnd = valueEnd - 1;
            }

            result.NameStart = nameStart;
            result.NameLength = nameEnd - nameStart;
            result.HasValue = true;
            result.ValueStart = valueStart;
            result.ValueLength = valueEnd - valueStart;
        }
        else
        {
            result.NameStart = segmentStart;
            result.NameLength = segmentEnd - segmentStart;
            result.ValueStart = segmentEnd;
        }

        result.NextOffset = hasDelimiter ? index + 1 : index;
        result.Success = !result.LimitExceeded;
        return result;
    }
}
