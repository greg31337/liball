using System;
using System.Globalization;

namespace websocket;

internal class WebSocketPortValidationResult
{
    internal bool Valid;
    internal int Port;
}

internal class WebSocketUriValidationResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal bool Valid;
    internal bool Secure;
    internal string Scheme = "";
    internal string Host = "";
    internal bool HostIsIpv4;
    internal bool HostIsIpv6;
    internal bool HasExplicitPort;
    internal int Port;
    internal string Authority = "";
    internal string RequestTarget = "";
    internal string NormalizedUri = "";
    internal WebSocketError Error = new WebSocketError();
}

internal static class WebSocketUriValidator
{
    internal static WebSocketUriValidationResult Validate(string uriText)
    {
        WebSocketUriValidationResult result = new WebSocketUriValidationResult();
        if (uriText == null || uriText.Length == 0 || ContainsUnsafeText(uriText))
        {
            return CreateFailure();
        }

        int schemeEnd = FindCharacter(uriText, ':', 0);
        if (schemeEnd <= 0 ||
            schemeEnd > uriText.Length - 3 ||
            uriText[schemeEnd + 1] != '/' ||
            uriText[schemeEnd + 2] != '/')
        {
            return CreateFailure();
        }

        ReadOnlySpan<char> schemeText = uriText.AsSpan(0, schemeEnd);
        bool secure = false;
        if (AsciiToken.EqualsIgnoreCase(schemeText, WebSocketProtocolConstants.WebSocketScheme))
        {
            secure = false;
        }
        else if (AsciiToken.EqualsIgnoreCase(
            schemeText,
            WebSocketProtocolConstants.SecureWebSocketScheme))
        {
            secure = true;
        }
        else
        {
            return CreateFailure();
        }

        if (FindCharacter(uriText, '#', schemeEnd + 3) >= 0)
        {
            return CreateFailure();
        }

        int authorityStart = schemeEnd + 3;
        int authorityEnd = FindAuthorityEnd(uriText, authorityStart);
        if (authorityEnd == authorityStart ||
            FindCharacterInRange(uriText, '@', authorityStart, authorityEnd) >= 0)
        {
            return CreateFailure();
        }

        ReadOnlySpan<char> rawHost;
        bool bracketedIpv6 = false;
        bool explicitPort = false;
        WebSocketPortValidationResult portResult = new WebSocketPortValidationResult();
        if (uriText[authorityStart] == '[')
        {
            int bracketEnd = FindCharacterInRange(
                uriText,
                ']',
                authorityStart + 1,
                authorityEnd);
            if (bracketEnd < 0 || bracketEnd == authorityStart + 1)
            {
                return CreateFailure();
            }

            rawHost = uriText.AsSpan(
                authorityStart + 1,
                bracketEnd - authorityStart - 1);
            bracketedIpv6 = true;
            if (bracketEnd + 1 < authorityEnd)
            {
                if (uriText[bracketEnd + 1] != ':')
                {
                    return CreateFailure();
                }

                explicitPort = true;
                portResult = ParsePort(uriText.AsSpan(bracketEnd + 2, authorityEnd - bracketEnd - 2));
            }
            else if (bracketEnd + 1 != authorityEnd)
            {
                return CreateFailure();
            }
        }
        else
        {
            int colonIndex = FindCharacterInRange(
                uriText,
                ':',
                authorityStart,
                authorityEnd);
            if (colonIndex >= 0)
            {
                rawHost = uriText.AsSpan(authorityStart, colonIndex - authorityStart);
                explicitPort = true;
                portResult = ParsePort(uriText.AsSpan(colonIndex + 1, authorityEnd - colonIndex - 1));
            }
            else
            {
                rawHost = uriText.AsSpan(authorityStart, authorityEnd - authorityStart);
            }
        }

        if (rawHost.Length == 0 || ContainsPercent(rawHost))
        {
            return CreateFailure();
        }

        if (explicitPort && !portResult.Valid)
        {
            return CreateFailure();
        }

        if (!Uri.IsWellFormedUriString(uriText, UriKind.Absolute))
        {
            return CreateFailure();
        }

        Uri uri = new Uri(uriText, UriKind.Absolute);
        if ((!secure && !AsciiToken.EqualsIgnoreCase(
                uri.Scheme,
                WebSocketProtocolConstants.WebSocketScheme)) ||
            (secure && !AsciiToken.EqualsIgnoreCase(
                uri.Scheme,
                WebSocketProtocolConstants.SecureWebSocketScheme)) ||
            uri.Host.Length == 0 ||
            uri.UserInfo.Length != 0 ||
            uri.Fragment.Length != 0)
        {
            return CreateFailure();
        }

        string asciiHost = uri.IdnHost.ToLowerInvariant();
        UriHostNameType hostType = Uri.CheckHostName(asciiHost);
        bool ipv4 = hostType == UriHostNameType.IPv4;
        bool ipv6 = hostType == UriHostNameType.IPv6;
        if (bracketedIpv6)
        {
            if (!ipv6)
            {
                return CreateFailure();
            }
        }
        else if (ipv6)
        {
            return CreateFailure();
        }
        else if (ContainsOnlyDigitsAndDots(rawHost))
        {
            if (!IsStrictIpv4(rawHost) || !ipv4)
            {
                return CreateFailure();
            }
        }
        else if (!ipv4 && !IsDnsHost(asciiHost))
        {
            return CreateFailure();
        }

        int port = secure ? 443 : 80;
        if (explicitPort)
        {
            port = portResult.Port;
            if (uri.Port != port)
            {
                return CreateFailure();
            }
        }

        string authorityHost = asciiHost;
        if (ipv6)
        {
            authorityHost = "[" + asciiHost + "]";
        }

        string authority = authorityHost;
        if (explicitPort)
        {
            authority = authority + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        string requestTarget = uri.PathAndQuery;
        if (requestTarget.Length == 0 || requestTarget[0] != '/')
        {
            requestTarget = "/" + requestTarget;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Valid = true;
        result.Secure = secure;
        result.Scheme = secure
            ? WebSocketProtocolConstants.SecureWebSocketScheme
            : WebSocketProtocolConstants.WebSocketScheme;
        result.Host = asciiHost;
        result.HostIsIpv4 = ipv4;
        result.HostIsIpv6 = ipv6;
        result.HasExplicitPort = explicitPort;
        result.Port = port;
        result.Authority = authority;
        result.RequestTarget = requestTarget;
        result.NormalizedUri = result.Scheme + "://" + authority + requestTarget;
        return result;
    }

    static WebSocketUriValidationResult CreateFailure()
    {
        WebSocketUriValidationResult result = new WebSocketUriValidationResult();
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = "websocket-uri-invalid";
        result.Error.Diagnostic = "The WebSocket URI is invalid or unsafe.";
        return result;
    }

    static WebSocketPortValidationResult ParsePort(ReadOnlySpan<char> value)
    {
        WebSocketPortValidationResult result = new WebSocketPortValidationResult();
        if (value.Length == 0)
        {
            return result;
        }

        int port = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return result;
            }

            int digit = character - '0';
            if (port > (65535 - digit) / 10)
            {
                return result;
            }

            port = port * 10 + digit;
            index = index + 1;
        }

        if (port == 0)
        {
            return result;
        }

        result.Valid = true;
        result.Port = port;
        return result;
    }

