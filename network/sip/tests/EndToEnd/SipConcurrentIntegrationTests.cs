namespace sip.Tests;

public class SipConcurrentIntegrationTests
{
    [Test]
    public async Task ConcurrentEndpointSendsAndReceivesKeepEveryRequest()
    {
        const int count = 16;
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipMessage[] outgoing = Enumerable.Range(0, count)
            .Select(index => Request("OPTIONS", "send-" + index,
                index + 1, "send-" + index)).ToArray();

        SipSendResult[] sent = await Task.WhenAll(outgoing.Select(
            (message, index) => Task.Run(() => harness.SendFromUac(
                message, SipTransportKind.Udp, "send-" + index))));
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        List<SipStackEvent> delivered =
            SipInProcessHarness.Drain(harness.Uas);

        SipEndpoint receiver = new SipEndpoint();
        SipStackOptions receiverOptions = new SipStackOptions();
        receiverOptions.MaxEventCount = 128;
        receiverOptions.TransactionOptions.MaxEventCount = 128;
        receiver.StartEndpoint(receiverOptions);
        DrainEndpoint(receiver);
        await Task.WhenAll(Enumerable.Range(0, count).Select(index =>
            Task.Run(() => receiver.ReceiveEndpointMessage(Envelope(
                Bytes(RequestText("MESSAGE", "receive-" + index,
                    index + 1, "receive-" + index, "payload")),
                "receive-" + index)))));
        SipStackEvent[] received = DrainEndpoint(receiver);
        Stop(receiver, "receiver-stop");

        Assert.Multiple(() =>
        {
            Assert.That(sent.All(value => value.Success), Is.True);
            Assert.That(delivered.Count(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.EqualTo(count));
            Assert.That(delivered.Where(value => value.Kind
                == SipStackEventKind.IncomingRequest).Select(value =>
                    Header(value.Message!, SipHeaderNames.CallId)).Distinct()
                .Count(), Is.EqualTo(count));
            Assert.That(received.Count(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.EqualTo(count));
            Assert.That(received.Where(value => value.Kind
                == SipStackEventKind.IncomingRequest).Select(value =>
                    value.CorrelationId), Is.EquivalentTo(
                        Enumerable.Range(0, count).Select(index =>
                            "receive-" + index)));
        });
    }

    [Test]
    public void ConcurrentTimerExpiryAndShutdownConvergeOnce()
    {
        const int count = 12;
        SipTransactionOptions options = new SipTransactionOptions();
        options.Timers.T1Milliseconds = 10;
        options.Timers.T2Milliseconds = 40;
        options.Timers.T4Milliseconds = 50;
        options.MaxTransactionCount = 64;
        options.MaxEventCount = 256;
        SipTransactionLayer layer = new SipTransactionLayer();
        layer.ConfigureTransactionSources(
            new SipManualMonotonicClock(0),
            new SipHarnessRandomSource(31), options);
        SipTransactionSnapshot[] transactions =
            new SipTransactionSnapshot[count];
        Parallel.For(0, count, index =>
        {
            SipTransactionOptions operation = new SipTransactionOptions();
            operation.Timers.T1Milliseconds = 10;
            operation.Timers.T2Milliseconds = 40;
            operation.Timers.T4Milliseconds = 50;
            operation.MaxTransactionCount = 64;
            operation.MaxEventCount = 256;
            transactions[index] = layer.CreateTransaction(
                Request("OPTIONS", "timer-" + index, index + 1,
                    "timer-" + index), operation);
        });
        DrainTransactions(layer);

        Parallel.For(0, count, index =>
            layer.AdvanceTransactionTime(640));
        SipTransactionEvent[] expired = DrainTransactions(layer);

        SipEndpoint endpoint = new SipEndpoint();
        endpoint.StartEndpoint(new SipStackOptions());
        DrainEndpoint(endpoint);
        SipCancellation shutdown = new SipCancellation();
        shutdown.Requested = true;
        shutdown.Id = "concurrent-shutdown";
        Parallel.For(0, count, index =>
            endpoint.StopEndpoint(shutdown));
        SipStackEvent[] stopped = DrainEndpoint(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(transactions.Select(value => value.Id).Distinct()
                .Count(), Is.EqualTo(count));
            Assert.That(expired.Count(value => value.Kind
                == SipTransactionEventKind.TimerFired),
                Is.GreaterThanOrEqualTo(count));
            Assert.That(expired.Count(value => value.Kind
                == SipTransactionEventKind.Terminated), Is.EqualTo(count));
            Assert.That(expired.Where(value => value.Kind
                == SipTransactionEventKind.Terminated).Select(value =>
                    value.TransactionId).Distinct().Count(),
                Is.EqualTo(count));
            Assert.That(endpoint.GetEndpointState(),
                Is.EqualTo(SipEndpointState.Stopped));
            Assert.That(stopped.Count(value => value.Kind
                == SipStackEventKind.Stopped), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ConcurrentRegistrarUpdatesRemainIndependentlyQueryable()
    {
        const int count = 16;
        SipLocationService location = new SipLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = false;
        options.Domains = new string[] { "example.com" };
        options.DefaultExpiresSeconds = 120;
        options.MinExpiresSeconds = 60;
        options.MaxExpiresSeconds = 600;

        SipRegistrationSnapshot[] updates = await Task.WhenAll(
            Enumerable.Range(0, count).Select(index => Task.Run(() =>
                registrar.ProcessRegistration(
                    Register("user" + index, index + 1), options))));
        SipLocationResult[] queries = await Task.WhenAll(
            Enumerable.Range(0, count).Select(index => Task.Run(() =>
            {
                SipLocationQuery query = new SipLocationQuery();
                query.AddressOfRecord = Uri(
                    "sip:user" + index + "@example.com");
                return location.QueryLocation(query);
            })));
        string updateErrors = string.Join(" | ", updates
            .Where(value => value.State != SipRegistrationState.Registered)
            .Select(value => value.ErrorMessage));

        Assert.Multiple(() =>
        {
            Assert.That(updates.All(value => value.State
                == SipRegistrationState.Registered), Is.True, updateErrors);
            Assert.That(updates.All(value => value.Bindings.Length == 1),
                Is.True);
            Assert.That(queries.All(value => value.Success), Is.True);
            Assert.That(queries.All(value => value.Bindings.Length == 1),
                Is.True);
            Assert.That(queries.Select(value =>
                value.Bindings[0].Contact!.Address!.Uri!.User).Distinct()
                .Count(), Is.EqualTo(count));
        });
    }

    [Test]
    public void ConcurrentDialogTerminationRemovesEveryIndependentDialog()
    {
        const int count = 12;
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot[] dialogs = new SipDialogSnapshot[count];
        Parallel.For(0, count, index =>
        {
            SipMessage invite = DialogInvite(index);
            SipDialogOptions options = new SipDialogOptions();
            options.MaxDialogCount = 32;
            options.MaxEventCount = 64;
            dialogs[index] = layer.CreateDialog(invite,
                DialogResponse(invite, index), options);
        });
        DrainDialogs(layer);
        Parallel.For(0, count, index =>
        {
            SipCancellation cancellation = new SipCancellation();
            cancellation.Reason = "concurrent termination " + index;
            layer.TerminateDialog(dialogs[index].Id!, cancellation);
        });
        SipDialogEvent[] terminated = DrainDialogs(layer);

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Count(value => value.Kind == "terminated"),
                Is.EqualTo(count));
            Assert.That(terminated.Select(value => value.DialogId!.CallId)
                .Distinct().Count(), Is.EqualTo(count));
            Assert.That(dialogs.All(dialog =>
                dialog.State == SipDialogState.Confirmed), Is.True);
        });
        foreach (SipDialogSnapshot dialog in dialogs)
        {
            Assert.Throws<SipDialogException>(() =>
                layer.FindDialog(dialog.Id!));
        }
    }

    [Test]
    public async Task ConcurrentProxyForkResponsesSelectOneFinalResult()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(ProxyRequest());
        DrainProxy(proxy);
        SipMessage unavailable = Response(
            started.Requests[0], 503, "Service Unavailable");
        SipMessage busy = Response(started.Requests[1], 486, "Busy Here");

        SipProxyResult[] results = await Task.WhenAll(
            Task.Run(() => proxy.ProcessProxyResponse(unavailable)),
            Task.Run(() => proxy.ProcessProxyResponse(busy)));
        SipProxyEvent[] events = DrainProxy(proxy);
        SipProxyResult completed = results.Single(value => value.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(started.Requests, Has.Length.EqualTo(2));
            Assert.That(results.All(value => value.Success), Is.True);
            Assert.That(completed.StatusCode,
                Is.EqualTo(SipStatusCodes.BusyHere));
            Assert.That(completed.ActiveBranchCount, Is.Zero);
            Assert.That(events.Count(value => value.Kind
                == SipProxyEventKind.FinalSelected), Is.EqualTo(1));
            Assert.That(events.Count(value => value.Kind
                == SipProxyEventKind.Completed), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock()
    {
        SipEndpoint endpoint = new SipEndpoint();
        ReentrantEndpointExtension extension =
            new ReentrantEndpointExtension();
        extension.Endpoint = endpoint;
        endpoint.RegisterEndpointExtension(extension);
        endpoint.StartEndpoint(new SipStackOptions());
        DrainEndpoint(endpoint);

        endpoint.ReceiveEndpointMessage(Envelope(
            Bytes(RequestText("OPTIONS", "extension-reentry", 1,
                "extension-reentry", "")), "extension-reentry"));
        SipStackEvent[] events = DrainEndpoint(endpoint);
        Stop(endpoint, "extension-stop");

        Assert.Multiple(() =>
        {
            Assert.That(extension.ReentryCompleted, Is.True);
            Assert.That(extension.ObservedState,
                Is.EqualTo(SipEndpointState.Running));
            Assert.That(events.Count(value => value.Kind
                == SipStackEventKind.Extension), Is.EqualTo(1));
            Assert.That(events.Single(value => value.Kind
                == SipStackEventKind.Extension).ExtensionResult!.Event,
                Is.EqualTo("reentered"));
        });
    }

    static SipTransactionEvent[] DrainTransactions(SipTransactionLayer layer)
    {
        List<SipTransactionEvent> result = new List<SipTransactionEvent>();
        SipTransactionEvent value = layer.PollTransactionEvent();
        while (value.Kind != SipTransactionEventKind.None)
        {
            result.Add(value);
            value = layer.PollTransactionEvent();
        }
        while (layer.PollTransactionSnapshot().State
            != SipTransactionState.None)
        {
        }
        return result.ToArray();
    }

    static SipStackEvent[] DrainEndpoint(SipEndpoint endpoint)
    {
        List<SipStackEvent> result = new List<SipStackEvent>();
        SipStackEvent value = endpoint.PollStackEvent();
        while (value.Kind != SipStackEventKind.None)
        {
            result.Add(value);
            value = endpoint.PollStackEvent();
        }
        return result.ToArray();
    }

    static SipProxyEvent[] DrainProxy(SipProxy proxy)
    {
        List<SipProxyEvent> result = new List<SipProxyEvent>();
        SipProxyEvent value = proxy.PollProxyEvent();
        while (value.Kind != SipProxyEventKind.None)
        {
            result.Add(value);
            value = proxy.PollProxyEvent();
        }
        return result.ToArray();
    }

    static SipDialogEvent[] DrainDialogs(SipDialogLayer layer)
    {
        List<SipDialogEvent> result = new List<SipDialogEvent>();
        SipDialogEvent value = layer.PollDialogEvent();
        while (value.Kind != null)
        {
            result.Add(value);
            value = layer.PollDialogEvent();
        }
        while (layer.PollDialogSnapshot().State != SipDialogState.None)
        {
        }
        return result.ToArray();
    }

    static void Stop(SipEndpoint endpoint, string id)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = id;
        endpoint.StopEndpoint(cancellation);
    }

    static SipTransportMessage Envelope(byte[] bytes, string correlationId)
    {
        SipTransportMessage result = new SipTransportMessage();
        result.Bytes = bytes;
        result.Transport = SipTransportKind.Tcp;
        result.CorrelationId = correlationId;
        return result;
    }

    static SipMessage Register(string user, int cseq)
    {
        return Parse("REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-reg-"
            + user + "\r\nMax-Forwards: 70\r\n"
            + "From: <sip:" + user + "@example.com>;tag=tag-" + user
            + "\r\nTo: <sip:" + user + "@example.com>\r\n"
            + "Call-ID: register-" + user + "\r\nCSeq: " + cseq
            + " REGISTER\r\nContact: <sip:" + user
            + "@device.example.com>;expires=120\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage DialogInvite(int index)
    {
        return Parse("INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-dialog-"
            + index + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-" + index
            + "\r\nTo: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: dialog-" + index + "\r\nCSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage DialogResponse(SipMessage request, int index)
    {
        return Parse("SIP/2.0 200 OK\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=remote-" + index + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + "Contact: <sip:bob@server.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipProxyRequest ProxyRequest()
    {
        SipMessage message = Request(
            "INVITE", "proxy-fork", 1, "proxy-fork");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.StatefulParallel;
        options.RecordRoute = true;
        options.TimerCMilliseconds = 5000;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = new SipProxyTarget[]
        {
            Target("sip:bob@one.example.com", "192.0.2.10"),
            Target("sip:bob@two.example.com", "192.0.2.20")
        };
        request.Options = options;
        request.CorrelationId = "concurrent-fork";
        return request;
    }

    static SipProxyTarget Target(string uri, string host)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = Uri(uri);
        target.Candidate = candidate;
        return target;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = host;
        result.Port = port;
        result.Transport = SipTransportKind.Udp;
        return result;
    }

    static SipMessage Response(SipMessage request, int status, string reason)
    {
        string vias = string.Concat(request.Headers.Where(header =>
            string.Equals(header.Name, SipHeaderNames.Via,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(header.Name, SipHeaderNames.CompactVia,
                StringComparison.OrdinalIgnoreCase)).Select(header =>
                    "Via: " + header.Value + "\r\n"));
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + vias
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=response-tag\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Request(string method, string callId, int cseq,
        string branch)
    {
        return Parse(RequestText(method, callId, cseq, branch, ""));
    }

    static string RequestText(string method, string callId, int cseq,
        string branch, string body)
    {
        byte[] bytes = Bytes(body);
        return method + " sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq + " "
            + method + "\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + (bytes.Length == 0 ? ""
                : "Content-Type: text/plain\r\n")
            + "Content-Length: " + bytes.Length + "\r\n\r\n" + body;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            Bytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            Bytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.First(value => string.Equals(
            value.Name, name, StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }
}

class ReentrantEndpointExtension : ISipExtension
{
    internal SipEndpoint? Endpoint;
    internal bool ReentryCompleted;
    internal SipEndpointState ObservedState;

    public SipCapabilitySet GetExtensionCapabilities()
    {
        SipCapabilitySet result = new SipCapabilitySet();
        result.Methods = new string[] { SipMethods.Options };
        return result;
    }

    public SipExtensionResult ProcessExtensionRequest(SipMessage message)
    {
        SipEndpoint endpoint = Endpoint!;
        Task<SipEndpointState> reentry = Task.Run(() =>
            endpoint.GetEndpointState());
        ReentryCompleted = reentry.Wait(2000);
        if (ReentryCompleted)
        {
            ObservedState = reentry.Result;
        }
        SipExtensionResult result = new SipExtensionResult();
        result.Handled = true;
        result.Event = "reentered";
        return result;
    }
}
