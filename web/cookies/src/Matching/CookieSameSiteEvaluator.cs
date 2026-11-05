namespace cookies;

internal static class CookieSameSiteEvaluator
{
    internal static CookieSameSiteResult EvaluateCreation(
        SetCookie cookie,
        CookieSetContext context)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        CookieJarOptions options = context.Options;
        if (!IsValidOptions(options) || !IsValidSameSiteContext(context.SameSiteContext) ||
            !IsValidAccessKind(context.AccessKind) ||
            !IsValidSuppliedMode(cookie.HasSameSite, cookie.SameSiteMode))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieSameSiteMode effectiveMode = ResolveEffectiveMode(
            cookie.HasSameSite,
            cookie.SameSiteMode,
            options);
        bool usedDefaultMode = !cookie.HasSameSite ||
            cookie.SameSiteMode == CookieSameSiteMode.Default;

        if (RequiresSecureNone(cookie, options) && !cookie.Secure)
        {
            return CreateDenied(effectiveMode, usedDefaultMode);
        }

        if (!options.EnforceSameSite || effectiveMode == CookieSameSiteMode.None)
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        if (context.SameSiteContext == CookieSameSiteContext.SameSite)
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        if (context.AccessKind == CookieAccessKind.Http && context.IsTopLevelNavigation)
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        return CreateDenied(effectiveMode, usedDefaultMode);
    }

    internal static CookieSameSiteResult EvaluateRetrieval(
        StoredCookie cookie,
        CookieRequestContext context,
        CookieJarOptions options,
        global::System.DateTimeOffset evaluationTime)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!IsValidOptions(options) || !IsValidSameSiteContext(context.SameSiteContext) ||
            !IsValidAccessKind(context.AccessKind) || context.RequestMethod == null ||
            !IsValidSuppliedMode(cookie.HasSameSite, cookie.SameSiteMode))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieSameSiteMode effectiveMode = ResolveEffectiveMode(
            cookie.HasSameSite,
            cookie.SameSiteMode,
            options);
        bool usedDefaultMode = !cookie.HasSameSite ||
            cookie.SameSiteMode == CookieSameSiteMode.Default;
        if (!options.EnforceSameSite || effectiveMode == CookieSameSiteMode.None)
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        if (IsEffectivelySameSite(context))
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        if (effectiveMode == CookieSameSiteMode.Strict)
        {
            return CreateDenied(effectiveMode, usedDefaultMode);
        }

        if (context.AccessKind != CookieAccessKind.Http || !context.IsTopLevelNavigation)
        {
            return CreateDenied(effectiveMode, usedDefaultMode);
        }

        if (IsSafeMethod(context.RequestMethod))
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, false);
        }

        if (CanUseLaxAllowingUnsafe(cookie, options, evaluationTime))
        {
            return CreateAllowed(effectiveMode, usedDefaultMode, true);
        }

        return CreateDenied(effectiveMode, usedDefaultMode);
    }

    internal static bool IsSafeMethod(string requestMethod)
    {
        return requestMethod == "GET" || requestMethod == "HEAD" ||
            requestMethod == "OPTIONS" || requestMethod == "TRACE";
    }

    static bool IsEffectivelySameSite(CookieRequestContext context)
    {
        if (context.RedirectChainIsCrossSite)
        {
            return false;
        }

        if (context.IsReload && context.IsTopLevelNavigation && !context.HasUserActivation)
        {
            return true;
        }

        return context.SameSiteContext == CookieSameSiteContext.SameSite;
    }

    static bool CanUseLaxAllowingUnsafe(
        StoredCookie cookie,
        CookieJarOptions options,
        global::System.DateTimeOffset evaluationTime)
    {
        if (!UsesModernRules(options.ComplianceMode) || !options.EnableLaxAllowingUnsafe ||
            cookie.HasSameSite || options.DefaultSameSiteMode == CookieSameSiteMode.Strict ||
            options.DefaultSameSiteMode == CookieSameSiteMode.None)
        {
            return false;
        }

        global::System.DateTimeOffset creationTime = cookie.CreationTime.ToUniversalTime();
        global::System.DateTimeOffset utcEvaluationTime = evaluationTime.ToUniversalTime();
        if (utcEvaluationTime <= creationTime)
        {
            return true;
        }

        long ageTicks = utcEvaluationTime.Ticks - creationTime.Ticks;
        return ageTicks <= options.LaxAllowingUnsafeGracePeriod.Ticks;
    }

    static bool RequiresSecureNone(SetCookie cookie, CookieJarOptions options)
    {
        return UsesModernRules(options.ComplianceMode) &&
            options.RequireSecureForSameSiteNone && cookie.HasSameSite &&
            cookie.SameSiteMode == CookieSameSiteMode.None;
    }

    static CookieSameSiteMode ResolveEffectiveMode(
        bool hasSameSite,
        CookieSameSiteMode suppliedMode,
        CookieJarOptions options)
    {
        CookieSameSiteMode mode = hasSameSite ? suppliedMode : CookieSameSiteMode.Default;
        if (mode == CookieSameSiteMode.Default &&
            options.DefaultSameSiteMode != CookieSameSiteMode.Default)
        {
            mode = options.DefaultSameSiteMode;
        }

        if (mode == CookieSameSiteMode.Default && !UsesModernRules(options.ComplianceMode))
        {
            return CookieSameSiteMode.None;
        }

        return mode;
    }

    static bool IsValidOptions(CookieJarOptions options)
    {
        if (options == null || options.LaxAllowingUnsafeGracePeriod.Ticks < 0 ||
            !IsValidComplianceMode(options.ComplianceMode))
        {
            return false;
        }

        return options.DefaultSameSiteMode == CookieSameSiteMode.Default ||
            options.DefaultSameSiteMode == CookieSameSiteMode.Lax ||
            options.DefaultSameSiteMode == CookieSameSiteMode.Strict ||
            options.DefaultSameSiteMode == CookieSameSiteMode.None;
    }

    static bool IsValidComplianceMode(CookieComplianceMode complianceMode)
    {
        return complianceMode == CookieComplianceMode.Rfc6265 ||
            complianceMode == CookieComplianceMode.Rfc6265Bis ||
            complianceMode == CookieComplianceMode.Compatible;
    }

    static bool UsesModernRules(CookieComplianceMode complianceMode)
    {
        return complianceMode == CookieComplianceMode.Rfc6265Bis ||
            complianceMode == CookieComplianceMode.Compatible;
    }

    static bool IsValidSameSiteContext(CookieSameSiteContext sameSiteContext)
    {
        return sameSiteContext == CookieSameSiteContext.SameSite ||
            sameSiteContext == CookieSameSiteContext.CrossSite;
    }

    static bool IsValidSuppliedMode(bool hasSameSite, CookieSameSiteMode sameSiteMode)
    {
        if (!hasSameSite)
        {
            return true;
        }

        return sameSiteMode == CookieSameSiteMode.Default ||
            sameSiteMode == CookieSameSiteMode.Lax ||
            sameSiteMode == CookieSameSiteMode.Strict ||
            sameSiteMode == CookieSameSiteMode.None;
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

    static CookieSameSiteResult CreateAllowed(
        CookieSameSiteMode effectiveMode,
        bool usedDefaultMode,
        bool usedLaxAllowingUnsafe)
    {
        CookieSameSiteResult result = new CookieSameSiteResult();
        result.Success = true;
        result.Allowed = true;
        result.ErrorCode = CookieErrorCode.None;
        result.EffectiveMode = effectiveMode;
        result.UsedDefaultMode = usedDefaultMode;
        result.UsedLaxAllowingUnsafe = usedLaxAllowingUnsafe;
        return result;
    }

    static CookieSameSiteResult CreateDenied(
        CookieSameSiteMode effectiveMode,
        bool usedDefaultMode)
    {
        CookieSameSiteResult result = new CookieSameSiteResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.SameSiteViolation;
        result.EffectiveMode = effectiveMode;
        result.UsedDefaultMode = usedDefaultMode;
        result.Issues = [CreateIssue(CookieErrorCode.SameSiteViolation)];
        return result;
    }

    static CookieSameSiteResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieSameSiteResult result = new CookieSameSiteResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
