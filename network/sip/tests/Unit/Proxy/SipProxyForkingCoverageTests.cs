namespace sip.Tests;

public class SipProxyForkingCoverageTests
{
    [Test]
    public void LocationChangesAffectNewOperationsButNotActiveForks()
    {
        MutableForkingLocationService location =
            new MutableForkingLocationService();
        location.Bindings = new SipRegistrationBinding[] {
            Binding("sip:bob@old-high.example.com", "198.51.100.80", 1000),
            Binding("sip:bob@old-low.example.com", "198.51.100.81", 500) };
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyLocationService(location);

        SipProxyResult first = proxy.ProcessProxyRequest(
            Request("location-old", SipProxyMode.StatefulSequential, 8));
        DrainEvents(proxy, 2);
        location.Bindings = new SipRegistrationBinding[] {
            Binding("sip:bob@new.example.com", "198.51.100.82", 1000) };
        SipProxyResult second = proxy.ProcessProxyResponse(
            Response(first.Request!, 486));
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.BranchCreated));
        SipProxyResult oldCompleted = proxy.ProcessProxyResponse(
            Response(second.Request!, 404));
        DrainEvents(proxy, 2);

        SipProxyResult newOperation = proxy.ProcessProxyRequest(
            Request("location-new", SipProxyMode.StatefulSequential, 8));

        Assert.Multiple(() =>
        {
            Assert.That(first.Request!.RequestUri!.Host,
                Is.EqualTo("old-high.example.com"));
            Assert.That(first.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.80"));
            Assert.That(second.Request!.RequestUri!.Host,
                Is.EqualTo("old-low.example.com"));
            Assert.That(second.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.81"));
            Assert.That(oldCompleted.Completed, Is.True);
            Assert.That(newOperation.Request!.RequestUri!.Host,
                Is.EqualTo("new.example.com"));
            Assert.That(newOperation.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.82"));
            Assert.That(location.Queries, Is.EqualTo(2));
        });
    }

    [Test]
    public void DistinctOutboundFlowsPreserveFlowPathAndConnectionRouting()
    {
        MutableForkingLocationService location =
            new MutableForkingLocationService();
        SipRegistrationBinding firstBinding = Binding(
            "sip:bob@shared.example.com", null, 1000);
        firstBinding.Flow = Flow("flow-one", "connection-one",
            "203.0.113.81", "sip:path-one.example.com;lr");
        SipRegistrationBinding secondBinding = Binding(
            "sip:bob@shared.example.com", null, 1000);
        secondBinding.Flow = Flow("flow-two", "connection-two",
            "203.0.113.82", "sip:path-two.example.com;lr");
        location.Bindings = new SipRegistrationBinding[] {
            firstBinding, secondBinding };
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyLocationService(location);

        SipProxyResult result = proxy.ProcessProxyRequest(
            Request("flow-forks", SipProxyMode.StatefulParallel, 8));

        SipSendRequest firstSend = result.SendRequests.Single(send =>
            send.FlowId == "flow-one");
        SipSendRequest secondSend = result.SendRequests.Single(send =>
            send.FlowId == "flow-two");
        SipMessage firstRequest = result.Requests.Single(message =>
            RouteHost(message) == "path-one.example.com");
        SipMessage secondRequest = result.Requests.Single(message =>
            RouteHost(message) == "path-two.example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Targets, Has.Length.EqualTo(2));
            Assert.That(result.Requests, Has.Length.EqualTo(2));
            Assert.That(firstRequest.RequestUri!.Host,
                Is.EqualTo("shared.example.com"));
            Assert.That(secondRequest.RequestUri!.Host,
                Is.EqualTo("shared.example.com"));
            Assert.That(firstSend.ConnectionId, Is.EqualTo("connection-one"));
            Assert.That(firstSend.RemoteAddress!.Host,
                Is.EqualTo("203.0.113.81"));
            Assert.That(secondSend.ConnectionId, Is.EqualTo("connection-two"));
            Assert.That(secondSend.RemoteAddress!.Host,
                Is.EqualTo("203.0.113.82"));
            Assert.That(firstSend.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(secondSend.Transport, Is.EqualTo(SipTransportKind.Tcp));
        });
    }

    [Test]
    public void DeduplicatesCandidateOnlyTargetsByTransportIdentity()
    {
        SipProxyRequest request = Request(
            "candidate-duplicates", SipProxyMode.StatefulParallel, 8);
        request.Targets = new SipProxyTarget[] {
            CandidateOnlyTarget("192.0.2.91", 5060, SipTransportKind.Udp),
            CandidateOnlyTarget("192.0.2.91", 5060, SipTransportKind.Udp),
            CandidateOnlyTarget("192.0.2.91", 5060, SipTransportKind.Tcp),
            CandidateOnlyTarget("192.0.2.92", 5060, SipTransportKind.Udp) };

        SipProxyResult result = new SipProxy().ProcessProxyRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Targets, Has.Length.EqualTo(3));
            Assert.That(result.Requests, Has.Length.EqualTo(3));
            Assert.That(result.SendRequests.Count(send =>
                send.RemoteAddress!.Host == "192.0.2.91"
                    && send.Transport == SipTransportKind.Udp), Is.EqualTo(1));
            Assert.That(result.SendRequests.Count(send =>
                send.RemoteAddress!.Host == "192.0.2.91"
                    && send.Transport == SipTransportKind.Tcp), Is.EqualTo(1));
            Assert.That(result.SendRequests.Count(send =>
                send.RemoteAddress!.Host == "192.0.2.92"), Is.EqualTo(1));
        });
    }

    [Test]
    public void RedirectContactsHonorQualityExpiryDuplicatesAndBranchLimit()
    {
        ForkingCoverageResolver resolver = new ForkingCoverageResolver();
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        SipProxyRequest request = Request(
            "redirect-filter", SipProxyMode.StatefulSequential, 3);
        request.Options.MaxRedirectCount = 2;
        request.Targets = new SipProxyTarget[] {
            UriCandidateTarget("sip:bob@start.example.com",
                "192.0.2.93", 1000) };

        SipProxyResult started = proxy.ProcessProxyRequest(request);
        DrainEvents(proxy, 2);
        SipProxyResult redirected = proxy.ProcessProxyResponse(
            Response(started.Request!, 302,
                "Contact: <sip:bob@expired.example.com>;expires=0;q=1.0\r\n"
                    + "Contact: <sip:bob@high.example.com>;q=0.9\r\n"
                    + "Contact: <sip:bob@high.example.com>;q=0.8\r\n"
                    + "Contact: <sip:bob@low.example.com>;q=0.5\r\n"
                    + "Contact: <sip:bob@overflow.example.com>;q=0.4\r\n"));
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.BranchCreated));
        SipProxyResult advanced = proxy.ProcessProxyResponse(
            Response(redirected.Request!, 486));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.Success, Is.True, redirected.ErrorMessage);
            Assert.That(redirected.Targets, Has.Length.EqualTo(3));
            Assert.That(redirected.Request!.RequestUri!.Host,
                Is.EqualTo("high.example.com"));
            Assert.That(redirected.Targets.Count(target =>
                target.Uri!.Host == "high.example.com"), Is.EqualTo(1));
            Assert.That(redirected.Targets.Any(target =>
                target.Uri!.Host == "expired.example.com"), Is.False);
            Assert.That(redirected.Targets.Any(target =>
                target.Uri!.Host == "overflow.example.com"), Is.False);
            Assert.That(advanced.Request!.RequestUri!.Host,
                Is.EqualTo("low.example.com"));
            Assert.That(resolver.Requests.Select(value => value.Uri!.Host),
                Is.EqualTo(new string[] {
                    "high.example.com", "low.example.com" }));
        });
    }

    [Test]
    public void SecureRedirectSkipsDowngradeAndUsesSecureContact()
    {
        ForkingCoverageResolver resolver = new ForkingCoverageResolver();
        resolver.Secure = true;
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        SipProxyRequest request = SecureRequest("secure-redirect");
        request.Targets = new SipProxyTarget[] {
            UriCandidateTarget("sips:bob@start.example.com",
                "192.0.2.94", 1000, SipTransportKind.Tls, true) };

        SipProxyResult started = proxy.ProcessProxyRequest(request);
        DrainEvents(proxy, 2);
        SipProxyResult redirected = proxy.ProcessProxyResponse(
            Response(started.Request!, 302,
                "Contact: <sip:bob@insecure.example.com>;q=1.0\r\n"
                    + "Contact: <sips:bob@secure.example.com>;q=0.5\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.Success, Is.True, redirected.ErrorMessage);
            Assert.That(redirected.Targets, Has.Length.EqualTo(2));
            Assert.That(redirected.Targets.Any(target =>
                target.Uri!.Host == "insecure.example.com"), Is.False);
            Assert.That(redirected.Request!.RequestUri!.Host,
                Is.EqualTo("secure.example.com"));
            Assert.That(redirected.Request.RequestUri.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(redirected.SendRequest!.Transport,
                Is.EqualTo(SipTransportKind.Tls));
            Assert.That(redirected.SendRequest.RemoteAddress!.Secure, Is.True);
        });
    }

    static SipProxyRequest Request(string callId, SipProxyMode mode,
        int maxBranches)
    {
        SipMessage message = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-"
            + callId + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return ProxyRequest(message, callId, mode, maxBranches,
            Endpoint("proxy.example.com", 5060, SipTransportKind.Udp, false));
    }

    static SipProxyRequest SecureRequest(string callId)
    {
        SipMessage message = Parse(
            "INVITE sips:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TLS client.example.com:5061;branch=z9hG4bK-"
            + callId + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sips:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sips:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sips:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return ProxyRequest(message, callId, SipProxyMode.StatefulSequential,
            8, Endpoint("proxy.example.com", 5061,
                SipTransportKind.Tls, true));
    }

    static SipProxyRequest ProxyRequest(SipMessage message, string callId,
        SipProxyMode mode, int maxBranches, SipEndpointAddress local)
    {
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = local.Transport;
        transport.LocalAddress = local;
        transport.RemoteAddress = Endpoint("198.51.100.90",
            local.Port, local.Transport, local.Secure);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = mode;
        options.MaxBranchCount = maxBranches;
        options.TimerCMilliseconds = 5000;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.CorrelationId = callId;
        return request;
    }

    static SipRegistrationBinding Binding(string uri, string? receivedHost,
        int quality)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = ParseUri(uri);
        SipContactValue contact = new SipContactValue();
        contact.Address = address;
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.Contact = contact;
        binding.QualityThousandths = quality;
        if (receivedHost != null)
        {
            binding.ReceivedAddress = Endpoint(receivedHost, 5090,
                SipTransportKind.Udp, false);
        }
        return binding;
    }

    static SipFlow Flow(string flowId, string connectionId,
        string remoteHost, string path)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = connectionId;
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tcp;
        connection.RemoteAddress = Endpoint(remoteHost, 5090,
            SipTransportKind.Tcp, false);
        SipFlow flow = new SipFlow();
        flow.Id = flowId;
        flow.Connection = connection;
        flow.Path = new SipRouteValue[] { Route(path) };
        return flow;
    }

    static SipProxyTarget CandidateOnlyTarget(string host, int port,
        SipTransportKind transport)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Candidate = Candidate(host, port, transport, false);
        return target;
    }

    static SipProxyTarget UriCandidateTarget(string uri, string host,
        int quality)
    {
        return UriCandidateTarget(uri, host, quality,
            SipTransportKind.Udp, false);
    }

    static SipProxyTarget UriCandidateTarget(string uri, string host,
        int quality, SipTransportKind transport, bool secure)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        target.Candidate = Candidate(host, transport == SipTransportKind.Tls
            ? 5061 : 5060, transport, secure);
        target.QualityThousandths = quality;
        return target;
    }

    static SipServerCandidate Candidate(string host, int port,
        SipTransportKind transport, bool secure)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = port;
        candidate.Transport = transport;
        candidate.Secure = secure;
        return candidate;
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

    static SipRouteValue Route(string uri)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = ParseUri(uri);
        SipRouteValue route = new SipRouteValue();
        route.Address = address;
        return route;
    }

    static string? RouteHost(SipMessage message)
    {
        return message.RouteValues.Length == 0
            || message.RouteValues[0].Address == null
            || message.RouteValues[0].Address!.Uri == null
            ? null : message.RouteValues[0].Address!.Uri!.Host;
    }

    static SipMessage Response(SipMessage request, int statusCode)
    {
        return Response(request, statusCode, null);
    }

    static SipMessage Response(SipMessage request, int statusCode,
        string? additionalHeaders)
    {
        string vias = string.Concat(request.Headers.Where(header =>
            IsHeader(header, SipHeaderNames.Via)
                || IsHeader(header, SipHeaderNames.CompactVia))
            .Select(header => "Via: " + header.Value + "\r\n"));
        string contact = statusCode >= 200 && statusCode < 300
            ? "Contact: <sip:bob@service.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + statusCode.ToString() + " Test\r\n"
            + vias
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=to-tag\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + (additionalHeaders == null ? "" : additionalHeaders)
            + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.Single(header => IsHeader(header, name)).Value!;
    }

    static bool IsHeader(SipHeader header, string name)
    {
        return string.Equals(header.Name, name,
            System.StringComparison.OrdinalIgnoreCase);
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Uri!;
    }

    static void DrainEvents(SipProxy proxy, int count)
    {
        int index = 0;
        while (index < count)
        {
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.Not.EqualTo(SipProxyEventKind.None));
            index++;
        }
    }
}

