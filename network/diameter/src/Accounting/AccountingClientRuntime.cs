using System;
using System.Collections.Generic;

namespace diameter;

internal class AccountingPendingLink
{
    internal long RecordIdentifier;
    internal long RequestIdentifier;
}

internal class AccountingAnswerValues
{
    internal bool Valid;
    internal string SessionId = string.Empty;
    internal DiameterAccountingRecordType RecordType;
    internal uint RecordNumber;
    internal uint ResultCode;
    internal ulong AccountingSubSessionId;
    internal bool AccountingSubSessionIdPresent;
    internal int InterimIntervalMilliseconds;
    internal bool InterimIntervalPresent;
    internal DiameterAccountingRealtimeRequired RealtimeRequired;
    internal bool RealtimeRequiredPresent;
    internal string ErrorMessage = string.Empty;
    internal List<DiameterAvp> ServiceAvps = new List<DiameterAvp>();
}

internal class AccountingClientRuntime
{
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal DiameterIdentity OriginRealm = new DiameterIdentity();
    internal IDiameterBaseAvpFactory AvpFactory;
    internal IDiameterRequestClient RequestClient;
    internal IAccountingRecordStore Store;
    internal IDiameterClock Clock;
    internal object AccountingLock = new object();
    internal List<DiameterAccountingRecord> Records =
        new List<DiameterAccountingRecord>();
    internal List<AccountingPendingLink> PendingLinks =
        new List<AccountingPendingLink>();
    internal DiameterAccountingRecord? PendingInterimOverwrite;
    internal long NextRecordIdentifier = 1;

    internal AccountingClientRuntime(
        DiameterIdentity originHost,
        DiameterIdentity originRealm,
        IDiameterBaseAvpFactory avpFactory,
        IDiameterRequestClient requestClient,
        IAccountingRecordStore store,
        IDiameterClock clock)
    {
        OriginHost = AccountingRecordCloner.CloneAccountingIdentity(
            originHost);
        OriginRealm = AccountingRecordCloner.CloneAccountingIdentity(
            originRealm);
        AvpFactory = avpFactory;
        RequestClient = requestClient;
        Store = store;
        Clock = clock;
    }

