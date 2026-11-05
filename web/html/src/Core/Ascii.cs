using System;

namespace html;

internal static class Ascii
{
    internal static bool IsAscii(int value)
    {
        return value >= 0 && value <= 127;
    }

    internal static bool IsUpper(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal static bool IsLower(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal static bool IsAlpha(char value)
    {
        return IsUpper(value) || IsLower(value);
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsAlphanumeric(char value)
    {
        return IsAlpha(value) || IsDigit(value);
    }

    internal static bool IsHexDigit(char value)
    {
        return IsDigit(value)
            || value >= 'A' && value <= 'F'
            || value >= 'a' && value <= 'f';
    }

    internal static int GetHexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static bool IsHtmlWhitespace(char value)
    {
        return value == '\t' || value == '\n' || value == '\f' || value == '\r' || value == ' ';
    }

    internal static bool IsControl(char value)
    {
        return value <= 31 || value == 127;
    }

    internal static bool IsTagNameDelimiter(char value)
    {
        return IsHtmlWhitespace(value) || value == '/' || value == '>';
    }

    internal static bool IsAttributeNameDelimiter(char value)
    {
        return IsHtmlWhitespace(value) || value == '/' || value == '>' || value == '=';
    }

    internal static char ToLower(char value)
    {
        if (IsUpper(value))
        {
            return (char)(value + ('a' - 'A'));
        }

        return value;
    }

    internal static char ToUpper(char value)
    {
        if (IsLower(value))
        {
            return (char)(value - ('a' - 'A'));
        }

        return value;
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
            if (ToLower(left[index]) != ToLower(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool StartsWithIgnoreCase(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix)
    {
        if (prefix.Length > value.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (ToLower(value[index]) != ToLower(prefix[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool EndsWithIgnoreCase(ReadOnlySpan<char> value, ReadOnlySpan<char> suffix)
    {
        if (suffix.Length > value.Length)
        {
            return false;
        }

        int start = value.Length - suffix.Length;
        int index = 0;
        while (index < suffix.Length)
        {
            if (ToLower(value[start + index]) != ToLower(suffix[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