    static bool IsStrictIpv4(ReadOnlySpan<char> value)
    {
        int componentCount = 0;
        int componentStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '.')
            {
                int componentLength = index - componentStart;
                if (componentLength == 0 ||
                    componentLength > 3 ||
                    (componentLength > 1 && value[componentStart] == '0'))
                {
                    return false;
                }

                int component = 0;
                int componentIndex = componentStart;
                while (componentIndex < index)
                {
                    char character = value[componentIndex];
                    if (character < '0' || character > '9')
                    {
                        return false;
                    }

                    component = component * 10 + character - '0';
                    componentIndex = componentIndex + 1;
                }

                if (component > 255)
                {
                    return false;
                }

                componentCount = componentCount + 1;
                componentStart = index + 1;
            }

            index = index + 1;
        }

        return componentCount == 4;
    }

    static bool IsDnsHost(ReadOnlySpan<char> value)
    {
        if (value.Length == 0 || value.Length > 254)
        {
            return false;
        }

        int effectiveLength = value.Length;
        if (value[value.Length - 1] == '.')
        {
            effectiveLength = effectiveLength - 1;
        }

        if (effectiveLength == 0 || effectiveLength > 253)
        {
            return false;
        }

        int labelStart = 0;
        int index = 0;
        while (index <= effectiveLength)
        {
            if (index == effectiveLength || value[index] == '.')
            {
                int labelLength = index - labelStart;
                if (labelLength == 0 ||
                    labelLength > 63 ||
                    value[labelStart] == '-' ||
                    value[index - 1] == '-')
                {
                    return false;
                }

                labelStart = index + 1;
            }
            else
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'z') ||
                      character == '-'))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsOnlyDigitsAndDots(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if ((character < '0' || character > '9') && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsPercent(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsUnsafeText(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= ' ' || character == (char)127)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int FindAuthorityEnd(string value, int offset)
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

        return value.Length;
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

    static int FindCharacterInRange(
        string value,
        char character,
        int offset,
        int end)
    {
        int index = offset;
        while (index < end)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
