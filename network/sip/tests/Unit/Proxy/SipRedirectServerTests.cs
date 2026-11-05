namespace sip.Tests;

public class SipRedirectServerTests
{
    [Test]
    public void CreatesOrderedRedirectContactsWithQualityAndExpiry()
    {
        ISipRedirectServer server = new SipRedirectServer();
        SipRouteDecision decision = new SipRouteDecision();
        decision.Redirect = true;
        decision.RedirectStatusCode = SipStatusCodes.MovedPermanently;
        decision.Reason = "Policy Move";
        decision.Targets = new SipProxyTarget[] {
            Target("sip:bob@first.example.com", 750, 120),
            Target("sip:bob@second.example.com", 1000, -1) };

        SipRedirectResult result = server.CreateRedirect(
            Request(false), decision);

        decision.Targets[0].Uri!.Host = "changed.example.com";
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.MovedPermanently));
            Assert.That(result.Response.ReasonPhrase,
                Is.EqualTo("Policy Move"));
            Assert.That(result.Contacts, Has.Length.EqualTo(2));
            Assert.That(result.Contacts[0].Address!.Uri!.Host,
                Is.EqualTo("first.example.com"));
            Assert.That(result.Contacts[0].QualityThousandths,
                Is.EqualTo(750));
            Assert.That(result.Contacts[0].ExpiresSeconds,
                Is.EqualTo(120));
            Assert.That(result.Contacts[1].Address!.Uri!.Host,
                Is.EqualTo("second.example.com"));
            Assert.That(result.Response.ContactValues[0].QualityThousandths,
                Is.EqualTo(750));
            Assert.That(Header(result.Response, SipHeaderNames.Contact),
                Does.Contain("<sip:bob@first.example.com>;q=0.75;expires=120"));
            Assert.That(Header(result.Response, SipHeaderNames.Contact),
                Does.Contain("<sip:bob@second.example.com>;q=1"));
        });
    }

    [Test]
    public void EnforcesSipsContactsWithoutDowngradingRedirects()
    {
        ISipRedirectServer server = new SipRedirectServer();
        SipRouteDecision insecure = new SipRouteDecision();
        insecure.Redirect = true;
        insecure.Targets = new SipProxyTarget[] {
            Target("sip:bob@insecure.example.com", 1000, -1) };
        SipRouteDecision secure = new SipRouteDecision();
        secure.Mode = SipProxyMode.Redirect;
        secure.RedirectStatusCode = SipStatusCodes.MultipleChoices;
        secure.Targets = new SipProxyTarget[] {
            Target("sips:bob@secure.example.com", 1000, -1) };

        SipRedirectResult rejected = server.CreateRedirect(
            Request(true), insecure);
        SipRedirectResult accepted = server.CreateRedirect(
            Request(true), secure);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
            Assert.That(rejected.Contacts, Is.Empty);
            Assert.That(accepted.Success, Is.True, accepted.ErrorMessage);
            Assert.That(accepted.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.MultipleChoices));
            Assert.That(accepted.Response.ContactValues[0]
                .Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
        });
    }

    [Test]
    public void ProducesErrorResponsesForMalformedAndInvalidPolicyResults()
    {
        ISipRedirectServer server = new SipRedirectServer();
        SipMessage malformed = Request(false);
        malformed.CSeq!.Method = SipMethod.Bye;
        SipRouteDecision valid = new SipRouteDecision();
        valid.Redirect = true;
        valid.Targets = new SipProxyTarget[] {
            Target("sip:bob@target.example.com", 1000, -1) };

        SipRedirectResult malformedResult =
            server.CreateRedirect(malformed, valid);

        SipRouteDecision empty = new SipRouteDecision();
        empty.Redirect = true;
        SipRedirectResult emptyResult = server.CreateRedirect(
            Request(false), empty);

        SipRouteDecision invalidStatus = new SipRouteDecision();
        invalidStatus.Redirect = true;
        invalidStatus.RedirectStatusCode = SipStatusCodes.Ok;
        invalidStatus.Targets = valid.Targets;
        SipRedirectResult invalidStatusResult = server.CreateRedirect(
            Request(false), invalidStatus);

        SipRouteDecision rejected = new SipRouteDecision();
        rejected.RejectStatusCode = SipStatusCodes.Forbidden;
        rejected.Reason = "Denied\r\nInjected: value";
        SipRedirectResult rejectedResult = server.CreateRedirect(
            Request(false), rejected);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.Success, Is.False);
            Assert.That(malformedResult.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(emptyResult.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.NotFound));
            Assert.That(invalidStatusResult.Success, Is.False);
            Assert.That(invalidStatusResult.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(rejectedResult.Success, Is.False);
            Assert.That(rejectedResult.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(rejectedResult.Response.ReasonPhrase,
                Does.Not.Contain("\r"));
            Assert.That(rejectedResult.Response.ReasonPhrase,
                Does.Not.Contain("\n"));
        });
    }

    [Test]
    public void ProxyEvaluatesRoutePolicyAndReturnsRedirectUpstream()
    {
        ForkingRoutePolicy policy = new ForkingRoutePolicy();
        policy.Decision.Redirect = true;
        policy.Decision.RedirectStatusCode =
            SipStatusCodes.MovedTemporarily;
        policy.Decision.Targets = new SipProxyTarget[] {
            Target("sip:bob@redirect.example.com", 600, 30) };
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyRoutePolicy(policy);
        SipProxyRequest request = ProxyRequest(Request(false));

        SipProxyResult result = proxy.ProcessProxyRequest(request);
        SipProxyEvent proxyEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(policy.Request, Is.Not.Null);
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.MovedTemporarily));
            Assert.That(result.Response.ContactValues,
                Has.Length.EqualTo(1));
            Assert.That(result.Response.ContactValues[0]
                .Address!.Uri!.Host,
                Is.EqualTo("redirect.example.com"));
            Assert.That(result.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.20"));
            Assert.That(result.SendRequest.RemoteAddress.Port,
                Is.EqualTo(62000));
            Assert.That(result.Requests, Is.Empty);
            Assert.That(result.ActiveBranchCount, Is.Zero);
            Assert.That(proxyEvent.Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(SipProxyEventKind.None));
        });
    }

    static SipProxyRequest ProxyRequest(SipMessage message)
    {
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = new SipProxyOptions();
        request.CorrelationId = "redirect-correlation";
        return request;
    }

    static SipMessage Request(bool secure)
    {
        string scheme = secure ? "sips" : "sip";
        string transport = secure ? "TLS" : "UDP";
        int port = secure ? 5061 : 5060;
        return Parse("INVITE " + scheme
            + ":bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/" + transport
            + " client.example.com:" + port
            + ";branch=z9hG4bK-redirect;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <" + scheme
            + ":alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <" + scheme + ":bob@example.com>\r\n"
            + "Call-ID: redirect@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <" + scheme
            + ":alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipProxyTarget Target(string uri, int quality, int expires)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        target.QualityThousandths = quality;
        target.ExpiresSeconds = expires;
        return target;
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
