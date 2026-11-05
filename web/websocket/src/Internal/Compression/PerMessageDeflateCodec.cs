using System;

namespace websocket;

internal class PerMessageDeflateCodecResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal bool MessageComplete;
    internal string ErrorIdentifier = "";
}

internal class PerMessageDeflateCodec
{
    static readonly byte[] SyncFlushTail = [0x00, 0x00, 0xff, 0xff];

    RawDeflateEncoder OutgoingEncoder;
    RawDeflateDecoder IncomingDecoder;
    bool OutgoingContextTakeover;
    bool IncomingContextTakeover;
    byte[] OutgoingTail = new byte[4];
    int OutgoingTailCount;
    byte[] OutgoingRawByte = new byte[1];
    bool OutgoingRawBytePending;
    bool OutgoingRawFlushComplete;
    bool OutgoingMessageActive;
    bool IncomingMessageActive;
    int IncomingTailOffset;
    bool IncomingSyncFlushBoundary;
    bool IncomingFinalBlockComplete;
    bool IncomingFinalPaddingComplete;
    bool OutgoingFailed;
    bool IncomingFailed;
    WebSocketParseStatus OutgoingFailureStatus =
        WebSocketParseStatus.ProtocolFailure;
    WebSocketParseStatus IncomingFailureStatus =
        WebSocketParseStatus.ProtocolFailure;
    string OutgoingFailureIdentifier = "";
    string IncomingFailureIdentifier = "";

    internal PerMessageDeflateCodec(
        PerMessageDeflateDirectionSettings settings,
        int maximumDecompressedMessageLength)
    {
        if (settings == null ||
            !settings.Valid ||
            settings.OutgoingWindowBits < 8 ||
            settings.OutgoingWindowBits > 15 ||
            settings.IncomingWindowBits < 8 ||
            settings.IncomingWindowBits > 15 ||
            maximumDecompressedMessageLength < 0)
        {
            throw new WebSocketStateException();
        }

        OutgoingContextTakeover = settings.OutgoingContextTakeover;
        IncomingContextTakeover = settings.IncomingContextTakeover;
        OutgoingEncoder = new RawDeflateEncoder(
            settings.OutgoingWindowBits);
        IncomingDecoder = new RawDeflateDecoder(
            settings.IncomingWindowBits,
            maximumDecompressedMessageLength);
    }

