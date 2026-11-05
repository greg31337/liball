namespace cookies;

internal static class CookieIssueFactory
{
    internal static CookieIssue CreateIssue(
        CookieErrorCode errorCode,
        CookieParseSeverity severity,
        int offset,
        int length,
        global::System.ReadOnlySpan<char> fragment)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = severity;
        issue.HasOffset = true;
        issue.Offset = offset;
        issue.Length = length;
        issue.SafeFragment = CreateSafeFragment(fragment);
        return issue;
    }

    static string CreateSafeFragment(global::System.ReadOnlySpan<char> fragment)
    {
        int length = fragment.Length;
        if (length > 32)
        {
            length = 32;
        }

        char[] characters = new char[length];
        int index = 0;
        while (index < length)
        {
            char character = fragment[index];
            if (character < (char)0x20 || character > (char)0x7e || character == (char)0x7f)
            {
                characters[index] = '?';
            }
            else
            {
                characters[index] = character;
            }

            index = index + 1;
        }

        return new string(characters);
    }
}
