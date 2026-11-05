using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientServerTransferOperationTests
{
    [Test]
    public void FXP_copy_append_and_unique_consume_both_replies_and_restore_sessions()
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
            int index = 0;
            while (index < 3)
            {
                Receive(stream, sourceCommands, "200 Type selected\r\n");
                Receive(stream, sourceCommands, "200 Active endpoint accepted\r\n");
                ReceiveTransfer(
                    stream, sourceCommands, "RETR /source.bin",
                    "150 Opening source\r\n", "226 Source complete\r\n");
                Receive(stream, sourceCommands, "200 Type restored\r\n");
                index++;
            }
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
            string[] storage = new[]
            {
                "STOR /target.bin", "APPE /target.bin", "STOU",
            };
            int index = 0;
            while (index < storage.Length)
            {
                Receive(stream, targetCommands, "200 Type selected\r\n");
                Receive(
                    stream, targetCommands,
                    "229 Entering Extended Passive Mode (|||41001|)\r\n");
                string preliminary = index == 2 ?
                    "150 FILE: generated-42.bin\r\n" :
                    "150 Opening target\r\n";
                ReceiveTransfer(
                    stream, targetCommands, storage[index], preliminary,
                    "226 Target complete\r\n");
                Receive(stream, targetCommands, "200 Type restored\r\n");
                index++;
            }
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        FtpServerTransferRequest request = CreateRequest(source, target);

        FtpServerTransferResult copy = source.TransferServerToServer(request);
        request.Append = true;
        FtpServerTransferResult append = source.TransferServerToServer(request);
        request.Append = false;
        request.Unique = true;
        FtpServerTransferResult unique = source.TransferServerToServer(request);
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(sourceCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPRT |1|127.0.0.1|41001|",
                "RETR /source.bin", "TYPE A N",
                "TYPE I", "EPRT |1|127.0.0.1|41001|",
                "RETR /source.bin", "TYPE A N",
                "TYPE I", "EPRT |1|127.0.0.1|41001|",
                "RETR /source.bin", "TYPE A N",
            }));
            Assert.That(targetCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPSV", "STOR /target.bin", "TYPE A N",
                "TYPE I", "EPSV", "APPE /target.bin", "TYPE A N",
                "TYPE I", "EPSV", "STOU", "TYPE A N",
            }));
            Assert.That(copy.Succeeded, Is.True);
            Assert.That(append.Succeeded, Is.True);
            Assert.That(unique.Succeeded, Is.True);
            Assert.That(unique.UniqueTargetName,
                Is.EqualTo("generated-42.bin"));
            Assert.That(copy.SourceCommands, Has.Length.EqualTo(4));
            Assert.That(copy.TargetCommands, Has.Length.EqualTo(4));
            Assert.That(copy.SourceCommands.Single(command =>
                command.Command.Kind == FtpCommandKind.Retr).Replies,
                Has.Length.EqualTo(2));
            Assert.That(copy.TargetCommands.Single(command =>
                command.Command.Kind == FtpCommandKind.Stor).Replies,
                Has.Length.EqualTo(2));
            Assert.That(source.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(target.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(source.State.Session.State.DataConnectionConfigured,
                Is.False);
            Assert.That(target.State.Session.State.DataConnectionConfigured,
                Is.False);
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    [Test]
    public void FXP_falls_back_from_EPSV_EPRT_to_PASV_PORT()
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
            Receive(stream, sourceCommands, "200 Type selected\r\n");
            Receive(stream, sourceCommands, "500 EPRT unavailable\r\n");
            Receive(stream, sourceCommands, "200 PORT accepted\r\n");
            ReceiveTransfer(
                stream, sourceCommands, "RETR /source.bin",
                "150 Opening source\r\n", "226 Source complete\r\n");
            Receive(stream, sourceCommands, "200 Type restored\r\n");
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
            Receive(stream, targetCommands, "200 Type selected\r\n");
            Receive(stream, targetCommands, "500 EPSV unavailable\r\n");
            Receive(
                stream, targetCommands,
                "227 Entering Passive Mode (127,0,0,1,156,64)\r\n");
            ReceiveTransfer(
                stream, targetCommands, "APPE /target.bin",
                "150 Opening target\r\n", "226 Target complete\r\n");
            Receive(stream, targetCommands, "200 Type restored\r\n");
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        FtpServerTransferRequest request = CreateRequest(source, target);
        request.Append = true;

        FtpServerTransferResult result =
            source.TransferServerToServer(request);
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(sourceCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPRT |1|127.0.0.1|40000|",
                "PORT 127,0,0,1,156,64", "RETR /source.bin", "TYPE A N",
            }));
            Assert.That(targetCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPSV", "PASV", "APPE /target.bin", "TYPE A N",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SourceCommands.Select(command => command.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type, FtpCommandKind.Eprt,
                    FtpCommandKind.Port, FtpCommandKind.Retr,
                    FtpCommandKind.Type,
                }));
            Assert.That(result.TargetCommands.Select(command => command.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type, FtpCommandKind.Epsv,
                    FtpCommandKind.Pasv, FtpCommandKind.Appe,
                    FtpCommandKind.Type,
                }));
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    [Test]
    public void FXP_source_rejection_drains_target_reply_and_keeps_both_sessions_usable()
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
            Receive(stream, sourceCommands, "200 Type selected\r\n");
            Receive(stream, sourceCommands, "200 Active endpoint accepted\r\n");
            Receive(stream, sourceCommands, "550 Source denied\r\n");
            Receive(stream, sourceCommands, "200 Type restored\r\n");
            Receive(stream, sourceCommands, "200 NOOP okay\r\n");
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
            Receive(stream, targetCommands, "200 Type selected\r\n");
            Receive(
                stream, targetCommands,
                "229 Entering Extended Passive Mode (|||41002|)\r\n");
            ReceiveTransfer(
                stream, targetCommands, "STOR /target.bin",
                "150 Opening target\r\n", "425 Data connection failed\r\n");
            Receive(stream, targetCommands, "200 Type restored\r\n");
            Receive(stream, targetCommands, "200 NOOP okay\r\n");
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        FtpServerTransferRequest request = CreateRequest(source, target);

        FtpServerTransferResult result =
            source.TransferServerToServer(request);
        FtpCommandResult sourceNoop = source.SendNoOperation();
        FtpCommandResult targetNoop = target.SendNoOperation();
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("RETR"));
            Assert.That(result.SourceCommands.Single(command =>
                command.Command.Kind == FtpCommandKind.Retr)
                .FinalReply!.Code, Is.EqualTo(550));
            Assert.That(result.TargetCommands.Single(command =>
                command.Command.Kind == FtpCommandKind.Stor)
                .FinalReply!.Code, Is.EqualTo(425));
            Assert.That(sourceNoop.Succeeded, Is.True);
            Assert.That(targetNoop.Succeeded, Is.True);
            Assert.That(sourceCommands.Last(), Is.EqualTo("NOOP"));
            Assert.That(targetCommands.Last(), Is.EqualTo("NOOP"));
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    [Test]
    public void FXP_target_rejection_does_not_start_source_RETR_and_restores_both_sessions()
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
            Receive(stream, sourceCommands, "200 Type selected\r\n");
            Receive(stream, sourceCommands, "200 Active endpoint accepted\r\n");
            Receive(stream, sourceCommands, "200 Type restored\r\n");
            Receive(stream, sourceCommands, "200 NOOP okay\r\n");
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
            Receive(stream, targetCommands, "200 Type selected\r\n");
            Receive(
                stream, targetCommands,
                "229 Entering Extended Passive Mode (|||41003|)\r\n");
            Receive(stream, targetCommands, "550 Target denied\r\n");
            Receive(stream, targetCommands, "200 Type restored\r\n");
            Receive(stream, targetCommands, "200 NOOP okay\r\n");
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        FtpServerTransferRequest request = CreateRequest(source, target);

        FtpServerTransferResult result =
            source.TransferServerToServer(request);
        FtpCommandResult sourceNoop = source.SendNoOperation();
        FtpCommandResult targetNoop = target.SendNoOperation();
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(sourceCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPRT |1|127.0.0.1|41003|",
                "TYPE A N", "NOOP",
            }));
            Assert.That(targetCommands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "EPSV", "STOR /target.bin", "TYPE A N", "NOOP",
            }));
            Assert.That(sourceCommands.Any(command =>
                command.StartsWith("RETR ")), Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("STOR"));
            Assert.That(result.SourceCommands.Any(command =>
                command.Command.Kind == FtpCommandKind.Retr), Is.False);
            Assert.That(result.TargetCommands.Single(command =>
                command.Command.Kind == FtpCommandKind.Stor)
                .FinalReply!.Code, Is.EqualTo(550));
            Assert.That(sourceNoop.Succeeded, Is.True);
            Assert.That(targetNoop.Succeeded, Is.True);
            Assert.That(source.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.Ascii));
            Assert.That(target.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.Ascii));
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FXP_anti_bounce_policy_requires_both_sessions_to_opt_in(
        bool restrictSource)
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();
        FtpClient restricted = restrictSource ? source : target;
        restricted.State.Options!.DataConnection.AllowForeignDataConnections = false;
        FtpServerTransferRequest request = CreateRequest(source, target);

        FtpConfigurationException? error = Assert.Throws<FtpConfigurationException>(() =>
            source.TransferServerToServer(request));

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("foreign data connections"));
            Assert.That(sourceCommands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
            Assert.That(targetCommands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FXP_rejects_protected_data_on_either_session_before_setup_commands(
        bool protectSource)
    {
        using TcpListener sourceListener = StartListener();
        using TcpListener targetListener = StartListener();
        List<string> sourceCommands = new List<string>();
        List<string> targetCommands = new List<string>();
        Task sourceServer = Task.Run(() =>
        {
            using TcpClient control = sourceListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, sourceCommands);
        });
        Task targetServer = Task.Run(() =>
        {
            using TcpClient control = targetListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, targetCommands);
        });
        FtpClient source = ConnectAuthenticated(sourceListener);
        FtpClient target = ConnectAuthenticated(targetListener);
        sourceServer.GetAwaiter().GetResult();
        targetServer.GetAwaiter().GetResult();
        FtpClient protectedClient = protectSource ? source : target;
        protectedClient.State.Session.State.DataProtectionLevel =
            FtpDataProtectionLevel.Private;
        FtpServerTransferRequest request = CreateRequest(source, target);

        Assert.Throws<FtpConfigurationException>(() =>
            source.TransferServerToServer(request));
        Assert.Multiple(() =>
        {
            Assert.That(sourceCommands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
            Assert.That(targetCommands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
            }));
        });

        source.AbortConnection();
        target.AbortConnection();
    }

    private static FtpServerTransferRequest CreateRequest(
        FtpClient source,
        FtpClient target)
    {
        FtpServerTransferRequest request = new FtpServerTransferRequest();
        request.SourceClient = source;
        request.TargetClient = target;
        request.SourcePath = "/source.bin";
        request.TargetPath = "/target.bin";
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
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
        options.DataConnection.AllowForeignDataConnections = true;
        options.DataConnection.RequireDataPeerMatch = false;
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

    private static void ReceiveTransfer(
        NetworkStream stream,
        List<string> commands,
        string expectedCommand,
        string preliminaryReply,
        string finalReply)
    {
        string command = ReadCommand(stream);
        commands.Add(command);
        Assert.That(command, Is.EqualTo(expectedCommand));
        WriteText(stream, preliminaryReply);
        WriteText(stream, finalReply);
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
