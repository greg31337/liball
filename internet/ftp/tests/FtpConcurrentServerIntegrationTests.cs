using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ftp.Tests;

public class FtpConcurrentServerIntegrationTests
{
    private string RootPath = null!;

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-concurrent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void Multiple_listeners_hosts_and_users_isolate_concurrent_transfers()
    {
        string firstRoot = Path.Combine(RootPath, "first");
        string secondRoot = Path.Combine(RootPath, "second");
        Directory.CreateDirectory(firstRoot);
        Directory.CreateDirectory(secondRoot);
        int firstPort = ReserveAvailablePort();
        int secondPort = ReserveDifferentPort(firstPort);
        FtpVirtualHostOptions firstHost = CreateHost(
            "first.example", firstRoot, "alice", "first-secret");
        FtpVirtualHostOptions secondHost = CreateHost(
            "second.example", secondRoot, "bob", "second-secret");
        FtpServerOptions options = CreateOptions(
            new[] { firstPort, secondPort },
            new[] { firstHost, secondHost });
        options.MaximumSimultaneousTransfers = 2;
        FtpServer server = CreateServer(options.VirtualHosts);
        FtpClient? firstClient = null;
        FtpClient? secondClient = null;
        using CountdownEvent firstTransferStarted = new CountdownEvent(1);
        using ManualResetEventSlim releaseTransfers =
            new ManualResetEventSlim(false);

        try
        {
            server.StartServer(options);
            firstClient = ConnectClient(
                firstPort, "first.example", "alice", "first-secret");
            secondClient = ConnectClient(
                secondPort, "second.example", "bob", "second-secret");
            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.That(dispatcher.GetActiveSessionCount(), Is.EqualTo(2));

            byte[] firstContent = CreateContent(65);
            byte[] secondContent = CreateContent(66);
            Task<FtpTransferResult> firstTransfer = Task.Factory.StartNew(
                () => Upload(
                    firstClient,
                    "/shared-name.bin",
                    firstContent,
                    new CoordinatingTransferObserver(
                        firstTransferStarted, releaseTransfers)),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            try
            {
                Assert.That(firstTransferStarted.Wait(5000), Is.True);
                Assert.That(GetLimits(server).GetTransferCount(), Is.EqualTo(1));
                TransferCountObserver secondObserver =
                    new TransferCountObserver(GetLimits(server), 2);
                FtpTransferResult secondResult = Upload(
                    secondClient,
                    "/shared-name.bin",
                    secondContent,
                    secondObserver);
                Assert.That(secondResult.Succeeded, Is.True,
                    secondResult.FailureMessage);
                Assert.That(secondObserver.ObservedExpectedCount, Is.True);
            }
            finally
            {
                releaseTransfers.Set();
            }

            Assert.That(firstTransfer.Wait(5000), Is.True);
            FtpTransferResult firstResult = firstTransfer.GetAwaiter().GetResult();
            byte[] firstDownloaded = Download(firstClient, "/shared-name.bin");
            byte[] secondDownloaded = Download(secondClient, "/shared-name.bin");

            Assert.Multiple(() =>
            {
                Assert.That(firstResult.Succeeded, Is.True,
                    firstResult.FailureMessage);
                Assert.That(firstDownloaded, Is.EqualTo(firstContent));
                Assert.That(secondDownloaded, Is.EqualTo(secondContent));
                Assert.That(File.ReadAllBytes(Path.Combine(
                    firstRoot, "shared-name.bin")), Is.EqualTo(firstContent));
                Assert.That(File.ReadAllBytes(Path.Combine(
                    secondRoot, "shared-name.bin")), Is.EqualTo(secondContent));
                Assert.That(dispatcher.GetPeakActiveSessionCount(),
                    Is.EqualTo(2));
                Assert.That(GetLimits(server).GetTransferCount(), Is.Zero);
            });
        }
        finally
        {
            releaseTransfers.Set();
            firstClient?.Disconnect();
            secondClient?.Disconnect();
            server.StopServer();
        }
    }

    [Test]
    public void Transfer_limit_rejects_overlap_then_recovers_after_release()
    {
        string hostRoot = Path.Combine(RootPath, "limited");
        Directory.CreateDirectory(hostRoot);
        int port = ReserveAvailablePort();
        FtpVirtualHostOptions host = CreateHost(
            "limited.example", hostRoot, "alice", "secret");
        FtpServerOptions options = CreateOptions(
            new[] { port }, new[] { host });
        options.MaximumSimultaneousTransfers = 1;
        FtpServer server = CreateServer(options.VirtualHosts);
        FtpClient? firstClient = null;
        FtpClient? secondClient = null;
        using CountdownEvent firstStarted = new CountdownEvent(1);
        using ManualResetEventSlim releaseFirst =
            new ManualResetEventSlim(false);

        try
        {
            server.StartServer(options);
            firstClient = ConnectClient(
                port, "limited.example", "alice", "secret");
            secondClient = ConnectClient(
                port, "limited.example", "alice", "secret");
            byte[] firstContent = CreateContent(67);
            byte[] secondContent = Encoding.ASCII.GetBytes("second-transfer");
            Task<FtpTransferResult> firstTransfer = Task.Factory.StartNew(
                () => Upload(
                    firstClient,
                    "/first.bin",
                    firstContent,
                    new CoordinatingTransferObserver(
                        firstStarted, releaseFirst)),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Assert.That(firstStarted.Wait(5000), Is.True);
            Assert.That(GetLimits(server).GetTransferCount(), Is.EqualTo(1));
            FtpTransferResult rejected = Upload(
                secondClient, "/second.bin", secondContent, null);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.FailureMessage,
                Does.Contain("STOR").And.Contain("450"));

            releaseFirst.Set();
            Assert.That(firstTransfer.Wait(5000), Is.True);
            FtpTransferResult firstResult = firstTransfer.GetAwaiter().GetResult();
            FtpTransferResult recovered = Upload(
                secondClient, "/second.bin", secondContent, null);

            Assert.Multiple(() =>
            {
                Assert.That(firstResult.Succeeded, Is.True,
                    firstResult.FailureMessage);
                Assert.That(recovered.Succeeded, Is.True,
                    recovered.FailureMessage);
                Assert.That(GetLimits(server).GetTransferCount(), Is.Zero);
                Assert.That(File.ReadAllBytes(Path.Combine(
                    hostRoot, "first.bin")), Is.EqualTo(firstContent));
                Assert.That(File.ReadAllBytes(Path.Combine(
                    hostRoot, "second.bin")), Is.EqualTo(secondContent));
            });
        }
        finally
        {
            releaseFirst.Set();
            firstClient?.Disconnect();
            secondClient?.Disconnect();
            server.StopServer();
        }
    }

    [Test]
    public void Graceful_stop_drains_all_concurrent_listener_sessions()
    {
        int firstPort = ReserveAvailablePort();
        int secondPort = ReserveDifferentPort(firstPort);
        FtpServerOptions options = CreateOptions(
            new[] { firstPort, secondPort },
            Array.Empty<FtpVirtualHostOptions>());
        options.Timeouts.GracefulStopTimeoutMilliseconds = 5000;
        FtpServer server = CreateServer(options.VirtualHosts);
        using CountdownEvent entered = new CountdownEvent(2);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        server.Runtime.SessionOperation = context =>
        {
            entered.Signal();
            release.Wait();
            return null;
        };
        Socket? firstClient = null;
        Socket? secondClient = null;
        Task? stop = null;

        try
        {
            server.StartServer(options);
            firstClient = ConnectSocket(firstPort);
            secondClient = ConnectSocket(secondPort);
            Assert.That(entered.Wait(5000), Is.True);
            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            stop = Task.Run(() => server.StopServer());
            Assert.That(SpinWait.SpinUntil(() =>
                server.GetServerState() == FtpServerState.Draining, 5000),
                Is.True);
            Assert.That(SpinWait.SpinUntil(() =>
                server.Runtime.Listeners.Count == 0, 5000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(stop.IsCompleted, Is.False);
                Assert.That(dispatcher.GetActiveSessionCount(), Is.EqualTo(2));
                Assert.That(server.Runtime.Listeners, Is.Empty);
            });

            release.Set();
            Assert.That(stop.Wait(5000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(),
                    Is.EqualTo(FtpServerState.Stopped));
                Assert.That(dispatcher.GetCompletedSessionCount(), Is.EqualTo(2));
                Assert.That(dispatcher.GetForcedSessionCount(), Is.Zero);
                Assert.That(dispatcher.GetFailedSessionCount(), Is.Zero);
                Assert.That(GetLimits(server).GetSessionCount(), Is.Zero);
            });
        }
        finally
        {
            release.Set();
            firstClient?.Dispose();
            secondClient?.Dispose();
            if (stop != null)
            {
                stop.Wait(5000);
            }
            server.StopServer();
        }
    }

