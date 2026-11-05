namespace sip.Tests;

public class SipHostileInputTests
{
    [Test]
    public void StreamFramingRejectsSmugglingAndSeparatesAmbiguousSuffix()
    {
        string start = "MESSAGE sip:bob@example.com SIP/2.0\r\n";
        string next = MinimalRequestText("OPTIONS", "next-call", 2,
            "next-branch", "");
        byte[] conflicting = Bytes(start
            + "Content-Length: 3\r\nl: 4\r\n\r\nabcX" + next);
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult rejected = codec.ParseStreamMessage(
            conflicting, new SipParseOptions());

        string framed = MinimalRequestText(
            "MESSAGE", "framed-call", 1, "framed-branch",
            "Transfer-Encoding: chunked\r\n"
                + "Content-Type: text/plain\r\n", "abc");
        byte[] pipeline = Bytes(framed + next);
        SipParseResult first = codec.ParseStreamMessage(
            pipeline, new SipParseOptions());
        SipParseResult second = codec.ParseStreamMessage(
            pipeline.Skip(first.Consumed).ToArray(), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(rejected.Message, Is.Null);
            Assert.That(first.Status, Is.EqualTo(SipParseStatus.Complete),
                first.ErrorMessage);
            Assert.That(first.Consumed, Is.EqualTo(Bytes(framed).Length));
            Assert.That(first.Message!.Body, Is.EqualTo(Bytes("abc")));
            Assert.That(second.Status, Is.EqualTo(SipParseStatus.Complete),
                second.ErrorMessage);
            Assert.That(second.Message!.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(Header(second.Message, SipHeaderNames.CallId),
                Is.EqualTo("next-call"));
        });
    }

