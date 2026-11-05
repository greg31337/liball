using System;

namespace websocket;

internal class FrameHeaderReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal bool HasHeader;
    internal WebSocketFrameHeader Header = new WebSocketFrameHeader();
}

internal class FramePayloadReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal byte[] Payload = [];
    internal int PayloadCount;
    internal bool EndOfFrame;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class FrameReadState
{
    internal byte[] HeaderBytes = new byte[WebSocketProtocolConstants.MaximumFrameHeaderLength];
    internal int HeaderByteCount;
    internal int RequiredHeaderByteCount = WebSocketProtocolConstants.MinimumFrameHeaderLength;
    internal int PayloadLengthMarker;
    internal bool BaseHeaderDecoded;
    internal bool PayloadLengthDecoded;
    internal bool MaskingKeyDecoded;
    internal bool HeaderComplete;
    internal bool HeaderFailed;
    internal WebSocketFrameHeader Header = new WebSocketFrameHeader();
    internal long PayloadByteCount;
    internal bool PayloadLimitChecked;
    internal bool PayloadComplete;
    internal bool PayloadFailed;
    internal WebSocketError PayloadError = new WebSocketError();
    internal WebSocketMasker Masker = new WebSocketMasker();

    internal FrameHeaderReadResult ReadHeader(ReadOnlySpan<byte> source)
    {
        FrameHeaderReadResult result = new FrameHeaderReadResult();
        if (HeaderFailed)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        if (HeaderComplete)
        {
            result.Status = WebSocketParseStatus.Complete;
            result.HasHeader = true;
            result.Header = Header;
            return result;
        }

        int sourceIndex = 0;
        while (!HeaderComplete && !HeaderFailed)
        {
            while (HeaderByteCount < RequiredHeaderByteCount && sourceIndex < source.Length)
            {
                HeaderBytes[HeaderByteCount] = source[sourceIndex];
                HeaderByteCount = HeaderByteCount + 1;
                sourceIndex = sourceIndex + 1;
            }

            if (HeaderByteCount < RequiredHeaderByteCount)
            {
                break;
            }

            if (!BaseHeaderDecoded)
            {
                DecodeBaseHeader();
            }
            else if (!PayloadLengthDecoded)
            {
                DecodePayloadLength();
            }
            else if (Header.Masked && !MaskingKeyDecoded)
            {
                DecodeMaskingKey();
            }
            else
            {
                HeaderComplete = true;
            }
        }

        result.ConsumedCount = sourceIndex;
        if (HeaderFailed)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
        }
        else if (HeaderComplete)
        {
            result.Status = WebSocketParseStatus.Complete;
            result.HasHeader = true;
            result.Header = Header;
        }

        return result;
    }

    internal FramePayloadReadResult ReadPayload(
        ReadOnlySpan<byte> source,
        int maximumFramePayloadLength)
    {
        FramePayloadReadResult result = new FramePayloadReadResult();
        if (HeaderFailed)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        if (!HeaderComplete)
        {
            return result;
        }

        if (PayloadFailed)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            result.HasError = true;
            result.Error = PayloadError;
            return result;
        }

        if (PayloadComplete)
        {
            result.Status = WebSocketParseStatus.Complete;
            result.EndOfFrame = true;
            return result;
        }

        if (!PayloadLimitChecked)
        {
            if (maximumFramePayloadLength <= 0)
            {
                PayloadFailed = true;
                PayloadError = CreatePayloadLimitError(
                    "frame-limit-invalid",
                    "The configured frame payload limit is invalid.");
            }
            else if (Header.PayloadLength > maximumFramePayloadLength)
            {
                PayloadFailed = true;
                PayloadError = CreatePayloadLimitError(
                    "frame-payload-too-large",
                    "The frame payload exceeds the configured limit.");
            }

            if (PayloadFailed)
            {
                result.Status = WebSocketParseStatus.LimitRejected;
                result.HasError = true;
                result.Error = PayloadError;
                return result;
            }

            PayloadLimitChecked = true;
        }

        if (Header.PayloadLength == 0)
        {
            PayloadComplete = true;
            result.Status = WebSocketParseStatus.Complete;
            result.EndOfFrame = true;
            return result;
        }

        long remainingCount = Header.PayloadLength - PayloadByteCount;
        int copyCount = source.Length;
        if (remainingCount < copyCount)
        {
            copyCount = (int)remainingCount;
        }

        if (copyCount == 0)
        {
            return result;
        }

        byte[] payload = new byte[copyCount];
        source.Slice(0, copyCount).CopyTo(payload);
        if (Header.Masked)
        {
            Masker.ApplyMask(payload, Header.MaskingKey, (int)PayloadByteCount);
        }

        PayloadByteCount = PayloadByteCount + copyCount;
        result.ConsumedCount = copyCount;
        result.Payload = payload;
        result.PayloadCount = copyCount;
        if (PayloadByteCount == Header.PayloadLength)
        {
            PayloadComplete = true;
            result.Status = WebSocketParseStatus.Complete;
            result.EndOfFrame = true;
        }

        return result;
    }

    internal void ResetHeader()
    {
        HeaderByteCount = 0;
        RequiredHeaderByteCount = WebSocketProtocolConstants.MinimumFrameHeaderLength;
        PayloadLengthMarker = 0;
        BaseHeaderDecoded = false;
        PayloadLengthDecoded = false;
        MaskingKeyDecoded = false;
        HeaderComplete = false;
        HeaderFailed = false;
        Header = new WebSocketFrameHeader();
        PayloadByteCount = 0;
        PayloadLimitChecked = false;
        PayloadComplete = false;
        PayloadFailed = false;
        PayloadError = new WebSocketError();
    }

    internal void DecodeBaseHeader()
    {
        int firstByte = HeaderBytes[0];
        int secondByte = HeaderBytes[1];
        Header.Final = (firstByte & 128) != 0;
        Header.Rsv1 = (firstByte & 64) != 0;
        Header.Rsv2 = (firstByte & 32) != 0;
        Header.Rsv3 = (firstByte & 16) != 0;
        Header.Opcode = (WebSocketOpcode)(firstByte & 15);
        Header.Masked = (secondByte & 128) != 0;
        PayloadLengthMarker = secondByte & 127;
        BaseHeaderDecoded = true;

        if (PayloadLengthMarker <= WebSocketProtocolConstants.BasicPayloadLengthMaximum)
        {
            Header.PayloadLength = PayloadLengthMarker;
            PayloadLengthDecoded = true;
            if (Header.Masked)
            {
                RequiredHeaderByteCount = RequiredHeaderByteCount + WebSocketProtocolConstants.MaskingKeyLength;
            }
            else
            {
                HeaderComplete = true;
            }
        }
        else if (PayloadLengthMarker == WebSocketProtocolConstants.Extended16PayloadLengthMarker)
        {
            RequiredHeaderByteCount = RequiredHeaderByteCount + 2;
        }
        else
        {
            RequiredHeaderByteCount = RequiredHeaderByteCount + 8;
        }
    }

    internal void DecodePayloadLength()
    {
        NetworkIntegerReadResult lengthResult;
        if (PayloadLengthMarker == WebSocketProtocolConstants.Extended16PayloadLengthMarker)
        {
            lengthResult = NetworkIntegerCodec.ReadCanonicalPayloadLength16(
                HeaderBytes.AsSpan(WebSocketProtocolConstants.MinimumFrameHeaderLength, 2));
        }
        else
        {
            lengthResult = NetworkIntegerCodec.ReadCanonicalPayloadLength63(
                HeaderBytes.AsSpan(WebSocketProtocolConstants.MinimumFrameHeaderLength, 8));
        }

        if (lengthResult.Status != WebSocketParseStatus.Complete)
        {
            HeaderFailed = true;
            return;
        }

        Header.PayloadLength = lengthResult.Value;
        PayloadLengthDecoded = true;
        if (Header.Masked)
        {
            RequiredHeaderByteCount = RequiredHeaderByteCount + WebSocketProtocolConstants.MaskingKeyLength;
        }
        else
        {
            HeaderComplete = true;
        }
    }

    internal void DecodeMaskingKey()
    {
        int keyOffset = RequiredHeaderByteCount - WebSocketProtocolConstants.MaskingKeyLength;
        Header.MaskingKey =
            ((uint)HeaderBytes[keyOffset] << 24) |
            ((uint)HeaderBytes[keyOffset + 1] << 16) |
            ((uint)HeaderBytes[keyOffset + 2] << 8) |
            (uint)HeaderBytes[keyOffset + 3];
        MaskingKeyDecoded = true;
        HeaderComplete = true;
    }

    internal WebSocketError CreatePayloadLimitError(string identifier, string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.MessageTooBig;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.MessageTooBig;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return error;
    }
}
