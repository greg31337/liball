namespace sip.Tests;

public class SipReliableConnectionCoreTests
{
    [Test]
    public void ReassemblesEveryByteFragmentIntoOneStreamMessage()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipReliableConnectionCore core = Core("fragmented", 8, 60000, 0, clock,
            "198.51.100.1", 5060);
        Assert.That(core.PollEvent().Kind, Is.EqualTo(SipTransportEventKind.Connected));
        byte[] message = Message("fragmented");

        int index = 0;
        while (index < message.Length)
        {
            SipReliableOperationResult accepted = core.ReceiveBytes(message, index, 1);
            Assert.That(accepted.Success, Is.True, accepted.ErrorMessage);
            Assert.That(accepted.BytesAccepted, Is.EqualTo(1));
            if (index + 1 < message.Length)
            {
                Assert.That(core.PollEvent().Kind,
                    Is.EqualTo(SipTransportEventKind.None), index.ToString());
            }
            index++;
        }

        SipTransportEvent received = core.PollEvent();

        Assert.Multiple(() =>
        {
            Assert.That(received.Kind, Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(received.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(received.Connection!.Id, Is.EqualTo("fragmented"));
            Assert.That(received.Message!.Bytes, Is.EqualTo(message));
            Assert.That(received.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), received.ErrorMessage);
            Assert.That(received.Message.RemoteAddress!.Host,
                Is.EqualTo("198.51.100.1"));
            Assert.That(received.Message.TimestampMilliseconds, Is.EqualTo(1000));
            Assert.That(core.CountBufferedBytes(), Is.Zero);
        });
    }

    [Test]
    public void FramesPipelinedMessagesAndBackpressuresWithoutLosingBytes()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipReliableConnectionCore core = Core("pipeline", 2, 60000, 0, clock,
            "198.51.100.2", 5060);
        core.PollEvent();
        byte[] first = Message("one");
        byte[] second = Message("two");
        byte[] third = Message("three");
        byte[] pipeline = first.Concat(second).Concat(third).ToArray();

