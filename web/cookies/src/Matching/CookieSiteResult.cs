namespace cookies;

internal class CookieSiteResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal string Site = "";
    internal string SiteHost = "";
    internal bool UsedHostFallback;
    internal CookieIssue[] Issues = [];
}
