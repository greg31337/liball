using System;

namespace http;

internal class Http2HeaderBlockResult
{
    internal HttpOperationStatus Status;
    internal Http2ErrorScope ErrorScope;
    internal Http2ErrorCode ErrorCode;
    internal string? ErrorCodeName;
    internal string? ErrorMessage;
    internal Http2MessageResult? Message;
}

internal class Http2HeaderBlockState
{
    internal Http2EndpointRole endpointRole;
    internal HpackEncoder encoder = new HpackEncoder();
    internal HpackDecoder decoder = new HpackDecoder();
    internal HpackEncoder? preparedEncoder;
    internal HpackOptions encoderOptions = new HpackOptions();
    internal HpackOptions decoderOptions = new HpackOptions();
    internal int maximumOutboundCompressedSize;
    internal int maximumInboundCompressedSize;
    internal int configuredEncoderHeaderListSize;
    internal bool outboundActive;
    internal int outboundStreamId;
    internal int outboundCompressedSize;
    internal bool inboundActive;
    internal int inboundStreamId;
    internal bool inboundEndStream;
    internal Http2FrameType inboundInitialFrameType;
    internal int inboundPromisedStreamId;
    internal byte[] inboundBytes = Array.Empty<byte>();

    internal void ResetHeaderBlocks(
        Http2EndpointRole role,
        HpackOptions outboundOptions,
        HpackOptions inboundOptions,
        HttpLimits connectionLimits)
    {
        endpointRole = role;
        encoder = new HpackEncoder();
        decoder = new HpackDecoder();
        preparedEncoder = null;
        encoderOptions = CopyHpackOptions(outboundOptions, connectionLimits);
        decoderOptions = CopyHpackOptions(inboundOptions, connectionLimits);
        maximumOutboundCompressedSize = MinimumValue(
            encoderOptions.MaximumCompressedBlockSize,
            encoderOptions.Limits.MaximumCompressedBlockSize);
        maximumInboundCompressedSize = MinimumValue(
            decoderOptions.MaximumCompressedBlockSize,
            decoderOptions.Limits.MaximumCompressedBlockSize);
        configuredEncoderHeaderListSize =
            encoderOptions.MaximumHeaderListSize;
        outboundActive = false;
        outboundStreamId = 0;
        outboundCompressedSize = 0;
        inboundActive = false;
        inboundStreamId = 0;
        inboundEndStream = false;
        inboundInitialFrameType = Http2FrameType.Unknown;
        inboundPromisedStreamId = 0;
        inboundBytes = Array.Empty<byte>();
    }

    internal void ApplyRemoteHeaderTableSetting(long maximumSize)
    {
        long selected = maximumSize;
        if (selected > encoderOptions.MaximumDynamicTableSize)
        {
            selected = encoderOptions.MaximumDynamicTableSize;
        }

        if (selected > int.MaxValue)
        {
            selected = int.MaxValue;
        }

        encoder.UpdateEncoderTableLimit((int)selected);
    }

    internal void ApplyAcknowledgedLocalHeaderTableSetting(long maximumSize)
    {
        long selected = maximumSize;
        if (selected > decoderOptions.MaximumDynamicTableSize)
        {
            selected = decoderOptions.MaximumDynamicTableSize;
        }

        if (selected > int.MaxValue)
        {
            selected = int.MaxValue;
        }

        decoder.UpdateDecoderTableLimit((int)selected);
    }

    internal void ApplyRemoteHeaderListSetting(long maximumSize)
    {
        long selected = maximumSize;
        if (selected > configuredEncoderHeaderListSize)
        {
            selected = configuredEncoderHeaderListSize;
        }

        if (selected > int.MaxValue)
        {
            selected = int.MaxValue;
        }

        encoderOptions.MaximumHeaderListSize = (int)selected;
    }

