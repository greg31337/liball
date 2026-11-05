using System.Collections.Generic;

namespace dns;

internal class DnsAuthoritativeRequestState
{
    internal DnsAuthoritativeRequest Request = new DnsAuthoritativeRequest();
    internal IDnsZoneStore ZoneStore = new MemoryDnsZoneStore();
    internal DnsQuestion? Question;
    internal int OpcodeValue;
    internal int RecordTypeValue;
    internal int ClassValue;
    internal int RequestFlags;
    internal byte[] CookieSecret = [];
    internal DnsResourceRecord? RequestOpt;
    internal bool EdnsMalformed;
    internal bool RateLimitSlip;
}

internal static class AuthoritativeRequestOperations
{
    internal static DnsAuthoritativeResult AnswerRequest(
        DnsAuthoritativeRequest request,
        IDnsZoneStore zoneStore,
        byte[] cookieSecret,
        DnsAuthorizationRateState authorizationRateState)
    {
        if (request == null)
        {
            return FailedResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "An authoritative request is required.");
        }

        if (request.CancellationRequested)
        {
            return FailedResult(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "request.cancellationRequested",
                "Authoritative processing was cancelled.");
        }

        DnsAuthoritativeResult optionFailure = ValidateOptions(request);
        if (!optionFailure.Success)
        {
            return optionFailure;
        }

        DnsAuthoritativeRequestState state = new DnsAuthoritativeRequestState();
        state.Request = request;
        state.ZoneStore = zoneStore;
        state.CookieSecret = cookieSecret;
        DnsAuthoritativeResult messageFailure = ValidateMessage(state);
        if (!messageFailure.Success || messageFailure.Response != null)
        {
            return messageFailure;
        }

