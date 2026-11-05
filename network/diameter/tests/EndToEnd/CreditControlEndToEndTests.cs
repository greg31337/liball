namespace diameter.Tests;

public class CreditControlEndToEndTests
{
    [Test]
    public void CompleteMultipleServiceFinalUnitAndEventSequencesRoundTripOverTcp()
    {
        TcpTlsPeerEndToEndContext? context = null;
        try
        {
            context = new TcpTlsPeerEndToEndContext(false, null);
            context.ExchangeCapabilities();
            RecordingRequestRouter router = new RecordingRequestRouter();
            router.RoutePeer = "server.example";
            DiameterRequestClient requestClient = new DiameterRequestClient(
                context.Client.Configuration,
                router,
                context.Client.Manager,
                context.Codec,
                context.Client.Identifiers,
                context.Clock);
            DiameterInMemoryCreditControlStore clientStore =
                new DiameterInMemoryCreditControlStore();
            DiameterInMemoryCreditControlStore serverStore =
                new DiameterInMemoryCreditControlStore();
            CreditControlApplicationEventHandler clientEvents =
                new CreditControlApplicationEventHandler();
            EndToEndCreditControlRatingHandler rating =
                new EndToEndCreditControlRatingHandler();
            DiameterCreditControlApplication client =
                CreateApplication(
                    "client.example",
                    context.Client.Identifiers,
                    requestClient,
                    clientStore,
                    new EndToEndCreditControlRatingHandler(),
                    clientEvents,
                    context.Clock);
            DiameterCreditControlApplication server =
                CreateApplication(
                    "server.example",
                    context.Server.Identifiers,
                    requestClient,
                    serverStore,
                    rating,
                    new CreditControlApplicationEventHandler(),
                    context.Clock);

            string sessionId = "client.example;dcca;1";
            DiameterCreditControlRequest initial = CreateSessionRequest(
                sessionId,
                DiameterCreditControlRequestType.Initial);
            AddMultipleServiceRequests(initial, false);
            DiameterCreditControlResult initialSent = client
                .BeginDiameterCreditControlSession(initial);
            DiameterMessage initialWireRequest = context.ReceiveMessage(
                context.ServerConnection);
            DiameterCreditControlResult initialServer = server
                .ProcessDiameterCreditControlRequest(
                    initialWireRequest,
                    CreateIdentity("client.example"));
            CompleteClientExchange(
                context,
                requestClient,
                initialServer.Answer!.Message!);
            DiameterCreditControlResult initialClient =
                clientEvents.Results[clientEvents.Results.Count - 1];

            context.SendMessage(
                context.ClientConnection,
                initialWireRequest);
            DiameterCreditControlResult duplicate = server
                .ProcessDiameterCreditControlRequest(
                    context.ReceiveMessage(context.ServerConnection),
                    CreateIdentity("client.example"));

            DiameterCreditControlRequest update = CreateSessionRequest(
                sessionId,
                DiameterCreditControlRequestType.Update);
            AddMultipleServiceRequests(update, true);
            DiameterCreditControlResult updateSent = client
                .UpdateDiameterCreditControlSession(update);
            DiameterCreditControlResult updateServer = server
                .ProcessDiameterCreditControlRequest(
                    context.ReceiveMessage(context.ServerConnection),
                    CreateIdentity("client.example"));
            CompleteClientExchange(
                context,
                requestClient,
                updateServer.Answer!.Message!);
            DiameterCreditControlResult updateClient =
                clientEvents.Results[clientEvents.Results.Count - 1];

            DiameterCreditControlRequest termination = CreateSessionRequest(
                sessionId,
                DiameterCreditControlRequestType.Termination);
            termination.TerminationCause = DiameterTerminationCause.Logout;
            termination.TerminationCausePresent = true;
            termination.RequireFinalUsageReport = true;
            termination.UsedServiceUnits.Add(CreateTimeUnit(30U));
            DiameterCreditControlResult terminationSent = client
                .TerminateDiameterCreditControlSession(termination);
            DiameterCreditControlResult terminationServer = server
                .ProcessDiameterCreditControlRequest(
                    context.ReceiveMessage(context.ServerConnection),
                    CreateIdentity("client.example"));
            CompleteClientExchange(
                context,
                requestClient,
                terminationServer.Answer!.Message!);
            DiameterCreditControlResult terminationClient =
                clientEvents.Results[clientEvents.Results.Count - 1];

            DiameterCreditControlRequest eventRequest =
                CreateSessionRequest(
                    "client.example;dcca;event;1",
                    DiameterCreditControlRequestType.Event);
            eventRequest.RequestedAction =
                DiameterRequestedAction.DirectDebiting;
            eventRequest.RequestedActionPresent = true;
            eventRequest.RequestedServiceUnit = CreateTimeUnit(1U);
            eventRequest.EventTimestamp = context.Clock.ReadUtcTime();
            eventRequest.EventTimestampPresent = true;
            DiameterCreditControlResult eventSent = client
                .ExecuteDiameterCreditControlEvent(eventRequest);
            DiameterCreditControlResult eventServer = server
                .ProcessDiameterCreditControlRequest(
                    context.ReceiveMessage(context.ServerConnection),
                    CreateIdentity("client.example"));
            CompleteClientExchange(
                context,
                requestClient,
                eventServer.Answer!.Message!);
            DiameterCreditControlResult eventClient =
                clientEvents.Results[clientEvents.Results.Count - 1];

            Assert.Multiple(() =>
            {
                Assert.That(initialSent.Accepted, Is.True);
                Assert.That(initialServer.Successful, Is.True);
                Assert.That(initialClient.State,
                    Is.EqualTo(DiameterCreditControlState.Open));
                Assert.That(initialClient.Session!.MultipleServiceCredits,
                    Has.Count.EqualTo(2));
                Assert.That(initialClient.Session.SessionFailoverSupported,
                    Is.True);
                Assert.That(duplicate.Duplicate, Is.True);
                Assert.That(duplicate.Retransmission, Is.True);
                Assert.That(duplicate.Code,
                    Is.EqualTo("credit-control-answer-replayed"));
                Assert.That(updateSent.Request.RequestNumber,
                    Is.EqualTo(1U));
                Assert.That(updateClient.Successful, Is.True);
                Assert.That(updateClient.FinalUnit, Is.True);
                Assert.That(updateClient.Session!.FinalUnitInstruction!.Action,
                    Is.EqualTo(DiameterFinalUnitAction.Terminate));
                Assert.That(updateClient.Session.MultipleServiceCredits[0]
                    .UsedUnits[0].Time, Is.EqualTo(30U));
                Assert.That(terminationSent.Request.RequestNumber,
                    Is.EqualTo(2U));
                Assert.That(terminationServer.State,
                    Is.EqualTo(DiameterCreditControlState.Closed));
                Assert.That(terminationClient.State,
                    Is.EqualTo(DiameterCreditControlState.Closed));
                Assert.That(eventSent.Accepted, Is.True);
                Assert.That(eventServer.Session, Is.Null);
                Assert.That(eventClient.Successful, Is.True);
                Assert.That(eventClient.State,
                    Is.EqualTo(DiameterCreditControlState.Idle));
                Assert.That(eventClient.Code,
                    Is.EqualTo("credit-control-direct-debit-completed"));
                Assert.That(rating.Requests, Has.Count.EqualTo(4));
            });
        }
        finally
        {
            if (context != null)
            {
                context.Close();
            }
        }
    }

