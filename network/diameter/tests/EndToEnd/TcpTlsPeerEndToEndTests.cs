using System.Net;
using System.Net.Sockets;
using System.Text;

namespace diameter.Tests;

public class TcpTlsPeerEndToEndTests
{
    [Test]
    public void PeerLifecycleAndApplicationTrafficRoundTripOverTcp()
    {
        RunPeerLifecycleAndApplicationTraffic(false, null);
    }

    internal static void RunPeerLifecycleAndApplicationTraffic(
        bool useTls,
        TlsTestCertificates? certificates)
    {
        TcpTlsPeerEndToEndContext? context = null;
        try
        {
            context = new TcpTlsPeerEndToEndContext(useTls, certificates);
            PeerCapabilitiesExchangeResult capabilities =
                context.ExchangeCapabilities();
            PeerWatchdogResult watchdog = context.ExchangeWatchdog();
            DiameterAnswerResult application =
                context.ExchangeApplicationRequest();
            PeerWatchdogResult failover =
                context.BeginMissedWatchdogFailover();
            PeerWatchdogResult failback =
                context.CompleteLateWatchdogFailback();
            PeerCapabilitiesUpdateResult update =
                context.ExchangeCapabilitiesUpdate();
            PeerDisconnectResult disconnected =
                context.ExchangeGracefulDisconnect();
            PeerWatchdogResult reconnected = context.ReconnectPeers();

            DiameterConnectionMetadata clientMetadata =
                context.ClientConnection
                    .ReadDiameterConnectionMetadata();
            DiameterPeerSnapshot clientPeer = context.Client.Manager.Table
                .ReadPeerTableSnapshot("server.example")!;
            DiameterPeerSnapshot serverPeer = context.Server.Manager.Table
                .ReadPeerTableSnapshot("client.example")!;

            Assert.Multiple(() =>
            {
                Assert.That(capabilities.Accepted, Is.True);
                Assert.That(capabilities.ResultCode,
                    Is.EqualTo(DiameterResultCodes.Success));
                Assert.That(clientPeer.Capabilities.ProductName,
                    Is.EqualTo("server-node"));
                Assert.That(serverPeer.Capabilities.ProductName,
                    Is.EqualTo("client-node-updated"));
                Assert.That(watchdog.Code,
                    Is.EqualTo("watchdog-answer-accepted"));
                Assert.That(watchdog.Peer.WatchdogState,
                    Is.EqualTo(DiameterPeerWatchdogState.Okay));
                Assert.That(application.Correlated, Is.True);
                Assert.That(application.Successful, Is.True);
                Assert.That(application.ResultCode,
                    Is.EqualTo(DiameterResultCodes.Success));
                Assert.That(failover.FailoverRequired, Is.True);
                Assert.That(failover.Peer.WatchdogState,
                    Is.EqualTo(DiameterPeerWatchdogState.Suspect));
                Assert.That(failback.FailbackReady, Is.True);
                Assert.That(failback.Peer.WatchdogState,
                    Is.EqualTo(DiameterPeerWatchdogState.Okay));
                Assert.That(update.Accepted, Is.True);
                Assert.That(update.Code,
                    Is.EqualTo("capabilities-update-answer-accepted"));
                Assert.That(disconnected.Code,
                    Is.EqualTo("disconnect-answer-accepted"));
                Assert.That(disconnected.Peer.State,
                    Is.EqualTo(DiameterPeerState.Closed));
                Assert.That(reconnected.Code,
                    Is.EqualTo("reopen-peer-stabilized"));
                Assert.That(reconnected.FailbackReady, Is.True);
                Assert.That(reconnected.Peer.Available, Is.True);
                Assert.That(context.ConnectionCount, Is.EqualTo(2));
                Assert.That(clientMetadata.Security.Mode,
                    Is.EqualTo(useTls ?
                        DiameterSecurityMode.Tls :
                        DiameterSecurityMode.None));
                Assert.That(clientMetadata.Security.Encrypted,
                    Is.EqualTo(useTls));
                Assert.That(clientMetadata.Security.MutuallyAuthenticated,
                    Is.EqualTo(useTls));
            });
        }
        finally
        {
            if (context != null)
            {
                context.Close();
            }
        }
    }
}

