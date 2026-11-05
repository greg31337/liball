using System;

namespace websocket;

internal class ExtendedConnectServerHandshakeState
{
    const string OriginFieldName = "origin";
    const string VersionFieldName = "sec-websocket-version";
    const string ProtocolFieldName = "sec-websocket-protocol";
    const string ExtensionsFieldName = "sec-websocket-extensions";
    const string ContentLengthFieldName = "content-length";

    internal bool HasEvaluation;
    internal WebSocketHandshakeTransport EvaluationTransport;
    internal bool Accepted;
    internal int ResponseStatusCode;
    internal bool AdvertiseVersion;
    internal bool HasSelectedSubprotocol;
    internal string SelectedSubprotocol = "";
    internal WebSocketExtensionNegotiation[] NegotiatedExtensions =
        Array.Empty<WebSocketExtensionNegotiation>();
    internal WebSocketHeaderField[] ResponseFields = Array.Empty<WebSocketHeaderField>();
    internal bool HasResponseBody;
    internal byte[] ResponseBody = Array.Empty<byte>();

    internal WebSocketHandshakeResult EvaluateRequest(
        WebSocketHttpRequest? request,
        WebSocketServerHandshakeOptions options)
    {
        ClearEvaluation();
        EvaluationTransport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        if (request != null &&
            request.Transport == WebSocketHandshakeTransport.Http3ExtendedConnect)
        {
            EvaluationTransport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        }

        WebSocketOptionValidator.ValidateServerHandshakeOptions(options);
        ValidateResponseFields(options.ExtraFields);
        ValidateResponseFields(options.RejectionFields);

        ExtendedConnectValidationResult common = ExtendedConnectValidator.ValidateRequest(
            request,
            EvaluationTransport);
        if (!common.Valid)
        {
            if (common.Error.Identifier == "extended-connect-protocol-invalid" &&
                request != null &&
                request.HasProtocol &&
                request.Protocol != null &&
                AsciiToken.IsValid(request.Protocol) &&
                request.Protocol.Length != 0)
            {
                return CompleteRejection(
                    CopyRequest(request),
                    501,
                    Array.Empty<WebSocketHeaderField>(),
                    false,
                    Array.Empty<byte>(),
                    "extended-connect-protocol-unsupported",
                    "The requested Extended CONNECT protocol is not supported.");
            }

            return CompleteFailure(
                CopyRequestIfSafe(request),
                400,
                common.Error.Identifier,
                common.Error.Diagnostic);
        }

        if (request == null)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketHttpRequest requestCopy = CopyRequest(request);
        if (!options.ExtendedConnectEnabled ||
            !SupportsTransport(
                options.SupportedTransports,
                EvaluationTransport))
        {
            return CompleteRejection(
                requestCopy,
                501,
                Array.Empty<WebSocketHeaderField>(),
                false,
                Array.Empty<byte>(),
                "extended-connect-not-supported",
                "Extended CONNECT is not enabled for this transport by this server.");
        }

        HeaderFieldLookupResult versions = HeaderFieldLookup.FindFields(
            request.Fields,
            VersionFieldName);
        if (versions.Status != WebSocketParseStatus.Complete || versions.Matches.Length != 1)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-version-invalid",
                "The Sec-WebSocket-Version field is missing or repeated.");
        }

        int requestedVersion = ParseVersion(versions.Matches[0].Value);
        if (requestedVersion < 0)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-version-invalid",
                "The Sec-WebSocket-Version field is malformed.");
        }

        if (requestedVersion != WebSocketProtocolConstants.Version)
        {
            AdvertiseVersion = true;
            return CompleteRejection(
                requestCopy,
                426,
                Array.Empty<WebSocketHeaderField>(),
                false,
                Array.Empty<byte>(),
                "extended-connect-version-unsupported",
                "The requested WebSocket protocol version is not supported.");
        }

        WebSocketSubprotocolOfferResult protocols = WebSocketSubprotocolNegotiator.ParseClientOffers(
            request.Fields,
            int.MaxValue);
        if (protocols.Status != WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-subprotocol-invalid",
                "The WebSocket subprotocol offer is malformed or duplicated.");
        }

        WebSocketExtensionOffer[]? clientExtensions = ParseClientExtensions(
            request.Fields,
            options);
        if (clientExtensions == null)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-extensions-invalid",
                "The WebSocket extension offer is malformed.");
        }

        HeaderFieldLookupResult origins = HeaderFieldLookup.FindFields(
            request.Fields,
            OriginFieldName);
        if (origins.Status != WebSocketParseStatus.Complete || origins.Matches.Length > 1)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-origin-invalid",
                "The Origin field is repeated or invalid.");
        }

        if (origins.Matches.Length == 1 &&
            WebSocketOriginParser.ParseOrigin(origins.Matches[0].Value).Status !=
                WebSocketParseStatus.Complete)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-origin-invalid",
                "The Origin field is malformed.");
        }

        IWebSocketOriginPolicy originPolicy = new DefaultWebSocketOriginPolicy(
            Array.Empty<string>());
        if (options.HasOriginPolicy && options.OriginPolicy != null)
        {
            originPolicy = options.OriginPolicy;
        }

        if (!originPolicy.IsOriginAllowed(CopyRequest(requestCopy)))
        {
            return CompleteRejection(
                requestCopy,
                options.RejectionStatusCode,
                CopyFields(options.RejectionFields),
                options.HasRejectionBody,
                CopyBytes(options.RejectionBody),
                "extended-connect-origin-rejected",
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
                400,
                "extended-connect-subprotocol-selection-invalid",
                "The selected WebSocket subprotocol was not offered and supported.");
        }

        WebSocketExtensionNegotiation[]? negotiated = NegotiateExtensions(
            clientExtensions,
            options);
        if (negotiated == null)
        {
            return CompleteFailure(
                requestCopy,
                400,
                "extended-connect-extensions-invalid",
                "A WebSocket extension rejected the offered configuration.");
        }

        HasEvaluation = true;
        Accepted = true;
        ResponseStatusCode = 200;
        HasSelectedSubprotocol = selection.HasSelection;
        SelectedSubprotocol = selection.SelectedProtocol;
        NegotiatedExtensions = negotiated;
        ResponseFields = CopyFields(options.ExtraFields);

        WebSocketHandshakeResult result = CreateBaseResult(requestCopy);
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
        HasSelectedSubprotocol = false;
        SelectedSubprotocol = "";
        NegotiatedExtensions = Array.Empty<WebSocketExtensionNegotiation>();
        ResponseFields = Array.Empty<WebSocketHeaderField>();
        HasResponseBody = false;
        ResponseBody = Array.Empty<byte>();
    }

    WebSocketHandshakeResult CompleteFailure(
        WebSocketHttpRequest? request,
        int statusCode,
        string identifier,
        string diagnostic)
    {
        HasEvaluation = true;
        ResponseStatusCode = statusCode;
        WebSocketHandshakeResult result = CreateErrorResult(
            request,
            WebSocketHandshakeOutcome.Failed,
            statusCode,
            identifier,
            diagnostic);
        return AddResponse(result);
    }

    WebSocketHandshakeResult CompleteRejection(
        WebSocketHttpRequest request,
        int statusCode,
        WebSocketHeaderField[] fields,
        bool hasBody,
        byte[] body,
        string identifier,
        string diagnostic)
    {
        HasEvaluation = true;
        ResponseStatusCode = statusCode;
        ResponseFields = fields;
        HasResponseBody = hasBody;
        ResponseBody = body;
        WebSocketHandshakeResult result = CreateErrorResult(
            request,
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

        ExtendedConnectValidationResult validation = ExtendedConnectValidator.ValidateResponse(
            response,
            EvaluationTransport);
        if (!validation.Valid)
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

        int requiredCount = 0;
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
        if (HasSelectedSubprotocol)
        {
            fields[fieldIndex] = CreateField(ProtocolFieldName, SelectedSubprotocol);
            fieldIndex = fieldIndex + 1;
        }

        if (configurations.Length != 0)
        {
            fields[fieldIndex] = CreateField(ExtensionsFieldName, extensionValue.Value);
            fieldIndex = fieldIndex + 1;
        }

        CopyFieldsInto(ResponseFields, fields, fieldIndex);
        WebSocketHttpResponse response = CreateResponseMetadata(200);
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
                VersionFieldName,
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

        WebSocketHttpResponse response = CreateResponseMetadata(ResponseStatusCode);
        response.Fields = fields;
        response.HasBody = HasResponseBody;
        response.Body = CopyBytes(ResponseBody);
        return response;
    }

    WebSocketHttpResponse CreateResponseMetadata(int statusCode)
    {
        WebSocketHttpResponse response = new WebSocketHttpResponse();
        response.Transport = EvaluationTransport;
        response.StatusCode = statusCode;
        return response;
    }

    WebSocketHandshakeResult CreateErrorResult(
        WebSocketHttpRequest? request,
        WebSocketHandshakeOutcome outcome,
        int statusCode,
        string identifier,
        string diagnostic)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = outcome;
        result.Transport = EvaluationTransport;
        if (request != null)
        {
            result.HasRequest = true;
            result.Request = request;
        }

        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.HasHttpStatusCode = true;
        result.Error.HttpStatusCode = statusCode;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    WebSocketHandshakeResult CreateBaseResult(WebSocketHttpRequest request)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Transport = EvaluationTransport;
        result.HasRequest = true;
        result.Request = request;
        return result;
    }

    static bool SupportsTransport(
        WebSocketHandshakeTransport[] transports,
        WebSocketHandshakeTransport expected)
    {
        int index = 0;
        while (index < transports.Length)
        {
            if (transports[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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
            ExtensionsFieldName);
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

    void ValidateResponseFields(WebSocketHeaderField[] fields)
    {
        WebSocketHttpResponse response = CreateResponseMetadata(400);
        response.Fields = fields;
        ExtendedConnectValidationResult validation = ExtendedConnectValidator.ValidateResponse(
            response,
            EvaluationTransport);
        if (!validation.Valid)
        {
            throw new WebSocketHandshakeException();
        }

        int index = 0;
        while (index < fields.Length)
        {
            string name = fields[index].Name;
            if (AsciiToken.EqualsIgnoreCase(name, OriginFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, VersionFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, ProtocolFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, ExtensionsFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, ContentLengthFieldName))
            {
                throw new WebSocketHandshakeException();
            }

            index = index + 1;
        }
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

    static WebSocketHttpRequest? CopyRequestIfSafe(WebSocketHttpRequest? source)
    {
        if (source == null ||
            source.Method == null ||
            source.RequestTarget == null ||
            source.HttpVersion == null ||
            source.Scheme == null ||
            source.Authority == null ||
            source.Path == null ||
            source.Protocol == null ||
            source.Fields == null)
        {
            return null;
        }

        int index = 0;
        while (index < source.Fields.Length)
        {
            if (source.Fields[index] == null ||
                source.Fields[index].Name == null ||
                source.Fields[index].Value == null)
            {
                return null;
            }

            index = index + 1;
        }

        return CopyRequest(source);
    }

    static WebSocketHeaderField[] CopyFields(WebSocketHeaderField[] source)
    {
        WebSocketHeaderField[] copy = new WebSocketHeaderField[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CreateField(source[index].Name, source[index].Value);
            index = index + 1;
        }

        return copy;
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

    static WebSocketHeaderField CreateField(string name, string value)
    {
        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = name;
        field.Value = value;
        return field;
    }
}
