namespace cookies;

/// <summary>
/// Provides canonical cookie matching operations using explicit policy dependencies.
/// </summary>
public class CookieMatcher : ICookieMatcher
{
    internal CookieJarOptions Options;
    internal IPublicSuffixList? PublicSuffixList;
    internal ISecureOriginPolicy SecureOriginPolicy;
    internal ICookieClock Clock;

    /// <summary>
    /// Creates a cookie matcher with its effective options and external policy dependencies.
    /// </summary>
    /// <param name="options">The effective cookie-jar options.</param>
    /// <param name="publicSuffixList">The public-suffix provider, or null when explicitly optional.</param>
    /// <param name="secureOriginPolicy">The secure-origin classifier.</param>
    /// <param name="clock">The deterministic UTC clock used for request evaluation.</param>
    /// <exception cref="CookiePolicyException">The options or dependency graph is invalid.</exception>
    public CookieMatcher(
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
    }

    /// <summary>
    /// Extracts and canonicalizes the host of an absolute origin or request URI.
    /// </summary>
    /// <param name="uri">The absolute URI.</param>
    /// <returns>The canonical ASCII host.</returns>
    /// <exception cref="CookiePolicyException">The URI host cannot be canonicalized.</exception>
    public string CanonicalizeHost(global::System.Uri uri)
    {
        CookieCanonicalHostResult result = CanonicalizeUriHost(uri);
        if (!result.Success)
        {
            throw new CookiePolicyException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Host;
    }

    /// <summary>
    /// Determines whether a canonical request host domain-matches a canonical cookie domain.
    /// </summary>
    /// <param name="requestHost">The canonical request host.</param>
    /// <param name="cookieDomain">The canonical cookie domain.</param>
    /// <returns>True when the domain-match rules are satisfied.</returns>
    public bool DomainMatches(string requestHost, string cookieDomain)
    {
        return CookieDomainMatcher.DomainMatches(requestHost, cookieDomain, false);
    }

    /// <summary>
    /// Computes the RFC default cookie path from an origin URI.
    /// </summary>
    /// <param name="origin">The absolute origin URI.</param>
    /// <returns>The canonical default path.</returns>
    /// <exception cref="CookiePolicyException">The origin cannot supply a cookie path.</exception>
    public string ComputeDefaultPath(global::System.Uri origin)
    {
        CookiePathResult result = CookiePathMatcher.ComputeDefaultPath(origin);
        if (!result.Success)
        {
            throw new CookiePolicyException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Path;
    }

    /// <summary>
    /// Determines whether a request path path-matches a cookie path.
    /// </summary>
    /// <param name="requestPath">The escaped request path.</param>
    /// <param name="cookiePath">The canonical cookie path.</param>
    /// <returns>True when the path-match rules are satisfied.</returns>
    public bool PathMatches(string requestPath, string cookiePath)
    {
        return CookiePathMatcher.PathMatches(requestPath, cookiePath);
    }

    /// <summary>
    /// Computes the schemeful site for an absolute URI.
    /// </summary>
    /// <param name="uri">The absolute URI.</param>
    /// <returns>The canonical schemeful site.</returns>
    /// <exception cref="CookiePolicyException">The site cannot be computed safely.</exception>
    public string ComputeSchemefulSite(global::System.Uri uri)
    {
        if (!HasValidConfiguration())
        {
            throw new CookiePolicyException(
                CreateFailureMessage(CookieErrorCode.InvalidConfiguration));
        }

        CookieSiteResult result = CookieSiteCalculator.Compute(
            uri,
            PublicSuffixList,
            Options.RequirePublicSuffixList);
        if (!result.Success)
        {
            throw new CookiePolicyException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Site;
    }

    /// <summary>
    /// Evaluates whether a stored cookie is eligible for a request.
    /// </summary>
    /// <param name="cookie">The stored cookie.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The include or exclude result.</returns>
    public CookieMatchResult EvaluateCookieMatch(
        StoredCookie cookie,
        CookieRequestContext context)
    {
        if (cookie == null || context == null || cookie.Name == null ||
            cookie.Value == null)
        {
            return CreateExcluded(cookie, CookieErrorCode.InvalidAttribute, []);
        }

        cookie = CookieGraphCloner.CloneStoredCookie(cookie);
        context = CookieGraphCloner.CloneRequestContext(context);

        if (!HasValidConfiguration())
        {
            return CreateExcluded(cookie, CookieErrorCode.InvalidConfiguration, []);
        }

        CookiePartitionResult partition = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            context,
            Options);
        if (!partition.Success || !partition.Allowed)
        {
            return CreateExcluded(cookie, partition.ErrorCode, partition.Issues);
        }

        global::System.Uri? requestUri = context.RequestUri;
        if (requestUri == null)
        {
            return CreateExcluded(cookie, CookieErrorCode.InvalidHost, []);
        }

        CookieCanonicalHostResult requestHost = CanonicalizeUriHost(requestUri);
        if (!requestHost.Success)
        {
            return CreateExcluded(cookie, requestHost.ErrorCode, []);
        }

        if (!CookieDomainMatcher.DomainMatches(
            requestHost.Host,
            cookie.Domain,
            cookie.HostOnly))
        {
            return CreateExcluded(cookie, CookieErrorCode.DomainMismatch, []);
        }

        CookiePublicSuffixResult publicSuffix =
            CookiePublicSuffixPolicy.ValidateStoredDomain(
                cookie,
                PublicSuffixList,
                Options.RequirePublicSuffixList);
        if (!publicSuffix.Success)
        {
            return CreateExcluded(cookie, publicSuffix.ErrorCode, publicSuffix.Issues);
        }

        CookiePathResult requestPath = CookiePathMatcher.ExtractRequestPath(requestUri);
        if (!requestPath.Success)
        {
            return CreateExcluded(
                cookie,
                requestPath.ErrorCode,
                CombineIssues(publicSuffix.Issues, [CreateIssue(requestPath.ErrorCode)]));
        }

        if (!CookiePathMatcher.PathMatches(requestPath.Path, cookie.Path))
        {
            return CreateExcluded(
                cookie,
                CookieErrorCode.InvalidPath,
                CombineIssues(
                    publicSuffix.Issues,
                    [CreateIssue(CookieErrorCode.InvalidPath)]));
        }

        CookieClockResult evaluationTime = GetEvaluationTime();
        if (!evaluationTime.Success)
        {
            return CreateExcluded(
                cookie,
                CookieErrorCode.InvalidConfiguration,
                CombineIssues(
                    publicSuffix.Issues,
                    [CreateIssue(CookieErrorCode.InvalidConfiguration)]));
        }

        if (CookieExpiryEvaluator.IsExpired(cookie, evaluationTime.Value))
        {
            return CreateExcluded(
                cookie,
                CookieErrorCode.Expired,
                CombineIssues(
                    publicSuffix.Issues,
                    [CreateIssue(CookieErrorCode.Expired)]));
        }

        CookieSecurityAttributeResult security =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                cookie,
                context,
                SecureOriginPolicy);
        if (!security.Success || !security.Allowed)
        {
            return CreateExcluded(
                cookie,
                security.ErrorCode,
                CombineIssues(publicSuffix.Issues, security.Issues));
        }

        CookieSameSiteResult sameSite = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie,
            context,
            Options,
            evaluationTime.Value);
        if (!sameSite.Success || !sameSite.Allowed)
        {
            return CreateExcluded(
                cookie,
                sameSite.ErrorCode,
                CombineIssues(publicSuffix.Issues, sameSite.Issues));
        }

        return CreateIncluded(cookie, publicSuffix.Issues);
    }

