using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class SessionDiscovery
{
    internal IJmapTransport Transport;

    internal SessionDiscovery(IJmapTransport transport)
    {
        if (transport == null)
        {
            throw Validation("A JMAP transport is required.", "transport");
        }

        Transport = transport;
    }

    internal Task<JmapSession> RetrieveDirectSessionAsync(JmapDiscoveryOptions? options, CancellationToken cancellationToken)
    {
        string sessionUrl = RequireDirectSessionUrl(options);
        return Transport.GetTransportSessionAsync(sessionUrl, cancellationToken);
    }

    internal string RequireDirectSessionUrl(JmapDiscoveryOptions? options)
    {
        if (options == null)
        {
            throw Validation("Session discovery options are required.", "discoveryOptions");
        }

        string? sessionUrl = options.SessionUrl;
        if (sessionUrl == null || !Uri.IsWellFormedUriString(sessionUrl, UriKind.Absolute))
        {
            throw Validation("A well-formed absolute Session URL is required for direct discovery.", "sessionUrl");
        }

        Uri uri = new Uri(sessionUrl, UriKind.Absolute);
        bool isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        bool isAllowedHttp = options.AllowInsecureHttp &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if ((!isHttps && !isAllowedHttp) || uri.Fragment.Length != 0)
        {
            throw Validation("The direct Session URL must use HTTPS and must not contain a fragment.", "sessionUrl");
        }

        return sessionUrl;
    }

    internal JmapValidationException Validation(string message, string path)
    {
        List<string> invalid = new List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(JsonElement));
    }
}
