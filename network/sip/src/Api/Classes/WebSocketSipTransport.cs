namespace sip;

/// <summary>Provides SIP connections over WebSocket or secure WebSocket.</summary>
public class WebSocketSipTransport : ISipTransport
{
    const int HeaderBytes = 16384;
    const int NetworkBufferBytes = 16384;
    const int ConnectMilliseconds = 5000;

    object gate = new object();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    System.Collections.Generic.List<SipWebSocketConnection> connections =
        new System.Collections.Generic.List<SipWebSocketConnection>();
    ISipMonotonicClock clock;
    SipProtocolTokenGenerator tokens;
    System.Net.Sockets.Socket? listener;
    SipReliableConnectionPool? pool;
    SipTransportOptions? transportOptions;
    SipTlsOptions? tlsOptions;
    SipWebSocketOptions? webSocketOptions;
    SipParseOptions? parseOptions;
    System.Security.Cryptography.X509Certificates.X509Certificate2? localCertificate;
    System.Security.Cryptography.X509Certificates.X509Certificate2[] trustedRoots =
        new System.Security.Cryptography.X509Certificates.X509Certificate2[0];
    SipEndpointAddress? localAddress;
    SipTransportKind transportKind;
    int maximumQueueCount;
    int keepAliveMilliseconds;
    bool bound;
    bool closed;
    bool closedEventReturned;

    /// <summary>Initializes an unbound SIP WebSocket transport.</summary>
    public WebSocketSipTransport()
    {
        clock = new SipSystemMonotonicClock();
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
    }

