namespace cookies;

/// <summary>
/// Applies the default cookie acceptance, normalization, return, and application-domain policy.
/// </summary>
public class DefaultCookiePolicy : ICookiePolicy
{
    internal CookieJarOptions Options;
    internal IPublicSuffixList? PublicSuffixList;
    internal ISecureOriginPolicy SecureOriginPolicy;
    internal ICookieClock Clock;
    internal CookieMatcher Matcher;

    /// <summary>
    /// Creates the default policy with its effective options and external dependencies.
    /// </summary>
    /// <param name="options">The effective cookie-jar options used for return policy.</param>
    /// <param name="publicSuffixList">The public-suffix provider, or null when explicitly optional.</param>
    /// <param name="secureOriginPolicy">The secure-origin classifier.</param>
    /// <param name="clock">The deterministic UTC clock used for policy evaluation.</param>
    /// <exception cref="CookiePolicyException">The options or dependency graph is invalid.</exception>
    public DefaultCookiePolicy(
        CookieJarOptions options,
        IPublicSuffixList? publicSuffixList,
        ISecureOriginPolicy secureOriginPolicy,
        ICookieClock clock)
    {
        if (!CookieServiceConfigurationValidator.IsValidMatcherConfiguration(
            options,
            publicSuffixList,
            secureOriginPolicy,
            clock))
        {
            throw new CookiePolicyException(
                CookieServiceConfigurationValidator.CreateFailureMessage());
        }

        Options = CookieGraphCloner.CloneJarOptions(options);
        PublicSuffixList = publicSuffixList;
        SecureOriginPolicy = secureOriginPolicy;
        Clock = clock;
        Matcher = new CookieMatcher(Options, publicSuffixList, secureOriginPolicy, clock);
    }

    /// <summary>
    /// Evaluates whether a supplied cookie can be accepted.
    /// </summary>
    /// <param name="cookie">The cookie proposed for storage.</param>
    /// <param name="context">The facts governing the cookie-setting operation.</param>
    /// <returns>The acceptance decision and normalized candidate.</returns>
    public CookieSetDecision EvaluateCookieAcceptance(
        SetCookie cookie,
        CookieSetContext context)
    {
        if (cookie == null || context == null)
        {
            return CreateRejected(CookieErrorCode.InvalidAttribute, []);
        }

        if (context.ExistingCookies == null)
        {
            return CreateRejected(CookieErrorCode.InvalidAttribute, []);
        }

        CookieJarOptions options = context.Options;
        CookieValidationResult validation =
            CookieAcceptanceValidator.ValidateCookie(cookie, options);
        if (!validation.Success)
        {
            return CreateRejected(validation.ErrorCode, validation.Issues);
        }

        cookie = CookieGraphCloner.CloneSetCookie(cookie);
        context = CookieGraphCloner.CloneSetContext(context);
        options = context.Options;

        global::System.Uri? origin = context.Origin;
        if (origin == null)
        {
            return CreateRejected(CookieErrorCode.InvalidHost, []);
        }

        CookieCanonicalHostResult canonicalOrigin =
            CookieMatcher.CanonicalizeUriHost(origin);
        if (!canonicalOrigin.Success)
        {
            return CreateRejected(canonicalOrigin.ErrorCode, []);
        }

        CookieClockResult receiptTime = CookieClockEvaluator.Evaluate(Clock);
        if (!receiptTime.Success)
        {
            return CreateRejected(CookieErrorCode.InvalidConfiguration, []);
        }

        CookieExpiryResult expiry = CookieExpiryEvaluator.Normalize(
            cookie,
            receiptTime.Value,
            options.ComplianceMode,
            options.Limits.MaximumLifetime);
        if (!expiry.Success)
        {
            return CreateRejected(expiry.ErrorCode, expiry.Issues);
        }

        CookieIssue[] issues = expiry.Issues;
        CookieDomainResult domain = CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            canonicalOrigin,
            options.ComplianceMode);
        if (!domain.Success)
        {
            return CreateRejected(
                domain.ErrorCode,
                AppendError(issues, domain.ErrorCode));
        }

        CookiePublicSuffixResult publicSuffix = CookiePublicSuffixPolicy.ValidateSetDomain(
            domain,
            canonicalOrigin,
            PublicSuffixList,
            options.RequirePublicSuffixList);
        if (!publicSuffix.Success)
        {
            return CreateRejected(
                publicSuffix.ErrorCode,
                CombineIssues(issues, publicSuffix.Issues));
        }

