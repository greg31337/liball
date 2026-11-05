using System.Collections.Generic;

namespace dns;

internal class DnsNotifyMessageResult
{
    internal bool Success;
    internal DnsMessage? Message;
    internal DnsNotifyRequest? Request;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class DnsNotifyAuthenticationResult
{
    internal bool Present;
    internal bool Accepted = true;
    internal DnsTsigResult? Tsig;
}

internal class DnsNotifySerialResult
{
    internal bool Success;
    internal uint Serial;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class NotifyOperations
{
    internal static DnsNotifyResult SendNotify(
        DnsNotifyRequest request,
        IDnsQueryClient queryClient,
        DnsAuthorizationRateState authorizationRateState)
    {
        DnsClockValue started = TransportTime.ReadClock();
        if (request == null)
        {
            return CompleteNotifyResult(FailedNotifyResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A DNS notification request is required."), started);
        }

        if (request.CancellationRequested)
        {
            return CompleteNotifyResult(FailedNotifyResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "DNS notification processing was cancelled."), started);
        }

        DnsNotifyMessageResult prepared = PrepareNotifyRequest(request);
        if (!prepared.Success || prepared.Message == null ||
            prepared.Request == null)
        {
            return CompleteNotifyResult(NotifyFailureFromDiagnostics(
                prepared.Diagnostics), started);
        }

        DnsNotifyRequest effective = prepared.Request;
        CopyNotifyContext(request, effective);
        DnsAuthorizationDecision authorization =
            AuthorizationOperations.AuthorizeNotify(
                effective,
                started,
                authorizationRateState);
        if (!authorization.Valid)
        {
            return CompleteNotifyResult(FailedNotifyResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.authorization",
                authorization.Reason), started);
        }

        if (!authorization.Authorized)
        {
            return CompleteNotifyResult(NotifyProtocolResult(
                prepared.Message,
                DnsResponseCode.Refused,
                false,
                DiagnosticList(
                    AuthorizationDiagnosticCode(authorization),
                    "request.authorization",
                    authorization.Reason)), started);
        }

        DnsQueryRequest query = CreateNotifyQuery(
            effective,
            prepared.Message);
        DnsQueryResult exchange = queryClient.Query(query);
        DnsNotifyResult result = new DnsNotifyResult();
        result.Response = exchange.Response;
        result.Tsig = exchange.Tsig;
        result.ElapsedMilliseconds = exchange.ElapsedMilliseconds;
        CopyAttempts(result.Attempts, exchange.Attempts);
        CopyDiagnostics(result.Diagnostics, exchange.Diagnostics);
        if (!exchange.Success || exchange.Response == null)
        {
            return CompleteNotifyResult(result, started);
        }

        DnsNotifyMessageResult response = ParseNotifyResponse(
            prepared.Message,
            exchange.Response);
        if (!response.Success)
        {
            CopyDiagnostics(result.Diagnostics, response.Diagnostics);
            return CompleteNotifyResult(result, started);
        }

        int responseCode = MessageOperations.ReadEffectiveResponseCode(
            exchange.Response);
        result.Success = true;
        result.ResponseCode = (DnsResponseCode)responseCode;
        result.ResponseCodeValue = responseCode;
        result.Acknowledged = responseCode == (int)DnsResponseCode.NoError ||
            responseCode == (int)DnsResponseCode.NotImplemented;
        return CompleteNotifyResult(result, started);
    }

    internal static DnsNotifyResult ProcessNotify(
        DnsNotifyRequest request,
        IDnsZoneStore zoneStore,
        IDnsQueryClient queryClient,
        IDnsTransferClient transferClient,
        DnsAuthorizationRateState authorizationRateState)
    {
        DnsClockValue started = TransportTime.ReadClock();
        if (request == null)
        {
            return CompleteNotifyResult(FailedNotifyResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "A DNS notification request is required."), started);
        }

        if (request.CancellationRequested)
        {
            return CompleteNotifyResult(FailedNotifyResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "DNS notification processing was cancelled."), started);
        }

