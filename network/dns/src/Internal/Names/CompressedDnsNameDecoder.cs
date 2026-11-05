using System;
using System.Collections.Generic;

namespace dns;

internal static class CompressedDnsNameDecoder
{
    internal static DnsDecodeResult DecodeName(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                offset);
        }

        if (options.MaximumMessageLength < 0)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length cannot be negative.",
                offset);
        }

        if (message.Length > options.MaximumMessageLength)
        {
            return Failure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "message",
                "The DNS message exceeds the configured length limit.",
                0);
        }

        if (options.MaximumCompressionPointerHops < 0)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumCompressionPointerHops",
                "The compression pointer hop limit cannot be negative.",
                offset);
        }

        if (offset < 0 || offset > message.Length)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "offset",
                "The DNS name offset is outside the message.",
                offset);
        }

        if (offset == message.Length)
        {
            return Failure(
                DnsDiagnosticCode.UnexpectedEnd,
                "name",
                "A DNS name requires at least a root label.",
                offset);
        }

        byte[] source = message.ToArray();
        bool[] visitedOffsets = new bool[source.Length];
        List<byte[]> labels = new List<byte[]>();
        int cursor = offset;
        int continuationOffset = -1;
        int expandedLength = 1;
        int pointerHops = 0;
        while (true)
        {
            if (cursor < 0 || cursor >= source.Length)
            {
                return Failure(
                    DnsDiagnosticCode.UnexpectedEnd,
                    "name",
                    "The DNS name is missing its terminating root label.",
                    cursor);
            }

            if (visitedOffsets[cursor])
            {
                return Failure(
                    DnsDiagnosticCode.CompressionLoop,
                    "name",
                    "A compression pointer chain contains a cycle.",
                    cursor);
            }

            visitedOffsets[cursor] = true;
            int lengthOctet = source[cursor];
            int labelTag = lengthOctet & 192;
            if (labelTag == 192)
            {
                if (cursor + 1 >= source.Length)
                {
                    return Failure(
                        DnsDiagnosticCode.UnexpectedEnd,
                        "name",
                        "A compression pointer is truncated.",
                        cursor + 1);
                }

                int target = ((lengthOctet & 63) << 8) | source[cursor + 1];
                if (target >= source.Length)
                {
                    return Failure(
                        DnsDiagnosticCode.CompressionPointerOutOfRange,
                        "name",
                        "A compression pointer target is outside the message.",
                        cursor);
                }

                if (visitedOffsets[target])
                {
                    return Failure(
                        DnsDiagnosticCode.CompressionLoop,
                        "name",
                        "A compression pointer chain contains a cycle.",
                        cursor);
                }

                if (target >= cursor)
                {
                    return Failure(
                        DnsDiagnosticCode.CompressionPointerForward,
                        "name",
                        "A compression pointer must refer to an earlier message location.",
                        cursor);
                }

                if (pointerHops >= options.MaximumCompressionPointerHops)
                {
                    return Failure(
                        DnsDiagnosticCode.CompressionHopLimitExceeded,
                        "name",
                        "The compression pointer hop limit was exceeded.",
                        cursor);
                }

                if (continuationOffset < 0)
                {
                    continuationOffset = cursor + 2;
                }

                pointerHops = pointerHops + 1;
                cursor = target;
                continue;
            }

            if (labelTag != 0)
            {
                return Failure(
                    DnsDiagnosticCode.InvalidLabel,
                    "name",
                    "A DNS label uses a reserved length-octet tag.",
                    cursor);
            }

            int labelLength = lengthOctet;
            if (labelLength == 0)
            {
                if (continuationOffset < 0)
                {
                    continuationOffset = cursor + 1;
                }

                RawDnsNameResult rawResult = RawDnsName.CreateAbsoluteName(labels);
                if (!rawResult.Success || rawResult.Name == null)
                {
                    return FromRawFailure(rawResult, cursor);
                }

                return Success(
                    rawResult.Name,
                    source,
                    offset,
                    continuationOffset);
            }

            if (cursor + 1 > source.Length - labelLength)
            {
                return Failure(
                    DnsDiagnosticCode.UnexpectedEnd,
                    "name",
                    "A DNS label payload is truncated.",
                    cursor + 1);
            }

            if (expandedLength >
                DnsProtocolLimits.MaximumNameLength - labelLength - 1)
            {
                return Failure(
                    DnsDiagnosticCode.NameTooLong,
                    "name",
                    "The expanded DNS name exceeds 255 octets.",
                    cursor);
            }

            byte[] label = new byte[labelLength];
            Array.Copy(source, cursor + 1, label, 0, labelLength);
            labels.Add(label);
            expandedLength = expandedLength + labelLength + 1;
            cursor = cursor + labelLength + 1;
        }
    }

    internal static DnsDecodeResult Success(
        DnsName name,
        byte[] source,
        int startingOffset,
        int continuationOffset)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.Name = name;
        result.BytesConsumed = continuationOffset - startingOffset;
        int trailingLength = source.Length - continuationOffset;
        byte[] trailingBytes = new byte[trailingLength];
        Array.Copy(source, continuationOffset, trailingBytes, 0, trailingLength);
        result.TrailingBytes = trailingBytes;
        return result;
    }

    internal static DnsDecodeResult FromRawFailure(
        RawDnsNameResult rawResult,
        int offset)
    {
        if (rawResult.Diagnostics.Count == 0)
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Decoded name data is unavailable.",
                offset);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        int index = 0;
        while (index < rawResult.Diagnostics.Count)
        {
            DnsDiagnostic sourceDiagnostic = rawResult.Diagnostics[index];
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = sourceDiagnostic.Code;
            diagnostic.HasOffset = true;
            diagnostic.Offset = offset;
            diagnostic.Field = sourceDiagnostic.Field;
            diagnostic.Reason = sourceDiagnostic.Reason;
            result.Diagnostics.Add(diagnostic);
            index = index + 1;
        }

        return result;
    }

    internal static DnsDecodeResult Failure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsDecodeResult result = new DnsDecodeResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