    /// <summary>
    /// Determines whether a stored cookie is expired at a supplied instant.
    /// </summary>
    /// <param name="cookie">The stored cookie.</param>
    /// <param name="evaluationTime">The explicit UTC evaluation instant.</param>
    /// <returns>True when the cookie is expired.</returns>
    /// <exception cref="CookiePolicyException">The cookie model is absent.</exception>
    public bool IsCookieExpired(
        StoredCookie cookie,
        global::System.DateTimeOffset evaluationTime)
    {
        if (cookie == null)
        {
            throw new CookiePolicyException(
                CreateFailureMessage(CookieErrorCode.InvalidAttribute));
        }

        return CookieExpiryEvaluator.IsExpired(cookie, evaluationTime);
    }

    internal static string CreateFailureMessage(CookieErrorCode errorCode)
    {
        return $"Cookie matching failed with error code {(int)errorCode}.";
    }

    internal static CookieCanonicalHostResult CanonicalizeUriHost(global::System.Uri uri)
    {
        return CookieHostCanonicalizer.Canonicalize(CookieUriHostExtractor.Extract(uri));
    }

    internal bool HasValidConfiguration()
    {
        if (Options == null || SecureOriginPolicy == null || Clock == null ||
            Options.LaxAllowingUnsafeGracePeriod.Ticks < 0)
        {
            return false;
        }

        if (Options.ComplianceMode != CookieComplianceMode.Rfc6265 &&
            Options.ComplianceMode != CookieComplianceMode.Rfc6265Bis &&
            Options.ComplianceMode != CookieComplianceMode.Compatible)
        {
            return false;
        }

        return Options.DefaultSameSiteMode == CookieSameSiteMode.Default ||
            Options.DefaultSameSiteMode == CookieSameSiteMode.Lax ||
            Options.DefaultSameSiteMode == CookieSameSiteMode.Strict ||
            Options.DefaultSameSiteMode == CookieSameSiteMode.None;
    }

    internal CookieClockResult GetEvaluationTime()
    {
        return CookieClockEvaluator.Evaluate(Clock);
    }

    internal static CookieMatchResult CreateIncluded(
        StoredCookie cookie,
        CookieIssue[] issues)
    {
        CookiePair pair = new CookiePair();
        pair.Name = cookie.Name;
        pair.Value = cookie.Value;
        pair.IsQuoted = cookie.IsValueQuoted;

        CookieMatchResult result = new CookieMatchResult();
        result.Cookie = cookie;
        result.Pair = pair;
        result.Status = CookieMatchStatus.Included;
        result.ErrorCode = CookieErrorCode.None;
        result.Issues = issues;
        return result;
    }

    internal static CookieMatchResult CreateExcluded(
        StoredCookie? cookie,
        CookieErrorCode errorCode,
        CookieIssue[] issues)
    {
        CookieMatchResult result = new CookieMatchResult();
        result.Cookie = cookie;
        result.Status = CookieMatchStatus.Excluded;
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

    internal static CookieIssue CreateIssue(CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    internal static CookieIssue[] CombineIssues(CookieIssue[] first, CookieIssue[] second)
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
}