    internal Http2HeaderBlockResult ValidateOutboundSequence(
        Http2Action action)
    {
        Http2HeaderBlockResult result = CreateDoneResult();
        if (action.Frame == null)
        {
            return RejectLocalHeaderBlock(
                result,
                "invalid-http2-send-frame-action",
                "A send-frame action requires an HTTP/2 frame.");
        }

        Http2Frame frame = action.Frame;
        if (outboundActive)
        {
            if (frame.FrameType != Http2FrameType.Continuation ||
                frame.StreamId != outboundStreamId)
            {
                return RejectLocalHeaderBlock(
                    result,
                    "http2-outbound-header-block-interleaved",
                    "Only a CONTINUATION frame on the active stream can follow an incomplete header block.");
            }

            if (action.Message != null)
            {
                return RejectLocalHeaderBlock(
                    result,
                    "http2-outbound-continuation-message",
                    "A CONTINUATION frame cannot start a new encoded message.");
            }

            return result;
        }

        if (frame.FrameType == Http2FrameType.Continuation)
        {
            return RejectLocalHeaderBlock(
                result,
                "http2-outbound-unexpected-continuation",
                "A CONTINUATION frame requires an incomplete header block.");
        }

        if (action.Message != null &&
            !IsHeaderStartFrame(frame.FrameType) &&
            frame.FrameType != Http2FrameType.Data)
        {
            return RejectLocalHeaderBlock(
                result,
                "http2-outbound-message-requires-headers",
                "An encoded header message requires HEADERS or PUSH_PROMISE.");
        }

        if (action.Message != null &&
            IsHeaderStartFrame(frame.FrameType) && !frame.EndHeaders)
        {
            return RejectLocalHeaderBlock(
                result,
                "http2-outbound-message-requires-end-headers",
                "An automatically encoded header message must complete in one frame.");
        }

        if (action.Message != null &&
            IsHeaderStartFrame(frame.FrameType) &&
            frame.HeaderBlockFragment != null &&
            frame.HeaderBlockFragment.Length != 0)
        {
            return RejectLocalHeaderBlock(
                result,
                "ambiguous-http2-outbound-header-block",
                "Provide encoded fields or a compressed fragment, but not both.");
        }

        return result;
    }

    internal Http2HeaderBlockResult PrepareOutboundFrame(Http2Action action)
    {
        Http2HeaderBlockResult result = CreateDoneResult();
        if (action.Frame == null)
        {
            return RejectLocalHeaderBlock(
                result,
                "invalid-http2-send-frame-action",
                "A send-frame action requires an HTTP/2 frame.");
        }

        Http2Frame frame = action.Frame;
        preparedEncoder = null;
        if (!IsHeaderStartFrame(frame.FrameType) &&
            frame.FrameType != Http2FrameType.Continuation)
        {
            return result;
        }

        if (action.Message != null)
        {
            HpackEncoder workingEncoder = CloneEncoderState();
            HpackResult encoded = workingEncoder.EncodeHeaderBlock(
                action.Message.Headers, encoderOptions);
            if (encoded.Status != HttpOperationStatus.Done)
            {
                return RejectLocalHeaderBlock(
                    result,
                    encoded.ErrorCode ?? "http2-hpack-encode-failed",
                    encoded.ErrorMessage ??
                        "The outbound HTTP/2 header block could not be encoded.");
            }

            frame.HeaderBlockFragment = encoded.Bytes;
            action.Message.Status = HttpOperationStatus.Done;
            preparedEncoder = workingEncoder;
        }

        if (frame.HeaderBlockFragment == null)
        {
            return RejectLocalHeaderBlock(
                result,
                "invalid-http2-header-block-fragment",
                "A header-block fragment is required.");
        }

        long aggregateSize = frame.HeaderBlockFragment.Length;
        if (outboundActive)
        {
            aggregateSize = aggregateSize + outboundCompressedSize;
        }

        if (aggregateSize > maximumOutboundCompressedSize)
        {
            preparedEncoder = null;
            return RejectLocalHeaderBlock(
                result,
                "http2-outbound-compressed-header-block-limit",
                "The outbound compressed header block exceeds the configured aggregate limit.");
        }

        return result;
    }

    internal void ApplyOutboundFrame(Http2Frame frame)
    {
        if (preparedEncoder != null)
        {
            encoder = preparedEncoder;
            preparedEncoder = null;
        }

        if (IsHeaderStartFrame(frame.FrameType))
        {
            if (frame.EndHeaders)
            {
                outboundActive = false;
                outboundStreamId = 0;
                outboundCompressedSize = 0;
                return;
            }

            outboundActive = true;
            outboundStreamId = frame.StreamId;
            outboundCompressedSize = frame.HeaderBlockFragment.Length;
            return;
        }

        if (frame.FrameType == Http2FrameType.Continuation)
        {
            outboundCompressedSize = outboundCompressedSize +
                frame.HeaderBlockFragment.Length;
            if (frame.EndHeaders)
            {
                outboundActive = false;
                outboundStreamId = 0;
                outboundCompressedSize = 0;
            }
        }
    }

