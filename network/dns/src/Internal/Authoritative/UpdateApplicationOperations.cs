using System.Collections.Generic;

namespace dns;

internal class DnsUpdateRecordDataResult
{
    internal bool Success = true;
    internal List<byte[]> Values = new List<byte[]>();
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class DnsUpdatePrerequisiteResult
{
    internal bool Satisfied = true;
    internal DnsResponseCode ResponseCode;
    internal string Reason = string.Empty;
}

internal class DnsUpdateAuthenticationResult
{
    internal bool Present;
    internal bool Accepted = true;
    internal DnsTsigResult? Tsig;
}

internal static class UpdateApplicationOperations
{
    internal static DnsUpdateResult ApplyUpdate(
        DnsUpdateRequest request,
        IDnsZoneStore zoneStore,
        DnsAuthorizationRateState authorizationRateState)
    {
        if (request == null)
        {
            return FailureResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A DNS update request is required.");
        }

        if (request.CancellationRequested)
        {
            return FailureResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "DNS update processing was cancelled.");
        }

        DnsUpdateMessageResult prepared;
        if (request.Message == null)
        {
            prepared = UpdateMessageOperations.BuildUpdateMessage(
                request,
                0);
            if (prepared.Success && prepared.Message != null)
            {
                prepared = UpdateMessageOperations.ParseUpdateMessage(
                    prepared.Message);
            }
        }
        else
        {
            prepared = UpdateMessageOperations.ParseUpdateMessage(
                request.Message);
        }

        if (!prepared.Success || prepared.Request == null)
        {
            DnsResponseCode code = DnsResponseCode.FormatError;
            if (IsOutsideZoneFailure(prepared.Diagnostics))
            {
                code = DnsResponseCode.NotZone;
            }

            return ProtocolResult(
                request,
                code,
                prepared.Diagnostics);
        }

        DnsUpdateRequest effective = prepared.Request;
        effective.ClientEndpoint = request.ClientEndpoint;
        effective.TsigKey = request.TsigKey;
        effective.Authorization = request.Authorization;
        effective.HasExpectedSerial = request.HasExpectedSerial;
        effective.ExpectedSerial = request.ExpectedSerial;
        DnsClockValue now = TransportTime.ReadClock();
        DnsUpdateAuthenticationResult authentication =
            AuthenticateRequest(effective, now);
        if (!authentication.Accepted)
        {
            DnsUpdateResult rejected = ProtocolResult(
                effective,
                DnsResponseCode.NotAuthoritative,
                AuthenticationDiagnostics(authentication));
            rejected.Tsig = authentication.Tsig;
            return rejected;
        }

        DnsZone? zone = zoneStore.ReadZone(
            effective.Zone,
            effective.Class,
            effective.ClassValue);
        if (zone == null)
        {
            return CompleteAuthenticatedResponse(ProtocolResult(
                effective,
                DnsResponseCode.NotAuthoritative,
                DiagnosticList(
                    DnsDiagnosticCode.InvalidZone,
                    "request.zone",
                    "The update zone is not hosted.")),
                effective,
                authentication,
                now);
        }

        DnsUpdatePrerequisiteResult prerequisites =
            EvaluatePrerequisites(effective.Prerequisites, zone);
        if (!prerequisites.Satisfied)
        {
            return CompleteAuthenticatedResponse(ProtocolResult(
                effective,
                prerequisites.ResponseCode,
                DiagnosticList(
                    DnsDiagnosticCode.ResponseMismatch,
                    "request.prerequisites",
                    prerequisites.Reason)),
                effective,
                authentication,
                now);
        }

        DnsSoaSerialResult current = ReadSoaSerial(zone);
        if (!current.Success)
        {
            return CompleteAuthenticatedResponse(ProtocolResult(
                effective,
                DnsResponseCode.ServerFailure,
                DiagnosticList(
                    DnsDiagnosticCode.InvalidZone,
                    "request.zone.soa",
                    "The hosted zone does not contain one readable SOA serial.")),
                effective,
                authentication,
                now);
        }

