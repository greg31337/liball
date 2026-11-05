namespace cookies;

internal static class CookieDomainMatcher
{
    internal static bool DomainMatches(string requestHost, string cookieDomain, bool hostOnly)
    {
        if (requestHost == null || cookieDomain == null ||
            requestHost.Length == 0 || cookieDomain.Length == 0)
        {
            return false;
        }

        if (EqualsAsciiIgnoreCase(requestHost, cookieDomain))
        {
            return true;
        }

        if (hostOnly || IsIpAddress(requestHost) || IsIpAddress(cookieDomain) ||
            requestHost.Length <= cookieDomain.Length)
        {
            return false;
        }

        int suffixStart = requestHost.Length - cookieDomain.Length;
        if (requestHost[suffixStart - 1] != '.')
        {
            return false;
        }

        int domainIndex = 0;
        while (domainIndex < cookieDomain.Length)
        {
            if (ToAsciiLower(requestHost[suffixStart + domainIndex]) !=
                ToAsciiLower(cookieDomain[domainIndex]))
            {
                return false;
            }

            domainIndex = domainIndex + 1;
        }

        return true;
    }

    internal static CookieDomainResult NormalizeForOrigin(
        SetCookie cookie,
        CookieCanonicalHostResult origin,
        CookieComplianceMode complianceMode)
    {
        if (complianceMode != CookieComplianceMode.Rfc6265 &&
            complianceMode != CookieComplianceMode.Rfc6265Bis &&
            complianceMode != CookieComplianceMode.Compatible)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (cookie == null || cookie.Domain == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!IsValidOrigin(origin))
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        bool originIsIp = origin.IsIpv4 || origin.IsIpv6;
        if (!cookie.HasDomain)
        {
            return CreateSuccess(origin.Host, true, false, originIsIp);
        }

        string domain = cookie.Domain;
        if (domain.Length == 0 || domain[domain.Length - 1] == '.')
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (domain[0] == '.')
        {
            domain = domain.Substring(1);
            if (domain.Length == 0 || domain[0] == '.')
            {
                return CreateFailure(CookieErrorCode.InvalidAttribute);
            }
        }

        if (ContainsBracket(domain))
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        global::System.UriHostNameType domainType = global::System.Uri.CheckHostName(domain);
        CookieUriHostResult extractedDomain = new CookieUriHostResult();
        extractedDomain.Success = true;
        extractedDomain.ErrorCode = CookieErrorCode.None;
        extractedDomain.Host = domain;
        if (domainType == global::System.UriHostNameType.Dns)
        {
            extractedDomain.IsDnsName = true;
        }
        else if (domainType == global::System.UriHostNameType.IPv4)
        {
            extractedDomain.IsIpv4 = true;
        }
        else if (domainType == global::System.UriHostNameType.IPv6)
        {
            extractedDomain.IsIpv6 = true;
        }
        else
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        CookieCanonicalHostResult canonicalDomain =
            CookieHostCanonicalizer.Canonicalize(extractedDomain);
        if (!canonicalDomain.Success)
        {
            if (canonicalDomain.ErrorCode == CookieErrorCode.InvalidHost)
            {
                return CreateFailure(CookieErrorCode.InvalidAttribute);
            }

            return CreateFailure(canonicalDomain.ErrorCode);
        }

        bool domainIsIp = canonicalDomain.IsIpv4 || canonicalDomain.IsIpv6;
        if (originIsIp)
        {
            if (!domainIsIp || origin.IsIpv4 != canonicalDomain.IsIpv4 ||
                origin.IsIpv6 != canonicalDomain.IsIpv6 ||
                !EqualsAsciiIgnoreCase(origin.Host, canonicalDomain.Host))
            {
                return CreateFailure(CookieErrorCode.DomainMismatch);
            }

            return CreateSuccess(origin.Host, true, true, true);
        }

        if (!canonicalDomain.IsDnsName ||
            !DomainMatches(origin.Host, canonicalDomain.Host, false))
        {
            return CreateFailure(CookieErrorCode.DomainMismatch);
        }

        return CreateSuccess(canonicalDomain.Host, false, true, false);
    }

    static bool IsValidOrigin(CookieCanonicalHostResult origin)
    {
        if (origin == null || !origin.Success || origin.Host == null || origin.Host.Length == 0)
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

        if (typeCount != 1)
        {
            return false;
        }

        if (origin.ErrorCode != CookieErrorCode.None)
        {
            return false;
        }

        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.ErrorCode = CookieErrorCode.None;
        extracted.Host = origin.Host;
        if (origin.IsIpv4)
        {
            extracted.IsIpv4 = true;
        }
        else if (origin.IsIpv6)
        {
            extracted.IsIpv6 = true;
        }
        else
        {
            extracted.IsDnsName = true;
        }

        CookieCanonicalHostResult canonical = CookieHostCanonicalizer.Canonicalize(extracted);
        return canonical.Success && canonical.Host == origin.Host &&
            canonical.IsDnsName == origin.IsDnsName &&
            canonical.IsIpv4 == origin.IsIpv4 && canonical.IsIpv6 == origin.IsIpv6;
    }

    static bool IsIpAddress(string host)
    {
        global::System.UriHostNameType type = global::System.Uri.CheckHostName(host);
        return type == global::System.UriHostNameType.IPv4 ||
            type == global::System.UriHostNameType.IPv6;
    }

    static bool ContainsBracket(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '[' || value[index] == ']')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool EqualsAsciiIgnoreCase(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (ToAsciiLower(left[index]) != ToAsciiLower(right[index]))
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

    static CookieDomainResult CreateSuccess(
        string domain,
        bool hostOnly,
        bool domainAttributeSupplied,
        bool isIpAddress)
    {
        CookieDomainResult result = new CookieDomainResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Domain = domain;
        result.HostOnly = hostOnly;
        result.DomainAttributeSupplied = domainAttributeSupplied;
        result.IsIpAddress = isIpAddress;
        return result;
    }

    static CookieDomainResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieDomainResult result = new CookieDomainResult();
        result.ErrorCode = errorCode;
        return result;
    }
}
