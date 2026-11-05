namespace sip;

/// <summary>Provides authenticated, encrypted SIP connections over IPv4 or IPv6 TLS.</summary>
public class TlsSipTransport : ISipTransport
{
    const int NetworkBufferBytes = 16384;
    const int NetworkOperationMilliseconds = 1000;
    const int ConnectMilliseconds = 5000;

    object gate = new object();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    System.Collections.Generic.List<SipTlsConnection> connections =
        new System.Collections.Generic.List<SipTlsConnection>();
    ISipMonotonicClock clock;
    SipProtocolTokenGenerator tokens;
    System.Net.Sockets.Socket? listener;
    SipReliableConnectionPool? pool;
    SipTransportOptions? transportOptions;
    SipTlsOptions? tlsOptions;
    SipParseOptions? parseOptions;
    System.Security.Cryptography.X509Certificates.X509Certificate2? localCertificate;
    System.Security.Cryptography.X509Certificates.X509Certificate2[] trustedRoots =
        new System.Security.Cryptography.X509Certificates.X509Certificate2[0];
    SipEndpointAddress? localAddress;
    int maximumQueueCount;
    bool bound;
    bool closed;
    bool closedEventReturned;

    /// <summary>Initializes an unbound TLS SIP transport.</summary>
    public TlsSipTransport()
    {
        clock = new SipSystemMonotonicClock();
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
    }

