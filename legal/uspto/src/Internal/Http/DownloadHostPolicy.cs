namespace uspto;

internal interface IDownloadHostResolver
{
    global::System.Net.IPAddress[] Resolve(string host, global::System.Threading.CancellationToken cancellationToken);
}

internal class SystemDownloadHostResolver : IDownloadHostResolver
{
    public global::System.Net.IPAddress[] Resolve(string host, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return global::System.Net.Dns.GetHostAddressesAsync(host, cancellationToken).GetAwaiter().GetResult();
    }
}

internal class DownloadHostValidation
{
    internal global::System.Uri Uri = new global::System.Uri("https://invalid.example/");
    internal string Host = string.Empty;
    internal global::System.Net.IPAddress[] Addresses = new global::System.Net.IPAddress[0];
    internal bool IsOfficialHost = false;
    internal bool IsLoopbackTestEndpoint = false;
}

internal class DownloadHostPolicy
{
    internal IDownloadHostResolver Resolver = new SystemDownloadHostResolver();

    internal DownloadHostPolicy(IDownloadHostResolver resolver)
    {
        if (resolver == null)
        {
            throw new ValidationException("A download host resolver is required.", "resolver", "REQUIRED", null);
        }

        Resolver = resolver;
    }

    internal static DownloadHostPolicy CreateDefault()
    {
        return new DownloadHostPolicy(new SystemDownloadHostResolver());
    }

