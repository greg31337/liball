namespace sip.Tests;

class SipInProcessHarness : IDisposable
{
    internal static readonly SipTransportKind[] SupportedTransportKinds =
    {
        SipTransportKind.Udp,
        SipTransportKind.Tcp,
        SipTransportKind.Tls,
        SipTransportKind.WebSocket,
        SipTransportKind.SecureWebSocket,
        SipTransportKind.Custom
    };

    internal SipEndpoint Uac = new SipEndpoint();
    internal SipEndpoint Uas = new SipEndpoint();
    internal SipManualMonotonicClock Clock =
        new SipManualMonotonicClock(1000);
    internal SipInProcessNetwork Network = new SipInProcessNetwork();
    internal SipHarnessCertificateFixture ServerCertificate =
        new SipHarnessCertificateFixture("server.test");
    internal SipHarnessCertificateFixture ClientCertificate =
        new SipHarnessCertificateFixture("client.test");

    internal SipInProcessHarness()
    {
        Network.ServerCertificate = ServerCertificate.Identity;
        Network.ClientCertificate = ClientCertificate.Identity;
        Network.MutualTls = true;
        SipTransactionOptions transactions = new SipTransactionOptions();
        transactions.Timers.T1Milliseconds = 10;
        transactions.Timers.T2Milliseconds = 40;
        transactions.Timers.T4Milliseconds = 50;
        Uac.ConfigureEndpointSources(Clock,
            new SipHarnessRandomSource(1), transactions);
        Uas.ConfigureEndpointSources(Clock,
            new SipHarnessRandomSource(129), transactions);

        int index = 0;
        while (index < SupportedTransportKinds.Length)
        {
            SipTransportKind kind = SupportedTransportKinds[index];
            SipInProcessTransport uacTransport =
                new SipInProcessTransport(Network, true, kind);
            SipInProcessTransport uasTransport =
                new SipInProcessTransport(Network, false, kind);
            Network.AddPair(uacTransport, uasTransport);
            Uac.RegisterEndpointTransport(uacTransport);
            Uas.RegisterEndpointTransport(uasTransport);
            index++;
        }

        SipStackOptions options = new SipStackOptions();
        options.TransactionOptions = transactions;
        Uac.StartEndpoint(options);
        Uas.StartEndpoint(options);
        Drain(Uac);
        Drain(Uas);
    }

    internal SipSendResult SendFromUac(SipMessage message,
        SipTransportKind transport, string correlationId)
    {
        SipSendRequest request = new SipSendRequest();
        request.Message = message;
        request.Transport = transport;
        request.RemoteAddress = EndpointAddress("uas.test", transport);
        request.CorrelationId = correlationId;
        return Uac.SendEndpointMessage(request);
    }

    internal SipSendResult SendFromUas(SipMessage message,
        SipTransportKind transport, string correlationId)
    {
        SipSendRequest request = new SipSendRequest();
        request.Message = message;
        request.Transport = transport;
        request.RemoteAddress = EndpointAddress("uac.test", transport);
        request.CorrelationId = correlationId;
        return Uas.SendEndpointMessage(request);
    }

    internal void DeliverPackets()
    {
        Network.DeliverPackets();
    }

    internal void AdvanceMilliseconds(long milliseconds)
    {
        Clock.AdvanceMilliseconds(milliseconds);
        long now = Clock.GetMilliseconds();
        Uac.AdvanceEndpointTime(now);
        Uas.AdvanceEndpointTime(now);
    }

    internal static List<SipStackEvent> Drain(SipEndpoint endpoint)
    {
        List<SipStackEvent> result = new List<SipStackEvent>();
        SipStackEvent value = endpoint.PollStackEvent();
        while (value.Kind != SipStackEventKind.None)
        {
            result.Add(value);
            value = endpoint.PollStackEvent();
        }
        return result;
    }

    public void Dispose()
    {
        Stop(Uac);
        Stop(Uas);
    }

    static SipEndpointAddress EndpointAddress(string host,
        SipTransportKind transport)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = host;
        result.Port = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket ? 5061 : 5060;
        result.Transport = transport;
        result.Secure = transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
        return result;
    }

    static void Stop(SipEndpoint endpoint)
    {
        if (endpoint.GetEndpointState() != SipEndpointState.Running)
        {
            return;
        }
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        endpoint.StopEndpoint(cancellation);
    }
}

class SipInProcessNetwork
{
    internal int DropNextPacketCount;
    internal int DuplicateNextPacketCount;
    internal bool ReorderNextPair;
    internal int FragmentBytes;
    internal int LastFragmentCount;
    internal int FailNextSendCount;
    internal bool MutualTls;
    internal SipCertificateIdentity? ServerCertificate;
    internal SipCertificateIdentity? ClientCertificate;
    List<SipHarnessPacket> packets = new List<SipHarnessPacket>();
    List<SipInProcessTransport> transports =
        new List<SipInProcessTransport>();
    object gate = new object();

    internal void AddPair(SipInProcessTransport uac,
        SipInProcessTransport uas)
    {
        transports.Add(uac);
        transports.Add(uas);
    }

