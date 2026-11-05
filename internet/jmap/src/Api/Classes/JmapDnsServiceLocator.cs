using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Locates a JMAP Session URL directly or through the user's <c>_jmap._tcp</c> DNS SRV record.
/// </summary>
public class JmapDnsServiceLocator : IJmapServiceLocator
{
    internal const string ServicePrefix = "_jmap._tcp.";
    internal const string WellKnownPath = "/.well-known/jmap";
    internal IDnsQueryClient Queries;
    internal DnsSrvParser Parser = new DnsSrvParser();
    internal DnsSrvSelector Selector = new DnsSrvSelector();
    internal IdnMapping InternationalDomains = new IdnMapping();
    internal ValueValidator Values = new ValueValidator();

    /// <summary>Creates a service locator that uses DNS servers configured by the operating system.</summary>
    public JmapDnsServiceLocator()
    {
        Queries = new DnsQueryClient();
    }

    /// <summary>Locates the absolute JMAP Session URL from direct or username-domain options.</summary>
    /// <param name="options">The direct URL or username-domain discovery options.</param>
    /// <param name="cancellationToken">A token that can cancel discovery.</param>
    /// <returns>A task containing the direct or discovered Session URL.</returns>
    public Task<string> LocateSessionUrlAsync(JmapDiscoveryOptions options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw Validation("Session discovery options are required.", "discoveryOptions");
        }

        if (options.SessionUrl != null)
        {
            return Task.FromResult(RequireDirectSessionUrl(options));
        }

        if (options.DnsTimeoutMilliseconds <= 0)
        {
            throw Validation("The DNS discovery timeout must be greater than zero.", "dnsTimeoutMilliseconds");
        }

        string domain = RequireUsernameDomain(options.Username);
        string queryName = ServicePrefix + domain;
        Task<byte[]> response = Queries.QuerySrvAsync(queryName, options.DnsTimeoutMilliseconds, cancellationToken);
        return response.ContinueWith(
            task => CreateDiscoveredUrl(task.GetAwaiter().GetResult(), domain, queryName),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    internal string RequireDirectSessionUrl(JmapDiscoveryOptions options)
    {
        string? sessionUrl = options.SessionUrl;
        if (sessionUrl == null || !Uri.IsWellFormedUriString(sessionUrl, UriKind.Absolute))
        {
            throw Validation("A well-formed absolute Session URL is required.", "sessionUrl");
        }

        Uri uri = new Uri(sessionUrl, UriKind.Absolute);
        bool isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        bool isAllowedHttp = options.AllowInsecureHttp &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if ((!isHttps && !isAllowedHttp) || uri.Fragment.Length != 0)
        {
            throw Validation("The Session URL must use HTTPS and must not contain a fragment.", "sessionUrl");
        }

        return sessionUrl;
    }

    internal string RequireUsernameDomain(string? username)
    {
        if (username == null || !Values.IsValidUnicode(username))
        {
            throw Validation("A valid username is required for domain discovery.", "username");
        }

        int separator = username.LastIndexOf('@');
        if (separator <= 0 || separator == username.Length - 1)
        {
            throw Validation("The discovery username must contain a local part and domain.", "username");
        }

        string domain = username.Substring(separator + 1);
        if (domain.EndsWith(".", StringComparison.Ordinal))
        {
            domain = domain.Substring(0, domain.Length - 1);
        }

        if (domain.Length == 0 || domain.Length > 253 ||
            Uri.CheckHostName(domain) != UriHostNameType.Dns)
        {
            throw Validation("The discovery username must contain a DNS domain.", "username");
        }

        string asciiDomain = "";
        int labelStart = 0;
        int labelEnd = 0;
        while (labelEnd <= domain.Length)
        {
            if (labelEnd == domain.Length || domain[labelEnd] == '.')
            {
                string label = domain.Substring(labelStart, labelEnd - labelStart);
                string asciiLabel = InternationalDomains.GetAscii(label).ToLowerInvariant();
                string dotSeparator = asciiDomain.Length == 0 ? "" : ".";
                if (asciiDomain.Length + dotSeparator.Length + asciiLabel.Length > 253)
                {
                    throw Validation("The discovery username domain is not a valid IDNA DNS name.", "username");
                }

                asciiDomain = asciiDomain + dotSeparator + asciiLabel;
                labelStart = labelEnd + 1;
            }

            labelEnd = labelEnd + 1;
        }

        if (Uri.CheckHostName(asciiDomain) != UriHostNameType.Dns)
        {
            throw Validation("The discovery username domain is not a valid IDNA DNS name.", "username");
        }

        return asciiDomain;
    }

    internal string CreateDiscoveredUrl(byte[] responseBytes, string fallbackDomain, string queryName)
    {
        DnsSrvResponse response = Parser.ParseSrvResponse(responseBytes);
        if (response.ResponseCode != 0 && response.ResponseCode != 3)
        {
            throw TransportFailure(
                "The DNS SRV query failed with response code " + response.ResponseCode + ".",
                queryName,
                true);
        }

        if (response.Records.Count == 0)
        {
            return BuildSessionUrl(fallbackDomain, 443);
        }

        if (response.Records.Count == 1 && response.Records[0].Target == ".")
        {
            throw TransportFailure("DNS explicitly reports that JMAP is unavailable for this domain.", queryName, false);
        }

        foreach (DnsSrvRecord available in response.Records)
        {
            if (available.Target == ".")
            {
                throw new JmapProtocolException(
                    "A DNS root target may only appear as the sole SRV record.",
                    "dns",
                    default(JsonElement));
            }
        }

        DnsSrvRecord? selected = Selector.SelectRecord(response.Records);
        if (selected == null || selected.Target == null)
        {
            return BuildSessionUrl(fallbackDomain, 443);
        }

        return BuildSessionUrl(selected.Target, selected.Port);
    }

    internal string BuildSessionUrl(string host, int port)
    {
        if (host.Length == 0 || Uri.CheckHostName(host) != UriHostNameType.Dns || port < 0 || port > 65535)
        {
            throw new JmapProtocolException("A DNS SRV endpoint is invalid.", "dns", default(JsonElement));
        }

        int uriPort = port == 443 ? -1 : port;
        UriBuilder builder = new UriBuilder(Uri.UriSchemeHttps, host, uriPort, WellKnownPath);
        return builder.Uri.AbsoluteUri;
    }

    internal JmapValidationException Validation(string message, string path)
    {
        List<string> invalid = new List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(JsonElement));
    }

    internal JmapTransportException TransportFailure(string message, string? endpoint, bool retryable)
    {
        return new JmapTransportException(message, endpoint, 0, false, retryable, null, null);
    }
}
