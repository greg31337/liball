using System;

namespace csv;

/// <summary>
/// Detects and handles text that spreadsheets may interpret as a formula.
/// </summary>
internal static class CsvInjectionGuard
{
    /// <summary>
    /// Applies the selected spreadsheet formula-injection policy.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="mode">The policy to apply.</param>
    /// <returns>The original, stripped, or apostrophe-prefixed value, depending on <c>mode</c>.</returns>
    /// <exception cref="InvalidOperationException">The value is dangerous and the mode is <see cref="CsvInjectionMode.Error"/>.</exception>
    internal static string Apply(string value, CsvInjectionMode mode)
    {
        if (!IsDangerous(value))
        {
            return value;
        }

        if (mode == CsvInjectionMode.None)
        {
            return value;
        }

        if (mode == CsvInjectionMode.Strip)
        {
            string stripped = StripDangerousPrefix(value);
            return stripped;
        }

        if (mode == CsvInjectionMode.Escape)
        {
            string escaped = "'" + value;
            return escaped;
        }

        throw new InvalidOperationException("Spreadsheet formula injection content is not allowed.");
    }

    /// <summary>
    /// Checks whether the first non-space character can start a spreadsheet formula.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <returns><c>true</c> for formula-like text; otherwise <c>false</c>.</returns>
    internal static bool IsDangerous(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = SkipLeadingSpaces(value);
        if (index >= value.Length)
        {
            return false;
        }

        char character = value[index];
        if (IsDangerousPrefixCharacter(character))
        {
            return true;
        }

        return false;
    }

    internal static string StripDangerousPrefix(string value)
    {
        int index = SkipLeadingSpaces(value);
        while (index < value.Length && IsDangerousPrefixCharacter(value[index]))
        {
            index = index + 1;
        }

        string stripped = value.Substring(index);
        return stripped;
    }

    internal static int SkipLeadingSpaces(string value)
    {
        int index = 0;
        while (index < value.Length && value[index] == ' ')
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool IsDangerousPrefixCharacter(char character)
    {
        if (character == '=' || character == '+' || character == '-' || character == '@')
        {
            return true;
        }

        if (character == '\t' || character == '\r' || character == '\n')
        {
            return true;
        }

        if (character == '\uFF1D' || character == '\uFF0B' || character == '\uFF0D' || character == '\uFF20')
        {
            return true;
        }

        return false;
    }
}
