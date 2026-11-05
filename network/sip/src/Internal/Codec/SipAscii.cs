namespace sip;

internal static class SipAscii
{
    internal static bool IsTokenCharacter(int value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        return value == '-' || value == '.' || value == '!' || value == '%' || value == '*'
            || value == '_' || value == '+' || value == '`' || value == '\'' || value == '~';
    }

    internal static bool IsToken(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsTokenCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool EqualsIgnoreCase(string? left, string? right)
    {
        if (System.Object.ReferenceEquals(left, right))
        {
            return true;
        }

        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (FoldCase(left[index]) != FoldCase(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CompareIgnoreCase(string? left, string? right)
    {
        if (System.Object.ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        int commonLength = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < commonLength)
        {
            int leftValue = FoldCase(left[index]);
            int rightValue = FoldCase(right[index]);
            if (leftValue < rightValue)
            {
                return -1;
            }

            if (leftValue > rightValue)
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

    static int FoldCase(int value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value + ('a' - 'A');
        }

        return value;
    }
}