internal class TcpTlsPeerEndToEndContext
{
    internal bool UseTls;
    internal int Port;
    internal IDiameterTransportFactory Factory;
    internal IDiameterListener Listener;
    internal DiameterTransportConfiguration ClientTransport;
    internal DiameterTransportConfiguration ServerTransport;
    internal DiameterSecurityConfiguration ClientSecurity;
    internal DiameterSecurityConfiguration ServerSecurity;
    internal InProcessHarnessClock Clock = new InProcessHarnessClock();
    internal DiameterMessageCodec Codec = new DiameterMessageCodec();
    internal TcpTlsEndToEndPeer Client;
    internal TcpTlsEndToEndPeer Server;
    internal QueuedDiameterConnection ClientConnection = null!;
    internal QueuedDiameterConnection ServerConnection = null!;
    internal DiameterMessage? HeldWatchdogAnswer;
    internal int ConnectionCount;

    internal TcpTlsPeerEndToEndContext(
        bool useTls,
        TlsTestCertificates? certificates)
    {
        UseTls = useTls;
        Port = ReservePort();
        Factory = useTls ?
            new DiameterTlsTransportFactory() :
            new DiameterTcpTransportFactory();
        ServerTransport = CreateTransport(Port, true);
        ClientTransport = CreateTransport(Port, false);
        if (useTls)
        {
            ServerSecurity = CreateSecurity(
                true,
                certificates!.ServerPath,
                certificates.RootPath,
                "client.example");
            ClientSecurity = CreateSecurity(
                true,
                certificates.ClientPath,
                certificates.RootPath,
                "server.example");
        }
        else
        {
            ServerSecurity = CreateSecurity(
                false,
                string.Empty,
                string.Empty,
                string.Empty);
            ClientSecurity = CreateSecurity(
                false,
                string.Empty,
                string.Empty,
                string.Empty);
        }
        DiameterListenerResult created = Factory.CreateDiameterListener(
            ServerTransport,
            ServerSecurity);
        IgnoreUnavailableMacOsKeychain(created);
        Assert.That(created.Status,
            Is.EqualTo(DiameterTransportStatus.Success),
            created.Code + ": " + created.Message);
        Listener = created.Listener!;
        Client = new TcpTlsEndToEndPeer(
            "client.example",
            "server.example",
            Port,
            useTls,
            ClientTransport,
            ClientSecurity,
            Clock,
            "client-node");
        Server = new TcpTlsEndToEndPeer(
            "server.example",
            "client.example",
            Port,
            useTls,
            ServerTransport,
            ServerSecurity,
            Clock,
            "server-node");
        ConnectTransportPair();
    }

    internal PeerCapabilitiesExchangeResult ExchangeCapabilities()
    {
        DiameterMessage request = Client.Manager.CapabilitiesExchange
            .CreateCapabilitiesExchangeRequest(Client.Capabilities);
        SendMessage(ClientConnection, request);
        DiameterMessage receivedRequest = ReceiveMessage(ServerConnection);
        PeerCapabilitiesExchangeResult parsedRequest = Server.Manager
            .CapabilitiesExchange.ParseCapabilitiesExchangeRequest(
                receivedRequest);
        parsedRequest.Capabilities.CommonApplications = Server.Manager
            .CapabilitiesExchange.ReadCommonPeerApplications(
                Server.Capabilities,
                parsedRequest.Capabilities);
        DiameterMessage answer = Server.Manager.CapabilitiesExchange
            .CreateCapabilitiesExchangeAnswer(
                receivedRequest,
                Server.Capabilities);
        SendMessage(ServerConnection, answer);
        DiameterMessage receivedAnswer = ReceiveMessage(ClientConnection);
        PeerCapabilitiesExchangeResult processed = Client.Manager
            .CapabilitiesExchange.ProcessCapabilitiesExchangeAnswer(
                receivedAnswer,
                request,
                Client.Capabilities);
        Server.Manager.RecordPeerManagerCapabilities(
            "client.example",
            parsedRequest.Capabilities);
        Client.Manager.RecordPeerManagerCapabilities(
            "server.example",
            processed.Capabilities);
        Client.Manager.Table.TransitionPeerTableState(
            "server.example",
            DiameterPeerState.InitiatorOpen);
        Server.Manager.Table.TransitionPeerTableState(
            "client.example",
            DiameterPeerState.ResponderOpen);
        Client.Manager.WatchdogController
            .HandlePeerWatchdogConnectionOpened("server.example");
        Server.Manager.WatchdogController
            .HandlePeerWatchdogConnectionOpened("client.example");
        return processed;
    }

