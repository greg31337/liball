using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class DiameterCreditControlApplicationTests
{
    [Test]
    public void RegistrationAdvertisesAndUpdatesCreditControlCapability()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterApplicationHost host = new DiameterApplicationHost(
            dictionary,
            new DiameterValidator(),
            context.Clock,
            new DiameterLimitConfiguration(),
            CreateApplicationIdentity("credit.example"),
            CreateApplicationIdentity("example"));

        DiameterApplicationRegistration registration =
            host.RegisterDiameterApplication(context.Application);
        DiameterPeerCapabilities active =
            host.Registry.ReadApplicationCapabilities();
        long registeredRevision =
            host.Registry.ReadApplicationCapabilityRevision();
        registration.Name = "mutated";
        DiameterApplicationRegistration owned = context.Application
            .ReadDiameterApplicationRegistration();
        IDiameterCreditControlApplication application =
            context.Application;
        DiameterApplicationId capability = application
            .ReadDiameterCreditControlCapability();
        bool removed = host.UnregisterDiameterApplication(
            "diameter-credit-control",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(active.AuthApplicationIds,
                Is.EqualTo(new uint[]
                {
                    DiameterApplicationIds.CreditControl
                }));
            Assert.That(active.AcctApplicationIds, Is.Empty);
            Assert.That(registeredRevision, Is.EqualTo(1));
            Assert.That(host.Registry.ReadApplicationCapabilities()
                .AuthApplicationIds, Is.Empty);
            Assert.That(host.Registry.ReadApplicationCapabilityRevision(),
                Is.EqualTo(2));
            Assert.That(owned.Name,
                Is.EqualTo("diameter-credit-control"));
            Assert.That(owned.Commands[0].CommandCode,
                Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(capability.Id,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(capability.VendorId,
                Is.EqualTo(DiameterVendorIds.Ietf));
            Assert.That(capability.IsAccounting, Is.False);
            Assert.That(removed, Is.True);
        });
    }

    [Test]
    public void ClientSessionSendsCcrAndCorrelatesCcaIntoOwnedOutcome()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-client.example;session;1",
            DiameterCreditControlRequestType.Initial);

        DiameterCreditControlResult sent = context.Application
            .BeginDiameterCreditControlSession(request);
        DiameterCreditControlAnswer answer = CreateApplicationAnswer(
            sent.Request,
            DiameterResultCodes.Success);
        context.RequestClient.CompleteCreditControlRequest(
            0,
            context.Application.CommandFactory
                .CreateCreditControlAnswer(
                    context.RequestClient.Requests[0].Request,
                    answer),
            DiameterResultCodes.Success);
        DiameterCreditControlSession session = context.Application
            .FindDiameterCreditControlSession(request.SessionId)!;
        DiameterCreditControlResult completed =
            context.Events.Results[0];
        request.CorrelationId[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(sent.Delivered, Is.True);
            Assert.That(context.RequestClient.Requests,
                Has.Count.EqualTo(1));
            Assert.That(context.RequestClient.Requests[0].Request.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(context.RequestClient.Requests[0].Request.ApplicationId,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(context.RequestClient.Handlers[0],
                Is.SameAs(context.Application));
            Assert.That(session.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(session.PendingRequestPresent, Is.False);
            Assert.That(session.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(completed.Successful, Is.True);
            Assert.That(completed.AnswerReceived, Is.True);
            Assert.That(completed.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
        });
    }

    [Test]
    public void ApplicationProcessesInboundCcrAndReturnsEncodedCca()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-server.example;session;1",
            DiameterCreditControlRequestType.Initial);
        DiameterMessage message = context.Application.CommandFactory
            .CreateCreditControlRequest(request);

        DiameterCreditControlResult result = context.Application
            .ProcessDiameterCreditControlRequest(
                message,
                CreateApplicationIdentity("peer.remote.example"));
        DiameterCreditControlAnswer parsed = context.Application
            .CommandParser.ParseCreditControlAnswer(
                result.Answer!.Message,
                request);
        message.Avps[0].Data[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True);
            Assert.That(result.Answer, Is.Not.Null);
            Assert.That(result.Answer!.Message, Is.Not.Null);
            Assert.That(result.Answer.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(parsed.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(context.Handler.Requests,
                Has.Count.EqualTo(1));
            Assert.That(context.Handler.Requests[0].PeerIdentity!.Value,
                Is.EqualTo("peer.remote.example"));
            Assert.That(context.Store.ReadStoredDiameterCreditControlSessions(),
                Has.Count.EqualTo(1));
            Assert.That(context.Store.ReadStoredDiameterCreditControlSessions()[0]
                .Role,
                Is.EqualTo(DiameterSessionRole.Server));
            Assert.That(context.Application.FindDiameterCreditControlSession(
                request.SessionId), Is.Null);
            Assert.That(context.Events.Results,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void StatelessEventCompletesWithoutRetainingSession()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-client.example;event;1",
            DiameterCreditControlRequestType.Event);
        request.RequestedActionPresent = true;
        request.RequestedAction = DiameterRequestedAction.CheckBalance;

        DiameterCreditControlResult sent = context.Application
            .ExecuteDiameterCreditControlEvent(request);
        DiameterCreditControlAnswer answer = CreateApplicationAnswer(
            sent.Request,
            DiameterResultCodes.Success);
        answer.CheckBalanceResultPresent = true;
        answer.CheckBalanceResult =
            DiameterCheckBalanceResult.EnoughCredit;
        context.RequestClient.CompleteCreditControlRequest(
            0,
            context.Application.CommandFactory
                .CreateCreditControlAnswer(
                    context.RequestClient.Requests[0].Request,
                    answer),
            DiameterResultCodes.Success);
        DiameterCreditControlResult completed =
            context.Events.Results[0];

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(completed.Successful, Is.True);
            Assert.That(completed.State,
                Is.EqualTo(DiameterCreditControlState.Idle));
            Assert.That(completed.Code,
                Is.EqualTo("credit-control-balance-check-completed"));
            Assert.That(context.Store
                .ReadStoredDiameterCreditControlSessions(), Is.Empty);
        });
    }

    [Test]
    public void RejectedSendCleansUpPendingInitialSession()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        context.RequestClient.Accept = false;
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-client.example;rejected;1",
            DiameterCreditControlRequestType.Initial);

        DiameterCreditControlResult result = context.Application
            .BeginDiameterCreditControlSession(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.State,
                Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(result.TerminateService, Is.True);
            Assert.That(context.Application.FindDiameterCreditControlSession(
                request.SessionId), Is.Null);
            Assert.That(context.Events.Results,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PersistedPendingSessionIsResubmittedAndCorrelated()
    {
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                13,
                0,
                0,
                TimeSpan.Zero),
            0);
        DiameterInMemoryCreditControlStore store =
            new DiameterInMemoryCreditControlStore();
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-client.example;recovered;1",
            DiameterCreditControlRequestType.Initial);
        new CreditControlClientStateMachine(store, clock)
            .BeginCreditControlClientSession(request, 55);
        CreditControlApplicationRequestClient requestClient =
            new CreditControlApplicationRequestClient();
        CreditControlApplicationEventHandler events =
            new CreditControlApplicationEventHandler();

        DiameterCreditControlApplication application =
            new DiameterCreditControlApplication(
                CreateApplicationIdentity("credit.example"),
                CreateApplicationIdentity("example"),
                new CreditControlCommandIdentifierSource(),
                requestClient,
                store,
                new CreditControlApplicationRatingHandler(),
                events,
                clock);
        DiameterCreditControlSession pending =
            application.FindDiameterCreditControlSession(
                request.SessionId)!;
        DiameterCreditControlAnswer answer = CreateApplicationAnswer(
            pending.LastRequest!,
            DiameterResultCodes.Success);
        requestClient.CompleteCreditControlRequest(
            0,
            application.CommandFactory.CreateCreditControlAnswer(
                requestClient.Requests[0].Request,
                answer),
            DiameterResultCodes.Success);
        DiameterCreditControlSession opened =
            application.FindDiameterCreditControlSession(
                request.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(requestClient.Requests,
                Has.Count.EqualTo(1));
            Assert.That(pending.PendingRequestIdentifier,
                Is.EqualTo(55));
            Assert.That(pending.Version, Is.EqualTo(2));
            Assert.That(opened.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(opened.PendingRequestPresent, Is.False);
            Assert.That(events.Results,
                Has.Count.EqualTo(1));
            Assert.That(events.Results[0].Retransmission, Is.True);
            Assert.That(events.Results[0].Successful, Is.True);
        });
    }

    [Test]
    public void ApplicationRejectsEveryMissingDependencyAndPublicBoundary()
    {
        DiameterIdentity host = CreateApplicationIdentity("credit.example");
        DiameterIdentity realm = CreateApplicationIdentity("example");
        CreditControlCommandIdentifierSource identifiers =
            new CreditControlCommandIdentifierSource();
        CreditControlApplicationRequestClient client =
            new CreditControlApplicationRequestClient();
        DiameterInMemoryCreditControlStore store =
            new DiameterInMemoryCreditControlStore();
        CreditControlApplicationRatingHandler handler =
            new CreditControlApplicationRatingHandler();
        TestDiameterClock clock = new TestDiameterClock(
            DateTimeOffset.UtcNow,
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    null, realm, identifiers, client, store, handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, null, identifiers, client, store, handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, realm, null, client, store, handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, realm, identifiers, null, store, handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, realm, identifiers, client, null, handler, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, realm, identifiers, client, store, null, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterCreditControlApplication(
                    host, realm, identifiers, client, store, handler, null, null));
        });

        DiameterCreditControlApplication application =
            new DiameterCreditControlApplication(
                host,
                realm,
                identifiers,
                client,
                store,
                handler,
                null,
                clock);
        Assert.Throws<ArgumentException>(() =>
            application.ExecuteDiameterCreditControlEvent(null));
        Assert.Throws<ArgumentException>(() =>
            application.ProcessDiameterCreditControlRequest(
                application.CommandFactory.CreateCreditControlRequest(
                    CreateApplicationRequest(
                        "credit-server.example;invalid-peer;1",
                        DiameterCreditControlRequestType.Initial)),
                CreateApplicationIdentity("")));
        Assert.That(application.FindDiameterApplicationRequestHandler(
            DiameterCommandCodes.CreditControl), Is.SameAs(application));
        Assert.That(application.FindDiameterApplicationRequestHandler(1U),
            Is.Null);
        Assert.Throws<ArgumentException>(() =>
            application.HandleDiameterApplicationRequest(null!));
        application.HandleDiameterAnswer(null!);
        application.HandleDiameterAnswer(new DiameterAnswerResult());
    }

    [Test]
    public void PendingCorrelationAndIdentifierBoundariesAreDeterministic()
    {
        CreditControlApplicationTestContext context =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest eventRequest = CreateApplicationRequest(
            "credit-client.example;event-failure;1",
            DiameterCreditControlRequestType.Event);
        eventRequest.RequestedActionPresent = true;
        eventRequest.RequestedAction = DiameterRequestedAction.CheckBalance;
        DiameterCreditControlResult sent = context.Application
            .ExecuteDiameterCreditControlEvent(eventRequest);
        DiameterAnswerResult failed = new DiameterAnswerResult();
        failed.RequestIdentifier =
            context.RequestClient.Pending[0].RequestIdentifier;
        failed.PendingRequest = context.RequestClient.Pending[0];
        failed.PendingRequest.Delivered = true;
        failed.PendingRequest.AttemptCount = 2;
        context.Application.HandleDiameterAnswer(failed);

        CreditControlApplicationPendingRequest first =
            new CreditControlApplicationPendingRequest();
        first.RequestIdentifier = 900L;
        first.Request = eventRequest;
        context.Application.AddCreditControlApplicationPendingRequest(first);
        CreditControlApplicationPendingRequest duplicate =
            new CreditControlApplicationPendingRequest();
        duplicate.RequestIdentifier = 900L;
        duplicate.Request = eventRequest;

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(context.Events.Results[0].Delivered, Is.True);
            Assert.That(context.Events.Results[0].Retransmission, Is.True);
            Assert.Throws<DiameterCreditControlException>(() =>
                context.Application.AddCreditControlApplicationPendingRequest(
                    duplicate));
            Assert.That(context.Application
                .TakeCreditControlApplicationPendingRequest(901L), Is.Null);
            Assert.That(context.Application
                .TakeCreditControlApplicationPendingRequest(900L),
                Is.SameAs(first));
        });

        context.Application.NextOperationIdentifier = long.MaxValue;
        Assert.Throws<DiameterCreditControlException>(() =>
            context.Application.CreateCreditControlApplicationOperationIdentifier());

        CreditControlApplicationTestContext distinct =
            CreateCreditControlApplicationContext();
        CreditControlApplicationPendingRequest retained =
            new CreditControlApplicationPendingRequest();
        retained.RequestIdentifier = 1L;
        retained.Request = eventRequest;
        distinct.Application.AddCreditControlApplicationPendingRequest(retained);
        CreditControlApplicationPendingRequest added =
            new CreditControlApplicationPendingRequest();
        added.RequestIdentifier = 2L;
        added.Request = eventRequest;
        Assert.DoesNotThrow(() =>
            distinct.Application.AddCreditControlApplicationPendingRequest(added));
    }

    [Test]
    public void DispatchHandlesNullAndIncompleteSendResultsForSessionAndEvent()
    {
        CreditControlApplicationTestContext sessionContext =
            CreateCreditControlApplicationContext();
        sessionContext.RequestClient.ReturnNull = true;
        DiameterCreditControlResult session = sessionContext.Application
            .BeginDiameterCreditControlSession(CreateApplicationRequest(
                "credit-client.example;null-send;1",
                DiameterCreditControlRequestType.Initial));

        CreditControlApplicationTestContext eventContext =
            CreateCreditControlApplicationContext();
        eventContext.RequestClient.IncludePending = false;
        eventContext.RequestClient.Accept = false;
        eventContext.RequestClient.DeliveredWhenRejected = true;
        eventContext.RequestClient.Code = "custom-rejection";
        DiameterCreditControlRequest request = CreateApplicationRequest(
            "credit-client.example;event-reject;1",
            DiameterCreditControlRequestType.Event);
        request.RequestedActionPresent = true;
        request.RequestedAction = DiameterRequestedAction.CheckBalance;
        DiameterCreditControlResult eventResult = eventContext.Application
            .ExecuteDiameterCreditControlEvent(request);

        Assert.Multiple(() =>
        {
            Assert.That(session.Accepted, Is.False);
            Assert.That(session.Delivered, Is.False);
            Assert.That(eventResult.Accepted, Is.False);
            Assert.That(eventResult.Delivered, Is.True);
            Assert.That(eventResult.Code, Is.EqualTo("custom-rejection"));
        });

        CreditControlApplicationTestContext eventWithoutSnapshot =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest secondEvent = CreateApplicationRequest(
            "credit-client.example;event-no-snapshot;1",
            DiameterCreditControlRequestType.Event);
        secondEvent.RequestedActionPresent = true;
        secondEvent.RequestedAction = DiameterRequestedAction.CheckBalance;
        eventWithoutSnapshot.Application.ExecuteDiameterCreditControlEvent(
            secondEvent);
        DiameterAnswerResult noSnapshot = new DiameterAnswerResult();
        noSnapshot.RequestIdentifier =
            eventWithoutSnapshot.RequestClient.Pending[0].RequestIdentifier;
        eventWithoutSnapshot.Application.HandleDiameterAnswer(noSnapshot);

        CreditControlApplicationTestContext sessionFailure =
            CreateCreditControlApplicationContext();
        sessionFailure.Application.BeginDiameterCreditControlSession(
            CreateApplicationRequest(
                "credit-client.example;answer-failure;1",
                DiameterCreditControlRequestType.Initial));
        DiameterAnswerResult noAnswer = new DiameterAnswerResult();
        noAnswer.RequestIdentifier =
            sessionFailure.RequestClient.Pending[0].RequestIdentifier;
        sessionFailure.Application.HandleDiameterAnswer(noAnswer);
        Assert.That(eventWithoutSnapshot.Events.Results, Has.Count.EqualTo(1));
        Assert.That(sessionFailure.Events.Results, Has.Count.EqualTo(1));

        CreditControlApplicationTestContext inbound =
            CreateCreditControlApplicationContext();
        DiameterCreditControlRequest inboundRequest = CreateApplicationRequest(
            "credit-server.example;host-handler;1",
            DiameterCreditControlRequestType.Initial);
        DiameterRequestContext requestContext = new DiameterRequestContext();
        requestContext.Request = inbound.Application.CommandFactory
            .CreateCreditControlRequest(inboundRequest);
        requestContext.PeerIdentity = null!;
        DiameterApplicationResult handled = inbound.Application
            .HandleDiameterApplicationRequest(requestContext);
        Assert.That(handled.OutputMessage, Is.Not.Null);
    }

    private static CreditControlApplicationTestContext
        CreateCreditControlApplicationContext()
    {
        CreditControlApplicationTestContext result =
            new CreditControlApplicationTestContext();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            0);
        result.Store = new DiameterInMemoryCreditControlStore();
        result.RequestClient =
            new CreditControlApplicationRequestClient();
        result.Handler = new CreditControlApplicationRatingHandler();
        result.Events = new CreditControlApplicationEventHandler();
        result.Application = new DiameterCreditControlApplication(
            CreateApplicationIdentity("credit.example"),
            CreateApplicationIdentity("example"),
            new CreditControlCommandIdentifierSource(),
            result.RequestClient,
            result.Store,
            result.Handler,
            result.Events,
            result.Clock);
        return result;
    }

    private static DiameterCreditControlRequest CreateApplicationRequest(
        string sessionId,
        DiameterCreditControlRequestType requestType)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = sessionId;
        result.DestinationRealm.Value = "remote.example";
        result.DestinationHost =
            CreateApplicationIdentity("credit.remote.example");
        result.PeerIdentity =
            CreateApplicationIdentity("peer.remote.example");
        result.ServiceContextId = "example.credit";
        result.RequestType = requestType;
        result.CorrelationId = new byte[] { 4, 5, 6 };
        return result;
    }

    private static DiameterCreditControlAnswer CreateApplicationAnswer(
        DiameterCreditControlRequest request,
        uint resultCode)
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = resultCode;
        result.Disposition = DiameterCreditControlDisposition.Granted;
        return result;
    }

    private static DiameterIdentity CreateApplicationIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class CreditControlApplicationTestContext
{
    internal TestDiameterClock Clock = null!;
    internal DiameterInMemoryCreditControlStore Store = null!;
    internal CreditControlApplicationRequestClient RequestClient = null!;
    internal CreditControlApplicationRatingHandler Handler = null!;
    internal CreditControlApplicationEventHandler Events = null!;
    internal DiameterCreditControlApplication Application = null!;
}

