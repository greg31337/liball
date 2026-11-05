namespace cookies;

internal static class CookieStoreValidator
{
    internal static bool IsValidStoredCookie(StoredCookie cookie)
    {
        if (cookie == null || cookie.Name == null || cookie.Value == null ||
            cookie.Domain == null || cookie.Domain.Length == 0 ||
            !IsCanonicalDomain(cookie.Domain) ||
            cookie.Path == null || cookie.Path.Length == 0 || cookie.Path[0] != '/' ||
            cookie.PartitionKey == null ||
            (cookie.HasPartitionKey && cookie.PartitionKey.Length == 0) ||
            cookie.Extensions == null)
        {
            return false;
        }

        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            CookieAttribute extension = cookie.Extensions[extensionIndex];
            if (extension == null || extension.Name == null || extension.Value == null)
            {
                return false;
            }

            extensionIndex = extensionIndex + 1;
        }

        return true;
    }

    internal static bool IsCanonicalDomain(string domain)
    {
        if (domain == null || domain.Length == 0)
        {
            return false;
        }

        global::System.UriHostNameType hostType =
            global::System.Uri.CheckHostName(domain);
        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.Host = domain;
        extracted.IsDnsName = hostType == global::System.UriHostNameType.Dns;
        extracted.IsIpv4 = hostType == global::System.UriHostNameType.IPv4;
        extracted.IsIpv6 = hostType == global::System.UriHostNameType.IPv6;
        CookieCanonicalHostResult canonical =
            CookieHostCanonicalizer.Canonicalize(extracted);
        return canonical.Success && canonical.Host == domain;
    }

    internal static bool IsImmediateDeletion(StoredCookie cookie)
    {
        if (cookie == null)
        {
            return false;
        }

        return CookieExpiryEvaluator.IsExpired(cookie, cookie.CreationTime);
    }
}
