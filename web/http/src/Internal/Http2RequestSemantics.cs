using System.Text;

namespace http;

internal static class Http2RequestSemantics
{
    internal static Http2MessageResult Decode(
        int streamId,
        HttpHeaders headers,
        bool endStream,
        bool extendedConnectEnabled,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateResult(streamId, headers, endStream);
        if (headers == null || headers.Fields == null)
        {
            return Reject(
                result,
                "invalid-http2-request-fields",
                "An HTTP/2 request requires a decoded field section.");
        }

        Http2FieldValidationResult fieldValidation =
            Http2FieldSemantics.ValidateFields(headers, true, limits);
        if (!fieldValidation.IsValid)
        {
            return Reject(
                result,
                fieldValidation.ErrorCodeName,
                fieldValidation.ErrorMessage);
        }

        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        bool regularSeen = false;
        bool methodSeen = false;
        bool schemeSeen = false;
        bool authoritySeen = false;
        bool pathSeen = false;
        bool protocolSeen = false;
        string method = "";
        string scheme = "";
        string authority = "";
        string path = "";
        string protocol = "";
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                return Reject(
                    result,
                    "invalid-http2-request-field",
                    "An HTTP/2 request contains an incomplete field.");
            }

            bool pseudo = field.Name.Length != 0 && field.Name[0] == ':';
            if (!pseudo)
            {
                regularSeen = true;
                request.Headers.Fields.Add(CopyField(field));
                index = index + 1;
                continue;
            }

            if (regularSeen)
            {
                return Reject(
                    result,
                    "http2-request-pseudo-header-order",
                    "Request pseudo-header fields must precede regular fields.");
            }

            if (!Ascii.IsValid(field.Value))
            {
                return Reject(
                    result,
                    "invalid-http2-request-pseudo-header-value",
                    "Request pseudo-header values must contain only ASCII octets.");
            }

            string value = Encoding.ASCII.GetString(field.Value);
            if (field.Name == ":method")
            {
                if (methodSeen)
                {
                    return RejectDuplicate(result, field.Name);
                }

                methodSeen = true;
                method = value;
            }
            else if (field.Name == ":scheme")
            {
                if (schemeSeen)
                {
                    return RejectDuplicate(result, field.Name);
                }

                schemeSeen = true;
                scheme = value;
            }
            else if (field.Name == ":authority")
            {
                if (authoritySeen)
                {
                    return RejectDuplicate(result, field.Name);
                }

                authoritySeen = true;
                authority = value;
            }
            else if (field.Name == ":path")
            {
                if (pathSeen)
                {
                    return RejectDuplicate(result, field.Name);
                }

                pathSeen = true;
                path = value;
            }
            else if (field.Name == ":protocol")
            {
                if (protocolSeen)
                {
                    return RejectDuplicate(result, field.Name);
                }

                protocolSeen = true;
                protocol = value;
            }
            else
            {
                return Reject(
                    result,
                    "invalid-http2-request-pseudo-header",
                    "The request contains a response, undefined, or unnegotiated pseudo-header field.");
            }

