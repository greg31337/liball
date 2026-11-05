namespace cookies;

internal class CookieSecureOverlayResult
{
    internal bool Success;
    internal bool Allowed;
    internal CookieErrorCode ErrorCode;
    internal bool OriginWasEvaluated;
    internal bool OriginIsSecure;
    internal bool HasOverlappingSecureCookie;
    internal int OverlappingCookieIndex = -1;
    internal StoredCookie? OverlappingCookie;
    internal CookieIssue[] Issues = [];
}
