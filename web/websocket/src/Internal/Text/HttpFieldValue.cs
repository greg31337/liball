using System;

namespace websocket;

internal static class HttpFieldValue
{
    internal static int SkipOptionalWhitespace(ReadOnlySpan<char> source, int offset)
    {
        if (offset < 0 || offset > source.Length)
        {
            return -1;
        }

        int index = offset;
        while (index < source.Length &&
               (source[index] == ' ' || source[index] == '\t'))
        {
            index = index + 1;
        }

        return index;
    }

    internal static string TrimOptionalWhitespace(ReadOnlySpan<char> source)
    {
        int start = SkipOptionalWhitespace(source, 0);
        int end = source.Length;
        while (end > start && (source[end - 1] == ' ' || source[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return new string(source.Slice(start, end - start));
    }

    internal static bool IsSafe(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != '\t' &&
                (character < ' ' || character == (char)127 || character > (char)255))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