        issues = CombineIssues(issues, publicSuffix.Issues);
        CookiePathResult path = CookiePathMatcher.NormalizeSetPath(cookie, origin);
        if (!path.Success)
        {
            return CreateRejected(
                path.ErrorCode,
                AppendError(issues, path.ErrorCode));
        }

        StoredCookie overlayCandidate = CreateOverlayCandidate(
            cookie,
            publicSuffix,
            path,
            context,
            options);
        StoredCookie? existingCookie = context.ExistingCookie;
        if (existingCookie == null)
        {
            existingCookie = FindExactExistingCookie(
                overlayCandidate,
                context.ExistingCookies);
        }

        CookieSecurityAttributeResult security =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                cookie,
                context,
                SecureOriginPolicy,
                existingCookie);
        if (!security.Success || !security.Allowed)
        {
            return CreateRejected(
                security.ErrorCode,
                CombineIssues(issues, security.Issues));
        }

        CookieSecureOverlayResult overlay = CookieSecureOverlayMatcher.Evaluate(
            overlayCandidate,
            origin,
            SecureOriginPolicy,
            context.ExistingCookies,
            options.ProtectSecureCookieOverlay);
        if (!overlay.Success || !overlay.Allowed)
        {
            return CreateRejected(
                overlay.ErrorCode,
                CombineIssues(issues, overlay.Issues));
        }

        CookieSameSiteResult sameSite = CookieSameSiteEvaluator.EvaluateCreation(
            cookie,
            context);
        if (!sameSite.Success || !sameSite.Allowed)
        {
            return CreateRejected(
                sameSite.ErrorCode,
                CombineIssues(issues, sameSite.Issues));
        }

        CookiePrefixValidationResult prefix = CookiePrefixValidator.Validate(
            cookie,
            context,
            SecureOriginPolicy);
        if (!prefix.Success)
        {
            return CreateRejected(
                prefix.ErrorCode,
                CombineIssues(issues, prefix.Issues));
        }

        if (!prefix.Allowed)
        {
            if (prefix.SilentlyRejected)
            {
                return CreateIgnored(
                    CookieErrorCode.PrefixViolation,
                    CombineIssues(issues, prefix.Issues));
            }

            return CreateRejected(
                prefix.ErrorCode,
                CombineIssues(issues, prefix.Issues));
        }

        CookiePartitionResult partition = CookiePartitionEvaluator.EvaluateCreation(
            cookie,
            context);
        if (!partition.Success || !partition.Allowed)
        {
            return CreateRejected(
                partition.ErrorCode,
                CombineIssues(issues, partition.Issues));
        }

        StoredCookie normalized = CreateNormalizedCookie(
            cookie,
            publicSuffix,
            path,
            expiry,
            partition,
            receiptTime.Value);
        if (!IsApplicationDomainAllowed(normalized.Domain, options))
        {
            return CreateRejected(
                CookieErrorCode.PolicyRejection,
                AppendError(issues, CookieErrorCode.PolicyRejection));
        }

        return CreateAccepted(normalized, issues);
    }

    /// <summary>
    /// Evaluates whether a stored cookie can be returned for a request.
    /// </summary>
    /// <param name="cookie">The stored cookie being considered.</param>
    /// <param name="context">The facts governing the request.</param>
    /// <returns>The cookie match decision.</returns>
    public CookieMatchResult EvaluateCookieReturn(
        StoredCookie cookie,
        CookieRequestContext context)
    {
        CookieValidationResult storedValidation =
            CookieAcceptanceValidator.ValidateStoredCookie(cookie, Options);
        if (!storedValidation.Success)
        {
            return CookieMatcher.CreateExcluded(
                cookie,
                storedValidation.ErrorCode,
                storedValidation.Issues);
        }

        CookieMatchResult mandatory = Matcher.EvaluateCookieMatch(cookie, context);
        if (mandatory.Status != CookieMatchStatus.Included)
        {
            return mandatory;
        }

        StoredCookie? matchedCookie = mandatory.Cookie;
        if (matchedCookie == null)
        {
            return CookieMatcher.CreateExcluded(
                null,
                CookieErrorCode.InvalidConfiguration,
                []);
        }

        if (!IsApplicationDomainAllowed(matchedCookie.Domain, Options))
        {
            return CookieMatcher.CreateExcluded(
                matchedCookie,
                CookieErrorCode.PolicyRejection,
                AppendError(mandatory.Issues, CookieErrorCode.PolicyRejection));
        }

        return mandatory;
    }

    internal static StoredCookie CreateOverlayCandidate(
        SetCookie cookie,
        CookiePublicSuffixResult domain,
        CookiePathResult path,
        CookieSetContext context,
        CookieJarOptions options)
    {
        StoredCookie candidate = new StoredCookie();
        candidate.Name = cookie.Name;
        candidate.Domain = domain.Domain;
        candidate.Path = path.Path;
        candidate.Secure = cookie.Secure;
        if (options.EnablePartitionedCookies && cookie.Partitioned && cookie.Secure &&
            context.HasTopLevelSite && context.TopLevelSite != null &&
            context.TopLevelSite.Length > 0)
        {
            candidate.HasPartitionKey = true;
            candidate.PartitionKey = context.TopLevelSite;
        }

        return candidate;
    }

    internal static bool IsApplicationDomainAllowed(
        string domain,
        CookieJarOptions options)
    {
        int index = 0;
        while (index < options.BlockedDomains.Length)
        {
            if (CookieAcceptanceValidator.DomainMatchesRule(
                domain,
                options.BlockedDomains[index]))
            {
                return false;
            }

            index = index + 1;
        }

        if (options.AllowedDomains.Length == 0)
        {
            return true;
        }

        index = 0;
        while (index < options.AllowedDomains.Length)
        {
            if (CookieAcceptanceValidator.DomainMatchesRule(
                domain,
                options.AllowedDomains[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static StoredCookie CreateNormalizedCookie(
        SetCookie cookie,
        CookiePublicSuffixResult domain,
        CookiePathResult path,
        CookieExpiryResult expiry,
        CookiePartitionResult partition,
        global::System.DateTimeOffset receiptTime)
    {
        StoredCookie normalized = new StoredCookie();
        normalized.Name = cookie.Name;
        normalized.Value = cookie.Value;
        normalized.Domain = domain.Domain;
        normalized.Path = path.Path;
        normalized.HasExpires = expiry.HasExpires;
        normalized.Expires = expiry.Expires;
        normalized.CreationTime = receiptTime;
        normalized.LastAccessTime = receiptTime;
        normalized.Persistent = expiry.Persistent;
        normalized.HostOnly = domain.HostOnly;
        normalized.Secure = cookie.Secure;
        normalized.HttpOnly = cookie.HttpOnly;
        normalized.HasSameSite = cookie.HasSameSite;
        normalized.SameSiteMode = cookie.SameSiteMode;
        normalized.HasPartitionKey = partition.HasPartitionKey;
        normalized.PartitionKey = partition.PartitionKey;
        normalized.IsValueQuoted = cookie.IsValueQuoted;
        normalized.Extensions = cookie.Extensions;
        return normalized;
    }

    internal static CookieSetDecision CreateAccepted(
        StoredCookie cookie,
        CookieIssue[] issues)
    {
        CookieSetDecision result = new CookieSetDecision();
        result.Status = CookieSetStatus.Accepted;
        result.ErrorCode = CookieErrorCode.None;
        result.NormalizedCookie = cookie;
        result.Issues = issues;
        return result;
    }

    internal static StoredCookie? FindExactExistingCookie(
        StoredCookie candidate,
        StoredCookie[] existingCookies)
    {
        if (candidate == null || existingCookies == null)
        {
            return null;
        }

        int index = 0;
        while (index < existingCookies.Length)
        {
            StoredCookie existing = existingCookies[index];
            if (existing == null)
            {
                return null;
            }

            if (CookieIdentityUtility.HaveSameStoredIdentity(candidate, existing))
            {
                return existing;
            }

            index = index + 1;
        }

        return null;
    }

    internal static CookieSetDecision CreateRejected(
        CookieErrorCode errorCode,
        CookieIssue[] issues)
    {
        CookieSetDecision result = new CookieSetDecision();
        result.Status = CookieSetStatus.Rejected;
        result.ErrorCode = errorCode;
        if (issues == null || issues.Length == 0)
        {
            result.Issues = [CreateIssue(errorCode)];
        }
        else
        {
            result.Issues = issues;
        }

        return result;
    }

    internal static CookieSetDecision CreateIgnored(
        CookieErrorCode errorCode,
        CookieIssue[] issues)
    {
        CookieSetDecision result = new CookieSetDecision();
        result.Status = CookieSetStatus.Ignored;
        result.ErrorCode = errorCode;
        result.Issues = issues;
        return result;
    }

    internal static CookieIssue[] AppendError(
        CookieIssue[] issues,
        CookieErrorCode errorCode)
    {
        return CombineIssues(issues, [CreateIssue(errorCode)]);
    }

    internal static CookieIssue[] CombineIssues(
        CookieIssue[] first,
        CookieIssue[] second)
    {
        CookieIssue[] combined = new CookieIssue[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            combined[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            combined[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return combined;
    }

    internal static CookieIssue CreateIssue(CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }
}