    internal bool QueuePacket(SipInProcessTransport sender,
        SipSendRequest request)
    {
        lock (gate)
        {
            if (FailNextSendCount > 0)
            {
                FailNextSendCount--;
                return false;
            }
            if (DropNextPacketCount > 0)
            {
                DropNextPacketCount--;
                return true;
            }
            SipHarnessPacket packet = new SipHarnessPacket();
            packet.FromUac = sender.FromUac;
            packet.Transport = sender.Kind;
            packet.Bytes = request.Bytes.ToArray();
            packet.CorrelationId = request.CorrelationId;
            packets.Add(packet);
            if (DuplicateNextPacketCount > 0)
            {
                DuplicateNextPacketCount--;
                packets.Add(packet.Copy());
            }
            return true;
        }
    }

    internal void DeliverPackets()
    {
        List<SipHarnessPacket> pending;
        lock (gate)
        {
            pending = packets.ToList();
            packets.Clear();
            if (ReorderNextPair && pending.Count >= 2)
            {
                SipHarnessPacket first = pending[0];
                pending[0] = pending[1];
                pending[1] = first;
                ReorderNextPair = false;
            }
        }
        foreach (SipHarnessPacket packet in pending)
        {
            DeliverPacket(packet);
        }
    }

    internal void Disconnect(bool fromUac, SipTransportKind kind)
    {
        int index = 0;
        while (index < transports.Count)
        {
            SipInProcessTransport candidate = transports[index];
            if (candidate.FromUac == fromUac && candidate.Kind == kind)
            {
                candidate.Disconnect();
                return;
            }
            index++;
        }
    }

    internal SipCertificateIdentity? PeerCertificate(bool fromUac)
    {
        SipCertificateIdentity? source = fromUac
            ? ServerCertificate : ClientCertificate;
        if (source == null)
        {
            return null;
        }
        SipCertificateIdentity result = new SipCertificateIdentity();
        result.Subject = source.Subject;
        result.Domains = source.Domains.ToArray();
        result.CertificateBytes = source.CertificateBytes.ToArray();
        result.ChainValid = source.ChainValid;
        result.NameValid = source.NameValid;
        result.MutualTls = MutualTls;
        return result;
    }

    void DeliverPacket(SipHarnessPacket packet)
    {
        byte[] reassembled = Reassemble(packet.Bytes);
        int index = 0;
        while (index < transports.Count)
        {
            SipInProcessTransport candidate = transports[index];
            if (candidate.FromUac != packet.FromUac
                && candidate.Kind == packet.Transport)
            {
                candidate.Receive(reassembled, packet.CorrelationId);
                return;
            }
            index++;
        }
        throw new InvalidOperationException(
            "The in-process peer transport was not registered.");
    }

    byte[] Reassemble(byte[] bytes)
    {
        int size = FragmentBytes <= 0 ? bytes.Length : FragmentBytes;
        if (size <= 0)
        {
            LastFragmentCount = 1;
            return Array.Empty<byte>();
        }
        LastFragmentCount = (bytes.Length + size - 1) / size;
        byte[] result = new byte[bytes.Length];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(size, bytes.Length - offset);
            Array.Copy(bytes, offset, result, offset, count);
            offset += count;
        }
        return result;
    }
}

class SipHarnessPacket
{
    internal bool FromUac;
    internal SipTransportKind Transport;
    internal byte[] Bytes = Array.Empty<byte>();
    internal string? CorrelationId;

    internal SipHarnessPacket Copy()
    {
        SipHarnessPacket result = new SipHarnessPacket();
        result.FromUac = FromUac;
        result.Transport = Transport;
        result.Bytes = Bytes.ToArray();
        result.CorrelationId = CorrelationId;
        return result;
    }
}

class SipInProcessTransport : ISipTransport
{
    internal bool FromUac;
    internal SipTransportKind Kind;
    SipInProcessNetwork network;
    Queue<SipTransportEvent> events = new Queue<SipTransportEvent>();
    object gate = new object();
    SipConnectionInfo? connection;
    int nextConnection = 1;

