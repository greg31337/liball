namespace sip.Tests;

public class UdpSipTransportTests
{
    [Test]
    public void SendsAndReceivesParsedSipDatagramWithIpv4EndpointMetadata()
    {
        UdpSipTransport sender = new UdpSipTransport();
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress senderAddress = Bind(sender, "127.0.0.1", 2048, 8);
        SipEndpointAddress receiverAddress = Bind(receiver, "127.0.0.1", 2048, 8);
        Assert.That(sender.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));
        Assert.That(receiver.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));
        byte[] original = MessageBytes();
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.Strict = false;
        SipMessage message = new SipMessageCodec().ParseDatagramMessage(
            original, parseOptions).Message!;
        SipSendRequest request = new SipSendRequest();
        request.Message = message;
        request.RemoteAddress = receiverAddress;
        request.Transport = SipTransportKind.Udp;
        request.CorrelationId = "udp-ipv4";

        SipSendResult sent = sender.SendTransport(request);
        SipTransportEvent sentEvent = sender.PollTransportEvent();
        SipTransportEvent receivedEvent = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Success, Is.True, sent.ErrorMessage);
            Assert.That(sent.BytesSent, Is.GreaterThan(0));
            Assert.That(sent.CorrelationId, Is.EqualTo("udp-ipv4"));
            Assert.That(sentEvent.Kind, Is.EqualTo(SipTransportEventKind.MessageSent));
            Assert.That(sentEvent.CorrelationId, Is.EqualTo("udp-ipv4"));
            Assert.That(sentEvent.Message!.LocalAddress!.Port,
                Is.EqualTo(senderAddress.Port));
            Assert.That(sentEvent.Message.RemoteAddress!.Port,
                Is.EqualTo(receiverAddress.Port));
            Assert.That(receivedEvent.Kind,
                Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(receivedEvent.Message!.Transport,
                Is.EqualTo(SipTransportKind.Udp));
            Assert.That(receivedEvent.Message.LocalAddress!.Port,
                Is.EqualTo(receiverAddress.Port));
            Assert.That(receivedEvent.Message.RemoteAddress!.Port,
                Is.EqualTo(senderAddress.Port));
            Assert.That(receivedEvent.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), receivedEvent.ErrorMessage);
            Assert.That(receivedEvent.Message.Message!.Method,
                Is.EqualTo(SipMethod.Options));
            Assert.That(receivedEvent.Message.TimestampMilliseconds,
                Is.GreaterThanOrEqualTo(0));
        });

        sender.CloseTransport();
        receiver.CloseTransport();
        Assert.That(sender.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Closed));
        Assert.That(receiver.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Closed));
    }

    [Test]
    public void BindsAndExchangesIpv6DatagramsWhenIpv6IsAvailable()
    {
        if (!System.Net.Sockets.Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not available on this host.");
        }
        UdpSipTransport sender = new UdpSipTransport();
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress senderAddress = Bind(sender, "::1", 2048, 8);
        SipEndpointAddress receiverAddress = Bind(receiver, "::1", 2048, 8);
        sender.PollTransportEvent();
        receiver.PollTransportEvent();
        SipSendRequest request = new SipSendRequest();
        request.Bytes = MessageBytes();
        request.RemoteAddress = receiverAddress;

        SipSendResult result = sender.SendTransport(request);
        sender.PollTransportEvent();
        SipTransportEvent received = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(senderAddress.Host, Is.EqualTo("::1"));
            Assert.That(receiverAddress.Host, Is.EqualTo("::1"));
            Assert.That(received.Message!.RemoteAddress!.Host, Is.EqualTo("::1"));
            Assert.That(received.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), received.ErrorMessage);
        });
        sender.CloseTransport();
        receiver.CloseTransport();
    }

    [Test]
    public void AcceptsAnExactSizeDatagramAndPreservesTransportMetadata()
    {
        byte[] bytes = MessageBytes();
        UdpSipTransport sender = new UdpSipTransport();
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress senderAddress = Bind(sender, "127.0.0.1", bytes.Length, 8);
        SipEndpointAddress receiverAddress = Bind(receiver, "127.0.0.1", bytes.Length, 8);
        sender.PollTransportEvent();
        receiver.PollTransportEvent();
        SipSendRequest request = new SipSendRequest();
        request.Bytes = bytes;
        request.RemoteAddress = receiverAddress;
        request.CorrelationId = "exact-limit";

        SipSendResult result = sender.SendTransport(request);
        SipTransportEvent sent = sender.PollTransportEvent();
        SipTransportEvent received = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.BytesSent, Is.EqualTo(bytes.Length));
            Assert.That(result.CorrelationId, Is.EqualTo("exact-limit"));
            Assert.That(sent.Kind, Is.EqualTo(SipTransportEventKind.MessageSent));
            Assert.That(sent.Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(sent.CorrelationId, Is.EqualTo("exact-limit"));
            Assert.That(sent.Message!.Bytes, Is.EqualTo(bytes));
            Assert.That(sent.Message.LocalAddress!.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(sent.Message.LocalAddress.Port, Is.EqualTo(senderAddress.Port));
            Assert.That(sent.Message.RemoteAddress!.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(sent.Message.RemoteAddress.Port, Is.EqualTo(receiverAddress.Port));
            Assert.That(received.Message!.Bytes, Is.EqualTo(bytes));
            Assert.That(received.Message.LocalAddress!.Port, Is.EqualTo(receiverAddress.Port));
            Assert.That(received.Message.RemoteAddress!.Port, Is.EqualTo(senderAddress.Port));
            Assert.That(received.Message.Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(received.Message.ParseResult!.Consumed, Is.EqualTo(bytes.Length));
            Assert.That(received.Message.ParseResult.Status,
                Is.EqualTo(SipParseStatus.Complete), received.ErrorMessage);
        });
        sender.CloseTransport();
        receiver.CloseTransport();
    }

    [Test]
    public void EnforcesOneCompleteMessagePerReceivedDatagram()
    {
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress address = Bind(receiver, "127.0.0.1", 512, 8);
        receiver.PollTransportEvent();
        using System.Net.Sockets.UdpClient sender = new System.Net.Sockets.UdpClient(
            System.Net.Sockets.AddressFamily.InterNetwork);
        byte[] valid = MessageBytes();
        sender.Send(valid, new System.Net.IPEndPoint(
            System.Net.IPAddress.Loopback, address.Port));

        SipTransportEvent complete = PollReceived(receiver);

        byte[] trailing = valid.Concat(System.Text.Encoding.ASCII.GetBytes("junk")).ToArray();
        sender.Send(trailing, new System.Net.IPEndPoint(
            System.Net.IPAddress.Loopback, address.Port));
        SipTransportEvent rejected = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Message!.Bytes, Is.EqualTo(valid));
            Assert.That(complete.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(rejected.Message!.Bytes, Is.EqualTo(trailing));
            Assert.That(rejected.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(rejected.ErrorMessage, Does.Contain("exactly one"));
        });
        receiver.CloseTransport();
    }

    [Test]
    public void RejectsPipelinedAndTruncatedMessagesAsTerminalDatagramErrors()
    {
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress address = Bind(receiver, "127.0.0.1", 512, 8);
        receiver.PollTransportEvent();
        using System.Net.Sockets.UdpClient sender = new System.Net.Sockets.UdpClient(
            System.Net.Sockets.AddressFamily.InterNetwork);
        byte[] valid = MessageBytes();
        byte[] pipelined = valid.Concat(valid).ToArray();
        byte[] truncated = System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sip:service@example.com SIP/2.0\r\nContent-Length:");
        sender.Send(pipelined, new System.Net.IPEndPoint(
            System.Net.IPAddress.Loopback, address.Port));
        SipTransportEvent multiple = PollReceived(receiver);
        sender.Send(truncated, new System.Net.IPEndPoint(
            System.Net.IPAddress.Loopback, address.Port));
        SipTransportEvent incomplete = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(multiple.Message!.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(multiple.ErrorMessage, Does.Contain("exactly one"));
            Assert.That(multiple.Message.Bytes, Is.EqualTo(pipelined));
            Assert.That(incomplete.Message!.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(incomplete.ErrorMessage, Does.Contain("complete SIP message"));
            Assert.That(incomplete.Message.ParseResult.ErrorOffset,
                Is.EqualTo(truncated.Length));
            Assert.That(incomplete.Message.ParseResult.RequiredBytes,
                Is.GreaterThan(truncated.Length));
            Assert.That(incomplete.Message.Bytes, Is.EqualTo(truncated));
        });
        receiver.CloseTransport();
    }

    [Test]
    public void RejectsOversizedIncomingAndOutgoingDatagrams()
    {
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress receiverAddress = Bind(receiver, "127.0.0.1", 64, 8);
        receiver.PollTransportEvent();
        using System.Net.Sockets.UdpClient external = new System.Net.Sockets.UdpClient(
            System.Net.Sockets.AddressFamily.InterNetwork);
        byte[] oversized = Enumerable.Repeat((byte)'x', 100).ToArray();
        external.Send(oversized, new System.Net.IPEndPoint(
            System.Net.IPAddress.Loopback, receiverAddress.Port));

        SipTransportEvent incoming = PollReceived(receiver);

        UdpSipTransport sender = new UdpSipTransport();
        Bind(sender, "127.0.0.1", 64, 8);
        sender.PollTransportEvent();
        SipSendRequest request = new SipSendRequest();
        request.Bytes = oversized;
        request.RemoteAddress = receiverAddress;
        request.CorrelationId = "too-large";
        SipSendResult outgoing = sender.SendTransport(request);
        SipTransportEvent failure = sender.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(incoming.Kind,
                Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(incoming.Message!.Bytes, Has.Length.EqualTo(65));
            Assert.That(incoming.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(incoming.ErrorMessage, Does.Contain("size limit"));
            Assert.That(outgoing.Success, Is.False);
            Assert.That(outgoing.BytesSent, Is.Zero);
            Assert.That(failure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(failure.CorrelationId, Is.EqualTo("too-large"));
        });
        sender.CloseTransport();
        receiver.CloseTransport();
    }

    [Test]
    public void HonorsCancellationDeadlinesQueueLimitsAndClosedState()
    {
        UdpSipTransport transport = new UdpSipTransport();
        SipEndpointAddress local = Bind(transport, "127.0.0.1", 2048, 2);
        transport.PollTransportEvent();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        SipSendRequest cancelled = SendRequest(local);
        cancelled.Cancellation = cancellation;
        cancelled.CorrelationId = "cancelled";

        SipSendResult cancelledResult = transport.SendTransport(cancelled);
        SipTransportEvent cancelledEvent = transport.PollTransportEvent();

        SipSendRequest expired = SendRequest(local);
        expired.DeadlineMilliseconds = System.Environment.TickCount64 - 1;
        SipSendResult expiredResult = transport.SendTransport(expired);
        SipTransportEvent expiredEvent = transport.PollTransportEvent();

        SipSendRequest wrongTransport = SendRequest(local);
        wrongTransport.Transport = SipTransportKind.Tcp;
        SipSendResult wrongTransportResult = transport.SendTransport(wrongTransport);
        SipTransportEvent wrongTransportEvent = transport.PollTransportEvent();

        transport.CloseTransport();
        SipTransportEvent closedEvent = transport.PollTransportEvent();
        SipSendResult closedResult = transport.SendTransport(SendRequest(local));
        SipTransportEvent closedFailure = transport.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(cancelledResult.Success, Is.False);
            Assert.That(cancelledEvent.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(cancelledEvent.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(expiredResult.Success, Is.False);
            Assert.That(expiredEvent.ErrorMessage, Does.Contain("deadline"));
            Assert.That(wrongTransportResult.Success, Is.False);
            Assert.That(wrongTransportEvent.ErrorMessage, Does.Contain("other than UDP"));
            Assert.That(closedEvent.Kind, Is.EqualTo(SipTransportEventKind.Closed));
            Assert.That(closedResult.Success, Is.False);
            Assert.That(closedFailure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(() => transport.ConnectTransport(local),
                Throws.TypeOf<SipTransportException>());
        });

        UdpSipTransport pressured = new UdpSipTransport();
        SipEndpointAddress pressuredAddress = Bind(pressured, "127.0.0.1", 2048, 1);
        SipSendResult pressureResult = pressured.SendTransport(
            SendRequest(pressuredAddress));
        Assert.That(pressureResult.Success, Is.False);
        Assert.That(pressureResult.ErrorMessage, Does.Contain("queue is full"));
        Assert.That(pressured.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));
        pressured.CloseTransport();
    }

    [Test]
    public void QueuePressurePreventsSendingAndRecoversAfterEventsAreDrained()
    {
        UdpSipTransport sender = new UdpSipTransport();
        UdpSipTransport receiver = new UdpSipTransport();
        SipEndpointAddress senderAddress = Bind(sender, "127.0.0.1", 2048, 1);
        SipEndpointAddress receiverAddress = Bind(receiver, "127.0.0.1", 2048, 4);
        receiver.PollTransportEvent();
        SipSendRequest first = SendRequest(receiverAddress);
        first.CorrelationId = "first";
        SipSendRequest blocked = SendRequest(receiverAddress);
        blocked.CorrelationId = "blocked";
        SipSendRequest resumed = SendRequest(receiverAddress);
        resumed.CorrelationId = "resumed";

        SipSendResult boundPressure = sender.SendTransport(first);
        SipTransportEvent bound = sender.PollTransportEvent();
        SipSendResult firstResult = sender.SendTransport(first);
        SipSendResult blockedResult = sender.SendTransport(blocked);
        SipTransportEvent firstSent = sender.PollTransportEvent();
        SipSendResult resumedResult = sender.SendTransport(resumed);
        SipTransportEvent resumedSent = sender.PollTransportEvent();
        SipTransportEvent firstReceived = PollReceived(receiver);
        SipTransportEvent resumedReceived = PollReceived(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(senderAddress.Port, Is.GreaterThan(0));
            Assert.That(boundPressure.Success, Is.False);
            Assert.That(boundPressure.ErrorMessage, Does.Contain("queue is full"));
            Assert.That(bound.Kind, Is.EqualTo(SipTransportEventKind.Bound));
            Assert.That(firstResult.Success, Is.True, firstResult.ErrorMessage);
            Assert.That(blockedResult.Success, Is.False);
            Assert.That(blockedResult.ErrorMessage, Does.Contain("queue is full"));
            Assert.That(firstSent.Kind, Is.EqualTo(SipTransportEventKind.MessageSent));
            Assert.That(firstSent.CorrelationId, Is.EqualTo("first"));
            Assert.That(resumedResult.Success, Is.True, resumedResult.ErrorMessage);
            Assert.That(resumedSent.CorrelationId, Is.EqualTo("resumed"));
            Assert.That(firstReceived.Message!.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(resumedReceived.Message!.Bytes, Is.EqualTo(resumed.Bytes));
        });
        sender.CloseTransport();
        receiver.CloseTransport();
    }

    [Test]
    public void ReportsEndpointFamilyAndSocketSendFailuresWithoutThrowing()
    {
        UdpSipTransport sender = new UdpSipTransport();
        Bind(sender, "127.0.0.1", 2048, 8);
        sender.PollTransportEvent();
        SipEndpointAddress namedRemote = new SipEndpointAddress();
        namedRemote.Host = "example.com";
        namedRemote.Port = 5060;
        SipSendRequest invalid = SendRequest(namedRemote);
        invalid.CorrelationId = "invalid-endpoint";
        SipEndpointAddress ipv6Remote = new SipEndpointAddress();
        ipv6Remote.Host = "::1";
        ipv6Remote.Port = 5060;
        SipSendRequest wrongFamily = SendRequest(ipv6Remote);
        wrongFamily.CorrelationId = "wrong-family";
        SipEndpointAddress broadcastRemote = new SipEndpointAddress();
        broadcastRemote.Host = "255.255.255.255";
        broadcastRemote.Port = 5060;
        SipSendRequest socketFailure = SendRequest(broadcastRemote);
        socketFailure.CorrelationId = "socket-failure";

        SipSendResult invalidResult = sender.SendTransport(invalid);
        SipTransportEvent invalidEvent = sender.PollTransportEvent();
        SipSendResult familyResult = sender.SendTransport(wrongFamily);
        SipTransportEvent familyEvent = sender.PollTransportEvent();
        SipSendResult socketResult = sender.SendTransport(socketFailure);
        SipTransportEvent socketEvent = sender.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidEvent.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(invalidEvent.CorrelationId, Is.EqualTo("invalid-endpoint"));
            Assert.That(invalidEvent.ErrorMessage, Does.Contain("endpoint is invalid"));
            Assert.That(familyResult.Success, Is.False);
            Assert.That(familyEvent.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(familyEvent.CorrelationId, Is.EqualTo("wrong-family"));
            Assert.That(familyEvent.ErrorMessage, Does.Contain("address family"));
            Assert.That(socketResult.Success, Is.False);
            Assert.That(socketEvent.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(socketEvent.CorrelationId, Is.EqualTo("socket-failure"));
            Assert.That(socketEvent.ErrorMessage, Does.Contain("send failed"));
        });
        sender.CloseTransport();
    }

    [Test]
    public void CloseIsIdempotentReplacesQueuedEventsAndReleasesTheSocket()
    {
        UdpSipTransport original = new UdpSipTransport();
        SipEndpointAddress address = Bind(original, "127.0.0.1", 2048, 1);

        original.CloseTransport();
        original.CloseTransport();
        SipTransportEvent closed = original.PollTransportEvent();
        SipTransportEvent none = original.PollTransportEvent();

        UdpSipTransport replacement = new UdpSipTransport();
        SipEndpointAddress rebound = BindAt(replacement, "127.0.0.1",
            address.Port, 2048, 2);

        Assert.Multiple(() =>
        {
            Assert.That(closed.Kind, Is.EqualTo(SipTransportEventKind.Closed));
            Assert.That(closed.Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(none.Kind, Is.EqualTo(SipTransportEventKind.None));
            Assert.That(rebound.Port, Is.EqualTo(address.Port));
            Assert.That(() => original.BindTransport(new SipTransportOptions(),
                new SipTlsOptions(), new SipWebSocketOptions()),
                Throws.TypeOf<SipTransportException>());
        });
        replacement.CloseTransport();
    }

    static SipEndpointAddress Bind(UdpSipTransport transport, string host,
        int maximumDatagramBytes, int maximumQueueCount)
    {
        return BindAt(transport, host, 0, maximumDatagramBytes, maximumQueueCount);
    }

    static SipEndpointAddress BindAt(UdpSipTransport transport, string host, int port,
        int maximumDatagramBytes, int maximumQueueCount)
    {
        SipEndpointAddress local = new SipEndpointAddress();
        local.Host = host;
        local.Port = port;
        local.Transport = SipTransportKind.Udp;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.Udp;
        options.LocalAddress = local;
        options.MaxDatagramBytes = maximumDatagramBytes;
        options.MaxQueueCount = maximumQueueCount;
        return transport.BindTransport(options, new SipTlsOptions(),
            new SipWebSocketOptions());
    }

    static SipSendRequest SendRequest(SipEndpointAddress remote)
    {
        SipSendRequest request = new SipSendRequest();
        request.Bytes = MessageBytes();
        request.RemoteAddress = remote;
        request.Transport = SipTransportKind.Udp;
        return request;
    }

    static SipTransportEvent PollReceived(UdpSipTransport transport)
    {
        long deadline = System.Environment.TickCount64 + 2000;
        SipTransportEvent result = transport.PollTransportEvent();
        while (result.Kind == SipTransportEventKind.None
            && System.Environment.TickCount64 < deadline)
        {
            System.Threading.Thread.Sleep(2);
            result = transport.PollTransportEvent();
        }
        Assert.That(result.Kind, Is.EqualTo(SipTransportEventKind.MessageReceived));
        return result;
    }

    static byte[] MessageBytes()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sip:service@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
    }
}
