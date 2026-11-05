namespace websocket;

internal static class WebSocketOptionValidator
{
    internal static void ValidateConnectionOptions(WebSocketConnectionOptions? options)
    {
        if (options == null)
        {
            throw new WebSocketStateException();
        }

        if (options.Role != WebSocketRole.Client && options.Role != WebSocketRole.Server)
        {
            throw new WebSocketStateException();
        }

        if (!IsKnownTransport(options.Transport))
        {
            throw new WebSocketStateException();
        }

        if (options.DeliveryMode != WebSocketMessageDeliveryMode.Reassembled &&
            options.DeliveryMode != WebSocketMessageDeliveryMode.Streaming)
        {
            throw new WebSocketStateException();
        }

        if (options.SelectedSubprotocol == null)
        {
            throw new WebSocketStateException();
        }

        if (options.HasSelectedSubprotocol)
        {
            if (options.SelectedSubprotocol.Length == 0)
            {
                throw new WebSocketStateException();
            }
        }
        else if (options.SelectedSubprotocol.Length != 0)
        {
            throw new WebSocketStateException();
        }

        if (options.Extensions == null)
        {
            throw new WebSocketStateException();
        }

        int extensionIndex = 0;
        while (extensionIndex < options.Extensions.Length)
        {
            if (options.Extensions[extensionIndex] == null)
            {
                throw new WebSocketStateException();
            }

            int priorExtensionIndex = 0;
            while (priorExtensionIndex < extensionIndex)
            {
                if (object.ReferenceEquals(
                    options.Extensions[priorExtensionIndex],
                    options.Extensions[extensionIndex]))
                {
                    throw new WebSocketStateException();
                }

                priorExtensionIndex = priorExtensionIndex + 1;
            }

            extensionIndex = extensionIndex + 1;
        }

        if (options.NegotiatedExtensions == null ||
            options.NegotiatedExtensions.Length != options.Extensions.Length)
        {
            throw new WebSocketStateException();
        }

        int negotiationIndex = 0;
        while (negotiationIndex < options.NegotiatedExtensions.Length)
        {
            WebSocketExtensionNegotiation negotiation =
                options.NegotiatedExtensions[negotiationIndex];
            if (negotiation == null ||
                negotiation.Status != WebSocketExtensionStatus.Negotiated ||
                !negotiation.HasAcceptedConfiguration ||
                negotiation.HasError ||
                !IsValidNegotiatedExtension(negotiation))
            {
                throw new WebSocketStateException();
            }

            int priorNegotiationIndex = 0;
            while (priorNegotiationIndex < negotiationIndex)
            {
                if (options.NegotiatedExtensions[priorNegotiationIndex].Name ==
                    negotiation.Name)
                {
                    throw new WebSocketStateException();
                }

                priorNegotiationIndex = priorNegotiationIndex + 1;
            }

            negotiationIndex = negotiationIndex + 1;
        }

        if (!WebSocketExtensionClaimValidator.ValidateClaims(
            options.NegotiatedExtensions))
        {
            throw new WebSocketStateException();
        }

        if (options.PerMessageDeflate == null)
        {
            throw new WebSocketStateException();
        }

        if (options.HasPerMessageDeflate)
        {
            ValidatePerMessageDeflateOptions(options.PerMessageDeflate);
        }

        if (options.MaximumFramePayloadLength <= 0 ||
            options.MaximumMessagePayloadLength <= 0 ||
            options.MaximumDecompressedMessagePayloadLength <= 0 ||
            options.MaximumFragmentCount <= 0 ||
            options.MaximumBufferedInputBytes <= 0 ||
            options.MaximumBufferedOutputBytes <= 0 ||
            options.MaximumQueuedEvents <= 0 ||
            options.AutomaticFramePayloadLength <= 0 ||
            options.CloseTimeoutMilliseconds <= 0)
        {
            throw new WebSocketLimitException();
        }

        if (options.MaximumFramePayloadLength > options.MaximumMessagePayloadLength)
        {
            throw new WebSocketLimitException();
        }

        if (options.AutomaticFramePayloadLength > options.MaximumFramePayloadLength)
        {
            throw new WebSocketLimitException();
        }

        if (options.MaximumBufferedInputBytes < WebSocketProtocolConstants.MaximumFrameHeaderLength ||
            options.MaximumBufferedOutputBytes < WebSocketProtocolConstants.MaximumFrameHeaderLength)
        {
            throw new WebSocketLimitException();
        }

        if (options.AutomaticFramePayloadLength >
            options.MaximumBufferedOutputBytes - WebSocketProtocolConstants.MaximumFrameHeaderLength)
        {
            throw new WebSocketLimitException();
        }

        int requiredFragments = options.MaximumMessagePayloadLength / options.AutomaticFramePayloadLength;
        if (options.MaximumMessagePayloadLength % options.AutomaticFramePayloadLength != 0)
        {
            requiredFragments = requiredFragments + 1;
        }

        if (requiredFragments > options.MaximumFragmentCount)
        {
            throw new WebSocketLimitException();
        }

        if (options.Heartbeat == null)
        {
            throw new WebSocketStateException();
        }

        if (options.Heartbeat.IdleIntervalMilliseconds <= 0 ||
            options.Heartbeat.PongTimeoutMilliseconds <= 0 ||
            options.Heartbeat.TokenLength <= 0 ||
            options.Heartbeat.TokenLength > WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            throw new WebSocketLimitException();
        }

        if (options.HasMaskKeySource)
        {
            if (options.MaskKeySource == null || options.Role != WebSocketRole.Client)
            {
                throw new WebSocketStateException();
            }
        }
        else if (options.MaskKeySource != null)
        {
            throw new WebSocketStateException();
        }
    }

