using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpServerPassiveListenerTests
{
    [Test]
    public void Passive_port_pool_reserves_uniquely_exhausts_and_reuses_released_ports()
    {
        FtpPassivePortPool pool = new FtpPassivePortPool(50000, 50002);

        int first = pool.ReserveNextPort();
        int second = pool.ReserveNextPort();
        int third = pool.ReserveNextPort();
        int exhausted = pool.ReserveNextPort();
        pool.ReleasePort(second);
        int reused = pool.ReserveNextPort();

        Assert.Multiple(() =>
        {
            Assert.That(new[] { first, second, third }, Is.Unique);
            Assert.That(exhausted, Is.Zero);
            Assert.That(reused, Is.EqualTo(second));
            Assert.That(pool.ReservedCount, Is.EqualTo(3));
            Assert.That(pool.IsReserved(first), Is.True);
            Assert.That(pool.IsReserved(second), Is.True);
            Assert.That(pool.IsReserved(third), Is.True);
            Assert.Throws<FtpConfigurationException>(() => pool.ReleasePort(49999));
        });
    }

    [Test]
    public void PASV_listener_uses_masquerade_address_and_releases_port_on_close()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback,
            IPAddress.Loopback,
            pool,
            "198.51.100.77",
            true,
            true);

        FtpReply reply = listener.OpenPassiveListener(false, FtpAddressFamily.IPv4);
        FtpPassiveReply parsed = FtpEndpointCodec.ParsePassiveReply(reply);
        Socket listeningSocket = listener.Listener!;

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(227));
            Assert.That(parsed.Endpoint.Host, Is.EqualTo("198.51.100.77"));
            Assert.That(parsed.Endpoint.Port, Is.EqualTo(port));
            Assert.That(listener.BindEndpoint.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(listener.AdvertisedEndpoint.Host, Is.EqualTo("198.51.100.77"));
            Assert.That(pool.IsReserved(port), Is.True);
        });

        listener.CloseListener();

        Assert.Multiple(() =>
        {
            Assert.That(listener.Open, Is.False);
            Assert.That(listener.Listener, Is.Null);
            Assert.That(pool.IsReserved(port), Is.False);
            Assert.That(listeningSocket.SafeHandle.IsClosed, Is.True);
        });
    }

    [Test]
    public void EPSV_reply_contains_only_the_bound_port_even_with_masquerade_configuration()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback,
            IPAddress.Loopback,
            pool,
            "198.51.100.88",
            true,
            true);

        FtpReply reply = listener.OpenPassiveListener(
            true, FtpAddressFamily.Unspecified);
        FtpPassiveReply parsed = FtpEndpointCodec.ParseExtendedPassiveReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(229));
            Assert.That(parsed.Endpoint.Host, Is.Empty);
            Assert.That(parsed.Endpoint.Port, Is.EqualTo(port));
            Assert.That(listener.Extended, Is.True);
            Assert.That(listener.AdvertisedEndpoint.Host, Is.EqualTo("198.51.100.88"));
        });

        listener.CloseListener();
    }

    [Test]
    public void Opening_a_new_passive_listener_closes_and_replaces_the_previous_one()
    {
        using PortReservation ports = FindConsecutivePortReservation(false);
        ports.ReleaseOccupiedPort();
        FtpPassivePortPool pool = new FtpPassivePortPool(
            ports.FirstPort, ports.SecondPort);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);

        listener.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        Socket firstSocket = listener.Listener!;
        listener.OpenPassiveListener(false, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(firstSocket.SafeHandle.IsClosed, Is.True);
            Assert.That(listener.Open, Is.True);
            Assert.That(listener.Extended, Is.False);
            Assert.That(pool.ReservedCount, Is.EqualTo(1));
        });

        listener.CloseListener();
    }

    [Test]
    public void Passive_listener_skips_an_OS_occupied_pool_port()
    {
        using PortReservation ports = FindConsecutivePortReservation(true);
        FtpPassivePortPool pool = new FtpPassivePortPool(
            ports.FirstPort, ports.SecondPort);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);

        listener.OpenPassiveListener(true, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(listener.ReservedPort, Is.EqualTo(ports.SecondPort));
            Assert.That(pool.IsReserved(ports.FirstPort), Is.False);
            Assert.That(pool.IsReserved(ports.SecondPort), Is.True);
        });

        listener.CloseListener();
    }

    [Test]
    public void Shared_pool_prevents_reuse_until_the_owning_listener_closes()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener first = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        FtpPassiveListener second = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        first.OpenPassiveListener(true, FtpAddressFamily.IPv4);

        Assert.Throws<FtpConnectionException>(() =>
            second.OpenPassiveListener(true, FtpAddressFamily.IPv4));

        first.CloseListener();
        FtpReply reply = second.OpenPassiveListener(true, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(229));
            Assert.That(second.ReservedPort, Is.EqualTo(port));
        });

        second.CloseListener();
    }

    [Test]
    public void EPSV_protocol_selection_returns_522_without_reserving_a_port_on_mismatch()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);

        FtpReply reply = listener.OpenPassiveListener(true, FtpAddressFamily.IPv6);
        FtpNetworkProtocolReply protocols = FtpEndpointCodec.ParseNetworkProtocolReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(522));
            Assert.That(protocols.SupportedProtocols, Is.EqualTo(new[] { 1 }));
            Assert.That(listener.Open, Is.False);
            Assert.That(pool.ReservedCount, Is.Zero);
        });
    }

    [Test]
    public void IPV6_EPSV_listener_uses_selected_family_and_dual_stack_setting()
    {
        int port = ReserveAvailablePort(IPAddress.IPv6Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.IPv6Loopback,
            IPAddress.IPv6Loopback,
            pool,
            null,
            true,
            true);

        FtpReply reply = listener.OpenPassiveListener(true, FtpAddressFamily.IPv6);
        Socket socket = listener.Listener!;

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(229));
            Assert.That(socket.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(socket.DualMode, Is.True);
            Assert.Throws<FtpProtocolException>(() =>
                listener.OpenPassiveListener(false, FtpAddressFamily.IPv4));
        });

        listener.CloseListener();
    }

    [Test]
    public void EPSV_ALL_closes_existing_listener_blocks_PASV_and_resets_with_session()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        listener.OpenPassiveListener(false, FtpAddressFamily.IPv4);
        Socket previous = listener.Listener!;

        listener.EnableExtendedPassiveOnly();

        Assert.Multiple(() =>
        {
            Assert.That(previous.SafeHandle.IsClosed, Is.True);
            Assert.That(listener.ExtendedPassiveOnly, Is.True);
            Assert.That(pool.ReservedCount, Is.Zero);
            Assert.Throws<FtpProtocolException>(() =>
                listener.OpenPassiveListener(false, FtpAddressFamily.IPv4));
        });

        FtpReply epsv = listener.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        listener.ResetSession();
        FtpReply pasv = listener.OpenPassiveListener(false, FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(epsv.Code, Is.EqualTo(229));
            Assert.That(listener.ExtendedPassiveOnly, Is.False);
            Assert.That(pasv.Code, Is.EqualTo(227));
        });

        listener.CloseListener();
    }

    [Test]
    public void One_shot_accept_returns_matching_peer_and_releases_pool_port()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        listener.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        Socket listeningSocket = listener.Listener!;
        using Socket client = ConnectToListener(listener);

        using Socket accepted = listener.AcceptConnection(
            5000, default(CancellationToken));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Connected, Is.True);
            Assert.That(listener.Open, Is.False);
            Assert.That(listener.Listener, Is.Null);
            Assert.That(listeningSocket.SafeHandle.IsClosed, Is.True);
            Assert.That(pool.ReservedCount, Is.Zero);
            Assert.Throws<FtpConfigurationException>(() =>
                listener.AcceptConnection(5000, default(CancellationToken)));
        });
    }

    [Test]
    public void Mismatched_passive_peer_is_rejected_and_pool_port_is_released()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpPassiveListener listener = CreateListener(
            IPAddress.Parse("127.0.0.2"), IPAddress.Loopback, pool, null, true, true);
        listener.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        using Socket client = ConnectToListener(listener);

        FtpConnectionException? error = Assert.Throws<FtpConnectionException>(() =>
            listener.AcceptConnection(5000, default(CancellationToken)));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.TypeOf<FtpConnectionException>());
            Assert.That(listener.Open, Is.False);
            Assert.That(pool.ReservedCount, Is.Zero);
        });
    }

    [Test]
    public void Canceled_and_expired_accepts_close_listeners_and_release_pool_ports()
    {
        using PortReservation ports = FindConsecutivePortReservation(false);
        ports.ReleaseOccupiedPort();
        FtpPassivePortPool pool = new FtpPassivePortPool(
            ports.FirstPort, ports.SecondPort);
        FtpPassiveListener canceled = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        FtpPassiveListener timedOut = CreateListener(
            IPAddress.Loopback, IPAddress.Loopback, pool, null, true, true);
        canceled.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        timedOut.OpenPassiveListener(true, FtpAddressFamily.IPv4);
        Socket canceledSocket = canceled.Listener!;
        Socket timedOutSocket = timedOut.Listener!;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                canceled.AcceptConnection(5000, cancellation.Token));
            Assert.That(canceledSocket.SafeHandle.IsClosed, Is.True);
            Assert.Throws<TimeoutException>(() =>
                timedOut.AcceptConnection(0, default(CancellationToken)));
            Assert.That(timedOutSocket.SafeHandle.IsClosed, Is.True);
            Assert.That(pool.ReservedCount, Is.Zero);
        });
    }

    [Test]
    public void Invalid_server_passive_configuration_is_rejected()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        FtpPassivePortPool pool = new FtpPassivePortPool(port, port);
        FtpDataConnectionOptions active = CreateDataOptions(port, port, true);
        active.Mode = FtpDataConnectionMode.Active;
        FtpDataConnectionOptions mismatchedRange = CreateDataOptions(port + 1, port + 1, true);
        FtpListenerOptions dnsMasquerade = CreateListenerOptions("ftp.example", true);
        FtpListenerOptions wrongFamily = CreateListenerOptions(null, true);
        wrongFamily.Endpoint.AddressFamily = FtpAddressFamily.IPv6;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(null, IPAddress.Loopback,
                    CreateListenerOptions(null, true),
                    CreateDataOptions(port, port, true), pool));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(IPAddress.Loopback, IPAddress.Loopback,
                    CreateListenerOptions(null, true), active, pool));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(IPAddress.Loopback, IPAddress.Loopback,
                    CreateListenerOptions(null, true), mismatchedRange, pool));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(IPAddress.Loopback, IPAddress.Loopback,
                    dnsMasquerade, CreateDataOptions(port, port, true), pool));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(IPAddress.Loopback, IPAddress.Loopback,
                    wrongFamily, CreateDataOptions(port, port, true), pool));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveListener(IPAddress.IPv6Loopback, IPAddress.Loopback,
                    CreateListenerOptions(null, true),
                    CreateDataOptions(port, port, true), pool));
        });
    }

    private static FtpPassiveListener CreateListener(
        IPAddress controlPeer,
        IPAddress controlLocal,
        FtpPassivePortPool pool,
        string? advertisedAddress,
        bool dualStack,
        bool requirePeerMatch)
    {
        FtpListenerOptions listenerOptions = CreateListenerOptions(
            advertisedAddress, dualStack);
        FtpDataConnectionOptions dataOptions = CreateDataOptions(
            pool.StartPort, pool.EndPort, requirePeerMatch);
        return new FtpPassiveListener(
            controlPeer, controlLocal, listenerOptions, dataOptions, pool);
    }

    private static FtpListenerOptions CreateListenerOptions(
        string? advertisedAddress,
        bool dualStack)
    {
        FtpListenerOptions options = new FtpListenerOptions();
        options.DualStack = dualStack;
        options.PassiveAdvertisedAddress = advertisedAddress;
        return options;
    }

    private static FtpDataConnectionOptions CreateDataOptions(
        int startPort,
        int endPort,
        bool requirePeerMatch)
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Mode = FtpDataConnectionMode.Passive;
        options.PassivePortStart = startPort;
        options.PassivePortEnd = endPort;
        options.RequireDataPeerMatch = requirePeerMatch;
        return options;
    }

    private static Socket ConnectToListener(FtpPassiveListener listener)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(IPAddress.Loopback, listener.ReservedPort);
        return socket;
    }

    private static int ReserveAvailablePort(IPAddress address)
    {
        using TcpListener listener = new TcpListener(address, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static PortReservation FindConsecutivePortReservation(bool keepFirstOccupied)
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
                PortReservation reservation = new PortReservation(first, port, port + 1);
                if (!keepFirstOccupied)
                {
                    reservation.ReleaseOccupiedPort();
                }

                return reservation;
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
        private bool released;

        public int FirstPort;
        public int SecondPort;

        public PortReservation(TcpListener listener, int firstPort, int secondPort)
        {
            this.listener = listener;
            released = false;
            FirstPort = firstPort;
            SecondPort = secondPort;
        }

        public void ReleaseOccupiedPort()
        {
            if (!released)
            {
                listener.Stop();
                released = true;
            }
        }

        public void Dispose()
        {
            ReleaseOccupiedPort();
        }
    }
}
