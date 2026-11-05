namespace cookies;

internal class CookieExpiryResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal bool HasExpires;
    internal global::System.DateTimeOffset Expires;
    internal bool Persistent;
    internal bool IsImmediateDeletion;
    internal bool UsedMaxAge;
    internal bool WasCapped;
    internal bool ArithmeticSaturated;
    internal CookieIssue[] Issues = [];
}