    internal PeerWatchdogResult ExchangeWatchdog()
    {
        Clock.AdvanceDiameterTime(6000);
        PeerWatchdogResult request = Client.Manager.WatchdogController
            .HandlePeerWatchdogTimer("server.example");
        SendMessage(ClientConnection, request.Message!);
        DiameterMessage received = ReceiveMessage(ServerConnection);
        PeerWatchdogResult answer = Server.Manager.WatchdogController
            .HandlePeerWatchdogRequest("client.example", received);
        SendMessage(ServerConnection, answer.Message!);
        return Client.Manager.WatchdogController.HandlePeerWatchdogAnswer(
            "server.example",
            ReceiveMessage(ClientConnection));
    }

    internal DiameterAnswerResult ExchangeApplicationRequest()
    {
        RecordingRequestRouter router = new RecordingRequestRouter();
        DiameterRequestClient requestClient = new DiameterRequestClient(
            Client.Configuration,
            router,
            Client.Manager,
            Codec,
            Client.Identifiers,
            Clock);
        DiameterSendRequest request = CreateApplicationRequest();
        DiameterSendResult sent = requestClient.SendDiameterRequest(
            request,
            null);
        Assert.That(sent.Accepted, Is.True, sent.Code + ": " + sent.Message);
        DiameterMessage received = ReceiveMessage(ServerConnection);
        DiameterMessage answer = CreateApplicationAnswer(received);
        DiameterEncodeResult encoded = Codec.EncodeMessageToArray(answer);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        DiameterTransportIoResult sentAnswer = Server.Manager
            .SendDiameterPeerBytes(
                CreateIdentity("client.example"),
                encoded.Data,
                3000);
        Assert.That(sentAnswer.Status,
            Is.EqualTo(DiameterTransportStatus.Success));
        return requestClient.HandleReceivedDiameterAnswer(
            CreateIdentity("server.example"),
            ReceiveMessage(ClientConnection));
    }

    internal PeerWatchdogResult BeginMissedWatchdogFailover()
    {
        Clock.AdvanceDiameterTime(6000);
        PeerWatchdogResult request = Client.Manager.WatchdogController
            .HandlePeerWatchdogTimer("server.example");
        SendMessage(ClientConnection, request.Message!);
        PeerWatchdogResult answer = Server.Manager.WatchdogController
            .HandlePeerWatchdogRequest(
                "client.example",
                ReceiveMessage(ServerConnection));
        HeldWatchdogAnswer = answer.Message;
        Clock.AdvanceDiameterTime(6000);
        return Client.Manager.WatchdogController.HandlePeerWatchdogTimer(
            "server.example");
    }

    internal PeerWatchdogResult CompleteLateWatchdogFailback()
    {
        SendMessage(ServerConnection, HeldWatchdogAnswer!);
        HeldWatchdogAnswer = null;
        return Client.Manager.WatchdogController.HandlePeerWatchdogAnswer(
            "server.example",
            ReceiveMessage(ClientConnection));
    }

    internal PeerCapabilitiesUpdateResult ExchangeCapabilitiesUpdate()
    {
        DiameterPeerCapabilities updated = CloneCapabilities(
            Client.Capabilities);
        updated.ProductName = "client-node-updated";
        updated.FirmwareRevision = 2;
        PeerCapabilitiesUpdateResult request = Client.Manager
            .CapabilitiesUpdateController.BeginPeerCapabilitiesUpdate(
                "server.example",
                updated);
        SendMessage(ClientConnection, request.Message!);
        PeerCapabilitiesUpdateResult answer = Server.Manager
            .CapabilitiesUpdateController.HandlePeerCapabilitiesUpdateRequest(
                "client.example",
                ReceiveMessage(ServerConnection),
                Server.Capabilities);
        SendMessage(ServerConnection, answer.Message!);
        PeerCapabilitiesUpdateResult result = Client.Manager
            .CapabilitiesUpdateController.ProcessPeerCapabilitiesUpdateAnswer(
                "server.example",
                ReceiveMessage(ClientConnection));
        Client.Capabilities = updated;
        return result;
    }

