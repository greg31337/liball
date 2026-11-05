using System.Collections.Generic;

namespace dns;

internal class DnsIxfrPreparation
{
    internal bool Success;
    internal DnsMessage? Query;
    internal byte[] QueryBytes = [];
    internal byte[] RequestMac = [];
    internal DnsName? Zone;
    internal int ClassValue;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class IxfrClientOperations
{
    const int ExpectOldSoa = 0;
    const int ReadingDeletes = 1;
    const int ReadingAdds = 2;

    internal static DnsTransferResult ReceiveTransfer(
        DnsTransferRequest request,
        DnsTransferClient client)
    {
        DnsTransferResult result = CreateResult();
        if (request == null || client == null)
        {
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A complete IXFR request and client are required.");
            return result;
        }

        if (request.CancellationRequested)
        {
            result.TransportStatus = DnsTransportStatus.Cancelled;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "The IXFR operation was cancelled.");
            return result;
        }

        DnsIxfrPreparation preparation = PrepareRequest(request);
        TransferClientOperations.CopyDiagnostics(
            preparation.Diagnostics,
            result.Diagnostics);
        if (!preparation.Success || preparation.Query == null ||
            preparation.Zone == null)
        {
            return result;
        }

        DnsClockValue deadline = TransportTime.CreateDeadline(
            request.TransportOptions.TotalTimeoutMilliseconds,
            false,
            new DnsClockValue());
        DnsAxfrExchangeResult exchange = client.ExchangeProvider.ReceiveMessages(
            request,
            preparation.Query,
            preparation.QueryBytes,
            deadline);
        result.TransportStatus = exchange.Status;
        result.ByteCount = exchange.ByteCount;
        result.MessageCount = exchange.Messages.Count;
        TransferClientOperations.CopyMessages(
            exchange.Messages,
            result.Messages);
        TransferClientOperations.CopyDiagnostics(
            exchange.Diagnostics,
            result.Diagnostics);
        if (exchange.Status != DnsTransportStatus.Success)
        {
            return result;
        }

        if (exchange.Messages.Count == 0)
        {
            return ProtocolFailure(
                result,
                "transfer.messages",
                "An IXFR response must contain at least one message.");
        }

        if (exchange.Messages.Count > request.MaximumMessages ||
            exchange.ByteCount < 0 ||
            exchange.ByteCount > request.MaximumBytes)
        {
            result.TransportStatus = DnsTransportStatus.ResponseTooLarge;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "transfer.limits",
                "The IXFR response exceeded a configured transfer limit.");
            return result;
        }

        DnsTsigStreamState? tsigState = null;
        if (request.TsigKey != null)
        {
            tsigState = TsigStreamOperations.CreateState(
                request.TsigKey,
                preparation.RequestMac);
        }

        List<DnsResourceRecord> records =
            new List<DnsResourceRecord>();
        int messageIndex = 0;
        while (messageIndex < exchange.Messages.Count)
        {
            DnsMessage message = exchange.Messages[messageIndex];
            if (!TransferClientOperations.ValidateResponseMessage(
                result,
                preparation.Query,
                message,
                messageIndex))
            {
                return result;
            }

            if (!TransferClientOperations.AuthenticateResponse(
                result,
                request.TsigKey,
                tsigState,
                message))
            {
                return result;
            }

            int answerIndex = 0;
            while (answerIndex < message.Answers.Count)
            {
                DnsResourceRecord record = message.Answers[answerIndex];
                result.RecordCount = result.RecordCount + 1;
                if (result.RecordCount > request.MaximumRecords)
                {
                    result.TransportStatus =
                        DnsTransportStatus.ResponseTooLarge;
                    AddDiagnostic(
                        result,
                        DnsDiagnosticCode.ResourceLimitExceeded,
                        "transfer.maximumRecords",
                        "The IXFR response exceeded the record limit.");
                    return result;
                }

                if (!TransferClientOperations.ValidateTransferRecord(
                    result,
                    record,
                    preparation.Zone,
                    preparation.ClassValue))
                {
                    return result;
                }

                records.Add(record);
                answerIndex = answerIndex + 1;
            }

            messageIndex = messageIndex + 1;
        }

