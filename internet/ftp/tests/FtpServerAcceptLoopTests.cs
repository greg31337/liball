using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ftp.Tests;

public class FtpServerAcceptLoopTests
{
    [Test]
    public void Malformed_and_failed_session_tasks_do_not_stop_later_or_sibling_accepts()
    {
        int[] ports = ReserveAvailablePorts(2);
        FtpServer server = CreateServer();
        int operationCount = 0;
        server.Runtime.SessionOperation = context =>
        {
            int current = Interlocked.Increment(ref operationCount);
            if (current == 1)
            {
                throw new FtpProtocolException("malformed session", null);
            }
            if (current == 2)
            {
                throw new InvalidOperationException("session provider failed");
            }
            return null;
        };
        FtpServerOptions options = CreateOptions(ports, 4);
        List<Socket> clients = new List<Socket>();

        try
        {
            server.StartServer(options);
            clients.Add(Connect(ports[0]));
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() >= 1, 3000), Is.True);

            clients.Add(Connect(ports[0]));
            clients.Add(Connect(ports[1]));
            clients.Add(Connect(ports[1]));
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 4, 3000), Is.True);

            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(4));
                Assert.That(dispatcher.GetCompletedSessionCount(), Is.EqualTo(4));
                Assert.That(dispatcher.GetFailedSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetActiveSessionCount(), Is.Zero);
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
                Assert.That(server.Runtime.Listeners[0].AcceptLoop!.IsCompleted, Is.False);
                Assert.That(server.Runtime.Listeners[1].AcceptLoop!.IsCompleted, Is.False);
                Assert.That(dispatcher.SessionTasks,
                    Has.All.Matches<Task<object?>>(task => !task.IsFaulted));
            });
        }
        finally
        {
            DisposeSockets(clients);
            server.StopServer();
        }
    }

    [Test]
    public void Shared_dispatch_capacity_bounds_sessions_across_all_accept_loops()
    {
        int[] ports = ReserveAvailablePorts(2);
        FtpServer server = CreateServer();
        using ManualResetEventSlim releaseSessions = new ManualResetEventSlim(false);
        using CountdownEvent firstSessions = new CountdownEvent(2);
        int startedSessions = 0;
        server.Runtime.SessionOperation = context =>
        {
            int current = Interlocked.Increment(ref startedSessions);
            if (current <= 2)
            {
                firstSessions.Signal();
            }
            releaseSessions.Wait();
            return null;
        };
        FtpServerOptions options = CreateOptions(ports, 2);
        List<Socket> clients = new List<Socket>();

        try
        {
            server.StartServer(options);
            clients.Add(Connect(ports[0]));
            clients.Add(Connect(ports[1]));
            clients.Add(Connect(ports[0]));
            clients.Add(Connect(ports[1]));

            Assert.That(firstSessions.Wait(3000), Is.True);
            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetActiveSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetPeakActiveSessionCount(), Is.EqualTo(2));
            });

            releaseSessions.Set();
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetCompletedSessionCount() == 4, 3000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(4));
                Assert.That(dispatcher.GetCompletedSessionCount(), Is.EqualTo(4));
                Assert.That(dispatcher.GetPeakActiveSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetFailedSessionCount(), Is.Zero);
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
            });
        }
        finally
        {
            releaseSessions.Set();
            DisposeSockets(clients);
            server.StopServer();
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

    private static FtpServerOptions CreateOptions(int[] ports, int capacity)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions[] listeners = new FtpListenerOptions[ports.Length];
        int index = 0;
        while (index < ports.Length)
        {
            FtpListenerOptions listener = new FtpListenerOptions();
            listener.Endpoint.Host = "127.0.0.1";
            listener.Endpoint.Port = ports[index];
            listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
            listener.SecurityMode = index == 0 ?
                FtpSecurityMode.Plain : FtpSecurityMode.ImplicitTls;
            listener.DualStack = false;
            listener.Backlog = 8;
            listeners[index] = listener;
            index++;
        }
        options.Listeners = listeners;
        options.MaximumSessions = capacity;
        options.MaximumSessionsPerAddress = capacity;
        options.MaximumSessionsPerUser = capacity;
        options.MaximumSimultaneousTransfers = capacity;
        options.MaximumPendingConnections = capacity;
        return options;
    }

    private static Socket Connect(int port)
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(IPAddress.Loopback, port);
        return socket;
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

    private static void DisposeSockets(List<Socket> sockets)
    {
        foreach (Socket socket in sockets)
        {
            socket.Dispose();
        }
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerAcceptProviderProxy>();
    }
}

public class FtpServerAcceptProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Accept-loop tests do not invoke public server providers.");
    }
}