    internal PeerDisconnectResult ExchangeGracefulDisconnect()
    {
        DiameterDisconnectRequest request = new DiameterDisconnectRequest();
        request.PeerIdentity.Value = "server.example";
        request.Cause = DiameterDisconnectCause.Rebooting;
        request.WaitForAnswer = true;
        request.SuppressReconnect = false;
        request.TimeoutMilliseconds = 5000;
        PeerDisconnectResult started = Client.Manager.DisconnectController
            .BeginPeerDisconnect(request);
        Client.Manager.DisconnectController.CompletePeerDisconnectDrain(
            "server.example");
        SendMessage(ClientConnection, started.Message!);
        PeerDisconnectResult answered = Server.Manager.DisconnectController
            .HandlePeerDisconnectRequest(
                "client.example",
                ReceiveMessage(ServerConnection));
        Server.Manager.DisconnectController.CompletePeerDisconnectDrain(
            "client.example");
        SendMessage(ServerConnection, answered.Message!);
        PeerDisconnectResult result = Client.Manager.DisconnectController
            .HandlePeerDisconnectAnswer(
                "server.example",
                ReceiveMessage(ClientConnection));
        ClientConnection.CloseDiameterConnection();
        ServerConnection.CloseDiameterConnection();
        Server.Manager.DisconnectController
            .HandlePeerDisconnectTransportClosed("client.example");
        Client.Manager.DetachPeerManagerConnection(
            CreateIdentity("server.example"));
        Server.Manager.DetachPeerManagerConnection(
            CreateIdentity("client.example"));
        return result;
    }

    internal PeerWatchdogResult ReconnectPeers()
    {
        ConnectTransportPair();
        Client.Manager.Table.TransitionPeerTableState(
            "server.example",
            DiameterPeerState.InitiatorOpen);
        Server.Manager.Table.TransitionPeerTableState(
            "client.example",
            DiameterPeerState.ResponderOpen);
        PeerWatchdogResult request = Client.Manager.WatchdogController
            .HandlePeerWatchdogConnectionOpened("server.example");
        PeerWatchdogResult result = new PeerWatchdogResult();
        int successCount = 0;
        while (successCount < 3)
        {
            SendMessage(ClientConnection, request.Message!);
            DiameterMessage received = ReceiveMessage(ServerConnection);
            DiameterBaseCommandOptions options = new DiameterBaseCommandOptions();
            options.OriginHost.Value = "server.example";
            options.OriginRealm.Value = "example";
            options.Request = received;
            options.ResultCode = DiameterResultCodes.Success;
            DiameterMessage answer = Server.Manager.WatchdogController
                .CommandFactory.CreateDeviceWatchdogAnswer(options);
            SendMessage(ServerConnection, answer);
            result = Client.Manager.WatchdogController
                .HandlePeerWatchdogAnswer(
                    "server.example",
                    ReceiveMessage(ClientConnection));
            successCount = successCount + 1;
            if (successCount < 3)
            {
                Clock.AdvanceDiameterTime(6000);
                request = Client.Manager.WatchdogController
                    .HandlePeerWatchdogTimer("server.example");
            }
        }

        return result;
    }

