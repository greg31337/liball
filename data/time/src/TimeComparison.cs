using System;

namespace time;

internal static class TimeComparison
{
    internal static int CompareInt(int left, int right)
    {
        if (left < right)
        {
            return -1;
        }

        if (left > right)
        {
            return 1;
        }

        return 0;
    }

    internal static int CompareLong(long left, long right)
    {
        if (left < right)
        {
            return -1;
        }

        if (left > right)
        {
            return 1;
        }

        return 0;
    }

    internal static int CompareStrings(string? left, string? right)
    {
        int comparison = string.Compare(left, right, StringComparison.Ordinal);
        if (comparison < 0)
        {
            return -1;
        }

        if (comparison > 0)
        {
            return 1;
        }

        return 0;
    }

    internal static bool EqualStrings(string? left, string? right)
    {
        return CompareStrings(left, right) == 0;
    }
}
