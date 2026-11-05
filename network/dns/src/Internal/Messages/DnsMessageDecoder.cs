using System;
using System.Collections.Generic;

namespace dns;

internal static class DnsMessageDecoder
{
    internal static DnsDecodeResult DecodeMessage(
        ReadOnlySpan<byte> message,
        DnsDecodeOptions options)
    {
        DnsDecodeResult headerResult = DnsHeaderCodec.DecodeHeader(
            message, options);
        if (!headerResult.Success || headerResult.Message == null)
        {
            return headerResult;
        }

        DnsMessage decodedMessage = headerResult.Message;
        bool updateMessage = DnsHeaderCodec.ResolveCode(
            decodedMessage.Header.OpcodeValue,
            (int)decodedMessage.Header.Opcode) == (int)DnsOpcode.Update;
        int offset = headerResult.BytesConsumed;
        int index = 0;
        while (index < decodedMessage.Header.QuestionCount)
        {
            if (offset == message.Length)
            {
                DnsDecodeResult countFailure = DnsHeaderCodec.DecodeFailure(
                    DnsDiagnosticCode.SectionCountMismatch,
                    "header.questionCount",
                    "The Question count exceeds the available entries.",
                    offset);
                return AttachSectionLocation(
                    countFailure, DnsMessageSection.Question, index);
            }

            DnsDecodeResult questionResult = DnsQuestionCodec.DecodeQuestion(
                message, offset, options, false);
            if (!questionResult.Success || questionResult.Question == null)
            {
                if (questionResult.Success)
                {
                    questionResult = DnsHeaderCodec.DecodeFailure(
                        DnsDiagnosticCode.UnexpectedEnd,
                        "question",
                        "The decoded question entry was unavailable.",
                        offset);
                }

                return AttachSectionLocation(
                    questionResult, DnsMessageSection.Question, index);
            }

            decodedMessage.Questions.Add(questionResult.Question);
            offset = offset + questionResult.BytesConsumed;
            index = index + 1;
        }

        DnsDecodeResult answerResult = DecodeRecordSection(
            message,
            offset,
            decodedMessage.Header.AnswerCount,
            DnsMessageSection.Answer,
            decodedMessage.Answers,
            options,
            updateMessage);
        if (!answerResult.Success)
        {
            return answerResult;
        }

        offset = offset + answerResult.BytesConsumed;
        DnsDecodeResult authorityResult = DecodeRecordSection(
            message,
            offset,
            decodedMessage.Header.AuthorityCount,
            DnsMessageSection.Authority,
            decodedMessage.Authorities,
            options,
            updateMessage);
        if (!authorityResult.Success)
        {
            return authorityResult;
        }

        offset = offset + authorityResult.BytesConsumed;
        DnsDecodeResult additionalResult = DecodeRecordSection(
            message,
            offset,
            decodedMessage.Header.AdditionalCount,
            DnsMessageSection.Additional,
            decodedMessage.Additionals,
            options,
            false);
        if (!additionalResult.Success)
        {
            return additionalResult;
        }

        offset = offset + additionalResult.BytesConsumed;
        EdnsPlacementResult ednsPlacement =
            EdnsRecordDataCodec.InspectPlacement(decodedMessage);
        if (!ednsPlacement.Success)
        {
            return FromEdnsPlacementFailure(ednsPlacement);
        }

        TsigPlacementResult tsigPlacement =
            TransactionRecordDataCodec.InspectTsigPlacement(decodedMessage);
        if (!tsigPlacement.Success)
        {
            return FromTsigPlacementFailure(tsigPlacement);
        }

        if (options.RequireCompleteInput && offset != message.Length)
        {
            return DnsHeaderCodec.DecodeFailure(
                DnsDiagnosticCode.TrailingData,
                "message",
                "Trailing octets follow the declared DNS message sections.",
                offset);
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.Message = decodedMessage;
        result.BytesConsumed = offset;
        result.TrailingBytes = SharedRecordDataCodec.CopyRegion(
            message, offset, message.Length - offset);
        return result;
    }

    static DnsDecodeResult DecodeRecordSection(
        ReadOnlySpan<byte> message,
        int offset,
        int count,
        DnsMessageSection section,
        List<DnsResourceRecord> records,
        DnsDecodeOptions options,
        bool allowUpdateControl)
    {
        int startingOffset = offset;
        int index = 0;
        while (index < count)
        {
            if (offset == message.Length)
            {
                DnsDecodeResult countFailure = DnsHeaderCodec.DecodeFailure(
                    DnsDiagnosticCode.SectionCountMismatch,
                    "header.sectionCount",
                    "The section count exceeds the available entries.",
                    offset);
                return AttachSectionLocation(
                    countFailure, section, index);
            }

            DnsDecodeResult recordResult;
            if (allowUpdateControl)
            {
                recordResult =
                    ResourceRecordCodec.DecodeUpdateResourceRecord(
                        message, offset, options);
            }
            else
            {
                recordResult = ResourceRecordCodec.DecodeResourceRecord(
                    message, offset, options);
            }
            if (!recordResult.Success || recordResult.Record == null)
            {
                if (recordResult.Success)
                {
                    recordResult = DnsHeaderCodec.DecodeFailure(
                        DnsDiagnosticCode.UnexpectedEnd,
                        "resourceRecord",
                        "The decoded resource-record entry was unavailable.",
                        offset);
                }

                return AttachSectionLocation(recordResult, section, index);
            }

            records.Add(recordResult.Record);
            offset = offset + recordResult.BytesConsumed;
            index = index + 1;
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.BytesConsumed = offset - startingOffset;
        return result;
    }

    static DnsDecodeResult AttachSectionLocation(
        DnsDecodeResult result,
        DnsMessageSection section,
        int recordIndex)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            DnsDiagnostic diagnostic = result.Diagnostics[index];
            diagnostic.HasSection = true;
            diagnostic.Section = section;
            diagnostic.HasRecordIndex = true;
            diagnostic.RecordIndex = recordIndex;
            index = index + 1;
        }

        return result;
    }

    static DnsDecodeResult FromTsigPlacementFailure(
        TsigPlacementResult placement)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        int index = 0;
        while (index < placement.Diagnostics.Count)
        {
            result.Diagnostics.Add(placement.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsDecodeResult FromEdnsPlacementFailure(
        EdnsPlacementResult placement)
    {
        DnsDecodeResult result = new DnsDecodeResult();
        int index = 0;
        while (index < placement.Diagnostics.Count)
        {
            result.Diagnostics.Add(placement.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
