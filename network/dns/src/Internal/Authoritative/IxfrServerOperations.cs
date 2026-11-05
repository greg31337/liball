using System.Collections.Generic;

namespace dns;

internal class DnsIxfrServerRequest
{
    internal bool Success;
    internal DnsTransferRequest? Request;
    internal DnsMessage? Query;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class IxfrServerOperations
{
    internal static DnsTransferResult ServeTransfer(
        DnsTransferRequest request,
        IDnsZoneStore zoneStore,
        DnsAuthorizationRateState authorizationRateState)
    {
        DnsTransferResult result = FailureResult(
            DnsDiagnosticCode.None,
            string.Empty,
            string.Empty);
        if (request == null || zoneStore == null ||
            authorizationRateState == null)
        {
            return FailureResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A complete IXFR request and zone store are required.");
        }

        if (request.CancellationRequested)
        {
            result.TransportStatus = DnsTransportStatus.Cancelled;
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "IXFR serving was cancelled."));
            return result;
        }

        DnsIxfrServerRequest prepared = PrepareRequest(request);
        TransferServerOperations.CopyDiagnostics(
            prepared.Diagnostics,
            result.Diagnostics);
        if (!prepared.Success || prepared.Request == null ||
            prepared.Query == null)
        {
            return result;
        }

        DnsTransferRequest effective = prepared.Request;
        DnsMessage query = prepared.Query;
        DnsClockValue now = TransportTime.ReadClock();
        DnsTransferAuthentication authentication =
            TransferServerOperations.AuthenticateRequest(
                effective,
                query,
                now);
        result.Tsig = authentication.Tsig;
        if (!authentication.Accepted)
        {
            DnsTransferResult rejected =
                TransferServerOperations.ProtocolResult(
                    effective,
                    query,
                    DnsResponseCode.NotAuthoritative,
                    TransferServerOperations.AuthenticationDiagnostics(
                        authentication),
                    null,
                    now);
            rejected.Tsig = authentication.Tsig;
            rejected.TransportStatus =
                DnsTransportStatus.AuthenticationFailed;
            return rejected;
        }