    internal void CancelPreparedOutboundFrame()
    {
        preparedEncoder = null;
    }

    internal Http2HeaderBlockResult ValidateInboundSequence(Http2Frame frame)
    {
        Http2HeaderBlockResult result = CreateDoneResult();
        if (inboundActive)
        {
            if (frame.FrameType != Http2FrameType.Continuation ||
                frame.StreamId != inboundStreamId)
            {
                return RejectInboundHeaderBlock(
                    result,
                    Http2ErrorCode.ProtocolError,
                    "http2-inbound-header-block-interleaved",
                    "Only a CONTINUATION frame on the active stream can follow an incomplete header block.");
            }

            return result;
        }

        if (frame.FrameType == Http2FrameType.Continuation)
        {
            return RejectInboundHeaderBlock(
                result,
                Http2ErrorCode.ProtocolError,
                "http2-inbound-unexpected-continuation",
                "A CONTINUATION frame requires an incomplete header block.");
        }

        return result;
    }

    internal Http2HeaderBlockResult ApplyInboundFrame(Http2Frame frame)
    {
        Http2HeaderBlockResult result = CreateDoneResult();
        if (!IsHeaderStartFrame(frame.FrameType) &&
            frame.FrameType != Http2FrameType.Continuation)
        {
            return result;
        }

        if (frame.HeaderBlockFragment == null)
        {
            return RejectInboundHeaderBlock(
                result,
                Http2ErrorCode.ProtocolError,
                "invalid-http2-header-block-fragment",
                "A header-block fragment is required.");
        }

        if (IsHeaderStartFrame(frame.FrameType))
        {
            inboundStreamId = frame.StreamId;
            inboundEndStream = frame.EndStream;
            inboundInitialFrameType = frame.FrameType;
            inboundPromisedStreamId = frame.HasPromisedStreamId ?
                frame.PromisedStreamId : 0;
            inboundBytes = Array.Empty<byte>();
        }

        if ((long)inboundBytes.Length + frame.HeaderBlockFragment.Length >
            maximumInboundCompressedSize)
        {
            inboundActive = false;
            inboundBytes = Array.Empty<byte>();
            return RejectInboundHeaderBlock(
                result,
                Http2ErrorCode.EnhanceYourCalm,
                "http2-inbound-compressed-header-block-limit",
                "The inbound compressed header block exceeds the configured aggregate limit.");
        }

        inboundBytes = AppendFragment(
            inboundBytes, frame.HeaderBlockFragment);
        if (!frame.EndHeaders)
        {
            inboundActive = true;
            return result;
        }

        inboundActive = false;
        byte[] completeBlock = inboundBytes;
        inboundBytes = Array.Empty<byte>();
        HpackResult decoded = decoder.DecodeHeaderBlock(
            completeBlock, decoderOptions);
        if (decoded.Status != HttpOperationStatus.Done)
        {
            if (decoded.IsCompressionError ||
                decoded.Consumed != completeBlock.Length)
            {
                return RejectInboundHeaderBlock(
                    result,
                    Http2ErrorCode.CompressionError,
                    decoded.ErrorCode ?? "http2-hpack-compression-error",
                    decoded.ErrorMessage ??
                        "The inbound HPACK block is malformed.");
            }

            return RejectInboundHeaderList(
                result,
                decoded.ErrorCode ?? "http2-header-list-limit",
                decoded.ErrorMessage ??
                    "The decoded HTTP/2 header list exceeds a configured limit.");
        }

        Http2MessageResult message = new Http2MessageResult();
        message.Status = HttpOperationStatus.Done;
        message.StreamId = inboundInitialFrameType ==
            Http2FrameType.PushPromise && inboundPromisedStreamId != 0 ?
            inboundPromisedStreamId : inboundStreamId;
        message.MessageKind = inboundInitialFrameType ==
            Http2FrameType.PushPromise ||
            endpointRole == Http2EndpointRole.Server ?
            HttpMessageKind.Request : HttpMessageKind.Response;
        message.Headers = decoded.Headers;
        message.EndStream = inboundEndStream;
        if (inboundInitialFrameType == Http2FrameType.PushPromise)
        {
            message.IsPushed = true;
            message.HasAssociatedStreamId = true;
            message.AssociatedStreamId = inboundStreamId;
        }

        result.Message = message;
        return result;
    }

