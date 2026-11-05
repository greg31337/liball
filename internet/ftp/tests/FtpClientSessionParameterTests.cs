using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientSessionParameterTests
{
    [Test]
    public void Representation_commands_use_canonical_TYPE_arguments_and_update_only_on_acceptance()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 TYPE I accepted\r\n");
            Receive(stream, commands, "504 TYPE A T rejected\r\n");
            Receive(stream, commands, "200 TYPE E C accepted\r\n");
            Receive(stream, commands, "200 TYPE L accepted\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpTransferOptions image = new FtpTransferOptions();
        FtpCommandResult imageResult = client.SetTransferRepresentation(image);
        FtpTransferOptions ascii = new FtpTransferOptions();
        ascii.Type = FtpTransferType.Ascii;
        ascii.TextFormat = FtpTextFormat.Telnet;
        FtpCommandResult asciiResult = client.SetTransferRepresentation(ascii);
        Assert.That(client.State.Session.State.TransferType,
            Is.EqualTo(FtpTransferType.Image));
        FtpTransferOptions ebcdic = new FtpTransferOptions();
        ebcdic.Type = FtpTransferType.Ebcdic;
        ebcdic.TextFormat = FtpTextFormat.CarriageControl;
        FtpCommandResult ebcdicResult = client.SetTransferRepresentation(ebcdic);
        FtpTransferOptions local = new FtpTransferOptions();
        local.Type = FtpTransferType.LocalByte;
        local.HasLocalByteSize = true;
        local.LocalByteSize = 36;
        FtpCommandResult localResult = client.SetTransferRepresentation(local);

        FtpTransferOptions invalid = new FtpTransferOptions();
        invalid.TextFormat = FtpTextFormat.Telnet;
        Assert.Throws<FtpConfigurationException>(() =>
            client.SetTransferRepresentation(invalid));
        FtpCommand invalidRaw = new FtpCommand();
        invalidRaw.Kind = FtpCommandKind.Type;
        invalidRaw.Argument = "Q";
        Assert.Throws<FtpProtocolException>(() =>
            client.ExecuteCommand(invalidRaw));
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "TYPE A T",
                "TYPE E C", "TYPE L 36", "NOOP",
            }));
            Assert.That(imageResult.Succeeded, Is.True);
            Assert.That(asciiResult.Succeeded, Is.False);
            Assert.That(ebcdicResult.Succeeded, Is.True);
            Assert.That(localResult.Succeeded, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.LocalByte));
            Assert.That(client.State.Session.State.TextFormat,
                Is.EqualTo(FtpTextFormat.NonPrint));
            Assert.That(client.State.Session.State.HasLocalByteSize, Is.True);
            Assert.That(client.State.Session.State.LocalByteSize, Is.EqualTo(36));
        });

        client.AbortConnection();
    }

    [Test]
    public void Structure_and_mode_commands_validate_combinations_and_MODE_Z_advertisement()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 STRU R accepted\r\n");
            Receive(stream, commands, "200 MODE B accepted\r\n");
            Receive(stream, commands, "200 MODE Z record accepted\r\n");
            Receive(stream, commands, "504 STRU F rejected\r\n");
            Receive(stream, commands, "200 STRU F accepted\r\n");
            Receive(stream, commands, "200 MODE Z accepted\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        FtpCommandResult record =
            client.SetFileStructure(FtpFileStructure.Record);
        FtpCommandResult block =
            client.SetTransferMode(FtpTransferMode.Block);
        FtpCommandResult recordDeflate =
            client.SetTransferMode(FtpTransferMode.Deflate);
        FtpCommandResult rejectedFile =
            client.SetFileStructure(FtpFileStructure.File);
        Assert.That(client.State.Session.State.FileStructure,
            Is.EqualTo(FtpFileStructure.Record));
        FtpCommandResult file =
            client.SetFileStructure(FtpFileStructure.File);
        FtpCommandResult deflate =
            client.SetTransferMode(FtpTransferMode.Deflate);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.ModeZ = false;
        Assert.Throws<FtpConfigurationException>(() =>
            client.SetTransferMode(FtpTransferMode.Deflate));
        Assert.Throws<FtpConfigurationException>(() =>
            client.SetFileStructure((FtpFileStructure)99));
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "STRU R", "MODE B",
                "MODE Z", "STRU F", "STRU F", "MODE Z", "NOOP",
            }));
            Assert.That(record.Succeeded, Is.True);
            Assert.That(block.Succeeded, Is.True);
            Assert.That(recordDeflate.Succeeded, Is.True);
            Assert.That(rejectedFile.Succeeded, Is.False);
            Assert.That(file.Succeeded, Is.True);
            Assert.That(deflate.Succeeded, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.State.Session.State.FileStructure,
                Is.EqualTo(FtpFileStructure.File));
            Assert.That(client.State.Session.State.TransferMode,
                Is.EqualTo(FtpTransferMode.Deflate));
        });

        client.AbortConnection();
    }

    [Test]
    public void Allocation_and_directory_commands_track_resolved_and_reported_working_directories()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "202 ALLO unnecessary\r\n");
            Receive(stream, commands, "250 Directory changed\r\n");
            Receive(stream, commands, "250 Directory changed\r\n");
            Receive(stream, commands, "200 Parent selected\r\n");
            Receive(stream, commands, "202 Mount unnecessary\r\n");
            Receive(stream, commands,
                "257 \"/reported/\"\"quoted\"\"\" is current\r\n");
            Receive(stream, commands,
                "211-Server status\r\n Ready\r\n211 End\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);

        Assert.Throws<FtpConfigurationException>(() =>
            client.AllocateStorage(-1));
        Assert.Throws<FtpConfigurationException>(() =>
            client.ChangeWorkingDirectory(" "));
        Assert.Throws<FtpConfigurationException>(() =>
            client.MountFileStructure("bad\nmount"));
        FtpCommandResult allocation = client.AllocateStorage(long.MaxValue);
        FtpCommandResult absolute = client.ChangeWorkingDirectory("/alpha/./work");
        FtpCommandResult relative = client.ChangeWorkingDirectory("../beta");
        Assert.That(client.State.Session.State.CurrentDirectory,
            Is.EqualTo("/alpha/beta"));
        FtpCommandResult parent = client.ChangeToParentDirectory();
        Assert.That(client.State.Session.State.CurrentDirectory,
            Is.EqualTo("/alpha"));
        FtpCommandResult mount = client.MountFileStructure("/volume");
        FtpCommandResult pwd = client.GetWorkingDirectory();
        FtpCommandResult status = client.RequestStatus(null);
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
                "ALLO 9223372036854775807",
                "CWD /alpha/./work", "CWD ../beta", "CDUP",
                "SMNT /volume", "PWD", "STAT", "NOOP",
            }));
            Assert.That(allocation.Succeeded, Is.True);
            Assert.That(absolute.Succeeded, Is.True);
            Assert.That(relative.Succeeded, Is.True);
            Assert.That(parent.Succeeded, Is.True);
            Assert.That(mount.Succeeded, Is.True);
            Assert.That(pwd.Succeeded, Is.True);
            Assert.That(status.Succeeded, Is.True);
            Assert.That(status.FinalReply!.Multiline, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.State.Session.State.CurrentDirectory,
                Is.EqualTo("/reported/\"quoted\""));
        });

        client.AbortConnection();
    }

    [Test]
    public void Historic_directory_fallback_is_opt_in_and_limited_to_unsupported_replies()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "502 CWD unsupported\r\n");
            Receive(stream, commands, "250 XCWD accepted\r\n");
            Receive(stream, commands, "500 CDUP unknown\r\n");
            Receive(stream, commands, "200 XCUP accepted\r\n");
            Receive(stream, commands, "504 PWD unavailable\r\n");
            Receive(stream, commands, "257 \"/reported\" current\r\n");
            Receive(stream, commands, "502 MKD unsupported\r\n");
            Receive(stream, commands, "257 \"/made\" created\r\n");
            Receive(stream, commands, "502 RMD unsupported\r\n");
            Receive(stream, commands, "250 XRMD accepted\r\n");
            Receive(stream, commands, "550 Missing directory\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true);

        FtpCommandResult changed =
            client.ChangeWorkingDirectory("/work");
        FtpCommandResult parent = client.ChangeToParentDirectory();
        FtpCommandResult printed = client.GetWorkingDirectory();
        FtpCommandResult created = client.CreateDirectory("/made");
        FtpCommandResult removed = client.RemoveDirectory("/old");
        FtpCommandResult missing = client.RemoveDirectory("/missing");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "CWD /work", "XCWD /work", "CDUP", "XCUP",
                "PWD", "XPWD", "MKD /made", "XMKD /made",
                "RMD /old", "XRMD /old", "RMD /missing",
            }));
            Assert.That(changed.Command.Kind, Is.EqualTo(FtpCommandKind.Xcwd));
            Assert.That(parent.Command.Kind, Is.EqualTo(FtpCommandKind.Xcup));
            Assert.That(printed.Command.Kind, Is.EqualTo(FtpCommandKind.Xpwd));
            Assert.That(created.Command.Kind, Is.EqualTo(FtpCommandKind.Xmkd));
            Assert.That(removed.Command.Kind, Is.EqualTo(FtpCommandKind.Xrmd));
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(client.State.Session.State.CurrentDirectory,
                Is.EqualTo("/reported"));
        });

        client.AbortConnection();
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClient ConnectAuthenticated(
        TcpListener listener,
        bool historicFallback = false)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.AllowPlaintextCredentials = true;
        options.Quirks.AllowHistoricAliasFallback = historicFallback;
        options.Credentials.UserName = "alice";
        options.Credentials.Password = "secret";
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        return client;
    }

    private static void Authenticate(Stream stream, List<string> commands)
    {
        WriteText(stream, "220 Ready\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void Receive(
        Stream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, reply);
    }

    private static string ReadCommand(Stream stream)
    {
        using MemoryStream bytes = new MemoryStream();
        bool carriageReturn = false;
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException(
                    "The client closed before sending the expected command.");
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
