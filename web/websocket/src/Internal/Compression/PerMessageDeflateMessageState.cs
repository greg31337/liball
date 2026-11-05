using System;

namespace websocket;

internal class PerMessageDeflateMessageResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal bool Compressed;
    internal bool SetRsv1;
    internal bool MessageComplete;
    internal string ErrorIdentifier = "";
}

internal class PerMessageDeflateMessageState
{
    PerMessageDeflateCodec Codec;
    int CompressionThreshold;
    bool OutgoingMessageActive;
    bool OutgoingFrameActive;
    bool OutgoingFinalFrame;
    bool OutgoingCompressed;
    WebSocketMessageType OutgoingMessageType;
    bool IncomingMessageActive;
    bool IncomingFrameActive;
    bool IncomingFinalFrame;
    bool IncomingCompressed;
    WebSocketMessageType IncomingMessageType;
    Utf8State IncomingTextState = new Utf8State();
    bool OutgoingFailed;
    bool IncomingFailed;
    WebSocketParseStatus IncomingFailureStatus =
        WebSocketParseStatus.ProtocolFailure;
    string OutgoingFailureIdentifier = "";
    string IncomingFailureIdentifier = "";

    internal PerMessageDeflateMessageState(
        PerMessageDeflateCodec codec,
        int compressionThreshold)
    {
        if (codec == null || compressionThreshold < 0)
        {
            throw new WebSocketStateException();
        }

        Codec = codec;
        CompressionThreshold = compressionThreshold;
    }

