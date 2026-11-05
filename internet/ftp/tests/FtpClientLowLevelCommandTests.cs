using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientLowLevelCommandTests
{
    [Test]
    public void Convenience_commands_preserve_multiline_and_negative_replies()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "331 Password required\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "230 Logged in\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "214-Help follows\r\n NOOP does nothing\r\n214 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "211-Status follows\r\n Connected\r\n211 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "215 UNIX Type: L8\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "500 NOOP disabled\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 SITE command accepted\r\n");
        });
        FtpClient client = new FtpClient(null);
        FtpClientOptions options = CreateOptions(listener);
        options.AllowPlaintextCredentials = true;
        options.Credentials.UserName = "alice";
        options.Credentials.Password = "secret";
        client.Connect(options);

        FtpCommandResult help = client.RequestHelp("NOOP");
        FtpCommandResult status = client.RequestStatus("/pub");
        FtpCommandResult system = client.RequestSystemType();
        FtpCommandResult noop = client.SendNoOperation();
        FtpCommandResult site = client.ExecuteSite("CHMOD 644 /pub/file.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice",
                "PASS secret",
                "HELP NOOP",
                "STAT /pub",
                "SYST",
                "NOOP",
                "SITE CHMOD 644 /pub/file.txt",
            }));
            Assert.That(help.Succeeded, Is.True);
            Assert.That(help.FinalReply!.Multiline, Is.True);
            Assert.That(status.FinalReply!.Code, Is.EqualTo(211));
            Assert.That(system.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.SystemType));
            Assert.That(noop.Succeeded, Is.False);
            Assert.That(noop.FinalReply!.Code, Is.EqualTo(500));
            Assert.That(noop.ConnectionClosed, Is.False);
            Assert.That(site.Succeeded, Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void Raw_and_batch_commands_are_normalized_and_negative_results_do_not_stop_the_batch()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "202 Extension accepted\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "214 Help available\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "500 Keepalive rejected\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "215 UNIX\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener));
        FtpCommand extension = CreateCommand(
            FtpCommandKind.Unknown, "x-vendor", "alpha beta");
        FtpCommand[] batch = new[]
        {
            CreateCommand(FtpCommandKind.Help, "ignored", null),
            CreateCommand(FtpCommandKind.Unknown, "noop", null),
            CreateCommand(FtpCommandKind.Syst, "ignored", null),
        };

        FtpCommandResult extensionResult = client.ExecuteCommand(extension);
        FtpCommandResult[] batchResults = client.ExecuteCommandBatch(batch);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "X-VENDOR alpha beta", "HELP", "NOOP", "SYST",
            }));
            Assert.That(extensionResult.Command.Kind,
                Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(extensionResult.Command.Verb, Is.EqualTo("X-VENDOR"));
            Assert.That(extensionResult.Succeeded, Is.True);
            Assert.That(batchResults, Has.Length.EqualTo(3));
            Assert.That(batchResults.Select(result => result.Succeeded),
                Is.EqualTo(new[] { true, false, true }));
            Assert.That(batchResults.Select(result => result.FinalReply!.Code),
                Is.EqualTo(new[] { 214, 500, 215 }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Batch_validation_and_coordinated_command_rejection_happen_before_wire_output()
    {
        using TcpListener listener = StartListener();
        string? received = null;
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            received = ReadCommand(stream);
            WriteText(stream, "200 NOOP okay\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener));
        FtpCommand injected = CreateCommand(
            FtpCommandKind.Site, "SITE", "safe\r\nDELE victim");
        FtpCommand[] batch = new[]
        {
            CreateCommand(FtpCommandKind.Noop, "NOOP", null),
            injected,
        };

        Assert.Throws<FtpProtocolException>(() =>
            client.ExecuteCommandBatch(batch));
        Assert.Throws<FtpProtocolException>(() => client.ExecuteCommand(
            CreateCommand(FtpCommandKind.Retr, "RETR", "file.bin")));
        Assert.Throws<FtpProtocolException>(() => client.ExecuteCommand(
            CreateCommand(FtpCommandKind.Auth, "AUTH", "TLS")));
        Assert.Throws<FtpConfigurationException>(() => client.ExecuteCommand(
            CreateCommand(FtpCommandKind.User, "USER", "alice")));
        FtpCommandResult result = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.EqualTo("NOOP"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Connected));
        });

        client.AbortConnection();
    }

    [Test]
    public void Quit_disconnect_and_abort_use_orderly_and_abrupt_closure_respectively()
    {
        using TcpListener listener = StartListener();
        List<string?> commands = new List<string?>();
        Task server = Task.Run(() =>
        {
            using (TcpClient accepted = listener.AcceptTcpClient())
            using (NetworkStream stream = accepted.GetStream())
            {
                WriteText(stream, "220 First\r\n");
                commands.Add(ReadCommandOrEnd(stream));
                WriteText(stream, "221 Goodbye\r\n");
            }
            using (TcpClient accepted = listener.AcceptTcpClient())
            using (NetworkStream stream = accepted.GetStream())
            {
                WriteText(stream, "220 Second\r\n");
                commands.Add(ReadCommandOrEnd(stream));
                WriteText(stream, "500 QUIT rejected\r\n");
                commands.Add(ReadCommandOrEnd(stream));
            }
            using (TcpClient accepted = listener.AcceptTcpClient())
            using (NetworkStream stream = accepted.GetStream())
            {
                WriteText(stream, "220 Third\r\n");
                commands.Add(ReadCommandOrEnd(stream));
            }
        });
        FtpClient first = new FtpClient(null);
        first.Connect(CreateOptions(listener));

        FtpCommandResult quit = first.QuitSession();
        FtpClient second = new FtpClient(null);
        second.Connect(CreateOptions(listener));
        second.Disconnect();
        FtpClient third = new FtpClient(null);
        third.Connect(CreateOptions(listener));
        third.AbortConnection();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new string?[]
            {
                "QUIT", "QUIT", null, null,
            }));
            Assert.That(quit.Succeeded, Is.True);
            Assert.That(quit.ConnectionClosed, Is.True);
            Assert.That(first.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(second.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(third.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
        });
    }

    [Test]
    public void Rein_resets_host_features_options_and_UTF8_while_leaving_plain_control_open()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "211-Features:\r\n UTF8\r\n REST STREAM\r\n211 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 UTF8 ON\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "220 HOST accepted\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "220 Reinitialized\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 NOOP okay\r\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.NegotiateFeatures = true;
        options.VirtualHost = "virtual.example";
        FtpClient client = new FtpClient(null);
        client.Connect(options);

        FtpCommandResult rein = client.ReinitializeSession();
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "FEAT", "OPTS UTF8 ON", "HOST virtual.example", "REIN", "NOOP",
            }));
            Assert.That(rein.Succeeded, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Session.State.SelectedHost, Is.Null);
            Assert.That(client.State.Features.Negotiated, Is.False);
            Assert.That(client.State.FeatureState.Utf8Advertised, Is.False);
            Assert.That(client.State.FeatureState.Utf8Enabled, Is.False);
            Assert.That(client.State.Connection!.Utf8Enabled, Is.False);
            Assert.That(client.State.FeatureState.RestStream, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Service_closure_and_abrupt_peer_disconnect_clear_client_connection_state()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using (TcpClient accepted = listener.AcceptTcpClient())
            using (NetworkStream stream = accepted.GetStream())
            {
                WriteText(stream, "220 First\r\n");
                RequireCommand(stream, "NOOP");
                WriteText(stream, "421 Closing\r\n");
            }
            using (TcpClient accepted = listener.AcceptTcpClient())
            using (NetworkStream stream = accepted.GetStream())
            {
                WriteText(stream, "220 Second\r\n");
                RequireCommand(stream, "SYST");
            }
        });
        FtpClient closed = new FtpClient(null);
        closed.Connect(CreateOptions(listener));
        FtpCommandResult[] closedBatch = closed.ExecuteCommandBatch(new[]
        {
            CreateCommand(FtpCommandKind.Noop, "NOOP", null),
            CreateCommand(FtpCommandKind.Syst, "SYST", null),
        });
        FtpCommandResult closure = closedBatch[0];
        FtpClient abrupt = new FtpClient(null);
        abrupt.Connect(CreateOptions(listener));

        Assert.Throws<FtpProtocolException>(() => abrupt.RequestSystemType());
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(closure.Succeeded, Is.False);
            Assert.That(closure.ConnectionClosed, Is.True);
            Assert.That(closedBatch, Has.Length.EqualTo(1));
            Assert.That(closed.State.Connection, Is.Null);
            Assert.That(closed.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(abrupt.State.Connection, Is.Null);
            Assert.That(abrupt.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
        });
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClientOptions CreateOptions(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static FtpCommand CreateCommand(
        FtpCommandKind kind,
        string verb,
        string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = verb;
        command.Argument = argument;
        return command;
    }

    private static void RequireCommand(Stream stream, string expected)
    {
        string actual = ReadCommand(stream);
        if (actual != expected)
        {
            throw new InvalidDataException(
                "Expected command '" + expected + "' but received '" + actual + "'.");
        }
    }

    private static string ReadCommand(Stream stream)
    {
        string? command = ReadCommandOrEnd(stream);
        if (command == null)
        {
            throw new EndOfStreamException(
                "The client closed before sending the expected command.");
        }
        return command;
    }

    private static string? ReadCommandOrEnd(Stream stream)
    {
        using MemoryStream bytes = new MemoryStream();
        bool carriageReturn = false;
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                if (bytes.Length == 0 && !carriageReturn)
                {
                    return null;
                }
                throw new EndOfStreamException(
                    "The client closed during a command.");
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException(
                        "The client command did not use CRLF.");
                }
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
            if (value == 13)
            {
                carriageReturn = true;
            }
            else
            {
                bytes.WriteByte((byte)value);
            }
        }
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
