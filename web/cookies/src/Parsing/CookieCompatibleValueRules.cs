namespace cookies;

internal static class CookieCompatibleValueRules
{
    internal static bool HasQuoteBoundary(global::System.ReadOnlySpan<char> value)
    {
        return value.Length > 0 &&
            (value[0] == '"' || value[value.Length - 1] == '"');
    }

    internal static bool IsSafeQuotedValue(global::System.ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < (char)0x20 || character > (char)0x7e ||
                character == '"' || character == ';' || character == '\\')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