        if (tsigState != null)
        {
            DnsTsigResult completed = TsigStreamOperations.Complete(tsigState);
            result.Tsig = completed;
            TransferClientOperations.CopyDiagnostics(
                completed.Diagnostics,
                result.Diagnostics);
            if (!completed.Success || !completed.Authenticated ||
                completed.ErrorValue != 0)
            {
                result.TransportStatus =
                    DnsTransportStatus.AuthenticationFailed;
                return result;
            }
        }

        if (records.Count == 0 ||
            !OpeningSoaValid(records[0], preparation.Zone))
        {
            return ProtocolFailure(
                result,
                "transfer.openingSoa",
                "The first IXFR record must be a typed apex SOA.");
        }

        uint latestSerial = ReadSoaSerial(records[0]);
        DnsSerialComparison latestComparison =
            DnsSerialArithmetic.CompareSerials(
                latestSerial,
                request.CurrentSerial);
        if (!latestComparison.Defined || latestComparison.Comparison < 0)
        {
            return ProtocolFailure(
                result,
                "transfer.serial",
                "The IXFR server serial cannot precede the requested serial.");
        }

        result.HasInitialSerial = true;
        result.InitialSerial = request.CurrentSerial;
        result.HasFinalSerial = true;
        result.FinalSerial = latestSerial;
        if (latestComparison.Comparison == 0)
        {
            if (records.Count != 1)
            {
                return ProtocolFailure(
                    result,
                    "transfer.upToDate",
                    "An up-to-date IXFR response must contain only the current SOA.");
            }

            result.Success = true;
            result.UpToDate = true;
            result.ZoneName = RecordSetOperations.CopyName(
                preparation.Zone,
                false);
            return result;
        }

        if (records.Count < 2)
        {
            return ProtocolFailure(
                result,
                "transfer.sequence",
                "A changed IXFR response requires a delta or AXFR fallback.");
        }

        if (!TransferClientOperations.IsSoa(records[1]) ||
            TransferClientOperations.SoaRecordsEqual(
                records[0],
                records[1]))
        {
            return CompleteAxfrFallback(
                result,
                preparation,
                records,
                request.CurrentSerial,
                latestSerial);
        }

