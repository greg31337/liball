using System.Net;

namespace diameter.Tests;

internal class InProcessDiameterHarness
{
    internal InProcessHarnessClock Clock = new InProcessHarnessClock();
    internal InProcessHarnessDnsResolver Dns =
        new InProcessHarnessDnsResolver();
    internal InProcessHarnessNetwork Network;
    internal InProcessHarnessNode First;
    internal InProcessHarnessNode Second;

    internal InProcessDiameterHarness()
    {
        Network = new InProcessHarnessNetwork(Clock);
        First = new InProcessHarnessNode(
            "first.example",
            41001,
            Network,
            Clock);
        Second = new InProcessHarnessNode(
            "second.example",
            41002,
            Network,
            Clock);
    }

    internal void StartDiameterNodes()
    {
        First.Node.ConfigureDiameterNode(First.Configuration);
        Second.Node.ConfigureDiameterNode(Second.Configuration);
        First.Node.StartDiameterNode();
        Second.Node.StartDiameterNode();
    }

    internal InProcessHarnessLink ConnectFirstToSecond()
    {
        NodeComposition firstComposition = First.Node.Lifecycle
            .ReadDiameterNodeComposition();
        int acceptedCount;
        lock (Second.Node.Lifecycle.LifecycleLock)
        {
            acceptedCount = Second.Node.Lifecycle
                .AcceptedConnections.Count;
        }

        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = Second.Configuration.OriginHost.Value;
        endpoint.Host = Second.Configuration.Transport.Host;
        endpoint.Address = IPAddress.Loopback;
        endpoint.Port = Second.Configuration.Transport.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        DiameterConnectionResult connected = firstComposition
            .TransportFactory.ConnectDiameterEndpoint(
                endpoint,
                firstComposition.Configuration.Transport,
                firstComposition.Configuration.Security,
                2000);
        if (connected.Status != DiameterTransportStatus.Success ||
            connected.Connection == null)
        {
            throw new InvalidOperationException(
                "The in-process connection was not created.");
        }

        bool accepted = SpinWait.SpinUntil(() =>
        {
            lock (Second.Node.Lifecycle.LifecycleLock)
            {
                return Second.Node.Lifecycle.AcceptedConnections.Count >
                    acceptedCount;
            }
        }, 2000);
        if (!accepted)
        {
            throw new InvalidOperationException(
                "The in-process connection was not accepted.");
        }

        IDiameterConnection inbound;
        lock (Second.Node.Lifecycle.LifecycleLock)
        {
            inbound = Second.Node.Lifecycle.AcceptedConnections[
                Second.Node.Lifecycle.AcceptedConnections.Count - 1];
        }

        InProcessHarnessLink result = new InProcessHarnessLink();
        result.Outbound = connected.Connection;
        result.Inbound = inbound;
        return result;
    }

    internal void AdvanceDiameterTime(int milliseconds)
    {
        Clock.AdvanceDiameterTime(milliseconds);
        Network.ReleaseDueDeliveries();
    }

    internal void StopDiameterNodes()
    {
        First.Node.StopDiameterNode();
        Second.Node.StopDiameterNode();
    }
}

internal class InProcessHarnessNode
{
    internal DiameterNode Node = new DiameterNode();
    internal DiameterNodeConfiguration Configuration =
        new DiameterNodeConfiguration();
    internal InProcessHarnessTransportFactory Transport;
    internal DiameterInMemorySessionStore SessionStore =
        new DiameterInMemorySessionStore();
    internal DiameterInMemoryAccountingRecordStore AccountingStore =
        new DiameterInMemoryAccountingRecordStore();
    internal DiameterInMemoryCreditControlStore CreditControlStore =
        new DiameterInMemoryCreditControlStore();
    internal DuplicateRequestStore DuplicateStore;

