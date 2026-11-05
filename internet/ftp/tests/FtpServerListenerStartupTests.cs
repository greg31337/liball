using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ftp.Tests;

public class FtpServerListenerStartupTests
{
    [Test]
    public void Multiple_plain_explicit_and_implicit_listeners_bind_and_accept_tcp_connections()
    {
        int[] ports = ReserveAvailablePorts(3, IPAddress.Loopback, false);
        FtpServerOptions options = CreateOptions(new[]
        {
            CreateListener("127.0.0.1", ports[0], FtpAddressFamily.IPv4,
                FtpSecurityMode.Plain, false, 2),
            CreateListener("127.0.0.1", ports[1], FtpAddressFamily.IPv4,
                FtpSecurityMode.ExplicitTlsRequired, false, 3),
            CreateListener("127.0.0.1", ports[2], FtpAddressFamily.IPv4,
                FtpSecurityMode.ImplicitTls, false, 4)
        });
        FtpServer server = CreateServer();
        Socket? first = null;
        Socket? second = null;
        Socket? third = null;

        try
        {
            server.StartServer(options);
            first = Connect(IPAddress.Loopback, ports[0]);
            second = Connect(IPAddress.Loopback, ports[1]);
            third = Connect(IPAddress.Loopback, ports[2]);

            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
                Assert.That(server.Runtime.Listeners, Has.Count.EqualTo(3));
                Assert.That(server.Runtime.Listeners[0].Settings.SecurityMode,
                    Is.EqualTo(FtpSecurityMode.Plain));
                Assert.That(server.Runtime.Listeners[1].Settings.SecurityMode,
                    Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
                Assert.That(server.Runtime.Listeners[2].Settings.SecurityMode,
                    Is.EqualTo(FtpSecurityMode.ImplicitTls));
                Assert.That(server.Runtime.Listeners[0].Settings.Backlog, Is.EqualTo(2));
                Assert.That(server.Runtime.Listeners[1].Settings.Backlog, Is.EqualTo(3));
                Assert.That(server.Runtime.Listeners[2].Settings.Backlog, Is.EqualTo(4));
                Assert.That(first.Connected, Is.True);
                Assert.That(second.Connected, Is.True);
                Assert.That(third.Connected, Is.True);
            });
        }
        finally
        {
            first?.Dispose();
            second?.Dispose();
            third?.Dispose();
            server.StopServer();
        }
    }

    [Test]
    public void Ipv6_and_unspecified_dual_stack_listeners_use_the_requested_families()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        int[] ports = ReserveAvailablePorts(2, IPAddress.IPv6Any, true);
        FtpServerOptions options = CreateOptions(new[]
        {
            CreateListener("", ports[0], FtpAddressFamily.Unspecified,
                FtpSecurityMode.Plain, true, 2),
            CreateListener("::1", ports[1], FtpAddressFamily.IPv6,
                FtpSecurityMode.ImplicitTls, false, 2)
        });
        FtpServer server = CreateServer();
        Socket? ipv4 = null;
        Socket? ipv6 = null;

        try
        {
            server.StartServer(options);
            ipv4 = Connect(IPAddress.Loopback, ports[0]);
            ipv6 = Connect(IPAddress.IPv6Loopback, ports[1]);

            Assert.Multiple(() =>
            {
                Assert.That(server.Runtime.Listeners[0].LocalEndpoint.AddressFamily,
                    Is.EqualTo(FtpAddressFamily.IPv6));
                Assert.That(server.Runtime.Listeners[0].Listener!.DualMode, Is.True);
                Assert.That(server.Runtime.Listeners[1].LocalEndpoint.AddressFamily,
                    Is.EqualTo(FtpAddressFamily.IPv6));
                Assert.That(server.Runtime.Listeners[1].Listener!.DualMode, Is.False);
                Assert.That(ipv4.Connected, Is.True);
                Assert.That(ipv6.Connected, Is.True);
            });
        }
        finally
        {
            ipv4?.Dispose();
            ipv6?.Dispose();
            server.StopServer();
        }
    }

    [Test]
    public void Partial_bind_failure_closes_started_listeners_and_allows_a_clean_retry()
    {
        int availablePort = ReserveAvailablePorts(
            1, IPAddress.Loopback, false)[0];
        using TcpListener occupied = new TcpListener(IPAddress.Loopback, 0);
        occupied.ExclusiveAddressUse = true;
        occupied.Start();
        int occupiedPort = ((IPEndPoint)occupied.LocalEndpoint).Port;
        FtpServerOptions options = CreateOptions(new[]
        {
            CreateListener("127.0.0.1", availablePort, FtpAddressFamily.IPv4,
                FtpSecurityMode.Plain, false, 2),
            CreateListener("127.0.0.1", occupiedPort, FtpAddressFamily.IPv4,
                FtpSecurityMode.ExplicitTlsRequired, false, 2)
        });
        FtpServer server = CreateServer();

        FtpConnectionException failure = Assert.Throws<FtpConnectionException>(
            () => server.StartServer(options))!;
        using TcpListener cleanupProbe =
            new TcpListener(IPAddress.Loopback, availablePort);
        cleanupProbe.ExclusiveAddressUse = true;
        cleanupProbe.Start();

        Assert.Multiple(() =>
        {
            Assert.That(failure.InnerException, Is.Not.Null);
            Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Faulted));
            Assert.That(server.Runtime.Listeners, Is.Empty);
            Assert.That(((IPEndPoint)cleanupProbe.LocalEndpoint).Port,
                Is.EqualTo(availablePort));
        });

        cleanupProbe.Stop();
        occupied.Stop();
        server.StopServer();
        server.StartServer(options);

        Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
        server.StopServer();
    }

    private static FtpServer CreateServer()
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            CreateProxy<IFtpAuthenticator>(),
            CreateProxy<IFtpAuthorizer>(),
            CreateProxy<IFtpHostResolver>(),
            CreateProxy<IFtpCertificateProvider>(),
            CreateProxy<IFtpEventSink>());
    }

    private static FtpServerOptions CreateOptions(
        FtpListenerOptions[] listeners)
    {
        FtpServerOptions options = new FtpServerOptions();
        options.Listeners = listeners;
        return options;
    }

    private static FtpListenerOptions CreateListener(
        string host,
        int port,
        FtpAddressFamily addressFamily,
        FtpSecurityMode securityMode,
        bool dualStack,
        int backlog)
    {
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = host;
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = addressFamily;
        listener.SecurityMode = securityMode;
        listener.DualStack = dualStack;
        listener.Backlog = backlog;
        return listener;
    }

    private static Socket Connect(IPAddress address, int port)
    {
        Socket socket = new Socket(
            address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(address, port);
        return socket;
    }

    private static int[] ReserveAvailablePorts(
        int count,
        IPAddress address,
        bool dualStack)
    {
        List<Socket> reservations = new List<Socket>();
        int[] ports = new int[count];
        try
        {
            int index = 0;
            while (index < count)
            {
                Socket reservation = new Socket(
                    address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (reservation.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    reservation.DualMode = dualStack;
                }
                reservation.ExclusiveAddressUse = true;
                reservation.Bind(new IPEndPoint(address, 0));
                reservation.Listen(1);
                reservations.Add(reservation);
                ports[index] = ((IPEndPoint)reservation.LocalEndPoint!).Port;
                index++;
            }
        }
        finally
        {
            foreach (Socket reservation in reservations)
            {
                reservation.Dispose();
            }
        }
        return ports;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerListenerProviderProxy>();
    }
}

public class FtpServerListenerProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Listener startup tests do not invoke provider operations.");
    }
}
