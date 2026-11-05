using System;
using System.Collections.Generic;

namespace diameter;

internal class AccountingServerResult
{
    internal bool Accepted;
    internal bool Duplicate;
    internal bool Replayed;
    internal bool ApplicationInvoked;
    internal DiameterAccountingRequest? Request;
    internal DiameterAccountingAnswer? AnswerData;
    internal DiameterMessage? Answer;
    internal uint ResultCode;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class AccountingServerParseState
{
    internal bool Valid = true;
    internal uint ResultCode = DiameterResultCodes.Success;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterIdentity OriginRealm = new DiameterIdentity();
    internal DiameterAccountingRequest Request =
        new DiameterAccountingRequest();
    internal int SessionCount;
    internal int OriginHostCount;
    internal int OriginRealmCount;
    internal int DestinationRealmCount;
    internal int DestinationHostCount;
    internal int ApplicationCount;
    internal int UserNameCount;
    internal int RecordTypeCount;
    internal int RecordNumberCount;
    internal int SubSessionCount;
    internal int SessionIdentifierCount;
    internal int MultiSessionCount;
    internal int IntervalCount;
    internal int RealtimeCount;
    internal int EventTimestampCount;
}

internal class AccountingServerProcessedRecord
{
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterAccountingRequest Request =
        new DiameterAccountingRequest();
    internal DiameterMessage SourceMessage = new DiameterMessage();
    internal DiameterAccountingAnswer AnswerData =
        new DiameterAccountingAnswer();
    internal DiameterMessage Answer = new DiameterMessage();
    internal DiameterAccountingState State;
}

internal class AccountingServerStateMachine
{
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterIdentity OriginRealm = new DiameterIdentity();
    internal IDiameterBaseCommandFactory CommandFactory;
    internal IDuplicateRequestStore DuplicateStore;
    internal IDiameterAccountingHandler Handler;
    internal IDiameterClock Clock;
    internal object ServerLock = new object();
    internal List<AccountingServerProcessedRecord> ProcessedRecords =
        new List<AccountingServerProcessedRecord>();

    internal AccountingServerStateMachine(
        DiameterIdentity originHost,
        DiameterIdentity originRealm,
        IDiameterBaseCommandFactory commandFactory,
        IDuplicateRequestStore duplicateStore,
        IDiameterAccountingHandler handler,
        IDiameterClock clock)
    {
        if (originHost == null || originRealm == null ||
            commandFactory == null || duplicateStore == null ||
            handler == null || clock == null)
        {
            throw new ArgumentException(
                "Accounting server identities and dependencies are required.");
        }

        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Accounting server identities must be valid.");
        }

        OriginHost.Value = normalizedHost.Text;
        OriginRealm.Value = normalizedRealm.Text;
        CommandFactory = commandFactory;
        DuplicateStore = duplicateStore;
        Handler = handler;
        Clock = clock;
    }