    [Test]
    public void FailureFailoverDuplicateAndRecoveryPreserveCcrIdentity()
    {
        TestDiameterClock clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                20,
                0,
                0,
                TimeSpan.Zero),
            0);
        TestCreditControlStore store = new TestCreditControlStore();
        CreditControlClientStateMachine client =
            new CreditControlClientStateMachine(store, clock);
        DiameterCreditControlRequest initial = CreateFailoverRequest(
            DiameterCreditControlRequestType.Initial);
        client.BeginCreditControlClientSession(initial, 101);
        DiameterCreditControlAnswer initialAnswer = WireAnswer(
            initial,
            DiameterResultCodes.Success,
            true,
            clock);
        client.ProcessCreditControlClientAnswer(101, initialAnswer);

        DiameterCreditControlRequest update = CreateFailoverRequest(
            DiameterCreditControlRequestType.Update);
        DiameterCreditControlResult pending = client
            .UpdateCreditControlClientSession(update, 102);
        DiameterCreditControlAnswer temporaryFailure = WireAnswer(
            pending.Request,
            DiameterResultCodes.TooBusy,
            false,
            clock);
        DiameterCreditControlResult failed = client
            .ProcessCreditControlClientAnswer(102, temporaryFailure);

        CreditControlClientStateMachine restarted =
            new CreditControlClientStateMachine(store, clock);
        List<DiameterCreditControlResult> recovered = restarted
            .RecoverCreditControlClientTransactions();
        DiameterCreditControlAnswer recoveredAnswer = WireAnswer(
            recovered[0].Request,
            DiameterResultCodes.Success,
            false,
            clock);
        DiameterCreditControlResult completed = restarted
            .ProcessCreditControlClientAnswer(102, recoveredAnswer);
        long completedVersion = completed.Session!.Version;
        DiameterCreditControlResult duplicate = restarted
            .ProcessCreditControlClientAnswer(102, recoveredAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(failed.RetryRequired, Is.True);
            Assert.That(failed.FailoverAttempted, Is.True);
            Assert.That(failed.Retransmission, Is.True);
            Assert.That(failed.Request.RequestNumber, Is.EqualTo(1U));
            Assert.That(failed.Request.DestinationHost!.Value,
                Is.EqualTo("secondary.credit.example"));
            Assert.That(recovered, Has.Count.EqualTo(1));
            Assert.That(recovered[0].RetryRequired, Is.True);
            Assert.That(recovered[0].Retransmission, Is.True);
            Assert.That(recovered[0].Request.RequestNumber,
                Is.EqualTo(1U));
            Assert.That(recovered[0].Session!.PendingRequestIdentifier,
                Is.EqualTo(102));
            Assert.That(completed.Successful, Is.True);
            Assert.That(completed.State,
                Is.EqualTo(DiameterCreditControlState.Open));
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Retransmission, Is.True);
            Assert.That(duplicate.Session!.Version,
                Is.EqualTo(completedVersion));
        });
    }

    private static DiameterCreditControlApplication CreateApplication(
        string host,
        IDiameterIdentifierSource identifiers,
        IDiameterRequestClient requestClient,
        ICreditControlStore store,
        ICreditControlRequestHandler handler,
        ICreditControlEventHandler events,
        IDiameterClock clock)
    {
        return new DiameterCreditControlApplication(
            CreateIdentity(host),
            CreateIdentity("example"),
            identifiers,
            requestClient,
            store,
            handler,
            events,
            clock);
    }

    private static void CompleteClientExchange(
        TcpTlsPeerEndToEndContext context,
        DiameterRequestClient requestClient,
        DiameterMessage answer)
    {
        context.SendMessage(context.ServerConnection, answer);
        DiameterAnswerResult correlated = requestClient
            .HandleReceivedDiameterAnswer(
                CreateIdentity("server.example"),
                context.ReceiveMessage(context.ClientConnection));
        Assert.That(correlated.Correlated, Is.True);
    }

    private static DiameterCreditControlRequest CreateSessionRequest(
        string sessionId,
        DiameterCreditControlRequestType type)
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = sessionId;
        result.DestinationRealm.Value = "example";
        result.DestinationHost = CreateIdentity("server.example");
        result.PeerIdentity = CreateIdentity("server.example");
        result.ServiceContextId = "example.dcca";
        result.UserName = "alice@example";
        result.RequestType = type;
        result.TimeoutMilliseconds = 5000;
        result.FailureHandling =
            DiameterCreditControlFailureHandling.RetryAndTerminate;
        return result;
    }

    private static void AddMultipleServiceRequests(
        DiameterCreditControlRequest request,
        bool used)
    {
        request.MultipleServicesIndicator =
            DiameterMultipleServicesIndicator.Supported;
        request.MultipleServicesIndicatorPresent = true;
        DiameterMultipleServiceCredit time = CreateMultipleCredit(
            10U,
            100U,
            CreateTimeUnit(60U));
        DiameterServiceUnit octets = new DiameterServiceUnit();
        octets.TotalOctets = 1000UL;
        octets.TotalOctetsPresent = true;
        DiameterMultipleServiceCredit data = CreateMultipleCredit(
            20U,
            200U,
            octets);
        if (used)
        {
            time.UsedUnits.Add(CreateTimeUnit(30U));
            DiameterServiceUnit usedOctets = new DiameterServiceUnit();
            usedOctets.TotalOctets = 500UL;
            usedOctets.TotalOctetsPresent = true;
            data.UsedUnits.Add(usedOctets);
        }

        request.MultipleServiceCredits.Add(time);
        request.MultipleServiceCredits.Add(data);
    }

    private static DiameterMultipleServiceCredit CreateMultipleCredit(
        uint serviceIdentifier,
        uint ratingGroup,
        DiameterServiceUnit requested)
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.ServiceIdentifiers.Add(serviceIdentifier);
        result.RatingGroup = ratingGroup;
        result.RatingGroupPresent = true;
        result.RequestedUnit = requested;
        return result;
    }

    private static DiameterCreditControlRequest CreateFailoverRequest(
        DiameterCreditControlRequestType type)
    {
        DiameterCreditControlRequest result = CreateSessionRequest(
            "client.example;dcca;failover",
            type);
        result.DestinationHost =
            CreateIdentity("primary.credit.example");
        result.PeerIdentity = CreateIdentity("primary-peer.example");
        result.RouteIdentifier = "primary-route";
        result.AllowFailover = true;
        result.TimeoutMilliseconds = 2000;
        DiameterCreditControlServer alternate =
            new DiameterCreditControlServer();
        alternate.DestinationHost =
            CreateIdentity("secondary.credit.example");
        alternate.PeerIdentity =
            CreateIdentity("secondary-peer.example");
        alternate.RouteIdentifier = "secondary-route";
        result.AlternateServers.Add(alternate);
        return result;
    }

    private static DiameterCreditControlAnswer WireAnswer(
        DiameterCreditControlRequest request,
        uint resultCode,
        bool failoverSupported,
        IDiameterClock clock)
    {
        CreditControlCommandIdentifierSource identifiers =
            new CreditControlCommandIdentifierSource();
        CreditControlCommandFactory factory =
            new CreditControlCommandFactory(
                identifiers,
                CreateIdentity("server.example"),
                CreateIdentity("example"));
        DiameterMessage requestMessage = factory
            .CreateCreditControlRequest(request);
        DiameterCreditControlAnswer answer =
            new DiameterCreditControlAnswer();
        answer.SessionId = request.SessionId;
        answer.RequestType = request.RequestType;
        answer.RequestNumber = request.RequestNumber;
        answer.ResultCode = resultCode;
        answer.Disposition = resultCode >= 2000U && resultCode < 3000U ?
            DiameterCreditControlDisposition.Granted :
            DiameterCreditControlDisposition.Failed;
        if (failoverSupported)
        {
            answer.SessionFailoverPresent = true;
            answer.SessionFailoverSupported = true;
        }

        DiameterMessage message = factory.CreateCreditControlAnswer(
            requestMessage,
            answer);
        DiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterEncodeResult encoded = codec.EncodeMessageToArray(message);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        DiameterDecodeResult decoded = codec.DecodeMessage(
            encoded.Data,
            false,
            new DiameterLimitConfiguration());
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return new CreditControlCommandParser(clock)
            .ParseCreditControlAnswer(decoded.Message, request);
    }

    private static DiameterServiceUnit CreateTimeUnit(uint time)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = time;
        result.TimePresent = true;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class EndToEndCreditControlRatingHandler :
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
        if (request.RequestType ==
            DiameterCreditControlRequestType.Initial)
        {
            result.SessionFailoverPresent = true;
            result.SessionFailoverSupported = true;
        }

        if (request.RequestType == DiameterCreditControlRequestType.Event)
        {
            result.GrantedServiceUnit = CreateRatedTimeUnit(1U);
            return result;
        }

        int index = 0;
        while (index < request.MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit source =
                request.MultipleServiceCredits[index];
            DiameterMultipleServiceCredit credit =
                new DiameterMultipleServiceCredit();
            credit.ServiceIdentifiers.AddRange(source.ServiceIdentifiers);
            credit.RatingGroup = source.RatingGroup;
            credit.RatingGroupPresent = source.RatingGroupPresent;
            if (index == 0)
            {
                credit.GrantedUnit = CreateRatedTimeUnit(
                    request.RequestType ==
                        DiameterCreditControlRequestType.Update ?
                        30U : 60U);
            }
            else
            {
                DiameterServiceUnit octets = new DiameterServiceUnit();
                octets.TotalOctets = request.RequestType ==
                    DiameterCreditControlRequestType.Update ?
                    500UL : 1000UL;
                octets.TotalOctetsPresent = true;
                credit.GrantedUnit = octets;
            }

            result.MultipleServiceCredits.Add(credit);
            index = index + 1;
        }

        if (request.RequestType == DiameterCreditControlRequestType.Update)
        {
            result.FinalUnitInstruction =
                new DiameterFinalUnitInstruction();
            result.FinalUnitInstruction.Action =
                DiameterFinalUnitAction.Terminate;
        }

        return result;
    }

    internal static DiameterServiceUnit CreateRatedTimeUnit(uint time)
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = time;
        result.TimePresent = true;
        return result;
    }
}