    internal InProcessHarnessNode(
        string identity,
        int port,
        InProcessHarnessNetwork network,
        InProcessHarnessClock clock)
    {
        Transport = new InProcessHarnessTransportFactory(
            network,
            identity);
        DuplicateStore = new DuplicateRequestStore(
            clock,
            1024,
            30000);
        Configuration.OriginHost.Value = identity;
        Configuration.OriginRealm.Value = "example";
        Configuration.ProductName = "diameter.Tests.Harness";
        Configuration.Roles =
            DiameterNodeRole.Client | DiameterNodeRole.Server;
        Configuration.Transport.Host = "127.0.0.1";
        Configuration.Transport.Port = port;
        Configuration.Transport.EnableListener = true;
        Configuration.Security.AllowUnsecuredTransport = true;
        Configuration.TransportFactory = Transport;
        Configuration.Clock = clock;
        Configuration.SessionStore = SessionStore;
        Configuration.AccountingRecordStore = AccountingStore;
        Configuration.CreditControlStore = CreditControlStore;
        Configuration.DuplicateRequestStore = DuplicateStore;
    }
}

internal class InProcessHarnessLink
{
    internal IDiameterConnection Outbound = null!;
    internal IDiameterConnection Inbound = null!;
}

internal class InProcessHarnessNetwork
{
    internal object NetworkLock = new object();
    internal Dictionary<int, InProcessHarnessListener> Listeners =
        new Dictionary<int, InProcessHarnessListener>();
    internal List<InProcessHarnessDelivery> DelayedDeliveries =
        new List<InProcessHarnessDelivery>();
    internal InProcessHarnessClock Clock;
    internal int MaximumFragmentSize;
    internal int DeliveryDelayMilliseconds;
    internal bool DropNextDelivery;
    internal bool DuplicateNextDelivery;
    internal long ConnectionCount;

    internal InProcessHarnessNetwork(InProcessHarnessClock clock)
    {
        Clock = clock;
    }

    internal void RegisterDiameterListener(
        int port,
        InProcessHarnessListener listener)
    {
        lock (NetworkLock)
        {
            Listeners[port] = listener;
        }
    }

    internal void RemoveDiameterListener(
        int port,
        InProcessHarnessListener listener)
    {
        lock (NetworkLock)
        {
            if (Listeners.ContainsKey(port) &&
                object.ReferenceEquals(Listeners[port], listener))
            {
                Listeners.Remove(port);
            }
        }
    }

    internal DiameterConnectionResult ConnectDiameterNodes(
        InProcessHarnessTransportFactory source,
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterConnectionResult failure =
            new DiameterConnectionResult();
        if (endpoint == null || transport == null || security == null)
        {
            failure.Status =
                DiameterTransportStatus.InvalidConfiguration;
            failure.Code = "harness-connect-invalid";
            return failure;
        }

        InProcessHarnessListener? listener;
        long connectionNumber;
        lock (NetworkLock)
        {
            listener = Listeners.ContainsKey(endpoint.Port) ?
                Listeners[endpoint.Port] : null;
            if (listener == null || listener.Closed)
            {
                failure.Status = DiameterTransportStatus.Failed;
                failure.Code = "harness-listener-unavailable";
                return failure;
            }

            ConnectionCount = ConnectionCount + 1L;
            connectionNumber = ConnectionCount;
        }

        DiameterEndpoint localEndpoint = CreateDiameterEndpoint(
            source.Identity,
            transport.Host,
            transport.Port);
        DiameterEndpoint remoteEndpoint = CreateDiameterEndpoint(
            listener.Identity,
            endpoint.Host,
            endpoint.Port);
        InProcessHarnessConnection outbound =
            new InProcessHarnessConnection(
                this,
                "harness-" + connectionNumber + "-out",
                localEndpoint,
                remoteEndpoint,
                DiameterConnectionDirection.Outbound,
                transport,
                security,
                Clock.ReadUtcTime());
        InProcessHarnessConnection inbound =
            new InProcessHarnessConnection(
                this,
                "harness-" + connectionNumber + "-in",
                remoteEndpoint,
                localEndpoint,
                DiameterConnectionDirection.Inbound,
                listener.Transport,
                listener.Security,
                Clock.ReadUtcTime());
        outbound.Peer = inbound;
        inbound.Peer = outbound;
        listener.EnqueueDiameterConnection(inbound);

        DiameterConnectionResult result =
            new DiameterConnectionResult();
        result.Connection = outbound;
        result.Metadata = outbound.ReadDiameterConnectionMetadata();
        result.Code = "harness-connected";
        return result;
    }