        SipReliableOperationResult accepted = core.ReceiveBytes(
            pipeline, 0, pipeline.Length);
        SipTransportEvent firstEvent = core.PollEvent();
        SipTransportEvent secondEvent = core.PollEvent();
        SipTransportEvent thirdEvent = core.PollEvent();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True, accepted.ErrorMessage);
            Assert.That(accepted.Backpressured, Is.True);
            Assert.That(firstEvent.Message!.Bytes, Is.EqualTo(first));
            Assert.That(secondEvent.Message!.Bytes, Is.EqualTo(second));
            Assert.That(thirdEvent.Message!.Bytes, Is.EqualTo(third));
            Assert.That(core.CountBufferedBytes(), Is.Zero);
        });

        SipReliableConnectionCore blocked = Core("blocked", 1, 60000, 0, clock,
            "198.51.100.3", 5060);
        SipReliableOperationResult refused = blocked.ReceiveBytes(first, 0, first.Length);
        Assert.Multiple(() =>
        {
            Assert.That(refused.Success, Is.False);
            Assert.That(refused.Backpressured, Is.True);
            Assert.That(refused.BytesAccepted, Is.Zero);
            Assert.That(blocked.CountBufferedBytes(), Is.Zero);
        });
        Assert.That(blocked.PollEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Connected));
        Assert.That(blocked.ReceiveBytes(first, 0, first.Length).Success, Is.True);
        Assert.That(blocked.PollEvent().Message!.Bytes, Is.EqualTo(first));
    }

    [Test]
    public void QueuesBoundedWritesAndAcknowledgesPartialOutput()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(2000);
        SipReliableConnectionCore core = Core("writes", 2, 60000, 0, clock,
            "203.0.113.10", 5060);
        core.PollEvent();
        SipSendRequest firstRequest = Request("first", -1, null);
        SipCancellation secondCancellation = new SipCancellation();
        SipSendRequest secondRequest = Request("second", -1, secondCancellation);

        SipReliableOperationResult firstQueued = core.QueueWrite(
            firstRequest, System.Text.Encoding.ASCII.GetBytes("abcdef"));
        SipReliableOperationResult secondQueued = core.QueueWrite(
            secondRequest, System.Text.Encoding.ASCII.GetBytes("second"));
        SipReliableOperationResult pressured = core.QueueWrite(
            Request("third", -1, null), System.Text.Encoding.ASCII.GetBytes("third"));
        SipReliableWriteChunk firstChunk = core.PollWrite(4);
        byte[] firstPart = firstChunk.Bytes.Skip(firstChunk.Offset)
            .Take(2).ToArray();
        SipReliableOperationResult firstPartial = core.CompleteWrite(2);
        SipReliableWriteChunk secondChunk = core.PollWrite(8);
        byte[] secondPart = secondChunk.Bytes.Skip(secondChunk.Offset)
            .Take(secondChunk.Count).ToArray();
        SipReliableOperationResult firstComplete = core.CompleteWrite(secondChunk.Count);
        SipTransportEvent sent = core.PollEvent();

        secondCancellation.Requested = true;
        SipReliableWriteChunk cancelledChunk = core.PollWrite(8);
        SipTransportEvent cancelled = core.PollEvent();

        Assert.Multiple(() =>
        {
            Assert.That(firstQueued.Success, Is.True);
            Assert.That(secondQueued.Success, Is.True);
            Assert.That(pressured.Success, Is.False);
            Assert.That(pressured.Backpressured, Is.True);
            Assert.That(firstChunk.Offset, Is.Zero);
            Assert.That(firstChunk.Count, Is.EqualTo(4));
            Assert.That(firstPartial.Success, Is.True);
            Assert.That(firstPartial.BytesAccepted, Is.EqualTo(2));
            Assert.That(secondChunk.Offset, Is.EqualTo(2));
            Assert.That(firstPart.Concat(secondPart).ToArray(),
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("abcdef")));
            Assert.That(firstComplete.Success, Is.True);
            Assert.That(sent.Kind, Is.EqualTo(SipTransportEventKind.MessageSent));
            Assert.That(sent.CorrelationId, Is.EqualTo("first"));
            Assert.That(sent.Message!.TimestampMilliseconds, Is.EqualTo(2000));
            Assert.That(cancelledChunk.Available, Is.False);
            Assert.That(cancelled.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(cancelled.CorrelationId, Is.EqualTo("second"));
            Assert.That(cancelled.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(core.CountQueuedWrites(), Is.Zero);
        });

        SipSendRequest deadline = Request("deadline", 2100, null);
        Assert.That(core.QueueWrite(deadline,
            System.Text.Encoding.ASCII.GetBytes("late")).Success, Is.True);
        clock.AdvanceMilliseconds(100);
        Assert.That(core.PollWrite(8).Available, Is.False);
        SipTransportEvent expired = core.PollEvent();
        Assert.That(expired.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
        Assert.That(expired.ErrorMessage, Does.Contain("deadline"));
    }

    [Test]
    public void SchedulesKeepAliveAndTracksIdleExpiryFromActivity()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipReliableConnectionCore core = Core("timed", 8, 500, 100, clock,
            "203.0.113.20", 5060);
        core.PollEvent();
        clock.AdvanceMilliseconds(99);
        Assert.That(core.PollWrite(8).Available, Is.False);
        clock.AdvanceMilliseconds(1);

        SipReliableWriteChunk first = core.PollWrite(2);
        SipReliableOperationResult partial = core.CompleteWrite(2);
        SipReliableWriteChunk second = core.PollWrite(2);
        SipReliableOperationResult complete = core.CompleteWrite(2);
        SipReliableOperationResult received = core.ReceiveBytes(
            new byte[] { 13, 10, 13, 10 }, 0, 4);
        SipTransportEvent keepAlive = core.PollEvent();

        Assert.Multiple(() =>
        {
            Assert.That(first.Available, Is.True);
            Assert.That(first.KeepAlive, Is.True);
            Assert.That(first.Bytes.Skip(first.Offset).Take(first.Count).ToArray(),
                Is.EqualTo(new byte[] { 13, 10 }));
            Assert.That(partial.Success, Is.True);
            Assert.That(second.Offset, Is.EqualTo(2));
            Assert.That(complete.Success, Is.True);
            Assert.That(received.Success, Is.True);
            Assert.That(keepAlive.Kind,
                Is.EqualTo(SipTransportEventKind.KeepAliveReceived));
            Assert.That(core.PollEvent().Kind, Is.EqualTo(SipTransportEventKind.None));
        });
        clock.AdvanceMilliseconds(499);
        Assert.That(core.IsIdleExpired(), Is.False);
        clock.AdvanceMilliseconds(1);
        Assert.That(core.IsIdleExpired(), Is.True);
    }

    [Test]
    public void SendsCrlfPongAndFailsWhenPingResponseTimesOut()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipReliableConnectionCore client = Core("keepalive-client", 8,
            60000, 100, clock, "203.0.113.21", 5060);
        client.PollEvent();
        clock.AdvanceMilliseconds(100);
        SipReliableWriteChunk ping = client.PollWrite(8);
        client.CompleteWrite(ping.Count);
        clock.AdvanceMilliseconds(9999);
        bool earlyFailure = client.IsKeepAliveFailed();
        SipReliableOperationResult pongReceived = client.ReceiveBytes(
            new byte[] { 13, 10 }, 0, 2);
        SipTransportEvent pongEvent = client.PollEvent();
        clock.AdvanceMilliseconds(100);
        SipReliableWriteChunk secondPing = client.PollWrite(8);
        client.CompleteWrite(secondPing.Count);
        clock.AdvanceMilliseconds(10000);

        SipReliableConnectionCore server = Core("keepalive-server", 8,
            60000, 0, clock, "203.0.113.22", 5060);
        server.PollEvent();
        byte[] incomingPing = new byte[] { 13, 10, 13, 10 };
        SipReliableOperationResult firstHalf = server.ReceiveBytes(
            incomingPing, 0, 2);
        SipReliableWriteChunk earlyPong = server.PollWrite(8);
        SipReliableOperationResult pingReceived = server.ReceiveBytes(
            incomingPing, 2, 2);
        SipReliableWriteChunk pong = server.PollWrite(8);
        SipReliableOperationResult pongSent = server.CompleteWrite(pong.Count);

        Assert.Multiple(() =>
        {
            Assert.That(ping.Bytes.Skip(ping.Offset).Take(ping.Count).ToArray(),
                Is.EqualTo(new byte[] { 13, 10, 13, 10 }));
            Assert.That(earlyFailure, Is.False);
            Assert.That(pongReceived.Success, Is.True);
            Assert.That(pongEvent.Kind,
                Is.EqualTo(SipTransportEventKind.KeepAliveReceived));
            Assert.That(client.IsKeepAliveFailed(), Is.True);
            Assert.That(firstHalf.Success, Is.True);
            Assert.That(earlyPong.Available, Is.False);
            Assert.That(pingReceived.Success, Is.True);
            Assert.That(pong.KeepAlive, Is.True);
            Assert.That(pong.Bytes.Skip(pong.Offset).Take(pong.Count).ToArray(),
                Is.EqualTo(new byte[] { 13, 10 }));
            Assert.That(pongSent.Success, Is.True);
            Assert.That(server.IsKeepAliveFailed(), Is.False);
        });
    }

    [Test]
    public void InvalidFramingFailsConnectionAndCloseProducesTerminalEvent()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipReliableConnectionCore core = Core("invalid", 2, 60000, 0, clock,
            "203.0.113.30", 5060);
        core.PollEvent();
        byte[] invalid = System.Text.Encoding.ASCII.GetBytes("NOT SIP\r\n\r\n");

        SipReliableOperationResult result = core.ReceiveBytes(invalid, 0, invalid.Length);
        SipTransportEvent error = core.PollEvent();
        core.Close("framing failed");
        SipTransportEvent disconnected = core.PollEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(error.Kind, Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(error.Message!.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(core.GetConnectionInfo().State,
                Is.EqualTo(SipConnectionState.Failed));
            Assert.That(disconnected.Kind,
                Is.EqualTo(SipTransportEventKind.Disconnected));
            Assert.That(disconnected.ErrorMessage, Is.EqualTo("framing failed"));
        });
    }

    [Test]
    public void PoolReusesIdentityAndEndpointAndExpiresIdleConnections()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(500);
        SipReliableConnectionPool pool = new SipReliableConnectionPool(clock, 2);
        SipReliableConnectionCore first = Core("first", 4, 50, 0, clock,
            "192.0.2.1", 5060);
        SipReliableConnectionCore second = Core("second", 4, 50, 0, clock,
            "192.0.2.2", 5070);
        SipReliableConnectionCore third = Core("third", 4, 50, 0, clock,
            "192.0.2.3", 5080);

        bool addedFirst = pool.AddConnection(first);
        bool addedSecond = pool.AddConnection(second);
        bool capacityRejected = pool.AddConnection(third);
        SipReliableConnectionCore? byId = pool.FindConnection(
            "first", null, SipTransportKind.Tcp, false);
        SipReliableConnectionCore? wrongTransport = pool.FindConnection(
            "first", null, SipTransportKind.Tls, false);
        SipEndpointAddress secondRemote = second.GetConnectionInfo().RemoteAddress!;
        SipReliableConnectionCore? byEndpoint = pool.FindConnection(
            null, secondRemote, SipTransportKind.Tcp, true);
        SipReliableConnectionCore? removed = pool.RemoveConnection("second");
        bool addedThird = pool.AddConnection(third);
        clock.AdvanceMilliseconds(50);
        SipReliableConnectionCore[] expired = pool.ExpireIdleConnections();

        Assert.Multiple(() =>
        {
            Assert.That(addedFirst, Is.True);
            Assert.That(addedSecond, Is.True);
            Assert.That(capacityRejected, Is.False);
            Assert.That(byId, Is.SameAs(first));
            Assert.That(wrongTransport, Is.Null);
            Assert.That(byEndpoint, Is.SameAs(second));
            Assert.That(removed, Is.SameAs(second));
            Assert.That(addedThird, Is.True);
            Assert.That(expired, Is.EqualTo(new[] { first, third }));
            Assert.That(pool.CountConnections(), Is.Zero);
            Assert.That(pool.GetTimestampMilliseconds(), Is.EqualTo(550));
            Assert.That(first.GetConnectionInfo().State,
                Is.EqualTo(SipConnectionState.Closed));
            Assert.That(third.GetConnectionInfo().State,
                Is.EqualTo(SipConnectionState.Closed));
        });
    }

    static SipReliableConnectionCore Core(string id, int maximumQueueCount,
        int idleMilliseconds, int keepAliveMilliseconds,
        SipManualMonotonicClock clock, string remoteHost, int remotePort)
    {
        SipConnectionInfo connection = new SipConnectionInfo();
        connection.Id = id;
        connection.State = SipConnectionState.Connected;
        connection.Transport = SipTransportKind.Tcp;
        connection.LocalAddress = Endpoint("127.0.0.1", 5060);
        connection.RemoteAddress = Endpoint(remoteHost, remotePort);
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.Tcp;
        options.MaxQueueCount = maximumQueueCount;
        options.IdleTimeoutMilliseconds = idleMilliseconds;
        options.KeepAliveMilliseconds = keepAliveMilliseconds;
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.Strict = false;
        parseOptions.PreserveOriginal = false;
        return new SipReliableConnectionCore(connection, options, parseOptions, clock);
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Tcp;
        return endpoint;
    }

    static SipSendRequest Request(string correlationId, long deadline,
        SipCancellation? cancellation)
    {
        SipSendRequest request = new SipSendRequest();
        request.CorrelationId = correlationId;
        request.DeadlineMilliseconds = deadline;
        request.Cancellation = cancellation;
        return request;
    }

    static byte[] Message(string user)
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sip:" + user + "@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
    }
}
