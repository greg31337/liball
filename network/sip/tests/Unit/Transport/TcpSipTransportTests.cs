namespace sip.Tests;

public class TcpSipTransportTests
{
    [Test]
    public void ConnectsPoolsAndExchangesBidirectionalIpv4Messages()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        SipEndpointAddress clientListener = Bind(client, "127.0.0.1", 8);
        SipEndpointAddress serverListener = Bind(server, "127.0.0.1", 8);
        Assert.That(client.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));
        Assert.That(server.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));

        SipConnectionInfo clientConnection = client.ConnectTransport(serverListener);
        SipConnectionInfo reused = client.ConnectTransport(serverListener);
        SipTransportEvent clientConnected = PollUntil(client,
            SipTransportEventKind.Connected);
        SipTransportEvent serverConnected = PollUntil(server,
            SipTransportEventKind.Connected);
        SipMessage outboundMessage = ParseMessage(RequestBytes("client"));
        SipSendRequest outbound = new SipSendRequest();
        outbound.Message = outboundMessage;
        outbound.ConnectionId = clientConnection.Id;
        outbound.CorrelationId = "client-send";

        SipSendResult clientResult = client.SendTransport(outbound);
        SipTransportEvent clientSent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent serverReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        SipSendRequest response = new SipSendRequest();
        response.Bytes = ResponseBytes();
        response.ConnectionId = serverReceived.Connection!.Id;
        response.CorrelationId = "server-send";
        SipSendResult serverResult = server.SendTransport(response);
        SipTransportEvent serverSent = PollUntil(server,
            SipTransportEventKind.MessageSent);
        SipTransportEvent clientReceived = PollUntil(client,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(clientListener.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(clientConnection.State, Is.EqualTo(SipConnectionState.Connected));
            Assert.That(clientConnection.RemoteAddress!.Port,
                Is.EqualTo(serverListener.Port));
            Assert.That(reused.Id, Is.EqualTo(clientConnection.Id));
            Assert.That(clientConnected.Connection!.Id,
                Is.EqualTo(clientConnection.Id));
            Assert.That(serverConnected.Connection!.RemoteAddress!.Port,
                Is.EqualTo(clientConnection.LocalAddress!.Port));
            Assert.That(clientResult.Success, Is.True, clientResult.ErrorMessage);
            Assert.That(clientResult.Connection!.Id, Is.EqualTo(clientConnection.Id));
            Assert.That(clientSent.CorrelationId, Is.EqualTo("client-send"));
            Assert.That(serverReceived.Message!.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), serverReceived.ErrorMessage);
            Assert.That(serverReceived.Message.Message!.Method,
                Is.EqualTo(SipMethod.Options));
            Assert.That(serverReceived.Message.LocalAddress!.Port,
                Is.EqualTo(serverListener.Port));
            Assert.That(serverResult.Success, Is.True, serverResult.ErrorMessage);
            Assert.That(serverSent.CorrelationId, Is.EqualTo("server-send"));
            Assert.That(clientReceived.Message!.Message!.StatusCode, Is.EqualTo(200));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void BindsAndExchangesIpv6MessagesWhenIpv6IsAvailable()
    {
        if (!System.Net.Sockets.Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not available on this host.");
        }
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "::1", 8);
        SipEndpointAddress serverListener = Bind(server, "::1", 8);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipConnectionInfo connection = client.ConnectTransport(serverListener);
        PollUntil(client, SipTransportEventKind.Connected);
        PollUntil(server, SipTransportEventKind.Connected);
        SipSendRequest request = new SipSendRequest();
        request.Bytes = RequestBytes("ipv6");
        request.ConnectionId = connection.Id;

        SipSendResult result = client.SendTransport(request);
        PollUntil(client, SipTransportEventKind.MessageSent);
        SipTransportEvent received = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(connection.LocalAddress!.Host, Is.EqualTo("::1"));
            Assert.That(connection.RemoteAddress!.Host, Is.EqualTo("::1"));
            Assert.That(received.Message!.RemoteAddress!.Host, Is.EqualTo("::1"));
            Assert.That(received.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), received.ErrorMessage);
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void ReceivesByteWiseFragmentsAndPipelinedMessagesFromARawStream()
    {
        TcpSipTransport server = new TcpSipTransport();
        SipEndpointAddress listener = Bind(server, "127.0.0.1", 8);
        server.PollTransportEvent();
        using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(
            System.Net.Sockets.AddressFamily.InterNetwork);
        client.NoDelay = true;
        client.Connect(System.Net.IPAddress.Loopback, listener.Port);
        SipTransportEvent connected = PollUntil(server,
            SipTransportEventKind.Connected);
        using System.Net.Sockets.NetworkStream stream = client.GetStream();
        byte[] fragmented = RequestBytes("fragmented");
        int index = 0;
        while (index < fragmented.Length)
        {
            stream.Write(fragmented, index, 1);
            if (index + 1 < fragmented.Length)
            {
                SipTransportEvent pending = server.PollTransportEvent();
                Assert.That(pending.Kind, Is.EqualTo(SipTransportEventKind.None),
                    index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            index++;
        }
        SipTransportEvent receivedFragmented = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        byte[] second = RequestBytes("pipeline-two");
        byte[] third = RequestBytes("pipeline-three");
        byte[] pipeline = second.Concat(third).ToArray();
        stream.Write(pipeline);
        SipTransportEvent receivedSecond = PollUntil(server,
            SipTransportEventKind.MessageReceived);
        SipTransportEvent receivedThird = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(receivedFragmented.Message!.Bytes, Is.EqualTo(fragmented));
            Assert.That(receivedSecond.Message!.Bytes, Is.EqualTo(second));
            Assert.That(receivedThird.Message!.Bytes, Is.EqualTo(third));
            Assert.That(receivedFragmented.Connection!.Id, Is.EqualTo(connected.Connection!.Id));
            Assert.That(receivedSecond.Connection!.Id, Is.EqualTo(connected.Connection.Id));
            Assert.That(receivedThird.Connection!.Id, Is.EqualTo(connected.Connection.Id));
            Assert.That(receivedThird.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), receivedThird.ErrorMessage);
        });
        server.CloseTransport();
    }

    [Test]
    public void CompletesLargeWritesAcrossMultipleNetworkChunks()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "127.0.0.1", 8);
        SipEndpointAddress listener = Bind(server, "127.0.0.1", 8);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipConnectionInfo connection = client.ConnectTransport(listener);
        PollUntil(client, SipTransportEventKind.Connected);
        PollUntil(server, SipTransportEventKind.Connected);
        SipSendRequest request = new SipSendRequest();
        request.Bytes = LargeMessageBytes(40000);
        request.ConnectionId = connection.Id;
        request.CorrelationId = "multi-chunk";

        SipSendResult result = client.SendTransport(request);
        SipTransportEvent sent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent received = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.BytesSent, Is.EqualTo(request.Bytes.Length));
            Assert.That(sent.CorrelationId, Is.EqualTo("multi-chunk"));
            Assert.That(sent.Message!.Bytes, Is.EqualTo(request.Bytes));
            Assert.That(received.Message!.Bytes, Is.EqualTo(request.Bytes));
            Assert.That(received.Message.Message!.Body, Has.Length.EqualTo(40000));
            Assert.That(received.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), received.ErrorMessage);
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void SupportsSimultaneousConnectionsAndOptionalEndpointReuse()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        BindConfigured(client, "127.0.0.1", 0, 8, 60000, 0, false);
        SipEndpointAddress listener = Bind(server, "127.0.0.1", 8);
        client.PollTransportEvent();
        server.PollTransportEvent();

        SipConnectionInfo first = client.ConnectTransport(listener);
        SipConnectionInfo second = client.ConnectTransport(listener);
        SipTransportEvent clientFirst = PollUntil(client,
            SipTransportEventKind.Connected);
        SipTransportEvent clientSecond = PollUntil(client,
            SipTransportEventKind.Connected);
        SipTransportEvent serverFirst = PollUntil(server,
            SipTransportEventKind.Connected);
        SipTransportEvent serverSecond = PollUntil(server,
            SipTransportEventKind.Connected);
        SipSendRequest firstRequest = new SipSendRequest();
        firstRequest.Bytes = RequestBytes("simultaneous-one");
        firstRequest.ConnectionId = first.Id;
        SipSendRequest secondRequest = new SipSendRequest();
        secondRequest.Bytes = RequestBytes("simultaneous-two");
        secondRequest.ConnectionId = second.Id;

        SipSendResult firstResult = client.SendTransport(firstRequest);
        SipSendResult secondResult = client.SendTransport(secondRequest);
        PollUntil(client, SipTransportEventKind.MessageSent);
        PollUntil(client, SipTransportEventKind.MessageSent);
        SipTransportEvent firstReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);
        SipTransportEvent secondReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(clientFirst.Connection!.Id, Is.Not.EqualTo(clientSecond.Connection!.Id));
            Assert.That(serverFirst.Connection!.Id, Is.Not.EqualTo(serverSecond.Connection!.Id));
            Assert.That(serverFirst.Connection.RemoteAddress!.Port,
                Is.Not.EqualTo(serverSecond.Connection.RemoteAddress!.Port));
            Assert.That(firstResult.Success, Is.True, firstResult.ErrorMessage);
            Assert.That(secondResult.Success, Is.True, secondResult.ErrorMessage);
            Assert.That(new[]
            {
                System.Text.Encoding.ASCII.GetString(firstReceived.Message!.Bytes),
                System.Text.Encoding.ASCII.GetString(secondReceived.Message!.Bytes)
            }, Is.EquivalentTo(new[]
            {
                System.Text.Encoding.ASCII.GetString(firstRequest.Bytes),
                System.Text.Encoding.ASCII.GetString(secondRequest.Bytes)
            }));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void SwitchesOversizedUdpRequestToTcpButRejectsSmallUdpRequest()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "127.0.0.1", 8);
        SipEndpointAddress serverListener = Bind(server, "127.0.0.1", 8);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        SipSendRequest cancelled = new SipSendRequest();
        cancelled.Bytes = RequestBytes("cancelled");
        cancelled.RemoteAddress = serverListener;
        cancelled.Cancellation = cancellation;
        cancelled.CorrelationId = "cancelled";
        SipSendResult cancelledResult = client.SendTransport(cancelled);
        SipTransportEvent cancelledFailure = client.PollTransportEvent();
        SipSendRequest small = new SipSendRequest();
        small.Bytes = RequestBytes("small");
        small.RemoteAddress = serverListener;
        small.Transport = SipTransportKind.Udp;
        small.CorrelationId = "small-udp";

        SipSendResult smallResult = client.SendTransport(small);
        SipTransportEvent smallFailure = client.PollTransportEvent();

        SipSendRequest oversized = new SipSendRequest();
        oversized.Bytes = LargeMessageBytes(1400);
        oversized.RemoteAddress = serverListener;
        oversized.Transport = SipTransportKind.Udp;
        oversized.CorrelationId = "tcp-fallback";
        SipSendResult fallbackResult = client.SendTransport(oversized);
        SipTransportEvent sent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent received = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(cancelledResult.Success, Is.False);
            Assert.That(cancelledFailure.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(cancelledFailure.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(smallResult.Success, Is.False);
            Assert.That(smallFailure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(smallFailure.CorrelationId, Is.EqualTo("small-udp"));
            Assert.That(fallbackResult.Success, Is.True, fallbackResult.ErrorMessage);
            Assert.That(fallbackResult.Connection!.Transport,
                Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(sent.Transport, Is.EqualTo(SipTransportKind.Tcp));
            Assert.That(sent.CorrelationId, Is.EqualTo("tcp-fallback"));
            Assert.That(received.Message!.Bytes, Is.EqualTo(oversized.Bytes));
            Assert.That(received.Message.Message!.Body, Has.Length.EqualTo(1400));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void AppliesWriteQueueBackpressureUntilPollingDrainsTheConnection()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "127.0.0.1", 1);
        SipEndpointAddress serverListener = Bind(server, "127.0.0.1", 4);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipConnectionInfo connection = client.ConnectTransport(serverListener);
        PollUntil(client, SipTransportEventKind.Connected);
        PollUntil(server, SipTransportEventKind.Connected);
        SipSendRequest first = new SipSendRequest();
        first.Bytes = RequestBytes("first");
        first.ConnectionId = connection.Id;
        first.CorrelationId = "first";
        SipSendRequest second = new SipSendRequest();
        second.Bytes = RequestBytes("second");
        second.ConnectionId = connection.Id;
        second.CorrelationId = "second";

        SipSendResult firstResult = client.SendTransport(first);
        SipSendResult secondResult = client.SendTransport(second);
        SipTransportEvent pressure = client.PollTransportEvent();
        SipTransportEvent sent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent received = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.False);
            Assert.That(secondResult.ErrorMessage, Does.Contain("queue is full"));
            Assert.That(pressure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(pressure.CorrelationId, Is.EqualTo("second"));
            Assert.That(sent.CorrelationId, Is.EqualTo("first"));
            Assert.That(received.Message!.Bytes, Is.EqualTo(first.Bytes));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void DefersSocketIoUntilSingleSlotConnectionEventsAreDrained()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "127.0.0.1", 1);
        SipEndpointAddress serverListener = Bind(server, "127.0.0.1", 1);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipSendRequest request = new SipSendRequest();
        request.Bytes = RequestBytes("deferred");
        request.RemoteAddress = serverListener;
        request.CorrelationId = "deferred";

        SipSendResult result = client.SendTransport(request);
        SipTransportEvent clientConnected = client.PollTransportEvent();
        SipTransportEvent clientSent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent serverConnected = server.PollTransportEvent();
        SipTransportEvent serverReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(clientConnected.Kind,
                Is.EqualTo(SipTransportEventKind.Connected));
            Assert.That(clientSent.CorrelationId, Is.EqualTo("deferred"));
            Assert.That(serverConnected.Kind,
                Is.EqualTo(SipTransportEventKind.Connected));
            Assert.That(serverReceived.Message!.Bytes, Is.EqualTo(request.Bytes));
            Assert.That(serverReceived.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void ReportsRemoteDisconnectAndUnavailableConnectionAffinity()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport server = new TcpSipTransport();
        Bind(client, "127.0.0.1", 8);
        SipEndpointAddress serverListener = Bind(server, "127.0.0.1", 8);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipConnectionInfo connection = client.ConnectTransport(serverListener);
        PollUntil(client, SipTransportEventKind.Connected);
        PollUntil(server, SipTransportEventKind.Connected);

        server.CloseTransport();
        SipTransportEvent disconnected = PollUntil(client,
            SipTransportEventKind.Disconnected);
        SipSendRequest stale = new SipSendRequest();
        stale.Bytes = RequestBytes("stale");
        stale.ConnectionId = connection.Id;
        stale.RemoteAddress = serverListener;
        stale.CorrelationId = "stale";
        SipSendResult result = client.SendTransport(stale);
        SipTransportEvent failure = client.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Connection!.Id, Is.EqualTo(connection.Id));
            Assert.That(disconnected.Connection.State,
                Is.EqualTo(SipConnectionState.Closed));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("reconnect is required"));
            Assert.That(failure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(failure.CorrelationId, Is.EqualTo("stale"));
        });
        client.CloseTransport();
    }

    [Test]
    public void SendsCrlfKeepalivesAndExpiresTrulyIdleConnections()
    {
        TcpSipTransport keepAliveClient = new TcpSipTransport();
        TcpSipTransport keepAliveServer = new TcpSipTransport();
        BindConfigured(keepAliveClient, "127.0.0.1", 0, 8, 1000, 20, true);
        SipEndpointAddress keepAliveListener = BindConfigured(keepAliveServer,
            "127.0.0.1", 0, 8, 1000, 20, true);
        keepAliveClient.PollTransportEvent();
        keepAliveServer.PollTransportEvent();
        keepAliveClient.ConnectTransport(keepAliveListener);
        PollUntil(keepAliveClient, SipTransportEventKind.Connected);
        PollUntil(keepAliveServer, SipTransportEventKind.Connected);

        bool clientObserved = false;
        bool serverObserved = false;
        long keepAliveDeadline = System.Environment.TickCount64 + 3000;
        while ((!clientObserved || !serverObserved)
            && System.Environment.TickCount64 < keepAliveDeadline)
        {
            SipTransportEvent clientEvent = keepAliveClient.PollTransportEvent();
            SipTransportEvent serverEvent = keepAliveServer.PollTransportEvent();
            clientObserved = clientObserved
                || clientEvent.Kind == SipTransportEventKind.KeepAliveReceived;
            serverObserved = serverObserved
                || serverEvent.Kind == SipTransportEventKind.KeepAliveReceived;
            System.Threading.Thread.Sleep(2);
        }

        TcpSipTransport idleClient = new TcpSipTransport();
        TcpSipTransport idleServer = new TcpSipTransport();
        BindConfigured(idleClient, "127.0.0.1", 0, 8, 40, 0, true);
        SipEndpointAddress idleListener = BindConfigured(idleServer,
            "127.0.0.1", 0, 8, 40, 0, true);
        idleClient.PollTransportEvent();
        idleServer.PollTransportEvent();
        idleClient.ConnectTransport(idleListener);
        PollUntil(idleClient, SipTransportEventKind.Connected);
        PollUntil(idleServer, SipTransportEventKind.Connected);
        System.Threading.Thread.Sleep(60);
        SipTransportEvent expired = PollUntil(idleClient,
            SipTransportEventKind.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(clientObserved, Is.True);
            Assert.That(serverObserved, Is.True);
            Assert.That(expired.Connection!.State, Is.EqualTo(SipConnectionState.Closed));
            Assert.That(expired.ErrorMessage, Does.Contain("idle timeout"));
        });
        keepAliveClient.CloseTransport();
        keepAliveServer.CloseTransport();
        idleClient.CloseTransport();
        idleServer.CloseTransport();
    }

    [Test]
    public void ReconnectsByEndpointButRejectsAnExplicitStaleConnection()
    {
        TcpSipTransport client = new TcpSipTransport();
        TcpSipTransport firstServer = new TcpSipTransport();
        Bind(client, "127.0.0.1", 8);
        SipEndpointAddress firstListener = Bind(firstServer, "127.0.0.1", 8);
        client.PollTransportEvent();
        firstServer.PollTransportEvent();
        SipConnectionInfo original = client.ConnectTransport(firstListener);
        PollUntil(client, SipTransportEventKind.Connected);
        PollUntil(firstServer, SipTransportEventKind.Connected);
        firstServer.CloseTransport();
        PollUntil(client, SipTransportEventKind.Disconnected);

        TcpSipTransport replacementServer = new TcpSipTransport();
        SipEndpointAddress replacementListener = BindConfigured(replacementServer,
            "127.0.0.1", firstListener.Port, 8, 60000, 0, true);
        replacementServer.PollTransportEvent();
        SipSendRequest stale = new SipSendRequest();
        stale.Bytes = RequestBytes("stale-explicit");
        stale.ConnectionId = original.Id;
        stale.RemoteAddress = replacementListener;
        stale.CorrelationId = "stale";
        SipSendResult staleResult = client.SendTransport(stale);
        SipTransportEvent staleFailure = client.PollTransportEvent();
        SipSendRequest reconnect = new SipSendRequest();
        reconnect.Bytes = RequestBytes("reconnected");
        reconnect.RemoteAddress = replacementListener;
        reconnect.CorrelationId = "reconnect";

        SipSendResult reconnectResult = client.SendTransport(reconnect);
        SipTransportEvent reconnected = PollUntil(client,
            SipTransportEventKind.Connected);
        SipTransportEvent sent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        PollUntil(replacementServer, SipTransportEventKind.Connected);
        SipTransportEvent received = PollUntil(replacementServer,
            SipTransportEventKind.MessageReceived);

        Assert.Multiple(() =>
        {
            Assert.That(staleResult.Success, Is.False);
            Assert.That(staleFailure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(staleFailure.CorrelationId, Is.EqualTo("stale"));
            Assert.That(staleFailure.ErrorMessage, Does.Contain("reconnect is required"));
            Assert.That(reconnectResult.Success, Is.True, reconnectResult.ErrorMessage);
            Assert.That(reconnectResult.Connection!.Id, Is.Not.EqualTo(original.Id));
            Assert.That(reconnected.Connection!.Id, Is.EqualTo(reconnectResult.Connection.Id));
            Assert.That(sent.CorrelationId, Is.EqualTo("reconnect"));
            Assert.That(received.Message!.Bytes, Is.EqualTo(reconnect.Bytes));
        });
        client.CloseTransport();
        replacementServer.CloseTransport();
    }

    static SipEndpointAddress Bind(TcpSipTransport transport, string host,
        int maximumQueueCount)
    {
        return BindConfigured(transport, host, 0, maximumQueueCount,
            60000, 0, true);
    }

    static SipEndpointAddress BindConfigured(TcpSipTransport transport, string host,
        int port, int maximumQueueCount, int idleTimeoutMilliseconds,
        int keepAliveMilliseconds, bool reuseConnections)
    {
        SipEndpointAddress local = new SipEndpointAddress();
        local.Host = host;
        local.Port = port;
        local.Transport = SipTransportKind.Tcp;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.Tcp;
        options.LocalAddress = local;
        options.MaxQueueCount = maximumQueueCount;
        options.IdleTimeoutMilliseconds = idleTimeoutMilliseconds;
        options.KeepAliveMilliseconds = keepAliveMilliseconds;
        options.ReuseConnections = reuseConnections;
        return transport.BindTransport(options, new SipTlsOptions(),
            new SipWebSocketOptions());
    }

    static SipTransportEvent PollUntil(TcpSipTransport transport,
        SipTransportEventKind kind)
    {
        long deadline = System.Environment.TickCount64 + 3000;
        SipTransportEvent result = transport.PollTransportEvent();
        while (result.Kind != kind && System.Environment.TickCount64 < deadline)
        {
            System.Threading.Thread.Sleep(2);
            result = transport.PollTransportEvent();
        }
        Assert.That(result.Kind, Is.EqualTo(kind), result.ErrorMessage);
        return result;
    }

    static SipMessage ParseMessage(byte[] bytes)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(bytes, options);
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static byte[] RequestBytes(string user)
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sip:" + user + "@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
    }

    static byte[] ResponseBytes()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "SIP/2.0 200 OK\r\nContent-Length: 0\r\n\r\n");
    }

    static byte[] LargeMessageBytes(int bodyLength)
    {
        byte[] headers = System.Text.Encoding.ASCII.GetBytes(
            "MESSAGE sip:large@example.com SIP/2.0\r\nContent-Length: "
            + bodyLength.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "\r\n\r\n");
        return headers.Concat(Enumerable.Repeat((byte)'a', bodyLength)).ToArray();
    }
}
