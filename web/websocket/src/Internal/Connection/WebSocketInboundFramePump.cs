using System;

namespace websocket;

internal class WebSocketInboundFrameCapacity
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int MaximumPayloadCount;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketInboundFrameChunk
{
    internal WebSocketFrameHeader Header = new WebSocketFrameHeader();
    internal bool FirstChunk;
    internal bool EndOfFrame;
    internal byte[] Payload = [];
    internal int PayloadOffset;
    internal int PayloadCount;
}

internal class WebSocketInboundFrameConsumeResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketInboundFramePumpResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int FrameCount;
    internal int ChunkCount;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal interface IWebSocketInboundFrameConsumer
{
    WebSocketInboundFrameCapacity GetInboundFrameCapacity(
        WebSocketFrameHeader header,
        bool firstChunk);

    WebSocketInboundFrameConsumeResult ConsumeInboundFrameChunk(
        WebSocketInboundFrameChunk chunk);

    void HandleInboundFailure(
        WebSocketParseStatus status,
        WebSocketError error);

    void ResetInboundFrameConsumer();
}

internal class WebSocketInboundFramePump
{
    IWebSocketFrameCodec Codec;
    IWebSocketInboundFrameConsumer Consumer;
    bool PartialHeaderActive;
    bool HeaderActive;
    WebSocketFrameHeader Header = new WebSocketFrameHeader();
    bool FirstChunk;
    bool EmptyFramePending;
    WebSocketInboundFrameChunk? PendingChunk;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketInboundFramePump(
        IWebSocketFrameCodec codec,
        IWebSocketInboundFrameConsumer consumer)
    {
        if (codec == null || consumer == null)
        {
            throw new WebSocketStateException();
        }

        Codec = codec;
        Consumer = consumer;
    }

