namespace sip.Tests;

public class SipProxyForkingTests
{
    [Test]
    public void ParallelPolicyForksEqualQualityThenAdvancesAndDeduplicates()
    {
        SipProxy proxy = new SipProxy();
        ForkingRoutePolicy policy = new ForkingRoutePolicy();
        policy.Decision.Mode = SipProxyMode.StatefulParallel;
        policy.Decision.RecordRoute = true;
        policy.Decision.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1", 1000),
            Target("sip:bob@one.example.com", "192.0.2.9", 900),
            Target("sip:bob@two.example.com", "192.0.2.2", 1000),
            Target("sip:bob@three.example.com", "192.0.2.3", 500) };
        proxy.ConfigureProxyRoutePolicy(policy);

        SipProxyResult started = proxy.ProcessProxyRequest(
            Request(SipProxyMode.StatefulSequential, 8));
        DrainEvents(proxy, 3);
        SipProxyResult waiting = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[0], 486, "Busy Here", null));
        SipProxyResult advanced = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[1], 480,
                "Temporarily Unavailable", null));
        SipProxyEvent nextBranch = proxy.PollProxyEvent();

        SipProxy limited = new SipProxy();
        limited.ConfigureProxyRoutePolicy(policy);
        SipProxyResult rejected = limited.ProcessProxyRequest(
            Request(SipProxyMode.StatefulParallel, 2));

        Assert.Multiple(() =>
        {
            Assert.That(policy.Request, Is.Not.Null);
            Assert.That(started.Success, Is.True, started.ErrorMessage);
            Assert.That(started.Requests, Has.Length.EqualTo(2));
            Assert.That(started.Targets, Has.Length.EqualTo(3));
            Assert.That(started.Requests.Select(message =>
                message.RequestUri!.Host),
                Is.EquivalentTo(new string[] {
                    "one.example.com", "two.example.com" }));
            Assert.That(waiting.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(waiting.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(advanced.Requests.Count(message =>
                message.Method == SipMethod.Invite), Is.EqualTo(1));
            Assert.That(advanced.Requests.Count(message =>
                message.Method == SipMethod.Ack), Is.EqualTo(1));
            Assert.That(advanced.Request!.RequestUri!.Host,
                Is.EqualTo("three.example.com"));
            Assert.That(advanced.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(nextBranch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
        });
    }

    [Test]
    public void SequentialForkingActivatesOneBranchAtATime()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest request = Request(
            SipProxyMode.StatefulSequential, 8);
        request.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1", 1000),
            Target("sip:bob@two.example.com", "192.0.2.2", 1000),
            Target("sip:bob@three.example.com", "192.0.2.3", 500) };

        SipProxyResult first = proxy.ProcessProxyRequest(request);
        DrainEvents(proxy, 2);
        SipProxyResult second = proxy.ProcessProxyResponse(
            ResponseFor(first.Request!, 486, "Busy Here", null));
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.BranchCreated));
        SipProxyResult third = proxy.ProcessProxyResponse(
            ResponseFor(second.Request!, 480,
                "Temporarily Unavailable", null));
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.BranchCreated));
        SipProxyResult completed = proxy.ProcessProxyResponse(
            ResponseFor(third.Request!, 404, "Not Found", null));

        Assert.Multiple(() =>
        {
            Assert.That(first.Requests, Has.Length.EqualTo(1));
            Assert.That(first.Request!.RequestUri!.Host,
                Is.EqualTo("one.example.com"));
            Assert.That(second.Requests.Count(message =>
                message.Method == SipMethod.Invite), Is.EqualTo(1));
            Assert.That(second.Request!.RequestUri!.Host,
                Is.EqualTo("two.example.com"));
            Assert.That(third.Requests.Count(message =>
                message.Method == SipMethod.Invite), Is.EqualTo(1));
            Assert.That(third.Request!.RequestUri!.Host,
                Is.EqualTo("three.example.com"));
            Assert.That(completed.Completed, Is.True);
            Assert.That(completed.StatusCode, Is.EqualTo(404));
            Assert.That(completed.ActiveBranchCount, Is.Zero);
        });
    }

    [Test]
    public void LocationBindingsSupplyOrderedTargetsAndReceivedDestinations()
    {
        SipProxy proxy = new SipProxy();
        ForkingLocationService location = new ForkingLocationService();
        location.Result.Success = true;
        location.Result.Bindings = new SipRegistrationBinding[] {
            Binding("sip:bob@low.example.com", "198.51.100.10", 500),
            Binding("sip:bob@high.example.com", "198.51.100.20", 1000) };
        proxy.ConfigureProxyLocationService(location);

        SipProxyResult started = proxy.ProcessProxyRequest(
            Request(SipProxyMode.StatefulParallel, 8));

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True, started.ErrorMessage);
            Assert.That(location.Query!.AddressOfRecord!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(location.Query.NowMilliseconds, Is.Zero);
            Assert.That(started.Targets, Has.Length.EqualTo(2));
            Assert.That(started.Requests, Has.Length.EqualTo(1));
            Assert.That(started.Request!.RequestUri!.Host,
                Is.EqualTo("high.example.com"));
            Assert.That(started.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.20"));
        });
    }

    [Test]
    public void RedirectTargetsRecurseOnceDeduplicateAndHonorDepthLimit()
    {
        ProxyResolver resolver = new ProxyResolver();
        resolver.Candidate = Candidate("203.0.113.50");
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        SipProxyRequest request = Request(
            SipProxyMode.StatefulSequential, 4);
        request.Options.MaxRedirectCount = 1;
        request.Targets = new SipProxyTarget[] {
            Target("sip:bob@start.example.com", "192.0.2.1", 1000) };

        SipProxyResult started = proxy.ProcessProxyRequest(request);
        DrainEvents(proxy, 2);
        SipProxyResult redirected = proxy.ProcessProxyResponse(
            ResponseFor(started.Request!, 302, "Moved Temporarily",
                "Contact: <sip:bob@next.example.com>;q=1.0, "
                + "<sip:bob@next.example.com>;q=0.5\r\n"));
        SipProxyEvent created = proxy.PollProxyEvent();
        SipProxyResult depthLimited = proxy.ProcessProxyResponse(
            ResponseFor(redirected.Request!, 302, "Moved Temporarily",
                "Contact: <sip:bob@third.example.com>\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(redirected.Success, Is.True,
                redirected.ErrorMessage);
            Assert.That(redirected.Requests.Count(message =>
                message.Method == SipMethod.Invite), Is.EqualTo(1));
            Assert.That(redirected.Request!.RequestUri!.Host,
                Is.EqualTo("next.example.com"));
            Assert.That(redirected.Targets, Has.Length.EqualTo(2));
            Assert.That(redirected.Targets[1].RedirectDepth, Is.EqualTo(1));
            Assert.That(created.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(depthLimited.Completed, Is.True);
            Assert.That(depthLimited.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(depthLimited.Targets, Has.Length.EqualTo(2));
            Assert.That(depthLimited.StatusCode, Is.EqualTo(302));
        });
    }

    static SipProxyRequest Request(SipProxyMode mode, int maxBranches)
    {
        SipMessage message = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: forking-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.2", 62000);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = mode;
        options.MaxBranchCount = maxBranches;
        options.TimerCMilliseconds = 5000;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.CorrelationId = "forking-correlation";
        return request;
    }

    static SipProxyTarget Target(string uri, string host, int quality)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        target.Candidate = Candidate(host);
        target.QualityThousandths = quality;
        return target;
    }

    static SipRegistrationBinding Binding(
        string uri, string receivedHost, int quality)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = ParseUri(uri);
        SipContactValue contact = new SipContactValue();
        contact.Address = address;
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.Contact = contact;
        binding.QualityThousandths = quality;
        binding.ReceivedAddress = Endpoint(receivedHost, 5090);
        return binding;
    }

    static SipServerCandidate Candidate(string host)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        return candidate;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
    }

    static SipMessage ResponseFor(SipMessage request, int status,
        string reason, string? extraHeaders)
    {
        string[] vias = request.Headers.Where(header =>
            header.Name!.Equals(SipHeaderNames.Via,
                System.StringComparison.OrdinalIgnoreCase)
            || header.Name.Equals(SipHeaderNames.CompactVia,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => "Via: " + header.Value + "\r\n").ToArray();
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + string.Concat(vias)
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: forking-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + (extraHeaders == null ? "" : extraHeaders)
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
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
            index = index + 1;
        }
    }
}

public class ForkingRoutePolicy : ISipRoutePolicy
{
    public SipRouteDecision Decision = new SipRouteDecision();
    public SipProxyRequest? Request;

    public SipRouteDecision ChooseRoute(SipProxyRequest request)
    {
        Request = request;
        return Decision;
    }
}

public class ForkingLocationService : ISipLocationService
{
    public SipLocationResult Result = new SipLocationResult();
    public SipLocationQuery? Query;

    public void ConfigureLocationStorage(ISipLocationStorage storage)
    {
    }

    public SipLocationResult QueryLocation(SipLocationQuery query)
    {
        Query = query;
        return Result;
    }

    public SipLocationResult UpdateLocation(SipRegistrationBinding binding)
    {
        return Result;
    }

    public SipLocationResult ReplaceLocations(SipLocationUpdate update)
    {
        return Result;
    }

    public SipLocationResult RemoveLocation(SipLocationQuery query)
    {
        return Result;
    }

    public SipLocationResult ExpireLocations(long nowMilliseconds)
    {
        return Result;
    }
}
