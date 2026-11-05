using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class SessionRedirectHandler
{
    internal HttpClient Client;
    internal IJmapAuthorizationProvider? AuthorizationProvider;
    internal HttpResponseSender Sender = new HttpResponseSender();

    internal SessionRedirectHandler(HttpClient client, IJmapAuthorizationProvider? authorizationProvider)
    {
        if (client == null)
        {
            throw Validation("An HTTP client is required for Session retrieval.", "httpClient");
        }

        Client = client;
        AuthorizationProvider = authorizationProvider;
    }

    internal Task<HttpResponseMessage> GetSessionResponseAsync(
        string? sessionUrl,
        JmapDiscoveryOptions? options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw Validation("Session discovery options are required.", "discoveryOptions");
        }

        if (options.MaximumRedirects < 0)
        {
            throw Validation("The maximum redirect count cannot be negative.", "maximumRedirects");
        }

        Uri initialUri = RequireInitialUri(sessionUrl, options.AllowInsecureHttp);
        return Task.Run(
            () => FollowRedirects(initialUri, options, cancellationToken),
            cancellationToken);
    }

    internal HttpResponseMessage FollowRedirects(
        Uri initialUri,
        JmapDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        string? authorization = null;
        if (AuthorizationProvider != null)
        {
            authorization = AuthorizationProvider.GetAuthorizationValueAsync(
                initialUri.AbsoluteUri,
                cancellationToken).GetAwaiter().GetResult();
            if (!IsSafeAuthorizationValue(authorization))
            {
                throw Validation("The authorization provider returned an invalid header value.", "authorization");
            }
        }

        Uri currentUri = initialUri;
        bool maySendAuthorization = authorization != null && authorization.Length != 0;
        int redirectsFollowed = 0;
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        visited.Add(CanonicalRequestUri(initialUri));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage response;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, currentUri))
            {
                request.Headers.Accept.ParseAdd(JmapMediaTypes.Json);
                if (maySendAuthorization)
                {
                    request.Headers.Add("Authorization", authorization);
                }

                response = Sender.SendResponseAsync(
                    Client,
                    request,
                    currentUri.AbsoluteUri,
                    cancellationToken).GetAwaiter().GetResult();
            }

            if (!IsRedirectStatus(response.StatusCode))
            {
                return response;
            }

            Uri? location = response.Headers.Location;
            int redirectStatus = (int)response.StatusCode;
            string redirectEndpoint = currentUri.AbsoluteUri;
            response.Dispose();
            if (location == null)
            {
                throw TransportFailure(
                    "The Session redirect response does not contain a Location header.",
                    redirectEndpoint,
                    redirectStatus);
            }

            if (redirectsFollowed >= options.MaximumRedirects)
            {
                throw TransportFailure(
                    "The Session redirect limit was exceeded.",
                    redirectEndpoint,
                    redirectStatus);
            }

            Uri nextUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
            ValidateRedirectUri(currentUri, nextUri, options.AllowInsecureHttp);
            string canonical = CanonicalRequestUri(nextUri);
            if (!visited.Add(canonical))
            {
                throw TransportFailure("The Session redirect chain contains a loop.", nextUri.AbsoluteUri, redirectStatus);
            }

            if (!HasSameOrigin(currentUri, nextUri))
            {
                maySendAuthorization = false;
            }

            currentUri = nextUri;
            redirectsFollowed = redirectsFollowed + 1;
        }
    }

    internal Uri RequireInitialUri(string? sessionUrl, bool allowInsecureHttp)
    {
        if (sessionUrl == null || !Uri.IsWellFormedUriString(sessionUrl, UriKind.Absolute))
        {
            throw Validation("A well-formed absolute Session URL is required.", "sessionUrl");
        }

        Uri uri = new Uri(sessionUrl, UriKind.Absolute);
        ValidateHttpUri(uri, allowInsecureHttp, "The Session URL is not permitted.");
        return uri;
    }

    internal void ValidateRedirectUri(Uri previousUri, Uri nextUri, bool allowInsecureHttp)
    {
        if (!nextUri.IsAbsoluteUri)
        {
            throw TransportFailure("The Session redirect Location is not an absolute or resolvable URI.", previousUri.AbsoluteUri, 0);
        }

        ValidateHttpUri(nextUri, allowInsecureHttp, "The Session redirect Location is not permitted.");
        if (string.Equals(previousUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(nextUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            throw TransportFailure("A Session redirect cannot downgrade HTTPS to HTTP.", previousUri.AbsoluteUri, 0);
        }
    }

    internal void ValidateHttpUri(Uri uri, bool allowInsecureHttp, string message)
    {
        bool isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        bool isAllowedHttp = allowInsecureHttp &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if ((!isHttps && !isAllowedHttp) || uri.Host.Length == 0 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
        {
            string? safeEndpoint = uri.IsAbsoluteUri ? CanonicalRequestUri(uri) : null;
            throw TransportFailure(message, safeEndpoint, 0);
        }
    }

    internal bool HasSameOrigin(Uri first, Uri second)
    {
        return string.Equals(first.Scheme, second.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(first.IdnHost, second.IdnHost, StringComparison.OrdinalIgnoreCase) &&
            first.Port == second.Port;
    }

    internal bool IsRedirectStatus(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.MovedPermanently ||
            statusCode == HttpStatusCode.Found ||
            statusCode == HttpStatusCode.SeeOther ||
            statusCode == HttpStatusCode.TemporaryRedirect ||
            (int)statusCode == 308;
    }

    internal bool IsSafeAuthorizationValue(string? authorization)
    {
        return authorization == null ||
            (authorization.IndexOf('\r') < 0 && authorization.IndexOf('\n') < 0);
    }

    internal string CanonicalRequestUri(Uri uri)
    {
        return uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    }

    internal JmapValidationException Validation(string message, string path)
    {
        List<string> invalid = new List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(JsonElement));
    }

    internal JmapTransportException TransportFailure(string message, string? endpoint, int httpStatus)
    {
        return new JmapTransportException(message, endpoint, httpStatus, httpStatus != 0, false, null, null);
    }
}
