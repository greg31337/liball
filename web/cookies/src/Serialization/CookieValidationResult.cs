namespace cookies;

internal class CookieValidationResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal CookieIssue[] Issues = [];
}
