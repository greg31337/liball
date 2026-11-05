using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class AccountingDataTests
{
    [Test]
    public void AccountingDataDefaultsAreSafeAndIndependentlyOwned()
    {
        DiameterAccountingRequest firstRequest =
            new DiameterAccountingRequest();
        DiameterAccountingRequest secondRequest =
            new DiameterAccountingRequest();
        DiameterAccountingAnswer firstAnswer =
            new DiameterAccountingAnswer();
        DiameterAccountingAnswer secondAnswer =
            new DiameterAccountingAnswer();
        DiameterAccountingRecord firstRecord =
            new DiameterAccountingRecord();
        DiameterAccountingRecord secondRecord =
            new DiameterAccountingRecord();
        DiameterAccountingResult result =
            new DiameterAccountingResult();
        DiameterAccountingStoreResult store =
            new DiameterAccountingStoreResult();

        firstRequest.ServiceAvps.Add(new DiameterAvp());
        firstRequest.ApplicationState = new byte[] { 1 };
        firstAnswer.ServiceAvps.Add(new DiameterAvp());

        Assert.Multiple(() =>
        {
            Assert.That(firstRequest.RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Event));
            Assert.That(firstRequest.RealtimeRequired,
                Is.EqualTo(
                    DiameterAccountingRealtimeRequired.GrantAndStore));
            Assert.That(firstRequest.TimeoutMilliseconds,
                Is.EqualTo(30000));
            Assert.That(firstRequest.AllowFailover, Is.True);
            Assert.That(firstRequest.AllowRetransmission, Is.True);
            Assert.That(secondRequest.ServiceAvps, Is.Empty);
            Assert.That(secondRequest.ApplicationState, Is.Empty);
            Assert.That(firstAnswer.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(secondAnswer.ServiceAvps, Is.Empty);
            Assert.That(firstRecord.State,
                Is.EqualTo(DiameterAccountingState.Idle));
            Assert.That(firstRecord.Request,
                Is.Not.SameAs(secondRecord.Request));
            Assert.That(result.Request,
                Is.Not.SameAs(firstRecord.Request));
            Assert.That(result.Record, Is.Null);
            Assert.That(result.Answer, Is.Null);
            Assert.That(store.Record, Is.Null);
            Assert.That(store.Found, Is.False);
            Assert.That(store.Saved, Is.False);
            Assert.That(store.Removed, Is.False);
            Assert.That(store.Conflict, Is.False);
        });
    }

    [Test]
    public void AccountingRequestValidatesRoutingPolicyAndOwnedData()
    {
        DiameterAccountingRequest valid = CreateValidRequest();
        valid.RecordType = DiameterAccountingRecordType.Interim;
        valid.RecordNumber = 3;
        valid.AccountingSubSessionId = 7;
        valid.AccountingSubSessionIdPresent = true;
        valid.AccountingSessionId = new byte[] { 4, 5 };
        valid.AccountingMultiSessionId = "multi-session-1";
        valid.InterimIntervalMilliseconds = 60000;
        valid.EventTimestampPresent = true;
        valid.EventTimestamp = DateTimeOffset.FromUnixTimeSeconds(1000);
        valid.RouteIdentifier = "accounting-route";
        valid.CorrelationText = "operation-1";
        valid.ServiceAvps.Add(new DiameterAvp());
        valid.ApplicationState = new byte[] { 9 };

        Assert.DoesNotThrow(valid.ValidateAccountingRequest);

        DiameterAccountingRequest missingSession = CreateValidRequest();
        missingSession.SessionId = string.Empty;
        DiameterAccountingRequest invalidRealm = CreateValidRequest();
        invalidRealm.DestinationRealm.Value = "bad realm";
        DiameterAccountingRequest invalidPeer = CreateValidRequest();
        invalidPeer.PeerIdentity = CreateIdentity("bad peer");
        DiameterAccountingRequest invalidType = CreateValidRequest();
        invalidType.RecordType = (DiameterAccountingRecordType)99;
        DiameterAccountingRequest invalidRealtime = CreateValidRequest();
        invalidRealtime.RealtimeRequired =
            (DiameterAccountingRealtimeRequired)99;
        DiameterAccountingRequest invalidInterval = CreateValidRequest();
        invalidInterval.InterimIntervalMilliseconds = -1;
        DiameterAccountingRequest invalidTimeout = CreateValidRequest();
        invalidTimeout.TimeoutMilliseconds = 0;
        DiameterAccountingRequest missingPayload = CreateValidRequest();
        DiameterAvp invalidAvp = new DiameterAvp();
        invalidAvp.Data = null!;
        missingPayload.ServiceAvps.Add(invalidAvp);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingSession.ValidateAccountingRequest);
            Assert.Throws<ArgumentException>(
                invalidRealm.ValidateAccountingRequest);
            Assert.Throws<ArgumentException>(
                invalidPeer.ValidateAccountingRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidType.ValidateAccountingRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidRealtime.ValidateAccountingRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidInterval.ValidateAccountingRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidTimeout.ValidateAccountingRequest);
            Assert.Throws<ArgumentException>(
                missingPayload.ValidateAccountingRequest);
        });
    }

    [Test]
    public void AccountingAnswerValidatesCorrelationAndOverrides()
    {
        DiameterAccountingAnswer valid = CreateValidAnswer();
        valid.AccountingSubSessionId = 7;
        valid.AccountingSubSessionIdPresent = true;
        valid.InterimIntervalMilliseconds = 45000;
        valid.InterimIntervalPresent = true;
        valid.RealtimeRequired =
            DiameterAccountingRealtimeRequired.DeliverAndGrant;
        valid.RealtimeRequiredPresent = true;
        valid.ServiceAvps.Add(new DiameterAvp());
        valid.Message = new DiameterMessage();

        Assert.DoesNotThrow(valid.ValidateAccountingAnswer);

        DiameterAccountingAnswer missingSession = CreateValidAnswer();
        missingSession.SessionId = string.Empty;
        DiameterAccountingAnswer invalidResult = CreateValidAnswer();
        invalidResult.ResultCode = 999;
        DiameterAccountingAnswer invalidType = CreateValidAnswer();
        invalidType.RecordType = (DiameterAccountingRecordType)99;
        DiameterAccountingAnswer invalidInterval = CreateValidAnswer();
        invalidInterval.InterimIntervalMilliseconds = -1;
        DiameterAccountingAnswer invalidOverride = CreateValidAnswer();
        invalidOverride.RealtimeRequiredPresent = true;
        invalidOverride.RealtimeRequired =
            (DiameterAccountingRealtimeRequired)99;
        DiameterAccountingAnswer invalidMessage = CreateValidAnswer();
        invalidMessage.Message = new DiameterMessage();
        invalidMessage.Message.Avps = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingSession.ValidateAccountingAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidResult.ValidateAccountingAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidType.ValidateAccountingAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidInterval.ValidateAccountingAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidOverride.ValidateAccountingAnswer);
            Assert.Throws<ArgumentException>(
                invalidMessage.ValidateAccountingAnswer);
        });
    }

    [Test]
    public void AccountingRecordValidatesPersistenceStateAndTimes()
    {
        DateTimeOffset created = DateTimeOffset.FromUnixTimeSeconds(1000);
        DiameterAccountingRecord valid = CreateValidRecord(created);
        valid.LastAnswer = new DiameterMessage();
        valid.Acknowledged = true;
        valid.NextAttemptPresent = true;
        valid.NextAttemptAt = created.AddSeconds(3);
        valid.NextInterimPresent = true;
        valid.NextInterimAt = created.AddMinutes(1);

        Assert.DoesNotThrow(valid.ValidateAccountingRecord);

        DiameterAccountingRecord invalidIdentifier =
            CreateValidRecord(created);
        invalidIdentifier.RecordIdentifier = 0;
        DiameterAccountingRecord invalidVersion = CreateValidRecord(created);
        invalidVersion.Version = -1;
        DiameterAccountingRecord invalidState = CreateValidRecord(created);
        invalidState.State = (DiameterAccountingState)99;
        DiameterAccountingRecord invalidResult = CreateValidRecord(created);
        invalidResult.ResultCode = 6000;
        DiameterAccountingRecord invalidTime = CreateValidRecord(created);
        invalidTime.UpdatedAt = created.AddSeconds(-1);
        DiameterAccountingRecord invalidAcknowledgement =
            CreateValidRecord(created);
        invalidAcknowledgement.Acknowledged = true;

        Assert.Multiple(() =>
        {
            Assert.That(valid.RecordIdentifier, Is.EqualTo(41));
            Assert.That(valid.Version, Is.EqualTo(2));
            Assert.That(valid.AttemptCount, Is.EqualTo(1));
            Assert.That(valid.PeerIdentity!.Value,
                Is.EqualTo("accounting.example"));
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidIdentifier.ValidateAccountingRecord);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidVersion.ValidateAccountingRecord);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidState.ValidateAccountingRecord);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidResult.ValidateAccountingRecord);
            Assert.Throws<ArgumentException>(
                invalidTime.ValidateAccountingRecord);
            Assert.Throws<ArgumentException>(
                invalidAcknowledgement.ValidateAccountingRecord);
        });
    }

    [Test]
    public void AccountingValidatorsRejectEveryRemainingOwnedDataBranch()
    {
        DiameterAccountingRequest nullServiceAvp = CreateValidRequest();
        nullServiceAvp.ServiceAvps.Add(null!);

        DiameterAccountingAnswer lowRecordType = CreateValidAnswer();
        lowRecordType.RecordType = (DiameterAccountingRecordType)(-1);
        DiameterAccountingAnswer lowRealtime = CreateValidAnswer();
        lowRealtime.RealtimeRequiredPresent = true;
        lowRealtime.RealtimeRequired =
            (DiameterAccountingRealtimeRequired)(-1);
        DiameterAccountingAnswer nullAnswerAvp = CreateValidAnswer();
        nullAnswerAvp.ServiceAvps.Add(null!);
        DiameterAccountingAnswer missingAnswerPayload = CreateValidAnswer();
        DiameterAvp answerAvp = new DiameterAvp();
        answerAvp.Data = null!;
        missingAnswerPayload.ServiceAvps.Add(answerAvp);

        DateTimeOffset created = DateTimeOffset.FromUnixTimeSeconds(1000);
        List<DiameterAccountingRecord> invalidRecords =
            new List<DiameterAccountingRecord>();
        AddInvalidRecord(invalidRecords, created, value => value.Request = null!);
        AddInvalidRecord(invalidRecords, created,
            value => value.State = (DiameterAccountingState)(-1));
        AddInvalidRecord(invalidRecords, created, value => value.ResultCode = 999U);
        AddInvalidRecord(invalidRecords, created, value => value.RouteIdentifier = null!);
        AddInvalidRecord(invalidRecords, created, value => value.Code = null!);
        AddInvalidRecord(invalidRecords, created, value => value.Message = null!);
        AddInvalidRecord(invalidRecords, created,
            value => value.RouteIdentifier = "bad\ud800route");
        AddInvalidRecord(invalidRecords, created,
            value => value.Code = "bad\ud800code");
        AddInvalidRecord(invalidRecords, created,
            value => value.Message = "bad\ud800message");
        AddInvalidRecord(invalidRecords, created, value =>
        {
            value.OutboundMessage = new DiameterMessage();
            value.OutboundMessage.Avps = null!;
        });
        AddInvalidRecord(invalidRecords, created, value =>
        {
            value.LastAnswer = new DiameterMessage();
            value.LastAnswer.Avps = null!;
        });
        AddInvalidRecord(invalidRecords, created,
            value => value.PeerIdentity = CreateIdentity("bad peer"));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                nullServiceAvp.ValidateAccountingRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                lowRecordType.ValidateAccountingAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                lowRealtime.ValidateAccountingAnswer);
            Assert.Throws<ArgumentException>(
                nullAnswerAvp.ValidateAccountingAnswer);
            Assert.Throws<ArgumentException>(
                missingAnswerPayload.ValidateAccountingAnswer);
            int index = 0;
            while (index < invalidRecords.Count)
            {
                Assert.That(
                    new TestDelegate(
                        invalidRecords[index].ValidateAccountingRecord),
                    Throws.InstanceOf<ArgumentException>());
                index = index + 1;
            }
        });
    }

    [Test]
    public void AccountingContractsCarryClientHandlerAndStoreOutcomes()
    {
        AccountingContractProbe probe = new AccountingContractProbe();
        DiameterAccountingRequest request = CreateValidRequest();
        DiameterAccountingRecord record = CreateValidRecord(
            DateTimeOffset.FromUnixTimeSeconds(1000));
        IDiameterAccountingClient client = probe;
        IDiameterAccountingHandler handler = probe;
        IAccountingRecordStore store = probe;

        DiameterAccountingResult eventResult =
            client.SendDiameterAccountingEvent(request);
        DiameterAccountingResult startResult =
            client.SendDiameterAccountingStart(request);
        DiameterAccountingResult interimResult =
            client.SendDiameterAccountingInterim(request);
        DiameterAccountingResult stopResult =
            client.SendDiameterAccountingStop(request);
        List<DiameterAccountingRecord> clientRecords =
            client.ReadDiameterAccountingRecords();
        DiameterAccountingAnswer answer =
            handler.HandleDiameterAccountingRequest(request);
        DiameterAccountingStoreResult loaded =
            store.LoadDiameterAccountingRecord(41);
        DiameterAccountingStoreResult saved =
            store.SaveDiameterAccountingRecord(record, 2);
        DiameterAccountingStoreResult removed =
            store.RemoveDiameterAccountingRecord(41, 3);
        List<DiameterAccountingRecord> storedRecords =
            store.ReadStoredDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(eventResult, Is.SameAs(probe.Result));
            Assert.That(startResult, Is.SameAs(probe.Result));
            Assert.That(interimResult, Is.SameAs(probe.Result));
            Assert.That(stopResult, Is.SameAs(probe.Result));
            Assert.That(clientRecords, Is.SameAs(probe.Records));
            Assert.That(answer, Is.SameAs(probe.Answer));
            Assert.That(loaded, Is.SameAs(probe.StoreResult));
            Assert.That(saved, Is.SameAs(probe.StoreResult));
            Assert.That(removed, Is.SameAs(probe.StoreResult));
            Assert.That(storedRecords, Is.SameAs(probe.Records));
            Assert.That(probe.Request, Is.SameAs(request));
            Assert.That(probe.Record, Is.SameAs(record));
            Assert.That(probe.RecordIdentifier, Is.EqualTo(41));
            Assert.That(probe.ExpectedVersion, Is.EqualTo(3));
            Assert.That(probe.SendCount, Is.EqualTo(4));
        });
    }

    private static DiameterAccountingRequest CreateValidRequest()
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = "client.example;accounting;1";
        result.ApplicationId = 3;
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost =
            CreateIdentity("accounting.remote.example");
        result.PeerIdentity = CreateIdentity("accounting.example");
        result.UserName = "alice@remote.example";
        return result;
    }

    private static DiameterAccountingAnswer CreateValidAnswer()
    {
        DiameterAccountingAnswer result =
            new DiameterAccountingAnswer();
        result.SessionId = "client.example;accounting;1";
        result.RecordType = DiameterAccountingRecordType.Stop;
        result.RecordNumber = 4;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterAccountingRecord CreateValidRecord(
        DateTimeOffset created)
    {
        DiameterAccountingRecord result =
            new DiameterAccountingRecord();
        result.RecordIdentifier = 41;
        result.Version = 2;
        result.Request = CreateValidRequest();
        result.Request.RecordType = DiameterAccountingRecordType.Start;
        result.State = DiameterAccountingState.Open;
        result.OutboundMessage = new DiameterMessage();
        result.PeerIdentity = CreateIdentity("accounting.example");
        result.RouteIdentifier = "accounting-route";
        result.CreatedAt = created;
        result.UpdatedAt = created.AddSeconds(2);
        result.LastAttemptAt = created.AddSeconds(1);
        result.AttemptCount = 1;
        result.Delivered = true;
        result.Durable = true;
        result.Code = "record-open";
        result.Message = "The START record is open.";
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static void AddInvalidRecord(
        List<DiameterAccountingRecord> records,
        DateTimeOffset created,
        Action<DiameterAccountingRecord> mutation)
    {
        DiameterAccountingRecord record = CreateValidRecord(created);
        mutation(record);
        records.Add(record);
    }
}

internal class AccountingContractProbe :
    IDiameterAccountingClient,
    IDiameterAccountingHandler,
    IAccountingRecordStore
{
    internal DiameterAccountingResult Result =
        new DiameterAccountingResult();
    internal DiameterAccountingAnswer Answer =
        new DiameterAccountingAnswer();
    internal DiameterAccountingStoreResult StoreResult =
        new DiameterAccountingStoreResult();
    internal List<DiameterAccountingRecord> Records =
        new List<DiameterAccountingRecord>();
    internal DiameterAccountingRequest? Request;
    internal DiameterAccountingRecord? Record;
    internal long RecordIdentifier;
    internal long ExpectedVersion;
    internal int SendCount;

    public DiameterAccountingResult SendDiameterAccountingEvent(
        DiameterAccountingRequest? request)
    {
        Request = request;
        SendCount = SendCount + 1;
        return Result;
    }

    public DiameterAccountingResult SendDiameterAccountingStart(
        DiameterAccountingRequest? request)
    {
        Request = request;
        SendCount = SendCount + 1;
        return Result;
    }

    public DiameterAccountingResult SendDiameterAccountingInterim(
        DiameterAccountingRequest? request)
    {
        Request = request;
        SendCount = SendCount + 1;
        return Result;
    }

    public DiameterAccountingResult SendDiameterAccountingStop(
        DiameterAccountingRequest? request)
    {
        Request = request;
        SendCount = SendCount + 1;
        return Result;
    }

    public List<DiameterAccountingRecord> ReadDiameterAccountingRecords()
    {
        return Records;
    }

    public DiameterAccountingAnswer HandleDiameterAccountingRequest(
        DiameterAccountingRequest? request)
    {
        Request = request;
        return Answer;
    }

    public DiameterAccountingStoreResult LoadDiameterAccountingRecord(
        long recordIdentifier)
    {
        RecordIdentifier = recordIdentifier;
        return StoreResult;
    }

    public DiameterAccountingStoreResult SaveDiameterAccountingRecord(
        DiameterAccountingRecord? record,
        long expectedVersion)
    {
        Record = record;
        ExpectedVersion = expectedVersion;
        return StoreResult;
    }

    public DiameterAccountingStoreResult RemoveDiameterAccountingRecord(
        long recordIdentifier,
        long expectedVersion)
    {
        RecordIdentifier = recordIdentifier;
        ExpectedVersion = expectedVersion;
        return StoreResult;
    }

    public List<DiameterAccountingRecord> ReadStoredDiameterAccountingRecords()
    {
        return Records;
    }
}
