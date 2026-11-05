using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientDirectoryDownloadOperationTests
{
    [Test]
    public void Recursive_download_preserves_empty_directories_filters_and_verifies_files()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        using TcpListener controlListener = StartListener();
        using TcpListener rootListingListener = StartListener();
        using TcpListener emptyListingListener = StartListener();
        using TcpListener fileListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                rootListingListener,
                commands,
                "Type=file;Size=4;Modify=20240830120000; skip.txt\r\n" +
                "Type=dir;Modify=20240830110000; empty\r\n" +
                "Type=file;Size=4;Modify=20240830120000; keep.txt\r\n",
                "MLSD /remote");
            SendListing(
                stream,
                emptyListingListener,
                commands,
                "",
                "MLSD /remote/empty");
            Receive(stream, commands, "200 Type selected\r\n");
            SendFile(
                stream, fileListener, commands, "data", "RETR /remote/keep.txt");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.Filter = new NameFilter("skip.txt");
        request.Options.Transfer.PreserveModifiedTime = true;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        string emptyPath = Path.Combine(localRoot, "empty");
        string keepPath = Path.Combine(localRoot, "keep.txt");
        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
                "EPSV", "MLSD /remote",
                "EPSV", "MLSD /remote/empty",
                "TYPE I", "EPSV", "RETR /remote/keep.txt",
            }));
            Assert.That(Directory.Exists(emptyPath), Is.True);
            Assert.That(File.ReadAllText(keepPath), Is.EqualTo("data"));
            Assert.That(File.Exists(Path.Combine(localRoot, "skip.txt")), Is.False);
            Assert.That(File.GetLastWriteTimeUtc(keepPath),
                Is.EqualTo(new DateTime(2024, 8, 30, 12, 0, 0, DateTimeKind.Utc)));
            Assert.That(Directory.GetLastWriteTimeUtc(emptyPath),
                Is.EqualTo(new DateTime(2024, 8, 30, 11, 0, 0, DateTimeKind.Utc)));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(4));
            Assert.That(result.SucceededItems, Is.EqualTo(3));
            Assert.That(result.SkippedItems, Is.EqualTo(1));
            Assert.That(result.FailedItems, Is.Zero);
            Assert.That(result.LogicalBytes, Is.EqualTo(4));
            Assert.That(result.Items.Single(item => item.EntryType == FtpEntryType.File &&
                item.Succeeded).TransferResult!.Verified, Is.True);
            Assert.That(result.Plan.Complete, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Continue_policy_reports_collision_and_downloads_later_files()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "a.txt"), "existing");
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        using TcpListener fileListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                listingListener,
                commands,
                "Type=file;Size=3; a.txt\r\n" +
                "Type=file;Size=3; b.txt\r\n",
                "MLSD /remote");
            Receive(stream, commands, "200 Type selected\r\n");
            SendFile(
                stream, fileListener, commands, "new", "RETR /remote/b.txt");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.FailurePolicy = FtpFailurePolicy.Continue;
        request.Options.Transfer.CollisionPolicy = FtpCollisionPolicy.Fail;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote", "TYPE I", "EPSV",
                "RETR /remote/b.txt",
            }));
            Assert.That(File.ReadAllText(Path.Combine(localRoot, "a.txt")),
                Is.EqualTo("existing"));
            Assert.That(File.ReadAllText(Path.Combine(localRoot, "b.txt")),
                Is.EqualTo("new"));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.TotalItems, Is.EqualTo(3));
            Assert.That(result.SucceededItems, Is.EqualTo(2));
            Assert.That(result.FailedItems, Is.EqualTo(1));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("a.txt"))
                .Message, Does.Contain("already exists"));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("b.txt"))
                .Succeeded, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Recursive_download_honors_cancellation_before_traversal()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        using ManualResetEventSlim releaseServer = new ManualResetEventSlim(false);
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            releaseServer.Wait();
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.Transfer.Timeouts.CancellationToken = cancellation.Token;

        OperationCanceledException? error;
        try
        {
            error = Assert.Throws<OperationCanceledException>(() =>
                client.TransferDirectory(request));
        }
        finally
        {
            releaseServer.Set();
        }
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Not.Null);
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
            Assert.That(Directory.Exists(localRoot), Is.False);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Symlink_skip_policy_does_not_materialize_or_follow_the_link()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                listingListener,
                commands,
                "lrwxrwxrwx 1 owner group 10 Jan 01 12:00 link -> target.txt\r\n",
                "LIST /remote");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = false;
        client.State.FeatureState.ExtendedPassive = true;
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.SymlinkPolicy = FtpSymlinkPolicy.Skip;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "LIST /remote",
            }));
            Assert.That(Directory.Exists(localRoot), Is.True);
            Assert.That(File.Exists(Path.Combine(localRoot, "link")), Is.False);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(2));
            Assert.That(result.SkippedItems, Is.EqualTo(1));
            Assert.That(result.Items[1].EntryType,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(result.Items[1].Skipped, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Symlink_preserve_policy_creates_a_safe_local_relative_link()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        using TcpListener fileListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                listingListener,
                commands,
                "lrwxrwxrwx 1 owner group 10 Jan 01 12:00 link -> target.txt\r\n" +
                "-rw-r--r-- 1 owner group 6 Jan 01 12:00 target.txt\r\n",
                "LIST /remote");
            Receive(
                stream,
                commands,
                "250-Listing\r\n Type=file;Size=6; /remote/target.txt\r\n" +
                "250 End\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            SendFile(
                stream,
                fileListener,
                commands,
                "linked",
                "RETR /remote/target.txt");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = false;
        client.State.FeatureState.MlstAdvertised = true;
        client.State.FeatureState.ExtendedPassive = true;
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.SymlinkPolicy = FtpSymlinkPolicy.Preserve;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        string linkPath = Path.Combine(localRoot, "link");
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "LIST /remote", "MLST /remote/target.txt",
                "TYPE I", "EPSV", "RETR /remote/target.txt",
            }));
            Assert.That(File.ReadAllText(linkPath), Is.EqualTo("linked"));
            Assert.That(new FileInfo(linkPath).LinkTarget,
                Is.EqualTo("target.txt"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(3));
            Assert.That(result.Items.Single(item =>
                item.EntryType == FtpEntryType.SymbolicLink).Succeeded, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    private static FtpDirectoryTransferRequest CreateRequest(
        string remotePath,
        string localPath)
    {
        FtpDirectoryTransferRequest request = new FtpDirectoryTransferRequest();
        request.RemotePath = remotePath;
        request.LocalPath = localPath;
        request.Options.Transfer.CollisionPolicy = FtpCollisionPolicy.Fail;
        return request;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "ftp-directory-download-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void EnableMachineListings(FtpClient client)
    {
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = true;
        client.State.FeatureState.ExtendedPassive = true;
    }

    private static void SendListing(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        string listing,
        string expectedCommand)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        string command = ReadCommand(control);
        commands.Add(command);
        Assert.That(command, Is.EqualTo(expectedCommand));
        WriteText(control, "150 Opening listing\r\n");
        WriteText(data.GetStream(), listing);
        data.Client.Shutdown(SocketShutdown.Send);
        WriteText(control, "226 Listing complete\r\n");
    }

    private static void SendFile(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        string content,
        string expectedCommand)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        string command = ReadCommand(control);
        commands.Add(command);
        Assert.That(command, Is.EqualTo(expectedCommand));
        WriteText(control, "150 Opening data\r\n");
        WriteText(data.GetStream(), content);
        data.Client.Shutdown(SocketShutdown.Send);
        WriteText(control, "226 Transfer complete\r\n");
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static int GetPort(TcpListener listener)
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static FtpClient ConnectAuthenticated(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        FtpAuthenticationResult authentication =
            client.AuthenticateUser(credentials);
        Assert.That(authentication.Status,
            Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(
        NetworkStream stream,
        List<string> commands)
    {
        WriteText(stream, "220 Ready\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void Receive(
        NetworkStream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, reply);
    }

    private static string ReadCommand(Stream stream)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }
            if (value == '\n')
            {
                break;
            }
            if (value != '\r')
            {
                bytes.Add((byte)value);
            }
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private class NameFilter : IFtpTransferFilter
    {
        public string ExcludedName;

        public NameFilter(string excludedName)
        {
            ExcludedName = excludedName;
        }

        public bool IncludeTransferEntry(FtpTransferFilterRequest request)
        {
            return request.Entry.Name != ExcludedName;
        }
    }
}
