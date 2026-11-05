using System.Text;

namespace diameter.Tests;

public class AuthorizationAccountingEndToEndTests
{
    [Test]
    public void AuthorizationLifecyclesRoundTripOverTcp()
    {
        TcpTlsPeerEndToEndContext? context = null;
        try
        {
            context = new TcpTlsPeerEndToEndContext(false, null);
            context.ExchangeCapabilities();
            SessionTable clientTable = new SessionTable();
            SessionTable serverTable = new SessionTable();
            AuthorizationClientStateMachine client =
                new AuthorizationClientStateMachine(
                    clientTable,
                    context.Clock);
            AuthorizationServerStateMachine server =
                new AuthorizationServerStateMachine(
                    serverTable,
                    context.Clock);

            string normalSessionId = "client.example;authorization;1";
            OpenStatefulSession(
                context,
                client,
                server,
                normalSessionId,
                10U);

            DiameterMessage reAuthRequest = CreateRequest(
                normalSessionId,
                DiameterCommandCodes.ReAuth,
                4U,
                20U);
            AddUnsigned32(
                reAuthRequest,
                DiameterAvpCodes.ReAuthRequestType,
                (uint)DiameterReAuthRequestType.AuthorizeAuthenticate);
            AuthorizationServerTransitionResult reAuthStarted =
                server.BeginServerReauthorization(
                    normalSessionId,
                    reAuthRequest,
                    20);
            context.SendMessage(context.ServerConnection, reAuthRequest);
            AuthorizationClientTransitionResult reAuthReceived =
                client.ProcessClientReAuthRequest(
                    normalSessionId,
                    context.ReceiveMessage(context.ClientConnection),
                    true);
            DiameterMessage reAuthAnswer = CreateAnswer(
                reAuthRequest,
                DiameterResultCodes.Success,
                false);
            context.SendMessage(context.ClientConnection, reAuthAnswer);
            AuthorizationServerTransitionResult reAuthCompleted =
                server.ProcessServerReAuthAnswer(
                    normalSessionId,
                    context.ReceiveMessage(context.ServerConnection));

            DiameterMessage applicationReAuth = CreateRequest(
                normalSessionId,
                300U,
                4U,
                30U);
            client.BeginClientReauthorization(
                normalSessionId,
                applicationReAuth,
                30);
            context.SendMessage(context.ClientConnection, applicationReAuth);
            DiameterMessage receivedApplicationReAuth =
                context.ReceiveMessage(context.ServerConnection);
            server.ProcessServerAuthorizationRequest(
                normalSessionId,
                receivedApplicationReAuth,
                DiameterResultCodes.Success);
            DiameterMessage applicationReAuthAnswer = CreateAnswer(
                receivedApplicationReAuth,
                DiameterResultCodes.Success,
                true);
            context.SendMessage(
                context.ServerConnection,
                applicationReAuthAnswer);
            AuthorizationClientTransitionResult applicationReauthorized =
                client.ProcessClientAuthorizationAnswer(
                    normalSessionId,
                    context.ReceiveMessage(context.ClientConnection));

            DiameterMessage terminationRequest = CreateRequest(
                normalSessionId,
                DiameterCommandCodes.SessionTermination,
                DiameterApplicationIds.Common,
                40U);
            client.BeginClientSessionTermination(
                normalSessionId,
                terminationRequest,
                40,
                DiameterTerminationCause.Logout);
            context.SendMessage(
                context.ClientConnection,
                terminationRequest);
            DiameterMessage receivedTermination =
                context.ReceiveMessage(context.ServerConnection);
            AuthorizationServerTransitionResult terminationReceived =
                server.ProcessServerTerminationRequest(
                    normalSessionId,
                    receivedTermination);
            DiameterMessage terminationAnswer = CreateAnswer(
                receivedTermination,
                DiameterResultCodes.Success,
                false);
            context.SendMessage(
                context.ServerConnection,
                terminationAnswer);
            AuthorizationClientTransitionResult terminationCompleted =
                client.ProcessClientTerminationAnswer(
                    normalSessionId,
                    context.ReceiveMessage(context.ClientConnection));

            string abortSessionId = "client.example;authorization;2";
            OpenStatefulSession(
                context,
                client,
                server,
                abortSessionId,
                50U);
            DiameterMessage abortRequest = CreateRequest(
                abortSessionId,
                DiameterCommandCodes.AbortSession,
                DiameterApplicationIds.Common,
                60U);
            AuthorizationServerTransitionResult abortStarted =
                server.BeginServerAbort(abortSessionId, abortRequest, 60);
            context.SendMessage(context.ServerConnection, abortRequest);
            AuthorizationClientTransitionResult abortReceived =
                client.ProcessClientAbortRequest(
                    abortSessionId,
                    context.ReceiveMessage(context.ClientConnection),
                    true);
            DiameterMessage abortAnswer = CreateAnswer(
                abortRequest,
                DiameterResultCodes.Success,
                false);
            context.SendMessage(context.ClientConnection, abortAnswer);
            AuthorizationServerTransitionResult abortCompleted =
                server.ProcessServerAbortAnswer(
                    abortSessionId,
                    context.ReceiveMessage(context.ServerConnection));

            string statelessSessionId = "client.example;authorization;3";
            SessionTable statelessTable = new SessionTable();
            StatelessAuthorizationOperation stateless =
                new StatelessAuthorizationOperation(
                    statelessTable,
                    context.Clock);
            DiameterMessage statelessRequest = CreateRequest(
                statelessSessionId,
                301U,
                4U,
                70U);
            AddUnsigned32(
                statelessRequest,
                DiameterAvpCodes.AuthSessionState,
                (uint)DiameterAuthSessionState.NoStateMaintained);
            context.SendMessage(
                context.ClientConnection,
                statelessRequest);
            DiameterMessage receivedStateless =
                context.ReceiveMessage(context.ServerConnection);
            StatelessAuthorizationResult statelessServer =
                stateless.ExecuteStatelessServerAuthorization(
                    CreateSessionConfiguration(
                        statelessSessionId,
                        DiameterSessionRole.Server,
                        true),
                    receivedStateless,
                    DiameterResultCodes.Success);
            DiameterMessage statelessAnswer = CreateAnswer(
                receivedStateless,
                DiameterResultCodes.Success,
                true);
            NetworkByteOrder.WriteUInt32(
                statelessAnswer.Avps[statelessAnswer.Avps.Count - 1].Data,
                (uint)DiameterAuthSessionState.NoStateMaintained);
            context.SendMessage(context.ServerConnection, statelessAnswer);
            StatelessAuthorizationResult statelessClient =
                stateless.ExecuteStatelessClientAuthorization(
                    CreateSessionConfiguration(
                        statelessSessionId,
                        DiameterSessionRole.Client,
                        true),
                    statelessRequest,
                    context.ReceiveMessage(context.ClientConnection));

            Assert.Multiple(() =>
            {
                Assert.That(reAuthStarted.ReAuthRequestRequired, Is.True);
                Assert.That(reAuthReceived.ReAuthAnswerRequired, Is.True);
                Assert.That(
                    reAuthReceived.AuthorizationRequestRequired,
                    Is.True);
                Assert.That(reAuthCompleted.Session!.State,
                    Is.EqualTo(DiameterSessionState.Open));
                Assert.That(applicationReauthorized.Session!.State,
                    Is.EqualTo(DiameterSessionState.Open));
                Assert.That(
                    terminationReceived.TerminationAnswerRequired,
                    Is.True);
                Assert.That(terminationReceived.CleanedUp, Is.True);
                Assert.That(terminationCompleted.Session!.State,
                    Is.EqualTo(DiameterSessionState.Closed));
                Assert.That(serverTable.ReadSessionRecord(normalSessionId),
                    Is.Null);
                Assert.That(abortStarted.AbortRequestRequired, Is.True);
                Assert.That(abortReceived.AbortAnswerRequired, Is.True);
                Assert.That(abortReceived.TerminationRequestRequired,
                    Is.True);
                Assert.That(abortCompleted.CleanedUp, Is.True);
                Assert.That(serverTable.ReadSessionRecord(abortSessionId),
                    Is.Null);
                Assert.That(statelessServer.AccessGranted, Is.True);
                Assert.That(statelessClient.AccessGranted, Is.True);
                Assert.That(statelessClient.StatefulSessionRequired,
                    Is.False);
                Assert.That(statelessTable.ReadSessionRecordCount(),
                    Is.Zero);
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
    public void AccountingAndPeerRestartInferenceRoundTripOverTcp()
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
            RecordingAccountingHandler handler =
                new RecordingAccountingHandler();
            DiameterAccountingApplication accounting =
                new DiameterAccountingApplication(
                    CreateIdentity("client.example"),
                    CreateIdentity("example"),
                    new DiameterBaseAvpFactory(),
                    new DiameterBaseCommandFactory(
                        context.Client.Identifiers),
                    requestClient,
                    new DuplicateRequestStore(
                        context.Clock,
                        64,
                        60000),
                    handler,
                    new DiameterInMemoryAccountingRecordStore(),
                    context.Clock);
            DiameterAccountingApplication accountingServer =
                new DiameterAccountingApplication(
                    CreateIdentity("server.example"),
                    CreateIdentity("example"),
                    new DiameterBaseAvpFactory(),
                    new DiameterBaseCommandFactory(
                        context.Server.Identifiers),
                    requestClient,
                    new DuplicateRequestStore(
                        context.Clock,
                        64,
                        60000),
                    handler,
                    new DiameterInMemoryAccountingRecordStore(),
                    context.Clock);
            DiameterAccountingRequest accountingRequest =
                new DiameterAccountingRequest();
            accountingRequest.SessionId = "client.example;accounting;1";
            accountingRequest.ApplicationId =
                DiameterApplicationIds.BaseAccounting;
            accountingRequest.DestinationRealm = CreateIdentity("example");
            accountingRequest.DestinationHost =
                CreateIdentity("server.example");
            accountingRequest.PeerIdentity =
                CreateIdentity("server.example");
            accountingRequest.UserName = "alice@example";
            accountingRequest.TimeoutMilliseconds = 5000;

            DiameterAccountingResult accountingSent =
                accounting.SendDiameterAccountingEvent(accountingRequest);
            DiameterMessage accountingWireRequest =
                context.ReceiveMessage(context.ServerConnection);
            DiameterAccountingProcessingResult accountingProcessed =
                accountingServer.ProcessDiameterAccountingRequest(
                    accountingWireRequest,
                    CreateIdentity("client.example"));
            context.SendMessage(
                context.ServerConnection,
                accountingProcessed.Answer!.Message!);
            DiameterAnswerResult accountingCorrelated =
                requestClient.HandleReceivedDiameterAnswer(
                    CreateIdentity("server.example"),
                    context.ReceiveMessage(context.ClientConnection));
            DiameterAccountingRecord accountingRecord =
                accounting.ReadDiameterAccountingRecords()[0];

            SessionTable sessionTable = new SessionTable();
            AuthorizationServerStateMachine server =
                new AuthorizationServerStateMachine(
                    sessionTable,
                    context.Clock);
            string restartSessionId = "server.example;restart;1";
            server.CreateStatefulServerSession(
                CreateSessionConfiguration(
                    restartSessionId,
                    DiameterSessionRole.Server,
                    false));
            server.ProcessServerAuthorizationRequest(
                restartSessionId,
                CreateRequest(restartSessionId, 300U, 4U, 90U),
                DiameterResultCodes.Success);
            DiameterSessionRecord restartSession =
                sessionTable.ReadSessionRecord(restartSessionId)!;
            uint previousOriginStateId = context.Server.Manager.Table
                .ReadPeerTableSnapshot("client.example")!
                .Capabilities.OriginStateId;
            restartSession.PeerOriginStateIdentifier =
                previousOriginStateId;
            restartSession.PeerOriginStateIdentifierPresent = true;
            sessionTable.SaveSessionRecord(
                restartSession,
                restartSession.Version);
            SessionContinuityController continuity =
                new SessionContinuityController(
                    sessionTable,
                    new RouteSessionAffinityStore(16),
                    context.Clock);
            EndToEndRestartSink requestSink =
                new EndToEndRestartSink();
            EndToEndRestartSink applicationSink =
                new EndToEndRestartSink();
            PeerRestartDetector detector = new PeerRestartDetector(
                context.Server.Manager.Table,
                requestSink,
                continuity,
                applicationSink);
            DiameterMessage restartMessage = CreateRequest(
                "client.example;restart-notice",
                DiameterCommandCodes.DeviceWatchdog,
                DiameterApplicationIds.Common,
                100U);
            restartMessage.Avps.Add(CreateTextAvp(
                DiameterAvpCodes.OriginHost,
                "client.example"));
            AddUnsigned32(
                restartMessage,
                DiameterAvpCodes.OriginStateId,
                previousOriginStateId + 1U);
            context.SendMessage(context.ClientConnection, restartMessage);
            PeerRestartObservation restart = detector.ObservePeerMessage(
                "client.example",
                context.ReceiveMessage(context.ServerConnection));

            Assert.Multiple(() =>
            {
                Assert.That(accountingSent.Accepted, Is.True);
                Assert.That(accountingSent.Delivered, Is.True);
                Assert.That(accountingProcessed.Accepted, Is.True);
                Assert.That(accountingProcessed.ApplicationInvoked, Is.True);
                Assert.That(accountingCorrelated.Correlated, Is.True);
                Assert.That(accountingCorrelated.Successful, Is.True);
                Assert.That(accountingRecord.State,
                    Is.EqualTo(DiameterAccountingState.Closed));
                Assert.That(accountingRecord.Acknowledged, Is.True);
                Assert.That(handler.Requests, Has.Count.EqualTo(1));
                Assert.That(previousOriginStateId, Is.GreaterThan(0U));
                Assert.That(restart.RestartDetected, Is.True);
                Assert.That(restart.NotifiedSubsystems, Is.EqualTo(3));
                Assert.That(requestSink.Events, Is.EqualTo(1));
                Assert.That(applicationSink.Events, Is.EqualTo(1));
                Assert.That(continuity.LastRestartResult.ReleasedSessions,
                    Is.EqualTo(1));
                Assert.That(sessionTable.ReadSessionRecord(restartSessionId),
                    Is.Null);
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

    private static void OpenStatefulSession(
        TcpTlsPeerEndToEndContext context,
        AuthorizationClientStateMachine client,
        AuthorizationServerStateMachine server,
        string sessionId,
        uint identifier)
    {
        client.CreateStatefulClientSession(
            CreateSessionConfiguration(
                sessionId,
                DiameterSessionRole.Client,
                false));
        server.CreateStatefulServerSession(
            CreateSessionConfiguration(
                sessionId,
                DiameterSessionRole.Server,
                false));
        DiameterMessage request = CreateRequest(
            sessionId,
            300U,
            4U,
            identifier);
        client.BeginInitialClientAuthorization(
            sessionId,
            request,
            identifier);
        context.SendMessage(context.ClientConnection, request);
        DiameterMessage received =
            context.ReceiveMessage(context.ServerConnection);
        server.ProcessServerAuthorizationRequest(
            sessionId,
            received,
            DiameterResultCodes.Success);
        DiameterMessage answer = CreateAnswer(
            received,
            DiameterResultCodes.Success,
            true);
        context.SendMessage(context.ServerConnection, answer);
        AuthorizationClientTransitionResult opened =
            client.ProcessClientAuthorizationAnswer(
                sessionId,
                context.ReceiveMessage(context.ClientConnection));
        Assert.That(opened.Session!.State,
            Is.EqualTo(DiameterSessionState.Open));
    }

    private static DiameterSessionConfiguration CreateSessionConfiguration(
        string sessionId,
        DiameterSessionRole role,
        bool stateless)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = sessionId;
        result.Kind = stateless ?
            DiameterSessionKind.StatelessAuthorization :
            DiameterSessionKind.StatefulAuthorization;
        result.Role = role;
        result.ApplicationId = 4U;
        result.AuthSessionState = stateless ?
            DiameterAuthSessionState.NoStateMaintained :
            DiameterAuthSessionState.StateMaintained;
        result.PeerIdentity = role == DiameterSessionRole.Client ?
            CreateIdentity("server.example") :
            CreateIdentity("client.example");
        result.UserName = "alice@example";
        if (role == DiameterSessionRole.Client)
        {
            result.DestinationRealm = CreateIdentity("example");
            result.DestinationHost = CreateIdentity("server.example");
        }

        return result;
    }

    private static DiameterMessage CreateRequest(
        string sessionId,
        uint commandCode,
        uint applicationId,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = commandCode;
        result.ApplicationId = applicationId;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100U;
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            sessionId));
        return result;
    }

    private static DiameterMessage CreateAnswer(
        DiameterMessage request,
        uint resultCode,
        bool includeAuthSessionState)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            Encoding.UTF8.GetString(request.Avps[0].Data)));
        AddUnsigned32(result, DiameterAvpCodes.ResultCode, resultCode);
        if (includeAuthSessionState)
        {
            AddUnsigned32(
                result,
                DiameterAvpCodes.AuthSessionState,
                (uint)DiameterAuthSessionState.StateMaintained);
        }

        return result;
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static void AddUnsigned32(
        DiameterMessage message,
        uint code,
        uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class EndToEndRestartSink : IPeerRestartNotificationSink
{
    internal int Events;

    public void HandlePeerRestart(DiameterPeerEvent peerEvent)
    {
        Events = Events + 1;
    }
}