    internal DownloadHostValidation Validate(global::System.Uri uri, UsptoClientOptions options, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (uri == null)
        {
            throw new ValidationException("A download URI is required.", "uri", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        if (!uri.IsAbsoluteUri)
        {
            throw new ValidationException("The download URI must be absolute.", "uri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (uri.UserInfo.Length != 0)
        {
            throw new ValidationException("The download URI cannot contain user information.", "uri", "DOWNLOAD_USERINFO_REJECTED", null);
        }

        if (uri.Fragment.Length != 0)
        {
            throw new ValidationException("The download URI cannot contain a fragment.", "uri", "DOWNLOAD_FRAGMENT_REJECTED", null);
        }

        bool isHttps = global::System.String.Equals(uri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase);
        bool isHttp = global::System.String.Equals(uri.Scheme, global::System.Uri.UriSchemeHttp, global::System.StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !isHttp)
        {
            throw new ValidationException("The download URI must use HTTP or HTTPS.", "uri", "DOWNLOAD_SCHEME_REJECTED", null);
        }

        string host = NormalizeHost(uri.IdnHost);
        if (host.Length == 0)
        {
            throw new ValidationException("The download URI must contain a host.", "uri", "DOWNLOAD_HOST_REQUIRED", null);
        }

        bool officialHost = IsOfficialDownloadHost(host);
        bool configuredHost = IsConfiguredDownloadHost(host, options.PermittedDownloadHosts);
        if (!officialHost && !configuredHost)
        {
            throw new ValidationException("The download host is not approved.", "uri", "DOWNLOAD_HOST_NOT_ALLOWED", null);
        }

        global::System.Net.IPAddress[] addresses = ResolveAddresses(uri, host, cancellationToken);
        if (addresses == null || addresses.Length == 0)
        {
            throw new ValidationException("The download host did not resolve to an address.", "uri", "DOWNLOAD_HOST_RESOLUTION_EMPTY", null);
        }

        bool allLoopback = true;
        bool allPublic = true;
        foreach (global::System.Net.IPAddress address in addresses)
        {
            if (address == null)
            {
                allLoopback = false;
                allPublic = false;
                continue;
            }

            if (!IsLoopbackAddress(address))
            {
                allLoopback = false;
            }

            if (!IsPublicAddress(address))
            {
                allPublic = false;
            }
        }

        bool loopbackTestEndpoint = configuredHost && options.PermitInsecureTestEndpoints && allLoopback;
        if (!allPublic && !loopbackTestEndpoint)
        {
            throw new ValidationException("The download host resolves to a loopback, private, or non-public address.", "uri", "DOWNLOAD_ADDRESS_REJECTED", null);
        }

        if (!isHttps && !loopbackTestEndpoint)
        {
            throw new ValidationException("Non-HTTPS downloads are permitted only for explicitly configured loopback tests.", "uri", "DOWNLOAD_HTTPS_REQUIRED", null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        DownloadHostValidation result = new DownloadHostValidation();
        result.Uri = uri;
        result.Host = host;
        result.Addresses = addresses;
        result.IsOfficialHost = officialHost;
        result.IsLoopbackTestEndpoint = loopbackTestEndpoint;
        return result;
    }

    internal global::System.Net.IPAddress[] ResolveAddresses(global::System.Uri uri, string host, global::System.Threading.CancellationToken cancellationToken)
    {
        if (uri.HostNameType == global::System.UriHostNameType.IPv4 || uri.HostNameType == global::System.UriHostNameType.IPv6)
        {
            string addressText = host;
            if (addressText.Length >= 2 && addressText[0] == '[' && addressText[addressText.Length - 1] == ']')
            {
                addressText = addressText.Substring(1, addressText.Length - 2);
            }

            return new global::System.Net.IPAddress[] { global::System.Net.IPAddress.Parse(addressText) };
        }

        return Resolver.Resolve(host, cancellationToken);
    }

    internal static bool IsOfficialDownloadHost(string host)
    {
        return global::System.String.Equals(host, "api.uspto.gov", global::System.StringComparison.OrdinalIgnoreCase)
            || global::System.String.Equals(host, "ptab.uspto.gov", global::System.StringComparison.OrdinalIgnoreCase)
            || global::System.String.Equals(host, "tsdrapi.uspto.gov", global::System.StringComparison.OrdinalIgnoreCase)
            || global::System.String.Equals(host, "bulkdata.uspto.gov", global::System.StringComparison.OrdinalIgnoreCase)
            || global::System.String.Equals(host, "search.patentsview.org", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsConfiguredDownloadHost(string host, string[] configuredHosts)
    {
        if (configuredHosts == null)
        {
            throw new ValidationException("The configured download host list is required.", "PermittedDownloadHosts", "REQUIRED", null);
        }

        bool matched = false;
        foreach (string configuredHost in configuredHosts)
        {
            string normalized = NormalizeConfiguredHost(configuredHost);
            if (global::System.String.Equals(host, normalized, global::System.StringComparison.OrdinalIgnoreCase))
            {
                matched = true;
            }
        }

        return matched;
    }

    internal static string NormalizeConfiguredHost(string configuredHost)
    {
        if (global::System.String.IsNullOrWhiteSpace(configuredHost))
        {
            throw new ValidationException("Configured download hosts cannot be blank.", "PermittedDownloadHosts", "INVALID_DOWNLOAD_HOST", null);
        }

        string host = configuredHost.Trim();
        if (host[0] == '[' && host[host.Length - 1] == ']' && host.Length > 2)
        {
            host = host.Substring(1, host.Length - 2);
        }

        host = NormalizeHost(host);
        if (host.Length == 0 || host.IndexOf('/') >= 0 || host.IndexOf('@') >= 0 || host.IndexOf('?') >= 0 || host.IndexOf('#') >= 0 || host.IndexOf('*') >= 0 || host.IndexOf('[') >= 0 || host.IndexOf(']') >= 0 || ContainsWhitespace(host))
        {
            throw new ValidationException("Configured download hosts must be exact host names without URI components or wildcards.", "PermittedDownloadHosts", "INVALID_DOWNLOAD_HOST", null);
        }

        int firstColon = host.IndexOf(':');
        if (firstColon >= 0 && firstColon == host.LastIndexOf(':'))
        {
            throw new ValidationException("Configured download hosts cannot contain ports.", "PermittedDownloadHosts", "INVALID_DOWNLOAD_HOST", null);
        }

        global::System.UriHostNameType hostNameType = global::System.Uri.CheckHostName(host);
        bool supportedHostType = hostNameType == global::System.UriHostNameType.Dns || hostNameType == global::System.UriHostNameType.IPv4 || hostNameType == global::System.UriHostNameType.IPv6;
        if (!supportedHostType)
        {
            throw new ValidationException("Configured download hosts must contain a valid DNS name or IP address.", "PermittedDownloadHosts", "INVALID_DOWNLOAD_HOST", null);
        }

        if (hostNameType == global::System.UriHostNameType.IPv4 || hostNameType == global::System.UriHostNameType.IPv6)
        {
            host = global::System.Net.IPAddress.Parse(host).ToString();
        }

        return host;
    }

    internal static bool ContainsWhitespace(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            if (global::System.Char.IsWhiteSpace(value[index]))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeHost(string host)
    {
        return host.Trim().TrimEnd('.');
    }

    internal static bool IsLoopbackAddress(global::System.Net.IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return global::System.Net.IPAddress.IsLoopback(address);
    }

    internal static bool IsPublicAddress(global::System.Net.IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return IsPublicIpv4Address(address.GetAddressBytes());
        }

        if (address.AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return IsPublicIpv6Address(address);
        }

        return false;
    }

    internal static bool IsPublicIpv4Address(byte[] bytes)
    {
        if (bytes.Length != 4)
        {
            return false;
        }

        int first = bytes[0];
        int second = bytes[1];
        int third = bytes[2];
        if (first == 0 || first == 10 || first == 127 || first >= 224)
        {
            return false;
        }

        if (first == 100 && second >= 64 && second <= 127)
        {
            return false;
        }

        if (first == 169 && second == 254)
        {
            return false;
        }

        if (first == 172 && second >= 16 && second <= 31)
        {
            return false;
        }

        if (first == 192 && second == 168)
        {
            return false;
        }

        if (first == 192 && second == 0 && third == 0)
        {
            return false;
        }

        if (first == 192 && second == 0 && third == 2)
        {
            return false;
        }

        if (first == 192 && second == 88 && third == 99)
        {
            return false;
        }

        if (first == 198 && (second == 18 || second == 19))
        {
            return false;
        }

        if (first == 198 && second == 51 && third == 100)
        {
            return false;
        }

        if (first == 203 && second == 0 && third == 113)
        {
            return false;
        }

        return true;
    }

    internal static bool IsPublicIpv6Address(global::System.Net.IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal || global::System.Net.IPAddress.IsLoopback(address))
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();
        if (bytes.Length != 16 || bytes[0] < 32 || bytes[0] > 63)
        {
            return false;
        }

        if (bytes[0] == 32 && bytes[1] == 1 && bytes[2] == 13 && bytes[3] == 184)
        {
            return false;
        }

        if (bytes[0] == 32 && bytes[1] == 1 && bytes[2] == 0 && bytes[3] == 0)
        {
            return false;
        }

        if (bytes[0] == 32 && bytes[1] == 2)
        {
            byte[] embedded = new byte[] { bytes[2], bytes[3], bytes[4], bytes[5] };
            return IsPublicIpv4Address(embedded);
        }

        return true;
    }
}
