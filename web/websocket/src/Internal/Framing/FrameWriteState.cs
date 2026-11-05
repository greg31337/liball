using System;

namespace websocket;

internal class FrameMeasurementResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int EncodedLength;
    internal int HeaderLength;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class FrameWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedOutputCapacity;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal bool EndOfFrame;
    internal bool HasMaskingKey;
    internal uint MaskingKey;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class FrameWriteState
{
    internal byte[] HeaderBytes = new byte[WebSocketProtocolConstants.MaximumFrameHeaderLength];
    internal int HeaderLength;
    internal int HeaderProducedCount;
    internal long PayloadLength;
    internal long PayloadProducedCount;
    internal bool Final;
    internal bool Rsv1;
    internal bool Rsv2;
    internal bool Rsv3;
    internal WebSocketOpcode Opcode;
    internal bool Masked;
    internal uint MaskingKey;
    internal bool Active;
    internal bool Complete;
    internal WebSocketMasker Masker = new WebSocketMasker();

    internal static FrameMeasurementResult MeasureFrame(WebSocketFrameWriteRequest? request)
    {
        if (request == null || request.Header == null || request.Payload == null)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-request-invalid",
                "The frame write request is incomplete.");
        }

        int opcode = (int)request.Header.Opcode;
        if (opcode < WebSocketProtocolConstants.MinimumOpcode ||
            opcode > WebSocketProtocolConstants.MaximumOpcode ||
            request.Header.PayloadLength < 0 ||
            request.MaskingOffset < 0)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                "frame-write-header-invalid",
                "The frame write header or masking offset is invalid.");
        }

        if (request.PayloadOffset < 0 ||
            request.PayloadCount < 0 ||
            request.PayloadOffset > request.Payload.Length - request.PayloadCount)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-range-invalid",
                "The frame payload range is invalid.");
        }

        if (request.MaskingOffset > request.Header.PayloadLength ||
            request.PayloadCount > request.Header.PayloadLength - request.MaskingOffset)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-chunk-invalid",
                "The payload chunk exceeds the declared frame payload.");
        }

        if (request.Header.Masked)
        {
            if (!request.HasMaskKeySource || request.MaskKeySource == null)
            {
                return CreateMeasurementFailure(
                    WebSocketParseStatus.ProtocolFailure,
                    WebSocketErrorKind.State,
                    "frame-write-mask-source-required",
                    "A masked frame requires a masking key source.");
            }
        }
        else if (request.HasMaskKeySource || request.MaskKeySource != null)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-mask-source-forbidden",
                "An unmasked frame cannot use a masking key source.");
        }

        int headerLength = WebSocketProtocolConstants.MinimumFrameHeaderLength;
        if (request.Header.PayloadLength > WebSocketProtocolConstants.Extended16PayloadLengthMaximum)
        {
            headerLength = headerLength + 8;
        }
        else if (request.Header.PayloadLength > WebSocketProtocolConstants.BasicPayloadLengthMaximum)
        {
            headerLength = headerLength + 2;
        }

        if (request.Header.Masked)
        {
            headerLength = headerLength + WebSocketProtocolConstants.MaskingKeyLength;
        }

        if (request.Header.PayloadLength > int.MaxValue - headerLength)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.LimitRejected,
                WebSocketErrorKind.Limit,
                "frame-write-size-overflow",
                "The encoded frame size exceeds the supported signed range.");
        }

        FrameMeasurementResult result = new FrameMeasurementResult();
        result.Status = WebSocketParseStatus.Complete;
        result.HeaderLength = headerLength;
        result.EncodedLength = headerLength + (int)request.Header.PayloadLength;
        return result;
    }

    internal FrameWriteResult WriteFrame(
        WebSocketFrameWriteRequest request,
        Span<byte> destination)
    {
        FrameWriteResult result = new FrameWriteResult();
        if (Complete)
        {
            result.Status = WebSocketParseStatus.Complete;
            result.EndOfFrame = true;
            result.HasMaskingKey = Masked;
            result.MaskingKey = MaskingKey;
            return result;
        }

        if (!Active)
        {
            FrameMeasurementResult startResult = StartFrame(request);
            if (startResult.Status != WebSocketParseStatus.Complete)
            {
                result.Status = startResult.Status;
                result.HasError = startResult.HasError;
                result.Error = startResult.Error;
                return result;
            }
        }

        result.HasMaskingKey = Masked;
        result.MaskingKey = MaskingKey;
        FrameMeasurementResult continuationResult = ValidateContinuation(request);
        if (continuationResult.Status != WebSocketParseStatus.Complete)
        {
            result.Status = continuationResult.Status;
            result.HasError = continuationResult.HasError;
            result.Error = continuationResult.Error;
            return result;
        }

        int destinationIndex = 0;
        while (HeaderProducedCount < HeaderLength && destinationIndex < destination.Length)
        {
            destination[destinationIndex] = HeaderBytes[HeaderProducedCount];
            destinationIndex = destinationIndex + 1;
            HeaderProducedCount = HeaderProducedCount + 1;
        }

        if (HeaderProducedCount < HeaderLength)
        {
            result.ProducedCount = destinationIndex;
            return result;
        }

        int availableCount = destination.Length - destinationIndex;
        int copyCount = request.PayloadCount;
        if (copyCount > availableCount)
        {
            copyCount = availableCount;
        }

        if (copyCount != 0)
        {
            request.Payload.AsSpan(request.PayloadOffset, copyCount).CopyTo(
                destination.Slice(destinationIndex, copyCount));
            if (Masked)
            {
                Masker.ApplyMask(
                    destination.Slice(destinationIndex, copyCount),
                    MaskingKey,
                    request.MaskingOffset);
            }

            PayloadProducedCount = PayloadProducedCount + copyCount;
            destinationIndex = destinationIndex + copyCount;
            result.ConsumedCount = copyCount;
        }

        result.ProducedCount = destinationIndex;
        if (PayloadProducedCount == PayloadLength)
        {
            Complete = true;
            result.Status = WebSocketParseStatus.Complete;
            result.EndOfFrame = true;
        }
        else if (copyCount == request.PayloadCount)
        {
            result.Status = WebSocketParseStatus.NeedInput;
        }

        return result;
    }

    internal void ResetFrame()
    {
        HeaderLength = 0;
        HeaderProducedCount = 0;
        PayloadLength = 0;
        PayloadProducedCount = 0;
        Final = false;
        Rsv1 = false;
        Rsv2 = false;
        Rsv3 = false;
        Opcode = WebSocketOpcode.Continuation;
        Masked = false;
        MaskingKey = 0;
        Active = false;
        Complete = false;
    }

    internal FrameMeasurementResult StartFrame(WebSocketFrameWriteRequest request)
    {
        FrameMeasurementResult measurement = MeasureFrame(request);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            return measurement;
        }

        if (request.MaskingOffset != 0)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-offset-invalid",
                "The first frame payload chunk must begin at offset zero.");
        }

        Final = request.Header.Final;
        Rsv1 = request.Header.Rsv1;
        Rsv2 = request.Header.Rsv2;
        Rsv3 = request.Header.Rsv3;
        Opcode = request.Header.Opcode;
        Masked = request.Header.Masked;
        PayloadLength = request.Header.PayloadLength;
        HeaderLength = measurement.HeaderLength;
        if (Masked)
        {
            IWebSocketMaskKeySource? maskKeySource = request.MaskKeySource;
            if (maskKeySource == null)
            {
                return CreateMeasurementFailure(
                    WebSocketParseStatus.ProtocolFailure,
                    WebSocketErrorKind.State,
                    "frame-write-mask-source-required",
                    "A masked frame requires a masking key source.");
            }

            MaskingKey = maskKeySource.CreateMaskingKey();
        }

        BuildHeader();
        Active = true;
        return measurement;
    }

    internal FrameMeasurementResult ValidateContinuation(WebSocketFrameWriteRequest request)
    {
        FrameMeasurementResult measurement = MeasureFrame(request);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            return measurement;
        }

        if (request.Header.Final != Final ||
            request.Header.Rsv1 != Rsv1 ||
            request.Header.Rsv2 != Rsv2 ||
            request.Header.Rsv3 != Rsv3 ||
            request.Header.Opcode != Opcode ||
            request.Header.Masked != Masked ||
            request.Header.PayloadLength != PayloadLength ||
            request.MaskingOffset != PayloadProducedCount)
        {
            return CreateMeasurementFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                "frame-write-continuation-invalid",
                "The frame payload continuation does not match the active frame.");
        }

        return measurement;
    }

    internal void BuildHeader()
    {
        int firstByte = (int)Opcode;
        if (Final)
        {
            firstByte = firstByte | 128;
        }

        if (Rsv1)
        {
            firstByte = firstByte | 64;
        }

        if (Rsv2)
        {
            firstByte = firstByte | 32;
        }

        if (Rsv3)
        {
            firstByte = firstByte | 16;
        }

        HeaderBytes[0] = (byte)firstByte;
        int maskBit = 0;
        if (Masked)
        {
            maskBit = 128;
        }

        int headerIndex = WebSocketProtocolConstants.MinimumFrameHeaderLength;
        if (PayloadLength <= WebSocketProtocolConstants.BasicPayloadLengthMaximum)
        {
            HeaderBytes[1] = (byte)(maskBit | (int)PayloadLength);
        }
        else if (PayloadLength <= WebSocketProtocolConstants.Extended16PayloadLengthMaximum)
        {
            HeaderBytes[1] = (byte)(maskBit | WebSocketProtocolConstants.Extended16PayloadLengthMarker);
            HeaderBytes[headerIndex] = (byte)((PayloadLength >> 8) & 255);
            HeaderBytes[headerIndex + 1] = (byte)(PayloadLength & 255);
            headerIndex = headerIndex + 2;
        }
        else
        {
            HeaderBytes[1] = (byte)(maskBit | WebSocketProtocolConstants.Extended63PayloadLengthMarker);
            int shift = 56;
            while (shift >= 0)
            {
                HeaderBytes[headerIndex] = (byte)((PayloadLength >> shift) & 255);
                headerIndex = headerIndex + 1;
                shift = shift - 8;
            }
        }

        if (Masked)
        {
            HeaderBytes[headerIndex] = (byte)((MaskingKey >> 24) & 255U);
            HeaderBytes[headerIndex + 1] = (byte)((MaskingKey >> 16) & 255U);
            HeaderBytes[headerIndex + 2] = (byte)((MaskingKey >> 8) & 255U);
            HeaderBytes[headerIndex + 3] = (byte)(MaskingKey & 255U);
        }
    }

    internal static FrameMeasurementResult CreateMeasurementFailure(
        WebSocketParseStatus status,
        WebSocketErrorKind kind,
        string identifier,
        string diagnostic)
    {
        FrameMeasurementResult result = new FrameMeasurementResult();
        result.Status = status;
        result.HasError = true;
        result.Error.Kind = kind;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }
}
