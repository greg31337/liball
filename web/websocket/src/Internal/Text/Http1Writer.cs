using System;

namespace websocket;

internal class Http1WriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int RequiredLength;
    internal int ProducedCount;
    internal WebSocketError Error = new WebSocketError();
}

internal class Http1ValidationResult
{
    internal bool Valid;
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketError Error = new WebSocketError();
}

internal static class Http1Writer
{
    const string HttpVersion = "HTTP/1.1";
    const string ContentLengthName = "Content-Length";
    const string TransferEncodingName = "Transfer-Encoding";

    internal static Http1WriteResult MeasureRequest(WebSocketHttpRequest request)
    {
        Http1ValidationResult validation = ValidateRequest(request);
        if (!validation.Valid)
        {
            return CreateValidationFailure(validation);
        }

        int length = 0;
        length = AddLength(length, request.Method.Length);
        length = AddLength(length, 1);
        length = AddLength(length, request.RequestTarget.Length);
        length = AddLength(length, 1);
        length = AddLength(length, request.HttpVersion.Length);
        length = AddLength(length, 2);
        length = MeasureFields(request.Fields, length);
        length = AddLength(length, 2);
        return CreateMeasurement(length);
    }

    internal static Http1WriteResult WriteRequest(
        WebSocketHttpRequest request,
        Span<byte> destination)
    {
        Http1WriteResult result = MeasureRequest(request);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        if (destination.Length < result.RequiredLength)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        int offset = 0;
        offset = WriteText(request.Method, destination, offset);
        destination[offset] = 32;
        offset = offset + 1;
        offset = WriteText(request.RequestTarget, destination, offset);
        destination[offset] = 32;
        offset = offset + 1;
        offset = WriteText(request.HttpVersion, destination, offset);
        offset = WriteLineEnding(destination, offset);
        offset = WriteFields(request.Fields, destination, offset);
        offset = WriteLineEnding(destination, offset);
        result.ProducedCount = offset;
        return result;
    }

    internal static Http1WriteResult MeasureResponse(WebSocketHttpResponse response)
    {
        Http1ValidationResult validation = ValidateResponse(response);
        if (!validation.Valid)
        {
            return CreateValidationFailure(validation);
        }

        int length = 0;
        length = AddLength(length, HttpVersion.Length);
        length = AddLength(length, 1);
        length = AddLength(length, 3);
        length = AddLength(length, 1);
        if (response.HasReasonPhrase)
        {
            length = AddLength(length, response.ReasonPhrase.Length);
        }

        length = AddLength(length, 2);
        length = MeasureFields(response.Fields, length);
        length = AddLength(length, 2);
        if (response.HasBody)
        {
            length = AddLength(length, response.Body.Length);
        }

        return CreateMeasurement(length);
    }

    internal static Http1WriteResult WriteResponse(
        WebSocketHttpResponse response,
        Span<byte> destination)
    {
        Http1WriteResult result = MeasureResponse(response);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        if (destination.Length < result.RequiredLength)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        int offset = 0;
        offset = WriteText(HttpVersion, destination, offset);
        destination[offset] = 32;
        offset = offset + 1;
        destination[offset] = (byte)('0' + response.StatusCode / 100);
        destination[offset + 1] = (byte)('0' + (response.StatusCode / 10) % 10);
        destination[offset + 2] = (byte)('0' + response.StatusCode % 10);
        offset = offset + 3;
        destination[offset] = 32;
        offset = offset + 1;
        if (response.HasReasonPhrase)
        {
            offset = WriteText(response.ReasonPhrase, destination, offset);
        }

        offset = WriteLineEnding(destination, offset);
        offset = WriteFields(response.Fields, destination, offset);
        offset = WriteLineEnding(destination, offset);
        if (response.HasBody)
        {
            response.Body.AsSpan().CopyTo(destination.Slice(offset));
            offset = offset + response.Body.Length;
        }

        result.ProducedCount = offset;
        return result;
    }

