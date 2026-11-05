using System;

namespace http;

/// <summary>
/// Parses and serializes HTTP/2 frames without performing transport I/O.
/// </summary>
public class Http2FrameCodec : IHttp2FrameCodec
{
    internal int configuredMaximumFrameSize =
        Http2Constants.DefaultMaximumFrameSize;
    internal int peerMaximumFrameSize = Http2Constants.DefaultMaximumFrameSize;
    internal bool directionalMaximumFrameSizes;
    internal bool ignoreRfc7540Priorities;

    /// <summary>Creates an HTTP/2 frame codec with protocol-default limits.</summary>
    public Http2FrameCodec()
    {
    }

    /// <summary>Parses one complete HTTP/2 frame when enough input is available.</summary>
    /// <param name="input">The available frame bytes.</param>
    /// <returns>The parse status, exact consumption, and decoded frame header.</returns>
    public Http2FrameResult ParseHttp2Frame(byte[] input)
    {
        Http2FrameResult result = new Http2FrameResult();
        if (input == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-input";
            result.ErrorMessage = "HTTP/2 frame input is required.";
            return result;
        }

        if (configuredMaximumFrameSize < 0 || peerMaximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (input.Length < Http2Constants.FrameHeaderSize)
        {
            result.Status = HttpOperationStatus.NeedInput;
            result.RequiredSize = Http2Constants.FrameHeaderSize;
            return result;
        }

        Http2Frame frame = ParseFrameHeader(input);
        result.Frame = frame;
        int effectiveMaximum = GetInboundMaximumFrameSize();

        int requiredSize = Http2Constants.FrameHeaderSize + frame.Length;
        result.RequiredSize = requiredSize;
        if (frame.Length > effectiveMaximum)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = Http2Constants.FrameHeaderSize;
            result.HasErrorPosition = true;
            result.ErrorPosition = 0;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The HTTP/2 frame payload exceeds the active maximum frame size.";
            return result;
        }

        if (input.Length < requiredSize)
        {
            result.Status = HttpOperationStatus.NeedInput;
            return result;
        }

        frame.Payload = CopyPayload(input, frame.Length);
        result.Status = HttpOperationStatus.Done;
        result.Consumed = requiredSize;
        if (frame.FrameType == Http2FrameType.Data)
        {
            return DecodeDataFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.Headers ||
            frame.FrameType == Http2FrameType.Continuation)
        {
            Http2FrameResult decoded = DecodeHeaderBlockFrame(result, frame);
            if (frame.HasPriority)
            {
                frame.PriorityIgnored = ignoreRfc7540Priorities;
            }

            return decoded;
        }

        if (frame.FrameType == Http2FrameType.Priority)
        {
            return DecodePriorityFrame(
                result, frame, ignoreRfc7540Priorities);
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            return DecodeResetStreamFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.Settings)
        {
            return DecodeSettingsFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            return DecodePushPromiseFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.Ping)
        {
            return DecodePingFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.GoAway)
        {
            return DecodeGoAwayFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.WindowUpdate)
        {
            return DecodeWindowUpdateFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.PriorityUpdate)
        {
            return DecodePriorityUpdateFrame(result, frame);
        }

        if (frame.FrameType == Http2FrameType.Origin)
        {
            return DecodeOriginFrame(result, frame);
        }

        return result;
    }

    /// <summary>Serializes one HTTP/2 frame into owned bytes.</summary>
    /// <param name="frame">The frame to serialize.</param>
    /// <returns>The serialization status and encoded bytes.</returns>
    public Http2FrameResult SerializeHttp2Frame(Http2Frame frame)
    {
        return BuildHttp2Frame(frame, GetOutboundMaximumFrameSize());
    }

    /// <summary>Writes one HTTP/2 frame into bounded output storage.</summary>
    /// <param name="frame">The frame to serialize.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The bounded-write status.</returns>
    public Http2FrameResult WriteHttp2Frame(
        Http2Frame frame,
        Span<byte> output)
    {
        Http2FrameResult result = BuildHttp2Frame(
            frame, GetOutboundMaximumFrameSize());
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        if (output.Length < result.RequiredSize)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            result.Written = 0;
            result.Bytes = Array.Empty<byte>();
            return result;
        }

        int index = 0;
        while (index < result.Bytes.Length)
        {
            output[index] = result.Bytes[index];
            index = index + 1;
        }

