using System.Collections.Generic;

namespace dns;

internal class DnsUpdateMessageResult
{
    internal bool Success;
    internal DnsMessage? Message;
    internal DnsUpdateRequest? Request;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class UpdateMessageOperations
{
    internal static DnsUpdateMessageResult BuildUpdateMessage(
        DnsUpdateRequest request,
        ushort identifier)
    {
        DnsUpdateMessageResult result = new DnsUpdateMessageResult();
        if (request == null || request.Prerequisites == null ||
            request.Changes == null || request.AdditionalRecords == null)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A complete DNS update request is required.");
        }

        int zoneClass = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        if (!ZoneClassValid(zoneClass) ||
            !AbsoluteNameValid(request.Zone))
        {
            return Failure(
                DnsDiagnosticCode.InvalidZone,
                "request.zone",
                "An UPDATE requires an absolute zone and ordinary DNS class.");
        }

        DnsMessage message = new DnsMessage();
        message.Header.Identifier = identifier;
        message.Header.Opcode = DnsOpcode.Update;
        message.Header.OpcodeValue = (int)DnsOpcode.Update;
        DnsQuestion zoneQuestion = new DnsQuestion();
        zoneQuestion.Name = RecordSetOperations.CopyName(
            request.Zone,
            false);
        zoneQuestion.RecordType = DnsRecordType.Soa;
        zoneQuestion.RecordTypeValue = (int)DnsRecordType.Soa;
        zoneQuestion.Class = (DnsClass)zoneClass;
        zoneQuestion.ClassValue = zoneClass;
        message.Questions.Add(zoneQuestion);

        int index = 0;
        while (index < request.Prerequisites.Count)
        {
            DnsUpdateMessageResult prerequisite = AddPrerequisite(
                request.Prerequisites[index],
                request.Zone,
                zoneClass,
                message.Answers,
                index);
            if (!prerequisite.Success)
            {
                return prerequisite;
            }

            index = index + 1;
        }

        index = 0;
        while (index < request.Changes.Count)
        {
            DnsUpdateMessageResult change = AddChange(
                request.Changes[index],
                request.Zone,
                zoneClass,
                message.Authorities,
                index);
            if (!change.Success)
            {
                return change;
            }

            index = index + 1;
        }

        index = 0;
        while (index < request.AdditionalRecords.Count)
        {
            DnsResourceRecord record = request.AdditionalRecords[index];
            if (record == null)
            {
                return Failure(
                    DnsDiagnosticCode.ValueOutOfRange,
                    "request.additionalRecords",
                    "An UPDATE additional record cannot be null.");
            }

            message.Additionals.Add(
                DnsZoneSnapshotOperations.CopyRecord(record));
            index = index + 1;
        }

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message,
            new DnsEncodeOptions());
        if (!encoded.Success || encoded.Message == null)
        {
            return FromEncoding(encoded);
        }