    static Http1ValidationResult ValidateRequest(WebSocketHttpRequest request)
    {
        if (request == null ||
            request.Transport != WebSocketHandshakeTransport.Http1Upgrade ||
            request.Method == null ||
            request.RequestTarget == null ||
            request.HttpVersion == null ||
            request.Scheme == null ||
            request.Authority == null ||
            request.Path == null ||
            request.Protocol == null ||
            request.Fields == null ||
            !AsciiToken.IsValid(request.Method) ||
            !request.HasRequestTarget ||
            !IsOriginFormTarget(request.RequestTarget) ||
            !request.HasHttpVersion ||
            !IsHttpVersion(request.HttpVersion) ||
            request.HasScheme || request.Scheme.Length != 0 ||
            request.HasAuthority || request.Authority.Length != 0 ||
            request.HasPath || request.Path.Length != 0 ||
            request.HasProtocol || request.Protocol.Length != 0)
        {
            return CreateInvalid(
                "http-write-request-invalid",
                "The HTTP/1 request metadata is inconsistent or unsafe.");
        }

        return ValidateFields(request.Fields, true, false, 0, 0);
    }

    static Http1ValidationResult ValidateResponse(WebSocketHttpResponse response)
    {
        if (response == null ||
            response.Transport != WebSocketHandshakeTransport.Http1Upgrade ||
            response.StatusCode < 100 ||
            response.StatusCode > 599 ||
            response.ReasonPhrase == null ||
            response.Fields == null ||
            response.Body == null ||
            !IsSafeText(response.ReasonPhrase) ||
            (response.HasReasonPhrase && response.ReasonPhrase.Length == 0) ||
            (!response.HasReasonPhrase && response.ReasonPhrase.Length != 0) ||
            (!response.HasBody && response.Body.Length != 0))
        {
            return CreateInvalid(
                "http-write-response-invalid",
                "The HTTP/1 response metadata is inconsistent or unsafe.");
        }

        bool bodyForbidden =
            response.StatusCode < 200 ||
            response.StatusCode == 204 ||
            response.StatusCode == 304;
        if (bodyForbidden && response.HasBody)
        {
            return CreateContradiction(
                "An HTTP response body is forbidden for the selected status code.");
        }

        return ValidateFields(
            response.Fields,
            false,
            response.HasBody,
            response.Body.Length,
            response.StatusCode);
    }

