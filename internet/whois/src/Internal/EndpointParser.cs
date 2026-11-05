namespace whois;

internal class EndpointParseResult
{
    internal WhoisEndpoint? Endpoint = null;
    internal WhoisReferralScheme Scheme = WhoisReferralScheme.Unspecified;
    internal string PathSuffix = string.Empty;
    internal string EqualityKey = string.Empty;
    internal bool IsValid = false;
    internal string RejectionReason = string.Empty;
}

internal static class EndpointParser
{
    internal static EndpointParseResult ParseValue(string advertisedValue)
    {
        InputGuard.RequireReference(advertisedValue, "advertisedValue");
        EndpointParseResult result = new EndpointParseResult();
        if (ContainsControlCharacter(advertisedValue))
        {
            result.RejectionReason = "The endpoint value contains a control character.";
            return result;
        }
        string candidate = advertisedValue.Trim();
        if (candidate.Length == 0)
        {
            result.RejectionReason = "The endpoint value is empty.";
            return result;
        }

        WhoisReferralScheme advertisedScheme = WhoisReferralScheme.Unspecified;
        WhoisReferralScheme effectiveScheme = WhoisReferralScheme.Whois;
        int defaultPort = WhoisDefaults.WhoisPort;
        string authorityAndSuffix = candidate;
        int schemeSeparator = candidate.IndexOf("://", System.StringComparison.Ordinal);
        if (schemeSeparator >= 0)
        {
            string schemeText = candidate.Substring(0, schemeSeparator).ToLowerInvariant();
            if (schemeText == "whois")
            {
                advertisedScheme = WhoisReferralScheme.Whois;
                effectiveScheme = WhoisReferralScheme.Whois;
                defaultPort = WhoisDefaults.WhoisPort;
            }
            else if (schemeText == "rwhois")
            {
                advertisedScheme = WhoisReferralScheme.RWhois;
                effectiveScheme = WhoisReferralScheme.RWhois;
                defaultPort = WhoisDefaults.RWhoisPort;
            }
            else if (schemeText == "http")
            {
                advertisedScheme = WhoisReferralScheme.Http;
                effectiveScheme = WhoisReferralScheme.Http;
                defaultPort = 80;
            }
            else if (schemeText == "https")
            {
                advertisedScheme = WhoisReferralScheme.Https;
                effectiveScheme = WhoisReferralScheme.Https;
                defaultPort = 443;
            }
            else
            {
                result.Scheme = WhoisReferralScheme.Unsupported;
                result.RejectionReason = "The endpoint URI scheme is not supported.";
                return result;
            }
            authorityAndSuffix = candidate.Substring(schemeSeparator + 3);
        }
        else if (candidate.IndexOf('/') >= 0 || candidate.IndexOf('?') >= 0 || candidate.IndexOf('#') >= 0)
        {
            result.RejectionReason = "A bare endpoint must contain only an authority.";
            return result;
        }

        int suffixIndex = FindSuffixIndex(authorityAndSuffix);
        string authority = authorityAndSuffix;
        string pathSuffix = string.Empty;
        if (suffixIndex >= 0)
        {
            authority = authorityAndSuffix.Substring(0, suffixIndex);
            pathSuffix = authorityAndSuffix.Substring(suffixIndex);
        }
        if (authority.Length == 0)
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "The endpoint host is missing.";
            return result;
        }
        if (authority.IndexOf('@') >= 0)
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "Endpoint user information is not permitted.";
            return result;
        }
        if (advertisedScheme == WhoisReferralScheme.Whois && pathSuffix.Length > 0 && pathSuffix != "/")
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "A WHOIS endpoint URI must not contain a path, query, or fragment.";
            return result;
        }
        if (advertisedScheme == WhoisReferralScheme.RWhois && (pathSuffix.IndexOf('?') >= 0 || pathSuffix.IndexOf('#') >= 0))
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "An RWhois endpoint URI must not contain a query or fragment.";
            return result;
        }

        string hostText;
        int port = defaultPort;
        bool bracketed = false;
        if (authority[0] == '[')
        {
            bracketed = true;
            int closingBracket = authority.IndexOf(']');
            if (closingBracket <= 1)
            {
                result.Scheme = advertisedScheme;
                result.RejectionReason = "The bracketed endpoint host is invalid.";
                return result;
            }
            hostText = authority.Substring(1, closingBracket - 1);
            if (closingBracket + 1 < authority.Length)
            {
                if (authority[closingBracket + 1] != ':')
                {
                    result.Scheme = advertisedScheme;
                    result.RejectionReason = "Unexpected text follows the bracketed endpoint host.";
                    return result;
                }
                port = ParsePort(authority.Substring(closingBracket + 2));
            }
            else if (closingBracket + 1 > authority.Length)
            {
                result.Scheme = advertisedScheme;
                result.RejectionReason = "The bracketed endpoint host is invalid.";
                return result;
            }
        }
        else
        {
            int firstColon = authority.IndexOf(':');
            if (firstColon >= 0)
            {
                if (authority.IndexOf(':', firstColon + 1) >= 0)
                {
                    result.Scheme = advertisedScheme;
                    result.RejectionReason = "An IPv6 endpoint must be enclosed in brackets.";
                    return result;
                }
                hostText = authority.Substring(0, firstColon);
                port = ParsePort(authority.Substring(firstColon + 1));
            }
            else
            {
                hostText = authority;
            }
        }

        if (port < 1 || port > 65535)
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "The endpoint port is invalid.";
            return result;
        }
        if (hostText.Length == 0 || hostText != hostText.Trim())
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "The endpoint host is missing or contains surrounding whitespace.";
            return result;
        }

        string normalizedHost = NormalizeHost(hostText, bracketed);
        if (normalizedHost.Length == 0)
        {
            result.Scheme = advertisedScheme;
            result.RejectionReason = "The endpoint host is invalid.";
            return result;
        }

        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Scheme = effectiveScheme;
        endpoint.Host = normalizedHost;
        endpoint.Port = port;
        endpoint.AdvertisedValue = advertisedValue;
        result.Endpoint = endpoint;
        result.Scheme = advertisedScheme;
        result.PathSuffix = pathSuffix;
        result.EqualityKey = GetEqualityKey(endpoint);
        result.IsValid = true;
        return result;
    }

    internal static string GetEqualityKey(WhoisEndpoint endpoint)
    {
        InputGuard.RequireReference(endpoint, "endpoint");
        string scheme = "whois";
        if (endpoint.Scheme == WhoisReferralScheme.RWhois)
        {
            scheme = "rwhois";
        }
        else if (endpoint.Scheme == WhoisReferralScheme.Http)
        {
            scheme = "http";
        }
        else if (endpoint.Scheme == WhoisReferralScheme.Https)
        {
            scheme = "https";
        }
        else if (endpoint.Scheme != WhoisReferralScheme.Whois && endpoint.Scheme != WhoisReferralScheme.Unspecified)
        {
            throw new System.ArgumentOutOfRangeException("endpoint.Scheme");
        }

        string host = endpoint.Host.ToLowerInvariant();
        if (System.Uri.CheckHostName(host) == System.UriHostNameType.IPv6)
        {
            host = "[" + host + "]";
        }
        return scheme + "://" + host + ":" + endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static int FindSuffixIndex(string value)
    {
        int result = -1;
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (slash >= 0)
        {
            result = slash;
        }
        if (query >= 0 && (result < 0 || query < result))
        {
            result = query;
        }
        if (fragment >= 0 && (result < 0 || fragment < result))
        {
            result = fragment;
        }
        return result;
    }

    internal static int ParsePort(string portText)
    {
        if (portText.Length == 0 || portText.Length > 5)
        {
            return -1;
        }
        int port = 0;
        int index = 0;
        while (index < portText.Length)
        {
            char digit = portText[index];
            if (digit < '0' || digit > '9')
            {
                return -1;
            }
            port = port * 10 + digit - '0';
            index = index + 1;
        }
        return port;
    }

    internal static string NormalizeHost(string hostText, bool bracketed)
    {
        if (hostText.IndexOf('%') >= 0)
        {
            return string.Empty;
        }
        System.UriHostNameType hostType = System.Uri.CheckHostName(hostText);
        if (bracketed)
        {
            if (hostType != System.UriHostNameType.IPv6)
            {
                return string.Empty;
            }
            return System.Net.IPAddress.Parse(hostText).ToString().ToLowerInvariant();
        }
        if (hostType == System.UriHostNameType.IPv4)
        {
            if (!WhoisQueryClassifier.IsStrictIpv4Literal(hostText))
            {
                return string.Empty;
            }
            return System.Net.IPAddress.Parse(hostText).ToString();
        }
        if (hostType != System.UriHostNameType.Dns)
        {
            return string.Empty;
        }

        System.Threading.Tasks.Task<WhoisQuery> normalizationTask = System.Threading.Tasks.Task.Run<WhoisQuery>(() => IdnNormalizer.NormalizeDomain(hostText));
        System.Threading.Tasks.Task completedTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { normalizationTask }).Result;
        if (completedTask != normalizationTask || !normalizationTask.IsCompletedSuccessfully)
        {
            System.AggregateException? normalizationError = normalizationTask.Exception;
            if (normalizationError != null)
            {
                normalizationError.Handle((error) => true);
            }
            return string.Empty;
        }
        return normalizationTask.Result.NormalizedText;
    }

    internal static bool ContainsControlCharacter(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsControl(value[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
