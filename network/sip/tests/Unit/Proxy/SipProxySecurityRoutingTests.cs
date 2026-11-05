namespace sip.Tests;

public class SipProxySecurityRoutingTests
{
    [Test]
    public void AuthenticatesAuthorizesAndSanitizesProxyCredentials()
    {
        ProxySecurityAuthenticationServer authentication =
            SuccessfulAuthentication("alice@example.com");
        ProxySecurityAuthorizationPolicy authorization =
            AllowedAuthorization("alice@example.com");
        SipProxy proxy = ConfiguredSecureProxy(authentication, authorization);
        SipProxyRequest request = Request(
            "Proxy-Authorization: Digest username=\"alice\", "
            + "realm=\"proxy.example.com\", nonce=\"nonce\", "
            + "uri=\"sip:bob@service.example.com\", "
            + "response=\"0123456789abcdef0123456789abcdef\"\r\n"
            + "P-Asserted-Identity: <sip:alice@example.com>\r\n");

        SipProxyResult result = proxy.ProcessProxyRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.AuthenticatedIdentity,
                Is.EqualTo("alice@example.com"));
            Assert.That(authentication.VerifyCount, Is.EqualTo(1));
            Assert.That(authentication.AuthorizeCount, Is.EqualTo(1));
            Assert.That(authentication.Request!.Scope,
                Is.EqualTo(SipCredentialScope.Proxy));
            Assert.That(authentication.Request.Target!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(authentication.Request.CorrelationId,
                Is.EqualTo("security-correlation"));
            Assert.That(authorization.Request, Is.Not.Null);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.ProxyAuthorization), Is.False);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.PAssertedIdentity), Is.True);
        });
    }

    [Test]
    public void ChallengesMissingCredentialsAndRejectsDeniedIdentity()
    {
        ProxySecurityAuthenticationServer challenged =
            new ProxySecurityAuthenticationServer();
        challenged.Verification.Status =
            SipAuthenticationStatus.ChallengeRequired;
        challenged.Challenge.Status =
            SipAuthenticationStatus.ChallengeRequired;
        challenged.Challenge.Message = ChallengeResponse();
        challenged.Challenge.ErrorMessage = "Credentials required.";
        SipProxy challengeProxy = ConfiguredSecureProxy(challenged,
            AllowedAuthorization("unused"));

        SipProxyResult challenge = challengeProxy.ProcessProxyRequest(
            Request(""));

        ProxySecurityAuthenticationServer authenticated =
            SuccessfulAuthentication("mallory@example.com");
        ProxySecurityAuthorizationPolicy deniedPolicy =
            new ProxySecurityAuthorizationPolicy();
        deniedPolicy.Decision.Allowed = false;
        deniedPolicy.Decision.StatusCode = SipStatusCodes.Forbidden;
        deniedPolicy.Decision.Reason = "Identity is not authorized.";
        SipProxy deniedProxy = ConfiguredSecureProxy(
            authenticated, deniedPolicy);

        SipProxyResult denied = deniedProxy.ProcessProxyRequest(Request(""));

        Assert.Multiple(() =>
        {
            Assert.That(challenge.Success, Is.True);
            Assert.That(challenge.Completed, Is.True);
            Assert.That(challenge.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ProxyAuthenticationRequired));
            Assert.That(challenge.SendRequest, Is.Not.Null);
            Assert.That(challenged.VerifyCount, Is.EqualTo(1));
            Assert.That(challenged.ChallengeCount, Is.EqualTo(1));
            Assert.That(denied.Success, Is.True);
            Assert.That(denied.Completed, Is.True);
            Assert.That(denied.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(deniedPolicy.Request, Is.Not.Null);
        });
    }

    [Test]
    public void TrustsAuthenticatedPeerAndHonorsIdentityPrivacy()
    {
        ProxySecurityAuthenticationServer authentication =
            SuccessfulAuthentication("unused");
        ProxySecurityAuthorizationPolicy authorization =
            AllowedAuthorization("edge.example.com");
        SipProxy proxy = ConfiguredSecureProxy(authentication, authorization);
        SipProxyRequest request = Request(
            "Privacy: user; id\r\n"
            + "P-Asserted-Identity: <sip:alice@example.com>\r\n"
            + "P-Preferred-Identity: <sip:alias@example.com>\r\n");
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = "trusted-connection";
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tls;
        connection.Secure = true;
        connection.PeerIdentity = "edge.example.com";
        request.TransportMessage!.Connection = connection;

        SipProxyResult result = proxy.ProcessProxyRequest(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.AuthenticatedIdentity,
                Is.EqualTo("edge.example.com"));
            Assert.That(authentication.VerifyCount, Is.Zero);
            Assert.That(authentication.AuthorizeCount, Is.EqualTo(1));
            Assert.That(authentication.Authentication!.Identity,
                Is.EqualTo("edge.example.com"));
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.PAssertedIdentity), Is.False);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.PPreferredIdentity), Is.False);
            Assert.That(HasHeader(result.Request!,
                SipHeaderNames.Privacy), Is.True);
        });
    }

    [Test]
    public void AppliesLocationRouteSetsRecordRouteAndReusesOutboundFlow()
    {
        ForkingLocationService location = new ForkingLocationService();
        location.Result.Success = true;
        SipRegistrationBinding pathBinding = Binding(
            "sip:bob@path-target.example.com", "198.51.100.70");
        pathBinding.Path = new SipRouteValue[] {
            Route("sip:path.example.com;lr") };
        pathBinding.ServiceRoute = new SipRouteValue[] {
            Route("sip:ignored.example.com;lr") };
        SipFlow flow = new SipFlow();
        flow.Id = "outbound-flow";
        flow.Connection = Connection("flow-connection",
            "203.0.113.70", 5090, SipTransportKind.Tcp);
        pathBinding.Flow = flow;
        SipRegistrationBinding serviceBinding = Binding(
            "sip:bob@service-target.example.com", "198.51.100.71");
        serviceBinding.ServiceRoute = new SipRouteValue[] {
            Route("sip:service-route.example.com;lr") };
        location.Result.Bindings = new SipRegistrationBinding[] {
            pathBinding, serviceBinding };

        ForkingRoutePolicy routePolicy = new ForkingRoutePolicy();
        routePolicy.Decision.Mode = SipProxyMode.StatefulParallel;
        routePolicy.Decision.RecordRoute = true;
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyLocationService(location);
        proxy.ConfigureProxyRoutePolicy(routePolicy);

        SipProxyResult result = proxy.ProcessProxyRequest(Request(""));
        SipMessage pathRequest = result.Requests.Single(message =>
            message.RequestUri!.Host == "path-target.example.com");
        SipMessage serviceRequest = result.Requests.Single(message =>
            message.RequestUri!.Host == "service-target.example.com");
        SipSendRequest pathSend = result.SendRequests.Single(send =>
            send.FlowId == "outbound-flow");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Requests, Has.Length.EqualTo(2));
            Assert.That(RouteHost(pathRequest),
                Is.EqualTo("path.example.com"));
            Assert.That(RouteHost(serviceRequest),
                Is.EqualTo("service-route.example.com"));
            Assert.That(pathRequest.RecordRouteValues,
                Has.Length.EqualTo(1));
            Assert.That(serviceRequest.RecordRouteValues,
                Has.Length.EqualTo(1));
            Assert.That(pathSend.ConnectionId,
                Is.EqualTo("flow-connection"));
            Assert.That(pathSend.RemoteAddress!.Host,
                Is.EqualTo("203.0.113.70"));
            Assert.That(pathSend.RemoteAddress.Port, Is.EqualTo(5090));
            Assert.That(pathSend.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(routePolicy.Request, Is.Not.Null);
        });
    }

    static ProxySecurityAuthenticationServer SuccessfulAuthentication(
        string identity)
    {
        ProxySecurityAuthenticationServer result =
            new ProxySecurityAuthenticationServer();
        result.Verification.Status = SipAuthenticationStatus.Success;
        result.Verification.Identity = identity;
        return result;
    }

    static ProxySecurityAuthorizationPolicy AllowedAuthorization(
        string identity)
    {
        ProxySecurityAuthorizationPolicy result =
            new ProxySecurityAuthorizationPolicy();
        result.Decision.Allowed = true;
        result.Decision.Identity = identity;
        return result;
    }

    static SipProxy ConfiguredSecureProxy(
        ProxySecurityAuthenticationServer authentication,
        ProxySecurityAuthorizationPolicy authorization)
    {
        SipProxy proxy = new SipProxy();
        SipNonceOptions options = new SipNonceOptions();
        options.Realm = "proxy.example.com";
        options.CurrentKey = new byte[] { 1, 2, 3, 4 };
        proxy.ConfigureProxySecurity(authentication, authorization, options);
        return proxy;
    }

    static SipProxyRequest Request(string additionalHeaders)
    {
        SipMessage message = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-security;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: proxy-security@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + additionalHeaders
            + "Content-Length: 0\r\n\r\n");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060,
            SipTransportKind.Udp);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000,
            SipTransportKind.Udp);
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri("sip:bob@target.example.com");
        target.Candidate = Candidate("192.0.2.80", 5060,
            SipTransportKind.Udp);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.Stateless;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = new SipProxyTarget[] { target };
        request.Options = options;
        request.CorrelationId = "security-correlation";
        return request;
    }

    static SipRegistrationBinding Binding(string uri, string receivedHost)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = ParseUri(uri);
        SipContactValue contact = new SipContactValue();
        contact.Address = address;
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.Contact = contact;
        binding.ReceivedAddress = Endpoint(receivedHost, 5060,
            SipTransportKind.Udp);
        return binding;
    }

    static SipConnectionInfo Connection(string id, string host, int port,
        SipTransportKind transport)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = transport;
        connection.RemoteAddress = Endpoint(host, port, transport);
        return connection;
    }

    static SipRouteValue Route(string uri)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = ParseUri(uri);
        SipRouteValue route = new SipRouteValue();
        route.Address = address;
        return route;
    }

    static SipMessage ChallengeResponse()
    {
        return Parse("SIP/2.0 407 Proxy Authentication Required\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-security;rport\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=proxy-tag\r\n"
            + "Call-ID: proxy-security@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Proxy-Authenticate: Digest realm=\"proxy.example.com\", "
            + "nonce=\"nonce\", algorithm=SHA-256\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static bool HasHeader(SipMessage message, string name)
    {
        return message.Headers.Any(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string? RouteHost(SipMessage message)
    {
        return message.RouteValues.Length == 0
            || message.RouteValues[0].Address == null
            || message.RouteValues[0].Address!.Uri == null
            ? null : message.RouteValues[0].Address!.Uri!.Host;
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

    static SipEndpointAddress Endpoint(string host, int port,
        SipTransportKind transport)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = transport;
        return endpoint;
    }

    static SipServerCandidate Candidate(string host, int port,
        SipTransportKind transport)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = port;
        candidate.Transport = transport;
        return candidate;
    }
}

public class ProxySecurityAuthenticationServer : ISipAuthenticationServer
{
    public SipAuthenticationResult Verification =
        new SipAuthenticationResult();
    public SipAuthenticationResult Challenge =
        new SipAuthenticationResult();
    public SipAuthenticationRequest? Request;
    public SipAuthenticationResult? Authentication;
    public int VerifyCount;
    public int ChallengeCount;
    public int AuthorizeCount;

    public SipAuthenticationResult CreateAuthenticationChallenge(
        SipAuthenticationRequest request, SipNonceOptions options)
    {
        Request = request;
        ChallengeCount = ChallengeCount + 1;
        return Challenge;
    }

    public SipAuthenticationResult VerifyAuthentication(
        SipAuthenticationRequest request)
    {
        Request = request;
        VerifyCount = VerifyCount + 1;
        return Verification;
    }

    public SipAuthenticationResult VerifyBearerAuthentication(
        SipAuthenticationRequest request, ISipTokenValidator tokenValidator)
    {
        Request = request;
        return Verification;
    }

    public SipAuthenticationResult VerifyCertificateAuthentication(
        SipAuthenticationRequest request, SipConnectionInfo connection,
        ISipCertificatePolicy certificatePolicy)
    {
        Request = request;
        return Verification;
    }

    public SipAuthorizationDecision AuthorizeAuthenticatedOperation(
        SipAuthenticationRequest request,
        SipAuthenticationResult authentication,
        ISipAuthorizationPolicy authorizationPolicy)
    {
        Request = request;
        Authentication = authentication;
        AuthorizeCount = AuthorizeCount + 1;
        return authorizationPolicy.AuthorizeOperation(request,
            authentication.CertificateIdentity,
            authentication.BearerToken);
    }
}

public class ProxySecurityAuthorizationPolicy : ISipAuthorizationPolicy
{
    public SipAuthorizationDecision Decision = new SipAuthorizationDecision();
    public SipAuthenticationRequest? Request;

    public SipAuthorizationDecision AuthorizeOperation(
        SipAuthenticationRequest request, SipCertificateIdentity? certificate,
        SipBearerToken? token)
    {
        Request = request;
        return Decision;
    }
}