    internal PerMessageDeflateMessageResult BeginOutgoingMessage(
        WebSocketMessageType messageType,
        bool requestCompression,
        int messageLength,
        bool finalFrame)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailure();
        }

        if (OutgoingMessageActive ||
            OutgoingFrameActive ||
            messageLength < 0 ||
            (messageType != WebSocketMessageType.Text &&
                messageType != WebSocketMessageType.Binary))
        {
            return FailOutgoing("permessage-deflate-outgoing-state-invalid");
        }

        OutgoingMessageActive = true;
        OutgoingFrameActive = true;
        OutgoingFinalFrame = finalFrame;
        OutgoingMessageType = messageType;
        OutgoingCompressed = requestCompression &&
            messageLength >= CompressionThreshold;
        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            false,
            "");
        result.Compressed = OutgoingCompressed;
        result.SetRsv1 = OutgoingCompressed;
        return result;
    }

    internal PerMessageDeflateMessageResult BeginOutgoingContinuation(
        bool finalFrame)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailure();
        }

        if (!OutgoingMessageActive || OutgoingFrameActive)
        {
            return FailOutgoing("permessage-deflate-outgoing-state-invalid");
        }

        OutgoingFrameActive = true;
        OutgoingFinalFrame = finalFrame;
        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            false,
            "");
        result.Compressed = OutgoingCompressed;
        return result;
    }

    internal PerMessageDeflateMessageResult TransformOutgoingPayload(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailure();
        }

        if (!OutgoingMessageActive || !OutgoingFrameActive)
        {
            return FailOutgoing("permessage-deflate-outgoing-state-invalid");
        }

        if (OutgoingCompressed)
        {
            PerMessageDeflateCodecResult transformed =
                Codec.EncodeCompressedBytes(source, destination);
            if (transformed.Status == WebSocketParseStatus.ProtocolFailure ||
                transformed.Status == WebSocketParseStatus.LimitRejected)
            {
                return FailOutgoing(transformed.ErrorIdentifier);
            }

            PerMessageDeflateMessageResult result = CreateMessageResult(
                transformed.Status,
                transformed.ConsumedCount,
                transformed.ProducedCount,
                false,
                "");
            result.Compressed = true;
            return result;
        }

        return CopyUncompressedBytes(source, destination, false);
    }

    internal PerMessageDeflateMessageResult FinishOutgoingFrame(
        Span<byte> destination)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailure();
        }

        if (!OutgoingMessageActive || !OutgoingFrameActive)
        {
            return FailOutgoing("permessage-deflate-outgoing-state-invalid");
        }

        if (!OutgoingFinalFrame)
        {
            OutgoingFrameActive = false;
            PerMessageDeflateMessageResult continued = CreateMessageResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                false,
                "");
            continued.Compressed = OutgoingCompressed;
            return continued;
        }

        if (OutgoingCompressed)
        {
            PerMessageDeflateCodecResult finished =
                Codec.FinishCompressedBytes(destination);
            if (finished.Status == WebSocketParseStatus.ProtocolFailure ||
                finished.Status == WebSocketParseStatus.LimitRejected)
            {
                return FailOutgoing(finished.ErrorIdentifier);
            }

            PerMessageDeflateMessageResult result = CreateMessageResult(
                finished.Status,
                0,
                finished.ProducedCount,
                finished.MessageComplete,
                "");
            result.Compressed = true;
            if (finished.MessageComplete)
            {
                CompleteOutgoingState();
            }

            return result;
        }

        WebSocketParseStatus complete =
            Codec.CompleteOutgoingUncompressedMessage();
        if (complete != WebSocketParseStatus.Complete)
        {
            return FailOutgoing("permessage-deflate-outgoing-state-invalid");
        }

        CompleteOutgoingState();
        return CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            true,
            "");
    }

    internal PerMessageDeflateMessageResult ObserveOutgoingControl(
        WebSocketOpcode opcode,
        bool rsv1)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailure();
        }

        if ((int)opcode < 8 || rsv1)
        {
            return FailOutgoing("permessage-deflate-control-rsv1-invalid");
        }

        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            false,
            "");
        result.Compressed = OutgoingCompressed;
        return result;
    }

    internal PerMessageDeflateMessageResult BeginIncomingDataFrame(
        WebSocketOpcode opcode,
        bool rsv1,
        bool finalFrame)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailure();
        }

        if (IncomingFrameActive)
        {
            return FailIncoming("permessage-deflate-incoming-state-invalid");
        }

        if (opcode == WebSocketOpcode.Text ||
            opcode == WebSocketOpcode.Binary)
        {
            if (IncomingMessageActive)
            {
                return FailIncoming(
                    "permessage-deflate-incoming-state-invalid");
            }

            IncomingMessageActive = true;
            IncomingCompressed = rsv1;
            if (opcode == WebSocketOpcode.Text)
            {
                IncomingMessageType = WebSocketMessageType.Text;
                IncomingTextState.Reset();
            }
            else
            {
                IncomingMessageType = WebSocketMessageType.Binary;
            }
        }
        else if (opcode == WebSocketOpcode.Continuation)
        {
            if (!IncomingMessageActive)
            {
                return FailIncoming(
                    "permessage-deflate-continuation-unexpected");
            }

            if (rsv1)
            {
                return FailIncoming(
                    "permessage-deflate-continuation-rsv1-invalid");
            }
        }
        else
        {
            return FailIncoming("permessage-deflate-data-opcode-invalid");
        }

        IncomingFrameActive = true;
        IncomingFinalFrame = finalFrame;
        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            false,
            "");
        result.Compressed = IncomingCompressed;
        return result;
    }

    internal PerMessageDeflateMessageResult TransformIncomingPayload(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailure();
        }

        if (!IncomingMessageActive || !IncomingFrameActive)
        {
            return FailIncoming("permessage-deflate-incoming-state-invalid");
        }

        PerMessageDeflateMessageResult result;
        if (IncomingCompressed)
        {
            PerMessageDeflateCodecResult transformed =
                Codec.DecodeCompressedBytes(source, destination);
            if (transformed.Status == WebSocketParseStatus.ProtocolFailure ||
                transformed.Status == WebSocketParseStatus.LimitRejected)
            {
                return FailIncomingWithStatus(
                    transformed.Status,
                    transformed.ErrorIdentifier);
            }

            result = CreateMessageResult(
                transformed.Status,
                transformed.ConsumedCount,
                transformed.ProducedCount,
                false,
                "");
            result.Compressed = true;
        }
        else
        {
            result = CopyUncompressedBytes(source, destination, true);
        }

        if (IncomingMessageType == WebSocketMessageType.Text &&
            result.ProducedCount != 0 &&
            !IncomingTextState.Validate(
                destination.Slice(0, result.ProducedCount)))
        {
            return FailIncoming("permessage-deflate-text-utf8-invalid");
        }

        return result;
    }

    internal PerMessageDeflateMessageResult FinishIncomingFrame(
        Span<byte> destination)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailure();
        }

        if (!IncomingMessageActive || !IncomingFrameActive)
        {
            return FailIncoming("permessage-deflate-incoming-state-invalid");
        }

        if (!IncomingFinalFrame)
        {
            IncomingFrameActive = false;
            PerMessageDeflateMessageResult continued = CreateMessageResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                false,
                "");
            continued.Compressed = IncomingCompressed;
            return continued;
        }

        int producedCount = 0;
        if (IncomingCompressed)
        {
            PerMessageDeflateCodecResult finished =
                Codec.FinishDecompressedBytes(destination);
            if (finished.Status == WebSocketParseStatus.ProtocolFailure ||
                finished.Status == WebSocketParseStatus.LimitRejected)
            {
                return FailIncomingWithStatus(
                    finished.Status,
                    finished.ErrorIdentifier);
            }

            producedCount = finished.ProducedCount;
            if (IncomingMessageType == WebSocketMessageType.Text &&
                producedCount != 0 &&
                !IncomingTextState.Validate(
                    destination.Slice(0, producedCount)))
            {
                return FailIncoming("permessage-deflate-text-utf8-invalid");
            }

            if (!finished.MessageComplete)
            {
                PerMessageDeflateMessageResult pending = CreateMessageResult(
                    finished.Status,
                    0,
                    producedCount,
                    false,
                    "");
                pending.Compressed = true;
                return pending;
            }
        }
        else
        {
            WebSocketParseStatus complete =
                Codec.CompleteIncomingUncompressedMessage();
            if (complete != WebSocketParseStatus.Complete)
            {
                return FailIncoming(
                    "permessage-deflate-incoming-state-invalid");
            }
        }

        if (IncomingMessageType == WebSocketMessageType.Text &&
            !IncomingTextState.Complete())
        {
            return FailIncoming("permessage-deflate-text-utf8-incomplete");
        }

        bool compressed = IncomingCompressed;
        CompleteIncomingState();
        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            producedCount,
            true,
            "");
        result.Compressed = compressed;
        return result;
    }

    internal PerMessageDeflateMessageResult ObserveIncomingControl(
        WebSocketOpcode opcode,
        bool rsv1)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailure();
        }

        if ((int)opcode < 8 || rsv1)
        {
            return FailIncoming("permessage-deflate-control-rsv1-invalid");
        }

        PerMessageDeflateMessageResult result = CreateMessageResult(
            WebSocketParseStatus.Complete,
            0,
            0,
            false,
            "");
        result.Compressed = IncomingCompressed;
        return result;
    }

    internal void ResetPerMessageDeflateMessageState()
    {
        Codec.ResetPerMessageDeflateCodec();
        OutgoingMessageActive = false;
        OutgoingFrameActive = false;
        OutgoingFinalFrame = false;
        OutgoingCompressed = false;
        IncomingMessageActive = false;
        IncomingFrameActive = false;
        IncomingFinalFrame = false;
        IncomingCompressed = false;
        IncomingTextState.Reset();
        OutgoingFailed = false;
        IncomingFailed = false;
        IncomingFailureStatus = WebSocketParseStatus.ProtocolFailure;
        OutgoingFailureIdentifier = "";
        IncomingFailureIdentifier = "";
    }

    internal bool GetOutgoingMessageCompressed()
    {
        return OutgoingMessageActive && OutgoingCompressed;
    }

    internal bool GetIncomingMessageCompressed()
    {
        return IncomingMessageActive && IncomingCompressed;
    }

    PerMessageDeflateMessageResult CopyUncompressedBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        bool incoming)
    {
        int copyCount = source.Length;
        if (copyCount > destination.Length)
        {
            copyCount = destination.Length;
        }

        source.Slice(0, copyCount).CopyTo(destination);
        WebSocketParseStatus status = WebSocketParseStatus.Complete;
        if (copyCount < source.Length)
        {
            status = WebSocketParseStatus.NeedOutputCapacity;
        }

        PerMessageDeflateMessageResult result = CreateMessageResult(
            status,
            copyCount,
            copyCount,
            false,
            "");
        if (incoming)
        {
            result.Compressed = IncomingCompressed;
        }
        else
        {
            result.Compressed = OutgoingCompressed;
        }

        return result;
    }

    void CompleteOutgoingState()
    {
        OutgoingMessageActive = false;
        OutgoingFrameActive = false;
        OutgoingFinalFrame = false;
        OutgoingCompressed = false;
    }

    void CompleteIncomingState()
    {
        IncomingMessageActive = false;
        IncomingFrameActive = false;
        IncomingFinalFrame = false;
        IncomingCompressed = false;
    }

    PerMessageDeflateMessageResult FailOutgoing(string identifier)
    {
        OutgoingFailed = true;
        OutgoingFailureIdentifier = identifier;
        return CreateMessageResult(
            WebSocketParseStatus.ProtocolFailure,
            0,
            0,
            false,
            identifier);
    }

    PerMessageDeflateMessageResult FailIncoming(string identifier)
    {
        return FailIncomingWithStatus(
            WebSocketParseStatus.ProtocolFailure,
            identifier);
    }

    PerMessageDeflateMessageResult FailIncomingWithStatus(
        WebSocketParseStatus status,
        string identifier)
    {
        IncomingFailed = true;
        IncomingFailureStatus = status;
        IncomingFailureIdentifier = identifier;
        return CreateMessageResult(
            status,
            0,
            0,
            false,
            identifier);
    }

    PerMessageDeflateMessageResult CreateOutgoingFailure()
    {
        return CreateMessageResult(
            IncomingFailureStatus,
            0,
            0,
            false,
            OutgoingFailureIdentifier);
    }

    PerMessageDeflateMessageResult CreateIncomingFailure()
    {
        return CreateMessageResult(
            WebSocketParseStatus.ProtocolFailure,
            0,
            0,
            false,
            IncomingFailureIdentifier);
    }

    PerMessageDeflateMessageResult CreateMessageResult(
        WebSocketParseStatus status,
        int consumedCount,
        int producedCount,
        bool messageComplete,
        string errorIdentifier)
    {
        PerMessageDeflateMessageResult result =
            new PerMessageDeflateMessageResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.ProducedCount = producedCount;
        result.MessageComplete = messageComplete;
        result.ErrorIdentifier = errorIdentifier;
        return result;
    }
}
