namespace cookies;

internal static class CookieAcceptanceValidator
{
    internal static CookieValidationResult ValidateCookie(
        SetCookie cookie,
        CookieJarOptions options)
    {
        CookieValidationResult optionResult = ValidateOptions(options);
        if (!optionResult.Success)
        {
            return optionResult;
        }

        if (cookie == null || cookie.Name == null || cookie.Value == null ||
            cookie.Domain == null || cookie.Path == null || cookie.Extensions == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (ContainsHeaderInjection(cookie.Name) || ContainsHeaderInjection(cookie.Value) ||
            ContainsHeaderInjection(cookie.Domain) || ContainsHeaderInjection(cookie.Path))
        {
            return CreateFailure(CookieErrorCode.HeaderInjection);
        }

        if (!IsValidName(cookie.Name, cookie.Value, options.ComplianceMode))
        {
            return CreateFailure(CookieErrorCode.InvalidName);
        }

        if (!IsValidValue(cookie.Value, cookie.IsValueQuoted, options.ComplianceMode))
        {
            return CreateFailure(CookieErrorCode.InvalidValue);
        }

        CookieLimits limits = options.Limits;
        if ((long)GetUtf8OctetCount(cookie.Name) + GetUtf8OctetCount(cookie.Value) >
            limits.MaximumNameValueOctets)
        {
            return CreateFailure(CookieErrorCode.NameValueLimitExceeded);
        }

        if (cookie.HasExpires && cookie.Expires.ToUniversalTime().Year < 1601)
        {
            return CreateFailure(CookieErrorCode.InvalidDate);
        }

        if (cookie.HasExpires && 29 > limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        if (cookie.HasMaxAge &&
            CountSignedDecimalOctets(cookie.MaxAgeSeconds) >
                limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        if (cookie.HasDomain && (cookie.Domain.Length == 0 ||
            GetUtf8OctetCount(cookie.Domain) > limits.MaximumAttributeValueOctets))
        {
            return CreateFailure(cookie.Domain.Length == 0 ?
                CookieErrorCode.InvalidAttribute : CookieErrorCode.AttributeLimitExceeded);
        }

        if (cookie.HasPath && (cookie.Path.Length == 0 || cookie.Path[0] != '/'))
        {
            return CreateFailure(CookieErrorCode.InvalidPath);
        }

        if (cookie.HasPath &&
            GetUtf8OctetCount(cookie.Path) > limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        if (cookie.HasSameSite && !IsValidSameSiteMode(cookie.SameSiteMode))
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (cookie.HasSameSite &&
            CountSameSiteValueOctets(cookie.SameSiteMode) >
                limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            CookieErrorCode extensionError = ValidateExtension(
                cookie.Extensions[extensionIndex],
                limits);
            if (extensionError != CookieErrorCode.None)
            {
                return CreateFailure(extensionError);
            }

            extensionIndex = extensionIndex + 1;
        }

        if (CountInputOctets(cookie) > limits.MaximumInputLineOctets)
        {
            return CreateFailure(CookieErrorCode.InputLimitExceeded);
        }

        return CreateSuccess();
    }

    internal static CookieValidationResult ValidateOptions(CookieJarOptions options)
    {
        if (options == null || options.Limits == null || options.AllowedDomains == null ||
            options.BlockedDomains == null)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (options.ComplianceMode != CookieComplianceMode.Rfc6265 &&
            options.ComplianceMode != CookieComplianceMode.Rfc6265Bis &&
            options.ComplianceMode != CookieComplianceMode.Compatible)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (options.DefaultSameSiteMode != CookieSameSiteMode.Default &&
            options.DefaultSameSiteMode != CookieSameSiteMode.Lax &&
            options.DefaultSameSiteMode != CookieSameSiteMode.Strict &&
            options.DefaultSameSiteMode != CookieSameSiteMode.None)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (options.PrefixPolicy != CookiePrefixPolicy.SilentReject &&
            options.PrefixPolicy != CookiePrefixPolicy.ErrorReject &&
            options.PrefixPolicy != CookiePrefixPolicy.Disabled)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        CookieLimits limits = options.Limits;
        if (limits.MaximumNameValueOctets < 0 ||
            limits.MaximumAttributeValueOctets < 0 ||
            limits.MaximumInputLineOctets < 0 ||
            limits.MaximumCookiesPerDomain < 0 || limits.MaximumCookies < 0 ||
            limits.MaximumRequestHeaderOctets < 0 || limits.MaximumLifetime.Ticks < 0 ||
            options.LaxAllowingUnsafeGracePeriod.Ticks < 0)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (!AreValidPolicyDomains(options.AllowedDomains) ||
            !AreValidPolicyDomains(options.BlockedDomains))
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        return CreateSuccess();
    }

    internal static CookieValidationResult ValidateStoredCookie(
        StoredCookie cookie,
        CookieJarOptions options)
    {
        CookieValidationResult optionResult = ValidateOptions(options);
        if (!optionResult.Success)
        {
            return optionResult;
        }

        if (cookie == null || cookie.Name == null || cookie.Value == null ||
            cookie.Domain == null || cookie.Path == null || cookie.PartitionKey == null ||
            cookie.Extensions == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (ContainsHeaderInjection(cookie.Name) || ContainsHeaderInjection(cookie.Value) ||
            ContainsHeaderInjection(cookie.Domain) || ContainsHeaderInjection(cookie.Path) ||
            ContainsHeaderInjection(cookie.PartitionKey))
        {
            return CreateFailure(CookieErrorCode.HeaderInjection);
        }

        if (!IsValidName(cookie.Name, cookie.Value, options.ComplianceMode))
        {
            return CreateFailure(CookieErrorCode.InvalidName);
        }

        if (!IsValidValue(cookie.Value, cookie.IsValueQuoted, options.ComplianceMode))
        {
            return CreateFailure(CookieErrorCode.InvalidValue);
        }

        if ((long)GetUtf8OctetCount(cookie.Name) + GetUtf8OctetCount(cookie.Value) >
            options.Limits.MaximumNameValueOctets)
        {
            return CreateFailure(CookieErrorCode.NameValueLimitExceeded);
        }

        if (cookie.HasSameSite && !IsValidSameSiteMode(cookie.SameSiteMode))
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (cookie.HasPartitionKey && cookie.PartitionKey.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            CookieErrorCode extensionError = ValidateExtension(
                cookie.Extensions[extensionIndex],
                options.Limits);
            if (extensionError != CookieErrorCode.None)
            {
                return CreateFailure(extensionError);
            }

            extensionIndex = extensionIndex + 1;
        }

        return CreateSuccess();
    }

    internal static bool DomainMatchesRule(string domain, string rule)
    {
        return CookieDomainMatcher.DomainMatches(domain, rule, false);
    }

    static bool AreValidPolicyDomains(string[] domains)
    {
        int index = 0;
        while (index < domains.Length)
        {
            string domain = domains[index];
            if (domain == null || domain.Length == 0 || !IsCanonicalDomain(domain))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsCanonicalDomain(string domain)
    {
        global::System.UriHostNameType type = global::System.Uri.CheckHostName(domain);
        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.ErrorCode = CookieErrorCode.None;
        extracted.Host = domain;
        if (type == global::System.UriHostNameType.Dns)
        {
            extracted.IsDnsName = true;
        }
        else if (type == global::System.UriHostNameType.IPv4)
        {
            extracted.IsIpv4 = true;
        }
        else if (type == global::System.UriHostNameType.IPv6)
        {
            extracted.IsIpv6 = true;
        }
        else
        {
            return false;
        }

        CookieCanonicalHostResult canonical = CookieHostCanonicalizer.Canonicalize(extracted);
        return canonical.Success && canonical.Host == domain;
    }

    static bool IsValidName(
        string name,
        string value,
        CookieComplianceMode complianceMode)
    {
        if (name.Length == 0)
        {
            return value.Length > 0 &&
                (complianceMode == CookieComplianceMode.Rfc6265Bis ||
                    complianceMode == CookieComplianceMode.Compatible);
        }

        int index = 0;
        while (index < name.Length)
        {
            if (!CookieCharacterRules.IsTokenCharacter(name[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsValidValue(
        string value,
        bool isQuoted,
        CookieComplianceMode complianceMode)
    {
        if (isQuoted)
        {
            if (complianceMode != CookieComplianceMode.Compatible)
            {
                return false;
            }

            global::System.ReadOnlySpan<char> quotedValue = value;
            return CookieCompatibleValueRules.IsSafeQuotedValue(quotedValue);
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsCookieOctet(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static CookieErrorCode ValidateExtension(
        CookieAttribute extension,
        CookieLimits limits)
    {
        if (extension == null || extension.Name == null || extension.Value == null)
        {
            return CookieErrorCode.InvalidAttribute;
        }

        if (ContainsHeaderInjection(extension.Name) ||
            ContainsHeaderInjection(extension.Value))
        {
            return CookieErrorCode.HeaderInjection;
        }

        if (extension.Name.Length == 0 || !IsToken(extension.Name) ||
            IsRecognizedAttributeName(extension.Name))
        {
            return CookieErrorCode.InvalidAttribute;
        }

        if ((!extension.HasValue && extension.Value.Length != 0) ||
            (extension.HasValue && !IsAttributeValue(extension.Value)))
        {
            return CookieErrorCode.InvalidAttribute;
        }

        if (extension.HasValue &&
            GetUtf8OctetCount(extension.Value) > limits.MaximumAttributeValueOctets)
        {
            return CookieErrorCode.AttributeLimitExceeded;
        }

        return CookieErrorCode.None;
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsTokenCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsAttributeValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsAttributeValueOctet(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsRecognizedAttributeName(string value)
    {
        return EqualsAsciiIgnoreCase(value, CookieAttributeNames.Expires) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.MaxAge) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Domain) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Path) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Secure) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.HttpOnly) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.SameSite) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Partitioned);
    }

    static bool EqualsAsciiIgnoreCase(string value, string expected)
    {
        if (value.Length != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char actual = value[index];
            if (actual >= 'A' && actual <= 'Z')
            {
                actual = (char)(actual + ('a' - 'A'));
            }

            char wanted = expected[index];
            if (wanted >= 'A' && wanted <= 'Z')
            {
                wanted = (char)(wanted + ('a' - 'A'));
            }

            if (actual != wanted)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsHeaderInjection(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (CookieCharacterRules.IsHeaderInjection(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsValidSameSiteMode(CookieSameSiteMode mode)
    {
        return mode == CookieSameSiteMode.Default || mode == CookieSameSiteMode.Lax ||
            mode == CookieSameSiteMode.Strict || mode == CookieSameSiteMode.None;
    }

    static long CountInputOctets(SetCookie cookie)
    {
        long count = GetUtf8OctetCount(cookie.Name) + 1L +
            GetUtf8OctetCount(cookie.Value);
        if (cookie.IsValueQuoted)
        {
            count = count + 2;
        }

        if (cookie.HasExpires)
        {
            count = count + 39;
        }

        if (cookie.HasMaxAge)
        {
            count = count + 10 + CountSignedDecimalOctets(cookie.MaxAgeSeconds);
        }

        if (cookie.HasDomain)
        {
            count = count + 9 + GetUtf8OctetCount(cookie.Domain);
        }

        if (cookie.HasPath)
        {
            count = count + 7 + GetUtf8OctetCount(cookie.Path);
        }

        if (cookie.Secure)
        {
            count = count + 8;
        }

        if (cookie.HttpOnly)
        {
            count = count + 10;
        }

        if (cookie.HasSameSite)
        {
            count = count + 11 + CountSameSiteValueOctets(cookie.SameSiteMode);
        }

        if (cookie.Partitioned)
        {
            count = count + 13;
        }

        int index = 0;
        while (index < cookie.Extensions.Length)
        {
            CookieAttribute extension = cookie.Extensions[index];
            count = count + 2 + GetUtf8OctetCount(extension.Name);
            if (extension.HasValue)
            {
                count = count + 1 + GetUtf8OctetCount(extension.Value);
            }

            index = index + 1;
        }

        return count;
    }

    static int CountSameSiteValueOctets(CookieSameSiteMode mode)
    {
        if (mode == CookieSameSiteMode.Lax)
        {
            return 3;
        }

        if (mode == CookieSameSiteMode.Strict)
        {
            return 6;
        }

        return 4;
    }

    static int CountSignedDecimalOctets(long value)
    {
        if (value == 0)
        {
            return 1;
        }

        int count = 0;
        long remaining = value;
        if (remaining < 0)
        {
            count = 1;
        }
        else
        {
            remaining = -remaining;
        }

        while (remaining < 0)
        {
            remaining = remaining / 10;
            count = count + 1;
        }

        return count;
    }

    static int GetUtf8OctetCount(string value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }

    static CookieValidationResult CreateSuccess()
    {
        CookieValidationResult result = new CookieValidationResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        return result;
    }

    static CookieValidationResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieValidationResult result = new CookieValidationResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
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
}
