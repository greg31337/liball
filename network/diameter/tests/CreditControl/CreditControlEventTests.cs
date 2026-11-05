using System;

namespace diameter.Tests;

public class CreditControlEventTests
{
    [TestCase(
        DiameterRequestedAction.DirectDebiting,
        "credit-control-direct-debit-completed")]
    [TestCase(
        DiameterRequestedAction.RefundAccount,
        "credit-control-refund-completed")]
    [TestCase(
        DiameterRequestedAction.CheckBalance,
        "credit-control-balance-check-completed")]
    [TestCase(
        DiameterRequestedAction.PriceEnquiry,
        "credit-control-price-enquiry-completed")]
    public void ServerProcessesEveryEventWithoutRetainingSessionState(
        DiameterRequestedAction action,
        string expectedCode)
    {
        TestCreditControlStore store = new TestCreditControlStore();
        TestCreditControlEventRequestHandler handler =
            new TestCreditControlEventRequestHandler();
        CreditControlServerStateMachine machine =
            new CreditControlServerStateMachine(
                store,
                handler,
                CreateClock());
        DiameterCreditControlRequest request = CreateEventRequest(action);

        DiameterCreditControlResult result =
            machine.ProcessCreditControlServerRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Successful, Is.True);
            Assert.That(result.ContinueService, Is.True);
            Assert.That(result.State,
                Is.EqualTo(DiameterCreditControlState.Idle));
            Assert.That(result.Session, Is.Null);
            Assert.That(result.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Request.RequestedAction,
                Is.EqualTo(action));
            Assert.That(result.Answer, Is.Not.Null);
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
            Assert.That(store.Sessions, Is.Empty);
        });

        if (action == DiameterRequestedAction.PriceEnquiry)
        {
            Assert.That(result.Answer!.CostInformation, Is.Not.Null);
        }
        else if (action == DiameterRequestedAction.CheckBalance)
        {
            Assert.That(result.Answer!.CheckBalanceResultPresent, Is.True);
        }
        else
        {
            Assert.That(result.Answer!.GrantedServiceUnit, Is.Not.Null);
        }
    }

    [TestCase(DiameterRequestedAction.DirectDebiting)]
    [TestCase(DiameterRequestedAction.RefundAccount)]
    [TestCase(DiameterRequestedAction.CheckBalance)]
    [TestCase(DiameterRequestedAction.PriceEnquiry)]
    public void EveryRequestedActionRoundTripsInEventRequest(
        DiameterRequestedAction action)
    {
        CreditControlEventCodecFixture fixture = CreateCodecFixture();
        DiameterCreditControlRequest request = CreateEventRequest(action);

        DiameterMessage message = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlRequest parsed = fixture.Parser
            .ParseCreditControlRequest(message);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.RequestType,
                Is.EqualTo(DiameterCreditControlRequestType.Event));
            Assert.That(parsed.RequestNumber, Is.EqualTo(0U));
            Assert.That(parsed.RequestedActionPresent, Is.True);
            Assert.That(parsed.RequestedAction, Is.EqualTo(action));
        });
    }

    [Test]
    public void PriceEnquiryCostRoundTripsInDictionaryValidAnswer()
    {
        CreditControlEventCodecFixture fixture = CreateCodecFixture();
        DiameterCreditControlRequest request = CreateEventRequest(
            DiameterRequestedAction.PriceEnquiry);
        DiameterMessage requestMessage = fixture.Factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer =
            CreateEventAnswer(request);

        DiameterMessage message = fixture.Factory
            .CreateCreditControlAnswer(requestMessage, answer);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlAnswer parsed = fixture.Parser
            .ParseCreditControlAnswer(message, request);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.CostInformation, Is.Not.Null);
            Assert.That(parsed.CostInformation!.ValueDigits,
                Is.EqualTo(125L));
            Assert.That(parsed.CostInformation.Exponent, Is.EqualTo(-2));
            Assert.That(parsed.CostInformation.CurrencyCode,
                Is.EqualTo(840U));
            Assert.That(parsed.CostInformation.CostUnit,
                Is.EqualTo("USD"));
        });
    }

    [TestCase(DiameterRequestedAction.DirectDebiting)]
    [TestCase(DiameterRequestedAction.RefundAccount)]
    [TestCase(DiameterRequestedAction.CheckBalance)]
    [TestCase(DiameterRequestedAction.PriceEnquiry)]
    public void SuccessfulEventAnswerRequiresActionSpecificResult(
        DiameterRequestedAction action)
    {
        TestCreditControlEventRequestHandler handler =
            new TestCreditControlEventRequestHandler();
        handler.IncludeActionResult = false;
        CreditControlServerStateMachine machine =
            new CreditControlServerStateMachine(
                new TestCreditControlStore(),
                handler,
                CreateClock());

        Assert.Throws<DiameterCreditControlException>(() =>
            machine.ProcessCreditControlServerRequest(
                CreateEventRequest(action)));
    }

    [Test]
    public void BalanceAndNotApplicableDebitProduceStatelessClientOutcomes()
    {
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(store, CreateClock());
        DiameterCreditControlRequest balanceRequest = CreateEventRequest(
            DiameterRequestedAction.CheckBalance);
        DiameterCreditControlAnswer balanceAnswer =
            CreateEventAnswer(balanceRequest);
        balanceAnswer.CheckBalanceResult =
            DiameterCheckBalanceResult.NoCredit;
        DiameterCreditControlResult balance = machine
            .ProcessCreditControlClientEvent(
                balanceRequest,
                balanceAnswer);
        DiameterCreditControlRequest debitRequest = CreateEventRequest(
            DiameterRequestedAction.DirectDebiting);
        DiameterCreditControlAnswer debitAnswer =
            CreateEventAnswer(debitRequest);
        debitAnswer.ResultCode =
            DiameterResultCodes.CreditControlNotApplicable;
        debitAnswer.GrantedServiceUnit = null;
        DiameterCreditControlResult debit = machine
            .ProcessCreditControlClientEvent(debitRequest, debitAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(balance.Disposition,
                Is.EqualTo(DiameterCreditControlDisposition.Denied));
            Assert.That(balance.ContinueService, Is.False);
            Assert.That(balance.Session, Is.Null);
            Assert.That(debit.Successful, Is.False);
            Assert.That(debit.ContinueService, Is.True);
            Assert.That(debit.Disposition,
                Is.EqualTo(DiameterCreditControlDisposition.Granted));
            Assert.That(debit.Code,
                Is.EqualTo("credit-control-debit-not-applicable"));
            Assert.That(debit.Session, Is.Null);
            Assert.That(store.Sessions, Is.Empty);
        });
    }

    [Test]
    public void EventWithoutRequestedActionIsRejected()
    {
        CreditControlEventCodecFixture fixture = CreateCodecFixture();
        DiameterCreditControlRequest request = CreateEventRequest(
            DiameterRequestedAction.DirectDebiting);
        request.RequestedActionPresent = false;

        Assert.Throws<ArgumentException>(() =>
            fixture.Factory.CreateCreditControlRequest(request));
    }

    private static DiameterCreditControlRequest CreateEventRequest(
        DiameterRequestedAction action)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;event;" + (int)action;
        result.DestinationRealm.Value = "remote.example";
        result.ServiceContextId = "example.event";
        result.UserName = "alice@remote.example";
        result.RequestType = DiameterCreditControlRequestType.Event;
        result.RequestedAction = action;
        result.RequestedActionPresent = true;
        result.ServiceIdentifiers.Add(51U);
        if (action == DiameterRequestedAction.DirectDebiting ||
            action == DiameterRequestedAction.RefundAccount)
        {
            result.RequestedServiceUnit = CreateTimeUnit(1U);
        }

        return result;
    }

    private static DiameterCreditControlAnswer CreateEventAnswer(
        DiameterCreditControlRequest request)
    {
        TestCreditControlEventRequestHandler handler =
            new TestCreditControlEventRequestHandler();
        return handler.HandleDiameterCreditControlRequest(request);
    }

    private static DiameterServiceUnit CreateTimeUnit(uint time)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = time;
        result.TimePresent = true;
        return result;
    }

    private static DiameterCostInformation CreatePrice()
    {
        DiameterCostInformation result =
            new DiameterCostInformation();
        result.ValueDigits = 125L;
        result.Exponent = -2;
        result.ExponentPresent = true;
        result.CurrencyCode = 840U;
        result.CurrencyCodePresent = true;
        result.CostUnit = "USD";
        return result;
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                21,
                0,
                0,
                TimeSpan.Zero),
            0);
    }

    private static CreditControlEventCodecFixture CreateCodecFixture()
    {
        CreditControlEventCodecFixture result =
            new CreditControlEventCodecFixture();
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        result.Factory = new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm);
        result.Parser = new CreditControlCommandParser(CreateClock());
        return result;
    }

    internal static DiameterCreditControlAnswer
        CreateActionResultAnswer(DiameterCreditControlRequest request)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        if (request.RequestedAction ==
            DiameterRequestedAction.PriceEnquiry)
        {
            result.CostInformation = CreatePrice();
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
}

internal class CreditControlEventCodecFixture
{
    internal CreditControlCommandFactory Factory = null!;
    internal CreditControlCommandParser Parser = null!;
}

internal class TestCreditControlEventRequestHandler :
    ICreditControlRequestHandler
{
    internal bool IncludeActionResult = true;
    internal System.Collections.Generic.List<DiameterCreditControlRequest>
        Requests =
            new System.Collections.Generic.List<DiameterCreditControlRequest>();

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("An event request is required.");
        }

        Requests.Add(
            CreditControlDataCloner.CloneCreditControlRequest(request));
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        if (IncludeActionResult)
        {
            result = CreditControlEventTests
                .CreateActionResultAnswer(request);
        }

        return result;
    }
}
