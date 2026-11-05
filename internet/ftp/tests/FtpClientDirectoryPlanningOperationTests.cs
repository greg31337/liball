using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientDirectoryPlanningOperationTests
{
    [Test]
    public void Upload_update_dry_run_compares_trees_without_mutating_them()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(Path.Combine(localRoot, "empty"));
        File.WriteAllText(Path.Combine(localRoot, "changed.txt"), "local");
        File.WriteAllText(Path.Combine(localRoot, "new.txt"), "new");
        File.WriteAllText(Path.Combine(localRoot, "same.txt"), "same");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "same.txt"), timestamp);
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
                "Type=file;Size=3;Modify=20240830110000; changed.txt\r\n" +
                "Type=file;Size=6;Modify=20240830110000; remote-only.txt\r\n" +
                "Type=file;Size=4;Modify=20240830120000; same.txt\r\n",
                "MLSD /remote");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Update;
        request.Options.DryRun = true;

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
            Assert.That(result.TotalItems, Is.EqualTo(5));
            Assert.That(result.SucceededItems, Is.Zero);
            Assert.That(result.LogicalBytes, Is.Zero);
            Assert.That(result.Items.All(item => item.Executed == false), Is.True);
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("same.txt"))
                .Skipped, Is.True);
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("changed.txt"))
                .Skipped, Is.False);
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("new.txt"))
                .Message, Does.Contain("uploaded"));
            Assert.That(result.Items.Single(item => item.RemotePath.EndsWith("empty"))
                .EntryType, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(result.Items.Any(item =>
                item.RemotePath.EndsWith("remote-only.txt")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(localRoot, "changed.txt")),
                Is.EqualTo("local"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Download_mirror_plan_orders_deletions_and_marks_filter_exclusions()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        string extraDirectory = Path.Combine(localRoot, "extra");
        Directory.CreateDirectory(extraDirectory);
        File.WriteAllText(Path.Combine(extraDirectory, "child.txt"), "child");
        File.WriteAllText(Path.Combine(localRoot, "local-only.txt"), "keep");
        File.WriteAllText(Path.Combine(localRoot, "same.txt"), "same");
        DateTime timestamp = new DateTime(
            2024, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(localRoot, "same.txt"), timestamp);
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
                "Type=file;Size=3;Modify=20240830110000; new.txt\r\n" +
                "Type=file;Size=4;Modify=20240830120000; same.txt\r\n",
                "MLSD /remote");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        DeletionFilter filter = new DeletionFilter("local-only.txt");
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Download, "/remote", localRoot);
        request.Options.SyncMode = FtpDirectorySyncMode.Mirror;
        request.Options.DeleteExtraneous = true;
        request.Options.DryRun = true;
        request.Options.Filter = filter;

        FtpDirectoryPlan plan = client.PlanDirectoryTransfer(request);
        server.GetAwaiter().GetResult();

        FtpDirectoryItemResult[] deletions = plan.Items
            .Where(item => item.Deleted).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /remote",
            }));
            Assert.That(plan.Complete, Is.True);
            Assert.That(plan.Items, Has.Length.EqualTo(6));
            Assert.That(plan.Items.Single(item => item.RemotePath.EndsWith("new.txt"))
                .Message, Does.Contain("downloaded"));
            Assert.That(plan.Items.Single(item => item.RemotePath.EndsWith("same.txt"))
                .Skipped, Is.True);
            Assert.That(plan.Items.Single(item => item.LocalPath.EndsWith("local-only.txt"))
                .Skipped, Is.True);
            Assert.That(deletions.Select(item => Path.GetFileName(item.LocalPath)),
                Is.EqualTo(new[] { "child.txt", "extra" }));
            Assert.That(filter.SawDeletion, Is.True);
            Assert.That(File.Exists(Path.Combine(localRoot, "local-only.txt")),
                Is.True);
            Assert.That(File.Exists(Path.Combine(extraDirectory, "child.txt")),
                Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Filter_fault_is_isolated_as_a_failed_plan_item()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "file.txt"), "data");
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream, listingListener, commands, "", "MLSD /remote");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FaultingTransferFilter filter = new FaultingTransferFilter();
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/remote", localRoot);
        request.Options.DryRun = true;
        request.Options.Filter = filter;

        FtpDirectoryPlan plan = client.PlanDirectoryTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(filter.Requests, Has.Count.EqualTo(1));
            Assert.That(filter.Requests[0].Direction,
                Is.EqualTo(FtpTransferDirection.Upload));
            Assert.That(filter.Requests[0].Entry.Name,
                Is.EqualTo("file.txt"));
            Assert.That(filter.Requests[0].Depth, Is.EqualTo(1));
            Assert.That(plan.Complete, Is.False);
            Assert.That(plan.FailureMessage,
                Is.EqualTo("The recursive transfer filter failed."));
            Assert.That(plan.Items.Single(item =>
                item.LocalPath.EndsWith("file.txt")).Message,
                Is.EqualTo("The recursive transfer filter failed."));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Missing_remote_upload_root_is_planned_without_MKD_or_STOR()
    {
        string directory = CreateTemporaryDirectory();
        string localRoot = Path.Combine(directory, "root");
        Directory.CreateDirectory(localRoot);
        File.WriteAllText(Path.Combine(localRoot, "file.txt"), "data");
        using TcpListener controlListener = StartListener();
        using TcpListener listingListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            RejectListing(
                stream, listingListener, commands, "LIST /missing");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = false;
        client.State.FeatureState.ExtendedPassive = true;
        FtpDirectoryTransferRequest request = CreateRequest(
            FtpTransferDirection.Upload, "/missing", localRoot);
        request.Options.DryRun = true;

        FtpDirectoryPlan plan = client.PlanDirectoryTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "LIST /missing",
            }));
            Assert.That(plan.Complete, Is.True);
            Assert.That(plan.Items, Has.Length.EqualTo(2));
            Assert.That(plan.Items[0].EntryType,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(plan.Items[0].Message, Does.Contain("created"));
            Assert.That(plan.Items[1].Message, Does.Contain("uploaded"));
            Assert.That(commands.Any(command => command.StartsWith("MKD")), Is.False);
            Assert.That(commands.Any(command => command.StartsWith("STOR")), Is.False);
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
            Path.GetTempPath(), "ftp-directory-plan-" +
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

    private static void RejectListing(
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
        WriteText(control, "550 Directory missing\r\n");
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

    private class DeletionFilter : IFtpTransferFilter
    {
        public string ExcludedDeletionName;
        public bool SawDeletion;

        public DeletionFilter(string excludedDeletionName)
        {
            ExcludedDeletionName = excludedDeletionName;
            SawDeletion = false;
        }

        public bool IncludeTransferEntry(FtpTransferFilterRequest request)
        {
            if (request.Deletion)
            {
                SawDeletion = true;
                return request.Entry.Name != ExcludedDeletionName;
            }
            return true;
        }
    }

    private class FaultingTransferFilter : IFtpTransferFilter
    {
        public List<FtpTransferFilterRequest> Requests;

        public FaultingTransferFilter()
        {
            Requests = new List<FtpTransferFilterRequest>();
        }

        public bool IncludeTransferEntry(FtpTransferFilterRequest request)
        {
            Requests.Add(request);
            throw new InvalidOperationException("transfer filter fault");
        }
    }
}
