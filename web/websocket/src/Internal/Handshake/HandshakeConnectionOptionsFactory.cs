using System;

namespace websocket;

internal static class HandshakeConnectionOptionsFactory
{
    internal static WebSocketConnectionOptions CreateOptions(
        WebSocketRole role,
        WebSocketHandshakeResult handshake,
        WebSocketConnectionOptions template)
    {
        if (handshake == null ||
            template == null ||
            handshake.Outcome != WebSocketHandshakeOutcome.Accepted ||
            handshake.HasError)
        {
            throw new WebSocketStateException();
        }

        WebSocketConnectionOptions result = CopyTemplate(template);
        result.Role = role;
        result.Transport = handshake.Transport;
        result.HasSelectedSubprotocol = handshake.HasSelectedSubprotocol;
        result.SelectedSubprotocol = handshake.SelectedSubprotocol;
        result.NegotiatedExtensions = CopyNegotiations(handshake.Extensions);
        WebSocketOptionValidator.ValidateConnectionOptions(result);
        return result;
    }

    static WebSocketConnectionOptions CopyTemplate(WebSocketConnectionOptions source)
    {
        WebSocketConnectionOptions copy = new WebSocketConnectionOptions();
        copy.DeliveryMode = source.DeliveryMode;
        copy.Extensions = CopyExtensions(source.Extensions);
        copy.HasPerMessageDeflate = source.HasPerMessageDeflate;
        copy.PerMessageDeflate = CopyPerMessageDeflate(source.PerMessageDeflate);
        copy.MaximumFramePayloadLength = source.MaximumFramePayloadLength;
        copy.MaximumMessagePayloadLength = source.MaximumMessagePayloadLength;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        copy.MaximumFragmentCount = source.MaximumFragmentCount;
        copy.MaximumBufferedInputBytes = source.MaximumBufferedInputBytes;
        copy.MaximumBufferedOutputBytes = source.MaximumBufferedOutputBytes;
        copy.MaximumQueuedEvents = source.MaximumQueuedEvents;
        copy.AutomaticFramePayloadLength = source.AutomaticFramePayloadLength;
        copy.AutomaticPong = source.AutomaticPong;
        copy.AutomaticCloseResponse = source.AutomaticCloseResponse;
        copy.CloseTimeoutMilliseconds = source.CloseTimeoutMilliseconds;
        copy.Heartbeat = CopyHeartbeat(source.Heartbeat);
        copy.HasMaskKeySource = source.HasMaskKeySource;
        copy.MaskKeySource = source.MaskKeySource;
        return copy;
    }