    /// <summary>Binds an IPv4 or IPv6 WebSocket listening endpoint.</summary>
    /// <param name="options">The common transport options.</param>
    /// <param name="tlsConfiguration">The TLS configuration used by secure WebSocket.</param>
    /// <param name="webSocketConfiguration">The WebSocket handshake and message policy.</param>
    /// <returns>The actual bound endpoint.</returns>
    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsConfiguration,
        SipWebSocketOptions webSocketConfiguration)
    {
        lock (gate)
        {
            if (bound || closed)
            {
                throw new SipTransportException(
                    "The WebSocket transport has already been bound or closed.",
                    SipTransportKind.WebSocket, null);
            }
            SipTransportKind selected = SipTransportKind.WebSocket;
            if (options != null)
            {
                selected = options.Kind;
                if (selected == SipTransportKind.Unknown)
                {
                    selected = SipTransportKind.WebSocket;
                }
            }
            SipEndpointAddress? requested = ValidateBindOptions(options, selected);
            SipTlsOptions? validatedTls = ValidateTlsOptions(tlsConfiguration);
            SipWebSocketOptions? validatedWebSocket =
                ValidateWebSocketOptions(webSocketConfiguration);
            if (options == null || requested == null || requested.Host == null
                || validatedTls == null || validatedWebSocket == null)
            {
                throw new SipTransportException(
                    "The WebSocket bind options are invalid.", selected, null);
            }
            System.Security.Cryptography.X509Certificates.X509Certificate2?
                certificate = selected == SipTransportKind.SecureWebSocket
                    ? LoadLocalCertificate(validatedTls) : null;
            System.Security.Cryptography.X509Certificates.X509Certificate2[] roots =
                selected == SipTransportKind.SecureWebSocket
                    ? LoadTrustedRoots(validatedTls.TrustedRoots)
                    : new System.Security.Cryptography.X509Certificates.X509Certificate2[0];
            System.Net.IPAddress address = System.Net.IPAddress.Parse(requested.Host);
            System.Net.Sockets.Socket created = new System.Net.Sockets.Socket(
                address.AddressFamily, System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp);
            created.Blocking = false;
            created.Bind(new System.Net.IPEndPoint(address, requested.Port));
            int backlog = options.MaxQueueCount < 512 ? options.MaxQueueCount : 512;
            created.Listen(backlog);
            System.Net.IPEndPoint? actual =
                (System.Net.IPEndPoint?)created.LocalEndPoint;
            if (actual == null)
            {
                created.Dispose();
                DisposeCertificate(certificate);
                DisposeCertificates(roots);
                throw new SipTransportException(
                    "The WebSocket listener did not report its bound endpoint.",
                    selected, null);
            }
            transportKind = selected;
            listener = created;
            localAddress = CreateEndpoint(actual);
            transportOptions = CopyTransportOptions(options);
            transportOptions.Kind = selected;
            transportOptions.KeepAliveMilliseconds = 0;
            tlsOptions = validatedTls;
            webSocketOptions = validatedWebSocket;
            parseOptions = CreateParseOptions(validatedWebSocket.MaxMessageBytes);
            localCertificate = certificate;
            trustedRoots = roots;
            maximumQueueCount = options.MaxQueueCount;
            keepAliveMilliseconds = options.KeepAliveMilliseconds;
            pool = new SipReliableConnectionPool(clock, SipDefaults.MaxConnectionCount);
            bound = true;
            AddEvent(CreateEvent(SipTransportEventKind.Bound, null));
            return CopyRequiredEndpoint(localAddress);
        }
    }

    /// <summary>Creates or reuses an outbound SIP WebSocket connection.</summary>
    /// <param name="remoteAddress">The numeric remote WebSocket endpoint.</param>
    /// <returns>The upgraded connection metadata.</returns>
    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        lock (gate)
        {
            if (!bound || closed || listener == null || pool == null
                || transportOptions == null || parseOptions == null
                || webSocketOptions == null || tlsOptions == null)
            {
                throw new SipTransportException(
                    "The WebSocket transport is not bound.", transportKind, null);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(remoteAddress);
            if (remote == null || remote.Host == null)
            {
                throw new SipTransportException(
                    "The WebSocket remote endpoint is invalid.", transportKind, null);
            }
            SipReliableConnectionCore? reusable = pool.FindConnection(
                null, remote, transportKind, transportOptions.ReuseConnections);
            if (reusable != null)
            {
                return reusable.GetConnectionInfo();
            }
            System.Net.IPAddress address = System.Net.IPAddress.Parse(remote.Host);
            if (address.AddressFamily != listener.AddressFamily)
            {
                throw new SipTransportException(
                    "The WebSocket destination address family does not match the listener.",
                    transportKind, null);
            }
            System.Net.Sockets.Socket created = new System.Net.Sockets.Socket(
                address.AddressFamily, System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp);
            created.Blocking = false;
            created.NoDelay = true;
            System.Threading.Tasks.Task connecting = created.ConnectAsync(
                new System.Net.IPEndPoint(address, remote.Port));
            long deadline = System.Environment.TickCount64 + ConnectMilliseconds;
            if (!CompleteTask(connecting, deadline)
                || !connecting.IsCompletedSuccessfully)
            {
                created.Dispose();
                throw new SipTransportException(
                    "The outbound WebSocket connection failed or timed out.",
                    transportKind, null);
            }
            created.Blocking = true;
            SipWebSocketConnection connection = CreateNetworkConnection(created, true);
            if (!CompleteClientUpgrade(connection, remote.Host))
            {
                DisposeConnection(connection);
                throw new SipTransportException(
                    "The outbound WebSocket upgrade failed.", transportKind, null);
            }
            if (!CompleteConnection(connection) || connection.Core == null
                || !pool.AddConnection(connection.Core))
            {
                DisposeConnection(connection);
                throw new SipTransportException(
                    "The outbound WebSocket connection could not enter the pool.",
                    transportKind, null);
            }
            connections.Add(connection);
            return connection.Core.GetConnectionInfo();
        }
    }

    /// <summary>Queues one SIP message as one WebSocket message.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The queueing result and selected connection.</returns>
    public SipSendResult SendTransport(SipSendRequest request)
    {
        lock (gate)
        {
            SipSendResult result = CreateSendResult(request);
            if (!bound || closed || pool == null || transportOptions == null
                || webSocketOptions == null)
            {
                return FailSend(result,
                    "The WebSocket transport is not bound.", request);
            }
            if (IsCancelled(request.Cancellation))
            {
                return FailSend(result,
                    "The WebSocket send was cancelled.", request);
            }
            if (request.DeadlineMilliseconds >= 0
                && System.Environment.TickCount64 >= request.DeadlineMilliseconds)
            {
                return FailSend(result,
                    "The WebSocket send deadline has expired.", request);
            }
            if (request.Transport != SipTransportKind.Unknown
                && request.Transport != transportKind)
            {
                return FailSend(result,
                    "The send request selects a different transport.", request);
            }
            SipWriteResult bytesResult = CreateSendBytes(request);
            if (!bytesResult.Success)
            {
                return FailSend(result, bytesResult.ErrorMessage == null
                    ? "The WebSocket message cannot be serialized."
                    : bytesResult.ErrorMessage, request);
            }
            if (bytesResult.Bytes.Length > webSocketOptions.MaxMessageBytes)
            {
                return FailSend(result,
                    "The WebSocket message exceeds the configured size limit.",
                    request);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(request.RemoteAddress);
            SipReliableConnectionCore? core = pool.FindConnection(
                request.ConnectionId, remote, transportKind,
                transportOptions.ReuseConnections);
            if (core == null && request.ConnectionId != null)
            {
                return FailSend(result,
                    "The requested WebSocket connection is unavailable; reconnect is required.",
                    request);
            }
            if (core == null)
            {
                if (remote == null)
                {
                    return FailSend(result,
                        "A WebSocket destination or live connection identifier is required.",
                        request);
                }
                SipConnectionInfo connected = ConnectTransport(remote);
                core = pool.FindConnection(connected.Id, remote,
                    transportKind, false);
            }
            if (core == null)
            {
                return FailSend(result,
                    "The WebSocket connection could not be selected.", request);
            }
            SipConnectionInfo info = core.GetConnectionInfo();
            SipSendRequest normalized = CopySendRequest(request);
            normalized.Transport = transportKind;
            normalized.ConnectionId = info.Id;
            normalized.LocalAddress = null;
            normalized.RemoteAddress = CopyOptionalEndpoint(info.RemoteAddress);
            SipReliableOperationResult queued = core.QueueWrite(
                normalized, bytesResult.Bytes);
            if (!queued.Success)
            {
                result.ErrorMessage = queued.ErrorMessage;
                if (queued.Backpressured)
                {
                    AddEvent(CreateFailureEvent(queued.ErrorMessage,
                        request.CorrelationId, info));
                }
                return result;
            }
            result.Success = true;
            result.BytesSent = queued.BytesAccepted;
            result.Connection = info;
            return result;
        }
    }

    /// <summary>Polls WebSocket upgrades, framed messages, and transport events.</summary>
    /// <returns>The next event, or an event with kind <see cref="SipTransportEventKind.None"/>.</returns>
    public SipTransportEvent PollTransportEvent()
    {
        lock (gate)
        {
            if (events.Count > 0)
            {
                return RemoveFirstEvent();
            }
            if (!closed)
            {
                AcceptConnection();
                ServiceConnections();
            }
            if (events.Count > 0)
            {
                return RemoveFirstEvent();
            }
            int index = 0;
            while (index < connections.Count)
            {
                SipWebSocketConnection connection = connections[index];
                if (connection.Core != null)
                {
                    SipTransportEvent value = connection.Core.PollEvent();
                    if (value.Kind != SipTransportEventKind.None)
                    {
                        RegisterReceivedAlias(connection, value);
                        return value;
                    }
                }
                if (connection.Closed)
                {
                    connections.RemoveAt(index);
                }
                else
                {
                    index = index + 1;
                }
            }
            if (closed && !closedEventReturned && connections.Count == 0)
            {
                closedEventReturned = true;
                return CreateEvent(SipTransportEventKind.Closed, null);
            }
            return CreateEvent(SipTransportEventKind.None, null);
        }
    }

    /// <summary>Closes the listener and all WebSocket connections.</summary>
    public void CloseTransport()
    {
        lock (gate)
        {
            if (closed)
            {
                return;
            }
            closed = true;
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
            int index = 0;
            while (index < connections.Count)
            {
                CloseConnection(connections[index],
                    "The WebSocket transport was closed.");
                index = index + 1;
            }
            DisposeCertificate(localCertificate);
            localCertificate = null;
            DisposeCertificates(trustedRoots);
            trustedRoots = new System.Security.Cryptography.X509Certificates.X509Certificate2[0];
        }
    }

    void AcceptConnection()
    {
        if (listener == null || pool == null
            || !listener.Poll(0, System.Net.Sockets.SelectMode.SelectRead))
        {
            return;
        }
        System.Threading.Tasks.Task<System.Net.Sockets.Socket> accepting =
            listener.AcceptAsync();
        if (!CompleteTask(accepting,
                System.Environment.TickCount64 + 1000)
            || !accepting.IsCompletedSuccessfully)
        {
            QueueHandshakeFailure(
                "The WebSocket listener failed to accept a connection.");
            return;
        }
        System.Net.Sockets.Socket accepted = accepting.Result;
        accepted.Blocking = true;
        accepted.NoDelay = true;
        SipWebSocketConnection connection = CreateNetworkConnection(accepted, false);
        connection.HandshakeTask = System.Threading.Tasks.Task.Run(
            () => CompleteServerUpgrade(connection));
        connections.Add(connection);
    }

    void ServiceConnections()
    {
        int index = 0;
        while (index < connections.Count)
        {
            SipWebSocketConnection connection = connections[index];
            if (!connection.Closed && connection.Core == null)
            {
                CompleteAcceptedUpgrade(connection);
            }
            if (!connection.Closed && connection.WebSocket != null
                && connection.Core != null)
            {
                ServiceRead(connection);
                if (!connection.Closed)
                {
                    ServiceWrite(connection);
                }
                if (!connection.Closed && connection.Core.IsIdleExpired())
                {
                    CloseConnection(connection,
                        "The WebSocket connection idle timeout expired.");
                }
            }
            index = index + 1;
        }
    }

    void CompleteAcceptedUpgrade(SipWebSocketConnection connection)
    {
        if (connection.HandshakeTask == null
            || !connection.HandshakeTask.IsCompleted)
        {
            return;
        }
        if (!connection.HandshakeTask.IsCompletedSuccessfully
            || !connection.HandshakeTask.Result
            || !CompleteConnection(connection) || connection.Core == null
            || pool == null || !pool.AddConnection(connection.Core))
        {
            DisposeConnection(connection);
            QueueHandshakeFailure("The accepted WebSocket upgrade failed.");
        }
    }

    bool CompleteClientUpgrade(SipWebSocketConnection connection,
        string remoteHost)
    {
        if (!PrepareClientStream(connection, remoteHost)
            || connection.Stream == null || webSocketOptions == null)
        {
            return false;
        }
        connection.Stream.ReadTimeout = ConnectMilliseconds;
        connection.Stream.WriteTimeout = ConnectMilliseconds;
        string key = SipWebSocketHandshake.CreateKey();
        string host = FormatHost(remoteHost, connection.Socket == null
            || connection.Socket.RemoteEndPoint == null ? 0
            : ((System.Net.IPEndPoint)connection.Socket.RemoteEndPoint).Port);
        string request = SipWebSocketHandshake.CreateClientRequest(
            webSocketOptions.Path == null ? "/" : webSocketOptions.Path,
            host, webSocketOptions.Origin, key);
        byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(request);
        connection.Stream.Write(requestBytes, 0, requestBytes.Length);
        connection.Stream.Flush();
        string? response = SipWebSocketHandshake.ReadHeader(
            connection.Stream, HeaderBytes);
        if (response == null
            || !SipWebSocketHandshake.ValidateServerResponse(response, key))
        {
            return false;
        }
        connection.WebSocket = System.Net.WebSockets.WebSocket.CreateFromStream(
            connection.Stream, false, SipTransportNames.WebSocketSubprotocol,
            KeepAliveInterval());
        return true;
    }

    bool CompleteServerUpgrade(SipWebSocketConnection connection)
    {
        if (!PrepareServerStream(connection)
            || connection.Stream == null || webSocketOptions == null)
        {
            return false;
        }
        connection.Stream.ReadTimeout = ConnectMilliseconds;
        connection.Stream.WriteTimeout = ConnectMilliseconds;
        string? request = SipWebSocketHandshake.ReadHeader(
            connection.Stream, HeaderBytes);
        string? key = request == null ? null
            : SipWebSocketHandshake.ValidateClientRequest(request,
                webSocketOptions.Path == null ? "/" : webSocketOptions.Path,
                webSocketOptions.Origin);
        if (key == null)
        {
            byte[] forbidden = System.Text.Encoding.ASCII.GetBytes(
                "HTTP/1.1 403 Forbidden\r\nConnection: close\r\n"
                + "Content-Length: 0\r\n\r\n");
            connection.Stream.Write(forbidden, 0, forbidden.Length);
            connection.Stream.Flush();
            return false;
        }
        byte[] response = System.Text.Encoding.ASCII.GetBytes(
            SipWebSocketHandshake.CreateServerResponse(key));
        connection.Stream.Write(response, 0, response.Length);
        connection.Stream.Flush();
        connection.WebSocket = System.Net.WebSockets.WebSocket.CreateFromStream(
            connection.Stream, true, SipTransportNames.WebSocketSubprotocol,
            KeepAliveInterval());
        return true;
    }

    bool PrepareClientStream(SipWebSocketConnection connection,
        string remoteHost)
    {
        if (connection.Stream == null)
        {
            return false;
        }
        if (transportKind != SipTransportKind.SecureWebSocket)
        {
            return true;
        }
        System.Net.Security.SslStream secure = new System.Net.Security.SslStream(
            connection.Stream, false, ValidateServerCertificate);
        connection.SecureStream = secure;
        connection.Stream = secure;
        System.Net.Security.SslClientAuthenticationOptions authentication =
            CreateClientAuthentication(remoteHost);
        System.Threading.Tasks.Task task = secure.AuthenticateAsClientAsync(
            authentication, new System.Threading.CancellationToken());
        long deadline = System.Environment.TickCount64 + ConnectMilliseconds;
        return CompleteTask(task, deadline) && task.IsCompletedSuccessfully
            && secure.IsAuthenticated && secure.IsEncrypted;
    }

    bool PrepareServerStream(SipWebSocketConnection connection)
    {
        if (connection.Stream == null)
        {
            return false;
        }
        if (transportKind != SipTransportKind.SecureWebSocket)
        {
            return true;
        }
        if (localCertificate == null || !localCertificate.HasPrivateKey)
        {
            return false;
        }
        System.Net.Security.SslStream secure = new System.Net.Security.SslStream(
            connection.Stream, false, ValidateClientCertificate);
        connection.SecureStream = secure;
        connection.Stream = secure;
        System.Net.Security.SslServerAuthenticationOptions authentication =
            CreateServerAuthentication();
        System.Threading.Tasks.Task task = secure.AuthenticateAsServerAsync(
            authentication, new System.Threading.CancellationToken());
        task.GetAwaiter().GetResult();
        return task.IsCompletedSuccessfully && secure.IsAuthenticated
            && secure.IsEncrypted;
    }

    bool CompleteConnection(SipWebSocketConnection connection)
    {
        if (connection.Socket == null || connection.WebSocket == null
            || connection.WebSocket.State != System.Net.WebSockets.WebSocketState.Open
            || transportOptions == null || parseOptions == null)
        {
            return false;
        }
        System.Net.IPEndPoint? local =
            (System.Net.IPEndPoint?)connection.Socket.LocalEndPoint;
        System.Net.IPEndPoint? remote =
            (System.Net.IPEndPoint?)connection.Socket.RemoteEndPoint;
        if (local == null || remote == null)
        {
            return false;
        }
        SipCertificateIdentity? certificateIdentity =
            connection.SecureStream == null ? null
            : CreateCertificateIdentity(connection.SecureStream,
                connection.ClientSide);
        SipConnectionInfo info = new SipConnectionInfo();
        info.Id = tokens.CreateOpaqueToken();
        info.State = SipConnectionState.Connected;
        info.Transport = transportKind;
        info.LocalAddress = CreateEndpoint(local);
        info.RemoteAddress = CreateEndpoint(remote);
        info.Secure = transportKind == SipTransportKind.SecureWebSocket;
        info.CertificateIdentity = certificateIdentity;
        info.PeerIdentity = GetPeerIdentity(certificateIdentity,
            connection.ClientSide);
        connection.Core = new SipReliableConnectionCore(
            info, transportOptions, parseOptions, clock);
        return true;
    }

    void ServiceRead(SipWebSocketConnection connection)
    {
        if (connection.WebSocket == null || connection.Core == null
            || webSocketOptions == null)
        {
            return;
        }
        if (connection.MessageComplete)
        {
            DeliverMessage(connection);
            return;
        }
        if (connection.ReadTask == null)
        {
            if (!connection.Core.CanReceiveBytes()
                || connection.WebSocket.State
                    != System.Net.WebSockets.WebSocketState.Open)
            {
                return;
            }
            connection.ReadBuffer = new byte[NetworkBufferBytes];
            connection.ReadTask = connection.WebSocket.ReceiveAsync(
                new System.ArraySegment<byte>(connection.ReadBuffer),
                new System.Threading.CancellationToken());
        }
        if (!connection.ReadTask.IsCompleted)
        {
            return;
        }
        if (!connection.ReadTask.IsCompletedSuccessfully)
        {
            CloseConnection(connection,
                "The WebSocket receive operation failed.");
            return;
        }
        System.Net.WebSockets.WebSocketReceiveResult received =
            connection.ReadTask.Result;
        connection.ReadTask = null;
        if (received.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
        {
            AcknowledgeClose(connection);
            CloseConnection(connection,
                "The remote WebSocket endpoint closed the connection.");
            return;
        }
        if (connection.MessageBuffer.Count == 0)
        {
            connection.MessageType = received.MessageType;
        }
        if (received.MessageType != connection.MessageType
            || received.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary
                && !webSocketOptions.AllowBinaryFrames
            || received.Count > webSocketOptions.MaxMessageBytes
                - connection.MessageBuffer.Count)
        {
            CloseWithProtocolError(connection,
                received.Count > webSocketOptions.MaxMessageBytes
                    - connection.MessageBuffer.Count
                    ? System.Net.WebSockets.WebSocketCloseStatus.MessageTooBig
                    : System.Net.WebSockets.WebSocketCloseStatus.InvalidMessageType,
                "The WebSocket message violates the configured frame policy.");
            return;
        }
        int index = 0;
        while (index < received.Count)
        {
            connection.MessageBuffer.Add(connection.ReadBuffer[index]);
            index = index + 1;
        }
        connection.ReadBuffer = new byte[0];
        if (!received.EndOfMessage)
        {
            return;
        }
        connection.MessageComplete = true;
        DeliverMessage(connection);
    }

    void DeliverMessage(SipWebSocketConnection connection)
    {
        if (connection.Core == null || !connection.MessageComplete)
        {
            return;
        }
        SipReliableOperationResult accepted = connection.Core.ReceiveMessage(
            connection.MessageBuffer.ToArray());
        if (accepted.Backpressured)
        {
            return;
        }
        connection.MessageBuffer.Clear();
        connection.MessageComplete = false;
        if (!accepted.Success && !accepted.Backpressured)
        {
            CloseWithProtocolError(connection,
                System.Net.WebSockets.WebSocketCloseStatus.InvalidPayloadData,
                accepted.ErrorMessage == null
                    ? "The WebSocket message is not one complete SIP message."
                    : accepted.ErrorMessage);
        }
    }

    void ServiceWrite(SipWebSocketConnection connection)
    {
        if (connection.WebSocket == null || connection.Core == null
            || webSocketOptions == null
            || connection.WriteTask == null && !connection.Core.CanWriteBytes())
        {
            return;
        }
        if (connection.WriteTask == null)
        {
            SipReliableWriteChunk chunk = connection.Core.PollWrite(
                webSocketOptions.MaxMessageBytes);
            if (!chunk.Available)
            {
                return;
            }
            connection.WriteCount = chunk.Count;
            connection.WriteTask = connection.WebSocket.SendAsync(
                new System.ArraySegment<byte>(chunk.Bytes, chunk.Offset, chunk.Count),
                System.Net.WebSockets.WebSocketMessageType.Binary, true,
                new System.Threading.CancellationToken());
        }
        if (!connection.WriteTask.IsCompleted)
        {
            return;
        }
        if (!connection.WriteTask.IsCompletedSuccessfully)
        {
            CloseConnection(connection,
                "The WebSocket send operation failed.");
            return;
        }
        SipReliableOperationResult completed =
            connection.Core.CompleteWrite(connection.WriteCount);
        connection.WriteTask = null;
        connection.WriteCount = 0;
        if (!completed.Success)
        {
            CloseConnection(connection, completed.ErrorMessage == null
                ? "The WebSocket write state is invalid."
                : completed.ErrorMessage);
        }
    }

    void AcknowledgeClose(SipWebSocketConnection connection)
    {
        if (connection.WebSocket != null
            && connection.WebSocket.State
                == System.Net.WebSockets.WebSocketState.CloseReceived)
        {
            System.Threading.Tasks.Task closing =
                connection.WebSocket.CloseOutputAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "SIP WebSocket closing",
                    new System.Threading.CancellationToken());
            CompleteTask(closing,
                System.Environment.TickCount64 + ConnectMilliseconds);
        }
    }

    void CloseWithProtocolError(SipWebSocketConnection connection,
        System.Net.WebSockets.WebSocketCloseStatus status, string message)
    {
        if (connection.WebSocket != null
            && connection.WebSocket.State
                == System.Net.WebSockets.WebSocketState.Open)
        {
            System.Threading.Tasks.Task closing =
                connection.WebSocket.CloseOutputAsync(status, message,
                    new System.Threading.CancellationToken());
            CompleteTask(closing,
                System.Environment.TickCount64 + ConnectMilliseconds);
        }
        CloseConnection(connection, message);
    }

    SipWebSocketConnection CreateNetworkConnection(
        System.Net.Sockets.Socket socket, bool clientSide)
    {
        SipWebSocketConnection result = new SipWebSocketConnection();
        result.Socket = socket;
        result.Stream = new System.Net.Sockets.NetworkStream(socket, true);
        result.ClientSide = clientSide;
        return result;
    }

    System.Net.Security.SslClientAuthenticationOptions CreateClientAuthentication(
        string remoteHost)
    {
        System.Net.Security.SslClientAuthenticationOptions result =
            new System.Net.Security.SslClientAuthenticationOptions();
        result.TargetHost = tlsOptions == null || tlsOptions.ServerName == null
            || tlsOptions.ServerName.Length == 0 ? remoteHost : tlsOptions.ServerName;
        result.EnabledSslProtocols = SafeProtocols();
        result.EncryptionPolicy = System.Net.Security.EncryptionPolicy.RequireEncryption;
        result.AllowRenegotiation = false;
        result.CertificateRevocationCheckMode =
            System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
        if (localCertificate != null)
        {
            result.ClientCertificates =
                new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
            result.ClientCertificates.Add(localCertificate);
        }
        if (trustedRoots.Length > 0)
        {
            result.CertificateChainPolicy = CreateChainPolicy();
        }
        return result;
    }

    System.Net.Security.SslServerAuthenticationOptions CreateServerAuthentication()
    {
        System.Net.Security.SslServerAuthenticationOptions result =
            new System.Net.Security.SslServerAuthenticationOptions();
        result.ServerCertificate = localCertificate;
        result.ClientCertificateRequired = tlsOptions != null
            && tlsOptions.RequireClientCertificate;
        result.EnabledSslProtocols = SafeProtocols();
        result.EncryptionPolicy = System.Net.Security.EncryptionPolicy.RequireEncryption;
        result.AllowRenegotiation = false;
        result.CertificateRevocationCheckMode =
            System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
        if (trustedRoots.Length > 0)
        {
            result.CertificateChainPolicy = CreateChainPolicy();
        }
        return result;
    }

    System.Security.Cryptography.X509Certificates.X509ChainPolicy CreateChainPolicy()
    {
        System.Security.Cryptography.X509Certificates.X509ChainPolicy result =
            new System.Security.Cryptography.X509Certificates.X509ChainPolicy();
        result.TrustMode = System.Security.Cryptography.X509Certificates
            .X509ChainTrustMode.CustomRootTrust;
        result.RevocationMode = System.Security.Cryptography.X509Certificates
            .X509RevocationMode.Online;
        result.VerificationFlags = SafeVerificationFlags();
        int index = 0;
        while (index < trustedRoots.Length)
        {
            result.CustomTrustStore.Add(trustedRoots[index]);
            index = index + 1;
        }
        return result;
    }

    SipCertificateIdentity? CreateCertificateIdentity(
        System.Net.Security.SslStream stream, bool clientSide)
    {
        if (stream.RemoteCertificate == null)
        {
            return null;
        }
        byte[] certificateBytes = stream.RemoteCertificate.Export(
            System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
        using (System.Security.Cryptography.X509Certificates.X509Certificate2 certificate =
            System.Security.Cryptography.X509Certificates.X509CertificateLoader
                .LoadCertificate(certificateBytes))
        {
            SipCertificateIdentity result = new SipCertificateIdentity();
            result.Subject = certificate.Subject;
            result.Domains = SipCertificateDomainIdentity.ExtractDomains(certificate);
            result.CertificateBytes = certificateBytes;
            result.ChainValid = tlsOptions != null && tlsOptions.ValidatePeer;
            result.NameValid = clientSide && result.ChainValid
                && tlsOptions != null && tlsOptions.ServerName != null
                && SipCertificateDomainIdentity.MatchesDomains(
                    result.Domains, tlsOptions.ServerName);
            result.MutualTls = localCertificate != null;
            return result;
        }
    }

    string? GetPeerIdentity(SipCertificateIdentity? identity, bool clientSide)
    {
        if (identity == null || !identity.ChainValid)
        {
            return null;
        }
        if (clientSide && tlsOptions != null && tlsOptions.ServerName != null
            && tlsOptions.ServerName.Length > 0)
        {
            return tlsOptions.ServerName;
        }
        if (identity.Domains.Length > 0)
        {
            return identity.Domains[0];
        }
        return identity.Subject;
    }

    void CloseConnection(SipWebSocketConnection connection, string message)
    {
        if (connection.Closed)
        {
            return;
        }
        connection.Closed = true;
        if (connection.WebSocket != null
            && connection.WebSocket.State
                == System.Net.WebSockets.WebSocketState.Open)
        {
            System.Threading.Tasks.Task closing =
                connection.WebSocket.CloseOutputAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "SIP WebSocket closing",
                    new System.Threading.CancellationToken());
            CompleteTask(closing,
                System.Environment.TickCount64 + ConnectMilliseconds);
        }
        if (connection.Core != null)
        {
            SipConnectionInfo info = connection.Core.GetConnectionInfo();
            connection.Core.Close(message);
            if (pool != null)
            {
                pool.RemoveConnection(info.Id);
            }
        }
        DisposeConnectionResources(connection);
    }

    void QueueHandshakeFailure(string message)
    {
        SipTransportEvent failed = CreateEvent(
            SipTransportEventKind.Disconnected, null);
        failed.ErrorMessage = message;
        AddEvent(failed);
    }

    SipEndpointAddress? ValidateBindOptions(SipTransportOptions? options,
        SipTransportKind selected)
    {
        if (options == null || options.MaxQueueCount <= 0
            || options.IdleTimeoutMilliseconds < 0
            || options.KeepAliveMilliseconds < 0
            || selected != SipTransportKind.WebSocket
                && selected != SipTransportKind.SecureWebSocket)
        {
            return null;
        }
        SipEndpointAddress requested = options.LocalAddress == null
            ? new SipEndpointAddress() : CopyRequiredEndpoint(options.LocalAddress);
        if (requested.Host == null || requested.Host.Length == 0)
        {
            requested.Host = "0.0.0.0";
        }
        if (requested.Port < 0)
        {
            requested.Port = selected == SipTransportKind.SecureWebSocket
                ? 443 : 80;
        }
        if (requested.Port > 65535
            || requested.Transport != SipTransportKind.Unknown
                && requested.Transport != selected
            || !IsNumericAddress(requested.Host))
        {
            return null;
        }
        requested.Transport = selected;
        requested.Secure = selected == SipTransportKind.SecureWebSocket;
        return requested;
    }

    SipEndpointAddress? ValidateRemoteAddress(SipEndpointAddress? remote)
    {
        if (remote == null || remote.Host == null || remote.Host.Length == 0
            || remote.Port <= 0 || remote.Port > 65535
            || remote.Transport != SipTransportKind.Unknown
                && remote.Transport != transportKind
            || !IsNumericAddress(remote.Host))
        {
            return null;
        }
        SipEndpointAddress result = CopyRequiredEndpoint(remote);
        result.Host = System.Net.IPAddress.Parse(remote.Host).ToString();
        result.Transport = transportKind;
        result.Secure = transportKind == SipTransportKind.SecureWebSocket;
        return result;
    }

    SipTlsOptions? ValidateTlsOptions(SipTlsOptions? source)
    {
        if (source == null || source.CertificateBytes == null
            || source.PrivateKeyBytes == null || source.TrustedRoots == null)
        {
            return null;
        }
        int index = 0;
        while (index < source.TrustedRoots.Length)
        {
            if (source.TrustedRoots[index] == null
                || source.TrustedRoots[index].Length == 0)
            {
                return null;
            }
            index = index + 1;
        }
        SipTlsOptions result = new SipTlsOptions();
        result.ServerName = source.ServerName;
        result.CertificateBytes = CopyBytes(source.CertificateBytes);
        result.PrivateKeyBytes = CopyBytes(source.PrivateKeyBytes);
        result.TrustedRoots = CopyByteArrays(source.TrustedRoots);
        result.RequireClientCertificate = source.RequireClientCertificate;
        result.ValidatePeer = source.ValidatePeer;
        return result;
    }

    static SipWebSocketOptions? ValidateWebSocketOptions(
        SipWebSocketOptions? source)
    {
        if (source == null || source.Path == null || source.Path.Length == 0
            || source.Path[0] != '/' || source.Path.IndexOf('\r') >= 0
            || source.Path.IndexOf('\n') >= 0
            || source.Origin != null && (source.Origin.IndexOf('\r') >= 0
                || source.Origin.IndexOf('\n') >= 0)
            || !System.String.Equals(source.Subprotocol,
                SipTransportNames.WebSocketSubprotocol,
                System.StringComparison.Ordinal)
            || source.MaxMessageBytes <= 0)
        {
            return null;
        }
        SipWebSocketOptions result = new SipWebSocketOptions();
        result.Path = source.Path;
        result.Origin = source.Origin;
        result.Subprotocol = source.Subprotocol;
        result.AllowBinaryFrames = source.AllowBinaryFrames;
        result.MaxMessageBytes = source.MaxMessageBytes;
        return result;
    }

    SipWriteResult CreateSendBytes(SipSendRequest request)
    {
        if (request.Bytes == null)
        {
            return CreateWriteError(
                "The WebSocket send byte collection cannot be null.");
        }
        if (request.Bytes.Length > 0)
        {
            SipWriteResult supplied = new SipWriteResult();
            supplied.Success = true;
            supplied.Bytes = CopyBytes(request.Bytes);
            supplied.RequiredBytes = supplied.Bytes.Length;
            return supplied;
        }
        if (request.Message == null)
        {
            return CreateWriteError(
                "A SIP message or pre-serialized bytes are required.");
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.ReconcileContentLength = true;
        return SipMessageFormatter.FormatMessageModel(request.Message, options);
    }

    SipSendResult FailSend(SipSendResult result, string message,
        SipSendRequest? request)
    {
        result.ErrorMessage = message;
        AddEvent(CreateFailureEvent(message,
            request == null ? null : request.CorrelationId, null));
        return result;
    }

    bool AddEvent(SipTransportEvent value)
    {
        if (maximumQueueCount <= 0 || events.Count >= maximumQueueCount)
        {
            return false;
        }
        events.Add(value);
        return true;
    }

    SipTransportEvent RemoveFirstEvent()
    {
        SipTransportEvent result = events[0];
        events.RemoveAt(0);
        return result;
    }

    SipTransportEvent CreateFailureEvent(string? message,
        string? correlationId, SipConnectionInfo? connection)
    {
        SipTransportEvent result = new SipTransportEvent();
        result.Kind = SipTransportEventKind.SendFailed;
        result.Transport = transportKind;
        result.CorrelationId = correlationId;
        result.ErrorMessage = message;
        result.Connection = connection;
        return result;
    }

    SipTransportEvent CreateEvent(SipTransportEventKind kind,
        string? correlationId)
    {
        SipTransportEvent result = new SipTransportEvent();
        result.Kind = kind;
        result.Transport = transportKind;
        result.CorrelationId = correlationId;
        return result;
    }

    static SipSendResult CreateSendResult(SipSendRequest? request)
    {
        SipSendResult result = new SipSendResult();
        result.CorrelationId = request == null ? null : request.CorrelationId;
        return result;
    }

    SipEndpointAddress CreateEndpoint(System.Net.IPEndPoint endpoint)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = endpoint.Address.ToString();
        result.Port = endpoint.Port;
        result.Transport = transportKind;
        result.Secure = transportKind == SipTransportKind.SecureWebSocket;
        return result;
    }

    static SipEndpointAddress CopyRequiredEndpoint(SipEndpointAddress source)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = source.Host;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Secure = source.Secure;
        return result;
    }

    static SipEndpointAddress? CopyOptionalEndpoint(SipEndpointAddress? source)
    {
        return source == null ? null : CopyRequiredEndpoint(source);
    }

    static SipSendRequest CopySendRequest(SipSendRequest source)
    {
        SipSendRequest result = new SipSendRequest();
        result.Message = source.Message;
        result.Bytes = source.Bytes;
        result.RemoteAddress = CopyOptionalEndpoint(source.RemoteAddress);
        result.LocalAddress = CopyOptionalEndpoint(source.LocalAddress);
        result.Transport = source.Transport;
        result.ConnectionId = source.ConnectionId;
        result.FlowId = source.FlowId;
        result.DeadlineMilliseconds = source.DeadlineMilliseconds;
        result.Cancellation = source.Cancellation;
        result.CorrelationId = source.CorrelationId;
        return result;
    }

    static SipTransportOptions CopyTransportOptions(SipTransportOptions source)
    {
        SipTransportOptions result = new SipTransportOptions();
        result.Kind = source.Kind;
        result.LocalAddress = CopyOptionalEndpoint(source.LocalAddress);
        result.MaxDatagramBytes = source.MaxDatagramBytes;
        result.MaxQueueCount = source.MaxQueueCount;
        result.IdleTimeoutMilliseconds = source.IdleTimeoutMilliseconds;
        result.KeepAliveMilliseconds = source.KeepAliveMilliseconds;
        result.ReuseConnections = source.ReuseConnections;
        return result;
    }

    static SipParseOptions CreateParseOptions(int maximumMessageBytes)
    {
        SipParseOptions result = new SipParseOptions();
        result.Strict = false;
        result.PreserveOriginal = true;
        result.MaxMessageBytes = maximumMessageBytes;
        result.MaxBodyBytes = maximumMessageBytes;
        return result;
    }

    static System.Security.Cryptography.X509Certificates.X509Certificate2?
        LoadLocalCertificate(SipTlsOptions options)
    {
        if (options.CertificateBytes.Length == 0)
        {
            return null;
        }
        if (options.PrivateKeyBytes.Length > 0)
        {
            string certificate = System.Text.Encoding.UTF8.GetString(
                options.CertificateBytes);
            string privateKey = System.Text.Encoding.UTF8.GetString(
                options.PrivateKeyBytes);
            return System.Security.Cryptography.X509Certificates.X509Certificate2
                .CreateFromPem(certificate, privateKey);
        }
        return System.Security.Cryptography.X509Certificates.X509CertificateLoader
            .LoadPkcs12(options.CertificateBytes, "",
                System.Security.Cryptography.X509Certificates
                    .X509KeyStorageFlags.DefaultKeySet,
                System.Security.Cryptography.X509Certificates
                    .Pkcs12LoaderLimits.Defaults);
    }

    static System.Security.Cryptography.X509Certificates.X509Certificate2[]
        LoadTrustedRoots(byte[][] source)
    {
        System.Security.Cryptography.X509Certificates.X509Certificate2[] result =
            new System.Security.Cryptography.X509Certificates.X509Certificate2[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = System.Security.Cryptography.X509Certificates
                .X509CertificateLoader.LoadCertificate(source[index]);
            index = index + 1;
        }
        return result;
    }

    static byte[][] CopyByteArrays(byte[][] source)
    {
        byte[][] result = new byte[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyBytes(source[index]);
            index = index + 1;
        }
        return result;
    }

    static SipWriteResult CreateWriteError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static bool IsNumericAddress(string value)
    {
        return SipUriSyntax.IsIpv4Address(value)
            || SipUriSyntax.IsIpv6Address(value);
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
    }

    string FormatHost(string host, int port)
    {
        string formatted = SipUriSyntax.IsIpv6Address(host)
            ? "[" + host + "]" : host;
        int defaultPort = transportKind == SipTransportKind.SecureWebSocket
            ? 443 : 80;
        return port == defaultPort ? formatted : formatted + ":" + port;
    }

    System.TimeSpan KeepAliveInterval()
    {
        return keepAliveMilliseconds <= 0
            ? System.Threading.Timeout.InfiniteTimeSpan
            : System.TimeSpan.FromMilliseconds(keepAliveMilliseconds);
    }

    void RegisterReceivedAlias(SipWebSocketConnection connection,
        SipTransportEvent value)
    {
        if (connection.ClientSide || connection.Core == null || pool == null
            || value.Kind != SipTransportEventKind.MessageReceived
            || value.Message == null || value.Message.Message == null
            || value.Message.Message.Kind != SipMessageKind.Request
            || value.Message.Message.ViaValues.Length == 0)
        {
            return;
        }
        pool.AddServerAlias(connection.Core,
            value.Message.Message.ViaValues[0]);
    }

    static System.Security.Authentication.SslProtocols SafeProtocols()
    {
        return System.Security.Authentication.SslProtocols.Tls12
            | System.Security.Authentication.SslProtocols.Tls13;
    }

    static System.Security.Cryptography.X509Certificates.X509VerificationFlags
        SafeVerificationFlags()
    {
        return System.Security.Cryptography.X509Certificates
                .X509VerificationFlags.IgnoreEndRevocationUnknown
            | System.Security.Cryptography.X509Certificates
                .X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown
            | System.Security.Cryptography.X509Certificates
                .X509VerificationFlags.IgnoreRootRevocationUnknown;
    }

    bool ValidateServerCertificate(object sender,
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Security.Cryptography.X509Certificates.X509Chain? chain,
        System.Net.Security.SslPolicyErrors errors)
    {
        return ValidateCertificate(certificate, errors, true);
    }

    bool ValidateClientCertificate(object sender,
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Security.Cryptography.X509Certificates.X509Chain? chain,
        System.Net.Security.SslPolicyErrors errors)
    {
        return ValidateCertificate(certificate, errors, false);
    }

    bool ValidateCertificate(
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Net.Security.SslPolicyErrors errors, bool serverCertificate)
    {
        if (tlsOptions != null && !tlsOptions.ValidatePeer)
        {
            return true;
        }
        if (certificate == null)
        {
            return !serverCertificate && tlsOptions != null
                && !tlsOptions.RequireClientCertificate;
        }
        if (serverCertificate
            && tlsOptions != null && tlsOptions.ServerName != null
            && tlsOptions.ServerName.Length > 0
            && !SipCertificateDomainIdentity.MatchesCertificate(
                certificate, tlsOptions.ServerName))
        {
            return false;
        }
        if (serverCertificate
            && (tlsOptions == null || tlsOptions.ServerName == null
                || tlsOptions.ServerName.Length == 0)
            && (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                != System.Net.Security.SslPolicyErrors.None)
        {
            return false;
        }
        byte[] bytes = certificate.Export(
            System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
        using (System.Security.Cryptography.X509Certificates.X509Certificate2 peer =
            System.Security.Cryptography.X509Certificates.X509CertificateLoader
                .LoadCertificate(bytes))
        using (System.Security.Cryptography.X509Certificates.X509Chain validation =
            new System.Security.Cryptography.X509Certificates.X509Chain())
        {
            validation.ChainPolicy.RevocationMode =
                System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
            validation.ChainPolicy.VerificationFlags = SafeVerificationFlags();
            validation.ChainPolicy.ApplicationPolicy.Add(
                new System.Security.Cryptography.Oid(serverCertificate
                    ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2"));
            if (trustedRoots.Length > 0)
            {
                validation.ChainPolicy.TrustMode =
                    System.Security.Cryptography.X509Certificates
                        .X509ChainTrustMode.CustomRootTrust;
                int index = 0;
                while (index < trustedRoots.Length)
                {
                    validation.ChainPolicy.CustomTrustStore.Add(trustedRoots[index]);
                    index = index + 1;
                }
            }
            return validation.Build(peer);
        }
    }

    static bool CompleteTask(System.Threading.Tasks.Task task, long deadline)
    {
        while (!task.IsCompleted && System.Environment.TickCount64 < deadline)
        {
            int interval = 10;
            long remaining = deadline - System.Environment.TickCount64;
            if (remaining < interval)
            {
                interval = remaining <= 0 ? 1 : (int)remaining;
            }
            System.Threading.Tasks.Task delay =
                System.Threading.Tasks.Task.Delay(interval);
            System.Threading.Tasks.Task.WhenAny(
                new System.Threading.Tasks.Task[] { task, delay })
                .GetAwaiter().GetResult();
        }
        return task.IsCompleted;
    }

    static void DisposeConnection(SipWebSocketConnection connection)
    {
        connection.Closed = true;
        DisposeConnectionResources(connection);
    }

    static void DisposeConnectionResources(SipWebSocketConnection connection)
    {
        if (connection.WebSocket != null)
        {
            connection.WebSocket.Dispose();
            connection.WebSocket = null;
        }
        if (connection.Stream != null)
        {
            connection.Stream.Dispose();
            connection.Stream = null;
        }
        if (connection.Socket != null)
        {
            connection.Socket.Dispose();
            connection.Socket = null;
        }
    }

    static void DisposeCertificate(
        System.Security.Cryptography.X509Certificates.X509Certificate2? certificate)
    {
        if (certificate != null)
        {
            certificate.Dispose();
        }
    }

    static void DisposeCertificates(
        System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates)
    {
        int index = 0;
        while (index < certificates.Length)
        {
            certificates[index].Dispose();
            index = index + 1;
        }
    }
}
