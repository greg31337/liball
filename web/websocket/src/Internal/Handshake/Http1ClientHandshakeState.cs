using System;

namespace websocket;

internal class Http1ClientHandshakeState
{
    const string HttpVersion = "HTTP/1.1";
    const string WebSocketFieldValue = "websocket";
    const string UpgradeFieldValue = "Upgrade";
    const string UserAgentFieldName = "User-Agent";
    const string ContentLengthFieldName = "Content-Length";
    const string TransferEncodingFieldName = "Transfer-Encoding";

    internal bool HasPendingRequest;
    internal string ClientKey = "";
    internal string ExpectedAccept = "";
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
        if (options.Transport != WebSocketHandshakeTransport.Http1Upgrade)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketUriValidationResult uri = WebSocketUriValidator.Validate(options.Uri);
        if (!uri.Valid)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketHandshakeValueResult nonce = WebSocketHandshakeCryptography.CreateClientNonce();
        if (nonce.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        WebSocketHandshakeValueResult accept = WebSocketHandshakeCryptography.ComputeAccept(nonce.Value);
        if (accept.Status != WebSocketParseStatus.Complete)
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
            nonce.Value,
            extensionValue.Value,
            clientOffers.Offers.Length != 0);
        Http1WriteResult measurement = Http1Writer.MeasureRequest(request);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            throw new WebSocketHandshakeException();
        }

        string[] protocols = CopyProtocols(options.Subprotocols);
        WebSocketExtensionOffer[] extensions =
            CopyExtensionOffers(clientOffers.Offers);
        HasPendingRequest = true;
        ClientKey = nonce.Value;
        ExpectedAccept = accept.Value;
        OfferedSubprotocols = protocols;
        OfferedExtensions = extensions;
        OfferedExtensionHandlers = clientOffers.Handlers;
        MaximumHeaderBytes = options.MaximumHeaderBytes;
        MaximumHeaderCount = options.MaximumHeaderCount;
        MaximumLineLength = options.MaximumLineLength;

        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        result.HasRequest = true;
        result.Request = request;
        result.HasClientKey = true;
        result.ClientKey = nonce.Value;
        return result;
    }

    internal WebSocketHandshakeResult ValidateResponse(WebSocketHttpResponse response)
    {
        if (!HasPendingRequest)
        {
            throw new WebSocketStateException();
        }

        string retainedKey = ClientKey;
        WebSocketHandshakeResult structuralFailure = ValidateResponseMetadata(response, retainedKey);
        if (structuralFailure.HasError)
        {
            ClearState();
            return structuralFailure;
        }

        WebSocketHttpResponse responseCopy = CopyResponse(response);
        if (response.StatusCode != WebSocketProtocolConstants.Http1SwitchingProtocolsStatus)
        {
            WebSocketHandshakeResult nonUpgrade = CreateResponseResult(
                responseCopy,
                retainedKey,
                response.StatusCode < 200
                    ? WebSocketHandshakeOutcome.Incomplete
                    : WebSocketHandshakeOutcome.Rejected);
            if (response.StatusCode >= 200)
            {
                ClearState();
            }

            return nonUpgrade;
        }

        HeaderTokenSearchResult upgrade = HeaderFieldLookup.ContainsHttpToken(
            response.Fields,
            WebSocketProtocolConstants.UpgradeHeaderName,
            WebSocketFieldValue,
            int.MaxValue);
        if (upgrade.Status != WebSocketParseStatus.Complete ||
            upgrade.FieldCount == 0 ||
            !upgrade.Found)
        {
            return FailResponse(
                responseCopy,
                retainedKey,
                "websocket-response-upgrade-invalid",
                "The HTTP Upgrade field does not select WebSocket.");
        }

        HeaderTokenSearchResult connection = HeaderFieldLookup.ContainsHttpToken(
            response.Fields,
            WebSocketProtocolConstants.ConnectionHeaderName,
            UpgradeFieldValue,
            int.MaxValue);
        if (connection.Status != WebSocketParseStatus.Complete ||
            connection.FieldCount == 0 ||
            !connection.Found)
        {
            return FailResponse(
                responseCopy,
                retainedKey,
                "websocket-response-connection-invalid",
                "The HTTP Connection field does not contain Upgrade.");
        }

        HeaderFieldLookupResult accepts = HeaderFieldLookup.FindFields(
            response.Fields,
            WebSocketProtocolConstants.AcceptHeaderName);
        if (accepts.Status != WebSocketParseStatus.Complete ||
            accepts.Matches.Length != 1 ||
            accepts.Matches[0].Value != ExpectedAccept)
        {
            return FailResponse(
                responseCopy,
                retainedKey,
                "websocket-response-accept-invalid",
                "The Sec-WebSocket-Accept field does not match the client key.");
        }

        WebSocketSubprotocolSelectionResult subprotocol = ValidateSelectedSubprotocol(response.Fields);
        if (subprotocol.Status != WebSocketParseStatus.Complete)
        {
            return FailResponse(
                responseCopy,
                retainedKey,
                "websocket-response-subprotocol-invalid",
                "The selected WebSocket subprotocol was not offered exactly once.");
        }

        WebSocketExtensionNegotiation[]? extensions = ValidateResponseExtensions(response.Fields);
        if (extensions == null)
        {
            return FailResponse(
                responseCopy,
                retainedKey,
                "websocket-response-extensions-invalid",
                "A negotiated WebSocket extension configuration was not offered.");
        }

        WebSocketHandshakeResult result = CreateResponseResult(
            responseCopy,
            retainedKey,
            WebSocketHandshakeOutcome.Accepted);
        result.HasSelectedSubprotocol = subprotocol.HasSelection;
        result.SelectedSubprotocol = subprotocol.SelectedProtocol;
        result.Extensions = extensions;
        ClearState();
        return result;
    }

    void ClearState()
    {
        HasPendingRequest = false;
        ClientKey = "";
        ExpectedAccept = "";
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
            WebSocketProtocolConstants.ProtocolHeaderName);
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
        if (!AsciiToken.IsValid(selected) ||
            !ContainsProtocol(OfferedSubprotocols, selected))
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
            WebSocketProtocolConstants.ExtensionsHeaderName);
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
            if (matchedIndex < 0 || ContainsNegotiatedName(negotiations, responseIndex, responseExtension.Name))
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

    WebSocketHandshakeResult FailResponse(
        WebSocketHttpResponse response,
        string retainedKey,
        string identifier,
        string diagnostic)
    {
        ClearState();
        return CreateFailure(response, retainedKey, identifier, diagnostic);
    }

    static WebSocketHttpRequest CreateRequestMetadata(
        WebSocketClientHandshakeOptions options,
        WebSocketUriValidationResult uri,
        string clientKey,
        string extensionValue,
        bool hasExtensions)
    {
        int fieldCount = 5 + options.ExtraFields.Length;
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
        fields[fieldIndex] = CreateField(WebSocketProtocolConstants.HostHeaderName, uri.Authority);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.UpgradeHeaderName,
            WebSocketFieldValue);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.ConnectionHeaderName,
            UpgradeFieldValue);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(WebSocketProtocolConstants.KeyHeaderName, clientKey);
        fieldIndex = fieldIndex + 1;
        fields[fieldIndex] = CreateField(
            WebSocketProtocolConstants.VersionHeaderName,
            WebSocketProtocolConstants.VersionText);
        fieldIndex = fieldIndex + 1;

        if (options.HasOrigin)
        {
            WebSocketOriginParseResult origin = WebSocketOriginParser.ParseOrigin(options.Origin);
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.OriginHeaderName,
                origin.SerializedOrigin);
            fieldIndex = fieldIndex + 1;
        }

        if (options.Subprotocols.Length != 0)
        {
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.ProtocolHeaderName,
                string.Join(", ", options.Subprotocols));
            fieldIndex = fieldIndex + 1;
        }

        if (hasExtensions)
        {
            fields[fieldIndex] = CreateField(
                WebSocketProtocolConstants.ExtensionsHeaderName,
                extensionValue);
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
            WebSocketHeaderField extra = options.ExtraFields[extraIndex];
            fields[fieldIndex] = CreateField(extra.Name, extra.Value);
            fieldIndex = fieldIndex + 1;
            extraIndex = extraIndex + 1;
        }

        WebSocketHttpRequest request = new WebSocketHttpRequest();
        request.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        request.Method = WebSocketProtocolConstants.Http1RequestMethod;
        request.HasRequestTarget = true;
        request.RequestTarget = uri.RequestTarget;
        request.HasHttpVersion = true;
        request.HttpVersion = HttpVersion;
        request.Fields = fields;
        return request;
    }

    static void ValidateExtraFields(WebSocketHeaderField[] fields, bool hasUserAgent)
    {
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value) ||
                HttpFieldValue.TrimOptionalWhitespace(field.Value) != field.Value ||
                IsCriticalField(field.Name) ||
                (hasUserAgent && AsciiToken.EqualsIgnoreCase(field.Name, UserAgentFieldName)))
            {
                throw new WebSocketHandshakeException();
            }

            fieldIndex = fieldIndex + 1;
        }
    }

    static bool IsCriticalField(string fieldName)
    {
        return AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.HostHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.UpgradeHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.ConnectionHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.OriginHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.KeyHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.AcceptHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.VersionHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.ProtocolHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, WebSocketProtocolConstants.ExtensionsHeaderName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, ContentLengthFieldName) ||
            AsciiToken.EqualsIgnoreCase(fieldName, TransferEncodingFieldName);
    }

    static WebSocketHeaderField CreateField(string name, string value)
    {
        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = name;
        field.Value = value;
        return field;
    }

    static WebSocketHandshakeResult ValidateResponseMetadata(
        WebSocketHttpResponse response,
        string retainedKey)
    {
        if (response == null ||
            response.Transport != WebSocketHandshakeTransport.Http1Upgrade ||
            response.StatusCode < 100 ||
            response.StatusCode > 599 ||
            response.ReasonPhrase == null ||
            response.Fields == null ||
            response.Body == null ||
            !HttpFieldValue.IsSafe(response.ReasonPhrase) ||
            (response.HasReasonPhrase && response.ReasonPhrase.Length == 0) ||
            (!response.HasReasonPhrase && response.ReasonPhrase.Length != 0) ||
            (!response.HasBody && response.Body.Length != 0) ||
            (response.StatusCode < 200 && response.HasBody))
        {
            return CreateStructuralFailure(retainedKey);
        }

        int fieldIndex = 0;
        int contentLengthCount = 0;
        while (fieldIndex < response.Fields.Length)
        {
            WebSocketHeaderField field = response.Fields[fieldIndex];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value) ||
                HttpFieldValue.TrimOptionalWhitespace(field.Value) != field.Value)
            {
                return CreateStructuralFailure(retainedKey);
            }

            if (response.StatusCode == WebSocketProtocolConstants.Http1SwitchingProtocolsStatus)
            {
                if (AsciiToken.EqualsIgnoreCase(field.Name, TransferEncodingFieldName) ||
                    AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.HostHeaderName) ||
                    AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.KeyHeaderName) ||
                    AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.OriginHeaderName) ||
                    AsciiToken.EqualsIgnoreCase(field.Name, WebSocketProtocolConstants.VersionHeaderName))
                {
                    return CreateStructuralFailure(retainedKey);
                }

                if (AsciiToken.EqualsIgnoreCase(field.Name, ContentLengthFieldName))
                {
                    contentLengthCount = contentLengthCount + 1;
                    if (contentLengthCount > 1 || field.Value != "0")
                    {
                        return CreateStructuralFailure(retainedKey);
                    }
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return new WebSocketHandshakeResult();
    }

    static WebSocketHandshakeResult CreateStructuralFailure(string retainedKey)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = WebSocketHandshakeOutcome.Failed;
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        result.HasClientKey = true;
        result.ClientKey = retainedKey;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = "websocket-response-metadata-invalid";
        result.Error.Diagnostic = "The HTTP/1 WebSocket response metadata is invalid or unsafe.";
        return result;
    }

    static WebSocketHandshakeResult CreateResponseResult(
        WebSocketHttpResponse response,
        string retainedKey,
        WebSocketHandshakeOutcome outcome)
    {
        WebSocketHandshakeResult result = new WebSocketHandshakeResult();
        result.Outcome = outcome;
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        result.HasResponse = true;
        result.Response = response;
        result.HasClientKey = true;
        result.ClientKey = retainedKey;
        return result;
    }

    static WebSocketHandshakeResult CreateFailure(
        WebSocketHttpResponse response,
        string retainedKey,
        string identifier,
        string diagnostic)
    {
        WebSocketHandshakeResult result = CreateResponseResult(
            response,
            retainedKey,
            WebSocketHandshakeOutcome.Failed);
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
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
            WebSocketHeaderField field = source.Fields[fieldIndex];
            fields[fieldIndex] = CreateField(field.Name, field.Value);
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
        int offerIndex = 0;
        while (offerIndex < offers.Length)
        {
            if (!usedOffers[offerIndex] && ExtensionOffersEqual(offers[offerIndex], response))
            {
                return offerIndex;
            }

            offerIndex = offerIndex + 1;
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

        int parameterIndex = 0;
        while (parameterIndex < left.Parameters.Length)
        {
            WebSocketExtensionParameter leftParameter = left.Parameters[parameterIndex];
            WebSocketExtensionParameter rightParameter = right.Parameters[parameterIndex];
            if (leftParameter.Name != rightParameter.Name ||
                leftParameter.HasValue != rightParameter.HasValue ||
                leftParameter.Value != rightParameter.Value)
            {
                return false;
            }

            parameterIndex = parameterIndex + 1;
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

    static WebSocketExtensionOffer CopyExtensionOffer(WebSocketExtensionOffer source)
    {
        WebSocketExtensionOffer copy = new WebSocketExtensionOffer();
        copy.Name = source.Name;
        copy.Parameters = CopyExtensionParameters(source.Parameters);
        return copy;
    }

    static string[] CopyProtocols(string[] protocols)
    {
        string[] copy = new string[protocols.Length];
        int index = 0;
        while (index < protocols.Length)
        {
            copy[index] = protocols[index];
            index = index + 1;
        }

        return copy;
    }

    static WebSocketExtensionOffer[] CopyExtensionOffers(WebSocketExtensionOffer[] offers)
    {
        WebSocketExtensionOffer[] copy = new WebSocketExtensionOffer[offers.Length];
        int offerIndex = 0;
        while (offerIndex < offers.Length)
        {
            WebSocketExtensionOffer source = offers[offerIndex];
            WebSocketExtensionOffer destination = new WebSocketExtensionOffer();
            destination.Name = source.Name;
            destination.Parameters = CopyExtensionParameters(source.Parameters);
            copy[offerIndex] = destination;
            offerIndex = offerIndex + 1;
        }

        return copy;
    }

    static WebSocketExtensionParameter[] CopyExtensionParameters(
        WebSocketExtensionParameter[] parameters)
    {
        WebSocketExtensionParameter[] copy = new WebSocketExtensionParameter[parameters.Length];
        int parameterIndex = 0;
        while (parameterIndex < parameters.Length)
        {
            WebSocketExtensionParameter source = parameters[parameterIndex];
            WebSocketExtensionParameter destination = new WebSocketExtensionParameter();
            destination.Name = source.Name;
            destination.HasValue = source.HasValue;
            destination.Value = source.Value;
            copy[parameterIndex] = destination;
            parameterIndex = parameterIndex + 1;
        }

        return copy;
    }
}
