namespace sip.Tests;

public class SipProxySafetyTests
{
    [Test]
    public void RejectsMalformedMessageModelsBeforeCreatingBranches()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest request = Request("malformed", "z9hG4bK-malformed",
            "sip:bob@service.example.com");
        request.Message!.CSeq!.Method = SipMethod.Bye;
        request.TransportMessage!.Message = request.Message;

        SipProxyResult result = proxy.ProcessProxyRequest(request);
        SipProxyEvent failure = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.SendRequests, Is.Empty);
            Assert.That(failure.Kind, Is.EqualTo(SipProxyEventKind.Failed));
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.None));
        });
    }

    [Test]
    public void RejectsLoopsAndMergedRequestsButAllowsLegalSpirals()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult first = proxy.ProcessProxyRequest(Request(
            "routing", "z9hG4bK-first", "sip:bob@one.example.com"));

        SipProxyResult loop = proxy.ProcessProxyRequest(Request(
            "routing", "z9hG4bK-first", "sip:bob@one.example.com"));
        SipProxyResult merged = proxy.ProcessProxyRequest(Request(
            "routing", "z9hG4bK-merged", "sip:bob@one.example.com"));
        SipProxyResult spiral = proxy.ProcessProxyRequest(Request(
            "routing", "z9hG4bK-spiral", "sip:bob@two.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(loop.Success, Is.True);
            Assert.That(loop.Completed, Is.True);
            Assert.That(loop.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.LoopDetected));
            Assert.That(merged.Success, Is.True);
            Assert.That(merged.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.LoopDetected));
            Assert.That(spiral.Success, Is.True, spiral.ErrorMessage);
            Assert.That(spiral.Response, Is.Null);
            Assert.That(spiral.Request, Is.Not.Null);
        });
    }

    [Test]
    public void ReturnsRetryAfterAtTransactionAndEventCapacity()
    {
        SipProxy transactionLimited = new SipProxy();
        SipProxySafetyOptions transactionOptions = SafetyOptions();
        transactionOptions.MaxTransactionCount = 1;
        transactionOptions.RetryAfterSeconds = 7;
        transactionLimited.ConfigureProxySafety(transactionOptions);
        SipProxyResult admitted = transactionLimited.ProcessProxyRequest(
            Request("capacity-one", "z9hG4bK-capacity-one",
                "sip:bob@one.example.com"));
        SipProxyResult transactionRejected =
            transactionLimited.ProcessProxyRequest(Request(
                "capacity-two", "z9hG4bK-capacity-two",
                "sip:bob@two.example.com"));

        SipProxy eventLimited = new SipProxy();
        SipProxySafetyOptions eventOptions = SafetyOptions();
        eventOptions.MaxEventCount = 2;
        eventOptions.RetryAfterSeconds = 9;
        eventLimited.ConfigureProxySafety(eventOptions);
        SipProxyResult eventAdmitted = eventLimited.ProcessProxyRequest(
            Request("event-one", "z9hG4bK-event-one",
                "sip:bob@one.example.com"));
        SipProxyResult eventRejected = eventLimited.ProcessProxyRequest(
            Request("event-two", "z9hG4bK-event-two",
                "sip:bob@two.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(admitted.Success, Is.True, admitted.ErrorMessage);
            Assert.That(transactionRejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(Header(transactionRejected.Response,
                SipHeaderNames.RetryAfter), Is.EqualTo("7"));
            Assert.That(eventAdmitted.Success, Is.True,
                eventAdmitted.ErrorMessage);
            Assert.That(eventRejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(Header(eventRejected.Response,
                SipHeaderNames.RetryAfter), Is.EqualTo("9"));
            Assert.That(eventLimited.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(eventLimited.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(eventLimited.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.None));
        });
    }

    [Test]
    public void EnforcesRateWindowAndRecoversAfterTimeAdvances()
    {
        SipProxy proxy = new SipProxy();
        SipProxySafetyOptions options = SafetyOptions();
        options.MaxRequestCount = 1;
        options.RequestWindowMilliseconds = 100;
        proxy.ConfigureProxySafety(options);

        SipProxyResult first = proxy.ProcessProxyRequest(Request(
            "rate-one", "z9hG4bK-rate-one", "sip:bob@one.example.com"));
        SipProxyResult limited = proxy.ProcessProxyRequest(Request(
            "rate-two", "z9hG4bK-rate-two", "sip:bob@two.example.com"));
        proxy.AdvanceProxyTime(101);
        SipProxyResult recovered = proxy.ProcessProxyRequest(Request(
            "rate-three", "z9hG4bK-rate-three",
            "sip:bob@three.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(limited.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(Header(limited.Response, SipHeaderNames.RetryAfter),
                Is.EqualTo("3"));
            Assert.That(recovered.Success, Is.True, recovered.ErrorMessage);
            Assert.That(recovered.Response, Is.Null);
        });
    }

    [Test]
    public void BoundsBranchesAndRollsBackPartialParallelAdmission()
    {
        SipProxy proxy = new SipProxy();
        SipProxySafetyOptions safety = SafetyOptions();
        safety.MaxBranchCount = 1;
        safety.MaxTransactionCount = 1;
        proxy.ConfigureProxySafety(safety);
        SipProxyRequest parallel = Request("parallel",
            "z9hG4bK-parallel", "sip:bob@service.example.com");
        parallel.Options.Mode = SipProxyMode.StatefulParallel;
        parallel.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.81"),
            Target("sip:bob@two.example.com", "192.0.2.82") };

        SipProxyResult rejected = proxy.ProcessProxyRequest(parallel);
        SipProxyResult following = proxy.ProcessProxyRequest(Request(
            "following", "z9hG4bK-following",
            "sip:bob@following.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(rejected.Completed, Is.True);
            Assert.That(following.Success, Is.True, following.ErrorMessage);
            Assert.That(following.Response, Is.Null);
        });
    }

    [Test]
    public void RedactsCredentialsFromEventsAndSanitizesPolicyReasons()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest request = Request("redaction",
            "z9hG4bK-redaction", "sip:bob@service.example.com");
        request.Message = Parse(RequestText("redaction",
            "z9hG4bK-redaction", "sip:bob@service.example.com",
            "Authorization: Bearer origin-secret\r\n"
            + "Proxy-Authorization: Bearer proxy-secret\r\n"));
        request.TransportMessage!.Message = request.Message;
        request.TransportMessage.Bytes =
            System.Text.Encoding.ASCII.GetBytes("transport-secret");

        SipProxyResult result = proxy.ProcessProxyRequest(request);
        SipProxyEvent accepted = proxy.PollProxyEvent();
        SipProxyEvent branch = proxy.PollProxyEvent();

        ForkingRoutePolicy policy = new ForkingRoutePolicy();
        policy.Decision.RejectStatusCode = SipStatusCodes.Forbidden;
        policy.Decision.Reason = "Denied\r\nInjected: value";
        SipProxy policyProxy = new SipProxy();
        policyProxy.ConfigureProxyRoutePolicy(policy);
        SipProxyResult denied = policyProxy.ProcessProxyRequest(Request(
            "policy", "z9hG4bK-policy", "sip:bob@service.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.Authorization), Is.True);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.ProxyAuthorization), Is.False);
            AssertEventRedacted(accepted);
            AssertEventRedacted(branch);
            Assert.That(accepted.Request!.TransportMessage!.Bytes, Is.Empty);
            Assert.That(denied.ErrorMessage, Does.Not.Contain("\r"));
            Assert.That(denied.ErrorMessage, Does.Not.Contain("\n"));
            Assert.That(policyProxy.PollProxyEvent().Reason,
                Does.Not.Contain("\n"));
        });
    }

    static void AssertEventRedacted(SipProxyEvent value)
    {
        Assert.That(HasHeader(value.Request!.Message!,
            SipHeaderNames.Authorization), Is.False);
        Assert.That(HasHeader(value.Request.Message!,
            SipHeaderNames.ProxyAuthorization), Is.False);
        Assert.That(HasHeader(value.Message!,
            SipHeaderNames.Authorization), Is.False);
        Assert.That(HasHeader(value.Message!,
            SipHeaderNames.ProxyAuthorization), Is.False);
        Assert.That(HasHeader(value.Result!.Request!,
            SipHeaderNames.Authorization), Is.False);
    }

    static SipProxySafetyOptions SafetyOptions()
    {
        SipProxySafetyOptions options = new SipProxySafetyOptions();
        options.MaxTransactionCount = 20;
        options.MaxBranchCount = 20;
        options.MaxEventCount = 100;
        options.MaxRequestCount = 20;
        options.RequestWindowMilliseconds = 1000;
        options.RetryAfterSeconds = 3;
        return options;
    }

    static SipProxyRequest Request(string callId, string branch, string uri)
    {
        SipMessage message = Parse(RequestText(callId, branch, uri, ""));
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.Stateless;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = new SipProxyTarget[] {
            Target("sip:bob@target.example.com", "192.0.2.80") };
        request.Options = options;
        request.CorrelationId = callId;
        return request;
    }

    static string RequestText(string callId, string branch, string uri,
        string extraHeaders)
    {
        return "INVITE " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch="
            + branch + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + extraHeaders
            + "Content-Length: 0\r\n\r\n";
    }

    static SipProxyTarget Target(string uri, string host)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        target.Candidate = candidate;
        return target;
    }

    static bool HasHeader(SipMessage message, string name)
    {
        return message.Headers.Any(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string? Header(SipMessage message, string name)
    {
        SipHeader? header = message.Headers.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name,
                System.StringComparison.OrdinalIgnoreCase));
        return header == null ? null : header.Value;
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

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
    }
}
