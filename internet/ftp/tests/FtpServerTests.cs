using System.Reflection;
using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpServerTests
{
    [Test]
    public void Constructor_requires_every_server_provider_and_starts_stopped()
    {
        IFtpFileSystem fileSystem = CreateProxy<IFtpFileSystem>();
        IFtpAuthenticator authenticator = CreateProxy<IFtpAuthenticator>();
        IFtpAuthorizer authorizer = CreateProxy<IFtpAuthorizer>();
        IFtpHostResolver hostResolver = CreateProxy<IFtpHostResolver>();
        IFtpCertificateProvider certificateProvider =
            CreateProxy<IFtpCertificateProvider>();
        IFtpEventSink eventSink = CreateProxy<IFtpEventSink>();

        FtpServer server = new FtpServer(
            fileSystem,
            authenticator,
            authorizer,
            hostResolver,
            certificateProvider,
            eventSink);

        Assert.Multiple(() =>
        {
            Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Stopped));
            Assert.Throws<FtpConfigurationException>(() => new FtpServer(
                null,
                authenticator,
                authorizer,
                hostResolver,
                certificateProvider,
                eventSink));
            Assert.Throws<FtpConfigurationException>(() => new FtpServer(
                fileSystem,
                authenticator,
                authorizer,
                hostResolver,
                certificateProvider,
                null));
            Assert.That(server.Runtime.FileSystem, Is.SameAs(fileSystem));
            Assert.That(server.Runtime.EventSink, Is.SameAs(eventSink));
        });
    }

    [Test]
    public void Interface_lifecycle_dispatches_start_stop_and_restart()
    {
        IFtpServer server = CreateServer();

        server.StopServer();
        server.StartServer(CreateValidOptions());
        FtpServerState running = server.GetServerState();
        server.StopServer();
        server.StopServer();
        FtpServerState stopped = server.GetServerState();
        server.StartServer(CreateValidOptions());

        Assert.Multiple(() =>
        {
            Assert.That(running, Is.EqualTo(FtpServerState.Running));
            Assert.That(stopped, Is.EqualTo(FtpServerState.Stopped));
            Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
        });
        server.StopServer();
    }

    [Test]
    public void Interface_start_binds_every_listener_and_exposes_running_state()
    {
        int[] ports = ReserveAvailablePorts(2);
        FtpServer concreteServer = CreateServer();
        concreteServer.Runtime.SessionOperation = context => null;
        IFtpServer server = concreteServer;
        FtpServerOptions options = CreateValidOptions();
        options.Listeners = new[]
        {
            CreateListener(ports[0]),
            CreateListener(ports[1])
        };
        Socket? firstClient = null;
        Socket? secondClient = null;

        try
        {
            server.StartServer(options);
            firstClient = Connect(ports[0]);
            secondClient = Connect(ports[1]);

            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
                Assert.That(concreteServer.Runtime.Listeners, Has.Count.EqualTo(2));
                Assert.That(concreteServer.Runtime.Listeners[0].LocalEndpoint.Port,
                    Is.EqualTo(ports[0]));
                Assert.That(concreteServer.Runtime.Listeners[1].LocalEndpoint.Port,
                    Is.EqualTo(ports[1]));
            });
        }
        finally
        {
            firstClient?.Dispose();
            secondClient?.Dispose();
            server.StopServer();
        }
    }

    [Test]
    public void Interface_stop_drains_and_rejects_only_overlapping_stop_calls()
    {
        int port = ReserveAvailablePort();
        FtpServer concreteServer = CreateServer();
        IFtpServer server = concreteServer;
        using ManualResetEventSlim sessionStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim releaseSession = new ManualResetEventSlim(false);
        concreteServer.Runtime.SessionOperation = context =>
        {
            sessionStarted.Set();
            releaseSession.Wait();
            return null;
        };
        FtpServerOptions options = CreateValidOptions();
        options.Listeners[0].Endpoint.Port = port;
        options.Timeouts.GracefulStopTimeoutMilliseconds = 3000;
        Socket? client = null;

        try
        {
            server.StartServer(options);
            client = Connect(port);
            Assert.That(sessionStarted.Wait(3000), Is.True);

            Task stopTask = Task.Run(() => server.StopServer());
            Assert.That(SpinWait.SpinUntil(() =>
                server.GetServerState() == FtpServerState.Draining, 3000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.Throws<FtpProtocolException>(() => server.StopServer());
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Draining));
            });

            releaseSession.Set();
            Assert.That(stopTask.Wait(5000), Is.True);
            Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Stopped));
            Assert.DoesNotThrow(() => server.StopServer());
        }
        finally
        {
            releaseSession.Set();
            client?.Dispose();
            if (server.GetServerState() != FtpServerState.Stopped)
            {
                server.StopServer();
            }
        }
    }

    [Test]
    public void Invalid_or_duplicate_start_preserves_the_active_lifecycle_state()
    {
        FtpServer server = CreateServer();
        FtpServerOptions invalid = new FtpServerOptions();

        Assert.Throws<FtpConfigurationException>(() => server.StartServer(invalid));
        Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Stopped));

        server.StartServer(CreateValidOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                server.StartServer(CreateValidOptions()));
            Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
        });
        server.StopServer();
    }

    [Test]
    public void Start_retains_a_deeply_isolated_validated_settings_snapshot()
    {
        FtpServer server = CreateServer();
        FtpServerOptions source = CreateValidOptions();

        server.StartServer(source);
        FtpServerOptions? retained = server.Runtime.Settings;

        source.Listeners[0].Endpoint.Host = "changed-listener";
        source.Listeners[0].ControlSocket.ReceiveBufferBytes = 8192;
        source.Listeners[0].Tls.CheckCertificateRevocation = false;
        source.VirtualHosts[0].Greeting = "changed greeting";
        source.VirtualHosts[0].Tls.RequireClientCertificate = true;
        source.Timeouts.IdleTimeoutMilliseconds = 1;
        source.DataConnection.Socket.SendBufferBytes = 4096;
        source.DataConnection.ActiveBindEndpoint!.Host = "changed-bind";
        source.Tls.AllowClearCommandChannel = true;
        source.FixedUsers!.Users[0].Salt[0] = 99;
        source.FixedUsers.Users[0].Roles[0] = "changed-role";
        source.Listeners = new FtpListenerOptions[0];
        source.VirtualHosts = new FtpVirtualHostOptions[0];

        Assert.That(retained, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(retained!.Listeners.Length, Is.EqualTo(1));
            Assert.That(retained.Listeners[0].Endpoint.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(retained.Listeners[0].ControlSocket.ReceiveBufferBytes, Is.EqualTo(2048));
            Assert.That(retained.Listeners[0].Tls.CheckCertificateRevocation, Is.True);
            Assert.That(retained.VirtualHosts.Length, Is.EqualTo(1));
            Assert.That(retained.VirtualHosts[0].Greeting, Is.EqualTo("ready"));
            Assert.That(retained.VirtualHosts[0].Tls.RequireClientCertificate, Is.False);
            Assert.That(retained.Timeouts.IdleTimeoutMilliseconds,
                Is.EqualTo(FtpDefaults.IdleTimeoutMilliseconds));
            Assert.That(retained.DataConnection.Socket.SendBufferBytes, Is.EqualTo(1024));
            Assert.That(retained.DataConnection.ActiveBindEndpoint!.Host,
                Is.EqualTo("127.0.0.1"));
            Assert.That(retained.Tls.AllowClearCommandChannel, Is.False);
            Assert.That(retained.FixedUsers!.Users[0].Salt[0], Is.EqualTo(1));
            Assert.That(retained.FixedUsers.Users[0].Roles[0], Is.EqualTo("writer"));
        });
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

    private static FtpServerOptions CreateValidOptions()
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = CreateListener(ReserveAvailablePort());
        listener.ControlSocket.ReceiveBufferBytes = 2048;
        options.Listeners = new[] { listener };

        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = "ftp.example";
        host.Greeting = "ready";
        host.RootPath = "/srv/ftp";
        options.VirtualHosts = new[] { host };

        options.DataConnection.Socket.SendBufferBytes = 1024;
        options.DataConnection.ActiveBindEndpoint = new FtpEndpoint();
        options.DataConnection.ActiveBindEndpoint.Host = "127.0.0.1";
        options.DataConnection.ActiveBindEndpoint.Port = 0;

        FtpUserRecord user = new FtpUserRecord();
        user.UserName = "user";
        user.Salt = new byte[] { 1, 2 };
        user.PasswordVerifier = new byte[] { 3, 4 };
        user.Roles = new[] { "writer" };
        user.Disabled = false;
        FtpFixedUserOptions fixedUsers = new FtpFixedUserOptions();
        fixedUsers.Users = new[] { user };
        options.FixedUsers = fixedUsers;
        return options;
    }

    private static FtpListenerOptions CreateListener(int port)
    {
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        return listener;
    }

    private static Socket Connect(int port)
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(IPAddress.Loopback, port);
        return socket;
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static int[] ReserveAvailablePorts(int count)
    {
        List<TcpListener> reservations = new List<TcpListener>();
        int[] ports = new int[count];
        try
        {
            int index = 0;
            while (index < count)
            {
                TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
                reservation.ExclusiveAddressUse = true;
                reservation.Start();
                reservations.Add(reservation);
                ports[index] = ((IPEndPoint)reservation.LocalEndpoint).Port;
                index++;
            }
        }
        finally
        {
            foreach (TcpListener reservation in reservations)
            {
                reservation.Stop();
            }
        }
        return ports;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerProviderProxy>();
    }
}

public class FtpServerProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Server lifecycle tests do not invoke provider operations.");
    }
}