        if ((state.RequestFlags & (int)DnsHeaderFlag.Response) != 0 ||
            (state.RequestFlags & (int)DnsHeaderFlag.Reserved) != 0 ||
            DnsHeaderCodec.ResolveCode(
                request.Message.Header.ResponseCodeValue,
                (int)request.Message.Header.ResponseCode) != 0)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.FormatError,
                DnsAuthoritativeAnswerKind.ServerFailure,
                false,
                null,
                DnsDiagnosticCode.ResponseMismatch,
                "message.header",
                "A DNS query must not contain response, reserved, or response-code bits.");
        }

        if (state.OpcodeValue != (int)DnsOpcode.Query)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.NotImplemented,
                DnsAuthoritativeAnswerKind.NotImplemented,
                false,
                null,
                DnsDiagnosticCode.ValueOutOfRange,
                "message.header.opcode",
                "The authoritative answer service supports the QUERY opcode only.");
        }

        if (request.Message.Questions.Count != 1 ||
            state.Question == null)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.FormatError,
                DnsAuthoritativeAnswerKind.ServerFailure,
                false,
                null,
                DnsDiagnosticCode.SectionCountMismatch,
                "message.questions",
                "An authoritative query requires exactly one question.");
        }

        RawDnsNameResult validatedName = RawDnsName.ValidateName(
            state.Question.Name);
        if (!validatedName.Success || validatedName.Name == null ||
            !validatedName.Name.IsAbsolute)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.FormatError,
                DnsAuthoritativeAnswerKind.ServerFailure,
                false,
                null,
                DnsDiagnosticCode.InvalidLabel,
                "message.questions[0].name",
                "The authoritative question requires a valid absolute DNS name.");
        }

        if (state.RecordTypeValue == 0 || state.ClassValue == 0 ||
            state.ClassValue == (int)DnsClass.None)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.FormatError,
                DnsAuthoritativeAnswerKind.ServerFailure,
                false,
                null,
                DnsDiagnosticCode.ValueOutOfRange,
                "message.questions[0]",
                "Reserved types and reserved or NONE classes are invalid in an ordinary query.");
        }

        if (state.ClassValue == (int)DnsClass.Any ||
            state.RecordTypeValue == (int)DnsRecordType.Ixfr ||
            state.RecordTypeValue == (int)DnsRecordType.Axfr)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.NotImplemented,
                DnsAuthoritativeAnswerKind.NotImplemented,
                false,
                null,
                DnsDiagnosticCode.ValueOutOfRange,
                "message.questions[0]",
                "Class-ANY and zone-transfer questions require a dedicated operation.");
        }

        DnsZone? zone = zoneStore.FindBestZone(
            validatedName.Name,
            (DnsClass)state.ClassValue,
            state.ClassValue);
        if (zone == null)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.Refused,
                DnsAuthoritativeAnswerKind.Refused,
                false,
                null,
                DnsDiagnosticCode.InvalidZone,
                "message.questions[0].name",
                "No hosted zone matches the question name and class.");
        }

        DnsClockValue authorizationTime = request.ReceivedAt;
        if (authorizationTime.MonotonicMilliseconds == 0 &&
            authorizationTime.UnixTimeMilliseconds == 0)
        {
            authorizationTime = TransportTime.ReadClock();
        }

        DnsAuthorizationDecision authorization =
            AuthorizationOperations.AuthorizeQuery(
                request.Options.Authorization,
                request.ClientEndpoint,
                zone.Apex,
                request.Tsig,
                authorizationTime,
                authorizationRateState);
        if (!authorization.Valid)
        {
            return FailedResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.options.authorization",
                authorization.Reason);
        }

        if (!authorization.Authorized)
        {
            DnsDiagnosticCode denialCode =
                DnsDiagnosticCode.ValueOutOfRange;
            if (authorization.RateLimited)
            {
                denialCode = DnsDiagnosticCode.ResourceLimitExceeded;
            }

            return CreateProtocolResult(
                state,
                DnsResponseCode.Refused,
                DnsAuthoritativeAnswerKind.Refused,
                false,
                zone,
                denialCode,
                "request.options.authorization",
                authorization.Reason);
        }

        state.RateLimitSlip = authorization.Slipped;

        return CreateProtocolResult(
            state,
            DnsResponseCode.NoError,
            DnsAuthoritativeAnswerKind.Positive,
            true,
            zone,
            DnsDiagnosticCode.None,
            string.Empty,
            string.Empty);
    }

    static DnsAuthoritativeResult ValidateOptions(
        DnsAuthoritativeRequest request)
    {
        if (request.Message == null || request.Options == null ||
            request.Options.Authorization == null ||
            request.Options.Security == null)
        {
            return FailedResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request",
                "The request message and complete authoritative options are required.");
        }

        DnsAuthoritativeOptions options = request.Options;
        if (options.MaximumUdpResponseLength < DnsProtocolLimits.HeaderLength ||
            options.MaximumUdpResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumTcpResponseLength < DnsProtocolLimits.HeaderLength ||
            options.MaximumTcpResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumAliasDepth < 0 ||
            options.MaximumResponseRecords < 0 ||
            options.MaximumResponseRecords >
                DnsProtocolLimits.MaximumSectionEntryCount ||
            (int)request.Transport < (int)DnsTransportKind.Udp ||
            (int)request.Transport > (int)DnsTransportKind.Quic)
        {
            return FailedResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.options",
                "Authoritative response limits and transport values must be bounded.");
        }

        DnsAuthoritativeResult result = new DnsAuthoritativeResult();
        result.Success = true;
        return result;
    }

    static DnsAuthoritativeResult ValidateMessage(
        DnsAuthoritativeRequestState state)
    {
        DnsMessage message = state.Request.Message;
        if (message.Header == null || message.Questions == null ||
            message.Answers == null || message.Authorities == null ||
            message.Additionals == null)
        {
            return FailedResult(
                DnsDiagnosticCode.ValueOutOfRange,
                "request.message",
                "A complete DNS message and all section collections are required.");
        }

        state.OpcodeValue = DnsHeaderCodec.ResolveCode(
            message.Header.OpcodeValue,
            (int)message.Header.Opcode);
        state.RequestFlags = MessageOperations.EffectiveFlags(message.Header);
        if (state.OpcodeValue < 0 || state.OpcodeValue > 15 ||
            message.Header.QuestionCount != message.Questions.Count ||
            message.Header.AnswerCount != message.Answers.Count ||
            message.Header.AuthorityCount != message.Authorities.Count ||
            message.Header.AdditionalCount != message.Additionals.Count ||
            message.Answers.Count != 0 || message.Authorities.Count != 0)
        {
            return CreateProtocolResult(
                state,
                DnsResponseCode.FormatError,
                DnsAuthoritativeAnswerKind.ServerFailure,
                false,
                null,
                DnsDiagnosticCode.SectionCountMismatch,
                "message.sections",
                "The DNS query header and section collections are inconsistent.");
        }

        if (message.Questions.Count == 1 && message.Questions[0] != null)
        {
            state.Question = message.Questions[0];
            state.RecordTypeValue = DnsHeaderCodec.ResolveCode(
                state.Question.RecordTypeValue,
                (int)state.Question.RecordType);
            state.ClassValue = DnsHeaderCodec.ResolveCode(
                state.Question.ClassValue,
                (int)state.Question.Class);
            if (state.RecordTypeValue < 0 ||
                state.RecordTypeValue > 65535 || state.ClassValue < 0 ||
                state.ClassValue > 65535)
            {
                return CreateProtocolResult(
                    state,
                    DnsResponseCode.FormatError,
                    DnsAuthoritativeAnswerKind.ServerFailure,
                    false,
                    null,
                    DnsDiagnosticCode.ValueOutOfRange,
                    "message.questions[0]",
                    "Question type and class values must fit in sixteen bits.");
            }
        }

        DnsAuthoritativeResult ednsFailure = ValidateEdnsRequest(state);
        if (!ednsFailure.Success || ednsFailure.Response != null)
        {
            return ednsFailure;
        }

        DnsAuthoritativeResult result = new DnsAuthoritativeResult();
        result.Success = true;
        return result;
    }

    static DnsAuthoritativeResult ValidateEdnsRequest(
        DnsAuthoritativeRequestState state)
    {
        int optCount = 0;
        int index = 0;
        while (index < state.Request.Message.Additionals.Count)
        {
            DnsResourceRecord record =
                state.Request.Message.Additionals[index];
            if (record == null)
            {
                state.EdnsMalformed = true;
                return EdnsFormatFailure(
                    state,
                    "An additional record cannot be null.");
            }

            int typeValue = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == (int)DnsRecordType.Opt)
            {
                optCount = optCount + 1;
                state.RequestOpt = record;
            }

            index = index + 1;
        }

        if (optCount > 1)
        {
            state.EdnsMalformed = true;
            state.RequestOpt = null;
            return EdnsFormatFailure(
                state,
                "A DNS query cannot contain more than one OPT record.");
        }

        if (state.RequestOpt == null)
        {
            DnsAuthoritativeResult noEdns = new DnsAuthoritativeResult();
            noEdns.Success = true;
            return noEdns;
        }

        DnsResourceRecord opt = state.RequestOpt;
        if (!opt.Name.IsAbsolute || opt.Name.Labels.Count != 0)
        {
            state.EdnsMalformed = true;
            return EdnsFormatFailure(
                state,
                "An OPT record owner must be the absolute root name.");
        }

        int payloadSize = DnsHeaderCodec.ResolveCode(
            opt.ClassValue,
            (int)opt.Class);
        int version = (int)((opt.TimeToLive >> 16) & 255);
        int flags = (int)(opt.TimeToLive & 65535);
        if (opt.Data != null && opt.Data.Edns != null)
        {
            DnsEdnsData edns = opt.Data.Edns;
            payloadSize = edns.UdpPayloadSize;
            version = edns.Version;
            flags = EdnsRecordDataCodec.ReadEffectiveFlags(edns);
            if (edns.Options == null || !ValidateEdnsOptions(edns.Options))
            {
                state.EdnsMalformed = true;
                return EdnsFormatFailure(
                    state,
                    "The OPT record contains invalid EDNS options.");
            }
        }

        if (payloadSize < 0 || payloadSize > 65535 ||
            version < 0 || version > 255 || flags < 0 || flags > 65535)
        {
            state.EdnsMalformed = true;
            return EdnsFormatFailure(
                state,
                "EDNS header values must fit their wire fields.");
        }

        DnsAuthoritativeResult result = new DnsAuthoritativeResult();
        result.Success = true;
        return result;
    }

    static bool ValidateEdnsOptions(List<DnsEdnsOption> options)
    {
        int cookieCount = 0;
        int index = 0;
        while (index < options.Count)
        {
            DnsEdnsOption option = options[index];
            if (option == null)
            {
                return false;
            }

            int code = DnsHeaderCodec.ResolveCode(
                option.CodeValue,
                (int)option.Code);
            if (code < 0 || code > 65535)
            {
                return false;
            }

            if (code == (int)DnsEdnsOptionCode.Cookie)
            {
                cookieCount = cookieCount + 1;
                if (option.Cookie == null ||
                    option.Cookie.ClientCookie == null ||
                    option.Cookie.ServerCookie == null ||
                    option.Cookie.ClientCookie.Length != 8 ||
                    (option.Cookie.ServerCookie.Length != 0 &&
                     (option.Cookie.ServerCookie.Length < 8 ||
                      option.Cookie.ServerCookie.Length > 32)))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return cookieCount <= 1;
    }

    static DnsAuthoritativeResult EdnsFormatFailure(
        DnsAuthoritativeRequestState state,
        string reason)
    {
        return CreateProtocolResult(
            state,
            DnsResponseCode.FormatError,
            DnsAuthoritativeAnswerKind.ServerFailure,
            false,
            null,
            DnsDiagnosticCode.InvalidRecordData,
            "message.additionals.opt",
            reason);
    }

    static DnsAuthoritativeResult CreateProtocolResult(
        DnsAuthoritativeRequestState state,
        DnsResponseCode responseCode,
        DnsAuthoritativeAnswerKind kind,
        bool authoritative,
        DnsZone? zone,
        DnsDiagnosticCode diagnosticCode,
        string diagnosticField,
        string diagnosticReason)
    {
        DnsAuthoritativeResult result = new DnsAuthoritativeResult();
        result.Kind = kind;
        result.ResponseCode = responseCode;
        result.ResponseCodeValue = (int)responseCode;
        result.Authoritative = authoritative;
        if (zone != null)
        {
            result.Zone = RecordSetOperations.CopyName(zone.Apex, false);
        }

        DnsMessage response = CreateBaseResponse(
            state,
            responseCode,
            authoritative);
        result.Response = response;
        if (diagnosticCode != DnsDiagnosticCode.None)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                diagnosticCode,
                diagnosticField,
                diagnosticReason));
        }

        if (zone != null && authoritative &&
            responseCode == DnsResponseCode.NoError &&
            state.Question != null)
        {
            DnsAuthoritativeAnswerPopulation population =
                AuthoritativeAnswerOperations.PopulateAnswer(
                    state,
                    zone,
                    result,
                    response);
            if (!population.Success)
            {
                result.Kind = DnsAuthoritativeAnswerKind.ServerFailure;
                result.ResponseCode = DnsResponseCode.ServerFailure;
                result.ResponseCodeValue =
                    (int)DnsResponseCode.ServerFailure;
                response.Header.ResponseCode = DnsResponseCode.ServerFailure;
                response.Header.ResponseCodeValue =
                    (int)DnsResponseCode.ServerFailure;
                response.Answers.Clear();
                response.Authorities.Clear();
                response.Additionals.Clear();
                response.Header.AnswerCount = 0;
                response.Header.AuthorityCount = 0;
                response.Header.AdditionalCount = 0;
                result.AnswerRecordSets.Clear();
                result.AuthorityRecordSets.Clear();
                result.AdditionalRecordSets.Clear();
                result.Diagnostics.Add(CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "answer.alias",
                    population.Reason));
            }
            else if (population.Referral)
            {
                result.Kind = DnsAuthoritativeAnswerKind.Referral;
                result.Authoritative = false;
                response.Header.Flags = response.Header.Flags &
                    ~DnsHeaderFlag.AuthoritativeAnswer;
                response.Header.FlagsValue = response.Header.FlagsValue &
                    ~(int)DnsHeaderFlag.AuthoritativeAnswer;
            }
            else if (population.Negative)
            {
                if (population.NameError)
                {
                    result.ResponseCode = DnsResponseCode.NameError;
                    result.ResponseCodeValue =
                        (int)DnsResponseCode.NameError;
                    response.Header.ResponseCode =
                        DnsResponseCode.NameError;
                    response.Header.ResponseCodeValue =
                        (int)DnsResponseCode.NameError;
                    result.Kind = DnsAuthoritativeAnswerKind.NameError;
                }
                else
                {
                    result.Kind = DnsAuthoritativeAnswerKind.NoData;
                }

                if (population.Alias)
                {
                    result.Kind = DnsAuthoritativeAnswerKind.Alias;
                }
            }
            else if (population.Alias)
            {
                result.Kind = DnsAuthoritativeAnswerKind.Alias;
            }
        }

        bool rateLimitTruncated = false;
        if (state.RateLimitSlip)
        {
            rateLimitTruncated =
                DnsMessageTruncator.TruncateRecordCount(response, 0);
        }

        bool recordLimitTruncated =
            DnsMessageTruncator.TruncateRecordCount(
                response,
                state.Request.Options.MaximumResponseRecords);
        if (rateLimitTruncated || recordLimitTruncated)
        {
            SynchronizeSelectedRecordSets(result, response);
        }

        DnsAuthoritativeEdnsResult edns =
            AuthoritativeEdnsOperations.PrepareResponse(
                state,
                result,
                response);
        response = edns.Response;
        result.Response = response;
        if (edns.BadVersion)
        {
            result.Kind = DnsAuthoritativeAnswerKind.NotImplemented;
            result.ResponseCode = DnsResponseCode.BadVersion;
            result.ResponseCodeValue = (int)DnsResponseCode.BadVersion;
        }
        else if (edns.BadCookie)
        {
            result.Kind = DnsAuthoritativeAnswerKind.ServerFailure;
            result.ResponseCode = DnsResponseCode.BadCookie;
            result.ResponseCodeValue = (int)DnsResponseCode.BadCookie;
        }

        DnsEncodeOptions encodeOptions = CreateResponseEncodeOptions(
            state.Request,
            edns.PayloadSizeLimit);
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            response,
            encodeOptions);
        if (!encoded.Success)
        {
            result.Kind = DnsAuthoritativeAnswerKind.ServerFailure;
            result.ResponseCode = DnsResponseCode.ServerFailure;
            result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
            response.Header.ResponseCode = DnsResponseCode.ServerFailure;
            response.Header.ResponseCodeValue =
                (int)DnsResponseCode.ServerFailure;
            int index = 0;
            while (index < encoded.Diagnostics.Count)
            {
                result.Diagnostics.Add(encoded.Diagnostics[index]);
                index = index + 1;
            }

            return result;
        }

        result.ResponseBytes = encoded.Bytes;
        if (encoded.Message != null)
        {
            result.Response = encoded.Message;
            response = encoded.Message;
        }

        SynchronizeSelectedRecordSets(result, response);
        result.Truncated = rateLimitTruncated ||
            recordLimitTruncated || encoded.Truncated;
        result.Success = true;
        return result;
    }

    static void SynchronizeSelectedRecordSets(
        DnsAuthoritativeResult result,
        DnsMessage response)
    {
        RetainSelectedRecordSets(
            result.AnswerRecordSets,
            response.Answers);
        RetainSelectedRecordSets(
            result.AuthorityRecordSets,
            response.Authorities);
        RetainSelectedRecordSets(
            result.AdditionalRecordSets,
            response.Additionals);
    }

    static void RetainSelectedRecordSets(
        List<DnsResourceRecordSet> selectedSets,
        List<DnsResourceRecord> responseRecords)
    {
        int setIndex = selectedSets.Count - 1;
        while (setIndex >= 0)
        {
            DnsResourceRecordSet selected = selectedSets[setIndex];
            bool found = false;
            int recordIndex = 0;
            while (recordIndex < responseRecords.Count && !found)
            {
                found = RecordBelongsToSet(
                    responseRecords[recordIndex],
                    selected);
                recordIndex = recordIndex + 1;
            }

            if (!found)
            {
                selectedSets.RemoveAt(setIndex);
            }

            setIndex = setIndex - 1;
        }
    }

    static bool RecordBelongsToSet(
        DnsResourceRecord record,
        DnsResourceRecordSet set)
    {
        int recordType = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue,
            (int)record.RecordType);
        int setType = DnsHeaderCodec.ResolveCode(
            set.RecordTypeValue,
            (int)set.RecordType);
        int recordClass = DnsHeaderCodec.ResolveCode(
            record.ClassValue,
            (int)record.Class);
        int setClass = DnsHeaderCodec.ResolveCode(
            set.ClassValue,
            (int)set.Class);
        return recordType == setType && recordClass == setClass &&
            RecordSetOperations.NamesEqual(record.Name, set.Name);
    }

    static DnsMessage CreateBaseResponse(
        DnsAuthoritativeRequestState state,
        DnsResponseCode responseCode,
        bool authoritative)
    {
        DnsMessage request = state.Request.Message;
        DnsMessage response = new DnsMessage();
        response.Header.Identifier = request.Header.Identifier;
        int responseOpcode = state.OpcodeValue;
        if (responseOpcode < 0 || responseOpcode > 15)
        {
            responseOpcode = (int)DnsOpcode.Query;
        }

        response.Header.Opcode = (DnsOpcode)responseOpcode;
        response.Header.OpcodeValue = responseOpcode;
        response.Header.ResponseCode = responseCode;
        response.Header.ResponseCodeValue = (int)responseCode;
        DnsHeaderFlag flags = DnsHeaderFlag.Response;
        if (authoritative)
        {
            flags = flags | DnsHeaderFlag.AuthoritativeAnswer;
        }

        if (state.Request.Options.EchoRecursionDesired &&
            (state.RequestFlags & (int)DnsHeaderFlag.RecursionDesired) != 0)
        {
            flags = flags | DnsHeaderFlag.RecursionDesired;
        }

        if (state.Request.Options.RecursionAvailable)
        {
            flags = flags | DnsHeaderFlag.RecursionAvailable;
        }

        response.Header.Flags = flags;
        response.Header.FlagsValue = (int)flags;
        if (state.Question != null && CanEchoQuestion(state))
        {
            response.Questions.Add(CopyQuestion(state.Question));
        }

        response.Header.QuestionCount = response.Questions.Count;
        response.Header.AnswerCount = 0;
        response.Header.AuthorityCount = 0;
        response.Header.AdditionalCount = 0;
        return response;
    }

    static bool CanEchoQuestion(DnsAuthoritativeRequestState state)
    {
        if (state.Question == null || state.RecordTypeValue < 0 ||
            state.RecordTypeValue > 65535 || state.ClassValue < 0 ||
            state.ClassValue > 65535)
        {
            return false;
        }

        RawDnsNameResult name = RawDnsName.ValidateName(state.Question.Name);
        return name.Success && name.Name != null && name.Name.IsAbsolute;
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

    static DnsEncodeOptions CreateResponseEncodeOptions(
        DnsAuthoritativeRequest request,
        int udpPayloadSize)
    {
        DnsEncodeOptions result = new DnsEncodeOptions();
        if (request.Transport == DnsTransportKind.Udp)
        {
            result.MaximumMessageLength =
                request.Options.MaximumUdpResponseLength;
            result.MaximumUdpPayloadSize = udpPayloadSize;
            result.TruncateToMaximumLength = true;
        }
        else
        {
            result.MaximumMessageLength =
                request.Options.MaximumTcpResponseLength;
        }

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

    static DnsAuthoritativeResult FailedResult(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsAuthoritativeResult result = new DnsAuthoritativeResult();
        result.Kind = DnsAuthoritativeAnswerKind.ServerFailure;
        result.ResponseCode = DnsResponseCode.ServerFailure;
        result.ResponseCodeValue = (int)DnsResponseCode.ServerFailure;
        result.Diagnostics.Add(CreateDiagnostic(code, field, reason));
        return result;
    }
}
