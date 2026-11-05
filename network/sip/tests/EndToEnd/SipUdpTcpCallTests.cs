namespace sip.Tests;

public class SipUdpTcpCallTests
{
    [TestCase(SipTransportKind.Udp)]
    [TestCase(SipTransportKind.Tcp)]
    public void CompletesDnsInviteRefreshAndByeCallFlow(
        SipTransportKind transport)
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        using SipHarnessDnsScript dns = new SipHarnessDnsScript();
        dns.AddAddress("uas.test", "192.0.2.88");
        SipResolutionResult resolution = Resolve(harness, dns, transport);

        SipMessage invite = Request(SipMethods.Invite, 1, "invite",
            null, "Session-Expires: 120;refresher=uac\r\n", Offer());
        List<SipStackEvent> inviteEvents = SendFromUac(
            harness, invite, transport, "invite");
        SipMessage receivedInvite = Incoming(inviteEvents, SipMethod.Invite);

        List<SipStackEvent> tryingEvents = SendFromUas(harness,
            Response(receivedInvite, 100, "Trying", null, null,
                Array.Empty<byte>()), transport, "trying");
        List<SipStackEvent> ringingEvents = SendFromUas(harness,
            Response(receivedInvite, 180, "Ringing", "uas-tag",
                "Contact: <sip:uas@uas.test>\r\n", Array.Empty<byte>()),
            transport, "ringing");
        SipMessage success = Response(receivedInvite, 200, "OK", "uas-tag",
            "Contact: <sip:uas@uas.test>\r\n"
                + "Session-Expires: 120;refresher=uac\r\n", Answer());
        List<SipStackEvent> successEvents = SendFromUas(
            harness, success, transport, "accepted");

        SipMessage ack = Request(SipMethods.Ack, 1, "ack", "uas-tag",
            null, Array.Empty<byte>());
        List<SipStackEvent> ackEvents = SendFromUac(
            harness, ack, transport, "ack");

        SipMessage reinvite = Request(SipMethods.Invite, 2, "reinvite",
            "uas-tag", null, Offer());
        List<SipStackEvent> reinviteEvents = SendFromUac(
            harness, reinvite, transport, "reinvite");
        SipMessage receivedReinvite = Incoming(
            reinviteEvents, SipMethod.Invite);
        SendFromUas(harness, Response(receivedReinvite, 200, "OK",
            "uas-tag", "Contact: <sip:uas@uas.test>\r\n", Answer()),
            transport, "reinvite-ok");
        SendFromUac(harness, Request(SipMethods.Ack, 2, "reinvite-ack",
            "uas-tag", null, Array.Empty<byte>()), transport,
            "reinvite-ack");

        SipMessage update = Request(SipMethods.Update, 3, "update",
            "uas-tag", null, Offer());
        List<SipStackEvent> updateEvents = SendFromUac(
            harness, update, transport, "update");
        SipMessage receivedUpdate = Incoming(updateEvents, SipMethod.Update);
        List<SipStackEvent> updateOkEvents = SendFromUas(harness,
            Response(receivedUpdate, 200, "OK", "uas-tag", null, Answer()),
            transport, "update-ok");

        SipMessage refresh = Request(SipMethods.Update, 4, "refresh",
            "uas-tag", "Session-Expires: 120;refresher=uac\r\n",
            Array.Empty<byte>());
        List<SipStackEvent> refreshEvents = SendFromUac(
            harness, refresh, transport, "session-refresh");
        SipMessage receivedRefresh = Incoming(refreshEvents, SipMethod.Update);
        List<SipStackEvent> refreshOkEvents = SendFromUas(harness,
            Response(receivedRefresh, 200, "OK", "uas-tag",
                "Session-Expires: 120;refresher=uac\r\n",
                Array.Empty<byte>()), transport, "refresh-ok");

