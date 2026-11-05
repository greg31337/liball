using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientDirectoryUploadOperationTests
{
    [Test]
    public void Recursive_upload_creates_parents_filters_preserves_timestamps_and_verifies_files()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        string emptyPath = Path.Combine(localRoot, "empty");
        Directory.CreateDirectory(emptyPath);
        File.WriteAllText(Path.Combine(localRoot, "keep.txt"), "data");
        File.WriteAllText(Path.Combine(localRoot, "skip.txt"), "omit");
        DateTime rootTime = new DateTime(
            2024, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        DateTime emptyTime = new DateTime(
            2024, 8, 30, 11, 0, 0, DateTimeKind.Utc);
        DateTime fileTime = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "keep.txt"), fileTime);
        Directory.SetLastWriteTimeUtc(emptyPath, emptyTime);
        Directory.SetLastWriteTimeUtc(localRoot, rootTime);
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "250 Directory changed\r\n");
            Receive(stream, commands, "250 Directory restored\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote/empty\" created\r\n");
            Receive(stream, commands, "213 Timestamp set\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands,
                "150 Opening data\r\n", "226 Transfer complete\r\n"));
            Receive(stream, commands, "213 Timestamp set\r\n");
            Receive(stream, commands, "213 4\r\n");
            Receive(stream, commands, "213 Timestamp set\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableUploadFeatures(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.Filter = new NameFilter("skip.txt");
        request.Options.Transfer.PreserveModifiedTime = true;
        request.Options.Transfer.VerificationMode = FtpVerificationMode.Size;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote",
                "PWD", "CWD /remote", "CWD /",
                "CWD /remote/empty", "MKD /remote/empty",
                "MFMT 20240830110000 /remote/empty",
                "TYPE I", "EPSV", "STOR /remote/keep.txt",
                "MFMT 20240830120000 /remote/keep.txt",
                "SIZE /remote/keep.txt",
                "MFMT 20240830100000 /remote",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "data" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(4));
            Assert.That(result.SucceededItems, Is.EqualTo(3));
            Assert.That(result.SkippedItems, Is.EqualTo(1));
            Assert.That(result.FailedItems, Is.Zero);
            Assert.That(result.LogicalBytes, Is.EqualTo(4));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("keep.txt"))
                .TransferResult!.Verified, Is.True);
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("skip.txt"))
                .Skipped, Is.True);
            Assert.That(result.Plan.Complete, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Collision_skip_does_not_open_a_data_transfer()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "exists.txt"), "local");
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
            Receive(
                stream, commands,
                "250-Listing\r\n Type=file;Size=6; /remote/exists.txt\r\n" +
                "250 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MlstAdvertised = true;
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.Transfer.CollisionPolicy = FtpCollisionPolicy.Skip;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote",
                "MLST /remote/exists.txt",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(2));
            Assert.That(result.SucceededItems, Is.EqualTo(1));
            Assert.That(result.SkippedItems, Is.EqualTo(1));
            Assert.That(result.Items[1].TransferResult, Is.Not.Null);
            Assert.That(result.Items[1].Skipped, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Continue_policy_reports_a_failed_upload_and_uploads_later_files()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "a.txt"), "bad");
        File.WriteAllText(Path.Combine(localRoot, "b.txt"), "new");
        using TcpListener controlListener = StartListener();
        using TcpListener rejectedListener = StartListener();
        using TcpListener acceptedListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            RejectUpload(stream, rejectedListener, commands);
            uploads.Add(ReceiveUpload(
                stream, acceptedListener, commands,
                "150 Opening data\r\n", "226 Transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.FeatureState.ExtendedPassive = true;
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.FailurePolicy = FtpFailurePolicy.Continue;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote", "TYPE I",
                "EPSV", "STOR /remote/a.txt",
                "EPSV", "STOR /remote/b.txt",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "new" }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.TotalItems, Is.EqualTo(3));
            Assert.That(result.SucceededItems, Is.EqualTo(2));
            Assert.That(result.FailedItems, Is.EqualTo(1));
            Assert.That(result.LogicalBytes, Is.EqualTo(3));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("a.txt"))
                .Message, Does.Contain("STOR"));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("b.txt"))
                .Succeeded, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Stop_policy_halts_after_recursive_upload_verification_failure()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "a.txt"), "bad");
        File.WriteAllText(Path.Combine(localRoot, "b.txt"), "later");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands,
                "150 Opening data\r\n", "226 Transfer complete\r\n"));
            Receive(stream, commands, "213 2\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableUploadFeatures(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.FailurePolicy = FtpFailurePolicy.Stop;
        request.Options.Transfer.VerificationMode = FtpVerificationMode.Size;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        FtpDirectoryItemResult failed = result.Items.Single(item =>
            item.RemotePath.EndsWith("a.txt"));
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote", "TYPE I",
                "EPSV", "STOR /remote/a.txt", "SIZE /remote/a.txt",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "bad" }));
            Assert.That(commands.Any(item => item.Contains("b.txt")), Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.TotalItems, Is.EqualTo(2));
            Assert.That(result.FailedItems, Is.EqualTo(1));
            Assert.That(result.Plan.Complete, Is.False);
            Assert.That(failed.TransferResult, Is.Not.Null);
            Assert.That(failed.TransferResult!.VerificationAttempted, Is.True);
            Assert.That(failed.TransferResult.Verified, Is.False);
            Assert.That(failed.Message, Does.Contain("metadata verification"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Follow_policy_detects_a_local_symbolic_link_cycle()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        Directory.CreateSymbolicLink(Path.Combine(localRoot, "loop"), ".");
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.SymlinkPolicy = FtpSymlinkPolicy.Follow;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.TotalItems, Is.EqualTo(2));
            Assert.That(result.FailedItems, Is.EqualTo(1));
            Assert.That(result.Plan.Complete, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("cycle"));
            Assert.That(result.Items[1].RemotePath, Is.EqualTo("/remote/loop"));
            Assert.That(result.Items[1].Message, Does.Contain("cycle"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Symlink_skip_policy_does_not_follow_an_escaping_local_link()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        string outside = Path.Combine(directory, "outside.txt");
        File.WriteAllText(outside, "outside");
        File.CreateSymbolicLink(Path.Combine(localRoot, "link"), outside);
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/remote\" created\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpDirectoryTransferRequest request = CreateRequest(
            "/remote", localRoot);
        request.Options.SymlinkPolicy = FtpSymlinkPolicy.Skip;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /remote", "MKD /remote",
            }));
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

    private static FtpDirectoryTransferRequest CreateRequest(
        string remotePath,
        string localPath)
    {
        FtpDirectoryTransferRequest request = new FtpDirectoryTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = remotePath;
        request.LocalPath = localPath;
        request.Options.Transfer.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.Transfer.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "ftp-directory-upload-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void EnableUploadFeatures(FtpClient client)
    {
        client.State.Features.Negotiated = true;
        client.State.FeatureState.ExtendedPassive = true;
        client.State.FeatureState.FileSize = true;
        client.State.FeatureState.ModificationTime = true;
    }

    private static void RejectUpload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(control));
        WriteText(control, "550 Upload rejected\r\n");
    }

    private static string ReceiveUpload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        string preliminaryReply,
        string finalReply)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(control));
        WriteText(control, preliminaryReply);
        string content;
        using (NetworkStream dataStream = data.GetStream())
        using (MemoryStream buffer = new MemoryStream())
        {
            dataStream.CopyTo(buffer);
            content = Encoding.ASCII.GetString(buffer.ToArray());
        }
        WriteText(control, finalReply);
        return content;
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
