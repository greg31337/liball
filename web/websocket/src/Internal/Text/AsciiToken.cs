using System;

namespace websocket;

internal static class AsciiToken
{
    internal static bool IsCharacter(char value)
    {
        if ((value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'Z') ||
            (value >= 'a' && value <= 'z'))
        {
            return true;
        }

        return value == '!' ||
            value == '#' ||
            value == '$' ||
            value == '%' ||
            value == '&' ||
            value == '\'' ||
            value == '*' ||
            value == '+' ||
            value == '-' ||
            value == '.' ||
            value == '^' ||
            value == '_' ||
            value == '`' ||
            value == '|' ||
            value == '~';
    }

    internal static bool IsValid(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int ReadLength(ReadOnlySpan<char> source, int offset)
    {
        if (offset < 0 || offset > source.Length)
        {
            return 0;
        }

        int index = offset;
        while (index < source.Length && IsCharacter(source[index]))
        {
            index = index + 1;
        }

        return index - offset;
    }

    internal static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            char leftCharacter = left[index];
            char rightCharacter = right[index];
            if (leftCharacter >= 'A' && leftCharacter <= 'Z')
            {
                leftCharacter = (char)(leftCharacter + ('a' - 'A'));
            }

            if (rightCharacter >= 'A' && rightCharacter <= 'Z')
            {
                rightCharacter = (char)(rightCharacter + ('a' - 'A'));
            }

            if (leftCharacter != rightCharacter)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
