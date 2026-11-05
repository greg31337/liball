namespace cookies;

internal static class CookiePublicSuffixPolicy
{
    internal static CookiePublicSuffixResult ValidateSetDomain(
        CookieDomainResult domain,
        CookieCanonicalHostResult origin,
        IPublicSuffixList? suffixList,
        bool requirePublicSuffixList)
    {
        if (domain == null || !domain.Success)
        {
            CookieErrorCode errorCode = CookieErrorCode.InvalidHost;
            if (domain != null && domain.ErrorCode != CookieErrorCode.None)
            {
                errorCode = domain.ErrorCode;
            }

            return CreateFailure(errorCode);
        }

        if (!IsValidDomainResult(domain) || !IsValidCanonicalOrigin(origin))
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        bool originIsIpAddress = origin.IsIpv4 || origin.IsIpv6;
        if (originIsIpAddress != domain.IsIpAddress ||
            !CookieDomainMatcher.DomainMatches(origin.Host, domain.Domain, domain.HostOnly))
        {
            return CreateFailure(CookieErrorCode.DomainMismatch);
        }

        if (domain.HostOnly || domain.IsIpAddress)
        {
            return CreateSuccess(
                domain.Domain,
                domain.HostOnly,
                domain.DomainAttributeSupplied,
                domain.IsIpAddress,
                []);
        }

        if (suffixList == null)
        {
            if (requirePublicSuffixList)
            {
                return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
            }

            return CreateSuccess(
                domain.Domain,
                domain.HostOnly,
                domain.DomainAttributeSupplied,
                domain.IsIpAddress,
                [CreateIssue(CookieErrorCode.PublicSuffixListUnavailable, CookieParseSeverity.Warning)]);
        }

        CookieBooleanResult suffixResult = IsPublicSuffix(suffixList, domain.Domain);
        if (!suffixResult.Success)
        {
            return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
        }

        if (!suffixResult.Value)
        {
            return CreateSuccess(
                domain.Domain,
                domain.HostOnly,
                domain.DomainAttributeSupplied,
                domain.IsIpAddress,
                []);
        }

        if (origin.IsDnsName && domain.Domain == origin.Host)
        {
            return CreateSuccess(origin.Host, true, domain.DomainAttributeSupplied, false, []);
        }

        return CreateFailure(CookieErrorCode.PublicSuffix);
    }

    internal static CookiePublicSuffixResult ValidateStoredDomain(
        StoredCookie cookie,
        IPublicSuffixList? suffixList,
        bool requirePublicSuffixList)
    {
        if (cookie == null || cookie.Domain == null || cookie.Domain.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        global::System.UriHostNameType domainType = global::System.Uri.CheckHostName(cookie.Domain);
        bool isIpAddress = domainType == global::System.UriHostNameType.IPv4 ||
            domainType == global::System.UriHostNameType.IPv6;
        if (!IsCanonicalDomain(cookie.Domain, isIpAddress) ||
            (isIpAddress && !cookie.HostOnly))
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (cookie.HostOnly || isIpAddress)
        {
            return CreateSuccess(cookie.Domain, cookie.HostOnly, false, isIpAddress, []);
        }

        if (suffixList == null)
        {
            if (requirePublicSuffixList)
            {
                return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
            }

            return CreateSuccess(
                cookie.Domain,
                cookie.HostOnly,
                false,
                false,
                [CreateIssue(CookieErrorCode.PublicSuffixListUnavailable, CookieParseSeverity.Warning)]);
        }

        CookieBooleanResult suffixResult = IsPublicSuffix(suffixList, cookie.Domain);
        if (!suffixResult.Success)
        {
            return CreateFailure(CookieErrorCode.PublicSuffixListUnavailable);
        }

        if (suffixResult.Value)
        {
            return CreateFailure(CookieErrorCode.PublicSuffix);
        }

        return CreateSuccess(cookie.Domain, cookie.HostOnly, false, false, []);
    }

    static bool IsValidDomainResult(CookieDomainResult domain)
    {
        if (domain.ErrorCode != CookieErrorCode.None || domain.Domain == null ||
            domain.Domain.Length == 0 || !IsCanonicalDomain(domain.Domain, domain.IsIpAddress))
        {
            return false;
        }

        if (domain.IsIpAddress)
        {
            return domain.HostOnly;
        }

        if (domain.HostOnly)
        {
            return !domain.DomainAttributeSupplied;
        }

        return domain.DomainAttributeSupplied;
    }

    static bool IsValidCanonicalOrigin(CookieCanonicalHostResult origin)
    {
        if (origin == null || !origin.Success || origin.ErrorCode != CookieErrorCode.None ||
            origin.Host == null || origin.Host.Length == 0)
        {
            return false;
        }

        int typeCount = 0;
        if (origin.IsDnsName)
        {
            typeCount = typeCount + 1;
        }

        if (origin.IsIpv4)
        {
            typeCount = typeCount + 1;
        }

        if (origin.IsIpv6)
        {
            typeCount = typeCount + 1;
        }

        bool isIpAddress = origin.IsIpv4 || origin.IsIpv6;
        global::System.UriHostNameType type = global::System.Uri.CheckHostName(origin.Host);
        if ((origin.IsIpv4 && type != global::System.UriHostNameType.IPv4) ||
            (origin.IsIpv6 && type != global::System.UriHostNameType.IPv6) ||
            (origin.IsDnsName && type != global::System.UriHostNameType.Dns))
        {
            return false;
        }

        return typeCount == 1 && IsCanonicalDomain(origin.Host, isIpAddress);
    }

    static bool IsCanonicalDomain(string domain, bool isIpAddress)
    {
        global::System.UriHostNameType type = global::System.Uri.CheckHostName(domain);
        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.ErrorCode = CookieErrorCode.None;
        extracted.Host = domain;
        if (type == global::System.UriHostNameType.Dns && !isIpAddress)
        {
            extracted.IsDnsName = true;
        }
        else if (type == global::System.UriHostNameType.IPv4 && isIpAddress)
        {
            extracted.IsIpv4 = true;
        }
        else if (type == global::System.UriHostNameType.IPv6 && isIpAddress)
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

    static CookieBooleanResult IsPublicSuffix(IPublicSuffixList suffixList, string domain)
    {
        global::System.Threading.Tasks.Task<bool> lookup =
            new global::System.Threading.Tasks.Task<bool>(() => suffixList.IsPublicSuffix(domain));
        lookup.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!lookup.IsCompletedSuccessfully)
        {
            lookup.Exception?.Handle(exception => true);
            return new CookieBooleanResult();
        }

        CookieBooleanResult result = new CookieBooleanResult();
        result.Success = true;
        result.Value = lookup.Result;
        return result;
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

    static CookiePublicSuffixResult CreateSuccess(
        string domain,
        bool hostOnly,
        bool domainAttributeSupplied,
        bool isIpAddress,
        CookieIssue[] issues)
    {
        CookiePublicSuffixResult result = new CookiePublicSuffixResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Domain = domain;
        result.HostOnly = hostOnly;
        result.DomainAttributeSupplied = domainAttributeSupplied;
        result.IsIpAddress = isIpAddress;
        result.Issues = issues;
        return result;
    }

    static CookiePublicSuffixResult CreateFailure(CookieErrorCode errorCode)
    {
        CookiePublicSuffixResult result = new CookiePublicSuffixResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode, CookieParseSeverity.Error)];
        return result;
    }
}
