using System;

namespace dns;

internal static class DnsQuestionCodec
{
    internal static DnsDecodeResult DecodeQuestion(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options,
        bool requireCompleteInput)
    {
        DnsDecodeResult nameResult = CompressedDnsNameDecoder.DecodeName(
            message,
            offset,
            options);
        if (!nameResult.Success || nameResult.Name == null)
        {
            return nameResult;
        }

        int valueOffset = offset + nameResult.BytesConsumed;
        if (message.Length - valueOffset < 2)
        {
            return DnsHeaderCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "question.type",
                "A DNS question type requires two octets.",
                valueOffset);
        }

        if (message.Length - valueOffset < 4)
        {
            return DnsHeaderCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "question.class",
                "A DNS question class requires two octets.",
                valueOffset + 2);
        }

        int recordTypeValue = DnsHeaderCodec.ReadUnsigned16(message, valueOffset);
        int classValue = DnsHeaderCodec.ReadUnsigned16(message, valueOffset + 2);
        int continuationOffset = valueOffset + 4;
        if (requireCompleteInput && continuationOffset != message.Length)
        {
            return DnsHeaderCodec.DecodeFailure(
                DnsDiagnosticCode.TrailingData,
                "question",
                "Trailing octets follow the DNS question.",
                continuationOffset);
        }

        DnsQuestion question = new DnsQuestion();
        question.Name = nameResult.Name;
        question.RecordType = (DnsRecordType)recordTypeValue;
        question.RecordTypeValue = recordTypeValue;
        question.Class = (DnsClass)classValue;
        question.ClassValue = classValue;
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.Question = question;
        result.BytesConsumed = continuationOffset - offset;
        int trailingLength = message.Length - continuationOffset;
        byte[] trailing = new byte[trailingLength];
        message.Slice(continuationOffset, trailingLength).CopyTo(trailing);
        result.TrailingBytes = trailing;
        return result;
    }

    internal static DnsEncodeResult EncodeQuestion(
        DnsQuestion question,
        DnsEncodeOptions options)
    {
        if (options == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length must fit the DNS message size.",
                0);
        }

        byte[] destination = new byte[options.MaximumMessageLength];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        WireWriteResult writeResult = WriteQuestion(
            writer,
            table,
            question,
            options);
        if (!writeResult.Success)
        {
            return FromWriteFailure(writeResult);
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = writer.CopyWrittenBytes();
        result.BytesWritten = writer.WrittenLength();
        return result;
    }

    internal static WireWriteResult WriteQuestion(
        WireWriter writer,
        CompressionTable table,
        DnsQuestion question,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || question == null || options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "question",
                "A writer, compression table, question, and options are required.",
                0);
        }

        int startOffset = writer.WriteOffset();
        int recordTypeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        if (recordTypeValue < 0 || recordTypeValue > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "question.type",
                "A DNS question type must fit in sixteen bits.",
                startOffset);
        }

        if (classValue < 0 || classValue > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "question.class",
                "A DNS question class must fit in sixteen bits.",
                startOffset);
        }

        WireWriteResult nameResult = table.WriteName(
            writer,
            question.Name,
            options,
            true);
        if (!nameResult.Success)
        {
            return nameResult;
        }

        WireWriteResult typeResult = writer.WriteUnsigned16((ulong)recordTypeValue);
        if (!typeResult.Success)
        {
            return typeResult;
        }

        WireWriteResult classResult = writer.WriteUnsigned16((ulong)classValue);
        if (!classResult.Success)
        {
            return classResult;
        }

        return writer.Succeed(
            startOffset,
            writer.WriteOffset() - startOffset,
            -1);
    }

    internal static DnsEncodeResult FromWriteFailure(WireWriteResult writeResult)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        int index = 0;
        while (index < writeResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(writeResult.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
