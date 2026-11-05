namespace uri;

internal static class AsciiCase
{
    internal static char FoldToLower(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + ('a' - 'A'));
        }

        return value;
    }

    internal static char FoldToUpper(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return (char)(value - ('a' - 'A'));
        }

        return value;
    }

    internal static bool EqualsIgnoreCase(string? left, string? right)
    {
        if (left == null)
        {
            return right == null;
        }

        if (right == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (FoldToLower(left[index]) != FoldToLower(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CompareIgnoreCase(string? left, string? right)
    {
        if (left == null)
        {
            if (right == null)
            {
                return 0;
            }

            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        int commonLength = left.Length;
        if (right.Length < commonLength)
        {
            commonLength = right.Length;
        }

        int index = 0;
        while (index < commonLength)
        {
            char leftCharacter = FoldToLower(left[index]);
            char rightCharacter = FoldToLower(right[index]);
            if (leftCharacter < rightCharacter)
            {
                return -1;
            }

            if (leftCharacter > rightCharacter)
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }
}
