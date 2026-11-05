namespace cookies;

internal static class CookieUriHostExtractor
{
    internal static CookieUriHostResult Extract(global::System.Uri uri)
    {
        if (uri == null || !uri.IsAbsoluteUri)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        string host = uri.Host;
        if (host.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            return CreateFailure(CookieErrorCode.UnsupportedScheme);
        }

        bool hadBrackets = false;
        if (host[0] == '[')
        {
            if (host.Length < 3 || host[host.Length - 1] != ']')
            {
                return CreateFailure(CookieErrorCode.InvalidHost);
            }

            hadBrackets = true;
            host = host.Substring(1, host.Length - 2);
        }
        else if (host[host.Length - 1] == ']')
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (host.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (host[host.Length - 1] == '.')
        {
            host = host.Substring(0, host.Length - 1);
            if (host.Length == 0 || host[host.Length - 1] == '.')
            {
                return CreateFailure(CookieErrorCode.InvalidHost);
            }
        }

        global::System.UriHostNameType hostType = global::System.Uri.CheckHostName(host);
        if (hadBrackets && hostType != global::System.UriHostNameType.IPv6)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        CookieUriHostResult result = new CookieUriHostResult();
        result.Host = host;
        if (hostType == global::System.UriHostNameType.Dns)
        {
            result.IsDnsName = true;
        }
        else if (hostType == global::System.UriHostNameType.IPv4)
        {
            result.IsIpv4 = true;
        }
        else if (hostType == global::System.UriHostNameType.IPv6)
        {
            result.IsIpv6 = true;
        }
        else
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        return result;
    }

    static CookieUriHostResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieUriHostResult result = new CookieUriHostResult();
        result.ErrorCode = errorCode;
        return result;
    }
}