    internal DiameterTransportIoResult SendDiameterBytes(
        InProcessHarnessConnection source,
        ReadOnlySpan<byte> bytes)
    {
        DiameterTransportIoResult result =
            new DiameterTransportIoResult();
        InProcessHarnessConnection? target = source.Peer;
        if (source.Closed || target == null || target.Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
            result.Code = "harness-connection-closed";
            return result;
        }

        byte[] owned = bytes.ToArray();
        bool drop;
        bool duplicate;
        int fragmentSize;
        int delay;
        lock (NetworkLock)
        {
            drop = DropNextDelivery;
            duplicate = DuplicateNextDelivery;
            fragmentSize = MaximumFragmentSize;
            delay = DeliveryDelayMilliseconds;
            DropNextDelivery = false;
            DuplicateNextDelivery = false;
        }

        if (!drop)
        {
            ScheduleDiameterPayload(
                target,
                owned,
                fragmentSize,
                delay);
            if (duplicate)
            {
                ScheduleDiameterPayload(
                    target,
                    owned,
                    fragmentSize,
                    delay);
            }
        }

        result.TransferredCount = owned.Length;
        result.Code = drop ?
            "harness-delivery-dropped" :
            "harness-delivery-scheduled";
        return result;
    }

    internal void ScheduleDiameterPayload(
        InProcessHarnessConnection target,
        byte[] payload,
        int fragmentSize,
        int delayMilliseconds)
    {
        int size = fragmentSize <= 0 ? payload.Length : fragmentSize;
        if (size <= 0)
        {
            size = 1;
        }

        int offset = 0;
        while (offset < payload.Length)
        {
            int count = Math.Min(size, payload.Length - offset);
            byte[] fragment = payload.AsSpan(offset, count).ToArray();
            if (delayMilliseconds <= 0)
            {
                target.EnqueueDiameterBytes(fragment);
            }
            else
            {
                InProcessHarnessDelivery delivery =
                    new InProcessHarnessDelivery();
                delivery.Target = target;
                delivery.Payload = fragment;
                delivery.DueMilliseconds =
                    Clock.ReadMonotonicMilliseconds() +
                    delayMilliseconds;
                lock (NetworkLock)
                {
                    DelayedDeliveries.Add(delivery);
                }
            }

            offset = offset + count;
        }
    }

    internal void ReleaseDueDeliveries()
    {
        List<InProcessHarnessDelivery> due =
            new List<InProcessHarnessDelivery>();
        long now = Clock.ReadMonotonicMilliseconds();
        lock (NetworkLock)
        {
            int index = 0;
            while (index < DelayedDeliveries.Count)
            {
                if (DelayedDeliveries[index].DueMilliseconds <= now)
                {
                    due.Add(DelayedDeliveries[index]);
                    DelayedDeliveries.RemoveAt(index);
                }
                else
                {
                    index = index + 1;
                }
            }
        }

        int dueIndex = 0;
        while (dueIndex < due.Count)
        {
            due[dueIndex].Target.EnqueueDiameterBytes(
                due[dueIndex].Payload);
            dueIndex = dueIndex + 1;
        }
    }

    internal static DiameterEndpoint CreateDiameterEndpoint(
        string identity,
        string host,
        int port)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = identity;
        result.Host = host;
        result.Address = IPAddress.Loopback;
        result.Port = port;
        result.Transport = DiameterTransportKind.Tcp;
        result.Security = DiameterSecurityMode.None;
        return result;
    }
}

