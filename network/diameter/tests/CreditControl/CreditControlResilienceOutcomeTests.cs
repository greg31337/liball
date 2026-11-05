using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class CreditControlResilienceOutcomeTests
{
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Terminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.PendingInitial,
        false,
        true,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.PendingUpdate,
        false,
        true,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.Continue,
        DiameterCreditControlState.Failed,
        false,
        false,
        true,
        false)]
    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingInitial,
        true,
        true,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.PendingUpdate,
        true,
        true,
        false,
        true)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlFailureHandling.RetryAndTerminate,
        DiameterCreditControlState.Failed,
        false,
        false,
        true,
        false)]
    public void PeerLossAppliesEveryCcfhToEverySessionOperation(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlFailureHandling handling,
        DiameterCreditControlState state,
        bool retry,
        bool continueService,
        bool terminateService,
        bool retained)
    {
        CreditControlResilienceContext context = CreateContext();
        long identifier = PreparePendingOperation(
            context,
            requestType,
            handling);

        DiameterCreditControlResult result = context.Machine
            .ProcessCreditControlClientDeliveryFailure(identifier, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Delivered, Is.False);
            Assert.That(result.AnswerReceived, Is.False);
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.RetryRequired, Is.EqualTo(retry));
            Assert.That(result.FailoverAttempted, Is.EqualTo(retry));
            Assert.That(result.Retransmission, Is.EqualTo(retry));
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(context.Store.LoadDiameterCreditControlSession(
                SessionId) != null, Is.EqualTo(retained));
        });
    }

    [TestCase(
        DiameterDirectDebitFailureHandling.Continue,
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        DiameterResultCodes.CreditLimitReached,
        false,
        false,
        false,
        true)]
    [TestCase(
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        DiameterDirectDebitFailureHandling.Continue,
        DiameterResultCodes.CreditLimitReached,
        false,
        false,
        true,
        false)]
    [TestCase(
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        DiameterDirectDebitFailureHandling.Continue,
        DiameterResultCodes.TooBusy,
        true,
        true,
        true,
        false)]
    [TestCase(
        DiameterDirectDebitFailureHandling.TerminateOrBuffer,
        DiameterDirectDebitFailureHandling.Continue,
        DiameterResultCodes.EndUserServiceDenied,
        false,
        false,
        false,
        true)]
    public void AnswerDdfhOverridesLocalPolicyForEveryFailureOutcome(
        DiameterDirectDebitFailureHandling localHandling,
        DiameterDirectDebitFailureHandling answerHandling,
        uint resultCode,
        bool allowFailover,
        bool retry,
        bool continueService,
        bool terminateService)
    {
        CreditControlClientStateMachine machine =
            new CreditControlClientStateMachine(
                new TestCreditControlStore(),
                CreateClock());
        DiameterCreditControlRequest request = CreateEventRequest();
        request.AllowFailover = allowFailover;
        request.DirectDebitFailureHandling = localHandling;
        DiameterCreditControlAnswer answer = CreateAnswer(
            request,
            resultCode);
        answer.DirectDebitFailureHandling = answerHandling;
        answer.DirectDebitFailureHandlingPresent = true;

        DiameterCreditControlResult result = machine
            .ProcessCreditControlClientEvent(request, answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.RetryRequired, Is.EqualTo(retry));
            Assert.That(result.ContinueService,
                Is.EqualTo(continueService));
            Assert.That(result.TerminateService,
                Is.EqualTo(terminateService));
            Assert.That(result.BufferRequest, Is.False);
            Assert.That(result.Session, Is.Null);
        });
    }

    [Test]
    public void FailoverExhaustsRoutesThenFailbackRestoresOwnedPrimaryAffinity()
    {
        CreditControlResilienceContext context = CreateContext();
        DiameterCreditControlRequest initial = CreateSessionRequest(
            DiameterCreditControlRequestType.Initial,
            DiameterCreditControlFailureHandling.RetryAndTerminate);
        AddAlternate(initial, "secondary-one.example", "route-two");
        AddAlternate(initial, "secondary-two.example", "route-three");
        context.Machine.BeginCreditControlClientSession(initial, 201L);
        DiameterCreditControlAnswer opened = CreateAnswer(
            initial,
            DiameterResultCodes.Success);
        opened.SessionFailoverSupported = true;
        opened.SessionFailoverPresent = true;
        context.Machine.ProcessCreditControlClientAnswer(201L, opened);
        initial.DestinationHost!.Value = "mutated.example";
        initial.AlternateServers[0].RouteIdentifier = "mutated-route";

        DiameterCreditControlResult pending = context.Machine
            .UpdateCreditControlClientSession(
                CreateSessionRequest(
                    DiameterCreditControlRequestType.Update,
                    DiameterCreditControlFailureHandling.RetryAndTerminate),
                202L);
        DiameterCreditControlResult first = context.Machine
            .ProcessCreditControlClientAnswer(
                202L,
                CreateAnswer(
                    pending.Request,
                    DiameterResultCodes.TooBusy));
        DiameterCreditControlResult second = context.Machine
            .ProcessCreditControlClientAnswer(
                202L,
                CreateAnswer(
                    pending.Request,
                    DiameterResultCodes.TooBusy));
        DiameterCreditControlResult recovered = context.Machine
            .ProcessCreditControlClientAnswer(
                202L,
                CreateAnswer(
                    pending.Request,
                    DiameterResultCodes.Success));
        DiameterCreditControlResult sticky = context.Machine
            .UpdateCreditControlClientSession(
                CreateSessionRequest(
                    DiameterCreditControlRequestType.Update,
                    DiameterCreditControlFailureHandling.RetryAndTerminate),
                203L);
        context.Machine.ProcessCreditControlClientAnswer(
            203L,
            CreateAnswer(sticky.Request, DiameterResultCodes.Success));
        DiameterCreditControlSession restored = context.Machine
            .FailbackCreditControlClientSession(SessionId);
        DiameterCreditControlResult primary = context.Machine
            .UpdateCreditControlClientSession(
                CreateSessionRequest(
                    DiameterCreditControlRequestType.Update,
                    DiameterCreditControlFailureHandling.RetryAndTerminate),
                204L);

        Assert.Multiple(() =>
        {
            Assert.That(first.FailoverAttempted, Is.True);
            Assert.That(first.Request.RouteIdentifier,
                Is.EqualTo("route-two"));
            Assert.That(first.Session!.AttemptCount, Is.EqualTo(2));
            Assert.That(second.FailoverAttempted, Is.True);
            Assert.That(second.Request.RouteIdentifier,
                Is.EqualTo("route-three"));
            Assert.That(second.Session!.AttemptCount, Is.EqualTo(3));
            Assert.That(recovered.Successful, Is.True);
            Assert.That(recovered.Session!.FailoverActive, Is.True);
            Assert.That(sticky.Request.RouteIdentifier,
                Is.EqualTo("route-three"));
            Assert.That(restored.FailoverActive, Is.False);
            Assert.That(restored.RouteIdentifier,
                Is.EqualTo("route-one"));
            Assert.That(primary.Request.RouteIdentifier,
                Is.EqualTo("route-one"));
            Assert.That(primary.Request.DestinationHost!.Value,
                Is.EqualTo("primary.example"));
        });
    }

    [TestCase(
        DiameterCreditControlRequestType.Initial,
        DiameterCreditControlState.PendingInitial,
        0U,
        "route-two",
        true,
        2)]
    [TestCase(
        DiameterCreditControlRequestType.Update,
        DiameterCreditControlState.PendingUpdate,
        1U,
        "route-two",
        true,
        2)]
    [TestCase(
        DiameterCreditControlRequestType.Termination,
        DiameterCreditControlState.PendingTermination,
        1U,
        "route-one",
        false,
        1)]
    public void PersistedRecoveryPreservesIdentityAndHonorsFailoverEligibility(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlState state,
        uint requestNumber,
        string route,
        bool failover,
        int attemptCount)
    {
        CreditControlResilienceContext context = CreateContext();
        long identifier = PreparePendingOperation(
            context,
            requestType,
            DiameterCreditControlFailureHandling.RetryAndTerminate);
        CreditControlClientStateMachine restarted =
            new CreditControlClientStateMachine(
                context.Store,
                context.Clock);

        List<DiameterCreditControlResult> recovered = restarted
            .RecoverCreditControlClientTransactions();
        DiameterCreditControlResult result = recovered[0];

        Assert.Multiple(() =>
        {
            Assert.That(recovered, Has.Count.EqualTo(1));
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Retransmission, Is.True);
            Assert.That(result.FailoverAttempted, Is.EqualTo(failover));
            Assert.That(result.Request.RequestNumber,
                Is.EqualTo(requestNumber));
            Assert.That(result.Request.RouteIdentifier,
                Is.EqualTo(route));
            Assert.That(result.Request.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(result.Request.ApplicationState,
                Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(result.Session!.PendingRequestIdentifier,
                Is.EqualTo(identifier));
            Assert.That(result.Session.AttemptCount,
                Is.EqualTo(attemptCount));
            Assert.That(result.Session.LastRequest!.RouteIdentifier,
                Is.EqualTo(route));
        });
    }

    [TestCase(
        DiameterCreditControlPrivacyAction.Preserve,
        DiameterAgentRole.None)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Preserve,
        DiameterAgentRole.Relay)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Preserve,
        DiameterAgentRole.Proxy)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Preserve,
        DiameterAgentRole.Redirect)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Preserve,
        DiameterAgentRole.Translation)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Remove,
        DiameterAgentRole.None)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Remove,
        DiameterAgentRole.Relay)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Remove,
        DiameterAgentRole.Proxy)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Remove,
        DiameterAgentRole.Redirect)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Remove,
        DiameterAgentRole.Translation)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Redact,
        DiameterAgentRole.None)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Redact,
        DiameterAgentRole.Relay)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Redact,
        DiameterAgentRole.Proxy)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Redact,
        DiameterAgentRole.Redirect)]
    [TestCase(
        DiameterCreditControlPrivacyAction.Redact,
        DiameterAgentRole.Translation)]
    public void PrivacyActionMatrixPreservesRequiredRoutingAndCorrelation(
        DiameterCreditControlPrivacyAction action,
        DiameterAgentRole role)
    {
        DiameterMessage source = CreatePrivacyMessage();
        CreditControlResiliencePrivacyPolicy policy =
            new CreditControlResiliencePrivacyPolicy();
        policy.Action = action;

        DiameterMessage result =
            new DiameterCreditControlPrivacyProcessor()
                .ApplyDiameterCreditControlPrivacyPolicy(
                    source,
                    policy,
                    role);
        bool routing = role == DiameterAgentRole.Relay ||
            role == DiameterAgentRole.Proxy;
        bool remove = action ==
            DiameterCreditControlPrivacyAction.Remove;
        bool redact = action ==
            DiameterCreditControlPrivacyAction.Redact;

        Assert.Multiple(() =>
        {
            AssertPrivacyAvp(
                result,
                DiameterAvpCodes.SessionId,
                true,
                false);
            AssertPrivacyAvp(
                result,
                DiameterAvpCodes.UserName,
                !remove || routing,
                redact && !routing);
            AssertPrivacyAvp(
                result,
                DiameterAvpCodes.UserEquipmentInfoValue,
                !remove,
                redact);
            AssertPrivacyAvp(
                result,
                DiameterAvpCodes.ServiceContextId,
                !remove || routing,
                redact && !routing);
            AssertPrivacyAvp(
                result,
                DiameterAvpCodes.DestinationRealm,
                true,
                false);
            Assert.That(policy.Observed, Has.Count.EqualTo(4));
            Assert.That(FindObservation(
                policy,
                DiameterAvpCodes.SessionId).Classification,
                Is.EqualTo(DiameterCreditControlPrivacyClass.Correlation));
            Assert.That(FindObservation(
                policy,
                DiameterAvpCodes.UserName).Classification,
                Is.EqualTo(
                    DiameterCreditControlPrivacyClass.SubscriberIdentity));
            Assert.That(FindObservation(
                policy,
                DiameterAvpCodes.UserEquipmentInfoValue).Classification,
                Is.EqualTo(
                    DiameterCreditControlPrivacyClass.EquipmentIdentity));
            Assert.That(FindObservation(
                policy,
                DiameterAvpCodes.ServiceContextId).Classification,
                Is.EqualTo(DiameterCreditControlPrivacyClass.ServiceData));
        });

        FindAvp(result, DiameterAvpCodes.DestinationRealm)!.Data[0] = 0;
        Assert.That(
            FindAvp(source, DiameterAvpCodes.DestinationRealm)!.Data[0],
            Is.Not.EqualTo(0));
    }

    private static long PreparePendingOperation(
        CreditControlResilienceContext context,
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlFailureHandling handling)
    {
        DiameterCreditControlRequest initial = CreateSessionRequest(
            DiameterCreditControlRequestType.Initial,
            handling);
        AddAlternate(initial, "secondary.example", "route-two");
        if (requestType == DiameterCreditControlRequestType.Initial)
        {
            context.Machine.BeginCreditControlClientSession(initial, 101L);
            return 101L;
        }

        context.Machine.BeginCreditControlClientSession(initial, 100L);
        DiameterCreditControlAnswer answer = CreateAnswer(
            initial,
            DiameterResultCodes.Success);
        answer.SessionFailoverSupported = true;
        answer.SessionFailoverPresent = true;
        context.Machine.ProcessCreditControlClientAnswer(100L, answer);
        DiameterCreditControlRequest operation = CreateSessionRequest(
            requestType,
            handling);
        if (requestType == DiameterCreditControlRequestType.Update)
        {
            context.Machine.UpdateCreditControlClientSession(
                operation,
                101L);
        }
        else
        {
            context.Machine.TerminateCreditControlClientSession(
                operation,
                101L);
        }

        return 101L;
    }

    private static CreditControlResilienceContext CreateContext()
    {
        CreditControlResilienceContext result =
            new CreditControlResilienceContext();
        result.Clock = CreateClock();
        result.Store = new TestCreditControlStore();
        result.Machine = new CreditControlClientStateMachine(
            result.Store,
            result.Clock);
        return result;
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                30,
                1,
                0,
                0,
                TimeSpan.Zero),
            0L);
    }

    private static DiameterCreditControlRequest CreateSessionRequest(
        DiameterCreditControlRequestType requestType,
        DiameterCreditControlFailureHandling handling)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = SessionId;
        result.DestinationRealm.Value = "credit.example";
        result.DestinationHost = CreateIdentity("primary.example");
        result.PeerIdentity = CreateIdentity("primary-peer.example");
        result.ServiceContextId = "resilience.service";
        result.UserName = "alice@credit.example";
        result.RequestType = requestType;
        result.AllowFailover = true;
        result.FailureHandling = handling;
        result.RouteIdentifier = "route-one";
        result.CorrelationId = new byte[] { 4, 5, 6 };
        result.ApplicationState = new byte[] { 7, 8, 9 };
        return result;
    }

    private static DiameterCreditControlRequest CreateEventRequest()
    {
        DiameterCreditControlRequest result = CreateSessionRequest(
            DiameterCreditControlRequestType.Event,
            DiameterCreditControlFailureHandling.Terminate);
        result.SessionId = "resilience.example;event;1";
        result.RequestedAction = DiameterRequestedAction.DirectDebiting;
        result.RequestedActionPresent = true;
        result.RequestedServiceUnit = new DiameterServiceUnit();
        result.RequestedServiceUnit.Time = 1U;
        result.RequestedServiceUnit.TimePresent = true;
        return result;
    }

    private static DiameterCreditControlAnswer CreateAnswer(
        DiameterCreditControlRequest request,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = resultCode;
        result.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        if (request.RequestType ==
                DiameterCreditControlRequestType.Event &&
            resultCode >= 2000U && resultCode < 3000U)
        {
            result.GrantedServiceUnit = new DiameterServiceUnit();
            result.GrantedServiceUnit.Time = 1U;
            result.GrantedServiceUnit.TimePresent = true;
        }

        return result;
    }

    private static void AddAlternate(
        DiameterCreditControlRequest request,
        string host,
        string route)
    {
        DiameterCreditControlServer server =
            new DiameterCreditControlServer();
        server.DestinationHost = CreateIdentity(host);
        server.PeerIdentity = CreateIdentity(host + "-peer");
        server.RouteIdentifier = route;
        request.AlternateServers.Add(server);
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterMessage CreatePrivacyMessage()
    {
        DiameterMessage result = new DiameterMessage();
        result.ApplicationId = DiameterApplicationIds.CreditControl;
        result.CommandCode = DiameterCommandCodes.CreditControl;
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            "resilience.example;privacy;1"));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            "alice@credit.example"));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserEquipmentInfoValue,
            "equipment-123"));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.ServiceContextId,
            "resilience.service"));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            "credit.example"));
        return result;
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static void AssertPrivacyAvp(
        DiameterMessage message,
        uint code,
        bool present,
        bool redacted)
    {
        DiameterAvp? avp = FindAvp(message, code);
        Assert.That(avp != null, Is.EqualTo(present), code.ToString());
        if (avp == null || !present)
        {
            return;
        }

        DiameterAvp original = FindAvp(CreatePrivacyMessage(), code)!;
        if (redacted)
        {
            Assert.That(avp.Data, Is.Not.EqualTo(original.Data),
                code.ToString());
        }
        else
        {
            Assert.That(avp.Data, Is.EqualTo(original.Data),
                code.ToString());
        }
    }

    private static DiameterAvp? FindAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        return null;
    }

    private static DiameterCreditControlAvpPrivacy FindObservation(
        CreditControlResiliencePrivacyPolicy policy,
        uint code)
    {
        int index = 0;
        while (index < policy.Observed.Count)
        {
            if (policy.Observed[index].AvpCode == code)
            {
                return policy.Observed[index];
            }

            index = index + 1;
        }

        throw new AssertionException(
            "Expected privacy observation was not present.");
    }

    private const string SessionId = "resilience.example;session;1";
}

internal class CreditControlResilienceContext
{
    internal TestDiameterClock Clock = null!;
    internal TestCreditControlStore Store = null!;
    internal CreditControlClientStateMachine Machine = null!;
}

internal class CreditControlResiliencePrivacyPolicy :
    ICreditControlPrivacyPolicy
{
    internal DiameterCreditControlPrivacyAction Action;
    internal List<DiameterCreditControlAvpPrivacy> Observed =
        new List<DiameterCreditControlAvpPrivacy>();

    public DiameterCreditControlPrivacyAction
        SelectDiameterCreditControlPrivacyAction(
            DiameterCreditControlAvpPrivacy privacy)
    {
        Observed.Add(privacy);
        return Action;
    }
}
