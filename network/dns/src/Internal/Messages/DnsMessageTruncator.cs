using System.Collections.Generic;

namespace dns;

internal static class DnsMessageTruncator
{
    internal static bool TruncateRecordCount(
        DnsMessage message,
        int maximumRecordCount)
    {
        int recordCount = message.Answers.Count +
            message.Authorities.Count + message.Additionals.Count;
        bool truncated = false;
        while (recordCount > maximumRecordCount &&
            RemoveLastRecordSet(message))
        {
            truncated = true;
            recordCount = message.Answers.Count +
                message.Authorities.Count + message.Additionals.Count;
        }

        if (truncated)
        {
            SetTruncatedFlag(message.Header);
            message.Header.AnswerCount = message.Answers.Count;
            message.Header.AuthorityCount = message.Authorities.Count;
            message.Header.AdditionalCount = message.Additionals.Count;
        }

        return truncated;
    }

    internal static DnsEncodeResult EncodeTruncatedMessage(
        DnsMessage message,
        DnsEncodeOptions options)
    {
        if (options.MaximumUdpPayloadSize < 0 ||
            options.MaximumUdpPayloadSize >
                DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumUdpPayloadSize",
                "The UDP payload limit must fit the EDNS payload-size field.",
                0);
        }

        int outputLimit = options.MaximumMessageLength;
        if (options.MaximumUdpPayloadSize < outputLimit)
        {
            outputLimit = options.MaximumUdpPayloadSize;
        }

        DnsEncodeOptions measurementOptions = CopyTruncationOptions(options);
        measurementOptions.TruncateToMaximumLength = false;
        measurementOptions.MaximumMessageLength =
            DnsProtocolLimits.MaximumTcpMessageLength;
        DnsEncodeResult full = DnsMessageEncoder.EncodeMessage(
            message, measurementOptions);
        if (!full.Success)
        {
            return full;
        }

        if (full.BytesWritten <= outputLimit)
        {
            return full;
        }

        DnsMessage? preparedMessage = full.Message;
        if (preparedMessage == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "message",
                "The measured DNS message representation is unavailable.",
                0);
        }

        SetTruncatedFlag(preparedMessage.Header);
        DnsEncodeOptions boundedOptions = CopyTruncationOptions(options);
        boundedOptions.TruncateToMaximumLength = false;
        boundedOptions.DeriveSectionCounts = true;
        boundedOptions.MaximumMessageLength = outputLimit;
        DnsEncodeResult lastFailure = DnsMessageEncoder.EncodeMessage(
            preparedMessage, boundedOptions);
        if (lastFailure.Success)
        {
            lastFailure.Truncated = true;
            return lastFailure;
        }

        while (!lastFailure.Success && RemoveLastRecordSet(preparedMessage))
        {
            DnsEncodeResult attempt = DnsMessageEncoder.EncodeMessage(
                preparedMessage, boundedOptions);
            if (attempt.Success)
            {
                attempt.Truncated = true;
                return attempt;
            }

            lastFailure = attempt;
        }

        if (!options.PreserveQuestionOnTruncation)
        {
            while (!lastFailure.Success &&
                preparedMessage.Questions.Count > 0)
            {
                preparedMessage.Questions.RemoveAt(
                    preparedMessage.Questions.Count - 1);
                DnsEncodeResult attempt = DnsMessageEncoder.EncodeMessage(
                    preparedMessage, boundedOptions);
                if (attempt.Success)
                {
                    attempt.Truncated = true;
                    return attempt;
                }

                lastFailure = attempt;
            }
        }

        return lastFailure;
    }

    static void SetTruncatedFlag(DnsMessageHeader header)
    {
        int flags = MessageOperations.EffectiveFlags(header) |
            (int)DnsHeaderFlag.Truncated;
        header.Flags = (DnsHeaderFlag)flags;
        header.FlagsValue = (header.FlagsValue &
            ~DnsHeaderCodec.HeaderFlagMask) | flags;
    }

    static bool RemoveLastRecordSet(DnsMessage message)
    {
        int additionalIndex = FindLastNonOptRecord(message.Additionals);
        if (additionalIndex >= 0)
        {
            RemoveSetAtIndex(message.Additionals, additionalIndex);
            return true;
        }

        if (message.Authorities.Count > 0)
        {
            RemoveLastSetFromSection(message.Authorities);
            return true;
        }

        if (message.Answers.Count > 0)
        {
            RemoveLastSetFromSection(message.Answers);
            return true;
        }

        return false;
    }

    static int FindLastNonOptRecord(List<DnsResourceRecord> records)
    {
        int index = records.Count - 1;
        while (index >= 0)
        {
            int typeValue = DnsHeaderCodec.ResolveCode(
                records[index].RecordTypeValue,
                (int)records[index].RecordType);
            if (typeValue != (int)DnsRecordType.Opt)
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    static void RemoveSetAtIndex(
        List<DnsResourceRecord> records,
        int selectedIndex)
    {
        DnsResourceRecord selected = records[selectedIndex];
        int index = records.Count - 1;
        while (index >= 0)
        {
            if (RecordsShareSet(records[index], selected))
            {
                records.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static void RemoveLastSetFromSection(
        List<DnsResourceRecord> records)
    {
        DnsResourceRecord last = records[records.Count - 1];
        int index = records.Count - 1;
        while (index >= 0)
        {
            if (RecordsShareSet(records[index], last))
            {
                records.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static bool RecordsShareSet(
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

    static DnsEncodeOptions CopyTruncationOptions(
        DnsEncodeOptions source)
    {
        DnsEncodeOptions result = new DnsEncodeOptions();
        result.EnableCompression = source.EnableCompression;
        result.Canonical = source.Canonical;
        result.DeriveSectionCounts = source.DeriveSectionCounts;
        result.TruncateToMaximumLength = source.TruncateToMaximumLength;
        result.PreserveQuestionOnTruncation =
            source.PreserveQuestionOnTruncation;
        result.MaximumMessageLength = source.MaximumMessageLength;
        result.MaximumUdpPayloadSize = source.MaximumUdpPayloadSize;
        result.CanonicalOrder = source.CanonicalOrder;
        return result;
    }
}