internal class InProcessHarnessDelivery
{
    internal InProcessHarnessConnection Target = null!;
    internal byte[] Payload = new byte[0];
    internal long DueMilliseconds;
}

internal class InProcessHarnessTransportFactory :
    IDiameterTransportFactory
{
    internal InProcessHarnessNetwork Network;
    internal string Identity;
    internal InProcessHarnessListener? Listener;

    internal InProcessHarnessTransportFactory(
        InProcessHarnessNetwork network,
        string identity)
    {
        Network = network;
        Identity = identity;
    }

    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult result =
            new DiameterTransportSupportResult();
        if (transport == null || security == null ||
            transport.Kind != DiameterTransportKind.Tcp ||
            security.Mode != DiameterSecurityMode.None)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "harness-transport-unsupported";
            return result;
        }

        result.Metadata.Kind = DiameterTransportKind.Tcp;
        result.Metadata.SupportsActiveConnections = true;
        result.Metadata.SupportsListeners = true;
        result.Metadata.SupportsIpv4 = true;
        result.Metadata.MessageOriented = false;
        result.Metadata.MaximumInboundStreams = 1;
        result.Metadata.MaximumOutboundStreams = 1;
        result.Code = "harness-transport-supported";
        return result;
    }

    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        if (transport == null || security == null)
        {
            result.Status =
                DiameterTransportStatus.InvalidConfiguration;
            return result;
        }

        Listener = new InProcessHarnessListener(
            Network,
            Identity,
            transport,
            security);
        Network.RegisterDiameterListener(transport.Port, Listener);
        result.Listener = Listener;
        result.Metadata = Listener.ReadDiameterListenerMetadata();
        return result;
    }

    public DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            DiameterConnectionResult invalid =
                new DiameterConnectionResult();
            invalid.Status =
                DiameterTransportStatus.InvalidConfiguration;
            return invalid;
        }

        return Network.ConnectDiameterNodes(
            this,
            endpoint,
            transport,
            security);
    }
}

internal class InProcessHarnessListener : IDiameterListener
{
    internal object ListenerLock = new object();
    internal Queue<InProcessHarnessConnection> Connections =
        new Queue<InProcessHarnessConnection>();
    internal InProcessHarnessNetwork Network;
    internal string Identity;
    internal DiameterTransportConfiguration Transport;
    internal DiameterSecurityConfiguration Security;
    internal bool Closed;

    internal InProcessHarnessListener(
        InProcessHarnessNetwork network,
        string identity,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security)
    {
        Network = network;
        Identity = identity;
        Transport = ConfigurationValidator
            .CloneDiameterTransportConfiguration(transport);
        Security = ConfigurationValidator
            .CloneDiameterSecurityConfiguration(security);
    }

    internal void EnqueueDiameterConnection(
        InProcessHarnessConnection connection)
    {
        lock (ListenerLock)
        {
            if (Closed)
            {
                connection.CloseDiameterConnection();
                return;
            }

            Connections.Enqueue(connection);
            Monitor.PulseAll(ListenerLock);
        }
    }

    public DiameterConnectionResult AcceptDiameterConnection(
        int timeoutMilliseconds)
    {
        DiameterConnectionResult result =
            new DiameterConnectionResult();
        lock (ListenerLock)
        {
            if (Connections.Count == 0 && !Closed)
            {
                Monitor.Wait(ListenerLock, timeoutMilliseconds);
            }

            if (Connections.Count > 0)
            {
                InProcessHarnessConnection connection =
                    Connections.Dequeue();
                result.Connection = connection;
                result.Metadata =
                    connection.ReadDiameterConnectionMetadata();
                result.Code = "harness-connection-accepted";
            }
            else
            {
                result.Status = Closed ?
                    DiameterTransportStatus.Closed :
                    DiameterTransportStatus.TimedOut;
            }
        }

        return result;
    }

