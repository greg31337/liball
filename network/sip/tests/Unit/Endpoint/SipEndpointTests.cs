namespace sip.Tests;

public class SipEndpointTests
{
    [Test]
    public void ExposesComposedComponentsAndRunsOrderlyLifecycle()
    {
        SipEndpoint endpoint = new SipEndpoint();
        EndpointTestTransport transport = new EndpointTestTransport();
        EndpointTestExtension extension = new EndpointTestExtension();
        EndpointCredentialProvider credentials =
            new EndpointCredentialProvider();
        endpoint.RegisterEndpointTransport(transport);
        Assert.That(endpoint.RegisterEndpointExtension(extension), Is.True);
        endpoint.ConfigureEndpointAuthentication(credentials);

        endpoint.StartEndpoint(new SipStackOptions());
        SipStackEvent started = endpoint.PollStackEvent();
        SipCancellation ignored = new SipCancellation();
        endpoint.StopEndpoint(ignored);
        SipCancellation shutdown = new SipCancellation();
        shutdown.Requested = true;
        shutdown.Id = "shutdown-1";
        shutdown.Reason = "test complete";
        endpoint.StopEndpoint(shutdown);
        SipStackEvent stopped = endpoint.PollStackEvent();

        Assert.Multiple(() =>
        {
            Assert.That(started.Kind, Is.EqualTo(SipStackEventKind.Started));
            Assert.That(started.State, Is.EqualTo(SipEndpointState.Running));
            Assert.That(endpoint.GetEndpointTransportManager(),
                Is.TypeOf<SipTransportManager>());
            Assert.That(endpoint.GetEndpointTransactionLayer(),
                Is.TypeOf<SipTransactionLayer>());
            Assert.That(endpoint.GetEndpointDialogLayer(),
                Is.TypeOf<SipDialogLayer>());
            Assert.That(endpoint.GetEndpointDnsResolver(),
                Is.TypeOf<SipDnsResolver>());
            Assert.That(endpoint.GetEndpointMessageCodec(),
                Is.TypeOf<SipMessageCodec>());
            Assert.That(endpoint.GetEndpointExtensionRegistry(),
                Is.TypeOf<SipExtensionRegistry>());
            Assert.That(endpoint.GetEndpointAuthenticationClient(),
                Is.TypeOf<SipAuthenticationClient>());
            Assert.That(endpoint.GetEndpointAuthenticationServer(),
                Is.TypeOf<SipAuthenticationServer>());
            Assert.That(stopped.Kind, Is.EqualTo(SipStackEventKind.Stopped));
            Assert.That(stopped.State, Is.EqualTo(SipEndpointState.Stopped));
            Assert.That(stopped.CorrelationId, Is.EqualTo("shutdown-1"));
            Assert.That(transport.CloseCount, Is.EqualTo(1));
            Assert.That(endpoint.GetEndpointState(),
                Is.EqualTo(SipEndpointState.Stopped));
            Assert.That(() => endpoint.StartEndpoint(new SipStackOptions()),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => endpoint.RegisterEndpointExtension(extension),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void SendsGenericUacRequestThroughTransactionAndTransportLayers()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        SipEndpoint endpoint = RunningEndpoint(transport, null);
        SipSendRequest request = SendRequest(RequestMessage());

        SipSendResult result = endpoint.SendEndpointMessage(request);
        request.Message!.RequestUri!.Host = "changed.example.com";
        List<SipStackEvent> events = Drain(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(transport.SendCount, Is.EqualTo(1));
            Assert.That(transport.LastRequest!.Message!.RequestUri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.Created), Is.True);
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Transport
                && value.TransportEvent!.Kind
                    == SipTransportEventKind.MessageSent), Is.True);
            Assert.That(events.Where(value => value.Kind
                == SipStackEventKind.Transaction).All(value =>
                    value.CorrelationId == "endpoint-send"), Is.True);
        });
    }

    [Test]
    public void DispatchesRawUasRequestAndSendsApplicationExtensionResponse()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        EndpointTestExtension extension = new EndpointTestExtension();
        SipEndpoint endpoint = RunningEndpoint(transport, extension);
        SipTransportMessage envelope = new SipTransportMessage();
        envelope.Bytes = System.Text.Encoding.ASCII.GetBytes(RequestText());
        envelope.Transport = SipTransportKind.Custom;
        envelope.CorrelationId = "endpoint-receive";

        endpoint.ReceiveEndpointMessage(envelope);
        envelope.Bytes[0] = 0;
        List<SipStackEvent> receivedEvents = Drain(endpoint);
        SipStackEvent incoming = receivedEvents.First(value =>
            value.Kind == SipStackEventKind.IncomingRequest);
        SipStackEvent dispatched = receivedEvents.First(value =>
            value.Kind == SipStackEventKind.Extension);
        SipSendRequest responseRequest = SendRequest(
            dispatched.ExtensionResult!.Response!);
        responseRequest.CorrelationId = "endpoint-response";
        SipSendResult sent = endpoint.SendEndpointMessage(responseRequest);
        List<SipStackEvent> responseEvents = Drain(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(incoming.Message!.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(incoming.Message.RequestUri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(dispatched.ExtensionResult!.Handled, Is.True);
            Assert.That(dispatched.ExtensionResult.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(extension.ProcessCount, Is.EqualTo(1));
            Assert.That(sent.Success, Is.True, sent.ErrorMessage);
            Assert.That(transport.LastRequest!.Message!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(responseEvents.Any(value => value.Kind
                == SipStackEventKind.OutgoingResponse
                && value.SendResult!.Success), Is.True);
        });
    }

    [Test]
    public void PollingTransportReceiveQueuesUnifiedRequestEvents()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        SipEndpoint endpoint = RunningEndpoint(transport, null);
        SipTransportEvent received = new SipTransportEvent();
        received.Kind = SipTransportEventKind.MessageReceived;
        received.Transport = SipTransportKind.Custom;
        received.Message = new SipTransportMessage();
        received.Message.Bytes =
            System.Text.Encoding.ASCII.GetBytes(RequestText());
        received.Message.CorrelationId = "transport-incoming";
        transport.Events.Add(received);

        SipStackEvent transportEvent = endpoint.PollStackEvent();
        List<SipStackEvent> queued = Drain(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(transportEvent.Kind,
                Is.EqualTo(SipStackEventKind.Transport));
            Assert.That(transportEvent.TransportEvent!.Kind,
                Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(queued[0].Kind,
                Is.EqualTo(SipStackEventKind.IncomingRequest));
            Assert.That(queued.Any(value => value.Kind
                == SipStackEventKind.Transaction), Is.True);
        });
    }

    [Test]
    public void CancellationDeadlineAndQueueLimitFailWithoutTransportSend()
    {
        EndpointTestTransport cancelledTransport = new EndpointTestTransport();
        SipEndpoint cancelledEndpoint = RunningEndpoint(
            cancelledTransport, null);
        SipSendRequest cancelled = SendRequest(RequestMessage());
        cancelled.Cancellation = new SipCancellation();
        cancelled.Cancellation.Requested = true;
        SipSendResult cancelledResult =
            cancelledEndpoint.SendEndpointMessage(cancelled);
        SipSendRequest expired = SendRequest(RequestMessage(2));
        expired.DeadlineMilliseconds = 0;
        SipSendResult expiredResult =
            cancelledEndpoint.SendEndpointMessage(expired);

        EndpointTestTransport limitedTransport = new EndpointTestTransport();
        SipEndpoint limitedEndpoint = new SipEndpoint();
        limitedEndpoint.RegisterEndpointTransport(limitedTransport);
        SipStackOptions limitedOptions = new SipStackOptions();
        limitedOptions.MaxEventCount = 1;
        limitedEndpoint.StartEndpoint(limitedOptions);
        SipSendRequest limited = SendRequest(RequestMessage(3));
        limited.Cancellation = new SipCancellation();
        limited.Cancellation.Requested = true;
        limitedEndpoint.SendEndpointMessage(limited);
        SipStackEvent overflow = limitedEndpoint.PollStackEvent();
        SipStackEvent empty = limitedEndpoint.PollStackEvent();

        Assert.Multiple(() =>
        {
            Assert.That(cancelledResult.Success, Is.False);
            Assert.That(cancelledResult.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(expiredResult.Success, Is.False);
            Assert.That(expiredResult.ErrorMessage, Does.Contain("deadline"));
            Assert.That(cancelledTransport.SendCount, Is.Zero);
            Assert.That(overflow.Kind, Is.EqualTo(SipStackEventKind.Error));
            Assert.That(overflow.ErrorMessage, Does.Contain("queue limit"));
            Assert.That(empty.Kind, Is.EqualTo(SipStackEventKind.None));
            Assert.That(limitedTransport.SendCount, Is.Zero);
        });
    }

    [Test]
    public void CreatesAndBindsConfiguredUdpTransport()
    {
        SipEndpoint endpoint = new SipEndpoint();
        SipStackOptions options = new SipStackOptions();
        SipTransportOptions udp = new SipTransportOptions();
        udp.Kind = SipTransportKind.Udp;
        udp.LocalAddress = new SipEndpointAddress();
        udp.LocalAddress.Host = "127.0.0.1";
        udp.LocalAddress.Port = 0;
        udp.LocalAddress.Transport = SipTransportKind.Udp;
        options.Transports = new SipTransportOptions[] { udp };

        endpoint.StartEndpoint(options);
        SipStackEvent started = endpoint.PollStackEvent();
        SipStackEvent bound = endpoint.PollStackEvent();
        Stop(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(started.Kind, Is.EqualTo(SipStackEventKind.Started));
            Assert.That(bound.Kind, Is.EqualTo(SipStackEventKind.Transport));
            Assert.That(bound.TransportEvent!.Kind,
                Is.EqualTo(SipTransportEventKind.Bound));
            Assert.That(bound.TransportEvent.Transport,
                Is.EqualTo(SipTransportKind.Udp));
        });
    }

    [Test]
    public void AdvancesClientTransactionTimersIntoUnifiedEvents()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        SipEndpoint endpoint = new SipEndpoint();
        endpoint.RegisterEndpointTransport(transport);
        SipStackOptions options = new SipStackOptions();
        options.TransactionOptions.Timers.T1Milliseconds = 1;
        options.TransactionOptions.Timers.T2Milliseconds = 2;
        options.TransactionOptions.Timers.T4Milliseconds = 2;
        endpoint.StartEndpoint(options);
        endpoint.PollStackEvent();
        endpoint.SendEndpointMessage(SendRequest(RequestMessage()));
        Drain(endpoint);

        endpoint.AdvanceEndpointTime(
            System.Environment.TickCount64 + 1000);
        List<SipStackEvent> events = Drain(endpoint);
        Stop(endpoint);

        Assert.That(events.Any(value => value.Kind
            == SipStackEventKind.Transaction
            && (value.TransactionEvent!.Kind
                    == SipTransactionEventKind.TimerFired
                || value.TransactionEvent.Kind
                    == SipTransactionEventKind.Terminated)), Is.True);
    }

    [Test]
    public void ShutdownTerminatesLiveTransactionsBeforeStoppedEvent()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        SipEndpoint endpoint = RunningEndpoint(transport, null);
        endpoint.SendEndpointMessage(SendRequest(RequestMessage()));
        Drain(endpoint);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Reason = "endpoint shutdown";

        endpoint.StopEndpoint(cancellation);
        List<SipStackEvent> events = Drain(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.Terminated), Is.True);
            Assert.That(events[^1].Kind,
                Is.EqualTo(SipStackEventKind.Stopped));
            Assert.That(transport.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ConcurrentServerResponsesUseTheirOwnTransactionBranches()
    {
        EndpointTestTransport transport = new EndpointTestTransport();
        SipEndpoint endpoint = RunningEndpoint(transport, null);
        SipMessage firstRequest = RequestMessage(
            "z9hG4bK-endpoint-first", "from-first");
        SipMessage secondRequest = RequestMessage(
            "z9hG4bK-endpoint-second", "from-second");
        ReceiveRequest(endpoint, firstRequest, "incoming-first");
        ReceiveRequest(endpoint, secondRequest, "incoming-second");
        Drain(endpoint);

        SipMessage firstResponse = SipUasResponseCore.CreateResponseMessage(
            firstRequest, SipStatusCodes.BusyHere,
            SipReasonPhrases.BusyHere)!;
        SipMessage secondResponse = SipUasResponseCore.CreateResponseMessage(
            secondRequest, SipStatusCodes.Ok, SipReasonPhrases.Ok)!;
        SipSendRequest firstSend = SendRequest(firstResponse);
        firstSend.CorrelationId = "outgoing-first";
        SipSendRequest secondSend = SendRequest(secondResponse);
        secondSend.CorrelationId = "outgoing-second";

        SipSendResult[] results = await Task.WhenAll(
            Task.Run(() => endpoint.SendEndpointMessage(firstSend)),
            Task.Run(() => endpoint.SendEndpointMessage(secondSend)));
        List<SipStackEvent> events = Drain(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(results.All(value => value.Success), Is.True);
            Assert.That(events.Any(value => TransactionMessageMatches(
                value, SipStatusCodes.BusyHere, "incoming-first")), Is.True);
            Assert.That(events.Any(value => TransactionMessageMatches(
                value, SipStatusCodes.Ok, "incoming-second")), Is.True);
            Assert.That(events.Count(value => value.Kind
                == SipStackEventKind.OutgoingResponse), Is.EqualTo(2));
            Assert.That(events.Where(value => value.Kind
                == SipStackEventKind.OutgoingResponse).Select(value =>
                    value.CorrelationId), Is.EquivalentTo(new string[]
                    { "outgoing-first", "outgoing-second" }));
            Assert.That(transport.SendCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void LifecycleAndRegistrationBoundariesRemainObservable()
    {
        SipEndpoint endpoint = new SipEndpoint();
        EndpointTestTransport transport = new EndpointTestTransport();
        EndpointTestExtension extension = new EndpointTestExtension();
        transport.Endpoint = endpoint;

        SipStackEvent idle = endpoint.PollStackEvent();
        bool registered = endpoint.RegisterEndpointExtension(extension);
        bool duplicate = endpoint.RegisterEndpointExtension(extension);
        bool removed = endpoint.RemoveEndpointExtension(extension);
        bool missing = endpoint.RemoveEndpointExtension(extension);
        endpoint.RegisterEndpointTransport(transport);
        endpoint.StartEndpoint(new SipStackOptions());
        SipStackEvent started = endpoint.PollStackEvent();
        endpoint.RegisterEndpointExtension(extension);
        SipCancellation shutdown = new SipCancellation();
        shutdown.Requested = true;
        shutdown.Id = "lifecycle-stop";
        endpoint.StopEndpoint(shutdown);
        SipStackEvent stopped = endpoint.PollStackEvent();
        SipSendResult afterStop = endpoint.SendEndpointMessage(
            SendRequest(RequestMessage()));

        Assert.Multiple(() =>
        {
            Assert.That(idle.Kind, Is.EqualTo(SipStackEventKind.None));
            Assert.That(idle.State, Is.EqualTo(SipEndpointState.None));
            Assert.That(registered, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(started.State, Is.EqualTo(SipEndpointState.Running));
            Assert.That(transport.CloseObservedState,
                Is.EqualTo(SipEndpointState.Stopping));
            Assert.That(stopped.State, Is.EqualTo(SipEndpointState.Stopped));
            Assert.That(stopped.CorrelationId, Is.EqualTo("lifecycle-stop"));
            Assert.That(afterStop.Success, Is.False);
            Assert.That(afterStop.ErrorMessage, Does.Contain("not running"));
            Assert.That(transport.SendCount, Is.Zero);
            Assert.That(() => endpoint.RemoveEndpointExtension(extension),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void FailedStartBecomesTerminalAndRejectsFurtherOperations()
    {
        SipEndpoint endpoint = new SipEndpoint();
        SipStackOptions options = new SipStackOptions();
        SipTransportOptions unsupported = new SipTransportOptions();
        unsupported.Kind = SipTransportKind.Custom;
        options.Transports = new SipTransportOptions[] { unsupported };

        Assert.That(() => endpoint.StartEndpoint(options),
            Throws.TypeOf<ArgumentException>());
        SipStackEvent empty = endpoint.PollStackEvent();
        SipSendResult send = endpoint.SendEndpointMessage(
            SendRequest(RequestMessage()));
        SipStackEvent failedSend = endpoint.PollStackEvent();
        SipCancellation shutdown = new SipCancellation();
        shutdown.Requested = true;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.GetEndpointState(),
                Is.EqualTo(SipEndpointState.Failed));
            Assert.That(empty.Kind, Is.EqualTo(SipStackEventKind.None));
            Assert.That(empty.State, Is.EqualTo(SipEndpointState.Failed));
            Assert.That(send.Success, Is.False);
            Assert.That(failedSend.Kind, Is.EqualTo(SipStackEventKind.Error));
            Assert.That(failedSend.State,
                Is.EqualTo(SipEndpointState.Failed));
            Assert.That(() => endpoint.StartEndpoint(new SipStackOptions()),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => endpoint.RegisterEndpointExtension(
                new EndpointTestExtension()),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => endpoint.StopEndpoint(shutdown),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    static SipEndpoint RunningEndpoint(
        EndpointTestTransport transport, ISipExtension? extension)
    {
        SipEndpoint endpoint = new SipEndpoint();
        endpoint.RegisterEndpointTransport(transport);
        if (extension != null)
        {
            endpoint.RegisterEndpointExtension(extension);
        }
        endpoint.StartEndpoint(new SipStackOptions());
        Assert.That(endpoint.PollStackEvent().Kind,
            Is.EqualTo(SipStackEventKind.Started));
        return endpoint;
    }

    static SipSendRequest SendRequest(SipMessage message)
    {
        SipSendRequest request = new SipSendRequest();
        request.Message = message;
        request.Transport = SipTransportKind.Custom;
        request.RemoteAddress = new SipEndpointAddress();
        request.RemoteAddress.Host = "192.0.2.20";
        request.RemoteAddress.Port = 5060;
        request.RemoteAddress.Transport = SipTransportKind.Custom;
        request.CorrelationId = "endpoint-send";
        return request;
    }

    static SipMessage RequestMessage()
    {
        return RequestMessage(1);
    }

    static SipMessage RequestMessage(int cseq)
    {
        return Parse(RequestText().Replace("CSeq: 1 OPTIONS",
            "CSeq: " + cseq + " OPTIONS", StringComparison.Ordinal));
    }

    static SipMessage RequestMessage(string branch, string fromTag)
    {
        return Parse(RequestText()
            .Replace("z9hG4bK-endpoint", branch, StringComparison.Ordinal)
            .Replace("tag=from-tag", "tag=" + fromTag,
                StringComparison.Ordinal));
    }

    static void ReceiveRequest(SipEndpoint endpoint, SipMessage message,
        string correlationId)
    {
        SipTransportMessage envelope = new SipTransportMessage();
        envelope.Bytes = System.Text.Encoding.ASCII.GetBytes(RequestText());
        envelope.Message = message;
        envelope.Transport = SipTransportKind.Custom;
        envelope.CorrelationId = correlationId;
        endpoint.ReceiveEndpointMessage(envelope);
    }

    static bool TransactionMessageMatches(SipStackEvent value,
        int statusCode, string correlationId)
    {
        return value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent != null
            && value.TransactionEvent.Message != null
            && value.TransactionEvent.Message.StatusCode == statusCode
            && value.CorrelationId == correlationId;
    }

    static string RequestText()
    {
        return "OPTIONS sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-endpoint\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Client <sip:client@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: endpoint-call\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n";
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static List<SipStackEvent> Drain(SipEndpoint endpoint)
    {
        List<SipStackEvent> values = new List<SipStackEvent>();
        SipStackEvent value = endpoint.PollStackEvent();
        while (value.Kind != SipStackEventKind.None)
        {
            values.Add(value);
            value = endpoint.PollStackEvent();
        }
        return values;
    }

    static void Stop(SipEndpoint endpoint)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        endpoint.StopEndpoint(cancellation);
    }
}

class EndpointTestTransport : ISipTransport
{
    public int SendCount;
    public int CloseCount;
    public SipSendRequest? LastRequest;
    public SipEndpoint? Endpoint;
    public SipEndpointState CloseObservedState;
    public List<SipTransportEvent> Events = new List<SipTransportEvent>();

    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        return new SipEndpointAddress();
    }

    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        return new SipConnectionInfo();
    }

    public SipSendResult SendTransport(SipSendRequest request)
    {
        SendCount++;
        LastRequest = request;
        SipSendResult result = new SipSendResult();
        result.Success = true;
        result.BytesSent = request.Bytes.Length;
        result.CorrelationId = request.CorrelationId;
        SipTransportEvent sent = new SipTransportEvent();
        sent.Kind = SipTransportEventKind.MessageSent;
        sent.Transport = SipTransportKind.Custom;
        sent.CorrelationId = request.CorrelationId;
        sent.Message = new SipTransportMessage();
        sent.Message.Message = request.Message;
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
        SipTransportEvent value = Events[0];
        Events.RemoveAt(0);
        return value;
    }

    public void CloseTransport()
    {
        if (Endpoint != null)
        {
            CloseObservedState = Endpoint.GetEndpointState();
        }
        CloseCount++;
    }
}

class EndpointTestExtension : ISipExtension
{
    public int ProcessCount;

    public SipCapabilitySet GetExtensionCapabilities()
    {
        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.Methods = new string[] { SipMethods.Options };
        return capabilities;
    }

    public SipExtensionResult ProcessExtensionRequest(SipMessage message)
    {
        ProcessCount++;
        SipExtensionResult result = new SipExtensionResult();
        result.Handled = true;
        result.Event = "options-received";
        result.Response = SipUasResponseCore.CreateResponseMessage(
            message, SipStatusCodes.Ok, SipReasonPhrases.Ok);
        return result;
    }
}

class EndpointCredentialProvider : ISipCredentialProvider
{
    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        return new SipCredential();
    }
}
