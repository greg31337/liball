namespace cookies;

internal class CookieSecurityAttributeResult
{
    internal bool Success;
    internal bool Allowed;
    internal CookieErrorCode ErrorCode;
    internal bool OriginWasEvaluated;
    internal bool OriginIsSecure;
    internal bool ProtectedExistingHttpOnly;
    internal CookieIssue[] Issues = [];
}
