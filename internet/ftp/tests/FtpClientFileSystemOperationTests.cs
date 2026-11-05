using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientFileSystemOperationTests
{
    [Test]
    public void Single_path_operations_validate_and_send_MKD_RMD_and_DELE()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/new\" created\r\n");
            Receive(stream, commands, "250 Directory removed\r\n");
            Receive(stream, commands, "250 File deleted\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        Assert.Throws<FtpConfigurationException>(() => client.CreateDirectory(" "));
        Assert.Throws<FtpConfigurationException>(() => client.RemoveDirectory("bad\npath"));
        Assert.Throws<FtpConfigurationException>(() => client.DeleteFile(""));
        FtpCommandResult created = client.CreateDirectory("/new");
        FtpCommandResult removed = client.RemoveDirectory("/old");
        FtpCommandResult deleted = client.DeleteFile("/old.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MKD /new", "RMD /old",
                "DELE /old.txt",
            }));
            Assert.That(created.Succeeded, Is.True);
            Assert.That(created.FinalReply!.Code, Is.EqualTo(257));
            Assert.That(removed.Succeeded, Is.True);
            Assert.That(deleted.Succeeded, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Rename_sends_RNTO_only_after_RNFR_readiness()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "350 Source accepted\r\n");
            Receive(stream, commands, "250 Rename complete\r\n");
            Receive(stream, commands, "550 Source missing\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpCommandResult[] renamed = client.RenamePath("/old.txt", "/new.txt");
        FtpCommandResult[] missing = client.RenamePath("/missing.txt", "/unused.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "RNFR /old.txt", "RNTO /new.txt",
                "RNFR /missing.txt",
            }));
            Assert.That(renamed, Has.Length.EqualTo(2));
            Assert.That(renamed[0].FinalReply!.Code, Is.EqualTo(350));
            Assert.That(renamed[1].Succeeded, Is.True);
            Assert.That(missing, Has.Length.EqualTo(1));
            Assert.That(missing[0].Succeeded, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Rename_returns_destination_collision_as_the_RNTO_result()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "350 Source accepted\r\n");
            Receive(stream, commands, "553 Destination exists\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpCommandResult[] results = client.RenamePath("from", "to");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results[1].Succeeded, Is.False);
            Assert.That(results[1].FinalReply!.Code, Is.EqualTo(553));
            Assert.That(client.State.Session.State.RenameSourceReady, Is.False);
            Assert.That(client.State.Session.State.RenameSource, Is.Null);
        });

        client.AbortConnection();
    }

    [Test]
    public void Parent_tree_creates_each_missing_relative_directory()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "550 Directory missing\r\n");
            Receive(stream, commands, "257 \"/home/reports\" created\r\n");
            Receive(stream, commands, "550 Directory missing\r\n");
            Receive(stream, commands, "257 \"/home/reports/2026\" created\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpCommandResult[] results = client.CreateParentDirectories("reports/2026");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /home/reports", "MKD /home/reports",
                "CWD /home/reports/2026", "MKD /home/reports/2026",
            }));
            Assert.That(results, Has.Length.EqualTo(5));
            Assert.That(results.Select(result => result.Command.Kind), Is.EqualTo(new[]
            {
                FtpCommandKind.Pwd, FtpCommandKind.Cwd, FtpCommandKind.Mkd,
                FtpCommandKind.Cwd, FtpCommandKind.Mkd,
            }));
            Assert.That(results[2].Succeeded, Is.True);
            Assert.That(results[4].Succeeded, Is.True);
            Assert.That(client.State.Session.State.CurrentDirectory, Is.EqualTo("/home"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Parent_tree_restores_after_existing_directories_and_create_collisions()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "250 Existing directory selected\r\n");
            Receive(stream, commands, "250 Original directory restored\r\n");
            Receive(stream, commands, "550 Directory missing\r\n");
            Receive(stream, commands, "550 Directory was concurrently created\r\n");
            Receive(stream, commands, "250 Concurrent directory selected\r\n");
            Receive(stream, commands, "250 Original directory restored\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpCommandResult[] results =
            client.CreateParentDirectories("existing/new");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /home/existing", "CWD /home",
                "CWD /home/existing/new", "MKD /home/existing/new",
                "CWD /home/existing/new", "CWD /home",
            }));
            Assert.That(results, Has.Length.EqualTo(7));
            Assert.That(results[4].Succeeded, Is.False);
            Assert.That(results[5].Succeeded, Is.True);
            Assert.That(results[6].Succeeded, Is.True);
            Assert.That(client.State.Session.State.CurrentDirectory, Is.EqualTo("/home"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_parent_probe_restore_closes_the_session()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "250 Existing directory selected\r\n");
            Receive(stream, commands, "550 Restore denied\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            client.CreateParentDirectories("existing"));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("restore"));
            Assert.That(commands[^1], Is.EqualTo("CWD /home"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
        });
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
