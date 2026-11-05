using System;

namespace websocket;

internal class ExtendedConnectValidationResult
{
    internal bool Valid;
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketError Error = new WebSocketError();
}

internal static class ExtendedConnectValidator
{
    const string HttpScheme = "http";
    const string HttpsScheme = "https";
    const string ProxyConnectionFieldName = "Proxy-Connection";
    const string KeepAliveFieldName = "Keep-Alive";
    const string TransferEncodingFieldName = "Transfer-Encoding";
    const string TeFieldName = "TE";
    const string TrailersFieldValue = "trailers";

    internal static ExtendedConnectValidationResult ValidateRequest(
        WebSocketHttpRequest? request,
        WebSocketHandshakeTransport expectedTransport)
    {
        if (!IsExtendedTransport(expectedTransport) ||
            request == null ||
            request.Transport != expectedTransport)
        {
            return CreateFailure(
                "extended-connect-transport-invalid",
                "The request transport is not the expected Extended CONNECT transport.");
        }

        if (request.Method == null ||
            request.Method != WebSocketProtocolConstants.ExtendedConnectMethod)
        {
            return CreateFailure(
                "extended-connect-method-invalid",
                "The Extended CONNECT request method is invalid.");
        }

        if (request.HasRequestTarget ||
            request.RequestTarget == null ||
            request.RequestTarget.Length != 0 ||
            request.HasHttpVersion ||
            request.HttpVersion == null ||
            request.HttpVersion.Length != 0)
        {
            return CreateFailure(
                "extended-connect-http1-metadata-invalid",
                "HTTP/1 request-target and version metadata is forbidden for Extended CONNECT.");
        }

        if (!request.HasScheme ||
            request.Scheme == null ||
            (request.Scheme != HttpScheme && request.Scheme != HttpsScheme))
        {
            return CreateFailure(
                "extended-connect-scheme-invalid",
                "The Extended CONNECT scheme must be http or https.");
        }

        if (!request.HasAuthority ||
            request.Authority == null ||
            WebSocketOriginParser.ParseAuthority(request.Scheme, request.Authority).Status !=
                WebSocketParseStatus.Complete)
        {
            return CreateFailure(
                "extended-connect-authority-invalid",
                "The Extended CONNECT authority is missing or invalid.");
        }

        if (!request.HasPath ||
            request.Path == null ||
            !IsOriginFormPath(request.Path))
        {
            return CreateFailure(
                "extended-connect-path-invalid",
                "The Extended CONNECT path is missing or invalid.");
        }

        if (!request.HasProtocol ||
            request.Protocol == null ||
            request.Protocol != WebSocketProtocolConstants.ExtendedConnectProtocol)
        {
            return CreateFailure(
                "extended-connect-protocol-invalid",
                "The Extended CONNECT protocol is not websocket.");
        }

        return ValidateRegularFields(request.Fields);
    }

    internal static ExtendedConnectValidationResult ValidateResponse(
        WebSocketHttpResponse? response,
        WebSocketHandshakeTransport expectedTransport)
    {
        if (!IsExtendedTransport(expectedTransport) ||
            response == null ||
            response.Transport != expectedTransport)
        {
            return CreateFailure(
                "extended-connect-response-transport-invalid",
                "The response transport is not the expected Extended CONNECT transport.");
        }

        if (response.StatusCode < 100 ||
            response.StatusCode > 599 ||
            response.HasReasonPhrase ||
            response.ReasonPhrase == null ||
            response.ReasonPhrase.Length != 0 ||
            response.Body == null ||
            (!response.HasBody && response.Body.Length != 0) ||
            (response.StatusCode < 200 && response.HasBody) ||
            (response.StatusCode >= 200 &&
                response.StatusCode < 300 &&
                response.HasBody))
        {
            return CreateFailure(
                "extended-connect-response-metadata-invalid",
                "The Extended CONNECT response control metadata is invalid.");
        }

        return ValidateRegularFields(response.Fields);
    }

