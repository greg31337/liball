using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlStateMachineMatrixTests
{
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlState.PendingInitial,
        DiameterCreditControlState.Open,
        0U,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlState.PendingUpdate,
        DiameterCreditControlState.Open,
        1U,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlState.PendingTermination,
        DiameterCreditControlState.Closed,
        1U,
        false)]
    public void ClientSuccessTransitionMatrixClearsTxStateAndAppliesCleanup(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlState pendingState,
        DiameterCreditControlState completedState,
        uint requestNumber,
        bool retained)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        PrepareClientOperation(context, requestType, 100L);

        DiameterCreditControlSession pending = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId)!;
        DiameterCreditControlResult completed = context.Machine
            .ProcessCreditControlClientAnswer(
                100L,
                CreateAnswer(
                    requestType,
                    requestNumber,
                    DiameterResultCodes.Success));
        DiameterCreditControlSession? stored = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId);

        Assert.Multiple(() =>
        {
            Assert.That(pending.State, Is.EqualTo(pendingState));
            Assert.That(pending.PendingRequestPresent, Is.True);
            Assert.That(pending.TransactionDeadlinePresent, Is.True);
            Assert.That(pending.PendingRequestNumber, Is.EqualTo(requestNumber));
            Assert.That(completed.State, Is.EqualTo(completedState));
            Assert.That(completed.Successful, Is.True);
            Assert.That(completed.Session!.PendingRequestPresent, Is.False);
            Assert.That(completed.Session.TransactionDeadlinePresent, Is.False);
            Assert.That(completed.Session.LastCompletedRequestIdentifier,
                Is.EqualTo(100L));
            Assert.That(stored != null, Is.EqualTo(retained));
        });
    }

    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.Idle,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.Idle,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.Failed,
        false,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingInitial,
        true,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingUpdate,
        true,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true)]
    public void ClientFailureTransitionMatrixAppliesCcfhAndTerminationRules(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlFailureHandling handling,
        DiameterCreditControlState expectedState,
        bool retry,
        bool continueService,
        bool terminateService)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        context.FailureHandling = handling;
        PrepareClientOperation(context, requestType, 200L);

        DiameterCreditControlResult result = context.Machine
            .ProcessCreditControlClientAnswer(
                200L,
                CreateAnswer(
                    requestType,
                    requestType == DiameterCreditControlRequestType.Initial ? 0U : 1U,
                    retry ? DiameterResultCodes.TooBusy :
                        DiameterResultCodes.RatingFailed));
        DiameterCreditControlSession? stored = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.RetryRequired, Is.EqualTo(retry));
            Assert.That(result.ContinueService, Is.EqualTo(continueService));
            Assert.That(result.TerminateService, Is.EqualTo(terminateService));
            Assert.That(stored != null, Is.EqualTo(retry));
            Assert.That(result.Session!.PendingRequestPresent, Is.EqualTo(retry));
        });
    }

    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.PendingInitial,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.PendingUpdate,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.Failed,
        false,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingInitial,
        true,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingUpdate,
        true,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.Failed,
        false,
        false)]
    public void ClientTxTimerMatrixCoversEveryPendingStateAndPolicy(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlFailureHandling handling,
        DiameterCreditControlState expectedState,
        bool retained,
        bool retry)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        context.FailureHandling = handling;
        PrepareClientOperation(context, requestType, 300L);
        DiameterCreditControlSession pending = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId)!;
        context.Clock.UtcTime = pending.TransactionDeadline.AddTicks(-1L);

        List<DiameterCreditControlResult> early = context.Machine
            .ExpireCreditControlClientTransactions();
        context.Clock.UtcTime = pending.TransactionDeadline;
        List<DiameterCreditControlResult> expired = context.Machine
            .ExpireCreditControlClientTransactions();
        DiameterCreditControlResult result = expired[0];

        Assert.Multiple(() =>
        {
            Assert.That(early, Is.Empty);
            Assert.That(expired, Has.Count.EqualTo(1));
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.RetryRequired, Is.EqualTo(retry));
            Assert.That(result.ContinueService, Is.EqualTo(retained));
            Assert.That(result.TerminateService, Is.EqualTo(!retained));
            Assert.That(result.Session!.PendingRequestPresent,
                Is.EqualTo(retained));
            Assert.That(context.Store.LoadDiameterCreditControlSession(
                ClientSessionId) != null, Is.EqualTo(retained));
        });
    }

    [TestCase(DiameterCreditControlRequestType.Initial, true)]
    [TestCase(DiameterCreditControlRequestType.Update, true)]
    [TestCase(DiameterCreditControlRequestType.Termination, false)]
    public void ClientDuplicateAnswerMatrixCoversRetainedAndCleanedSessions(
        DiameterCreditControlRequestType requestType,
        bool replayable)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        PrepareClientOperation(context, requestType, 400L);
        uint number = requestType == DiameterCreditControlRequestType.Initial ? 0U : 1U;
        DiameterCreditControlAnswer answer = CreateAnswer(
            requestType,
            number,
            DiameterResultCodes.Success);
        context.Machine.ProcessCreditControlClientAnswer(400L, answer);

        if (!replayable)
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                context.Machine.ProcessCreditControlClientAnswer(400L, answer));
            Assert.That(context.Store.Sessions, Is.Empty);
            return;
        }

        DiameterCreditControlResult duplicate = context.Machine
            .ProcessCreditControlClientAnswer(400L, answer);
        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Retransmission, Is.True);
            Assert.That(duplicate.Code,
                Is.EqualTo("credit-control-answer-duplicate"));
            Assert.That(context.Store.Sessions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ConflictingDuplicateClientAnswerIsRejectedWithoutChangingOpenSession()
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        PrepareClientOperation(
            context,
            DiameterCreditControlRequestType.Initial,
            450L);
        context.Machine.ProcessCreditControlClientAnswer(
            450L,
            CreateAnswer(
                DiameterCreditControlRequestType.Initial,
                0U,
                DiameterResultCodes.Success));

        Assert.Throws<DiameterCreditControlException>(() =>
            context.Machine.ProcessCreditControlClientAnswer(
                450L,
                CreateAnswer(
                    DiameterCreditControlRequestType.Initial,
                    0U,
                    DiameterResultCodes.CreditLimitReached)));
        DiameterCreditControlSession stored = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId)!;
        Assert.Multiple(() =>
        {
            Assert.That(stored.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(stored.LastAnswer!.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(stored.PendingRequestPresent, Is.False);
        });
    }

    [TestCase(DiameterCreditControlRequestType.Initial, false)]
    [TestCase(DiameterCreditControlRequestType.Initial, true)]
    [TestCase(DiameterCreditControlRequestType.Update, false)]
    [TestCase(DiameterCreditControlRequestType.Update, true)]
    [TestCase(DiameterCreditControlRequestType.Termination, false)]
    [TestCase(DiameterCreditControlRequestType.Termination, true)]
    public void DeliveryFailureCleansEveryPendingStateAndPreservesDeliveryFlag(
        DiameterCreditControlRequestType requestType,
        bool delivered)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        PrepareClientOperation(context, requestType, 475L);

        DiameterCreditControlResult result = context.Machine
            .ProcessCreditControlClientDeliveryFailure(475L, delivered);

        Assert.Multiple(() =>
        {
            Assert.That(result.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(result.Delivered, Is.EqualTo(delivered));
            Assert.That(result.TerminateService, Is.True);
            Assert.That(result.Session!.PendingRequestPresent, Is.False);
            Assert.That(context.Store.Sessions, Is.Empty);
            Assert.Throws<DiameterCreditControlException>(() =>
                context.Machine.ProcessCreditControlClientDeliveryFailure(
                    475L,
                    delivered));
        });
    }

    [TestCase("identifier")]
    [TestCase("type")]
    [TestCase("number")]
    [TestCase("session")]
    public void ClientMismatchedAnswersLeavePendingStateUntouched(string mismatch)
    {
        CreditControlClientMatrixContext context = CreateClientContext();
        PrepareClientOperation(
            context,
            DiameterCreditControlRequestType.Initial,
            500L);
        DiameterCreditControlAnswer answer = CreateAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);
        long identifier = 500L;
        if (mismatch == "identifier")
        {
            identifier = 501L;
        }
        else if (mismatch == "type")
        {
            answer.RequestType = DiameterCreditControlRequestType.Update;
        }
        else if (mismatch == "number")
        {
            answer.RequestNumber = 1U;
        }
        else
        {
            answer.SessionId = "unknown.example;credit;1";
        }

        Assert.Throws<DiameterCreditControlException>(() =>
            context.Machine.ProcessCreditControlClientAnswer(identifier, answer));
        DiameterCreditControlSession pending = context.Store
            .LoadDiameterCreditControlSession(ClientSessionId)!;
        Assert.Multiple(() =>
        {
            Assert.That(pending.State,
                Is.EqualTo(DiameterCreditControlState.PendingInitial));
            Assert.That(pending.PendingRequestPresent, Is.True);
            Assert.That(pending.PendingRequestIdentifier, Is.EqualTo(500L));
            Assert.That(pending.TransactionDeadlinePresent, Is.True);
        });
    }

    [TestCase(
        DiameterCreditControlRequestType.Initial,
        true,
        DiameterCreditControlState.Open,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        false,
        DiameterCreditControlState.Failed,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        true,
        DiameterCreditControlState.Open,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        false,
        DiameterCreditControlState.Failed,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        true,
        DiameterCreditControlState.Closed,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        false,
        DiameterCreditControlState.Closed,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Event,
        true,
        DiameterCreditControlState.Idle,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Event,
        false,
        DiameterCreditControlState.Idle,
        false)]
    public void ServerDecisionMatrixCoversEveryRequestTypeAndHandlerResult(
        DiameterCreditControlRequestType requestType,
        bool successful,
        DiameterCreditControlState expectedState,
        bool retained)
    {
        CreditControlServerMatrixContext context = CreateServerContext();
        uint requestNumber = PrepareServerOperation(context, requestType);
        context.Handler.ResultCode = successful ?
            DiameterResultCodes.Success :
            DiameterResultCodes.CreditLimitReached;
        DiameterCreditControlRequest request = CreateRequest(
            ServerSessionId,
            requestType);
        request.RequestNumber = requestNumber;

        DiameterCreditControlResult result = context.Machine
            .ProcessCreditControlServerRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(expectedState));
            Assert.That(result.Successful, Is.EqualTo(successful));
            Assert.That(result.AnswerReceived, Is.True);
            Assert.That(result.Session != null, Is.EqualTo(retained));
            Assert.That(context.Store.LoadDiameterCreditControlSession(
                ServerSessionId) != null, Is.EqualTo(retained));
            Assert.That(context.Handler.Requests[^1].RequestType,
                Is.EqualTo(requestType));
        });
    }

    [TestCase(DiameterCreditControlRequestType.Initial, 0U)]
    [TestCase(DiameterCreditControlRequestType.Update, 1U)]
    [TestCase(DiameterCreditControlRequestType.Termination, 1U)]
    public void ServerReplaysEverySessionRequestTypeWithoutCallingHandlerAgain(
        DiameterCreditControlRequestType requestType,
        uint requestNumber)
    {
        CreditControlServerMatrixContext context = CreateServerContext();
        PrepareServerOperation(context, requestType);
        DiameterCreditControlRequest request = CreateRequest(
            ServerSessionId,
            requestType);
        request.RequestNumber = requestNumber;
        DiameterCreditControlResult first = context.Machine
            .ProcessCreditControlServerRequest(request);
        int callCount = context.Handler.Requests.Count;
        long version = first.Session!.Version;

        DiameterCreditControlResult replay = context.Machine
            .ProcessCreditControlServerRequest(CreateRequestCopy(request));

        Assert.Multiple(() =>
        {
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.Retransmission, Is.True);
            Assert.That(replay.Answer!.ResultCode, Is.EqualTo(first.ResultCode));
            Assert.That(replay.Session!.Version, Is.EqualTo(version));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(callCount));
        });
    }

    [TestCase("update-unknown", DiameterResultCodes.UnknownSessionId)]
    [TestCase("termination-unknown", DiameterResultCodes.UnknownSessionId)]
    [TestCase("initial-after-update", DiameterResultCodes.InvalidAvpValue)]
    [TestCase("update-skipped", DiameterResultCodes.InvalidAvpValue)]
    [TestCase("termination-skipped", DiameterResultCodes.InvalidAvpValue)]
    [TestCase("update-closed", DiameterResultCodes.UnknownSessionId)]
    public void ServerOutOfOrderMatrixRejectsWithoutCallingTheHandler(
        string scenario,
        uint expectedResultCode)
    {
        CreditControlServerMatrixContext context = CreateServerContext();
        DiameterCreditControlRequest request;
        if (scenario == "update-unknown")
        {
            request = CreateRequest(ServerSessionId,
                DiameterCreditControlRequestType.Update);
            request.RequestNumber = 1U;
        }
        else if (scenario == "termination-unknown")
        {
            request = CreateRequest(ServerSessionId,
                DiameterCreditControlRequestType.Termination);
            request.RequestNumber = 1U;
        }
        else
        {
            context.Machine.ProcessCreditControlServerRequest(
                CreateRequest(ServerSessionId,
                    DiameterCreditControlRequestType.Initial));
            if (scenario == "update-closed")
            {
                DiameterCreditControlRequest termination = CreateRequest(
                    ServerSessionId,
                    DiameterCreditControlRequestType.Termination);
                termination.RequestNumber = 1U;
                context.Machine.ProcessCreditControlServerRequest(termination);
                request = CreateRequest(ServerSessionId,
                    DiameterCreditControlRequestType.Update);
                request.RequestNumber = 2U;
            }
            else if (scenario == "initial-after-update")
            {
                DiameterCreditControlRequest update = CreateRequest(
                    ServerSessionId,
                    DiameterCreditControlRequestType.Update);
                update.RequestNumber = 1U;
                context.Machine.ProcessCreditControlServerRequest(update);
                request = CreateRequest(ServerSessionId,
                    DiameterCreditControlRequestType.Initial);
            }
            else
            {
                request = CreateRequest(
                    ServerSessionId,
                    scenario == "update-skipped" ?
                        DiameterCreditControlRequestType.Update :
                        DiameterCreditControlRequestType.Termination);
                request.RequestNumber = 2U;
            }
        }

        int calls = context.Handler.Requests.Count;
        DiameterCreditControlResult result = context.Machine
            .ProcessCreditControlServerRequest(request);
        Assert.Multiple(() =>
        {
            Assert.That(result.ResultCode, Is.EqualTo(expectedResultCode));
            Assert.That(result.TerminateService, Is.True);
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(calls));
        });
    }

    [Test]
    public void InvalidInitialRequestNumberIsRejectedBeforeHandlerDispatch()
    {
        CreditControlServerMatrixContext context = CreateServerContext();
        DiameterCreditControlRequest request = CreateRequest(
            ServerSessionId,
            DiameterCreditControlRequestType.Initial);
        request.RequestNumber = 1U;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Machine.ProcessCreditControlServerRequest(request));
        Assert.Multiple(() =>
        {
            Assert.That(context.Handler.Requests, Is.Empty);
            Assert.That(context.Store.Sessions, Is.Empty);
        });
    }

    [TestCase("null")]
    [TestCase("session")]
    [TestCase("type")]
    [TestCase("number")]
    [TestCase("invalid")]
    [TestCase("throw")]
    public void InvalidServerHandlerOutcomesDoNotCreateSessionState(string mode)
    {
        CreditControlServerMatrixContext context = CreateServerContext();
        context.Handler.Mode = mode;

        if (mode == "throw")
        {
            Assert.Throws<InvalidOperationException>(() =>
                ProcessInitialServerRequest(context));
        }
        else if (mode == "invalid")
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ProcessInitialServerRequest(context));
        }
        else
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                ProcessInitialServerRequest(context));
        }

        Assert.That(context.Store.Sessions, Is.Empty);
    }

    private const string ClientSessionId = "client.example;matrix;1";
    private const string ServerSessionId = "server.example;matrix;1";

    private static CreditControlClientMatrixContext CreateClientContext()
    {
        CreditControlClientMatrixContext result =
            new CreditControlClientMatrixContext();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero),
            0L);
        result.Store = new TestCreditControlStore();
        result.Machine = new CreditControlClientStateMachine(
            result.Store,
            result.Clock);
        return result;
    }

    private static void PrepareClientOperation(
        CreditControlClientMatrixContext context,
        DiameterCreditControlRequestType requestType,
        long identifier)
    {
        if (requestType == DiameterCreditControlRequestType.Initial)
        {
            DiameterCreditControlRequest initial = CreateRequest(
                ClientSessionId,
                requestType);
            initial.FailureHandling = context.FailureHandling;
            initial.AllowFailover = true;
            context.Machine.BeginCreditControlClientSession(initial, identifier);
            return;
        }

        DiameterCreditControlRequest opening = CreateRequest(
            ClientSessionId,
            DiameterCreditControlRequestType.Initial);
        opening.FailureHandling = context.FailureHandling;
        opening.AllowFailover = true;
        context.Machine.BeginCreditControlClientSession(opening, identifier - 1L);
        DiameterCreditControlAnswer opened = CreateAnswer(
            DiameterCreditControlRequestType.Initial,
            0U,
            DiameterResultCodes.Success);
        opened.SessionFailoverPresent = true;
        opened.SessionFailoverSupported = true;
        context.Machine.ProcessCreditControlClientAnswer(identifier - 1L, opened);
        DiameterCreditControlRequest request = CreateRequest(
            ClientSessionId,
            requestType);
        request.FailureHandling = context.FailureHandling;
        request.AllowFailover = true;
        if (requestType == DiameterCreditControlRequestType.Update)
        {
            context.Machine.UpdateCreditControlClientSession(request, identifier);
        }
        else
        {
            context.Machine.TerminateCreditControlClientSession(request, identifier);
        }
    }

    private static CreditControlServerMatrixContext CreateServerContext()
    {
        CreditControlServerMatrixContext result =
            new CreditControlServerMatrixContext();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(2026, 8, 29, 13, 0, 0, TimeSpan.Zero),
            0L);
        result.Store = new TestCreditControlStore();
        result.Handler = new MatrixCreditControlRequestHandler();
        result.Machine = new CreditControlServerStateMachine(
            result.Store,
            result.Handler,
            result.Clock);
        return result;
    }

    private static uint PrepareServerOperation(
        CreditControlServerMatrixContext context,
        DiameterCreditControlRequestType requestType)
    {
        if (requestType == DiameterCreditControlRequestType.Initial ||
            requestType == DiameterCreditControlRequestType.Event)
        {
            return 0U;
        }

        context.Machine.ProcessCreditControlServerRequest(
            CreateRequest(
                ServerSessionId,
                DiameterCreditControlRequestType.Initial));
        return 1U;
    }

    private static void ProcessInitialServerRequest(
        CreditControlServerMatrixContext context)
    {
        context.Machine.ProcessCreditControlServerRequest(
            CreateRequest(
                ServerSessionId,
                DiameterCreditControlRequestType.Initial));
    }

    private static DiameterCreditControlRequest CreateRequest(
        string sessionId,
        DiameterCreditControlRequestType requestType)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = sessionId;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "matrix.service";
        result.UserName = "alice@remote.example";
        result.RequestType = requestType;
        result.TimeoutMilliseconds = 1000;
        result.AllowFailover = true;
        if (requestType == DiameterCreditControlRequestType.Termination)
        {
            result.TerminationCause = DiameterTerminationCause.Logout;
            result.TerminationCausePresent = true;
        }
        else if (requestType == DiameterCreditControlRequestType.Event)
        {
            result.RequestedAction = DiameterRequestedAction.DirectDebiting;
            result.RequestedActionPresent = true;
            result.RequestedServiceUnit = new DiameterServiceUnit();
            result.RequestedServiceUnit.Time = 1U;
            result.RequestedServiceUnit.TimePresent = true;
        }

        return result;
    }

    private static DiameterCreditControlRequest CreateRequestCopy(
        DiameterCreditControlRequest request)
    {
        return CreditControlDataCloner.CloneCreditControlRequest(request);
    }

    private static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequestType requestType,
        uint requestNumber,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = ClientSessionId;
        result.RequestType = requestType;
        result.RequestNumber = requestNumber;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        return result;
    }
}