    internal WebSocketInboundFramePumpResult PumpInboundFrames(
        ReadOnlySpan<byte> source)
    {
        if (Failed)
        {
            return CreateLatchedFailure();
        }

        int sourceIndex = 0;
        int frameCount = 0;
        int chunkCount = 0;
        while (true)
        {
            if (PendingChunk != null)
            {
                WebSocketInboundFrameChunk pending = PendingChunk;
                bool endOfFrame = pending.EndOfFrame;
                WebSocketInboundFrameConsumeResult consumed =
                    Consumer.ConsumeInboundFrameChunk(pending);
                if (!ValidateConsumeResult(consumed))
                {
                    return LatchInvalidConsumerResult(
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (consumed.HasError)
                {
                    return LatchConsumerFailure(
                        consumed.Status,
                        consumed.Error,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (!consumed.Accepted)
                {
                    return CreatePumpResult(
                        consumed.Status,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                PendingChunk = null;
                chunkCount = chunkCount + 1;
                if (endOfFrame)
                {
                    CompleteFrame();
                    frameCount = frameCount + 1;
                }
                else
                {
                    FirstChunk = false;
                }

                continue;
            }

            if (HeaderActive)
            {
                WebSocketInboundFrameCapacity capacity =
                    Consumer.GetInboundFrameCapacity(
                        CopyHeader(Header),
                        FirstChunk);
                if (!ValidateCapacity(capacity, EmptyFramePending))
                {
                    return LatchInvalidConsumerResult(
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (capacity.HasError)
                {
                    return LatchConsumerFailure(
                        capacity.Status,
                        capacity.Error,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (capacity.Status != WebSocketParseStatus.Complete)
                {
                    return CreatePumpResult(
                        capacity.Status,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (EmptyFramePending)
                {
                    WebSocketInboundFrameChunk empty = CreateFrameChunk(
                        [],
                        0,
                        0,
                        true);
                    PendingChunk = empty;
                    EmptyFramePending = false;
                    continue;
                }

                if (sourceIndex == source.Length)
                {
                    return CreatePumpResult(
                        WebSocketParseStatus.NeedInput,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                int readCount = source.Length - sourceIndex;
                if (readCount > capacity.MaximumPayloadCount)
                {
                    readCount = capacity.MaximumPayloadCount;
                }

                WebSocketFrameReadResult payload = Codec.ReadFrame(
                    source.Slice(sourceIndex, readCount));
                if (!ValidateCodecResult(payload, readCount, false))
                {
                    return LatchInvalidCodecResult(
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                sourceIndex = sourceIndex + payload.ConsumedCount;

                if (payload.HasError)
                {
                    return LatchConsumerFailure(
                        payload.Status,
                        payload.Error,
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                if (payload.PayloadCount != 0 || payload.EndOfFrame)
                {
                    PendingChunk = CreateFrameChunk(
                        payload.Payload,
                        payload.PayloadOffset,
                        payload.PayloadCount,
                        payload.EndOfFrame);
                    continue;
                }

                if (payload.ConsumedCount == 0)
                {
                    return LatchInvalidCodecResult(
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                continue;
            }

            if (sourceIndex == source.Length)
            {
                WebSocketParseStatus status = WebSocketParseStatus.Complete;
                if (PartialHeaderActive)
                {
                    status = WebSocketParseStatus.NeedInput;
                }

                return CreatePumpResult(
                    status,
                    sourceIndex,
                    frameCount,
                    chunkCount);
            }

            WebSocketFrameReadResult header = Codec.ReadFrame(
                source.Slice(sourceIndex, 1));
            if (!ValidateCodecResult(header, 1, true))
            {
                return LatchInvalidCodecResult(
                    sourceIndex,
                    frameCount,
                    chunkCount);
            }

            sourceIndex = sourceIndex + header.ConsumedCount;

            if (header.HasError)
            {
                return LatchConsumerFailure(
                    header.Status,
                    header.Error,
                    sourceIndex,
                    frameCount,
                    chunkCount);
            }

            if (header.HasHeader)
            {
                Header = CopyHeader(header.Header);
                HeaderActive = true;
                PartialHeaderActive = false;
                FirstChunk = true;
                EmptyFramePending = header.EndOfFrame;
                if (header.PayloadCount != 0)
                {
                    return LatchInvalidCodecResult(
                        sourceIndex,
                        frameCount,
                        chunkCount);
                }

                continue;
            }

            if (header.ConsumedCount == 0)
            {
                return LatchInvalidCodecResult(
                    sourceIndex,
                    frameCount,
                    chunkCount);
            }

            PartialHeaderActive = true;
        }
    }

    internal void ResetInboundFramePump()
    {
        Codec.ResetFrameRead();
        Consumer.ResetInboundFrameConsumer();
        PartialHeaderActive = false;
        HeaderActive = false;
        Header = new WebSocketFrameHeader();
        FirstChunk = false;
        EmptyFramePending = false;
        PendingChunk = null;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    internal void LatchConnectionFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        if (Failed)
        {
            return;
        }

        if ((status != WebSocketParseStatus.ProtocolFailure &&
                status != WebSocketParseStatus.LimitRejected) ||
            error == null)
        {
            throw new WebSocketStateException();
        }

        Failed = true;
        FailureStatus = status;
        FailureError = CopyError(error);
    }

    WebSocketInboundFrameChunk CreateFrameChunk(
        byte[] payload,
        int payloadOffset,
        int payloadCount,
        bool endOfFrame)
    {
        WebSocketInboundFrameChunk chunk = new WebSocketInboundFrameChunk();
        chunk.Header = CopyHeader(Header);
        chunk.FirstChunk = FirstChunk;
        chunk.EndOfFrame = endOfFrame;
        chunk.Payload = payload;
        chunk.PayloadOffset = payloadOffset;
        chunk.PayloadCount = payloadCount;
        return chunk;
    }

    void CompleteFrame()
    {
        Codec.ResetFrameRead();
        PartialHeaderActive = false;
        HeaderActive = false;
        Header = new WebSocketFrameHeader();
        FirstChunk = false;
        EmptyFramePending = false;
    }

    WebSocketInboundFramePumpResult LatchConsumerFailure(
        WebSocketParseStatus status,
        WebSocketError error,
        int consumedCount,
        int frameCount,
        int chunkCount)
    {
        Consumer.HandleInboundFailure(status, error);
        Failed = true;
        FailureStatus = status;
        FailureError = CopyError(error);
        WebSocketInboundFramePumpResult result = CreatePumpResult(
            status,
            consumedCount,
            frameCount,
            chunkCount);
        result.HasError = true;
        result.Error = CopyError(FailureError);
        return result;
    }

    WebSocketInboundFramePumpResult LatchInvalidConsumerResult(
        int consumedCount,
        int frameCount,
        int chunkCount)
    {
        WebSocketError error = CreateInternalError(
            "frame-consumer-result-invalid",
            "The inbound frame consumer returned an inconsistent result.");
        return LatchConsumerFailure(
            WebSocketParseStatus.ProtocolFailure,
            error,
            consumedCount,
            frameCount,
            chunkCount);
    }

    WebSocketInboundFramePumpResult LatchInvalidCodecResult(
        int consumedCount,
        int frameCount,
        int chunkCount)
    {
        WebSocketError error = CreateInternalError(
            "frame-codec-result-invalid",
            "The frame codec returned an inconsistent incremental result.");
        return LatchConsumerFailure(
            WebSocketParseStatus.ProtocolFailure,
            error,
            consumedCount,
            frameCount,
            chunkCount);
    }

    WebSocketInboundFramePumpResult CreateLatchedFailure()
    {
        WebSocketInboundFramePumpResult result = CreatePumpResult(
            FailureStatus,
            0,
            0,
            0);
        result.HasError = true;
        result.Error = CopyError(FailureError);
        return result;
    }

    static bool ValidateCapacity(
        WebSocketInboundFrameCapacity capacity,
        bool emptyFrame)
    {
        if (capacity == null || capacity.Error == null)
        {
            return false;
        }

        if (capacity.Status == WebSocketParseStatus.Complete)
        {
            return !capacity.HasError &&
                capacity.MaximumPayloadCount >= 0 &&
                (emptyFrame || capacity.MaximumPayloadCount > 0);
        }

        if (capacity.Status == WebSocketParseStatus.NeedOutputCapacity ||
            capacity.Status == WebSocketParseStatus.NeedEventCapacity)
        {
            return !capacity.HasError;
        }

        if (capacity.Status == WebSocketParseStatus.ProtocolFailure ||
            capacity.Status == WebSocketParseStatus.LimitRejected)
        {
            return capacity.HasError;
        }

        return false;
    }

    static bool ValidateConsumeResult(
        WebSocketInboundFrameConsumeResult result)
    {
        if (result == null || result.Error == null)
        {
            return false;
        }

        if (result.Status == WebSocketParseStatus.Complete)
        {
            return result.Accepted && !result.HasError;
        }

        if (result.Status == WebSocketParseStatus.NeedOutputCapacity ||
            result.Status == WebSocketParseStatus.NeedEventCapacity)
        {
            return !result.Accepted && !result.HasError;
        }

        if (result.Status == WebSocketParseStatus.ProtocolFailure ||
            result.Status == WebSocketParseStatus.LimitRejected)
        {
            return !result.Accepted && result.HasError;
        }

        return false;
    }

    static bool ValidateCodecResult(
        WebSocketFrameReadResult result,
        int sourceLength,
        bool readingHeader)
    {
        if (result == null ||
            result.Header == null ||
            result.Payload == null ||
            result.Error == null ||
            result.ConsumedCount < 0 ||
            result.ConsumedCount > sourceLength ||
            result.PayloadOffset < 0 ||
            result.PayloadOffset > result.Payload.Length ||
            result.PayloadCount < 0 ||
            result.PayloadCount > result.Payload.Length - result.PayloadOffset)
        {
            return false;
        }

        if (result.HasError)
        {
            return result.Status == WebSocketParseStatus.ProtocolFailure ||
                result.Status == WebSocketParseStatus.LimitRejected;
        }

        if (result.Status != WebSocketParseStatus.Complete &&
            result.Status != WebSocketParseStatus.NeedInput)
        {
            return false;
        }

        if ((result.Status == WebSocketParseStatus.Complete &&
                !result.EndOfFrame) ||
            (result.EndOfFrame &&
                result.Status != WebSocketParseStatus.Complete))
        {
            return false;
        }

        if (readingHeader)
        {
            return result.PayloadCount == 0 &&
                (result.HasHeader ||
                    result.Status == WebSocketParseStatus.NeedInput);
        }

        return !result.HasHeader;
    }

    static WebSocketInboundFramePumpResult CreatePumpResult(
        WebSocketParseStatus status,
        int consumedCount,
        int frameCount,
        int chunkCount)
    {
        WebSocketInboundFramePumpResult result =
            new WebSocketInboundFramePumpResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.FrameCount = frameCount;
        result.ChunkCount = chunkCount;
        return result;
    }

    static WebSocketError CreateInternalError(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.InternalError;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.InternalError;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return error;
    }

    static WebSocketFrameHeader CopyHeader(WebSocketFrameHeader source)
    {
        WebSocketFrameHeader copy = new WebSocketFrameHeader();
        copy.Final = source.Final;
        copy.Rsv1 = source.Rsv1;
        copy.Rsv2 = source.Rsv2;
        copy.Rsv3 = source.Rsv3;
        copy.Opcode = source.Opcode;
        copy.Masked = source.Masked;
        copy.MaskingKey = source.MaskingKey;
        copy.PayloadLength = source.PayloadLength;
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