        if (effective.HasExpectedSerial &&
            effective.ExpectedSerial != current.Serial)
        {
            return CompleteAuthenticatedResponse(ProtocolResult(
                effective,
                DnsResponseCode.Refused,
                DiagnosticList(
                    DnsDiagnosticCode.ResponseMismatch,
                    "request.expectedSerial",
                    "The expected SOA serial does not match the hosted zone.")),
                effective,
                authentication,
                now);
        }

        DnsAuthorizationDecision authorization =
            AuthorizationOperations.AuthorizeUpdate(
                effective,
                now,
                authorizationRateState);
        if (!authorization.Valid)
        {
            DnsUpdateResult invalidPolicy = FailureResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.authorization",
                authorization.Reason);
            invalidPolicy.Tsig = authentication.Tsig;
            return invalidPolicy;
        }

        if (!authorization.Authorized)
        {
            DnsDiagnosticCode denialCode = DnsDiagnosticCode.ValueOutOfRange;
            if (authorization.RateLimited)
            {
                denialCode = DnsDiagnosticCode.ResourceLimitExceeded;
            }

            return CompleteAuthenticatedResponse(ProtocolResult(
                effective,
                DnsResponseCode.Refused,
                DiagnosticList(
                    denialCode,
                    "request.authorization",
                    authorization.Reason)),
                effective,
                authentication,
                now);
        }

        DnsZoneTransaction transaction = CreateTransaction(
            effective,
            zone,
            current.Serial);
        DnsZoneValidationResult applied = zoneStore.ApplyZoneTransaction(
            effective.Zone,
            effective.Class,
            effective.ClassValue,
            transaction);
        if (!applied.Valid || !transaction.Committed)
        {
            DnsUpdateResult rejected = ProtocolResult(
                effective,
                DnsResponseCode.Refused,
                ConvertDiagnostics(applied.Diagnostics));
            rejected.Transaction = transaction;
            return CompleteAuthenticatedResponse(
                rejected,
                effective,
                authentication,
                now);
        }

        DnsUpdateResult result = ProtocolResult(
            effective,
            DnsResponseCode.NoError,
            new List<DnsDiagnostic>());
        result.Applied = true;
        result.Transaction = transaction;
        DnsZone? updated = zoneStore.ReadZone(
            effective.Zone,
            effective.Class,
            effective.ClassValue);
        if (updated != null)
        {
            DnsSoaSerialResult resulting = ReadSoaSerial(updated);
            if (resulting.Success)
            {
                result.HasResultingSerial = true;
                result.ResultingSerial = resulting.Serial;
            }
        }

