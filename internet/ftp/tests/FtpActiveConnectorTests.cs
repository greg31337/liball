using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpActiveConnectorTests
{
    [Test]
    public void Listener_uses_control_local_address_OS_port_and_EPRT_by_default()
    {
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, CreateOptions());
        FtpActiveListener listener = connector.OpenListener();
        using Socket listenerSocket = listener.Listener!;

        FtpCommand command = connector.CreateExtendedActiveCommand(listener);
        FtpEndpoint advertised = FtpEndpointCodec.ParseExtendedPortArgument(command.Argument);

        Assert.Multiple(() =>
        {
            Assert.That(listener.Open, Is.True);
            Assert.That(listener.BindEndpoint.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(listener.BindEndpoint.Port, Is.GreaterThan(0));
            Assert.That(listener.AdvertisedEndpoint.Host,
                Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(listener.AdvertisedEndpoint.Port, Is.EqualTo(listener.BindEndpoint.Port));
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Eprt));
            Assert.That(command.Verb, Is.EqualTo("EPRT"));
            Assert.That(advertised.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(advertised.Port, Is.EqualTo(listener.BindEndpoint.Port));
            Assert.That(advertised.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
        });
    }

    [Test]
    public void Wildcard_bind_uses_configured_external_advertised_address()
    {
        FtpDataConnectionOptions options = CreateOptions();
        options.ActiveBindEndpoint = CreateEndpoint("", 0, FtpAddressFamily.IPv4);
        options.ActiveAdvertisedAddress = "198.51.100.44";
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, options);
        FtpActiveListener listener = connector.OpenListener();
        using Socket listenerSocket = listener.Listener!;

        FtpEndpoint commandEndpoint = FtpEndpointCodec.ParseExtendedPortArgument(
            connector.CreateExtendedActiveCommand(listener).Argument);

        Assert.Multiple(() =>
        {
            Assert.That(listener.BindEndpoint.Host, Is.EqualTo("0.0.0.0"));
            Assert.That(listener.AdvertisedEndpoint.Host, Is.EqualTo("198.51.100.44"));
            Assert.That(commandEndpoint.Host, Is.EqualTo("198.51.100.44"));
            Assert.That(commandEndpoint.Port, Is.EqualTo(listener.BindEndpoint.Port));
        });
    }

    [Test]
    public void Explicit_fixed_active_port_is_used()
    {
        int port = ReserveAvailablePort();
        FtpDataConnectionOptions options = CreateOptions();
        options.ActiveBindEndpoint = CreateEndpoint(
            IPAddress.Loopback.ToString(), port, FtpAddressFamily.IPv4);
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, options);

        FtpActiveListener listener = connector.OpenListener();
        using Socket listenerSocket = listener.Listener!;

        Assert.That(listener.BindEndpoint.Port, Is.EqualTo(port));
    }

    [Test]
    public void Active_port_range_skips_an_occupied_port_and_uses_the_next_available_port()
    {
        using PortReservation reservation = FindConsecutivePortReservation();
        FtpDataConnectionOptions options = CreateOptions();
        options.ActivePortStart = reservation.OccupiedPort;
        options.ActivePortEnd = reservation.AvailablePort;
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, options);

        FtpActiveListener listener = connector.OpenListener();
        using Socket listenerSocket = listener.Listener!;

        Assert.That(listener.BindEndpoint.Port, Is.EqualTo(reservation.AvailablePort));
    }

    [Test]
    public void IPV4_supports_direct_PORT_and_standard_EPRT_fallback()
    {
        FtpActiveConnector extended = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, CreateOptions());
        FtpActiveListener extendedListener = extended.OpenListener();
        using Socket extendedSocket = extendedListener.Listener!;
        FtpCommand fallback = extended.CreateActiveFallbackCommand(
            extendedListener, CreateReply(500, "EPRT unsupported"));

        FtpDataConnectionOptions directOptions = CreateOptions();
        directOptions.UseExtendedActive = false;
        FtpActiveConnector direct = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, directOptions);
        FtpActiveListener directListener = direct.OpenListener();
        using Socket directSocket = directListener.Listener!;
        FtpCommand directCommand = direct.CreateActiveCommand(directListener);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Kind, Is.EqualTo(FtpCommandKind.Port));
            Assert.That(FtpEndpointCodec.ParsePortArgument(fallback.Argument).Port,
                Is.EqualTo(extendedListener.AdvertisedEndpoint.Port));
            Assert.That(directCommand.Kind, Is.EqualTo(FtpCommandKind.Port));
            Assert.Throws<FtpProtocolException>(() =>
                extended.CreateActiveFallbackCommand(
                    extendedListener, CreateReply(425, "data failure")));
            Assert.Throws<FtpConfigurationException>(() =>
                direct.CreateExtendedActiveCommand(directListener));
        });
    }

    [Test]
    public void IPV6_uses_EPRT_and_never_PORT_fallback()
    {
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, CreateOptions());
        FtpActiveListener listener = CreateSyntheticListener(
            IPAddress.IPv6Loopback, 50123, FtpAddressFamily.IPv6);
        using Socket listenerSocket = listener.Listener!;

        FtpCommand command = connector.CreateExtendedActiveCommand(listener);

        Assert.Multiple(() =>
        {
            Assert.That(command.Argument, Is.EqualTo("|2|::1|50123|"));
            Assert.Throws<FtpProtocolException>(() =>
                connector.CreateActiveFallbackCommand(
                    listener, CreateReply(500, "EPRT unsupported")));
        });
    }

    [Test]
    public void Accept_returns_matching_control_peer_and_closes_one_shot_listener()
    {
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, CreateOptions());
        FtpActiveListener listener = connector.OpenListener();
        Socket listeningSocket = listener.Listener!;
        using Socket client = ConnectToListener(listener);

        using Socket accepted = connector.AcceptConnection(
            listener, 5000, default(CancellationToken));
        IPEndPoint remote = (IPEndPoint)accepted.RemoteEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(remote.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(listener.Open, Is.False);
            Assert.That(listener.Listener, Is.Null);
            Assert.That(listeningSocket.SafeHandle.IsClosed, Is.True);
            Assert.That(accepted.Connected, Is.True);
        });
    }

    [Test]
    public void Mismatched_active_peer_is_rejected_and_both_sockets_are_closed()
    {
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Parse("127.0.0.2"), IPAddress.Loopback, CreateOptions());
        FtpActiveListener listener = connector.OpenListener();
        Socket listeningSocket = listener.Listener!;
        using Socket client = ConnectToListener(listener);

        FtpConnectionException? error = Assert.Throws<FtpConnectionException>(() =>
            connector.AcceptConnection(listener, 5000, default(CancellationToken)));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.TypeOf<FtpConnectionException>());
            Assert.That(listener.Open, Is.False);
            Assert.That(listener.Listener, Is.Null);
            Assert.That(listeningSocket.SafeHandle.IsClosed, Is.True);
        });
    }

    [Test]
    public void Explicit_foreign_peer_policy_accepts_a_non_control_data_peer()
    {
        FtpDataConnectionOptions options = CreateOptions();
        options.RequireDataPeerMatch = false;
        options.AllowForeignDataConnections = true;
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Parse("127.0.0.2"), IPAddress.Loopback, options);
        FtpActiveListener listener = connector.OpenListener();
        using Socket client = ConnectToListener(listener);

        using Socket accepted = connector.AcceptConnection(
            listener, 5000, default(CancellationToken));

        Assert.That(accepted.Connected, Is.True);
    }

    [Test]
    public void Canceled_and_expired_accepts_close_the_one_shot_listener()
    {
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, CreateOptions());
        FtpActiveListener canceledListener = connector.OpenListener();
        Socket canceledSocket = canceledListener.Listener!;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FtpActiveListener timedOutListener = connector.OpenListener();
        Socket timedOutSocket = timedOutListener.Listener!;

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                connector.AcceptConnection(canceledListener, 5000, cancellation.Token));
            Assert.That(canceledListener.Open, Is.False);
            Assert.That(canceledSocket.SafeHandle.IsClosed, Is.True);
            Assert.Throws<TimeoutException>(() =>
                connector.AcceptConnection(
                    timedOutListener, 0, default(CancellationToken)));
            Assert.That(timedOutListener.Open, Is.False);
            Assert.That(timedOutSocket.SafeHandle.IsClosed, Is.True);
        });
    }

    [Test]
    public void Invalid_active_modes_addresses_and_port_sources_are_rejected()
    {
        FtpDataConnectionOptions passive = CreateOptions();
        passive.Mode = FtpDataConnectionMode.Passive;
        FtpDataConnectionOptions ipv6Port = CreateOptions();
        ipv6Port.UseExtendedActive = false;
        FtpDataConnectionOptions dnsBind = CreateOptions();
        dnsBind.ActiveBindEndpoint = CreateEndpoint("example.test", 0, FtpAddressFamily.IPv4);
        FtpDataConnectionOptions familyMismatch = CreateOptions();
        familyMismatch.ActiveBindEndpoint = CreateEndpoint("::1", 0, FtpAddressFamily.IPv6);
        FtpDataConnectionOptions unspecifiedAdvertised = CreateOptions();
        unspecifiedAdvertised.ActiveAdvertisedAddress = "0.0.0.0";
        FtpDataConnectionOptions conflictingPorts = CreateOptions();
        conflictingPorts.ActiveBindEndpoint = CreateEndpoint(
            IPAddress.Loopback.ToString(), 45000, FtpAddressFamily.IPv4);
        conflictingPorts.ActivePortStart = 45001;
        conflictingPorts.ActivePortEnd = 45002;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(IPAddress.Loopback, IPAddress.Loopback, passive));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(
                    IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, ipv6Port));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(IPAddress.Loopback, IPAddress.Loopback, dnsBind));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(IPAddress.Loopback, IPAddress.Loopback, familyMismatch));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(
                    IPAddress.Loopback, IPAddress.Loopback, unspecifiedAdvertised));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(IPAddress.Loopback, IPAddress.Loopback, conflictingPorts));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpActiveConnector(
                    IPAddress.IPv6Loopback, IPAddress.Loopback, CreateOptions()));
        });
    }

    private static FtpDataConnectionOptions CreateOptions()
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Mode = FtpDataConnectionMode.Active;
        return options;
    }

    private static FtpEndpoint CreateEndpoint(
        string host,
        int port,
        FtpAddressFamily addressFamily)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.AddressFamily = addressFamily;
        return endpoint;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }

    private static FtpActiveListener CreateSyntheticListener(
        IPAddress address,
        int port,
        FtpAddressFamily family)
    {
        FtpActiveListener listener = new FtpActiveListener();
        listener.Listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Open = true;
        listener.BindEndpoint = CreateEndpoint(address.ToString(), port, family);
        listener.AdvertisedEndpoint = CreateEndpoint(address.ToString(), port, family);
        return listener;
    }

    private static Socket ConnectToListener(FtpActiveListener listener)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(IPAddress.Loopback, listener.BindEndpoint.Port);
        return socket;
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static PortReservation FindConsecutivePortReservation()
    {
        int port = 40000;
        while (port < 60000)
        {
            TcpListener first = new TcpListener(IPAddress.Loopback, port);
            TcpListener second = new TcpListener(IPAddress.Loopback, port + 1);
            try
            {
                first.Start();
                second.Start();
                second.Stop();
                return new PortReservation(first, port, port + 1);
            }
            catch (SocketException)
            {
                first.Stop();
                second.Stop();
            }

            port += 2;
        }

        throw new InvalidOperationException("No consecutive loopback ports were available for the test.");
    }

    private class PortReservation : IDisposable
    {
        private readonly TcpListener listener;

        public int OccupiedPort;
        public int AvailablePort;

        public PortReservation(TcpListener listener, int occupiedPort, int availablePort)
        {
            this.listener = listener;
            OccupiedPort = occupiedPort;
            AvailablePort = availablePort;
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
