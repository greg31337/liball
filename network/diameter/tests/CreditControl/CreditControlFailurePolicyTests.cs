using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlFailurePolicyTests
{
    [TestCase(
        DiameterCreditControlFailureHandling.Terminate,
        false,
        true)]
    [TestCase(
        DiameterCreditControlFailureHandling.Continue,
        true,
        false)]
    [TestCase(
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        false,
        true)]
    public void PermanentSessionFailureAppliesConfiguredCcfh(
        DiameterCreditControlFailureHandling handling,
        bool continueService,
        bool terminateService)
    {
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                store,
                CreateClock());
        DiameterCreditControlRequest request = CreateSessionRequest(
            handling);
        machine.BeginCreditControlClientSession(request, 701);

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientAnswer(
                701,
                CreateSessionAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.CreditLimitReached));

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.State, Is.EqualTo(continueService ?
                DiameterCreditControlState.Idle :
                DiameterCreditControlState.Failed));
            Assert.That(store.Sessions, Is.Empty);
        });
    }

    [Test]
    public void TemporaryFailureRequestsRetryAndAcceptsLaterAnswer()
    {
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                store,
                CreateClock());
        DiameterCreditControlRequest request = CreateSessionRequest(
            DiameterCreditControlFailureHandling.RetryAndTerminate);
        machine.BeginCreditControlClientSession(request, 702);

        DiameterCreditControlResult retry = machine
            .ProcessCreditControlClientAnswer(
                702,
                CreateSessionAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.TooBusy));
        DiameterCreditControlResult recovered = machine
            .ProcessCreditControlClientAnswer(
                702,
                CreateSessionAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.Success));

        Assert.Multiple(() =>
        {
            Assert.That(retry.RetryRequired, Is.True);
            Assert.That(retry.ContinueService, Is.False);
            Assert.That(retry.TerminateService, Is.False);
            Assert.That(retry.State,
                Is.EqualTo(DiameterCreditControlState.PendingInitial));
            Assert.That(retry.Session!.PendingRequestPresent, Is.True);
            Assert.That(recovered.Successful, Is.True);
            Assert.That(recovered.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(recovered.ContinueService, Is.True);
        });
    }

    [TestCase(
        DiameterCreditControlFailureHandling.Terminate,
        false,
        false,
        true)]
    [TestCase(
        DiameterCreditControlFailureHandling.Continue,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        true,
        true,
        false)]
    public void TransactionExpiryAppliesEveryCcfh(
        DiameterCreditControlFailureHandling handling,
        bool retryRequired,
        bool continueService,
        bool terminateService)
    {
        TestDiameterClock clock = CreateClock();
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest request = CreateSessionRequest(
            handling);
        request.TimeoutMilliseconds = 1000;
        machine.BeginCreditControlClientSession(request, 703);
        clock.UtcTime = clock.UtcTime.AddMilliseconds(1001);

        List<DiameterCreditControlResult> expired =
            machine.ExpireCreditControlClientTransactions();
        DiameterCreditControlResult result = expired[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.RetryRequired,
                Is.EqualTo(retryRequired));
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(result.Session!.PendingRequestPresent,
                Is.EqualTo(continueService));
            Assert.That(store.Sessions.Count,
                Is.EqualTo(continueService ? 1 : 0));
        });
    }

    [Test]
    public void AnswerCcfhOverridesLocalFallbackForLaterFailure()
    {
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                store,
                CreateClock());
        DiameterCreditControlRequest initial = CreateSessionRequest(
            DiameterCreditControlFailureHandling.Terminate);
        machine.BeginCreditControlClientSession(initial, 704);
        DiameterCreditControlAnswer opened = CreateSessionAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);
        opened.FailureHandling =
            DiameterCreditControlFailureHandling.Continue;
        opened.FailureHandlingPresent = true;
        machine.ProcessCreditControlClientAnswer(704, opened);
        DiameterCreditControlRequest update = CreateSessionRequest(
            DiameterCreditControlFailureHandling.Terminate);
        update.RequestType = DiameterCreditControlRequestType.Update;
        machine.UpdateCreditControlClientSession(update, 705);

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientAnswer(
                705,
                CreateSessionAnswer(
                    DiameterCreditControlRequestType.Update,
                    1U,
                    DiameterResultCodes.RatingFailed));

        Assert.Multiple(() =>
        {
            Assert.That(result.ContinueService, Is.True);
            Assert.That(result.TerminateService, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("credit-control-failure-continued"));
            Assert.That(store.Sessions, Is.Empty);
        });
    }

    [TestCase(
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        false,
        true)]
    [TestCase(
        DiameterDirectDebitFailureHandling.Continue,
        true,
        false)]
    public void FailedDebitAnswerAppliesDdfh(
        DiameterDirectDebitFailureHandling handling,
        bool continueService,
        bool terminateService)
    {
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateEventRequest(
            DiameterRequestedAction.DirectDebiting,
            handling);
        request.AllowFailover = false;
        DiameterCreditControlAnswer answer = CreateEventFailureAnswer(
            request,
            DiameterResultCodes.CreditLimitReached);

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientEvent(request, answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(result.BufferRequest, Is.False);
        });
    }

    [TestCase(
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        true)]
    [TestCase(
        DiameterDirectDebitFailureHandling.Continue,
        false)]
    public void AmbiguousDebitFailureContinuesAndAppliesBufferPolicy(
        DiameterDirectDebitFailureHandling handling,
        bool bufferRequest)
    {
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateEventRequest(
            DiameterRequestedAction.DirectDebiting,
            handling);
        request.AllowFailover = false;

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientEventFailure(request, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.AnswerReceived, Is.False);
            Assert.That(result.ContinueService, Is.True);
            Assert.That(result.TerminateService, Is.False);
            Assert.That(result.BufferRequest,
                Is.EqualTo(bufferRequest));
            Assert.That(result.RetryRequired, Is.False);
        });
    }

    [Test]
    public void RefundDeliveryFailureBuffersWithoutGrantingService()
    {
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateEventRequest(
            DiameterRequestedAction.RefundAccount,
            DiameterDirectDebitFailureHandling.TerminateOrBuffer);
        request.AllowFailover = false;

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientEventFailure(request, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.BufferRequest, Is.True);
            Assert.That(result.ContinueService, Is.False);
            Assert.That(result.TerminateService, Is.False);
            Assert.That(result.Delivered, Is.False);
        });
    }

    private static DiameterCreditControlRequest CreateSessionRequest(
        DiameterCreditControlFailureHandling handling)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;failure;1";
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.failure";
        result.UserName = "alice@remote.example";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        result.FailureHandling = handling;
        return result;
    }

    private static DiameterCreditControlAnswer CreateSessionAnswer(
        DiameterCreditControlRequestType type,
        uint number,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = "client.example;failure;1";
        result.RequestType = type;
        result.RequestNumber = number;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        return result;
    }

    private static DiameterCreditControlRequest CreateEventRequest(
        DiameterRequestedAction action,
        DiameterDirectDebitFailureHandling handling)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;failure-event;" + (int)action;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.failure-event";
        result.RequestType = DiameterCreditControlRequestType.Event;
        result.RequestedAction = action;
        result.RequestedActionPresent = true;
        result.DirectDebitFailureHandling = handling;
        return result;
    }

    private static DiameterCreditControlAnswer CreateEventFailureAnswer(
        DiameterCreditControlRequest request,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = resultCode;
        result.Disposition = DiameterCreditControlDisposition.Failed;
        return result;
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                22,
                0,
                0,
                TimeSpan.Zero),
            0);
    }
}