    [Test]
    public void Session_fault_is_isolated_before_forced_stop_closes_blocked_peer()
    {
        int faultPort = ReserveAvailablePort();
        int blockedPort = ReserveDifferentPort(faultPort);
        FtpServerOptions options = CreateOptions(
            new[] { faultPort, blockedPort },
            Array.Empty<FtpVirtualHostOptions>());
        options.Timeouts.GracefulStopTimeoutMilliseconds = 25;
        FtpServer server = CreateServer(options.VirtualHosts);
        using ManualResetEventSlim blockedEntered =
            new ManualResetEventSlim(false);
        using ManualResetEventSlim faultEntered =
            new ManualResetEventSlim(false);
        server.Runtime.SessionOperation = context =>
        {
            int localPort = ((IPEndPoint)context.Socket.LocalEndPoint!).Port;
            if (localPort == faultPort)
            {
                faultEntered.Set();
                throw new InvalidOperationException("isolated session fault");
            }
            blockedEntered.Set();
            byte[] buffer = new byte[1];
            context.Socket.Receive(buffer);
            return null;
        };
        Socket? blockedClient = null;
        Socket? faultClient = null;

        try
        {
            server.StartServer(options);
            blockedClient = ConnectSocket(blockedPort);
            Assert.That(blockedEntered.Wait(5000), Is.True);
            faultClient = ConnectSocket(faultPort);
            Assert.That(faultEntered.Wait(5000), Is.True);
            FtpServerSessionDispatcher dispatcher = GetDispatcher(server);
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetCompletedSessionCount() == 1, 5000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(),
                    Is.EqualTo(FtpServerState.Running));
                Assert.That(dispatcher.GetFailedSessionCount(), Is.EqualTo(1));
                Assert.That(dispatcher.GetActiveSessionCount(), Is.EqualTo(1));
                Assert.That(GetLimits(server).GetSessionCount(), Is.EqualTo(1));
            });

            server.StopServer();
            Assert.That(SpinWait.SpinUntil(() =>
                dispatcher.GetCompletedSessionCount() == 2, 5000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(server.GetServerState(),
                    Is.EqualTo(FtpServerState.Stopped));
                Assert.That(dispatcher.GetForcedSessionCount(), Is.EqualTo(1));
                Assert.That(dispatcher.GetFailedSessionCount(), Is.EqualTo(2));
                Assert.That(GetLimits(server).GetSessionCount(), Is.Zero);
                Assert.That(server.Runtime.Listeners, Is.Empty);
            });
        }
        finally
        {
            blockedClient?.Dispose();
            faultClient?.Dispose();
            server.StopServer();
        }
    }

    private static FtpServer CreateServer(FtpVirtualHostOptions[] hosts)
    {
        string fallbackRoot = Path.GetTempPath();
        return new FtpServer(
            new LocalFtpFileSystem(fallbackRoot),
            new FixedUserFtpAuthenticator(CreateUsers("fallback", "secret")),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(hosts),
            new UnavailableCertificateProvider(),
            new NullFtpEventSink());
    }

    private static FtpServerOptions CreateOptions(
        int[] ports,
        FtpVirtualHostOptions[] hosts)
    {
        FtpListenerOptions[] listeners = new FtpListenerOptions[ports.Length];
        int index = 0;
        while (index < ports.Length)
        {
            FtpListenerOptions listener = new FtpListenerOptions();
            listener.Endpoint.Host = IPAddress.Loopback.ToString();
            listener.Endpoint.Port = ports[index];
            listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
            listener.SecurityMode = FtpSecurityMode.Plain;
            listener.AllowPlaintextCredentials = true;
            listener.DualStack = false;
            listeners[index] = listener;
            index++;
        }
        FtpServerOptions options = new FtpServerOptions();
        options.Listeners = listeners;
        options.VirtualHosts = hosts;
        options.MaximumPendingConnections = 8;
        options.MaximumSessions = 8;
        options.MaximumSessionsPerAddress = 8;
        options.MaximumSessionsPerUser = 8;
        options.MaximumSimultaneousTransfers = 8;
        options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
        return options;
    }

    private static FtpVirtualHostOptions CreateHost(
        string hostName,
        string rootPath,
        string userName,
        string password)
    {
        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = hostName;
        host.Greeting = hostName + " ready";
        host.RootPath = "/";
        host.FileSystem = new LocalFtpFileSystem(rootPath);
        host.Authenticator = new FixedUserFtpAuthenticator(
            CreateUsers(userName, password));
        host.Authorizer = new AllowAllFtpAuthorizer();
        host.MaximumSessions = 8;
        host.MaximumSessionsPerUser = 8;
        return host;
    }

    private static FtpFixedUserOptions CreateUsers(
        string userName,
        string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes(userName + "-salt");
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] salted = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, salted, 0, salt.Length);
        Buffer.BlockCopy(
            passwordBytes, 0, salted, salt.Length, passwordBytes.Length);
        FtpUserRecord user = new FtpUserRecord();
        user.UserName = userName;
        user.Salt = salt;
        user.PasswordVerifier = SHA256.HashData(salted);
        user.HomeDirectory = "/";
        user.Disabled = false;
        user.AllowWrite = true;
        FtpFixedUserOptions users = new FtpFixedUserOptions();
        users.Users = new[] { user };
        users.RequireProtectedControl = false;
        users.FailureDelayMilliseconds = 0;
        return users;
    }

    private static FtpClient ConnectClient(
        int port,
        string hostName,
        string userName,
        string password)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.VirtualHost = hostName;
        options.Credentials.UserName = userName;
        options.Credentials.Password = password;
        options.AllowPlaintextCredentials = true;
        options.DataConnection.Mode = FtpDataConnectionMode.Active;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        Assert.That(client.GetSessionState(),
            Is.EqualTo(FtpSessionState.Authenticated));
        return client;
    }

    private static FtpTransferResult Upload(
        FtpClient client,
        string path,
        byte[] content,
        IFtpTransferObserver? observer)
    {
        using MemoryStream source = new MemoryStream(content);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = path;
        request.Content = source;
        request.Options.BufferBytes = 1024;
        request.Options.VerificationMode = FtpVerificationMode.None;
        request.Observer = observer;
        return client.Transfer(request);
    }

    private static byte[] Download(FtpClient client, string path)
    {
        using MemoryStream destination = new MemoryStream();
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Download;
        request.RemotePath = path;
        request.Content = destination;
        request.Options.VerificationMode = FtpVerificationMode.None;
        FtpTransferResult result = client.Transfer(request);
        Assert.That(result.Succeeded, Is.True, result.FailureMessage);
        return destination.ToArray();
    }

    private static byte[] CreateContent(byte value)
    {
        byte[] content = new byte[1024 * 1024];
        Array.Fill(content, value);
        return content;
    }

    private static Socket ConnectSocket(int port)
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

    private static int ReserveDifferentPort(int excluded)
    {
        int port = ReserveAvailablePort();
        while (port == excluded)
        {
            port = ReserveAvailablePort();
        }
        return port;
    }

    private static FtpServerSessionDispatcher GetDispatcher(FtpServer server)
    {
        FtpServerSessionDispatcher? dispatcher = server.Runtime.SessionDispatcher;
        Assert.That(dispatcher, Is.Not.Null);
        return dispatcher!;
    }

    private static FtpServerLimitManager GetLimits(FtpServer server)
    {
        FtpServerLimitManager? limits = server.Runtime.Limits;
        Assert.That(limits, Is.Not.Null);
        return limits!;
    }

    private sealed class CoordinatingTransferObserver : IFtpTransferObserver
    {
        private CountdownEvent Started;
        private ManualResetEventSlim Release;
        private int Signaled;

        public CoordinatingTransferObserver(
            CountdownEvent started,
            ManualResetEventSlim release)
        {
            Started = started;
            Release = release;
            Signaled = 0;
        }

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
            if (Interlocked.Exchange(ref Signaled, 1) == 0)
            {
                Started.Signal();
                Release.Wait();
            }
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
        }
    }

    private sealed class TransferCountObserver : IFtpTransferObserver
    {
        private FtpServerLimitManager Limits;
        private int ExpectedCount;
        public bool ObservedExpectedCount;

        public TransferCountObserver(
            FtpServerLimitManager limits,
            int expectedCount)
        {
            Limits = limits;
            ExpectedCount = expectedCount;
            ObservedExpectedCount = false;
        }

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
            ObservedExpectedCount =
                Limits.GetTransferCount() == ExpectedCount;
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
        }
    }

    private sealed class UnavailableCertificateProvider : IFtpCertificateProvider
    {
        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            FtpCertificateResult result = new FtpCertificateResult();
            result.Handled = true;
            result.Succeeded = false;
            result.FailureMessage = "TLS is unavailable in this plain FTP test.";
            return result;
        }
    }
}
