using System;

namespace websocket;

/// <summary>
/// Implements RFC 7692 <c>permessage-deflate</c> negotiation and incremental message transforms.
/// </summary>
public class PerMessageDeflateExtension : IWebSocketExtension
{
    const int MaximumTransformChunkLength = 8192;

    PerMessageDeflateOptions Options;
    WebSocketExtensionOffer[] ClientOffers = [];
    WebSocketExtensionOffer AcceptedConfiguration = new WebSocketExtensionOffer();
    bool Negotiated;
    WebSocketRole NegotiatedRole;
    PerMessageDeflateMessageState MessageState;
    bool TransformInitialized;
    int EffectiveDecompressedLimit;
    bool OutgoingCallActive;
    bool OutgoingFinishing;
    bool OutgoingRsv1Pending;
    bool IncomingCallActive;
    bool IncomingFinishing;

    /// <summary>
    /// Initializes a per-message DEFLATE extension with copied negotiation and resource options.
    /// </summary>
    /// <param name="options">The extension negotiation and resource options.</param>
    /// <exception cref="WebSocketExtensionException">The options are invalid.</exception>
    public PerMessageDeflateExtension(PerMessageDeflateOptions options)
    {
        WebSocketOptionValidator.ValidatePerMessageDeflateOptions(options);
        Options = CopyOptions(options);
        PerMessageDeflateDirectionSettings settings =
            new PerMessageDeflateDirectionSettings();
        settings.Valid = true;
        PerMessageDeflateCodec codec = new PerMessageDeflateCodec(
            settings,
            Options.MaximumDecompressedMessagePayloadLength);
        MessageState = new PerMessageDeflateMessageState(
            codec,
            Options.CompressionThreshold);
    }