internal class CreditControlClientMatrixContext
{
    internal TestDiameterClock Clock = null!;
    internal TestCreditControlStore Store = null!;
    internal CreditControlClientStateMachine Machine = null!;
    internal DiameterCreditControlFailureHandling FailureHandling =
        DiameterCreditControlFailureHandling.Terminate;
}

internal class CreditControlServerMatrixContext
{
    internal TestDiameterClock Clock = null!;
    internal TestCreditControlStore Store = null!;
    internal MatrixCreditControlRequestHandler Handler = null!;
    internal CreditControlServerStateMachine Machine = null!;
}

internal class MatrixCreditControlRequestHandler :
    ICreditControlRequestHandler
{
    internal List<DiameterCreditControlRequest> Requests =
        new List<DiameterCreditControlRequest>();
    internal uint ResultCode = DiameterResultCodes.Success;
    internal string Mode = string.Empty;

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("A request is required.");
        }

        Requests.Add(CreditControlDataCloner.CloneCreditControlRequest(request));
        if (Mode == "throw")
        {
            throw new InvalidOperationException("rating failed");
        }

        if (Mode == "null")
        {
            return null!;
        }

        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = Mode == "session" ?
            "wrong.example;matrix;1" : request.SessionId;
        result.RequestType = Mode == "type" ?
            DiameterCreditControlRequestType.Update : request.RequestType;
        result.RequestNumber = Mode == "number" ?
            request.RequestNumber + 1U : request.RequestNumber;
        result.ResultCode = Mode == "invalid" ? 0U : ResultCode;
        result.Disposition = ResultCode >= 2000U && ResultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        if (request.RequestType == DiameterCreditControlRequestType.Event &&
            ResultCode >= 2000U && ResultCode < 3000U)
        {
            result.GrantedServiceUnit = new DiameterServiceUnit();
            result.GrantedServiceUnit.Time = 1U;
            result.GrantedServiceUnit.TimePresent = true;
        }

        return result;
    }
}
