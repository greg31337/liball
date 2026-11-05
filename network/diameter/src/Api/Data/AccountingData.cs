using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Contains one accounting record request and its delivery policy.</summary>
public class DiameterAccountingRequest
{
    /// <summary>The globally unique Session-Id.</summary>
    public string SessionId = string.Empty;

    /// <summary>The accounting application identifier carried by the command.</summary>
    public uint ApplicationId;

    /// <summary>The destination realm for the accounting request.</summary>
    public DiameterIdentity DestinationRealm = new DiameterIdentity();

    /// <summary>The destination host, or <c>null</c> when selected by routing.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The preferred delivery peer, or <c>null</c> when selected by routing.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The User-Name associated with the accounted service.</summary>
    public string UserName = string.Empty;

    /// <summary>The EVENT, START, INTERIM, or STOP record type.</summary>
    public DiameterAccountingRecordType RecordType =
        DiameterAccountingRecordType.Event;

    /// <summary>The monotonically assigned record number within the session.</summary>
    public uint RecordNumber;

    /// <summary>The accounting sub-session identifier.</summary>
    public ulong AccountingSubSessionId;

    /// <summary>Indicates that an accounting sub-session identifier is present.</summary>
    public bool AccountingSubSessionIdPresent;

    /// <summary>The owned RADIUS-compatible Acct-Session-Id octets.</summary>
    public byte[] AccountingSessionId = new byte[0];

    /// <summary>The globally unique multi-session correlation text.</summary>
    public string AccountingMultiSessionId = string.Empty;

    /// <summary>The nonnegative interim-record interval in milliseconds.</summary>
    public int InterimIntervalMilliseconds;

    /// <summary>The accounting delivery failure policy.</summary>
    public DiameterAccountingRealtimeRequired RealtimeRequired =
        DiameterAccountingRealtimeRequired.GrantAndStore;

    /// <summary>The timestamp associated with the accounted event.</summary>
    public DateTimeOffset EventTimestamp;

    /// <summary>Indicates that an event timestamp is present.</summary>
    public bool EventTimestampPresent;

    /// <summary>The positive time allowed for a terminal delivery outcome.</summary>
    public int TimeoutMilliseconds = 30000;

    /// <summary>Allows delivery through an alternate accounting server.</summary>
    public bool AllowFailover = true;

    /// <summary>Allows uncertain delivery to be retransmitted.</summary>
    public bool AllowRetransmission = true;

    /// <summary>The retained route identifier, or an empty string.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>Caller-defined correlation text returned with outcomes.</summary>
    public string CorrelationText = string.Empty;

    /// <summary>Owned service-specific AVPs included in the accounting command.</summary>
    public List<DiameterAvp> ServiceAvps = new List<DiameterAvp>();

    /// <summary>Owned application state retained with the accounting operation.</summary>
    public byte[] ApplicationState = new byte[0];

    internal void ValidateAccountingRequest()
    {
        int recordType = (int)RecordType;
        int realtime = (int)RealtimeRequired;
        if (recordType < (int)DiameterAccountingRecordType.Event ||
            recordType > (int)DiameterAccountingRecordType.Stop ||
            realtime <
                (int)DiameterAccountingRealtimeRequired.DeliverAndGrant ||
            realtime >
                (int)DiameterAccountingRealtimeRequired.GrantAndLose)
        {
            throw new ArgumentOutOfRangeException(
                "Accounting record type or real-time policy");
        }

        if (SessionId == null || SessionId.Length == 0 ||
            UserName == null || AccountingSessionId == null ||
            AccountingMultiSessionId == null || RouteIdentifier == null ||
            CorrelationText == null || ServiceAvps == null ||
            ApplicationState == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(
                AccountingMultiSessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(CorrelationText) >= 0)
        {
            throw new ArgumentException(
                "Accounting request text, collections, and owned data must be valid.");
        }

        if (DestinationRealm == null ||
            ValueCodecs.NormalizeDiameterRealm(DestinationRealm).Status !=
                DiameterCodecStatus.Success ||
            (DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success) ||
            (PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success))
        {
            throw new ArgumentException(
                "Accounting routing identities must be valid.");
        }

        if (InterimIntervalMilliseconds < 0 ||
            TimeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "Accounting interval or timeout");
        }

        ValidateAccountingServiceAvps();
    }