    internal static void ValidateClientHandshakeOptions(WebSocketClientHandshakeOptions? options)
    {
        if (options == null)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateHeaderLimits(options.MaximumHeaderBytes, options.MaximumHeaderCount, options.MaximumLineLength);

        if (options.Uri == null || options.Origin == null || options.UserAgent == null)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.Uri.Length == 0 || !IsKnownTransport(options.Transport))
        {
            throw new WebSocketHandshakeException();
        }

        if (options.Transport != WebSocketHandshakeTransport.Http1Upgrade && !options.PeerEnabledExtendedConnect)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.HasOrigin)
        {
            WebSocketOriginParseResult origin = WebSocketOriginParser.ParseOrigin(options.Origin);
            if (origin.Status != WebSocketParseStatus.Complete)
            {
                throw new WebSocketHandshakeException();
            }
        }
        else if (options.Origin.Length != 0)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.HasUserAgent)
        {
            if (options.UserAgent.Length == 0)
            {
                throw new WebSocketHandshakeException();
            }
        }
        else if (options.UserAgent.Length != 0)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateProtocolArray(options.Subprotocols);
        ValidateExtensionOfferArray(options.Extensions);
        ValidateExtensionHandlerArray(options.ExtensionHandlers);
        if (options.Extensions.Length != 0 && options.ExtensionHandlers.Length != 0)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateHeaderFieldArray(options.ExtraFields);
    }

    internal static void ValidateServerHandshakeOptions(WebSocketServerHandshakeOptions? options)
    {
        if (options == null)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateHeaderLimits(options.MaximumHeaderBytes, options.MaximumHeaderCount, options.MaximumLineLength);

        if (options.SupportedTransports == null || options.SupportedTransports.Length == 0)
        {
            throw new WebSocketHandshakeException();
        }

        int transportIndex = 0;
        while (transportIndex < options.SupportedTransports.Length)
        {
            WebSocketHandshakeTransport transport = options.SupportedTransports[transportIndex];
            if (!IsKnownTransport(transport))
            {
                throw new WebSocketHandshakeException();
            }

            if (transport != WebSocketHandshakeTransport.Http1Upgrade && !options.ExtendedConnectEnabled)
            {
                throw new WebSocketHandshakeException();
            }

            int comparisonIndex = transportIndex + 1;
            while (comparisonIndex < options.SupportedTransports.Length)
            {
                if (transport == options.SupportedTransports[comparisonIndex])
                {
                    throw new WebSocketHandshakeException();
                }

                comparisonIndex = comparisonIndex + 1;
            }

            transportIndex = transportIndex + 1;
        }

        if (options.HasOriginPolicy)
        {
            if (options.OriginPolicy == null)
            {
                throw new WebSocketHandshakeException();
            }
        }
        else if (options.OriginPolicy != null)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.HasSubprotocolSelector)
        {
            if (options.SubprotocolSelector == null)
            {
                throw new WebSocketHandshakeException();
            }
        }
        else if (options.SubprotocolSelector != null)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.RejectionStatusCode < 400 || options.RejectionStatusCode > 599)
        {
            throw new WebSocketHandshakeException();
        }

        if (options.RejectionBody == null)
        {
            throw new WebSocketHandshakeException();
        }

        if (!options.HasRejectionBody && options.RejectionBody.Length != 0)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateProtocolArray(options.SupportedSubprotocols);
        ValidateExtensionOfferArray(options.SupportedExtensions);
        ValidateExtensionHandlerArray(options.ExtensionHandlers);
        if (options.SupportedExtensions.Length != 0 &&
            options.ExtensionHandlers.Length != 0)
        {
            throw new WebSocketHandshakeException();
        }

        ValidateHeaderFieldArray(options.ExtraFields);
        ValidateHeaderFieldArray(options.RejectionFields);
    }

    internal static void ValidatePerMessageDeflateOptions(PerMessageDeflateOptions? options)
    {
        if (options == null)
        {
            throw new WebSocketExtensionException();
        }

        if (options.RequireServerNoContextTakeover && !options.OfferServerNoContextTakeover)
        {
            throw new WebSocketExtensionException();
        }

        if (options.RequireClientNoContextTakeover && !options.OfferClientNoContextTakeover)
        {
            throw new WebSocketExtensionException();
        }

        if (options.HasServerMaxWindowBits)
        {
            if (!options.OfferServerMaxWindowBits ||
                options.ServerMaxWindowBits < 8 ||
                options.ServerMaxWindowBits > 15)
            {
                throw new WebSocketExtensionException();
            }
        }

        if (options.HasClientMaxWindowBits)
        {
            if (!options.OfferClientMaxWindowBits ||
                options.ClientMaxWindowBits < 8 ||
                options.ClientMaxWindowBits > 15)
            {
                throw new WebSocketExtensionException();
            }
        }

        if (options.CompressionThreshold < 0 || options.MaximumDecompressedMessagePayloadLength <= 0)
        {
            throw new WebSocketExtensionException();
        }
    }

    static void ValidateHeaderLimits(int maximumHeaderBytes, int maximumHeaderCount, int maximumLineLength)
    {
        if (maximumHeaderBytes <= 0 || maximumHeaderCount <= 0 || maximumLineLength <= 0)
        {
            throw new WebSocketLimitException();
        }

        if (maximumLineLength > maximumHeaderBytes)
        {
            throw new WebSocketLimitException();
        }
    }

    static bool IsValidNegotiatedExtension(
        WebSocketExtensionNegotiation negotiation)
    {
        if (negotiation.Name == null ||
            !AsciiToken.IsValid(negotiation.Name) ||
            negotiation.AcceptedConfiguration == null ||
            negotiation.AcceptedConfiguration.Name != negotiation.Name ||
            negotiation.AcceptedConfiguration.Parameters == null ||
            negotiation.ClaimedOpcodes == null ||
            negotiation.Error == null)
        {
            return false;
        }

        int parameterIndex = 0;
        while (parameterIndex <
            negotiation.AcceptedConfiguration.Parameters.Length)
        {
            WebSocketExtensionParameter parameter =
                negotiation.AcceptedConfiguration.Parameters[parameterIndex];
            if (parameter == null ||
                parameter.Name == null ||
                !AsciiToken.IsValid(parameter.Name) ||
                parameter.Value == null ||
                (!parameter.HasValue && parameter.Value.Length != 0) ||
                (parameter.HasValue && !AsciiToken.IsValid(parameter.Value)))
            {
                return false;
            }

            parameterIndex = parameterIndex + 1;
        }

        return true;
    }

    static void ValidateProtocolArray(string[] protocols)
    {
        if (!WebSocketSubprotocolNegotiator.ValidateProtocols(protocols))
        {
            throw new WebSocketHandshakeException();
        }
    }

    static void ValidateExtensionOfferArray(WebSocketExtensionOffer[] offers)
    {
        if (offers == null)
        {
            throw new WebSocketHandshakeException();
        }

        foreach (WebSocketExtensionOffer offer in offers)
        {
            if (offer == null ||
                offer.Name == null ||
                !AsciiToken.IsValid(offer.Name) ||
                offer.Parameters == null)
            {
                throw new WebSocketHandshakeException();
            }

            foreach (WebSocketExtensionParameter parameter in offer.Parameters)
            {
                if (parameter == null ||
                    parameter.Name == null ||
                    !AsciiToken.IsValid(parameter.Name) ||
                    parameter.Value == null)
                {
                    throw new WebSocketHandshakeException();
                }

                if (!parameter.HasValue && parameter.Value.Length != 0)
                {
                    throw new WebSocketHandshakeException();
                }

                if (parameter.HasValue && !AsciiToken.IsValid(parameter.Value))
                {
                    throw new WebSocketHandshakeException();
                }
            }
        }
    }

    static void ValidateExtensionHandlerArray(IWebSocketExtension[] extensions)
    {
        if (extensions == null)
        {
            throw new WebSocketHandshakeException();
        }

        int index = 0;
        while (index < extensions.Length)
        {
            if (extensions[index] == null)
            {
                throw new WebSocketHandshakeException();
            }

            index = index + 1;
        }
    }

    static void ValidateHeaderFieldArray(WebSocketHeaderField[] fields)
    {
        if (fields == null)
        {
            throw new WebSocketHandshakeException();
        }

        foreach (WebSocketHeaderField field in fields)
        {
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value) ||
                HttpFieldValue.TrimOptionalWhitespace(field.Value) != field.Value)
            {
                throw new WebSocketHandshakeException();
            }
        }
    }

    static bool IsKnownTransport(WebSocketHandshakeTransport transport)
    {
        switch (transport)
        {
            case WebSocketHandshakeTransport.Http1Upgrade:
                return true;
            case WebSocketHandshakeTransport.Http2ExtendedConnect:
                return true;
            case WebSocketHandshakeTransport.Http3ExtendedConnect:
                return true;
            default:
                return false;
        }
    }
}