        return CompleteIxfrDeltas(
            result,
            preparation,
            records,
            request.CurrentSerial,
            latestSerial);
    }

    static DnsIxfrPreparation PrepareRequest(DnsTransferRequest request)
    {
        DnsIxfrPreparation result = new DnsIxfrPreparation();
        if (request.Kind != DnsTransferKind.Ixfr ||
            !request.HasCurrentSerial)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request.kind",
                "IXFR requires a supplied current SOA serial.");
            return result;
        }

        if (request.Transport != DnsTransportKind.Tcp)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request.transport",
                "IXFR requires DNS over TCP.");
            return result;
        }

        if (request.Endpoint == null || request.TransportOptions == null ||
            request.MaximumMessages < 1 || request.MaximumRecords < 1 ||
            request.MaximumBytes < DnsProtocolLimits.HeaderLength ||
            request.TransportOptions.TotalTimeoutMilliseconds < 0 ||
            request.TransportOptions.MaximumResponseLength < 1 ||
            request.TransportOptions.MaximumResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request.limits",
                "IXFR endpoints, timeouts, and limits must be valid.");
            return result;
        }

        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(request.Zone);
        if (classValue < 1 || classValue > 253 || !zone.Success ||
            zone.Name == null || !zone.Name.IsAbsolute)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.InvalidZone,
                "request.zone",
                "IXFR requires an absolute zone and ordinary DNS class.");
            return result;
        }

        DnsMessage query;
        if (request.Message == null)
        {
            query = CreateQuery(
                zone.Name,
                classValue,
                request.CurrentSerial);
        }
        else
        {
            query = request.Message;
            if (!ValidateQuery(
                result,
                query,
                zone.Name,
                classValue,
                request.CurrentSerial))
            {
                return result;
            }
        }

        if (request.TsigKey != null)
        {
            DnsTsigResult signed = TsigOperations.Sign(
                query,
                request.TsigKey,
                [],
                TransportTime.ReadClock());
            TransferClientOperations.CopyDiagnostics(
                signed.Diagnostics,
                result.Diagnostics);
            if (!signed.Success || signed.Message == null)
            {
                return result;
            }

            query = signed.Message;
            result.RequestMac = RecordSetOperations.CopyBytes(signed.Mac);
        }

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            query,
            new DnsEncodeOptions());
        TransferClientOperations.CopyDiagnostics(
            encoded.Diagnostics,
            result.Diagnostics);
        if (!encoded.Success || encoded.Message == null)
        {
            return result;
        }

        result.Success = true;
        result.Query = encoded.Message;
        result.QueryBytes = encoded.Bytes;
        result.Zone = RecordSetOperations.CopyName(zone.Name, false);
        result.ClassValue = classValue;
        return result;
    }

    static DnsMessage CreateQuery(
        DnsName zone,
        int classValue,
        uint serial)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = TransportEntropy.NextIdentifier();
        DnsQuestion question = new DnsQuestion();
        question.Name = RecordSetOperations.CopyName(zone, false);
        question.RecordType = DnsRecordType.Ixfr;
        question.RecordTypeValue = (int)DnsRecordType.Ixfr;
        question.Class = (DnsClass)classValue;
        question.ClassValue = classValue;
        result.Questions.Add(question);
        result.Authorities.Add(CreateSerialSoa(zone, classValue, serial));
        result.Header.QuestionCount = 1;
        result.Header.AuthorityCount = 1;
        return result;
    }

    static DnsResourceRecord CreateSerialSoa(
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

    static bool ValidateQuery(
        DnsIxfrPreparation result,
        DnsMessage query,
        DnsName zone,
        int classValue,
        uint serial)
    {
        if (!MessageCollectionsValid(query) || !SectionCountsValid(query) ||
            query.Questions.Count != 1 || query.Answers.Count != 0 ||
            query.Authorities.Count != 1 || query.Additionals.Count != 0)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.SectionCountMismatch,
                "request.message.sections",
                "An IXFR request requires one question and one authority SOA.");
            return false;
        }

        int flags = MessageOperations.EffectiveFlags(query.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            query.Header.OpcodeValue,
            (int)query.Header.Opcode);
        DnsQuestion question = query.Questions[0];
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int questionClass = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        DnsResourceRecord authority = query.Authorities[0];
        if ((flags & ((int)DnsHeaderFlag.Response |
            (int)DnsHeaderFlag.Truncated)) != 0 ||
            opcode != (int)DnsOpcode.Query ||
            MessageOperations.ReadEffectiveResponseCode(query) != 0 ||
            typeValue != (int)DnsRecordType.Ixfr ||
            questionClass != classValue ||
            !RecordSetOperations.NamesEqual(question.Name, zone) ||
            !OpeningSoaValid(authority, zone) ||
            ReadSoaSerial(authority) != serial)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "request.message",
                "The IXFR request message does not match its zone and current serial.");
            return false;
        }

        return true;
    }

    static DnsTransferResult CompleteAxfrFallback(
        DnsTransferResult result,
        DnsIxfrPreparation preparation,
        List<DnsResourceRecord> records,
        uint initialSerial,
        uint finalSerial)
    {
        if (preparation.Zone == null)
        {
            return ProtocolFailure(
                result,
                "transfer.fallback.zone",
                "The AXFR fallback requires a validated zone name.");
        }

        DnsName zoneName = preparation.Zone;
        DnsResourceRecord opening = records[0];
        if (records.Count < 2 ||
            !TransferClientOperations.SoaRecordsEqual(
                opening,
                records[records.Count - 1]))
        {
            return ProtocolFailure(
                result,
                "transfer.fallback.soaBoundary",
                "An AXFR fallback requires matching opening and closing SOA records.");
        }

        List<DnsResourceRecord> zoneRecords =
            new List<DnsResourceRecord>();
        zoneRecords.Add(opening);
        int index = 1;
        while (index < records.Count - 1)
        {
            if (TransferClientOperations.IsSoa(records[index]))
            {
                return ProtocolFailure(
                    result,
                    "transfer.fallback.records",
                    "An AXFR fallback may contain SOA only at its boundaries.");
            }

            zoneRecords.Add(records[index]);
            index = index + 1;
        }

        DnsZone zone = TransferClientOperations.CreateZone(
            zoneName,
            preparation.ClassValue,
            zoneRecords);
        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            TransferClientOperations.CopyZoneDiagnostics(
                validation.Diagnostics,
                result.Diagnostics);
            return ProtocolFailure(
                result,
                "transfer.fallback.zone",
                "The AXFR fallback did not produce a valid complete zone.");
        }

        result.Success = true;
        result.FellBackToAxfr = true;
        result.InitialSerial = initialSerial;
        result.FinalSerial = finalSerial;
        result.ZoneName = RecordSetOperations.CopyName(
            zoneName,
            false);
        result.Zone = zone;
        return result;
    }

    static DnsTransferResult CompleteIxfrDeltas(
        DnsTransferResult result,
        DnsIxfrPreparation preparation,
        List<DnsResourceRecord> records,
        uint initialSerial,
        uint finalSerial)
    {
        if (preparation.Zone == null)
        {
            return ProtocolFailure(
                result,
                "transfer.delta.zone",
                "IXFR deltas require a validated zone name.");
        }

        DnsName zoneName = preparation.Zone;
        List<DnsZoneJournalEntry> entries =
            new List<DnsZoneJournalEntry>();
        int phase = ExpectOldSoa;
        uint expectedFrom = initialSerial;
        DnsZoneJournalEntry? current = null;
        int index = 1;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            bool finalRecord = index == records.Count - 1;
            if (finalRecord)
            {
                if (phase != ReadingAdds || current == null ||
                    !TransferClientOperations.SoaRecordsEqual(
                        records[0],
                        record) || current.ToSerial != finalSerial)
                {
                    return ProtocolFailure(
                        result,
                        "transfer.delta.finalSoa",
                        "IXFR deltas must end with the opening current SOA.");
                }

                CommitEntry(current, entries);
                current = null;
            }
            else if (phase == ExpectOldSoa)
            {
                if (!OpeningSoaValid(record, zoneName) ||
                    ReadSoaSerial(record) != expectedFrom)
                {
                    return ProtocolFailure(
                        result,
                        "transfer.delta.oldSoa",
                        "Each IXFR delta must begin with the expected old SOA serial.");
                }

                current = CreateEntry(expectedFrom, entries.Count + 1);
                phase = ReadingDeletes;
            }
            else if (phase == ReadingDeletes)
            {
                if (TransferClientOperations.IsSoa(record))
                {
                    if (current == null ||
                        !OpeningSoaValid(record, zoneName))
                    {
                        return ProtocolFailure(
                            result,
                            "transfer.delta.newSoa",
                            "Each IXFR delete sequence must end with a typed new SOA.");
                    }

                    uint newSerial = ReadSoaSerial(record);
                    DnsSerialComparison comparison =
                        DnsSerialArithmetic.CompareSerials(
                            newSerial,
                            current.FromSerial);
                    if (!comparison.Defined || comparison.Comparison != 1)
                    {
                        return ProtocolFailure(
                            result,
                            "transfer.delta.serial",
                            "Each IXFR delta must advance its SOA serial.");
                    }

                    SetEntryResult(current, newSerial);
                    expectedFrom = newSerial;
                    phase = ReadingAdds;
                }
                else
                {
                    if (current == null)
                    {
                        return ProtocolFailure(
                            result,
                            "transfer.delta.delete",
                            "An IXFR delete record requires an active delta.");
                    }

                    current.Transaction.Changes.Add(CreateChange(
                        record,
                        DnsZoneChangeKind.DeleteRecord));
                }
            }
            else if (TransferClientOperations.IsSoa(record))
            {
                if (current == null ||
                    !OpeningSoaValid(record, zoneName) ||
                    ReadSoaSerial(record) != expectedFrom)
                {
                    return ProtocolFailure(
                        result,
                        "transfer.delta.chain",
                        "IXFR delta serial transitions must form one continuous chain.");
                }

                CommitEntry(current, entries);
                current = CreateEntry(expectedFrom, entries.Count + 1);
                phase = ReadingDeletes;
            }
            else
            {
                if (current == null)
                {
                    return ProtocolFailure(
                        result,
                        "transfer.delta.add",
                        "An IXFR add record requires an active delta.");
                }

                current.Transaction.Changes.Add(CreateChange(
                    record,
                    DnsZoneChangeKind.Add));
            }

            index = index + 1;
        }

        if (current != null || entries.Count == 0 ||
            entries[entries.Count - 1].ToSerial != finalSerial)
        {
            return ProtocolFailure(
                result,
                "transfer.delta.sequence",
                "The IXFR response ended with an incomplete serial transition.");
        }

        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            result.JournalEntries.Add(entries[entryIndex]);
            entryIndex = entryIndex + 1;
        }

        result.Success = true;
        result.InitialSerial = initialSerial;
        result.FinalSerial = finalSerial;
        result.ZoneName = RecordSetOperations.CopyName(
            zoneName,
            false);
        return result;
    }

    static DnsZoneJournalEntry CreateEntry(uint serial, int sequence)
    {
        DnsZoneJournalEntry result = new DnsZoneJournalEntry();
        result.Sequence = sequence;
        result.FromSerial = serial;
        result.Transaction.Atomic = true;
        result.Transaction.HasExpectedSerial = true;
        result.Transaction.ExpectedSerial = serial;
        return result;
    }

    static void SetEntryResult(
        DnsZoneJournalEntry entry,
        uint serial)
    {
        entry.ToSerial = serial;
        entry.Transaction.HasResultingSerial = true;
        entry.Transaction.ResultingSerial = serial;
    }

    static void CommitEntry(
        DnsZoneJournalEntry entry,
        List<DnsZoneJournalEntry> entries)
    {
        entry.Transaction.Committed = true;
        entries.Add(entry);
    }

    static DnsZoneChange CreateChange(
        DnsResourceRecord record,
        DnsZoneChangeKind kind)
    {
        DnsZoneChange result = new DnsZoneChange();
        result.Kind = kind;
        result.Name = RecordSetOperations.CopyName(record.Name, false);
        result.RecordType = record.RecordType;
        result.RecordTypeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        result.Class = record.Class;
        result.ClassValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        result.Record = DnsZoneSnapshotOperations.CopyRecord(record);
        return result;
    }

    static bool OpeningSoaValid(
        DnsResourceRecord record,
        DnsName zone)
    {
        return TransferClientOperations.IsTypedSoa(record) &&
            RecordSetOperations.NamesEqual(record.Name, zone);
    }

    static uint ReadSoaSerial(DnsResourceRecord record)
    {
        if (record.Data == null || record.Data.Soa == null)
        {
            return 0;
        }

        return record.Data.Soa.Serial;
    }

    static DnsTransferResult CreateResult()
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = DnsTransferKind.Ixfr;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        return result;
    }

    static DnsTransferResult ProtocolFailure(
        DnsTransferResult result,
        string field,
        string reason)
    {
        result.Success = false;
        result.Zone = null;
        result.JournalEntries.Clear();
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        AddDiagnostic(
            result,
            DnsDiagnosticCode.ResponseMismatch,
            field,
            reason);
        return result;
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

    static void AddDiagnostic(
        DnsTransferResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void AddPreparationDiagnostic(
        DnsIxfrPreparation result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }
}
