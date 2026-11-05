namespace sip.Tests;

public class SipTransportManagerTests
{
    [Test]
    public void SelectsRegisteredBuiltInAndCustomTransportsFromTargets()
    {
        ManagedTestTransport udp = new ManagedTestTransport(
            SipTransportKind.Udp, true);
        ManagedTestTransport custom = new ManagedTestTransport(
            SipTransportKind.Custom, true);
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(udp);
        manager.RegisterTransport(custom);
        SipSendRequest request = Request(
            SipTransportKind.Unknown, SipTransportKind.Custom, 40,
            "custom-send");

        SipSendResult result = manager.SendManaged(request);
        SipTransportEvent firstBound = manager.PollManagedTransportEvent();
        SipTransportEvent secondBound = manager.PollManagedTransportEvent();
        SipTransportEvent sent = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(udp.SendCount, Is.Zero);
            Assert.That(custom.SendCount, Is.EqualTo(1));
            Assert.That(custom.Requests[0].Transport,
                Is.EqualTo(SipTransportKind.Custom));
            Assert.That(request.Transport, Is.EqualTo(SipTransportKind.Unknown));
            Assert.That(request.RemoteAddress!.Transport,
                Is.EqualTo(SipTransportKind.Custom));
            Assert.That(firstBound.Kind, Is.EqualTo(SipTransportEventKind.Bound));
            Assert.That(secondBound.Kind, Is.EqualTo(SipTransportEventKind.Bound));
            Assert.That(sent.Kind, Is.EqualTo(SipTransportEventKind.MessageSent));
            Assert.That(sent.Transport, Is.EqualTo(SipTransportKind.Custom));
            Assert.That(sent.CorrelationId, Is.EqualTo("custom-send"));
        });
    }

    [Test]
    public void ChoosesUdpByDefaultAndTcpForOversizedUdpTargets()
    {
        ManagedTestTransport tcp = new ManagedTestTransport(
            SipTransportKind.Tcp, true);
        ManagedTestTransport udp = new ManagedTestTransport(
            SipTransportKind.Udp, true);
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(tcp);
        manager.RegisterTransport(udp);

        SipSendResult small = manager.SendManaged(Request(
            SipTransportKind.Unknown, SipTransportKind.Unknown, 40, "small"));
        SipSendRequest largeRequest = Request(
            SipTransportKind.Unknown, SipTransportKind.Udp,
            SipDefaults.UdpSwitchThresholdBytes + 1, "large");
        SipSendResult large = manager.SendManaged(largeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(small.Success, Is.True, small.ErrorMessage);
            Assert.That(large.Success, Is.True, large.ErrorMessage);
            Assert.That(udp.SendCount, Is.EqualTo(1));
            Assert.That(tcp.SendCount, Is.EqualTo(1));
            Assert.That(tcp.Requests[0].Transport,
                Is.EqualTo(SipTransportKind.Udp));
            Assert.That(tcp.Requests[0].RemoteAddress!.Transport,
                Is.EqualTo(SipTransportKind.Udp));
            Assert.That(largeRequest.Transport,
                Is.EqualTo(SipTransportKind.Unknown));
        });
    }

    [Test]
    public void FallsBackToTheNextRegistrationOfTheSelectedKind()
    {
        ManagedTestTransport failing = new ManagedTestTransport(
            SipTransportKind.Custom, true);
        failing.FailSends = true;
        ManagedTestTransport succeeding = new ManagedTestTransport(
            SipTransportKind.Custom, true);
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(failing);
        manager.RegisterTransport(succeeding);

        SipSendResult result = manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "failover"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(failing.SendCount, Is.EqualTo(1));
            Assert.That(succeeding.SendCount, Is.EqualTo(1));
            Assert.That(result.CorrelationId, Is.EqualTo("failover"));
        });
    }

    [Test]
    public void ReusesTrackedFlowAndConnectionAffinity()
    {
        ManagedTestTransport custom = new ManagedTestTransport(
            SipTransportKind.Custom, true);
        SipConnectionInfo connection = Connection("connection-one");
        SipFlow flow = new SipFlow();
        flow.Id = "flow-one";
        flow.Connection = connection;
        flow.Primary = true;
        custom.ResultConnection = connection;
        custom.ResultFlow = flow;
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(custom);

        SipSendResult first = manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 40, "first"));
        custom.ResultConnection = null;
        custom.ResultFlow = null;
        SipSendRequest byFlow = new SipSendRequest();
        byFlow.Bytes = Bytes(20);
        byFlow.FlowId = "flow-one";
        byFlow.CorrelationId = "flow";
        SipSendResult second = manager.SendManaged(byFlow);
        SipSendRequest byEndpoint = Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "endpoint");
        SipSendResult third = manager.SendManaged(byEndpoint);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(third.Success, Is.True, third.ErrorMessage);
            Assert.That(custom.SendCount, Is.EqualTo(3));
            Assert.That(custom.Requests[1].ConnectionId,
                Is.EqualTo("connection-one"));
            Assert.That(custom.Requests[1].RemoteAddress!.Host,
                Is.EqualTo("192.0.2.10"));
            Assert.That(custom.Requests[1].Transport,
                Is.EqualTo(SipTransportKind.Custom));
            Assert.That(second.Flow!.Id, Is.EqualTo("flow-one"));
            Assert.That(second.Connection!.Id, Is.EqualTo("connection-one"));
            Assert.That(custom.Requests[2].ConnectionId,
                Is.EqualTo("connection-one"));
        });
    }

    [Test]
    public void ConvertsConnectionLossIntoFlowFailureAndRejectsFailedFlow()
    {
        ManagedTestTransport custom = new ManagedTestTransport(
            SipTransportKind.Custom, true);
        SipConnectionInfo connection = Connection("connection-lost");
        SipFlow flow = new SipFlow();
        flow.Id = "flow-lost";
        flow.Connection = connection;
        custom.ResultConnection = connection;
        custom.ResultFlow = flow;
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(custom);
        manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "initial"));
        DrainManagedEvents(manager);
        SipTransportEvent disconnected = new SipTransportEvent();
        disconnected.Kind = SipTransportEventKind.Disconnected;
        disconnected.Transport = SipTransportKind.Custom;
        disconnected.Connection = connection;
        disconnected.ErrorMessage = "connection lost";
        disconnected.CorrelationId = "disconnect";
        custom.QueueEvent(disconnected);

        SipTransportEvent loss = manager.PollManagedTransportEvent();
        SipTransportEvent flowFailure = manager.PollManagedTransportEvent();
        int sendsBeforeRetry = custom.SendCount;
        SipSendRequest retry = new SipSendRequest();
        retry.Bytes = Bytes(20);
        retry.FlowId = "flow-lost";
        retry.CorrelationId = "retry";
        SipSendResult failed = manager.SendManaged(retry);
        SipTransportEvent sendFailure = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(loss.Kind, Is.EqualTo(SipTransportEventKind.Disconnected));
            Assert.That(flowFailure.Kind,
                Is.EqualTo(SipTransportEventKind.FlowFailed));
            Assert.That(flowFailure.Flow!.Id, Is.EqualTo("flow-lost"));
            Assert.That(flowFailure.Flow.Failed, Is.True);
            Assert.That(flowFailure.ErrorMessage, Is.EqualTo("connection lost"));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.ErrorMessage, Does.Contain("flow has failed"));
            Assert.That(custom.SendCount, Is.EqualTo(sendsBeforeRetry));
            Assert.That(sendFailure.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(sendFailure.CorrelationId, Is.EqualTo("retry"));
            Assert.That(sendFailure.Flow!.Failed, Is.True);
        });
    }

    [Test]
    public void BoundsManagerEventsAndRecoversAfterPolling()
    {
        SipTransportManager manager = new SipTransportManager();
        int index = 0;
        while (index < SipDefaults.MaxQueueCount)
        {
            SipSendRequest missing = Request(
                SipTransportKind.Custom, SipTransportKind.Custom,
                20, "missing-" + index);
            SipSendResult missingResult = manager.SendManaged(missing);
            Assert.That(missingResult.Success, Is.False);
            index = index + 1;
        }
        ManagedTestTransport custom = new ManagedTestTransport(
            SipTransportKind.Custom, false);
        manager.RegisterTransport(custom);

        SipSendResult pressured = manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "pressure"));
        SipTransportEvent released = manager.PollManagedTransportEvent();
        SipSendResult recovered = manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "recovered"));

        Assert.Multiple(() =>
        {
            Assert.That(pressured.Success, Is.False);
            Assert.That(pressured.ErrorMessage, Does.Contain("queue is full"));
            Assert.That(released.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(recovered.Success, Is.True, recovered.ErrorMessage);
            Assert.That(custom.SendCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReportsUnroutableSendsAndClosesEachTransportOnce()
    {
        SipTransportManager empty = new SipTransportManager();
        SipSendResult unroutable = empty.SendManaged(Request(
            SipTransportKind.Tls, SipTransportKind.Tls, 20, "unroutable"));
        SipTransportEvent failure = empty.PollManagedTransportEvent();
        ManagedTestTransport custom = new ManagedTestTransport(
            SipTransportKind.Custom, false);
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(custom);
        Assert.That(() => manager.RegisterTransport(custom),
            Throws.TypeOf<ArgumentException>());

        manager.CloseManagedTransports();
        manager.CloseManagedTransports();
        SipSendResult closed = manager.SendManaged(Request(
            SipTransportKind.Custom, SipTransportKind.Custom, 20, "closed"));
        SipTransportEvent closedEvent = manager.PollManagedTransportEvent();
        SipTransportEvent closedFailure = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(unroutable.Success, Is.False);
            Assert.That(failure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(failure.Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(failure.CorrelationId, Is.EqualTo("unroutable"));
            Assert.That(custom.CloseCount, Is.EqualTo(1));
            Assert.That(closed.Success, Is.False);
            Assert.That(closedEvent.Kind, Is.EqualTo(SipTransportEventKind.Closed));
            Assert.That(closedFailure.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(closedFailure.CorrelationId, Is.EqualTo("closed"));
            Assert.That(() => manager.RegisterTransport(
                new ManagedTestTransport(SipTransportKind.Custom, false)),
                Throws.TypeOf<SipTransportException>());
        });
    }

    static SipSendRequest Request(SipTransportKind requestTransport,
        SipTransportKind endpointTransport, int byteCount, string correlationId)
    {
        SipSendRequest request = new SipSendRequest();
        request.Bytes = Bytes(byteCount);
        request.RemoteAddress = Endpoint(endpointTransport);
        request.Transport = requestTransport;
        request.CorrelationId = correlationId;
        return request;
    }

    static byte[] Bytes(int count)
    {
        return Enumerable.Repeat((byte)'x', count).ToArray();
    }

    static SipEndpointAddress Endpoint(SipTransportKind transport)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = "192.0.2.10";
        result.Port = 5060;
        result.Transport = transport;
        result.Secure = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
        return result;
    }

    static SipConnectionInfo Connection(string id)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.Id = id;
        result.State = SipConnectionState.Connected;
        result.Transport = SipTransportKind.Custom;
        result.LocalAddress = Endpoint(SipTransportKind.Custom);
        result.RemoteAddress = Endpoint(SipTransportKind.Custom);
        return result;
    }

    static void DrainManagedEvents(SipTransportManager manager)
    {
        SipTransportEvent value = manager.PollManagedTransportEvent();
        while (value.Kind != SipTransportEventKind.None)
        {
            value = manager.PollManagedTransportEvent();
        }
    }
}

class ManagedTestTransport : ISipTransport
{
    public SipTransportKind Kind;
    public int SendCount;
    public int CloseCount;
    public bool FailSends;
    public SipConnectionInfo? ResultConnection;
    public SipFlow? ResultFlow;
    public List<SipSendRequest> Requests = new List<SipSendRequest>();
    public List<SipTransportEvent> Events = new List<SipTransportEvent>();

    public ManagedTestTransport(SipTransportKind kind, bool announce)
    {
        Kind = kind;
        if (announce)
        {
            SipTransportEvent bound = new SipTransportEvent();
            bound.Kind = SipTransportEventKind.Bound;
            bound.Transport = kind;
            Events.Add(bound);
        }
    }

    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        return EndpointForTransport();
    }

    public SipConnectionInfo ConnectTransport(
        SipEndpointAddress remoteAddress)
    {
        return ResultConnection ?? new SipConnectionInfo();
    }

    public SipSendResult SendTransport(SipSendRequest request)
    {
        SendCount++;
        Requests.Add(request);
        SipSendResult result = new SipSendResult();
        result.CorrelationId = request.CorrelationId;
        bool oversizedFallback = Kind == SipTransportKind.Tcp
            && request.Transport == SipTransportKind.Udp
            && request.Bytes.Length > SipDefaults.UdpSwitchThresholdBytes;
        if (FailSends || request.Transport != Kind && !oversizedFallback)
        {
            result.ErrorMessage = "The test transport rejected the send.";
            SipTransportEvent failure = new SipTransportEvent();
            failure.Kind = SipTransportEventKind.SendFailed;
            failure.Transport = Kind;
            failure.ErrorMessage = result.ErrorMessage;
            failure.CorrelationId = result.CorrelationId;
            Events.Add(failure);
            return result;
        }
        result.Success = true;
        result.BytesSent = request.Bytes.Length;
        result.Connection = ResultConnection;
        result.Flow = ResultFlow;
        SipTransportEvent sent = new SipTransportEvent();
        sent.Kind = SipTransportEventKind.MessageSent;
        sent.Transport = Kind;
        sent.Connection = ResultConnection;
        sent.Flow = ResultFlow;
        sent.CorrelationId = request.CorrelationId;
        sent.Message = new SipTransportMessage();
        sent.Message.Transport = Kind;
        sent.Message.RemoteAddress = request.RemoteAddress;
        sent.Message.Connection = ResultConnection;
        sent.Message.Flow = ResultFlow;
        sent.Message.Bytes = request.Bytes;
        Events.Add(sent);
        return result;
    }

    public SipTransportEvent PollTransportEvent()
    {
        if (Events.Count == 0)
        {
            return new SipTransportEvent();
        }
        SipTransportEvent result = Events[0];
        Events.RemoveAt(0);
        return result;
    }

    public void CloseTransport()
    {
        CloseCount++;
        SipTransportEvent value = new SipTransportEvent();
        value.Kind = SipTransportEventKind.Closed;
        value.Transport = Kind;
        Events.Add(value);
    }

    public void QueueEvent(SipTransportEvent value)
    {
        Events.Add(value);
    }

    SipEndpointAddress EndpointForTransport()
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = "127.0.0.1";
        result.Port = 5060;
        result.Transport = Kind;
        return result;
    }
}