    internal DiameterAccountingResult SendAccountingClientRecord(
        DiameterAccountingRequest? request,
        DiameterAccountingRecordType recordType,
        IDiameterAnswerHandler handler)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "An accounting request is required.");
        }

        request.ValidateAccountingRequest();
        ValidateAccountingExtensionAvps(request);
        DiameterAccountingRequest owned =
            AccountingRecordCloner.CloneAccountingRequest(request);
        owned.RecordType = recordType;
        owned.ValidateAccountingRequest();
        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterAccountingRecord record;
        lock (AccountingLock)
        {
            PendingInterimOverwrite = null;
            PrepareAccountingSequence(owned);
            record = CreatePendingAccountingRecord(owned, now);
            Records.Add(record);
            if ((RequiresDurableAccountingRecord(record) ||
                PendingInterimOverwrite != null) &&
                !PersistAccountingClientRecord(record))
            {
                record.State = DiameterAccountingState.Failed;
                record.Code = "accounting-store-failed";
                record.Message =
                    "The accounting record could not be stored durably.";
                DiameterSendResult storageFailure =
                    new DiameterSendResult();
                storageFailure.Status = DiameterSendStatus.Failed;
                storageFailure.Code = record.Code;
                storageFailure.Message = record.Message;
                return CreateAccountingOperationResult(
                    record,
                    storageFailure);
            }

            if (PendingInterimOverwrite != null)
            {
                SupersedePendingAccountingInterim(
                    PendingInterimOverwrite);
                PendingInterimOverwrite = null;
            }
        }

        DiameterSendRequest sendRequest = CreateAccountingSendRequest(
            record);
        DiameterSendResult send = RequestClient.SendDiameterRequest(
            sendRequest,
            handler);
        lock (AccountingLock)
        {
            ApplyAccountingSendResult(record, send, now);
            if (!send.Accepted && record.Durable)
            {
                RetainAccountingClientRecord(record, now);
            }
        }

        return CreateAccountingOperationResult(record, send);
    }

    internal List<DiameterAccountingRecord> ReadAccountingClientRecords()
    {
        List<DiameterAccountingRecord> result =
            new List<DiameterAccountingRecord>();
        lock (AccountingLock)
        {
            int index = 0;
            while (index < Records.Count)
            {
                result.Add(
                    AccountingRecordCloner.CloneAccountingRecord(
                        Records[index]));
                index = index + 1;
            }
        }

        return result;
    }

    internal void HandleAccountingClientAnswer(DiameterAnswerResult result)
    {
        if (result == null)
        {
            return;
        }

        DateTimeOffset now = Clock.ReadUtcTime();
        lock (AccountingLock)
        {
            DiameterAccountingRecord? record =
                FindAccountingRecordForAnswer(result);
            if (record == null)
            {
                return;
            }

            ApplyAccountingAttemptMetadata(record, result);
            bool retryable = ApplyAccountingAnswerResult(
                record,
                result,
                now);
            if (retryable && record.Durable)
            {
                RetainAccountingClientRecord(record, now);
            }
            else if (record.Durable)
            {
                RemoveDurableAccountingClientRecord(record);
            }

            RemoveAccountingPendingLink(record.RecordIdentifier);
        }
    }

    internal void PrepareAccountingSequence(
        DiameterAccountingRequest request)
    {
        if (request.RecordType == DiameterAccountingRecordType.Event)
        {
            request.RecordNumber = 0;
            return;
        }

        DiameterAccountingRecord? latest =
            FindLatestAccountingSequenceRecord(request);
        if (request.RecordType == DiameterAccountingRecordType.Start)
        {
            if (latest != null)
            {
                throw new InvalidOperationException(
                    "An accounting sequence already exists for the session and sub-session.");
            }

            request.RecordNumber = 0;
            return;
        }

        if (request.RecordType == DiameterAccountingRecordType.Interim &&
            latest != null &&
            latest.State == DiameterAccountingState.PendingInterim &&
            latest.Durable && latest.NextAttemptPresent)
        {
            ValidateAccountingOverwriteCorrelation(latest, request);
            request.RecordNumber = latest.Request.RecordNumber;
            if (request.InterimIntervalMilliseconds == 0 &&
                latest.Request.InterimIntervalMilliseconds > 0)
            {
                request.InterimIntervalMilliseconds =
                    latest.Request.InterimIntervalMilliseconds;
            }

            PendingInterimOverwrite = latest;
            return;
        }

        if (latest == null || latest.State != DiameterAccountingState.Open)
        {
            throw new InvalidOperationException(
                "An open accounting sequence is required.");
        }

        if (latest.Request.RecordNumber == uint.MaxValue)
        {
            throw new InvalidOperationException(
                "The accounting record number is exhausted.");
        }

        if (latest.Request.AccountingMultiSessionId !=
            request.AccountingMultiSessionId)
        {
            throw new ArgumentException(
                "Accounting multi-session correlation must remain stable.");
        }

        if (latest.Request.ApplicationId != request.ApplicationId ||
            !AreAccountingBytesEqual(
                latest.Request.AccountingSessionId,
                request.AccountingSessionId))
        {
            throw new ArgumentException(
                "Accounting application and session correlation must remain stable.");
        }

        request.RecordNumber = latest.Request.RecordNumber + 1U;
        if (request.RecordType == DiameterAccountingRecordType.Interim &&
            request.InterimIntervalMilliseconds == 0 &&
            latest.Request.InterimIntervalMilliseconds > 0)
        {
            request.InterimIntervalMilliseconds =
                latest.Request.InterimIntervalMilliseconds;
        }

    }

    internal void ValidateAccountingOverwriteCorrelation(
        DiameterAccountingRecord latest,
        DiameterAccountingRequest request)
    {
        if (latest.Request.AccountingMultiSessionId !=
                request.AccountingMultiSessionId ||
            latest.Request.ApplicationId != request.ApplicationId ||
            !AreAccountingBytesEqual(
                latest.Request.AccountingSessionId,
                request.AccountingSessionId))
        {
            throw new ArgumentException(
                "A cumulative interim overwrite must preserve accounting correlation.");
        }
    }

    internal void SupersedePendingAccountingInterim(
        DiameterAccountingRecord record)
    {
        RemoveDurableAccountingClientRecord(record);
        record.Superseded = true;
        record.State = DiameterAccountingState.Closed;
        record.NextAttemptPresent = false;
        record.NextInterimPresent = false;
        record.Code = "accounting-interim-superseded";
        record.Message =
            "A newer cumulative interim record replaced this pending record.";
    }

    internal bool AreAccountingBytesEqual(byte[] left, byte[] right)
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

    internal DiameterAccountingRecord? FindLatestAccountingSequenceRecord(
        DiameterAccountingRequest request)
    {
        int index = Records.Count - 1;
        while (index >= 0)
        {
            DiameterAccountingRecord candidate = Records[index];
            if (candidate.Request.RecordType !=
                    DiameterAccountingRecordType.Event &&
                IsSameAccountingSequence(candidate.Request, request))
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool IsSameAccountingSequence(
        DiameterAccountingRequest left,
        DiameterAccountingRequest right)
    {
        return left.SessionId == right.SessionId &&
            left.AccountingSubSessionIdPresent ==
                right.AccountingSubSessionIdPresent &&
            (!left.AccountingSubSessionIdPresent ||
            left.AccountingSubSessionId == right.AccountingSubSessionId);
    }

    internal bool RequiresDurableAccountingRecord(
        DiameterAccountingRecord record)
    {
        return record.Request.RecordType ==
                DiameterAccountingRecordType.Stop ||
            record.Request.RealtimeRequired !=
                DiameterAccountingRealtimeRequired.GrantAndLose;
    }

    internal bool PersistAccountingClientRecord(
        DiameterAccountingRecord record)
    {
        long expectedVersion = record.Version;
        record.Durable = true;
        DiameterAccountingStoreResult saved =
            Store.SaveDiameterAccountingRecord(
                AccountingRecordCloner.CloneAccountingRecord(record),
                expectedVersion);
        if (!saved.Saved || saved.Conflict || saved.Record == null)
        {
            record.Durable = false;
            return false;
        }

        record.Version = saved.Record.Version;
        return true;
    }

    internal void RemoveDurableAccountingClientRecord(
        DiameterAccountingRecord record)
    {
        if (!record.Durable)
        {
            return;
        }

        DiameterAccountingStoreResult removed =
            Store.RemoveDiameterAccountingRecord(
                record.RecordIdentifier,
                record.Version);
        if (removed.Conflict)
        {
            throw new InvalidOperationException(
                "The durable accounting record version changed.");
        }

        if (!removed.Removed && removed.Found)
        {
            throw new InvalidOperationException(
                "The durable accounting record could not be removed.");
        }

        record.Durable = false;
    }

    internal void RetainAccountingClientRecord(
        DiameterAccountingRecord record,
        DateTimeOffset now)
    {
        record.State = ReadPendingAccountingState(
            record.Request.RecordType);
        record.NextAttemptAt = now;
        record.NextAttemptPresent = true;
        record.Code = "accounting-pending-retained";
        record.Message =
            "The accounting record was retained for another delivery attempt.";
        if (!PersistAccountingClientRecord(record))
        {
            record.State = DiameterAccountingState.Failed;
            record.NextAttemptPresent = false;
            record.Code = "accounting-retention-failed";
            record.Message =
                "The pending accounting record could not be retained.";
        }
    }

    internal void RecoverAccountingClientRecords(
        IDiameterAnswerHandler handler)
    {
        List<DiameterAccountingRecord> stored =
            Store.ReadStoredDiameterAccountingRecords();
        int index = 0;
        while (index < stored.Count)
        {
            DiameterAccountingRecord recovered =
                AccountingRecordCloner.CloneAccountingRecord(
                    stored[index]);
            recovered.ValidateAccountingRecord();
            recovered.Durable = true;
            lock (AccountingLock)
            {
                if (FindAccountingRecordByIdentifier(
                    recovered.RecordIdentifier) == null)
                {
                    Records.Add(recovered);
                    if (recovered.RecordIdentifier >=
                        NextRecordIdentifier)
                    {
                        NextRecordIdentifier =
                            recovered.RecordIdentifier + 1;
                    }
                }
            }

            if (!recovered.Acknowledged && !recovered.Superseded)
            {
                ReplayRecoveredAccountingRecord(recovered, handler);
            }

            index = index + 1;
        }
    }

    internal void ReplayRecoveredAccountingRecord(
        DiameterAccountingRecord record,
        IDiameterAnswerHandler handler)
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (AccountingLock)
        {
            record.State = ReadPendingAccountingState(
                record.Request.RecordType);
            if (record.OutboundMessage == null)
            {
                record.OutboundMessage = CreateAccountingMessage(
                    record.Request);
            }

            record.OutboundMessage.Flags =
                record.OutboundMessage.Flags |
                DiameterMessageFlags.PotentiallyRetransmitted;
            record.AttemptCount = record.AttemptCount + 1;
            record.LastAttemptAt = now;
            record.UpdatedAt = now;
            record.NextAttemptPresent = false;
            record.Code = "accounting-recovery-pending";
            record.Message =
                "The retained accounting record is being replayed.";
            if (!PersistAccountingClientRecord(record))
            {
                record.State = DiameterAccountingState.Failed;
                record.NextAttemptPresent = true;
                record.Code = "accounting-recovery-conflict";
                record.Message =
                    "The recovered accounting record could not be updated safely.";
                return;
            }
        }

        DiameterSendRequest sendRequest = CreateAccountingSendRequest(
            record);
        DiameterSendResult send = RequestClient.SendDiameterRequest(
            sendRequest,
            handler);
        lock (AccountingLock)
        {
            ApplyAccountingSendResult(record, send, now);
            if (!send.Accepted && record.Durable)
            {
                RetainAccountingClientRecord(record, now);
            }
        }
    }

    internal DiameterAccountingRecord CreatePendingAccountingRecord(
        DiameterAccountingRequest request,
        DateTimeOffset now)
    {
        DiameterAccountingRecord record =
            new DiameterAccountingRecord();
        record.RecordIdentifier = NextRecordIdentifier;
        NextRecordIdentifier = NextRecordIdentifier + 1;
        record.Request = request;
        record.State = ReadPendingAccountingState(request.RecordType);
        record.OutboundMessage = CreateAccountingMessage(request);
        record.PeerIdentity =
            AccountingRecordCloner.CloneOptionalAccountingIdentity(
                request.PeerIdentity);
        record.RouteIdentifier = request.RouteIdentifier;
        record.CreatedAt = now;
        record.UpdatedAt = now;
        record.LastAttemptAt = now;
        record.AttemptCount = 1;
        record.Code = "accounting-pending";
        record.Message = "The accounting record is pending delivery.";
        ScheduleNextAccountingInterim(record, now);
        return record;
    }

    internal DiameterAccountingState ReadPendingAccountingState(
        DiameterAccountingRecordType recordType)
    {
        if (recordType == DiameterAccountingRecordType.Interim)
        {
            return DiameterAccountingState.PendingInterim;
        }

        if (recordType == DiameterAccountingRecordType.Stop)
        {
            return DiameterAccountingState.PendingStop;
        }

        return DiameterAccountingState.PendingEvent;
    }

    internal void ScheduleNextAccountingInterim(
        DiameterAccountingRecord record,
        DateTimeOffset now)
    {
        if (record.Request.RecordType != DiameterAccountingRecordType.Stop &&
            record.Request.RecordType != DiameterAccountingRecordType.Event &&
            record.Request.InterimIntervalMilliseconds > 0)
        {
            record.NextInterimAt = now.AddMilliseconds(
                record.Request.InterimIntervalMilliseconds);
            record.NextInterimPresent = true;
        }
        else
        {
            record.NextInterimPresent = false;
        }
    }

    internal DiameterMessage CreateAccountingMessage(
        DiameterAccountingRequest request)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        message.CommandCode = DiameterCommandCodes.Accounting;
        message.ApplicationId = request.ApplicationId;
        message.Avps.Add(AvpFactory.CreateSessionIdAvp(request.SessionId));
        message.Avps.Add(AvpFactory.CreateOriginHostAvp(OriginHost));
        message.Avps.Add(AvpFactory.CreateOriginRealmAvp(OriginRealm));
        message.Avps.Add(AvpFactory.CreateDestinationRealmAvp(
            request.DestinationRealm));
        if (request.DestinationHost != null)
        {
            message.Avps.Add(AvpFactory.CreateDestinationHostAvp(
                request.DestinationHost));
        }

        message.Avps.Add(AvpFactory.CreateAcctApplicationIdAvp(
            request.ApplicationId));
        message.Avps.Add(CreateAccountingUnsigned32Avp(
            DiameterAvpCodes.AccountingRecordType,
            (uint)request.RecordType));
        message.Avps.Add(CreateAccountingUnsigned32Avp(
            DiameterAvpCodes.AccountingRecordNumber,
            request.RecordNumber));
        AddAccountingOptionalAvps(message, request);
        int index = 0;
        while (index < request.ServiceAvps.Count)
        {
            message.Avps.Add(AccountingRecordCloner.CloneAccountingAvp(
                request.ServiceAvps[index]));
            index = index + 1;
        }

        return message;
    }

    internal void AddAccountingOptionalAvps(
        DiameterMessage message,
        DiameterAccountingRequest request)
    {
        if (request.UserName.Length > 0)
        {
            message.Avps.Add(CreateAccountingTextAvp(
                DiameterAvpCodes.UserName,
                request.UserName));
        }

        if (request.AccountingSubSessionIdPresent)
        {
            message.Avps.Add(CreateAccountingUnsigned64Avp(
                DiameterAvpCodes.AccountingSubSessionId,
                request.AccountingSubSessionId));
        }

        if (request.AccountingSessionId.Length > 0)
        {
            message.Avps.Add(CreateAccountingOctetsAvp(
                DiameterAvpCodes.AcctSessionId,
                request.AccountingSessionId));
        }

        if (request.AccountingMultiSessionId.Length > 0)
        {
            message.Avps.Add(CreateAccountingTextAvp(
                DiameterAvpCodes.AcctMultiSessionId,
                request.AccountingMultiSessionId));
        }

        if (request.InterimIntervalMilliseconds > 0)
        {
            long seconds =
                ((long)request.InterimIntervalMilliseconds + 999L) /
                1000L;
            message.Avps.Add(CreateAccountingUnsigned32Avp(
                DiameterAvpCodes.AcctInterimInterval,
                (uint)seconds));
        }

        message.Avps.Add(CreateAccountingUnsigned32Avp(
            DiameterAvpCodes.AccountingRealtimeRequired,
            (uint)request.RealtimeRequired));
        if (request.EventTimestampPresent)
        {
            message.Avps.Add(CreateAccountingTimeAvp(
                request.EventTimestamp));
        }
    }

    internal DiameterAvp CreateAccountingUnsigned32Avp(
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

    internal DiameterAvp CreateAccountingUnsigned64Avp(
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

    internal DiameterAvp CreateAccountingTextAvp(
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
                "An accounting text AVP is invalid.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal DiameterAvp CreateAccountingOctetsAvp(
        uint code,
        byte[] value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = AccountingRecordCloner.CloneAccountingBytes(value);
        return result;
    }

    internal DiameterAvp CreateAccountingTimeAvp(DateTimeOffset value)
    {
        byte[] data = new byte[4];
        DiameterValueResult encoded = ValueCodecs.EncodeTime(data, value);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The accounting event timestamp is invalid.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.EventTimestamp;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal DiameterSendRequest CreateAccountingSendRequest(
        DiameterAccountingRecord record)
    {
        DiameterSendRequest result = new DiameterSendRequest();
        DiameterMessage? message =
            AccountingRecordCloner.CloneOptionalAccountingMessage(
                record.OutboundMessage);
        if (message == null)
        {
            throw new InvalidOperationException(
                "An outbound accounting message is required.");
        }

        result.Request = message;
        result.PreferredPeer =
            AccountingRecordCloner.CloneOptionalAccountingIdentity(
                record.Request.PeerIdentity);
        result.TimeoutMilliseconds =
            record.Request.TimeoutMilliseconds;
        result.AllowFailover = record.Request.AllowFailover;
        result.AllowRetransmission =
            record.Request.AllowRetransmission;
        result.CorrelationText = record.Request.CorrelationText;
        return result;
    }

    internal void ApplyAccountingSendResult(
        DiameterAccountingRecord record,
        DiameterSendResult send,
        DateTimeOffset now)
    {
        record.UpdatedAt = now;
        record.Delivered = send.Delivered;
        record.PeerIdentity =
            AccountingRecordCloner.CloneOptionalAccountingIdentity(
                send.PeerIdentity);
        if (send.Accepted)
        {
            record.Code = send.Code.Length > 0 ?
                send.Code :
                "accounting-accepted";
            record.Message = send.Message;
            if (send.PendingRequest != null &&
                (record.State == DiameterAccountingState.PendingEvent ||
                record.State == DiameterAccountingState.PendingInterim ||
                record.State == DiameterAccountingState.PendingStop))
            {
                AccountingPendingLink link = new AccountingPendingLink();
                link.RecordIdentifier = record.RecordIdentifier;
                link.RequestIdentifier =
                    send.PendingRequest.RequestIdentifier;
                PendingLinks.Add(link);
            }
        }
        else
        {
            record.State = DiameterAccountingState.Failed;
            record.NextInterimPresent = false;
            record.Code = send.Code.Length > 0 ?
                send.Code :
                "accounting-rejected";
            record.Message = send.Message;
        }
    }

    internal void ApplyAccountingAttemptMetadata(
        DiameterAccountingRecord record,
        DiameterAnswerResult result)
    {
        if (result.PendingRequest == null)
        {
            return;
        }

        DiameterPendingRequestSnapshot pending = result.PendingRequest;
        if (pending.AttemptCount > record.AttemptCount)
        {
            record.AttemptCount = pending.AttemptCount;
        }

        record.OutboundMessage =
            AccountingRecordCloner.CloneOptionalAccountingMessage(
                pending.OutboundMessage);
        record.PeerIdentity =
            AccountingRecordCloner.CloneOptionalAccountingIdentity(
                pending.PeerIdentity);
    }

    internal DiameterAccountingResult CreateAccountingOperationResult(
        DiameterAccountingRecord record,
        DiameterSendResult send)
    {
        DiameterAccountingResult result =
            new DiameterAccountingResult();
        lock (AccountingLock)
        {
            result.Request =
                AccountingRecordCloner.CloneAccountingRequest(
                    record.Request);
            result.Record =
                AccountingRecordCloner.CloneAccountingRecord(record);
            result.State = record.State;
            result.Delivered = record.Delivered;
            result.ResultCode = record.ResultCode;
            result.Code = record.Code;
            result.Message = record.Message;
            result.Accepted = send.Accepted;
            result.AnswerReceived = record.LastAnswer != null;
            result.Successful = record.Acknowledged;
            result.Stored = record.Durable;
            result.Retransmission = record.OutboundMessage != null &&
                (record.OutboundMessage.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted) !=
                    DiameterMessageFlags.None;
            if (record.LastAnswer != null)
            {
                AccountingAnswerValues values =
                    ReadAccountingCorrelationValues(
                        record.LastAnswer,
                        true);
                if (values.Valid)
                {
                    result.Answer = CreateAccountingAnswerData(
                        record.LastAnswer,
                        values);
                }
            }

            result.ServiceGranted = record.Acknowledged ||
                record.Request.RealtimeRequired ==
                    DiameterAccountingRealtimeRequired.GrantAndLose ||
                (record.Request.RealtimeRequired ==
                    DiameterAccountingRealtimeRequired.GrantAndStore &&
                record.Durable);
            result.TerminateService = record.State ==
                    DiameterAccountingState.Failed &&
                !result.ServiceGranted;
        }

        return result;
    }

    internal DiameterAccountingAnswer CreateAccountingAnswerData(
        DiameterMessage message,
        AccountingAnswerValues values)
    {
        DiameterAccountingAnswer result =
            new DiameterAccountingAnswer();
        result.SessionId = values.SessionId;
        result.RecordType = values.RecordType;
        result.RecordNumber = values.RecordNumber;
        result.ResultCode = values.ResultCode;
        result.AccountingSubSessionId =
            values.AccountingSubSessionId;
        result.AccountingSubSessionIdPresent =
            values.AccountingSubSessionIdPresent;
        result.InterimIntervalMilliseconds =
            values.InterimIntervalMilliseconds;
        result.InterimIntervalPresent = values.InterimIntervalPresent;
        result.RealtimeRequired = values.RealtimeRequired;
        result.RealtimeRequiredPresent =
            values.RealtimeRequiredPresent;
        result.ErrorMessage = values.ErrorMessage;
        result.ServiceAvps = AccountingRecordCloner.CloneAccountingAvps(
            values.ServiceAvps);
        result.Message =
            AccountingRecordCloner.CloneOptionalAccountingMessage(
                message);
        return result;
    }

    internal DiameterAccountingRecord? FindAccountingRecordForAnswer(
        DiameterAnswerResult result)
    {
        int linkIndex = 0;
        while (linkIndex < PendingLinks.Count)
        {
            AccountingPendingLink link = PendingLinks[linkIndex];
            if (link.RequestIdentifier == result.RequestIdentifier)
            {
                return FindAccountingRecordByIdentifier(
                    link.RecordIdentifier);
            }

            linkIndex = linkIndex + 1;
        }

        if (result.PendingRequest == null)
        {
            return null;
        }

        AccountingAnswerValues requestValues =
            ReadAccountingCorrelationValues(
                result.PendingRequest.OutboundMessage,
                false);
        if (!requestValues.Valid)
        {
            return null;
        }

        int recordIndex = Records.Count - 1;
        while (recordIndex >= 0)
        {
            DiameterAccountingRecord record = Records[recordIndex];
            if (record.Request.SessionId == requestValues.SessionId &&
                record.Request.RecordType == requestValues.RecordType &&
                record.Request.RecordNumber == requestValues.RecordNumber)
            {
                return record;
            }

            recordIndex = recordIndex - 1;
        }

        return null;
    }

    internal DiameterAccountingRecord? FindAccountingRecordByIdentifier(
        long recordIdentifier)
    {
        int index = 0;
        while (index < Records.Count)
        {
            if (Records[index].RecordIdentifier == recordIdentifier)
            {
                return Records[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool ApplyAccountingAnswerResult(
        DiameterAccountingRecord record,
        DiameterAnswerResult result,
        DateTimeOffset now)
    {
        record.UpdatedAt = now;
        if (!result.AnswerReceived || !result.Correlated ||
            result.Answer == null)
        {
            record.State = DiameterAccountingState.Failed;
            record.NextInterimPresent = false;
            record.Code = result.Code.Length > 0 ?
                result.Code :
                "accounting-delivery-failed";
            record.Message = result.Message;
            return true;
        }

        AccountingAnswerValues values = ReadAccountingCorrelationValues(
            result.Answer,
            true);
        if (!IsCorrelatedAccountingAnswer(record, result.Answer, values))
        {
            record.State = DiameterAccountingState.Failed;
            record.NextInterimPresent = false;
            record.Code = "accounting-answer-mismatch";
            record.Message =
                "The Accounting-Answer did not match its request.";
            return true;
        }

        record.LastAnswer =
            AccountingRecordCloner.CloneOptionalAccountingMessage(
                result.Answer);
        record.ResultCode = values.ResultCode;
        record.Acknowledged = values.ResultCode >= 2000U &&
            values.ResultCode <= 2999U;
        if (!record.Acknowledged)
        {
            record.State = DiameterAccountingState.Failed;
            record.NextInterimPresent = false;
            record.Code = "accounting-answer-failure";
            record.Message = values.ErrorMessage;
            return false;
        }

        if (values.InterimIntervalPresent)
        {
            record.Request.InterimIntervalMilliseconds =
                values.InterimIntervalMilliseconds;
        }

        if (values.RealtimeRequiredPresent)
        {
            record.Request.RealtimeRequired = values.RealtimeRequired;
        }

        if (record.Request.RecordType == DiameterAccountingRecordType.Event ||
            record.Request.RecordType == DiameterAccountingRecordType.Stop)
        {
            record.State = DiameterAccountingState.Closed;
            record.NextInterimPresent = false;
        }
        else
        {
            record.State = DiameterAccountingState.Open;
            ScheduleNextAccountingInterim(record, now);
        }

        record.Code = "accounting-answer-success";
        record.Message = "The Accounting-Answer was correlated successfully.";
        return false;
    }

    internal bool IsCorrelatedAccountingAnswer(
        DiameterAccountingRecord record,
        DiameterMessage answer,
        AccountingAnswerValues values)
    {
        if (!values.Valid ||
            record.OutboundMessage == null ||
            answer.CommandCode != DiameterCommandCodes.Accounting ||
            answer.ApplicationId != record.Request.ApplicationId ||
            answer.HopByHopIdentifier !=
                record.OutboundMessage.HopByHopIdentifier ||
            answer.EndToEndIdentifier !=
                record.OutboundMessage.EndToEndIdentifier ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            values.SessionId != record.Request.SessionId ||
            values.RecordType != record.Request.RecordType ||
            values.RecordNumber != record.Request.RecordNumber)
        {
            return false;
        }

        if (values.AccountingSubSessionIdPresent !=
                record.Request.AccountingSubSessionIdPresent ||
            values.AccountingSubSessionIdPresent &&
            values.AccountingSubSessionId !=
                record.Request.AccountingSubSessionId)
        {
            return false;
        }

        return true;
    }

    internal AccountingAnswerValues ReadAccountingCorrelationValues(
        DiameterMessage message,
        bool requireResult)
    {
        AccountingAnswerValues result = new AccountingAnswerValues();
        int sessionCount = 0;
        int typeCount = 0;
        int numberCount = 0;
        int resultCount = 0;
        int subSessionCount = 0;
        int intervalCount = 0;
        int realtimeCount = 0;
        int errorCount = 0;
        bool malformed = false;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp != null && avp.VendorId == DiameterVendorIds.Ietf)
            {
                if (avp.Code == DiameterAvpCodes.SessionId)
                {
                    sessionCount = sessionCount + 1;
                    if (avp.Data == null)
                    {
                        malformed = true;
                    }
                    else
                    {
                        DiameterValueResult decoded =
                            ValueCodecs.DecodeUtf8String(avp.Data);
                        if (decoded.Status == DiameterCodecStatus.Success)
                        {
                            result.SessionId = decoded.Text;
                        }
                        else
                        {
                            malformed = true;
                        }
                    }
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingRecordType)
                {
                    typeCount = typeCount + 1;
                    if (avp.Data != null && avp.Data.Length == 4)
                    {
                        result.RecordType =
                            (DiameterAccountingRecordType)
                            NetworkByteOrder.ReadUInt32(avp.Data);
                    }
                    else
                    {
                        malformed = true;
                    }
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingRecordNumber)
                {
                    numberCount = numberCount + 1;
                    if (avp.Data != null && avp.Data.Length == 4)
                    {
                        result.RecordNumber =
                            NetworkByteOrder.ReadUInt32(avp.Data);
                    }
                    else
                    {
                        malformed = true;
                    }
                }
                else if (avp.Code == DiameterAvpCodes.ResultCode)
                {
                    resultCount = resultCount + 1;
                    if (avp.Data != null && avp.Data.Length == 4)
                    {
                        result.ResultCode =
                            NetworkByteOrder.ReadUInt32(avp.Data);
                    }
                    else
                    {
                        malformed = true;
                    }
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingSubSessionId)
                {
                    subSessionCount = subSessionCount + 1;
                    if (avp.Data != null && avp.Data.Length == 8)
                    {
                        result.AccountingSubSessionId =
                            NetworkByteOrder.ReadUInt64(avp.Data);
                        result.AccountingSubSessionIdPresent = true;
                    }
                    else
                    {
                        malformed = true;
                    }
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AcctInterimInterval)
                {
                    intervalCount = intervalCount + 1;
                    if (avp.Data == null || avp.Data.Length != 4 ||
                        NetworkByteOrder.ReadUInt32(avp.Data) >
                            (uint)(int.MaxValue / 1000))
                    {
                        malformed = true;
                    }
                    else
                    {
                        ReadAccountingAnswerInterval(result, avp);
                    }
                }
                else if (avp.Code ==
                    DiameterAvpCodes.AccountingRealtimeRequired)
                {
                    realtimeCount = realtimeCount + 1;
                    if (avp.Data == null || avp.Data.Length != 4)
                    {
                        malformed = true;
                    }
                    else
                    {
                        uint realtimeValue =
                            NetworkByteOrder.ReadUInt32(avp.Data);
                        if (realtimeValue <
                                (uint)DiameterAccountingRealtimeRequired.
                                    DeliverAndGrant ||
                            realtimeValue >
                                (uint)DiameterAccountingRealtimeRequired.
                                    GrantAndLose)
                        {
                            malformed = true;
                        }
                        else
                        {
                            ReadAccountingAnswerRealtime(result, avp);
                        }
                    }
                }
                else if (avp.Code == DiameterAvpCodes.ErrorMessage)
                {
                    errorCount = errorCount + 1;
                    if (avp.Data == null)
                    {
                        malformed = true;
                    }
                    else
                    {
                        DiameterValueResult error =
                            ValueCodecs.DecodeUtf8String(avp.Data);
                        if (error.Status == DiameterCodecStatus.Success)
                        {
                            result.ErrorMessage = error.Text;
                        }
                        else
                        {
                            malformed = true;
                        }
                    }
                }
                else
                {
                    result.ServiceAvps.Add(
                        AccountingRecordCloner.CloneAccountingAvp(avp));
                }
            }

            index = index + 1;
        }

        result.Valid = !malformed &&
            sessionCount == 1 && typeCount == 1 &&
            numberCount == 1 && subSessionCount <= 1 &&
            intervalCount <= 1 && realtimeCount <= 1 &&
            errorCount <= 1 &&
            (!requireResult || resultCount == 1) &&
            (requireResult || resultCount == 0) &&
            result.SessionId.Length > 0 &&
            (int)result.RecordType >=
                (int)DiameterAccountingRecordType.Event &&
            (int)result.RecordType <=
                (int)DiameterAccountingRecordType.Stop &&
            (!requireResult ||
            (result.ResultCode >= 1000U && result.ResultCode <= 5999U));
        return result;
    }

    internal void ReadAccountingAnswerInterval(
        AccountingAnswerValues result,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            return;
        }

        uint seconds = NetworkByteOrder.ReadUInt32(avp.Data);
        if (seconds <= (uint)(int.MaxValue / 1000))
        {
            result.InterimIntervalMilliseconds = (int)seconds * 1000;
            result.InterimIntervalPresent = true;
        }
    }

    internal void ReadAccountingAnswerRealtime(
        AccountingAnswerValues result,
        DiameterAvp avp)
    {
        if (avp.Data.Length != 4)
        {
            return;
        }

        uint value = NetworkByteOrder.ReadUInt32(avp.Data);
        if (value >=
                (uint)DiameterAccountingRealtimeRequired.DeliverAndGrant &&
            value <=
                (uint)DiameterAccountingRealtimeRequired.GrantAndLose)
        {
            result.RealtimeRequired =
                (DiameterAccountingRealtimeRequired)value;
            result.RealtimeRequiredPresent = true;
        }
    }

    internal void RemoveAccountingPendingLink(long recordIdentifier)
    {
        int index = PendingLinks.Count - 1;
        while (index >= 0)
        {
            if (PendingLinks[index].RecordIdentifier == recordIdentifier)
            {
                PendingLinks.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal void ValidateAccountingExtensionAvps(
        DiameterAccountingRequest request)
    {
        int index = 0;
        while (index < request.ServiceAvps.Count)
        {
            DiameterAvp avp = request.ServiceAvps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                IsManagedAccountingAvpCode(avp.Code))
            {
                throw new ArgumentException(
                    "Service AVPs cannot replace accounting state AVPs.");
            }

            index = index + 1;
        }
    }

    internal bool IsManagedAccountingAvpCode(uint code)
    {
        return code == DiameterAvpCodes.SessionId ||
            code == DiameterAvpCodes.OriginHost ||
            code == DiameterAvpCodes.OriginRealm ||
            code == DiameterAvpCodes.DestinationRealm ||
            code == DiameterAvpCodes.DestinationHost ||
            code == DiameterAvpCodes.AcctApplicationId ||
            code == DiameterAvpCodes.AccountingRecordType ||
            code == DiameterAvpCodes.AccountingRecordNumber ||
            code == DiameterAvpCodes.AccountingSubSessionId ||
            code == DiameterAvpCodes.AcctSessionId ||
            code == DiameterAvpCodes.AcctMultiSessionId ||
            code == DiameterAvpCodes.AcctInterimInterval ||
            code == DiameterAvpCodes.AccountingRealtimeRequired ||
            code == DiameterAvpCodes.EventTimestamp ||
            code == DiameterAvpCodes.UserName;
    }
}
