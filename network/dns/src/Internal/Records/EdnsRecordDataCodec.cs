using System;
using System.Collections.Generic;

namespace dns;

internal class EdnsRecordHeaderResult
{
    internal bool Success;
    internal int UdpPayloadSize;
    internal uint TimeToLive;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class EdnsPlacementResult
{
    internal bool Success;
    internal int OptCount;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class EdnsRecordDataCodec
{
    const int KnownFlagMask = 57344;
    const int FlagFieldMask = 65535;

    internal static DnsDecodeResult DecodeRecordData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsRecordData data = new DnsRecordData();
        DnsEdnsData edns = new DnsEdnsData();
        data.Edns = edns;
        int consumed = 0;
        int optionIndex = 0;
        while (consumed < length)
        {
            int optionOffset = offset + consumed;
            if (length - consumed < 4)
            {
                return DecodeOptionFailure(
                    "edns.option.header",
                    "An EDNS option requires a four-octet code and length header.",
                    optionOffset,
                    optionIndex);
            }

            int optionLength = DnsHeaderCodec.ReadUnsigned16(
                message, optionOffset + 2);
            if (optionLength > length - consumed - 4)
            {
                return DecodeOptionFailure(
                    "edns.option.length",
                    "The EDNS option length exceeds the remaining OPT data.",
                    optionOffset + 2,
                    optionIndex);
            }

            int framedLength = 4 + optionLength;
            DnsEdnsOptionResult decoded =
                TypedEdnsOptionCodec.DecodeOption(
                    message.Slice(optionOffset, framedLength), options);
            if (!decoded.Success || decoded.Option == null)
            {
                if (decoded.Diagnostics.Count == 0)
                {
                    return DecodeOptionFailure(
                        "edns.option",
                        "The EDNS option could not be decoded.",
                        optionOffset,
                        optionIndex);
                }

                DnsDiagnostic diagnostic = decoded.Diagnostics[0];
                int diagnosticOffset = optionOffset;
                if (diagnostic.HasOffset)
                    diagnosticOffset = optionOffset + diagnostic.Offset;
                return DecodeOptionFailure(
                    diagnostic.Field == null ?
                        "edns.option" : diagnostic.Field,
                    diagnostic.Reason,
                    diagnosticOffset,
                    optionIndex);
            }

            edns.Options.Add(decoded.Option);
            consumed = consumed + framedLength;
            optionIndex = optionIndex + 1;
        }

        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        result.BytesConsumed = consumed;
        result.RecordData = data;
        return result;
    }

    internal static WireWriteResult EncodeRecordData(
        WireWriter writer,
        DnsRecordData data)
    {
        if (writer == null || data == null || data.Edns == null ||
            data.Edns.Options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.InvalidEdns,
                "edns",
                "Typed OPT data and its option collection are required.",
                0);
        }

        int start = writer.WriteOffset();
        int index = 0;
        while (index < data.Edns.Options.Count)
        {
            DnsEdnsOption option = data.Edns.Options[index];
            if (option == null)
            {
                return AttachOptionIndex(
                    writer.FailAt(
                        DnsDiagnosticCode.InvalidEdns,
                        "edns.option",
                        "Every EDNS option and its data are required.",
                        writer.WriteOffset()),
                    index);
            }

            DnsEdnsOptionResult encoded =
                TypedEdnsOptionCodec.EncodeOption(option);
            if (!encoded.Success)
            {
                if (encoded.Diagnostics.Count == 0)
                {
                    return AttachOptionIndex(
                        writer.FailAt(
                            DnsDiagnosticCode.InvalidEdns,
                            "edns.option",
                            "The EDNS option could not be encoded.",
                            writer.WriteOffset()),
                        index);
                }

                DnsDiagnostic diagnostic = encoded.Diagnostics[0];
                int diagnosticOffset = writer.WriteOffset();
                if (diagnostic.HasOffset)
                    diagnosticOffset = diagnosticOffset + diagnostic.Offset;
                return AttachOptionIndex(
                    writer.FailAt(
                        diagnostic.Code,
                        diagnostic.Field == null ?
                            "edns.option" : diagnostic.Field,
                        diagnostic.Reason,
                        diagnosticOffset),
                    index);
            }

            WireWriteResult write = writer.WriteBytes(encoded.Bytes);
            if (!write.Success) return AttachOptionIndex(write, index);
            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult PopulateDecodedRecord(
        DnsResourceRecord record,
        int offset)
    {
        if (!OwnerIsRoot(record.Name))
        {
            return DecodeFailure(
                "edns.owner",
                "An OPT record owner must be the DNS root name.",
                offset);
        }

        if (record.Data == null || record.Data.Edns == null)
        {
            return DecodeFailure(
                "edns.data",
                "Decoded OPT data is unavailable.",
                offset);
        }

        DnsEdnsData edns = record.Data.Edns;
        int flags = (int)(record.TimeToLive & FlagFieldMask);
        edns.UdpPayloadSize = record.ClassValue;
        edns.ExtendedResponseCode = (int)(record.TimeToLive >> 24);
        edns.Version = (int)((record.TimeToLive >> 16) & 255);
        edns.Flags = (DnsEdnsFlag)(flags & KnownFlagMask);
        edns.FlagsValue = flags;
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    internal static EdnsRecordHeaderResult ReadEncodedHeader(
        DnsResourceRecord record)
    {
        EdnsRecordHeaderResult result = new EdnsRecordHeaderResult();
        if (!OwnerIsRoot(record.Name))
        {
            AddHeaderDiagnostic(
                result,
                "edns.owner",
                "An OPT record owner must be the DNS root name.");
            return result;
        }

        if (record.UseRawData)
        {
            result.UdpPayloadSize = DnsHeaderCodec.ResolveCode(
                record.ClassValue, (int)record.Class);
            result.TimeToLive = record.TimeToLive;
            if (result.UdpPayloadSize < 0 ||
                result.UdpPayloadSize > DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
            {
                AddHeaderDiagnostic(
                    result,
                    "edns.udpPayloadSize",
                    "The EDNS UDP payload size must fit in sixteen bits.");
                return result;
            }

            result.Success = true;
            return result;
        }

        if (record.Data == null || record.Data.Edns == null)
        {
            AddHeaderDiagnostic(
                result,
                "edns.data",
                "Typed OPT data is required when raw-data mode is disabled.");
            return result;
        }

        DnsEdnsData edns = record.Data.Edns;
        int flags = (int)edns.Flags;
        if (edns.UdpPayloadSize < 0 ||
            edns.UdpPayloadSize > DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            AddHeaderDiagnostic(
                result,
                "edns.udpPayloadSize",
                "The EDNS UDP payload size must fit in sixteen bits.");
            return result;
        }

        if (edns.ExtendedResponseCode < 0 ||
            edns.ExtendedResponseCode > 255)
        {
            AddHeaderDiagnostic(
                result,
                "edns.extendedResponseCode",
                "The EDNS extended response code must fit in eight bits.");
            return result;
        }

        if (edns.Version < 0 || edns.Version > 255)
        {
            AddHeaderDiagnostic(
                result,
                "edns.version",
                "The EDNS version must fit in eight bits.");
            return result;
        }

        if (edns.FlagsValue < 0 || edns.FlagsValue > FlagFieldMask ||
            flags < 0 || (flags & ~KnownFlagMask) != 0)
        {
            AddHeaderDiagnostic(
                result,
                "edns.flags",
                "EDNS raw flags must fit in sixteen bits and typed flags must be recognized.");
            return result;
        }

        int effectiveFlags = ReadEffectiveFlags(edns);
        result.UdpPayloadSize = edns.UdpPayloadSize;
        result.TimeToLive = ((uint)edns.ExtendedResponseCode << 24) |
            ((uint)edns.Version << 16) | (uint)effectiveFlags;
        result.Success = true;
        return result;
    }

    internal static int ReadEffectiveFlags(DnsEdnsData edns)
    {
        int recognizedFlags = (int)edns.Flags;
        if (recognizedFlags == 0)
            recognizedFlags = edns.FlagsValue & KnownFlagMask;
        return (edns.FlagsValue & ~KnownFlagMask) | recognizedFlags;
    }

    internal static EdnsPlacementResult InspectPlacement(DnsMessage message)
    {
        EdnsPlacementResult result = new EdnsPlacementResult();
        InspectSection(
            result, message.Answers, DnsMessageSection.Answer, false);
        InspectSection(
            result, message.Authorities, DnsMessageSection.Authority, false);
        InspectSection(
            result, message.Additionals, DnsMessageSection.Additional, true);
        InspectClientSubnetScopes(result, message);
        InspectTypedOptionRules(result, message);
        result.Success = result.Diagnostics.Count == 0;
        return result;
    }

    static void InspectTypedOptionRules(
        EdnsPlacementResult result,
        DnsMessage message)
    {
        int flags = MessageOperations.EffectiveFlags(message.Header);
        bool response = (flags & (int)DnsHeaderFlag.Response) != 0;
        int recordIndex = 0;
        while (recordIndex < message.Additionals.Count)
        {
            DnsResourceRecord record = message.Additionals[recordIndex];
            if (record != null && !record.UseRawData &&
                DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue, (int)record.RecordType) == 41 &&
                record.Data != null && record.Data.Edns != null &&
                record.Data.Edns.Options != null)
            {
                InspectOptionCollection(
                    result,
                    message,
                    record.Data.Edns.Options,
                    recordIndex,
                    response);
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void InspectOptionCollection(
        EdnsPlacementResult result,
        DnsMessage message,
        List<DnsEdnsOption> options,
        int recordIndex,
        bool response)
    {
        int reportChannelCount = 0;
        int zoneVersionCount = 0;
        int paddingCount = 0;
        int optionIndex = 0;
        while (optionIndex < options.Count)
        {
            DnsEdnsOption option = options[optionIndex];
            if (option != null && !option.UseRawData)
            {
                int code = DnsHeaderCodec.ResolveCode(
                    option.CodeValue, (int)option.Code);
                if (code == 15 && !response)
                {
                    AddOptionDiagnostic(
                        result,
                        recordIndex,
                        optionIndex,
                        "ednsOption.extendedError",
                        "An Extended DNS Error option may occur only in a response.");
                }

                if (code == 18)
                {
                    reportChannelCount = reportChannelCount + 1;
                    InspectReportChannelRule(
                        result,
                        recordIndex,
                        optionIndex,
                        response,
                        reportChannelCount);
                }

                if (code == 19)
                {
                    zoneVersionCount = zoneVersionCount + 1;
                    InspectZoneVersionRule(
                        result,
                        message,
                        options,
                        option,
                        optionIndex,
                        recordIndex,
                        response,
                        zoneVersionCount);
                }

                if (code == 12)
                {
                    paddingCount = paddingCount + 1;
                    if (paddingCount > 1)
                    {
                        AddOptionDiagnostic(
                            result,
                            recordIndex,
                            optionIndex,
                            "ednsOption.padding",
                            "A DNS message may contain at most one Padding option.");
                    }
                }
            }

            optionIndex = optionIndex + 1;
        }
    }

    static void InspectReportChannelRule(
        EdnsPlacementResult result,
        int recordIndex,
        int optionIndex,
        bool response,
        int count)
    {
        if (!response)
        {
            AddOptionDiagnostic(
                result,
                recordIndex,
                optionIndex,
                "ednsOption.reportChannel",
                "A Report Channel option may occur only in a response.");
        }

        if (count > 1)
        {
            AddOptionDiagnostic(
                result,
                recordIndex,
                optionIndex,
                "ednsOption.reportChannel",
                "A response may contain at most one Report Channel option.");
        }
    }

    static void InspectZoneVersionRule(
        EdnsPlacementResult result,
        DnsMessage message,
        List<DnsEdnsOption> options,
        DnsEdnsOption option,
        int optionIndex,
        int recordIndex,
        bool response,
        int count)
    {
        DnsEdnsZoneVersion? version = option.ZoneVersion;
        if (!response)
        {
            if (count > 1)
            {
                AddOptionDiagnostic(
                    result,
                    recordIndex,
                    optionIndex,
                    "ednsOption.zoneVersion",
                    "A request may contain at most one Zone Version option.");
            }

            if (version != null && version.HasVersion)
            {
                AddOptionDiagnostic(
                    result,
                    recordIndex,
                    optionIndex,
                    "ednsOption.zoneVersion.length",
                    "A Zone Version option in a request must be empty.");
            }

            return;
        }

        if (version == null || !version.HasVersion)
        {
            AddOptionDiagnostic(
                result,
                recordIndex,
                optionIndex,
                "ednsOption.zoneVersion.length",
                "A Zone Version option in a response must contain version data.");
            return;
        }

        int questionLabels = 0;
        if (message.Questions.Count > 0 && message.Questions[0] != null &&
            message.Questions[0].Name != null)
        {
            questionLabels = message.Questions[0].Name.Labels.Count;
        }

        if (version.LabelCount > questionLabels)
        {
            AddOptionDiagnostic(
                result,
                recordIndex,
                optionIndex,
                "ednsOption.zoneVersion.labelCount",
                "The Zone Version label count exceeds the original query name.");
        }

        int priorIndex = 0;
        while (priorIndex < optionIndex)
        {
            DnsEdnsOption prior = options[priorIndex];
            if (prior != null && !prior.UseRawData &&
                DnsHeaderCodec.ResolveCode(
                    prior.CodeValue, (int)prior.Code) == 19 &&
                prior.ZoneVersion != null && prior.ZoneVersion.HasVersion &&
                prior.ZoneVersion.LabelCount == version.LabelCount &&
                prior.ZoneVersion.Type == version.Type)
            {
                AddOptionDiagnostic(
                    result,
                    recordIndex,
                    optionIndex,
                    "ednsOption.zoneVersion",
                    "A response cannot repeat a Zone Version type and label count.");
                return;
            }

            priorIndex = priorIndex + 1;
        }
    }

    static void InspectClientSubnetScopes(
        EdnsPlacementResult result,
        DnsMessage message)
    {
        int flags = MessageOperations.EffectiveFlags(message.Header);
        if ((flags & (int)DnsHeaderFlag.Response) != 0)
            return;

        int recordIndex = 0;
        while (recordIndex < message.Additionals.Count)
        {
            DnsResourceRecord record = message.Additionals[recordIndex];
            if (record != null && !record.UseRawData &&
                DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue, (int)record.RecordType) == 41 &&
                record.Data != null &&
                record.Data.Edns != null &&
                record.Data.Edns.Options != null)
            {
                List<DnsEdnsOption> options = record.Data.Edns.Options;
                int optionIndex = 0;
                while (optionIndex < options.Count)
                {
                    DnsEdnsOption option = options[optionIndex];
                    if (option != null && !option.UseRawData &&
                        DnsHeaderCodec.ResolveCode(
                            option.CodeValue, (int)option.Code) == 8 &&
                        option.ClientSubnet != null &&
                        option.ClientSubnet.ScopePrefixLength != 0)
                    {
                        AddClientSubnetScopeDiagnostic(
                            result, recordIndex, optionIndex);
                    }

                    optionIndex = optionIndex + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void InspectSection(
        EdnsPlacementResult result,
        List<DnsResourceRecord> records,
        DnsMessageSection section,
        bool additional)
    {
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (record != null)
            {
                int typeValue = DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue, (int)record.RecordType);
                if (typeValue == 41)
                {
                    result.OptCount = result.OptCount + 1;
                    if (!additional)
                    {
                        AddPlacementDiagnostic(
                            result,
                            section,
                            index,
                            "An OPT record may occur only in the Additional section.");
                    }

                    if (result.OptCount > 1)
                    {
                        AddPlacementDiagnostic(
                            result,
                            section,
                            index,
                            "A DNS message may contain at most one OPT record.");
                    }
                }
            }

            index = index + 1;
        }
    }

    static bool OwnerIsRoot(DnsName name)
    {
        if (name == null)
            return false;
        RawDnsNameResult validation = RawDnsName.ValidateName(name);
        return validation.Success && validation.Name != null &&
            validation.Name.IsAbsolute && validation.Name.Labels.Count == 0;
    }

    static DnsDecodeResult DecodeFailure(
        string field,
        string reason,
        int offset)
    {
        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidEdns, field, reason, offset);
    }

    static DnsDecodeResult DecodeOptionFailure(
        string field,
        string reason,
        int offset,
        int optionIndex)
    {
        DnsDecodeResult result = DecodeFailure(field, reason, offset);
        if (result.Diagnostics.Count > 0)
        {
            result.Diagnostics[0].HasOptionIndex = true;
            result.Diagnostics[0].OptionIndex = optionIndex;
        }

        return result;
    }

    static WireWriteResult AttachOptionIndex(
        WireWriteResult result,
        int optionIndex)
    {
        int diagnosticIndex = 0;
        while (diagnosticIndex < result.Diagnostics.Count)
        {
            result.Diagnostics[diagnosticIndex].HasOptionIndex = true;
            result.Diagnostics[diagnosticIndex].OptionIndex = optionIndex;
            diagnosticIndex = diagnosticIndex + 1;
        }

        return result;
    }

    static void AddHeaderDiagnostic(
        EdnsRecordHeaderResult result,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidEdns;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void AddPlacementDiagnostic(
        EdnsPlacementResult result,
        DnsMessageSection section,
        int recordIndex,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidEdns;
        diagnostic.HasSection = true;
        diagnostic.Section = section;
        diagnostic.HasRecordIndex = true;
        diagnostic.RecordIndex = recordIndex;
        diagnostic.Field = "resourceRecord.type";
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void AddClientSubnetScopeDiagnostic(
        EdnsPlacementResult result,
        int recordIndex,
        int optionIndex)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidEdns;
        diagnostic.HasSection = true;
        diagnostic.Section = DnsMessageSection.Additional;
        diagnostic.HasRecordIndex = true;
        diagnostic.RecordIndex = recordIndex;
        diagnostic.HasOptionIndex = true;
        diagnostic.OptionIndex = optionIndex;
        diagnostic.Field = "ednsOption.clientSubnet.scopePrefixLength";
        diagnostic.Reason =
            "A Client Subnet option in a DNS request must have a zero scope prefix.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddOptionDiagnostic(
        EdnsPlacementResult result,
        int recordIndex,
        int optionIndex,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidEdns;
        diagnostic.HasSection = true;
        diagnostic.Section = DnsMessageSection.Additional;
        diagnostic.HasRecordIndex = true;
        diagnostic.RecordIndex = recordIndex;
        diagnostic.HasOptionIndex = true;
        diagnostic.OptionIndex = optionIndex;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }
}
