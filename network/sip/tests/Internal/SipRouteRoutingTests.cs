namespace sip.Tests;

public class SipRouteRoutingTests
{
    [Test]
    public void AppliesLooseRouteSetWithoutChangingRemoteTarget()
    {
        SipMessage request = ParseRequest("sip:old@example.com", "");
        SipUri remoteTarget = ParseUri("sip:bob@remote.example.com;transport=tcp");
        SipRouteValue[] routeSet = ParseRouteValues(
            "<sip:proxy1.example.com;lr;foo=one>, <sip:proxy2.example.com;lr>");

        SipRouteRouting.ApplyRouteSet(request, remoteTarget, routeSet);
        SipMessage reparsed = RoundTrip(request);

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestUri, Is.Not.SameAs(remoteTarget));
            Assert.That(request.RequestUri!.Host, Is.EqualTo("remote.example.com"));
            Assert.That(request.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "proxy1.example.com", "proxy2.example.com" }));
            Assert.That(request.RouteValues, Is.Not.SameAs(routeSet));
            Assert.That(request.Headers.Count(header => header.Name == SipHeaderNames.Route), Is.EqualTo(1));
            Assert.That(reparsed.RequestUri!.Host, Is.EqualTo("remote.example.com"));
            Assert.That(reparsed.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "proxy1.example.com", "proxy2.example.com" }));
        });
    }

    [Test]
    public void AppliesStrictRouteSetAndAppendsRemoteTarget()
    {
        SipMessage request = ParseRequest("sip:old@example.com", "");
        SipUri remoteTarget = ParseUri("sip:bob@remote.example.com;transport=tcp");
        SipRouteValue[] routeSet = ParseRouteValues(
            "<sip:strict.example.com?Subject=ignored>, <sip:loose.example.com;lr>");

        SipRouteRouting.ApplyRouteSet(request, remoteTarget, routeSet);
        SipMessage reparsed = RoundTrip(request);

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestUri!.Host, Is.EqualTo("strict.example.com"));
            Assert.That(request.RequestUri.Headers, Is.Empty);
            Assert.That(request.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "loose.example.com", "remote.example.com" }));
            Assert.That(request.RouteValues.Last().Address!.Uri!.User, Is.EqualTo("bob"));
            Assert.That(reparsed.RequestUri!.Host, Is.EqualTo("strict.example.com"));
            Assert.That(reparsed.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "loose.example.com", "remote.example.com" }));
        });
    }

    [Test]
    public void RecoversIncomingStrictRouteAndTransformsNextStrictHop()
    {
        SipUri localRoute = ParseUri("sip:proxy1.example.com");
        SipMessage request = ParseRequest("sip:proxy1.example.com",
            "Route: <sip:proxy2.example.com>, <sip:proxy3.example.com;lr>, "
            + "<sip:bob@remote.example.com>\r\n");

        bool preprocessed = SipRouteRouting.PreprocessIncomingRoute(request, localRoute);
        string[] afterPreprocessing = request.RouteValues.Select(RouteHost).ToArray();
        bool postprocessed = SipRouteRouting.PostprocessStrictRoute(request);
        SipMessage reparsed = RoundTrip(request);

        SipMessage loose = ParseRequest("sip:bob@remote.example.com",
            "Route: <sip:proxy1.example.com;lr>, <sip:next.example.com;lr>\r\n");
        bool consumed = SipRouteRouting.PreprocessIncomingRoute(loose,
            ParseUri("sip:proxy1.example.com;lr"));

        Assert.Multiple(() =>
        {
            Assert.That(preprocessed, Is.True);
            Assert.That(afterPreprocessing,
                Is.EqualTo(new string[] { "proxy2.example.com", "proxy3.example.com" }));
            Assert.That(postprocessed, Is.True);
            Assert.That(request.RequestUri!.Host, Is.EqualTo("proxy2.example.com"));
            Assert.That(request.RouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "proxy3.example.com", "remote.example.com" }));
            Assert.That(reparsed.RequestUri!.Host, Is.EqualTo("proxy2.example.com"));
            Assert.That(consumed, Is.True);
            Assert.That(loose.RequestUri!.Host, Is.EqualTo("remote.example.com"));
            Assert.That(loose.RouteValues.Single().Address!.Uri!.Host, Is.EqualTo("next.example.com"));
        });
    }

    [Test]
    public void ConstructsRecordRouteWithLooseAndSecureRoutingRequirements()
    {
        SipMessage secureRequest = ParseRequest("sips:bob@example.com",
            "Record-Route: <sips:previous.example.com;lr;trace=one>\r\n");
        SipUri suppliedLocal = ParseUri("sip:edge.example.com;transport=tcp");

        SipRouteValue inserted = SipRouteRouting.InsertRecordRoute(secureRequest, suppliedLocal);
        SipMessage reparsed = RoundTrip(secureRequest);

        SipMessage ordinaryRequest = ParseRequest("sip:bob@example.com", "");
        SipRouteValue ordinary = SipRouteRouting.InsertRecordRoute(ordinaryRequest,
            ParseUri("sips:edge.example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(inserted.Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(inserted.LooseRouting, Is.True);
            Assert.That(inserted.Address.Uri.Parameters.Select(parameter => parameter.Name),
                Does.Contain("lr"));
            Assert.That(suppliedLocal.Scheme, Is.EqualTo(SipUriScheme.Sip));
            Assert.That(secureRequest.RecordRouteValues.Select(RouteHost),
                Is.EqualTo(new string[] { "edge.example.com", "previous.example.com" }));
            Assert.That(reparsed.RecordRouteValues[0].Address!.Uri!.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(ordinary.Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sip));
        });
    }

    [Test]
    public void ReversesRecordRoutesWithIndependentCopies()
    {
        SipRouteValue[] records = ParseRouteValues(
            "<sip:first.example.com;lr;trace=one>, <sip:second.example.com;lr>, "
            + "<sip:third.example.com;lr>");

        SipRouteValue[] reversed = SipRouteRouting.ReverseRouteSet(records);
        reversed[2].Address!.Uri!.Host = "changed.example.com";
        reversed[2].Address!.Uri!.Parameters[1].Value = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(reversed.Select(RouteHost),
                Is.EqualTo(new string[] { "third.example.com", "second.example.com", "changed.example.com" }));
            Assert.That(records[0].Address!.Uri!.Host, Is.EqualTo("first.example.com"));
            Assert.That(records[0].Address!.Uri!.Parameters[1].Value, Is.EqualTo("one"));
        });
    }

    [Test]
    public void PreservesUasRouteDirectionAndReversesUacRouteDirection()
    {
        SipRouteValue[] responseRecords = ParseRouteValues(
            "<sip:edge.example.com;lr>, <sip:core.example.com;lr>, "
            + "<sip:origin.example.com;lr>");

        SipRouteValue[] uacRouteSet =
            SipRouteRouting.ReverseRouteSet(responseRecords);

        Assert.Multiple(() =>
        {
            Assert.That(responseRecords.Select(RouteHost), Is.EqualTo(
                new string[]
                {
                    "edge.example.com", "core.example.com", "origin.example.com"
                }));
            Assert.That(uacRouteSet.Select(RouteHost), Is.EqualTo(
                new string[]
                {
                    "origin.example.com", "core.example.com", "edge.example.com"
                }));
            Assert.That(uacRouteSet[0], Is.Not.SameAs(responseRecords[2]));
        });
    }

    [Test]
    public void SelectsRouteThenOutboundProxyThenRequestUri()
    {
        SipMessage routed = ParseRequest("sip:bob@target.example.com",
            "Route: <sip:route.example.com;lr>\r\n");
        SipUri outbound = ParseUri("sip:outbound.example.com;lr");
        SipUri routedHop = SipRouteRouting.SelectNextHop(routed, outbound);

        SipMessage proxied = ParseRequest("sip:bob@target.example.com", "");
        SipUri proxyHop = SipRouteRouting.SelectNextHop(proxied, outbound);
        SipUri directHop = SipRouteRouting.SelectNextHop(proxied, null);

        SipMessage secure = ParseRequest("sips:bob@secure.example.com",
            "Route: <sip:edge.example.com;lr>\r\n");
        SipUri secureHop = SipRouteRouting.SelectNextHop(secure, outbound);

        Assert.Multiple(() =>
        {
            Assert.That(routedHop.Host, Is.EqualTo("route.example.com"));
            Assert.That(proxyHop.Host, Is.EqualTo("outbound.example.com"));
            Assert.That(directHop.Host, Is.EqualTo("target.example.com"));
            Assert.That(secureHop.Host, Is.EqualTo("edge.example.com"));
            Assert.That(secureHop.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(secure.RouteValues[0].Address!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sip));
        });
    }

    [Test]
    public void SelectsContactRemoteTargetWithFallbackAndSecureChecks()
    {
        SipMessage response = ParseResponse(
            "Contact: <sip:bob@contact.example.com;transport=tcp>\r\n");
        SipUri fallback = ParseUri("sip:bob@fallback.example.com");
        SipUri contactTarget = SipRouteRouting.SelectRemoteTarget(response, fallback, false);

        SipMessage withoutContact = ParseResponse("");
        SipUri fallbackTarget = SipRouteRouting.SelectRemoteTarget(withoutContact, fallback, false);

        SipMessage secureResponse = ParseResponse(
            "Contact: <sips:bob@secure-contact.example.com>\r\n");
        SipUri secureTarget = SipRouteRouting.SelectRemoteTarget(
            secureResponse, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(contactTarget.Host, Is.EqualTo("contact.example.com"));
            Assert.That(contactTarget, Is.Not.SameAs(response.ContactValues[0].Address!.Uri));
            Assert.That(fallbackTarget.Host, Is.EqualTo("fallback.example.com"));
            Assert.That(secureTarget.Host, Is.EqualTo("secure-contact.example.com"));
            Assert.Throws<SipProtocolException>(() => SipRouteRouting.SelectRemoteTarget(
                response, null, true));
        });
    }

    [Test]
    public void RejectsStrictRoutingThatWouldDowngradeSips()
    {
        SipMessage request = ParseRequest("sip:unchanged.example.com", "");
        SipUri protectedTarget = ParseUri("sips:bob@secure.example.com");
        SipRouteValue[] insecureStrictRoute = ParseRouteValues("<sip:strict.example.com>");

        Assert.Throws<SipProtocolException>(() => SipRouteRouting.ApplyRouteSet(
            request, protectedTarget, insecureStrictRoute));

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestUri!.Host, Is.EqualTo("unchanged.example.com"));
            Assert.That(request.RouteValues, Is.Empty);
            Assert.That(request.Headers.Any(header => header.Name == SipHeaderNames.Route), Is.False);
        });
    }

    static SipMessage ParseRequest(string requestUri, string routingHeaders)
    {
        return ParseMessage("OPTIONS " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-route\r\n"
            + routingHeaders
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: route-call@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ParseResponse(string additionalHeaders)
    {
        return ParseMessage("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-route\r\n"
            + additionalHeaders
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: route-call@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipRouteValue[] ParseRouteValues(string value)
    {
        SipHeaderParseResult result = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes("Route: " + value + "\r\n"),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.RouteValues;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Uri!;
    }

    static SipMessage ParseMessage(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static SipMessage RoundTrip(SipMessage message)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;
        SipWriteResult written = new SipMessageCodec().SerializeMessage(message, options);
        Assert.That(written.Success, Is.True, written.ErrorMessage);
        return ParseMessage(System.Text.Encoding.UTF8.GetString(written.Bytes));
    }

    static string RouteHost(SipRouteValue route)
    {
        return route.Address!.Uri!.Host!;
    }
}