    internal AccountingServerResult ProcessAccountingServerRequest(
        DiameterMessage? message,
        DiameterIdentity? peerIdentity)
    {
        DiameterMessage input = ValidateAccountingServerHeader(message);
        if (peerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The accounting transport peer identity is invalid.");
        }

        AccountingServerParseState parsed = ParseAccountingServerRequest(
            input,
            peerIdentity);
        if (!parsed.Valid)
        {
            return CreateAccountingServerFailure(
                input,
                parsed.Request,
                parsed.ResultCode,
                parsed.Code,
                parsed.Message,
                false);
        }

        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterDuplicateResult observation =
            DuplicateStore.ObserveDiameterDuplicateRequest(
                input,
                parsed.OriginHost,
                now);
        if (observation.Duplicate)
        {
            if (observation.CachedAnswerAvailable &&
                observation.CachedAnswer != null)
            {
                return CreateAccountingServerReplay(
                    parsed.Request,
                    observation.CachedAnswer,
                    "accounting-protocol-duplicate");
            }

            return CreateAccountingServerFailure(
                input,
                parsed.Request,
                DiameterResultCodes.UnableToComply,
                observation.Code,
                observation.Message,
                true);
        }

        lock (ServerLock)
        {
            AccountingServerProcessedRecord? duplicate =
                FindAccountingServerProcessedRecord(
                    parsed.OriginHost,
                    parsed.Request);
            if (duplicate != null)
            {
                if (!AreAccountingServerMessagesEquivalent(
                    duplicate.SourceMessage,
                    input))
                {
                    AccountingServerResult collision =
                        CreateAccountingServerFailure(
                            input,
                            parsed.Request,
                            DiameterResultCodes.UnableToComply,
                            "accounting-record-key-collision",
                            "The accounting record key was reused with different data.",
                            true);
                    CacheAccountingServerAnswer(
                        observation,
                        collision.Answer);
                    return collision;
                }

                DiameterMessage replay =
                    CreateAccountingServerCorrelatedReplay(
                        duplicate.Answer,
                        input);
                CacheAccountingServerAnswer(observation, replay);
                return CreateAccountingServerReplay(
                    parsed.Request,
                    replay,
                    "accounting-record-duplicate");
            }

            uint orderFailure = ValidateAccountingServerOrder(
                parsed.OriginHost,
                parsed.Request);
            if (orderFailure != DiameterResultCodes.Success)
            {
                AccountingServerResult rejected =
                    CreateAccountingServerFailure(
                        input,
                        parsed.Request,
                        orderFailure,
                        "accounting-record-out-of-order",
                        "The accounting record is not the next valid record in its sequence.",
                        false);
                CacheAccountingServerAnswer(
                    observation,
                    rejected.Answer);
                return rejected;
            }

            DiameterAccountingRequest handoff =
                AccountingRecordCloner.CloneAccountingRequest(
                    parsed.Request);
            DiameterAccountingAnswer applicationAnswer =
                Handler.HandleDiameterAccountingRequest(handoff);
            ValidateAccountingServerApplicationAnswer(
                parsed.Request,
                applicationAnswer);
            DiameterAccountingAnswer ownedAnswer =
                AccountingRecordCloner.CloneAccountingAnswer(
                    applicationAnswer);
            ApplyAccountingServerEchoes(parsed.Request, ownedAnswer);
            DiameterMessage answer = CreateAccountingServerAnswerMessage(
                input,
                ownedAnswer);
            StoreAccountingServerProcessedRecord(
                parsed.OriginHost,
                parsed.Request,
                input,
                ownedAnswer,
                answer);
            CacheAccountingServerAnswer(observation, answer);
            AccountingServerResult result = new AccountingServerResult();
            result.Accepted = ownedAnswer.ResultCode >= 2000U &&
                ownedAnswer.ResultCode <= 2999U;
            result.ApplicationInvoked = true;
            result.Request =
                AccountingRecordCloner.CloneAccountingRequest(
                    parsed.Request);
            result.AnswerData =
                AccountingRecordCloner.CloneAccountingAnswer(
                    ownedAnswer);
            result.Answer =
                AccountingRecordCloner.CloneOptionalAccountingMessage(
                    answer);
            result.ResultCode = ownedAnswer.ResultCode;
            result.Code = result.Accepted ?
                "accounting-record-processed" :
                "accounting-record-rejected";
            result.Message = result.Accepted ?
                "The accounting record was processed in order." :
                "The accounting application rejected the record.";
            return result;
        }
    }

