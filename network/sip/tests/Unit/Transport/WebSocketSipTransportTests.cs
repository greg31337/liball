namespace sip.Tests;

public class WebSocketSipTransportTests
{
    [Test]
    public void HandshakeRequiresHostOriginSipSubprotocolAndHttp11Response()
    {
        string key = "dGhlIHNhbXBsZSBub25jZQ==";
        string validRequest = SipWebSocketHandshake.CreateClientRequest(
            "/sip", "example.com:5060", "https://allowed.example", key);
        string missingHost = validRequest.Replace(
            "Host: example.com:5060\r\n", "",
            System.StringComparison.Ordinal);
        string missingOrigin = SipWebSocketHandshake.CreateClientRequest(
            "/sip", "example.com:5060", null, key);
        string tokenRequest = validRequest.Replace(
            "Upgrade: websocket\r\nConnection: Upgrade\r\n",
            "Upgrade: HTTP/1.1, WebSocket\r\n"
                + "Connection: keep-alive, Upgrade\r\n",
            System.StringComparison.Ordinal);
        string validResponse = SipWebSocketHandshake.CreateServerResponse(key);
        string http10Response = validResponse.Replace(
            "HTTP/1.1", "HTTP/1.0", System.StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(SipWebSocketHandshake.ValidateClientRequest(
                validRequest, "/sip", "https://allowed.example"), Is.EqualTo(key));
            Assert.That(SipWebSocketHandshake.ValidateClientRequest(
                tokenRequest, "/sip", "https://allowed.example"), Is.EqualTo(key));
            Assert.That(SipWebSocketHandshake.ValidateClientRequest(
                missingHost, "/sip", "https://allowed.example"), Is.Null);
            Assert.That(SipWebSocketHandshake.ValidateClientRequest(
                missingOrigin, "/sip", "https://allowed.example"), Is.Null);
            Assert.That(SipWebSocketHandshake.ValidateClientRequest(
                validRequest, "/other", "https://allowed.example"), Is.Null);
            Assert.That(SipWebSocketHandshake.ValidateServerResponse(
                validResponse, key), Is.True);
            Assert.That(SipWebSocketHandshake.ValidateServerResponse(
                http10Response, key), Is.False);
        });
    }

    [Test]
    public void ConnectsPoolsAndExchangesBidirectionalMessages()
    {
        WebSocketSipTransport client = new WebSocketSipTransport();
        WebSocketSipTransport server = new WebSocketSipTransport();
        Bind(client, "/sip", "https://client.example", true, 4096, 25);
        SipEndpointAddress serverEndpoint = Bind(server, "/sip",
            "https://client.example", true, 4096, 25);
        Assert.That(client.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));
        Assert.That(server.PollTransportEvent().Kind,
            Is.EqualTo(SipTransportEventKind.Bound));

        SipConnectionInfo connection = Connect(client, server, serverEndpoint);
        SipTransportEvent clientConnected = PollUntil(client,
            SipTransportEventKind.Connected);
        SipConnectionInfo reused = client.ConnectTransport(serverEndpoint);
        SipSendRequest outbound = new SipSendRequest();
        outbound.Bytes = RequestBytes("client");
        outbound.ConnectionId = connection.Id;
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
            Assert.That(connection.Transport,
                Is.EqualTo(SipTransportKind.WebSocket));
            Assert.That(connection.Secure, Is.False);
            Assert.That(reused.Id, Is.EqualTo(connection.Id));
            Assert.That(clientConnected.Connection!.Id,
                Is.EqualTo(connection.Id));
            Assert.That(clientResult.Success, Is.True, clientResult.ErrorMessage);
            Assert.That(clientSent.CorrelationId, Is.EqualTo("client-send"));
            Assert.That(serverReceived.Message!.Bytes,
                Is.EqualTo(outbound.Bytes));
            Assert.That(serverReceived.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(serverResult.Success, Is.True, serverResult.ErrorMessage);
            Assert.That(serverSent.CorrelationId, Is.EqualTo("server-send"));
            Assert.That(clientReceived.Message!.Message!.StatusCode,
                Is.EqualTo(200));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void AcceptsFragmentedTextAndBinaryWebSocketMessages()
    {
        WebSocketSipTransport server = new WebSocketSipTransport();
        SipEndpointAddress endpoint = Bind(server, "/socket", null,
            true, 4096, 0);
        server.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket client =
            ConnectRaw(server, endpoint, "/socket", null,
                SipTransportNames.WebSocketSubprotocol);
        byte[] text = RequestBytes("fragmented-text");
        int split = text.Length / 2;

        client.SendAsync(new System.ArraySegment<byte>(text, 0, split),
            System.Net.WebSockets.WebSocketMessageType.Text, false,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        client.SendAsync(new System.ArraySegment<byte>(text, split,
                text.Length - split),
            System.Net.WebSockets.WebSocketMessageType.Text, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent textReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        byte[] binary = RequestBytes("binary");
        client.SendAsync(new System.ArraySegment<byte>(binary),
            System.Net.WebSockets.WebSocketMessageType.Binary, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent binaryReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);

        System.Threading.Tasks.Task closing = client.CloseOutputAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "done", new System.Threading.CancellationToken());
        SipTransportEvent disconnected = PollUntil(server,
            SipTransportEventKind.Disconnected);
        closing.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(textReceived.Message!.Bytes, Is.EqualTo(text));
            Assert.That(textReceived.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(binaryReceived.Message!.Bytes, Is.EqualTo(binary));
            Assert.That(binaryReceived.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(disconnected.Connection!.State,
                Is.EqualTo(SipConnectionState.Closed));
        });
        server.CloseTransport();
    }

    [Test]
    public void RejectsMissingSipSubprotocolAndWrongOrigin()
    {
        WebSocketSipTransport subprotocolServer = new WebSocketSipTransport();
        SipEndpointAddress subprotocolEndpoint = Bind(subprotocolServer,
            "/sip", null, true, 4096, 0);
        subprotocolServer.PollTransportEvent();
        System.Net.WebSockets.ClientWebSocket missingProtocol =
            new System.Net.WebSockets.ClientWebSocket();
        System.Threading.Tasks.Task missingTask = missingProtocol.ConnectAsync(
            CreateUri(subprotocolEndpoint, "/sip"),
            new System.Threading.CancellationToken());
        PollUntilComplete(subprotocolServer, missingTask);
        SipTransportEvent subprotocolFailure = PollUntil(subprotocolServer,
            SipTransportEventKind.Disconnected);

        WebSocketSipTransport originServer = new WebSocketSipTransport();
        SipEndpointAddress originEndpoint = Bind(originServer, "/sip",
            "https://allowed.example", true, 4096, 0);
        originServer.PollTransportEvent();
        System.Net.WebSockets.ClientWebSocket wrongOrigin =
            new System.Net.WebSockets.ClientWebSocket();
        wrongOrigin.Options.AddSubProtocol(SipTransportNames.WebSocketSubprotocol);
        wrongOrigin.Options.SetRequestHeader("Origin", "https://denied.example");
        System.Threading.Tasks.Task originTask = wrongOrigin.ConnectAsync(
            CreateUri(originEndpoint, "/sip"),
            new System.Threading.CancellationToken());
        PollUntilComplete(originServer, originTask);
        SipTransportEvent originFailure = PollUntil(originServer,
            SipTransportEventKind.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(missingTask.IsCompletedSuccessfully, Is.False);
            Assert.That(subprotocolFailure.ErrorMessage,
                Does.Contain("upgrade failed"));
            Assert.That(originTask.IsCompletedSuccessfully, Is.False);
            Assert.That(originFailure.ErrorMessage,
                Does.Contain("upgrade failed"));
        });
        missingProtocol.Dispose();
        wrongOrigin.Dispose();
        subprotocolServer.CloseTransport();
        originServer.CloseTransport();
    }

    [Test]
    public void EnforcesOneSipMessagePerWebSocketMessage()
    {
        WebSocketSipTransport server = new WebSocketSipTransport();
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, 4096, 0);
        server.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket client =
            ConnectRaw(server, endpoint, "/sip", null,
                SipTransportNames.WebSocketSubprotocol);
        byte[] first = RequestBytes("first");
        byte[] second = RequestBytes("second");
        byte[] combined = new byte[first.Length + second.Length];
        System.Array.Copy(first, combined, first.Length);
        System.Array.Copy(second, 0, combined, first.Length, second.Length);

        client.SendAsync(new System.ArraySegment<byte>(combined),
            System.Net.WebSockets.WebSocketMessageType.Text, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent invalid = PollUntil(server,
            SipTransportEventKind.MessageReceived);
        SipTransportEvent disconnected = PollUntil(server,
            SipTransportEventKind.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Message!.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(invalid.ErrorMessage,
                Does.Contain("exactly one SIP message"));
            Assert.That(disconnected.ErrorMessage,
                Does.Contain("exactly one SIP message"));
        });
        server.CloseTransport();
    }

    [Test]
    public void RejectsACompletedButTruncatedSipPayloadWithInvalidPayloadClose()
    {
        WebSocketSipTransport server = new WebSocketSipTransport();
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, 4096, 0);
        server.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket client =
            ConnectRaw(server, endpoint, "/sip", null,
                SipTransportNames.WebSocketSubprotocol);
        byte[] truncated = System.Text.Encoding.ASCII.GetBytes(
            "MESSAGE sip:user@example.com SIP/2.0\r\n"
            + "Content-Length: 5\r\n\r\nxx");
        byte[] closeBuffer = new byte[128];
        System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult>
            receivingClose = client.ReceiveAsync(
                new System.ArraySegment<byte>(closeBuffer),
                new System.Threading.CancellationToken());

        client.SendAsync(new System.ArraySegment<byte>(truncated),
            System.Net.WebSockets.WebSocketMessageType.Text, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent invalid = PollUntil(server,
            SipTransportEventKind.MessageReceived);
        System.Net.WebSockets.WebSocketReceiveResult closed =
            receivingClose.GetAwaiter().GetResult();
        SipTransportEvent disconnected = PollUntil(server,
            SipTransportEventKind.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Message!.Bytes, Is.EqualTo(truncated));
            Assert.That(invalid.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(invalid.ErrorMessage, Does.Contain("body is complete"));
            Assert.That(closed.MessageType,
                Is.EqualTo(System.Net.WebSockets.WebSocketMessageType.Close));
            Assert.That(closed.CloseStatus,
                Is.EqualTo(System.Net.WebSockets.WebSocketCloseStatus.InvalidPayloadData));
            Assert.That(disconnected.ErrorMessage,
                Does.Contain("body is complete"));
        });
        server.CloseTransport();
    }

    [Test]
    public void AcceptsExactMessageLimitAndReportsOutgoingOverflow()
    {
        byte[] exactBytes = RequestBytes("exact-limit");
        WebSocketSipTransport client = new WebSocketSipTransport();
        WebSocketSipTransport server = new WebSocketSipTransport();
        Bind(client, "/sip", null, true, exactBytes.Length, 0);
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, exactBytes.Length, 0);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipConnectionInfo connection = Connect(client, server, endpoint);
        PollUntil(client, SipTransportEventKind.Connected);
        SipSendRequest exact = new SipSendRequest();
        exact.Bytes = exactBytes;
        exact.ConnectionId = connection.Id;
        exact.CorrelationId = "exact";

        SipSendResult exactResult = client.SendTransport(exact);
        SipTransportEvent exactSent = PollUntil(client,
            SipTransportEventKind.MessageSent);
        SipTransportEvent exactReceived = PollUntil(server,
            SipTransportEventKind.MessageReceived);
        SipSendRequest overflow = new SipSendRequest();
        overflow.Bytes = exactBytes.Concat(new byte[] { (byte)'x' }).ToArray();
        overflow.ConnectionId = connection.Id;
        overflow.CorrelationId = "overflow";
        SipSendResult overflowResult = client.SendTransport(overflow);
        SipTransportEvent overflowFailure = client.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Success, Is.True, exactResult.ErrorMessage);
            Assert.That(exactResult.BytesSent, Is.EqualTo(exactBytes.Length));
            Assert.That(exactSent.CorrelationId, Is.EqualTo("exact"));
            Assert.That(exactReceived.Message!.Bytes, Is.EqualTo(exactBytes));
            Assert.That(exactReceived.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete), exactReceived.ErrorMessage);
            Assert.That(overflowResult.Success, Is.False);
            Assert.That(overflowResult.BytesSent, Is.Zero);
            Assert.That(overflowResult.ErrorMessage, Does.Contain("size limit"));
            Assert.That(overflowFailure.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(overflowFailure.CorrelationId, Is.EqualTo("overflow"));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void HonorsCancellationBeforeConnectionAndAfterWriteQueueing()
    {
        WebSocketSipTransport client = new WebSocketSipTransport();
        WebSocketSipTransport server = new WebSocketSipTransport();
        Bind(client, "/sip", null, true, 4096, 0);
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, 4096, 0);
        client.PollTransportEvent();
        server.PollTransportEvent();
        SipCancellation preCancellation = new SipCancellation();
        preCancellation.Requested = true;
        SipSendRequest preCancelled = new SipSendRequest();
        preCancelled.Bytes = RequestBytes("pre-cancelled");
        preCancelled.RemoteAddress = endpoint;
        preCancelled.Cancellation = preCancellation;
        preCancelled.CorrelationId = "pre-cancelled";

        SipSendResult preResult = client.SendTransport(preCancelled);
        SipTransportEvent preFailure = client.PollTransportEvent();
        SipConnectionInfo connection = Connect(client, server, endpoint);
        PollUntil(client, SipTransportEventKind.Connected);
        SipCancellation queuedCancellation = new SipCancellation();
        SipSendRequest queued = new SipSendRequest();
        queued.Bytes = RequestBytes("queued-cancelled");
        queued.ConnectionId = connection.Id;
        queued.Cancellation = queuedCancellation;
        queued.CorrelationId = "queued-cancelled";
        SipSendResult queuedResult = client.SendTransport(queued);
        queuedCancellation.Requested = true;
        SipTransportEvent queuedFailure = PollUntil(client,
            SipTransportEventKind.SendFailed);

        Assert.Multiple(() =>
        {
            Assert.That(preResult.Success, Is.False);
            Assert.That(preFailure.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(preFailure.CorrelationId, Is.EqualTo("pre-cancelled"));
            Assert.That(preFailure.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(queuedResult.Success, Is.True, queuedResult.ErrorMessage);
            Assert.That(queuedFailure.CorrelationId, Is.EqualTo("queued-cancelled"));
            Assert.That(queuedFailure.ErrorMessage, Does.Contain("cancelled"));
        });
        client.CloseTransport();
        server.CloseTransport();
    }

    [Test]
    public void ClosesConnectionsThatViolateBinaryOrSizePolicy()
    {
        WebSocketSipTransport binaryServer = new WebSocketSipTransport();
        SipEndpointAddress binaryEndpoint = Bind(binaryServer, "/sip", null,
            false, 4096, 0);
        binaryServer.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket binaryClient =
            ConnectRaw(binaryServer, binaryEndpoint, "/sip", null,
                SipTransportNames.WebSocketSubprotocol);
        byte[] request = RequestBytes("binary-denied");
        binaryClient.SendAsync(new System.ArraySegment<byte>(request),
            System.Net.WebSockets.WebSocketMessageType.Binary, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent binaryClosed = PollUntil(binaryServer,
            SipTransportEventKind.Disconnected);

        WebSocketSipTransport sizeServer = new WebSocketSipTransport();
        SipEndpointAddress sizeEndpoint = Bind(sizeServer, "/sip", null,
            true, 64, 0);
        sizeServer.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket sizeClient =
            ConnectRaw(sizeServer, sizeEndpoint, "/sip", null,
                SipTransportNames.WebSocketSubprotocol);
        sizeClient.SendAsync(new System.ArraySegment<byte>(request),
            System.Net.WebSockets.WebSocketMessageType.Text, true,
            new System.Threading.CancellationToken()).GetAwaiter().GetResult();
        SipTransportEvent sizeClosed = PollUntil(sizeServer,
            SipTransportEventKind.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(binaryClosed.ErrorMessage,
                Does.Contain("frame policy"));
            Assert.That(sizeClosed.ErrorMessage,
                Does.Contain("frame policy"));
        });
        binaryServer.CloseTransport();
        sizeServer.CloseTransport();
    }

    [Test]
    public void SendsWebSocketCloseWhenTransportCloses()
    {
        WebSocketSipTransport server = new WebSocketSipTransport();
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, 4096, 0);
        server.PollTransportEvent();
        using System.Net.WebSockets.ClientWebSocket client =
            ConnectRaw(server, endpoint, "/sip", null,
                SipTransportNames.WebSocketSubprotocol);
        byte[] buffer = new byte[64];
        System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult>
            receiving = client.ReceiveAsync(new System.ArraySegment<byte>(buffer),
                new System.Threading.CancellationToken());

        server.CloseTransport();
        System.Net.WebSockets.WebSocketReceiveResult received =
            receiving.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(received.MessageType,
                Is.EqualTo(System.Net.WebSockets.WebSocketMessageType.Close));
            Assert.That(received.CloseStatus,
                Is.EqualTo(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure));
        });
    }

    [Test]
    public void SendsWebSocketControlKeepAliveAtConfiguredInterval()
    {
        WebSocketSipTransport server = new WebSocketSipTransport();
        SipEndpointAddress endpoint = Bind(server, "/sip", null,
            true, 4096, 20);
        server.PollTransportEvent();
        using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
        client.Connect(endpoint.Host!, endpoint.Port);
        System.Net.Sockets.NetworkStream stream = client.GetStream();
        stream.ReadTimeout = 2000;
        byte[] request = System.Text.Encoding.ASCII.GetBytes(
            "GET /sip HTTP/1.1\r\nHost: " + endpoint.Host + ":" + endpoint.Port
            + "\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
            + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"
            + "Sec-WebSocket-Version: 13\r\n"
            + "Sec-WebSocket-Protocol: sip\r\n\r\n");
        stream.Write(request, 0, request.Length);
        WaitForData(server, stream);
        string response = ReadHeader(stream);
        WaitForData(server, stream);
        int frame = stream.ReadByte();
        int length = stream.ReadByte();
        int opcode = frame & 15;

        Assert.Multiple(() =>
        {
            Assert.That(response, Does.StartWith("HTTP/1.1 101"));
            Assert.That(response, Does.Contain("Sec-WebSocket-Protocol: sip"));
            Assert.That(opcode, Is.AnyOf(9, 10));
            Assert.That(length & 128, Is.EqualTo(0));
        });
        server.CloseTransport();
    }

    static SipEndpointAddress Bind(WebSocketSipTransport transport,
        string path, string? origin, bool allowBinary, int maximumBytes,
        int keepAliveMilliseconds)
    {
        SipEndpointAddress local = new SipEndpointAddress();
        local.Host = "127.0.0.1";
        local.Port = 0;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.WebSocket;
        options.LocalAddress = local;
        options.MaxQueueCount = 16;
        options.IdleTimeoutMilliseconds = 60000;
        options.KeepAliveMilliseconds = keepAliveMilliseconds;
        SipWebSocketOptions webSocket = new SipWebSocketOptions();
        webSocket.Path = path;
        webSocket.Origin = origin;
        webSocket.AllowBinaryFrames = allowBinary;
        webSocket.MaxMessageBytes = maximumBytes;
        return transport.BindTransport(options, new SipTlsOptions(), webSocket);
    }

    static SipConnectionInfo Connect(WebSocketSipTransport client,
        WebSocketSipTransport server, SipEndpointAddress endpoint)
    {
        System.Threading.Tasks.Task<SipConnectionInfo> task =
            System.Threading.Tasks.Task.Run(() => client.ConnectTransport(endpoint));
        PollUntilComplete(server, task);
        return task.GetAwaiter().GetResult();
    }

    static System.Net.WebSockets.ClientWebSocket ConnectRaw(
        WebSocketSipTransport server, SipEndpointAddress endpoint,
        string path, string? origin, string subprotocol)
    {
        System.Net.WebSockets.ClientWebSocket client =
            new System.Net.WebSockets.ClientWebSocket();
        client.Options.AddSubProtocol(subprotocol);
        if (origin != null)
        {
            client.Options.SetRequestHeader("Origin", origin);
        }
        System.Threading.Tasks.Task task = client.ConnectAsync(
            CreateUri(endpoint, path), new System.Threading.CancellationToken());
        PollUntilComplete(server, task);
        task.GetAwaiter().GetResult();
        return client;
    }

    static void PollUntilComplete(WebSocketSipTransport server,
        System.Threading.Tasks.Task task)
    {
        long deadline = System.Environment.TickCount64 + 5000;
        while (!task.IsCompleted && System.Environment.TickCount64 < deadline)
        {
            server.PollTransportEvent();
            System.Threading.Thread.Sleep(2);
        }
        Assert.That(task.IsCompleted, Is.True, "WebSocket operation timed out.");
    }

    static SipTransportEvent PollUntil(WebSocketSipTransport transport,
        SipTransportEventKind kind)
    {
        long deadline = System.Environment.TickCount64 + 5000;
        SipTransportEvent result = transport.PollTransportEvent();
        while (result.Kind != kind && System.Environment.TickCount64 < deadline)
        {
            System.Threading.Thread.Sleep(2);
            result = transport.PollTransportEvent();
        }
        Assert.That(result.Kind, Is.EqualTo(kind), result.ErrorMessage);
        return result;
    }

    static void WaitForData(WebSocketSipTransport server,
        System.Net.Sockets.NetworkStream stream)
    {
        long deadline = System.Environment.TickCount64 + 3000;
        while (!stream.DataAvailable && System.Environment.TickCount64 < deadline)
        {
            server.PollTransportEvent();
            System.Threading.Thread.Sleep(2);
        }
        Assert.That(stream.DataAvailable, Is.True,
            "The WebSocket peer did not produce data.");
    }

    static string ReadHeader(System.Net.Sockets.NetworkStream stream)
    {
        System.Collections.Generic.List<byte> bytes =
            new System.Collections.Generic.List<byte>();
        while (bytes.Count < 16384)
        {
            int value = stream.ReadByte();
            Assert.That(value, Is.GreaterThanOrEqualTo(0));
            bytes.Add((byte)value);
            int count = bytes.Count;
            if (count >= 4 && bytes[count - 4] == 13 && bytes[count - 3] == 10
                && bytes[count - 2] == 13 && bytes[count - 1] == 10)
            {
                return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
            }
        }
        Assert.Fail("The WebSocket upgrade response exceeded its bound.");
        return "";
    }

    static System.Uri CreateUri(SipEndpointAddress endpoint, string path)
    {
        return new System.Uri("ws://" + endpoint.Host + ":"
            + endpoint.Port + path);
    }

    static byte[] RequestBytes(string branch)
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sip:user@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/WS client.example;branch=z9hG4bK-" + branch + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static byte[] ResponseBytes()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "SIP/2.0 200 OK\r\nContent-Length: 0\r\n\r\n");
    }
}
