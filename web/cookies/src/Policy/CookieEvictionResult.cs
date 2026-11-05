namespace cookies;

internal class CookieEvictionResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal StoredCookie[] RetainedCookies = [];
    internal CookieEvictionEntry[] Evictions = [];
    internal CookieIssue[] Issues = [];
}
