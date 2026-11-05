using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientMetadataOperationTests
{
    [Test]
    public void SIZE_returns_file_metadata_without_running_fallbacks()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "213 42\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.GetFileSize("/reports/file.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "SIZE /reports/file.txt",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Commands, Has.Length.EqualTo(1));
            Assert.That(result.Entry!.Name, Is.EqualTo("file.txt"));
            Assert.That(result.Entry.FullPath, Is.EqualTo("/reports/file.txt"));
            Assert.That(result.Entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(result.Entry.HasSize, Is.True);
            Assert.That(result.Entry.Size, Is.EqualTo(42));
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_SIZE_uses_MLST_when_fallback_is_enabled()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 SIZE unavailable\r\n");
            Receive(stream, commands,
                "250-Listing\r\n Type=file;Size=73; /fallback.bin\r\n250 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.GetFileSize("/fallback.bin");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "SIZE /fallback.bin",
                "MLST /fallback.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Entry!.Size, Is.EqualTo(73));
            Assert.That(result.Commands.Select(command => command.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Size, FtpCommandKind.Mlst }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Disabled_SIZE_fallback_returns_the_primary_failure_only()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 SIZE unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, false, true);

        FtpPathProbeResult result = client.GetFileSize("/fallback.bin");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Has.Count.EqualTo(3));
            Assert.That(commands[2], Is.EqualTo("SIZE /fallback.bin"));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Exists, Is.False);
            Assert.That(result.Commands, Has.Length.EqualTo(1));
            Assert.That(result.FailureMessage, Does.Contain("SIZE failed with reply 500"));
        });

        client.AbortConnection();
    }

    [Test]
    public void MDTM_returns_exact_UTC_timestamp_metadata()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "213 20240229123456.125\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.GetModificationTime("/report.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[2], Is.EqualTo("MDTM /report.txt"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entry!.Modified.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc)
                    .AddTicks(1250000)));
            Assert.That(result.Entry.Modified.FractionalSeconds, Is.EqualTo("125"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_MDTM_uses_MLST_when_fallback_is_enabled()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 MDTM unavailable\r\n");
            Receive(stream, commands,
                "250-Listing\r\n" +
                " Type=file;Modify=20260102030405.75; /fallback.txt\r\n" +
                "250 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.GetModificationTime("/fallback.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "MDTM /fallback.txt", "MLST /fallback.txt",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Entry!.Modified.HasValue, Is.True);
            Assert.That(result.Entry.Modified.ValueUtc, Is.EqualTo(
                new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
                    .AddTicks(7500000)));
            Assert.That(result.Commands.Select(command => command.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Mdtm, FtpCommandKind.Mlst }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Disabled_MDTM_fallback_returns_the_primary_failure_only()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "550 MDTM denied\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, false);

        FtpPathProbeResult result = client.GetModificationTime("/blocked.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "MDTM /blocked.txt",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Exists, Is.False);
            Assert.That(result.Commands, Has.Length.EqualTo(1));
            Assert.That(result.FailureMessage, Does.Contain("MDTM failed with reply 550"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Metadata_falls_back_from_MLST_to_SIZE()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 MLST unavailable\r\n");
            Receive(stream, commands, "213 19\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.GetMetadata("/report.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "MLST /report.txt", "SIZE /report.txt",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Entry!.HasSize, Is.True);
            Assert.That(result.Entry.Size, Is.EqualTo(19));
        });

        client.AbortConnection();
    }

    [Test]
    public void Directory_probe_uses_PWD_and_restores_the_original_working_directory()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 MLST unavailable\r\n");
            Receive(stream, commands, "500 SIZE unavailable\r\n");
            Receive(stream, commands, "500 MDTM unavailable\r\n");
            Receive(stream, commands, "257 \"/home/alice\" is current\r\n");
            Receive(stream, commands, "250 Directory changed\r\n");
            Receive(stream, commands, "250 Directory restored\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpPathProbeResult result = client.ProbePath("/archive");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "MLST /archive", "SIZE /archive", "MDTM /archive",
                "PWD", "CWD /archive", "CWD /home/alice",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Entry!.Type, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(result.Entry.FullPath, Is.EqualTo("/archive"));
            Assert.That(result.Commands, Has.Length.EqualTo(6));
            Assert.That(client.State.Session.State.CurrentDirectory,
                Is.EqualTo("/home/alice"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Probe_uses_NLST_after_known_metadata_absence_and_failed_LIST()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener listDataListener = StartListener();
        using TcpListener namesDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "550 Not a directory\r\n");

            PreparePassive(stream, listDataListener, commands);
            using (TcpClient listData = listDataListener.AcceptTcpClient())
            {
                Receive(stream, commands, "550 LIST unavailable\r\n");
            }

            PreparePassive(stream, namesDataListener, commands);
            using TcpClient namesData = namesDataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
            WriteText(stream, "150 Opening names\r\n");
            using (NetworkStream dataStream = namesData.GetStream())
            {
                WriteText(dataStream, "/target.bin\r\n");
            }
            WriteText(stream, "226 Names complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener, true, true);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MlstAdvertised = false;
        client.State.FeatureState.FileSize = false;
        client.State.FeatureState.ModificationTimeQuery = false;
        client.State.FeatureState.ExtendedPassive = true;

        FtpPathProbeResult result = client.ProbePath("/target.bin");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "PWD", "CWD /target.bin",
                "EPSV", "LIST /target.bin", "EPSV", "NLST /target.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Entry!.Name, Is.EqualTo("/target.bin"));
            Assert.That(result.Entry.Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(result.Entry.FullPath, Is.EqualTo("/target.bin"));
            Assert.That(result.Commands.Select(command => command.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Pwd, FtpCommandKind.Cwd,
                    FtpCommandKind.List, FtpCommandKind.Nlst,
                }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Probe_reports_a_missing_path_after_every_safe_fallback_rejects_it()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener listDataListener = StartListener();
        using TcpListener namesDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "550 Not a directory\r\n");

            PreparePassive(stream, listDataListener, commands);
            using (TcpClient listData = listDataListener.AcceptTcpClient())
            {
                Receive(stream, commands, "550 LIST path missing\r\n");
            }

            PreparePassive(stream, namesDataListener, commands);
            using (TcpClient namesData = namesDataListener.AcceptTcpClient())
            {
                Receive(stream, commands, "550 NLST path missing\r\n");
            }
        });
        FtpClient client = ConnectAuthenticated(controlListener, true, true);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MlstAdvertised = false;
        client.State.FeatureState.FileSize = false;
        client.State.FeatureState.ModificationTimeQuery = false;
        client.State.FeatureState.ExtendedPassive = true;

        FtpPathProbeResult result = client.ProbePath("/missing.bin");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "PWD", "CWD /missing.bin",
                "EPSV", "LIST /missing.bin", "EPSV", "NLST /missing.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Commands.Select(command => command.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Pwd, FtpCommandKind.Cwd,
                    FtpCommandKind.List, FtpCommandKind.Nlst,
                }));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void Failed_directory_restore_closes_the_session()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 MLST unavailable\r\n");
            Receive(stream, commands, "500 SIZE unavailable\r\n");
            Receive(stream, commands, "500 MDTM unavailable\r\n");
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "250 Directory changed\r\n");
            Receive(stream, commands, "550 Restore denied\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true, true);

        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            client.ProbePath("/archive"));
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

    private static FtpClient ConnectAuthenticated(
        TcpListener listener,
        bool allowSizeFallback,
        bool allowMdtmFallback)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        options.Quirks.AllowSizeFallback = allowSizeFallback;
        options.Quirks.AllowMdtmFallback = allowMdtmFallback;
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

    private static void PreparePassive(
        NetworkStream stream,
        TcpListener dataListener,
        List<string> commands)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
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
