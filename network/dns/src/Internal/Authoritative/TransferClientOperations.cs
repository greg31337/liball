using System.Collections.Generic;

namespace dns;

internal class DnsAxfrPreparation
{
    internal bool Success;
    internal DnsMessage? Query;
    internal byte[] QueryBytes = [];
    internal byte[] RequestMac = [];
    internal DnsName? Zone;
    internal int ClassValue;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class TransferClientOperations
{
    internal static DnsTransferResult ReceiveTransfer(
        DnsTransferRequest request,
        DnsTransferClient client)
    {
        if (request != null && request.Kind == DnsTransferKind.Ixfr)
        {
            return IxfrClientOperations.ReceiveTransfer(request, client);
        }

        DnsTransferResult result = CreateResult();
        if (request == null || client == null)
        {
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A complete AXFR request and client are required.");
            return result;
        }

        result.Kind = request.Kind;
        if (request.CancellationRequested)
        {
            result.TransportStatus = DnsTransportStatus.Cancelled;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "The AXFR operation was cancelled.");
            return result;
        }

        DnsAxfrPreparation preparation = PrepareRequest(request);
        CopyDiagnostics(preparation.Diagnostics, result.Diagnostics);
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
        CopyDiagnostics(exchange.Diagnostics, result.Diagnostics);
        CopyMessages(exchange.Messages, result.Messages);
        result.MessageCount = exchange.Messages.Count;
        if (exchange.Status != DnsTransportStatus.Success)
        {
            return result;
        }

        if (exchange.Messages.Count == 0)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "transfer.messages",
                "An AXFR response must contain at least one message.");
            return result;
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
                "The AXFR response exceeded a configured transfer limit.");
            return result;
        }

        DnsTsigStreamState? tsigState = null;
        if (request.TsigKey != null)
        {
            tsigState = TsigStreamOperations.CreateState(
                request.TsigKey,
                preparation.RequestMac);
        }

        List<DnsResourceRecord> zoneRecords =
            new List<DnsResourceRecord>();
        DnsResourceRecord? openingSoa = null;
        bool closed = false;
        int totalRecords = 0;
        int messageIndex = 0;
        while (messageIndex < exchange.Messages.Count)
        {
            DnsMessage message = exchange.Messages[messageIndex];
            if (!ValidateResponseMessage(
                result,
                preparation.Query,
                message,
                messageIndex))
            {
                return result;
            }

            if (!AuthenticateResponse(
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
                totalRecords = totalRecords + 1;
                result.RecordCount = totalRecords;
                if (totalRecords > request.MaximumRecords)
                {
                    result.TransportStatus = DnsTransportStatus.ResponseTooLarge;
                    AddDiagnostic(
                        result,
                        DnsDiagnosticCode.ResourceLimitExceeded,
                        "transfer.maximumRecords",
                        "The AXFR response exceeded the record limit.");
                    return result;
                }

                if (!ValidateTransferRecord(
                    result,
                    record,
                    preparation.Zone,
                    preparation.ClassValue))
                {
                    return result;
                }

                if (openingSoa == null)
                {
                    if (!IsTypedSoa(record) || record.Data == null ||
                        record.Data.Soa == null ||
                        !RecordSetOperations.NamesEqual(
                            record.Name,
                            preparation.Zone))
                    {
                        result.TransportStatus = DnsTransportStatus.ProtocolError;
                        AddDiagnostic(
                            result,
                            DnsDiagnosticCode.InvalidZone,
                            "transfer.openingSoa",
                            "The first AXFR record must be a typed apex SOA.");
                        return result;
                    }

                    openingSoa = record;
                    zoneRecords.Add(record);
                    DnsRecordData openingRecordData = record.Data;
                    DnsSoaData openingData = openingRecordData.Soa;
                    result.HasInitialSerial = true;
                    result.InitialSerial = openingData.Serial;
                }
                else if (IsSoa(record))
                {
                    if (!SoaRecordsEqual(openingSoa, record) ||
                        record.Data == null || record.Data.Soa == null ||
                        answerIndex != message.Answers.Count - 1 ||
                        messageIndex != exchange.Messages.Count - 1)
                    {
                        result.TransportStatus = DnsTransportStatus.ProtocolError;
                        AddDiagnostic(
                            result,
                            DnsDiagnosticCode.InvalidZone,
                            "transfer.closingSoa",
                            "The final AXFR record must repeat the opening SOA exactly.");
                        return result;
                    }

                    closed = true;
                    DnsRecordData closingRecordData = record.Data;
                    DnsSoaData closingData = closingRecordData.Soa;
                    result.HasFinalSerial = true;
                    result.FinalSerial = closingData.Serial;
                }
                else
                {
                    if (closed)
                    {
                        result.TransportStatus = DnsTransportStatus.ProtocolError;
                        AddDiagnostic(
                            result,
                            DnsDiagnosticCode.InvalidZone,
                            "transfer.records",
                            "No AXFR records may follow the closing SOA.");
                        return result;
                    }

                    zoneRecords.Add(record);
                }

                answerIndex = answerIndex + 1;
            }

            messageIndex = messageIndex + 1;
        }

        if (!closed || openingSoa == null)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.InvalidZone,
                "transfer.soaBoundary",
                "The AXFR response did not contain matching SOA boundaries.");
            return result;
        }

        if (tsigState != null)
        {
            DnsTsigResult completed = TsigStreamOperations.Complete(tsigState);
            result.Tsig = completed;
            CopyDiagnostics(completed.Diagnostics, result.Diagnostics);
            if (!completed.Success || !completed.Authenticated ||
                completed.ErrorValue != 0)
            {
                result.TransportStatus = DnsTransportStatus.AuthenticationFailed;
                return result;
            }
        }

        DnsZone zone = CreateZone(
            preparation.Zone,
            preparation.ClassValue,
            zoneRecords);
        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            CopyZoneDiagnostics(validation.Diagnostics, result.Diagnostics);
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            return result;
        }

        result.ZoneName = RecordSetOperations.CopyName(
            preparation.Zone,
            false);
        result.Zone = zone;
        result.ResponseCode = DnsResponseCode.NoError;
        result.ResponseCodeValue = 0;
        result.Success = true;
        return result;
    }

    static DnsAxfrPreparation PrepareRequest(DnsTransferRequest request)
    {
        DnsAxfrPreparation result = new DnsAxfrPreparation();
        if (request.Kind != DnsTransferKind.Axfr)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request.kind",
                "This client receive state accepts AXFR requests only.");
            return result;
        }

        if (request.Transport != DnsTransportKind.Tcp)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "request.transport",
                "AXFR requires DNS over TCP.");
            return result;
        }

        if (request.Endpoint == null || request.TransportOptions == null ||
            request.MaximumMessages < 1 || request.MaximumRecords < 2 ||
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
                "AXFR endpoints, timeouts, and resource limits must be valid.");
            return result;
        }

        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(request.Zone);
        if (classValue < 1 || classValue > 253 ||
            !zone.Success || zone.Name == null || !zone.Name.IsAbsolute)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.InvalidZone,
                "request.zone",
                "AXFR requires an absolute zone and ordinary DNS class.");
            return result;
        }

        DnsMessage query;
        if (request.Message == null)
        {
            query = CreateQuery(zone.Name, classValue);
        }
        else
        {
            query = request.Message;
            if (!ValidateQuery(result, query, zone.Name, classValue))
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
            CopyDiagnostics(signed.Diagnostics, result.Diagnostics);
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
        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
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

    static DnsMessage CreateQuery(DnsName zone, int classValue)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = TransportEntropy.NextIdentifier();
        result.Header.Opcode = DnsOpcode.Query;
        result.Header.OpcodeValue = (int)DnsOpcode.Query;
        DnsQuestion question = new DnsQuestion();
        question.Name = RecordSetOperations.CopyName(zone, false);
        question.RecordType = DnsRecordType.Axfr;
        question.RecordTypeValue = (int)DnsRecordType.Axfr;
        question.Class = (DnsClass)classValue;
        question.ClassValue = classValue;
        result.Questions.Add(question);
        result.Header.QuestionCount = 1;
        return result;
    }

    static bool ValidateQuery(
        DnsAxfrPreparation result,
        DnsMessage query,
        DnsName zone,
        int classValue)
    {
        if (!MessageCollectionsValid(query) || !SectionCountsValid(query) ||
            query.Questions.Count != 1 || query.Answers.Count != 0 ||
            query.Authorities.Count != 0 || query.Additionals.Count != 0)
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.SectionCountMismatch,
                "request.message.sections",
                "An AXFR request requires one question and no resource records.");
            return false;
        }

        int flags = MessageOperations.EffectiveFlags(query.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            query.Header.OpcodeValue,
            (int)query.Header.Opcode);
        if ((flags & ((int)DnsHeaderFlag.Response |
            (int)DnsHeaderFlag.Truncated)) != 0 ||
            opcode != (int)DnsOpcode.Query ||
            MessageOperations.ReadEffectiveResponseCode(query) != 0 ||
            !QuestionMatches(query.Questions[0], zone, classValue))
        {
            AddPreparationDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "request.message",
                "The AXFR request message does not match its zone question.");
            return false;
        }

        return true;
    }

    internal static bool ValidateResponseMessage(
        DnsTransferResult result,
        DnsMessage query,
        DnsMessage message,
        int messageIndex)
    {
        if (!MessageCollectionsValid(message) || !SectionCountsValid(message) ||
            message.Answers.Count == 0 || message.Authorities.Count != 0)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.SectionCountMismatch,
                "transfer.message.sections",
                "Every AXFR message requires consistent, nonempty answer sections.");
            return false;
        }

        int flags = MessageOperations.EffectiveFlags(message.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            message.Header.OpcodeValue,
            (int)message.Header.Opcode);
        int responseCode = MessageOperations.ReadEffectiveResponseCode(message);
        result.ResponseCode = (DnsResponseCode)responseCode;
        result.ResponseCodeValue = responseCode;
        if (message.Header.Identifier != query.Header.Identifier ||
            (flags & (int)DnsHeaderFlag.Response) == 0 ||
            (flags & (int)DnsHeaderFlag.AuthoritativeAnswer) == 0 ||
            (flags & (int)DnsHeaderFlag.Truncated) != 0 ||
            opcode != (int)DnsOpcode.Query || responseCode != 0)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "transfer.message.header",
                "An AXFR response header must match the query and report no error.");
            return false;
        }

        if ((messageIndex == 0 &&
             !MessageOperations.QuestionsEqual(
                 query.Questions,
                 message.Questions)) ||
            (messageIndex > 0 && message.Questions.Count > 1) ||
            (messageIndex > 0 && message.Questions.Count == 1 &&
             !MessageOperations.QuestionsEqual(
                 query.Questions,
                 message.Questions)))
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "transfer.message.question",
                "The AXFR response question does not match the request.");
            return false;
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success ||
            message.Additionals.Count > (placement.HasTsig ? 1 : 0))
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            CopyDiagnostics(placement.Diagnostics, result.Diagnostics);
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "transfer.message.additional",
                "AXFR messages may contain only a final TSIG additional record.");
            return false;
        }

        return true;
    }

    internal static bool AuthenticateResponse(
        DnsTransferResult result,
        DnsTsigKey? key,
        DnsTsigStreamState? state,
        DnsMessage message)
    {
        if (key == null || state == null)
        {
            TsigPlacementResult placement =
                TransactionRecordDataCodec.InspectTsigPlacement(message);
            if (!placement.Success || placement.HasTsig)
            {
                result.TransportStatus =
                    DnsTransportStatus.AuthenticationFailed;
                CopyDiagnostics(placement.Diagnostics, result.Diagnostics);
                AddDiagnostic(
                    result,
                    DnsDiagnosticCode.InvalidTsigPlacement,
                    "transfer.tsig",
                    "An unsigned AXFR request cannot accept signed responses.");
                return false;
            }

            return true;
        }

        DnsTsigResult verification = TsigStreamOperations.VerifyNext(
            state,
            message,
            TransportTime.ReadClock());
        result.Tsig = verification;
        CopyDiagnostics(verification.Diagnostics, result.Diagnostics);
        if (!verification.Success || verification.ErrorValue != 0)
        {
            result.TransportStatus = DnsTransportStatus.AuthenticationFailed;
            return false;
        }

        return true;
    }

    internal static bool ValidateTransferRecord(
        DnsTransferResult result,
        DnsResourceRecord record,
        DnsName zone,
        int classValue)
    {
        if (record == null)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "transfer.record",
                "AXFR answer sections cannot contain null records.");
            return false;
        }

        int recordClass = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        DnsNameOperationResult suffix = DnsNameOperations.HasNameSuffix(
            record.Name,
            zone);
        if (recordClass != classValue || !suffix.Success ||
            !suffix.BooleanValue)
        {
            result.TransportStatus = DnsTransportStatus.ProtocolError;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.InvalidZone,
                "transfer.record.owner",
                "Every AXFR record must use the zone class and be in-zone.");
            return false;
        }

        return true;
    }

    internal static bool IsSoa(DnsResourceRecord record)
    {
        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        return typeValue == (int)DnsRecordType.Soa;
    }

    internal static bool IsTypedSoa(DnsResourceRecord record)
    {
        return IsSoa(record) && record.Data != null &&
            record.Data.Soa != null && !record.UseRawData;
    }

    internal static bool SoaRecordsEqual(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        if (!IsTypedSoa(left) || !IsTypedSoa(right) ||
            left.Data == null || left.Data.Soa == null ||
            right.Data == null || right.Data.Soa == null ||
            !RecordSetOperations.NamesEqual(left.Name, right.Name) ||
            DnsHeaderCodec.ResolveCode(
                left.ClassValue,
                (int)left.Class) !=
            DnsHeaderCodec.ResolveCode(
                right.ClassValue,
                (int)right.Class) ||
            left.TimeToLive != right.TimeToLive)
        {
            return false;
        }

        DnsRecordData leftRecordData = left.Data;
        DnsRecordData rightRecordData = right.Data;
        DnsSoaData leftData = leftRecordData.Soa;
        DnsSoaData rightData = rightRecordData.Soa;
        return RecordSetOperations.NamesEqual(
                leftData.PrimaryNameServer,
                rightData.PrimaryNameServer) &&
            RecordSetOperations.NamesEqual(
                leftData.ResponsibleMailbox,
                rightData.ResponsibleMailbox) &&
            leftData.Serial == rightData.Serial &&
            leftData.Refresh == rightData.Refresh &&
            leftData.Retry == rightData.Retry &&
            leftData.Expire == rightData.Expire &&
            leftData.Minimum == rightData.Minimum;
    }

    internal static DnsZone CreateZone(
        DnsName apex,
        int classValue,
        List<DnsResourceRecord> records)
    {
        DnsZone result = new DnsZone();
        result.Apex = RecordSetOperations.CopyName(apex, false);
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        List<DnsResourceRecordSet> sets =
            RecordSetOperations.GroupRecordSets(records);
        int setIndex = 0;
        while (setIndex < sets.Count)
        {
            DnsResourceRecordSet set = sets[setIndex];
            DnsZoneNode? node = null;
            int nodeIndex = 0;
            while (nodeIndex < result.Nodes.Count && node == null)
            {
                if (RecordSetOperations.NamesEqual(
                    result.Nodes[nodeIndex].Name,
                    set.Name))
                {
                    node = result.Nodes[nodeIndex];
                }

                nodeIndex = nodeIndex + 1;
            }

            if (node == null)
            {
                node = new DnsZoneNode();
                node.Name = RecordSetOperations.CopyName(set.Name, false);
                result.Nodes.Add(node);
            }

            node.RecordSets.Add(set);
            setIndex = setIndex + 1;
        }

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

    static bool QuestionMatches(
        DnsQuestion question,
        DnsName zone,
        int classValue)
    {
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int questionClass = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        return typeValue == (int)DnsRecordType.Axfr &&
            questionClass == classValue &&
            RecordSetOperations.NamesEqual(question.Name, zone);
    }

    static DnsTransferResult CreateResult()
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = DnsTransferKind.Axfr;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        return result;
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
        DnsAxfrPreparation result,
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

    internal static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CopyMessages(
        List<DnsMessage> source,
        List<DnsMessage> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CopyZoneDiagnostics(
        List<DnsZoneDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = source[index].Code;
            diagnostic.Field = "transfer.zone";
            diagnostic.Reason = source[index].Reason;
            destination.Add(diagnostic);
            index = index + 1;
        }
    }
}