        DnsAuthorizationDecision authorization =
            AuthorizationOperations.AuthorizeTransfer(
                effective,
                now,
                authorizationRateState);
        if (!authorization.Valid)
        {
            DnsTransferResult invalid = FailureResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.authorization",
                authorization.Reason);
            invalid.Tsig = authentication.Tsig;
            return invalid;
        }

        if (!authorization.Authorized || authorization.Slipped)
        {
            DnsDiagnosticCode code = DnsDiagnosticCode.ValueOutOfRange;
            if (authorization.RateLimited)
            {
                code = DnsDiagnosticCode.ResourceLimitExceeded;
            }

            string reason = authorization.Reason;
            if (authorization.Slipped)
            {
                reason = "A rate-limited IXFR cannot be served partially.";
            }

            return TransferServerOperations.ProtocolResult(
                effective,
                query,
                DnsResponseCode.Refused,
                TransferServerOperations.DiagnosticList(
                    code,
                    "request.authorization",
                    reason),
                authentication,
                now);
        }

        DnsZone? zone = zoneStore.ReadZone(
            effective.Zone,
            effective.Class,
            effective.ClassValue);
        if (zone == null)
        {
            return TransferServerOperations.ProtocolResult(
                effective,
                query,
                DnsResponseCode.NotAuthoritative,
                TransferServerOperations.DiagnosticList(
                    DnsDiagnosticCode.InvalidZone,
                    "request.zone",
                    "The requested IXFR zone is not hosted."),
                authentication,
                now);
        }

        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            result.Diagnostics.Clear();
            TransferServerOperations.CopyZoneDiagnostics(
                validation.Diagnostics,
                result.Diagnostics);
            result.Tsig = authentication.Tsig;
            return result;
        }

        DnsResourceRecord? currentSoa = FindApexSoa(zone);
        if (currentSoa == null || currentSoa.Data == null ||
            currentSoa.Data.Soa == null)
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.InvalidZone,
                "request.zone.soa",
                "The hosted IXFR zone requires one typed apex SOA."));
            return result;
        }

        uint currentSerial = currentSoa.Data.Soa.Serial;
        DnsSerialComparison comparison = DnsSerialArithmetic.CompareSerials(
            currentSerial,
            effective.CurrentSerial);
        if (!comparison.Defined || comparison.Comparison < 0)
        {
            return TransferServerOperations.ProtocolResult(
                effective,
                query,
                DnsResponseCode.Refused,
                TransferServerOperations.DiagnosticList(
                    DnsDiagnosticCode.ResponseMismatch,
                    "request.currentSerial",
                    "The requested IXFR serial is not comparable to the hosted serial."),
                authentication,
                now);
        }

        List<DnsResourceRecord> records;
        List<DnsZoneJournalEntry> journal =
            new List<DnsZoneJournalEntry>();
        bool upToDate = comparison.Comparison == 0;
        bool fallback = false;
        if (upToDate)
        {
            records = new List<DnsResourceRecord>();
            records.Add(DnsZoneSnapshotOperations.CopyRecord(currentSoa));
        }
        else
        {
            journal = zoneStore.ReadZoneJournal(
                effective.Zone,
                effective.Class,
                effective.ClassValue,
                effective.CurrentSerial);
            if (JournalCoverageValid(
                journal,
                effective.CurrentSerial,
                currentSerial))
            {
                records = CreateIxfrRecords(currentSoa, journal);
            }
            else
            {
                records = TransferServerOperations.CreateAxfrRecords(zone);
                fallback = true;
            }
        }

        if (records.Count == 0 || records.Count > effective.MaximumRecords)
        {
            DnsTransferResult limited = FailureResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.maximumRecords",
                "The complete IXFR response exceeds the record limit.");
            limited.TransportStatus = DnsTransportStatus.ResponseTooLarge;
            limited.Tsig = authentication.Tsig;
            return limited;
        }

        DnsTsigStreamState? streamState = CreateStreamState(
            effective,
            authentication);
        DnsTransferResult streamed = TransferServerOperations.StreamRecords(
            effective,
            query,
            zone,
            records,
            streamState,
            now);
        if (!streamed.Success)
        {
            streamed.Tsig = authentication.Tsig;
            return streamed;
        }

        if (!upToDate && (streamed.Messages.Count == 0 ||
            streamed.Messages[0].Answers.Count < 2))
        {
            streamed.Success = false;
            streamed.Messages.Clear();
            streamed.MessageCount = 0;
            streamed.RecordCount = 0;
            streamed.ByteCount = 0;
            streamed.TransportStatus = DnsTransportStatus.ResponseTooLarge;
            streamed.Diagnostics.Add(
                TransferServerOperations.CreateDiagnostic(
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "request.maximumResponseLength",
                    "A changed IXFR first message must fit its opening and next boundary record."));
            streamed.Tsig = authentication.Tsig;
            return streamed;
        }

        if (!CompleteAuthentication(streamed, streamState, authentication))
        {
            return streamed;
        }

        streamed.UpToDate = upToDate;
        streamed.FellBackToAxfr = fallback;
        streamed.HasInitialSerial = true;
        streamed.InitialSerial = effective.CurrentSerial;
        streamed.HasFinalSerial = true;
        streamed.FinalSerial = currentSerial;
        if (fallback)
        {
            streamed.Zone = DnsZoneSnapshotOperations.CopyZone(zone);
        }
        else if (!upToDate)
        {
            CopyJournal(journal, streamed.JournalEntries);
        }

        return streamed;
    }

    static DnsIxfrServerRequest PrepareRequest(
        DnsTransferRequest request)
    {
        DnsIxfrServerRequest result = new DnsIxfrServerRequest();
        if (request.Kind != DnsTransferKind.Ixfr ||
            request.Transport != DnsTransportKind.Tcp)
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.kind",
                "The IXFR service accepts incremental transfers over TCP only."));
            return result;
        }

        if (request.Endpoint == null || request.TransportOptions == null ||
            request.Authorization == null || request.MaximumMessages < 1 ||
            request.MaximumRecords < 1 ||
            request.MaximumBytes < DnsProtocolLimits.HeaderLength ||
            request.TransportOptions.MaximumResponseLength <
                DnsProtocolLimits.HeaderLength ||
            request.TransportOptions.MaximumResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.limits",
                "IXFR context and response limits must be complete and bounded."));
            return result;
        }

        DnsMessage query;
        DnsName zone;
        int classValue;
        uint currentSerial;
        if (request.Message == null)
        {
            RawDnsNameResult validated = RawDnsName.ValidateName(request.Zone);
            classValue = DnsHeaderCodec.ResolveCode(
                request.ClassValue,
                (int)request.Class);
            if (!request.HasCurrentSerial || !validated.Success ||
                validated.Name == null || !validated.Name.IsAbsolute ||
                classValue < 1 || classValue > 253)
            {
                result.Diagnostics.Add(
                    TransferServerOperations.CreateDiagnostic(
                        DnsDiagnosticCode.InvalidZone,
                        "request.zone",
                        "IXFR requires an absolute zone, ordinary class, and current serial."));
                return result;
            }

            zone = validated.Name;
            currentSerial = request.CurrentSerial;
            query = CreateQuery(zone, classValue, currentSerial);
        }
        else
        {
            DnsIxfrServerRequest parsed = ParseQuery(request.Message);
            if (!parsed.Success || parsed.Request == null ||
                parsed.Query == null)
            {
                return parsed;
            }

            zone = parsed.Request.Zone;
            classValue = parsed.Request.ClassValue;
            currentSerial = parsed.Request.CurrentSerial;
            query = parsed.Query;
        }

        DnsTransferRequest effective = CopyRequestContext(
            request,
            zone,
            classValue,
            currentSerial,
            query);
        result.Success = true;
        result.Request = effective;
        result.Query = query;
        return result;
    }

    static DnsIxfrServerRequest ParseQuery(DnsMessage query)
    {
        DnsIxfrServerRequest result = new DnsIxfrServerRequest();
        if (!MessageCollectionsValid(query) || !SectionCountsValid(query) ||
            query.Questions.Count != 1 || query.Answers.Count != 0 ||
            query.Authorities.Count != 1)
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.SectionCountMismatch,
                "request.message.sections",
                "An IXFR request requires one question and one authority SOA."));
            return result;
        }

        int flags = MessageOperations.EffectiveFlags(query.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            query.Header.OpcodeValue,
            (int)query.Header.Opcode);
        DnsQuestion question = query.Questions[0];
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(question.Name);
        DnsResourceRecord authority = query.Authorities[0];
        if (flags != 0 || opcode != (int)DnsOpcode.Query ||
            MessageOperations.ReadEffectiveResponseCode(query) != 0 ||
            typeValue != (int)DnsRecordType.Ixfr || classValue < 1 ||
            classValue > 253 || !zone.Success || zone.Name == null ||
            !zone.Name.IsAbsolute || !SoaMatches(
                authority,
                zone.Name,
                classValue))
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.message",
                "The IXFR request must contain a matching current-serial authority SOA."));
            return result;
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(query);
        if (!placement.Success ||
            query.Additionals.Count > (placement.HasTsig ? 1 : 0))
        {
            TransferServerOperations.CopyDiagnostics(
                placement.Diagnostics,
                result.Diagnostics);
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                DnsDiagnosticCode.InvalidTsigPlacement,
                "request.message.additional",
                "An IXFR request may contain only a final TSIG record."));
            return result;
        }

        DnsTransferRequest parsed = new DnsTransferRequest();
        parsed.Kind = DnsTransferKind.Ixfr;
        parsed.Zone = RecordSetOperations.CopyName(zone.Name, false);
        parsed.Class = (DnsClass)classValue;
        parsed.ClassValue = classValue;
        parsed.HasCurrentSerial = true;
        parsed.CurrentSerial = ReadSoaSerial(authority);
        parsed.Message = query;
        result.Success = true;
        result.Request = parsed;
        result.Query = query;
        return result;
    }

    static DnsTransferRequest CopyRequestContext(
        DnsTransferRequest source,
        DnsName zone,
        int classValue,
        uint currentSerial,
        DnsMessage query)
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Kind = DnsTransferKind.Ixfr;
        result.Zone = RecordSetOperations.CopyName(zone, false);
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.HasCurrentSerial = true;
        result.CurrentSerial = currentSerial;
        result.Endpoint = source.Endpoint;
        result.Transport = source.Transport;
        result.TransportOptions = source.TransportOptions;
        result.Message = query;
        result.TsigKey = source.TsigKey;
        result.Authorization = source.Authorization;
        result.MaximumMessages = source.MaximumMessages;
        result.MaximumRecords = source.MaximumRecords;
        result.MaximumBytes = source.MaximumBytes;
        result.CancellationRequested = source.CancellationRequested;
        return result;
    }

    static DnsMessage CreateQuery(
        DnsName zone,
        int classValue,
        uint serial)
    {
        DnsMessage result = new DnsMessage();
        DnsQuestion question = new DnsQuestion();
        question.Name = RecordSetOperations.CopyName(zone, false);
        question.RecordType = DnsRecordType.Ixfr;
        question.RecordTypeValue = (int)DnsRecordType.Ixfr;
        question.Class = (DnsClass)classValue;
        question.ClassValue = classValue;
        result.Questions.Add(question);
        result.Authorities.Add(CreateBoundarySoa(
            zone,
            classValue,
            serial));
        result.Header.QuestionCount = 1;
        result.Header.AuthorityCount = 1;
        return result;
    }

    static DnsResourceRecord CreateBoundarySoa(
        DnsName zone,
        int classValue,
        uint serial)
    {
        DnsName root = new DnsName();
        root.IsAbsolute = true;
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = RecordSetOperations.CopyName(zone, false);
        result.RecordType = DnsRecordType.Soa;
        result.RecordTypeValue = (int)DnsRecordType.Soa;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.PrimaryNameServer = root;
        result.Data.Soa.ResponsibleMailbox = root;
        result.Data.Soa.Serial = serial;
        return result;
    }

    static bool JournalCoverageValid(
        List<DnsZoneJournalEntry> journal,
        uint requestedSerial,
        uint currentSerial)
    {
        if (journal == null || journal.Count == 0)
        {
            return false;
        }

        uint expected = requestedSerial;
        int index = 0;
        while (index < journal.Count)
        {
            DnsZoneJournalEntry entry = journal[index];
            if (entry == null || entry.Transaction == null ||
                !entry.Transaction.Committed || entry.FromSerial != expected ||
                !TransitionValid(entry.FromSerial, entry.ToSerial) ||
                !ChangesReconstructable(entry.Transaction.Changes))
            {
                return false;
            }

            expected = entry.ToSerial;
            index = index + 1;
        }

        return expected == currentSerial;
    }

    static bool ChangesReconstructable(List<DnsZoneChange> changes)
    {
        if (changes == null)
        {
            return false;
        }

        int index = 0;
        while (index < changes.Count)
        {
            DnsZoneChange change = changes[index];
            int typeValue = 0;
            if (change != null)
            {
                typeValue = DnsHeaderCodec.ResolveCode(
                    change.RecordTypeValue,
                    (int)change.RecordType);
            }

            if (change == null ||
                (change.Kind == DnsZoneChangeKind.Add &&
                 change.Record == null) ||
                (change.Kind == DnsZoneChangeKind.DeleteRecord &&
                 change.Record == null) ||
                (change.Kind == DnsZoneChangeKind.ReplaceRecordSet &&
                 typeValue != (int)DnsRecordType.Soa) ||
                (change.Kind != DnsZoneChangeKind.Add &&
                 change.Kind != DnsZoneChangeKind.DeleteRecord &&
                 change.Kind != DnsZoneChangeKind.ReplaceRecordSet))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static List<DnsResourceRecord> CreateIxfrRecords(
        DnsResourceRecord currentSoa,
        List<DnsZoneJournalEntry> journal)
    {
        List<DnsResourceRecord> result =
            new List<DnsResourceRecord>();
        result.Add(DnsZoneSnapshotOperations.CopyRecord(currentSoa));
        int entryIndex = 0;
        while (entryIndex < journal.Count)
        {
            DnsZoneJournalEntry entry = journal[entryIndex];
            result.Add(CopySoaWithSerial(currentSoa, entry.FromSerial));
            AddChanges(
                entry.Transaction.Changes,
                DnsZoneChangeKind.DeleteRecord,
                result);
            result.Add(CopySoaWithSerial(currentSoa, entry.ToSerial));
            AddChanges(
                entry.Transaction.Changes,
                DnsZoneChangeKind.Add,
                result);
            entryIndex = entryIndex + 1;
        }

        result.Add(DnsZoneSnapshotOperations.CopyRecord(currentSoa));
        return result;
    }

    static void AddChanges(
        List<DnsZoneChange> changes,
        DnsZoneChangeKind kind,
        List<DnsResourceRecord> records)
    {
        int index = 0;
        while (index < changes.Count)
        {
            DnsZoneChange change = changes[index];
            if (change.Kind == kind && change.Record != null)
            {
                records.Add(DnsZoneSnapshotOperations.CopyRecord(
                    change.Record));
            }

            index = index + 1;
        }
    }

    static DnsResourceRecord CopySoaWithSerial(
        DnsResourceRecord source,
        uint serial)
    {
        DnsResourceRecord result =
            DnsZoneSnapshotOperations.CopyRecord(source);
        if (result.Data != null && result.Data.Soa != null)
        {
            result.Data.Soa.Serial = serial;
        }

        return result;
    }

    static bool CompleteAuthentication(
        DnsTransferResult result,
        DnsTsigStreamState? streamState,
        DnsTransferAuthentication authentication)
    {
        if (streamState == null)
        {
            result.Tsig = authentication.Tsig;
            return true;
        }

        DnsTsigResult completed = TsigStreamOperations.Complete(streamState);
        result.Tsig = completed;
        TransferServerOperations.CopyDiagnostics(
            completed.Diagnostics,
            result.Diagnostics);
        if (completed.Success && completed.Authenticated)
        {
            return true;
        }

        result.Success = false;
        result.Messages.Clear();
        result.MessageCount = 0;
        result.RecordCount = 0;
        result.ByteCount = 0;
        result.TransportStatus = DnsTransportStatus.AuthenticationFailed;
        return false;
    }

    static DnsTsigStreamState? CreateStreamState(
        DnsTransferRequest request,
        DnsTransferAuthentication authentication)
    {
        if (!authentication.Present || authentication.Tsig == null ||
            request.TsigKey == null)
        {
            return null;
        }

        return TsigStreamOperations.CreateState(
            request.TsigKey,
            authentication.Tsig.Mac);
    }

    static DnsResourceRecord? FindApexSoa(DnsZone zone)
    {
        List<DnsResourceRecord> records =
            TransferServerOperations.CreateAxfrRecords(zone);
        if (records.Count == 0)
        {
            return null;
        }

        return records[0];
    }

    static bool SoaMatches(
        DnsResourceRecord record,
        DnsName zone,
        int classValue)
    {
        int recordClass = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        return typeValue == (int)DnsRecordType.Soa &&
            recordClass == classValue &&
            RecordSetOperations.NamesEqual(record.Name, zone) &&
            record.Data != null && record.Data.Soa != null &&
            !record.UseRawData;
    }

    static uint ReadSoaSerial(DnsResourceRecord record)
    {
        if (record.Data == null || record.Data.Soa == null)
        {
            return 0;
        }

        return record.Data.Soa.Serial;
    }

    static bool TransitionValid(uint fromSerial, uint toSerial)
    {
        DnsSerialComparison comparison = DnsSerialArithmetic.CompareSerials(
            toSerial,
            fromSerial);
        return comparison.Defined && comparison.Comparison == 1;
    }

    static bool MessageCollectionsValid(DnsMessage message)
    {
        return message != null && message.Header != null &&
            message.Questions != null && message.Answers != null &&
            message.Authorities != null && message.Additionals != null;
    }

    static bool SectionCountsValid(DnsMessage message)
    {
        return message.Header.QuestionCount == message.Questions.Count &&
            message.Header.AnswerCount == message.Answers.Count &&
            message.Header.AuthorityCount == message.Authorities.Count &&
            message.Header.AdditionalCount == message.Additionals.Count;
    }

    static void CopyJournal(
        List<DnsZoneJournalEntry> source,
        List<DnsZoneJournalEntry> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(DnsZoneSnapshotOperations.CopyJournalEntry(
                source[index]));
            index = index + 1;
        }
    }

    static DnsTransferResult FailureResult(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = DnsTransferKind.Ixfr;
        result.ResponseCode = DnsResponseCode.ServerFailure;
        result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        if (code != DnsDiagnosticCode.None)
        {
            result.Diagnostics.Add(TransferServerOperations.CreateDiagnostic(
                code,
                field,
                reason));
        }

        return result;
    }
}