        DnsNotifyMessageResult prepared = PrepareNotifyRequest(request);
        if (!prepared.Success || prepared.Message == null ||
            prepared.Request == null)
        {
            return CompleteNotifyResult(NotifyProtocolResult(
                request.Message,
                DnsResponseCode.FormatError,
                false,
                prepared.Diagnostics), started);
        }

        DnsNotifyRequest effective = prepared.Request;
        CopyNotifyContext(request, effective);
        DnsNotifyAuthenticationResult authentication =
            AuthenticateNotifyRequest(effective, started);
        if (!authentication.Accepted)
        {
            DnsNotifyResult rejected = NotifyProtocolResult(
                prepared.Message,
                DnsResponseCode.NotAuthoritative,
                false,
                AuthenticationDiagnostics(authentication));
            rejected.Tsig = authentication.Tsig;
            return CompleteNotifyResult(rejected, started);
        }

        DnsZone? zone = zoneStore.ReadZone(
            effective.Zone,
            effective.Class,
            effective.ClassValue);
        if (zone == null)
        {
            return CompleteNotifyResult(CompleteNotifyAuthentication(
                NotifyProtocolResult(
                    prepared.Message,
                    DnsResponseCode.NotAuthoritative,
                    false,
                    DiagnosticList(
                        DnsDiagnosticCode.InvalidZone,
                        "request.zone",
                        "The notified zone is not hosted.")),
                effective,
                authentication,
                started), started);
        }