        result.Bytes = Array.Empty<byte>();
        return result;
    }

    internal static Http2Frame ParseFrameHeader(byte[] input)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = input[0] << 16 | input[1] << 8 | input[2];
        frame.TypeCode = input[3];
        frame.FrameType = GetFrameType(input[3]);
        frame.IsKnownType = frame.FrameType != Http2FrameType.Unknown;
        frame.Flags = input[4];
        frame.ReservedBit = (input[5] & 0x80) != 0;
        frame.StreamId = (input[5] & 0x7f) << 24 |
            input[6] << 16 | input[7] << 8 | input[8];
        return frame;
    }

    internal static Http2FrameType GetFrameType(byte typeCode)
    {
        switch (typeCode)
        {
            case 0x00:
                return Http2FrameType.Data;
            case 0x01:
                return Http2FrameType.Headers;
            case 0x02:
                return Http2FrameType.Priority;
            case 0x03:
                return Http2FrameType.RstStream;
            case 0x04:
                return Http2FrameType.Settings;
            case 0x05:
                return Http2FrameType.PushPromise;
            case 0x06:
                return Http2FrameType.Ping;
            case 0x07:
                return Http2FrameType.GoAway;
            case 0x08:
                return Http2FrameType.WindowUpdate;
            case 0x09:
                return Http2FrameType.Continuation;
            case 0x0c:
                return Http2FrameType.Origin;
            case 0x10:
                return Http2FrameType.PriorityUpdate;
            default:
                return Http2FrameType.Unknown;
        }
    }

    internal static byte[] CopyPayload(byte[] input, int payloadLength)
    {
        byte[] payload = new byte[payloadLength];
        int index = 0;
        while (index < payloadLength)
        {
            payload[index] = input[Http2Constants.FrameHeaderSize + index];
            index = index + 1;
        }

        return payload;
    }

    internal int GetInboundMaximumFrameSize()
    {
        int effectiveMaximum = Http2Constants.MaximumFrameSize;
        if (configuredMaximumFrameSize < effectiveMaximum)
        {
            effectiveMaximum = configuredMaximumFrameSize;
        }

        if (!directionalMaximumFrameSizes &&
            peerMaximumFrameSize < effectiveMaximum)
        {
            effectiveMaximum = peerMaximumFrameSize;
        }

        return effectiveMaximum;
    }

    internal int GetOutboundMaximumFrameSize()
    {
        int effectiveMaximum = Http2Constants.MaximumFrameSize;
        if (peerMaximumFrameSize < effectiveMaximum)
        {
            effectiveMaximum = peerMaximumFrameSize;
        }

        if (!directionalMaximumFrameSizes &&
            configuredMaximumFrameSize < effectiveMaximum)
        {
            effectiveMaximum = configuredMaximumFrameSize;
        }

        return effectiveMaximum;
    }

    internal static Http2FrameResult BuildHttp2Frame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        if (frame == null)
        {
            Http2FrameResult missing = new Http2FrameResult();
            missing.Status = HttpOperationStatus.Rejected;
            missing.ErrorCode = "invalid-http2-frame";
            missing.ErrorMessage = "An HTTP/2 frame is required.";
            return missing;
        }

        if (frame.FrameType == Http2FrameType.Data)
        {
            return BuildDataFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Headers ||
            frame.FrameType == Http2FrameType.Continuation)
        {
            return BuildHeaderBlockFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Priority)
        {
            return BuildPriorityFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            return BuildResetStreamFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Settings)
        {
            return BuildSettingsFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            return BuildPushPromiseFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Ping)
        {
            return BuildPingFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.GoAway)
        {
            return BuildGoAwayFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.WindowUpdate)
        {
            return BuildWindowUpdateFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.PriorityUpdate)
        {
            return BuildPriorityUpdateFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Origin)
        {
            return BuildOriginFrame(frame, maximumFrameSize);
        }

        if (frame.FrameType == Http2FrameType.Unknown)
        {
            return BuildUnknownFrame(frame, maximumFrameSize);
        }

        Http2FrameResult unsupported = new Http2FrameResult();
        unsupported.Status = HttpOperationStatus.Unsupported;
        unsupported.Frame = frame;
        unsupported.ErrorCode = "http2-frame-serialization-not-implemented";
        unsupported.ErrorMessage =
            "Serialization for this HTTP/2 frame type is not implemented.";
        return unsupported;
    }

    internal static Http2FrameResult DecodeDataFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId == 0)
        {
            return RejectDataFrame(
                result,
                frame,
                "http2-data-stream-zero",
                "A DATA frame must identify a nonzero stream.");
        }

        frame.EndStream = (frame.Flags & Http2Constants.EndStreamFlag) != 0;
        frame.IsPadded = (frame.Flags & Http2Constants.PaddedFlag) != 0;
        frame.FlowControlledLength = frame.Length;
        int dataOffset = 0;
        int dataLength = frame.Payload.Length;
        if (frame.IsPadded)
        {
            if (frame.Payload.Length == 0)
            {
                return RejectDataFrame(
                    result,
                    frame,
                    "http2-data-pad-length-missing",
                    "A padded DATA frame requires a pad-length octet.");
            }

            frame.PaddingLength = frame.Payload[0];
            if (frame.PaddingLength >= frame.Payload.Length)
            {
                return RejectDataFrame(
                    result,
                    frame,
                    "http2-data-padding-invalid",
                    "DATA padding must leave a nonnegative application-data region.");
            }

            dataOffset = 1;
            dataLength = frame.Payload.Length - frame.PaddingLength - 1;
        }

        frame.Data = CopyDataRange(frame.Payload, dataOffset, dataLength);
        return result;
    }

    internal static Http2FrameResult BuildDataFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        if (frame == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame";
            result.ErrorMessage = "An HTTP/2 frame is required.";
            return result;
        }

        result.Frame = frame;
        if (frame.FrameType != Http2FrameType.Data)
        {
            result.Status = HttpOperationStatus.Unsupported;
            result.ErrorCode = "http2-frame-serialization-not-implemented";
            result.ErrorMessage =
                "Serialization for this HTTP/2 frame type is not implemented.";
            return result;
        }

        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId <= 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-data-stream-zero";
            result.ErrorMessage = "A DATA frame must identify a valid nonzero stream.";
            return result;
        }

        if (frame.Data == null || frame.PaddingLength < 0 ||
            frame.PaddingLength > 255 ||
            !frame.IsPadded && frame.PaddingLength != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-data-frame";
            result.ErrorMessage =
                "DATA bytes and consistent padding metadata are required.";
            return result;
        }

        long payloadLength = frame.Data.Length;
        if (frame.IsPadded)
        {
            payloadLength = payloadLength + frame.PaddingLength + 1;
        }

        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The DATA payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x00;
        byte flags = (byte)(frame.Flags & 0xf6);
        if (frame.EndStream)
        {
            flags = (byte)(flags | Http2Constants.EndStreamFlag);
        }

        if (frame.IsPadded)
        {
            flags = (byte)(flags | Http2Constants.PaddedFlag);
        }

        output[4] = flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadOffset = Http2Constants.FrameHeaderSize;
        if (frame.IsPadded)
        {
            output[payloadOffset] = (byte)frame.PaddingLength;
            payloadOffset = payloadOffset + 1;
        }

        int dataIndex = 0;
        while (dataIndex < frame.Data.Length)
        {
            output[payloadOffset + dataIndex] = frame.Data[dataIndex];
            dataIndex = dataIndex + 1;
        }

        Http2Frame snapshot = CreateDataSnapshot(
            frame, flags, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2FrameResult DecodeHeaderBlockFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId == 0)
        {
            return RejectHeaderBlockFrame(
                result,
                frame,
                5,
                "http2-header-block-stream-zero",
                "HEADERS and CONTINUATION frames require a nonzero stream.");
        }

        frame.EndHeaders =
            (frame.Flags & Http2Constants.EndHeadersFlag) != 0;
        if (frame.FrameType == Http2FrameType.Continuation)
        {
            frame.HeaderBlockFragment = CopyDataRange(
                frame.Payload, 0, frame.Payload.Length);
            return result;
        }

        frame.EndStream =
            (frame.Flags & Http2Constants.EndStreamFlag) != 0;
        frame.IsPadded = (frame.Flags & Http2Constants.PaddedFlag) != 0;
        frame.HasPriority = (frame.Flags & Http2Constants.PriorityFlag) != 0;
        int fragmentOffset = 0;
        int fragmentEnd = frame.Payload.Length;
        if (frame.IsPadded)
        {
            if (frame.Payload.Length == 0)
            {
                return RejectHeaderBlockFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize,
                    "http2-headers-pad-length-missing",
                    "A padded HEADERS frame requires a pad-length octet.");
            }

            frame.PaddingLength = frame.Payload[0];
            fragmentOffset = 1;
            fragmentEnd = frame.Payload.Length - frame.PaddingLength;
            if (fragmentEnd < fragmentOffset)
            {
                return RejectHeaderBlockFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize,
                    "http2-headers-padding-invalid",
                    "HEADERS padding exceeds the available payload.");
            }
        }

        if (frame.HasPriority)
        {
            if (fragmentEnd - fragmentOffset <
                Http2Constants.PriorityPayloadSize)
            {
                return RejectHeaderBlockFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize + fragmentOffset,
                    "http2-headers-priority-truncated",
                    "HEADERS priority metadata requires five octets.");
            }

            int priorityOffset = fragmentOffset;
            frame.ExclusiveDependency =
                (frame.Payload[priorityOffset] & 0x80) != 0;
            frame.StreamDependency =
                (frame.Payload[priorityOffset] & 0x7f) << 24 |
                frame.Payload[priorityOffset + 1] << 16 |
                frame.Payload[priorityOffset + 2] << 8 |
                frame.Payload[priorityOffset + 3];
            frame.Weight = frame.Payload[priorityOffset + 4] + 1;
            if (frame.StreamDependency == frame.StreamId)
            {
                return RejectHeaderBlockFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize + priorityOffset,
                    "http2-headers-self-dependency",
                    "A HEADERS priority dependency cannot name its own stream.");
            }

            fragmentOffset = fragmentOffset +
                Http2Constants.PriorityPayloadSize;
        }

        frame.HeaderBlockFragment = CopyDataRange(
            frame.Payload,
            fragmentOffset,
            fragmentEnd - fragmentOffset);
        return result;
    }

    internal static Http2FrameResult BuildHeaderBlockFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId <= 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-header-block-stream-zero";
            result.ErrorMessage =
                "HEADERS and CONTINUATION frames require a valid nonzero stream.";
            return result;
        }

        if (frame.HeaderBlockFragment == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-header-block-frame";
            result.ErrorMessage = "A header-block fragment is required.";
            return result;
        }

        bool isHeaders = frame.FrameType == Http2FrameType.Headers;
        if (!isHeaders &&
            (frame.EndStream || frame.IsPadded || frame.PaddingLength != 0 ||
            frame.HasPriority || frame.ExclusiveDependency ||
            frame.StreamDependency != 0 || frame.Weight != 0))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-continuation-frame";
            result.ErrorMessage =
                "CONTINUATION frames cannot carry HEADERS-only metadata.";
            return result;
        }

        if (isHeaders &&
            (frame.PaddingLength < 0 || frame.PaddingLength > 255 ||
            !frame.IsPadded && frame.PaddingLength != 0))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-headers-padding";
            result.ErrorMessage =
                "HEADERS padding metadata must be internally consistent.";
            return result;
        }

        if (isHeaders && frame.HasPriority &&
            (frame.StreamDependency < 0 ||
            frame.StreamDependency > Http2Constants.MaximumStreamIdentifier ||
            frame.StreamDependency == frame.StreamId ||
            frame.Weight < 1 || frame.Weight > 256))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-headers-priority";
            result.ErrorMessage =
                "HEADERS priority metadata has an invalid dependency or weight.";
            return result;
        }

        if (isHeaders && !frame.HasPriority &&
            (frame.ExclusiveDependency || frame.StreamDependency != 0 ||
            frame.Weight != 0))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-headers-priority";
            result.ErrorMessage =
                "HEADERS priority metadata requires the priority indicator.";
            return result;
        }

        long payloadLength = frame.HeaderBlockFragment.Length;
        if (isHeaders && frame.IsPadded)
        {
            payloadLength = payloadLength + frame.PaddingLength + 1;
        }

        if (isHeaders && frame.HasPriority)
        {
            payloadLength = payloadLength + Http2Constants.PriorityPayloadSize;
        }

        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The header-block payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = isHeaders ? (byte)0x01 : (byte)0x09;
        byte flags = frame.Flags;
        if (isHeaders)
        {
            flags = (byte)(flags & 0xd2);
            if (frame.EndStream)
            {
                flags = (byte)(flags | Http2Constants.EndStreamFlag);
            }

            if (frame.IsPadded)
            {
                flags = (byte)(flags | Http2Constants.PaddedFlag);
            }

            if (frame.HasPriority)
            {
                flags = (byte)(flags | Http2Constants.PriorityFlag);
            }
        }
        else
        {
            flags = (byte)(flags & 0xfb);
        }

        if (frame.EndHeaders)
        {
            flags = (byte)(flags | Http2Constants.EndHeadersFlag);
        }

        output[4] = flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadOffset = Http2Constants.FrameHeaderSize;
        if (isHeaders && frame.IsPadded)
        {
            output[payloadOffset] = (byte)frame.PaddingLength;
            payloadOffset = payloadOffset + 1;
        }

        if (isHeaders && frame.HasPriority)
        {
            uint dependency = (uint)frame.StreamDependency;
            if (frame.ExclusiveDependency)
            {
                dependency = dependency | Http2Constants.ReservedBitMask;
            }

            output[payloadOffset] = (byte)(dependency >> 24 & 0xffu);
            output[payloadOffset + 1] = (byte)(dependency >> 16 & 0xffu);
            output[payloadOffset + 2] = (byte)(dependency >> 8 & 0xffu);
            output[payloadOffset + 3] = (byte)(dependency & 0xffu);
            output[payloadOffset + 4] = (byte)(frame.Weight - 1);
            payloadOffset = payloadOffset + Http2Constants.PriorityPayloadSize;
        }

        int fragmentIndex = 0;
        while (fragmentIndex < frame.HeaderBlockFragment.Length)
        {
            output[payloadOffset + fragmentIndex] =
                frame.HeaderBlockFragment[fragmentIndex];
            fragmentIndex = fragmentIndex + 1;
        }

        Http2Frame snapshot = CreateHeaderBlockSnapshot(
            frame, flags, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateHeaderBlockSnapshot(
        Http2Frame source,
        byte flags,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = source.FrameType;
        frame.TypeCode = source.FrameType == Http2FrameType.Headers ?
            (byte)0x01 : (byte)0x09;
        frame.IsKnownType = true;
        frame.Flags = flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.HeaderBlockFragment = CopyDataRange(
            source.HeaderBlockFragment, 0, source.HeaderBlockFragment.Length);
        frame.EndHeaders = source.EndHeaders;
        if (source.FrameType == Http2FrameType.Headers)
        {
            frame.EndStream = source.EndStream;
            frame.IsPadded = source.IsPadded;
            frame.PaddingLength = source.IsPadded ? source.PaddingLength : 0;
            frame.HasPriority = source.HasPriority;
            frame.ExclusiveDependency = source.HasPriority &&
                source.ExclusiveDependency;
            frame.StreamDependency = source.HasPriority ?
                source.StreamDependency : 0;
            frame.Weight = source.HasPriority ? source.Weight : 0;
            frame.PriorityIgnored = source.HasPriority &&
                source.PriorityIgnored;
        }

        return frame;
    }

    internal static Http2FrameResult RejectHeaderBlockFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodePriorityFrame(
        Http2FrameResult result,
        Http2Frame frame,
        bool priorityIgnored)
    {
        if (frame.Length != Http2Constants.PriorityPayloadSize)
        {
            return RejectPriorityFrame(
                result,
                frame,
                0,
                "http2-priority-frame-size",
                "A PRIORITY frame payload must contain exactly five octets.");
        }

        if (frame.StreamId == 0)
        {
            return RejectPriorityFrame(
                result,
                frame,
                5,
                "http2-priority-stream-zero",
                "A PRIORITY frame must identify a nonzero stream.");
        }

        frame.HasPriority = true;
        frame.PriorityIgnored = priorityIgnored;
        frame.ExclusiveDependency = (frame.Payload[0] & 0x80) != 0;
        frame.StreamDependency = (frame.Payload[0] & 0x7f) << 24 |
            frame.Payload[1] << 16 |
            frame.Payload[2] << 8 |
            frame.Payload[3];
        frame.Weight = frame.Payload[4] + 1;
        if (frame.StreamDependency == frame.StreamId)
        {
            return RejectPriorityFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize,
                "http2-priority-self-dependency",
                "A PRIORITY dependency cannot name its own stream.");
        }

        return result;
    }

    internal static Http2FrameResult BuildPriorityFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < Http2Constants.PriorityPayloadSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = maximumFrameSize < 0 ?
                "invalid-http2-frame-size-limit" :
                "http2-frame-size-limit-exceeded";
            result.ErrorMessage = maximumFrameSize < 0 ?
                "HTTP/2 frame-size limits cannot be negative." :
                "The PRIORITY payload exceeds the active maximum frame size.";
            return result;
        }

        if (frame.StreamId <= 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-priority-stream-zero";
            result.ErrorMessage =
                "A PRIORITY frame must identify a valid nonzero stream.";
            return result;
        }

        if (frame.StreamDependency < 0 ||
            frame.StreamDependency > Http2Constants.MaximumStreamIdentifier ||
            frame.StreamDependency == frame.StreamId ||
            frame.Weight < 1 || frame.Weight > 256)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-priority-frame";
            result.ErrorMessage =
                "PRIORITY requires a valid dependency and weight.";
            return result;
        }

        int requiredSize = Http2Constants.FrameHeaderSize +
            Http2Constants.PriorityPayloadSize;
        byte[] output = new byte[requiredSize];
        output[2] = (byte)Http2Constants.PriorityPayloadSize;
        output[3] = 0x02;
        output[4] = frame.Flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        uint dependency = (uint)frame.StreamDependency;
        if (frame.ExclusiveDependency)
        {
            dependency = dependency | Http2Constants.ReservedBitMask;
        }

        int payloadOffset = Http2Constants.FrameHeaderSize;
        output[payloadOffset] = (byte)(dependency >> 24 & 0xffu);
        output[payloadOffset + 1] = (byte)(dependency >> 16 & 0xffu);
        output[payloadOffset + 2] = (byte)(dependency >> 8 & 0xffu);
        output[payloadOffset + 3] = (byte)(dependency & 0xffu);
        output[payloadOffset + 4] = (byte)(frame.Weight - 1);

        Http2Frame snapshot = CreatePrioritySnapshot(frame, output);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreatePrioritySnapshot(
        Http2Frame source,
        byte[] encoded)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = Http2Constants.PriorityPayloadSize;
        frame.FrameType = Http2FrameType.Priority;
        frame.TypeCode = 0x02;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded,
            Http2Constants.FrameHeaderSize,
            Http2Constants.PriorityPayloadSize);
        frame.HasPriority = true;
        frame.ExclusiveDependency = source.ExclusiveDependency;
        frame.StreamDependency = source.StreamDependency;
        frame.Weight = source.Weight;
        frame.PriorityIgnored = source.PriorityIgnored;
        return frame;
    }

    internal static Http2FrameResult RejectPriorityFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodeResetStreamFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.Length != Http2Constants.ResetStreamPayloadSize)
        {
            return RejectResetStreamFrame(
                result,
                frame,
                0,
                "http2-rst-stream-frame-size",
                "An RST_STREAM payload must contain exactly four octets.");
        }

        if (frame.StreamId == 0)
        {
            return RejectResetStreamFrame(
                result,
                frame,
                5,
                "http2-rst-stream-stream-zero",
                "An RST_STREAM frame must identify a nonzero stream.");
        }

        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCodeValue = (uint)frame.Payload[0] << 24 |
            (uint)frame.Payload[1] << 16 |
            (uint)frame.Payload[2] << 8 |
            frame.Payload[3];
        frame.ErrorCode = GetHttp2ErrorCode(frame.ErrorCodeValue);
        frame.IsKnownErrorCode = frame.ErrorCode != Http2ErrorCode.Unknown;
        return result;
    }

    internal static Http2FrameResult BuildResetStreamFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < Http2Constants.ResetStreamPayloadSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = maximumFrameSize < 0 ?
                "invalid-http2-frame-size-limit" :
                "http2-frame-size-limit-exceeded";
            result.ErrorMessage = maximumFrameSize < 0 ?
                "HTTP/2 frame-size limits cannot be negative." :
                "The RST_STREAM payload exceeds the active maximum frame size.";
            return result;
        }

        if (frame.StreamId <= 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-rst-stream-stream-zero";
            result.ErrorMessage =
                "An RST_STREAM frame must identify a valid nonzero stream.";
            return result;
        }

        Http2ErrorCode mappedCode = GetHttp2ErrorCode(frame.ErrorCodeValue);
        bool mappedIsKnown = mappedCode != Http2ErrorCode.Unknown;
        if (!frame.HasErrorCode || frame.ErrorCode != mappedCode ||
            frame.IsKnownErrorCode != mappedIsKnown)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-rst-stream-error-code";
            result.ErrorMessage =
                "RST_STREAM error-code metadata must match its exact wire value.";
            return result;
        }

        int requiredSize = Http2Constants.FrameHeaderSize +
            Http2Constants.ResetStreamPayloadSize;
        byte[] output = new byte[requiredSize];
        output[2] = (byte)Http2Constants.ResetStreamPayloadSize;
        output[3] = 0x03;
        output[4] = frame.Flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadOffset = Http2Constants.FrameHeaderSize;
        output[payloadOffset] =
            (byte)(frame.ErrorCodeValue >> 24 & 0xffu);
        output[payloadOffset + 1] =
            (byte)(frame.ErrorCodeValue >> 16 & 0xffu);
        output[payloadOffset + 2] =
            (byte)(frame.ErrorCodeValue >> 8 & 0xffu);
        output[payloadOffset + 3] = (byte)(frame.ErrorCodeValue & 0xffu);

        Http2Frame snapshot = CreateResetStreamSnapshot(frame, output);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateResetStreamSnapshot(
        Http2Frame source,
        byte[] encoded)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = Http2Constants.ResetStreamPayloadSize;
        frame.FrameType = Http2FrameType.RstStream;
        frame.TypeCode = 0x03;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded,
            Http2Constants.FrameHeaderSize,
            Http2Constants.ResetStreamPayloadSize);
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCode = source.ErrorCode;
        frame.ErrorCodeValue = source.ErrorCodeValue;
        frame.IsKnownErrorCode = source.IsKnownErrorCode;
        return frame;
    }

    internal static Http2ErrorCode GetHttp2ErrorCode(uint errorCode)
    {
        switch (errorCode)
        {
            case 0x00u:
                return Http2ErrorCode.NoError;
            case 0x01u:
                return Http2ErrorCode.ProtocolError;
            case 0x02u:
                return Http2ErrorCode.InternalError;
            case 0x03u:
                return Http2ErrorCode.FlowControlError;
            case 0x04u:
                return Http2ErrorCode.SettingsTimeout;
            case 0x05u:
                return Http2ErrorCode.StreamClosed;
            case 0x06u:
                return Http2ErrorCode.FrameSizeError;
            case 0x07u:
                return Http2ErrorCode.RefusedStream;
            case 0x08u:
                return Http2ErrorCode.Cancel;
            case 0x09u:
                return Http2ErrorCode.CompressionError;
            case 0x0au:
                return Http2ErrorCode.ConnectError;
            case 0x0bu:
                return Http2ErrorCode.EnhanceYourCalm;
            case 0x0cu:
                return Http2ErrorCode.InadequateSecurity;
            case 0x0du:
                return Http2ErrorCode.Http11Required;
            default:
                return Http2ErrorCode.Unknown;
        }
    }

    internal static Http2FrameResult RejectResetStreamFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodeSettingsFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId != 0)
        {
            return RejectSettingsFrame(
                result,
                frame,
                5,
                "http2-settings-stream-nonzero",
                "A SETTINGS frame must use stream zero.");
        }

        frame.Acknowledgement =
            (frame.Flags & Http2Constants.AckFlag) != 0;
        frame.Settings = new Http2Settings();
        if (frame.Acknowledgement && frame.Length != 0)
        {
            return RejectSettingsFrame(
                result,
                frame,
                0,
                "http2-settings-ack-payload",
                "A SETTINGS acknowledgement must have an empty payload.");
        }

        if (frame.Length % Http2Constants.SettingSize != 0)
        {
            return RejectSettingsFrame(
                result,
                frame,
                0,
                "http2-settings-frame-size",
                "A SETTINGS payload must be a multiple of six octets.");
        }

        int offset = 0;
        while (offset < frame.Payload.Length)
        {
            Http2Setting setting = ReadHttp2Setting(frame.Payload, offset);
            frame.Settings.Values.Add(setting);
            ApplyHttp2Setting(frame.Settings, setting);
            if (setting.IsKnown && !setting.IsValid)
            {
                return RejectSettingsFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize + offset + 2,
                    "http2-settings-value-invalid",
                    "A recognized SETTINGS parameter has an invalid value.");
            }

            offset = offset + Http2Constants.SettingSize;
        }

        return result;
    }

    internal static Http2FrameResult BuildSettingsFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-settings-stream-nonzero";
            result.ErrorMessage = "A SETTINGS frame must use stream zero.";
            return result;
        }

        Http2Settings settingsData = new Http2Settings();
        if (frame.Settings != null)
        {
            settingsData = frame.Settings;
        }

        int settingCount = settingsData.Values.Count;

        if (frame.Acknowledgement && settingCount != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-settings-ack-payload";
            result.ErrorMessage =
                "A SETTINGS acknowledgement cannot contain parameters.";
            return result;
        }

        if (!frame.Acknowledgement && frame.Settings == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-settings-frame";
            result.ErrorMessage =
                "A non-acknowledgement SETTINGS frame requires settings data.";
            return result;
        }

        long payloadLength = (long)settingCount * Http2Constants.SettingSize;
        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The SETTINGS payload exceeds the active maximum frame size.";
            return result;
        }

        int validationIndex = 0;
        while (validationIndex < settingCount)
        {
            Http2Setting setting = settingsData.Values[validationIndex];
            if (setting == null || setting.Identifier < 0 ||
                setting.Identifier > 0xffff)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCode = "invalid-http2-setting-identifier";
                result.ErrorMessage =
                    "A SETTINGS identifier must be an unsigned 16-bit value.";
                return result;
            }

            bool known = IsKnownHttp2Setting(setting.Identifier);
            bool valid = IsValidHttp2Setting(setting.Identifier, setting.Value);
            if (known && !valid)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCode = "http2-settings-value-invalid";
                result.ErrorMessage =
                    "A recognized SETTINGS parameter has an invalid value.";
                return result;
            }

            validationIndex = validationIndex + 1;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x04;
        byte flags = (byte)(frame.Flags & 0xfe);
        if (frame.Acknowledgement)
        {
            flags = (byte)(flags | Http2Constants.AckFlag);
        }

        output[4] = flags;
        int settingIndex = 0;
        while (settingIndex < settingCount)
        {
            Http2Setting setting = settingsData.Values[settingIndex];
            int offset = Http2Constants.FrameHeaderSize +
                settingIndex * Http2Constants.SettingSize;
            output[offset] = (byte)(setting.Identifier >> 8 & 0xff);
            output[offset + 1] = (byte)(setting.Identifier & 0xff);
            output[offset + 2] = (byte)(setting.Value >> 24 & 0xffu);
            output[offset + 3] = (byte)(setting.Value >> 16 & 0xffu);
            output[offset + 4] = (byte)(setting.Value >> 8 & 0xffu);
            output[offset + 5] = (byte)(setting.Value & 0xffu);
            settingIndex = settingIndex + 1;
        }

        Http2Frame snapshot = CreateSettingsSnapshot(
            frame, flags, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateSettingsSnapshot(
        Http2Frame source,
        byte flags,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.Settings;
        frame.TypeCode = 0x04;
        frame.IsKnownType = true;
        frame.Flags = flags;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.Acknowledgement = source.Acknowledgement;
        frame.Settings = new Http2Settings();
        int index = 0;
        while (source.Settings != null &&
            index < source.Settings.Values.Count)
        {
            Http2Setting sourceSetting = source.Settings.Values[index];
            Http2Setting setting = new Http2Setting();
            setting.Identifier = sourceSetting.Identifier;
            setting.Value = sourceSetting.Value;
            setting.IsKnown = IsKnownHttp2Setting(setting.Identifier);
            setting.IsValid = IsValidHttp2Setting(
                setting.Identifier, setting.Value);
            frame.Settings.Values.Add(setting);
            ApplyHttp2Setting(frame.Settings, setting);
            index = index + 1;
        }

        return frame;
    }

    internal static Http2Setting ReadHttp2Setting(byte[] payload, int offset)
    {
        Http2Setting setting = new Http2Setting();
        setting.Identifier = payload[offset] << 8 | payload[offset + 1];
        setting.Value = (uint)payload[offset + 2] << 24 |
            (uint)payload[offset + 3] << 16 |
            (uint)payload[offset + 4] << 8 |
            payload[offset + 5];
        setting.IsKnown = IsKnownHttp2Setting(setting.Identifier);
        setting.IsValid = IsValidHttp2Setting(
            setting.Identifier, setting.Value);
        return setting;
    }

    internal static bool IsKnownHttp2Setting(int identifier)
    {
        switch (identifier)
        {
            case Http2SettingIdentifiers.HeaderTableSize:
            case Http2SettingIdentifiers.EnablePush:
            case Http2SettingIdentifiers.MaxConcurrentStreams:
            case Http2SettingIdentifiers.InitialWindowSize:
            case Http2SettingIdentifiers.MaxFrameSize:
            case Http2SettingIdentifiers.MaxHeaderListSize:
            case Http2SettingIdentifiers.EnableConnectProtocol:
            case Http2SettingIdentifiers.NoRfc7540Priorities:
            case Http2SettingIdentifiers.TlsRenegPermitted:
            case Http2SettingIdentifiers.EnableMetadata:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsValidHttp2Setting(int identifier, uint value)
    {
        switch (identifier)
        {
            case Http2SettingIdentifiers.EnablePush:
            case Http2SettingIdentifiers.EnableConnectProtocol:
            case Http2SettingIdentifiers.NoRfc7540Priorities:
            case Http2SettingIdentifiers.EnableMetadata:
                return value == 0u || value == 1u;
            case Http2SettingIdentifiers.InitialWindowSize:
                return value <= Http2Constants.MaximumWindowSize;
            case Http2SettingIdentifiers.MaxFrameSize:
                return value >= Http2Constants.DefaultMaximumFrameSize &&
                    value <= Http2Constants.MaximumFrameSize;
            case Http2SettingIdentifiers.TlsRenegPermitted:
                return value <= 3u;
            default:
                return IsKnownHttp2Setting(identifier);
        }
    }

    internal static void ApplyHttp2Setting(
        Http2Settings settings,
        Http2Setting setting)
    {
        if (!setting.IsValid)
        {
            return;
        }

        switch (setting.Identifier)
        {
            case Http2SettingIdentifiers.HeaderTableSize:
                settings.HasHeaderTableSize = true;
                settings.HeaderTableSize = setting.Value;
                break;
            case Http2SettingIdentifiers.EnablePush:
                settings.HasEnablePush = true;
                settings.EnablePush = setting.Value == 1u;
                break;
            case Http2SettingIdentifiers.MaxConcurrentStreams:
                settings.HasMaxConcurrentStreams = true;
                settings.MaxConcurrentStreams = setting.Value;
                break;
            case Http2SettingIdentifiers.InitialWindowSize:
                settings.HasInitialWindowSize = true;
                settings.InitialWindowSize = (int)setting.Value;
                break;
            case Http2SettingIdentifiers.MaxFrameSize:
                settings.HasMaxFrameSize = true;
                settings.MaxFrameSize = (int)setting.Value;
                break;
            case Http2SettingIdentifiers.MaxHeaderListSize:
                settings.HasMaxHeaderListSize = true;
                settings.MaxHeaderListSize = setting.Value;
                break;
            case Http2SettingIdentifiers.EnableConnectProtocol:
                settings.HasEnableConnectProtocol = true;
                settings.EnableConnectProtocol = setting.Value == 1u;
                break;
            case Http2SettingIdentifiers.NoRfc7540Priorities:
                settings.HasNoRfc7540Priorities = true;
                settings.NoRfc7540Priorities = setting.Value == 1u;
                break;
            default:
                break;
        }
    }

    internal static Http2FrameResult RejectSettingsFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodePushPromiseFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId == 0)
        {
            return RejectPushPromiseFrame(
                result,
                frame,
                5,
                "http2-push-promise-stream-zero",
                "A PUSH_PROMISE frame must identify a nonzero stream.");
        }

        frame.EndHeaders =
            (frame.Flags & Http2Constants.EndHeadersFlag) != 0;
        frame.IsPadded = (frame.Flags & Http2Constants.PaddedFlag) != 0;
        int fragmentOffset = 0;
        int fragmentEnd = frame.Payload.Length;
        if (frame.IsPadded)
        {
            if (frame.Payload.Length == 0)
            {
                return RejectPushPromiseFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize,
                    "http2-push-promise-pad-length-missing",
                    "A padded PUSH_PROMISE requires a pad-length octet.");
            }

            frame.PaddingLength = frame.Payload[0];
            fragmentOffset = 1;
            fragmentEnd = frame.Payload.Length - frame.PaddingLength;
            if (fragmentEnd < fragmentOffset)
            {
                return RejectPushPromiseFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize,
                    "http2-push-promise-padding-invalid",
                    "PUSH_PROMISE padding exceeds the available payload.");
            }
        }

        if (fragmentEnd - fragmentOffset < 4)
        {
            return RejectPushPromiseFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize + fragmentOffset,
                "http2-push-promise-id-truncated",
                "A PUSH_PROMISE payload requires a four-octet promised stream id.");
        }

        int promisedOffset = fragmentOffset;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamReservedBit =
            (frame.Payload[promisedOffset] & 0x80) != 0;
        frame.PromisedStreamId =
            (frame.Payload[promisedOffset] & 0x7f) << 24 |
            frame.Payload[promisedOffset + 1] << 16 |
            frame.Payload[promisedOffset + 2] << 8 |
            frame.Payload[promisedOffset + 3];
        if (frame.PromisedStreamId == 0)
        {
            return RejectPushPromiseFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize + promisedOffset,
                "http2-push-promise-id-zero",
                "A promised stream identifier must be nonzero.");
        }

        if (frame.PromisedStreamId == frame.StreamId)
        {
            return RejectPushPromiseFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize + promisedOffset,
                "http2-push-promise-self",
                "A PUSH_PROMISE cannot promise its associated stream.");
        }

        fragmentOffset = fragmentOffset + 4;
        frame.HeaderBlockFragment = CopyDataRange(
            frame.Payload,
            fragmentOffset,
            fragmentEnd - fragmentOffset);
        return result;
    }

    internal static Http2FrameResult BuildPushPromiseFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId <= 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-push-promise-stream-zero";
            result.ErrorMessage =
                "A PUSH_PROMISE frame must identify a valid nonzero stream.";
            return result;
        }

        if (!frame.HasPromisedStreamId || frame.PromisedStreamId <= 0 ||
            frame.PromisedStreamId > Http2Constants.MaximumStreamIdentifier ||
            frame.PromisedStreamId == frame.StreamId)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-push-promise-id";
            result.ErrorMessage =
                "PUSH_PROMISE requires a distinct valid promised stream id.";
            return result;
        }

        if (frame.HeaderBlockFragment == null ||
            frame.PaddingLength < 0 || frame.PaddingLength > 255 ||
            !frame.IsPadded && frame.PaddingLength != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-push-promise-frame";
            result.ErrorMessage =
                "PUSH_PROMISE requires a fragment and consistent padding.";
            return result;
        }

        long payloadLength = frame.HeaderBlockFragment.Length + 4L;
        if (frame.IsPadded)
        {
            payloadLength = payloadLength + frame.PaddingLength + 1;
        }

        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The PUSH_PROMISE payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x05;
        byte flags = (byte)(frame.Flags & 0xf3);
        if (frame.EndHeaders)
        {
            flags = (byte)(flags | Http2Constants.EndHeadersFlag);
        }

        if (frame.IsPadded)
        {
            flags = (byte)(flags | Http2Constants.PaddedFlag);
        }

        output[4] = flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadOffset = Http2Constants.FrameHeaderSize;
        if (frame.IsPadded)
        {
            output[payloadOffset] = (byte)frame.PaddingLength;
            payloadOffset = payloadOffset + 1;
        }

        output[payloadOffset] =
            (byte)(frame.PromisedStreamId >> 24 & 0xff);
        output[payloadOffset + 1] =
            (byte)(frame.PromisedStreamId >> 16 & 0xff);
        output[payloadOffset + 2] =
            (byte)(frame.PromisedStreamId >> 8 & 0xff);
        output[payloadOffset + 3] =
            (byte)(frame.PromisedStreamId & 0xff);
        payloadOffset = payloadOffset + 4;
        int fragmentIndex = 0;
        while (fragmentIndex < frame.HeaderBlockFragment.Length)
        {
            output[payloadOffset + fragmentIndex] =
                frame.HeaderBlockFragment[fragmentIndex];
            fragmentIndex = fragmentIndex + 1;
        }

        Http2Frame snapshot = CreatePushPromiseSnapshot(
            frame, flags, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreatePushPromiseSnapshot(
        Http2Frame source,
        byte flags,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.PushPromise;
        frame.TypeCode = 0x05;
        frame.IsKnownType = true;
        frame.Flags = flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.EndHeaders = source.EndHeaders;
        frame.IsPadded = source.IsPadded;
        frame.PaddingLength = source.IsPadded ? source.PaddingLength : 0;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = source.PromisedStreamId;
        frame.HeaderBlockFragment = CopyDataRange(
            source.HeaderBlockFragment, 0, source.HeaderBlockFragment.Length);
        return frame;
    }

    internal static Http2FrameResult RejectPushPromiseFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodePingFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId != 0)
        {
            return RejectPingFrame(
                result,
                frame,
                5,
                "http2-ping-stream-nonzero",
                "A PING frame must use stream zero.");
        }

        frame.Acknowledgement =
            (frame.Flags & Http2Constants.AckFlag) != 0;
        if (frame.Length != Http2Constants.PingPayloadSize)
        {
            return RejectPingFrame(
                result,
                frame,
                0,
                "http2-ping-frame-size",
                "A PING payload must contain exactly eight octets.");
        }

        frame.OpaqueData = CopyDataRange(
            frame.Payload, 0, Http2Constants.PingPayloadSize);
        return result;
    }

    internal static Http2FrameResult BuildPingFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-ping-stream-nonzero";
            result.ErrorMessage = "A PING frame must use stream zero.";
            return result;
        }

        if (frame.OpaqueData == null ||
            frame.OpaqueData.Length != Http2Constants.PingPayloadSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-ping-data";
            result.ErrorMessage =
                "PING correlation data must contain exactly eight octets.";
            return result;
        }

        if (Http2Constants.PingPayloadSize > maximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The PING payload exceeds the active maximum frame size.";
            return result;
        }

        int requiredSize = Http2Constants.FrameHeaderSize +
            Http2Constants.PingPayloadSize;
        byte[] output = new byte[requiredSize];
        output[2] = Http2Constants.PingPayloadSize;
        output[3] = 0x06;
        byte flags = (byte)(frame.Flags & 0xfe);
        if (frame.Acknowledgement)
        {
            flags = (byte)(flags | Http2Constants.AckFlag);
        }

        output[4] = flags;
        int index = 0;
        while (index < Http2Constants.PingPayloadSize)
        {
            output[Http2Constants.FrameHeaderSize + index] =
                frame.OpaqueData[index];
            index = index + 1;
        }

        Http2Frame snapshot = CreatePingSnapshot(frame, flags, output);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreatePingSnapshot(
        Http2Frame source,
        byte flags,
        byte[] encoded)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = Http2Constants.PingPayloadSize;
        frame.FrameType = Http2FrameType.Ping;
        frame.TypeCode = 0x06;
        frame.IsKnownType = true;
        frame.Flags = flags;
        frame.Payload = CopyDataRange(
            encoded,
            Http2Constants.FrameHeaderSize,
            Http2Constants.PingPayloadSize);
        frame.Acknowledgement = source.Acknowledgement;
        frame.OpaqueData = CopyDataRange(
            source.OpaqueData, 0, Http2Constants.PingPayloadSize);
        return frame;
    }

    internal static Http2FrameResult RejectPingFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodeGoAwayFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId != 0)
        {
            return RejectGoAwayFrame(
                result,
                frame,
                5,
                "http2-goaway-stream-nonzero",
                "A GOAWAY frame must use stream zero.");
        }

        if (frame.Length < Http2Constants.GoAwayMinimumPayloadSize)
        {
            return RejectGoAwayFrame(
                result,
                frame,
                0,
                "http2-goaway-frame-size",
                "A GOAWAY payload must contain at least eight octets.");
        }

        frame.HasLastStreamId = true;
        frame.LastStreamReservedBit = (frame.Payload[0] & 0x80) != 0;
        frame.LastStreamId = (frame.Payload[0] & 0x7f) << 24 |
            frame.Payload[1] << 16 |
            frame.Payload[2] << 8 |
            frame.Payload[3];
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCodeValue = (uint)frame.Payload[4] << 24 |
            (uint)frame.Payload[5] << 16 |
            (uint)frame.Payload[6] << 8 |
            frame.Payload[7];
        frame.ErrorCode = GetHttp2ErrorCode(frame.ErrorCodeValue);
        frame.IsKnownErrorCode = frame.ErrorCode != Http2ErrorCode.Unknown;
        frame.DebugData = CopyDataRange(
            frame.Payload,
            Http2Constants.GoAwayMinimumPayloadSize,
            frame.Payload.Length - Http2Constants.GoAwayMinimumPayloadSize);
        return result;
    }

    internal static Http2FrameResult BuildGoAwayFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-goaway-stream-nonzero";
            result.ErrorMessage = "A GOAWAY frame must use stream zero.";
            return result;
        }

        if (!frame.HasLastStreamId || frame.LastStreamId < 0 ||
            frame.LastStreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-goaway-last-stream-id";
            result.ErrorMessage =
                "GOAWAY requires a valid 31-bit last-stream identifier.";
            return result;
        }

        Http2ErrorCode mappedCode = GetHttp2ErrorCode(frame.ErrorCodeValue);
        bool mappedIsKnown = mappedCode != Http2ErrorCode.Unknown;
        if (!frame.HasErrorCode || frame.ErrorCode != mappedCode ||
            frame.IsKnownErrorCode != mappedIsKnown)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-goaway-error-code";
            result.ErrorMessage =
                "GOAWAY error-code metadata must match its exact wire value.";
            return result;
        }

        if (frame.DebugData == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-goaway-debug-data";
            result.ErrorMessage = "GOAWAY debug data cannot be null.";
            return result;
        }

        long payloadLength = Http2Constants.GoAwayMinimumPayloadSize +
            (long)frame.DebugData.Length;
        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The GOAWAY payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x07;
        output[4] = frame.Flags;
        int payloadOffset = Http2Constants.FrameHeaderSize;
        output[payloadOffset] =
            (byte)(frame.LastStreamId >> 24 & 0xff);
        output[payloadOffset + 1] =
            (byte)(frame.LastStreamId >> 16 & 0xff);
        output[payloadOffset + 2] =
            (byte)(frame.LastStreamId >> 8 & 0xff);
        output[payloadOffset + 3] = (byte)(frame.LastStreamId & 0xff);
        output[payloadOffset + 4] =
            (byte)(frame.ErrorCodeValue >> 24 & 0xffu);
        output[payloadOffset + 5] =
            (byte)(frame.ErrorCodeValue >> 16 & 0xffu);
        output[payloadOffset + 6] =
            (byte)(frame.ErrorCodeValue >> 8 & 0xffu);
        output[payloadOffset + 7] =
            (byte)(frame.ErrorCodeValue & 0xffu);
        int debugIndex = 0;
        while (debugIndex < frame.DebugData.Length)
        {
            output[payloadOffset + Http2Constants.GoAwayMinimumPayloadSize +
                debugIndex] = frame.DebugData[debugIndex];
            debugIndex = debugIndex + 1;
        }

        Http2Frame snapshot = CreateGoAwaySnapshot(
            frame, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateGoAwaySnapshot(
        Http2Frame source,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.GoAway;
        frame.TypeCode = 0x07;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.HasLastStreamId = true;
        frame.LastStreamId = source.LastStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = source.ErrorCode;
        frame.ErrorCodeValue = source.ErrorCodeValue;
        frame.IsKnownErrorCode = source.IsKnownErrorCode;
        frame.DebugData = CopyDataRange(
            source.DebugData, 0, source.DebugData.Length);
        return frame;
    }

    internal static Http2FrameResult RejectGoAwayFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodeWindowUpdateFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.Length != Http2Constants.WindowUpdatePayloadSize)
        {
            return RejectWindowUpdateFrame(
                result,
                frame,
                0,
                "http2-window-update-frame-size",
                "A WINDOW_UPDATE payload must contain exactly four octets.");
        }

        frame.HasWindowIncrement = true;
        frame.WindowIncrementReservedBit = (frame.Payload[0] & 0x80) != 0;
        frame.WindowIncrement = (frame.Payload[0] & 0x7f) << 24 |
            frame.Payload[1] << 16 |
            frame.Payload[2] << 8 |
            frame.Payload[3];
        if (frame.WindowIncrement == 0)
        {
            return RejectWindowUpdateFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize,
                "http2-window-update-zero-increment",
                "A WINDOW_UPDATE increment must be nonzero.");
        }

        return result;
    }

    internal static Http2FrameResult BuildWindowUpdateFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < Http2Constants.WindowUpdatePayloadSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = maximumFrameSize < 0 ?
                "invalid-http2-frame-size-limit" :
                "http2-frame-size-limit-exceeded";
            result.ErrorMessage = maximumFrameSize < 0 ?
                "HTTP/2 frame-size limits cannot be negative." :
                "The WINDOW_UPDATE payload exceeds the active maximum frame size.";
            return result;
        }

        if (frame.StreamId < 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-window-update-stream";
            result.ErrorMessage =
                "A WINDOW_UPDATE target must be a valid stream identifier.";
            return result;
        }

        if (!frame.HasWindowIncrement || frame.WindowIncrement <= 0 ||
            frame.WindowIncrement > Http2Constants.MaximumWindowSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-window-update-increment";
            result.ErrorMessage =
                "A WINDOW_UPDATE increment must be a nonzero 31-bit value.";
            return result;
        }

        int requiredSize = Http2Constants.FrameHeaderSize +
            Http2Constants.WindowUpdatePayloadSize;
        byte[] output = new byte[requiredSize];
        output[2] = (byte)Http2Constants.WindowUpdatePayloadSize;
        output[3] = 0x08;
        output[4] = frame.Flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadOffset = Http2Constants.FrameHeaderSize;
        output[payloadOffset] =
            (byte)(frame.WindowIncrement >> 24 & 0xff);
        output[payloadOffset + 1] =
            (byte)(frame.WindowIncrement >> 16 & 0xff);
        output[payloadOffset + 2] =
            (byte)(frame.WindowIncrement >> 8 & 0xff);
        output[payloadOffset + 3] = (byte)(frame.WindowIncrement & 0xff);

        Http2Frame snapshot = CreateWindowUpdateSnapshot(frame, output);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateWindowUpdateSnapshot(
        Http2Frame source,
        byte[] encoded)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = Http2Constants.WindowUpdatePayloadSize;
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.TypeCode = 0x08;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded,
            Http2Constants.FrameHeaderSize,
            Http2Constants.WindowUpdatePayloadSize);
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = source.WindowIncrement;
        return frame;
    }

    internal static Http2FrameResult RejectWindowUpdateFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodePriorityUpdateFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId != 0)
        {
            return RejectPriorityUpdateFrame(
                result,
                frame,
                5,
                "http2-priority-update-stream-nonzero",
                "A PRIORITY_UPDATE on a nonzero stream is a connection protocol error.");
        }

        if (frame.Length < 4)
        {
            return RejectPriorityUpdateFrame(
                result,
                frame,
                0,
                "http2-priority-update-frame-size",
                "A PRIORITY_UPDATE payload requires a four-octet stream id.");
        }

        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementReservedBit =
            (frame.Payload[0] & 0x80) != 0;
        frame.PrioritizedElementId = (frame.Payload[0] & 0x7f) << 24 |
            frame.Payload[1] << 16 |
            frame.Payload[2] << 8 |
            frame.Payload[3];
        frame.PrioritizedElementIsPush =
            frame.PrioritizedElementId != 0 &&
            frame.PrioritizedElementId % 2 == 0;
        if (frame.PrioritizedElementId == 0)
        {
            return RejectPriorityUpdateFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize,
                "http2-priority-update-element-zero",
                "A zero prioritized stream id is a connection protocol error.");
        }

        frame.PriorityFieldValue = CopyDataRange(
            frame.Payload, 4, frame.Payload.Length - 4);
        frame.Priority = PriorityCodec.ParseValidated(frame.PriorityFieldValue);
        if (frame.Priority == null)
        {
            return RejectPriorityUpdateFrame(
                result,
                frame,
                Http2Constants.FrameHeaderSize + 4,
                "http2-priority-update-field-value",
                "The PRIORITY_UPDATE field value is not an ASCII Structured Dictionary.");
        }

        return result;
    }

    internal static Http2FrameResult BuildPriorityUpdateFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-priority-update-stream-nonzero";
            result.ErrorMessage =
                "A PRIORITY_UPDATE frame must use stream zero.";
            return result;
        }

        bool isPush = frame.PrioritizedElementId > 0 &&
            frame.PrioritizedElementId % 2 == 0;
        if (!frame.HasPrioritizedElementId ||
            frame.PrioritizedElementId <= 0 ||
            frame.PrioritizedElementId >
                Http2Constants.MaximumStreamIdentifier ||
            frame.PrioritizedElementIsPush != isPush)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-priority-update-element";
            result.ErrorMessage =
                "PRIORITY_UPDATE requires a consistent nonzero request or push stream id.";
            return result;
        }

        HttpPriority? priority =
            PriorityCodec.ParseValidated(frame.PriorityFieldValue);
        if (priority == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-priority-update-field-value";
            result.ErrorMessage =
                "The priority field value must be an ASCII Structured Dictionary.";
            return result;
        }

        long payloadLength = frame.PriorityFieldValue.Length + 4L;
        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The PRIORITY_UPDATE payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x10;
        output[4] = frame.Flags;
        int payloadOffset = Http2Constants.FrameHeaderSize;
        output[payloadOffset] =
            (byte)(frame.PrioritizedElementId >> 24 & 0xff);
        output[payloadOffset + 1] =
            (byte)(frame.PrioritizedElementId >> 16 & 0xff);
        output[payloadOffset + 2] =
            (byte)(frame.PrioritizedElementId >> 8 & 0xff);
        output[payloadOffset + 3] =
            (byte)(frame.PrioritizedElementId & 0xff);
        int fieldIndex = 0;
        while (fieldIndex < frame.PriorityFieldValue.Length)
        {
            output[payloadOffset + 4 + fieldIndex] =
                frame.PriorityFieldValue[fieldIndex];
            fieldIndex = fieldIndex + 1;
        }

        Http2Frame snapshot = CreatePriorityUpdateSnapshot(
            frame, priority, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreatePriorityUpdateSnapshot(
        Http2Frame source,
        HttpPriority priority,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.PriorityUpdate;
        frame.TypeCode = 0x10;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = source.PrioritizedElementId;
        frame.PrioritizedElementIsPush = source.PrioritizedElementIsPush;
        frame.PriorityFieldValue = CopyDataRange(
            source.PriorityFieldValue, 0, source.PriorityFieldValue.Length);
        frame.Priority = priority;
        return frame;
    }

    internal static Http2FrameResult RejectPriorityUpdateFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult DecodeOriginFrame(
        Http2FrameResult result,
        Http2Frame frame)
    {
        if (frame.StreamId != 0 || (frame.Flags & 0x0f) != 0)
        {
            frame.OriginIgnored = true;
            return result;
        }

        int offset = 0;
        while (offset < frame.Payload.Length)
        {
            if (frame.Payload.Length - offset < 2)
            {
                return RejectOriginFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize + offset,
                    "http2-origin-length-truncated",
                    "An ORIGIN entry requires a two-octet length.");
            }

            int originLength = frame.Payload[offset] << 8 |
                frame.Payload[offset + 1];
            offset = offset + 2;
            if (originLength > frame.Payload.Length - offset)
            {
                return RejectOriginFrame(
                    result,
                    frame,
                    Http2Constants.FrameHeaderSize + offset - 2,
                    "http2-origin-value-truncated",
                    "An ORIGIN entry exceeds the remaining frame payload.");
            }

            string? origin = ReadOriginValue(
                frame.Payload, offset, originLength);
            if (origin != null)
            {
                frame.Origins.Add(origin);
            }

            offset = offset + originLength;
        }

        return result;
    }

    internal static Http2FrameResult BuildOriginFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.StreamId != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-origin-stream";
            result.ErrorMessage = "An ORIGIN frame must use stream zero.";
            return result;
        }

        if ((frame.Flags & 0x0f) != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-origin-flags";
            result.ErrorMessage =
                "The low four ORIGIN flag bits have incompatible semantics.";
            return result;
        }

        if (frame.Origins == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-origin-list";
            result.ErrorMessage = "An ORIGIN frame requires an origin list.";
            return result;
        }

        long payloadLength = 0;
        int validationIndex = 0;
        while (validationIndex < frame.Origins.Count)
        {
            string origin = frame.Origins[validationIndex];
            if (!IsValidSerializedOrigin(origin) || origin.Length > 0xffff)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCode = "invalid-http2-origin-value";
                result.ErrorMessage =
                    "Each ORIGIN entry must be a canonical ASCII HTTP origin.";
                return result;
            }

            payloadLength = payloadLength + origin.Length + 2L;
            validationIndex = validationIndex + 1;
        }

        if (payloadLength > maximumFrameSize ||
            payloadLength > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The ORIGIN payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadSize = (int)payloadLength;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadSize;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadSize >> 16 & 0xff);
        output[1] = (byte)(payloadSize >> 8 & 0xff);
        output[2] = (byte)(payloadSize & 0xff);
        output[3] = 0x0c;
        output[4] = frame.Flags;
        int payloadOffset = Http2Constants.FrameHeaderSize;
        int originIndex = 0;
        while (originIndex < frame.Origins.Count)
        {
            string origin = frame.Origins[originIndex];
            output[payloadOffset] = (byte)(origin.Length >> 8 & 0xff);
            output[payloadOffset + 1] = (byte)(origin.Length & 0xff);
            payloadOffset = payloadOffset + 2;
            int characterIndex = 0;
            while (characterIndex < origin.Length)
            {
                output[payloadOffset + characterIndex] =
                    (byte)origin[characterIndex];
                characterIndex = characterIndex + 1;
            }

            payloadOffset = payloadOffset + origin.Length;
            originIndex = originIndex + 1;
        }

        Http2Frame snapshot = CreateOriginSnapshot(frame, output, payloadSize);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateOriginSnapshot(
        Http2Frame source,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.Origin;
        frame.TypeCode = 0x0c;
        frame.IsKnownType = true;
        frame.Flags = source.Flags;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        int index = 0;
        while (index < source.Origins.Count)
        {
            frame.Origins.Add(source.Origins[index]);
            index = index + 1;
        }

        return frame;
    }

    internal static string? ReadOriginValue(
        byte[] payload,
        int offset,
        int length)
    {
        char[] characters = new char[length];
        int index = 0;
        while (index < length)
        {
            byte value = payload[offset + index];
            if (value > 0x7f)
            {
                return null;
            }

            characters[index] = (char)value;
            index = index + 1;
        }

        string origin = new string(characters);
        if (!IsValidSerializedOrigin(origin))
        {
            return null;
        }

        return origin;
    }

    internal static bool IsValidSerializedOrigin(string origin)
    {
        if (origin == null || origin.Length == 0)
        {
            return false;
        }

        int characterIndex = 0;
        while (characterIndex < origin.Length)
        {
            char character = origin[characterIndex];
            if (character > 0x7f || character >= 'A' && character <= 'Z')
            {
                return false;
            }

            characterIndex = characterIndex + 1;
        }

        UriValidationResult parsed = UriCodec.ValidateUriReference(
            origin, origin.Length);
        if (parsed.Status != HttpOperationStatus.Done ||
            !parsed.IsAbsolute || !parsed.HasAuthority ||
            parsed.HasUserInfo || parsed.Host.Length == 0 ||
            parsed.Path.Length != 0 || parsed.HasQuery || parsed.HasFragment ||
            parsed.Scheme != "http" && parsed.Scheme != "https" ||
            ContainsOriginCharacter(parsed.Host, '*') ||
            ContainsOriginCharacter(parsed.Host, '%'))
        {
            return false;
        }

        if (!parsed.HasPort)
        {
            return true;
        }

        if (!parsed.HasPortValue || !parsed.IsPortInRange ||
            parsed.PortText.Length == 0 ||
            parsed.PortText.Length > 1 && parsed.PortText[0] == '0' ||
            parsed.Scheme == "http" && parsed.Port == 80 ||
            parsed.Scheme == "https" && parsed.Port == 443)
        {
            return false;
        }

        return true;
    }

    internal static bool ContainsOriginCharacter(string value, char sought)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == sought)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static Http2FrameResult RejectOriginFrame(
        Http2FrameResult result,
        Http2Frame frame,
        int errorPosition,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = errorPosition;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2FrameResult BuildUnknownFrame(
        Http2Frame frame,
        int maximumFrameSize)
    {
        Http2FrameResult result = new Http2FrameResult();
        result.Frame = frame;
        if (maximumFrameSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-frame-size-limit";
            result.ErrorMessage = "HTTP/2 frame-size limits cannot be negative.";
            return result;
        }

        if (frame.IsKnownType ||
            GetFrameType(frame.TypeCode) != Http2FrameType.Unknown)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-unknown-frame-type";
            result.ErrorMessage =
                "An unknown frame must use an unregistered type byte.";
            return result;
        }

        if (frame.StreamId < 0 ||
            frame.StreamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-unknown-frame-stream";
            result.ErrorMessage =
                "An unknown frame requires a valid stream identifier.";
            return result;
        }

        if (frame.Payload == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-http2-unknown-frame-payload";
            result.ErrorMessage = "An unknown frame payload cannot be null.";
            return result;
        }

        if (frame.Payload.Length > maximumFrameSize ||
            frame.Payload.Length > Http2Constants.MaximumFrameSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "http2-frame-size-limit-exceeded";
            result.ErrorMessage =
                "The unknown frame payload exceeds the active maximum frame size.";
            return result;
        }

        int payloadLength = frame.Payload.Length;
        int requiredSize = Http2Constants.FrameHeaderSize + payloadLength;
        byte[] output = new byte[requiredSize];
        output[0] = (byte)(payloadLength >> 16 & 0xff);
        output[1] = (byte)(payloadLength >> 8 & 0xff);
        output[2] = (byte)(payloadLength & 0xff);
        output[3] = frame.TypeCode;
        output[4] = frame.Flags;
        output[5] = (byte)(frame.StreamId >> 24 & 0xff);
        output[6] = (byte)(frame.StreamId >> 16 & 0xff);
        output[7] = (byte)(frame.StreamId >> 8 & 0xff);
        output[8] = (byte)(frame.StreamId & 0xff);
        int payloadIndex = 0;
        while (payloadIndex < payloadLength)
        {
            output[Http2Constants.FrameHeaderSize + payloadIndex] =
                frame.Payload[payloadIndex];
            payloadIndex = payloadIndex + 1;
        }

        Http2Frame snapshot = CreateUnknownFrameSnapshot(frame, output);
        result.Status = HttpOperationStatus.Done;
        result.Frame = snapshot;
        result.Bytes = output;
        result.Written = requiredSize;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static Http2Frame CreateUnknownFrameSnapshot(
        Http2Frame source,
        byte[] encoded)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = source.Payload.Length;
        frame.FrameType = Http2FrameType.Unknown;
        frame.TypeCode = source.TypeCode;
        frame.IsKnownType = false;
        frame.Flags = source.Flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded,
            Http2Constants.FrameHeaderSize,
            source.Payload.Length);
        return frame;
    }

    internal static Http2Frame CreateDataSnapshot(
        Http2Frame source,
        byte flags,
        byte[] encoded,
        int payloadLength)
    {
        Http2Frame frame = new Http2Frame();
        frame.Length = payloadLength;
        frame.FrameType = Http2FrameType.Data;
        frame.TypeCode = 0x00;
        frame.IsKnownType = true;
        frame.Flags = flags;
        frame.StreamId = source.StreamId;
        frame.Payload = CopyDataRange(
            encoded, Http2Constants.FrameHeaderSize, payloadLength);
        frame.Data = CopyDataRange(source.Data, 0, source.Data.Length);
        frame.EndStream = source.EndStream;
        frame.IsPadded = source.IsPadded;
        frame.PaddingLength = source.IsPadded ? source.PaddingLength : 0;
        frame.FlowControlledLength = payloadLength;
        return frame;
    }

    internal static Http2FrameResult RejectDataFrame(
        Http2FrameResult result,
        Http2Frame frame,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Frame = frame;
        result.HasErrorPosition = true;
        result.ErrorPosition = Http2Constants.FrameHeaderSize;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static byte[] CopyDataRange(
        byte[] input,
        int offset,
        int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = input[offset + index];
            index = index + 1;
        }

        return result;
    }
}
