namespace sip.Tests;

public class SipViaRoutingTests
{
    [Test]
    public void AddsValuelessAliasOnlyToTlsRequestVia()
    {
        SipMessage tls = ParseRequest(
            "Via: SIP/2.0/TLS client.example.com:5061;branch=z9hG4bK-tls\r\n");
        SipMessage tcp = ParseRequest(
            "Via: SIP/2.0/TCP client.example.com:5060;branch=z9hG4bK-tcp\r\n");

        bool added = SipViaRouting.AddRequestViaAlias(tls);
        bool repeated = SipViaRouting.AddRequestViaAlias(tls);
        bool insecure = SipViaRouting.AddRequestViaAlias(tcp);
        SipMessage reparsed = RoundTrip(tls);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(repeated, Is.True);
            Assert.That(insecure, Is.False);
            Assert.That(tls.Headers[0].Value, Does.EndWith(";alias"));
            Assert.That(reparsed.ViaValues[0].Parameters.Count(parameter =>
                parameter.Name == "alias"), Is.EqualTo(1));
            Assert.That(reparsed.ViaValues[0].Parameters.Single(parameter =>
                parameter.Name == "alias").HasValue, Is.False);
        });
    }

    [Test]
    public void InsertsTopViaIntoTypedAndWireHeaderCollections()
    {
        SipMessage request = ParseRequest("X-Trace: keep\r\n"
            + "Via: SIP/2.0/UDP previous.example.com;branch=z9hG4bK-previous\r\n");
        SipEndpointAddress sentBy = Endpoint("2001:db8::10", 5070, SipTransportKind.Tcp);

        SipViaValue inserted = SipViaRouting.InsertRequestVia(
            request, sentBy, "z9hG4bK-new", true);
        SipMessage reparsed = RoundTrip(request);

        Assert.Multiple(() =>
        {
            Assert.That(inserted, Is.SameAs(request.ViaValues[0]));
            Assert.That(request.Headers[0].Name, Is.EqualTo("X-Trace"));
            Assert.That(request.Headers[1].Name, Is.EqualTo(SipHeaderNames.Via));
            Assert.That(request.Headers[1].Value,
                Is.EqualTo("SIP/2.0/TCP [2001:db8::10]:5070;branch=z9hG4bK-new;rport"));
            Assert.That(request.OriginalBytes, Is.Empty);
            Assert.That(reparsed.ViaValues, Has.Length.EqualTo(2));
            Assert.That(reparsed.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-new"));
            Assert.That(reparsed.ViaValues[0].HasRPort, Is.True);
            Assert.That(reparsed.ViaValues[0].RPort, Is.EqualTo(-1));
            Assert.That(reparsed.ViaValues[1].Branch, Is.EqualTo("z9hG4bK-previous"));
        });
    }

    [Test]
    public void MatchesAndRemovesOnlyTheExpectedTopVia()
    {
        SipMessage response = ParseResponse(
            "v: SIP/2.0/UDP Proxy.Example.com:5060;branch=z9hG4bK-top, "
            + "SIP/2.0/TCP middle.example.com:5070;branch=z9hG4bK-middle\r\n"
            + "Via: SIP/2.0/TLS last.example.com:5061;branch=z9hG4bK-last\r\n");

        bool exactMatch = SipViaRouting.TopViaMatches(
            response, "proxy.example.com", 5060, "z9hG4bK-top");
        bool wrongPort = SipViaRouting.TopViaMatches(
            response, "proxy.example.com", 5061, "z9hG4bK-top");
        bool wrongBranch = SipViaRouting.RemoveResponseVia(
            response, "proxy.example.com", 5060, "z9hG4bK-wrong");
        bool removedTop = SipViaRouting.RemoveResponseVia(
            response, "proxy.example.com", 5060, "z9hG4bK-top");
        SipMessage afterTop = RoundTrip(response);
        bool removedMiddle = SipViaRouting.RemoveResponseVia(
            response, "MIDDLE.EXAMPLE.COM", 5070, "z9hG4bK-middle");
        SipMessage afterMiddle = RoundTrip(response);

        Assert.Multiple(() =>
        {
            Assert.That(exactMatch, Is.True);
            Assert.That(wrongPort, Is.False);
            Assert.That(wrongBranch, Is.False);
            Assert.That(removedTop, Is.True);
            Assert.That(afterTop.ViaValues.Select(value => value.Branch),
                Is.EqualTo(new string[] { "z9hG4bK-middle", "z9hG4bK-last" }));
            Assert.That(removedMiddle, Is.True);
            Assert.That(afterMiddle.ViaValues, Has.Length.EqualTo(1));
            Assert.That(afterMiddle.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-last"));
            Assert.That(response.Headers.Single(header =>
                header.Name!.Equals("Via", System.StringComparison.OrdinalIgnoreCase)).Value,
                Does.Contain("last.example.com"));
        });
    }

    [Test]
    public void MatchesDefaultPortsAndEquivalentIpv6SourceAddresses()
    {
        SipMessage udpResponse = ParseResponse(
            "Via: SIP/2.0/UDP proxy.example.com;branch=z9hG4bK-udp\r\n");
        SipMessage tlsResponse = ParseResponse(
            "Via: SIP/2.0/TLS proxy.example.com;branch=z9hG4bK-tls\r\n");
        SipMessage ipv6Request = ParseRequest(
            "Via: SIP/2.0/UDP [2001:0db8:0:0:0:0:0:1];"
            + "branch=z9hG4bK-ipv6\r\n");

        bool udpDefault = SipViaRouting.TopViaMatches(
            udpResponse, "PROXY.EXAMPLE.COM", SipTransportNames.SipPort,
            "z9hG4bK-udp");
        bool udpUnspecified = SipViaRouting.TopViaMatches(
            udpResponse, "proxy.example.com", -1, "z9hG4bK-udp");
        bool tlsDefault = SipViaRouting.TopViaMatches(
            tlsResponse, "proxy.example.com", SipTransportNames.SipsPort,
            "z9hG4bK-tls");
        bool wrongDefault = SipViaRouting.TopViaMatches(
            tlsResponse, "proxy.example.com", SipTransportNames.SipPort,
            "z9hG4bK-tls");
        bool updated = SipViaRouting.ApplyReceivedAndRPort(ipv6Request,
            Endpoint("2001:db8::1", 62000, SipTransportKind.Udp));

        Assert.Multiple(() =>
        {
            Assert.That(udpDefault, Is.True);
            Assert.That(udpUnspecified, Is.True);
            Assert.That(tlsDefault, Is.True);
            Assert.That(wrongDefault, Is.False);
            Assert.That(updated, Is.True);
            Assert.That(ipv6Request.ViaValues[0].Received, Is.Null);
            Assert.That(ipv6Request.ViaValues[0].Parameters.Select(
                parameter => parameter.Name),
                Does.Not.Contain("received"));
        });
    }

    [Test]
    public void UpdatesReceivedAndValuelessRPortFromObservedSource()
    {
        SipMessage requested = ParseRequest(
            "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-one;rport;foo=bar\r\n");
        SipViaValue originalTop = requested.ViaValues[0];
        SipEndpointAddress observed = Endpoint("2001:db8::20", 62000, SipTransportKind.Udp);

        bool updated = SipViaRouting.ApplyReceivedAndRPort(requested, observed);
        SipMessage reparsed = RoundTrip(requested);

        SipMessage sameAddress = ParseRequest(
            "Via: SIP/2.0/UDP 192.0.2.10:5060;branch=z9hG4bK-two\r\n");
        bool sameUpdated = SipViaRouting.ApplyReceivedAndRPort(sameAddress,
            Endpoint("192.0.2.10", 51000, SipTransportKind.Udp));

        SipMessage mismatched = ParseRequest(
            "Via: SIP/2.0/UDP 192.0.2.11:5060;branch=z9hG4bK-three;received=192.0.2.99\r\n");
        bool mismatchUpdated = SipViaRouting.ApplyReceivedAndRPort(mismatched,
            Endpoint("192.0.2.12", 51001, SipTransportKind.Udp));

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(requested.ViaValues[0], Is.Not.SameAs(originalTop));
            Assert.That(requested.ViaValues[0].Received, Is.EqualTo("2001:db8::20"));
            Assert.That(requested.ViaValues[0].RPort, Is.EqualTo(62000));
            Assert.That(requested.ViaValues[0].Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "branch", "received", "rport", "foo" }));
            Assert.That(reparsed.ViaValues[0].Received, Is.EqualTo("2001:db8::20"));
            Assert.That(reparsed.ViaValues[0].RPort, Is.EqualTo(62000));
            Assert.That(sameUpdated, Is.True);
            Assert.That(sameAddress.ViaValues[0].Received, Is.Null);
            Assert.That(mismatchUpdated, Is.True);
            Assert.That(mismatched.ViaValues[0].Received, Is.EqualTo("192.0.2.12"));
        });
    }

    [Test]
    public void CalculatesUnreliableResponseDestinationsInProtocolOrder()
    {
        SipTransportMessage received = new SipTransportMessage();
        received.LocalAddress = Endpoint("192.0.2.1", 5060, SipTransportKind.Udp);

        SipSendRequest multicast = SipViaRouting.CreateResponseSendRequest(ParseResponse(
            "Via: SIP/2.0/UDP client.example.com:5090;branch=z9hG4bK-a;received=198.51.100.1;"
            + "rport=62000;maddr=224.0.1.75;ttl=5\r\n"), received);
        SipSendRequest symmetric = SipViaRouting.CreateResponseSendRequest(ParseResponse(
            "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-b;"
            + "received=198.51.100.2;rport=62001\r\n"), received);
        SipSendRequest receivedOnly = SipViaRouting.CreateResponseSendRequest(ParseResponse(
            "Via: SIP/2.0/UDP client.example.com:5080;branch=z9hG4bK-c;"
            + "received=198.51.100.3\r\n"), received);
        SipSendRequest sentBy = SipViaRouting.CreateResponseSendRequest(ParseResponse(
            "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-d\r\n"), received);

        Assert.Multiple(() =>
        {
            AssertDestination(multicast, "224.0.1.75", 5090, SipTransportKind.Udp);
            Assert.That(multicast.LocalAddress, Is.Null);
            AssertDestination(symmetric, "198.51.100.2", 62001, SipTransportKind.Udp);
            Assert.That(symmetric.LocalAddress, Is.Not.SameAs(received.LocalAddress));
            Assert.That(symmetric.LocalAddress!.Host, Is.EqualTo("192.0.2.1"));
            AssertDestination(receivedOnly, "198.51.100.3", 5080, SipTransportKind.Udp);
            AssertDestination(sentBy, "client.example.com", SipTransportNames.SipPort,
                SipTransportKind.Udp);
        });
    }

    [Test]
    public void PrefersOpenReliableConnectionAndFallsBackWhenClosed()
    {
        SipMessage response = ParseResponse(
            "Via: SIP/2.0/TCP client.example.com:5070;branch=z9hG4bK-one;"
            + "received=198.51.100.20;rport=62000\r\n");
        SipTransportMessage received = new SipTransportMessage();
        received.LocalAddress = Endpoint("192.0.2.5", 5060, SipTransportKind.Tcp);
        received.RemoteAddress = Endpoint("198.51.100.20", 62000, SipTransportKind.Tcp);
        received.Connection = Connection("connection-1", SipConnectionState.Connected,
            Endpoint("192.0.2.6", 5070, SipTransportKind.Tcp),
            Endpoint("198.51.100.21", 62001, SipTransportKind.Tcp));

        SipSendRequest affine = SipViaRouting.CreateResponseSendRequest(response, received);
        received.Connection.State = SipConnectionState.Closed;
        SipSendRequest fallback = SipViaRouting.CreateResponseSendRequest(response, received);

        Assert.Multiple(() =>
        {
            Assert.That(affine.ConnectionId, Is.EqualTo("connection-1"));
            AssertDestination(affine, "198.51.100.21", 62001, SipTransportKind.Tcp);
            Assert.That(affine.LocalAddress!.Host, Is.EqualTo("192.0.2.6"));
            Assert.That(affine.RemoteAddress, Is.Not.SameAs(received.Connection.RemoteAddress));
            Assert.That(fallback.ConnectionId, Is.Null);
            AssertDestination(fallback, "198.51.100.20", 5070, SipTransportKind.Tcp);
            Assert.That(fallback.LocalAddress, Is.Null);
        });
    }

    [Test]
    public void RejectsInvalidViaRoutingInputsWithoutPartialMutation()
    {
        SipMessage request = ParseRequest(
            "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-one\r\n");
        SipMessage response = ParseResponse(
            "Via: SIP/2.0/FUTURE client.example.com;branch=z9hG4bK-one\r\n");
        SipEndpointAddress sentBy = Endpoint("client.example.com", 5060, SipTransportKind.Udp);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => SipViaRouting.InsertRequestVia(
                request, sentBy, "not-a-cookie", false));
            Assert.That(request.ViaValues, Has.Length.EqualTo(1));
            Assert.That(SipViaRouting.ApplyReceivedAndRPort(request,
                Endpoint("not-an-address.example", 5000, SipTransportKind.Udp)), Is.False);
            Assert.Throws<SipProtocolException>(() => SipViaRouting.CreateResponseSendRequest(
                response, new SipTransportMessage()));
        });
    }

    static SipMessage ParseRequest(string viaHeaders)
    {
        return ParseMessage("OPTIONS sip:service@example.com SIP/2.0\r\n"
            + viaHeaders
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>\r\n"
            + "Call-ID: routing-call@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ParseResponse(string viaHeaders)
    {
        return ParseMessage("SIP/2.0 200 OK\r\n"
            + viaHeaders
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>;tag=to-tag\r\n"
            + "Call-ID: routing-call@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Content-Length: 0\r\n\r\n");
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

    static SipEndpointAddress Endpoint(string host, int port, SipTransportKind transport)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = transport;
        return endpoint;
    }

    static SipConnectionInfo Connection(string id, SipConnectionState state,
        SipEndpointAddress local, SipEndpointAddress remote)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = state;
        connection.Transport = SipTransportKind.Tcp;
        connection.LocalAddress = local;
        connection.RemoteAddress = remote;
        return connection;
    }

    static void AssertDestination(SipSendRequest request, string host, int port,
        SipTransportKind transport)
    {
        Assert.That(request.Transport, Is.EqualTo(transport));
        Assert.That(request.RemoteAddress, Is.Not.Null);
        Assert.That(request.RemoteAddress!.Host, Is.EqualTo(host));
        Assert.That(request.RemoteAddress.Port, Is.EqualTo(port));
        Assert.That(request.RemoteAddress.Transport, Is.EqualTo(transport));
    }
}
