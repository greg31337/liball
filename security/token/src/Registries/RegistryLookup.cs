using System;
namespace token;

internal static class RegistryLookup
{
    internal static bool Contains(string[]? values, string? value, bool ignoreCase)
    {
        if (value == null)
        {
            return false;
        }

        StringComparison comparison = ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            if (string.Equals(values[index], value, comparison))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static string[] Copy(string[] values)
    {
        string[] copy = new string[values.Length];
        values.CopyTo(copy, 0);
        return copy;
    }

    internal static bool Contains(int[]? values, int value)
    {
        int index = 0;
        while (index < (values ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static int IndexOf(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static int[] Copy(int[] values)
    {
        int[] copy = new int[values.Length];
        values.CopyTo(copy, 0);
        return copy;
    }
}
