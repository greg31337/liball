namespace languagecodes;

internal enum AsciiCodeLength
{
    Invalid = 0,
    Alpha2 = 2,
    Alpha3 = 3
}

internal static class AsciiCode
{
    internal static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static char ToLowerAscii(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + ('a' - 'A'));
        }

        return value;
    }

    internal static bool LowercaseAscii(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination)
    {
        if (destination.Length < value.Length)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            destination[index] = ToLowerAscii(value[index]);
        }

        return true;
    }

    internal static AsciiCodeLength ClassifyLength(global::System.ReadOnlySpan<char> value)
    {
        if (value.Length == 2)
        {
            return AsciiCodeLength.Alpha2;
        }

        if (value.Length == 3)
        {
            return AsciiCodeLength.Alpha3;
        }

        return AsciiCodeLength.Invalid;
    }

    internal static int FindTagSeparator(global::System.ReadOnlySpan<char> value, int startIndex)
    {
        if (startIndex < 0 || startIndex >= value.Length)
        {
            return -1;
        }

        int index;
        for (index = startIndex; index < value.Length; index++)
        {
            if (value[index] == '-' || value[index] == '_')
            {
                return index;
            }
        }

        return -1;
    }

    internal static bool IsLexicallyValid(global::System.ReadOnlySpan<char> value)
    {
        if (ClassifyLength(value) == AsciiCodeLength.Invalid)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            if (!IsAsciiLetter(value[index]))
            {
                return false;
            }
        }

        return true;
    }
}
