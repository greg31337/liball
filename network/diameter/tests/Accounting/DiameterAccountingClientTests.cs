using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class DiameterAccountingClientTests
{
    [Test]
    public void ConstructorRejectsEveryMissingIdentityAndDependency()
    {
        DiameterIdentity host = CreateIdentity("client.example");
        DiameterIdentity realm = CreateIdentity("example");
        DiameterBaseAvpFactory avps = new DiameterBaseAvpFactory();
        ScriptedAccountingRequestClient requests =
            new ScriptedAccountingRequestClient();
        PolicyAccountingRecordStore store =
            new PolicyAccountingRecordStore();
        TestDiameterClock clock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(1000),
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    null, realm, avps, requests, store, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    host, null, avps, requests, store, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    host, realm, null, requests, store, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    host, realm, avps, null, store, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    host, realm, avps, requests, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterAccountingClient(
                    host, realm, avps, requests, store, null));
        });
    }

    [Test]
    public void EventBuildsCompleteIndependentAccountingRequest()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest request = CreateAccountingRequest(
            "event.example;1");
        request.RecordNumber = 99;
        request.AccountingSubSessionId = 7;
        request.AccountingSubSessionIdPresent = true;
        request.AccountingSessionId = new byte[] { 1, 2, 3 };
        request.AccountingMultiSessionId = "multi-1";
        request.EventTimestamp = context.Clock.UtcTime.AddSeconds(-1);
        request.EventTimestampPresent = true;
        request.CorrelationText = "event-correlation";
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 900;
        extension.Data = new byte[] { 4, 5 };
        request.ServiceAvps.Add(extension);

        DiameterAccountingResult result =
            context.Client.SendDiameterAccountingEvent(request);
        request.SessionId = "mutated";
        request.AccountingSessionId[0] = 9;
        extension.Data[0] = 9;
        DiameterSendRequest sent = context.RequestClient.Requests[0];
        context.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        List<DiameterAccountingRecord> records =
            context.Client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Delivered, Is.True);
            Assert.That(result.State,
                Is.EqualTo(DiameterAccountingState.PendingEvent));
            Assert.That(result.Request.RecordType,
                Is.EqualTo(DiameterAccountingRecordType.Event));
            Assert.That(result.Request.RecordNumber, Is.Zero);
            Assert.That(result.Request.SessionId,
                Is.EqualTo("event.example;1"));
            Assert.That(result.Request.AccountingSessionId,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Request.ServiceAvps[0].Data,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(sent.Request.CommandCode,
                Is.EqualTo(DiameterCommandCodes.Accounting));
            Assert.That(sent.Request.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.BaseAccounting));
            Assert.That(sent.Request.Flags,
                Is.EqualTo(DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable));
            Assert.That(ReadAccountingUnsigned32(
                sent.Request,
                DiameterAvpCodes.AccountingRecordType),
                Is.EqualTo((uint)DiameterAccountingRecordType.Event));
            Assert.That(ReadAccountingUnsigned32(
                sent.Request,
                DiameterAvpCodes.AccountingRecordNumber),
                Is.Zero);
            Assert.That(ReadAccountingUnsigned64(
                sent.Request,
                DiameterAvpCodes.AccountingSubSessionId),
                Is.EqualTo(7UL));
            Assert.That(FindAccountingAvp(sent.Request, 900).Data,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(sent.CorrelationText,
                Is.EqualTo("event-correlation"));
            Assert.That(records[0].State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(records[0].Acknowledged, Is.True);
        });
    }

    [Test]
    public void StartInterimStopUseMonotonicNumbersAndIntervals()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest start = CreateAccountingRequest(
            "sequence.example;1");
        start.AccountingMultiSessionId = "multi-sequence";
        start.InterimIntervalMilliseconds = 10000;

        DiameterAccountingResult startResult =
            context.Client.SendDiameterAccountingStart(start);
        context.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        List<DiameterAccountingRecord> afterStart =
            context.Client.ReadDiameterAccountingRecords();

        context.Clock.UtcTime = context.Clock.UtcTime.AddSeconds(10);
        DiameterAccountingRequest interim = CreateAccountingRequest(
            "sequence.example;1");
        interim.AccountingMultiSessionId = "multi-sequence";
        DiameterAccountingResult interimResult =
            context.Client.SendDiameterAccountingInterim(interim);
        context.RequestClient.CompleteAccountingRequest(
            1,
            DiameterResultCodes.Success,
            0,
            0);

        context.Clock.UtcTime = context.Clock.UtcTime.AddSeconds(10);
        DiameterAccountingRequest stop = CreateAccountingRequest(
            "sequence.example;1");
        stop.AccountingMultiSessionId = "multi-sequence";
        DiameterAccountingResult stopResult =
            context.Client.SendDiameterAccountingStop(stop);
        context.RequestClient.CompleteAccountingRequest(
            2,
            DiameterResultCodes.Success,
            0,
            0);
        List<DiameterAccountingRecord> records =
            context.Client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(startResult.Request.RecordNumber, Is.Zero);
            Assert.That(afterStart[0].State,
                Is.EqualTo(DiameterAccountingState.Open));
            Assert.That(afterStart[0].NextInterimPresent, Is.True);
            Assert.That(afterStart[0].NextInterimAt,
                Is.EqualTo(context.InitialTime.AddSeconds(10)));
            Assert.That(interimResult.Request.RecordNumber,
                Is.EqualTo(1U));
            Assert.That(interimResult.Request.InterimIntervalMilliseconds,
                Is.EqualTo(10000));
            Assert.That(stopResult.Request.RecordNumber,
                Is.EqualTo(2U));
            Assert.That(records.Count, Is.EqualTo(3));
            Assert.That(records[0].State,
                Is.EqualTo(DiameterAccountingState.Open));
            Assert.That(records[1].State,
                Is.EqualTo(DiameterAccountingState.Open));
            Assert.That(records[2].State,
                Is.EqualTo(DiameterAccountingState.Closed));
            Assert.That(records[2].NextInterimPresent, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                context.Client.SendDiameterAccountingInterim(stop));
        });
    }

    [Test]
    public void SubSessionsHaveIndependentSequencesAndStableMultiSessionIds()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest first = CreateSubSessionRequest(
            "sub.example;1",
            10,
            "multi-a");
        DiameterAccountingRequest second = CreateSubSessionRequest(
            "sub.example;1",
            20,
            "multi-b");

        DiameterAccountingResult firstStart =
            context.Client.SendDiameterAccountingStart(first);
        context.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingResult secondStart =
            context.Client.SendDiameterAccountingStart(second);
        context.RequestClient.CompleteAccountingRequest(
            1,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingRequest firstInterim = CreateSubSessionRequest(
            "sub.example;1",
            10,
            "multi-a");
        DiameterAccountingResult interim =
            context.Client.SendDiameterAccountingInterim(firstInterim);
        context.RequestClient.CompleteAccountingRequest(
            2,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingRequest wrongMulti = CreateSubSessionRequest(
            "sub.example;1",
            20,
            "changed");

        Assert.Multiple(() =>
        {
            Assert.That(firstStart.Request.RecordNumber, Is.Zero);
            Assert.That(secondStart.Request.RecordNumber, Is.Zero);
            Assert.That(interim.Request.RecordNumber, Is.EqualTo(1U));
            Assert.That(interim.Request.AccountingSubSessionId,
                Is.EqualTo(10UL));
            Assert.Throws<ArgumentException>(() =>
                context.Client.SendDiameterAccountingInterim(wrongMulti));
            Assert.Throws<InvalidOperationException>(() =>
                context.Client.SendDiameterAccountingStart(first));
        });
    }

    [Test]
    public void AnswerOverridesIntervalAndMustMatchAccountingIdentifiers()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest start = CreateAccountingRequest(
            "answer.example;1");
        start.InterimIntervalMilliseconds = 5000;
        context.Client.SendDiameterAccountingStart(start);

        context.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            12);
        List<DiameterAccountingRecord> successful =
            context.Client.ReadDiameterAccountingRecords();

        DiameterAccountingRequest eventRequest = CreateAccountingRequest(
            "answer.example;event");
        context.Client.SendDiameterAccountingEvent(eventRequest);
        context.RequestClient.CompleteAccountingRequest(
            1,
            DiameterResultCodes.Success,
            1,
            0);
        List<DiameterAccountingRecord> records =
            context.Client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(successful[0].State,
                Is.EqualTo(DiameterAccountingState.Open));
            Assert.That(successful[0].Acknowledged, Is.True);
            Assert.That(
                successful[0].Request.InterimIntervalMilliseconds,
                Is.EqualTo(12000));
            Assert.That(successful[0].NextInterimAt,
                Is.EqualTo(context.InitialTime.AddSeconds(12)));
            Assert.That(records[1].State,
                Is.EqualTo(DiameterAccountingState.PendingEvent));
            Assert.That(records[1].Acknowledged, Is.False);
            Assert.That(records[1].LastAnswer, Is.Null);
            Assert.That(records[1].Durable, Is.True);
            Assert.That(records[1].NextAttemptPresent, Is.True);
            Assert.That(records[1].Code,
                Is.EqualTo("accounting-pending-retained"));
        });
    }

    [Test]
    public void InvalidSequenceAndManagedExtensionsAreRejected()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest interim = CreateAccountingRequest(
            "invalid.example;1");
        DiameterAccountingRequest stop = CreateAccountingRequest(
            "invalid.example;1");
        DiameterAccountingRequest managed = CreateAccountingRequest(
            "invalid.example;2");
        DiameterAvp recordNumber = new DiameterAvp();
        recordNumber.Code = DiameterAvpCodes.AccountingRecordNumber;
        recordNumber.Data = new byte[4];
        managed.ServiceAvps.Add(recordNumber);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                context.Client.SendDiameterAccountingEvent(null));
            Assert.Throws<InvalidOperationException>(() =>
                context.Client.SendDiameterAccountingInterim(interim));
            Assert.Throws<InvalidOperationException>(() =>
                context.Client.SendDiameterAccountingStop(stop));
            Assert.Throws<ArgumentException>(() =>
                context.Client.SendDiameterAccountingEvent(managed));
            Assert.That(context.RequestClient.Requests, Is.Empty);
        });
    }

    [Test]
    public void IncompleteMalformedAndHeaderMismatchedAnswersAreRetained()
    {
        AccountingClientTestContext context = CreateAccountingContext();
        DiameterAccountingRequest subSession = CreateAccountingRequest(
            "answer-sub-session;1");
        subSession.AccountingSubSessionId = 17;
        subSession.AccountingSubSessionIdPresent = true;
        context.Client.SendDiameterAccountingEvent(subSession);
        DiameterMessage missingSubSession =
            CreateAccountingTestAnswer(context, 0, false);
        CompleteAccountingTestAnswer(
            context,
            0,
            missingSubSession);

        DiameterAccountingRequest malformed = CreateAccountingRequest(
            "answer-malformed;1");
        context.Client.SendDiameterAccountingEvent(malformed);
        DiameterMessage malformedInterval =
            CreateAccountingTestAnswer(context, 1, true);
        DiameterAvp interval = new DiameterAvp();
        interval.Code = DiameterAvpCodes.AcctInterimInterval;
        interval.Flags = DiameterAvpFlags.Mandatory;
        interval.Data = new byte[] { 0, 0, 1 };
        malformedInterval.Avps.Add(interval);
        CompleteAccountingTestAnswer(
            context,
            1,
            malformedInterval);

        DiameterAccountingRequest mismatched = CreateAccountingRequest(
            "answer-header;1");
        context.Client.SendDiameterAccountingEvent(mismatched);
        DiameterMessage badHeader =
            CreateAccountingTestAnswer(context, 2, true);
        badHeader.HopByHopIdentifier =
            badHeader.HopByHopIdentifier + 1U;
        CompleteAccountingTestAnswer(context, 2, badHeader);

        List<DiameterAccountingRecord> records =
            context.Client.ReadDiameterAccountingRecords();

        Assert.Multiple(() =>
        {
            Assert.That(records, Has.Count.EqualTo(3));
            Assert.That(records[0].Code,
                Is.EqualTo("accounting-pending-retained"));
            Assert.That(records[0].Acknowledged, Is.False);
            Assert.That(records[0].LastAnswer, Is.Null);
            Assert.That(records[0].Durable, Is.True);
            Assert.That(records[1].Code,
                Is.EqualTo("accounting-pending-retained"));
            Assert.That(records[1].Request.InterimIntervalMilliseconds,
                Is.Zero);
            Assert.That(records[1].Acknowledged, Is.False);
            Assert.That(records[1].Durable, Is.True);
            Assert.That(records[2].Code,
                Is.EqualTo("accounting-pending-retained"));
            Assert.That(records[2].Acknowledged, Is.False);
            Assert.That(records[2].Durable, Is.True);
            Assert.That(context.RequestClient.Pending, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void SequenceRejectsChangedApplicationSessionAndExhaustedNumber()
    {
        AccountingClientTestContext application =
            CreateAccountingContext();
        DiameterAccountingRequest applicationStart =
            CreateAccountingRequest("sequence-application;1");
        applicationStart.AccountingSessionId = new byte[] { 1 };
        application.Client.SendDiameterAccountingStart(applicationStart);
        application.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingRequest changedApplication =
            CreateAccountingRequest("sequence-application;1");
        changedApplication.ApplicationId =
            DiameterApplicationIds.CreditControl;
        changedApplication.AccountingSessionId = new byte[] { 1 };

        AccountingClientTestContext session = CreateAccountingContext();
        DiameterAccountingRequest sessionStart =
            CreateAccountingRequest("sequence-session;1");
        sessionStart.AccountingSessionId = new byte[] { 1 };
        session.Client.SendDiameterAccountingStart(sessionStart);
        session.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        DiameterAccountingRequest changedSession =
            CreateAccountingRequest("sequence-session;1");
        changedSession.AccountingSessionId = new byte[] { 2 };

        AccountingClientTestContext exhausted =
            CreateAccountingContext();
        DiameterAccountingRequest exhaustedStart =
            CreateAccountingRequest("sequence-exhausted;1");
        exhausted.Client.SendDiameterAccountingStart(exhaustedStart);
        exhausted.RequestClient.CompleteAccountingRequest(
            0,
            DiameterResultCodes.Success,
            0,
            0);
        exhausted.Client.Runtime.Records[0].Request.RecordNumber =
            uint.MaxValue;
        DiameterAccountingRequest next =
            CreateAccountingRequest("sequence-exhausted;1");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => application.Client.SendDiameterAccountingInterim(
                    changedApplication),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => session.Client.SendDiameterAccountingInterim(
                    changedSession),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => exhausted.Client.SendDiameterAccountingInterim(
                    next),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(application.RequestClient.Requests,
                Has.Count.EqualTo(1));
            Assert.That(session.RequestClient.Requests,
                Has.Count.EqualTo(1));
            Assert.That(exhausted.RequestClient.Requests,
                Has.Count.EqualTo(1));
        });
    }

    private static DiameterMessage CreateAccountingTestAnswer(
        AccountingClientTestContext context,
        int requestIndex,
        bool includeSubSession)
    {
        DiameterMessage request =
            context.RequestClient.Requests[requestIndex].Request;
        DiameterMessage result = new DiameterMessage();
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        result.Avps.Add(context.RequestClient.CloneAccountingTestAvp(
            context.RequestClient.FindAccountingTestAvp(
                request,
                DiameterAvpCodes.SessionId)));
        result.Avps.Add(
            context.RequestClient.CreateAccountingTestUnsigned32(
                DiameterAvpCodes.ResultCode,
                DiameterResultCodes.Success));
        result.Avps.Add(context.RequestClient.CloneAccountingTestAvp(
            context.RequestClient.FindAccountingTestAvp(
                request,
                DiameterAvpCodes.AccountingRecordType)));
        result.Avps.Add(context.RequestClient.CloneAccountingTestAvp(
            context.RequestClient.FindAccountingTestAvp(
                request,
                DiameterAvpCodes.AccountingRecordNumber)));
        DiameterAvp? subSession =
            context.RequestClient.FindOptionalAccountingTestAvp(
                request,
                DiameterAvpCodes.AccountingSubSessionId);
        if (includeSubSession && subSession != null)
        {
            result.Avps.Add(
                context.RequestClient.CloneAccountingTestAvp(
                    subSession));
        }

        return result;
    }

    private static void CompleteAccountingTestAnswer(
        AccountingClientTestContext context,
        int requestIndex,
        DiameterMessage answer)
    {
        DiameterPendingRequestSnapshot pending =
            context.RequestClient.Pending[requestIndex];
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = pending.RequestIdentifier;
        result.PendingRequest = pending;
        result.Answer = answer;
        result.PeerIdentity = pending.PeerIdentity;
        result.AnswerReceived = true;
        result.Correlated = true;
        result.Successful = true;
        result.ResultCode = DiameterResultCodes.Success;
        result.Code = "scripted-answer";
        context.Client.HandleDiameterAnswer(result);
    }

    private static AccountingClientTestContext CreateAccountingContext()
    {
        AccountingClientTestContext result =
            new AccountingClientTestContext();
        result.InitialTime = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
        result.Clock = new TestDiameterClock(result.InitialTime, 0);
        result.RequestClient = new ScriptedAccountingRequestClient();
        result.Client = new DiameterAccountingClient(
            CreateIdentity("client.example"),
            CreateIdentity("example"),
            new DiameterBaseAvpFactory(),
            result.RequestClient,
            new PolicyAccountingRecordStore(),
            result.Clock);
        return result;
    }

    private static DiameterAccountingRequest CreateAccountingRequest(
        string sessionId)
    {
        DiameterAccountingRequest result =
            new DiameterAccountingRequest();
        result.SessionId = sessionId;
        result.ApplicationId = DiameterApplicationIds.BaseAccounting;
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost =
            CreateIdentity("accounting.remote.example");
        result.PeerIdentity = CreateIdentity("peer.remote.example");
        result.UserName = "alice@remote.example";
        return result;
    }

    private static DiameterAccountingRequest CreateSubSessionRequest(
        string sessionId,
        ulong subSessionId,
        string multiSessionId)
    {
        DiameterAccountingRequest result =
            CreateAccountingRequest(sessionId);
        result.AccountingSubSessionId = subSessionId;
        result.AccountingSubSessionIdPresent = true;
        result.AccountingMultiSessionId = multiSessionId;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterAvp FindAccountingAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        throw new AssertionException("The expected accounting AVP is absent.");
    }

    private static uint ReadAccountingUnsigned32(
        DiameterMessage message,
        uint code)
    {
        return NetworkByteOrder.ReadUInt32(
            FindAccountingAvp(message, code).Data);
    }

    private static ulong ReadAccountingUnsigned64(
        DiameterMessage message,
        uint code)
    {
        return NetworkByteOrder.ReadUInt64(
            FindAccountingAvp(message, code).Data);
    }
}

internal class AccountingClientTestContext
{
    internal DateTimeOffset InitialTime;
    internal TestDiameterClock Clock = new TestDiameterClock(
        default(DateTimeOffset),
        0);
    internal ScriptedAccountingRequestClient RequestClient =
        new ScriptedAccountingRequestClient();
    internal DiameterAccountingClient Client = null!;
}

internal class ScriptedAccountingRequestClient : IDiameterRequestClient
{
    internal List<DiameterSendRequest> Requests =
        new List<DiameterSendRequest>();
    internal List<IDiameterAnswerHandler?> Handlers =
        new List<IDiameterAnswerHandler?>();
    internal List<DiameterPendingRequestSnapshot> Pending =
        new List<DiameterPendingRequestSnapshot>();
    internal long NextRequestIdentifier = 101;

    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        if (request == null)
        {
            throw new ArgumentException("A scripted request is required.");
        }

        DiameterPendingRequestSnapshot pending =
            new DiameterPendingRequestSnapshot();
        pending.RequestIdentifier = NextRequestIdentifier;
        NextRequestIdentifier = NextRequestIdentifier + 1;
        pending.SendRequest = request;
        pending.OutboundMessage = request.Request;
        pending.PeerIdentity.Value = "peer.remote.example";
        Requests.Add(request);
        Handlers.Add(handler);
        Pending.Add(pending);
        DiameterSendResult result = new DiameterSendResult();
        result.Status = DiameterSendStatus.Accepted;
        result.PendingRequest = pending;
        result.PeerIdentity = pending.PeerIdentity;
        result.Accepted = true;
        result.Delivered = true;
        result.Code = "request-delivered";
        return result;
    }

    public DiameterAnswerResult CancelDiameterRequest(
        long requestIdentifier)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = requestIdentifier;
        result.Cancelled = true;
        return result;
    }

    public List<DiameterPendingRequestSnapshot> ReadPendingDiameterRequests()
    {
        return Pending;
    }

    internal void CompleteAccountingRequest(
        int requestIndex,
        uint resultCode,
        uint recordNumberDelta,
        uint interimIntervalSeconds)
    {
        DiameterPendingRequestSnapshot pending = Pending[requestIndex];
        DiameterMessage request = Requests[requestIndex].Request;
        DiameterMessage answer = new DiameterMessage();
        answer.CommandCode = request.CommandCode;
        answer.ApplicationId = request.ApplicationId;
        answer.HopByHopIdentifier = request.HopByHopIdentifier;
        answer.EndToEndIdentifier = request.EndToEndIdentifier;
        answer.Avps.Add(CloneAccountingTestAvp(
            FindAccountingTestAvp(request, DiameterAvpCodes.SessionId)));
        answer.Avps.Add(CreateAccountingTestUnsigned32(
            DiameterAvpCodes.ResultCode,
            resultCode));
        answer.Avps.Add(CloneAccountingTestAvp(
            FindAccountingTestAvp(
                request,
                DiameterAvpCodes.AccountingRecordType)));
        uint recordNumber = NetworkByteOrder.ReadUInt32(
            FindAccountingTestAvp(
                request,
                DiameterAvpCodes.AccountingRecordNumber).Data);
        answer.Avps.Add(CreateAccountingTestUnsigned32(
            DiameterAvpCodes.AccountingRecordNumber,
            recordNumber + recordNumberDelta));
        DiameterAvp? subSession = FindOptionalAccountingTestAvp(
            request,
            DiameterAvpCodes.AccountingSubSessionId);
        if (subSession != null)
        {
            answer.Avps.Add(CloneAccountingTestAvp(subSession));
        }

        if (interimIntervalSeconds > 0)
        {
            answer.Avps.Add(CreateAccountingTestUnsigned32(
                DiameterAvpCodes.AcctInterimInterval,
                interimIntervalSeconds));
        }

        DiameterAnswerResult outcome = new DiameterAnswerResult();
        outcome.RequestIdentifier = pending.RequestIdentifier;
        outcome.PendingRequest = pending;
        outcome.Answer = answer;
        outcome.PeerIdentity = pending.PeerIdentity;
        outcome.AnswerReceived = true;
        outcome.Correlated = true;
        outcome.Successful = resultCode >= 2000U && resultCode <= 2999U;
        outcome.ResultCode = resultCode;
        outcome.Code = "scripted-answer";
        IDiameterAnswerHandler? handler = Handlers[requestIndex];
        if (handler != null)
        {
            handler.HandleDiameterAnswer(outcome);
        }
    }

    internal DiameterAvp FindAccountingTestAvp(
        DiameterMessage message,
        uint code)
    {
        DiameterAvp? result = FindOptionalAccountingTestAvp(message, code);
        if (result == null)
        {
            throw new AssertionException(
                "The scripted accounting AVP is absent.");
        }

        return result;
    }

    internal DiameterAvp? FindOptionalAccountingTestAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                return avp;
            }

            index = index + 1;
        }

        return null;
    }

    internal DiameterAvp CloneAccountingTestAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }

    internal DiameterAvp CreateAccountingTestUnsigned32(
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
}