    internal void ValidateAccountingServiceAvps()
    {
        int index = 0;
        while (index < ServiceAvps.Count)
        {
            DiameterAvp avp = ServiceAvps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Accounting service AVPs must own their payload data.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains the application decision for one Accounting-Request.</summary>
public class DiameterAccountingAnswer
{
    /// <summary>The Session-Id copied from the request.</summary>
    public string SessionId = string.Empty;

    /// <summary>The record type copied from the request.</summary>
    public DiameterAccountingRecordType RecordType =
        DiameterAccountingRecordType.Event;

    /// <summary>The record number copied from the request.</summary>
    public uint RecordNumber;

    /// <summary>The standard Diameter Result-Code.</summary>
    public uint ResultCode = DiameterResultCodes.Success;

    /// <summary>The accounting sub-session identifier.</summary>
    public ulong AccountingSubSessionId;

    /// <summary>Indicates that an accounting sub-session identifier is present.</summary>
    public bool AccountingSubSessionIdPresent;

    /// <summary>The server-selected interim interval in milliseconds.</summary>
    public int InterimIntervalMilliseconds;

    /// <summary>Indicates that the answer overrides the interim interval.</summary>
    public bool InterimIntervalPresent;

    /// <summary>The server-selected real-time delivery policy.</summary>
    public DiameterAccountingRealtimeRequired RealtimeRequired =
        DiameterAccountingRealtimeRequired.GrantAndStore;

    /// <summary>Indicates that the answer overrides the delivery policy.</summary>
    public bool RealtimeRequiredPresent;

    /// <summary>The optional human-readable failure description.</summary>
    public string ErrorMessage = string.Empty;

    /// <summary>Owned application-specific AVPs included in the answer.</summary>
    public List<DiameterAvp> ServiceAvps = new List<DiameterAvp>();

    /// <summary>The owned Accounting-Answer message after encoding, or <c>null</c>.</summary>
    public DiameterMessage? Message;

    internal void ValidateAccountingAnswer()
    {
        int recordType = (int)RecordType;
        int realtime = (int)RealtimeRequired;
        if (recordType < (int)DiameterAccountingRecordType.Event ||
            recordType > (int)DiameterAccountingRecordType.Stop ||
            ResultCode < 1000U || ResultCode > 5999U ||
            InterimIntervalMilliseconds < 0 ||
            (RealtimeRequiredPresent &&
            (realtime <
                (int)DiameterAccountingRealtimeRequired.DeliverAndGrant ||
            realtime >
                (int)DiameterAccountingRealtimeRequired.GrantAndLose)))
        {
            throw new ArgumentOutOfRangeException(
                "Accounting answer value");
        }

        if (SessionId == null || SessionId.Length == 0 ||
            ErrorMessage == null || ServiceAvps == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(ErrorMessage) >= 0 ||
            (Message != null && Message.Avps == null))
        {
            throw new ArgumentException(
                "Accounting answer data must be valid.");
        }

        int index = 0;
        while (index < ServiceAvps.Count)
        {
            DiameterAvp avp = ServiceAvps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Accounting answer AVPs must own their payload data.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains the complete persistable state of one accounting record.</summary>
public class DiameterAccountingRecord
{
    /// <summary>The positive local accounting record identifier.</summary>
    public long RecordIdentifier;

    /// <summary>The optimistic persistence version.</summary>
    public long Version;

    /// <summary>The original owned accounting request.</summary>
    public DiameterAccountingRequest Request =
        new DiameterAccountingRequest();

    /// <summary>The current accounting operation state.</summary>
    public DiameterAccountingState State = DiameterAccountingState.Idle;

    /// <summary>The owned outbound Accounting-Request message, or <c>null</c>.</summary>
    public DiameterMessage? OutboundMessage;

    /// <summary>The owned terminal Accounting-Answer message, or <c>null</c>.</summary>
    public DiameterMessage? LastAnswer;

    /// <summary>The peer used by the current delivery attempt, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The route used by the current delivery attempt.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The time at which the record was created.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The time at which the record was most recently updated.</summary>
    public DateTimeOffset UpdatedAt;

    /// <summary>The time of the most recent delivery attempt.</summary>
    public DateTimeOffset LastAttemptAt;

    /// <summary>The next scheduled delivery attempt.</summary>
    public DateTimeOffset NextAttemptAt;

    /// <summary>Indicates that another delivery attempt is scheduled.</summary>
    public bool NextAttemptPresent;

    /// <summary>The next scheduled interim-record production time.</summary>
    public DateTimeOffset NextInterimAt;

    /// <summary>Indicates that interim-record production is scheduled.</summary>
    public bool NextInterimPresent;

    /// <summary>The number of delivery attempts made.</summary>
    public int AttemptCount;

    /// <summary>Indicates that a delivery reached a peer transport.</summary>
    public bool Delivered;

    /// <summary>Indicates that a successful correlated answer was received.</summary>
    public bool Acknowledged;

    /// <summary>Indicates that this pending interim was replaced by newer cumulative data.</summary>
    public bool Superseded;

    /// <summary>Indicates that the record is retained in durable storage.</summary>
    public bool Durable;

    /// <summary>The most recent standard Result-Code, or zero when absent.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable record status code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable record status description.</summary>
    public string Message = string.Empty;

    internal void ValidateAccountingRecord()
    {
        if (RecordIdentifier <= 0 || Version < 0 || AttemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                "Accounting identifier, version, or attempt count");
        }

        if (Request == null)
        {
            throw new ArgumentException(
                "An accounting record requires its original request.");
        }

        Request.ValidateAccountingRequest();
        int state = (int)State;
        if (state < (int)DiameterAccountingState.Idle ||
            state > (int)DiameterAccountingState.Failed ||
            ResultCode > 0U &&
            (ResultCode < 1000U || ResultCode > 5999U))
        {
            throw new ArgumentOutOfRangeException(
                "Accounting state or result code");
        }

        if (RouteIdentifier == null || Code == null || Message == null ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Message) >= 0 ||
            (OutboundMessage != null && OutboundMessage.Avps == null) ||
            (LastAnswer != null && LastAnswer.Avps == null) ||
            (PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success))
        {
            throw new ArgumentException(
                "Persisted accounting record data must be valid.");
        }

        if (UpdatedAt < CreatedAt ||
            LastAttemptAt != default(DateTimeOffset) &&
            (LastAttemptAt < CreatedAt || LastAttemptAt > UpdatedAt) ||
            NextAttemptPresent && NextAttemptAt < UpdatedAt ||
            NextInterimPresent && NextInterimAt < UpdatedAt)
        {
            throw new ArgumentException(
                "Accounting record timestamps are inconsistent.");
        }

        if (Acknowledged && LastAnswer == null)
        {
            throw new ArgumentException(
                "An acknowledged accounting record requires its answer.");
        }
    }
}

/// <summary>Contains one accounting client operation outcome.</summary>
public class DiameterAccountingResult
{
    /// <summary>The accounting request represented by the outcome.</summary>
    public DiameterAccountingRequest Request =
        new DiameterAccountingRequest();

    /// <summary>The retained record, or <c>null</c> when none was created.</summary>
    public DiameterAccountingRecord? Record;

    /// <summary>The application answer, or <c>null</c> when unavailable.</summary>
    public DiameterAccountingAnswer? Answer;

    /// <summary>The current accounting operation state.</summary>
    public DiameterAccountingState State = DiameterAccountingState.Idle;

    /// <summary>Indicates that the operation was admitted.</summary>
    public bool Accepted;

    /// <summary>Indicates that the Accounting-Request reached a peer transport.</summary>
    public bool Delivered;

    /// <summary>Indicates that the record was retained for later delivery.</summary>
    public bool Stored;

    /// <summary>Indicates that a correlated Accounting-Answer was received.</summary>
    public bool AnswerReceived;

    /// <summary>Indicates that accounting completed successfully.</summary>
    public bool Successful;

    /// <summary>Indicates that the request was a duplicate.</summary>
    public bool Duplicate;

    /// <summary>Indicates that the request was retransmitted.</summary>
    public bool Retransmission;

    /// <summary>Indicates that service may be granted or continued.</summary>
    public bool ServiceGranted;

    /// <summary>Indicates that the accounted service must terminate.</summary>
    public bool TerminateService;

    /// <summary>The standard Result-Code, or zero when unavailable.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains the outcome of processing one inbound Accounting-Request.</summary>
public class DiameterAccountingProcessingResult
{
    /// <summary>The parsed owned accounting request.</summary>
    public DiameterAccountingRequest Request =
        new DiameterAccountingRequest();

    /// <summary>The owned application answer data, or <c>null</c> when no answer can be built.</summary>
    public DiameterAccountingAnswer? Answer;

    /// <summary>Indicates that the application accepted the accounting record.</summary>
    public bool Accepted;

    /// <summary>Indicates that the request duplicated an observed accounting record.</summary>
    public bool Duplicate;

    /// <summary>Indicates that a cached answer was replayed.</summary>
    public bool Replayed;

    /// <summary>Indicates that the accounting handler was invoked.</summary>
    public bool ApplicationInvoked;

    /// <summary>The standard Diameter Result-Code.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable processing code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable processing description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains one atomic accounting-store operation outcome.</summary>
public class DiameterAccountingStoreResult
{
    /// <summary>The affected local accounting record identifier.</summary>
    public long RecordIdentifier;

    /// <summary>The owned stored record, or <c>null</c> when unavailable.</summary>
    public DiameterAccountingRecord? Record;

    /// <summary>Indicates that the requested record was found.</summary>
    public bool Found;

    /// <summary>Indicates that the record was saved.</summary>
    public bool Saved;

    /// <summary>Indicates that the record was removed.</summary>
    public bool Removed;

    /// <summary>Indicates that an optimistic version conflict occurred.</summary>
    public bool Conflict;

    /// <summary>The stable machine-readable store outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable store outcome description.</summary>
    public string Message = string.Empty;
}