        result.Success = true;
        result.Message = encoded.Message;
        return result;
    }

    internal static DnsUpdateMessageResult ParseUpdateMessage(
        DnsMessage message)
    {
        if (message == null || message.Header == null ||
            message.Questions == null || message.Answers == null ||
            message.Authorities == null || message.Additionals == null)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message",
                "A complete DNS UPDATE message is required.");
        }

        int opcode = DnsHeaderCodec.ResolveCode(
            message.Header.OpcodeValue,
            (int)message.Header.Opcode);
        int responseCode = DnsHeaderCodec.ResolveCode(
            message.Header.ResponseCodeValue,
            (int)message.Header.ResponseCode);
        int flags = MessageOperations.EffectiveFlags(message.Header);
        if (opcode != (int)DnsOpcode.Update || flags != 0 ||
            responseCode != 0)
        {
            return Failure(
                DnsDiagnosticCode.ResponseMismatch,
                "message.header",
                "An UPDATE request requires opcode UPDATE and zero request flags and response code.");
        }

        if (message.Header.QuestionCount != message.Questions.Count ||
            message.Header.AnswerCount != message.Answers.Count ||
            message.Header.AuthorityCount != message.Authorities.Count ||
            message.Header.AdditionalCount != message.Additionals.Count ||
            message.Questions.Count != 1)
        {
            return Failure(
                DnsDiagnosticCode.SectionCountMismatch,
                "message.sections",
                "An UPDATE requires one zone question and consistent section counts.");
        }

        DnsQuestion zoneQuestion = message.Questions[0];
        int zoneType = DnsHeaderCodec.ResolveCode(
            zoneQuestion.RecordTypeValue,
            (int)zoneQuestion.RecordType);
        int zoneClass = DnsHeaderCodec.ResolveCode(
            zoneQuestion.ClassValue,
            (int)zoneQuestion.Class);
        if (zoneType != (int)DnsRecordType.Soa ||
            !ZoneClassValid(zoneClass) ||
            !AbsoluteNameValid(zoneQuestion.Name))
        {
            return Failure(
                DnsDiagnosticCode.InvalidZone,
                "message.zone",
                "The UPDATE zone section must contain one absolute SOA question in an ordinary class.");
        }

        DnsEncodeResult encodable = DnsMessageEncoder.EncodeMessage(
            message,
            new DnsEncodeOptions());
        if (!encodable.Success)
        {
            return FromEncoding(encodable);
        }

        DnsUpdateRequest request = new DnsUpdateRequest();
        request.Zone = RecordSetOperations.CopyName(
            zoneQuestion.Name,
            false);
        request.Class = (DnsClass)zoneClass;
        request.ClassValue = zoneClass;
        request.Message = message;

        int index = 0;
        while (index < message.Answers.Count)
        {
            DnsUpdateMessageResult prerequisite = ParsePrerequisite(
                message.Answers[index],
                request,
                zoneClass,
                index);
            if (!prerequisite.Success)
            {
                return prerequisite;
            }

            index = index + 1;
        }

        index = 0;
        while (index < message.Authorities.Count)
        {
            DnsUpdateMessageResult change = ParseChange(
                message.Authorities[index],
                request,
                zoneClass,
                index);
            if (!change.Success)
            {
                return change;
            }

            index = index + 1;
        }

        index = 0;
        while (index < message.Additionals.Count)
        {
            request.AdditionalRecords.Add(
                DnsZoneSnapshotOperations.CopyRecord(
                    message.Additionals[index]));
            index = index + 1;
        }

        DnsUpdateMessageResult result = new DnsUpdateMessageResult();
        result.Success = true;
        result.Message = message;
        result.Request = request;
        return result;
    }

    static DnsUpdateMessageResult AddPrerequisite(
        DnsUpdatePrerequisite prerequisite,
        DnsName zone,
        int zoneClass,
        List<DnsResourceRecord> records,
        int index)
    {
        if (prerequisite == null || prerequisite.Records == null ||
            !OwnerNameValid(prerequisite.Name, zone))
        {
            return IndexedFailure(
                DnsDiagnosticCode.InvalidZone,
                "request.prerequisites",
                "A prerequisite owner must be an absolute name inside the update zone.",
                index);
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            prerequisite.RecordTypeValue,
            (int)prerequisite.RecordType);
        switch (prerequisite.Kind)
        {
            case DnsUpdatePrerequisiteKind.NameInUse:
                if (prerequisite.Records.Count != 0)
                {
                    return InvalidPrerequisite(index);
                }

                records.Add(ControlRecord(
                    prerequisite.Name,
                    (int)DnsRecordType.Any,
                    (int)DnsClass.Any));
                break;
            case DnsUpdatePrerequisiteKind.NameNotInUse:
                if (prerequisite.Records.Count != 0)
                {
                    return InvalidPrerequisite(index);
                }

                records.Add(ControlRecord(
                    prerequisite.Name,
                    (int)DnsRecordType.Any,
                    (int)DnsClass.None));
                break;
            case DnsUpdatePrerequisiteKind.RecordSetExists:
                if (!DataTypeValid(typeValue) ||
                    prerequisite.Records.Count != 0)
                {
                    return InvalidPrerequisite(index);
                }

                records.Add(ControlRecord(
                    prerequisite.Name,
                    typeValue,
                    (int)DnsClass.Any));
                break;
            case DnsUpdatePrerequisiteKind.RecordSetExistsWithValues:
                if (!DataTypeValid(typeValue) ||
                    prerequisite.Records.Count == 0)
                {
                    return InvalidPrerequisite(index);
                }

                if (!AddValueRecords(
                        prerequisite.Records,
                        prerequisite.Name,
                        typeValue,
                        zoneClass,
                        0,
                        true,
                        records))
                {
                    return InvalidPrerequisite(index);
                }

                break;
            case DnsUpdatePrerequisiteKind.RecordSetDoesNotExist:
                if (!DataTypeValid(typeValue) ||
                    prerequisite.Records.Count != 0)
                {
                    return InvalidPrerequisite(index);
                }

                records.Add(ControlRecord(
                    prerequisite.Name,
                    typeValue,
                    (int)DnsClass.None));
                break;
            default:
                return InvalidPrerequisite(index);
        }

        return SuccessStep();
    }

    static DnsUpdateMessageResult AddChange(
        DnsUpdateChange change,
        DnsName zone,
        int zoneClass,
        List<DnsResourceRecord> records,
        int index)
    {
        if (change == null || change.Records == null ||
            !OwnerNameValid(change.Name, zone))
        {
            return IndexedFailure(
                DnsDiagnosticCode.InvalidZone,
                "request.changes",
                "An update owner must be an absolute name inside the update zone.",
                index);
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            change.RecordTypeValue,
            (int)change.RecordType);
        switch (change.Kind)
        {
            case DnsUpdateChangeKind.Add:
                if (!DataTypeValid(typeValue) ||
                    change.Records.Count == 0 ||
                    !AddValueRecords(
                        change.Records,
                        change.Name,
                        typeValue,
                        zoneClass,
                        change.TimeToLive,
                        true,
                        records))
                {
                    return InvalidChange(index);
                }

                break;
            case DnsUpdateChangeKind.DeleteRecord:
                if (!DataTypeValid(typeValue) ||
                    change.TimeToLive != 0 ||
                    change.Records.Count == 0 ||
                    !AddValueRecords(
                        change.Records,
                        change.Name,
                        typeValue,
                        (int)DnsClass.None,
                        0,
                        false,
                        records))
                {
                    return InvalidChange(index);
                }

                break;
            case DnsUpdateChangeKind.DeleteRecordSet:
                if (!DataTypeValid(typeValue) ||
                    change.TimeToLive != 0 ||
                    change.Records.Count != 0)
                {
                    return InvalidChange(index);
                }

                records.Add(ControlRecord(
                    change.Name,
                    typeValue,
                    (int)DnsClass.Any));
                break;
            case DnsUpdateChangeKind.DeleteName:
                if (change.TimeToLive != 0 || change.Records.Count != 0)
                {
                    return InvalidChange(index);
                }

                records.Add(ControlRecord(
                    change.Name,
                    (int)DnsRecordType.Any,
                    (int)DnsClass.Any));
                break;
            default:
                return InvalidChange(index);
        }

        return SuccessStep();
    }

    static bool AddValueRecords(
        List<DnsResourceRecord> source,
        DnsName owner,
        int typeValue,
        int classValue,
        uint timeToLive,
        bool requireClassAndTimeToLive,
        List<DnsResourceRecord> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            DnsResourceRecord record = source[index];
            if (record == null ||
                !RecordSetOperations.NamesEqual(record.Name, owner) ||
                DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue,
                    (int)record.RecordType) != typeValue ||
                (requireClassAndTimeToLive &&
                 (DnsHeaderCodec.ResolveCode(
                    record.ClassValue,
                    (int)record.Class) != classValue ||
                  record.TimeToLive != timeToLive)))
            {
                return false;
            }

            DnsResourceRecord copy =
                DnsZoneSnapshotOperations.CopyRecord(record);
            copy.Name = RecordSetOperations.CopyName(owner, false);
            copy.RecordType = (DnsRecordType)typeValue;
            copy.RecordTypeValue = typeValue;
            copy.Class = (DnsClass)classValue;
            copy.ClassValue = classValue;
            copy.TimeToLive = timeToLive;
            destination.Add(copy);
            index = index + 1;
        }

        return true;
    }

    static DnsUpdateMessageResult ParsePrerequisite(
        DnsResourceRecord record,
        DnsUpdateRequest request,
        int zoneClass,
        int index)
    {
        if (record == null || !OwnerNameValid(record.Name, request.Zone) ||
            record.TimeToLive != 0)
        {
            return IndexedFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "message.prerequisites",
                "A prerequisite requires an in-zone owner and zero TTL.",
                index);
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        DnsUpdatePrerequisite prerequisite = new DnsUpdatePrerequisite();
        prerequisite.Name = RecordSetOperations.CopyName(record.Name, false);
        prerequisite.RecordType = (DnsRecordType)typeValue;
        prerequisite.RecordTypeValue = typeValue;
        prerequisite.Class = (DnsClass)classValue;
        prerequisite.ClassValue = classValue;
        if (classValue == (int)DnsClass.Any && EmptyControl(record))
        {
            if (typeValue == (int)DnsRecordType.Any)
            {
                prerequisite.Kind = DnsUpdatePrerequisiteKind.NameInUse;
            }
            else if (DataTypeValid(typeValue))
            {
                prerequisite.Kind =
                    DnsUpdatePrerequisiteKind.RecordSetExists;
            }
            else
            {
                return InvalidParsedPrerequisite(index);
            }

            request.Prerequisites.Add(prerequisite);
            return SuccessStep();
        }

        if (classValue == (int)DnsClass.None && EmptyControl(record))
        {
            if (typeValue == (int)DnsRecordType.Any)
            {
                prerequisite.Kind = DnsUpdatePrerequisiteKind.NameNotInUse;
            }
            else if (DataTypeValid(typeValue))
            {
                prerequisite.Kind =
                    DnsUpdatePrerequisiteKind.RecordSetDoesNotExist;
            }
            else
            {
                return InvalidParsedPrerequisite(index);
            }

            request.Prerequisites.Add(prerequisite);
            return SuccessStep();
        }

        if (classValue != zoneClass || !DataTypeValid(typeValue))
        {
            return InvalidParsedPrerequisite(index);
        }

        DnsUpdatePrerequisite? existing = FindValuePrerequisite(
            request.Prerequisites,
            record.Name,
            typeValue);
        if (existing == null)
        {
            prerequisite.Kind =
                DnsUpdatePrerequisiteKind.RecordSetExistsWithValues;
            request.Prerequisites.Add(prerequisite);
            existing = prerequisite;
        }

        existing.Records.Add(DnsZoneSnapshotOperations.CopyRecord(record));
        return SuccessStep();
    }

    static DnsUpdateMessageResult ParseChange(
        DnsResourceRecord record,
        DnsUpdateRequest request,
        int zoneClass,
        int index)
    {
        if (record == null || !OwnerNameValid(record.Name, request.Zone))
        {
            return IndexedFailure(
                DnsDiagnosticCode.InvalidZone,
                "message.changes",
                "An update record owner must be inside the update zone.",
                index);
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        DnsUpdateChange change = new DnsUpdateChange();
        change.Name = RecordSetOperations.CopyName(record.Name, false);
        change.RecordType = (DnsRecordType)typeValue;
        change.RecordTypeValue = typeValue;
        change.Class = (DnsClass)classValue;
        change.ClassValue = classValue;
        change.TimeToLive = record.TimeToLive;
        if (classValue == zoneClass && DataTypeValid(typeValue))
        {
            change.Kind = DnsUpdateChangeKind.Add;
            DnsUpdateChange? existing = FindValueChange(
                request.Changes,
                record.Name,
                typeValue,
                record.TimeToLive,
                DnsUpdateChangeKind.Add);
            if (existing == null)
            {
                request.Changes.Add(change);
                existing = change;
            }

            existing.Records.Add(
                DnsZoneSnapshotOperations.CopyRecord(record));
            return SuccessStep();
        }

        if (classValue == (int)DnsClass.None &&
            record.TimeToLive == 0 && DataTypeValid(typeValue))
        {
            change.Kind = DnsUpdateChangeKind.DeleteRecord;
            DnsUpdateChange? existing = FindValueChange(
                request.Changes,
                record.Name,
                typeValue,
                0,
                DnsUpdateChangeKind.DeleteRecord);
            if (existing == null)
            {
                request.Changes.Add(change);
                existing = change;
            }

            existing.Records.Add(
                DnsZoneSnapshotOperations.CopyRecord(record));
            return SuccessStep();
        }

        if (classValue == (int)DnsClass.Any &&
            record.TimeToLive == 0 && EmptyControl(record))
        {
            if (typeValue == (int)DnsRecordType.Any)
            {
                change.Kind = DnsUpdateChangeKind.DeleteName;
            }
            else if (DataTypeValid(typeValue))
            {
                change.Kind = DnsUpdateChangeKind.DeleteRecordSet;
            }
            else
            {
                return InvalidParsedChange(index);
            }

            request.Changes.Add(change);
            return SuccessStep();
        }

        return InvalidParsedChange(index);
    }

    static DnsUpdatePrerequisite? FindValuePrerequisite(
        List<DnsUpdatePrerequisite> prerequisites,
        DnsName name,
        int typeValue)
    {
        int index = 0;
        while (index < prerequisites.Count)
        {
            DnsUpdatePrerequisite candidate = prerequisites[index];
            if (candidate.Kind ==
                    DnsUpdatePrerequisiteKind.RecordSetExistsWithValues &&
                candidate.RecordTypeValue == typeValue &&
                RecordSetOperations.NamesEqual(candidate.Name, name))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsUpdateChange? FindValueChange(
        List<DnsUpdateChange> changes,
        DnsName name,
        int typeValue,
        uint timeToLive,
        DnsUpdateChangeKind kind)
    {
        int index = 0;
        while (index < changes.Count)
        {
            DnsUpdateChange candidate = changes[index];
            if (candidate.Kind == kind &&
                candidate.RecordTypeValue == typeValue &&
                candidate.TimeToLive == timeToLive &&
                RecordSetOperations.NamesEqual(candidate.Name, name))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecord ControlRecord(
        DnsName name,
        int typeValue,
        int classValue)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = RecordSetOperations.CopyName(name, false);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.UseRawData = true;
        result.RawData = [];
        return result;
    }

    static bool EmptyControl(DnsResourceRecord record)
    {
        if (record.RawData == null || record.RawData.Length != 0)
        {
            return false;
        }

        if (record.UseRawData)
        {
            return true;
        }

        return record.Data != null && record.Data.Opaque != null &&
            record.Data.Opaque.Bytes != null &&
            record.Data.Opaque.Bytes.Length == 0;
    }

    static bool DataTypeValid(int typeValue)
    {
        if (typeValue <= 0 || typeValue > 65535 ||
            typeValue == (int)DnsRecordType.Opt ||
            typeValue == (int)DnsRecordType.Tkey ||
            typeValue == (int)DnsRecordType.Tsig)
        {
            return false;
        }

        return RecordDataModelCatalog.ReadRecordDataModel(typeValue) !=
            RecordDataModel.MetaQuery;
    }

    static bool ZoneClassValid(int classValue)
    {
        return classValue > 0 && classValue <= 65535 &&
            classValue != (int)DnsClass.Any &&
            classValue != (int)DnsClass.None;
    }

    static bool AbsoluteNameValid(DnsName name)
    {
        if (name == null)
        {
            return false;
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        return validated.Success && validated.Name != null &&
            validated.Name.IsAbsolute;
    }

    static bool OwnerNameValid(DnsName name, DnsName zone)
    {
        if (!AbsoluteNameValid(name))
        {
            return false;
        }

        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            name,
            zone);
        return suffix.Success && suffix.BooleanValue;
    }

    static DnsUpdateMessageResult InvalidPrerequisite(int index)
    {
        return IndexedFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "request.prerequisites",
            "The prerequisite kind, type, records, class, or TTL is invalid.",
            index);
    }

    static DnsUpdateMessageResult InvalidChange(int index)
    {
        return IndexedFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "request.changes",
            "The update kind, type, records, class, or TTL is invalid.",
            index);
    }

    static DnsUpdateMessageResult InvalidParsedPrerequisite(int index)
    {
        return IndexedFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "message.prerequisites",
            "The prerequisite class, type, TTL, or RDATA form is invalid.",
            index);
    }

    static DnsUpdateMessageResult InvalidParsedChange(int index)
    {
        return IndexedFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "message.changes",
            "The update class, type, TTL, or RDATA form is invalid.",
            index);
    }

    static DnsUpdateMessageResult SuccessStep()
    {
        DnsUpdateMessageResult result = new DnsUpdateMessageResult();
        result.Success = true;
        return result;
    }

    static DnsUpdateMessageResult FromEncoding(DnsEncodeResult encoded)
    {
        DnsUpdateMessageResult result = new DnsUpdateMessageResult();
        int index = 0;
        while (index < encoded.Diagnostics.Count)
        {
            result.Diagnostics.Add(encoded.Diagnostics[index]);
            index = index + 1;
        }

        if (result.Diagnostics.Count == 0)
        {
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
            diagnostic.Field = "message";
            diagnostic.Reason = "The UPDATE message could not be encoded.";
            result.Diagnostics.Add(diagnostic);
        }

        return result;
    }

    static DnsUpdateMessageResult IndexedFailure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int index)
    {
        DnsUpdateMessageResult result = Failure(code, field, reason);
        result.Diagnostics[0].HasRecordIndex = true;
        result.Diagnostics[0].RecordIndex = index;
        return result;
    }

    static DnsUpdateMessageResult Failure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsUpdateMessageResult result = new DnsUpdateMessageResult();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