        DnsAuthorizationDecision authorization =
            AuthorizationOperations.AuthorizeNotify(
                effective,
                started,
                authorizationRateState);
        if (!authorization.Valid)
        {
            DnsNotifyResult invalid = FailedNotifyResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.authorization",
                authorization.Reason);
            invalid.Tsig = authentication.Tsig;
            return CompleteNotifyResult(invalid, started);
        }

        if (!authorization.Authorized)
        {
            return CompleteNotifyResult(CompleteNotifyAuthentication(
                NotifyProtocolResult(
                    prepared.Message,
                    DnsResponseCode.Refused,
                    false,
                    DiagnosticList(
                        AuthorizationDiagnosticCode(authorization),
                        "request.authorization",
                        authorization.Reason)),
                effective,
                authentication,
                started), started);
        }

        DnsNotifySerialResult local = ReadZoneSerial(zone);
        if (!local.Success)
        {
            return CompleteNotifyResult(CompleteNotifyAuthentication(
                NotifyProtocolResult(
                    prepared.Message,
                    DnsResponseCode.ServerFailure,
                    false,
                    local.Diagnostics),
                effective,
                authentication,
                started), started);
        }

        DnsNotifyResult result = NotifyProtocolResult(
            prepared.Message,
            DnsResponseCode.NoError,
            true,
            new List<DnsDiagnostic>());
        result = CompleteNotifyAuthentication(
            result,
            effective,
            authentication,
            started);
        if (effective.HasSerial && effective.Serial == local.Serial)
        {
            return CompleteNotifyResult(result, started);
        }

        DnsQueryResult soaQuery = queryClient.Query(CreateSoaQuery(effective));
        CopyDiagnostics(result.Diagnostics, soaQuery.Diagnostics);
        if (!soaQuery.Success || soaQuery.Response == null)
        {
            return CompleteNotifyResult(result, started);
        }

        DnsNotifySerialResult remote = ReadResponseSerial(
            effective,
            soaQuery.Response);
        CopyDiagnostics(result.Diagnostics, remote.Diagnostics);
        if (!remote.Success)
        {
            return CompleteNotifyResult(result, started);
        }

        DnsSerialComparison comparison = DnsSerialArithmetic.CompareSerials(
            remote.Serial,
            local.Serial);
        if (!comparison.Defined)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "response.soa.serial",
                "The primary and secondary SOA serials are not comparable."));
            return CompleteNotifyResult(result, started);
        }

        if (comparison.Comparison != 1)
        {
            return CompleteNotifyResult(result, started);
        }

        result.TransferRequired = true;
        DnsTransferRequest transferRequest = CreateTransferRequest(
            effective,
            local.Serial);
        result.Transfer = transferClient.ReceiveTransfer(transferRequest);
        result.TransferTriggered = true;
        return CompleteNotifyResult(result, started);
    }

    static DnsNotifyMessageResult PrepareNotifyRequest(
        DnsNotifyRequest request)
    {
        if (request.Message != null)
        {
            return ParseNotifyRequest(request.Message);
        }

        return BuildNotifyRequest(request);
    }

    static DnsNotifyMessageResult BuildNotifyRequest(
        DnsNotifyRequest request)
    {
        DnsNotifyMessageResult result = new DnsNotifyMessageResult();
        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(request.Zone);
        if (!NotifyClassValid(classValue) || !zone.Success ||
            zone.Name == null || !zone.Name.IsAbsolute)
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.InvalidZone,
                "request.zone",
                "A NOTIFY requires an absolute zone and ordinary DNS class.");
        }

        DnsMessage message = new DnsMessage();
        message.Header.Identifier = TransportEntropy.NextIdentifier();
        message.Header.Opcode = DnsOpcode.Notify;
        message.Header.OpcodeValue = (int)DnsOpcode.Notify;
        message.Header.Flags = DnsHeaderFlag.AuthoritativeAnswer;
        message.Header.FlagsValue = (int)DnsHeaderFlag.AuthoritativeAnswer;
        message.Questions.Add(CreateNotifyQuestion(zone.Name, classValue));
        message.Header.QuestionCount = 1;
        if (request.HasSerial)
        {
            message.Answers.Add(CreateSoaHint(
                zone.Name,
                classValue,
                request.Serial));
            message.Header.AnswerCount = 1;
        }

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message,
            new DnsEncodeOptions());
        if (!encoded.Success || encoded.Message == null)
        {
            return NotifyMessageFromEncoding(encoded);
        }

        result.Success = true;
        result.Message = encoded.Message;
        result.Request = CopyParsedNotifyRequest(
            zone.Name,
            classValue,
            request.HasSerial,
            request.Serial,
            encoded.Message);
        return result;
    }

    static DnsNotifyMessageResult ParseNotifyRequest(DnsMessage message)
    {
        if (!NotifyMessageCollectionsValid(message))
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message",
                "A complete NOTIFY message is required.");
        }

        int opcode = DnsHeaderCodec.ResolveCode(
            message.Header.OpcodeValue,
            (int)message.Header.Opcode);
        int responseCode = MessageOperations.ReadEffectiveResponseCode(message);
        int flags = MessageOperations.EffectiveFlags(message.Header);
        if (opcode != (int)DnsOpcode.Notify ||
            flags != (int)DnsHeaderFlag.AuthoritativeAnswer ||
            responseCode != 0)
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.ResponseMismatch,
                "message.header",
                "A NOTIFY request requires opcode NOTIFY, AA, QR clear, and zero response code.");
        }

        if (!NotifyCountsValid(message) || message.Questions.Count != 1)
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.SectionCountMismatch,
                "message.sections",
                "A NOTIFY request requires one question and consistent section counts.");
        }

        DnsQuestion question = message.Questions[0];
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue,
            (int)question.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            question.ClassValue,
            (int)question.Class);
        RawDnsNameResult zone = RawDnsName.ValidateName(question.Name);
        if (typeValue != (int)DnsRecordType.Soa ||
            !NotifyClassValid(classValue) || !zone.Success ||
            zone.Name == null || !zone.Name.IsAbsolute)
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.InvalidZone,
                "message.question",
                "The NOTIFY question must identify an absolute SOA zone.");
        }

        bool hasSerial = false;
        uint serial = 0;
        int index = 0;
        while (index < message.Answers.Count)
        {
            DnsResourceRecord record = message.Answers[index];
            if (!NotifySoaRecordValid(record, zone.Name, classValue) ||
                hasSerial)
            {
                return NotifyMessageFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "message.answers",
                    "A NOTIFY may contain at most one matching typed SOA hint.");
            }

            hasSerial = true;
            if (record.Data != null && record.Data.Soa != null)
            {
                serial = record.Data.Soa.Serial;
            }
            index = index + 1;
        }

        DnsEncodeResult encodable = DnsMessageEncoder.EncodeMessage(
            message,
            new DnsEncodeOptions());
        if (!encodable.Success)
        {
            return NotifyMessageFromEncoding(encodable);
        }

        DnsNotifyMessageResult result = new DnsNotifyMessageResult();
        result.Success = true;
        result.Message = message;
        result.Request = CopyParsedNotifyRequest(
            zone.Name,
            classValue,
            hasSerial,
            serial,
            message);
        return result;
    }

    static DnsNotifyMessageResult ParseNotifyResponse(
        DnsMessage request,
        DnsMessage response)
    {
        if (!NotifyMessageCollectionsValid(response) ||
            !NotifyCountsValid(response))
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.SectionCountMismatch,
                "response.sections",
                "A complete NOTIFY response with consistent counts is required.");
        }

        int opcode = DnsHeaderCodec.ResolveCode(
            response.Header.OpcodeValue,
            (int)response.Header.Opcode);
        int flags = MessageOperations.EffectiveFlags(response.Header);
        int allowedFlags = (int)(DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer);
        int responseCode = MessageOperations.ReadEffectiveResponseCode(
            response);
        if (response.Header.Identifier != request.Header.Identifier ||
            opcode != (int)DnsOpcode.Notify || flags != allowedFlags ||
            responseCode < 0 || responseCode > 4095 ||
            response.Questions.Count != 1 ||
            !QuestionsEqual(request.Questions[0], response.Questions[0]))
        {
            return NotifyMessageFailure(
                DnsDiagnosticCode.ResponseMismatch,
                "response",
                "The NOTIFY response does not match its request.");
        }

        DnsNotifyMessageResult result = new DnsNotifyMessageResult();
        result.Success = true;
        result.Message = response;
        return result;
    }

    static DnsNotifyAuthenticationResult AuthenticateNotifyRequest(
        DnsNotifyRequest request,
        DnsClockValue now)
    {
        DnsNotifyAuthenticationResult result =
            new DnsNotifyAuthenticationResult();
        if (request.Message == null)
        {
            return result;
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(request.Message);
        result.Present = placement.Success && placement.HasTsig;
        if (!placement.Success)
        {
            result.Accepted = false;
            DnsTsigResult invalid = new DnsTsigResult();
            CopyDiagnostics(invalid.Diagnostics, placement.Diagnostics);
            result.Tsig = invalid;
            return result;
        }

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
                "No verification key is available for the signed NOTIFY."));
            result.Accepted = false;
            result.Tsig = missing;
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

    static DnsNotifyResult CompleteNotifyAuthentication(
        DnsNotifyResult result,
        DnsNotifyRequest request,
        DnsNotifyAuthenticationResult authentication,
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

    static DnsQueryRequest CreateNotifyQuery(
        DnsNotifyRequest request,
        DnsMessage message)
    {
        DnsQueryRequest result = new DnsQueryRequest();
        result.Question = CopyQuestion(message.Questions[0]);
        result.Message = MessageOperations.CloneMessage(message);
        result.TsigKey = request.TsigKey;
        result.Servers.Add(CopyEndpoint(request.Endpoint));
        result.TransportOptions = request.TransportOptions;
        result.RecursionDesired = false;
        result.UseEdns = false;
        result.AllowEdnsDowngrade = false;
        result.CancellationRequested = request.CancellationRequested;
        return result;
    }

    static DnsQueryRequest CreateSoaQuery(DnsNotifyRequest request)
    {
        DnsQueryRequest result = new DnsQueryRequest();
        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        result.Question = CreateNotifyQuestion(request.Zone, classValue);
        result.TsigKey = request.TsigKey;
        result.Servers.Add(CopyEndpoint(request.Endpoint));
        result.TransportOptions = request.TransportOptions;
        result.RecursionDesired = false;
        result.UseEdns = false;
        result.AllowEdnsDowngrade = false;
        return result;
    }

    static DnsTransferRequest CreateTransferRequest(
        DnsNotifyRequest request,
        uint currentSerial)
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Kind = DnsTransferKind.Ixfr;
        result.Zone = RecordSetOperations.CopyName(request.Zone, false);
        result.Class = request.Class;
        result.ClassValue = request.ClassValue;
        result.HasCurrentSerial = true;
        result.CurrentSerial = currentSerial;
        result.Endpoint = CopyEndpoint(request.Endpoint);
        result.TransportOptions = request.TransportOptions;
        result.Transport = request.TransportOptions.Kind;
        result.TsigKey = request.TsigKey;
        result.Authorization = request.Authorization;
        return result;
    }

    static DnsNotifySerialResult ReadResponseSerial(
        DnsNotifyRequest request,
        DnsMessage response)
    {
        DnsNotifySerialResult result = new DnsNotifySerialResult();
        if (!NotifyMessageCollectionsValid(response) ||
            !NotifyCountsValid(response) || response.Questions.Count != 1 ||
            !QuestionsEqual(
                CreateNotifyQuestion(
                    request.Zone,
                    DnsHeaderCodec.ResolveCode(
                        request.ClassValue,
                        (int)request.Class)),
                response.Questions[0]))
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "response",
                "The primary SOA response does not match its query."));
            return result;
        }

        int flags = MessageOperations.EffectiveFlags(response.Header);
        int opcode = DnsHeaderCodec.ResolveCode(
            response.Header.OpcodeValue,
            (int)response.Header.Opcode);
        int classValue = DnsHeaderCodec.ResolveCode(
            request.ClassValue,
            (int)request.Class);
        if (opcode != (int)DnsOpcode.Query ||
            (flags & (int)DnsHeaderFlag.Response) == 0 ||
            (flags & (int)DnsHeaderFlag.AuthoritativeAnswer) == 0 ||
            MessageOperations.ReadEffectiveResponseCode(response) != 0)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "response.header",
                "The primary SOA response must be authoritative and successful."));
            return result;
        }

        int index = 0;
        while (index < response.Answers.Count)
        {
            DnsResourceRecord record = response.Answers[index];
            if (NotifySoaRecordValid(record, request.Zone, classValue))
            {
                if (result.Success)
                {
                    result.Success = false;
                    result.Diagnostics.Add(CreateDiagnostic(
                        DnsDiagnosticCode.ResponseMismatch,
                        "response.answers",
                        "The primary SOA response contains multiple SOA records."));
                    return result;
                }

                result.Success = true;
                if (record.Data != null && record.Data.Soa != null)
                {
                    result.Serial = record.Data.Soa.Serial;
                }
            }

            index = index + 1;
        }

        if (!result.Success)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResponseMismatch,
                "response.answers",
                "The primary SOA response does not contain the notified zone SOA."));
        }

        return result;
    }

    static DnsNotifySerialResult ReadZoneSerial(DnsZone zone)
    {
        DnsNotifySerialResult result = new DnsNotifySerialResult();
        int classValue = DnsHeaderCodec.ResolveCode(
            zone.ClassValue,
            (int)zone.Class);
        int nodeIndex = 0;
        while (nodeIndex < zone.Nodes.Count)
        {
            DnsZoneNode node = zone.Nodes[nodeIndex];
            if (RecordSetOperations.NamesEqual(node.Name, zone.Apex))
            {
                int setIndex = 0;
                while (setIndex < node.RecordSets.Count)
                {
                    DnsResourceRecordSet set = node.RecordSets[setIndex];
                    int typeValue = DnsHeaderCodec.ResolveCode(
                        set.RecordTypeValue,
                        (int)set.RecordType);
                    int setClass = DnsHeaderCodec.ResolveCode(
                        set.ClassValue,
                        (int)set.Class);
                    if (typeValue == (int)DnsRecordType.Soa &&
                        setClass == classValue && set.Records.Count == 1)
                    {
                        DnsResourceRecord record = set.Records[0];
                        if (record.Data != null && record.Data.Soa != null)
                        {
                            result.Success = true;
                            result.Serial = record.Data.Soa.Serial;
                            return result;
                        }
                    }

                    setIndex = setIndex + 1;
                }
            }

            nodeIndex = nodeIndex + 1;
        }

        result.Diagnostics.Add(CreateDiagnostic(
            DnsDiagnosticCode.InvalidZone,
            "zone.soa",
            "The hosted secondary zone does not contain one readable apex SOA."));
        return result;
    }

    static DnsNotifyResult NotifyProtocolResult(
        DnsMessage? request,
        DnsResponseCode responseCode,
        bool acknowledged,
        List<DnsDiagnostic> diagnostics)
    {
        DnsNotifyResult result = new DnsNotifyResult();
        result.Success = true;
        result.Acknowledged = acknowledged;
        result.ResponseCode = responseCode;
        result.ResponseCodeValue = (int)responseCode;
        result.Response = CreateNotifyResponse(request, responseCode);
        CopyDiagnostics(result.Diagnostics, diagnostics);
        return result;
    }

    static DnsMessage CreateNotifyResponse(
        DnsMessage? request,
        DnsResponseCode responseCode)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Opcode = DnsOpcode.Notify;
        result.Header.OpcodeValue = (int)DnsOpcode.Notify;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        if (request != null && request.Header != null)
        {
            result.Header.Identifier = request.Header.Identifier;
            if (request.Questions != null && request.Questions.Count == 1)
            {
                result.Questions.Add(CopyQuestion(request.Questions[0]));
                result.Header.QuestionCount = 1;
            }

            if (request.Answers != null)
            {
                int index = 0;
                while (index < request.Answers.Count)
                {
                    result.Answers.Add(
                        DnsZoneSnapshotOperations.CopyRecord(
                            request.Answers[index]));
                    index = index + 1;
                }

                result.Header.AnswerCount = result.Answers.Count;
            }
        }

        return result;
    }

    static DnsNotifyResult CompleteNotifyResult(
        DnsNotifyResult result,
        DnsClockValue started)
    {
        DnsClockValue completed = TransportTime.ReadClock();
        result.ElapsedMilliseconds = TransportTime.ElapsedMilliseconds(
            started,
            completed);
        return result;
    }

    static DnsNotifyResult FailedNotifyResult(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsNotifyResult result = new DnsNotifyResult();
        result.ResponseCode = DnsResponseCode.ServerFailure;
        result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
        result.Diagnostics.Add(CreateDiagnostic(code, field, reason));
        return result;
    }

    static DnsNotifyResult NotifyFailureFromDiagnostics(
        List<DnsDiagnostic> diagnostics)
    {
        DnsNotifyResult result = new DnsNotifyResult();
        result.ResponseCode = DnsResponseCode.FormatError;
        result.ResponseCodeValue = (int)DnsResponseCode.FormatError;
        CopyDiagnostics(result.Diagnostics, diagnostics);
        return result;
    }

    static DnsNotifyMessageResult NotifyMessageFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsNotifyMessageResult result = new DnsNotifyMessageResult();
        result.Diagnostics.Add(CreateDiagnostic(code, field, reason));
        return result;
    }

    static DnsNotifyMessageResult NotifyMessageFromEncoding(
        DnsEncodeResult encoded)
    {
        DnsNotifyMessageResult result = new DnsNotifyMessageResult();
        CopyDiagnostics(result.Diagnostics, encoded.Diagnostics);
        return result;
    }

    static DnsNotifyRequest CopyParsedNotifyRequest(
        DnsName zone,
        int classValue,
        bool hasSerial,
        uint serial,
        DnsMessage message)
    {
        DnsNotifyRequest result = new DnsNotifyRequest();
        result.Zone = RecordSetOperations.CopyName(zone, false);
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.HasSerial = hasSerial;
        result.Serial = serial;
        result.Message = message;
        return result;
    }

    static void CopyNotifyContext(
        DnsNotifyRequest source,
        DnsNotifyRequest destination)
    {
        destination.Endpoint = source.Endpoint;
        destination.TransportOptions = source.TransportOptions;
        destination.TsigKey = source.TsigKey;
        destination.Authorization = source.Authorization;
        destination.CancellationRequested = source.CancellationRequested;
    }

    static DnsQuestion CreateNotifyQuestion(
        DnsName zone,
        int classValue)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = RecordSetOperations.CopyName(zone, false);
        result.RecordType = DnsRecordType.Soa;
        result.RecordTypeValue = (int)DnsRecordType.Soa;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        return result;
    }

    static DnsResourceRecord CreateSoaHint(
        DnsName zone,
        int classValue,
        uint serial)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = RecordSetOperations.CopyName(zone, false);
        result.RecordType = DnsRecordType.Soa;
        result.RecordTypeValue = (int)DnsRecordType.Soa;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.PrimaryNameServer.IsAbsolute = true;
        result.Data.Soa.ResponsibleMailbox.IsAbsolute = true;
        result.Data.Soa.Serial = serial;
        return result;
    }

    static bool NotifySoaRecordValid(
        DnsResourceRecord record,
        DnsName zone,
        int classValue)
    {
        if (record == null || record.Data == null ||
            record.Data.Soa == null || record.UseRawData ||
            !RecordSetOperations.NamesEqual(record.Name, zone))
        {
            return false;
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        int recordClass = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        return typeValue == (int)DnsRecordType.Soa &&
            recordClass == classValue;
    }

    static bool NotifyMessageCollectionsValid(DnsMessage message)
    {
        return message != null && message.Header != null &&
            message.Questions != null && message.Answers != null &&
            message.Authorities != null && message.Additionals != null;
    }

    static bool NotifyCountsValid(DnsMessage message)
    {
        return message.Header.QuestionCount == message.Questions.Count &&
            message.Header.AnswerCount == message.Answers.Count &&
            message.Header.AuthorityCount == message.Authorities.Count &&
            message.Header.AdditionalCount == message.Additionals.Count;
    }

    static bool NotifyClassValid(int classValue)
    {
        return classValue > 0 && classValue < (int)DnsClass.None;
    }

    static bool QuestionsEqual(DnsQuestion left, DnsQuestion right)
    {
        int leftType = DnsHeaderCodec.ResolveCode(
            left.RecordTypeValue,
            (int)left.RecordType);
        int rightType = DnsHeaderCodec.ResolveCode(
            right.RecordTypeValue,
            (int)right.RecordType);
        int leftClass = DnsHeaderCodec.ResolveCode(
            left.ClassValue,
            (int)left.Class);
        int rightClass = DnsHeaderCodec.ResolveCode(
            right.ClassValue,
            (int)right.Class);
        return leftType == rightType && leftClass == rightClass &&
            RecordSetOperations.NamesEqual(left.Name, right.Name);
    }

    static DnsQuestion CopyQuestion(DnsQuestion source)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.Address = RecordSetOperations.CopyBytes(source.Address);
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        return result;
    }

    static DnsDiagnosticCode AuthorizationDiagnosticCode(
        DnsAuthorizationDecision authorization)
    {
        if (authorization.RateLimited)
        {
            return DnsDiagnosticCode.ResourceLimitExceeded;
        }

        return DnsDiagnosticCode.ValueOutOfRange;
    }

    static List<DnsDiagnostic> AuthenticationDiagnostics(
        DnsNotifyAuthenticationResult authentication)
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
                "The NOTIFY TSIG could not be authenticated."));
        }

        return result;
    }

    static List<DnsDiagnostic> DiagnosticList(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        List<DnsDiagnostic> result = new List<DnsDiagnostic>();
        result.Add(CreateDiagnostic(code, field, reason));
        return result;
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

    static void CopyAttempts(
        List<DnsTransportResult> destination,
        List<DnsTransportResult> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
