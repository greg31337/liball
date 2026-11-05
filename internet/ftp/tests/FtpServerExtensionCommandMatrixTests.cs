using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerExtensionCommandMatrixTests
{
    private string RootPath = "";

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-extension-matrix-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void Extension_commands_return_exact_argument_shape_errors()
    {
        int port = ReserveAvailablePort();
        FtpVirtualHostOptions host = CreateHost();
        FtpServer server = CreateServer(host);
        string[] commands =
        {
            "FEAT extra",
            "OPTS",
            "EPRT",
            "EPSV extra",
            "LANG en_uk",
            "MDTM",
            "SIZE",
            "REST",
            "AUTH",
            "PBSZ",
            "PROT",
            "CCC extra",
            "HOST"
        };
        string[] expected =
        {
            "501 The FTP command does not accept an argument.",
            "501 The FTP command requires an argument.",
            "500 Malformed FTP command line.",
            "500 Malformed FTP command line.",
            "500 Malformed FTP command line.",
            "501 The FTP command requires an argument.",
            "501 The FTP command requires an argument.",
            "500 Malformed FTP command line.",
            "501 The FTP command requires an argument.",
            "500 Malformed FTP command line.",
            "501 The FTP command requires an argument.",
            "501 The FTP command does not accept an argument.",
            "500 Malformed FTP command line."
        };

        try
        {
            server.StartServer(CreateOptions(port, host, 0));
            List<string?> actual = new List<string?>();
            List<string?> probes = new List<string?>();
            int index = 0;
            while (index < commands.Length)
            {
                using TcpClient client = Connect(port);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = CreateReader(stream);
                Assert.That(reader.ReadLine(), Is.EqualTo("220 Matrix ready"));
                WriteText(stream, commands[index] + "\r\nNOOP\r\nQUIT\r\n");
                actual.Add(reader.ReadLine());
                probes.Add(reader.ReadLine());
                Assert.That(reader.ReadLine(), Is.EqualTo(
                    "221 FTP service closing control connection."));
                index++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(probes, Has.All.EqualTo(
                    "200 NOOP command successful."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Extension_commands_return_exact_pre_login_matrix()
    {
        int port = ReserveAvailablePort();
        FtpVirtualHostOptions host = CreateHost();
        FtpServer server = CreateServer(host);
        string[] commands =
        {
            "OPTS UTF8 ON",
            "EPRT |1|127.0.0.1|49152|",
            "EPSV",
            "LANG",
            "MDTM item.txt",
            "SIZE item.txt",
            "MLST item.txt",
            "MLSD",
            "REST 0",
            "AUTH GSSAPI",
            "PBSZ 0",
            "PROT P",
            "CCC",
            "HOST ftp.matrix"
        };
        string[] expected =
        {
            "200 UTF8 ON",
            "530 The FTP command requires an authenticated session.",
            "530 The FTP command requires an authenticated session.",
            "200 Language set to en.",
            "530 The FTP command requires an authenticated session.",
            "530 The FTP command requires an authenticated session.",
            "530 The FTP command requires an authenticated session.",
            "530 The FTP command requires an authenticated session.",
            "530 The FTP command requires an authenticated session.",
            "504 Only the TLS and TLS-C security mechanisms are supported.",
            "503 PBSZ requires an active security association before CCC.",
            "503 PROT requires a protected control channel and a completed PBSZ negotiation.",
            "503 CCC requires a completed security exchange.",
            "220 Matrix ready"
        };

        try
        {
            server.StartServer(CreateOptions(port, host, 0));
            List<string?> actual = new List<string?>();
            List<string?> probes = new List<string?>();
            int index = 0;
            while (index < commands.Length)
            {
                using TcpClient client = Connect(port);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = CreateReader(stream);
                Assert.That(reader.ReadLine(), Is.EqualTo("220 Matrix ready"));
                WriteText(stream, commands[index] + "\r\nPWD\r\nQUIT\r\n");
                actual.Add(reader.ReadLine());
                probes.Add(reader.ReadLine());
                Assert.That(reader.ReadLine(), Is.EqualTo(
                    "221 FTP service closing control connection."));
                index++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(probes, Has.All.EqualTo(
                    "530 The FTP command requires an authenticated session."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Advertised_and_authenticated_extensions_execute_complete_matrix()
    {
        string path = Path.Combine(RootPath, "item.txt");
        File.WriteAllText(path, "data");
        File.SetLastWriteTimeUtc(path,
            new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        int port = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(port);
        int activePort = ReserveDifferentPort(port, passivePort);
        FtpVirtualHostOptions host = CreateHost();
        FtpServer server = CreateServer(host);

        try
        {
            server.StartServer(CreateOptions(port, host, passivePort));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 Matrix ready"));

            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            WriteText(stream,
                "HOST FTP.MATRIX\r\n" +
                "LANG fr\r\n" +
                "OPTS MLST size;type;unknown;\r\n" +
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "SIZE item.txt\r\n" +
                "MDTM item.txt\r\n");
            string? selectedHost = reader.ReadLine();
            string? language = reader.ReadLine();
            string? options = reader.ReadLine();
            string? user = reader.ReadLine();
            string? password = reader.ReadLine();
            string? account = reader.ReadLine();
            string? size = reader.ReadLine();
            string? modified = reader.ReadLine();

            WriteText(stream, "MLST item.txt\r\n");
            FtpEntry metadata = FtpMlsxCodec.ParseControlReply(
                ReadMultilineReply(reader));
            WriteText(stream,
                "MLSD\r\n" +
                "REST 2\r\n" +
                "EPRT |1|127.0.0.1|" + activePort.ToString() + "|\r\n" +
                "EPSV\r\n");
            string? missingSetup = reader.ReadLine();
            string? restart = reader.ReadLine();
            string? active = reader.ReadLine();
            string? passive = reader.ReadLine();
            int dataPort = ParseExtendedPassivePort(passive);

            WriteText(stream, "MLSD\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));
            string listing;
            using (TcpClient data = Connect(dataPort))
            using (StreamReader dataReader = CreateReader(data.GetStream()))
            {
                listing = dataReader.ReadToEnd();
            }
            string? listingComplete = reader.ReadLine();
            WriteText(stream,
                "AUTH TLS\r\n" +
                "PBSZ 0\r\n" +
                "PROT P\r\n" +
                "CCC\r\n" +
                "HOST ftp.matrix\r\n" +
                "QUIT\r\n");
            string? unavailableTls = reader.ReadLine();
            string? buffer = reader.ReadLine();
            string? protection = reader.ReadLine();
            string? clear = reader.ReadLine();
            string? lateHost = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(feature => feature.Name),
                    Is.EqualTo(new[]
                    {
                        "UTF8", "LANG", "MDTM", "SIZE", "REST STREAM",
                        "TVFS", "MLST", "HOST"
                    }));
                Assert.That(FindFeature(features, "LANG").Parameters,
                    Is.EqualTo("en*;fr"));
                Assert.That(selectedHost, Is.EqualTo("220 Matrix ready"));
                Assert.That(language, Is.EqualTo("200 Language set to fr."));
                Assert.That(options, Is.EqualTo("200 MLST OPTS type;size;"));
                Assert.That(user,
                    Is.EqualTo("331 User name accepted; password required."));
                Assert.That(password,
                    Is.EqualTo("332 Account information required."));
                Assert.That(account,
                    Is.EqualTo("230 User logged in; proceed."));
                Assert.That(size, Is.EqualTo("213 4"));
                Assert.That(modified, Is.EqualTo("213 20240203040506"));
                Assert.That(metadata.Name, Is.EqualTo("/item.txt"));
                Assert.That(metadata.Facts.Select(fact => fact.Name),
                    Is.EqualTo(new[] { "type", "size" }));
                Assert.That(missingSetup, Is.EqualTo(
                    "425 Use PORT, EPRT, PASV, or EPSV before a directory listing."));
                Assert.That(restart, Is.EqualTo("350 Restart marker accepted."));
                Assert.That(active, Is.EqualTo("200 EPRT command successful"));
                Assert.That(passive,
                    Does.StartWith("229 Entering Extended Passive Mode ("));
                Assert.That(listing, Does.Contain(" item.txt\r\n"));
                Assert.That(listingComplete, Is.EqualTo(
                    "226 Closing data connection; transfer complete"));
                Assert.That(unavailableTls, Is.EqualTo(
                    "534 TLS is not available for this FTP listener."));
                Assert.That(buffer, Is.EqualTo(
                    "503 PBSZ requires an active security association before CCC."));
                Assert.That(protection, Is.EqualTo(
                    "503 PROT requires a protected control channel and a completed PBSZ negotiation."));
                Assert.That(clear, Is.EqualTo(
                    "503 CCC requires a completed security exchange."));
                Assert.That(lateHost, Is.EqualTo(
                    "503 HOST must be issued before the user is authenticated."));
                Assert.That(quit, Is.EqualTo(
                    "221 FTP service closing control connection."));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private FtpServer CreateServer(FtpVirtualHostOptions host)
    {
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = true;
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new ScriptedServerAuthenticator(),
            authorizer,
            new DefaultFtpHostResolver(new[] { host }),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink());
    }

    private static FtpVirtualHostOptions CreateHost()
    {
        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = "ftp.matrix";
        host.Greeting = "Matrix ready";
        host.RootPath = "/";
        host.Features.Features = new[]
        {
            CreateFeature("LANG", "en*;fr"),
            CreateFeature("MLST", "type*;size*;modify;")
        };
        return host;
    }

    private static FtpFeature CreateFeature(string name, string parameters)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = name;
        feature.Parameters = parameters;
        feature.RawLine = " " + name + " " + parameters;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    private static FtpFeature FindFeature(
        FtpFeatureSet features,
        string name)
    {
        return features.Features.Single(feature =>
            string.Equals(feature.Name, name, StringComparison.Ordinal));
    }

    private static FtpServerOptions CreateOptions(
        int port,
        FtpVirtualHostOptions host,
        int passivePort)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
        options.VirtualHosts = new[] { host };
        options.DefaultHostName = host.HostName;
        if (passivePort > 0)
        {
            options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
            options.DataConnection.PassivePortStart = passivePort;
            options.DataConnection.PassivePortEnd = passivePort;
        }
        options.MaximumSessionsPerAddress = 128;
        options.MaximumSessionsPerUser = 128;
        options.MaximumCommandsPerMinute = 1000;
        options.MaximumMalformedLines = 100;
        return options;
    }

    private static FtpReply ReadMultilineReply(StreamReader reader)
    {
        string? opening = reader.ReadLine();
        Assert.That(opening, Is.Not.Null);
        Assert.That(opening![3], Is.EqualTo('-'));
        string code = opening.Substring(0, 3);
        StringBuilder text = new StringBuilder();
        text.Append(opening);
        text.Append("\r\n");
        string? line = reader.ReadLine();
        while (line != null && !line.StartsWith(
            code + " ", StringComparison.Ordinal))
        {
            text.Append(line);
            text.Append("\r\n");
            line = reader.ReadLine();
        }
        Assert.That(line, Is.Not.Null);
        text.Append(line);
        text.Append("\r\n");
        return new FtpReplyParser(
            FtpDefaults.MaximumReplyBytes,
            FtpDefaults.MaximumReplyLines,
            true).ParseReplyText(text.ToString());
    }

    private static int ParseExtendedPassivePort(string? reply)
    {
        Assert.That(reply, Is.Not.Null);
        string[] fields = reply!.Split('|');
        Assert.That(fields, Has.Length.GreaterThan(3));
        return int.Parse(
            fields[3], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static TcpClient Connect(int port)
    {
        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        client.Connect(IPAddress.Loopback, port);
        return client;
    }

    private static StreamReader CreateReader(Stream stream)
    {
        stream.ReadTimeout = 5000;
        return new StreamReader(
            stream, new UTF8Encoding(false, true), false, 1024, true);
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static int ReserveDifferentPort(params int[] otherPorts)
    {
        int port = ReserveAvailablePort();
        while (otherPorts.Contains(port))
        {
            port = ReserveAvailablePort();
        }
        return port;
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation =
            new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerLoginProviderProxy>();
    }
}
