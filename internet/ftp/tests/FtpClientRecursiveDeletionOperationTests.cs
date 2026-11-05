using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientRecursiveDeletionOperationTests
{
    [Test]
    public void Recursive_deletion_requires_opt_in_and_rejects_the_FTP_root()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(listener);
        server.GetAwaiter().GetResult();
        FtpDirectoryTransferRequest request = CreateRequest("/tree");
        request.Options.AllowRecursiveDeletion = false;

        Assert.Throws<FtpConfigurationException>(() =>
            client.DeleteDirectoryTree(request));
        request.Options.AllowRecursiveDeletion = true;
        request.RemotePath = "/./";
        Assert.Throws<FtpConfigurationException>(() =>
            client.DeleteDirectoryTree(request));

        Assert.That(commands, Is.EqualTo(new[] { "USER alice", "PASS secret" }));
        client.AbortConnection();
    }

    [Test]
    public void Recursive_deletion_is_deterministic_and_post_order()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener rootDataListener = StartListener();
        using TcpListener childDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                rootDataListener,
                commands,
                "Type=dir;Unique=child-id; sub\r\n" +
                "Type=file; b.txt\r\n" +
                "Type=file; a.txt\r\n");
            Receive(stream, commands, "250 a deleted\r\n");
            Receive(stream, commands, "250 b deleted\r\n");
            SendListing(
                stream,
                childDataListener,
                commands,
                "Type=file; inner.txt\r\n");
            Receive(stream, commands, "250 inner deleted\r\n");
            Receive(stream, commands, "250 sub removed\r\n");
            Receive(stream, commands, "250 root removed\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);

        FtpDirectoryTransferResult result =
            client.DeleteDirectoryTree(CreateRequest("/tree"));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
                "EPSV", "MLSD /tree", "DELE /tree/a.txt", "DELE /tree/b.txt",
                "EPSV", "MLSD /tree/sub", "DELE /tree/sub/inner.txt",
                "RMD /tree/sub", "RMD /tree",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TotalItems, Is.EqualTo(5));
            Assert.That(result.DeletedItems, Is.EqualTo(5));
            Assert.That(result.FailedItems, Is.Zero);
            Assert.That(result.Items.Select(item => item.RemotePath), Is.EqualTo(new[]
            {
                "/tree/a.txt", "/tree/b.txt", "/tree/sub/inner.txt",
                "/tree/sub", "/tree",
            }));
            Assert.That(result.Items.All(item => item.CommandResult != null), Is.True);
            Assert.That(result.Plan.Complete, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Continue_policy_reports_each_server_authorization_result()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendListing(
                stream,
                dataListener,
                commands,
                "Type=file; denied.txt\r\nType=file; permitted.txt\r\n");
            Receive(stream, commands, "550 Delete denied by policy\r\n");
            Receive(stream, commands, "250 File deleted\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        EnableMachineListings(client);
        FtpDirectoryTransferRequest request = CreateRequest("/tree");
        request.Options.FailurePolicy = FtpFailurePolicy.Continue;

        FtpDirectoryTransferResult result = client.DeleteDirectoryTree(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "MLSD /tree", "DELE /tree/denied.txt",
                "DELE /tree/permitted.txt",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DeletedItems, Is.EqualTo(1));
            Assert.That(result.FailedItems, Is.EqualTo(1));
            Assert.That(result.SkippedItems, Is.EqualTo(1));
            Assert.That(result.Items[0].CommandResult!.FinalReply!.Code, Is.EqualTo(550));
            Assert.That(result.Items[0].Message, Does.Contain("reply 550"));
            Assert.That(result.Items[1].CommandResult!.FinalReply!.Code, Is.EqualTo(250));
            Assert.That(result.Items[2].EntryType, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(result.Items[2].Executed, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Follow_policy_detects_a_symbolic_link_cycle_without_deleting_it()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            SendHumanListing(
                stream,
                dataListener,
                commands,
                "lrwxrwxrwx 1 owner group 5 Jan 01 12:00 loop -> /tree\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = false;
        client.State.FeatureState.ExtendedPassive = true;
        FtpDirectoryTransferRequest request = CreateRequest("/tree");
        request.Options.SymlinkPolicy = FtpSymlinkPolicy.Follow;

        FtpDirectoryTransferResult result = client.DeleteDirectoryTree(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "EPSV", "LIST /tree",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DeletedItems, Is.Zero);
            Assert.That(result.Items[0].EntryType, Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(result.Items[0].Message, Does.Contain("cycle"));
            Assert.That(result.Items[^1].Skipped, Is.True);
        });

        client.AbortConnection();
    }

    private static FtpDirectoryTransferRequest CreateRequest(string path)
    {
        FtpDirectoryTransferRequest request = new FtpDirectoryTransferRequest();
        request.RemotePath = path;
        request.Options.AllowRecursiveDeletion = true;
        return request;
    }

    private static void EnableMachineListings(FtpClient client)
    {
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = true;
        client.State.FeatureState.ExtendedPassive = true;
    }

    private static void SendListing(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string listing)
    {
        PreparePassive(controlStream, dataListener, commands);
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening listing\r\n");
        using (NetworkStream dataStream = data.GetStream())
        {
            WriteText(dataStream, listing);
        }
        WriteText(controlStream, "226 Listing complete\r\n");
    }

    private static void SendHumanListing(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string listing)
    {
        SendListing(controlStream, dataListener, commands, listing);
    }

    private static void PreparePassive(
        NetworkStream stream,
        TcpListener dataListener,
        List<string> commands)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
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
        FtpAuthenticationResult authentication = client.AuthenticateUser(credentials);
        Assert.That(authentication.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(NetworkStream stream, List<string> commands)
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

    private static string ReadCommand(NetworkStream stream)
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
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static void WriteText(NetworkStream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