    internal HpackEncoder CloneEncoderState()
    {
        HpackEncoder copy = new HpackEncoder();
        copy.dynamicTable = encoder.dynamicTable.CloneTable();
        copy.maximumDynamicTableSize = encoder.maximumDynamicTableSize;
        copy.minimumPendingDynamicTableSize =
            encoder.minimumPendingDynamicTableSize;
        copy.finalPendingDynamicTableSize =
            encoder.finalPendingDynamicTableSize;
        copy.requiresDynamicTableSizeUpdate =
            encoder.requiresDynamicTableSizeUpdate;
        return copy;
    }

    internal HpackOptions CopyHpackOptions(
        HpackOptions source,
        HttpLimits connectionLimits)
    {
        HpackOptions copy = new HpackOptions();
        copy.MaximumDynamicTableSize = source.MaximumDynamicTableSize;
        copy.DynamicTableSize = source.DynamicTableSize;
        copy.MaximumHeaderListSize = source.MaximumHeaderListSize;
        copy.MaximumCompressedBlockSize = source.MaximumCompressedBlockSize;
        copy.MaximumStringSize = source.MaximumStringSize;
        copy.MaximumHeaderCount = source.MaximumHeaderCount;
        copy.IndexingMode = source.IndexingMode;
        copy.UseHuffman = source.UseHuffman;
        copy.UseHuffmanOnlyWhenSmaller = source.UseHuffmanOnlyWhenSmaller;
        copy.NeverIndexSensitiveFields = source.NeverIndexSensitiveFields;
        HttpLimits limits = new HttpLimits();
        limits.MaximumFieldCount = MinimumValue(
            source.Limits.MaximumFieldCount,
            connectionLimits.MaximumFieldCount);
        limits.MaximumHeaderListSize = MinimumValue(
            source.Limits.MaximumHeaderListSize,
            connectionLimits.MaximumHeaderListSize);
        limits.MaximumCompressedBlockSize = MinimumValue(
            source.Limits.MaximumCompressedBlockSize,
            connectionLimits.MaximumCompressedBlockSize);
        limits.MaximumStringSize = MinimumValue(
            source.Limits.MaximumStringSize,
            connectionLimits.MaximumStringSize);
        copy.Limits = limits;
        return copy;
    }

    internal int MinimumValue(int first, int second)
    {
        return first < second ? first : second;
    }

    internal bool IsHeaderStartFrame(Http2FrameType frameType)
    {
        return frameType == Http2FrameType.Headers ||
            frameType == Http2FrameType.PushPromise;
    }

    internal byte[] AppendFragment(byte[] existing, byte[] fragment)
    {
        byte[] combined = new byte[existing.Length + fragment.Length];
        int index = 0;
        while (index < existing.Length)
        {
            combined[index] = existing[index];
            index = index + 1;
        }

        int fragmentIndex = 0;
        while (fragmentIndex < fragment.Length)
        {
            combined[existing.Length + fragmentIndex] =
                fragment[fragmentIndex];
            fragmentIndex = fragmentIndex + 1;
        }

        return combined;
    }

    internal Http2HeaderBlockResult CreateDoneResult()
    {
        Http2HeaderBlockResult result = new Http2HeaderBlockResult();
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal Http2HeaderBlockResult RejectLocalHeaderBlock(
        Http2HeaderBlockResult result,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCodeName = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal Http2HeaderBlockResult RejectInboundHeaderBlock(
        Http2HeaderBlockResult result,
        Http2ErrorCode errorCode,
        string errorName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorScope = Http2ErrorScope.Connection;
        result.ErrorCode = errorCode;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal Http2HeaderBlockResult RejectInboundHeaderList(
        Http2HeaderBlockResult result,
        string errorName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorScope = Http2ErrorScope.Stream;
        result.ErrorCode = Http2ErrorCode.EnhanceYourCalm;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
