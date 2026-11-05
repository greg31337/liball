using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlEventMatrixTests
{
    [TestCase(DiameterRequestedAction.DirectDebiting)]
    [TestCase(DiameterRequestedAction.RefundAccount)]
    [TestCase(DiameterRequestedAction.CheckBalance)]
    [TestCase(DiameterRequestedAction.PriceEnquiry)]
    public void DuplicateEventsReplayOwnedAnswersWithoutRepeatingRating(
        DiameterRequestedAction action)
    {
        CreditControlEventMatrixContext context = CreateContext();
        DiameterCreditControlRequest request = CreateRequest(action);

        DiameterCreditControlResult first = context.Server
            .ProcessCreditControlServerRequest(request);
        first.Answer!.UserName = "mutated";
        DiameterCreditControlResult replay = context.Server
            .ProcessCreditControlServerRequest(
                CreditControlDataCloner.CloneCreditControlRequest(request));

        Assert.Multiple(() =>
        {
            Assert.That(first.Duplicate, Is.False);
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.Retransmission, Is.True);
            Assert.That(replay.Code,
                Is.EqualTo("credit-control-event-answer-replayed"));
            Assert.That(replay.Answer!.UserName, Is.EqualTo("rated-event"));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
            Assert.That(context.Server.EventReplays, Has.Count.EqualTo(1));
            Assert.That(context.Store.Sessions, Is.Empty);
            Assert.That(replay.Session, Is.Null);
        });
    }

    [Test]
    public void EventSessionIdCollisionIsRejectedWithoutRepeatingRating()
    {
        CreditControlEventMatrixContext context = CreateContext();
        DiameterCreditControlRequest debit = CreateRequest(
            DiameterRequestedAction.DirectDebiting);
        context.Server.ProcessCreditControlServerRequest(debit);
        DiameterCreditControlRequest collision = CreateRequest(
            DiameterRequestedAction.PriceEnquiry);
        collision.SessionId = debit.SessionId;

        DiameterCreditControlResult result = context.Server
            .ProcessCreditControlServerRequest(collision);

        Assert.Multiple(() =>
        {
            Assert.That(result.Duplicate, Is.True);
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(result.Code,
                Is.EqualTo("credit-control-event-key-collision"));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
            Assert.That(context.Store.Sessions, Is.Empty);
        });
    }

    [Test]
    public void EventReplayExpiresAtTheRequestTimeoutWithoutSessionState()
    {
        CreditControlEventMatrixContext context = CreateContext();
        DiameterCreditControlRequest request = CreateRequest(
            DiameterRequestedAction.RefundAccount);
        request.TimeoutMilliseconds = 1000;
        context.Server.ProcessCreditControlServerRequest(request);
        context.Clock.UtcTime = context.Clock.UtcTime.AddMilliseconds(999);
        DiameterCreditControlResult beforeExpiry = context.Server
            .ProcessCreditControlServerRequest(request);
        context.Clock.UtcTime = context.Clock.UtcTime.AddMilliseconds(1);
        DiameterCreditControlResult afterExpiry = context.Server
            .ProcessCreditControlServerRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(beforeExpiry.Duplicate, Is.True);
            Assert.That(afterExpiry.Duplicate, Is.False);
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(2));
            Assert.That(context.Server.EventReplays, Has.Count.EqualTo(1));
            Assert.That(context.Store.Sessions, Is.Empty);
        });
    }

    [TestCase(DiameterResultCodes.CreditLimitReached)]
    [TestCase(DiameterResultCodes.TooBusy)]
    [TestCase(DiameterResultCodes.UserUnknown)]
    public void FailedEventDecisionsAreAlsoDuplicateProtected(uint resultCode)
    {
        CreditControlEventMatrixContext context = CreateContext();
        context.Handler.ResultCode = resultCode;
        DiameterCreditControlRequest request = CreateRequest(
            DiameterRequestedAction.DirectDebiting);

        DiameterCreditControlResult first = context.Server
            .ProcessCreditControlServerRequest(request);
        DiameterCreditControlResult replay = context.Server
            .ProcessCreditControlServerRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Successful, Is.False);
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.ResultCode, Is.EqualTo(resultCode));
            Assert.That(context.Handler.Requests, Has.Count.EqualTo(1));
            Assert.That(context.Store.Sessions, Is.Empty);
        });
    }

    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        DiameterCheckBalanceResult.EnoughCredit,
        DiameterCreditControlDisposition.Granted,
        true)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterCheckBalanceResult.EnoughCredit,
        DiameterCreditControlDisposition.Granted,
        true)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterCheckBalanceResult.NoCredit,
        DiameterCreditControlDisposition.Denied,
        false)]
    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterCheckBalanceResult.EnoughCredit,
        DiameterCreditControlDisposition.Granted,
        true)]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        DiameterCheckBalanceResult.EnoughCredit,
        DiameterCreditControlDisposition.Granted,
        true)]
    public void SuccessfulClientEventOutcomeMatrixIsStateless(
        DiameterRequestedAction action,
        DiameterCheckBalanceResult balance,
        DiameterCreditControlDisposition disposition,
        bool continueService)
    {
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine client =
            new CreditControlClientStateMachine(store, CreateClock());
        DiameterCreditControlRequest request = CreateRequest(action);
        DiameterCreditControlAnswer answer = CreateAnswer(
            request,
            DiameterResultCodes.Success);
        if (action == DiameterRequestedAction.CheckBalance)
        {
            answer.CheckBalanceResult = balance;
        }

        DiameterCreditControlResult result = client
            .ProcessCreditControlClientEvent(request, answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True);
            Assert.That(result.Disposition, Is.EqualTo(disposition));
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService, Is.False);
            Assert.That(result.Session, Is.Null);
            Assert.That(store.Sessions, Is.Empty);
        });
    }

    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterResultCodes.TooBusy,
        true,
        true,
        false)]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        DiameterResultCodes.TooBusy,
        true,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterResultCodes.TooBusy,
        true,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        DiameterResultCodes.TooBusy,
        true,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterResultCodes.CreditLimitReached,
        false,
        true,
        false)]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        DiameterResultCodes.CreditLimitReached,
        false,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterResultCodes.CreditLimitReached,
        false,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        DiameterResultCodes.CreditLimitReached,
        false,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterResultCodes.EndUserServiceDenied,
        false,
        false,
        true)]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        DiameterResultCodes.EndUserServiceDenied,
        false,
        false,
        true)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterResultCodes.EndUserServiceDenied,
        false,
        false,
        true)]
    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        DiameterResultCodes.EndUserServiceDenied,
        false,
        false,
        true)]
    public void ClientEventFailureMatrixAppliesActionSpecificPolicy(
        DiameterRequestedAction action,
        uint resultCode,
        bool retry,
        bool continueService,
        bool terminateService)
    {
        CreditControlClientStateMachine client =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateRequest(action);
        request.DirectDebitFailureHandling =
            DiameterDirectDebitFailureHandling.Continue;
        DiameterCreditControlAnswer answer = CreateAnswer(
            request,
            resultCode);

        DiameterCreditControlResult result = client
            .ProcessCreditControlClientEvent(request, answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.RetryRequired, Is.EqualTo(retry));
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(result.Session, Is.Null);
        });
    }

    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        true,
        true)]
    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        DiameterDirectDebitFailureHandling.Continue,
        true,
        false)]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        false,
        true)]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        false,
        false)]
    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        false,
        false)]
    public void DeliveryFailureMatrixAppliesBufferAndContinuationRules(
        DiameterRequestedAction action,
        DiameterDirectDebitFailureHandling handling,
        bool continueService,
        bool buffer)
    {
        CreditControlClientStateMachine client =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateRequest(action);
        request.AllowFailover = false;
        request.DirectDebitFailureHandling = handling;

        DiameterCreditControlResult result = client
            .ProcessCreditControlClientEventFailure(request, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.BufferRequest, Is.EqualTo(buffer));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.State,
                Is.EqualTo(DiameterCreditControlState.Idle));
            Assert.That(result.Session, Is.Null);
        });
    }

    [Test]
    public void UnsupportedRequestedActionIsRejectedWithoutRatingOrState()
    {
        CreditControlEventMatrixContext context = CreateContext();
        DiameterCreditControlRequest request = CreateRequest(
            DiameterRequestedAction.DirectDebiting);
        request.RequestedAction = (DiameterRequestedAction)99;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Server.ProcessCreditControlServerRequest(request));
        Assert.Multiple(() =>
        {
            Assert.That(context.Handler.Requests, Is.Empty);
            Assert.That(context.Server.EventReplays, Is.Empty);
            Assert.That(context.Store.Sessions, Is.Empty);
        });
    }

    private static CreditControlEventMatrixContext CreateContext()
    {
        CreditControlEventMatrixContext result =
            new CreditControlEventMatrixContext();
        result.Clock = CreateClock();
        result.Store = new TestCreditControlStore();
        result.Handler = new MatrixCreditControlEventHandler();
        result.Server = new CreditControlServerStateMachine(
            result.Store,
            result.Handler,
            result.Clock);
        return result;
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(2026, 8, 29, 22, 0, 0, TimeSpan.Zero),
            0L);
    }

    private static DiameterCreditControlRequest CreateRequest(
        DiameterRequestedAction action)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;event-matrix;" + (int)action;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "event.matrix";
        result.UserName = "alice@remote.example";
        result.RequestType = DiameterCreditControlRequestType.Event;
        result.RequestedAction = action;
        result.RequestedActionPresent = true;
        result.EventTimestamp = CreateClock().UtcTime;
        result.EventTimestampPresent = true;
        result.ServiceIdentifiers.Add(51U);
        if (action == DiameterRequestedAction.DirectDebiting ||
            action == DiameterRequestedAction.RefundAccount)
        {
            result.RequestedServiceUnit = CreateTimeUnit(1U);
        }

        return result;
    }

    private static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequest request,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = DiameterCreditControlRequestType.Event;
        result.RequestNumber = 0U;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        if (resultCode < 2000U || resultCode >= 3000U)
        {
            return result;
        }

        if (request.RequestedAction == DiameterRequestedAction.PriceEnquiry)
        {
            result.CostInformation = new DiameterCostInformation();
            result.CostInformation.ValueDigits = 125L;
            result.CostInformation.Exponent = -2;
            result.CostInformation.ExponentPresent = true;
            result.CostInformation.CurrencyCode = 840U;
            result.CostInformation.CurrencyCodePresent = true;
        }
        else if (request.RequestedAction ==
            DiameterRequestedAction.CheckBalance)
        {
            result.CheckBalanceResult =
                DiameterCheckBalanceResult.EnoughCredit;
            result.CheckBalanceResultPresent = true;
        }
        else
        {
            result.GrantedServiceUnit = CreateTimeUnit(1U);
        }

        return result;
    }

    private static DiameterServiceUnit CreateTimeUnit(uint value)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = value;
        result.TimePresent = true;
        return result;
    }
}

