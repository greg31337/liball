using System;

namespace htmlgui;

internal static class ValueComparer
{
    internal static readonly StringComparer Ordinal = StringComparer.Ordinal;
    internal static readonly StringComparer OrdinalIgnoreCase = StringComparer.OrdinalIgnoreCase;

    internal static bool TextEquals(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    internal static int CompareText(string? left, string? right)
    {
        return string.Compare(left, right, StringComparison.Ordinal);
    }

    internal static bool TextEqualsIgnoreCase(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static int CompareTextIgnoreCase(string? left, string? right)
    {
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static string[] SortText(string[] values)
    {
        Guard.ArgumentNotNull(values, "values");
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        Array.Sort(result, Ordinal);
        return result;
    }
}