            index = index + 1;
        }

        Http2CookieSemantics.CombineDecodedCookies(request.Headers);

        if (!methodSeen || !IsValidMethod(method))
        {
            return Reject(
                result,
                "invalid-http2-request-method",
                "An HTTP/2 request requires exactly one valid :method value.");
        }

        if (method == HttpMethods.Connect)
        {
            if (protocolSeen)
            {
                return DecodeExtendedConnect(
                    result, request, method, schemeSeen, authoritySeen,
                    pathSeen, scheme, authority, path, protocol,
                    extendedConnectEnabled, endStream);
            }

            return DecodeConnect(
                result, request, method, schemeSeen, authoritySeen,
                pathSeen, authority, endStream);
        }

        if (protocolSeen)
        {
            return Reject(
                result,
                "http2-protocol-requires-connect",
                "The :protocol pseudo-header field is valid only on CONNECT requests.");
        }

        if (!schemeSeen || !IsValidScheme(scheme))
        {
            return Reject(
                result,
                "invalid-http2-request-scheme",
                "A non-CONNECT HTTP/2 request requires exactly one valid :scheme value.");
        }

        if (!pathSeen || !IsValidPath(method, path))
        {
            return Reject(
                result,
                "invalid-http2-request-path",
                "A non-CONNECT HTTP/2 request requires a valid :path value.");
        }

        if (authoritySeen && !IsValidAuthority(authority, scheme, false))
        {
            return Reject(
                result,
                "invalid-http2-request-authority",
                "The request :authority value is malformed or unsafe.");
        }

        Http2FieldValidationResult authorityValidation =
            Http2FieldSemantics.ValidateRequestAuthority(
                request.Headers, authoritySeen, authority, scheme);
        if (!authorityValidation.IsValid)
        {
            return Reject(
                result,
                authorityValidation.ErrorCodeName,
                authorityValidation.ErrorMessage);
        }

        request.Method = method;
        request.MethodMetadata = MethodSemantics.Describe(method);
        request.Scheme = scheme;
        request.Authority = authoritySeen ? authority : null;
        request.Target = path;
        if (path == "*")
        {
            request.TargetForm = HttpRequestTargetForm.Asterisk;
        }
        else
        {
            request.TargetForm = HttpRequestTargetForm.Origin;
            ApplyPath(request, path);
        }

        return Complete(result, request, endStream);
    }

    internal static Http2MessageResult DecodeExtendedConnect(
        Http2MessageResult result,
        HttpRequest request,
        string method,
        bool schemeSeen,
        bool authoritySeen,
        bool pathSeen,
        string scheme,
        string authority,
        string path,
        string protocol,
        bool extendedConnectEnabled,
        bool endStream)
    {
        if (!extendedConnectEnabled)
        {
            return Reject(
                result,
                "http2-connect-protocol-not-enabled",
                "The peer has not enabled the extended CONNECT protocol.");
        }

        if (!IsValidProtocol(protocol))
        {
            return Reject(
                result,
                "invalid-http2-connect-protocol",
                "The extended CONNECT :protocol value must be a non-empty token.");
        }

        if (!schemeSeen || !IsValidScheme(scheme) ||
            !pathSeen || !IsValidPath(method, path) ||
            !authoritySeen || !IsValidAuthority(authority, scheme, false))
        {
            return Reject(
                result,
                "invalid-http2-extended-connect-pseudo-headers",
                "Extended CONNECT requires valid :scheme, :authority, and :path values.");
        }

        Http2FieldValidationResult authorityValidation =
            Http2FieldSemantics.ValidateRequestAuthority(
                request.Headers, true, authority, scheme);
        if (!authorityValidation.IsValid)
        {
            return Reject(
                result,
                authorityValidation.ErrorCodeName,
                authorityValidation.ErrorMessage);
        }

        request.Method = method;
        request.MethodMetadata = MethodSemantics.Describe(method);
        request.Scheme = scheme;
        request.Authority = authority;
        request.Target = path;
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.ConnectProtocol = protocol;
        ApplyPath(request, path);
        Http2MessageResult completed = Complete(
            result, request, endStream);
        completed.IsTunnel = true;
        return completed;
    }

    internal static Http2MessageResult DecodeConnect(
        Http2MessageResult result,
        HttpRequest request,
        string method,
        bool schemeSeen,
        bool authoritySeen,
        bool pathSeen,
        string authority,
        bool endStream)
    {
        if (schemeSeen || pathSeen)
        {
            return Reject(
                result,
                "invalid-http2-connect-pseudo-headers",
                "A standard CONNECT request must omit :scheme and :path.");
        }

        if (!authoritySeen || !IsValidAuthority(authority, "", true))
        {
            return Reject(
                result,
                "invalid-http2-connect-authority",
                "A CONNECT request requires a :authority value with a host and port.");
        }

        Http2FieldValidationResult authorityValidation =
            Http2FieldSemantics.ValidateRequestAuthority(
                request.Headers, true, authority, "");
        if (!authorityValidation.IsValid)
        {
            return Reject(
                result,
                authorityValidation.ErrorCodeName,
                authorityValidation.ErrorMessage);
        }

        request.Method = method;
        request.MethodMetadata = MethodSemantics.Describe(method);
        request.Authority = authority;
        request.Target = authority;
        request.TargetForm = HttpRequestTargetForm.Authority;
        return Complete(result, request, endStream);
    }

    internal static bool IsValidMethod(string method)
    {
        if (method.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < method.Length)
        {
            char character = method[index];
            if (character > 0x7f ||
                !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidScheme(string scheme)
    {
        if (scheme.Length == 0)
        {
            return false;
        }

        UriValidationResult validation = UriCodec.ValidateScheme(
            scheme, int.MaxValue);
        return validation.Status == HttpOperationStatus.Done;
    }

    internal static bool IsValidProtocol(string protocol)
    {
        if (protocol.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < protocol.Length)
        {
            char character = protocol[index];
            if (character > 0x7f ||
                !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidAuthority(
        string authority,
        string scheme,
        bool requirePort)
    {
        UriValidationResult validation = UriCodec.ValidateAuthority(
            authority, int.MaxValue);
        if (validation.Status != HttpOperationStatus.Done ||
            validation.HasPort && validation.HasPortValue &&
                !validation.IsPortInRange)
        {
            return false;
        }

        if (requirePort)
        {
            return !validation.HasUserInfo && validation.Host.Length != 0 &&
                validation.HasPort && validation.HasPortValue &&
                validation.IsPortInRange;
        }

        bool httpScheme = Ascii.EqualsStringsIgnoreCase(scheme, "http") ||
            Ascii.EqualsStringsIgnoreCase(scheme, "https");
        if (httpScheme &&
            (validation.HasUserInfo || validation.Host.Length == 0))
        {
            return false;
        }

        return true;
    }

    internal static bool IsValidPath(string method, string path)
    {
        if (path == "*")
        {
            return method == HttpMethods.Options;
        }

        if (path.Length == 0 || path[0] != '/' ||
            FindCharacter(path, '#') >= 0)
        {
            return false;
        }

        int queryIndex = FindCharacter(path, '?');
        int pathLength = queryIndex < 0 ? path.Length : queryIndex;
        string pathPart = path.Substring(0, pathLength);
        UriValidationResult pathValidation = UriCodec.ValidatePath(
            pathPart, int.MaxValue);
        if (pathValidation.Status != HttpOperationStatus.Done)
        {
            return false;
        }

        if (queryIndex >= 0)
        {
            string query = path.Substring(queryIndex + 1);
            UriValidationResult queryValidation = UriCodec.ValidateQuery(
                query, int.MaxValue);
            if (queryValidation.Status != HttpOperationStatus.Done)
            {
                return false;
            }
        }

        return true;
    }

    internal static void ApplyPath(HttpRequest request, string target)
    {
        int queryIndex = FindCharacter(target, '?');
        if (queryIndex < 0)
        {
            request.Path = target;
            return;
        }

        request.Path = target.Substring(0, queryIndex);
        request.HasQuery = true;
        request.Query = target.Substring(queryIndex + 1);
    }

    internal static int FindCharacter(string value, char character)
    {
        int index = 0;
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

    internal static HttpHeaderField CopyField(HttpHeaderField source)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = source.Name;
        field.Value = CopyBytes(source.Value);
        field.IsSensitive = source.IsSensitive;
        field.NeverIndex = source.NeverIndex;
        return field;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static Http2MessageResult CreateResult(
        int streamId,
        HttpHeaders headers,
        bool endStream)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.StreamId = streamId;
        result.MessageKind = HttpMessageKind.Request;
        result.EndStream = endStream;
        if (headers != null)
        {
            result.Headers = headers;
        }

        return result;
    }

    internal static Http2MessageResult Complete(
        Http2MessageResult result,
        HttpRequest request,
        bool endStream)
    {
        request.IsComplete = endStream;
        result.Status = HttpOperationStatus.Done;
        result.Request = request;
        result.IsComplete = endStream;
        return result;
    }

    internal static Http2MessageResult RejectDuplicate(
        Http2MessageResult result,
        string name)
    {
        return Reject(
            result,
            "duplicate-http2-request-pseudo-header",
            $"The request contains more than one {name} pseudo-header field.");
    }

    internal static Http2MessageResult Reject(
        Http2MessageResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.IsMalformed = true;
        result.ErrorScope = Http2ErrorScope.Stream;
        result.HasErrorCode = true;
        result.ErrorCode = Http2ErrorCode.ProtocolError;
        result.ErrorCodeValue = (uint)Http2ErrorCode.ProtocolError;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