        SipMessage bye = Request(SipMethods.Bye, 5, "bye", "uas-tag",
            null, Array.Empty<byte>());
        List<SipStackEvent> byeEvents = SendFromUac(
            harness, bye, transport, "bye");
        SipMessage receivedBye = Incoming(byeEvents, SipMethod.Bye);
        List<SipStackEvent> byeOkEvents = SendFromUas(harness,
            Response(receivedBye, 200, "OK", "uas-tag", null,
                Array.Empty<byte>()), transport, "bye-ok");

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Status,
                Is.EqualTo(SipResolutionStatus.Success),
                resolution.ErrorMessage);
            Assert.That(resolution.Candidates[0].Transport,
                Is.EqualTo(transport));
            Assert.That(ResponseStatus(tryingEvents), Is.EqualTo(100));
            Assert.That(ResponseStatus(ringingEvents), Is.EqualTo(180));
            Assert.That(ResponseStatus(successEvents), Is.EqualTo(200));
            Assert.That(ackEvents.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.Message!.Method == SipMethod.Ack), Is.True);
            Assert.That(receivedReinvite.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(receivedUpdate.Body, Is.EqualTo(Offer()));
            Assert.That(ResponseStatus(updateOkEvents), Is.EqualTo(200));
            Assert.That(HeaderValue(receivedRefresh, "Session-Expires"),
                Does.StartWith("120"));
            Assert.That(ResponseStatus(refreshOkEvents), Is.EqualTo(200));
            Assert.That(receivedBye.CSeq!.SequenceNumber, Is.EqualTo(5));
            Assert.That(ResponseStatus(byeOkEvents), Is.EqualTo(200));
        });
    }

    [Test]
    public void LostUdpInviteIsRetransmittedButTcpNeverRetransmits()
    {
        using SipInProcessHarness udp = new SipInProcessHarness();
        udp.Network.DropNextPacketCount = 1;
        SipSendResult udpSend = udp.SendFromUac(
            Request(SipMethods.Invite, 10, "lost-udp", null, null, Offer()),
            SipTransportKind.Udp, "lost-udp");
        SipInProcessHarness.Drain(udp.Uac);
        udp.DeliverPackets();
        List<SipStackEvent> initiallyLost =
            SipInProcessHarness.Drain(udp.Uas);
        udp.AdvanceMilliseconds(10);
        List<SipStackEvent> retransmissionEvents =
            SipInProcessHarness.Drain(udp.Uac);
        udp.DeliverPackets();
        List<SipStackEvent> retransmitted =
            SipInProcessHarness.Drain(udp.Uas);

        using SipInProcessHarness tcp = new SipInProcessHarness();
        tcp.Network.DropNextPacketCount = 1;
        SipSendResult tcpSend = tcp.SendFromUac(
            Request(SipMethods.Invite, 11, "lost-tcp", null, null, Offer()),
            SipTransportKind.Tcp, "lost-tcp");
        SipInProcessHarness.Drain(tcp.Uac);
        tcp.AdvanceMilliseconds(10);
        tcp.DeliverPackets();
        List<SipStackEvent> tcpReceived =
            SipInProcessHarness.Drain(tcp.Uas);

        using SipInProcessHarness failedRetry = new SipInProcessHarness();
        failedRetry.Network.DropNextPacketCount = 1;
        failedRetry.SendFromUac(Request(SipMethods.Invite, 12,
            "failed-retry", null, null, Offer()), SipTransportKind.Udp,
            "failed-retry");
        SipInProcessHarness.Drain(failedRetry.Uac);
        failedRetry.Network.FailNextSendCount = 1;
        failedRetry.AdvanceMilliseconds(10);
        List<SipStackEvent> failedRetryEvents =
            SipInProcessHarness.Drain(failedRetry.Uac);

        Assert.Multiple(() =>
        {
            Assert.That(udpSend.Success, Is.True);
            Assert.That(initiallyLost.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.False);
            Assert.That(retransmissionEvents.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.Retransmitted), Is.True);
            Assert.That(retransmitted.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest
                && value.Message!.Method == SipMethod.Invite), Is.True);
            Assert.That(tcpSend.Success, Is.True);
            Assert.That(tcpReceived.Any(value => value.Kind
                == SipStackEventKind.IncomingRequest), Is.False);
            Assert.That(failedRetryEvents.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.TransportError), Is.True);
            Assert.That(failedRetryEvents.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.Terminated), Is.True);
        });
    }

    [Test]
    public void TcpConnectionFailureTerminatesTheClientTransaction()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        harness.Network.FailNextSendCount = 1;

        SipSendResult result = harness.SendFromUac(
            Request(SipMethods.Invite, 20, "failed-tcp", null, null, Offer()),
            SipTransportKind.Tcp, "failed-tcp");
        List<SipStackEvent> events = SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        List<SipStackEvent> remote = SipInProcessHarness.Drain(harness.Uas);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("connection failed"));
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.TransportError), Is.True);
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Transaction
                && value.TransactionEvent!.Kind
                    == SipTransactionEventKind.Terminated), Is.True);
            Assert.That(events.Any(value => value.Kind
                == SipStackEventKind.Error
                && value.CorrelationId == "failed-tcp"), Is.True);
            Assert.That(remote, Is.Empty);
        });
    }

    static List<SipStackEvent> SendFromUac(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult result = harness.SendFromUac(
            message, transport, correlationId);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas);
    }

    static List<SipStackEvent> SendFromUas(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult result = harness.SendFromUas(
            message, transport, correlationId);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac);
    }

    static SipResolutionResult Resolve(SipInProcessHarness harness,
        SipHarnessDnsScript dns, SipTransportKind transport)
    {
        SipResolutionRequest request = new SipResolutionRequest();
        string transportName = transport == SipTransportKind.Tcp
            ? "tcp" : "udp";
        request.Uri = ParseUri("sip:service@uas.test:5060;transport="
            + transportName);
        request.Transport = transport;
        request.CorrelationId = "call-dns";
        return harness.Uac.GetEndpointDnsResolver().ResolveTarget(
            request, dns.CreateOptions());
    }

    static SipMessage Incoming(List<SipStackEvent> events, SipMethod method)
    {
        return events.First(value => value.Kind
            == SipStackEventKind.IncomingRequest
            && value.Message!.Method == method).Message!;
    }

    static int ResponseStatus(List<SipStackEvent> events)
    {
        return events.First(value => value.Kind
            == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null)
            .TransactionEvent!.Message!.StatusCode;
    }

    static SipMessage Request(string method, int sequence, string branch,
        string? toTag, string? extraHeaders, byte[] body)
    {
        string tag = toTag == null ? "" : ";tag=" + toTag;
        string contentType = body.Length == 0
            ? "" : "Content-Type: application/sdp\r\n";
        return Parse(method + " sip:uas@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP uac.test:5060;branch=z9hG4bK-" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: UAC <sip:uac@uac.test>;tag=uac-tag\r\n"
            + "To: UAS <sip:uas@uas.test>" + tag + "\r\n"
            + "Call-ID: end-to-end-call@uac.test\r\n"
            + "CSeq: " + sequence + " " + method + "\r\n"
            + "Contact: <sip:uac@uac.test>\r\n"
            + (extraHeaders ?? "") + contentType
            + "Content-Length: " + body.Length + "\r\n\r\n"
            + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipMessage Response(SipMessage request, int status, string reason,
        string? toTag, string? extraHeaders, byte[] body)
    {
        string tag = toTag == null ? "" : ";tag=" + toTag;
        string contentType = body.Length == 0
            ? "" : "Content-Type: application/sdp\r\n";
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + "Via: SIP/2.0/" + request.ViaValues[0].Transport + " "
            + request.ViaValues[0].Host + ":5060;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: UAC <sip:uac@uac.test>;tag=uac-tag\r\n"
            + "To: UAS <sip:uas@uas.test>" + tag + "\r\n"
            + "Call-ID: end-to-end-call@uac.test\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber + " "
            + request.MethodToken + "\r\n" + (extraHeaders ?? "")
            + contentType + "Content-Length: " + body.Length + "\r\n\r\n"
            + System.Text.Encoding.ASCII.GetString(body));
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => string.Equals(
            header.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static byte[] Offer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\no=uac 1 1 IN IP4 192.0.2.10\r\ns=call\r\nt=0 0\r\n");
    }

    static byte[] Answer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\no=uas 1 1 IN IP4 192.0.2.20\r\ns=call\r\nt=0 0\r\n");
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
}