    /// <summary>
    /// Creates and retains this extension's client offer.
    /// </summary>
    /// <returns>The <c>permessage-deflate</c> client offer.</returns>
    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        WebSocketExtensionOffer offer =
            PerMessageDeflateNegotiator.CreateClientOffer(Options);
        ClientOffers = [WebSocketExtensionNegotiator.CopyOffer(offer)];
        ClearNegotiatedState();
        return WebSocketExtensionNegotiator.CopyOffer(offer);
    }

    /// <summary>
    /// Evaluates a client offer and retains an accepted server configuration.
    /// </summary>
    /// <param name="offer">The client extension offer.</param>
    /// <returns>The server negotiation result.</returns>
    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(
        WebSocketExtensionOffer offer)
    {
        WebSocketExtensionNegotiation result =
            PerMessageDeflateNegotiator.AcceptServerOffer(offer, Options);
        if (result.Status == WebSocketExtensionStatus.Negotiated &&
            result.HasAcceptedConfiguration)
        {
            RetainNegotiation(result.AcceptedConfiguration, WebSocketRole.Server);
        }
        else
        {
            ClearNegotiatedState();
        }

        return result;
    }

    /// <summary>
    /// Validates a server response against the retained client offer.
    /// </summary>
    /// <param name="response">The server extension response.</param>
    /// <returns>The client validation result.</returns>
    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(
        WebSocketExtensionOffer response)
    {
        WebSocketExtensionNegotiation result =
            PerMessageDeflateNegotiator.ValidateServerResponse(
                response,
                ClientOffers,
                Options);
        if (result.Status == WebSocketExtensionStatus.Negotiated &&
            result.HasAcceptedConfiguration)
        {
            RetainNegotiation(result.AcceptedConfiguration, WebSocketRole.Client);
        }
        else
        {
            ClearNegotiatedState();
        }

        return result;
    }

    /// <summary>
    /// Incrementally compresses or copies an outbound message chunk.
    /// </summary>
    /// <param name="context">The outbound transform context.</param>
    /// <param name="source">The source message bytes.</param>
    /// <returns>The incremental transform result.</returns>
    public WebSocketTransformResult TransformOutboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        WebSocketTransformResult invalid = ValidateTransformContext(context, true);
        if (invalid.HasError)
        {
            return invalid;
        }

        PerMessageDeflateMessageState state = MessageState;
        if (!OutgoingCallActive)
        {
            PerMessageDeflateMessageResult began;
            if (context.FirstChunk)
            {
                int messageLength = source.Length;
                if (!context.FinalChunk &&
                    messageLength < Options.CompressionThreshold)
                {
                    messageLength = Options.CompressionThreshold;
                }

                began = state.BeginOutgoingMessage(
                    context.MessageType,
                    context.RequestCompression,
                    messageLength,
                    context.FinalChunk);
            }
            else
            {
                began = state.BeginOutgoingContinuation(context.FinalChunk);
            }

            if (IsFailureStatus(began.Status))
            {
                return CreateFailure(began.Status, began.ErrorIdentifier);
            }

            OutgoingCallActive = true;
            OutgoingFinishing = false;
            OutgoingRsv1Pending = began.SetRsv1;
        }

        byte[] output = CreateOutputBuffer(context.MaximumBufferedOutputBytes);
        int consumedCount = 0;
        int producedCount = 0;
        if (!OutgoingFinishing)
        {
            PerMessageDeflateMessageResult transformed =
                state.TransformOutgoingPayload(source, output);
            consumedCount = transformed.ConsumedCount;
            producedCount = transformed.ProducedCount;
            if (IsFailureStatus(transformed.Status))
            {
                return CreateFailure(
                    transformed.Status,
                    transformed.ErrorIdentifier);
            }

            if (transformed.Status == WebSocketParseStatus.NeedOutputCapacity)
            {
                return CreateSuccess(
                    transformed.Status,
                    consumedCount,
                    producedCount,
                    output,
                    TakeOutgoingRsv1());
            }

            OutgoingFinishing = true;
        }

        PerMessageDeflateMessageResult finished = state.FinishOutgoingFrame(
            output.AsSpan(producedCount));
        producedCount = producedCount + finished.ProducedCount;
        if (IsFailureStatus(finished.Status))
        {
            return CreateFailure(finished.Status, finished.ErrorIdentifier);
        }

        if (finished.Status == WebSocketParseStatus.Complete)
        {
            OutgoingCallActive = false;
            OutgoingFinishing = false;
        }

        return CreateSuccess(
            finished.Status,
            consumedCount,
            producedCount,
            output,
            TakeOutgoingRsv1());
    }

    /// <summary>
    /// Incrementally decompresses or copies an inbound message chunk.
    /// </summary>
    /// <param name="context">The inbound transform context.</param>
    /// <param name="source">The source message bytes.</param>
    /// <returns>The incremental transform result.</returns>
    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        WebSocketTransformResult invalid = ValidateTransformContext(context, false);
        if (invalid.HasError)
        {
            return invalid;
        }

        PerMessageDeflateMessageState state = MessageState;
        if (!IncomingCallActive)
        {
            if (!context.FirstFragment && context.Rsv1)
            {
                return CreateFailure(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-continuation-rsv1-invalid");
            }

            WebSocketOpcode opcode = WebSocketOpcode.Continuation;
            if (context.FirstChunk)
            {
                opcode = WebSocketOpcode.Binary;
                if (context.MessageType == WebSocketMessageType.Text)
                {
                    opcode = WebSocketOpcode.Text;
                }
            }

            PerMessageDeflateMessageResult began =
                state.BeginIncomingDataFrame(
                    opcode,
                    context.FirstChunk && context.Rsv1,
                    context.FinalChunk);
            if (IsFailureStatus(began.Status))
            {
                return CreateFailure(began.Status, began.ErrorIdentifier);
            }

            IncomingCallActive = true;
            IncomingFinishing = false;
        }

        byte[] output = CreateOutputBuffer(context.MaximumBufferedOutputBytes);
        int consumedCount = 0;
        int producedCount = 0;
        if (!IncomingFinishing)
        {
            PerMessageDeflateMessageResult transformed =
                state.TransformIncomingPayload(source, output);
            consumedCount = transformed.ConsumedCount;
            producedCount = transformed.ProducedCount;
            if (IsFailureStatus(transformed.Status))
            {
                return CreateFailure(
                    transformed.Status,
                    transformed.ErrorIdentifier);
            }

            if (transformed.Status == WebSocketParseStatus.NeedOutputCapacity)
            {
                return CreateSuccess(
                    transformed.Status,
                    consumedCount,
                    producedCount,
                    output,
                    false);
            }

            IncomingFinishing = true;
        }

        PerMessageDeflateMessageResult finished = state.FinishIncomingFrame(
            output.AsSpan(producedCount));
        producedCount = producedCount + finished.ProducedCount;
        if (IsFailureStatus(finished.Status))
        {
            return CreateFailure(finished.Status, finished.ErrorIdentifier);
        }

        if (finished.Status == WebSocketParseStatus.Complete)
        {
            IncomingCallActive = false;
            IncomingFinishing = false;
        }

        return CreateSuccess(
            finished.Status,
            consumedCount,
            producedCount,
            output,
            false);
    }

    /// <summary>
    /// Completes bookkeeping after the final transformed chunk of a message.
    /// </summary>
    /// <param name="context">The final message context.</param>
    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
        if (context == null ||
            !context.FinalChunk ||
            (context.Outbound && OutgoingCallActive) ||
            (!context.Outbound && IncomingCallActive))
        {
            return;
        }

        OutgoingRsv1Pending = false;
    }

    /// <summary>
    /// Resets negotiation, compression history, pending transforms, and retained offers.
    /// </summary>
    public void ResetExtensionState()
    {
        MessageState.ResetPerMessageDeflateMessageState();
        ClientOffers = [];
        ClearNegotiatedState();
    }

    WebSocketTransformResult ValidateTransformContext(
        WebSocketExtensionContext context,
        bool outbound)
    {
        if (context == null ||
            !Negotiated ||
            context.Role != NegotiatedRole ||
            context.Outbound != outbound ||
            (context.MessageType != WebSocketMessageType.Text &&
                context.MessageType != WebSocketMessageType.Binary) ||
            context.MaximumBufferedOutputBytes <= 0 ||
            context.MaximumDecompressedMessagePayloadLength <= 0)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-transform-context-invalid");
        }

        int effectiveLimit = Options.MaximumDecompressedMessagePayloadLength;
        if (context.MaximumDecompressedMessagePayloadLength < effectiveLimit)
        {
            effectiveLimit = context.MaximumDecompressedMessagePayloadLength;
        }

        if (!TransformInitialized)
        {
            PerMessageDeflateDirectionSettings settings =
                PerMessageDeflateNegotiator.CreateDirectionSettings(
                    context.Role,
                    AcceptedConfiguration);
            if (!settings.Valid)
            {
                return CreateFailure(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-negotiation-state-invalid");
            }

            PerMessageDeflateCodec codec = new PerMessageDeflateCodec(
                settings,
                effectiveLimit);
            MessageState = new PerMessageDeflateMessageState(
                codec,
                Options.CompressionThreshold);
            EffectiveDecompressedLimit = effectiveLimit;
            TransformInitialized = true;
        }
        else if (effectiveLimit != EffectiveDecompressedLimit)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-transform-limits-changed");
        }

        return new WebSocketTransformResult();
    }

    void RetainNegotiation(
        WebSocketExtensionOffer configuration,
        WebSocketRole role)
    {
        ClearTransformState();
        AcceptedConfiguration =
            WebSocketExtensionNegotiator.CopyOffer(configuration);
        NegotiatedRole = role;
        Negotiated = true;
    }

    void ClearNegotiatedState()
    {
        ClearTransformState();
        AcceptedConfiguration = new WebSocketExtensionOffer();
        Negotiated = false;
        NegotiatedRole = WebSocketRole.Client;
    }

    void ClearTransformState()
    {
        MessageState.ResetPerMessageDeflateMessageState();
        TransformInitialized = false;
        EffectiveDecompressedLimit = 0;
        OutgoingCallActive = false;
        OutgoingFinishing = false;
        OutgoingRsv1Pending = false;
        IncomingCallActive = false;
        IncomingFinishing = false;
    }

    bool TakeOutgoingRsv1()
    {
        bool value = OutgoingRsv1Pending;
        OutgoingRsv1Pending = false;
        return value;
    }

    static bool IsFailureStatus(WebSocketParseStatus status)
    {
        return status == WebSocketParseStatus.ProtocolFailure ||
            status == WebSocketParseStatus.LimitRejected;
    }

    static byte[] CreateOutputBuffer(int maximumBufferedOutputBytes)
    {
        int length = maximumBufferedOutputBytes;
        if (length > MaximumTransformChunkLength)
        {
            length = MaximumTransformChunkLength;
        }

        return new byte[length];
    }

    static WebSocketTransformResult CreateSuccess(
        WebSocketParseStatus status,
        int consumedCount,
        int producedCount,
        byte[] output,
        bool setRsv1)
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.ProducedCount = producedCount;
        result.Output = output;
        result.SetRsv1 = setRsv1;
        return result;
    }

    static WebSocketTransformResult CreateFailure(
        WebSocketParseStatus status,
        string identifier)
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = status;
        result.HasError = true;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = "The permessage-deflate transform failed.";
        result.Error.HasCloseCode = true;
        if (status == WebSocketParseStatus.LimitRejected)
        {
            result.Error.Kind = WebSocketErrorKind.Limit;
            result.Error.CloseCode = WebSocketCloseCodes.MessageTooBig;
            result.Error.HasKnownCloseCode = true;
            result.Error.KnownCloseCode = WebSocketCloseCode.MessageTooBig;
        }
        else if (identifier.Contains("utf8", StringComparison.Ordinal))
        {
            result.Error.Kind = WebSocketErrorKind.Utf8;
            result.Error.CloseCode = WebSocketCloseCodes.InvalidFramePayloadData;
            result.Error.HasKnownCloseCode = true;
            result.Error.KnownCloseCode = WebSocketCloseCode.InvalidFramePayloadData;
        }
        else
        {
            result.Error.Kind = WebSocketErrorKind.Compression;
            result.Error.CloseCode = WebSocketCloseCodes.ProtocolError;
            result.Error.HasKnownCloseCode = true;
            result.Error.KnownCloseCode = WebSocketCloseCode.ProtocolError;
        }

        return result;
    }

    static PerMessageDeflateOptions CopyOptions(PerMessageDeflateOptions source)
    {
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
}
