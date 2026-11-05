namespace cookies;

internal class CookieStringSerializationResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal string Value = "";
    internal CookieIssue[] Issues = [];
}