    /// <summary>Binds an IPv4 or IPv6 TLS listening endpoint.</summary>
    /// <param name="options">The common transport options.</param>
    /// <param name="tlsConfiguration">The TLS identity and trust configuration.</param>
    /// <param name="webSocketOptions">Unused WebSocket options supplied by the common contract.</param>
    /// <returns>The actual secure listening endpoint.</returns>
    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsConfiguration, SipWebSocketOptions webSocketOptions)
    {
        lock (gate)
        {
            if (bound || closed)
            {
                throw new SipTransportException(
                    "The TLS transport has already been bound or closed.",
                    SipTransportKind.Tls, null);
            }
            SipEndpointAddress? requested = ValidateBindOptions(options);
            SipTlsOptions? validatedTls = ValidateTlsOptions(tlsConfiguration);
            if (requested == null || requested.Host == null || validatedTls == null)
            {
                throw new SipTransportException(
                    "The TLS bind options are invalid.", SipTransportKind.Tls, null);
            }
            System.Security.Cryptography.X509Certificates.X509Certificate2? certificate =
                LoadLocalCertificate(validatedTls);
            System.Security.Cryptography.X509Certificates.X509Certificate2[] roots =
                LoadTrustedRoots(validatedTls.TrustedRoots);
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
                    "The TLS listener did not report its bound endpoint.",
                    SipTransportKind.Tls, null);
            }
            listener = created;
            SipEndpointAddress boundEndpoint = CreateEndpoint(actual);
            localAddress = boundEndpoint;
            transportOptions = CopyTransportOptions(options);
            tlsOptions = validatedTls;
            parseOptions = CreateParseOptions();
            localCertificate = certificate;
            trustedRoots = roots;
            maximumQueueCount = options.MaxQueueCount;
            pool = new SipReliableConnectionPool(clock, SipDefaults.MaxConnectionCount);
            bound = true;
            AddEvent(CreateEvent(SipTransportEventKind.Bound, null));
            return CopyRequiredEndpoint(boundEndpoint);
        }
    }

    /// <summary>Creates or reuses an authenticated outbound TLS connection.</summary>
    /// <param name="remoteAddress">The numeric remote TLS endpoint.</param>
    /// <returns>The authenticated connection metadata.</returns>
    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        lock (gate)
        {
            if (!bound || closed || listener == null || pool == null
                || transportOptions == null || parseOptions == null || tlsOptions == null)
            {
                throw new SipTransportException(
                    "The TLS transport is not bound.", SipTransportKind.Tls, null);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(remoteAddress);
            if (remote == null || remote.Host == null)
            {
                throw new SipTransportException(
                    "The TLS remote endpoint is invalid.", SipTransportKind.Tls, null);
            }
            string? destinationDomain = tlsOptions.ServerName;
            SipReliableConnectionCore? reusable = transportOptions.ReuseConnections
                && destinationDomain != null && destinationDomain.Length > 0
                ? pool.FindAliasedConnection(remote, destinationDomain)
                : pool.FindConnection(null, remote, SipTransportKind.Tls,
                    transportOptions.ReuseConnections);
            if (reusable != null)
            {
                return reusable.GetConnectionInfo();
            }
            System.Net.IPAddress address = System.Net.IPAddress.Parse(remote.Host);
            if (address.AddressFamily != listener.AddressFamily)
            {
                throw new SipTransportException(
                    "The TLS destination address family does not match the listener.",
                    SipTransportKind.Tls, null);
            }
            System.Net.Sockets.Socket created = new System.Net.Sockets.Socket(
                address.AddressFamily, System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp);
            created.Blocking = false;
            created.NoDelay = true;
            System.Threading.Tasks.Task connecting = created.ConnectAsync(
                new System.Net.IPEndPoint(address, remote.Port));
            long connectDeadline = System.Environment.TickCount64 + ConnectMilliseconds;
            if (!CompleteTask(connecting, connectDeadline)
                || !connecting.IsCompletedSuccessfully)
            {
                created.Dispose();
                throw new SipTransportException(
                    "The outbound TLS socket connection failed or timed out.",
                    SipTransportKind.Tls, null);
            }
            created.Blocking = true;
            SipTlsConnection secureConnection = CreateStreamConnection(created, true);
            if (secureConnection.Stream == null)
            {
                DisposeConnection(secureConnection);
                throw new SipTransportException(
                    "The outbound TLS stream could not be created.",
                    SipTransportKind.Tls, null);
            }
            System.Net.Security.SslClientAuthenticationOptions authentication =
                CreateClientAuthentication(remote.Host);
            System.Threading.Tasks.Task handshake =
                secureConnection.Stream.AuthenticateAsClientAsync(authentication,
                    new System.Threading.CancellationToken());
            secureConnection.HandshakeTask = handshake;
            long handshakeDeadline = System.Environment.TickCount64 + ConnectMilliseconds;
            if (!CompleteTask(handshake, handshakeDeadline)
                || !handshake.IsCompletedSuccessfully
                || !CompleteConnection(secureConnection))
            {
                DisposeConnection(secureConnection);
                throw new SipTransportException(
                    "The outbound TLS authentication failed or timed out.",
                    SipTransportKind.Tls, null);
            }
            if (secureConnection.Core == null
                || !pool.AddConnection(secureConnection.Core))
            {
                DisposeConnection(secureConnection);
                throw new SipTransportException(
                    "The outbound TLS connection could not enter the connection pool.",
                    SipTransportKind.Tls, null);
            }
            connections.Add(secureConnection);
            if (destinationDomain != null && destinationDomain.Length > 0)
            {
                pool.AddClientAlias(
                    secureConnection.Core, remote, destinationDomain);
            }
            return secureConnection.Core.GetConnectionInfo();
        }
    }

    /// <summary>Queues one SIP message for an authenticated TLS connection.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The queueing result and secure connection.</returns>
    public SipSendResult SendTransport(SipSendRequest request)
    {
        lock (gate)
        {
            SipSendResult result = CreateSendResult(request);
            if (!bound || closed || pool == null || transportOptions == null)
            {
                return FailSend(result, "The TLS transport is not bound.", request);
            }
            if (IsCancelled(request.Cancellation))
            {
                return FailSend(result, "The TLS send was cancelled.", request);
            }
            if (request.DeadlineMilliseconds >= 0
                && System.Environment.TickCount64 >= request.DeadlineMilliseconds)
            {
                return FailSend(result, "The TLS send deadline has expired.", request);
            }
            if (request.Transport != SipTransportKind.Unknown
                && request.Transport != SipTransportKind.Tls)
            {
                return FailSend(result,
                    "The send request selects a transport other than TLS.", request);
            }
            SipWriteResult bytesResult = CreateSendBytes(request);
            if (!bytesResult.Success)
            {
                return FailSend(result, bytesResult.ErrorMessage == null
                    ? "The TLS message cannot be serialized." : bytesResult.ErrorMessage,
                    request);
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(request.RemoteAddress);
            string? destinationDomain = GetDestinationDomain(request);
            SipReliableConnectionCore? core = pool.FindConnection(
                request.ConnectionId, remote, SipTransportKind.Tls,
                transportOptions.ReuseConnections
                    && (destinationDomain == null || destinationDomain.Length == 0));
            if (core == null && request.ConnectionId == null && remote != null
                && transportOptions.ReuseConnections && destinationDomain != null
                && destinationDomain.Length > 0)
            {
                core = pool.FindAliasedConnection(remote, destinationDomain);
            }
            if (core == null && request.ConnectionId != null)
            {
                return FailSend(result,
                    "The requested TLS connection is unavailable; reconnect is required.",
                    request);
            }
            if (core != null && destinationDomain != null
                && destinationDomain.Length > 0
                && !ConnectionMatchesDomain(core, destinationDomain))
            {
                return FailSend(result,
                    "The TLS connection is not authenticated for the destination domain.",
                    request);
            }
            if (core == null)
            {
                if (remote == null)
                {
                    return FailSend(result,
                        "A TLS destination or live connection identifier is required.",
                        request);
                }
                if (destinationDomain != null && destinationDomain.Length > 0
                    && (tlsOptions == null || tlsOptions.ServerName == null
                        || !SipCertificateDomainIdentity.MatchesDomains(
                            new string[] { tlsOptions.ServerName }, destinationDomain)))
                {
                    return FailSend(result,
                        "The TLS transport is not configured to authenticate the destination domain.",
                        request);
                }
                SipConnectionInfo connected = ConnectTransport(remote);
                core = pool.FindConnection(connected.Id, remote,
                    SipTransportKind.Tls, false);
            }
            if (core == null)
            {
                return FailSend(result,
                    "The TLS connection could not be selected.", request);
            }
            SipConnectionInfo info = core.GetConnectionInfo();
            SipSendRequest normalized = CopySendRequest(request);
            normalized.Transport = SipTransportKind.Tls;
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

    /// <summary>Polls TLS handshakes, encrypted connections, and the next event.</summary>
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
                SipTlsConnection connection = connections[index];
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

    /// <summary>Closes the TLS listener, streams, certificates, and pooled connections.</summary>
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
                    "The TLS transport was closed.");
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
        long deadline = System.Environment.TickCount64 + NetworkOperationMilliseconds;
        if (!CompleteTask(accepting, deadline) || !accepting.IsCompletedSuccessfully)
        {
            QueueHandshakeFailure("The TLS listener failed to accept a connection.");
            return;
        }
        System.Net.Sockets.Socket accepted = accepting.Result;
        accepted.Blocking = true;
        accepted.NoDelay = true;
        if (localCertificate == null || !localCertificate.HasPrivateKey)
        {
            accepted.Dispose();
            QueueHandshakeFailure(
                "The TLS listener has no server certificate with a private key.");
            return;
        }
        SipTlsConnection connection = CreateStreamConnection(accepted, false);
        if (connection.Stream == null)
        {
            DisposeConnection(connection);
            QueueHandshakeFailure("The accepted TLS stream could not be created.");
            return;
        }
        System.Net.Security.SslServerAuthenticationOptions authentication =
            CreateServerAuthentication();
        connection.HandshakeTask = connection.Stream.AuthenticateAsServerAsync(
            authentication, new System.Threading.CancellationToken());
        connections.Add(connection);
    }

    void ServiceConnections()
    {
        int index = 0;
        while (index < connections.Count)
        {
            SipTlsConnection connection = connections[index];
            if (!connection.Closed && connection.Core == null)
            {
                CompleteAcceptedHandshake(connection);
            }
            if (!connection.Closed && connection.Stream != null
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
                        "The TLS keepalive response timed out.");
                }
                if (!connection.Closed && connection.Core.IsIdleExpired())
                {
                    CloseConnection(connection,
                        "The TLS connection idle timeout expired.");
                }
            }
            index = index + 1;
        }
    }

    void CompleteAcceptedHandshake(SipTlsConnection connection)
    {
        if (connection.HandshakeTask == null || !connection.HandshakeTask.IsCompleted)
        {
            return;
        }
        if (!connection.HandshakeTask.IsCompletedSuccessfully
            || !CompleteConnection(connection) || connection.Core == null
            || pool == null || !pool.AddConnection(connection.Core))
        {
            DisposeConnection(connection);
            QueueHandshakeFailure("The accepted TLS authentication failed.");
        }
    }

    bool CompleteConnection(SipTlsConnection connection)
    {
        if (connection.Socket == null || connection.Stream == null
            || !connection.Stream.IsAuthenticated || !connection.Stream.IsEncrypted
            || transportOptions == null || parseOptions == null || tlsOptions == null)
        {
            return false;
        }
        if (!connection.ClientSide && tlsOptions.RequireClientCertificate
            && connection.Stream.RemoteCertificate == null)
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
            CreateCertificateIdentity(connection.Stream, connection.ClientSide);
        SipConnectionInfo info = new SipConnectionInfo();
        info.Id = tokens.CreateOpaqueToken();
        info.State = SipConnectionState.Connected;
        info.Transport = SipTransportKind.Tls;
        info.LocalAddress = CreateEndpoint(local);
        info.RemoteAddress = CreateEndpoint(remote);
        info.Secure = true;
        info.CertificateIdentity = certificateIdentity;
        info.PeerIdentity = GetPeerIdentity(certificateIdentity, connection.ClientSide);
        connection.Core = new SipReliableConnectionCore(
            info, transportOptions, parseOptions, clock);
        return true;
    }

    void ServiceRead(SipTlsConnection connection)
    {
        if (connection.Stream == null || connection.Core == null)
        {
            return;
        }
        if (connection.ReadTask == null)
        {
            if (!connection.Core.CanReceiveBytes())
            {
                return;
            }
            connection.ReadBuffer = new byte[NetworkBufferBytes];
            connection.ReadTask = connection.Stream.ReadAsync(
                connection.ReadBuffer, 0, connection.ReadBuffer.Length);
        }
        if (!connection.ReadTask.IsCompleted)
        {
            return;
        }
        if (!connection.ReadTask.IsCompletedSuccessfully)
        {
            CloseConnection(connection, "The TLS receive operation failed.");
            return;
        }
        int count = connection.ReadTask.Result;
        if (count <= 0)
        {
            CloseConnection(connection, "The remote TLS endpoint closed the connection.");
            return;
        }
        SipReliableOperationResult received = connection.Core.ReceiveBytes(
            connection.ReadBuffer, 0, count);
        connection.ReadTask = null;
        connection.ReadBuffer = new byte[0];
        if (!received.Success && !received.Backpressured)
        {
            CloseConnection(connection, received.ErrorMessage == null
                ? "The TLS stream framing failed." : received.ErrorMessage);
        }
    }

    void ServiceWrite(SipTlsConnection connection)
    {
        if (connection.Stream == null || connection.Core == null
            || connection.WriteTask == null && !connection.Core.CanWriteBytes())
        {
            return;
        }
        if (connection.WriteTask == null)
        {
            SipReliableWriteChunk chunk = connection.Core.PollWrite(NetworkBufferBytes);
            if (!chunk.Available)
            {
                return;
            }
            connection.WriteCount = chunk.Count;
            connection.WriteTask = connection.Stream.WriteAsync(
                chunk.Bytes, chunk.Offset, chunk.Count);
        }
        if (!connection.WriteTask.IsCompleted)
        {
            return;
        }
        if (!connection.WriteTask.IsCompletedSuccessfully)
        {
            CloseConnection(connection, "The TLS send operation failed.");
            return;
        }
        SipReliableOperationResult completed =
            connection.Core.CompleteWrite(connection.WriteCount);
        connection.WriteTask = null;
        connection.WriteCount = 0;
        if (!completed.Success)
        {
            CloseConnection(connection, completed.ErrorMessage == null
                ? "The TLS write state is invalid." : completed.ErrorMessage);
        }
    }

    SipTlsConnection CreateStreamConnection(System.Net.Sockets.Socket socket,
        bool clientSide)
    {
        System.Net.Sockets.NetworkStream network =
            new System.Net.Sockets.NetworkStream(socket, true);
        System.Net.Security.SslStream stream = new System.Net.Security.SslStream(
            network, false, clientSide
                ? ValidateServerCertificate : ValidateClientCertificate);
        SipTlsConnection result = new SipTlsConnection();
        result.Socket = socket;
        result.Stream = stream;
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
        result.TrustMode = System.Security.Cryptography.X509Certificates.X509ChainTrustMode.CustomRootTrust;
        result.RevocationMode =
            System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
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
            System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificate(
                certificateBytes))
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
            result.MutualTls = stream.LocalCertificate != null;
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

    void CloseConnection(SipTlsConnection connection, string message)
    {
        if (connection.Closed)
        {
            return;
        }
        connection.Closed = true;
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

    SipEndpointAddress? ValidateBindOptions(SipTransportOptions? options)
    {
        if (options == null || options.MaxQueueCount <= 0
            || options.IdleTimeoutMilliseconds < 0
            || options.KeepAliveMilliseconds < 0
            || options.Kind != SipTransportKind.Unknown
                && options.Kind != SipTransportKind.Tls)
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
            requested.Port = SipTransportNames.SipsPort;
        }
        if (requested.Port > 65535
            || requested.Transport != SipTransportKind.Unknown
                && requested.Transport != SipTransportKind.Tls
            || !IsNumericAddress(requested.Host))
        {
            return null;
        }
        requested.Transport = SipTransportKind.Tls;
        requested.Secure = true;
        return requested;
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

    SipEndpointAddress? ValidateRemoteAddress(SipEndpointAddress? remote)
    {
        if (remote == null || remote.Host == null || remote.Host.Length == 0
            || remote.Port <= 0 || remote.Port > 65535
            || remote.Transport != SipTransportKind.Unknown
                && remote.Transport != SipTransportKind.Tls
            || !IsNumericAddress(remote.Host))
        {
            return null;
        }
        SipEndpointAddress result = CopyRequiredEndpoint(remote);
        result.Host = System.Net.IPAddress.Parse(remote.Host).ToString();
        result.Transport = SipTransportKind.Tls;
        result.Secure = true;
        return result;
    }

    SipWriteResult CreateSendBytes(SipSendRequest request)
    {
        if (request.Bytes == null)
        {
            return CreateWriteError("The TLS send byte collection cannot be null.");
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
        result.Transport = SipTransportKind.Tls;
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
        result.Transport = SipTransportKind.Tls;
        result.Secure = true;
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
        result.Kind = SipTransportKind.Tls;
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
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet,
                System.Security.Cryptography.X509Certificates.Pkcs12LoaderLimits.Defaults);
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
        return SipUriSyntax.IsIpv4Address(value) || SipUriSyntax.IsIpv6Address(value);
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
    }

    string? GetDestinationDomain(SipSendRequest request)
    {
        if (request.Message != null && request.Message.RequestUri != null
            && request.Message.RequestUri.Host != null
            && request.Message.RequestUri.Host.Length > 0)
        {
            return request.Message.RequestUri.Host;
        }
        return tlsOptions == null ? null : tlsOptions.ServerName;
    }

    static bool ConnectionMatchesDomain(SipReliableConnectionCore connection,
        string domain)
    {
        SipConnectionInfo info = connection.GetConnectionInfo();
        return info.Secure && info.Transport == SipTransportKind.Tls
            && info.CertificateIdentity != null
            && info.CertificateIdentity.ChainValid
            && SipCertificateDomainIdentity.MatchesDomains(
                info.CertificateIdentity.Domains, domain);
    }

    void RegisterReceivedAlias(SipTlsConnection connection,
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
        return ValidateCertificate(certificate, chain, errors, true);
    }

    bool ValidateClientCertificate(object sender,
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Security.Cryptography.X509Certificates.X509Chain? chain,
        System.Net.Security.SslPolicyErrors errors)
    {
        return ValidateCertificate(certificate, chain, errors, false);
    }

    bool ValidateCertificate(
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        System.Security.Cryptography.X509Certificates.X509Chain? suppliedChain,
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
            if (suppliedChain != null)
            {
                int chainIndex = 1;
                while (chainIndex < suppliedChain.ChainElements.Count)
                {
                    validation.ChainPolicy.ExtraStore.Add(
                        suppliedChain.ChainElements[chainIndex].Certificate);
                    chainIndex = chainIndex + 1;
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
            System.Threading.Tasks.Task delay = System.Threading.Tasks.Task.Delay(interval);
            System.Threading.Tasks.Task.WhenAny(
                new System.Threading.Tasks.Task[] { task, delay }).GetAwaiter().GetResult();
        }
        return task.IsCompleted;
    }

    static void DisposeConnection(SipTlsConnection connection)
    {
        connection.Closed = true;
        DisposeConnectionResources(connection);
    }

    static void DisposeConnectionResources(SipTlsConnection connection)
    {
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