internal class CreditControlEventMatrixContext
{
    internal TestDiameterClock Clock = null!;
    internal TestCreditControlStore Store = null!;
    internal MatrixCreditControlEventHandler Handler = null!;
    internal CreditControlServerStateMachine Server = null!;
}

internal class MatrixCreditControlEventHandler :
    ICreditControlRequestHandler
{
    internal List<DiameterCreditControlRequest> Requests =
        new List<DiameterCreditControlRequest>();
    internal uint ResultCode = DiameterResultCodes.Success;

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("An event request is required.");
        }

        Requests.Add(CreditControlDataCloner.CloneCreditControlRequest(request));
        DiameterCreditControlAnswer result =
            CreditControlEventMatrixTestsAccess.CreateAnswer(
                request,
                ResultCode);
        result.UserName = "rated-event";
        return result;
    }
}

internal static class CreditControlEventMatrixTestsAccess
{
    internal static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequest request,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = DiameterCreditControlRequestType.Event;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        if (resultCode < 2000U || resultCode >= 3000U)
        {
            return result;
        }

        if (request.RequestedAction == DiameterRequestedAction.PriceEnquiry)
        {
            result.CostInformation = new DiameterCostInformation();
            result.CostInformation.ValueDigits = 125L;
            result.CostInformation.Exponent = -2;
            result.CostInformation.ExponentPresent = true;
            result.CostInformation.CurrencyCode = 840U;
            result.CostInformation.CurrencyCodePresent = true;
        }
        else if (request.RequestedAction ==
            DiameterRequestedAction.CheckBalance)
        {
            result.CheckBalanceResult =
                DiameterCheckBalanceResult.EnoughCredit;
            result.CheckBalanceResultPresent = true;
        }
        else
        {
            result.GrantedServiceUnit = new DiameterServiceUnit();
            result.GrantedServiceUnit.Time = 1U;
            result.GrantedServiceUnit.TimePresent = true;
        }

        return result;
    }
}