    internal DiameterMessage ValidateAccountingServerHeader(
        DiameterMessage? message)
    {
        if (message == null || message.Avps == null)
        {
            throw new ArgumentException(
                "An owned Accounting-Request is required.");
        }

        DiameterMessage input = message;
        int flags = (int)input.Flags;
        if (input.Version != DiameterProtocolConstants.Version ||
            input.CommandCode != DiameterCommandCodes.Accounting ||
            input.ApplicationId != DiameterApplicationIds.BaseAccounting ||
            (input.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (input.Flags & DiameterMessageFlags.Proxyable) ==
                DiameterMessageFlags.None ||
            (input.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            flags < 0 || flags > 255 ||
            (flags & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            throw new ArgumentException(
                "The message is not a valid base Accounting-Request.");
        }

        int index = 0;
        while (index < input.Avps.Count)
        {
            DiameterAvp avp = input.Avps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Accounting AVPs must own their payloads.");
            }

            index = index + 1;
        }

        return input;
    }

    internal AccountingServerParseState ParseAccountingServerRequest(
        DiameterMessage message,
        DiameterIdentity? peerIdentity)
    {
        AccountingServerParseState state =
            new AccountingServerParseState();
        state.Request.ApplicationId = message.ApplicationId;
        state.Request.PeerIdentity =
            AccountingRecordCloner.CloneOptionalAccountingIdentity(
                peerIdentity);
        int index = 0;
        while (index < message.Avps.Count && state.Valid)
        {
            ParseAccountingServerAvp(state, message.Avps[index]);
            index = index + 1;
        }

        if (state.Valid)
        {
            ValidateAccountingServerCounts(state);
        }

        return state;
    }

    internal void ParseAccountingServerAvp(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.VendorId != DiameterVendorIds.Ietf)
        {
            state.Request.ServiceAvps.Add(
                AccountingRecordCloner.CloneAccountingAvp(avp));
            return;
        }

        switch (avp.Code)
        {
            case DiameterAvpCodes.SessionId:
                state.SessionCount = state.SessionCount + 1;
                state.Request.SessionId = ReadAccountingServerText(
                    state,
                    avp,
                    false);
                break;
            case DiameterAvpCodes.OriginHost:
                state.OriginHostCount = state.OriginHostCount + 1;
                state.OriginHost = ReadAccountingServerIdentity(
                    state,
                    avp,
                    false);
                break;
            case DiameterAvpCodes.OriginRealm:
                state.OriginRealmCount = state.OriginRealmCount + 1;
                state.OriginRealm = ReadAccountingServerIdentity(
                    state,
                    avp,
                    true);
                break;
            case DiameterAvpCodes.DestinationRealm:
                state.DestinationRealmCount =
                    state.DestinationRealmCount + 1;
                state.Request.DestinationRealm =
                    ReadAccountingServerIdentity(
                        state,
                        avp,
                        true);
                break;
            case DiameterAvpCodes.DestinationHost:
                state.DestinationHostCount =
                    state.DestinationHostCount + 1;
                state.Request.DestinationHost =
                    ReadAccountingServerIdentity(
                        state,
                        avp,
                        false);
                break;
            case DiameterAvpCodes.AcctApplicationId:
                state.ApplicationCount = state.ApplicationCount + 1;
                ReadAccountingServerApplication(state, avp);
                break;
            case DiameterAvpCodes.UserName:
                state.UserNameCount = state.UserNameCount + 1;
                state.Request.UserName = ReadAccountingServerText(
                    state,
                    avp,
                    true);
                break;
            case DiameterAvpCodes.AccountingRecordType:
                state.RecordTypeCount = state.RecordTypeCount + 1;
                ReadAccountingServerRecordType(state, avp);
                break;
            case DiameterAvpCodes.AccountingRecordNumber:
                state.RecordNumberCount = state.RecordNumberCount + 1;
                ReadAccountingServerRecordNumber(state, avp);
                break;
            case DiameterAvpCodes.AccountingSubSessionId:
                state.SubSessionCount = state.SubSessionCount + 1;
                ReadAccountingServerSubSession(state, avp);
                break;
            case DiameterAvpCodes.AcctSessionId:
                state.SessionIdentifierCount =
                    state.SessionIdentifierCount + 1;
                state.Request.AccountingSessionId =
                    AccountingRecordCloner.CloneAccountingBytes(
                        avp.Data);
                break;
            case DiameterAvpCodes.AcctMultiSessionId:
                state.MultiSessionCount = state.MultiSessionCount + 1;
                state.Request.AccountingMultiSessionId =
                    ReadAccountingServerText(state, avp, true);
                break;
            case DiameterAvpCodes.AcctInterimInterval:
                state.IntervalCount = state.IntervalCount + 1;
                ReadAccountingServerInterval(state, avp);
                break;
            case DiameterAvpCodes.AccountingRealtimeRequired:
                state.RealtimeCount = state.RealtimeCount + 1;
                ReadAccountingServerRealtime(state, avp);
                break;
            case DiameterAvpCodes.EventTimestamp:
                state.EventTimestampCount =
                    state.EventTimestampCount + 1;
                ReadAccountingServerEventTimestamp(state, avp);
                break;
            case DiameterAvpCodes.OriginStateId:
            case DiameterAvpCodes.ProxyInfo:
            case DiameterAvpCodes.RouteRecord:
                break;
            default:
                state.Request.ServiceAvps.Add(
                    AccountingRecordCloner.CloneAccountingAvp(avp));
                break;
        }
    }

    internal string ReadAccountingServerText(
        AccountingServerParseState state,
        DiameterAvp avp,
        bool allowEmpty)
    {
        DiameterValueResult decoded = ValueCodecs.DecodeUtf8String(
            avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success ||
            (!allowEmpty && decoded.Text.Length == 0))
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-invalid-text",
                "An accounting text AVP is invalid.");
            return string.Empty;
        }

