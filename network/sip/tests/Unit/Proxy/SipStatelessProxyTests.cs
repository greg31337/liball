namespace sip.Tests;

public class SipStatelessProxyTests
{
    [Test]
    public void ForwardsRequestWithRouteViaRecordRouteContactAndDnsTransport()
    {
        ProxyResolver resolver = new ProxyResolver();
        resolver.Candidate = Candidate("203.0.113.40", 5080,
            SipTransportKind.Tcp, false);
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage incoming = Parse("INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client;rport\r\n"
            + "Route: <sip:proxy.example.com:5060;lr>, "
            + "<sip:edge.example.com;lr>\r\n"
            + "Max-Forwards: 5\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipProxyRequest request = ProxyRequest(incoming);
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri("sip:bob@new-target.example.com");
        request.Targets = new SipProxyTarget[] { target };

        SipProxyResult result = proxy.ProcessProxyRequest(request);
        SipProxyEvent accepted = proxy.PollProxyEvent();
        SipProxyEvent branch = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Request!.RequestUri!.Host,
                Is.EqualTo("new-target.example.com"));
            Assert.That(result.Request.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "edge.example.com" }));
            Assert.That(Header(result.Request, SipHeaderNames.MaxForwards),
                Is.EqualTo("4"));
            Assert.That(result.Request.ViaValues, Has.Length.EqualTo(2));
            Assert.That(result.Request.ViaValues[0].Host,
                Is.EqualTo("proxy.example.com"));
            Assert.That(result.Request.ViaValues[0].Transport,
                Is.EqualTo(SipTransportNames.Tcp));
            Assert.That(result.Request.ViaValues[0].HasRPort, Is.True);
            Assert.That(result.Request.ViaValues[1].Received,
                Is.EqualTo("198.51.100.20"));
            Assert.That(result.Request.ViaValues[1].RPort,
                Is.EqualTo(62000));
            Assert.That(result.Request.RecordRouteValues, Has.Length.EqualTo(1));
            Assert.That(result.Request.RecordRouteValues[0].Address!.Uri!.Host,
                Is.EqualTo("proxy.example.com"));
            Assert.That(result.Request.RecordRouteValues[0].Address!.Uri!.Parameters
                .Any(parameter => parameter.Name == "transport"
                    && parameter.Value == "tcp"), Is.True);
            Assert.That(result.Request.ContactValues[0].Address!.Uri!.Host,
                Is.EqualTo("client.example.com"));
            Assert.That(result.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("203.0.113.40"));
            Assert.That(result.SendRequest.Transport,
                Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(result.Targets[0].Branch,
                Does.StartWith("z9hG4bK"));
            Assert.That(resolver.Request!.Uri!.Host,
                Is.EqualTo("edge.example.com"));
            Assert.That(Header(incoming, SipHeaderNames.MaxForwards),
                Is.EqualTo("5"));
            Assert.That(incoming.ViaValues[0].Received, Is.Null);
            Assert.That(accepted.Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(branch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(branch.Message!.ViaValues[0].Branch,
                Is.EqualTo(result.Targets[0].Branch));
        });
    }

    [Test]
    public void RoutesResponseAfterRemovingProxyViaAndUsesUpstreamRPort()
    {
        ProxyResolver resolver = new ProxyResolver();
        resolver.Candidate = Candidate("203.0.113.40", 5080,
            SipTransportKind.Tcp, false);
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage incoming = OptionsRequest("Max-Forwards: 8\r\n");
        SipProxyResult forwarded = proxy.ProcessProxyRequest(
            ProxyRequest(incoming));
        proxy.PollProxyEvent();
        proxy.PollProxyEvent();
        SipMessage response = ResponseFor(forwarded.Request!);

        SipProxyResult result = proxy.ProcessProxyResponse(response);
        SipProxyEvent registrationEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Response!.ViaValues, Has.Length.EqualTo(1));
            Assert.That(result.Response.ViaValues[0].Branch,
                Is.EqualTo("z9hG4bK-client"));
            Assert.That(result.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.20"));
            Assert.That(result.SendRequest.RemoteAddress.Port,
                Is.EqualTo(62000));
            Assert.That(result.SendRequest.Transport,
                Is.EqualTo(SipTransportKind.Udp));
            Assert.That(result.SendRequest.LocalAddress!.Host,
                Is.EqualTo("proxy.example.com"));
            Assert.That(response.ViaValues, Has.Length.EqualTo(2));
            Assert.That(registrationEvent.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(registrationEvent.Message!.ViaValues,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void RecoversStrictRoutingAndAddsMissingMaxForwards()
    {
        ProxyResolver resolver = new ProxyResolver();
        resolver.Candidate = Candidate("192.0.2.44", 5060,
            SipTransportKind.Udp, false);
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage incoming = ParseCompatible(
            "OPTIONS sip:proxy.example.com:5060 SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client\r\n"
            + "Route: <sip:strict.example.com>, "
            + "<sip:bob@final.example.com>\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: strict-call@example.com\r\n"
            + "CSeq: 2 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");

        SipProxyResult result = proxy.ProcessProxyRequest(
            ProxyRequest(incoming));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(Header(result.Request!, SipHeaderNames.MaxForwards),
                Is.EqualTo("70"));
            Assert.That(result.Request!.RequestUri!.Host,
                Is.EqualTo("strict.example.com"));
            Assert.That(result.Request.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "final.example.com" }));
            Assert.That(resolver.Request!.Uri!.Host,
                Is.EqualTo("strict.example.com"));
        });
    }

    [Test]
    public void EnforcesHopValidationAndSipsTransportAndContactRules()
    {
        ProxyResolver ordinaryResolver = new ProxyResolver();
        ordinaryResolver.Candidate = Candidate("192.0.2.50", 5060,
            SipTransportKind.Udp, false);
        SipProxy ordinary = ConfiguredProxy(ordinaryResolver);
        SipProxyResult exhausted = ordinary.ProcessProxyRequest(
            ProxyRequest(OptionsRequest("Max-Forwards: 0\r\n")));
        SipMessage repeatedRequest = ParseCompatible(
            "OPTIONS sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client\r\n"
            + "Max-Forwards: 4\r\nMax-Forwards: 3\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: repeated-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipProxyResult repeated = ordinary.ProcessProxyRequest(
            ProxyRequest(repeatedRequest));

        ProxyResolver insecureResolver = new ProxyResolver();
        insecureResolver.Candidate = Candidate("192.0.2.51", 5060,
            SipTransportKind.Udp, false);
        SipProxy insecure = ConfiguredProxy(insecureResolver);
        SipProxyResult insecureTransport = insecure.ProcessProxyRequest(
            ProxyRequest(SecureInvite("sips:alice@client.example.com")));
        SipProxyResult insecureContact = insecure.ProcessProxyRequest(
            ProxyRequest(SecureInvite("sip:alice@client.example.com")));

        ProxyResolver secureResolver = new ProxyResolver();
        secureResolver.Candidate = Candidate("192.0.2.52", 5061,
            SipTransportKind.Tls, true);
        SipProxy secure = ConfiguredProxy(secureResolver);
        SipProxyResult secureResult = secure.ProcessProxyRequest(
            ProxyRequest(SecureInvite("sips:alice@client.example.com")));

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.StatusCode,
                Is.EqualTo(SipStatusCodes.TooManyHops));
            Assert.That(repeated.Success, Is.False);
            Assert.That(repeated.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(insecureTransport.Success, Is.False);
            Assert.That(insecureTransport.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
            Assert.That(insecureContact.Success, Is.False);
            Assert.That(insecureContact.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
            Assert.That(secureResult.Success, Is.True,
                secureResult.ErrorMessage);
            Assert.That(secureResult.Request!.ViaValues[0].Transport,
                Is.EqualTo(SipTransportNames.Tls));
            Assert.That(secureResult.Request.RecordRouteValues[0]
                .Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(secureResult.SendRequest!.RemoteAddress!.Secure,
                Is.True);
        });
    }

    static SipProxy ConfiguredProxy(ProxyResolver resolver)
    {
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        return proxy;
    }

    static SipProxyRequest ProxyRequest(SipMessage message)
    {
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060,
            SipTransportKind.Udp, false);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000,
            SipTransportKind.Udp, false);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.Stateless;
        options.RecordRoute = true;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.CorrelationId = "proxy-correlation";
        return request;
    }

    static SipMessage OptionsRequest(string maxForwards)
    {
        return Parse("OPTIONS sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client;rport\r\n"
            + maxForwards
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: stateless-proxy-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage SecureInvite(string contact)
    {
        return ParseCompatible(
            "INVITE sips:bob@secure.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TLS client.example.com:5061;"
            + "branch=z9hG4bK-client;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sips:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sips:bob@example.com>\r\n"
            + "Call-ID: secure-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <" + contact + ">\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ResponseFor(SipMessage request)
    {
        string[] vias = request.Headers.Where(header =>
            header.Name!.Equals(SipHeaderNames.Via,
                System.StringComparison.OrdinalIgnoreCase)
            || header.Name.Equals(SipHeaderNames.CompactVia,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => "Via: " + header.Value + "\r\n").ToArray();
        return Parse("SIP/2.0 200 OK\r\n"
            + string.Concat(vias)
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>;tag=to-tag\r\n"
            + "Call-ID: stateless-proxy-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\n"
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

    static SipMessage ParseCompatible(string text)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), options);
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

    static string Header(SipMessage message, string name)
    {
        return message.Headers.Single(header => header.Name!.Equals(
            name, System.StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static string RouteHost(SipRouteValue route)
    {
        return route.Address!.Uri!.Host!;
    }
}

public class ProxyResolver : ISipDnsResolver
{
    public SipResolutionRequest? Request;
    public SipServerCandidate? Candidate;
    public int FailureCount;
    public int FlushCount;

    public SipResolutionResult ResolveTarget(
        SipResolutionRequest request, SipDnsOptions options)
    {
        Request = request;
        SipResolutionResult result = new SipResolutionResult();
        if (Candidate == null)
        {
            result.Status = SipResolutionStatus.NoRecords;
            result.ErrorMessage = "No candidate was configured.";
            return result;
        }
        result.Status = SipResolutionStatus.Success;
        result.Candidates = new SipServerCandidate[] { Candidate };
        return result;
    }

    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
        FailureCount = FailureCount + 1;
    }

    public void FlushResolverCache()
    {
        FlushCount = FlushCount + 1;
    }
}
