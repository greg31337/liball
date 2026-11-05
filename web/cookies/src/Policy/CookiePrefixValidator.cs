namespace cookies;

internal static class CookiePrefixValidator
{
    internal static CookiePrefixValidationResult Validate(
        SetCookie cookie,
        CookieSetContext context,
        ISecureOriginPolicy secureOriginPolicy)
    {
        if (cookie == null || context == null || cookie.Name == null || cookie.Value == null ||
            cookie.Path == null || cookie.Domain == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        CookieJarOptions options = context.Options;
        if (!IsValidOptions(options))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        bool ignoreCase = options.ComplianceMode == CookieComplianceMode.Rfc6265Bis ||
            options.UseCaseInsensitivePrefixes;
        string prefixSource = cookie.Name;
        bool namelessPrefix = false;
        if (prefixSource.Length == 0)
        {
            prefixSource = cookie.Value;
            namelessPrefix = true;
        }

        bool hasHostPrefix = StartsWithPrefix(
            prefixSource,
            CookieAttributeNames.HostPrefix,
            ignoreCase);
        bool hasSecurePrefix = !hasHostPrefix && StartsWithPrefix(
            prefixSource,
            CookieAttributeNames.SecurePrefix,
            ignoreCase);
        if (!hasHostPrefix && !hasSecurePrefix)
        {
            return CreateAllowed(false, false, false, ignoreCase, false, false, false);
        }

        if (options.PrefixPolicy == CookiePrefixPolicy.Disabled)
        {
            return CreateAllowed(
                hasSecurePrefix,
                hasHostPrefix,
                namelessPrefix,
                ignoreCase,
                true,
                false,
                false);
        }

        if (namelessPrefix || !cookie.Secure)
        {
            return CreatePolicyRejection(
                options.PrefixPolicy,
                hasSecurePrefix,
                hasHostPrefix,
                namelessPrefix,
                ignoreCase,
                false,
                false);
        }

        if (hasHostPrefix &&
            (!cookie.HasPath || cookie.Path != "/" || cookie.HasDomain))
        {
            return CreatePolicyRejection(
                options.PrefixPolicy,
                false,
                true,
                false,
                ignoreCase,
                false,
                false);
        }

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
            return CreatePolicyRejection(
                options.PrefixPolicy,
                hasSecurePrefix,
                hasHostPrefix,
                false,
                ignoreCase,
                true,
                false);
        }

        return CreateAllowed(
            hasSecurePrefix,
            hasHostPrefix,
            false,
            ignoreCase,
            false,
            true,
            true);
    }

    internal static bool StartsWithPrefix(string value, string prefix, bool ignoreCase)
    {
        if (value == null || prefix == null || value.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            char actual = value[index];
            char expected = prefix[index];
            if (ignoreCase)
            {
                actual = ToAsciiLower(actual);
                expected = ToAsciiLower(expected);
            }

            if (actual != expected)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static char ToAsciiLower(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + ('a' - 'A'));
        }

        return character;
    }

    static bool IsValidOptions(CookieJarOptions options)
    {
        if (options == null ||
            (options.ComplianceMode != CookieComplianceMode.Rfc6265 &&
                options.ComplianceMode != CookieComplianceMode.Rfc6265Bis &&
                options.ComplianceMode != CookieComplianceMode.Compatible))
        {
            return false;
        }

        return options.PrefixPolicy == CookiePrefixPolicy.SilentReject ||
            options.PrefixPolicy == CookiePrefixPolicy.ErrorReject ||
            options.PrefixPolicy == CookiePrefixPolicy.Disabled;
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

    static CookiePrefixValidationResult CreateAllowed(
        bool hasSecurePrefix,
        bool hasHostPrefix,
        bool wasNamelessPrefix,
        bool usedCaseInsensitiveMatch,
        bool enforcementDisabled,
        bool originWasEvaluated,
        bool originIsSecure)
    {
        CookiePrefixValidationResult result = new CookiePrefixValidationResult();
        result.Success = true;
        result.Allowed = true;
        result.ErrorCode = CookieErrorCode.None;
        result.HasSecurePrefix = hasSecurePrefix;
        result.HasHostPrefix = hasHostPrefix;
        result.WasNamelessPrefix = wasNamelessPrefix;
        result.UsedCaseInsensitiveMatch = usedCaseInsensitiveMatch;
        result.EnforcementDisabled = enforcementDisabled;
        result.OriginWasEvaluated = originWasEvaluated;
        result.OriginIsSecure = originIsSecure;
        return result;
    }

    static CookiePrefixValidationResult CreatePolicyRejection(
        CookiePrefixPolicy policy,
        bool hasSecurePrefix,
        bool hasHostPrefix,
        bool wasNamelessPrefix,
        bool usedCaseInsensitiveMatch,
        bool originWasEvaluated,
        bool originIsSecure)
    {
        CookiePrefixValidationResult result = new CookiePrefixValidationResult();
        result.Success = true;
        result.HasSecurePrefix = hasSecurePrefix;
        result.HasHostPrefix = hasHostPrefix;
        result.WasNamelessPrefix = wasNamelessPrefix;
        result.UsedCaseInsensitiveMatch = usedCaseInsensitiveMatch;
        result.OriginWasEvaluated = originWasEvaluated;
        result.OriginIsSecure = originIsSecure;
        if (policy == CookiePrefixPolicy.SilentReject)
        {
            result.SilentlyRejected = true;
        }
        else
        {
            result.ErrorCode = CookieErrorCode.PrefixViolation;
            result.Issues = [CreateIssue(CookieErrorCode.PrefixViolation)];
        }

        return result;
    }

    static CookiePrefixValidationResult CreateFailure(CookieErrorCode errorCode)
    {
        CookiePrefixValidationResult result = new CookiePrefixValidationResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