    static ExtendedConnectValidationResult ValidateRegularFields(
        WebSocketHeaderField[]? fields)
    {
        if (fields == null)
        {
            return CreateFailure(
                "extended-connect-fields-invalid",
                "The Extended CONNECT regular field collection is invalid.");
        }

        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value) ||
                HttpFieldValue.TrimOptionalWhitespace(field.Value) != field.Value)
            {
                return CreateFailure(
                    "extended-connect-field-invalid",
                    "An Extended CONNECT regular field name or value is invalid.");
            }

            if (IsForbiddenField(field.Name))
            {
                return CreateFailure(
                    "extended-connect-field-forbidden",
                    "An HTTP/1 or connection-specific field is forbidden for Extended CONNECT.");
            }

            if (!IsLowercaseName(field.Name))
            {
                return CreateFailure(
                    "extended-connect-field-name-not-lowercase",
                    "Extended CONNECT regular field names must be lowercase.");
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, TeFieldName) &&
                !AsciiToken.EqualsIgnoreCase(field.Value, TrailersFieldValue))
            {
                return CreateFailure(
                    "extended-connect-te-invalid",
                    "The TE field can contain only trailers in Extended CONNECT.");
            }

            fieldIndex = fieldIndex + 1;
        }

        ExtendedConnectValidationResult result = new ExtendedConnectValidationResult();
        result.Valid = true;
        result.Status = WebSocketParseStatus.Complete;
        return result;
    }

    static bool IsExtendedTransport(WebSocketHandshakeTransport transport)
    {
        return transport == WebSocketHandshakeTransport.Http2ExtendedConnect ||
            transport == WebSocketHandshakeTransport.Http3ExtendedConnect;
    }

    static bool IsForbiddenField(string name)
    {
        return AsciiToken.EqualsIgnoreCase(
                name,
                WebSocketProtocolConstants.ConnectionHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, ProxyConnectionFieldName) ||
            AsciiToken.EqualsIgnoreCase(name, KeepAliveFieldName) ||
            AsciiToken.EqualsIgnoreCase(name, TransferEncodingFieldName) ||
            AsciiToken.EqualsIgnoreCase(
                name,
                WebSocketProtocolConstants.UpgradeHeaderName) ||
            AsciiToken.EqualsIgnoreCase(
                name,
                WebSocketProtocolConstants.HostHeaderName) ||
            AsciiToken.EqualsIgnoreCase(
                name,
                WebSocketProtocolConstants.KeyHeaderName) ||
            AsciiToken.EqualsIgnoreCase(
                name,
                WebSocketProtocolConstants.AcceptHeaderName);
    }

    static bool IsLowercaseName(string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            if (name[index] >= 'A' && name[index] <= 'Z')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsOriginFormPath(ReadOnlySpan<char> value)
    {
        if (value.Length == 0 || value[0] != '/')
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '?' || current == '/')
            {
                index = index + 1;
            }
            else if (current == '%')
            {
                if (index > value.Length - 3 ||
                    !IsHexDigit(value[index + 1]) ||
                    !IsHexDigit(value[index + 2]))
                {
                    return false;
                }

                index = index + 3;
            }
            else if (IsPathCharacter(current))
            {
                index = index + 1;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    static bool IsPathCharacter(char value)
    {
        if ((value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'Z') ||
            (value >= 'a' && value <= 'z'))
        {
            return true;
        }

        return value == '!' ||
            value == '$' ||
            value == '&' ||
            value == '\'' ||
            value == '(' ||
            value == ')' ||
            value == '*' ||
            value == '+' ||
            value == ',' ||
            value == '-' ||
            value == '.' ||
            value == ':' ||
            value == ';' ||
            value == '=' ||
            value == '@' ||
            value == '_' ||
            value == '~';
    }

    static bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'F') ||
            (value >= 'a' && value <= 'f');
    }

    static ExtendedConnectValidationResult CreateFailure(
        string identifier,
        string diagnostic)
    {
        ExtendedConnectValidationResult result = new ExtendedConnectValidationResult();
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }
}
