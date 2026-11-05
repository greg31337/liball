using System;
using System.Globalization;

namespace websocket;

internal class WebSocketOriginParseResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal bool Opaque;
    internal string Scheme = "";
    internal string Host = "";
    internal bool HostIsIpv4;
    internal bool HostIsIpv6;
    internal int Port;
    internal string SerializedOrigin = "";
}

internal static class WebSocketOriginParser
{
    const string HttpScheme = "http";
    const string HttpsScheme = "https";

    internal static WebSocketOriginParseResult ParseOrigin(string originText)
    {
        WebSocketOriginParseResult result = new WebSocketOriginParseResult();
        if (originText == null || originText.Length == 0)
        {
            return result;
        }

        if (originText == "null")
        {
            result.Status = WebSocketParseStatus.Complete;
            result.Opaque = true;
            result.SerializedOrigin = "null";
            return result;
        }

        int schemeEnd = FindCharacter(originText, ':', 0);
        if (schemeEnd <= 0 ||
            schemeEnd > originText.Length - 3 ||
            originText[schemeEnd + 1] != '/' ||
            originText[schemeEnd + 2] != '/')
        {
            return result;
        }

        bool secure;
        if (AsciiToken.EqualsIgnoreCase(originText.AsSpan(0, schemeEnd), HttpScheme))
        {
            secure = false;
        }
        else if (AsciiToken.EqualsIgnoreCase(originText.AsSpan(0, schemeEnd), HttpsScheme))
        {
            secure = true;
        }
        else
        {
            return result;
        }

        int authorityStart = schemeEnd + 3;
        if (authorityStart == originText.Length ||
            FindAuthorityTerminator(originText, authorityStart) >= 0)
        {
            return result;
        }

        string webSocketUri = secure
            ? WebSocketProtocolConstants.SecureWebSocketScheme + originText.Substring(schemeEnd)
            : WebSocketProtocolConstants.WebSocketScheme + originText.Substring(schemeEnd);
        WebSocketUriValidationResult uri = WebSocketUriValidator.Validate(webSocketUri);
        if (!uri.Valid)
        {
            return result;
        }

        string authorityHost = uri.Host;
        if (uri.HostIsIpv6)
        {
            authorityHost = "[" + authorityHost + "]";
        }

        string scheme = secure ? HttpsScheme : HttpScheme;
        string serializedOrigin = scheme + "://" + authorityHost;
        int defaultPort = secure ? 443 : 80;
        if (uri.Port != defaultPort)
        {
            serializedOrigin = serializedOrigin + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Scheme = scheme;
        result.Host = uri.Host;
        result.HostIsIpv4 = uri.HostIsIpv4;
        result.HostIsIpv6 = uri.HostIsIpv6;
        result.Port = uri.Port;
        result.SerializedOrigin = serializedOrigin;
        return result;
    }

    internal static WebSocketOriginParseResult ParseAuthority(string scheme, string authority)
    {
        if (scheme == null || authority == null)
        {
            return new WebSocketOriginParseResult();
        }

        return ParseOrigin(scheme + "://" + authority);
    }

    internal static bool AreSame(
        WebSocketOriginParseResult left,
        WebSocketOriginParseResult right)
    {
        if (left == null ||
            right == null ||
            left.Status != WebSocketParseStatus.Complete ||
            right.Status != WebSocketParseStatus.Complete ||
            left.Opaque ||
            right.Opaque ||
            left.Port != right.Port ||
            !EqualsCaseSensitive(left.Scheme, right.Scheme) ||
            !EqualsCaseSensitive(left.Host, right.Host))
        {
            return false;
        }

        return true;
    }

    static int FindCharacter(string value, char character, int offset)
    {
        int index = offset;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindAuthorityTerminator(string value, int offset)
    {
        int index = offset;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '/' || character == '?' || character == '#')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool EqualsCaseSensitive(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
