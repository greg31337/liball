namespace cookies;

internal class CookiePrefixValidationResult
{
    internal bool Success;
    internal bool Allowed;
    internal CookieErrorCode ErrorCode;
    internal bool HasSecurePrefix;
    internal bool HasHostPrefix;
    internal bool WasNamelessPrefix;
    internal bool UsedCaseInsensitiveMatch;
    internal bool EnforcementDisabled;
    internal bool SilentlyRejected;
    internal bool OriginWasEvaluated;
    internal bool OriginIsSecure;
    internal CookieIssue[] Issues = [];
}