    internal PerMessageDeflateCodecResult EncodeCompressedBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailureResult();
        }

        if (OutgoingRawFlushComplete)
        {
            return FailOutgoing(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-outgoing-state-invalid",
                0,
                0);
        }

        OutgoingMessageActive = true;
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (true)
        {
            destinationIndex = destinationIndex +
                DrainOutgoingRawByte(
                    destination.Slice(destinationIndex));
            if (OutgoingRawBytePending)
            {
                return CreateCodecResult(
                    WebSocketParseStatus.NeedOutputCapacity,
                    sourceIndex,
                    destinationIndex,
                    false,
                    "");
            }

            RawDeflateEncodeResult encoded =
                OutgoingEncoder.EncodeRawDeflate(
                    source.Slice(sourceIndex),
                    OutgoingRawByte);
            sourceIndex = sourceIndex + encoded.ConsumedCount;
            if (encoded.ProducedCount != 0)
            {
                OutgoingRawBytePending = true;
                continue;
            }

            if (encoded.Status == WebSocketParseStatus.ProtocolFailure)
            {
                return FailOutgoing(
                    encoded.Status,
                    encoded.ErrorIdentifier,
                    sourceIndex,
                    destinationIndex);
            }

            if (encoded.Status ==
                WebSocketParseStatus.NeedOutputCapacity)
            {
                continue;
            }

            if (sourceIndex == source.Length)
            {
                return CreateCodecResult(
                    WebSocketParseStatus.Complete,
                    sourceIndex,
                    destinationIndex,
                    false,
                    "");
            }
        }
    }

    internal PerMessageDeflateCodecResult FinishCompressedBytes(
        Span<byte> destination)
    {
        if (OutgoingFailed)
        {
            return CreateOutgoingFailureResult();
        }

        OutgoingMessageActive = true;
        int destinationIndex = 0;
        while (true)
        {
            destinationIndex = destinationIndex +
                DrainOutgoingRawByte(
                    destination.Slice(destinationIndex));
            if (OutgoingRawBytePending)
            {
                return CreateCodecResult(
                    WebSocketParseStatus.NeedOutputCapacity,
                    0,
                    destinationIndex,
                    false,
                    "");
            }

            if (OutgoingRawFlushComplete)
            {
                return CompleteOutgoingMessage(destinationIndex);
            }

            RawDeflateEncodeResult encoded =
                OutgoingEncoder.FinishRawDeflate(
                    OutgoingRawByte,
                    false);
            if (encoded.ProducedCount != 0)
            {
                OutgoingRawBytePending = true;
            }

            if (encoded.Status == WebSocketParseStatus.ProtocolFailure)
            {
                return FailOutgoing(
                    encoded.Status,
                    encoded.ErrorIdentifier,
                    0,
                    destinationIndex);
            }

            if (encoded.Status == WebSocketParseStatus.Complete)
            {
                OutgoingRawFlushComplete = true;
            }

            if (encoded.ProducedCount == 0 &&
                encoded.Status ==
                    WebSocketParseStatus.NeedOutputCapacity)
            {
                return FailOutgoing(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-outgoing-state-invalid",
                    0,
                    destinationIndex);
            }
        }
    }

    internal PerMessageDeflateCodecResult DecodeCompressedBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailureResult();
        }

        if (IncomingTailOffset != 0)
        {
            return FailIncoming(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-incoming-state-invalid",
                0,
                0);
        }

        if (IncomingFinalBlockComplete)
        {
            if (IncomingFinalPaddingComplete)
            {
                if (source.Length != 0)
                {
                    return FailIncoming(
                        WebSocketParseStatus.ProtocolFailure,
                        "permessage-deflate-final-block-invalid",
                        0,
                        0);
                }

                return CreateCodecResult(
                    WebSocketParseStatus.Complete,
                    0,
                    0,
                    false,
                    "");
            }

            if (source.Length == 0)
            {
                return CreateCodecResult(
                    WebSocketParseStatus.Complete,
                    0,
                    0,
                    false,
                    "");
            }

            if (source.Length != 1 || source[0] != 0)
            {
                return FailIncoming(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-final-block-invalid",
                    0,
                    0);
            }

            IncomingFinalPaddingComplete = true;
            return CreateCodecResult(
                WebSocketParseStatus.Complete,
                1,
                0,
                false,
                "");
        }

        IncomingMessageActive = true;
        if (IncomingSyncFlushBoundary && source.Length != 0)
        {
            IncomingSyncFlushBoundary = false;
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (true)
        {
            RawDeflateDecodeResult decoded =
                IncomingDecoder.DecodeRawDeflate(
                    source.Slice(sourceIndex),
                    destination.Slice(destinationIndex));
            sourceIndex = sourceIndex + decoded.ConsumedCount;
            destinationIndex = destinationIndex + decoded.ProducedCount;
            if (decoded.Status == WebSocketParseStatus.ProtocolFailure ||
                decoded.Status == WebSocketParseStatus.LimitRejected)
            {
                return FailIncoming(
                    decoded.Status,
                    decoded.ErrorIdentifier,
                    sourceIndex,
                    destinationIndex);
            }

            if (decoded.Status == WebSocketParseStatus.Complete)
            {
                if (decoded.StreamComplete)
                {
                    IncomingFinalBlockComplete = true;
                    int remaining = source.Length - sourceIndex;
                    if (remaining == 0)
                    {
                        return CreateCodecResult(
                            WebSocketParseStatus.Complete,
                            sourceIndex,
                            destinationIndex,
                            false,
                            "");
                    }

                    if (remaining == 1 && source[sourceIndex] == 0)
                    {
                        sourceIndex = sourceIndex + 1;
                        IncomingFinalPaddingComplete = true;
                        return CreateCodecResult(
                            WebSocketParseStatus.Complete,
                            sourceIndex,
                            destinationIndex,
                            false,
                            "");
                    }

                    return FailIncoming(
                        WebSocketParseStatus.ProtocolFailure,
                        "permessage-deflate-final-block-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                if (!decoded.SyncFlushComplete)
                {
                    return FailIncoming(
                        WebSocketParseStatus.ProtocolFailure,
                        "permessage-deflate-incoming-state-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                IncomingSyncFlushBoundary = true;
                if (sourceIndex < source.Length)
                {
                    IncomingSyncFlushBoundary = false;
                    continue;
                }

                return CreateCodecResult(
                    WebSocketParseStatus.Complete,
                    sourceIndex,
                    destinationIndex,
                    false,
                    "");
            }

            WebSocketParseStatus status = decoded.Status;
            if (status == WebSocketParseStatus.NeedInput &&
                sourceIndex == source.Length)
            {
                status = WebSocketParseStatus.Complete;
            }

            return CreateCodecResult(
                status,
                sourceIndex,
                destinationIndex,
                false,
                "");
        }
    }

    internal PerMessageDeflateCodecResult FinishDecompressedBytes(
        Span<byte> destination)
    {
        if (IncomingFailed)
        {
            return CreateIncomingFailureResult();
        }

        if (IncomingFinalBlockComplete)
        {
            if (!IncomingFinalPaddingComplete)
            {
                return FailIncoming(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-final-block-invalid",
                    0,
                    0);
            }

            return CompleteIncomingMessage(0);
        }

        if (IncomingSyncFlushBoundary)
        {
            return FailIncoming(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-tail-not-removed",
                0,
                0);
        }

        IncomingMessageActive = true;
        RawDeflateDecodeResult decoded =
            IncomingDecoder.DecodeRawDeflate(
                SyncFlushTail.AsSpan(IncomingTailOffset),
                destination);
        IncomingTailOffset = IncomingTailOffset + decoded.ConsumedCount;
        if (decoded.Status == WebSocketParseStatus.ProtocolFailure ||
            decoded.Status == WebSocketParseStatus.LimitRejected)
        {
            return FailIncoming(
                decoded.Status,
                decoded.ErrorIdentifier,
                0,
                decoded.ProducedCount);
        }

        if (decoded.Status == WebSocketParseStatus.Complete)
        {
            if (!decoded.SyncFlushComplete || decoded.StreamComplete)
            {
                return FailIncoming(
                    WebSocketParseStatus.ProtocolFailure,
                    "permessage-deflate-final-block-invalid",
                    0,
                    decoded.ProducedCount);
            }

            return CompleteIncomingMessage(decoded.ProducedCount);
        }

        if (decoded.Status == WebSocketParseStatus.NeedOutputCapacity)
        {
            return CreateCodecResult(
                decoded.Status,
                0,
                decoded.ProducedCount,
                false,
                "");
        }

        if (IncomingTailOffset == SyncFlushTail.Length)
        {
            RawDeflateDecodeResult finished =
                IncomingDecoder.FinishRawDeflateInput();
            string identifier = finished.ErrorIdentifier;
            if (identifier.Length == 0)
            {
                identifier = "permessage-deflate-payload-truncated";
            }

            return FailIncoming(
                WebSocketParseStatus.ProtocolFailure,
                identifier,
                0,
                decoded.ProducedCount);
        }

        return CreateCodecResult(
            WebSocketParseStatus.NeedOutputCapacity,
            0,
            decoded.ProducedCount,
            false,
            "");
    }

    internal WebSocketParseStatus CompleteOutgoingUncompressedMessage()
    {
        if (OutgoingMessageActive || OutgoingRawBytePending)
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        return WebSocketParseStatus.Complete;
    }

    internal WebSocketParseStatus CompleteIncomingUncompressedMessage()
    {
        if (IncomingMessageActive || IncomingTailOffset != 0)
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        return WebSocketParseStatus.Complete;
    }

    internal void ResetPerMessageDeflateCodec()
    {
        OutgoingEncoder.ResetRawDeflateEncoder(false);
        IncomingDecoder.ResetRawDeflateDecoder(false);
        OutgoingTail.AsSpan().Clear();
        OutgoingTailCount = 0;
        OutgoingRawByte[0] = 0;
        OutgoingRawBytePending = false;
        OutgoingRawFlushComplete = false;
        OutgoingMessageActive = false;
        IncomingMessageActive = false;
        IncomingTailOffset = 0;
        IncomingSyncFlushBoundary = false;
        IncomingFinalBlockComplete = false;
        IncomingFinalPaddingComplete = false;
        OutgoingFailed = false;
        IncomingFailed = false;
        OutgoingFailureStatus = WebSocketParseStatus.ProtocolFailure;
        IncomingFailureStatus = WebSocketParseStatus.ProtocolFailure;
        OutgoingFailureIdentifier = "";
        IncomingFailureIdentifier = "";
    }

    internal int GetOutgoingCompressionHistoryCount()
    {
        return OutgoingEncoder.GetRawDeflateEncoderHistoryCount();
    }

    internal int GetIncomingCompressionHistoryCount()
    {
        return IncomingDecoder.GetRawDeflateHistoryCount();
    }

    int DrainOutgoingRawByte(Span<byte> destination)
    {
        if (!OutgoingRawBytePending)
        {
            return 0;
        }

        if (OutgoingTailCount < OutgoingTail.Length)
        {
            OutgoingTail[OutgoingTailCount] = OutgoingRawByte[0];
            OutgoingTailCount = OutgoingTailCount + 1;
            OutgoingRawBytePending = false;
            return 0;
        }

        if (destination.Length == 0)
        {
            return 0;
        }

        destination[0] = OutgoingTail[0];
        OutgoingTail[0] = OutgoingTail[1];
        OutgoingTail[1] = OutgoingTail[2];
        OutgoingTail[2] = OutgoingTail[3];
        OutgoingTail[3] = OutgoingRawByte[0];
        OutgoingRawBytePending = false;
        return 1;
    }

    PerMessageDeflateCodecResult CompleteOutgoingMessage(int producedCount)
    {
        if (OutgoingTailCount != SyncFlushTail.Length ||
            OutgoingTail[0] != SyncFlushTail[0] ||
            OutgoingTail[1] != SyncFlushTail[1] ||
            OutgoingTail[2] != SyncFlushTail[2] ||
            OutgoingTail[3] != SyncFlushTail[3])
        {
            return FailOutgoing(
                WebSocketParseStatus.ProtocolFailure,
                "permessage-deflate-tail-invalid",
                0,
                producedCount);
        }

        OutgoingEncoder.ResetRawDeflateEncoder(
            OutgoingContextTakeover);
        OutgoingTail.AsSpan().Clear();
        OutgoingTailCount = 0;
        OutgoingRawFlushComplete = false;
        OutgoingMessageActive = false;
        return CreateCodecResult(
            WebSocketParseStatus.Complete,
            0,
            producedCount,
            true,
            "");
    }

    PerMessageDeflateCodecResult CompleteIncomingMessage(int producedCount)
    {
        IncomingDecoder.ResetRawDeflateDecoder(
            IncomingContextTakeover);
        IncomingTailOffset = 0;
        IncomingSyncFlushBoundary = false;
        IncomingFinalBlockComplete = false;
        IncomingFinalPaddingComplete = false;
        IncomingMessageActive = false;
        return CreateCodecResult(
            WebSocketParseStatus.Complete,
            0,
            producedCount,
            true,
            "");
    }

    PerMessageDeflateCodecResult FailOutgoing(
        WebSocketParseStatus status,
        string identifier,
        int consumedCount,
        int producedCount)
    {
        OutgoingFailed = true;
        OutgoingFailureStatus = status;
        OutgoingFailureIdentifier = identifier;
        return CreateCodecResult(
            status,
            consumedCount,
            producedCount,
            false,
            identifier);
    }

    PerMessageDeflateCodecResult FailIncoming(
        WebSocketParseStatus status,
        string identifier,
        int consumedCount,
        int producedCount)
    {
        IncomingFailed = true;
        IncomingFailureStatus = status;
        IncomingFailureIdentifier = identifier;
        return CreateCodecResult(
            status,
            consumedCount,
            producedCount,
            false,
            identifier);
    }

    PerMessageDeflateCodecResult CreateOutgoingFailureResult()
    {
        return CreateCodecResult(
            OutgoingFailureStatus,
            0,
            0,
            false,
            OutgoingFailureIdentifier);
    }

    PerMessageDeflateCodecResult CreateIncomingFailureResult()
    {
        return CreateCodecResult(
            IncomingFailureStatus,
            0,
            0,
            false,
            IncomingFailureIdentifier);
    }

    PerMessageDeflateCodecResult CreateCodecResult(
        WebSocketParseStatus status,
        int consumedCount,
        int producedCount,
        bool messageComplete,
        string errorIdentifier)
    {
        PerMessageDeflateCodecResult result =
            new PerMessageDeflateCodecResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.ProducedCount = producedCount;
        result.MessageComplete = messageComplete;
        result.ErrorIdentifier = errorIdentifier;
        return result;
    }
}
