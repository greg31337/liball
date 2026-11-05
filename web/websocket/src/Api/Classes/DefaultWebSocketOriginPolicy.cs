using System;

namespace websocket;

/// <summary>
/// Allows same-origin WebSocket requests and configured exact origin tuples.
/// </summary>
public class DefaultWebSocketOriginPolicy : IWebSocketOriginPolicy
{
    internal string[] AllowedOrigins;

    /// <summary>
    /// Initializes the policy with additional exact origins allowed alongside same-origin requests.
    /// </summary>
    /// <param name="allowedOrigins">Origins to allow after scheme, host, and port normalization.</param>
    /// <exception cref="WebSocketHandshakeException">Thrown when an allow-list entry is invalid, opaque, or duplicated.</exception>
    public DefaultWebSocketOriginPolicy(string[] allowedOrigins)
    {
        if (allowedOrigins == null)
        {
            throw new WebSocketHandshakeException();
        }

        string[] normalizedOrigins = new string[allowedOrigins.Length];
        int originIndex = 0;
        while (originIndex < allowedOrigins.Length)
        {
            string allowedOrigin = allowedOrigins[originIndex];
            WebSocketOriginParseResult parsed = WebSocketOriginParser.ParseOrigin(allowedOrigin);
            if (parsed.Status != WebSocketParseStatus.Complete || parsed.Opaque)
            {
                throw new WebSocketHandshakeException();
            }

            int comparisonIndex = 0;
            while (comparisonIndex < originIndex)
            {
                if (normalizedOrigins[comparisonIndex] == parsed.SerializedOrigin)
                {
                    throw new WebSocketHandshakeException();
                }

                comparisonIndex = comparisonIndex + 1;
            }

            normalizedOrigins[originIndex] = parsed.SerializedOrigin;
            originIndex = originIndex + 1;
        }

        AllowedOrigins = normalizedOrigins;
    }

    /// <summary>
    /// Determines whether an absent, same-origin, or explicitly allowed Origin is acceptable.
    /// </summary>
    /// <param name="request">The validated HTTP request metadata.</param>
    /// <returns><c>true</c> when the request Origin is allowed; otherwise <c>false</c>.</returns>
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        if (request == null || request.Fields == null)
        {
            return false;
        }

        HeaderFieldLookupResult origins = HeaderFieldLookup.FindFields(
            request.Fields,
            WebSocketProtocolConstants.OriginHeaderName);
        if (origins.Status != WebSocketParseStatus.Complete)
        {
            return false;
        }

        if (origins.Matches.Length == 0)
        {
            return true;
        }

        if (origins.Matches.Length != 1)
        {
            return false;
        }

        WebSocketOriginParseResult requestOrigin = WebSocketOriginParser.ParseOrigin(
            origins.Matches[0].Value);
        if (requestOrigin.Status != WebSocketParseStatus.Complete || requestOrigin.Opaque)
        {
            return false;
        }

        WebSocketOriginParseResult targetOrigin = ReadTargetOrigin(request, requestOrigin.Scheme);
        if (targetOrigin.Status != WebSocketParseStatus.Complete || targetOrigin.Opaque)
        {
            return false;
        }

        if (WebSocketOriginParser.AreSame(requestOrigin, targetOrigin))
        {
            return true;
        }

        int allowedIndex = 0;
        while (allowedIndex < AllowedOrigins.Length)
        {
            if (AllowedOrigins[allowedIndex] == requestOrigin.SerializedOrigin)
            {
                return true;
            }

            allowedIndex = allowedIndex + 1;
        }

        return false;
    }

    static WebSocketOriginParseResult ReadTargetOrigin(
        WebSocketHttpRequest request,
        string http1Scheme)
    {
        if (request.Transport == WebSocketHandshakeTransport.Http1Upgrade)
        {
            HeaderFieldLookupResult hosts = HeaderFieldLookup.FindFields(
                request.Fields,
                WebSocketProtocolConstants.HostHeaderName);
            if (hosts.Status != WebSocketParseStatus.Complete || hosts.Matches.Length != 1)
            {
                return new WebSocketOriginParseResult();
            }

            return WebSocketOriginParser.ParseAuthority(http1Scheme, hosts.Matches[0].Value);
        }

        if (request.Transport == WebSocketHandshakeTransport.Http2ExtendedConnect ||
            request.Transport == WebSocketHandshakeTransport.Http3ExtendedConnect)
        {
            if (!request.HasScheme ||
                !request.HasAuthority ||
                request.Scheme == null ||
                request.Authority == null)
            {
                return new WebSocketOriginParseResult();
            }

            return WebSocketOriginParser.ParseAuthority(request.Scheme, request.Authority);
        }

        return new WebSocketOriginParseResult();
    }
}