    internal void ConnectTransportPair()
    {
        DiameterConnectionResult connected = Factory.ConnectDiameterEndpoint(
            CreateEndpoint("server.example", Port, UseTls),
            ClientTransport,
            ClientSecurity,
            5000);
        if (OperatingSystem.IsMacOS() &&
            connected.Status == DiameterTransportStatus.InvalidConfiguration &&
            connected.Message.Contains(
                "keychain",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore(
                "The test sandbox does not expose a writable macOS Keychain.");
        }

        DiameterConnectionResult accepted = Listener
            .AcceptDiameterConnection(5000);
        Assert.That(connected.Status,
            Is.EqualTo(DiameterTransportStatus.Success),
            connected.Code + ": " + connected.Message);
        Assert.That(accepted.Status,
            Is.EqualTo(DiameterTransportStatus.Success),
            accepted.Code + ": " + accepted.Message);
        ClientConnection = new QueuedDiameterConnection(
            connected.Connection!, 16, 16, 65536, 65536);
        ServerConnection = new QueuedDiameterConnection(
            accepted.Connection!, 16, 16, 65536, 65536);
        Client.Manager.Table.AttachPeerTableConnection(
            "server.example",
            Client.PeerConfiguration.Endpoints[0],
            ClientConnection.ReadDiameterConnectionMetadata(),
            DiameterConnectionDirection.Outbound);
        Server.Manager.Table.AttachPeerTableConnection(
            "client.example",
            Server.PeerConfiguration.Endpoints[0],
            ServerConnection.ReadDiameterConnectionMetadata(),
            DiameterConnectionDirection.Inbound);
        Client.Manager.AttachPeerManagerConnection(
            CreateIdentity("server.example"),
            ClientConnection);
        Server.Manager.AttachPeerManagerConnection(
            CreateIdentity("client.example"),
            ServerConnection);
        Client.Manager.Table.TransitionPeerTableState(
            "server.example",
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer);
        ConnectionCount = ConnectionCount + 1;
    }

    internal void SendMessage(
        IDiameterConnection connection,
        DiameterMessage message)
    {
        DiameterEncodeResult encoded = Codec.EncodeMessageToArray(message);
        Assert.That(encoded.Status,
            Is.EqualTo(DiameterCodecStatus.Success));
        DiameterTransportIoResult sent = connection
            .SendDiameterConnectionBytes(encoded.Data, 3000);
        Assert.That(sent.Status,
            Is.EqualTo(DiameterTransportStatus.Success),
            sent.Code + ": " + sent.Message);
        Assert.That(sent.TransferredCount, Is.EqualTo(encoded.Data.Length));
    }

    internal DiameterMessage ReceiveMessage(IDiameterConnection connection)
    {
        byte[] buffer = new byte[65536];
        DiameterTransportIoResult received = connection
            .ReceiveDiameterConnectionBytes(buffer, 5000);
        Assert.That(received.Status,
            Is.EqualTo(DiameterTransportStatus.Success),
            received.Code + ": " + received.Message);
        DiameterDecodeResult decoded = Codec.DecodeMessage(
            buffer.AsSpan(0, received.TransferredCount),
            false,
            new DiameterLimitConfiguration());
        Assert.That(decoded.Status,
            Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Message!;
    }

    internal void Close()
    {
        if (ClientConnection != null)
        {
            ClientConnection.CloseDiameterConnection();
        }

        if (ServerConnection != null)
        {
            ServerConnection.CloseDiameterConnection();
        }

        if (Listener != null)
        {
            Listener.CloseDiameterListener();
        }
    }

    internal static DiameterSendRequest CreateApplicationRequest()
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = 300;
        result.Request.ApplicationId = 4;
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            "example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationHost,
            "server.example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginHost,
            "client.example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            "client.example;end-to-end"));
        result.PreferredPeer = CreateIdentity("server.example");
        result.TimeoutMilliseconds = 5000;
        return result;
    }

    internal static DiameterMessage CreateApplicationAnswer(
        DiameterMessage request)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        DiameterAvp resultCode = new DiameterAvp();
        resultCode.Code = DiameterAvpCodes.ResultCode;
        resultCode.Flags = DiameterAvpFlags.Mandatory;
        resultCode.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            resultCode.Data,
            DiameterResultCodes.Success);
        result.Avps.Add(resultCode);
        return result;
    }

    internal static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    internal static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    internal static DiameterPeerCapabilities CloneCapabilities(
        DiameterPeerCapabilities source)
    {
        return PeerTableCloner.ClonePeerTableCapabilities(source);
    }

    internal static DiameterEndpoint CreateEndpoint(
        string identity,
        int port,
        bool tls)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = identity;
        result.Host = identity;
        result.Address = IPAddress.Loopback;
        result.Port = port;
        result.Transport = DiameterTransportKind.Tcp;
        result.Security = tls ?
            DiameterSecurityMode.Tls :
            DiameterSecurityMode.None;
        return result;
    }

    internal static DiameterTransportConfiguration CreateTransport(
        int port,
        bool listener)
    {
        DiameterTransportConfiguration result =
            new DiameterTransportConfiguration();
        result.Host = "127.0.0.1";
        result.Port = port;
        result.EnableIpv4 = true;
        result.EnableIpv6 = false;
        result.EnableListener = listener;
        result.EnableActiveConnections = !listener;
        result.ListenBacklog = 4;
        return result;
    }

    internal static DiameterSecurityConfiguration CreateSecurity(
        bool tls,
        string certificatePath,
        string trustAnchorPath,
        string expectedPeerName)
    {
        DiameterSecurityConfiguration result =
            new DiameterSecurityConfiguration();
        if (tls)
        {
            result.Mode = DiameterSecurityMode.Tls;
            result.LocalCertificatePath = certificatePath;
            result.TrustAnchorPath = trustAnchorPath;
            result.ExpectedPeerName = expectedPeerName;
            result.RequirePeerCertificate = true;
        }
        else
        {
            result.AllowUnsecuredTransport = true;
        }

        return result;
    }

    internal static void IgnoreUnavailableMacOsKeychain(
        DiameterListenerResult result)
    {
        if (OperatingSystem.IsMacOS() &&
            result.Status == DiameterTransportStatus.InvalidConfiguration &&
            result.Message.Contains(
                "keychain",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore(
                "The test sandbox does not expose a writable macOS Keychain.");
        }
    }

    internal static int ReservePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int result = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return result;
    }
}

