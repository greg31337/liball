using System;

namespace dns;

internal static class DnsHeaderCodec
{
    internal const int HeaderFlagMask = 34800;

    internal static DnsDecodeResult DecodeHeader(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        if (options == null)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                0);
        }

        DnsDecodeResult optionFailure = ValidateDecodeOptions(options);
        if (!optionFailure.Success)
        {
            return optionFailure;
        }

        if (source.Length > options.MaximumMessageLength)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "message",
                "The DNS message exceeds the configured length limit.",
                0);
        }

        if (source.Length < DnsProtocolLimits.HeaderLength)
        {
            return DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "header",
                "A DNS header requires exactly twelve octets.",
                source.Length);
        }

        int identifier = (source[0] << 8) | source[1];
        int flagsValue = (source[2] << 8) | source[3];
        if (options.RejectReservedHeaderBits && (flagsValue & 64) != 0)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "header.flags",
                "The reserved DNS header bit must be zero.",
                2);
        }

        int questionCount = ReadUnsigned16(source, 4);
        int answerCount = ReadUnsigned16(source, 6);
        int authorityCount = ReadUnsigned16(source, 8);
        int additionalCount = ReadUnsigned16(source, 10);
        DnsDecodeResult countFailure = ValidateDecodedCounts(
            questionCount,
            answerCount,
            authorityCount,
            additionalCount,
            options);
        if (!countFailure.Success)
        {
            return countFailure;
        }

        int opcodeValue = (flagsValue >> 11) & 15;
        int responseCodeValue = flagsValue & 15;
        DnsMessageHeader header = new DnsMessageHeader();
        header.Identifier = (ushort)identifier;
        header.Opcode = (DnsOpcode)opcodeValue;
        header.OpcodeValue = opcodeValue;
        header.ResponseCode = (DnsResponseCode)responseCodeValue;
        header.ResponseCodeValue = responseCodeValue;
        header.Flags = (DnsHeaderFlag)(flagsValue & HeaderFlagMask);
        header.FlagsValue = flagsValue;
        header.QuestionCount = questionCount;
        header.AnswerCount = answerCount;
        header.AuthorityCount = authorityCount;
        header.AdditionalCount = additionalCount;
        DnsMessage message = new DnsMessage();
        message.Header = header;

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.Message = message;
        result.BytesConsumed = DnsProtocolLimits.HeaderLength;
        int trailingLength = source.Length - DnsProtocolLimits.HeaderLength;
        byte[] trailing = new byte[trailingLength];
        source.Slice(DnsProtocolLimits.HeaderLength, trailingLength).CopyTo(trailing);
        result.TrailingBytes = trailing;
        return result;
    }

    internal static DnsEncodeResult EncodeHeader(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        if (options == null)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length must fit the DNS message size.",
                0);
        }

        if (options.MaximumMessageLength < DnsProtocolLimits.HeaderLength)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "options.MaximumMessageLength",
                "The configured message length cannot contain a DNS header.",
                0);
        }

        if (message == null || message.Header == null)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message",
                "A DNS message and header are required.",
                0);
        }

        if (message.Questions == null || message.Answers == null ||
            message.Authorities == null || message.Additionals == null)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message.sections",
                "All DNS message section collections are required.",
                0);
        }

        DnsMessageHeader header = message.Header;
        int questionCount = header.QuestionCount;
        int answerCount = header.AnswerCount;
        int authorityCount = header.AuthorityCount;
        int additionalCount = header.AdditionalCount;
        if (options.DeriveSectionCounts)
        {
            questionCount = message.Questions.Count;
            answerCount = message.Answers.Count;
            authorityCount = message.Authorities.Count;
            additionalCount = message.Additionals.Count;
        }
        else if (questionCount != message.Questions.Count ||
            answerCount != message.Answers.Count ||
            authorityCount != message.Authorities.Count ||
            additionalCount != message.Additionals.Count)
        {
            return EncodeFailure(
                DnsDiagnosticCode.SectionCountMismatch,
                "header.counts",
                "Header section counts do not match the message collections.",
                4);
        }

        DnsEncodeResult countFailure = ValidateEncodedCounts(
            questionCount,
            answerCount,
            authorityCount,
            additionalCount);
        if (!countFailure.Success)
        {
            return countFailure;
        }

        int opcodeValue = ResolveCode(header.OpcodeValue, (int)header.Opcode);
        int responseCodeValue = ResolveCode(
            header.ResponseCodeValue,
            (int)header.ResponseCode);
        if (opcodeValue < 0 || opcodeValue > 15)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "header.opcode",
                "A DNS opcode must fit in four bits.",
                2);
        }

        if (responseCodeValue < 0 || responseCodeValue > 15)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "header.responseCode",
                "A base DNS response code must fit in four bits.",
                3);
        }

        int enumFlags = (int)header.Flags;
        if (enumFlags < 0 || (enumFlags & ~HeaderFlagMask) != 0 ||
            header.FlagsValue < 0 || header.FlagsValue > 65535)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "header.flags",
                "DNS header flags contain bits outside the recognized flag mask.",
                2);
        }

        int rawFlagBits = header.FlagsValue & HeaderFlagMask;
        int flagBits = enumFlags;
        if (flagBits == 0 && rawFlagBits != 0)
        {
            flagBits = rawFlagBits;
        }

        int flagsValue = flagBits | (opcodeValue << 11) | responseCodeValue;
        byte[] bytes = new byte[DnsProtocolLimits.HeaderLength];
        bytes[0] = (byte)(header.Identifier >> 8);
        bytes[1] = (byte)(header.Identifier & 255);
        bytes[2] = (byte)(flagsValue >> 8);
        bytes[3] = (byte)(flagsValue & 255);
        WriteUnsigned16(bytes, 4, questionCount);
        WriteUnsigned16(bytes, 6, answerCount);
        WriteUnsigned16(bytes, 8, authorityCount);
        WriteUnsigned16(bytes, 10, additionalCount);

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = bytes;
        result.BytesWritten = bytes.Length;
        return result;
    }

    internal static DnsDecodeResult ValidateDecodeOptions(DnsDecodeOptions options)
    {
        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength > DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumSectionEntryCount < 0 ||
            options.MaximumTotalSectionEntryCount < 0 ||
            options.MaximumRecordDataLength < 0 ||
            options.MaximumRecordDataLength > DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumCompressionPointerHops < 0)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode limits must be nonnegative protocol-width values.",
                0);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    internal static DnsDecodeResult ValidateDecodedCounts(
        int questionCount,
        int answerCount,
        int authorityCount,
        int additionalCount,
        DnsDecodeOptions options)
    {
        int[] counts = [questionCount, answerCount, authorityCount, additionalCount];
        int[] offsets = [4, 6, 8, 10];
        int index = 0;
        int total = 0;
        while (index < counts.Length)
        {
            if (counts[index] > options.MaximumSectionEntryCount)
            {
                return DecodeFailure(
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "header.counts",
                    "A DNS section count exceeds the configured limit.",
                    offsets[index]);
            }

            total = total + counts[index];
            index = index + 1;
        }

        if (total > options.MaximumTotalSectionEntryCount)
        {
            return DecodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "header.counts",
                "The total DNS section count exceeds the configured limit.",
                4);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    internal static DnsEncodeResult ValidateEncodedCounts(
        int questionCount,
        int answerCount,
        int authorityCount,
        int additionalCount)
    {
        int[] counts = [questionCount, answerCount, authorityCount, additionalCount];
        int[] offsets = [4, 6, 8, 10];
        int index = 0;
        while (index < counts.Length)
        {
            if (counts[index] < 0 || counts[index] > 65535)
            {
                return EncodeFailure(
                    DnsDiagnosticCode.ValueOutOfRange,
                    "header.counts",
                    "A DNS section count must fit in sixteen bits.",
                    offsets[index]);
            }

            index = index + 1;
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        return result;
    }

    internal static int ResolveCode(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }

    internal static int ReadUnsigned16(ReadOnlySpan<byte> source, int offset)
    {
        return (source[offset] << 8) | source[offset + 1];
    }

    internal static void WriteUnsigned16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)(value & 255);
    }

    internal static DnsDecodeResult DecodeFailure(
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

    internal static DnsEncodeResult EncodeFailure(
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
        DnsEncodeResult result = new DnsEncodeResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