    internal SipInProcessTransport(SipInProcessNetwork network,
        bool fromUac, SipTransportKind kind)
    {
        this.network = network;
        FromUac = fromUac;
        Kind = kind;
        SipTransportEvent bound = new SipTransportEvent();
        bound.Kind = SipTransportEventKind.Bound;
        bound.Transport = kind;
        events.Enqueue(bound);
    }

    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Transport = Kind;
        return result;
    }

    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.State = SipConnectionState.Connected;
        result.Transport = Kind;
        return result;
    }

    public SipSendResult SendTransport(SipSendRequest request)
    {
        if (request.Bytes.Length == 0 && request.Message != null)
        {
            SipWriteOptions options = new SipWriteOptions();
            options.Canonical = true;
            options.ReconcileContentLength = true;
            SipWriteResult written = new SipMessageCodec().SerializeMessage(
                request.Message, options);
            if (!written.Success)
            {
                SipSendResult failure = new SipSendResult();
                failure.ErrorMessage = written.ErrorMessage;
                failure.CorrelationId = request.CorrelationId;
                return failure;
            }
            request.Bytes = written.Bytes;
        }
        SipSendResult result = new SipSendResult();
        if (IsReliable() && request.ConnectionId != null
            && (connection == null
                || connection.Id != request.ConnectionId))
        {
            result.ErrorMessage =
                "The in-process connection is stale; reconnect is required.";
            result.CorrelationId = request.CorrelationId;
            return result;
        }
        if (IsReliable() && connection == null)
        {
            connection = CreateConnection(request.RemoteAddress);
        }
        result.Success = network.QueuePacket(this, request);
        result.BytesSent = result.Success ? request.Bytes.Length : 0;
        result.ErrorMessage = result.Success
            ? null : "The in-process connection failed.";
        result.CorrelationId = request.CorrelationId;
        result.Connection = connection == null
            ? null : CopyConnection(connection);
        return result;
    }

    public SipTransportEvent PollTransportEvent()
    {
        lock (gate)
        {
            return events.Count == 0
                ? new SipTransportEvent() : events.Dequeue();
        }
    }

    public void CloseTransport()
    {
        lock (gate)
        {
            events.Clear();
        }
    }

    internal void Receive(byte[] bytes, string? correlationId)
    {
        SipTransportMessage message = new SipTransportMessage();
        message.Bytes = bytes.ToArray();
        message.Transport = Kind;
        message.CorrelationId = correlationId;
        SipTransportEvent received = new SipTransportEvent();
        received.Kind = SipTransportEventKind.MessageReceived;
        received.Transport = Kind;
        received.Message = message;
        received.CorrelationId = correlationId;
        lock (gate)
        {
            events.Enqueue(received);
        }
    }

    internal void Disconnect()
    {
        if (connection == null)
        {
            return;
        }
        SipConnectionInfo disconnected = CopyConnection(connection);
        disconnected.State = SipConnectionState.Failed;
        SipTransportEvent value = new SipTransportEvent();
        value.Kind = SipTransportEventKind.Disconnected;
        value.Transport = Kind;
        value.Connection = disconnected;
        value.ErrorMessage = "The in-process peer disconnected.";
        lock (gate)
        {
            events.Enqueue(value);
        }
        connection = null;
    }

    bool IsReliable()
    {
        return Kind == SipTransportKind.Tcp
            || Kind == SipTransportKind.Tls
            || Kind == SipTransportKind.WebSocket
            || Kind == SipTransportKind.SecureWebSocket;
    }

    SipConnectionInfo CreateConnection(SipEndpointAddress? remote)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.Id = Kind + "-" + (FromUac ? "uac-" : "uas-")
            + nextConnection;
        nextConnection++;
        result.State = SipConnectionState.Connected;
        result.Transport = Kind;
        result.RemoteAddress = remote;
        result.Secure = Kind == SipTransportKind.Tls
            || Kind == SipTransportKind.SecureWebSocket;
        result.CertificateIdentity = result.Secure
            ? network.PeerCertificate(FromUac) : null;
        if (result.CertificateIdentity != null
            && result.CertificateIdentity.Domains.Length > 0)
        {
            result.PeerIdentity = result.CertificateIdentity.Domains[0];
        }
        return result;
    }

    static SipConnectionInfo CopyConnection(SipConnectionInfo source)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.Id = source.Id;
        result.State = source.State;
        result.Transport = source.Transport;
        result.LocalAddress = source.LocalAddress;
        result.RemoteAddress = source.RemoteAddress;
        result.Secure = source.Secure;
        result.PeerIdentity = source.PeerIdentity;
        result.CertificateIdentity = source.CertificateIdentity;
        return result;
    }
}

class SipHarnessRandomSource : ISipRandomSource
{
    byte next;

    internal SipHarnessRandomSource(byte first)
    {
        next = first;
    }

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = next;
            next++;
            index++;
        }
    }
}

class SipHarnessCertificateFixture
{
    internal byte[] CertificateBytes;
    internal byte[] PrivateKeyBytes;
    internal SipCertificateIdentity Identity = new SipCertificateIdentity();

    internal SipHarnessCertificateFixture(string domain)
    {
        using System.Security.Cryptography.RSA key =
            System.Security.Cryptography.RSA.Create(2048);
        System.Security.Cryptography.X509Certificates.CertificateRequest request =
            new System.Security.Cryptography.X509Certificates.CertificateRequest(
                "CN=" + domain, key,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder names =
            new System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder();
        names.AddDnsName(domain);
        request.CertificateExtensions.Add(names.Build());
        System.Security.Cryptography.X509Certificates.X509SignatureGenerator signer =
            System.Security.Cryptography.X509Certificates.X509SignatureGenerator
                .CreateForRSA(key,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate =
            request.Create(request.SubjectName, signer,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        CertificateBytes = System.Text.Encoding.ASCII.GetBytes(
            certificate.ExportCertificatePem());
        PrivateKeyBytes = System.Text.Encoding.ASCII.GetBytes(
            key.ExportPkcs8PrivateKeyPem());
        Identity.Domains = new string[] { domain };
        Identity.CertificateBytes = certificate.Export(
            System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
        Identity.ChainValid = true;
        Identity.NameValid = true;
    }
}