    static Http1ValidationResult ValidateFields(
        WebSocketHeaderField[] fields,
        bool request,
        bool hasBody,
        int bodyLength,
        int statusCode)
    {
        int hostCount = 0;
        int keyCount = 0;
        int versionCount = 0;
        int acceptCount = 0;
        int protocolCount = 0;
        int originCount = 0;
        int contentLengthCount = 0;
        int contentLength = 0;
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !IsSafeText(field.Value) ||
                HasOuterWhitespace(field.Value))
            {
                return CreateInvalid(
                    "http-write-field-invalid",
                    "An HTTP field name or value is unsafe.");
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, TransferEncodingName))
            {
                return CreateContradiction(
                    "Transfer-Encoding is not permitted in an opening handshake message.");
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, ContentLengthName))
            {
                contentLengthCount = contentLengthCount + 1;
                contentLength = ParseCanonicalLength(field.Value);
                if (contentLength < 0)
                {
                    return CreateContradiction(
                        "Content-Length must be one canonical non-negative decimal value.");
                }
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.HostHeaderName))
            {
                hostCount = hostCount + 1;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.KeyHeaderName))
            {
                keyCount = keyCount + 1;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.VersionHeaderName))
            {
                versionCount = versionCount + 1;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.AcceptHeaderName))
            {
                acceptCount = acceptCount + 1;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.ProtocolHeaderName))
            {
                protocolCount = protocolCount + 1;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.OriginHeaderName))
            {
                originCount = originCount + 1;
            }

            fieldIndex = fieldIndex + 1;
        }

        bool repeatedRequestSingleton = request &&
            (hostCount > 1 ||
             keyCount > 1 ||
             versionCount > 1 ||
             originCount > 1);
        bool repeatedResponseSingleton = !request &&
            (acceptCount > 1 || protocolCount > 1);
        if (contentLengthCount > 1 ||
            repeatedRequestSingleton ||
            repeatedResponseSingleton)
        {
            return CreateContradiction(
                "A protocol-critical singleton HTTP field is repeated.");
        }

        if ((request && acceptCount != 0) ||
            (!request && (hostCount != 0 || keyCount != 0 || originCount != 0)))
        {
            return CreateContradiction(
                "A protocol-critical HTTP field is used in the wrong message direction.");
        }

        if (request)
        {
            if (contentLengthCount != 0 && contentLength != 0)
            {
                return CreateContradiction(
                    "An HTTP opening handshake request cannot declare a body.");
            }
        }
        else if (hasBody)
        {
            if (contentLengthCount != 1 || contentLength != bodyLength)
            {
                return CreateContradiction(
                    "Content-Length does not match the HTTP response body.");
            }
        }
        else if (contentLengthCount != 0 && contentLength != 0)
        {
            return CreateContradiction(
                "Content-Length declares a response body that is not present.");
        }

        if (!request && statusCode == 101 && contentLengthCount != 0 && contentLength != 0)
        {
            return CreateContradiction(
                "A switching-protocols response cannot declare a body.");
        }

        Http1ValidationResult result = new Http1ValidationResult();
        result.Valid = true;
        result.Status = WebSocketParseStatus.Complete;
        return result;
    }

    static int MeasureFields(WebSocketHeaderField[] fields, int initialLength)
    {
        int length = initialLength;
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            length = AddLength(length, fields[fieldIndex].Name.Length);
            length = AddLength(length, 2);
            length = AddLength(length, fields[fieldIndex].Value.Length);
            length = AddLength(length, 2);
            fieldIndex = fieldIndex + 1;
        }

        return length;
    }

    static int WriteFields(
        WebSocketHeaderField[] fields,
        Span<byte> destination,
        int initialOffset)
    {
        int offset = initialOffset;
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            offset = WriteText(field.Name, destination, offset);
            destination[offset] = 58;
            destination[offset + 1] = 32;
            offset = offset + 2;
            offset = WriteText(field.Value, destination, offset);
            offset = WriteLineEnding(destination, offset);
            fieldIndex = fieldIndex + 1;
        }

        return offset;
    }

    static int WriteText(string value, Span<byte> destination, int initialOffset)
    {
        int offset = initialOffset;
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            destination[offset] = (byte)value[valueIndex];
            offset = offset + 1;
            valueIndex = valueIndex + 1;
        }

        return offset;
    }

    static int WriteLineEnding(Span<byte> destination, int initialOffset)
    {
        destination[initialOffset] = 13;
        destination[initialOffset + 1] = 10;
        return initialOffset + 2;
    }

    static int AddLength(int current, int addition)
    {
        if (current < 0 || addition < 0 || current > int.MaxValue - addition)
        {
            return -1;
        }

        return current + addition;
    }

    static int ParseCanonicalLength(ReadOnlySpan<char> value)
    {
        if (value.Length == 0 || (value.Length > 1 && value[0] == '0'))
        {
            return -1;
        }

        int parsed = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (parsed > (int.MaxValue - digit) / 10)
            {
                return -1;
            }

            parsed = parsed * 10 + digit;
            index = index + 1;
        }

        return parsed;
    }

    static bool IsHttpVersion(ReadOnlySpan<char> value)
    {
        return value.Length == 8 &&
            value[0] == 'H' &&
            value[1] == 'T' &&
            value[2] == 'T' &&
            value[3] == 'P' &&
            value[4] == '/' &&
            value[5] >= '0' && value[5] <= '9' &&
            value[6] == '.' &&
            value[7] >= '0' && value[7] <= '9';
    }

    static bool IsOriginFormTarget(ReadOnlySpan<char> value)
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

    static bool IsSafeText(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != '\t' &&
                (current < ' ' || current == (char)127 || current > (char)255))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool HasOuterWhitespace(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        return value[0] == ' ' ||
            value[0] == '\t' ||
            value[value.Length - 1] == ' ' ||
            value[value.Length - 1] == '\t';
    }

    static Http1ValidationResult CreateInvalid(string identifier, string diagnostic)
    {
        Http1ValidationResult result = new Http1ValidationResult();
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    static Http1ValidationResult CreateContradiction(string diagnostic)
    {
        return CreateInvalid("http-write-field-contradictory", diagnostic);
    }

    static Http1WriteResult CreateValidationFailure(Http1ValidationResult validation)
    {
        Http1WriteResult result = new Http1WriteResult();
        result.Status = validation.Status;
        result.Error = validation.Error;
        return result;
    }

    static Http1WriteResult CreateMeasurement(int length)
    {
        Http1WriteResult result = new Http1WriteResult();
        if (length < 0)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            result.Error.Kind = WebSocketErrorKind.Limit;
            result.Error.Identifier = "http-write-too-large";
            result.Error.Diagnostic = "The encoded HTTP message length exceeds the supported limit.";
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.RequiredLength = length;
        return result;
    }
}
