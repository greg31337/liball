namespace cookies;

internal static class CookieSiteCalculator
{
    internal static CookieSiteResult Compute(
        global::System.Uri uri,
        IPublicSuffixList? suffixList,
        bool requirePublicSuffixList)
    {
        CookieUriHostResult extracted = CookieUriHostExtractor.Extract(uri);
        if (!extracted.Success)
        {
            return CreateFailure(extracted.ErrorCode);
        }

        CookieCanonicalHostResult canonical = CookieHostCanonicalizer.Canonicalize(extracted);
        if (!canonical.Success)
        {
            return CreateFailure(canonical.ErrorCode);
        }

        if (canonical.IsIpv4 || canonical.IsIpv6)
        {
            return CreateSuccess(uri.Scheme, canonical.Host, canonical.IsIpv6, true, []);
        }

        if (!canonical.IsDnsName)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (!ContainsDot(canonical.Host))
        {
            return CreateSuccess(uri.Scheme, canonical.Host, false, true, []);
        }

        if (suffixList == null)
        {
            if (requirePublicSuffixList)
            {
                return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
            }

            return CreateSuccess(
                uri.Scheme,
                canonical.Host,
                false,
                true,
                [CreateIssue(CookieErrorCode.PublicSuffixListUnavailable, CookieParseSeverity.Warning)]);
        }

        CookieRegistrableDomainResult lookup = GetRegistrableDomain(suffixList, canonical.Host);
        if (!lookup.Success)
        {
            return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
        }

        if (!lookup.HasDomain)
        {
            return CreateSuccess(uri.Scheme, canonical.Host, false, true, []);
        }

        if (!IsValidRegistrableDomain(canonical.Host, lookup.Domain))
        {
            return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
        }

        return CreateSuccess(uri.Scheme, lookup.Domain, false, false, []);
    }

    static CookieRegistrableDomainResult GetRegistrableDomain(
        IPublicSuffixList suffixList,
        string host)
    {
        global::System.Threading.Tasks.Task<string?> lookup =
            new global::System.Threading.Tasks.Task<string?>(
                () => suffixList.GetRegistrableDomain(host));
        lookup.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!lookup.IsCompletedSuccessfully)
        {
            lookup.Exception?.Handle(exception => true);
            return new CookieRegistrableDomainResult();
        }

        CookieRegistrableDomainResult result = new CookieRegistrableDomainResult();
        result.Success = true;
        if (lookup.Result != null)
        {
            result.HasDomain = true;
            result.Domain = lookup.Result;
        }

        return result;
    }

    static bool IsValidRegistrableDomain(string host, string registrableDomain)
    {
        if (registrableDomain == null || registrableDomain.Length == 0)
        {
            return false;
        }

        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.ErrorCode = CookieErrorCode.None;
        extracted.Host = registrableDomain;
        extracted.IsDnsName = true;
        CookieCanonicalHostResult canonical = CookieHostCanonicalizer.Canonicalize(extracted);
        return canonical.Success && canonical.IsDnsName && canonical.Host == registrableDomain &&
            CookieDomainMatcher.DomainMatches(host, registrableDomain, false);
    }

    static bool ContainsDot(string host)
    {
        int index = 0;
        while (index < host.Length)
        {
            if (host[index] == '.')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static CookieIssue CreateIssue(CookieErrorCode errorCode, CookieParseSeverity severity)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = severity;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    static CookieSiteResult CreateSuccess(
        string scheme,
        string siteHost,
        bool isIpv6,
        bool usedHostFallback,
        CookieIssue[] issues)
    {
        CookieSiteResult result = new CookieSiteResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.SiteHost = siteHost;
        result.UsedHostFallback = usedHostFallback;
        result.Issues = issues;
        if (isIpv6)
        {
            result.Site = scheme + "://[" + siteHost + "]";
        }
        else
        {
            result.Site = scheme + "://" + siteHost;
        }

        return result;
    }

    static CookieSiteResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieSiteResult result = new CookieSiteResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode, CookieParseSeverity.Error)];
        return result;
    }
}
