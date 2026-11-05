using System;
using System.Collections.Generic;
using System.Net;

namespace dns;

internal static class ResolverConfigurationOperations
{
    const int MaximumParsedServerCount = 3;
    const int MaximumExplicitServerCount = 16;
    const int MaximumSearchDomainCount = 6;
    const int MaximumNameDots = 15;
    const int MaximumAttempts = 5;
    const int MaximumTimeoutSeconds = 30;

    internal static DnsResolverConfiguration ParseConfiguration(string source)
    {
        if (source == null)
        {
            throw new DnsResolutionException(
                "Resolver configuration text is required.");
        }

        DnsResolverConfiguration result = new DnsResolverConfiguration();
        string[] lines = source.Split('\n');
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string line = RemoveComment(lines[lineIndex]).Trim();
            if (line.Length > 0)
            {
                string[] tokens = line.Split(
                    [' ', '\t', '\r'],
                    StringSplitOptions.RemoveEmptyEntries);
                string keyword = tokens[0].ToLowerInvariant();
                if (keyword == "nameserver")
                {
                    ParseNameServer(tokens, result, lineIndex);
                }
                else if (keyword == "search")
                {
                    ParseSearch(tokens, result, lineIndex);
                }
                else if (keyword == "domain")
                {
                    ParseDomain(tokens, result, lineIndex);
                }
                else if (keyword == "options")
                {
                    ParseOptions(tokens, result, lineIndex);
                }
            }

            lineIndex = lineIndex + 1;
        }