    public DiameterListenerMetadata ReadDiameterListenerMetadata()
    {
        DiameterListenerMetadata result =
            new DiameterListenerMetadata();
        lock (ListenerLock)
        {
            result.Open = !Closed;
        }

        result.LocalEndpoint =
            InProcessHarnessNetwork.CreateDiameterEndpoint(
                Identity,
                Transport.Host,
                Transport.Port);
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Transport.SupportsActiveConnections = true;
        result.Transport.SupportsListeners = true;
        result.Transport.SupportsIpv4 = true;
        result.Transport.MaximumInboundStreams = 1;
        result.Transport.MaximumOutboundStreams = 1;
        result.SecurityMode = DiameterSecurityMode.None;
        return result;
    }

    public DiameterTransportOperationResult CloseDiameterListener()
    {
        lock (ListenerLock)
        {
            Closed = true;
            Monitor.PulseAll(ListenerLock);
        }

        Network.RemoveDiameterListener(Transport.Port, this);
        return new DiameterTransportOperationResult();
    }
}

internal class InProcessHarnessConnection : IDiameterConnection
{
    internal object ConnectionLock = new object();
    internal Queue<byte[]> Received = new Queue<byte[]>();
    internal InProcessHarnessNetwork Network;
    internal string ConnectionIdentifier;
    internal DiameterEndpoint LocalEndpoint;
    internal DiameterEndpoint RemoteEndpoint;
    internal DiameterConnectionDirection Direction;
    internal DiameterTransportConfiguration Transport;
    internal DiameterSecurityConfiguration Security;
    internal DateTimeOffset EstablishedAt;
    internal InProcessHarnessConnection? Peer;
    internal bool Closed;

    internal InProcessHarnessConnection(
        InProcessHarnessNetwork network,
        string connectionIdentifier,
        DiameterEndpoint localEndpoint,
        DiameterEndpoint remoteEndpoint,
        DiameterConnectionDirection direction,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DateTimeOffset establishedAt)
    {
        Network = network;
        ConnectionIdentifier = connectionIdentifier;
        LocalEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
            localEndpoint);
        RemoteEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
            remoteEndpoint);
        Direction = direction;
        Transport = ConfigurationValidator
            .CloneDiameterTransportConfiguration(transport);
        Security = ConfigurationValidator
            .CloneDiameterSecurityConfiguration(security);
        EstablishedAt = establishedAt;
    }

    internal void EnqueueDiameterBytes(byte[] bytes)
    {
        lock (ConnectionLock)
        {
            if (!Closed)
            {
                Received.Enqueue(bytes);
                Monitor.PulseAll(ConnectionLock);
            }
        }
    }

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            DiameterTransportIoResult invalid =
                new DiameterTransportIoResult();
            invalid.Status =
                DiameterTransportStatus.InvalidConfiguration;
            return invalid;
        }

        return Network.SendDiameterBytes(this, source);
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result =
            new DiameterTransportIoResult();
        if (destination.Length == 0 || timeoutMilliseconds <= 0)
        {
            result.Status =
                DiameterTransportStatus.InvalidConfiguration;
            return result;
        }

        lock (ConnectionLock)
        {
            if (Received.Count == 0 && !Closed)
            {
                Monitor.Wait(ConnectionLock, timeoutMilliseconds);
            }

            if (Received.Count == 0)
            {
                result.Status = Closed ?
                    DiameterTransportStatus.Closed :
                    DiameterTransportStatus.TimedOut;
                return result;
            }

            byte[] bytes = Received.Dequeue();
            int count = Math.Min(destination.Length, bytes.Length);
            bytes.AsSpan(0, count).CopyTo(destination);
            result.TransferredCount = count;
            if (count < bytes.Length)
            {
                Queue<byte[]> remaining = new Queue<byte[]>();
                remaining.Enqueue(bytes.AsSpan(count).ToArray());
                while (Received.Count > 0)
                {
                    remaining.Enqueue(Received.Dequeue());
                }

                Received = remaining;
            }

            return result;
        }
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        DiameterConnectionMetadata result =
            new DiameterConnectionMetadata();
        result.ConnectionId = ConnectionIdentifier;
        result.LocalEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
            LocalEndpoint);
        result.RemoteEndpoint = PeerTableCloner.ClonePeerTableEndpoint(
            RemoteEndpoint);
        result.Direction = Direction;
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Transport.SupportsActiveConnections = true;
        result.Transport.SupportsListeners = true;
        result.Transport.SupportsIpv4 = true;
        result.Transport.MaximumInboundStreams = 1;
        result.Transport.MaximumOutboundStreams = 1;
        result.Security.Mode = DiameterSecurityMode.None;
        result.EstablishedAt = EstablishedAt;
        lock (ConnectionLock)
        {
            result.Open = !Closed;
        }

        return result;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        InProcessHarnessConnection? peer = Peer;
        lock (ConnectionLock)
        {
            Closed = true;
            Monitor.PulseAll(ConnectionLock);
        }

        if (peer != null)
        {
            peer.CloseDiameterConnectionFromPeer();
        }

        return new DiameterTransportOperationResult();
    }

    internal void CloseDiameterConnectionFromPeer()
    {
        lock (ConnectionLock)
        {
            Closed = true;
            Monitor.PulseAll(ConnectionLock);
        }
    }
}