internal class TcpTlsEndToEndPeer
{
    internal DiameterNodeConfiguration Configuration;
    internal TcpTlsEndToEndIdentifierSource Identifiers;
    internal DiameterPeerManager Manager;
    internal DiameterPeerConfiguration PeerConfiguration;
    internal DiameterPeerCapabilities Capabilities;

    internal TcpTlsEndToEndPeer(
        string identity,
        string peerIdentity,
        int port,
        bool tls,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        InProcessHarnessClock clock,
        string productName)
    {
        Configuration = new DiameterNodeConfiguration();
        Configuration.OriginHost.Value = identity;
        Configuration.OriginRealm.Value = "example";
        Configuration.ProductName = productName;
        Configuration.Roles =
            DiameterNodeRole.Client | DiameterNodeRole.Server;
        Configuration.Transport = ConfigurationValidator
            .CloneDiameterTransportConfiguration(transport);
        Configuration.Transport.EnableListener = true;
        Configuration.Transport.EnableActiveConnections = true;
        Configuration.Security = ConfigurationValidator
            .CloneDiameterSecurityConfiguration(security);
        Configuration.Timers.ConnectionRetryMilliseconds = 1000;
        Configuration.Timers.ConnectionRetryJitterPercent = 0;
        Configuration.Timers.WatchdogIntervalMilliseconds = 6000;
        Configuration.Timers.WatchdogJitterMilliseconds = 0;
        Identifiers = new TcpTlsEndToEndIdentifierSource(identity);
        Manager = new DiameterPeerManager(
            Configuration,
            clock,
            Identifiers);
        PeerConfiguration = new DiameterPeerConfiguration();
        PeerConfiguration.Identity.Value = peerIdentity;
        PeerConfiguration.Realm.Value = "example";
        PeerConfiguration.Endpoints.Add(
            TcpTlsPeerEndToEndContext.CreateEndpoint(
                peerIdentity,
                port,
                tls));
        DiameterApplicationId required = new DiameterApplicationId();
        required.Id = 4;
        PeerConfiguration.RequiredApplications.Add(required);
        Manager.AddDiameterPeer(PeerConfiguration);
        Capabilities = new DiameterPeerCapabilities();
        Capabilities.OriginHost.Value = identity;
        Capabilities.OriginRealm.Value = "example";
        Capabilities.HostAddresses.Add(IPAddress.Loopback);
        Capabilities.ProductName = productName;
        Capabilities.VendorId = 10415;
        Capabilities.FirmwareRevision = 1;
        Capabilities.SupportedVendorIds.Add(10415);
        Capabilities.AuthApplicationIds.Add(
            DiameterApplicationIds.CapabilitiesUpdate);
        Capabilities.AuthApplicationIds.Add(4);
        Capabilities.AuthApplicationIds.Add(
            DiameterApplicationIds.CreditControl);
        Capabilities.AcctApplicationIds.Add(
            DiameterApplicationIds.BaseAccounting);
    }
}

internal class TcpTlsEndToEndIdentifierSource : IDiameterIdentifierSource
{
    internal string OriginHost;
    internal uint HopByHopIdentifier = 1000;
    internal uint EndToEndIdentifier = 2000;
    internal uint SessionIdentifier;

    internal TcpTlsEndToEndIdentifierSource(string originHost)
    {
        OriginHost = originHost;
    }

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        EndToEndIdentifier = EndToEndIdentifier + 1;
        return EndToEndIdentifier;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        SessionIdentifier = SessionIdentifier + 1;
        return OriginHost + ";end-to-end;" + SessionIdentifier;
    }

    public uint CreateOriginStateIdentifier()
    {
        return 1;
    }
}
