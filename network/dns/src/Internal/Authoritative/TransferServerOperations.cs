using System.Collections.Generic;

namespace dns;

internal class DnsTransferServerRequest
{
    internal bool Success;
    internal DnsTransferRequest? Request;
    internal DnsMessage? Query;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class DnsTransferAuthentication
{
    internal bool Present;
    internal bool Accepted = true;
    internal DnsTsigResult? Tsig;
}

internal class DnsTransferMessageEncoding
{
    internal bool Fits;
    internal DnsMessage? Message;
    internal byte[] Bytes = [];
    internal DnsTsigResult? Tsig;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class TransferServerOperations
{
    internal static DnsTransferResult ServeTransfer(
        DnsTransferRequest request,
        IDnsZoneStore zoneStore,
        DnsAuthorizationRateState authorizationRateState)
    {
        if (request != null && request.Kind == DnsTransferKind.Ixfr)
        {
            return IxfrServerOperations.ServeTransfer(
                request,
                zoneStore,
                authorizationRateState);
        }

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
                "A complete AXFR request and zone store are required.");
        }

        if (request.CancellationRequested)
        {
            result.TransportStatus = DnsTransportStatus.Cancelled;
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "AXFR serving was cancelled."));
            return result;
        }

        DnsTransferServerRequest prepared = PrepareRequest(request);
        CopyDiagnostics(prepared.Diagnostics, result.Diagnostics);
        if (!prepared.Success || prepared.Request == null ||
            prepared.Query == null)
        {
            return result;
        }

        DnsTransferRequest effective = prepared.Request;
        DnsMessage query = prepared.Query;
        DnsClockValue now = TransportTime.ReadClock();
        DnsTransferAuthentication authentication =
            AuthenticateRequest(effective, query, now);
        result.Tsig = authentication.Tsig;
        if (!authentication.Accepted)
        {
            DnsTransferResult rejected = ProtocolResult(
                effective,
                query,
                DnsResponseCode.NotAuthoritative,
                AuthenticationDiagnostics(authentication),
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
                reason = "A rate-limited AXFR cannot be served partially.";
            }

            return ProtocolResult(
                effective,
                query,
                DnsResponseCode.Refused,
                DiagnosticList(
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
            return ProtocolResult(
                effective,
                query,
                DnsResponseCode.NotAuthoritative,
                DiagnosticList(
                    DnsDiagnosticCode.InvalidZone,
                    "request.zone",
                    "The requested AXFR zone is not hosted."),
                authentication,
                now);
        }

        DnsZoneValidationResult validation =
            DnsZoneValidator.ValidateZone(zone);
        if (!validation.Valid)
        {
            result.Diagnostics.Clear();
            CopyZoneDiagnostics(validation.Diagnostics, result.Diagnostics);
            result.Tsig = authentication.Tsig;
            return result;
        }

        List<DnsResourceRecord> records = CreateAxfrRecords(zone);
        if (records.Count > effective.MaximumRecords)
        {
            DnsTransferResult limited = FailureResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.maximumRecords",
                "The complete AXFR exceeds the configured record limit.");
            limited.TransportStatus = DnsTransportStatus.ResponseTooLarge;
            limited.Tsig = authentication.Tsig;
            return limited;
        }

        DnsTsigStreamState? streamState = null;
        if (authentication.Present && authentication.Tsig != null &&
            effective.TsigKey != null)
        {
            streamState = TsigStreamOperations.CreateState(
                effective.TsigKey,
                authentication.Tsig.Mac);
        }

        DnsTransferResult streamed = StreamRecords(
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

        if (streamState != null)
        {
            DnsTsigResult completed = TsigStreamOperations.Complete(
                streamState);
            streamed.Tsig = completed;
            CopyDiagnostics(completed.Diagnostics, streamed.Diagnostics);
            if (!completed.Success || !completed.Authenticated)
            {
                streamed.Success = false;
                streamed.Messages.Clear();
                streamed.MessageCount = 0;
                streamed.ByteCount = 0;
                streamed.TransportStatus =
                    DnsTransportStatus.AuthenticationFailed;
                return streamed;
            }
        }
        else
        {
            streamed.Tsig = authentication.Tsig;
        }

        streamed.Zone = DnsZoneSnapshotOperations.CopyZone(zone);
        streamed.ZoneName = RecordSetOperations.CopyName(zone.Apex, false);
        return streamed;
    }

    static DnsTransferServerRequest PrepareRequest(
        DnsTransferRequest request)
    {
        DnsTransferServerRequest result = new DnsTransferServerRequest();
        if (request.Kind != DnsTransferKind.Axfr ||
            request.Transport != DnsTransportKind.Tcp)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.kind",
                "The AXFR service accepts full transfers over TCP only."));
            return result;
        }

        if (request.Endpoint == null || request.TransportOptions == null ||
            request.Authorization == null || request.MaximumMessages < 1 ||
            request.MaximumRecords < 2 ||
            request.MaximumBytes < DnsProtocolLimits.HeaderLength ||
            request.TransportOptions.MaximumResponseLength <
                DnsProtocolLimits.HeaderLength ||
            request.TransportOptions.MaximumResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.limits",
                "AXFR context and response limits must be complete and bounded."));
            return result;
        }

        DnsMessage query;
        DnsName zone;
        int classValue;
        if (request.Message == null)
        {
            RawDnsNameResult validated = RawDnsName.ValidateName(request.Zone);
            classValue = DnsHeaderCodec.ResolveCode(
                request.ClassValue,
                (int)request.Class);
            if (!validated.Success || validated.Name == null ||
                !validated.Name.IsAbsolute || classValue < 1 ||
                classValue > 253)
            {
                result.Diagnostics.Add(CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "request.zone",
                    "AXFR requires an absolute zone and ordinary DNS class."));
                return result;
            }

            zone = validated.Name;
            query = CreateQuery(zone, classValue);
        }
        else
        {
            DnsTransferServerRequest parsed = ParseQuery(request.Message);
            if (!parsed.Success || parsed.Request == null ||
                parsed.Query == null)
            {
                return parsed;
            }

            zone = parsed.Request.Zone;
            classValue = parsed.Request.ClassValue;
            query = parsed.Query;
        }

        DnsTransferRequest effective = CopyRequestContext(
            request,
            zone,
            classValue,
            query);
        result.Success = true;
        result.Request = effective;
        result.Query = query;
        return result;
    }

    static DnsTransferServerRequest ParseQuery(DnsMessage query)
    {
        DnsTransferServerRequest result = new DnsTransferServerRequest();
        if (!MessageCollectionsValid(query) || !SectionCountsValid(query) ||
            query.Questions.Count != 1 || query.Answers.Count != 0 ||
            query.Authorities.Count != 0)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.SectionCountMismatch,
                "request.message.sections",
                "An AXFR request requires one question and no answer or authority records."));
            return result;
        }

        int flags = MessageOperations.EffectiveFlags(query.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            query.Header.OpcodeValue,
            (int)query.Header.Opcode);
        if (flags != 0 || opcode != (int)DnsOpcode.Query ||
            MessageOperations.ReadEffectiveResponseCode(query) != 0)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.message.header",
                "An AXFR request requires QUERY opcode and zero header flags and response code."));
            return result;
        }

        DnsQuestion question = query.Questions[0];
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(question.Name);
        if (typeValue != (int)DnsRecordType.Axfr || classValue < 1 ||
            classValue > 253 || !zone.Success || zone.Name == null ||
            !zone.Name.IsAbsolute)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.InvalidZone,
                "request.message.question",
                "The AXFR question requires an absolute zone and ordinary class."));
            return result;
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(query);
        if (!placement.Success ||
            query.Additionals.Count > (placement.HasTsig ? 1 : 0))
        {
            CopyDiagnostics(placement.Diagnostics, result.Diagnostics);
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.InvalidTsigPlacement,
                "request.message.additional",
                "An AXFR request may contain only a final TSIG additional record."));
            return result;
        }

        DnsTransferRequest parsed = new DnsTransferRequest();
        parsed.Zone = RecordSetOperations.CopyName(zone.Name, false);
        parsed.Class = (DnsClass)classValue;
        parsed.ClassValue = classValue;
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
        DnsMessage query)
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Kind = DnsTransferKind.Axfr;
        result.Zone = RecordSetOperations.CopyName(zone, false);
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
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

    internal static DnsTransferAuthentication AuthenticateRequest(
        DnsTransferRequest request,
        DnsMessage query,
        DnsClockValue now)
    {
        DnsTransferAuthentication result =
            new DnsTransferAuthentication();
        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(query);
        if (!placement.Success)
        {
            result.Accepted = false;
            DnsTsigResult invalid = new DnsTsigResult();
            CopyDiagnostics(placement.Diagnostics, invalid.Diagnostics);
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
            DnsTsigResult missing = new DnsTsigResult();
            missing.Success = true;
            missing.Error = DnsTsigError.BadKey;
            missing.ErrorValue = (int)DnsTsigError.BadKey;
            missing.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.tsigKey",
                "No key is available for the signed AXFR request."));
            result.Accepted = false;
            result.Tsig = missing;
            return result;
        }

        DnsTsigResult verified = TsigOperations.Verify(
            query,
            request.TsigKey,
            [],
            now);
        result.Tsig = verified;
        result.Accepted = verified.Success && verified.Authenticated &&
            verified.ErrorValue == 0;
        return result;
    }

    internal static DnsTransferResult StreamRecords(
        DnsTransferRequest request,
        DnsMessage query,
        DnsZone zone,
        List<DnsResourceRecord> records,
        DnsTsigStreamState? streamState,
        DnsClockValue now)
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = request.Kind;
        result.ResponseCode = DnsResponseCode.NoError;
        result.ResponseCodeValue = 0;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        List<DnsMessage> messages = new List<DnsMessage>();
        long totalBytes = 0;
        int recordIndex = 0;
        while (recordIndex < records.Count)
        {
            if (messages.Count >= request.MaximumMessages)
            {
                return StreamFailure(
                    result,
                    DnsTransportStatus.ResponseTooLarge,
                    "request.maximumMessages",
                    "The AXFR requires more than the permitted number of messages.");
            }

            DnsMessage message = CreateResponse(
                query,
                DnsResponseCode.NoError,
                messages.Count == 0);
            int accepted = 0;
            while (recordIndex < records.Count)
            {
                message.Answers.Add(records[recordIndex]);
                message.Header.AnswerCount = message.Answers.Count;
                DnsTransferMessageEncoding candidate = EncodeCandidate(
                    message,
                    streamState,
                    now,
                    request.TransportOptions.MaximumResponseLength);
                if (!candidate.Fits)
                {
                    message.Answers.RemoveAt(message.Answers.Count - 1);
                    message.Header.AnswerCount = message.Answers.Count;
                    if (accepted == 0)
                    {
                        CopyDiagnostics(
                            candidate.Diagnostics,
                            result.Diagnostics);
                        return StreamFailure(
                            result,
                            DnsTransportStatus.ResponseTooLarge,
                            "request.maximumResponseLength",
                            "One AXFR record cannot fit in a response message.");
                    }

                    break;
                }

                accepted = accepted + 1;
                recordIndex = recordIndex + 1;
            }

            DnsTransferMessageEncoding finalized = FinalizeMessage(
                message,
                streamState,
                now,
                request.TransportOptions.MaximumResponseLength);
            CopyDiagnostics(finalized.Diagnostics, result.Diagnostics);
            if (!finalized.Fits || finalized.Message == null)
            {
                return StreamFailure(
                    result,
                    DnsTransportStatus.ProtocolError,
                    "transfer.encoding",
                    "The finalized AXFR message could not be encoded.");
            }

            if (totalBytes > request.MaximumBytes - finalized.Bytes.Length)
            {
                return StreamFailure(
                    result,
                    DnsTransportStatus.ResponseTooLarge,
                    "request.maximumBytes",
                    "The encoded AXFR exceeds the total wire-octet limit.");
            }

            messages.Add(finalized.Message);
            totalBytes = totalBytes + finalized.Bytes.Length;
        }

        CopyMessages(messages, result.Messages);
        result.MessageCount = messages.Count;
        result.RecordCount = records.Count;
        result.ByteCount = totalBytes;
        result.ZoneName = RecordSetOperations.CopyName(zone.Apex, false);
        DnsResourceRecord opening = records[0];
        if (opening.Data != null && opening.Data.Soa != null)
        {
            result.HasInitialSerial = true;
            result.InitialSerial = opening.Data.Soa.Serial;
            result.HasFinalSerial = true;
            result.FinalSerial = opening.Data.Soa.Serial;
        }

        result.Success = true;
        result.TransportStatus = DnsTransportStatus.Success;
        return result;
    }

    internal static DnsTransferMessageEncoding EncodeCandidate(
        DnsMessage message,
        DnsTsigStreamState? streamState,
        DnsClockValue now,
        int maximumLength)
    {
        if (streamState == null)
        {
            return EncodeUnsigned(message, maximumLength);
        }

        DnsTsigStreamState copy = CopyStreamState(streamState);
        return EncodeSigned(message, copy, now, maximumLength);
    }

    internal static DnsTransferMessageEncoding FinalizeMessage(
        DnsMessage message,
        DnsTsigStreamState? streamState,
        DnsClockValue now,
        int maximumLength)
    {
        if (streamState == null)
        {
            return EncodeUnsigned(message, maximumLength);
        }

        return EncodeSigned(message, streamState, now, maximumLength);
    }

    static DnsTransferMessageEncoding EncodeUnsigned(
        DnsMessage message,
        int maximumLength)
    {
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength = maximumLength;
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message,
            options);
        DnsTransferMessageEncoding result =
            new DnsTransferMessageEncoding();
        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
        if (encoded.Success && encoded.Message != null)
        {
            result.Fits = true;
            result.Message = encoded.Message;
            result.Bytes = encoded.Bytes;
        }

        return result;
    }

    static DnsTransferMessageEncoding EncodeSigned(
        DnsMessage message,
        DnsTsigStreamState streamState,
        DnsClockValue now,
        int maximumLength)
    {
        DnsTransferMessageEncoding result =
            new DnsTransferMessageEncoding();
        DnsTsigResult signed = TsigStreamOperations.SignNext(
            streamState,
            message,
            now);
        result.Tsig = signed;
        CopyDiagnostics(signed.Diagnostics, result.Diagnostics);
        if (!signed.Success || signed.Message == null)
        {
            return result;
        }

        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength = maximumLength;
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            signed.Message,
            options);
        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
        if (encoded.Success && encoded.Message != null)
        {
            result.Fits = true;
            result.Message = encoded.Message;
            result.Bytes = encoded.Bytes;
        }

        return result;
    }

    static DnsTsigStreamState CopyStreamState(
        DnsTsigStreamState source)
    {
        DnsTsigStreamState result = new DnsTsigStreamState();
        result.Key = source.Key;
        result.RequestMac = RecordSetOperations.CopyBytes(
            source.RequestMac);
        result.PriorMac = RecordSetOperations.CopyBytes(source.PriorMac);
        int index = 0;
        while (index < source.UnsignedMessages.Count)
        {
            result.UnsignedMessages.Add(RecordSetOperations.CopyBytes(
                source.UnsignedMessages[index]));
            index = index + 1;
        }

        result.FirstMessage = source.FirstMessage;
        result.LastMessageWasSigned = source.LastMessageWasSigned;
        result.HasTimeSigned = source.HasTimeSigned;
        result.LastTimeSigned = source.LastTimeSigned;
        result.MessagesSinceSignature = source.MessagesSinceSignature;
        result.MessageCount = source.MessageCount;
        return result;
    }

    internal static List<DnsResourceRecord> CreateAxfrRecords(DnsZone zone)
    {
        List<DnsZoneNode> nodes = new List<DnsZoneNode>();
        List<DnsResourceRecord> all = new List<DnsResourceRecord>();
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            CollectNodeRecords(zone.Nodes[index], nodes, all);
            index = index + 1;
        }

        DnsResourceRecord? soa = null;
        index = 0;
        while (index < all.Count)
        {
            if (RecordTypeValue(all[index]) == (int)DnsRecordType.Soa)
            {
                soa = all[index];
            }

            index = index + 1;
        }

        List<DnsResourceRecord> result = new List<DnsResourceRecord>();
        if (soa == null)
        {
            return result;
        }

        result.Add(DnsZoneSnapshotOperations.CopyRecord(soa));
        index = 0;
        while (index < all.Count)
        {
            if (RecordTypeValue(all[index]) != (int)DnsRecordType.Soa)
            {
                result.Add(DnsZoneSnapshotOperations.CopyRecord(all[index]));
            }

            index = index + 1;
        }

        result.Add(DnsZoneSnapshotOperations.CopyRecord(soa));
        return result;
    }

    static void CollectNodeRecords(
        DnsZoneNode node,
        List<DnsZoneNode> visited,
        List<DnsResourceRecord> records)
    {
        if (ContainsNode(visited, node))
        {
            return;
        }

        visited.Add(node);
        int setIndex = 0;
        while (setIndex < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[setIndex];
            int recordIndex = 0;
            while (recordIndex < set.Records.Count)
            {
                records.Add(set.Records[recordIndex]);
                recordIndex = recordIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            CollectNodeRecords(node.Children[childIndex], visited, records);
            childIndex = childIndex + 1;
        }
    }

    static bool ContainsNode(
        List<DnsZoneNode> nodes,
        DnsZoneNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int RecordTypeValue(DnsResourceRecord record)
    {
        return DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
    }

    static DnsMessage CreateQuery(DnsName zone, int classValue)
    {
        DnsMessage result = new DnsMessage();
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

    static DnsMessage CreateResponse(
        DnsMessage query,
        DnsResponseCode responseCode,
        bool includeQuestion)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = query.Header.Identifier;
        result.Header.Opcode = DnsOpcode.Query;
        result.Header.OpcodeValue = (int)DnsOpcode.Query;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        if (includeQuestion && query.Questions.Count > 0)
        {
            DnsQuestion source = query.Questions[0];
            DnsQuestion question = new DnsQuestion();
            question.Name = RecordSetOperations.CopyName(
                source.Name,
                false);
            question.RecordType = source.RecordType;
            question.RecordTypeValue = source.RecordTypeValue;
            question.Class = source.Class;
            question.ClassValue = source.ClassValue;
            result.Questions.Add(question);
            result.Header.QuestionCount = 1;
        }

        return result;
    }

    internal static DnsTransferResult ProtocolResult(
        DnsTransferRequest request,
        DnsMessage query,
        DnsResponseCode responseCode,
        List<DnsDiagnostic> diagnostics,
        DnsTransferAuthentication? authentication,
        DnsClockValue now)
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = request.Kind;
        result.ResponseCode = responseCode;
        result.ResponseCodeValue = (int)responseCode;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        CopyDiagnostics(diagnostics, result.Diagnostics);
        DnsMessage response = CreateResponse(query, responseCode, true);
        DnsTransferMessageEncoding encoded;
        if (authentication != null && authentication.Present &&
            authentication.Tsig != null && request.TsigKey != null)
        {
            DnsTsigResult signed = TsigOperations.Sign(
                response,
                request.TsigKey,
                authentication.Tsig.Mac,
                now);
            result.Tsig = signed;
            if (!signed.Success || signed.Message == null)
            {
                CopyDiagnostics(signed.Diagnostics, result.Diagnostics);
                return result;
            }

            encoded = EncodeUnsigned(
                signed.Message,
                request.TransportOptions.MaximumResponseLength);
        }
        else
        {
            encoded = EncodeUnsigned(
                response,
                request.TransportOptions.MaximumResponseLength);
        }

        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
        if (encoded.Fits && encoded.Message != null &&
            encoded.Bytes.Length <= request.MaximumBytes)
        {
            result.Messages.Add(encoded.Message);
            result.MessageCount = 1;
            result.ByteCount = encoded.Bytes.Length;
        }

        return result;
    }

    static DnsTransferResult FailureResult(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsTransferResult result = new DnsTransferResult();
        result.Kind = DnsTransferKind.Axfr;
        result.ResponseCode = DnsResponseCode.ServerFailure;
        result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
        result.TransportStatus = DnsTransportStatus.ProtocolError;
        if (code != DnsDiagnosticCode.None)
        {
            result.Diagnostics.Add(CreateDiagnostic(code, field, reason));
        }

        return result;
    }

    static DnsTransferResult StreamFailure(
        DnsTransferResult result,
        DnsTransportStatus status,
        string field,
        string reason)
    {
        result.Success = false;
        result.TransportStatus = status;
        result.Messages.Clear();
        result.MessageCount = 0;
        result.RecordCount = 0;
        result.ByteCount = 0;
        result.Diagnostics.Add(CreateDiagnostic(
            DnsDiagnosticCode.ResourceLimitExceeded,
            field,
            reason));
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

    internal static List<DnsDiagnostic> AuthenticationDiagnostics(
        DnsTransferAuthentication authentication)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        if (authentication.Tsig != null)
        {
            CopyDiagnostics(authentication.Tsig.Diagnostics, result);
        }

        if (result.Count == 0)
        {
            result.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "request.tsig",
                "The AXFR request TSIG could not be authenticated."));
        }

        return result;
    }

    internal static List<DnsDiagnostic> DiagnosticList(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        result.Add(CreateDiagnostic(code, field, reason));
        return result;
    }

    internal static DnsDiagnostic CreateDiagnostic(
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

    internal static void CopyZoneDiagnostics(
        List<DnsZoneDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(CreateDiagnostic(
                source[index].Code,
                "request.zone",
                source[index].Reason));
            index = index + 1;
        }
    }

    static void CopyMessages(
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
}
