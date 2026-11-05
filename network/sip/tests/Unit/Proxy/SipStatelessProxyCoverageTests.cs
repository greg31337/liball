namespace sip.Tests;

public class SipStatelessProxyCoverageTests
{
    [TestCase(SipMethods.Ack)]
    [TestCase(SipMethods.Bye)]
    [TestCase(SipMethods.Info)]
    [TestCase(SipMethods.Invite)]
    [TestCase(SipMethods.Message)]
    [TestCase(SipMethods.Notify)]
    [TestCase(SipMethods.Options)]
    [TestCase(SipMethods.Prack)]
    [TestCase(SipMethods.Publish)]
    [TestCase(SipMethods.Refer)]
    [TestCase(SipMethods.Register)]
    [TestCase(SipMethods.Subscribe)]
    [TestCase(SipMethods.Update)]
    [TestCase("X-EXAMPLE")]
    public void ForwardsEveryNonCancelMethodStatelessly(string method)
    {
        CoverageProxyResolver resolver = Resolver(Candidate(
            "192.0.2.40", 5060, SipTransportKind.Udp, false));
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage message = Request(method, "method-" + method.ToLowerInvariant(),
            "z9hG4bK-method", "Max-Forwards: 10\r\n", "");

        SipProxyResult result = proxy.ProcessProxyRequest(ProxyRequest(message));
        SipProxyEvent firstEvent = proxy.PollProxyEvent();
        SipProxyEvent secondEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Request!.MethodToken, Is.EqualTo(method));
            Assert.That(result.Request.CSeq!.MethodToken, Is.EqualTo(method));
            Assert.That(Header(result.Request, SipHeaderNames.MaxForwards),
                Is.EqualTo("9"));
            Assert.That(result.Request.ViaValues, Has.Length.EqualTo(2));
            Assert.That(result.SendRequest, Is.Not.Null);
            if (method == SipMethods.Ack)
            {
                Assert.That(result.Completed, Is.True);
                Assert.That(firstEvent.Kind,
                    Is.EqualTo(SipProxyEventKind.AckForwarded));
                Assert.That(secondEvent.Kind, Is.EqualTo(SipProxyEventKind.None));
            }
            else
            {
                Assert.That(firstEvent.Kind,
                    Is.EqualTo(SipProxyEventKind.RequestAccepted));
                Assert.That(secondEvent.Kind,
                    Is.EqualTo(SipProxyEventKind.BranchCreated));
            }
        });
    }

    [Test]
    public void ReturnsTransactionDoesNotExistForUnmatchedCancel()
    {
        SipProxy proxy = ConfiguredProxy(Resolver(Candidate(
            "192.0.2.41", 5060, SipTransportKind.Udp, false)));
        SipMessage cancel = Request(SipMethods.Cancel, "unmatched-cancel",
            "z9hG4bK-cancel", "Max-Forwards: 10\r\n", "");

        SipProxyResult result = proxy.ProcessProxyRequest(ProxyRequest(cancel));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.CallOrTransactionDoesNotExist));
            Assert.That(result.Request, Is.Null);
            Assert.That(result.SendRequest, Is.Not.Null);
        });
    }

    [TestCase(100)]
    [TestCase(200)]
    [TestCase(302)]
    [TestCase(404)]
    [TestCase(503)]
    [TestCase(603)]
    public void ForwardsEveryResponseClass(int statusCode)
    {
        SipProxy proxy = ConfiguredProxy(Resolver(Candidate(
            "192.0.2.42", 5060, SipTransportKind.Udp, false)));
        SipProxyResult request = proxy.ProcessProxyRequest(ProxyRequest(
            Request(SipMethods.Options, "response-" + statusCode.ToString(),
                "z9hG4bK-response", "Max-Forwards: 10\r\n", "")));
        proxy.PollProxyEvent();
        proxy.PollProxyEvent();

        SipProxyResult result = proxy.ProcessProxyResponse(
            Response(request.Request!, statusCode));
        SipProxyEvent responseEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.Response!.ViaValues, Has.Length.EqualTo(1));
            Assert.That(result.SendRequest, Is.Not.Null);
            Assert.That(responseEvent.Kind, Is.EqualTo(statusCode < 200
                ? SipProxyEventKind.ProvisionalForwarded
                : SipProxyEventKind.FinalSelected));
        });

        if (statusCode < 200)
        {
            SipProxyResult final = proxy.ProcessProxyResponse(
                Response(request.Request!, 487));
            Assert.That(final.Success, Is.True, final.ErrorMessage);
        }
        else
        {
            SipProxyResult repeated = proxy.ProcessProxyResponse(
                Response(request.Request!, statusCode));
            Assert.That(repeated.StatusCode,
                Is.EqualTo(SipStatusCodes.CallOrTransactionDoesNotExist));
        }
    }

    [Test]
    public void DeliversResponseLocallyWhenProxyViaIsTheLastVia()
    {
        SipProxy proxy = ConfiguredProxy(Resolver(Candidate(
            "192.0.2.43", 5060, SipTransportKind.Udp, false)));
        SipProxyResult request = proxy.ProcessProxyRequest(ProxyRequest(
            Request(SipMethods.Options, "local-response",
                "z9hG4bK-local", "Max-Forwards: 10\r\n", "")));
        proxy.PollProxyEvent();
        proxy.PollProxyEvent();
        SipMessage response = Response(request.Request!, 200);
        response.ViaValues = new SipViaValue[] { response.ViaValues[0] };
        KeepFirstViaHeaderOnly(response);

        SipProxyResult result = proxy.ProcessProxyResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Response!.ViaValues, Is.Empty);
            Assert.That(result.SendRequest, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
        });
    }

    [Test]
    public void AppliesReceivedWithoutRPortAndRoutesResponseToViaPort()
    {
        CoverageProxyResolver resolver = Resolver(Candidate(
            "192.0.2.44", 5060, SipTransportKind.Udp, false));
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage message = Request(SipMethods.Options, "received-only",
            "z9hG4bK-received", "Max-Forwards: 3\r\n", "");
        SipProxyRequest proxyRequest = ProxyRequest(message);

        SipProxyResult forwarded = proxy.ProcessProxyRequest(proxyRequest);
        proxy.PollProxyEvent();
        proxy.PollProxyEvent();
        SipProxyResult response = proxy.ProcessProxyResponse(
            Response(forwarded.Request!, 200));

        Assert.Multiple(() =>
        {
            Assert.That(forwarded.Request!.ViaValues[1].Received,
                Is.EqualTo("198.51.100.20"));
            Assert.That(forwarded.Request.ViaValues[1].HasRPort, Is.False);
            Assert.That(forwarded.Request.ViaValues[1].RPort, Is.EqualTo(-1));
            Assert.That(response.SendRequest!.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.20"));
            Assert.That(response.SendRequest.RemoteAddress.Port,
                Is.EqualTo(5070));
            Assert.That(response.SendRequest.LocalAddress, Is.Null);
        });
    }

    [Test]
    public void HonorsRecordRoutePolicyAndSelectedTransport()
    {
        CoverageProxyResolver tcpResolver = Resolver(Candidate(
            "192.0.2.45", 5080, SipTransportKind.Tcp, false));
        SipProxy withRecordRoute = ConfiguredProxy(tcpResolver);
        SipProxyRequest recordedRequest = ProxyRequest(Request(
            SipMethods.Invite, "record-route-on", "z9hG4bK-record-on",
            "Max-Forwards: 6\r\n", ""));
        recordedRequest.Options.RecordRoute = true;
        SipProxyResult recorded = withRecordRoute.ProcessProxyRequest(
            recordedRequest);

        CoverageProxyResolver udpResolver = Resolver(Candidate(
            "192.0.2.46", 5060, SipTransportKind.Udp, false));
        SipProxy withoutRecordRoute = ConfiguredProxy(udpResolver);
        SipProxyRequest plainRequest = ProxyRequest(Request(
            SipMethods.Invite, "record-route-off", "z9hG4bK-record-off",
            "Max-Forwards: 6\r\n", ""));
        plainRequest.Options.RecordRoute = false;
        SipProxyResult plain = withoutRecordRoute.ProcessProxyRequest(
            plainRequest);

        Assert.Multiple(() =>
        {
            Assert.That(recorded.Request!.RecordRouteValues,
                Has.Length.EqualTo(1));
            Assert.That(recorded.Request.RecordRouteValues[0].Address!.Uri!
                .Parameters.Any(parameter => parameter.Name == "transport"
                    && parameter.Value == "tcp"), Is.True);
            Assert.That(recorded.Request.ViaValues[0].Transport,
                Is.EqualTo(SipTransportNames.Tcp));
            Assert.That(recorded.SendRequest!.Transport,
                Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(plain.Request!.RecordRouteValues, Is.Empty);
            Assert.That(FindHeader(plain.Request, SipHeaderNames.RecordRoute),
                Is.Null);
        });
    }

    [Test]
    public void RejectsResolutionAndTransportCandidateFailures()
    {
        CoverageProxyResolver noRecordsResolver = new CoverageProxyResolver();
        noRecordsResolver.Status = SipResolutionStatus.NoRecords;
        SipProxyResult noRecords = ConfiguredProxy(noRecordsResolver)
            .ProcessProxyRequest(ProxyRequest(Request(SipMethods.Options,
                "dns-none", "z9hG4bK-dns-none", "Max-Forwards: 5\r\n", "")));

        CoverageProxyResolver invalidResolver = Resolver(Candidate(
            "192.0.2.47", 0, SipTransportKind.Udp, false));
        SipProxyResult invalidCandidate = ConfiguredProxy(invalidResolver)
            .ProcessProxyRequest(ProxyRequest(Request(SipMethods.Options,
                "dns-invalid", "z9hG4bK-dns-invalid",
                "Max-Forwards: 5\r\n", "")));

        CoverageProxyResolver customResolver = Resolver(Candidate(
            "192.0.2.48", 5060, SipTransportKind.Custom, false));
        SipProxyResult customTransport = ConfiguredProxy(customResolver)
            .ProcessProxyRequest(ProxyRequest(Request(SipMethods.Options,
                "transport-custom", "z9hG4bK-transport-custom",
                "Max-Forwards: 5\r\n", "")));

        CoverageProxyResolver inconsistentResolver = Resolver(Candidate(
            "192.0.2.49", 5061, SipTransportKind.Tls, false));
        SipProxyResult inconsistentSecurity = ConfiguredProxy(
            inconsistentResolver).ProcessProxyRequest(ProxyRequest(Request(
                SipMethods.Options, "transport-security",
                "z9hG4bK-transport-security", "Max-Forwards: 5\r\n", "")));

        Assert.Multiple(() =>
        {
            AssertFailure(noRecords, SipStatusCodes.BadGateway,
                "could not be resolved");
            AssertFailure(invalidCandidate, SipStatusCodes.BadGateway,
                "could not be resolved");
            AssertFailure(customTransport, SipStatusCodes.BadGateway,
                "could not be resolved");
            AssertFailure(inconsistentSecurity,
                SipStatusCodes.UnsupportedUriScheme, "securely reach");
            Assert.That(noRecordsResolver.Request, Is.Not.Null);
            Assert.That(noRecordsResolver.Request!.CorrelationId,
                Is.EqualTo("coverage-correlation"));
        });
    }

    [Test]
    public void RejectsInvalidStrictRoutingWithoutResolvingTarget()
    {
        CoverageProxyResolver resolver = Resolver(Candidate(
            "192.0.2.50", 5061, SipTransportKind.Tls, true));
        SipProxy proxy = ConfiguredProxy(resolver);
        SipMessage missingTrailingRoute = ParseCompatible(
            RequestText(SipMethods.Options, "strict-missing",
                "z9hG4bK-strict-missing", "sip:proxy.example.com:5060",
                "Max-Forwards: 5\r\n", ""));
        SipProxyResult missing = proxy.ProcessProxyRequest(
            ProxyRequest(missingTrailingRoute));

        SipMessage downgrade = ParseCompatible(
            RequestText(SipMethods.Options, "strict-downgrade",
                "z9hG4bK-strict-downgrade", "sips:proxy.example.com:5060",
                "Max-Forwards: 5\r\n"
                    + "Route: <sips:strict.example.com>, "
                    + "<sip:bob@final.example.com>\r\n", ""));
        SipProxyResult insecure = proxy.ProcessProxyRequest(
            ProxyRequest(downgrade));

        Assert.Multiple(() =>
        {
            AssertFailure(missing, SipStatusCodes.BadRequest,
                "Route set is invalid");
            AssertFailure(insecure, SipStatusCodes.UnsupportedUriScheme,
                "downgrade");
            Assert.That(resolver.Calls, Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsMalformedStatelessRequestsAndResponses()
    {
        SipProxy proxy = ConfiguredProxy(Resolver(Candidate(
            "192.0.2.51", 5060, SipTransportKind.Udp, false)));
        SipProxyRequest missingOptions = ProxyRequest(Request(
            SipMethods.Options, "missing-options", "z9hG4bK-options",
            "Max-Forwards: 5\r\n", ""));
        missingOptions.Options = null!;
        SipProxyResult optionsFailure = proxy.ProcessProxyRequest(missingOptions);

        SipProxyRequest missingLocal = ProxyRequest(Request(
            SipMethods.Options, "missing-local", "z9hG4bK-local-endpoint",
            "Max-Forwards: 5\r\n", ""));
        missingLocal.TransportMessage!.LocalAddress = null;
        SipProxyResult localFailure = proxy.ProcessProxyRequest(missingLocal);

        SipProxyRequest multipleTargets = ProxyRequest(Request(
            SipMethods.Options, "multiple-targets", "z9hG4bK-targets",
            "Max-Forwards: 5\r\n", ""));
        multipleTargets.Targets = new SipProxyTarget[] {
            Target("sip:one.example.com"), Target("sip:two.example.com") };
        SipProxyResult targetFailure = proxy.ProcessProxyRequest(multipleTargets);

        SipProxyRequest invalidOptions = ProxyRequest(Request(
            SipMethods.Options, "invalid-options", "z9hG4bK-invalid-options",
            "Max-Forwards: 5\r\n", ""));
        invalidOptions.Options.MaxBranchCount = 0;
        SipProxyResult policyFailure = proxy.ProcessProxyRequest(invalidOptions);

        SipProxyResult nullResponse = proxy.ProcessProxyResponse(null!);
        SipMessage requestAsResponse = Request(SipMethods.Options,
            "request-response", "z9hG4bK-request-response",
            "Max-Forwards: 5\r\n", "");
        SipProxyResult wrongKind = proxy.ProcessProxyResponse(requestAsResponse);
        SipMessage unmatched = Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP elsewhere.example.com:5060;"
            + "branch=z9hG4bK-unknown\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: unmatched@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipProxyResult noBranch = proxy.ProcessProxyResponse(unmatched);

        Assert.Multiple(() =>
        {
            AssertFailure(optionsFailure, SipStatusCodes.BadRequest,
                "options are required");
            AssertFailure(localFailure, SipStatusCodes.BadRequest,
                "malformed");
            AssertFailure(targetFailure, SipStatusCodes.BadRequest,
                "at most one target");
            AssertFailure(policyFailure, SipStatusCodes.BadRequest,
                "options are invalid");
            AssertFailure(nullResponse, SipStatusCodes.BadRequest,
                "valid SIP response");
            AssertFailure(wrongKind, SipStatusCodes.BadRequest,
                "valid SIP response");
            AssertFailure(noBranch,
                SipStatusCodes.CallOrTransactionDoesNotExist,
                "does not identify a proxy branch");
        });
    }

    static SipProxy ConfiguredProxy(CoverageProxyResolver resolver)
    {
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        return proxy;
    }

    static CoverageProxyResolver Resolver(SipServerCandidate candidate)
    {
        CoverageProxyResolver resolver = new CoverageProxyResolver();
        resolver.Status = SipResolutionStatus.Success;
        resolver.Candidate = candidate;
        return resolver;
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
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.CorrelationId = "coverage-correlation";
        return request;
    }

    static SipProxyTarget Target(string uri)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        return target;
    }

    static SipMessage Request(string method, string callId, string branch,
        string additionalHeaders, string bodyHeaders)
    {
        string requiredContact = method == SipMethods.Invite
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(RequestText(method, callId, branch,
            "sip:service@service.example.com", additionalHeaders,
            requiredContact + bodyHeaders));
    }

    static string RequestText(string method, string callId, string branch,
        string requestUri, string additionalHeaders, string bodyHeaders)
    {
        return method + " " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5070;branch="
            + branch + "\r\n"
            + additionalHeaders
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + bodyHeaders
            + "Content-Length: 0\r\n\r\n";
    }

    static SipMessage Response(SipMessage request, int statusCode)
    {
        string vias = string.Concat(request.Headers.Where(header =>
            IsHeader(header, SipHeaderNames.Via)
                || IsHeader(header, SipHeaderNames.CompactVia))
            .Select(header => "Via: " + header.Value + "\r\n"));
        return Parse("SIP/2.0 " + statusCode.ToString() + " Test\r\n"
            + vias
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=to-tag\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static void KeepFirstViaHeaderOnly(SipMessage response)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        bool keptVia = false;
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader header = response.Headers[index];
            if (!IsHeader(header, SipHeaderNames.Via)
                && !IsHeader(header, SipHeaderNames.CompactVia))
            {
                headers.Add(header);
            }
            else if (!keptVia)
            {
                headers.Add(header);
                keptVia = true;
            }
            index++;
        }
        response.Headers = headers.ToArray();
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
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
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

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => IsHeader(header, name));
    }

    static string Header(SipMessage message, string name)
    {
        return FindHeader(message, name)!.Value!;
    }

    static void AssertFailure(SipProxyResult result, int statusCode,
        string messageFragment)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        Assert.That(result.ErrorMessage, Does.Contain(messageFragment));
    }
}

public class CoverageProxyResolver : ISipDnsResolver
{
    public SipResolutionStatus Status = SipResolutionStatus.NoRecords;
    public SipServerCandidate? Candidate;
    public SipResolutionRequest? Request;
    public int Calls;

    public SipResolutionResult ResolveTarget(
        SipResolutionRequest request, SipDnsOptions options)
    {
        Calls++;
        Request = request;
        SipResolutionResult result = new SipResolutionResult();
        result.Status = Status;
        result.ErrorMessage = Status == SipResolutionStatus.Success
            ? null : "resolution failed";
        result.Candidates = Candidate == null
            ? new SipServerCandidate[0]
            : new SipServerCandidate[] { Candidate };
        return result;
    }

    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
    }

    public void FlushResolverCache()
    {
    }
}