public class MutableForkingLocationService : ISipLocationService
{
    public SipRegistrationBinding[] Bindings = new SipRegistrationBinding[0];
    public int Queries;

    public void ConfigureLocationStorage(ISipLocationStorage storage)
    {
    }

    public SipLocationResult QueryLocation(SipLocationQuery query)
    {
        Queries++;
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = Bindings;
        return result;
    }

    public SipLocationResult UpdateLocation(SipRegistrationBinding binding)
    {
        return Result();
    }

    public SipLocationResult ReplaceLocations(SipLocationUpdate update)
    {
        return Result();
    }

    public SipLocationResult RemoveLocation(SipLocationQuery query)
    {
        return Result();
    }

    public SipLocationResult ExpireLocations(long nowMilliseconds)
    {
        return Result();
    }

    static SipLocationResult Result()
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        return result;
    }
}

public class ForkingCoverageResolver : ISipDnsResolver
{
    public bool Secure;
    public System.Collections.Generic.List<SipResolutionRequest> Requests =
        new System.Collections.Generic.List<SipResolutionRequest>();

    public SipResolutionResult ResolveTarget(
        SipResolutionRequest request, SipDnsOptions options)
    {
        Requests.Add(request);
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = Secure ? "203.0.113.95" : "203.0.113.96";
        candidate.Port = Secure ? 5061 : 5060;
        candidate.Transport = Secure
            ? SipTransportKind.Tls : SipTransportKind.Udp;
        candidate.Secure = Secure;
        SipResolutionResult result = new SipResolutionResult();
        result.Status = SipResolutionStatus.Success;
        result.Candidates = new SipServerCandidate[] { candidate };
        return result;
    }

    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
    }

    public void FlushResolverCache()
    {
    }
}