internal class InProcessHarnessClock : IDiameterClock
{
    internal object ClockLock = new object();
    internal DateTimeOffset UtcTime = new DateTimeOffset(
        2026, 8, 29, 20, 0, 0, TimeSpan.Zero);
    internal long MonotonicMilliseconds;

    public DateTimeOffset ReadUtcTime()
    {
        lock (ClockLock)
        {
            return UtcTime;
        }
    }

    public long ReadMonotonicMilliseconds()
    {
        lock (ClockLock)
        {
            return MonotonicMilliseconds;
        }
    }

    internal void AdvanceDiameterTime(int milliseconds)
    {
        if (milliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("milliseconds");
        }

        lock (ClockLock)
        {
            UtcTime = UtcTime.AddMilliseconds(milliseconds);
            MonotonicMilliseconds =
                MonotonicMilliseconds + milliseconds;
        }
    }
}

internal class InProcessHarnessDnsResolver : IDnsResolver
{
    internal object DnsLock = new object();
    internal Dictionary<string, DiameterDnsQueryResult> Naptr =
        new Dictionary<string, DiameterDnsQueryResult>();
    internal Dictionary<string, DiameterDnsQueryResult> Srv =
        new Dictionary<string, DiameterDnsQueryResult>();
    internal Dictionary<string, DiameterDnsQueryResult> Ipv4 =
        new Dictionary<string, DiameterDnsQueryResult>();
    internal Dictionary<string, DiameterDnsQueryResult> Ipv6 =
        new Dictionary<string, DiameterDnsQueryResult>();

    internal void SetDiameterSrvResult(
        string name,
        DiameterDnsQueryResult result)
    {
        lock (DnsLock)
        {
            Srv[name] = CloneDiameterDnsResult(result);
        }
    }

    internal void SetDiameterNaptrResult(
        string name,
        DiameterDnsQueryResult result)
    {
        lock (DnsLock)
        {
            Naptr[name] = CloneDiameterDnsResult(result);
        }
    }

    internal void SetDiameterIpv4Result(
        string name,
        DiameterDnsQueryResult result)
    {
        lock (DnsLock)
        {
            Ipv4[name] = CloneDiameterDnsResult(result);
        }
    }

    internal void SetDiameterIpv6Result(
        string name,
        DiameterDnsQueryResult result)
    {
        lock (DnsLock)
        {
            Ipv6[name] = CloneDiameterDnsResult(result);
        }
    }

    public DiameterDnsQueryResult ResolveDiameterNaptrRecords(
        string name,
        int timeoutMilliseconds)
    {
        return ReadDiameterDnsResult(Naptr, name);
    }

