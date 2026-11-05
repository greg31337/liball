using System;

namespace websocket;

internal class ExtendedConnectClientHandshakeState
{
    const string HttpScheme = "http";
    const string HttpsScheme = "https";
    const string OriginFieldName = "origin";
    const string VersionFieldName = "sec-websocket-version";
    const string ProtocolFieldName = "sec-websocket-protocol";
    const string ExtensionsFieldName = "sec-websocket-extensions";
    const string UserAgentFieldName = "user-agent";

    internal bool HasPendingRequest;
    internal WebSocketHandshakeTransport PendingTransport;
    internal string[] OfferedSubprotocols = Array.Empty<string>();
    internal WebSocketExtensionOffer[] OfferedExtensions = Array.Empty<WebSocketExtensionOffer>();
    internal IWebSocketExtension[] OfferedExtensionHandlers =
        Array.Empty<IWebSocketExtension>();
    internal int MaximumHeaderBytes = WebSocketDefaults.MaximumHttpHeaderBytes;
    internal int MaximumHeaderCount = WebSocketDefaults.MaximumHttpHeaderCount;
    internal int MaximumLineLength = WebSocketDefaults.MaximumHttpLineLength;

    internal WebSocketHandshakeResult CreateRequest(WebSocketClientHandshakeOptions options)
    {
        ClearState();
        WebSocketOptionValidator.ValidateClientHandshakeOptions(options);
        if (!IsExtendedTransport(options.Transport) ||
            !options.PeerEnabledExtendedConnect)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketUriValidationResult uri = WebSocketUriValidator.Validate(options.Uri);
        if (!uri.Valid)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketExtensionNegotiatorResult clientOffers =
            WebSocketExtensionNegotiator.CreateClientOffers(
                options.Extensions,
                options.ExtensionHandlers);
        if (clientOffers.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketExtensionOfferFormatResult extensionValue =
            WebSocketExtensionOfferFormatter.FormatOffers(clientOffers.Offers);
        if (extensionValue.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateExtraFields(options.ExtraFields, options.HasUserAgent);
        WebSocketHttpRequest request = CreateRequestMetadata(
            options,
            uri,
            extensionValue.Value,
            clientOffers.Offers.Length != 0);
        ExtendedConnectValidationResult validation = ExtendedConnectValidator.ValidateRequest(
            request,
            options.Transport);
        if (!validation.Valid)
        {
            throw new WebSocketHandshakeException();
        }

        OfferedSubprotocols = CopyProtocols(options.Subprotocols);
        OfferedExtensions = CopyExtensionOffers(clientOffers.Offers);
        OfferedExtensionHandlers = clientOffers.Handlers;
        MaximumHeaderBytes = options.MaximumHeaderBytes;
        MaximumHeaderCount = options.MaximumHeaderCount;
        MaximumLineLength = options.MaximumLineLength;
        PendingTransport = options.Transport;
        HasPendingRequest = true;

        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Transport = options.Transport;
        result.HasRequest = true;
        result.Request = request;
        return result;
    }

    internal WebSocketHandshakeResult ValidateResponse(WebSocketHttpResponse response)
    {
        if (!HasPendingRequest)
        {
            throw new WebSocketStateException();
        }

        WebSocketHandshakeTransport expectedTransport = PendingTransport;
        ExtendedConnectValidationResult validation = ExtendedConnectValidator.ValidateResponse(
            response,
            expectedTransport);
        if (!validation.Valid)
        {
            ClearState();
            return CreateValidationFailure(validation.Error, expectedTransport);
        }

        WebSocketHttpResponse responseCopy = CopyResponse(response);
        if (response.StatusCode == WebSocketProtocolConstants.Http1SwitchingProtocolsStatus)
        {
            ClearState();
            return CreateFailure(
                responseCopy,
                "extended-connect-response-status-invalid",
                "Status 101 is forbidden for an Extended CONNECT response.",
                expectedTransport);
        }

        if (response.StatusCode < 200)
        {
            return CreateResponseResult(
                responseCopy,
                WebSocketHandshakeOutcome.Incomplete,
                expectedTransport);
        }

        if (response.StatusCode >= 300)
        {
            ClearState();
            return CreateResponseResult(
                responseCopy,
                WebSocketHandshakeOutcome.Rejected,
                expectedTransport);
        }

        WebSocketSubprotocolSelectionResult subprotocol = ValidateSelectedSubprotocol(
            response.Fields);
        if (subprotocol.Status != WebSocketParseStatus.Complete)
        {
            ClearState();
            return CreateFailure(
                responseCopy,
                "extended-connect-response-subprotocol-invalid",
                "The Extended CONNECT response selected an invalid or unoffered subprotocol.",
                expectedTransport);
        }

        WebSocketExtensionNegotiation[]? extensions = ValidateResponseExtensions(
            response.Fields);
        if (extensions == null)
        {
            ClearState();
            return CreateFailure(
                responseCopy,
                "extended-connect-response-extensions-invalid",
                "The Extended CONNECT response selected an invalid or unoffered extension configuration.",
                expectedTransport);
        }

        WebSocketHandshakeResult result = CreateResponseResult(
            responseCopy,
            WebSocketHandshakeOutcome.Accepted,
            expectedTransport);
        result.HasSelectedSubprotocol = subprotocol.HasSelection;
        result.SelectedSubprotocol = subprotocol.SelectedProtocol;
        result.Extensions = extensions;
        ClearState();
        return result;
    }

    void ClearState()
    {
        HasPendingRequest = false;
        OfferedSubprotocols = Array.Empty<string>();
        OfferedExtensions = Array.Empty<WebSocketExtensionOffer>();
        OfferedExtensionHandlers = Array.Empty<IWebSocketExtension>();
    }

    WebSocketSubprotocolSelectionResult ValidateSelectedSubprotocol(
        WebSocketHeaderField[] fields)
    {
        WebSocketSubprotocolSelectionResult result = new WebSocketSubprotocolSelectionResult();
        HeaderFieldLookupResult protocols = HeaderFieldLookup.FindFields(
            fields,
            ProtocolFieldName);
        if (protocols.Status != WebSocketParseStatus.Complete || protocols.Matches.Length > 1)
        {
            return result;
        }

        if (protocols.Matches.Length == 0)
        {
            result.Status = WebSocketParseStatus.Complete;
            return result;
        }

        string selected = protocols.Matches[0].Value;
        if (!AsciiToken.IsValid(selected) || !ContainsProtocol(OfferedSubprotocols, selected))
        {
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.HasSelection = true;
        result.SelectedProtocol = selected;
        return result;
    }

    WebSocketExtensionNegotiation[]? ValidateResponseExtensions(
        WebSocketHeaderField[] fields)
    {
        HeaderFieldLookupResult extensionFields = HeaderFieldLookup.FindFields(
            fields,
            ExtensionsFieldName);
        if (extensionFields.Status != WebSocketParseStatus.Complete ||
            extensionFields.Matches.Length > 1)
        {
            return null;
        }

        if (extensionFields.Matches.Length == 0)
        {
            return Array.Empty<WebSocketExtensionNegotiation>();
        }

        WebSocketExtensionResponseParseResult parsed =
            WebSocketExtensionResponseParser.ParseFields(
                extensionFields.Matches,
                1,
                OfferedExtensions.Length,
                MaximumHeaderCount,
                MaximumLineLength,
                MaximumHeaderBytes);
        if (parsed.Status != WebSocketParseStatus.Complete)
        {
            return null;
        }

        if (OfferedExtensionHandlers.Length != 0)
        {
            WebSocketExtensionNegotiatorResult validation =
                WebSocketExtensionNegotiator.ValidateServerExtensions(
                    parsed.Extensions,
                    OfferedExtensions,
                    OfferedExtensionHandlers);
            if (validation.Status != WebSocketParseStatus.Complete)
            {
                return null;
            }

            return validation.Negotiations;
        }

        bool[] usedOffers = new bool[OfferedExtensions.Length];
        WebSocketExtensionNegotiation[] negotiations =
            new WebSocketExtensionNegotiation[parsed.Extensions.Length];
        int responseIndex = 0;
        while (responseIndex < parsed.Extensions.Length)
        {
            WebSocketExtensionOffer responseExtension = parsed.Extensions[responseIndex];
            int matchedIndex = FindMatchingExtension(
                OfferedExtensions,
                usedOffers,
                responseExtension);
            if (matchedIndex < 0 ||
                ContainsNegotiatedName(negotiations, responseIndex, responseExtension.Name))
            {
                return null;
            }

            usedOffers[matchedIndex] = true;
            WebSocketExtensionNegotiation negotiation = new WebSocketExtensionNegotiation();
            negotiation.Name = responseExtension.Name;
            negotiation.Status = WebSocketExtensionStatus.Negotiated;
            negotiation.HasAcceptedConfiguration = true;
            negotiation.AcceptedConfiguration = CopyExtensionOffer(responseExtension);
            negotiations[responseIndex] = negotiation;
            responseIndex = responseIndex + 1;
        }

        return negotiations;
    }

    static WebSocketHttpRequest CreateRequestMetadata(
        WebSocketClientHandshakeOptions options,
        WebSocketUriValidationResult uri,
        string extensionValue,
        bool hasExtensions)
    {
        int fieldCount = 1 + options.ExtraFields.Length;
        if (options.HasOrigin)
        {
            fieldCount = fieldCount + 1;
        }

        if (options.Subprotocols.Length != 0)
        {
            fieldCount = fieldCount + 1;
        }

        if (hasExtensions)
        {
            fieldCount = fieldCount + 1;
        }

        if (options.HasUserAgent)
        {
            fieldCount = fieldCount + 1;
        }

        WebSocketHeaderField[] fields = new WebSocketHeaderField[fieldCount];
        int fieldIndex = 0;
        fields[fieldIndex] = CreateField(
            VersionFieldName,
            WebSocketProtocolConstants.VersionText);
        fieldIndex = fieldIndex + 1;
        if (options.HasOrigin)
        {
            WebSocketOriginParseResult origin = WebSocketOriginParser.ParseOrigin(options.Origin);
            fields[fieldIndex] = CreateField(OriginFieldName, origin.SerializedOrigin);
            fieldIndex = fieldIndex + 1;
        }

        if (options.Subprotocols.Length != 0)
        {
            fields[fieldIndex] = CreateField(
                ProtocolFieldName,
                string.Join(", ", options.Subprotocols));
            fieldIndex = fieldIndex + 1;
        }

        if (hasExtensions)
        {
            fields[fieldIndex] = CreateField(ExtensionsFieldName, extensionValue);
            fieldIndex = fieldIndex + 1;
        }

        if (options.HasUserAgent)
        {
            fields[fieldIndex] = CreateField(UserAgentFieldName, options.UserAgent);
            fieldIndex = fieldIndex + 1;
        }

        int extraIndex = 0;
        while (extraIndex < options.ExtraFields.Length)
        {
            fields[fieldIndex] = CreateField(
                options.ExtraFields[extraIndex].Name,
                options.ExtraFields[extraIndex].Value);
            fieldIndex = fieldIndex + 1;
            extraIndex = extraIndex + 1;
        }

        WebSocketHttpRequest request = new WebSocketHttpRequest();
        request.Transport = options.Transport;
        request.Method = WebSocketProtocolConstants.ExtendedConnectMethod;
        request.HasScheme = true;
        request.Scheme = uri.Secure ? HttpsScheme : HttpScheme;
        request.HasAuthority = true;
        request.Authority = uri.Authority;
        request.HasPath = true;
        request.Path = uri.RequestTarget;
        request.HasProtocol = true;
        request.Protocol = WebSocketProtocolConstants.ExtendedConnectProtocol;
        request.Fields = fields;
        return request;
    }

    static void ValidateExtraFields(WebSocketHeaderField[] fields, bool hasUserAgent)
    {
        int index = 0;
        while (index < fields.Length)
        {
            string name = fields[index].Name;
            if (AsciiToken.EqualsIgnoreCase(name, OriginFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, VersionFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, ProtocolFieldName) ||
                AsciiToken.EqualsIgnoreCase(name, ExtensionsFieldName) ||
                (hasUserAgent && AsciiToken.EqualsIgnoreCase(name, UserAgentFieldName)))
            {
                throw new WebSocketHandshakeException();
            }

            index = index + 1;
        }
    }

    static WebSocketHandshakeResult CreateValidationFailure(
        WebSocketError error,
        WebSocketHandshakeTransport transport)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = WebSocketHandshakeOutcome.Failed;
        result.Transport = transport;
        result.HasError = true;
        result.Error.Kind = error.Kind;
        result.Error.Identifier = error.Identifier;
        result.Error.Diagnostic = error.Diagnostic;
        return result;
    }

    static WebSocketHandshakeResult CreateResponseResult(
        WebSocketHttpResponse response,
        WebSocketHandshakeOutcome outcome,
        WebSocketHandshakeTransport transport)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = outcome;
        result.Transport = transport;
        result.HasResponse = true;
        result.Response = response;
        return result;
    }

    static WebSocketHandshakeResult CreateFailure(
        WebSocketHttpResponse response,
        string identifier,
        string diagnostic,
        WebSocketHandshakeTransport transport)
    {
        WebSocketHandshakeResult result = CreateResponseResult(
            response,
            WebSocketHandshakeOutcome.Failed,
            transport);
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    static bool IsExtendedTransport(WebSocketHandshakeTransport transport)
    {
        return transport == WebSocketHandshakeTransport.Http2ExtendedConnect ||
            transport == WebSocketHandshakeTransport.Http3ExtendedConnect;
    }

    static WebSocketHttpResponse CopyResponse(WebSocketHttpResponse source)
    {
        WebSocketHttpResponse copy = new WebSocketHttpResponse();
        copy.Transport = source.Transport;
        copy.StatusCode = source.StatusCode;
        copy.HasReasonPhrase = source.HasReasonPhrase;
        copy.ReasonPhrase = source.ReasonPhrase;
        WebSocketHeaderField[] fields = new WebSocketHeaderField[source.Fields.Length];
        int fieldIndex = 0;
        while (fieldIndex < source.Fields.Length)
        {
            fields[fieldIndex] = CreateField(
                source.Fields[fieldIndex].Name,
                source.Fields[fieldIndex].Value);
            fieldIndex = fieldIndex + 1;
        }

        copy.Fields = fields;
        copy.HasBody = source.HasBody;
        byte[] body = new byte[source.Body.Length];
        source.Body.AsSpan().CopyTo(body);
        copy.Body = body;
        return copy;
    }

    static bool ContainsProtocol(string[] protocols, string candidate)
    {
        int index = 0;
        while (index < protocols.Length)
        {
            if (protocols[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int FindMatchingExtension(
        WebSocketExtensionOffer[] offers,
        bool[] usedOffers,
        WebSocketExtensionOffer response)
    {
        int index = 0;
        while (index < offers.Length)
        {
            if (!usedOffers[index] && ExtensionOffersEqual(offers[index], response))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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

    static string[] CopyProtocols(string[] source)
    {
        string[] copy = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static WebSocketExtensionOffer[] CopyExtensionOffers(WebSocketExtensionOffer[] source)
    {
        WebSocketExtensionOffer[] copy = new WebSocketExtensionOffer[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyExtensionOffer(source[index]);
            index = index + 1;
        }

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

    static WebSocketHeaderField CreateField(string name, string value)
    {
        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = name;
        field.Value = value;
        return field;
    }
}
