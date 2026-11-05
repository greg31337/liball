namespace sip.Tests;

public class SipSecureTransportCallTests
{
    [TestCase(SipTransportKind.Tls)]
    [TestCase(SipTransportKind.SecureWebSocket)]
    public void SecureCallAuthenticatesBothPeersAndReusesConnections(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipMessage invite = Request(SipMethods.Invite, 1, "secure-invite",
            "sips", transport, Array.Empty<byte>(), null);

        SipSendResult inviteSend = harness.SendFromUac(
            invite, transport, "secure-invite");
        harness.DeliverPackets();
        List<SipStackEvent> inviteEvents =
            SipInProcessHarness.Drain(harness.Uas);
        SipMessage receivedInvite = Incoming(inviteEvents, SipMethod.Invite);
        SipMessage accepted = Response(receivedInvite, 200, "OK", transport);
        SipSendResult acceptedSend = harness.SendFromUas(
            accepted, transport, "secure-ok");
        harness.DeliverPackets();
        SipInProcessHarness.Drain(harness.Uac);

        SipSendResult ackSend = harness.SendFromUac(
            Request(SipMethods.Ack, 1, "secure-ack", "sips",
                transport, Array.Empty<byte>(), "uas-tag"),
            transport, "secure-ack");
        harness.DeliverPackets();
        List<SipStackEvent> ackEvents =
            SipInProcessHarness.Drain(harness.Uas);
        SipSendResult byeSend = harness.SendFromUac(
            Request(SipMethods.Bye, 2, "secure-bye", "sips",
                transport, Array.Empty<byte>(), "uas-tag"),
            transport, "secure-bye");
        harness.DeliverPackets();
        List<SipStackEvent> byeEvents =
            SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            AssertSecureConnection(inviteSend.Connection, "server.test",
                transport);
            AssertSecureConnection(acceptedSend.Connection, "client.test",
                transport);
            Assert.That(ackSend.Connection!.Id,
                Is.EqualTo(inviteSend.Connection!.Id));
            Assert.That(byeSend.Connection!.Id,
                Is.EqualTo(inviteSend.Connection.Id));
            Assert.That(receivedInvite.RequestUri!.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(ackEvents.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.Message!.Method == SipMethod.Ack), Is.True);
            Assert.That(byeEvents.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.Message!.Method == SipMethod.Bye), Is.True);
        });
    }

    [TestCase(SipTransportKind.WebSocket)]
    [TestCase(SipTransportKind.SecureWebSocket)]
    public void WebSocketFramingPreservesCompleteSipMessages(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        harness.Network.FragmentBytes = 5;
        byte[] body = System.Text.Encoding.UTF8.GetBytes(
            "framed websocket payload");
        string scheme = transport == SipTransportKind.SecureWebSocket
            ? "sips" : "sip";

        SipSendResult first = harness.SendFromUac(
            Request(SipMethods.Message, 10, "websocket-one", scheme,
                transport, body, null), transport, "websocket-one");
        harness.DeliverPackets();
        List<SipStackEvent> received =
            SipInProcessHarness.Drain(harness.Uas);
        SipSendResult second = harness.SendFromUac(
            Request(SipMethods.Info, 11, "websocket-two", scheme,
                transport, body, null), transport, "websocket-two");
        harness.DeliverPackets();
        List<SipStackEvent> secondReceived =
            SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            Assert.That(harness.Network.LastFragmentCount,
                Is.GreaterThan(1));
            Assert.That(Incoming(received, SipMethod.Message).Body,
                Is.EqualTo(body));
            Assert.That(Incoming(secondReceived, SipMethod.Info).Body,
                Is.EqualTo(body));
            Assert.That(second.Connection!.Id,
                Is.EqualTo(first.Connection!.Id));
            Assert.That(first.Connection.Secure,
                Is.EqualTo(transport == SipTransportKind.SecureWebSocket));
        });
    }

    [TestCase(SipTransportKind.Tls)]
    [TestCase(SipTransportKind.WebSocket)]
    public void DisconnectReconnectsByEndpointAndRejectsStaleAffinity(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        string scheme = transport == SipTransportKind.Tls ? "sips" : "sip";
        SipSendResult first = harness.SendFromUac(
            Request(SipMethods.Options, 20, "reconnect-one", scheme,
                transport, Array.Empty<byte>(), null),
            transport, "reconnect-one");
        harness.DeliverPackets();
        SipInProcessHarness.Drain(harness.Uas);
        SipInProcessHarness.Drain(harness.Uac);

        harness.Network.Disconnect(true, transport);
        List<SipStackEvent> disconnected =
            SipInProcessHarness.Drain(harness.Uac);
        SipSendResult second = harness.SendFromUac(
            Request(SipMethods.Options, 21, "reconnect-two", scheme,
                transport, Array.Empty<byte>(), null),
            transport, "reconnect-two");

        SipSendRequest staleRequest = new SipSendRequest();
        staleRequest.Message = Request(SipMethods.Options, 22,
            "reconnect-stale", scheme, transport, Array.Empty<byte>(), null);
        staleRequest.Transport = transport;
        staleRequest.RemoteAddress = Endpoint(transport);
        staleRequest.ConnectionId = first.Connection!.Id;
        staleRequest.CorrelationId = "reconnect-stale";
        SipSendResult stale = harness.Uac.SendEndpointMessage(staleRequest);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Any(value => value.Kind
                == SipStackEventKind.Transport
                && value.TransportEvent!.Kind
                    == SipTransportEventKind.Disconnected), Is.True);
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(second.Connection!.Id,
                Is.Not.EqualTo(first.Connection.Id));
            Assert.That(stale.Success, Is.False);
            Assert.That(stale.ErrorMessage, Does.Contain("reconnect"));
        });
    }

    [TestCase(SipTransportKind.Udp)]
    [TestCase(SipTransportKind.Tcp)]
    [TestCase(SipTransportKind.WebSocket)]
    public void RejectsSipsDowngradeToInsecureTransport(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipSendResult result = harness.SendFromUac(
            Request(SipMethods.Invite, 30, "downgrade", "sips",
                transport, Array.Empty<byte>(), null),
            transport, "downgrade");
        harness.DeliverPackets();
        List<SipStackEvent> remote =
            SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("downgrade"));
            Assert.That(remote.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.False);
        });
    }

    [Test]
    public void RejectsInsecureTransportForSecureRouteAndDestination()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipMessage routed = Request(SipMethods.Options, 40,
            "secure-route", "sip", SipTransportKind.Tcp,
            Array.Empty<byte>(), null);
        string routedText = Serialize(routed).Replace(
            "Max-Forwards: 70\r\n",
            "Max-Forwards: 70\r\nRoute: <sips:proxy@proxy.test;lr>\r\n",
            StringComparison.Ordinal);
        routed = Parse(routedText.Replace("Contact: <sip:",
            "Contact: <sips:", StringComparison.Ordinal));
        SipSendResult routeResult = harness.SendFromUac(
            routed, SipTransportKind.Tcp, "secure-route");

        SipSendRequest destination = new SipSendRequest();
        destination.Message = Request(SipMethods.Options, 41,
            "secure-destination", "sip", SipTransportKind.Tcp,
            Array.Empty<byte>(), null);
        destination.Transport = SipTransportKind.Tcp;
        destination.RemoteAddress = Endpoint(SipTransportKind.Tcp);
        destination.RemoteAddress.Secure = true;
        destination.CorrelationId = "secure-destination";
        SipSendResult destinationResult =
            harness.Uac.SendEndpointMessage(destination);

        Assert.Multiple(() =>
        {
            Assert.That(routeResult.Success, Is.False);
            Assert.That(routeResult.ErrorMessage, Does.Contain("downgrade"));
            Assert.That(destinationResult.Success, Is.False);
            Assert.That(destinationResult.ErrorMessage,
                Does.Contain("downgrade"));
        });
    }

    static void AssertSecureConnection(SipConnectionInfo? connection,
        string domain, SipTransportKind transport)
    {
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection!.State, Is.EqualTo(SipConnectionState.Connected));
        Assert.That(connection.Transport, Is.EqualTo(transport));
        Assert.That(connection.Secure, Is.True);
        Assert.That(connection.PeerIdentity, Is.EqualTo(domain));
        Assert.That(connection.CertificateIdentity, Is.Not.Null);
        Assert.That(connection.CertificateIdentity!.ChainValid, Is.True);
        Assert.That(connection.CertificateIdentity.NameValid, Is.True);
        Assert.That(connection.CertificateIdentity.MutualTls, Is.True);
        Assert.That(connection.CertificateIdentity.Domains,
            Does.Contain(domain));
    }

    static SipMessage Incoming(List<SipStackEvent> events, SipMethod method)
    {
        return events.First(value => value.Kind
            == SipStackEventKind.IncomingRequest
            && value.Message!.Method == method).Message!;
    }

    static SipEndpointAddress Endpoint(SipTransportKind transport)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = "uas.test";
        result.Port = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket ? 5061 : 5060;
        result.Transport = transport;
        result.Secure = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
        return result;
    }

    static SipMessage Request(string method, int sequence, string branch,
        string scheme, SipTransportKind transport, byte[] body, string? toTag)
    {
        string tag = toTag == null ? "" : ";tag=" + toTag;
        string contentType = body.Length == 0
            ? "" : "Content-Type: application/octet-stream\r\n";
        return Parse(method + " " + scheme + ":uas@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/" + TransportToken(transport)
            + " uac.test;branch=z9hG4bK-" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: UAC <" + scheme
            + ":uac@uac.test>;tag=uac-tag\r\n"
            + "To: UAS <" + scheme + ":uas@uas.test>" + tag + "\r\n"
            + "Call-ID: secure-call@uac.test\r\n"
            + "CSeq: " + sequence + " " + method + "\r\n"
            + "Contact: <" + scheme + ":uac@uac.test>\r\n"
            + contentType + "Content-Length: " + body.Length + "\r\n\r\n"
            + System.Text.Encoding.UTF8.GetString(body));
    }

    static SipMessage Response(SipMessage request, int status, string reason,
        SipTransportKind transport)
    {
        string scheme = request.RequestUri!.Scheme == SipUriScheme.Sips
            ? "sips" : "sip";
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + "Via: SIP/2.0/" + TransportToken(transport)
            + " uac.test;branch=" + request.ViaValues[0].Branch + "\r\n"
            + "From: UAC <" + scheme
            + ":uac@uac.test>;tag=uac-tag\r\n"
            + "To: UAS <" + scheme + ":uas@uas.test>;tag=uas-tag\r\n"
            + "Call-ID: secure-call@uac.test\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber + " "
            + request.MethodToken + "\r\n"
            + "Contact: <" + scheme + ":uas@uas.test>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static string TransportToken(SipTransportKind transport)
    {
        if (transport == SipTransportKind.Tls) return "TLS";
        if (transport == SipTransportKind.WebSocket) return "WS";
        if (transport == SipTransportKind.SecureWebSocket) return "WSS";
        if (transport == SipTransportKind.Tcp) return "TCP";
        return "UDP";
    }

    static string Serialize(SipMessage message)
    {
        SipWriteResult result = new SipMessageCodec().SerializeMessage(
            message, new SipWriteOptions());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return System.Text.Encoding.ASCII.GetString(result.Bytes);
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }
}
