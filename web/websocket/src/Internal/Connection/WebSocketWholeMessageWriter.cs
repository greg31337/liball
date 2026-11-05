using System;

namespace websocket;

internal class WebSocketWholeMessageWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal int FrameCount;
    internal int EncodedCount;
    internal byte[][] EncodedFrames = [];
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketWholeMessageWriter
{
    WebSocketRole Role;
    int MaximumFramePayloadLength;
    int MaximumMessagePayloadLength;
    int MaximumDecompressedMessagePayloadLength;
    int MaximumFragmentCount;
    int AutomaticFramePayloadLength;
    int MaximumScheduledFramePayloadLength;
    int MaximumTransformOutputCount;
    bool HasPerMessageDeflate;
    IWebSocketMaskKeySource? MaskKeySource;
    WebSocketExtensionPipeline ExtensionPipeline;
    IWebSocketFrameCodec FrameCodec;
    IWebSocketUtf8Validator Utf8Validator;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketWholeMessageWriter(
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
        AutomaticFramePayloadLength = options.AutomaticFramePayloadLength;
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
        Utf8Validator = new WebSocketUtf8Validator();
    }

    internal WebSocketWholeMessageWriteResult EncodeWholeMessage(
        WebSocketSendRequest? request)
    {
        if (Failed)
        {
            return CreateLatchedFailure();
        }

        WebSocketWholeMessageWriteResult validation = ValidateRequest(request);
        if (validation.HasError)
        {
            return validation;
        }

        if (request == null)
        {
            return CreateStateFailure(
                "send-request-invalid",
                "The whole-message send request is incomplete.");
        }

        WebSocketSendRequest validRequest = request;
        int framePayloadLength = AutomaticFramePayloadLength;
        if (validRequest.HasFramePayloadLength)
        {
            framePayloadLength = validRequest.FramePayloadLength;
        }

        if (validRequest.MessageType == WebSocketMessageType.Text)
        {
            Utf8Validator.ResetValidation();
            bool validText = Utf8Validator.ValidateChunk(
                validRequest.Payload.AsSpan(
                    validRequest.PayloadOffset,
                    validRequest.PayloadCount));
            if (!validText || !Utf8Validator.CompleteValidation())
            {
                return CreateUtf8Failure();
            }
        }

        int maximumTransformedCount = CalculateMaximumTransformedCount(
            framePayloadLength);
        WebSocketReassemblyBuffer transformedBuffer =
            new WebSocketReassemblyBuffer(maximumTransformedCount);
        int transformOutputCount = MaximumTransformOutputCount;
        if (ExtensionPipeline.Extensions.Length == 0 &&
            validRequest.PayloadCount > transformOutputCount)
        {
            transformOutputCount = validRequest.PayloadCount;
        }

        WebSocketExtensionContext context = CreateTransformContext(
            validRequest,
            transformOutputCount);
        int sourceOffset = 0;
        bool rsv1 = false;
        bool rsv2 = false;
        bool rsv3 = false;
        while (true)
        {
            ReadOnlySpan<byte> source = validRequest.Payload.AsSpan(
                validRequest.PayloadOffset + sourceOffset,
                validRequest.PayloadCount - sourceOffset);
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
                    "send-transform-status-invalid",
                    "The outbound extension transform returned an invalid status.");
            }

            sourceOffset = sourceOffset + transformed.ConsumedCount;
            if (!transformedBuffer.AppendMessageBytes(
                transformed.Output,
                0,
                transformed.ProducedCount))
            {
                return LatchLimitFailure(
                    "send-fragment-limit-exceeded",
                    "The transformed message requires too many frames.");
            }

            rsv1 = rsv1 || transformed.SetRsv1;
            rsv2 = rsv2 || transformed.SetRsv2;
            rsv3 = rsv3 || transformed.SetRsv3;
            if (transformed.Status == WebSocketParseStatus.Complete)
            {
                if (sourceOffset != validRequest.PayloadCount)
                {
                    return LatchStateFailure(
                        "send-transform-consumption-invalid",
                        "The outbound extension transform did not consume the message.");
                }

                break;
            }

            if (transformed.ConsumedCount == 0 &&
                transformed.ProducedCount == 0)
            {
                return LatchStateFailure(
                    "send-transform-progress-invalid",
                    "The outbound extension transform made no progress.");
            }
        }

        byte[] transformedPayload = transformedBuffer.TakeMessageBytes();
        return EncodeTransformedFrames(
            validRequest.MessageType,
            transformedPayload,
            framePayloadLength,
            rsv1,
            rsv2,
            rsv3);
    }

    internal bool HasWholeMessageWriterFailed()
    {
        return Failed;
    }

    internal WebSocketError GetWholeMessageWriterError()
    {
        return WebSocketFrameCodec.CopyError(FailureError);
    }

    WebSocketWholeMessageWriteResult ValidateRequest(
        WebSocketSendRequest? request)
    {
        if (request == null || request.Payload == null)
        {
            return CreateStateFailure(
                "send-request-invalid",
                "The whole-message send request is incomplete.");
        }

        if (request.MessageType != WebSocketMessageType.Text &&
            request.MessageType != WebSocketMessageType.Binary)
        {
            return CreateStateFailure(
                "send-message-type-invalid",
                "The whole-message send type is invalid.");
        }

        if (request.PayloadOffset < 0 ||
            request.PayloadOffset > request.Payload.Length ||
            request.PayloadCount < 0 ||
            request.PayloadCount >
                request.Payload.Length - request.PayloadOffset)
        {
            return CreateStateFailure(
                "send-payload-range-invalid",
                "The whole-message payload range is invalid.");
        }

        if (request.PayloadCount > MaximumMessagePayloadLength)
        {
            return CreateLimitFailure(
                "send-message-payload-limit-exceeded",
                "The whole-message payload exceeds the configured limit.");
        }

        if (request.Compress && !HasPerMessageDeflate)
        {
            return CreateStateFailure(
                "send-compression-unavailable",
                "Compression was requested without negotiated per-message compression.");
        }

        if (request.HasFramePayloadLength)
        {
            if (request.FramePayloadLength <= 0 ||
                request.FramePayloadLength > MaximumFramePayloadLength ||
                request.FramePayloadLength >
                    MaximumScheduledFramePayloadLength)
            {
                return CreateLimitFailure(
                    "send-frame-payload-length-invalid",
                    "The requested automatic frame payload length is invalid.");
            }
        }
        else if (request.FramePayloadLength != 0)
        {
            return CreateStateFailure(
                "send-frame-payload-sentinel-invalid",
                "The frame payload override requires its has-value flag.");
        }

        return new WebSocketWholeMessageWriteResult();
    }

    WebSocketWholeMessageWriteResult EncodeTransformedFrames(
        WebSocketMessageType messageType,
        byte[] payload,
        int framePayloadLength,
        bool rsv1,
        bool rsv2,
        bool rsv3)
    {
        int frameCount = 1;
        if (payload.Length != 0)
        {
            frameCount = payload.Length / framePayloadLength;
            if (payload.Length % framePayloadLength != 0)
            {
                frameCount = frameCount + 1;
            }
        }

        if (frameCount > MaximumFragmentCount)
        {
            return LatchLimitFailure(
                "send-fragment-limit-exceeded",
                "The transformed message requires too many frames.");
        }

        byte[][] frames = new byte[frameCount][];
        int payloadOffset = 0;
        int encodedCount = 0;
        int frameIndex = 0;
        while (frameIndex < frameCount)
        {
            int payloadCount = payload.Length - payloadOffset;
            if (payloadCount > framePayloadLength)
            {
                payloadCount = framePayloadLength;
            }

            WebSocketFrameWriteRequest frameRequest = CreateFrameRequest(
                messageType,
                payload,
                payloadOffset,
                payloadCount,
                frameIndex,
                frameCount,
                rsv1,
                rsv2,
                rsv3);
            int encodedLength = FrameCodec.MeasureFrame(frameRequest);
            if (encodedLength > int.MaxValue - encodedCount)
            {
                return LatchLimitFailure(
                    "send-encoded-size-overflow",
                    "The encoded message exceeds the supported signed size.");
            }

            byte[] encoded = new byte[encodedLength];
            int written = FrameCodec.WriteFrame(frameRequest, encoded);
            if (written != encodedLength || frameRequest.PayloadCount != 0)
            {
                return LatchStateFailure(
                    "send-frame-write-incomplete",
                    "The frame codec did not complete the prepared frame.");
            }

            frames[frameIndex] = encoded;
            encodedCount = encodedCount + encodedLength;
            payloadOffset = payloadOffset + payloadCount;
            frameIndex = frameIndex + 1;
        }

        WebSocketWholeMessageWriteResult result =
            new WebSocketWholeMessageWriteResult();
        result.FrameCount = frameCount;
        result.EncodedCount = encodedCount;
        result.EncodedFrames = frames;
        return result;
    }

    WebSocketFrameWriteRequest CreateFrameRequest(
        WebSocketMessageType messageType,
        byte[] payload,
        int payloadOffset,
        int payloadCount,
        int frameIndex,
        int frameCount,
        bool rsv1,
        bool rsv2,
        bool rsv3)
    {
        WebSocketFrameHeader header = new WebSocketFrameHeader();
        header.Final = frameIndex == frameCount - 1;
        header.Opcode = WebSocketOpcode.Continuation;
        if (frameIndex == 0)
        {
            header.Opcode = WebSocketOpcode.Binary;
            if (messageType == WebSocketMessageType.Text)
            {
                header.Opcode = WebSocketOpcode.Text;
            }

            header.Rsv1 = rsv1;
            header.Rsv2 = rsv2;
            header.Rsv3 = rsv3;
        }

        header.Masked = Role == WebSocketRole.Client;
        header.PayloadLength = payloadCount;
        WebSocketFrameWriteRequest request =
            new WebSocketFrameWriteRequest();
        request.Header = header;
        request.Payload = payload;
        request.PayloadOffset = payloadOffset;
        request.PayloadCount = payloadCount;
        request.HasMaskKeySource = header.Masked;
        if (header.Masked)
        {
            request.MaskKeySource = MaskKeySource;
        }

        return request;
    }

    WebSocketExtensionContext CreateTransformContext(
        WebSocketSendRequest request,
        int maximumOutputCount)
    {
        WebSocketExtensionContext context = new WebSocketExtensionContext();
        context.Role = Role;
        context.Outbound = true;
        context.RequestCompression = request.Compress;
        context.HasOpcode = true;
        context.Opcode = WebSocketOpcode.Binary;
        if (request.MessageType == WebSocketMessageType.Text)
        {
            context.Opcode = WebSocketOpcode.Text;
        }

        context.MessageType = request.MessageType;
        context.FirstChunk = true;
        context.FinalChunk = true;
        context.FirstFragment = true;
        context.FinalFragment = true;
        context.MaximumFramePayloadLength = MaximumFramePayloadLength;
        context.MaximumMessagePayloadLength = MaximumMessagePayloadLength;
        context.MaximumDecompressedMessagePayloadLength =
            MaximumDecompressedMessagePayloadLength;
        context.MaximumBufferedOutputBytes = maximumOutputCount;
        return context;
    }

    int CalculateMaximumTransformedCount(int framePayloadLength)
    {
        long maximum = (long)framePayloadLength * MaximumFragmentCount;
        if (maximum > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)maximum;
    }

    WebSocketWholeMessageWriteResult LatchStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketWholeMessageWriteResult result = CreateStateFailure(
            identifier,
            diagnostic);
        SetFailure(result.Status, result.Error);
        return CreateLatchedFailure();
    }

    WebSocketWholeMessageWriteResult LatchLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketWholeMessageWriteResult result = CreateLimitFailure(
            identifier,
            diagnostic);
        SetFailure(result.Status, result.Error);
        return CreateLatchedFailure();
    }

    WebSocketWholeMessageWriteResult LatchFailure(
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

    WebSocketWholeMessageWriteResult CreateLatchedFailure()
    {
        WebSocketWholeMessageWriteResult result =
            new WebSocketWholeMessageWriteResult();
        result.Status = FailureStatus;
        result.Accepted = false;
        result.HasError = true;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    static WebSocketWholeMessageWriteResult CreateUtf8Failure()
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Utf8;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.InvalidFramePayloadData;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.InvalidFramePayloadData;
        error.Identifier = "send-text-utf8-invalid";
        error.Diagnostic = "The outgoing text message is not valid UTF-8.";
        return CreateFailureResult(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    static WebSocketWholeMessageWriteResult CreateStateFailure(
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

    static WebSocketWholeMessageWriteResult CreateLimitFailure(
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

    static WebSocketWholeMessageWriteResult CreateFailureResult(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        WebSocketWholeMessageWriteResult result =
            new WebSocketWholeMessageWriteResult();
        result.Status = status;
        result.Accepted = false;
        result.HasError = true;
        result.Error = error;
        return result;
    }
}
