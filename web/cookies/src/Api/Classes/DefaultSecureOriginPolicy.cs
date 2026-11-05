namespace cookies;

/// <summary>
/// Classifies secure cookie origins using secure schemes and an optional loopback exception.
/// </summary>
public class DefaultSecureOriginPolicy : ISecureOriginPolicy
{
    internal bool TreatLoopbackAsSecure;

    /// <summary>
    /// Creates the default secure-origin policy.
    /// </summary>
    /// <param name="treatLoopbackAsSecure">
    /// True to trust HTTP and WebSocket origins on loopback addresses or localhost names.
    /// </param>
    public DefaultSecureOriginPolicy(bool treatLoopbackAsSecure)
    {
        TreatLoopbackAsSecure = treatLoopbackAsSecure;
    }

    /// <summary>
    /// Determines whether an absolute origin URI is secure.
    /// </summary>
    /// <param name="origin">The absolute origin URI.</param>
    /// <returns>True when the origin is secure.</returns>
    public bool IsSecureOrigin(global::System.Uri origin)
    {
        if (origin == null || !origin.IsAbsoluteUri)
        {
            return false;
        }

        string scheme = origin.Scheme;
        if (scheme == "https" || scheme == "wss")
        {
            return GetCanonicalHost(origin).Success;
        }

        if (!TreatLoopbackAsSecure || (scheme != "http" && scheme != "ws"))
        {
            return false;
        }

        CookieCanonicalHostResult host = GetCanonicalHost(origin);
        if (!host.Success)
        {
            return false;
        }

        if (host.IsIpv4 || host.IsIpv6)
        {
            global::System.Net.IPAddress address = global::System.Net.IPAddress.Parse(host.Host);
            return global::System.Net.IPAddress.IsLoopback(address);
        }

        return IsLocalhostName(host.Host);
    }

    internal static CookieCanonicalHostResult GetCanonicalHost(global::System.Uri origin)
    {
        string host = origin.Host;
        if (host.Length == 0)
        {
            return new CookieCanonicalHostResult();
        }

        if (host[0] == '[')
        {
            if (host.Length < 3 || host[host.Length - 1] != ']')
            {
                return new CookieCanonicalHostResult();
            }

            host = host.Substring(1, host.Length - 2);
        }
        else if (host[host.Length - 1] == ']')
        {
            return new CookieCanonicalHostResult();
        }

        if (host.Length == 0)
        {
            return new CookieCanonicalHostResult();
        }

        if (host[host.Length - 1] == '.')
        {
            host = host.Substring(0, host.Length - 1);
            if (host.Length == 0 || host[host.Length - 1] == '.')
            {
                return new CookieCanonicalHostResult();
            }
        }

        global::System.UriHostNameType hostType = global::System.Uri.CheckHostName(host);
        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.ErrorCode = CookieErrorCode.None;
        extracted.Host = host;
        if (hostType == global::System.UriHostNameType.Dns)
        {
            extracted.IsDnsName = true;
        }
        else if (hostType == global::System.UriHostNameType.IPv4)
        {
            extracted.IsIpv4 = true;
        }
        else if (hostType == global::System.UriHostNameType.IPv6)
        {
            extracted.IsIpv6 = true;
        }
        else
        {
            return new CookieCanonicalHostResult();
        }

        return CookieHostCanonicalizer.Canonicalize(extracted);
    }

    internal static bool IsLocalhostName(string host)
    {
        const string localhost = "localhost";
        if (host == localhost)
        {
            return true;
        }

        if (host.Length <= localhost.Length)
        {
            return false;
        }

        int suffixStart = host.Length - localhost.Length;
        if (host[suffixStart - 1] != '.')
        {
            return false;
        }

        int index = 0;
        while (index < localhost.Length)
        {
            if (host[suffixStart + index] != localhost[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
