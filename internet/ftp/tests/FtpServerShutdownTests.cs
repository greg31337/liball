using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ftp.Tests;

public class FtpServerShutdownTests
{
    [Test]
    public void Stop_drains_sessions_and_closes_every_registered_resource_before_deadline()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(port, 2000);
        using ManualResetEventSlim registered = new ManualResetEventSlim(false);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        Socket passiveSocket = CreateSocket();
        Socket dataSocket = CreateSocket();
        TrackingServerStream tlsStream = new TrackingServerStream();
        TrackingServerStream fileStream = new TrackingServerStream();
        FtpServerSessionContext? acceptedContext = null;
        server.Runtime.SessionOperation = context =>
        {
            acceptedContext = context;
            context.RegisterPassiveSocket(passiveSocket);
            context.RegisterDataSocket(dataSocket);
            context.RegisterTlsResource(tlsStream);
            context.RegisterFileResource(fileStream);
            registered.Set();
            release.Wait();
            return null;
        };
        Socket? client = null;
        Stopwatch elapsed = new Stopwatch();

        try
        {
            server.StartServer(options);
            client = Connect(port);
            Assert.That(registered.Wait(3000), Is.True);

            Task stopTask = Task.Run(() =>
            {
                elapsed.Start();
                server.StopServer();
                elapsed.Stop();
            });
            Assert.That(SpinWait.SpinUntil(() =>
                server.GetServerState() == FtpServerState.Draining, 3000), Is.True);
            release.Set();
            Assert.That(stopTask.Wait(3000), Is.True);
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 1, 3000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Stopped));
                Assert.That(GetDispatcher(server).GetForcedSessionCount(), Is.Zero);
                Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(2000));
                Assert.That(acceptedContext, Is.Not.Null);
                Assert.That(acceptedContext!.ResourcesClosed, Is.True);
                Assert.That(passiveSocket.SafeHandle.IsClosed, Is.True);
                Assert.That(dataSocket.SafeHandle.IsClosed, Is.True);
                Assert.That(tlsStream.Disposed, Is.True);
                Assert.That(fileStream.Disposed, Is.True);
                Assert.That(server.Runtime.Listeners, Is.Empty);
            });
        }
        finally
        {
            release.Set();
            client?.Dispose();
            server.StopServer();
            passiveSocket.Dispose();
            dataSocket.Dispose();
            tlsStream.Dispose();
            fileStream.Dispose();
        }
    }

    [Test]
    public void Stop_forces_blocked_session_resources_closed_at_the_grace_deadline()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(port, 100);
        using ManualResetEventSlim registered = new ManualResetEventSlim(false);
        Socket passiveSocket = CreateSocket();
        Socket dataSocket = CreateSocket();
        TrackingServerStream tlsStream = new TrackingServerStream();
        TrackingServerStream fileStream = new TrackingServerStream();
        FtpServerSessionContext? acceptedContext = null;
        server.Runtime.SessionOperation = context =>
        {
            acceptedContext = context;
            context.RegisterPassiveSocket(passiveSocket);
            context.RegisterDataSocket(dataSocket);
            context.RegisterTlsResource(tlsStream);
            context.RegisterFileResource(fileStream);
            registered.Set();
            byte[] buffer = new byte[1];
            context.Socket.Receive(buffer);
            return null;
        };
        Socket? client = null;
        Stopwatch elapsed = new Stopwatch();

        try
        {
            server.StartServer(options);
            client = Connect(port);
            Assert.That(registered.Wait(3000), Is.True);

            elapsed.Start();
            server.StopServer();
            elapsed.Stop();
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 1, 3000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Stopped));
                Assert.That(GetDispatcher(server).GetForcedSessionCount(), Is.EqualTo(1));
                Assert.That(elapsed.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(50));
                Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(2000));
                Assert.That(acceptedContext, Is.Not.Null);
                Assert.That(acceptedContext!.ResourcesClosed, Is.True);
                Assert.That(passiveSocket.SafeHandle.IsClosed, Is.True);
                Assert.That(dataSocket.SafeHandle.IsClosed, Is.True);
                Assert.That(tlsStream.Disposed, Is.True);
                Assert.That(fileStream.Disposed, Is.True);
                Assert.That(server.Runtime.Listeners, Is.Empty);
            });
        }
        finally
        {
            client?.Dispose();
            server.StopServer();
            passiveSocket.Dispose();
            dataSocket.Dispose();
            tlsStream.Dispose();
            fileStream.Dispose();
        }
    }

    private static FtpServerSessionDispatcher GetDispatcher(FtpServer server)
    {
        FtpServerSessionDispatcher? dispatcher = server.Runtime.SessionDispatcher;
        Assert.That(dispatcher, Is.Not.Null);
        return dispatcher!;
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
        int port,
        int gracefulStopTimeoutMilliseconds)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        options.Listeners = new[] { listener };
        options.Timeouts.GracefulStopTimeoutMilliseconds =
            gracefulStopTimeoutMilliseconds;
        return options;
    }

    private static Socket Connect(int port)
    {
        Socket socket = CreateSocket();
        socket.Connect(IPAddress.Loopback, port);
        return socket;
    }

    private static Socket CreateSocket()
    {
        return new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerShutdownProviderProxy>();
    }
}

public class TrackingServerStream : MemoryStream
{
    public bool Disposed;

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

public class FtpServerShutdownProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Server shutdown tests do not invoke public providers.");
    }
}