    public DiameterDnsQueryResult ResolveDiameterSrvRecords(
        string name,
        int timeoutMilliseconds)
    {
        return ReadDiameterDnsResult(Srv, name);
    }

    public DiameterDnsQueryResult ResolveDiameterIpv4Records(
        string name,
        int timeoutMilliseconds)
    {
        return ReadDiameterDnsResult(Ipv4, name);
    }

    public DiameterDnsQueryResult ResolveDiameterIpv6Records(
        string name,
        int timeoutMilliseconds)
    {
        return ReadDiameterDnsResult(Ipv6, name);
    }

    internal DiameterDnsQueryResult ReadDiameterDnsResult(
        Dictionary<string, DiameterDnsQueryResult> results,
        string name)
    {
        lock (DnsLock)
        {
            if (results.ContainsKey(name))
            {
                return CloneDiameterDnsResult(results[name]);
            }
        }

        DiameterDnsQueryResult missing =
            new DiameterDnsQueryResult();
        missing.QueryName = name;
        missing.ResponseCode = 3;
        missing.Code = "harness-dns-not-found";
        return missing;
    }

    internal DiameterDnsQueryResult CloneDiameterDnsResult(
        DiameterDnsQueryResult source)
    {
        DiameterDnsQueryResult result =
            new DiameterDnsQueryResult();
        result.QueryName = source.QueryName;
        result.RecordType = source.RecordType;
        result.ResponseCode = source.ResponseCode;
        result.Successful = source.Successful;
        result.Authoritative = source.Authoritative;
        result.RecursionAvailable = source.RecursionAvailable;
        result.UsedTcpFallback = source.UsedTcpFallback;
        result.NegativeTimeToLiveSeconds =
            source.NegativeTimeToLiveSeconds;
        result.Code = source.Code;
        result.Message = source.Message;
        int index = 0;
        while (index < source.NaptrRecords.Count)
        {
            DiameterNaptrRecord record = new DiameterNaptrRecord();
            record.Name = source.NaptrRecords[index].Name;
            record.Order = source.NaptrRecords[index].Order;
            record.Preference = source.NaptrRecords[index].Preference;
            record.Flags = source.NaptrRecords[index].Flags;
            record.Service = source.NaptrRecords[index].Service;
            record.RegularExpression =
                source.NaptrRecords[index].RegularExpression;
            record.Replacement = source.NaptrRecords[index].Replacement;
            record.TimeToLiveSeconds =
                source.NaptrRecords[index].TimeToLiveSeconds;
            record.ExpiresAt = source.NaptrRecords[index].ExpiresAt;
            result.NaptrRecords.Add(record);
            index = index + 1;
        }

        index = 0;
        while (index < source.SrvRecords.Count)
        {
            DiameterSrvRecord record = new DiameterSrvRecord();
            record.Name = source.SrvRecords[index].Name;
            record.Priority = source.SrvRecords[index].Priority;
            record.Weight = source.SrvRecords[index].Weight;
            record.Port = source.SrvRecords[index].Port;
            record.Target = source.SrvRecords[index].Target;
            record.TimeToLiveSeconds =
                source.SrvRecords[index].TimeToLiveSeconds;
            record.ExpiresAt = source.SrvRecords[index].ExpiresAt;
            result.SrvRecords.Add(record);
            index = index + 1;
        }

        index = 0;
        while (index < source.AddressRecords.Count)
        {
            DiameterAddressRecord record = new DiameterAddressRecord();
            record.Name = source.AddressRecords[index].Name;
            record.Address = source.AddressRecords[index].Address;
            record.TimeToLiveSeconds =
                source.AddressRecords[index].TimeToLiveSeconds;
            record.ExpiresAt = source.AddressRecords[index].ExpiresAt;
            result.AddressRecords.Add(record);
            index = index + 1;
        }

        return result;
    }
}
