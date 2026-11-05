using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace diameter.Tests;

public class TcpTransportTests
{
    [Test]
    public void TcpFactoryReportsCapabilitiesAndConfigurationFailures()
    {
        IDiameterTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterTransportConfiguration transport = CreateTransport(false);
        DiameterSecurityConfiguration unsecured = CreateUnsecuredSecurity();
        DiameterTransportSupportResult supported = factory.ProbeDiameterTransportSupport(
            transport,
            unsecured);
        DiameterListenerResult listener = factory.CreateDiameterListener(
            transport,
            unsecured);

        DiameterTransportConfiguration sctp = CreateTransport(false);
        sctp.Kind = DiameterTransportKind.Sctp;
        DiameterTransportSupportResult wrongTransport =
            factory.ProbeDiameterTransportSupport(sctp, unsecured);
        DiameterSecurityConfiguration tls = new DiameterSecurityConfiguration();
        tls.Mode = DiameterSecurityMode.Tls;
        DiameterTransportSupportResult wrongSecurity =
            factory.ProbeDiameterTransportSupport(transport, tls);
        DiameterSecurityConfiguration implicitInsecure =
            new DiameterSecurityConfiguration();
        DiameterTransportSupportResult insecureNotApproved =
            factory.ProbeDiameterTransportSupport(transport, implicitInsecure);
        DiameterConnectionResult invalidTimeout = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, 3868),
            transport,
            unsecured,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(supported.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(supported.Metadata.Kind, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(supported.Metadata.SupportsActiveConnections, Is.True);
            Assert.That(supported.Metadata.SupportsListeners, Is.True);
            Assert.That(supported.Metadata.MessageOriented, Is.False);
            Assert.That(listener.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(listener.Listener, Is.Null);
            Assert.That(wrongTransport.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(wrongSecurity.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(insecureNotApproved.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(invalidTimeout.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        });
    }

    [Test]
    public void TcpProbeAndConnectionRejectEveryConfigurationBoundary()
    {
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterTransportConfiguration valid = CreateTransport(false);
        DiameterSecurityConfiguration unsecured = CreateUnsecuredSecurity();
        Assert.That(factory.ProbeDiameterTransportSupport(null, unsecured).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        Assert.That(factory.ProbeDiameterTransportSupport(valid, null).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));

        DiameterSecurityConfiguration ipsec = new DiameterSecurityConfiguration();
        ipsec.Mode = DiameterSecurityMode.Ipsec;
        ipsec.AllowUnsecuredTransport = true;
        Assert.That(factory.ProbeDiameterTransportSupport(valid, ipsec).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));

        Action<DiameterSecurityConfiguration>[] certificateSettings =
            new Action<DiameterSecurityConfiguration>[]
        {
            value => value.LocalCertificatePath = "certificate.pfx",
            value => value.LocalCertificatePassword = "password",
            value => value.TrustAnchorPath = "root.pem",
            value => value.ExpectedPeerName = "peer.example",
            value => value.RequirePeerCertificate = true,
            value => value.CheckCertificateRevocation = true
        };
        int securityIndex = 0;
        while (securityIndex < certificateSettings.Length)
        {
            DiameterSecurityConfiguration changed = CreateUnsecuredSecurity();
            certificateSettings[securityIndex](changed);
            Assert.That(factory.ProbeDiameterTransportSupport(valid, changed).Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            securityIndex = securityIndex + 1;
        }

        Action<DiameterTransportConfiguration>[] invalidTransport =
            new Action<DiameterTransportConfiguration>[]
        {
            value => value.Port = 0,
            value => value.Port = 65536,
            value => { value.EnableIpv4 = false; value.EnableIpv6 = false; },
            value => value.ReceiveBufferSize = 1,
            value => value.SendBufferSize = 1,
            value => value.SctpStreamCount = 2
        };
        int transportIndex = 0;
        while (transportIndex < invalidTransport.Length)
        {
            DiameterTransportConfiguration changed = CreateTransport(false);
            invalidTransport[transportIndex](changed);
            Assert.That(factory.ProbeDiameterTransportSupport(
                changed, unsecured).Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            transportIndex = transportIndex + 1;
        }

        DiameterTransportConfiguration active = CreateTransport(false);
        active.EnableActiveConnections = true;
        DiameterEndpoint endpoint = CreateEndpoint(IPAddress.Loopback, 3868);
        Assert.That(factory.ConnectDiameterEndpoint(
            null, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        DiameterEndpoint disabledEndpoint = CreateEndpoint(IPAddress.Loopback, 3868);
        DiameterTransportConfiguration disabled = CreateTransport(false);
        disabled.EnableActiveConnections = false;
        Assert.That(factory.ConnectDiameterEndpoint(
            disabledEndpoint, disabled, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 0;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 65536;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 3868;
        endpoint.Transport = DiameterTransportKind.Sctp;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
    }

    [Test]
    public void TcpAddressHelpersCoverResolutionFilteringAndDefensiveSocketGuards()
    {
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterTransportConfiguration transport = CreateTransport(false);
        DiameterSecurityConfiguration security = CreateUnsecuredSecurity();
        Assert.That(factory.CreateDiameterListener(null, security).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        DiameterTransportConfiguration wrong = CreateTransport(false);
        wrong.Kind = DiameterTransportKind.Sctp;
        Assert.That(factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, 3868),
            wrong,
            security,
            1).Status, Is.EqualTo(DiameterTransportStatus.Unsupported));

        DiameterTransportConfiguration named = CreateTransport(false);
        named.Host = "127.0.0.1";
        TcpAddressResolution namedListener =
            factory.ResolveTcpListenerAddresses(
                named,
                Environment.TickCount64 + 1000);
        Assert.That(namedListener.Addresses, Has.Count.EqualTo(1));

        DiameterTransportConfiguration dual = CreateTransport(false);
        dual.EnableIpv6 = true;
        TcpAddressResolution wildcard = factory.ResolveTcpListenerAddresses(
            dual,
            Environment.TickCount64 + 1000);
        Assert.That(wildcard.Addresses, Is.Not.Empty);

        DiameterTransportConfiguration ipv6Only = CreateTransport(true);
        TcpAddressResolution filtered = factory.ResolveTcpAddresses(
            IPAddress.Loopback,
            "",
            ipv6Only,
            Environment.TickCount64 + 1000);
        Assert.That(filtered.Status,
            Is.EqualTo(DiameterTransportStatus.Unsupported));
        TcpAddressResolution timedOut = factory.ResolveTcpAddresses(
            null,
            "localhost",
            transport,
            Environment.TickCount64 - 1);
        Assert.That(timedOut.Status,
            Is.EqualTo(DiameterTransportStatus.TimedOut));
        List<IPAddress> addresses = new List<IPAddress>();
        addresses.Add(IPAddress.Loopback);
        Assert.That(factory.FindTcpAddressFamily(
            addresses,
            AddressFamily.InterNetwork), Is.EqualTo(IPAddress.Loopback));
        Assert.That(factory.FindTcpAddressFamily(
            addresses,
            AddressFamily.InterNetworkV6), Is.Null);

        Socket unbound = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        Assert.Throws<DiameterTransportException>(() =>
            factory.CreateTcpConnectionMetadata(
                unbound,
                CreateEndpoint(IPAddress.Loopback, 3868),
                IPAddress.Loopback,
                security));
        Assert.Throws<DiameterTransportException>(() =>
            factory.CreateTcpListenerMetadata(unbound, transport, security));
        unbound.Close();

        Assert.That(factory.ReadTaskFailureMessage(
            Task.CompletedTask,
            "fallback"), Is.EqualTo("fallback"));
        Assert.That(factory.ReadTaskFailureMessage(
            Task.FromException(new InvalidOperationException("failure")),
            "fallback"), Is.EqualTo("failure"));
    }

    [Test]
    public void ActiveIpv4ConnectionResolvesHostAndTransfersPartialWritesExactly()
    {
        TcpListener server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        int port = ((IPEndPoint)server.LocalEndpoint).Port;
        Task<Socket> accepting = server.AcceptSocketAsync();
        DiameterTransportConfiguration transport = CreateTransport(false);
        transport.Host = "127.0.0.1";
        transport.SendBufferSize = 512;
        transport.ReceiveBufferSize = 512;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "localhost";
        endpoint.Host = "localhost";
        endpoint.Address = null;
        endpoint.Port = port;
        IDiameterTransportFactory factory = new DiameterTcpTransportFactory();

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            CreateUnsecuredSecurity(),
            5000);
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success),
            connected.Code + ": " + connected.Message);
        Assert.That(accepting.Wait(2000), Is.True);
        Socket peer = accepting.Result;
        IDiameterConnection connection = connected.Connection!;
        TcpSocketDiameterConnection tcp = (TcpSocketDiameterConnection)connection;
        byte[] outbound = CreatePayload(1048576);
        Task<byte[]> receiving = Task.Run(() => ReadSocketBytes(peer, outbound.Length));

        DiameterTransportIoResult sent = connection.SendDiameterConnectionBytes(
            outbound,
            5000);
        Assert.That(receiving.Wait(5000), Is.True);
        bool noDelay = tcp.OwnedSocket.NoDelay;
        int keepAlive = Convert.ToInt32(tcp.OwnedSocket.GetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive));
        byte[] inbound = new byte[] { 9, 8, 7, 6, 5 };
        WriteSocketBytes(peer, inbound);
        byte[] destination = new byte[inbound.Length];
        DiameterTransportIoResult received = connection.ReceiveDiameterConnectionBytes(
            destination,
            5000);
        peer.Shutdown(SocketShutdown.Send);
        DiameterTransportIoResult end = connection.ReceiveDiameterConnectionBytes(
            new byte[8],
            5000);
        DiameterTransportOperationResult close = connection.CloseDiameterConnection();
        DiameterTransportOperationResult repeatedClose = connection.CloseDiameterConnection();
        DiameterTransportIoResult emptyAfterClose = connection.SendDiameterConnectionBytes(
            Array.Empty<byte>(),
            1000);
        peer.Close();
        server.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(outbound.Length));
            Assert.That(receiving.Result, Is.EqualTo(outbound));
            Assert.That(tcp.SendOperationCount, Is.GreaterThan(1));
            Assert.That(noDelay, Is.True);
            Assert.That(keepAlive, Is.Not.Zero);
            Assert.That(received.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(received.TransferredCount, Is.EqualTo(inbound.Length));
            Assert.That(destination, Is.EqualTo(inbound));
            Assert.That(end.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(end.EndOfStream, Is.True);
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(repeatedClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(emptyAfterClose.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(connected.Metadata.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(connected.Metadata.LocalEndpoint.Address,
                Is.EqualTo(IPAddress.Loopback));
            Assert.That(connected.Metadata.RemoteEndpoint.Address,
                Is.EqualTo(IPAddress.Loopback));
        });
    }

    [Test]
    public void ActiveIpv6ConnectionTransfersBytesWhenIpv6IsAvailable()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not supported on this host.");
        }

        TcpListener server = new TcpListener(IPAddress.IPv6Loopback, 0);
        server.Server.DualMode = false;
        server.Start();
        int port = ((IPEndPoint)server.LocalEndpoint).Port;
        Task<Socket> accepting = server.AcceptSocketAsync();
        DiameterTransportConfiguration transport = CreateTransport(true);
        transport.Host = "::1";
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.IPv6Loopback, port),
            transport,
            CreateUnsecuredSecurity(),
            5000);
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success),
            connected.Code + ": " + connected.Message);
        Assert.That(accepting.Wait(2000), Is.True);
        Socket peer = accepting.Result;
        byte[] payload = new byte[] { 1, 3, 5, 7 };
        DiameterTransportIoResult sent = connected.Connection!
            .SendDiameterConnectionBytes(payload, 2000);
        byte[] received = ReadSocketBytes(peer, payload.Length);
        DiameterTransportOperationResult close = connected.Connection!
            .CloseDiameterConnection();
        peer.Close();
        server.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(payload.Length));
            Assert.That(received, Is.EqualTo(payload));
            Assert.That(connected.Metadata.LocalEndpoint.Address!.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(connected.Metadata.RemoteEndpoint.Address,
                Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
        });
    }

    [Test]
    public void RefusedConnectionAndReceiveTimeoutReturnNormalFailureResults()
    {
        TcpListener unavailable = new TcpListener(IPAddress.Loopback, 0);
        unavailable.Start();
        int unavailablePort = ((IPEndPoint)unavailable.LocalEndpoint).Port;
        unavailable.Stop();
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterConnectionResult refused = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, unavailablePort),
            CreateTransport(false),
            CreateUnsecuredSecurity(),
            1000);

        TcpListener server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        int port = ((IPEndPoint)server.LocalEndpoint).Port;
        Task<Socket> accepting = server.AcceptSocketAsync();
        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, port),
            CreateTransport(false),
            CreateUnsecuredSecurity(),
            2000);
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success));
        Assert.That(accepting.Wait(2000), Is.True);
        Socket peer = accepting.Result;
        DiameterTransportIoResult timeout = connected.Connection!
            .ReceiveDiameterConnectionBytes(new byte[8], 30);
        WriteSocketBytes(peer, new byte[] { 7 });
        byte[] resumedBytes = new byte[1];
        DiameterTransportIoResult resumed = connected.Connection!
            .ReceiveDiameterConnectionBytes(resumedBytes, 1000);
        DiameterConnectionMetadata metadata = connected.Connection!
            .ReadDiameterConnectionMetadata();
        connected.Connection!.CloseDiameterConnection();
        peer.Close();
        server.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(refused.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(refused.Connection, Is.Null);
            Assert.That(refused.Code, Is.EqualTo("connect-failed"));
            Assert.That(timeout.Status, Is.EqualTo(DiameterTransportStatus.TimedOut));
            Assert.That(timeout.Code, Is.EqualTo("receive-timeout"));
            Assert.That(resumed.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(resumed.TransferredCount, Is.EqualTo(1));
            Assert.That(resumedBytes, Is.EqualTo(new byte[] { 7 }));
            Assert.That(metadata.Open, Is.True);
        });
    }

    private static DiameterTransportConfiguration CreateTransport(bool ipv6)
    {
        DiameterTransportConfiguration transport = new DiameterTransportConfiguration();
        transport.EnableIpv4 = !ipv6;
        transport.EnableIpv6 = ipv6;
        transport.NoDelay = true;
        transport.KeepAlive = true;
        return transport;
    }

    private static DiameterSecurityConfiguration CreateUnsecuredSecurity()
    {
        DiameterSecurityConfiguration security = new DiameterSecurityConfiguration();
        security.AllowUnsecuredTransport = true;
        return security;
    }

    private static DiameterEndpoint CreateEndpoint(IPAddress address, int port)
    {
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = address.ToString();
        endpoint.Address = address;
        endpoint.Port = port;
        return endpoint;
    }

    private static byte[] CreatePayload(int count)
    {
        byte[] data = new byte[count];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(index % 251);
            index = index + 1;
        }

        return data;
    }

    private static byte[] ReadSocketBytes(Socket socket, int count)
    {
        byte[] data = new byte[count];
        int offset = 0;
        while (offset < data.Length)
        {
            int received = socket.Receive(
                data,
                offset,
                data.Length - offset,
                SocketFlags.None);
            if (received == 0)
            {
                throw new AssertionException("The TCP peer closed before all bytes arrived.");
            }

            offset = offset + received;
        }

        return data;
    }

    private static void WriteSocketBytes(Socket socket, byte[] data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            offset = offset + socket.Send(
                data,
                offset,
                data.Length - offset,
                SocketFlags.None);
        }
    }
}
