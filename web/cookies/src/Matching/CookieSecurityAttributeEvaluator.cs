namespace cookies;

internal static class CookieSecurityAttributeEvaluator
{
    internal static CookieSecurityAttributeResult EvaluateSet(
        SetCookie cookie,
        CookieSetContext context,
        ISecureOriginPolicy secureOriginPolicy,
        StoredCookie? existingCookie)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!IsValidAccessKind(context.AccessKind))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (cookie.Secure)
        {
            if (context.Origin == null || !context.Origin.IsAbsoluteUri)
            {
                return CreateFailure(CookieErrorCode.InvalidHost);
            }

            CookieBooleanResult secureOrigin = CookieSecureOriginEvaluator.Evaluate(
                secureOriginPolicy,
                context.Origin);
            if (!secureOrigin.Success)
            {
                return CreateFailure(CookieErrorCode.InvalidConfiguration);
            }

            if (!secureOrigin.Value)
            {
                return CreateDenied(
                    CookieErrorCode.InsecureSecureCookie,
                    true,
                    false,
                    false);
            }
        }

        if (context.AccessKind == CookieAccessKind.NonHttp && cookie.HttpOnly)
        {
            return CreateDenied(
                CookieErrorCode.HttpOnlyApiViolation,
                cookie.Secure,
                cookie.Secure,
                false);
        }

        if (context.AccessKind == CookieAccessKind.NonHttp &&
            existingCookie != null && existingCookie.HttpOnly)
        {
            return CreateDenied(
                CookieErrorCode.HttpOnlyApiViolation,
                cookie.Secure,
                cookie.Secure,
                true);
        }

        return CreateAllowed(cookie.Secure, cookie.Secure);
    }

    internal static CookieSecurityAttributeResult EvaluateRetrieval(
        StoredCookie cookie,
        CookieRequestContext context,
        ISecureOriginPolicy secureOriginPolicy)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!IsValidAccessKind(context.AccessKind))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (cookie.Secure)
        {
            if (context.RequestUri == null || !context.RequestUri.IsAbsoluteUri)
            {
                return CreateFailure(CookieErrorCode.InvalidHost);
            }

            CookieBooleanResult secureOrigin = CookieSecureOriginEvaluator.Evaluate(
                secureOriginPolicy,
                context.RequestUri);
            if (!secureOrigin.Success)
            {
                return CreateFailure(CookieErrorCode.InvalidConfiguration);
            }

            if (!secureOrigin.Value)
            {
                return CreateDenied(
                    CookieErrorCode.InsecureSecureCookie,
                    true,
                    false,
                    false);
            }
        }

        if (context.AccessKind == CookieAccessKind.NonHttp && cookie.HttpOnly)
        {
            return CreateDenied(
                CookieErrorCode.HttpOnlyApiViolation,
                cookie.Secure,
                cookie.Secure,
                false);
        }

        return CreateAllowed(cookie.Secure, cookie.Secure);
    }

    static bool IsValidAccessKind(CookieAccessKind accessKind)
    {
        return accessKind == CookieAccessKind.Http || accessKind == CookieAccessKind.NonHttp;
    }

    static CookieIssue CreateIssue(CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    static CookieSecurityAttributeResult CreateAllowed(
        bool originWasEvaluated,
        bool originIsSecure)
    {
        CookieSecurityAttributeResult result = new CookieSecurityAttributeResult();
        result.Success = true;
        result.Allowed = true;
        result.ErrorCode = CookieErrorCode.None;
        result.OriginWasEvaluated = originWasEvaluated;
        result.OriginIsSecure = originIsSecure;
        return result;
    }

    static CookieSecurityAttributeResult CreateDenied(
        CookieErrorCode errorCode,
        bool originWasEvaluated,
        bool originIsSecure,
        bool protectedExistingHttpOnly)
    {
        CookieSecurityAttributeResult result = new CookieSecurityAttributeResult();
        result.Success = true;
        result.ErrorCode = errorCode;
        result.OriginWasEvaluated = originWasEvaluated;
        result.OriginIsSecure = originIsSecure;
        result.ProtectedExistingHttpOnly = protectedExistingHttpOnly;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }

    static CookieSecurityAttributeResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieSecurityAttributeResult result = new CookieSecurityAttributeResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
