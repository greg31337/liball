namespace sip;

/// <summary>Provides polled SIP listeners and connections over IPv4 or IPv6 TCP.</summary>
public class TcpSipTransport : ISipTransport
{
    const int NetworkBufferBytes = 16384;
    const int NetworkOperationMilliseconds = 1000;
    const int ConnectMilliseconds = 5000;

    object gate = new object();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    System.Collections.Generic.List<SipTcpConnection> connections =
        new System.Collections.Generic.List<SipTcpConnection>();
    ISipMonotonicClock clock;
    SipProtocolTokenGenerator tokens;
    System.Net.Sockets.Socket? listener;
    SipReliableConnectionPool? pool;
    SipTransportOptions? transportOptions;
    SipParseOptions? parseOptions;
    SipEndpointAddress? localAddress;
    int maximumQueueCount;
    bool bound;
    bool closed;
    bool closedEventReturned;

    /// <summary>Initializes an unbound TCP SIP transport.</summary>
    public TcpSipTransport()
    {
        clock = new SipSystemMonotonicClock();
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
    }

    /// <summary>Binds an IPv4 or IPv6 TCP listening endpoint.</summary>
    /// <param name="options">The common transport options.</param>
    /// <param name="tlsOptions">Unused TLS options supplied by the common contract.</param>
    /// <param name="webSocketOptions">Unused WebSocket options supplied by the common contract.</param>
    /// <returns>The actual listening endpoint, including an assigned ephemeral port.</returns>
    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        lock (gate)
        {
            if (bound || closed)
            {
                throw new SipTransportException(
                    "The TCP transport has already been bound or closed.",
                    SipTransportKind.Tcp, null);
            }
            SipEndpointAddress? requested = ValidateBindOptions(options);
            if (requested == null || requested.Host == null)
            {
                throw new SipTransportException(
                    "The TCP bind options are invalid.", SipTransportKind.Tcp, null);
            }
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
                throw new SipTransportException(
                    "The TCP listener did not report its bound endpoint.",
                    SipTransportKind.Tcp, null);
            }
            listener = created;
            SipEndpointAddress boundEndpoint = CreateEndpoint(actual);
            localAddress = boundEndpoint;
            transportOptions = CopyTransportOptions(options);
            parseOptions = CreateParseOptions();
            maximumQueueCount = options.MaxQueueCount;
            pool = new SipReliableConnectionPool(clock, SipDefaults.MaxConnectionCount);
            bound = true;
            AddEvent(CreateEvent(SipTransportEventKind.Bound, null));
            return CopyRequiredEndpoint(boundEndpoint);
        }
    }

    /// <summary>Creates or reuses an outbound TCP connection.</summary>
    /// <param name="remoteAddress">The numeric remote TCP endpoint.</param>
    /// <returns>The connected endpoint metadata.</returns>
    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        lock (gate)
        {
            if (!bound || closed || listener == null || pool == null
                || transportOptions == null || parseOptions == null)
            {
                throw new SipTransportException(
                    "The TCP transport is not bound.", SipTransportKind.Tcp, null);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(remoteAddress, false);
            if (remote == null || remote.Host == null)
            {
                throw new SipTransportException(
                    "The TCP remote endpoint is invalid.", SipTransportKind.Tcp, null);
            }
            SipReliableConnectionCore? reusable = pool.FindConnection(
                null, remote, SipTransportKind.Tcp, transportOptions.ReuseConnections);
            if (reusable != null)
            {
                return reusable.GetConnectionInfo();
            }
            System.Net.IPAddress address = System.Net.IPAddress.Parse(remote.Host);
            if (address.AddressFamily != listener.AddressFamily)
            {
                throw new SipTransportException(
                    "The TCP destination address family does not match the listener.",
                    SipTransportKind.Tcp, null);
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
                    "The outbound TCP connection failed or timed out.",
                    SipTransportKind.Tcp, null);
            }
            SipTcpConnection? connection = CreateConnection(created);
            if (connection == null || connection.Core == null
                || !pool.AddConnection(connection.Core))
            {
                created.Dispose();
                throw new SipTransportException(
                    "The outbound TCP connection could not enter the connection pool.",
                    SipTransportKind.Tcp, null);
            }
            connections.Add(connection);
            return connection.Core.GetConnectionInfo();
        }
    }

    /// <summary>Queues one SIP message for a selected or newly connected TCP stream.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The queueing result and selected connection.</returns>
    public SipSendResult SendTransport(SipSendRequest request)
    {
        lock (gate)
        {
            SipSendResult result = CreateSendResult(request);
            if (!bound || closed || pool == null || transportOptions == null)
            {
                return FailSend(result, "The TCP transport is not bound.", request);
            }
            if (IsCancelled(request.Cancellation))
            {
                return FailSend(result, "The TCP send was cancelled.", request);
            }
            if (request.DeadlineMilliseconds >= 0
                && System.Environment.TickCount64 >= request.DeadlineMilliseconds)
            {
                return FailSend(result,
                    "The TCP send deadline has expired.", request);
            }
            SipWriteResult bytesResult = CreateSendBytes(request);
            if (!bytesResult.Success)
            {
                return FailSend(result, bytesResult.ErrorMessage == null
                    ? "The TCP message cannot be serialized." : bytesResult.ErrorMessage,
                    request);
            }
            bool udpFallback = SipTcpTransportPolicy.IsOversizedUdpFallback(
                request.Transport, bytesResult.Bytes.Length);
            if (request.Transport != SipTransportKind.Unknown
                && request.Transport != SipTransportKind.Tcp && !udpFallback)
            {
                return FailSend(result,
                    "The send request does not select TCP or require oversized UDP fallback.",
                    request);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(
                request.RemoteAddress, udpFallback);
            SipReliableConnectionCore? core = pool.FindConnection(
                request.ConnectionId, remote, SipTransportKind.Tcp,
                transportOptions.ReuseConnections);
            if (core == null && request.ConnectionId != null)
            {
                return FailSend(result,
                    "The requested TCP connection is unavailable; reconnect is required.",
                    request);
            }
            if (core == null)
            {
                if (remote == null)
                {
                    return FailSend(result,
                        "A TCP destination or live connection identifier is required.",
                        request);
                }
                SipConnectionInfo connected = ConnectTransport(remote);
                core = pool.FindConnection(connected.Id, remote,
                    SipTransportKind.Tcp, false);
            }
            if (core == null)
            {
                return FailSend(result,
                    "The TCP connection could not be selected.", request);
            }
            SipConnectionInfo info = core.GetConnectionInfo();
            SipSendRequest normalized = CopySendRequest(request);
            normalized.Transport = SipTransportKind.Tcp;
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

    /// <summary>Polls listeners, connections, and the next TCP transport event.</summary>
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
                SipTcpConnection connection = connections[index];
                if (connection.Core != null)
                {
                    SipTransportEvent value = connection.Core.PollEvent();
                    if (value.Kind != SipTransportEventKind.None)
                    {
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

    /// <summary>Closes the TCP listener and all pooled connections.</summary>
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
                    "The TCP transport was closed.");
                index = index + 1;
            }
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
        long deadline = System.Environment.TickCount64 + NetworkOperationMilliseconds;
        if (!CompleteTask(accepting, deadline) || !accepting.IsCompletedSuccessfully)
        {
            SipTransportEvent failed = CreateEvent(
                SipTransportEventKind.Disconnected, null);
            failed.ErrorMessage = "The TCP listener failed to accept a connection.";
            AddEvent(failed);
            return;
        }
        System.Net.Sockets.Socket accepted = accepting.Result;
        accepted.Blocking = false;
        accepted.NoDelay = true;
        SipTcpConnection? connection = CreateConnection(accepted);
        if (connection == null || connection.Core == null
            || !pool.AddConnection(connection.Core))
        {
            accepted.Dispose();
            SipTransportEvent refused = CreateEvent(
                SipTransportEventKind.Disconnected, null);
            refused.ErrorMessage = "The accepted TCP connection exceeded the pool limit.";
            AddEvent(refused);
            return;
        }
        connections.Add(connection);
    }

    void ServiceConnections()
    {
        int index = 0;
        while (index < connections.Count)
        {
            SipTcpConnection connection = connections[index];
            if (!connection.Closed && connection.Socket != null
                && connection.Core != null)
            {
                ServiceRead(connection);
                if (!connection.Closed)
                {
                    ServiceWrite(connection);
                }
                if (!connection.Closed && connection.Core.IsKeepAliveFailed())
                {
                    CloseConnection(connection,
                        "The TCP keepalive response timed out.");
                }
                if (!connection.Closed && connection.Core.IsIdleExpired())
                {
                    CloseConnection(connection,
                        "The TCP connection idle timeout expired.");
                }
            }
            index = index + 1;
        }
    }

    void ServiceRead(SipTcpConnection connection)
    {
        if (connection.Socket == null || connection.Core == null
            || !connection.Core.CanReceiveBytes()
            || !connection.Socket.Poll(0, System.Net.Sockets.SelectMode.SelectRead))
        {
            return;
        }
        byte[] buffer = new byte[NetworkBufferBytes];
        System.Threading.Tasks.Task<int> receiving = connection.Socket.ReceiveAsync(
            new System.ArraySegment<byte>(buffer), System.Net.Sockets.SocketFlags.None);
        long deadline = System.Environment.TickCount64 + NetworkOperationMilliseconds;
        if (!CompleteTask(receiving, deadline) || !receiving.IsCompletedSuccessfully)
        {
            CloseConnection(connection, "The TCP receive operation failed.");
            return;
        }
        int count = receiving.Result;
        if (count <= 0)
        {
            CloseConnection(connection, "The remote TCP endpoint closed the connection.");
            return;
        }
        SipReliableOperationResult received = connection.Core.ReceiveBytes(buffer, 0, count);
        if (!received.Success && !received.Backpressured)
        {
            CloseConnection(connection, received.ErrorMessage == null
                ? "The TCP stream framing failed." : received.ErrorMessage);
        }
    }

    void ServiceWrite(SipTcpConnection connection)
    {
        if (connection.Socket == null || connection.Core == null)
        {
            return;
        }
        if (!connection.Core.CanWriteBytes())
        {
            return;
        }
        SipReliableWriteChunk chunk = connection.Core.PollWrite(NetworkBufferBytes);
        if (!chunk.Available
            || !connection.Socket.Poll(0, System.Net.Sockets.SelectMode.SelectWrite))
        {
            return;
        }
        System.Threading.Tasks.Task<int> sending = connection.Socket.SendAsync(
            new System.ArraySegment<byte>(chunk.Bytes, chunk.Offset, chunk.Count),
            System.Net.Sockets.SocketFlags.None);
        long deadline = System.Environment.TickCount64 + NetworkOperationMilliseconds;
        if (!CompleteTask(sending, deadline) || !sending.IsCompletedSuccessfully
            || sending.Result <= 0)
        {
            CloseConnection(connection, "The TCP send operation failed.");
            return;
        }
        SipReliableOperationResult completed =
            connection.Core.CompleteWrite(sending.Result);
        if (!completed.Success)
        {
            CloseConnection(connection, completed.ErrorMessage == null
                ? "The TCP partial write state is invalid." : completed.ErrorMessage);
        }
    }

    SipTcpConnection? CreateConnection(System.Net.Sockets.Socket socket)
    {
        if (transportOptions == null || parseOptions == null)
        {
            return null;
        }
        System.Net.IPEndPoint? local =
            (System.Net.IPEndPoint?)socket.LocalEndPoint;
        System.Net.IPEndPoint? remote =
            (System.Net.IPEndPoint?)socket.RemoteEndPoint;
        if (local == null || remote == null)
        {
            return null;
        }
        SipConnectionInfo info = new SipConnectionInfo();
        info.Id = tokens.CreateOpaqueToken();
        info.State = SipConnectionState.Connected;
        info.Transport = SipTransportKind.Tcp;
        info.LocalAddress = CreateEndpoint(local);
        info.RemoteAddress = CreateEndpoint(remote);
        SipReliableConnectionCore core = new SipReliableConnectionCore(
            info, transportOptions, parseOptions, clock);
        SipTcpConnection result = new SipTcpConnection();
        result.Socket = socket;
        result.Core = core;
        return result;
    }

    void CloseConnection(SipTcpConnection connection, string message)
    {
        if (connection.Closed)
        {
            return;
        }
        connection.Closed = true;
        if (connection.Socket != null)
        {
            connection.Socket.Dispose();
            connection.Socket = null;
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
    }

    SipEndpointAddress? ValidateBindOptions(SipTransportOptions? options)
    {
        if (options == null || options.MaxQueueCount <= 0
            || options.IdleTimeoutMilliseconds < 0
            || options.KeepAliveMilliseconds < 0
            || options.Kind != SipTransportKind.Unknown
                && options.Kind != SipTransportKind.Tcp)
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
            requested.Port = SipTransportNames.SipPort;
        }
        if (requested.Port > 65535 || requested.Secure
            || requested.Transport != SipTransportKind.Unknown
                && requested.Transport != SipTransportKind.Tcp
            || !IsNumericAddress(requested.Host))
        {
            return null;
        }
        requested.Transport = SipTransportKind.Tcp;
        return requested;
    }

    SipEndpointAddress? ValidateRemoteAddress(SipEndpointAddress? remote,
        bool allowUdpFallback)
    {
        if (remote == null || remote.Host == null || remote.Host.Length == 0
            || remote.Port <= 0 || remote.Port > 65535 || remote.Secure
            || remote.Transport != SipTransportKind.Unknown
                && remote.Transport != SipTransportKind.Tcp
                && (!allowUdpFallback || remote.Transport != SipTransportKind.Udp)
            || !IsNumericAddress(remote.Host))
        {
            return null;
        }
        SipEndpointAddress result = CopyRequiredEndpoint(remote);
        result.Host = System.Net.IPAddress.Parse(remote.Host).ToString();
        result.Transport = SipTransportKind.Tcp;
        return result;
    }

    SipWriteResult CreateSendBytes(SipSendRequest request)
    {
        if (request.Bytes == null)
        {
            return CreateWriteError("The TCP send byte collection cannot be null.");
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
            return CreateWriteError("A SIP message or pre-serialized bytes are required.");
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

    static SipTransportEvent CreateFailureEvent(string? message,
        string? correlationId, SipConnectionInfo? connection)
    {
        SipTransportEvent result = CreateEvent(
            SipTransportEventKind.SendFailed, correlationId);
        result.ErrorMessage = message;
        result.Connection = connection;
        return result;
    }

    static SipTransportEvent CreateEvent(SipTransportEventKind kind,
        string? correlationId)
    {
        SipTransportEvent result = new SipTransportEvent();
        result.Kind = kind;
        result.Transport = SipTransportKind.Tcp;
        result.CorrelationId = correlationId;
        return result;
    }

    static SipSendResult CreateSendResult(SipSendRequest? request)
    {
        SipSendResult result = new SipSendResult();
        result.CorrelationId = request == null ? null : request.CorrelationId;
        return result;
    }

    static SipEndpointAddress CreateEndpoint(System.Net.IPEndPoint endpoint)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = endpoint.Address.ToString();
        result.Port = endpoint.Port;
        result.Transport = SipTransportKind.Tcp;
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

    static SipEndpointAddress? CopyOptionalEndpoint(SipEndpointAddress? source)
    {
        return source == null ? null : CopyRequiredEndpoint(source);
    }

    static SipTransportOptions CopyTransportOptions(SipTransportOptions source)
    {
        SipTransportOptions result = new SipTransportOptions();
        result.Kind = SipTransportKind.Tcp;
        result.LocalAddress = CopyOptionalEndpoint(source.LocalAddress);
        result.MaxDatagramBytes = source.MaxDatagramBytes;
        result.MaxQueueCount = source.MaxQueueCount;
        result.IdleTimeoutMilliseconds = source.IdleTimeoutMilliseconds;
        result.KeepAliveMilliseconds = source.KeepAliveMilliseconds;
        result.ReuseConnections = source.ReuseConnections;
        return result;
    }

    static SipParseOptions CreateParseOptions()
    {
        SipParseOptions result = new SipParseOptions();
        result.Strict = false;
        result.PreserveOriginal = true;
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
        return SipUriSyntax.IsIpv4Address(value) || SipUriSyntax.IsIpv6Address(value);
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
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
            System.Threading.Tasks.Task delay = System.Threading.Tasks.Task.Delay(interval);
            System.Threading.Tasks.Task.WhenAny(
                new System.Threading.Tasks.Task[] { task, delay }).GetAwaiter().GetResult();
        }
        return task.IsCompleted;
    }
}
