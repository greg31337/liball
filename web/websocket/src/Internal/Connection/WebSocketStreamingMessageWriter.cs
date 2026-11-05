using System;

namespace websocket;

internal class WebSocketStreamingMessageWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal bool MessageStarted;
    internal bool MessageEnded;
    internal WebSocketMessageType MessageType;
    internal int FragmentCount;
    internal int EncodedCount;
    internal byte[] EncodedFrame = [];
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketStreamingMessageWriter
{
    WebSocketRole Role;
    int MaximumFramePayloadLength;
    int MaximumMessagePayloadLength;
    int MaximumDecompressedMessagePayloadLength;
    int MaximumFragmentCount;
    int MaximumScheduledFramePayloadLength;
    int MaximumTransformOutputCount;
    bool HasPerMessageDeflate;
    IWebSocketMaskKeySource? MaskKeySource;
    WebSocketExtensionPipeline ExtensionPipeline;
    IWebSocketFrameCodec FrameCodec;
    bool MessageActive;
    WebSocketMessageType ActiveMessageType;
    bool ActiveCompression;
    long ActivePayloadCount;
    int ActiveFragmentCount;
    Utf8State TextState = new Utf8State();
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketStreamingMessageWriter(
        WebSocketConnectionOptions options,
        WebSocketExtensionPipeline? extensionPipeline)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (extensionPipeline == null)
        {
            throw new WebSocketStateException();
        }

        Role = options.Role;
        MaximumFramePayloadLength = options.MaximumFramePayloadLength;
        MaximumMessagePayloadLength = options.MaximumMessagePayloadLength;
        MaximumDecompressedMessagePayloadLength =
            options.MaximumDecompressedMessagePayloadLength;
        MaximumFragmentCount = options.MaximumFragmentCount;
        MaximumScheduledFramePayloadLength = MaximumFramePayloadLength;
        int scheduledFramePayloadLength =
            options.MaximumBufferedOutputBytes -
            WebSocketProtocolConstants.MaximumFrameHeaderLength;
        if (scheduledFramePayloadLength < MaximumScheduledFramePayloadLength)
        {
            MaximumScheduledFramePayloadLength = scheduledFramePayloadLength;
        }
        MaximumTransformOutputCount = options.MaximumBufferedOutputBytes;
        HasPerMessageDeflate = options.HasPerMessageDeflate;
        MaskKeySource = options.MaskKeySource;
        if (Role == WebSocketRole.Client && MaskKeySource == null)
        {
            MaskKeySource = new CryptographicWebSocketMaskKeySource();
        }

        ExtensionPipeline = extensionPipeline;
        FrameCodec = new WebSocketFrameCodec(
            Role,
            MaximumFramePayloadLength,
            options.NegotiatedExtensions);
        TextState.Reset();
    }

    internal WebSocketStreamingMessageWriteResult EncodeMessageChunk(
        WebSocketSendChunk? chunk)
    {
        if (Failed)
        {
            return CreateLatchedFailure();
        }

        WebSocketStreamingMessageWriteResult validation =
            ValidateChunk(chunk);
        if (validation.HasError)
        {
            return validation;
        }

        if (chunk == null)
        {
            return CreateStateFailure(
                "stream-send-chunk-invalid",
                "The streaming send chunk is incomplete.");
        }

        WebSocketMessageType messageType = ActiveMessageType;
        bool compression = ActiveCompression;
        long messagePayloadCount =
            ActivePayloadCount + chunk.PayloadCount;
        int fragmentCount = ActiveFragmentCount + 1;
        if (chunk.First)
        {
            messageType = chunk.MessageType;
            compression = chunk.Compress;
            messagePayloadCount = chunk.PayloadCount;
            fragmentCount = 1;
        }

        Utf8State? candidateTextState = null;
        if (messageType == WebSocketMessageType.Text)
        {
            candidateTextState = CreateCandidateTextState(chunk.First);
            bool validText = candidateTextState.Validate(
                chunk.Payload.AsSpan(
                    chunk.PayloadOffset,
                    chunk.PayloadCount));
            if (!validText ||
                (chunk.Final && !candidateTextState.Complete()))
            {
                return CreateUtf8Failure();
            }
        }

        WebSocketExtensionContext context = CreateTransformContext(
            chunk,
            messageType,
            compression);
        WebSocketReassemblyBuffer transformedBuffer =
            new WebSocketReassemblyBuffer(
                MaximumScheduledFramePayloadLength);
        int sourceOffset = 0;
        bool rsv1 = false;
        bool rsv2 = false;
        bool rsv3 = false;
        while (true)
        {
            ReadOnlySpan<byte> source = chunk.Payload.AsSpan(
                chunk.PayloadOffset + sourceOffset,
                chunk.PayloadCount - sourceOffset);
            WebSocketTransformResult transformed =
                ExtensionPipeline.TransformOutgoing(context, source);
            if (transformed.HasError)
            {
                return LatchFailure(transformed.Status, transformed.Error);
            }

            if (transformed.Status != WebSocketParseStatus.Complete &&
                transformed.Status != WebSocketParseStatus.NeedInput &&
                transformed.Status != WebSocketParseStatus.NeedOutputCapacity)
            {
                return LatchStateFailure(
                    "stream-send-transform-status-invalid",
                    "The outbound streaming transform returned an invalid status.");
            }

            sourceOffset = sourceOffset + transformed.ConsumedCount;
            if (!transformedBuffer.AppendMessageBytes(
                transformed.Output,
                0,
                transformed.ProducedCount))
            {
                return LatchLimitFailure(
                    "stream-send-frame-payload-limit-exceeded",
                    "The transformed streaming fragment exceeds the frame limit.");
            }

            rsv1 = rsv1 || transformed.SetRsv1;
            rsv2 = rsv2 || transformed.SetRsv2;
            rsv3 = rsv3 || transformed.SetRsv3;
            if (transformed.Status == WebSocketParseStatus.Complete)
            {
                if (sourceOffset != chunk.PayloadCount)
                {
                    return LatchStateFailure(
                        "stream-send-transform-consumption-invalid",
                        "The outbound streaming transform did not consume the chunk.");
                }

                break;
            }

            if (transformed.ConsumedCount == 0 &&
                transformed.ProducedCount == 0)
            {
                return LatchStateFailure(
                    "stream-send-transform-progress-invalid",
                    "The outbound streaming transform made no progress.");
            }
        }

        byte[] transformedPayload = transformedBuffer.TakeMessageBytes();
        WebSocketStreamingMessageWriteResult encoded = EncodeFrame(
            chunk,
            messageType,
            transformedPayload,
            fragmentCount,
            rsv1,
            rsv2,
            rsv3);
        if (encoded.HasError)
        {
            return encoded;
        }

        if (candidateTextState != null)
        {
            TextState = candidateTextState;
        }

        MessageActive = !chunk.Final;
        ActiveMessageType = messageType;
        ActiveCompression = compression;
        ActivePayloadCount = messagePayloadCount;
        ActiveFragmentCount = fragmentCount;
        if (!MessageActive)
        {
            ActiveMessageType = WebSocketMessageType.Text;
            ActiveCompression = false;
            ActivePayloadCount = 0;
            ActiveFragmentCount = 0;
        }

        return encoded;
    }

    internal bool HasActiveStreamingMessage()
    {
        return MessageActive;
    }

    internal int GetActiveStreamingFragmentCount()
    {
        return ActiveFragmentCount;
    }

    internal long GetActiveStreamingPayloadCount()
    {
        return ActivePayloadCount;
    }

    internal bool HasStreamingMessageWriterFailed()
    {
        return Failed;
    }

    internal WebSocketError GetStreamingMessageWriterError()
    {
        return WebSocketFrameCodec.CopyError(FailureError);
    }

    WebSocketStreamingMessageWriteResult ValidateChunk(
        WebSocketSendChunk? chunk)
    {
        if (chunk == null || chunk.Payload == null)
        {
            return CreateStateFailure(
                "stream-send-chunk-invalid",
                "The streaming send chunk is incomplete.");
        }

        if (chunk.PayloadOffset < 0 ||
            chunk.PayloadOffset > chunk.Payload.Length ||
            chunk.PayloadCount < 0 ||
            chunk.PayloadCount >
                chunk.Payload.Length - chunk.PayloadOffset)
        {
            return CreateStateFailure(
                "stream-send-payload-range-invalid",
                "The streaming send payload range is invalid.");
        }

        if (MessageActive && chunk.First)
        {
            return CreateStateFailure(
                "stream-send-message-overlap",
                "A streaming message is already active.");
        }

        if (!MessageActive && !chunk.First)
        {
            return CreateStateFailure(
                "stream-send-continuation-unexpected",
                "A streaming continuation requires an active message.");
        }

        if (chunk.First &&
            chunk.MessageType != WebSocketMessageType.Text &&
            chunk.MessageType != WebSocketMessageType.Binary)
        {
            return CreateStateFailure(
                "stream-send-message-type-invalid",
                "The streaming message type is invalid.");
        }

        if (chunk.First && chunk.Compress && !HasPerMessageDeflate)
        {
            return CreateStateFailure(
                "stream-send-compression-unavailable",
                "Compression was requested without negotiated per-message compression.");
        }

        long payloadCount = ActivePayloadCount + chunk.PayloadCount;
        int fragmentCount = ActiveFragmentCount + 1;
        if (chunk.First)
        {
            payloadCount = chunk.PayloadCount;
            fragmentCount = 1;
        }

        if (payloadCount > MaximumMessagePayloadLength)
        {
            return CreateLimitFailure(
                "stream-send-message-payload-limit-exceeded",
                "The streaming message exceeds the configured payload limit.");
        }

        if (fragmentCount > MaximumFragmentCount ||
            (fragmentCount == MaximumFragmentCount && !chunk.Final))
        {
            return CreateLimitFailure(
                "stream-send-fragment-limit-exceeded",
                "The streaming message contains too many fragments.");
        }

        if (ExtensionPipeline.Extensions.Length == 0 &&
            (chunk.PayloadCount > MaximumFramePayloadLength ||
                chunk.PayloadCount > MaximumScheduledFramePayloadLength))
        {
            return CreateLimitFailure(
                "stream-send-frame-payload-limit-exceeded",
                "The streaming fragment exceeds the configured frame limit.");
        }

        return new WebSocketStreamingMessageWriteResult();
    }

    WebSocketStreamingMessageWriteResult EncodeFrame(
        WebSocketSendChunk chunk,
        WebSocketMessageType messageType,
        byte[] payload,
        int fragmentCount,
        bool rsv1,
        bool rsv2,
        bool rsv3)
    {
        WebSocketFrameHeader header = new WebSocketFrameHeader();
        header.Final = chunk.Final;
        header.Rsv1 = rsv1;
        header.Rsv2 = rsv2;
        header.Rsv3 = rsv3;
        header.Opcode = WebSocketOpcode.Continuation;
        if (chunk.First)
        {
            header.Opcode = WebSocketOpcode.Binary;
            if (messageType == WebSocketMessageType.Text)
            {
                header.Opcode = WebSocketOpcode.Text;
            }
        }

        header.Masked = Role == WebSocketRole.Client;
        header.PayloadLength = payload.Length;
        WebSocketFrameWriteRequest frameRequest =
            new WebSocketFrameWriteRequest();
        frameRequest.Header = header;
        frameRequest.Payload = payload;
        frameRequest.PayloadCount = payload.Length;
        frameRequest.HasMaskKeySource = header.Masked;
        if (header.Masked)
        {
            frameRequest.MaskKeySource = MaskKeySource;
        }

        int encodedLength = FrameCodec.MeasureFrame(frameRequest);
        byte[] encodedBytes = new byte[encodedLength];
        int written = FrameCodec.WriteFrame(frameRequest, encodedBytes);
        if (written != encodedLength || frameRequest.PayloadCount != 0)
        {
            return LatchStateFailure(
                "stream-send-frame-write-incomplete",
                "The frame codec did not complete the streaming fragment.");
        }

        WebSocketStreamingMessageWriteResult result =
            new WebSocketStreamingMessageWriteResult();
        result.MessageStarted = chunk.First;
        result.MessageEnded = chunk.Final;
        result.MessageType = messageType;
        result.FragmentCount = fragmentCount;
        result.EncodedCount = encodedLength;
        result.EncodedFrame = encodedBytes;
        return result;
    }

    WebSocketExtensionContext CreateTransformContext(
        WebSocketSendChunk chunk,
        WebSocketMessageType messageType,
        bool compression)
    {
        WebSocketExtensionContext context = new WebSocketExtensionContext();
        context.Role = Role;
        context.Outbound = true;
        context.RequestCompression = compression;
        context.HasOpcode = true;
        context.Opcode = WebSocketOpcode.Continuation;
        if (chunk.First)
        {
            context.Opcode = WebSocketOpcode.Binary;
            if (messageType == WebSocketMessageType.Text)
            {
                context.Opcode = WebSocketOpcode.Text;
            }
        }

        context.MessageType = messageType;
        context.FirstChunk = chunk.First;
        context.FinalChunk = chunk.Final;
        context.FirstFragment = chunk.First;
        context.FinalFragment = chunk.Final;
        context.MaximumFramePayloadLength = MaximumFramePayloadLength;
        context.MaximumMessagePayloadLength = MaximumMessagePayloadLength;
        context.MaximumDecompressedMessagePayloadLength =
            MaximumDecompressedMessagePayloadLength;
        context.MaximumBufferedOutputBytes = MaximumTransformOutputCount;
        return context;
    }

    Utf8State CreateCandidateTextState(bool first)
    {
        Utf8State candidate = new Utf8State();
        if (first)
        {
            candidate.Reset();
            return candidate;
        }

        candidate.IsValid = TextState.IsValid;
        candidate.IsComplete = TextState.IsComplete;
        candidate.RemainingContinuationCount =
            TextState.RemainingContinuationCount;
        candidate.ContinuationMinimum = TextState.ContinuationMinimum;
        candidate.ContinuationMaximum = TextState.ContinuationMaximum;
        return candidate;
    }

    WebSocketStreamingMessageWriteResult LatchStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketStreamingMessageWriteResult result = CreateStateFailure(
            identifier,
            diagnostic);
        SetFailure(result.Status, result.Error);
        return CreateLatchedFailure();
    }

    WebSocketStreamingMessageWriteResult LatchLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketStreamingMessageWriteResult result = CreateLimitFailure(
            identifier,
            diagnostic);
        SetFailure(result.Status, result.Error);
        return CreateLatchedFailure();
    }

    WebSocketStreamingMessageWriteResult LatchFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        SetFailure(status, error);
        return CreateLatchedFailure();
    }

    void SetFailure(WebSocketParseStatus status, WebSocketError error)
    {
        Failed = true;
        FailureStatus = status;
        FailureError = WebSocketFrameCodec.CopyError(error);
    }

    WebSocketStreamingMessageWriteResult CreateLatchedFailure()
    {
        WebSocketStreamingMessageWriteResult result =
            new WebSocketStreamingMessageWriteResult();
        result.Status = FailureStatus;
        result.Accepted = false;
        result.HasError = true;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    static WebSocketStreamingMessageWriteResult CreateUtf8Failure()
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Utf8;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.InvalidFramePayloadData;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.InvalidFramePayloadData;
        error.Identifier = "stream-send-text-utf8-invalid";
        error.Diagnostic = "The outgoing streaming text is not valid UTF-8.";
        return CreateFailureResult(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    static WebSocketStreamingMessageWriteResult CreateStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailureResult(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    static WebSocketStreamingMessageWriteResult CreateLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.MessageTooBig;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.MessageTooBig;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailureResult(
            WebSocketParseStatus.LimitRejected,
            error);
    }

    static WebSocketStreamingMessageWriteResult CreateFailureResult(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        WebSocketStreamingMessageWriteResult result =
            new WebSocketStreamingMessageWriteResult();
        result.Status = status;
        result.Accepted = false;
        result.HasError = true;
        result.Error = error;
        return result;
    }
}
