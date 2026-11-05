using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientDirectorySynchronizationOperationTests
{
    [Test]
    public void Upload_update_no_op_returns_deterministically_ordered_skipped_items()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "z.txt"), "z");
        File.WriteAllText(Path.Combine(localRoot, "a.txt"), "a");
        File.WriteAllText(Path.Combine(localRoot, "m.txt"), "m");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "z.txt"), timestamp);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "a.txt"), timestamp);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "m.txt"), timestamp);
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream, listingListener, commands,
                "Type=file;Size=1;Modify=20240830120000; z.txt\r\n" +
                "Type=file;Size=1;Modify=20240830120000; m.txt\r\n" +
                "Type=file;Size=1;Modify=20240830120000; a.txt\r\n",
                "MLSD /remote");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Update;
        request.Options.Transfer.VerificationMode = FtpVerificationMode.None;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "MLSD /remote",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Plan.Complete, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(4));
            Assert.That(result.SucceededItems, Is.Zero);
            Assert.That(result.SkippedItems, Is.EqualTo(4));
            Assert.That(result.FailedItems, Is.Zero);
            Assert.That(result.LogicalBytes, Is.Zero);
            Assert.That(result.Items.Select(item => item.RemotePath),
                Is.EqualTo(new[]
                {
                    "/remote", "/remote/a.txt", "/remote/m.txt", "/remote/z.txt",
                }));
            Assert.That(result.Items.All(item => item.Planned), Is.True);
            Assert.That(result.Items.All(item => !item.Executed), Is.True);
            Assert.That(result.Items.All(item => item.Skipped), Is.True);
            Assert.That(result.Items.Skip(1).All(item =>
                item.Message != null && item.Message.Contains("unchanged")), Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Upload_update_transfers_only_new_and_changed_files()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "changed.txt"), "local");
        File.WriteAllText(Path.Combine(localRoot, "new.txt"), "new");
        File.WriteAllText(Path.Combine(localRoot, "same.txt"), "same");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "same.txt"), timestamp);
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        using TcpListener changedListener = StartListener();
        using TcpListener newListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream, listingListener, commands,
                "Type=file;Size=3;Modify=20240830110000; changed.txt\r\n" +
                "Type=file;Size=4;Modify=20240830120000; same.txt\r\n",
                "MLSD /remote");
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, changedListener, commands,
                "STOR /remote/changed.txt"));
            uploads.Add(ReceiveUpload(
                stream, newListener, commands,
                "STOR /remote/new.txt"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Update;
        request.Options.Transfer.VerificationMode = FtpVerificationMode.None;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote",
                "TYPE I", "EPSV", "STOR /remote/changed.txt",
                "EPSV", "STOR /remote/new.txt",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "local", "new" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(4));
            Assert.That(result.SucceededItems, Is.EqualTo(2));
            Assert.That(result.SkippedItems, Is.EqualTo(2));
            Assert.That(result.FailedItems, Is.Zero);
            Assert.That(result.LogicalBytes, Is.EqualTo(8));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("same.txt"))
                .Executed, Is.False);
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("changed.txt"))
                .TransferResult, Is.Not.Null);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Download_mirror_transfers_source_and_deletes_local_extras_post_order()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        string extraDirectory = Path.Combine(localRoot, "extra");
        Directory.CreateDirectory(extraDirectory);
        File.WriteAllText(Path.Combine(extraDirectory, "child.txt"), "child");
        File.WriteAllText(Path.Combine(localRoot, "old.txt"), "old");
        File.WriteAllText(Path.Combine(localRoot, "same.txt"), "same");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "same.txt"), timestamp);
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
                stream, listingListener, commands,
                "Type=file;Size=3;Modify=20240830110000; new.txt\r\n" +
                "Type=file;Size=4;Modify=20240830120000; same.txt\r\n",
                "MLSD /remote");
            Receive(stream, commands, "200 Type selected\r\n");
            SendFile(
                stream, fileListener, commands,
                "new", "RETR /remote/new.txt");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Download, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.AllowRecursiveDeletion = true;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote",
                "TYPE I", "EPSV", "RETR /remote/new.txt",
            }));
            Assert.That(File.ReadAllText(Path.Combine(localRoot, "new.txt")),
                Is.EqualTo("new"));
            Assert.That(File.Exists(Path.Combine(localRoot, "old.txt")), Is.False);
            Assert.That(Directory.Exists(extraDirectory), Is.False);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(6));
            Assert.That(result.SucceededItems, Is.EqualTo(4));
            Assert.That(result.SkippedItems, Is.EqualTo(2));
            Assert.That(result.DeletedItems, Is.EqualTo(3));
            Assert.That(result.LogicalBytes, Is.EqualTo(3));
            Assert.That(result.Items.Where(item => item.Deleted)
                .Select(item => Path.GetFileName(item.LocalPath)),
                Is.EqualTo(new[] { "old.txt", "child.txt", "extra" }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Upload_mirror_deletes_remote_extras_child_before_parent()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "same.txt"), "same");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "same.txt"), timestamp);
        using TcpListener controlListener = StartListener();
        using TcpListener rootListingListener = StartListener();
        using TcpListener extraListingListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream, rootListingListener, commands,
                "Type=dir; extra\r\n" +
                "Type=file;Size=4;Modify=20240830120000; same.txt\r\n",
                "MLSD /remote");
            SendListing(
                stream, extraListingListener, commands,
                "Type=file;Size=5; child.txt\r\n",
                "MLSD /remote/extra");
            Receive(stream, commands, "250 File deleted\r\n");
            Receive(stream, commands, "250 Directory removed\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.AllowRecursiveDeletion = true;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote",
                "EPSV", "MLSD /remote/extra",
                "DELE /remote/extra/child.txt", "RMD /remote/extra",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(4));
            Assert.That(result.SucceededItems, Is.EqualTo(2));
            Assert.That(result.SkippedItems, Is.EqualTo(2));
            Assert.That(result.DeletedItems, Is.EqualTo(2));
            Assert.That(result.Items.Where(item => item.Deleted)
                .Select(item => item.RemotePath), Is.EqualTo(new[]
                {
                    "/remote/extra/child.txt", "/remote/extra",
                }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Download_mirror_replaces_a_conflicting_local_directory_safely()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        string conflictingPath = Path.Combine(localRoot, "node");
        Directory.CreateDirectory(conflictingPath);
        File.WriteAllText(Path.Combine(conflictingPath, "child.txt"), "child");
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
                stream, listingListener, commands,
                "Type=file;Size=4; node\r\n",
                "MLSD /remote");
            Receive(stream, commands, "200 Type selected\r\n");
            SendFile(
                stream, fileListener, commands,
                "file", "RETR /remote/node");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Download, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.AllowRecursiveDeletion = true;

        FtpDirectoryTransferResult result = client.TransferDirectory(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote",
                "TYPE I", "EPSV", "RETR /remote/node",
            }));
            Assert.That(Directory.Exists(conflictingPath), Is.False);
            Assert.That(File.ReadAllText(conflictingPath), Is.EqualTo("file"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.DeletedItems, Is.EqualTo(2));
            Assert.That(result.Items.Any(item => item.EntryType ==
                FtpEntryType.Directory && item.Deleted), Is.True);
            Assert.That(result.Items.Single(item => item.TransferResult != null)
                .Succeeded, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Upload_mirror_rejects_delete_extraneous_at_FTP_root()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        server.GetAwaiter().GetResult();
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/./", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.AllowRecursiveDeletion = true;

        Assert.Throws<FtpConfigurationException>(() =>
            client.TransferDirectory(request));
        Assert.That(commands, Is.EqualTo(new[]
        {
            "USER alice", "PASS secret",
        }));

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Download_mirror_rejects_delete_extraneous_at_local_filesystem_root()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.GetPathRoot(Path.GetFullPath(directory)) ?? "/";
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        server.GetAwaiter().GetResult();
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Download, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.AllowRecursiveDeletion = true;

        FtpConfigurationException? error = Assert.Throws<FtpConfigurationException>(() =>
            client.TransferDirectory(request));

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("local filesystem root"));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    private static FtpDirectoryTransferRequest CreateRequest(
        FtpTransferDirection direction,
        string remotePath,
        string localPath)
    {
        FtpDirectoryTransferRequest request = new FtpDirectoryTransferRequest();
        request.Direction = direction;
        request.RemotePath = remotePath;
        request.LocalPath = localPath;
        request.Options.Transfer.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        return request;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "ftp-directory-sync-" +
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

    private static string ReceiveUpload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        string expectedCommand)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        string command = ReadCommand(control);
        commands.Add(command);
        Assert.That(command, Is.EqualTo(expectedCommand));
        WriteText(control, "150 Opening upload\r\n");
        string content;
        using (NetworkStream dataStream = data.GetStream())
        using (MemoryStream buffer = new MemoryStream())
        {
            dataStream.CopyTo(buffer);
            content = Encoding.ASCII.GetString(buffer.ToArray());
        }
        WriteText(control, "226 Upload complete\r\n");
        return content;
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
        WriteText(control, "150 Opening download\r\n");
        WriteText(data.GetStream(), content);
        data.Client.Shutdown(SocketShutdown.Send);
        WriteText(control, "226 Download complete\r\n");
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
}
