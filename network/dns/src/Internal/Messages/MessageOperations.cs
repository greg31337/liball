using System.Collections.Generic;
using System.Text.Json;

namespace dns;

internal static class MessageOperations
{
    internal static DnsMessage CloneMessage(DnsMessage message)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
        serializerOptions.IncludeFields = true;
        string serialized = JsonSerializer.Serialize(message, serializerOptions);
        DnsMessage? result = JsonSerializer.Deserialize<DnsMessage>(
            serialized,
            serializerOptions);
        if (result == null)
        {
            throw new System.InvalidOperationException("The DNS message could not be cloned.");
        }

        return result;
    }

    internal static bool MessagesEqual(
        DnsMessage left,
        DnsMessage right,
        DnsCanonicalOrderOptions options)
    {
        if (left == null || right == null || options == null)
        {
            throw new System.ArgumentNullException("message");
        }

        if (left.Header == null || right.Header == null ||
            left.Questions == null || right.Questions == null ||
            left.Answers == null || right.Answers == null ||
            left.Authorities == null || right.Authorities == null ||
            left.Additionals == null || right.Additionals == null)
        {
            throw new System.ArgumentException("A complete DNS message is required.");
        }

        if (!HeadersEqual(left, right) ||
            !QuestionsEqual(left.Questions, right.Questions) ||
            !SectionsEqual(left.Answers, right.Answers, options) ||
            !SectionsEqual(left.Authorities, right.Authorities, options) ||
            !SectionsEqual(left.Additionals, right.Additionals, options))
        {
            return false;
        }

        return true;
    }

    internal static int EstimateMessageSize(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        if (message == null || options == null)
        {
            throw new System.ArgumentNullException("message");
        }

        if (message.Header == null || message.Questions == null ||
            message.Answers == null || message.Authorities == null ||
            message.Additionals == null)
        {
            throw new System.ArgumentException("A complete DNS message is required.");
        }

        DnsEncodeOptions measurementOptions = CopyEncodeOptions(options);
        measurementOptions.MaximumMessageLength = DnsProtocolLimits.MaximumTcpMessageLength;
        WireWriter writer = new WireWriter(
            null,
            0,
            DnsProtocolLimits.MaximumTcpMessageLength);
        CompressionTable table = new CompressionTable(0);
        RequireHeaderSpace(writer.WriteBytes(new byte[DnsProtocolLimits.HeaderLength]));

        int index = 0;
        while (index < message.Questions.Count)
        {
            RequireQuestionWrite(DnsQuestionCodec.WriteQuestion(
                writer,
                table,
                message.Questions[index],
                measurementOptions));
            index = index + 1;
        }

        WriteRecords(writer, table, message.Answers, measurementOptions);
        WriteRecords(writer, table, message.Authorities, measurementOptions);
        WriteRecords(writer, table, message.Additionals, measurementOptions);
        return writer.WrittenLength();
    }

    internal static int ReadEffectiveResponseCode(DnsMessage message)
    {
        if (message == null || message.Header == null || message.Additionals == null)
        {
            throw new System.ArgumentNullException("message");
        }

        int baseCode = DnsHeaderCodec.ResolveCode(
            message.Header.ResponseCodeValue,
            (int)message.Header.ResponseCode);
        if (baseCode < 0 || baseCode > 15)
        {
            throw new System.ArgumentException(
                "The base DNS response code must fit in four bits.");
        }

        DnsResourceRecord? opt = FindOptRecord(message);
        int extendedCode = 0;
        if (opt != null && opt.Data != null && opt.Data.Edns != null)
        {
            extendedCode = opt.Data.Edns.ExtendedResponseCode;
        }
        else if (opt != null)
        {
            extendedCode = (int)(opt.TimeToLive >> 24);
        }

        if (extendedCode < 0 || extendedCode > 255)
        {
            throw new System.ArgumentException(
                "The extended DNS response code must fit in eight bits.");
        }

        return (extendedCode << 4) | baseCode;
    }

    internal static void WriteEffectiveResponseCode(
        DnsMessage message,
        int responseCodeValue)
    {
        if (message == null || message.Header == null || message.Additionals == null)
        {
            throw new System.ArgumentNullException("message");
        }

        if (responseCodeValue < 0 || responseCodeValue > 4095)
        {
            throw new System.ArgumentOutOfRangeException("responseCodeValue");
        }

        int baseCode = responseCodeValue & 15;
        int extendedCode = responseCodeValue >> 4;
        message.Header.ResponseCode = (DnsResponseCode)baseCode;
        message.Header.ResponseCodeValue = baseCode;
        message.Header.FlagsValue = (message.Header.FlagsValue & ~15) | baseCode;
        DnsResourceRecord? opt = FindOptRecord(message);
        if (opt == null && extendedCode != 0)
        {
            opt = CreateOptRecord();
            message.Additionals.Add(opt);
        }

        if (opt != null)
        {
            if (opt.Data == null)
            {
                opt.Data = new DnsRecordData();
            }

            if (opt.Data.Edns == null)
            {
                opt.Data.Edns = new DnsEdnsData();
                opt.Data.Edns.UdpPayloadSize = DnsHeaderCodec.ResolveCode(
                    opt.ClassValue,
                    (int)opt.Class);
            }

            opt.Data.Edns.ExtendedResponseCode = extendedCode;
            opt.TimeToLive = (opt.TimeToLive & 16777215) |
                ((uint)extendedCode << 24);
        }
    }

    internal static bool HeadersEqual(DnsMessage left, DnsMessage right)
    {
        DnsMessageHeader leftHeader = left.Header;
        DnsMessageHeader rightHeader = right.Header;
        int leftOpcode = DnsHeaderCodec.ResolveCode(
            leftHeader.OpcodeValue,
            (int)leftHeader.Opcode);
        int rightOpcode = DnsHeaderCodec.ResolveCode(
            rightHeader.OpcodeValue,
            (int)rightHeader.Opcode);
        return leftHeader.Identifier == rightHeader.Identifier &&
            leftOpcode == rightOpcode &&
            ReadEffectiveResponseCode(left) == ReadEffectiveResponseCode(right) &&
            EffectiveFlags(leftHeader) == EffectiveFlags(rightHeader);
    }

    internal static int EffectiveFlags(DnsMessageHeader header)
    {
        int flags = (int)header.Flags;
        int rawFlags = header.FlagsValue & DnsHeaderCodec.HeaderFlagMask;
        if (flags == 0 && rawFlags != 0)
        {
            flags = rawFlags;
        }

        return flags;
    }

    internal static bool QuestionsEqual(
        List<DnsQuestion> left,
        List<DnsQuestion> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            DnsQuestion leftQuestion = left[index];
            DnsQuestion rightQuestion = right[index];
            if (leftQuestion == null || rightQuestion == null ||
                !RecordSetOperations.NamesEqual(
                    leftQuestion.Name,
                    rightQuestion.Name) ||
                DnsHeaderCodec.ResolveCode(
                    leftQuestion.RecordTypeValue,
                    (int)leftQuestion.RecordType) !=
                DnsHeaderCodec.ResolveCode(
                    rightQuestion.RecordTypeValue,
                    (int)rightQuestion.RecordType) ||
                DnsHeaderCodec.ResolveCode(
                    leftQuestion.ClassValue,
                    (int)leftQuestion.Class) !=
                DnsHeaderCodec.ResolveCode(
                    rightQuestion.ClassValue,
                    (int)rightQuestion.Class))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool SectionsEqual(
        List<DnsResourceRecord> left,
        List<DnsResourceRecord> right,
        DnsCanonicalOrderOptions options)
    {
        List<DnsResourceRecordSet> leftSets = RecordSetOperations.SortRecordSets(
            NormalizeSets(RecordSetOperations.GroupRecordSets(left), options));
        List<DnsResourceRecordSet> rightSets = RecordSetOperations.SortRecordSets(
            NormalizeSets(RecordSetOperations.GroupRecordSets(right), options));
        if (leftSets.Count != rightSets.Count)
        {
            return false;
        }

        int index = 0;
        while (index < leftSets.Count)
        {
            if (!RecordSetOperations.RecordSetsEqual(
                leftSets[index],
                rightSets[index],
                options))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static List<DnsResourceRecordSet> NormalizeSets(
        List<DnsResourceRecordSet> sets,
        DnsCanonicalOrderOptions options)
    {
        List<DnsResourceRecordSet> result = new List<DnsResourceRecordSet>();
        int index = 0;
        while (index < sets.Count)
        {
            result.Add(RecordSetOperations.NormalizeRecordSet(sets[index], options));
            index = index + 1;
        }

        return result;
    }

    internal static DnsResourceRecord? FindOptRecord(DnsMessage message)
    {
        DnsResourceRecord? result = null;
        int optCount = 0;
        int index = 0;
        while (index < message.Additionals.Count)
        {
            DnsResourceRecord record = message.Additionals[index];
            if (record == null)
            {
                throw new System.ArgumentException("An additional record cannot be null.");
            }

            int typeValue = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == 41)
            {
                optCount = optCount + 1;
                result = record;
            }

            index = index + 1;
        }

        if (optCount > 1)
        {
            throw new System.ArgumentException(
                "A DNS message cannot contain more than one OPT record.");
        }

        return result;
    }

    internal static DnsResourceRecord CreateOptRecord()
    {
        DnsEdnsData edns = new DnsEdnsData();
        DnsRecordData data = new DnsRecordData();
        data.Edns = edns;
        DnsName root = new DnsName();
        root.IsAbsolute = true;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = root;
        record.RecordType = DnsRecordType.Opt;
        record.RecordTypeValue = 41;
        record.Class = (DnsClass)edns.UdpPayloadSize;
        record.ClassValue = edns.UdpPayloadSize;
        record.Data = data;
        return record;
    }

    internal static void WriteRecords(
        WireWriter writer,
        CompressionTable table,
        List<DnsResourceRecord> records,
        DnsEncodeOptions options)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (record == null || record.RawData == null)
            {
                throw new System.ArgumentException("A complete resource record is required.");
            }

            int typeValue = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue,
                (int)record.RecordType);
            int classValue = DnsHeaderCodec.ResolveCode(
                record.ClassValue,
                (int)record.Class);
            if (typeValue < 0 || typeValue > 65535 ||
                classValue < 0 || classValue > 65535 ||
                record.RawData.Length > 65535)
            {
                throw new System.ArgumentException(
                    "Resource-record type, class, and data lengths must fit in sixteen bits.");
            }

            RequireRecordWrite(table.WriteName(writer, record.Name, options, true));
            RequireRecordWrite(writer.WriteUnsigned16((ulong)typeValue));
            RequireRecordWrite(writer.WriteUnsigned16((ulong)classValue));
            RequireRecordWrite(writer.WriteUnsigned32(record.TimeToLive));
            RequireRecordWrite(writer.WriteUnsigned16((ulong)record.RawData.Length));
            RequireRecordWrite(writer.WriteBytes(record.RawData));
            index = index + 1;
        }
    }

    internal static DnsEncodeOptions CopyEncodeOptions(DnsEncodeOptions source)
    {
        DnsEncodeOptions result = new DnsEncodeOptions();
        result.EnableCompression = source.EnableCompression;
        result.Canonical = source.Canonical;
        result.DeriveSectionCounts = source.DeriveSectionCounts;
        result.TruncateToMaximumLength = source.TruncateToMaximumLength;
        result.PreserveQuestionOnTruncation = source.PreserveQuestionOnTruncation;
        result.MaximumMessageLength = source.MaximumMessageLength;
        result.MaximumUdpPayloadSize = source.MaximumUdpPayloadSize;
        result.CanonicalOrder = RecordSetOperations.CopyCanonicalOptions(
            source.CanonicalOrder);
        return result;
    }

    internal static void RequireHeaderSpace(WireWriteResult result)
    {
        if (!result.Success)
        {
            throw new System.InvalidOperationException(
                "The DNS header exceeds the maximum message size.");
        }
    }

    internal static void RequireQuestionWrite(WireWriteResult result)
    {
        if (!result.Success)
        {
            throw new System.ArgumentException(ReadFailureReason(result));
        }
    }

    internal static void RequireRecordWrite(WireWriteResult result)
    {
        if (!result.Success)
        {
            throw new System.ArgumentException(ReadFailureReason(result));
        }
    }

    internal static string ReadFailureReason(WireWriteResult result)
    {
        if (result.Diagnostics.Count > 0)
        {
            return result.Diagnostics[0].Reason;
        }

        return "The DNS message cannot be encoded.";
    }
}
