using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ftp.Tests;

public class FtpServerLimitTests
{
    [Test]
    public void Address_and_pending_login_limits_reject_excess_and_recover_after_release()
    {
        RunAdmissionScenario(false);
        RunAdmissionScenario(true);
    }

    [Test]
    public void Session_limits_enforce_user_idle_command_malformed_and_transfer_bounds()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(port);
        options.MaximumSessions = 2;
        options.MaximumSessionsPerAddress = 2;
        options.MaximumSessionsPerUser = 1;
        options.MaximumPendingConnections = 2;
        options.MaximumSimultaneousTransfers = 1;
        options.MaximumCommandsPerMinute = 2;
        options.MaximumMalformedLines = 1;
        options.Timeouts.IdleTimeoutMilliseconds = 100;
        using CountdownEvent started = new CountdownEvent(2);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        List<FtpServerSessionContext> contexts =
            new List<FtpServerSessionContext>();
        object contextLock = new object();
        server.Runtime.SessionOperation = context =>
        {
            lock (contextLock)
            {
                contexts.Add(context);
            }
            started.Signal();
            release.Wait();
            return null;
        };
        Socket? firstClient = null;
        Socket? secondClient = null;

        try
        {
            server.StartServer(options);
            firstClient = Connect(port);
            secondClient = Connect(port);
            Assert.That(started.Wait(3000), Is.True);

            FtpServerSessionContext first;
            FtpServerSessionContext second;
            lock (contextLock)
            {
                first = contexts[0];
                second = contexts[1];
            }
            FtpServerLimitManager limits = GetLimits(server);

            bool firstAuthentication = first.TryAuthenticateUser("same-user");
            bool secondAuthentication = second.TryAuthenticateUser("same-user");
            first.RecordActivity(1000);
            bool firstCommand = first.TryRecordCommand(1000);
            bool secondCommand = first.TryRecordCommand(2000);
            bool excessCommand = first.TryRecordCommand(3000);
            bool rollingCommand = first.TryRecordCommand(61000);
            bool activeBeforeIdleDeadline = first.HasIdleExpired(61099);
            bool expiredAtIdleDeadline = first.HasIdleExpired(61100);
            bool firstMalformed = first.TryRecordMalformedLine();
            bool excessMalformed = first.TryRecordMalformedLine();
            bool firstTransfer = first.TryBeginTransfer();
            bool duplicateTransfer = first.TryBeginTransfer();
            bool excessTransfer = second.TryBeginTransfer();
            first.EndTransfer();
            bool releasedTransfer = second.TryBeginTransfer();
            second.EndTransfer();

            Assert.Multiple(() =>
            {
                Assert.That(firstAuthentication, Is.True);
                Assert.That(secondAuthentication, Is.False);
                Assert.That(limits.GetUserSessionCount("same-user"), Is.EqualTo(1));
                Assert.That(limits.GetPendingLoginCount(), Is.EqualTo(1));
                Assert.That(firstCommand, Is.True);
                Assert.That(secondCommand, Is.True);
                Assert.That(excessCommand, Is.False);
                Assert.That(rollingCommand, Is.True);
                Assert.That(activeBeforeIdleDeadline, Is.False);
                Assert.That(expiredAtIdleDeadline, Is.True);
                Assert.That(firstMalformed, Is.True);
                Assert.That(excessMalformed, Is.False);
                Assert.That(firstTransfer, Is.True);
                Assert.That(duplicateTransfer, Is.False);
                Assert.That(excessTransfer, Is.False);
                Assert.That(releasedTransfer, Is.True);
                Assert.That(limits.GetTransferCount(), Is.Zero);
                Assert.That(limits.GetSessionCount(), Is.EqualTo(2));
            });

            release.Set();
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 2, 3000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(limits.GetSessionCount(), Is.Zero);
                Assert.That(limits.GetPendingLoginCount(), Is.Zero);
                Assert.That(limits.GetUserSessionCount("same-user"), Is.Zero);
                Assert.That(limits.GetTransferCount(), Is.Zero);
            });
        }
        finally
        {
            release.Set();
            firstClient?.Dispose();
            secondClient?.Dispose();
            server.StopServer();
        }
    }

    [Test]
    public void Global_session_limit_bounds_dispatch_until_a_session_finishes()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(port);
        options.MaximumSessions = 1;
        options.MaximumSessionsPerAddress = 1;
        options.MaximumSessionsPerUser = 1;
        options.MaximumPendingConnections = 1;
        options.MaximumSimultaneousTransfers = 1;
        using ManualResetEventSlim firstStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        int operationCount = 0;
        server.Runtime.SessionOperation = context =>
        {
            int current = Interlocked.Increment(ref operationCount);
            if (current == 1)
            {
                firstStarted.Set();
                release.Wait();
            }
            return null;
        };
        Socket? firstClient = null;
        Socket? secondClient = null;

        try
        {
            server.StartServer(options);
            firstClient = Connect(port);
            Assert.That(firstStarted.Wait(3000), Is.True);
            secondClient = Connect(port);

            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(1));
                Assert.That(dispatcher.GetActiveSessionCount(), Is.EqualTo(1));
                Assert.That(GetLimits(server).GetSessionCount(), Is.EqualTo(1));
            });

            release.Set();
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetCompletedSessionCount() == 2, 3000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetPeakActiveSessionCount(), Is.EqualTo(1));
                Assert.That(GetLimits(server).GetSessionCount(), Is.Zero);
            });
        }
        finally
        {
            release.Set();
            firstClient?.Dispose();
            secondClient?.Dispose();
            server.StopServer();
        }
    }

    private static void RunAdmissionScenario(bool pendingLoginLimit)
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(port);
        options.MaximumSessions = 3;
        options.MaximumSessionsPerAddress = pendingLoginLimit ? 3 : 1;
        options.MaximumSessionsPerUser = 3;
        options.MaximumPendingConnections = pendingLoginLimit ? 1 : 3;
        options.MaximumSimultaneousTransfers = 3;
        using ManualResetEventSlim firstStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        FtpServerSessionContext? firstContext = null;
        int operationCount = 0;
        server.Runtime.SessionOperation = context =>
        {
            int current = Interlocked.Increment(ref operationCount);
            if (current == 1)
            {
                firstContext = context;
                firstStarted.Set();
            }
            release.Wait();
            return null;
        };
        List<Socket> clients = new List<Socket>();

        try
        {
            server.StartServer(options);
            clients.Add(Connect(port));
            Assert.That(firstStarted.Wait(3000), Is.True);
            clients.Add(Connect(port));
            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetRejectedSessionCount() == 1, 3000), Is.True);
            Assert.That(operationCount, Is.EqualTo(1));

            if (pendingLoginLimit)
            {
                Assert.That(firstContext, Is.Not.Null);
                Assert.That(firstContext!.TryAuthenticateUser("first-user"), Is.True);
                clients.Add(Connect(port));
                Assert.That(SpinWait.SpinUntil(() =>
                    operationCount == 2, 3000), Is.True);
            }
            else
            {
                release.Set();
                Assert.That(SpinWait.SpinUntil(() =>
                    dispatcher.GetCompletedSessionCount() == 1, 3000), Is.True);
                clients.Add(Connect(port));
                Assert.That(SpinWait.SpinUntil(() =>
                    operationCount == 2, 3000), Is.True);
            }

            release.Set();
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetCompletedSessionCount() == 2, 3000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.GetAcceptedSessionCount(), Is.EqualTo(3));
                Assert.That(dispatcher.GetRejectedSessionCount(), Is.EqualTo(1));
                Assert.That(GetLimits(server).GetSessionCount(), Is.Zero);
                Assert.That(GetLimits(server).GetPendingLoginCount(), Is.Zero);
            });
        }
        finally
        {
            release.Set();
            DisposeSockets(clients);
            server.StopServer();
        }
    }

    private static FtpServerLimitManager GetLimits(FtpServer server)
    {
        FtpServerLimitManager? limits = server.Runtime.Limits;
        Assert.That(limits, Is.Not.Null);
        return limits!;
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

    private static FtpServerOptions CreateOptions(int port)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
        return options;
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
        using TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
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
        return DispatchProxy.Create<T, FtpServerLimitProviderProxy>();
    }
}

public class FtpServerLimitProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Server limit tests do not invoke public providers.");
    }
}