        return ValidateConfiguration(result);
    }

    internal static DnsResolverConfiguration ValidateConfiguration(
        DnsResolverConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new DnsResolutionException(
                "Resolver configuration is required.");
        }

        if (configuration.Servers == null ||
            configuration.SearchDomains == null)
        {
            throw new DnsResolutionException(
                "Resolver server and search-domain collections are required.");
        }

        if (configuration.Servers.Count > MaximumExplicitServerCount)
        {
            throw new DnsResolutionException(
                "Resolver configuration contains too many servers.");
        }

        if (configuration.SearchDomains.Count > MaximumSearchDomainCount)
        {
            throw new DnsResolutionException(
                "Resolver configuration contains too many search domains.");
        }

        if (configuration.NameDots < 0 ||
            configuration.NameDots > MaximumNameDots)
        {
            throw new DnsResolutionException(
                "Resolver ndots must be between zero and fifteen.");
        }

        if (configuration.Attempts < 1 ||
            configuration.Attempts > MaximumAttempts)
        {
            throw new DnsResolutionException(
                "Resolver attempts must be between one and five.");
        }

        if (configuration.TimeoutMilliseconds < 1 ||
            configuration.TimeoutMilliseconds >
                MaximumTimeoutSeconds * 1000)
        {
            throw new DnsResolutionException(
                "Resolver timeout must be between one and 30000 milliseconds.");
        }

        DnsResolverConfiguration result = new DnsResolverConfiguration();
        result.NameDots = configuration.NameDots;
        result.Attempts = configuration.Attempts;
        result.TimeoutMilliseconds = configuration.TimeoutMilliseconds;
        result.RotateServers = configuration.RotateServers;
        result.UseTcp = configuration.UseTcp;
        result.TrustAuthenticData = configuration.TrustAuthenticData;
        result.UseEdns = configuration.UseEdns;
        int serverIndex = 0;
        while (serverIndex < configuration.Servers.Count)
        {
            result.Servers.Add(CopyServer(
                configuration.Servers[serverIndex],
                serverIndex));
            serverIndex = serverIndex + 1;
        }

        if (result.Servers.Count == 0)
        {
            result.Servers.Add(CreateLoopbackServer());
        }

        bool hasEnabledServer = false;
        serverIndex = 0;
        while (serverIndex < result.Servers.Count)
        {
            if (result.Servers[serverIndex].Enabled)
            {
                hasEnabledServer = true;
            }

            serverIndex = serverIndex + 1;
        }

        if (!hasEnabledServer)
        {
            throw new DnsResolutionException(
                "Resolver configuration requires an enabled server.");
        }

        int searchIndex = 0;
        while (searchIndex < configuration.SearchDomains.Count)
        {
            DnsName searchDomain = configuration.SearchDomains[searchIndex];
            if (searchDomain == null)
            {
                throw new DnsResolutionException(
                    "A resolver search domain cannot be null.");
            }

            DnsName normalized = CopySearchDomain(searchDomain);
            result.SearchDomains.Add(normalized);
            searchIndex = searchIndex + 1;
        }

        return result;
    }

    static string RemoveComment(string line)
    {
        int hashIndex = line.IndexOf('#');
        int semicolonIndex = line.IndexOf(';');
        int commentIndex = hashIndex;
        if (commentIndex < 0 ||
            (semicolonIndex >= 0 && semicolonIndex < commentIndex))
        {
            commentIndex = semicolonIndex;
        }

        if (commentIndex >= 0)
        {
            return line.Substring(0, commentIndex);
        }

        return line;
    }

    static void ParseNameServer(
        string[] tokens,
        DnsResolverConfiguration configuration,
        int lineIndex)
    {
        if (tokens.Length != 2)
        {
            throw LineFailure(
                lineIndex,
                "A nameserver directive requires one numeric address.");
        }

        DnsEndpoint endpoint = ParseNumericEndpoint(tokens[1], lineIndex);
        if (configuration.Servers.Count < MaximumParsedServerCount)
        {
            DnsResolverServer server = new DnsResolverServer();
            server.Endpoint = endpoint;
            server.Priority = configuration.Servers.Count;
            configuration.Servers.Add(server);
        }
    }

    static void ParseSearch(
        string[] tokens,
        DnsResolverConfiguration configuration,
        int lineIndex)
    {
        if (tokens.Length < 2)
        {
            throw LineFailure(
                lineIndex,
                "A search directive requires at least one domain.");
        }

        if (tokens.Length - 1 > MaximumSearchDomainCount)
        {
            throw LineFailure(
                lineIndex,
                "A search directive contains too many domains.");
        }

        configuration.SearchDomains.Clear();
        int index = 1;
        while (index < tokens.Length)
        {
            configuration.SearchDomains.Add(ParseSearchDomain(
                tokens[index],
                lineIndex));
            index = index + 1;
        }
    }

    static void ParseDomain(
        string[] tokens,
        DnsResolverConfiguration configuration,
        int lineIndex)
    {
        if (tokens.Length != 2)
        {
            throw LineFailure(
                lineIndex,
                "A domain directive requires one domain.");
        }

        configuration.SearchDomains.Clear();
        configuration.SearchDomains.Add(ParseSearchDomain(
            tokens[1],
            lineIndex));
    }

    static void ParseOptions(
        string[] tokens,
        DnsResolverConfiguration configuration,
        int lineIndex)
    {
        int index = 1;
        while (index < tokens.Length)
        {
            string option = tokens[index].ToLowerInvariant();
            if (option.StartsWith("ndots:", StringComparison.Ordinal))
            {
                configuration.NameDots = ReadBoundedOption(
                    option.Substring(6),
                    0,
                    MaximumNameDots,
                    "ndots",
                    lineIndex);
            }
            else if (option.StartsWith("timeout:", StringComparison.Ordinal))
            {
                int seconds = ReadBoundedOption(
                    option.Substring(8),
                    1,
                    MaximumTimeoutSeconds,
                    "timeout",
                    lineIndex);
                configuration.TimeoutMilliseconds = seconds * 1000;
            }
            else if (option.StartsWith("attempts:", StringComparison.Ordinal))
            {
                configuration.Attempts = ReadBoundedOption(
                    option.Substring(9),
                    1,
                    MaximumAttempts,
                    "attempts",
                    lineIndex);
            }
            else if (option == "rotate")
            {
                configuration.RotateServers = true;
            }
            else if (option == "tcp" || option == "use-vc" ||
                option == "usevc")
            {
                configuration.UseTcp = true;
            }
            else if (option == "trust-ad")
            {
                configuration.TrustAuthenticData = true;
            }
            else if (option == "edns0")
            {
                configuration.UseEdns = true;
            }

            index = index + 1;
        }
    }

    static int ReadBoundedOption(
        string text,
        int minimum,
        int maximum,
        string option,
        int lineIndex)
    {
        if (text.Length == 0)
        {
            throw LineFailure(
                lineIndex,
                $"Resolver option {option} requires an integer value.");
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw LineFailure(
                    lineIndex,
                    $"Resolver option {option} requires an integer value.");
            }

            int digit = character - '0';
            if (value < maximum)
            {
                if (value > (maximum - digit) / 10)
                {
                    value = maximum;
                }
                else
                {
                    value = (value * 10) + digit;
                    if (value > maximum)
                    {
                        value = maximum;
                    }
                }
            }

            index = index + 1;
        }

        if (value < minimum)
        {
            value = minimum;
        }

        return value;
    }

    static DnsEndpoint ParseNumericEndpoint(string text, int lineIndex)
    {
        UriHostNameType hostType = Uri.CheckHostName(text);
        if (hostType != UriHostNameType.IPv4 &&
            hostType != UriHostNameType.IPv6)
        {
            throw LineFailure(
                lineIndex,
                "A nameserver must be a numeric IPv4 or IPv6 address.");
        }

        IPAddress address = IPAddress.Parse(text);
        DnsEndpoint endpoint = new DnsEndpoint();
        endpoint.HasAddress = true;
        endpoint.Address = address.GetAddressBytes();
        endpoint.Port = DnsPorts.Dns;
        if (hostType == UriHostNameType.IPv4)
        {
            endpoint.AddressFamily = 4;
        }
        else
        {
            endpoint.AddressFamily = 6;
        }

        return endpoint;
    }

    static DnsName ParseSearchDomain(string text, int lineIndex)
    {
        string absoluteText = text;
        if (!absoluteText.EndsWith(".", StringComparison.Ordinal))
        {
            absoluteText = absoluteText + ".";
        }

        DnsTextResult parsed = PresentationDnsName.ParseName(
            absoluteText,
            null);
        if (!parsed.Success || parsed.Name == null)
        {
            throw LineFailure(
                lineIndex,
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    parsed.Diagnostics,
                    "The search domain is invalid."));
        }

        return RecordSetOperations.CopyName(parsed.Name, true);
    }

    static DnsResolverServer CopyServer(
        DnsResolverServer server,
        int serverIndex)
    {
        if (server == null || server.Endpoint == null)
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} is incomplete.");
        }

        int transportValue = (int)server.Transport;
        if (transportValue < (int)DnsTransportKind.Udp ||
            transportValue > (int)DnsTransportKind.Quic)
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} has an invalid transport.");
        }

        if (server.Priority < 0)
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} has a negative priority.");
        }

        DnsResolverServer result = new DnsResolverServer();
        result.Endpoint = CopyEndpoint(server.Endpoint, serverIndex);
        result.Transport = server.Transport;
        result.AuthenticationName = server.AuthenticationName;
        result.Enabled = server.Enabled;
        result.Priority = server.Priority;
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint endpoint, int serverIndex)
    {
        if (endpoint.Port < 1 || endpoint.Port > 65535)
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} has an invalid port.");
        }

        DnsEndpoint result = new DnsEndpoint();
        result.Port = endpoint.Port;
        result.HasAddress = endpoint.HasAddress;
        if (endpoint.HasAddress)
        {
            if (endpoint.Address == null ||
                (endpoint.AddressFamily == 4 && endpoint.Address.Length != 4) ||
                (endpoint.AddressFamily == 6 && endpoint.Address.Length != 16) ||
                (endpoint.AddressFamily != 4 && endpoint.AddressFamily != 6))
            {
                throw new DnsResolutionException(
                    $"Resolver server {serverIndex} has an invalid numeric address.");
            }

            result.AddressFamily = endpoint.AddressFamily;
            result.Address = RecordSetOperations.CopyBytes(endpoint.Address);
            return result;
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} requires a host or address.");
        }

        string host = endpoint.Host.Trim();
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            throw new DnsResolutionException(
                $"Resolver server {serverIndex} has an invalid host.");
        }

        result.Host = host.ToLowerInvariant();
        return result;
    }

    static DnsName CopySearchDomain(DnsName searchDomain)
    {
        RawDnsNameResult validated = RawDnsName.ValidateName(searchDomain);
        if (!validated.Success || validated.Name == null)
        {
            throw new DnsResolutionException(
                "A resolver search domain is invalid.");
        }

        DnsName result = RecordSetOperations.CopyName(validated.Name, true);
        result.IsAbsolute = true;
        return result;
    }

    static DnsResolverServer CreateLoopbackServer()
    {
        DnsResolverServer result = new DnsResolverServer();
        result.Endpoint.HasAddress = true;
        result.Endpoint.Address = [127, 0, 0, 1];
        result.Endpoint.AddressFamily = 4;
        result.Endpoint.Port = DnsPorts.Dns;
        return result;
    }

    static DnsResolutionException LineFailure(int lineIndex, string reason)
    {
        return new DnsResolutionException(
            $"Resolver configuration line {lineIndex + 1}: {reason}");
    }
}
