using System;
using System.Collections.Generic;

namespace dns;

internal class TcpFrameAccumulationResult
{
    internal bool Success;
    internal int BytesAccepted;
    internal int BufferedByteCount;
    internal List<DnsTcpFrame> Frames = new List<DnsTcpFrame>();
    internal DnsTcpFrame? PartialFrame;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class DnsTcpFrameAccumulator
{
    List<byte> bufferedBytes = new List<byte>();

    internal DnsTcpFrameAccumulator()
    {
    }

    internal TcpFrameAccumulationResult AppendBytes(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation =
            DnsTcpFrameCodec.ValidateFrameDecodeOptions(options);
        if (!validation.Success)
        {
            TcpFrameAccumulationResult invalid =
                FromDecodeFailure(validation, 0);
            invalid.BufferedByteCount = this.bufferedBytes.Count;
            return invalid;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            this.bufferedBytes.Add(source[sourceIndex]);
            sourceIndex = sourceIndex + 1;
        }

        TcpFrameAccumulationResult result = new TcpFrameAccumulationResult();
        result.Success = true;
        result.BytesAccepted = source.Length;
        int cursor = 0;
        while (this.bufferedBytes.Count - cursor >=
            DnsProtocolLimits.TcpLengthPrefixLength)
        {
            int messageLength =
                (this.bufferedBytes[cursor] << 8) |
                this.bufferedBytes[cursor + 1];
            if (messageLength == 0)
            {
                DnsDecodeResult failure = DnsTcpFrameCodec.FrameDecodeFailure(
                    DnsDiagnosticCode.InvalidTcpFrame,
                    "tcpFrame.messageLength",
                    "A DNS-over-TCP frame cannot contain an empty message.",
                    cursor);
                this.bufferedBytes.Clear();
                return FailAccumulation(
                    result, failure, source.Length);
            }

            if (messageLength > options.MaximumMessageLength)
            {
                DnsDecodeResult failure = DnsTcpFrameCodec.FrameDecodeFailure(
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "tcpFrame.messageLength",
                    "The DNS-over-TCP frame exceeds the configured message limit.",
                    cursor);
                this.bufferedBytes.Clear();
                return FailAccumulation(
                    result, failure, source.Length);
            }

            int frameLength = DnsProtocolLimits.TcpLengthPrefixLength +
                messageLength;
            if (this.bufferedBytes.Count - cursor < frameLength)
            {
                break;
            }

            DnsTcpFrame frame = new DnsTcpFrame();
            frame.MessageLength = messageLength;
            frame.MessageBytes = CopyBufferedRegion(
                this.bufferedBytes,
                cursor + DnsProtocolLimits.TcpLengthPrefixLength,
                messageLength);
            frame.Complete = true;
            result.Frames.Add(frame);
            cursor = cursor + frameLength;
        }

        if (cursor > 0)
        {
            this.bufferedBytes.RemoveRange(0, cursor);
        }

        result.BufferedByteCount = this.bufferedBytes.Count;
        if (this.bufferedBytes.Count >=
            DnsProtocolLimits.TcpLengthPrefixLength)
        {
            int partialLength =
                (this.bufferedBytes[0] << 8) | this.bufferedBytes[1];
            DnsTcpFrame partial = new DnsTcpFrame();
            partial.MessageLength = partialLength;
            partial.MessageBytes = CopyBufferedRegion(
                this.bufferedBytes,
                DnsProtocolLimits.TcpLengthPrefixLength,
                this.bufferedBytes.Count -
                    DnsProtocolLimits.TcpLengthPrefixLength);
            partial.Complete = false;
            result.PartialFrame = partial;
        }

        return result;
    }

    internal void ResetAccumulator()
    {
        this.bufferedBytes.Clear();
    }

    static TcpFrameAccumulationResult FromDecodeFailure(
        DnsDecodeResult failure,
        int bytesAccepted)
    {
        TcpFrameAccumulationResult result = new TcpFrameAccumulationResult();
        result.BytesAccepted = bytesAccepted;
        int index = 0;
        while (index < failure.Diagnostics.Count)
        {
            result.Diagnostics.Add(failure.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static TcpFrameAccumulationResult FailAccumulation(
        TcpFrameAccumulationResult result,
        DnsDecodeResult failure,
        int bytesAccepted)
    {
        result.Success = false;
        result.BytesAccepted = bytesAccepted;
        result.BufferedByteCount = 0;
        int index = 0;
        while (index < failure.Diagnostics.Count)
        {
            result.Diagnostics.Add(failure.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static byte[] CopyBufferedRegion(
        List<byte> source,
        int offset,
        int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[offset + index];
            index = index + 1;
        }

        return result;
    }
}

internal static class DnsTcpFrameCodec
{
    internal static DnsDecodeResult DecodeTcpFrame(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = ValidateFrameDecodeOptions(options);
        if (!validation.Success)
        {
            return validation;
        }

        if (source.Length < DnsProtocolLimits.TcpLengthPrefixLength)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "tcpFrame.lengthPrefix",
                "A DNS-over-TCP frame requires a two-octet length prefix.",
                source.Length);
        }

        int messageLength = DnsHeaderCodec.ReadUnsigned16(source, 0);
        if (messageLength == 0)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.InvalidTcpFrame,
                "tcpFrame.messageLength",
                "A DNS-over-TCP frame cannot contain an empty message.",
                0);
        }

        if (messageLength > options.MaximumMessageLength)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "tcpFrame.messageLength",
                "The DNS-over-TCP frame exceeds the configured message limit.",
                0);
        }

        int availableLength = source.Length -
            DnsProtocolLimits.TcpLengthPrefixLength;
        int copiedLength = availableLength;
        if (messageLength < copiedLength)
        {
            copiedLength = messageLength;
        }

        DnsTcpFrame frame = new DnsTcpFrame();
        frame.MessageLength = messageLength;
        frame.MessageBytes = CopySpanRegion(
            source,
            DnsProtocolLimits.TcpLengthPrefixLength,
            copiedLength);
        frame.Complete = availableLength >= messageLength;
        int consumed = DnsProtocolLimits.TcpLengthPrefixLength + copiedLength;
        if (frame.Complete && options.RequireCompleteInput &&
            source.Length != consumed)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.TrailingData,
                "tcpFrame",
                "Trailing octets follow the DNS-over-TCP frame.",
                consumed);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.TcpFrame = frame;
        result.BytesConsumed = consumed;
        result.TrailingBytes = CopySpanRegion(
            source, consumed, source.Length - consumed);
        return result;
    }

    internal static DnsEncodeResult EncodeTcpFrame(
        DnsTcpFrame frame,
        DnsEncodeOptions options)
    {
        DnsEncodeResult validation = ValidateFrameEncodeOptions(options);
        if (!validation.Success)
        {
            return validation;
        }

        if (frame == null || frame.MessageBytes == null)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "tcpFrame",
                "A DNS-over-TCP frame and its message bytes are required.",
                0);
        }

        if (!frame.Complete)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.InvalidTcpFrame,
                "tcpFrame.complete",
                "Only a complete DNS-over-TCP frame can be encoded.",
                0);
        }

        int messageLength = frame.MessageBytes.Length;
        if (messageLength == 0)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.InvalidTcpFrame,
                "tcpFrame.messageLength",
                "A DNS-over-TCP frame cannot contain an empty message.",
                0);
        }

        if (frame.MessageLength != 0 &&
            frame.MessageLength != messageLength)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.InvalidTcpFrame,
                "tcpFrame.messageLength",
                "The declared frame length must match the message bytes.",
                0);
        }

        if (messageLength > options.MaximumMessageLength)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "tcpFrame.messageLength",
                "The DNS-over-TCP frame exceeds the configured message limit.",
                0);
        }

        byte[] bytes = new byte[
            DnsProtocolLimits.TcpLengthPrefixLength + messageLength];
        DnsHeaderCodec.WriteUnsigned16(bytes, 0, messageLength);
        System.Array.Copy(
            frame.MessageBytes,
            0,
            bytes,
            DnsProtocolLimits.TcpLengthPrefixLength,
            messageLength);
        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = bytes;
        result.BytesWritten = bytes.Length;
        return result;
    }

    internal static DnsDecodeResult ValidateFrameDecodeOptions(
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return FrameDecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum frame message length must fit in sixteen bits.",
                0);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    internal static DnsEncodeResult ValidateFrameEncodeOptions(
        DnsEncodeOptions options)
    {
        if (options == null)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return FrameEncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum frame message length must fit in sixteen bits.",
                0);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        return result;
    }

    internal static DnsDecodeResult FrameDecodeFailure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        return DnsHeaderCodec.DecodeFailure(code, field, reason, offset);
    }

    internal static DnsEncodeResult FrameEncodeFailure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        return DnsHeaderCodec.EncodeFailure(code, field, reason, offset);
    }

    static byte[] CopySpanRegion(
        ReadOnlySpan<byte> source,
        int offset,
        int length)
    {
        byte[] result = new byte[length];
        source.Slice(offset, length).CopyTo(result);
        return result;
    }
}
