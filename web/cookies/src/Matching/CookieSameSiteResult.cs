namespace cookies;

internal class CookieSameSiteResult
{
    internal bool Success;
    internal bool Allowed;
    internal CookieErrorCode ErrorCode;
    internal CookieSameSiteMode EffectiveMode;
    internal bool UsedDefaultMode;
    internal bool UsedLaxAllowingUnsafe;
    internal CookieIssue[] Issues = [];
}
