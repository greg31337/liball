using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientListingOperationTests
{
    [Test]
    public void LIST_detects_the_format_marks_hidden_entries_filters_pseudo_entries_and_retains_raw_lines()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        string[] rawLines =
        {
            "drwxr-xr-x 2 alice staff 4096 Jan 01 2024 .",
            "drwx------ 2 alice staff 4096 Jan 02 2024 .hidden",
            "-rw-r--r-- 1 alice staff 12 Jan 03 2024 visible.txt",
            "server-specific raw entry",
        };
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            PreparePassive(controlStream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening LIST data\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteText(dataStream, string.Join("\r\n", rawLines) + "\r\n");
            }
            WriteText(controlStream, "226 LIST complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListHumanDirectory("");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "LIST",
            }));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("Unix"));
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.RawLines, Is.EqualTo(rawLines));
            Assert.That(listing.Entries, Has.Length.EqualTo(3));
            Assert.That(listing.Entries[0].Name, Is.EqualTo(".hidden"));
            Assert.That(listing.Entries[0].Hidden, Is.True);
            Assert.That(listing.Entries[1].Name, Is.EqualTo("visible.txt"));
            Assert.That(listing.Entries[2].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(listing.Entries[2].RawLine,
                Is.EqualTo("server-specific raw entry"));
            Assert.That(listing.CommandResult!.PreliminaryReply!.Code, Is.EqualTo(150));
            Assert.That(listing.CommandResult.FinalReply!.Code, Is.EqualTo(226));
        });

        client.AbortConnection();
    }

    [Test]
    public void NLST_preserves_names_without_inventing_types_or_path_safety()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            PreparePassive(controlStream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening NLST data\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteText(dataStream, ".hidden\r\nfolder name\r\n../literal-name\r\n");
            }
            WriteText(controlStream, "226 NLST complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListNames("/incoming");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[3], Is.EqualTo("NLST /incoming"));
            Assert.That(listing.FormatName, Is.EqualTo("Names"));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.Entries.Select(entry => entry.Name), Is.EqualTo(new[]
            {
                ".hidden", "folder name", "../literal-name",
            }));
            Assert.That(listing.Entries, Has.All.Matches<FtpEntry>(entry =>
                entry.Type == FtpEntryType.Unknown));
            Assert.That(listing.Entries[0].Hidden, Is.True);
            Assert.That(listing.Entries[2].FullPath, Is.Null);
            Assert.That(listing.RawLines[2], Is.EqualTo("../literal-name"));
        });

        client.AbortConnection();
    }

    [Test]
    public void STAT_parses_multiline_listing_without_opening_a_data_connection()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "213-Status follows\r\n" +
                " -rw-r--r-- 1 alice staff 9 Feb 29 2024 status.txt\r\n" +
                " drwxr-xr-x 2 alice staff 0 Feb 29 2024 .\r\n" +
                " provider diagnostic row\r\n" +
                "213 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListStatus("/reports");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "STAT /reports",
            }));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("Unix"));
            Assert.That(listing.RawLines, Is.EqualTo(new[]
            {
                " -rw-r--r-- 1 alice staff 9 Feb 29 2024 status.txt",
                " drwxr-xr-x 2 alice staff 0 Feb 29 2024 .",
                " provider diagnostic row",
            }));
            Assert.That(listing.Entries, Has.Length.EqualTo(2));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("status.txt"));
            Assert.That(listing.Entries[1].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(listing.CommandResult!.FinalReply!.Code, Is.EqualTo(213));
            Assert.That(client.State.DataStrategy, Is.Null);
        });

        client.AbortConnection();
    }

    [Test]
    public void Adaptive_listing_prefers_MLSD_when_feature_state_is_unknown()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            PreparePassive(controlStream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening MLSD data\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteText(dataStream,
                    "type=cdir; .\r\n" +
                    "type=file;size=5; .hidden\r\n");
            }
            WriteText(controlStream, "226 MLSD complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListDirectory("/machine");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[3], Is.EqualTo("MLSD /machine"));
            Assert.That(listing.MachineReadable, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("MLSD"));
            Assert.That(listing.Entries, Has.Length.EqualTo(1));
            Assert.That(listing.Entries[0].Name, Is.EqualTo(".hidden"));
            Assert.That(listing.Entries[0].Hidden, Is.True);
            Assert.That(listing.RawLines, Has.Length.EqualTo(2));
        });

        client.AbortConnection();
    }

    [Test]
    public void Negotiated_MLSD_absence_selects_LIST_and_preserves_unicode_symlink_cycles()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        string[] rawLines =
        {
            "lrwxrwxrwx 1 alice staff 1 Jan 01 2024 loop -> .",
            "-rw-r--r-- 1 alice staff 12 Jan 02 2024 café-文件.txt",
            "drwx------ 2 alice staff 0 Jan 03 2024 .秘密",
            "opaque fournisseur 雪",
        };
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            PreparePassive(controlStream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening LIST data\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteText(dataStream, string.Join("\r\n", rawLines) + "\r\n");
            }
            WriteText(controlStream, "226 LIST complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = false;
        client.State.FeatureState.ExtendedPassive = true;
        client.State.FeatureState.Utf8Enabled = true;
        client.State.Connection!.Utf8Enabled = true;

        FtpListing listing = client.ListDirectory("/répertoire");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "LIST /répertoire",
            }));
            Assert.That(commands, Has.None.Contains("MLSD"));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("Unix"));
            Assert.That(listing.RawLines, Is.EqualTo(rawLines));
            Assert.That(listing.Entries, Has.Length.EqualTo(4));
            Assert.That(listing.Entries[0].Type,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("loop"));
            Assert.That(listing.Entries[0].LinkTarget, Is.EqualTo("."));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("café-文件.txt"));
            Assert.That(listing.Entries[2].Name, Is.EqualTo(".秘密"));
            Assert.That(listing.Entries[2].Hidden, Is.True);
            Assert.That(listing.Entries[3].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(listing.Entries[3].RawLine, Is.EqualTo("opaque fournisseur 雪"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Unrecognized_LIST_payload_is_returned_as_a_complete_raw_listing()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        string[] rawLines =
        {
            "provider-object alpha",
            "provider-object beta with spaces",
        };
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            PreparePassive(controlStream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening provider listing\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteText(dataStream, string.Join("\r\n", rawLines) + "\r\n");
            }
            WriteText(controlStream, "226 Provider listing complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListHumanDirectory("/opaque");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[3], Is.EqualTo("LIST /opaque"));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("Raw"));
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.RawLines, Is.EqualTo(rawLines));
            Assert.That(listing.Entries.Select(entry => entry.Type),
                Is.EqualTo(new[] { FtpEntryType.Unknown, FtpEntryType.Unknown }));
            Assert.That(listing.Entries.Select(entry => entry.RawLine),
                Is.EqualTo(rawLines));
        });

        client.AbortConnection();
    }

    [Test]
    public void Adaptive_listing_falls_back_once_when_advertised_MLSD_is_rejected()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener machineDataListener = StartListener();
        using TcpListener humanDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);

            PreparePassive(controlStream, machineDataListener, commands);
            using (TcpClient machineData = machineDataListener.AcceptTcpClient())
            {
                commands.Add(ReadCommand(controlStream));
                WriteText(controlStream, "500 MLSD is unavailable\r\n");
            }

            PreparePassive(controlStream, humanDataListener, commands);
            using TcpClient humanData = humanDataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteText(controlStream, "150 Opening LIST data\r\n");
            using (NetworkStream dataStream = humanData.GetStream())
            {
                WriteText(dataStream,
                    "02-29-24  03:04PM  12 fallback.txt\r\n");
            }
            WriteText(controlStream, "226 LIST complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MachineListing = true;
        client.State.FeatureState.ExtendedPassive = true;

        FtpListing listing = client.ListDirectory("/fallback");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
                "EPSV", "MLSD /fallback",
                "EPSV", "LIST /fallback",
            }));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("DOS/Windows"));
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.Entries[0].Name, Is.EqualTo("fallback.txt"));
            Assert.That(client.State.FeatureState.MachineListing, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_LIST_returns_an_incomplete_diagnostic_listing()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            PreparePassive(stream, dataListener, commands);
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
            WriteText(stream, "550 Directory unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing listing = client.ListHumanDirectory("/missing");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(listing.Complete, Is.False);
            Assert.That(listing.Entries, Is.Empty);
            Assert.That(listing.RawLines, Is.Empty);
            Assert.That(listing.FormatName, Is.EqualTo("Raw"));
            Assert.That(listing.CommandResult!.FinalReply!.Code, Is.EqualTo(550));
            Assert.That(listing.FailureMessage, Does.Contain("LIST failed with reply 550"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_NLST_and_STAT_return_command_specific_diagnostic_listings()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            PreparePassive(stream, dataListener, commands);
            using (TcpClient data = dataListener.AcceptTcpClient())
            {
                commands.Add(ReadCommand(stream));
                WriteText(stream, "550 Names unavailable\r\n");
            }
            commands.Add(ReadCommand(stream));
            WriteText(stream, "450 Status temporarily unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpListing names = client.ListNames("/missing");
        FtpListing status = client.ListStatus("/missing");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "NLST /missing",
                "STAT /missing",
            }));
            Assert.That(names.Complete, Is.False);
            Assert.That(names.FormatName, Is.EqualTo("Names"));
            Assert.That(names.FailureMessage, Does.Contain("NLST failed with reply 550"));
            Assert.That(names.CommandResult!.FinalReply!.Code, Is.EqualTo(550));
            Assert.That(status.Complete, Is.False);
            Assert.That(status.FormatName, Is.EqualTo("Raw"));
            Assert.That(status.FailureMessage, Does.Contain("STAT failed with reply 450"));
            Assert.That(status.CommandResult!.FinalReply!.Code, Is.EqualTo(450));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
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
        commands.Add(ReadCommand(stream));
        WriteText(stream, "331 Password required\r\n");
        commands.Add(ReadCommand(stream));
        WriteText(stream, "230 Logged in\r\n");
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
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static void WriteText(NetworkStream stream, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