internal class CreditControlApplicationRequestClient :
    IDiameterRequestClient
{
    internal bool Accept = true;
    internal bool ReturnNull;
    internal bool IncludePending = true;
    internal bool Delivered = true;
    internal bool DeliveredWhenRejected;
    internal string Code = string.Empty;
    internal List<DiameterSendRequest> Requests =
        new List<DiameterSendRequest>();
    internal List<IDiameterAnswerHandler> Handlers =
        new List<IDiameterAnswerHandler>();
    internal List<DiameterPendingRequestSnapshot> Pending =
        new List<DiameterPendingRequestSnapshot>();

    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        if (request == null || handler == null)
        {
            throw new ArgumentException(
                "A request and answer handler are required.");
        }

        Requests.Add(request);
        Handlers.Add(handler);
        if (ReturnNull)
        {
            return null!;
        }

        DiameterSendResult result = new DiameterSendResult();
        result.Accepted = Accept;
        result.Delivered = Accept ? Delivered : DeliveredWhenRejected;
        result.Status = Accept ?
            DiameterSendStatus.Accepted : DiameterSendStatus.Failed;
        result.Code = Code.Length > 0 ? Code : Accept ?
            "credit-control-test-sent" : "credit-control-test-rejected";
        if (Accept && IncludePending)
        {
            DiameterPendingRequestSnapshot pending =
                new DiameterPendingRequestSnapshot();
            pending.RequestIdentifier = Pending.Count + 100;
            pending.SendRequest = request;
            pending.OutboundMessage = request.Request;
            pending.Delivered = true;
            pending.AttemptCount = 1;
            Pending.Add(pending);
            result.PendingRequest = pending;
        }

        return result;
    }

    public DiameterAnswerResult CancelDiameterRequest(
        long requestIdentifier)
    {
        return new DiameterAnswerResult();
    }

    public List<DiameterPendingRequestSnapshot>
        ReadPendingDiameterRequests()
    {
        return new List<DiameterPendingRequestSnapshot>(Pending);
    }

    internal void CompleteCreditControlRequest(
        int index,
        DiameterMessage answer,
        uint resultCode)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = Pending[index].RequestIdentifier;
        result.PendingRequest = Pending[index];
        result.Answer = answer;
        result.AnswerReceived = true;
        result.Correlated = true;
        result.Successful = resultCode >= 2000U && resultCode < 3000U;
        result.ResultCode = resultCode;
        Handlers[index].HandleDiameterAnswer(result);
    }
}

internal class CreditControlApplicationRatingHandler :
    ICreditControlRequestHandler
{
    internal List<DiameterCreditControlRequest> Requests =
        new List<DiameterCreditControlRequest>();

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException(
                "A credit-control request is required.");
        }

        Requests.Add(CreditControlDataCloner
            .CloneCreditControlRequest(request));
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        result.Disposition = DiameterCreditControlDisposition.Granted;
        return result;
    }
}

internal class CreditControlApplicationEventHandler :
    ICreditControlEventHandler
{
    internal List<DiameterCreditControlResult> Results =
        new List<DiameterCreditControlResult>();

    public void HandleDiameterCreditControlEvent(
        DiameterCreditControlResult eventData)
    {
        Results.Add(CreditControlDataCloner.CloneCreditControlResult(
            eventData));
    }
}
