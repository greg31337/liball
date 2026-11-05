namespace cookies;

internal static class CookiePartitionEvaluator
{
    internal static CookiePartitionResult EvaluateCreation(
        SetCookie cookie,
        CookieSetContext context)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute, false, false);
        }

        CookieJarOptions options = context.Options;
        if (options == null)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration, cookie.Partitioned, false);
        }

        if (!cookie.Partitioned)
        {
            return CreateAllowed(
                options.EnablePartitionedCookies,
                false,
                false,
                false,
                "",
                true);
        }

        if (!options.EnablePartitionedCookies)
        {
            return CreateAllowed(false, true, true, false, "", true);
        }

        if (!cookie.Secure)
        {
            return CreateDenied(
                CookieErrorCode.InsecureSecureCookie,
                true,
                true,
                false,
                "",
                false);
        }

        if (!context.HasTopLevelSite || context.TopLevelSite == null ||
            context.TopLevelSite.Length == 0)
        {
            return CreateDenied(
                CookieErrorCode.MissingPartitionContext,
                true,
                true,
                false,
                "",
                false);
        }

        return CreateAllowed(true, true, false, true, context.TopLevelSite, true);
    }

    internal static CookiePartitionResult EvaluateRetrieval(
        StoredCookie cookie,
        CookieRequestContext context,
        CookieJarOptions options)
    {
        if (cookie == null || context == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute, false, false);
        }

        if (options == null)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration, false, false);
        }

        if (!cookie.HasPartitionKey)
        {
            return CreateAllowed(
                options.EnablePartitionedCookies,
                false,
                false,
                false,
                "",
                true);
        }

        if (cookie.PartitionKey == null || cookie.PartitionKey.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute, false, options.EnablePartitionedCookies);
        }

        if (!options.EnablePartitionedCookies)
        {
            return CreateDenied(
                CookieErrorCode.PartitionMismatch,
                false,
                false,
                true,
                cookie.PartitionKey,
                false);
        }

        if (!context.HasTopLevelSite || context.TopLevelSite == null ||
            context.TopLevelSite.Length == 0)
        {
            return CreateDenied(
                CookieErrorCode.MissingPartitionContext,
                true,
                false,
                true,
                cookie.PartitionKey,
                false);
        }

        if (cookie.PartitionKey != context.TopLevelSite)
        {
            return CreateDenied(
                CookieErrorCode.PartitionMismatch,
                true,
                false,
                true,
                cookie.PartitionKey,
                false);
        }

        return CreateAllowed(true, false, false, true, cookie.PartitionKey, true);
    }

    internal static bool PartitionKeysMatch(
        bool firstHasPartitionKey,
        string firstPartitionKey,
        bool secondHasPartitionKey,
        string secondPartitionKey)
    {
        if (firstHasPartitionKey != secondHasPartitionKey)
        {
            return false;
        }

        if (!firstHasPartitionKey)
        {
            return true;
        }

        return firstPartitionKey != null && firstPartitionKey.Length > 0 &&
            secondPartitionKey != null && secondPartitionKey.Length > 0 &&
            firstPartitionKey == secondPartitionKey;
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

    static CookiePartitionResult CreateAllowed(
        bool enforcementEnabled,
        bool attributeObserved,
        bool attributeIgnored,
        bool hasPartitionKey,
        string partitionKey,
        bool partitionMatched)
    {
        CookiePartitionResult result = new CookiePartitionResult();
        result.Success = true;
        result.Allowed = true;
        result.ErrorCode = CookieErrorCode.None;
        result.EnforcementEnabled = enforcementEnabled;
        result.AttributeObserved = attributeObserved;
        result.AttributeIgnored = attributeIgnored;
        result.HasPartitionKey = hasPartitionKey;
        result.PartitionKey = partitionKey;
        result.PartitionMatched = partitionMatched;
        return result;
    }

    static CookiePartitionResult CreateDenied(
        CookieErrorCode errorCode,
        bool enforcementEnabled,
        bool attributeObserved,
        bool hasPartitionKey,
        string partitionKey,
        bool partitionMatched)
    {
        CookiePartitionResult result = new CookiePartitionResult();
        result.Success = true;
        result.ErrorCode = errorCode;
        result.EnforcementEnabled = enforcementEnabled;
        result.AttributeObserved = attributeObserved;
        result.HasPartitionKey = hasPartitionKey;
        result.PartitionKey = partitionKey;
        result.PartitionMatched = partitionMatched;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }

    static CookiePartitionResult CreateFailure(
        CookieErrorCode errorCode,
        bool attributeObserved,
        bool enforcementEnabled)
    {
        CookiePartitionResult result = new CookiePartitionResult();
        result.ErrorCode = errorCode;
        result.EnforcementEnabled = enforcementEnabled;
        result.AttributeObserved = attributeObserved;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