        return decoded.Text;
    }

    internal DiameterIdentity ReadAccountingServerIdentity(
        AccountingServerParseState state,
        DiameterAvp avp,
        bool realm)
    {
        DiameterValueResult decoded = ValueCodecs.DecodeUtf8String(
            avp.Data);
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = decoded.Text;
        DiameterValueResult normalized = realm ?
            ValueCodecs.NormalizeDiameterRealm(identity) :
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (decoded.Status != DiameterCodecStatus.Success ||
            normalized.Status != DiameterCodecStatus.Success)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-invalid-identity",
                "An accounting identity AVP is invalid.");
            return new DiameterIdentity();
        }

        identity.Value = normalized.Text;
        return identity;
    }

    internal void ReadAccountingServerApplication(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4 ||
            NetworkByteOrder.ReadUInt32(avp.Data) !=
                DiameterApplicationIds.BaseAccounting)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-application-mismatch",
                "The accounting application identifier is invalid.");
        }
    }

    internal void ReadAccountingServerRecordType(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            SetAccountingServerLengthFailure(state);
            return;
        }

        uint value = NetworkByteOrder.ReadUInt32(avp.Data);
        if (value < (uint)DiameterAccountingRecordType.Event ||
            value > (uint)DiameterAccountingRecordType.Stop)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-invalid-record-type",
                "The Accounting-Record-Type is invalid.");
            return;
        }

        state.Request.RecordType = (DiameterAccountingRecordType)value;
    }

    internal void ReadAccountingServerRecordNumber(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            SetAccountingServerLengthFailure(state);
            return;
        }

        state.Request.RecordNumber = NetworkByteOrder.ReadUInt32(avp.Data);
    }

    internal void ReadAccountingServerSubSession(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 8)
        {
            SetAccountingServerLengthFailure(state);
            return;
        }

        state.Request.AccountingSubSessionId =
            NetworkByteOrder.ReadUInt64(avp.Data);
        state.Request.AccountingSubSessionIdPresent = true;
    }

    internal void ReadAccountingServerInterval(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            SetAccountingServerLengthFailure(state);
            return;
        }

        uint seconds = NetworkByteOrder.ReadUInt32(avp.Data);
        if (seconds > (uint)(int.MaxValue / 1000))
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-interval-overflow",
                "The accounting interim interval is too large.");
            return;
        }

        state.Request.InterimIntervalMilliseconds = (int)seconds * 1000;
    }

    internal void ReadAccountingServerRealtime(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            SetAccountingServerLengthFailure(state);
            return;
        }

        uint value = NetworkByteOrder.ReadUInt32(avp.Data);
        if (value <
                (uint)DiameterAccountingRealtimeRequired.DeliverAndGrant ||
            value >
                (uint)DiameterAccountingRealtimeRequired.GrantAndLose)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-invalid-realtime-policy",
                "The Accounting-Realtime-Required value is invalid.");
            return;
        }

        state.Request.RealtimeRequired =
            (DiameterAccountingRealtimeRequired)value;
    }

    internal void ReadAccountingServerEventTimestamp(
        AccountingServerParseState state,
        DiameterAvp avp)
    {
        DiameterValueResult decoded = ValueCodecs.DecodeTime(
            avp.Data,
            Clock.ReadUtcTime());
        if (decoded.Status != DiameterCodecStatus.Success ||
            decoded.ConsumedCount != avp.Data.Length)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.InvalidAvpValue,
                "accounting-invalid-event-time",
                "The accounting event timestamp is invalid.");
            return;
        }

        state.Request.EventTimestamp = decoded.Time;
        state.Request.EventTimestampPresent = true;
    }

    internal void ValidateAccountingServerCounts(
        AccountingServerParseState state)
    {
        if (state.SessionCount == 0 || state.OriginHostCount == 0 ||
            state.OriginRealmCount == 0 ||
            state.DestinationRealmCount == 0 ||
            state.RecordTypeCount == 0 || state.RecordNumberCount == 0)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.MissingAvp,
                "accounting-required-avp-missing",
                "The Accounting-Request is missing a required AVP.");
            return;
        }

        if (state.SessionCount > 1 || state.OriginHostCount > 1 ||
            state.OriginRealmCount > 1 ||
            state.DestinationRealmCount > 1 ||
            state.DestinationHostCount > 1 ||
            state.ApplicationCount > 1 || state.UserNameCount > 1 ||
            state.RecordTypeCount > 1 || state.RecordNumberCount > 1 ||
            state.SubSessionCount > 1 ||
            state.SessionIdentifierCount > 1 ||
            state.MultiSessionCount > 1 || state.IntervalCount > 1 ||
            state.RealtimeCount > 1 ||
            state.EventTimestampCount > 1)
        {
            SetAccountingServerParseFailure(
                state,
                DiameterResultCodes.AvpOccursTooManyTimes,
                "accounting-avp-repeated",
                "An accounting AVP occurs too many times.");
        }
    }

    internal void SetAccountingServerLengthFailure(
        AccountingServerParseState state)
    {
        SetAccountingServerParseFailure(
            state,
            DiameterResultCodes.InvalidAvpLength,
            "accounting-invalid-avp-length",
            "An accounting AVP has an invalid payload length.");
    }

    internal void SetAccountingServerParseFailure(
        AccountingServerParseState state,
        uint resultCode,
        string code,
        string message)
    {
        state.Valid = false;
        state.ResultCode = resultCode;
        state.Code = code;
        state.Message = message;
    }

    internal uint ValidateAccountingServerOrder(
        DiameterIdentity originHost,
        DiameterAccountingRequest request)
    {
        if (request.RecordType == DiameterAccountingRecordType.Event)
        {
            return request.RecordNumber == 0U ?
                DiameterResultCodes.Success :
                DiameterResultCodes.InvalidAvpValue;
        }

        AccountingServerProcessedRecord? latest =
            FindLatestAccountingServerSequence(originHost, request);
        if (request.RecordType == DiameterAccountingRecordType.Start)
        {
            if (request.RecordNumber != 0U)
            {
                return DiameterResultCodes.InvalidAvpValue;
            }

            return latest == null ?
                DiameterResultCodes.Success :
                DiameterResultCodes.InvalidAvpValue;
        }

        if (latest == null || latest.State != DiameterAccountingState.Open)
        {
            return DiameterResultCodes.UnknownSessionId;
        }

        if (latest.Request.RecordNumber == uint.MaxValue ||
            request.RecordNumber != latest.Request.RecordNumber + 1U)
        {
            return DiameterResultCodes.InvalidAvpValue;
        }

        if (latest.Request.ApplicationId != request.ApplicationId ||
            latest.Request.AccountingMultiSessionId !=
                request.AccountingMultiSessionId ||
            !AreAccountingServerBytesEqual(
                latest.Request.AccountingSessionId,
                request.AccountingSessionId))
        {
            return DiameterResultCodes.ContradictingAvps;
        }

        return DiameterResultCodes.Success;
    }

    internal AccountingServerProcessedRecord?
        FindLatestAccountingServerSequence(
            DiameterIdentity originHost,
            DiameterAccountingRequest request)
    {
        int index = ProcessedRecords.Count - 1;
        while (index >= 0)
        {
            AccountingServerProcessedRecord candidate =
                ProcessedRecords[index];
            if (candidate.Request.RecordType !=
                    DiameterAccountingRecordType.Event &&
                IsSameAccountingServerSequence(
                    candidate.OriginHost,
                    candidate.Request,
                    originHost,
                    request))
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal AccountingServerProcessedRecord?
        FindAccountingServerProcessedRecord(
            DiameterIdentity originHost,
            DiameterAccountingRequest request)
    {
        int index = ProcessedRecords.Count - 1;
        while (index >= 0)
        {
            AccountingServerProcessedRecord candidate =
                ProcessedRecords[index];
            if (candidate.Request.RecordType == request.RecordType &&
                candidate.Request.RecordNumber == request.RecordNumber &&
                IsSameAccountingServerSequence(
                    candidate.OriginHost,
                    candidate.Request,
                    originHost,
                    request))
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool IsSameAccountingServerSequence(
        DiameterIdentity leftOrigin,
        DiameterAccountingRequest left,
        DiameterIdentity rightOrigin,
        DiameterAccountingRequest right)
    {
        return leftOrigin.Value == rightOrigin.Value &&
            left.SessionId == right.SessionId &&
            left.AccountingSubSessionIdPresent ==
                right.AccountingSubSessionIdPresent &&
            (!left.AccountingSubSessionIdPresent ||
            left.AccountingSubSessionId == right.AccountingSubSessionId);
    }

    internal bool AreAccountingServerBytesEqual(
        byte[] left,
        byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool AreAccountingServerMessagesEquivalent(
        DiameterMessage left,
        DiameterMessage right)
    {
        int leftFlags = (int)left.Flags &
            ~(int)DiameterMessageFlags.PotentiallyRetransmitted;
        int rightFlags = (int)right.Flags &
            ~(int)DiameterMessageFlags.PotentiallyRetransmitted;
        if (left.Version != right.Version || leftFlags != rightFlags ||
            left.CommandCode != right.CommandCode ||
            left.ApplicationId != right.ApplicationId ||
            left.Avps.Count != right.Avps.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Avps.Count)
        {
            DiameterAvp leftAvp = left.Avps[index];
            DiameterAvp rightAvp = right.Avps[index];
            if (leftAvp.Code != rightAvp.Code ||
                leftAvp.Flags != rightAvp.Flags ||
                leftAvp.VendorId != rightAvp.VendorId ||
                !AreAccountingServerBytesEqual(
                    leftAvp.Data,
                    rightAvp.Data))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateAccountingServerApplicationAnswer(
        DiameterAccountingRequest request,
        DiameterAccountingAnswer answer)
    {
        if (answer == null)
        {
            throw new ArgumentException(
                "The accounting handler must return an answer.");
        }

        answer.ValidateAccountingAnswer();
        if (answer.SessionId != request.SessionId ||
            answer.RecordType != request.RecordType ||
            answer.RecordNumber != request.RecordNumber ||
            (answer.AccountingSubSessionIdPresent &&
            (!request.AccountingSubSessionIdPresent ||
            answer.AccountingSubSessionId !=
                request.AccountingSubSessionId)))
        {
            throw new ArgumentException(
                "The application answer does not match the accounting request.");
        }

        int index = 0;
        while (index < answer.ServiceAvps.Count)
        {
            DiameterAvp avp = answer.ServiceAvps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                IsManagedAccountingServerAnswerAvp(avp.Code))
            {
                throw new ArgumentException(
                    "Application AVPs cannot replace accounting answer AVPs.");
            }

            index = index + 1;
        }
    }

    internal void ApplyAccountingServerEchoes(
        DiameterAccountingRequest request,
        DiameterAccountingAnswer answer)
    {
        if (request.AccountingSubSessionIdPresent)
        {
            answer.AccountingSubSessionId =
                request.AccountingSubSessionId;
            answer.AccountingSubSessionIdPresent = true;
        }
    }

    internal bool IsManagedAccountingServerAnswerAvp(uint code)
    {
        return code == DiameterAvpCodes.SessionId ||
            code == DiameterAvpCodes.ResultCode ||
            code == DiameterAvpCodes.OriginHost ||
            code == DiameterAvpCodes.OriginRealm ||
            code == DiameterAvpCodes.AccountingRecordType ||
            code == DiameterAvpCodes.AccountingRecordNumber ||
            code == DiameterAvpCodes.AccountingSubSessionId ||
            code == DiameterAvpCodes.AcctInterimInterval ||
            code == DiameterAvpCodes.AccountingRealtimeRequired ||
            code == DiameterAvpCodes.ErrorMessage;
    }

    internal DiameterMessage CreateAccountingServerAnswerMessage(
        DiameterMessage request,
        DiameterAccountingAnswer answer)
    {
        DiameterBaseCommandOptions options =
            new DiameterBaseCommandOptions();
        options.OriginHost = OriginHost;
        options.OriginRealm = OriginRealm;
        options.Request = request;
        options.ResultCode = answer.ResultCode;
        DiameterMessage message =
            CommandFactory.CreateAccountingAnswer(options);
        if (answer.AccountingSubSessionIdPresent)
        {
            message.Avps.Add(CreateAccountingServerUnsigned64Avp(
                DiameterAvpCodes.AccountingSubSessionId,
                answer.AccountingSubSessionId));
        }

        if (answer.InterimIntervalPresent)
        {
            long seconds =
                ((long)answer.InterimIntervalMilliseconds + 999L) /
                1000L;
            message.Avps.Add(CreateAccountingServerUnsigned32Avp(
                DiameterAvpCodes.AcctInterimInterval,
                (uint)seconds));
        }

        if (answer.RealtimeRequiredPresent)
        {
            message.Avps.Add(CreateAccountingServerUnsigned32Avp(
                DiameterAvpCodes.AccountingRealtimeRequired,
                (uint)answer.RealtimeRequired));
        }

        if (answer.ErrorMessage.Length > 0)
        {
            message.Avps.Add(CreateAccountingServerTextAvp(
                DiameterAvpCodes.ErrorMessage,
                answer.ErrorMessage));
        }

        int index = 0;
        while (index < answer.ServiceAvps.Count)
        {
            message.Avps.Add(
                AccountingRecordCloner.CloneAccountingAvp(
                    answer.ServiceAvps[index]));
            index = index + 1;
        }

        answer.Message =
            AccountingRecordCloner.CloneOptionalAccountingMessage(
                message);
        return message;
    }

    internal DiameterAvp CreateAccountingServerUnsigned32Avp(
        uint code,
        uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }

    internal DiameterAvp CreateAccountingServerUnsigned64Avp(
        uint code,
        ulong value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[8];
        NetworkByteOrder.WriteUInt64(result.Data, value);
        return result;
    }

    internal DiameterAvp CreateAccountingServerTextAvp(
        uint code,
        string value)
    {
        DiameterValueResult measurement = ValueCodecs.EncodeUtf8String(
            new byte[0],
            value);
        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoded = ValueCodecs.EncodeUtf8String(
            data,
            value);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The accounting answer text is invalid.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal AccountingServerResult CreateAccountingServerFailure(
        DiameterMessage request,
        DiameterAccountingRequest parsedRequest,
        uint resultCode,
        string code,
        string message,
        bool duplicate)
    {
        DiameterAccountingAnswer answerData =
            new DiameterAccountingAnswer();
        answerData.SessionId = parsedRequest.SessionId;
        answerData.RecordType = parsedRequest.RecordType;
        answerData.RecordNumber = parsedRequest.RecordNumber;
        answerData.ResultCode = resultCode;
        answerData.ErrorMessage = message;
        if (parsedRequest.AccountingSubSessionIdPresent)
        {
            answerData.AccountingSubSessionId =
                parsedRequest.AccountingSubSessionId;
            answerData.AccountingSubSessionIdPresent = true;
        }

        DiameterMessage? answer = null;
        if (CanCreateAccountingServerAnswer(request) &&
            parsedRequest.SessionId.Length > 0 &&
            (int)parsedRequest.RecordType >=
                (int)DiameterAccountingRecordType.Event &&
            (int)parsedRequest.RecordType <=
                (int)DiameterAccountingRecordType.Stop)
        {
            answer = CreateAccountingServerAnswerMessage(
                request,
                answerData);
        }

        AccountingServerResult result = new AccountingServerResult();
        result.Duplicate = duplicate;
        result.Request =
            AccountingRecordCloner.CloneAccountingRequest(parsedRequest);
        result.AnswerData =
            AccountingRecordCloner.CloneAccountingAnswer(answerData);
        result.Answer =
            AccountingRecordCloner.CloneOptionalAccountingMessage(answer);
        result.ResultCode = resultCode;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal bool CanCreateAccountingServerAnswer(DiameterMessage request)
    {
        int sessionCount = 0;
        int typeCount = 0;
        int numberCount = 0;
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                if (avp.Code == DiameterAvpCodes.SessionId)
                {
                    sessionCount = sessionCount + 1;
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingRecordType)
                {
                    typeCount = typeCount + 1;
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingRecordNumber)
                {
                    numberCount = numberCount + 1;
                }
            }

            index = index + 1;
        }

        return sessionCount == 1 && typeCount == 1 &&
            numberCount == 1;
    }

    internal AccountingServerResult CreateAccountingServerReplay(
        DiameterAccountingRequest request,
        DiameterMessage answer,
        string code)
    {
        AccountingServerResult result = new AccountingServerResult();
        result.Duplicate = true;
        result.Replayed = true;
        result.Request =
            AccountingRecordCloner.CloneAccountingRequest(request);
        result.Answer =
            AccountingRecordCloner.CloneOptionalAccountingMessage(answer);
        result.ResultCode = ReadAccountingServerResultCode(answer);
        result.Accepted = result.ResultCode >= 2000U &&
            result.ResultCode <= 2999U;
        result.Code = code;
        result.Message = "A cached Accounting-Answer was replayed.";
        return result;
    }

    internal uint ReadAccountingServerResultCode(DiameterMessage answer)
    {
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ResultCode &&
                avp.Data.Length == 4)
            {
                return NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        return 0U;
    }

    internal DiameterMessage CreateAccountingServerCorrelatedReplay(
        DiameterMessage cached,
        DiameterMessage request)
    {
        DiameterMessage? clone =
            AccountingRecordCloner.CloneOptionalAccountingMessage(cached);
        if (clone == null)
        {
            throw new InvalidOperationException(
                "A cached Accounting-Answer is required.");
        }

        clone.HopByHopIdentifier = request.HopByHopIdentifier;
        clone.EndToEndIdentifier = request.EndToEndIdentifier;
        return clone;
    }

    internal void CacheAccountingServerAnswer(
        DiameterDuplicateResult observation,
        DiameterMessage? answer)
    {
        if (answer == null)
        {
            return;
        }

        observation.CachedAnswer =
            AccountingRecordCloner.CloneOptionalAccountingMessage(answer);
        observation.ReplaySafe = true;
        DuplicateStore.CacheDiameterDuplicateAnswer(observation);
    }

    internal void StoreAccountingServerProcessedRecord(
        DiameterIdentity originHost,
        DiameterAccountingRequest request,
        DiameterMessage source,
        DiameterAccountingAnswer answerData,
        DiameterMessage answer)
    {
        AccountingServerProcessedRecord record =
            new AccountingServerProcessedRecord();
        record.OriginHost =
            AccountingRecordCloner.CloneAccountingIdentity(originHost);
        record.Request =
            AccountingRecordCloner.CloneAccountingRequest(request);
        DiameterMessage? sourceClone =
            AccountingRecordCloner.CloneOptionalAccountingMessage(source);
        DiameterMessage? answerClone =
            AccountingRecordCloner.CloneOptionalAccountingMessage(answer);
        if (sourceClone == null || answerClone == null)
        {
            throw new InvalidOperationException(
                "Processed accounting messages must be retained.");
        }

        record.SourceMessage = sourceClone;
        record.AnswerData =
            AccountingRecordCloner.CloneAccountingAnswer(answerData);
        record.Answer = answerClone;
        if (answerData.ResultCode < 2000U ||
            answerData.ResultCode > 2999U)
        {
            record.State = DiameterAccountingState.Failed;
        }
        else if (request.RecordType == DiameterAccountingRecordType.Event ||
            request.RecordType == DiameterAccountingRecordType.Stop)
        {
            record.State = DiameterAccountingState.Closed;
        }
        else
        {
            record.State = DiameterAccountingState.Open;
        }

        ProcessedRecords.Add(record);
    }
}
