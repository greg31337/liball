using System.Collections.Generic;

namespace dns;

internal static class DnsMessageEncoder
{
    internal static DnsEncodeResult EncodeMessage(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        DnsEncodeResult validation = ValidateMessageInput(message, options);
        if (!validation.Success)
        {
            return validation;
        }

        EdnsPlacementResult ednsPlacement =
            EdnsRecordDataCodec.InspectPlacement(message);
        if (!ednsPlacement.Success)
        {
            return FromEdnsPlacementFailure(ednsPlacement);
        }

        if (options.TruncateToMaximumLength)
        {
            return DnsMessageTruncator.EncodeTruncatedMessage(
                message, options);
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success)
        {
            return FromTsigPlacementFailure(placement);
        }

        DnsMessage prepared = CreatePreparedMessage(message);
        DnsEncodeResult questions = PrepareQuestions(
            message.Questions, prepared.Questions, options);
        if (!questions.Success)
        {
            return questions;
        }

        DnsEncodeResult answers = PrepareRecordSection(
            message.Answers,
            prepared.Answers,
            options,
            DnsMessageSection.Answer,
            false);
        if (!answers.Success)
        {
            return answers;
        }

        DnsEncodeResult authorities = PrepareRecordSection(
            message.Authorities,
            prepared.Authorities,
            options,
            DnsMessageSection.Authority,
            false);
        if (!authorities.Success)
        {
            return authorities;
        }

        DnsEncodeResult additionals = PrepareRecordSection(
            message.Additionals,
            prepared.Additionals,
            options,
            DnsMessageSection.Additional,
            placement.HasTsig);
        if (!additionals.Success)
        {
            return additionals;
        }

        if (options.DeriveSectionCounts)
        {
            prepared.Header.QuestionCount = prepared.Questions.Count;
            prepared.Header.AnswerCount = prepared.Answers.Count;
            prepared.Header.AuthorityCount = prepared.Authorities.Count;
            prepared.Header.AdditionalCount = prepared.Additionals.Count;
        }

        DnsEncodeResult header = DnsHeaderCodec.EncodeHeader(prepared, options);
        if (!header.Success)
        {
            return header;
        }

        WireWriter counter = new WireWriter(
            null, 0, options.MaximumMessageLength);
        DnsEncodeResult measured = WritePreparedMessage(
            counter,
            new CompressionTable(0),
            prepared,
            header.Bytes,
            options);
        if (!measured.Success)
        {
            return measured;
        }

        byte[] bytes = new byte[counter.WrittenLength()];
        WireWriter writer = new WireWriter(bytes, 0, bytes.Length);
        DnsEncodeResult written = WritePreparedMessage(
            writer,
            new CompressionTable(0),
            prepared,
            header.Bytes,
            options);
        if (!written.Success)
        {
            return written;
        }

        if (writer.WrittenLength() != bytes.Length)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "message",
                "Measured and written DNS message lengths do not match.",
                writer.WrittenLength());
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = bytes;
        result.BytesWritten = bytes.Length;
        result.Message = prepared;
        return result;
    }

    static DnsEncodeResult ValidateMessageInput(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        if (options == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encoding options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length must fit the DNS message size.",
                0);
        }

        if (message == null || message.Header == null ||
            message.Questions == null || message.Answers == null ||
            message.Authorities == null || message.Additionals == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message",
                "A complete DNS message and all section collections are required.",
                0);
        }

        if (options.Canonical && options.CanonicalOrder == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.CanonicalOrder",
                "Canonical ordering options are required in canonical mode.",
                0);
        }

        return SuccessfulEncodingStep();
    }

    static DnsMessage CreatePreparedMessage(DnsMessage source)
    {
        DnsMessageHeader header = new DnsMessageHeader();
        header.Identifier = source.Header.Identifier;
        header.Opcode = source.Header.Opcode;
        header.OpcodeValue = source.Header.OpcodeValue;
        header.ResponseCode = source.Header.ResponseCode;
        header.ResponseCodeValue = source.Header.ResponseCodeValue;
        header.Flags = source.Header.Flags;
        header.FlagsValue = source.Header.FlagsValue;
        header.QuestionCount = source.Header.QuestionCount;
        header.AnswerCount = source.Header.AnswerCount;
        header.AuthorityCount = source.Header.AuthorityCount;
        header.AdditionalCount = source.Header.AdditionalCount;
        DnsMessage result = new DnsMessage();
        result.Header = header;
        return result;
    }

    static DnsEncodeResult PrepareQuestions(
        List<DnsQuestion> source,
        List<DnsQuestion> destination,
        DnsEncodeOptions options)
    {
        bool lowercase = options.Canonical &&
            options.CanonicalOrder.LowercaseNames;
        int index = 0;
        while (index < source.Count)
        {
            DnsQuestion question = source[index];
            if (question == null)
            {
                DnsEncodeResult missing = DnsHeaderCodec.EncodeFailure(
                    DnsDiagnosticCode.ValueOutOfRange,
                    "question",
                    "A DNS question entry is required.",
                    0);
                return AttachSectionLocation(
                    missing, DnsMessageSection.Question, index);
            }

            RawDnsNameResult name = RawDnsName.ValidateName(question.Name);
            if (!name.Success || name.Name == null)
            {
                return FromNameFailure(
                    name, DnsMessageSection.Question, index);
            }

            DnsQuestion copy = new DnsQuestion();
            copy.Name = RecordSetOperations.CopyName(name.Name, lowercase);
            copy.RecordType = question.RecordType;
            copy.RecordTypeValue = question.RecordTypeValue;
            copy.Class = question.Class;
            copy.ClassValue = question.ClassValue;
            destination.Add(copy);
            index = index + 1;
        }

        return SuccessfulEncodingStep();
    }

    static DnsEncodeResult PrepareRecordSection(
        List<DnsResourceRecord> source,
        List<DnsResourceRecord> destination,
        DnsEncodeOptions options,
        DnsMessageSection section,
        bool preserveFinalTsig)
    {
        if (!options.Canonical)
        {
            int copyIndex = 0;
            while (copyIndex < source.Count)
            {
                if (source[copyIndex] == null)
                {
                    DnsEncodeResult missing = DnsHeaderCodec.EncodeFailure(
                        DnsDiagnosticCode.ValueOutOfRange,
                        "resourceRecord",
                        "A resource-record entry is required.",
                        0);
                    return AttachSectionLocation(missing, section, copyIndex);
                }

                destination.Add(source[copyIndex]);
                copyIndex = copyIndex + 1;
            }

            return SuccessfulEncodingStep();
        }

        int orderedCount = source.Count;
        if (preserveFinalTsig)
        {
            orderedCount = orderedCount - 1;
        }

        List<DnsResourceRecord> working = new List<DnsResourceRecord>();
        int index = 0;
        while (index < orderedCount)
        {
            DnsEncodeResult addition = AddCanonicalRecord(
                source[index], working, options, section, index);
            if (!addition.Success)
            {
                return addition;
            }

            index = index + 1;
        }

        ApplyCanonicalSectionPolicy(working, destination, options);
        if (preserveFinalTsig)
        {
            DnsEncodeResult tsig = AddCanonicalRecord(
                source[source.Count - 1],
                destination,
                options,
                section,
                source.Count - 1);
            if (!tsig.Success)
            {
                return tsig;
            }
        }

        return SuccessfulEncodingStep();
    }

    static DnsEncodeResult AddCanonicalRecord(
        DnsResourceRecord source,
        List<DnsResourceRecord> destination,
        DnsEncodeOptions options,
        DnsMessageSection section,
        int recordIndex)
    {
        if (source == null)
        {
            DnsEncodeResult missing = DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "A resource-record entry is required.",
                0);
            return AttachSectionLocation(missing, section, recordIndex);
        }

        RawDnsNameResult name = RawDnsName.ValidateName(source.Name);
        if (!name.Success || name.Name == null)
        {
            return FromNameFailure(name, section, recordIndex);
        }

        DnsResourceRecord copy = new DnsResourceRecord();
        copy.Name = RecordSetOperations.CopyName(
            name.Name, options.CanonicalOrder.LowercaseNames);
        copy.RecordType = source.RecordType;
        copy.RecordTypeValue = source.RecordTypeValue;
        copy.Class = source.Class;
        copy.ClassValue = source.ClassValue;
        copy.TimeToLive = source.TimeToLive;
        copy.Data = source.Data;
        copy.UseRawData = source.UseRawData;
        if (source.UseRawData)
        {
            if (source.RawData == null)
            {
                DnsEncodeResult missingData = DnsHeaderCodec.EncodeFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "resourceRecord.rawData",
                    "Raw resource-record data is required.",
                    0);
                return AttachSectionLocation(
                    missingData, section, recordIndex);
            }

            copy.RawData = CopyBytes(source.RawData);
        }
        else
        {
            DnsRecordData? recordData = source.Data;
            if (recordData == null)
            {
                DnsEncodeResult missingData = DnsHeaderCodec.EncodeFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "resourceRecord.data",
                    "Typed or opaque resource-record data is required.",
                    0);
                return AttachSectionLocation(
                    missingData, section, recordIndex);
            }

            int recordTypeValue = DnsHeaderCodec.ResolveCode(
                source.RecordTypeValue, (int)source.RecordType);
            DnsEncodeResult data = ResourceRecordCodec.EncodeRecordData(
                recordTypeValue, recordData, options);
            if (!data.Success)
            {
                return AttachSectionLocation(data, section, recordIndex);
            }

            copy.RawData = data.Bytes;
        }

        destination.Add(copy);
        return SuccessfulEncodingStep();
    }

    static void ApplyCanonicalSectionPolicy(
        List<DnsResourceRecord> source,
        List<DnsResourceRecord> destination,
        DnsEncodeOptions options)
    {
        DnsCanonicalOrderOptions order = options.CanonicalOrder;
        if (order.SortRecords)
        {
            List<DnsResourceRecordSet> sets =
                RecordSetOperations.GroupRecordSets(source);
            List<DnsResourceRecordSet> normalized =
                new List<DnsResourceRecordSet>();
            int setIndex = 0;
            while (setIndex < sets.Count)
            {
                normalized.Add(RecordSetOperations.NormalizeRecordSet(
                    sets[setIndex], order));
                setIndex = setIndex + 1;
            }

            List<DnsResourceRecordSet> sorted =
                RecordSetOperations.SortRecordSets(normalized);
            setIndex = 0;
            while (setIndex < sorted.Count)
            {
                int recordIndex = 0;
                while (recordIndex < sorted[setIndex].Records.Count)
                {
                    destination.Add(sorted[setIndex].Records[recordIndex]);
                    recordIndex = recordIndex + 1;
                }

                setIndex = setIndex + 1;
            }

            return;
        }

        if (order.NormalizeRecordTtl)
        {
            NormalizeTtlPreservingOrder(source);
        }

        int index = 0;
        while (index < source.Count)
        {
            if (!order.SuppressDuplicateRecords ||
                !ContainsEquivalentRecord(destination, source[index]))
            {
                destination.Add(source[index]);
            }

            index = index + 1;
        }
    }

    static void NormalizeTtlPreservingOrder(
        List<DnsResourceRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            uint minimum = record.TimeToLive;
            int candidateIndex = 0;
            while (candidateIndex < records.Count)
            {
                DnsResourceRecord candidate = records[candidateIndex];
                if (RecordSetKeysEqual(record, candidate) &&
                    candidate.TimeToLive < minimum)
                {
                    minimum = candidate.TimeToLive;
                }

                candidateIndex = candidateIndex + 1;
            }

            record.TimeToLive = minimum;
            index = index + 1;
        }
    }

    static bool ContainsEquivalentRecord(
        List<DnsResourceRecord> records,
        DnsResourceRecord candidate)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (RecordSetKeysEqual(record, candidate) &&
                record.TimeToLive == candidate.TimeToLive &&
                RecordSetOperations.BytesEqual(
                    record.RawData, candidate.RawData))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool RecordSetKeysEqual(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        int leftType = DnsHeaderCodec.ResolveCode(
            left.RecordTypeValue, (int)left.RecordType);
        int rightType = DnsHeaderCodec.ResolveCode(
            right.RecordTypeValue, (int)right.RecordType);
        int leftClass = DnsHeaderCodec.ResolveCode(
            left.ClassValue, (int)left.Class);
        int rightClass = DnsHeaderCodec.ResolveCode(
            right.ClassValue, (int)right.Class);
        return leftType == rightType && leftClass == rightClass &&
            RecordSetOperations.NamesEqual(left.Name, right.Name);
    }

    static DnsEncodeResult WritePreparedMessage(
        WireWriter writer,
        CompressionTable table,
        DnsMessage message,
        byte[] header,
        DnsEncodeOptions options)
    {
        WireWriteResult headerWrite = writer.WriteBytes(header);
        if (!headerWrite.Success)
        {
            return FromWireFailure(headerWrite);
        }

        int index = 0;
        while (index < message.Questions.Count)
        {
            WireWriteResult question = DnsQuestionCodec.WriteQuestion(
                writer, table, message.Questions[index], options);
            if (!question.Success)
            {
                return AttachSectionLocation(
                    FromWireFailure(question),
                    DnsMessageSection.Question,
                    index);
            }

            index = index + 1;
        }

        bool updateMessage = DnsHeaderCodec.ResolveCode(
            message.Header.OpcodeValue,
            (int)message.Header.Opcode) == (int)DnsOpcode.Update;

        DnsEncodeResult answers = WriteRecordSection(
            writer,
            table,
            message.Answers,
            options,
            DnsMessageSection.Answer,
            updateMessage);
        if (!answers.Success)
        {
            return answers;
        }

        DnsEncodeResult authorities = WriteRecordSection(
            writer,
            table,
            message.Authorities,
            options,
            DnsMessageSection.Authority,
            updateMessage);
        if (!authorities.Success)
        {
            return authorities;
        }

        DnsEncodeResult additionals = WriteRecordSection(
            writer,
            table,
            message.Additionals,
            options,
            DnsMessageSection.Additional,
            false);
        if (!additionals.Success)
        {
            return additionals;
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.BytesWritten = writer.WrittenLength();
        return result;
    }

    static DnsEncodeResult WriteRecordSection(
        WireWriter writer,
        CompressionTable table,
        List<DnsResourceRecord> records,
        DnsEncodeOptions options,
        DnsMessageSection section,
        bool allowUpdateControl)
    {
        int index = 0;
        while (index < records.Count)
        {
            WireWriteResult record;
            if (allowUpdateControl)
            {
                record = ResourceRecordCodec.WriteUpdateResourceRecord(
                    writer, table, records[index], options);
            }
            else
            {
                record = ResourceRecordCodec.WriteResourceRecord(
                    writer, table, records[index], options);
            }
            if (!record.Success)
            {
                return AttachSectionLocation(
                    FromWireFailure(record), section, index);
            }

            index = index + 1;
        }

        return SuccessfulEncodingStep();
    }

    static DnsEncodeResult FromNameFailure(
        RawDnsNameResult name,
        DnsMessageSection section,
        int recordIndex)
    {
        if (name.Diagnostics.Count == 0)
        {
            DnsEncodeResult unavailable = DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated DNS name data is unavailable.",
                0);
            return AttachSectionLocation(
                unavailable, section, recordIndex);
        }

        DnsDiagnostic diagnostic = name.Diagnostics[0];
        DnsEncodeResult result = DnsHeaderCodec.EncodeFailure(
            diagnostic.Code,
            diagnostic.Field == null ? "name" : diagnostic.Field,
            diagnostic.Reason,
            0);
        return AttachSectionLocation(result, section, recordIndex);
    }

    static DnsEncodeResult FromWireFailure(WireWriteResult write)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        int index = 0;
        while (index < write.Diagnostics.Count)
        {
            result.Diagnostics.Add(write.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsEncodeResult AttachSectionLocation(
        DnsEncodeResult result,
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

    static DnsEncodeResult FromTsigPlacementFailure(
        TsigPlacementResult placement)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        int index = 0;
        while (index < placement.Diagnostics.Count)
        {
            result.Diagnostics.Add(placement.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }

    static DnsEncodeResult SuccessfulEncodingStep()
    {
        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        return result;
    }

    static DnsEncodeResult FromEdnsPlacementFailure(
        EdnsPlacementResult placement)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        int index = 0;
        while (index < placement.Diagnostics.Count)
        {
            result.Diagnostics.Add(placement.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