        return CompleteAuthenticatedResponse(
            result,
            effective,
            authentication,
            now);
    }

    static DnsUpdateAuthenticationResult AuthenticateRequest(
        DnsUpdateRequest request,
        DnsClockValue now)
    {
        DnsUpdateAuthenticationResult result =
            new DnsUpdateAuthenticationResult();
        if (request.Message == null)
        {
            return result;
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(
                request.Message);
        if (!placement.Success)
        {
            result.Accepted = false;
            DnsTsigResult invalid = new DnsTsigResult();
            CopyDiagnostics(invalid.Diagnostics, placement.Diagnostics);
            result.Tsig = invalid;
            return result;
        }

        result.Present = placement.HasTsig;
        if (!placement.HasTsig && request.TsigKey == null)
        {
            return result;
        }

        if (request.TsigKey == null)
        {
            DnsTsigResult missingKey = new DnsTsigResult();
            missingKey.Success = true;
            missingKey.Error = DnsTsigError.BadKey;
            missingKey.ErrorValue = (int)DnsTsigError.BadKey;
            if (placement.Record != null)
            {
                missingKey.KeyName = RecordSetOperations.CopyName(
                    placement.Record.Name,
                    true);
            }

            missingKey.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.tsigKey",
                "No verification key is available for the signed update."));
            result.Accepted = false;
            result.Tsig = missingKey;
            return result;
        }

        DnsTsigResult verified = TsigOperations.Verify(
            request.Message,
            request.TsigKey,
            [],
            now);
        result.Tsig = verified;
        result.Accepted = verified.Success && verified.Authenticated &&
            verified.ErrorValue == 0;
        return result;
    }

    static DnsUpdateResult CompleteAuthenticatedResponse(
        DnsUpdateResult result,
        DnsUpdateRequest request,
        DnsUpdateAuthenticationResult authentication,
        DnsClockValue now)
    {
        result.Tsig = authentication.Tsig;
        if (!authentication.Present || authentication.Tsig == null ||
            request.TsigKey == null || result.Response == null)
        {
            return result;
        }

        DnsTsigResult signing = TsigOperations.Sign(
            result.Response,
            request.TsigKey,
            authentication.Tsig.Mac,
            now);
        if (!signing.Success || signing.Message == null)
        {
            result.Success = false;
            CopyDiagnostics(result.Diagnostics, signing.Diagnostics);
            return result;
        }

        result.Response = signing.Message;
        return result;
    }

    static List<DnsDiagnostic> AuthenticationDiagnostics(
        DnsUpdateAuthenticationResult authentication)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        if (authentication.Tsig != null)
        {
            CopyDiagnostics(result, authentication.Tsig.Diagnostics);
        }

        if (result.Count == 0)
        {
            result.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.tsig",
                "The update TSIG could not be authenticated."));
        }

        return result;
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static DnsDiagnostic CreateDiagnostic(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic result = new DnsDiagnostic();
        result.Code = code;
        result.Field = field;
        result.Reason = reason;
        return result;
    }

    static DnsUpdatePrerequisiteResult EvaluatePrerequisites(
        List<DnsUpdatePrerequisite> prerequisites,
        DnsZone zone)
    {
        int zoneClass = DnsHeaderCodec.ResolveCode(
            zone.ClassValue,
            (int)zone.Class);
        int index = 0;
        while (index < prerequisites.Count)
        {
            DnsUpdatePrerequisite prerequisite = prerequisites[index];
            DnsZoneNode? node = FindNode(zone, prerequisite.Name);
            int typeValue = DnsHeaderCodec.ResolveCode(
                prerequisite.RecordTypeValue,
                (int)prerequisite.RecordType);
            DnsResourceRecordSet? set = null;
            if (node != null)
            {
                set = FindRecordSet(node, typeValue, zoneClass);
            }

            if (prerequisite.Kind ==
                    DnsUpdatePrerequisiteKind.NameInUse && node == null)
            {
                return FailedPrerequisite(
                    DnsResponseCode.NameError,
                    "A required owner name does not exist.");
            }

            if (prerequisite.Kind ==
                    DnsUpdatePrerequisiteKind.NameNotInUse && node != null)
            {
                return FailedPrerequisite(
                    DnsResponseCode.NameExists,
                    "An owner name exists when absence was required.");
            }

            if (prerequisite.Kind ==
                    DnsUpdatePrerequisiteKind.RecordSetExists && set == null)
            {
                return FailedPrerequisite(
                    DnsResponseCode.RecordSetDoesNotExist,
                    "A required resource-record set does not exist.");
            }

            if (prerequisite.Kind ==
                DnsUpdatePrerequisiteKind.RecordSetExistsWithValues)
            {
                if (set == null ||
                    !RecordSetValuesEqual(set.Records,
                        prerequisite.Records))
                {
                    return FailedPrerequisite(
                        DnsResponseCode.RecordSetDoesNotExist,
                        "A required resource-record set does not have the supplied values.");
                }
            }

            if (prerequisite.Kind ==
                    DnsUpdatePrerequisiteKind.RecordSetDoesNotExist &&
                set != null)
            {
                return FailedPrerequisite(
                    DnsResponseCode.RecordSetExists,
                    "A resource-record set exists when absence was required.");
            }

            index = index + 1;
        }

        return new DnsUpdatePrerequisiteResult();
    }

    static bool RecordSetValuesEqual(
        List<DnsResourceRecord> left,
        List<DnsResourceRecord> right)
    {
        DnsUpdateRecordDataResult leftValues = CanonicalValues(left);
        DnsUpdateRecordDataResult rightValues = CanonicalValues(right);
        if (!leftValues.Success || !rightValues.Success ||
            leftValues.Values.Count != rightValues.Values.Count)
        {
            return false;
        }

        int index = 0;
        while (index < leftValues.Values.Count)
        {
            if (!ContainsBytes(
                    rightValues.Values,
                    leftValues.Values[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static DnsUpdateRecordDataResult CanonicalValues(
        List<DnsResourceRecord> records)
    {
        DnsUpdateRecordDataResult result = new DnsUpdateRecordDataResult();
        int index = 0;
        while (index < records.Count)
        {
            DnsEncodeResult encoded =
                CanonicalDnsData.EncodeCanonicalRecordData(records[index]);
            if (!encoded.Success)
            {
                result.Success = false;
                int diagnosticIndex = 0;
                while (diagnosticIndex < encoded.Diagnostics.Count)
                {
                    result.Diagnostics.Add(
                        encoded.Diagnostics[diagnosticIndex]);
                    diagnosticIndex = diagnosticIndex + 1;
                }

                return result;
            }

            if (!ContainsBytes(result.Values, encoded.Bytes))
            {
                result.Values.Add(encoded.Bytes);
            }

            index = index + 1;
        }

        return result;
    }

    static bool ContainsBytes(List<byte[]> values, byte[] candidate)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (RecordSetOperations.BytesEqual(values[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsZoneTransaction CreateTransaction(
        DnsUpdateRequest request,
        DnsZone zone,
        uint currentSerial)
    {
        DnsZoneTransaction result = new DnsZoneTransaction();
        result.Atomic = true;
        result.HasExpectedSerial = true;
        result.ExpectedSerial = currentSerial;
        uint selectedSerial = currentSerial;
        int zoneClass = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        int index = 0;
        while (index < request.Changes.Count)
        {
            DnsUpdateChange update = request.Changes[index];
            int typeValue = DnsHeaderCodec.ResolveCode(
                update.RecordTypeValue,
                (int)update.RecordType);
            if (update.Kind == DnsUpdateChangeKind.Add &&
                typeValue == (int)DnsRecordType.Soa)
            {
                selectedSerial = AddSoaReplacements(
                    result,
                    update,
                    zone,
                    zoneClass,
                    selectedSerial);
            }
            else if ((update.Kind == DnsUpdateChangeKind.DeleteRecord ||
                      update.Kind == DnsUpdateChangeKind.DeleteRecordSet) &&
                typeValue == (int)DnsRecordType.Soa &&
                RecordSetOperations.NamesEqual(update.Name, zone.Apex))
            {
                ;
            }
            else if (update.Kind == DnsUpdateChangeKind.Add)
            {
                AddRecordChanges(
                    result,
                    update,
                    typeValue,
                    zoneClass,
                    DnsZoneChangeKind.Add);
            }
            else if (update.Kind == DnsUpdateChangeKind.DeleteRecord)
            {
                AddRecordChanges(
                    result,
                    update,
                    typeValue,
                    zoneClass,
                    DnsZoneChangeKind.DeleteRecord);
            }
            else if (update.Kind == DnsUpdateChangeKind.DeleteName &&
                RecordSetOperations.NamesEqual(update.Name, zone.Apex))
            {
                AddApexDeleteNameChanges(result, zone, zoneClass);
            }
            else
            {
                DnsZoneChange change = CreateZoneChange(
                    update.Name,
                    typeValue,
                    zoneClass);
                if (update.Kind == DnsUpdateChangeKind.DeleteRecordSet)
                {
                    change.Kind = DnsZoneChangeKind.DeleteRecordSet;
                }
                else
                {
                    change.Kind = DnsZoneChangeKind.DeleteName;
                }

                result.Changes.Add(change);
            }

            index = index + 1;
        }

        return result;
    }

    static uint AddSoaReplacements(
        DnsZoneTransaction transaction,
        DnsUpdateChange update,
        DnsZone zone,
        int classValue,
        uint currentSerial)
    {
        if (!RecordSetOperations.NamesEqual(update.Name, zone.Apex))
        {
            return currentSerial;
        }

        uint result = currentSerial;
        int index = 0;
        while (index < update.Records.Count)
        {
            DnsResourceRecord source = update.Records[index];
            if (source.Data != null && source.Data.Soa != null)
            {
                uint candidate = source.Data.Soa.Serial;
                DnsSerialComparison comparison =
                    DnsSerialArithmetic.CompareSerials(candidate, result);
                if (comparison.Defined && comparison.Comparison == 1)
                {
                    DnsResourceRecord record =
                        DnsZoneSnapshotOperations.CopyRecord(source);
                    record.Name = RecordSetOperations.CopyName(
                        zone.Apex,
                        false);
                    record.RecordType = DnsRecordType.Soa;
                    record.RecordTypeValue = (int)DnsRecordType.Soa;
                    record.Class = (DnsClass)classValue;
                    record.ClassValue = classValue;
                    DnsResourceRecordSet set = new DnsResourceRecordSet();
                    set.Name = RecordSetOperations.CopyName(
                        zone.Apex,
                        false);
                    set.RecordType = DnsRecordType.Soa;
                    set.RecordTypeValue = (int)DnsRecordType.Soa;
                    set.Class = (DnsClass)classValue;
                    set.ClassValue = classValue;
                    set.TimeToLive = record.TimeToLive;
                    set.Records.Add(record);
                    DnsZoneChange change = CreateZoneChange(
                        zone.Apex,
                        (int)DnsRecordType.Soa,
                        classValue);
                    change.Kind = DnsZoneChangeKind.ReplaceRecordSet;
                    change.RecordSet = set;
                    transaction.Changes.Add(change);
                    transaction.HasResultingSerial = true;
                    transaction.ResultingSerial = candidate;
                    result = candidate;
                }
            }

            index = index + 1;
        }

        return result;
    }

    static void AddApexDeleteNameChanges(
        DnsZoneTransaction transaction,
        DnsZone zone,
        int classValue)
    {
        DnsZoneNode? apex = FindNode(zone, zone.Apex);
        if (apex == null)
        {
            return;
        }

        int index = 0;
        while (index < apex.RecordSets.Count)
        {
            DnsResourceRecordSet set = apex.RecordSets[index];
            int typeValue = DnsHeaderCodec.ResolveCode(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue != (int)DnsRecordType.Soa &&
                typeValue != (int)DnsRecordType.Ns)
            {
                DnsZoneChange change = CreateZoneChange(
                    zone.Apex,
                    typeValue,
                    classValue);
                change.Kind = DnsZoneChangeKind.DeleteRecordSet;
                transaction.Changes.Add(change);
            }

            index = index + 1;
        }
    }

    static void AddRecordChanges(
        DnsZoneTransaction transaction,
        DnsUpdateChange update,
        int typeValue,
        int classValue,
        DnsZoneChangeKind kind)
    {
        int index = 0;
        while (index < update.Records.Count)
        {
            DnsZoneChange change = CreateZoneChange(
                update.Name,
                typeValue,
                classValue);
            change.Kind = kind;
            change.Record = DnsZoneSnapshotOperations.CopyRecord(
                update.Records[index]);
            change.Record.Name = RecordSetOperations.CopyName(
                update.Name,
                false);
            change.Record.RecordType = (DnsRecordType)typeValue;
            change.Record.RecordTypeValue = typeValue;
            change.Record.Class = (DnsClass)classValue;
            change.Record.ClassValue = classValue;
            if (kind == DnsZoneChangeKind.DeleteRecord)
            {
                change.Record.TimeToLive = 0;
            }

            transaction.Changes.Add(change);
            index = index + 1;
        }
    }

    static DnsZoneChange CreateZoneChange(
        DnsName name,
        int typeValue,
        int classValue)
    {
        DnsZoneChange result = new DnsZoneChange();
        result.Name = RecordSetOperations.CopyName(name, false);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        return result;
    }

    static DnsZoneNode? FindNode(DnsZone zone, DnsName name)
    {
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            if (RecordSetOperations.NamesEqual(zone.Nodes[index].Name, name))
            {
                return zone.Nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecordSet? FindRecordSet(
        DnsZoneNode node,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[index];
            int setType = DnsHeaderCodec.ResolveCode(
                set.RecordTypeValue,
                (int)set.RecordType);
            int setClass = DnsHeaderCodec.ResolveCode(
                set.ClassValue,
                (int)set.Class);
            if (setType == typeValue && setClass == classValue)
            {
                return set;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsSoaSerialResult ReadSoaSerial(DnsZone zone)
    {
        DnsSoaSerialResult result = new DnsSoaSerialResult();
        DnsZoneNode? apex = FindNode(zone, zone.Apex);
        if (apex == null)
        {
            return result;
        }

        DnsResourceRecordSet? soa = FindRecordSet(
            apex,
            (int)DnsRecordType.Soa,
            DnsHeaderCodec.ResolveCode(
                zone.ClassValue,
                (int)zone.Class));
        if (soa != null && soa.Records.Count == 1)
        {
            DnsResourceRecord record = soa.Records[0];
            if (record.Data != null && record.Data.Soa != null)
            {
                result.Success = true;
                result.Serial = record.Data.Soa.Serial;
            }
        }

        return result;
    }

    static DnsUpdatePrerequisiteResult FailedPrerequisite(
        DnsResponseCode responseCode,
        string reason)
    {
        DnsUpdatePrerequisiteResult result =
            new DnsUpdatePrerequisiteResult();
        result.Satisfied = false;
        result.ResponseCode = responseCode;
        result.Reason = reason;
        return result;
    }

    static DnsUpdateResult ProtocolResult(
        DnsUpdateRequest request,
        DnsResponseCode responseCode,
        List<DnsDiagnostic> diagnostics)
    {
        DnsUpdateResult result = new DnsUpdateResult();
        result.Success = true;
        result.ResponseCode = responseCode;
        result.ResponseCodeValue = (int)responseCode;
        result.Response = CreateResponse(request, responseCode);
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsMessage CreateResponse(
        DnsUpdateRequest request,
        DnsResponseCode responseCode)
    {
        DnsMessage result = new DnsMessage();
        if (request.Message != null)
        {
            result.Header.Identifier = request.Message.Header.Identifier;
        }

        result.Header.Opcode = DnsOpcode.Update;
        result.Header.OpcodeValue = (int)DnsOpcode.Update;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        result.Header.Flags = DnsHeaderFlag.Response;
        result.Header.FlagsValue = (int)DnsHeaderFlag.Response;
        RawDnsNameResult zone = RawDnsName.ValidateName(request.Zone);
        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        if (zone.Success && zone.Name != null && zone.Name.IsAbsolute &&
            classValue > 0 && classValue <= 65535)
        {
            DnsQuestion question = new DnsQuestion();
            question.Name = RecordSetOperations.CopyName(
                zone.Name,
                false);
            question.RecordType = DnsRecordType.Soa;
            question.RecordTypeValue = (int)DnsRecordType.Soa;
            question.Class = (DnsClass)classValue;
            question.ClassValue = classValue;
            result.Questions.Add(question);
            result.Header.QuestionCount = 1;
        }

        return result;
    }

    static bool IsOutsideZoneFailure(List<DnsDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            DnsDiagnostic diagnostic = diagnostics[index];
            if (diagnostic.Code == DnsDiagnosticCode.InvalidZone &&
                diagnostic.Field != null &&
                diagnostic.Field != "request.zone" &&
                diagnostic.Field != "message.zone")
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static List<DnsDiagnostic> DiagnosticList(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Add(diagnostic);
        return result;
    }

    static List<DnsDiagnostic> ConvertDiagnostics(
        List<DnsZoneDiagnostic> diagnostics)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        int index = 0;
        while (index < diagnostics.Count)
        {
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = diagnostics[index].Code;
            diagnostic.Field = "request.changes";
            diagnostic.Reason = diagnostics[index].Reason;
            result.Add(diagnostic);
            index = index + 1;
        }

        return result;
    }

    static DnsUpdateResult FailureResult(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsUpdateResult result = new DnsUpdateResult();
        result.ResponseCode = DnsResponseCode.ServerFailure;
        result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
        result.Diagnostics.Add(DiagnosticList(code, field, reason)[0]);
        return result;
    }
}