    static IWebSocketExtension[] CopyExtensions(IWebSocketExtension[] source)
    {
        if (source == null)
        {
            throw new WebSocketStateException();
        }

        IWebSocketExtension[] copy = new IWebSocketExtension[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == null)
            {
                throw new WebSocketStateException();
            }

            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static PerMessageDeflateOptions CopyPerMessageDeflate(PerMessageDeflateOptions source)
    {
        if (source == null)
        {
            throw new WebSocketStateException();
        }

        PerMessageDeflateOptions copy = new PerMessageDeflateOptions();
        copy.OfferServerNoContextTakeover = source.OfferServerNoContextTakeover;
        copy.RequireServerNoContextTakeover = source.RequireServerNoContextTakeover;
        copy.OfferClientNoContextTakeover = source.OfferClientNoContextTakeover;
        copy.RequireClientNoContextTakeover = source.RequireClientNoContextTakeover;
        copy.OfferServerMaxWindowBits = source.OfferServerMaxWindowBits;
        copy.HasServerMaxWindowBits = source.HasServerMaxWindowBits;
        copy.ServerMaxWindowBits = source.ServerMaxWindowBits;
        copy.OfferClientMaxWindowBits = source.OfferClientMaxWindowBits;
        copy.HasClientMaxWindowBits = source.HasClientMaxWindowBits;
        copy.ClientMaxWindowBits = source.ClientMaxWindowBits;
        copy.CompressionThreshold = source.CompressionThreshold;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        return copy;
    }

    static WebSocketHeartbeatOptions CopyHeartbeat(WebSocketHeartbeatOptions source)
    {
        if (source == null)
        {
            throw new WebSocketStateException();
        }

        WebSocketHeartbeatOptions copy = new WebSocketHeartbeatOptions();
        copy.Enabled = source.Enabled;
        copy.IdleIntervalMilliseconds = source.IdleIntervalMilliseconds;
        copy.PongTimeoutMilliseconds = source.PongTimeoutMilliseconds;
        copy.TokenLength = source.TokenLength;
        return copy;
    }

    static WebSocketExtensionNegotiation[] CopyNegotiations(
        WebSocketExtensionNegotiation[] source)
    {
        if (source == null)
        {
            throw new WebSocketStateException();
        }

        WebSocketExtensionNegotiation[] copy =
            new WebSocketExtensionNegotiation[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            ValidateNegotiation(source, index);
            WebSocketExtensionNegotiation negotiation =
                new WebSocketExtensionNegotiation();
            negotiation.Name = source[index].Name;
            negotiation.Status = source[index].Status;
            negotiation.HasAcceptedConfiguration =
                source[index].HasAcceptedConfiguration;
            negotiation.AcceptedConfiguration = CopyOffer(
                source[index].AcceptedConfiguration);
            negotiation.ClaimsRsv1 = source[index].ClaimsRsv1;
            negotiation.ClaimsRsv2 = source[index].ClaimsRsv2;
            negotiation.ClaimsRsv3 = source[index].ClaimsRsv3;
            negotiation.ClaimedOpcodes = CopyOpcodes(source[index].ClaimedOpcodes);
            negotiation.HasError = source[index].HasError;
            negotiation.Error = CopyError(source[index].Error);
            copy[index] = negotiation;
            index = index + 1;
        }

        return copy;
    }

    static void ValidateNegotiation(
        WebSocketExtensionNegotiation[] negotiations,
        int index)
    {
        WebSocketExtensionNegotiation negotiation = negotiations[index];
        if (negotiation == null ||
            negotiation.Name == null ||
            !AsciiToken.IsValid(negotiation.Name) ||
            negotiation.Status != WebSocketExtensionStatus.Negotiated ||
            !negotiation.HasAcceptedConfiguration ||
            negotiation.AcceptedConfiguration == null ||
            negotiation.AcceptedConfiguration.Name != negotiation.Name ||
            negotiation.ClaimedOpcodes == null ||
            negotiation.HasError ||
            negotiation.Error == null)
        {
            throw new WebSocketStateException();
        }

        int comparisonIndex = 0;
        while (comparisonIndex < index)
        {
            if (negotiations[comparisonIndex].Name == negotiation.Name)
            {
                throw new WebSocketStateException();
            }

            comparisonIndex = comparisonIndex + 1;
        }

        ValidateOffer(negotiation.AcceptedConfiguration);
    }

    static void ValidateOffer(WebSocketExtensionOffer offer)
    {
        if (offer.Name == null ||
            !AsciiToken.IsValid(offer.Name) ||
            offer.Parameters == null)
        {
            throw new WebSocketStateException();
        }

        int index = 0;
        while (index < offer.Parameters.Length)
        {
            WebSocketExtensionParameter parameter = offer.Parameters[index];
            if (parameter == null ||
                parameter.Name == null ||
                !AsciiToken.IsValid(parameter.Name) ||
                parameter.Value == null ||
                (!parameter.HasValue && parameter.Value.Length != 0) ||
                (parameter.HasValue && !AsciiToken.IsValid(parameter.Value)))
            {
                throw new WebSocketStateException();
            }

            index = index + 1;
        }
    }

    static WebSocketExtensionOffer CopyOffer(WebSocketExtensionOffer source)
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

    static WebSocketOpcode[] CopyOpcodes(WebSocketOpcode[] source)
    {
        WebSocketOpcode[] copy = new WebSocketOpcode[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static WebSocketError CopyError(WebSocketError source)
    {
        WebSocketError copy = new WebSocketError();
        copy.Kind = source.Kind;
        copy.HasCloseCode = source.HasCloseCode;
        copy.CloseCode = source.CloseCode;
        copy.HasKnownCloseCode = source.HasKnownCloseCode;
        copy.KnownCloseCode = source.KnownCloseCode;
        copy.HasHttpStatusCode = source.HasHttpStatusCode;
        copy.HttpStatusCode = source.HttpStatusCode;
        copy.Identifier = source.Identifier;
        copy.Diagnostic = source.Diagnostic;
        return copy;
    }
}