    [Test]
    public void EndpointRejectsOversizedHeadersAndBodiesBeforeDispatch()
    {
        SipEndpoint endpoint = new SipEndpoint();
        SipStackOptions options = new SipStackOptions();
        options.ParseOptions.MaxHeaderLineBytes = 96;
        options.ParseOptions.MaxBodyBytes = 4;
        endpoint.StartEndpoint(options);
        Drain(endpoint);

        string oversizedField = MinimalRequestText(
            "MESSAGE", "large-header", 1, "large-header",
            "X-Oversized: " + new string('x', 97) + "\r\n");
        endpoint.ReceiveEndpointMessage(Envelope(
            Bytes(oversizedField), "oversized-header"));
        SipStackEvent[] headerEvents = Drain(endpoint);

        string oversizedBody = MinimalRequestText(
            "MESSAGE", "large-body", 2, "large-body",
            "Content-Type: text/plain\r\n", "12345");
        endpoint.ReceiveEndpointMessage(Envelope(
            Bytes(oversizedBody), "oversized-body"));
        SipStackEvent[] bodyEvents = Drain(endpoint);
        Stop(endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(headerEvents, Has.Length.EqualTo(1));
            Assert.That(headerEvents[0].Kind,
                Is.EqualTo(SipStackEventKind.Error));
            Assert.That(headerEvents[0].ErrorMessage,
                Does.Contain("line size"));
            Assert.That(headerEvents[0].CorrelationId,
                Is.EqualTo("oversized-header"));
            Assert.That(bodyEvents, Has.Length.EqualTo(1));
            Assert.That(bodyEvents[0].Kind,
                Is.EqualTo(SipStackEventKind.Error));
            Assert.That(bodyEvents[0].ErrorMessage,
                Does.Contain("body"));
            Assert.That(bodyEvents[0].CorrelationId,
                Is.EqualTo("oversized-body"));
        });
    }

    [Test]
    public void DnsClientRejectsCompressionPointerLoopReceivedOverUdp()
    {
        using System.Net.Sockets.UdpClient server =
            new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0));
        int port = ((System.Net.IPEndPoint)
            server.Client.LocalEndPoint!).Port;
        Task responder = Task.Run(() =>
        {
            System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                System.Net.IPAddress.Loopback, 0);
            byte[] query = server.Receive(ref remote);
            byte[] response = PointerLoopResponse(query);
            server.Send(response, response.Length, remote);
        });
        SipDnsOptions options = new SipDnsOptions();
        SipEndpointAddress nameServer = new SipEndpointAddress();
        nameServer.Host = "127.0.0.1";
        nameServer.Port = port;
        nameServer.Transport = SipTransportKind.Udp;
        options.NameServers = new SipEndpointAddress[] { nameServer };
        options.MaxAttempts = 1;
        options.TimeoutMilliseconds = 1000;

        SipDnsClientResult result = SipDnsClient.QueryRecords(
            "hostile.example", SipDnsRecordKind.A, options, null);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipResolutionStatus.Failed));
            Assert.That(result.ErrorMessage,
                Does.Contain("compression pointer"));
            Assert.That(result.Attempts, Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedMimeAndXmlBodiesAreRejectedBeforeStateIsStored()
    {
        string nestedMime =
            "Content-Type: multipart/mixed;boundary=outer\r\n\r\n"
            + "--outer\r\n"
            + "Content-Type: multipart/mixed;boundary=inner\r\n\r\n"
            + "--inner\r\nContent-Type: text/plain\r\n\r\ndata\r\n"
            + "--inner--\r\n--outer--\r\n";
        SipMessage mimeMessage = ParseDatagram(MinimalRequestText(
            "MESSAGE", "mime-depth", 1, "mime-depth",
            "Content-Type: message/rfc822\r\n", nestedMime));
        SipParseOptions mimeOptions = new SipParseOptions();
        mimeOptions.MaxMimeDepth = 1;
        SipParseResult mime = new SipMimeCodec().ParseMimeEntity(
            mimeMessage.Body, mimeOptions);

        string xml = "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\"><tuple id=\"one\">"
            + "<status><basic>open</basic></status></tuple></presence>";
        SipMessage notify = ParseDatagram(MinimalRequestText(
            "NOTIFY", "xml-depth", 1, "xml-depth",
            "Content-Type: application/pidf+xml\r\n"
                + "Event: presence\r\n"
                + "Subscription-State: active\r\n", xml));
        SipEventPackageRequest request = new SipEventPackageRequest();
        request.SubscriptionId = "hostile-presence";
        request.EventPackage = "presence";
        request.Body = notify.Body;
        request.ContentType = notify.ContentType;
        request.Options.MaxXmlDepth = 3;
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult presence = registry.ProcessEventBody(request);

        Assert.Multiple(() =>
        {
            Assert.That(mime.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(mime.ErrorMessage, Does.Contain("depth"));
            Assert.That(presence.Accepted, Is.False);
            Assert.That(presence.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(presence.ErrorMessage, Does.Contain("depth"));
            Assert.That(registry.GetEventPackageState("hostile-presence"),
                Is.Null);
        });
    }

    [Test]
    public void DigestVerificationRejectsReplayAndTamperedNonceAfterTransport()
    {
        EndToEndCredentialProvider provider =
            new EndToEndCredentialProvider();
        provider.Credential.UserName = "alice";
        provider.Credential.Secret = Bytes("correct horse battery staple");
        provider.Credential.Realm = "example.com";
        provider.Credential.Scope = SipCredentialScope.Origin;
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipMessage message = ParseDatagram(MinimalRequestText(
            "MESSAGE", "digest-hostile", 1, "digest-hostile",
            "Content-Type: text/plain\r\n", "authenticated body"));
        SipAuthenticationRequest challengeRequest =
            AuthenticationRequest(message);
        SipDigestChallenge policy = new SipDigestChallenge();
        policy.Algorithm = SipDigestAlgorithm.Sha256;
        policy.Qualities =
            new SipDigestQuality[] { SipDigestQuality.Auth };
        challengeRequest.Challenges = new SipDigestChallenge[] { policy };
        SipNonceOptions nonceOptions = new SipNonceOptions();
        nonceOptions.Realm = "example.com";
        nonceOptions.CurrentKey = Enumerable.Range(1, 32)
            .Select(value => (byte)value).ToArray();
        SipAuthenticationResult challenge =
            server.CreateAuthenticationChallenge(
                challengeRequest, nonceOptions);
        SipAuthenticationRequest authorize = AuthenticationRequest(message);
        authorize.Challenges = ReadChallenges(challenge.Message!);
        SipAuthenticationResult authorization =
            client.AuthorizeRequest(authorize);

        using SipInProcessHarness validHarness = new SipInProcessHarness();
        SipMessage received = SendRequest(validHarness,
            authorization.Message!, SipTransportKind.Tcp, "valid-digest");
        SipAuthenticationRequest verification =
            AuthenticationRequest(received);
        SipAuthenticationResult valid =
            server.VerifyAuthentication(verification);
        SipAuthenticationResult replay =
            server.VerifyAuthentication(verification);

        SipMessage tamperedMessage = CloneMessage(authorization.Message!);
        TamperNonce(tamperedMessage);
        using SipInProcessHarness tamperedHarness =
            new SipInProcessHarness();
        SipMessage receivedTampered = SendRequest(tamperedHarness,
            tamperedMessage, SipTransportKind.Tcp, "tampered-digest");
        SipAuthenticationResult tampered = server.VerifyAuthentication(
            AuthenticationRequest(receivedTampered));

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(valid.Identity, Is.EqualTo("alice"));
            Assert.That(replay.Status,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(replay.Identity, Is.Null);
            Assert.That(tampered.Status,
                Is.EqualTo(SipAuthenticationStatus.InvalidCredentials));
            Assert.That(tampered.Identity, Is.Null);
        });
    }

    [Test]
    public void QueuePressureIsBoundedAndProxyReturnsOverloadResponse()
    {
        SipEndpoint endpoint = new SipEndpoint();
        SipStackOptions endpointOptions = new SipStackOptions();
        endpointOptions.MaxEventCount = 2;
        endpoint.StartEndpoint(endpointOptions);
        Drain(endpoint);
        endpoint.ReceiveEndpointMessage(Envelope(Bytes(MinimalRequestText(
            "OPTIONS", "queue-one", 1, "queue-one", "")), "queue-one"));
        endpoint.ReceiveEndpointMessage(Envelope(Bytes(MinimalRequestText(
            "OPTIONS", "queue-two", 1, "queue-two", "")), "queue-two"));
        SipStackEvent[] pressure = Drain(endpoint);
        Stop(endpoint);

        using SipInProcessHarness harness = new SipInProcessHarness();
        SipProxySafetyOptions safety = new SipProxySafetyOptions();
        safety.MaxTransactionCount = 1;
        safety.MaxBranchCount = 4;
        safety.MaxEventCount = 10;
        safety.MaxRequestCount = 10;
        safety.RequestWindowMilliseconds = 1000;
        safety.RetryAfterSeconds = 2;
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxySafety(safety);
        SipMessage firstMessage = SendRequest(harness,
            ParseDatagram(InviteText("proxy-one", "proxy-one")),
            SipTransportKind.Udp, "proxy-one");
        SipProxyResult admitted = proxy.ProcessProxyRequest(
            ProxyRequest(firstMessage, "proxy-one"));
        SipMessage secondMessage = SendRequest(harness,
            ParseDatagram(InviteText("proxy-two", "proxy-two")),
            SipTransportKind.Udp, "proxy-two");
        SipProxyResult overloaded = proxy.ProcessProxyRequest(
            ProxyRequest(secondMessage, "proxy-two"));
        SipMessage overloadResponse = ReturnResponse(harness,
            overloaded.Response!, SipTransportKind.Udp, "overload-response");

        Assert.Multiple(() =>
        {
            Assert.That(pressure, Has.Length.EqualTo(2));
            Assert.That(pressure.Any(value =>
                value.Kind == SipStackEventKind.Error
                && value.ErrorMessage != null
                && value.ErrorMessage.Contains("queue limit")), Is.True);
            Assert.That(admitted.Success, Is.True, admitted.ErrorMessage);
            Assert.That(admitted.Completed, Is.False);
            Assert.That(overloaded.Success, Is.True);
            Assert.That(overloaded.Completed, Is.True);
            Assert.That(overloadResponse.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(Header(overloadResponse, SipHeaderNames.RetryAfter),
                Is.EqualTo("2"));
        });
    }

    static SipTransportMessage Envelope(byte[] bytes, string correlationId)
    {
        SipTransportMessage result = new SipTransportMessage();
        result.Bytes = bytes;
        result.Transport = SipTransportKind.Tcp;
        result.CorrelationId = correlationId;
        return result;
    }

    static SipStackEvent[] Drain(SipEndpoint endpoint)
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

    static void Stop(SipEndpoint endpoint)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        endpoint.StopEndpoint(cancellation);
    }

    static byte[] PointerLoopResponse(byte[] query)
    {
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        AddUInt16(response, 0x8180);
        AddUInt16(response, 1);
        AddUInt16(response, 1);
        AddUInt16(response, 0);
        AddUInt16(response, 0);
        response.AddRange(query.Skip(12));
        int ownerOffset = response.Count;
        AddPointer(response, ownerOffset);
        AddUInt16(response, 1);
        AddUInt16(response, 1);
        AddUInt32(response, 60);
        AddUInt16(response, 4);
        response.AddRange(new byte[] { 192, 0, 2, 10 });
        return response.ToArray();
    }

    static void AddPointer(List<byte> bytes, int offset)
    {
        bytes.Add((byte)(0xc0 | offset >> 8));
        bytes.Add((byte)offset);
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static SipAuthenticationRequest AuthenticationRequest(SipMessage message)
    {
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = message;
        request.Target = message.RequestUri;
        request.Body = message.Body.ToArray();
        request.Scope = SipCredentialScope.Origin;
        request.CorrelationId = "hostile-digest";
        return request;
    }

    static SipDigestChallenge[] ReadChallenges(SipMessage message)
    {
        List<SipDigestChallenge> result = new List<SipDigestChallenge>();
        foreach (SipHeader header in message.Headers)
        {
            if (!string.Equals(header.Name,
                SipHeaderNames.WwwAuthenticate,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            SipAuthenticationHeaderResult parsed =
                SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                    header.Name, header.Value, new SipParseOptions());
            Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
            result.Add(parsed.Challenge!);
        }
        return result.ToArray();
    }

    static void TamperNonce(SipMessage message)
    {
        SipHeader header = message.Headers.First(value => string.Equals(
            value.Name, SipHeaderNames.Authorization,
            StringComparison.OrdinalIgnoreCase));
        string value = header.Value!;
        int start = value.IndexOf("nonce=\"", StringComparison.Ordinal) + 7;
        char replacement = value[start] == 'A' ? 'B' : 'A';
        header.Value = value.Substring(0, start) + replacement
            + value.Substring(start + 1);
        header.OriginalBytes = new byte[0];
    }

    static SipMessage CloneMessage(SipMessage message)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.ReconcileContentLength = true;
        SipWriteResult written = new SipMessageCodec().SerializeMessage(
            message, options);
        Assert.That(written.Success, Is.True, written.ErrorMessage);
        return ParseDatagram(
            System.Text.Encoding.UTF8.GetString(written.Bytes));
    }

    static SipMessage SendRequest(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUac(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas).First(value =>
            value.Kind == SipStackEventKind.IncomingRequest).Message!;
    }

    static SipMessage ReturnResponse(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUas(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac).First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null
            && value.TransactionEvent.Message.Kind == SipMessageKind.Response)
            .TransactionEvent!.Message!;
    }

    static SipProxyRequest ProxyRequest(SipMessage message,
        string correlationId)
    {
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = "192.0.2.80";
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = Uri("sip:bob@target.example.com");
        target.Candidate = candidate;
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.Stateless;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = new SipProxyTarget[] { target };
        request.Options = options;
        request.CorrelationId = correlationId;
        return request;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = host;
        result.Port = port;
        result.Transport = SipTransportKind.Udp;
        return result;
    }

    static string InviteText(string callId, string branch)
    {
        return "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-"
            + branch + ";rport\r\nMax-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n";
    }

    static string MinimalRequestText(string method, string callId, int cseq,
        string branch, string extraHeaders)
    {
        return MinimalRequestText(method, callId, cseq, branch,
            extraHeaders, "");
    }

    static string MinimalRequestText(string method, string callId, int cseq,
        string branch, string extraHeaders, string body)
    {
        return method + " sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq + " "
            + method + "\r\n" + extraHeaders + "Content-Length: "
            + Bytes(body).Length + "\r\n\r\n" + body;
    }

    static SipMessage ParseDatagram(string text)
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
