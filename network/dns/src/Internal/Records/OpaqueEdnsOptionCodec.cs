using System;

namespace dns;

internal static class OpaqueEdnsOptionCodec
{
    internal static DnsEdnsOptionResult DecodeOption(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength >
                DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumRecordDataLength < 0 ||
            options.MaximumRecordDataLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "EDNS decode limits must fit DNS wire fields.",
                0);
        }

        if (source.Length > options.MaximumMessageLength)
        {
            return OptionFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "ednsOption",
                "The EDNS option exceeds the configured message limit.",
                0);
        }

        if (source.Length < 4)
        {
            return OptionFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "ednsOption.header",
                "An EDNS option requires a four-octet header.",
                source.Length);
        }

        int codeValue = DnsHeaderCodec.ReadUnsigned16(source, 0);
        int length = DnsHeaderCodec.ReadUnsigned16(source, 2);
        if (length > options.MaximumRecordDataLength)
        {
            return OptionFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "ednsOption.length",
                "The EDNS option payload exceeds the configured data limit.",
                2);
        }

        if (length > source.Length - 4)
        {
            return OptionFailure(
                DnsDiagnosticCode.InvalidEdns,
                "ednsOption.length",
                "The EDNS option length exceeds the available payload.",
                2);
        }

        int consumed = 4 + length;
        if (options.RequireCompleteInput && consumed != source.Length)
        {
            return OptionFailure(
                DnsDiagnosticCode.TrailingData,
                "ednsOption",
                "Trailing octets follow the EDNS option.",
                consumed);
        }

        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)codeValue;
        option.CodeValue = codeValue;
        option.RawData = SharedRecordDataCodec.CopyRegion(
            source, 4, length);
        option.UseRawData = true;
        DnsEdnsOptionResult result = new DnsEdnsOptionResult();
        result.Success = true;
        result.BytesProcessed = consumed;
        result.Option = option;
        return result;
    }

    internal static DnsEdnsOptionResult EncodeOption(
        DnsEdnsOption option)
    {
        if (option == null || option.RawData == null)
        {
            return OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "ednsOption",
                "An EDNS option and its raw data are required.",
                0);
        }

        if (!option.UseRawData)
        {
            return OptionFailure(
                DnsDiagnosticCode.InvalidEdns,
                "ednsOption.data",
                "Opaque EDNS option encoding requires raw-data mode.",
                0);
        }

        int codeValue = DnsHeaderCodec.ResolveCode(
            option.CodeValue, (int)option.Code);
        if (codeValue < 0 || codeValue > 65535 ||
            option.RawData.Length > 65535)
        {
            return OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "ednsOption",
                "EDNS option codes and lengths must fit sixteen bits.",
                0);
        }

        byte[] bytes = new byte[4 + option.RawData.Length];
        DnsHeaderCodec.WriteUnsigned16(bytes, 0, codeValue);
        DnsHeaderCodec.WriteUnsigned16(bytes, 2, option.RawData.Length);
        System.Array.Copy(
            option.RawData, 0, bytes, 4, option.RawData.Length);
        DnsEdnsOptionResult result = new DnsEdnsOptionResult();
        result.Success = true;
        result.BytesProcessed = bytes.Length;
        result.Option = option;
        result.Bytes = bytes;
        return result;
    }

    internal static DnsEdnsOptionResult OptionFailure(
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
        DnsEdnsOptionResult result = new DnsEdnsOptionResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
