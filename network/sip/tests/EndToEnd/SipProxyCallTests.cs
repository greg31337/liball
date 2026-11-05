namespace sip.Tests;

public class SipProxyCallTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void StatelessProxyForwardsLooseAndStrictRoutesEndToEnd(
        bool strict)
    {
        using SipInProcessHarness upstream = new SipInProcessHarness();
        using SipInProcessHarness downstream = new SipInProcessHarness();
        SipProxy proxy = new SipProxy();
        SipMessage source = ReceiveRequest(upstream,
            RoutedOptions(strict), SipTransportKind.Tcp, "stateless-in");
        SipProxyRequest operation = ProxyRequest(source,
            SipProxyMode.Stateless, SipTransportKind.Tcp);
        operation.Targets = new SipProxyTarget[] {
            Target(null, "192.0.2.40", SipTransportKind.Tcp, 1000) };

        SipProxyResult forwarded = proxy.ProcessProxyRequest(operation);
        SipMessage received = SendProxyRequest(downstream,
            forwarded.SendRequest!, "stateless-out");
        SipMessage branchResponse = ReturnResponse(downstream,
            Response(received, 200, "OK", "stateless-tag", null),
            SipTransportKind.Tcp, "stateless-branch-ok");
        SipProxyResult routed = proxy.ProcessProxyResponse(branchResponse);
        SipMessage callerResponse = SendProxyResponse(upstream,
            routed.SendRequest!, "stateless-upstream-ok");

        Assert.Multiple(() =>
        {
            Assert.That(forwarded.Success, Is.True, forwarded.ErrorMessage);
            Assert.That(received.RequestUri!.Host,
                Is.EqualTo(strict ? "strict.example.com" : "uas.test"));
            Assert.That(received.RouteValues.Select(RouteHost),
                Is.EqualTo(strict
                    ? new string[] { "uas.test" }
                    : new string[] { "edge.example.com" }));
            Assert.That(received.ViaValues, Has.Length.EqualTo(2));
            Assert.That(received.ViaValues[0].Host,
                Is.EqualTo("proxy.example.com"));
            Assert.That(received.RecordRouteValues, Has.Length.EqualTo(1));
            Assert.That(callerResponse.StatusCode, Is.EqualTo(200));
            Assert.That(callerResponse.ViaValues, Has.Length.EqualTo(1));
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
        });
    }

    [Test]
    public void StatefulForkHandlesCancelAckRaceAndMultipleSuccesses()
    {
        using SipInProcessHarness upstream = new SipInProcessHarness();
        using SipInProcessHarness downstreamOne = new SipInProcessHarness();
        using SipInProcessHarness downstreamTwo = new SipInProcessHarness();
        SipProxy proxy = new SipProxy();
        SipMessage invite = ReceiveRequest(upstream, Invite("fork-call", 1),
            SipTransportKind.Udp, "fork-invite");
        SipProxyRequest operation = ProxyRequest(invite,
            SipProxyMode.StatefulParallel, SipTransportKind.Udp);
        operation.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.51",
                SipTransportKind.Udp, 1000),
            Target("sip:bob@two.example.com", "192.0.2.52",
                SipTransportKind.Udp, 1000) };

        SipProxyResult forked = proxy.ProcessProxyRequest(operation);
        SipMessage firstInvite = SendProxyRequest(downstreamOne,
            forked.SendRequests[0], "fork-one");
        SipMessage secondInvite = SendProxyRequest(downstreamTwo,
            forked.SendRequests[1], "fork-two");
        SipMessage ringing = ReturnResponse(downstreamTwo,
            Response(secondInvite, 180, "Ringing", "two-tag", null),
            SipTransportKind.Udp, "fork-ringing");
        SipProxyResult provisional = proxy.ProcessProxyResponse(ringing);
        SipMessage callerRinging = SendProxyResponse(upstream,
            provisional.SendRequest!, "fork-upstream-ringing");

        SipMessage accepted = ReturnResponse(downstreamOne,
            Response(firstInvite, 200, "OK", "one-tag",
                "Contact: <sip:bob@one.example.com>\r\n"),
            SipTransportKind.Udp, "fork-one-ok");
        SipProxyResult firstSuccess = proxy.ProcessProxyResponse(accepted);
        SipMessage callerSuccess = SendProxyResponse(upstream,
            firstSuccess.SendRequest!, "fork-upstream-one-ok");
        SipSendRequest cancelSend = firstSuccess.SendRequests.Single(value =>
            value.Message!.Method == SipMethod.Cancel);
        SipMessage branchCancel = SendProxyRequest(downstreamTwo,
            cancelSend, "fork-cancel-two");

        SipMessage ack = ReceiveRequest(upstream,
            Ack("fork-call", "one-tag", "sip:bob@one.example.com"),
            SipTransportKind.Udp, "fork-ack");
        SipProxyRequest ackOperation = ProxyRequest(ack,
            SipProxyMode.StatefulParallel, SipTransportKind.Udp);
        ackOperation.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.51",
                SipTransportKind.Udp, 1000) };
        SipProxyResult forwardedAck = proxy.ProcessProxyRequest(ackOperation);
        SipMessage receivedAck = SendProxyRequest(downstreamOne,
            forwardedAck.SendRequest!, "fork-ack-out");

        SipMessage lateAccepted = ReturnResponse(downstreamTwo,
            Response(secondInvite, 200, "OK", "two-tag",
                "Contact: <sip:bob@two.example.com>\r\n"),
            SipTransportKind.Udp, "fork-two-ok");
        SipProxyResult additional =
            proxy.ProcessProxyResponse(lateAccepted);
        List<SipProxyEvent> events = DrainProxyEvents(proxy);

        Assert.Multiple(() =>
        {
            Assert.That(forked.Requests, Has.Length.EqualTo(2));
            Assert.That(callerRinging.StatusCode, Is.EqualTo(180));
            Assert.That(callerSuccess.StatusCode, Is.EqualTo(200));
            Assert.That(firstSuccess.Completed, Is.True);
            Assert.That(branchCancel.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(branchCancel.ViaValues[0].Branch,
                Is.EqualTo(secondInvite.ViaValues[0].Branch));
            Assert.That(forwardedAck.Completed, Is.True);
            Assert.That(receivedAck.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(receivedAck.RequestUri!.Host,
                Is.EqualTo("one.example.com"));
            Assert.That(additional.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(additional.SendRequest, Is.Not.Null);
            Assert.That(events.Any(value => value.Kind
                == SipProxyEventKind.CancelForwarded), Is.True);
            Assert.That(events.Any(value => value.Kind
                == SipProxyEventKind.AdditionalSuccessForwarded), Is.True);
        });
    }

    [Test]
    public void StatefulProxyFollowsRecursiveRedirectsEndToEnd()
    {
        using SipInProcessHarness upstream = new SipInProcessHarness();
        using SipInProcessHarness downstreamStart = new SipInProcessHarness();
        using SipInProcessHarness downstreamNext = new SipInProcessHarness();
        using SipInProcessHarness downstreamFinal = new SipInProcessHarness();
        EndToEndProxyResolver resolver = new EndToEndProxyResolver();
        resolver.Candidates = new SipServerCandidate[] {
            Candidate("192.0.2.60", SipTransportKind.Udp) };
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        SipMessage invite = ReceiveRequest(upstream,
            Invite("redirect-call", 1), SipTransportKind.Udp, "redirect-invite");
        SipProxyRequest operation = ProxyRequest(invite,
            SipProxyMode.StatefulSequential, SipTransportKind.Udp);
        operation.Options.MaxRedirectCount = 2;
        operation.Targets = new SipProxyTarget[] {
            Target("sip:bob@start.example.com", "192.0.2.61",
                SipTransportKind.Udp, 1000) };

        SipProxyResult initial = proxy.ProcessProxyRequest(operation);
        SipMessage start = SendProxyRequest(downstreamStart,
            initial.SendRequest!, "redirect-start");
        SipMessage firstRedirectResponse = ReturnResponse(downstreamStart,
            Response(start, 302,
                "Moved Temporarily", "start-tag",
                "Contact: <sip:bob@next.example.com>\r\n"),
                SipTransportKind.Udp, "redirect-first");
        Assert.That(firstRedirectResponse.ContactValues, Has.Length.EqualTo(1));
        SipProxyResult firstRedirect =
            proxy.ProcessProxyResponse(firstRedirectResponse);
        Assert.That(firstRedirect.Requests.Any(value =>
            value.Method == SipMethod.Invite), Is.True,
            firstRedirect.ErrorMessage);
        SipSendRequest nextSend = firstRedirect.SendRequests.Single(value =>
            value.Message!.Method == SipMethod.Invite);
        SipMessage next = SendProxyRequest(downstreamNext,
            nextSend, "redirect-next");
        SipProxyResult secondRedirect = proxy.ProcessProxyResponse(
            ReturnResponse(downstreamNext, Response(next, 302,
                "Moved Temporarily", "next-tag",
                "Contact: <sip:bob@final.example.com>\r\n"),
                SipTransportKind.Udp, "redirect-second"));
        SipSendRequest finalSend = secondRedirect.SendRequests.Single(value =>
            value.Message!.Method == SipMethod.Invite);
        SipMessage finalInvite = SendProxyRequest(downstreamFinal,
            finalSend, "redirect-final");
        SipProxyResult completed = proxy.ProcessProxyResponse(
            ReturnResponse(downstreamFinal, Response(finalInvite, 200, "OK",
                "final-tag", "Contact: <sip:bob@final.example.com>\r\n"),
                SipTransportKind.Udp, "redirect-ok"));
        SipMessage callerResponse = SendProxyResponse(upstream,
            completed.SendRequest!, "redirect-upstream-ok");

        Assert.Multiple(() =>
        {
            Assert.That(start.RequestUri!.Host,
                Is.EqualTo("start.example.com"));
            Assert.That(next.RequestUri!.Host,
                Is.EqualTo("next.example.com"));
            Assert.That(finalInvite.RequestUri!.Host,
                Is.EqualTo("final.example.com"));
            Assert.That(secondRedirect.Targets.Any(value =>
                value.RedirectDepth == 2), Is.True);
            Assert.That(completed.Completed, Is.True);
            Assert.That(callerResponse.StatusCode, Is.EqualTo(200));
            Assert.That(resolver.ResolveCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void AuthenticatedSipsProxySkipsInsecureCandidateAndUsesTls()
    {
        using SipInProcessHarness upstream = new SipInProcessHarness();
        using SipInProcessHarness downstream = new SipInProcessHarness();
        EndToEndProxyResolver resolver = new EndToEndProxyResolver();
        resolver.Candidates = new SipServerCandidate[] {
            Candidate("192.0.2.70", SipTransportKind.Udp),
            Candidate("192.0.2.71", SipTransportKind.Tls) };
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        EndToEndProxyCredentialProvider credentials =
            new EndToEndProxyCredentialProvider();
        SipNonceOptions nonce = new SipNonceOptions();
        nonce.Realm = "proxy.example.com";
        nonce.CurrentKey = Enumerable.Range(1, 32)
            .Select(value => (byte)value).ToArray();
        proxy.ConfigureProxySecurity(new SipAuthenticationServer(credentials),
            new EndToEndProxyAuthorizationPolicy(), nonce);
        SipMessage options = ReceiveRequest(upstream, SecureOptions(),
            SipTransportKind.Tls, "sips-in");
        SipProxyRequest operation = ProxyRequest(options,
            SipProxyMode.Stateless, SipTransportKind.Tls);
        operation.TransportMessage!.Connection = TrustedConnection();

        SipProxyResult forwarded = proxy.ProcessProxyRequest(operation);
        SipMessage received = SendProxyRequest(downstream,
            forwarded.SendRequest!, "sips-out");
        SipProxyResult completed = proxy.ProcessProxyResponse(
            ReturnResponse(downstream,
                Response(received, 200, "OK", "secure-tag", null),
                SipTransportKind.Tls, "sips-ok"));
        SipMessage callerResponse = SendProxyResponse(upstream,
            completed.SendRequest!, "sips-upstream-ok");

        Assert.Multiple(() =>
        {
            Assert.That(forwarded.Success, Is.True, forwarded.ErrorMessage);
            Assert.That(forwarded.AuthenticatedIdentity,
                Is.EqualTo("edge.example.com"));
            Assert.That(forwarded.SendRequest!.Transport,
                Is.EqualTo(SipTransportKind.Tls));
            Assert.That(forwarded.SendRequest.RemoteAddress!.Host,
                Is.EqualTo("192.0.2.71"));
            Assert.That(received.RequestUri!.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(received.ViaValues[0].Transport,
                Is.EqualTo(SipTransportNames.Tls));
            Assert.That(callerResponse.StatusCode, Is.EqualTo(200));
            Assert.That(resolver.ResolveCount, Is.EqualTo(1));
        });
    }

    static SipMessage ReceiveRequest(SipInProcessHarness harness,
        SipMessage request, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUac(
            request, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas).First(value =>
            value.Kind == SipStackEventKind.IncomingRequest).Message!;
    }

    static SipMessage SendProxyRequest(SipInProcessHarness harness,
        SipSendRequest request, string correlationId)
    {
        request.CorrelationId = correlationId;
        SipSendResult sent = harness.Uac.SendEndpointMessage(request);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas).First(value =>
            value.Kind == SipStackEventKind.IncomingRequest).Message!;
    }

    static SipMessage ReturnResponse(SipInProcessHarness harness,
        SipMessage response, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUas(
            response, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac).First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null
            && value.TransactionEvent.Message.Kind
                == SipMessageKind.Response)
            .TransactionEvent!.Message!;
    }

    static SipMessage SendProxyResponse(SipInProcessHarness harness,
        SipSendRequest request, string correlationId)
    {
        request.CorrelationId = correlationId;
        request.ConnectionId = null;
        SipSendResult sent = harness.Uas.SendEndpointMessage(request);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac).First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null
            && value.TransactionEvent.Message.Kind
                == SipMessageKind.Response)
            .TransactionEvent!.Message!;
    }

    static SipProxyRequest ProxyRequest(SipMessage message,
        SipProxyMode mode, SipTransportKind transport)
    {
        SipTransportMessage envelope = new SipTransportMessage();
        envelope.Message = message;
        envelope.Transport = transport;
        bool secure = transport == SipTransportKind.Tls;
        envelope.LocalAddress = Endpoint("proxy.example.com",
            secure ? 5061 : 5060, transport, secure);
        envelope.RemoteAddress = Endpoint("198.51.100.10",
            secure ? 62001 : 62000, transport, secure);
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = envelope;
        request.Options.Mode = mode;
        request.Options.RecordRoute = true;
        request.Options.TimerCMilliseconds = 5000;
        request.CorrelationId = "proxy-end-to-end";
        return request;
    }

    static SipProxyTarget Target(string? uri, string host,
        SipTransportKind transport, int quality)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = uri == null ? null : Uri(uri);
        target.Candidate = Candidate(host, transport);
        target.QualityThousandths = quality;
        return target;
    }

    static SipServerCandidate Candidate(string host,
        SipTransportKind transport)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = transport == SipTransportKind.Tls ? 5061 : 5060;
        candidate.Transport = transport;
        candidate.Secure = transport == SipTransportKind.Tls;
        return candidate;
    }

    static SipConnectionInfo TrustedConnection()
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = "trusted-tls";
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tls;
        connection.Secure = true;
        connection.PeerIdentity = "edge.example.com";
        return connection;
    }

    static SipEndpointAddress Endpoint(string host, int port,
        SipTransportKind transport, bool secure)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = transport;
        endpoint.Secure = secure;
        return endpoint;
    }

    static SipMessage RoutedOptions(bool strict)
    {
        string requestUri = strict
            ? "sip:proxy.example.com:5060" : "sip:service@uas.test";
        string routes = strict
            ? "Route: <sip:strict.example.com>, <sip:service@uas.test>\r\n"
            : "Route: <sip:proxy.example.com:5060;lr>, "
                + "<sip:edge.example.com;lr>\r\n";
        return Parse("OPTIONS " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP caller.example.com:5060;"
            + "branch=z9hG4bK-stateless;rport\r\n"
            + routes + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: stateless-route@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Invite(string callId, int sequence)
    {
        return Parse("INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;"
            + "branch=z9hG4bK-" + callId + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: " + sequence + " INVITE\r\n"
            + "Contact: <sip:alice@caller.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Ack(string callId, string toTag, string requestUri)
    {
        return Parse("ACK " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com:5060;"
            + "branch=z9hG4bK-ack\r\nMax-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 ACK\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage SecureOptions()
    {
        return Parse("OPTIONS sips:service@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/TLS caller.example.com:5061;"
            + "branch=z9hG4bK-secure;rport\r\nMax-Forwards: 10\r\n"
            + "From: Alice <sips:alice@example.com>;tag=secure-from\r\n"
            + "To: Service <sips:service@example.com>\r\n"
            + "Call-ID: secure-proxy@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Response(SipMessage request, int status, string reason,
        string toTag, string? extraHeaders)
    {
        string vias = string.Concat(request.Headers.Where(header =>
            string.Equals(header.Name, SipHeaderNames.Via,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(header.Name, SipHeaderNames.CompactVia,
                StringComparison.OrdinalIgnoreCase))
            .Select(header => "Via: " + header.Value + "\r\n"));
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n" + vias
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=" + toTag + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + (extraHeaders ?? "")
            + "Content-Length: 0\r\n\r\n");
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.First(value => string.Equals(
            value.Name, name, StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static string RouteHost(SipRouteValue route)
    {
        return route.Address!.Uri!.Host!;
    }

    static List<SipProxyEvent> DrainProxyEvents(SipProxy proxy)
    {
        List<SipProxyEvent> events = new List<SipProxyEvent>();
        SipProxyEvent value = proxy.PollProxyEvent();
        while (value.Kind != SipProxyEventKind.None)
        {
            events.Add(value);
            value = proxy.PollProxyEvent();
        }
        return events;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }
}

class EndToEndProxyResolver : ISipDnsResolver
{
    internal SipServerCandidate[] Candidates = new SipServerCandidate[0];
    internal int ResolveCount;

    public SipResolutionResult ResolveTarget(
        SipResolutionRequest request, SipDnsOptions options)
    {
        ResolveCount++;
        SipResolutionResult result = new SipResolutionResult();
        result.Status = SipResolutionStatus.Success;
        result.Candidates = Candidates;
        result.CorrelationId = request.CorrelationId;
        return result;
    }

    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
    }

    public void FlushResolverCache()
    {
    }
}

class EndToEndProxyCredentialProvider : ISipCredentialProvider
{
    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        SipCredential credential = new SipCredential();
        credential.UserName = "edge";
        credential.Realm = "proxy.example.com";
        credential.Secret = System.Text.Encoding.UTF8.GetBytes("password");
        credential.Scope = SipCredentialScope.Proxy;
        return credential;
    }
}

class EndToEndProxyAuthorizationPolicy : ISipAuthorizationPolicy
{
    public SipAuthorizationDecision AuthorizeOperation(
        SipAuthenticationRequest request,
        SipCertificateIdentity? certificate, SipBearerToken? token)
    {
        SipAuthorizationDecision decision = new SipAuthorizationDecision();
        decision.Allowed = true;
        decision.Identity = "edge.example.com";
        return decision;
    }
}
