using System;

namespace websocket;

internal class Http1ServerHandshakeState
{
    const string WebSocketFieldValue = "websocket";
    const string UpgradeFieldValue = "Upgrade";
    const string ContentLengthFieldName = "Content-Length";
    const string TransferEncodingFieldName = "Transfer-Encoding";

    internal bool HasEvaluation;
    internal bool Accepted;
    internal int ResponseStatusCode;
    internal bool AdvertiseVersion;
    internal string ClientKey = "";
    internal bool HasSelectedSubprotocol;
    internal string SelectedSubprotocol = "";
    internal WebSocketExtensionNegotiation[] NegotiatedExtensions =
        Array.Empty<WebSocketExtensionNegotiation>();
    internal WebSocketHeaderField[] ResponseFields = Array.Empty<WebSocketHeaderField>();
    internal bool HasResponseBody;
    internal byte[] ResponseBody = Array.Empty<byte>();

    internal WebSocketHandshakeResult EvaluateRequest(
        WebSocketHttpRequest request,
        WebSocketServerHandshakeOptions options)
    {
        ClearEvaluation();
        WebSocketOptionValidator.ValidateServerHandshakeOptions(options);
        ValidateResponseFields(options.ExtraFields);
        ValidateResponseFields(options.RejectionFields);
        if (!SupportsHttp1(options.SupportedTransports))
        {
            throw new WebSocketHandshakeException();
        }

        if (request == null || !IsRequestMetadataSafe(request))
        {
            return CompleteFailure(
                null,
                "",
                400,
                "websocket-request-metadata-invalid",
                "The HTTP/1 WebSocket request metadata is invalid or unsafe.");
        }

        WebSocketHttpRequest requestCopy = CopyRequest(request);
        if (request.Method != WebSocketProtocolConstants.Http1RequestMethod ||
            !IsHttp11OrNewer(request.HttpVersion))
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-line-invalid",
                "The HTTP request line is not valid for a WebSocket Upgrade.");
        }

        HeaderFieldLookupResult hosts = HeaderFieldLookup.FindFields(
            request.Fields,
            WebSocketProtocolConstants.HostHeaderName);
        if (hosts.Status != WebSocketParseStatus.Complete ||
            hosts.Matches.Length != 1 ||
            WebSocketOriginParser.ParseAuthority("http", hosts.Matches[0].Value).Status !=
                WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-host-invalid",
                "The HTTP Host field is missing, repeated, or invalid.");
        }

        HeaderTokenSearchResult upgrade = HeaderFieldLookup.ContainsHttpToken(
            request.Fields,
            WebSocketProtocolConstants.UpgradeHeaderName,
            WebSocketFieldValue,
            int.MaxValue);
        if (upgrade.Status != WebSocketParseStatus.Complete ||
            upgrade.FieldCount == 0 ||
            !upgrade.Found)
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-upgrade-invalid",
                "The HTTP Upgrade field does not select WebSocket.");
        }

        HeaderTokenSearchResult connection = HeaderFieldLookup.ContainsHttpToken(
            request.Fields,
            WebSocketProtocolConstants.ConnectionHeaderName,
            UpgradeFieldValue,
            int.MaxValue);
        if (connection.Status != WebSocketParseStatus.Complete ||
            connection.FieldCount == 0 ||
            !connection.Found)
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-connection-invalid",
                "The HTTP Connection field does not contain Upgrade.");
        }

        HeaderFieldLookupResult keys = HeaderFieldLookup.FindFields(
            request.Fields,
            WebSocketProtocolConstants.KeyHeaderName);
        byte[] nonce = new byte[WebSocketProtocolConstants.ClientNonceLength];
        if (keys.Status != WebSocketParseStatus.Complete || keys.Matches.Length != 1)
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-key-invalid",
                "The Sec-WebSocket-Key field is missing or repeated.");
        }

        Base64DecodeResult keyDecode = Base64Codec.DecodeClientNonce(keys.Matches[0].Value, nonce);
        if (keyDecode.Status != WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                "",
                400,
                "websocket-request-key-invalid",
                "The Sec-WebSocket-Key field is not a canonical sixteen-byte nonce.");
        }

        string clientKey = keys.Matches[0].Value;
        HeaderFieldLookupResult versions = HeaderFieldLookup.FindFields(
            request.Fields,
            WebSocketProtocolConstants.VersionHeaderName);
        if (versions.Status != WebSocketParseStatus.Complete || versions.Matches.Length != 1)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-version-invalid",
                "The Sec-WebSocket-Version field is missing or repeated.");
        }

        int requestedVersion = ParseVersion(versions.Matches[0].Value);
        if (requestedVersion < 0)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-version-invalid",
                "The Sec-WebSocket-Version field is malformed.");
        }

        if (requestedVersion != WebSocketProtocolConstants.Version)
        {
            AdvertiseVersion = true;
            return CompleteRejection(
                requestCopy,
                clientKey,
                426,
                Array.Empty<WebSocketHeaderField>(),
                false,
                Array.Empty<byte>(),
                "websocket-version-unsupported",
                "The requested WebSocket protocol version is not supported.");
        }

        WebSocketSubprotocolOfferResult protocols = WebSocketSubprotocolNegotiator.ParseClientOffers(
            request.Fields,
            int.MaxValue);
        if (protocols.Status != WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-subprotocol-invalid",
                "The WebSocket subprotocol offer is malformed or duplicated.");
        }

        WebSocketExtensionOffer[]? clientExtensions = ParseClientExtensions(
            request.Fields,
            options);
        if (clientExtensions == null)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-extensions-invalid",
                "The WebSocket extension offer is malformed.");
        }

        HeaderFieldLookupResult origins = HeaderFieldLookup.FindFields(
            request.Fields,
            WebSocketProtocolConstants.OriginHeaderName);
        if (origins.Status != WebSocketParseStatus.Complete || origins.Matches.Length > 1)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-origin-invalid",
                "The Origin field is repeated or invalid.");
        }

        if (origins.Matches.Length == 1 &&
            WebSocketOriginParser.ParseOrigin(origins.Matches[0].Value).Status !=
                WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-origin-invalid",
                "The Origin field is malformed.");
        }

        IWebSocketOriginPolicy originPolicy = new DefaultWebSocketOriginPolicy(Array.Empty<string>());
        if (options.HasOriginPolicy && options.OriginPolicy != null)
        {
            originPolicy = options.OriginPolicy;
        }
        if (!originPolicy.IsOriginAllowed(CopyRequest(requestCopy)))
        {
            return CompleteRejection(
                requestCopy,
                clientKey,
                options.RejectionStatusCode,
                CopyFields(options.RejectionFields),
                options.HasRejectionBody,
                CopyBytes(options.RejectionBody),
                "websocket-origin-rejected",
                "The request Origin was rejected by server policy.");
        }

        IWebSocketSubprotocolSelector selector = new DefaultWebSocketSubprotocolSelector(false);
        if (options.HasSubprotocolSelector && options.SubprotocolSelector != null)
        {
            selector = options.SubprotocolSelector;
        }
        WebSocketSubprotocolSelectionResult selection = WebSocketSubprotocolNegotiator.Choose(
            protocols.Protocols,
            options.SupportedSubprotocols,
            selector);
        if (selection.Status != WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-subprotocol-selection-invalid",
                "The selected WebSocket subprotocol was not offered and supported.");
        }

        WebSocketExtensionNegotiation[]? negotiated = NegotiateExtensions(
            clientExtensions,
            options);
        if (negotiated == null)
        {
            return CompleteFailure(
                requestCopy,
                clientKey,
                400,
                "websocket-request-extensions-invalid",
                "A WebSocket extension rejected the offered configuration.");
        }

        HasEvaluation = true;
        Accepted = true;
        ResponseStatusCode = WebSocketProtocolConstants.Http1SwitchingProtocolsStatus;
        ClientKey = clientKey;
        HasSelectedSubprotocol = selection.HasSelection;
        SelectedSubprotocol = selection.SelectedProtocol;
        NegotiatedExtensions = negotiated;
        ResponseFields = CopyFields(options.ExtraFields);

        WebSocketHandshakeResult result = CreateBaseResult(requestCopy, clientKey);
        result.Outcome = WebSocketHandshakeOutcome.Accepted;
        result.HasSelectedSubprotocol = selection.HasSelection;
        result.SelectedSubprotocol = selection.SelectedProtocol;
        result.Extensions = CopyNegotiations(negotiated);
        return AddResponse(result);
    }

    void ClearEvaluation()
    {
        HasEvaluation = false;
        Accepted = false;
        ResponseStatusCode = 0;
        AdvertiseVersion = false;
        ClientKey = "";
        HasSelectedSubprotocol = false;
        SelectedSubprotocol = "";
        NegotiatedExtensions = Array.Empty<WebSocketExtensionNegotiation>();
        ResponseFields = Array.Empty<WebSocketHeaderField>();
        HasResponseBody = false;
        ResponseBody = Array.Empty<byte>();
    }

    WebSocketHandshakeResult CompleteFailure(
        WebSocketHttpRequest? request,
        string clientKey,
        int statusCode,
        string identifier,
        string diagnostic)
    {
        HasEvaluation = true;
        ResponseStatusCode = statusCode;
        ClientKey = clientKey;
        WebSocketHandshakeResult result = CreateErrorResult(
            request,
            clientKey,
            WebSocketHandshakeOutcome.Failed,
            statusCode,
            identifier,
            diagnostic);
        return AddResponse(result);
    }

    WebSocketHandshakeResult CompleteRejection(
        WebSocketHttpRequest request,
        string clientKey,
        int statusCode,
        WebSocketHeaderField[] fields,
        bool hasBody,
        byte[] body,
        string identifier,
        string diagnostic)
    {
        HasEvaluation = true;
        ResponseStatusCode = statusCode;
        ClientKey = clientKey;
        ResponseFields = fields;
        HasResponseBody = hasBody;
        ResponseBody = body;
        WebSocketHandshakeResult result = CreateErrorResult(
            request,
            clientKey,
            WebSocketHandshakeOutcome.Rejected,
            statusCode,
            identifier,
            diagnostic);
        return AddResponse(result);
    }

    WebSocketHandshakeResult AddResponse(WebSocketHandshakeResult result)
    {
        WebSocketHttpResponse response;
        if (Accepted)
        {
            response = CreateAcceptanceResponse();
        }
        else
        {
            response = CreateRejectionResponse();
        }

        if (Http1Writer.MeasureResponse(response).Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        ResponseFields = CopyFields(response.Fields);
        ResponseBody = CopyBytes(response.Body);
        result.HasResponse = true;
        result.Response = response;
        return result;
    }

    WebSocketHttpResponse CreateAcceptanceResponse()
    {
        WebSocketHandshakeValueResult accept = WebSocketHandshakeCryptography.ComputeAccept(
            ClientKey);
        if (accept.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketExtensionOffer[] configurations =
            new WebSocketExtensionOffer[NegotiatedExtensions.Length];
        int extensionIndex = 0;
        while (extensionIndex < NegotiatedExtensions.Length)
        {
            configurations[extensionIndex] = CopyExtensionOffer(
                NegotiatedExtensions[extensionIndex].AcceptedConfiguration);
            extensionIndex = extensionIndex + 1;
        }

        WebSocketExtensionOfferFormatResult extensionValue =
            WebSocketExtensionOfferFormatter.FormatOffers(configurations);
        if (extensionValue.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        int requiredCount = 3;
        if (HasSelectedSubprotocol)
        {
            requiredCount = requiredCount + 1;
        }

        if (configurations.Length != 0)
        {
            requiredCount = requiredCount + 1;
        }

        WebSocketHeaderField[] fields = new WebSocketHeaderField[
            requiredCount + ResponseFields.Length];
        int fieldIndex = 0;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.UpgradeHeaderName,
            WebSocketFieldValue);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.ConnectionHeaderName,
            UpgradeFieldValue);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.AcceptHeaderName,
            accept.Value);
        fieldIndex = fieldIndex + 1;
        if (HasSelectedSubprotocol)
        {
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.ProtocolHeaderName,
                SelectedSubprotocol);
            fieldIndex = fieldIndex + 1;
        }

        if (configurations.Length != 0)
        {
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.ExtensionsHeaderName,
                extensionValue.Value);
            fieldIndex = fieldIndex + 1;
        }

        CopyFieldsInto(ResponseFields, fields, fieldIndex);
        WebSocketHttpResponse response = CreateResponseMetadata(
            WebSocketProtocolConstants.Http1SwitchingProtocolsStatus,
            "Switching Protocols");
        response.Fields = fields;
        return response;
    }

    WebSocketHttpResponse CreateRejectionResponse()
    {
        int requiredCount = 0;
        if (AdvertiseVersion)
        {
            requiredCount = requiredCount + 1;
        }

        if (HasResponseBody)
        {
            requiredCount = requiredCount + 1;
        }

        WebSocketHeaderField[] fields = new WebSocketHeaderField[
            requiredCount + ResponseFields.Length];
        int fieldIndex = 0;
        if (AdvertiseVersion)
        {
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.VersionHeaderName,
                WebSocketProtocolConstants.VersionText);
            fieldIndex = fieldIndex + 1;
        }

        CopyFieldsInto(ResponseFields, fields, fieldIndex);
        fieldIndex = fieldIndex + ResponseFields.Length;
        if (HasResponseBody)
        {
            fields[fieldIndex] = CreateField(
                ContentLengthFieldName,
                ResponseBody.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        WebSocketHttpResponse response = CreateResponseMetadata(
            ResponseStatusCode,
            ReadReasonPhrase(ResponseStatusCode));
        response.Fields = fields;
        response.HasBody = HasResponseBody;
        response.Body = CopyBytes(ResponseBody);
        return response;
    }

    static WebSocketHttpResponse CreateResponseMetadata(int statusCode, string reasonPhrase)
    {
        WebSocketHttpResponse response = new WebSocketHttpResponse();
        response.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        response.StatusCode = statusCode;
        response.HasReasonPhrase = reasonPhrase.Length != 0;
        response.ReasonPhrase = reasonPhrase;
        return response;
    }

    static string ReadReasonPhrase(int statusCode)
    {
        if (statusCode == WebSocketProtocolConstants.Http1SwitchingProtocolsStatus)
        {
            return "Switching Protocols";
        }

        if (statusCode == 400)
        {
            return "Bad Request";
        }

        if (statusCode == 403)
        {
            return "Forbidden";
        }

        if (statusCode == 404)
        {
            return "Not Found";
        }

        if (statusCode == 426)
        {
            return "Upgrade Required";
        }

        return "";
    }

    static WebSocketHeaderField CreateField(string name, string value)
    {
        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = name;
        field.Value = value;
        return field;
    }

    static void CopyFieldsInto(
        WebSocketHeaderField[] source,
        WebSocketHeaderField[] destination,
        int destinationOffset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationOffset + index] = CreateField(
                source[index].Name,
                source[index].Value);
            index = index + 1;
        }
    }

    static WebSocketHandshakeResult CreateErrorResult(
        WebSocketHttpRequest? request,
        string clientKey,
        WebSocketHandshakeOutcome outcome,
        int statusCode,
        string identifier,
        string diagnostic)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = outcome;
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        if (request != null)
        {
            result.HasRequest = true;
            result.Request = request;
        }

        if (clientKey.Length != 0)
        {
            result.HasClientKey = true;
            result.ClientKey = clientKey;
        }

        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.HasHttpStatusCode = true;
        result.Error.HttpStatusCode = statusCode;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    static WebSocketHandshakeResult CreateBaseResult(
        WebSocketHttpRequest request,
        string clientKey)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        result.HasRequest = true;
        result.Request = request;
        result.HasClientKey = true;
        result.ClientKey = clientKey;
        return result;
    }

    static bool SupportsHttp1(WebSocketHandshakeTransport[] transports)
    {
        int index = 0;
        while (index < transports.Length)
        {
            if (transports[index] == WebSocketHandshakeTransport.Http1Upgrade)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsRequestMetadataSafe(WebSocketHttpRequest request)
    {
        if (request.Transport != WebSocketHandshakeTransport.Http1Upgrade ||
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
            !IsHttpVersionText(request.HttpVersion) ||
            request.HasScheme || request.Scheme.Length != 0 ||
            request.HasAuthority || request.Authority.Length != 0 ||
            request.HasPath || request.Path.Length != 0 ||
            request.HasProtocol || request.Protocol.Length != 0)
        {
            return false;
        }

        int index = 0;
        while (index < request.Fields.Length)
        {
            WebSocketHeaderField field = request.Fields[index];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsHttpVersionText(ReadOnlySpan<char> value)
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

    static bool IsHttp11OrNewer(string version)
    {
        return version.Length == 8 &&
            version[0] == 'H' &&
            version[1] == 'T' &&
            version[2] == 'T' &&
            version[3] == 'P' &&
            version[4] == '/' &&
            version[5] >= '0' &&
            version[5] <= '9' &&
            version[6] == '.' &&
            version[7] >= '0' &&
            version[7] <= '9' &&
            (version[5] > '1' || (version[5] == '1' && version[7] >= '1'));
    }

    static int ParseVersion(string value)
    {
        if (value.Length == 0 || value.Length > 3 ||
            (value.Length > 1 && value[0] == '0'))
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

            parsed = parsed * 10 + character - '0';
            index = index + 1;
        }

        return parsed <= 255 ? parsed : -1;
    }

    static WebSocketExtensionOffer[]? ParseClientExtensions(
        WebSocketHeaderField[] fields,
        WebSocketServerHandshakeOptions options)
    {
        HeaderFieldLookupResult lookup = HeaderFieldLookup.FindFields(
            fields,
            WebSocketProtocolConstants.ExtensionsHeaderName);
        if (lookup.Status != WebSocketParseStatus.Complete)
        {
            return null;
        }

        if (lookup.Matches.Length == 0)
        {
            return Array.Empty<WebSocketExtensionOffer>();
        }

        WebSocketExtensionResponseParseResult parsed =
            WebSocketExtensionResponseParser.ParseFields(
                lookup.Matches,
                options.MaximumHeaderCount,
                options.MaximumHeaderCount,
                options.MaximumHeaderCount,
                options.MaximumLineLength,
                options.MaximumHeaderBytes);
        return parsed.Status == WebSocketParseStatus.Complete ? parsed.Extensions : null;
    }

    static WebSocketExtensionNegotiation[]? NegotiateExtensions(
        WebSocketExtensionOffer[] clientOffers,
        WebSocketServerHandshakeOptions options)
    {
        if (options.ExtensionHandlers.Length != 0)
        {
            WebSocketExtensionNegotiatorResult generic =
                WebSocketExtensionNegotiator.SelectServerExtensions(
                    clientOffers,
                    options.ExtensionHandlers);
            if (generic.Status != WebSocketParseStatus.Complete)
            {
                return null;
            }

            return generic.Negotiations;
        }

        WebSocketExtensionOffer[] supportedOffers = options.SupportedExtensions;
        WebSocketExtensionNegotiation[] candidates =
            new WebSocketExtensionNegotiation[supportedOffers.Length];
        int count = 0;
        int supportedIndex = 0;
        while (supportedIndex < supportedOffers.Length)
        {
            WebSocketExtensionOffer supported = supportedOffers[supportedIndex];
            if (!ContainsNegotiatedName(candidates, count, supported.Name) &&
                ContainsExtension(clientOffers, supported))
            {
                WebSocketExtensionNegotiation negotiation = new WebSocketExtensionNegotiation();
                negotiation.Name = supported.Name;
                negotiation.Status = WebSocketExtensionStatus.Negotiated;
                negotiation.HasAcceptedConfiguration = true;
                negotiation.AcceptedConfiguration = CopyExtensionOffer(supported);
                candidates[count] = negotiation;
                count = count + 1;
            }

            supportedIndex = supportedIndex + 1;
        }

        WebSocketExtensionNegotiation[] result = new WebSocketExtensionNegotiation[count];
        int index = 0;
        while (index < count)
        {
            result[index] = candidates[index];
            index = index + 1;
        }

        return result;
    }

    static bool ContainsExtension(
        WebSocketExtensionOffer[] offers,
        WebSocketExtensionOffer candidate)
    {
        int index = 0;
        while (index < offers.Length)
        {
            if (ExtensionOffersEqual(offers[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ExtensionOffersEqual(
        WebSocketExtensionOffer left,
        WebSocketExtensionOffer right)
    {
        if (left.Name != right.Name || left.Parameters.Length != right.Parameters.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Parameters.Length)
        {
            WebSocketExtensionParameter leftParameter = left.Parameters[index];
            WebSocketExtensionParameter rightParameter = right.Parameters[index];
            if (leftParameter.Name != rightParameter.Name ||
                leftParameter.HasValue != rightParameter.HasValue ||
                leftParameter.Value != rightParameter.Value)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsNegotiatedName(
        WebSocketExtensionNegotiation[] negotiations,
        int count,
        string name)
    {
        int index = 0;
        while (index < count)
        {
            if (negotiations[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void ValidateResponseFields(WebSocketHeaderField[] fields)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (IsCriticalField(fields[index].Name))
            {
                throw new WebSocketHandshakeException();
            }

            index = index + 1;
        }
    }

    static bool IsCriticalField(string name)
    {
        return AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.HostHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.UpgradeHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.ConnectionHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.OriginHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.KeyHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.AcceptHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.VersionHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.ProtocolHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, WebSocketProtocolConstants.ExtensionsHeaderName) ||
            AsciiToken.EqualsIgnoreCase(name, ContentLengthFieldName) ||
            AsciiToken.EqualsIgnoreCase(name, TransferEncodingFieldName);
    }

    static WebSocketHttpRequest CopyRequest(WebSocketHttpRequest source)
    {
        WebSocketHttpRequest copy = new WebSocketHttpRequest();
        copy.Transport = source.Transport;
        copy.Method = source.Method;
        copy.HasRequestTarget = source.HasRequestTarget;
        copy.RequestTarget = source.RequestTarget;
        copy.HasHttpVersion = source.HasHttpVersion;
        copy.HttpVersion = source.HttpVersion;
        copy.HasScheme = source.HasScheme;
        copy.Scheme = source.Scheme;
        copy.HasAuthority = source.HasAuthority;
        copy.Authority = source.Authority;
        copy.HasPath = source.HasPath;
        copy.Path = source.Path;
        copy.HasProtocol = source.HasProtocol;
        copy.Protocol = source.Protocol;
        copy.Fields = CopyFields(source.Fields);
        return copy;
    }

    static WebSocketHeaderField[] CopyFields(WebSocketHeaderField[] source)
    {
        WebSocketHeaderField[] copy = new WebSocketHeaderField[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            WebSocketHeaderField field = new WebSocketHeaderField();
            field.Name = source[index].Name;
            field.Value = source[index].Value;
            copy[index] = field;
            index = index + 1;
        }

        return copy;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        source.AsSpan().CopyTo(copy);
        return copy;
    }

    static WebSocketExtensionOffer CopyExtensionOffer(WebSocketExtensionOffer source)
    {
        WebSocketExtensionOffer copy = new WebSocketExtensionOffer();
        copy.Name = source.Name;
        WebSocketExtensionParameter[] parameters =
            new WebSocketExtensionParameter[source.Parameters.Length];
        int index = 0;
        while (index < source.Parameters.Length)
        {
            WebSocketExtensionParameter parameter = new WebSocketExtensionParameter();
            parameter.Name = source.Parameters[index].Name;
            parameter.HasValue = source.Parameters[index].HasValue;
            parameter.Value = source.Parameters[index].Value;
            parameters[index] = parameter;
            index = index + 1;
        }

        copy.Parameters = parameters;
        return copy;
    }

    static WebSocketExtensionNegotiation[] CopyNegotiations(
        WebSocketExtensionNegotiation[] source)
    {
        WebSocketExtensionNegotiation[] copy = new WebSocketExtensionNegotiation[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = WebSocketExtensionNegotiator.CopyNegotiation(source[index]);
            index = index + 1;
        }

        return copy;
    }
}
