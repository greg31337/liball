using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerDataChannelTests
{
    private string RootPath = "";

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-data-channel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        File.WriteAllText(Path.Combine(RootPath, "content.bin"), "payload");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [TestCase(false, "PORT")]
    [TestCase(false, "EPRT")]
    [TestCase(false, "PASV")]
    [TestCase(false, "EPSV")]
    [TestCase(true, "EPRT")]
    [TestCase(true, "EPSV")]
    public void Every_legal_IPv4_and_IPv6_setup_transfers_with_half_close(
        bool ipv6,
        string setupKind)
    {
        if (ipv6 && !Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not available on this platform.");
        }
        IPAddress address = ipv6 ?
            IPAddress.IPv6Loopback : IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            address, controlPort, passivePort, 5000);

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(address, controlPort);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "TYPE I\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 Transfer representation accepted."));

            byte[] received;
            string? setupReply;
            if (setupKind == "PORT" || setupKind == "EPRT")
            {
                using TcpListener dataListener = new TcpListener(address, 0);
                if (ipv6)
                {
                    dataListener.Server.DualMode = false;
                }
                dataListener.Start();
                int dataPort =
                    ((IPEndPoint)dataListener.LocalEndpoint).Port;
                string setup = CreateActiveSetup(
                    setupKind, address, dataPort);
                WriteText(stream, setup + "\r\nRETR content.bin\r\n");
                setupReply = reader.ReadLine();
                Assert.That(reader.ReadLine(), Is.EqualTo(
                    "150 File status okay; opening data connection"));
                using Socket data = dataListener.AcceptSocket();
                received = ReceiveAfterWriteHalfClose(data);
            }
            else
            {
                string setup = setupKind == "PASV" ?
                    "PASV" : ipv6 ? "EPSV 2" : "EPSV 1";
                WriteText(stream, setup + "\r\n");
                setupReply = reader.ReadLine();
                int dataPort = setupKind == "PASV" ?
                    ParsePassivePort(setupReply) :
                    ParseExtendedPassivePort(setupReply);
                WriteText(stream, "RETR content.bin\r\n");
                Assert.That(reader.ReadLine(), Is.EqualTo(
                    "150 File status okay; opening data connection"));
                using TcpClient dataClient = Connect(address, dataPort);
                received = ReceiveAfterWriteHalfClose(dataClient.Client);
            }
            string? completed = reader.ReadLine();
            WriteText(stream, "ABOR\r\nNOOP\r\nQUIT\r\n");
            string? lateAbort = reader.ReadLine();
            string? noOperation = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(setupReply, Does.StartWith(
                    setupKind == "PORT" || setupKind == "EPRT" ?
                        "200 " : setupKind == "PASV" ? "227 " : "229 "));
                Assert.That(Encoding.ASCII.GetString(received),
                    Is.EqualTo("payload"));
                Assert.That(completed, Is.EqualTo(
                    "226 Closing data connection; transfer complete"));
                Assert.That(lateAbort,
                    Is.EqualTo("225 No transfer is in progress."));
                Assert.That(noOperation,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(quit, Is.EqualTo(
                    "221 FTP service closing control connection."));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Masquerade_and_shared_passive_pool_exhaust_and_reuse_live_port()
    {
        int controlPort = ReserveAvailablePort(IPAddress.Loopback);
        int passivePort = ReserveDifferentPort(
            IPAddress.Loopback, controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            IPAddress.Loopback, controlPort, passivePort, 5000);
        options.Listeners[0].PassiveAdvertisedAddress = "198.51.100.77";

        try
        {
            server.StartServer(options);
            using TcpClient first = Connect(IPAddress.Loopback, controlPort);
            using NetworkStream firstStream = first.GetStream();
            using StreamReader firstReader = CreateReader(firstStream);
            Login(firstStream, firstReader);
            using TcpClient second = Connect(IPAddress.Loopback, controlPort);
            using NetworkStream secondStream = second.GetStream();
            using StreamReader secondReader = CreateReader(secondStream);
            Login(secondStream, secondReader);

            WriteText(firstStream, "PASV\r\n");
            string? masqueraded = firstReader.ReadLine();
            FtpPassiveReply parsed = ParsePassiveReply(masqueraded);
            WriteText(secondStream, "EPSV\r\n");
            string? exhausted = secondReader.ReadLine();
            WriteText(firstStream, "ABOR\r\n");
            string? releasedSetup = firstReader.ReadLine();
            Assert.That(SpinWait.SpinUntil(() =>
                server.Runtime.PassivePortPool!.ReservedCount == 0, 3000),
                Is.True);
            WriteText(secondStream, "EPSV\r\n");
            string? reused = secondReader.ReadLine();
            WriteText(secondStream, "ABOR\r\nQUIT\r\n");
            string? secondAbort = secondReader.ReadLine();
            string? secondQuit = secondReader.ReadLine();
            WriteText(firstStream, "QUIT\r\n");
            string? firstQuit = firstReader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(parsed.Endpoint.Host,
                    Is.EqualTo("198.51.100.77"));
                Assert.That(parsed.Endpoint.Port, Is.EqualTo(passivePort));
                Assert.That(exhausted, Is.EqualTo(
                    "425 The data connection could not be opened."));
                Assert.That(releasedSetup,
                    Is.EqualTo("225 No transfer is in progress."));
                Assert.That(reused,
                    Does.StartWith("229 Entering Extended Passive Mode ("));
                Assert.That(ParseExtendedPassivePort(reused),
                    Is.EqualTo(passivePort));
                Assert.That(secondAbort,
                    Is.EqualTo("225 No transfer is in progress."));
                Assert.That(secondQuit, Does.StartWith("221 "));
                Assert.That(firstQuit, Does.StartWith("221 "));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Peer_validation_and_passive_accept_timeout_preserve_control_session()
    {
        int controlPort = ReserveAvailablePort(IPAddress.Loopback);
        int passivePort = ReserveDifferentPort(
            IPAddress.Loopback, controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            IPAddress.Loopback, controlPort, passivePort, 100);
        options.DataConnection.RequireDataPeerMatch = true;

        try
        {
            server.StartServer(options);
            using (TcpClient mismatchControl =
                Connect(IPAddress.Loopback, controlPort))
            using (NetworkStream mismatchStream = mismatchControl.GetStream())
            using (StreamReader mismatchReader = CreateReader(mismatchStream))
            {
                Login(mismatchStream, mismatchReader);
                WriteText(mismatchStream,
                    "EPRT |1|127.0.0.2|50000|\r\n");
                string? rejected = mismatchReader.ReadLine();
                WriteText(mismatchStream, "NOOP\r\nQUIT\r\n");
                string? probe = mismatchReader.ReadLine();
                string? quit = mismatchReader.ReadLine();
                Assert.Multiple(() =>
                {
                    Assert.That(rejected, Is.EqualTo(
                        "501 The data-connection setup request is invalid or prohibited."));
                    Assert.That(probe,
                        Is.EqualTo("200 NOOP command successful."));
                    Assert.That(quit, Does.StartWith("221 "));
                });
            }

            Assert.That(SpinWait.SpinUntil(() =>
                server.Runtime.PassivePortPool!.ReservedCount == 0, 3000),
                Is.True);
            using TcpClient timeoutControl =
                Connect(IPAddress.Loopback, controlPort);
            using NetworkStream timeoutStream = timeoutControl.GetStream();
            using StreamReader timeoutReader = CreateReader(timeoutStream);
            Login(timeoutStream, timeoutReader);
            WriteText(timeoutStream, "EPSV\r\n");
            Assert.That(timeoutReader.ReadLine(), Does.StartWith("229 "));
            WriteText(timeoutStream, "RETR content.bin\r\n");
            Assert.That(timeoutReader.ReadLine(), Is.EqualTo(
                "150 File status okay; opening data connection"));
            string? timedOut = timeoutReader.ReadLine();
            WriteText(timeoutStream, "NOOP\r\nQUIT\r\n");
            string? timeoutProbe = timeoutReader.ReadLine();
            string? timeoutQuit = timeoutReader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(timedOut, Is.EqualTo(
                    "426 Data connection closed; transfer aborted."));
                Assert.That(timeoutProbe,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(timeoutQuit, Does.StartWith("221 "));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Protected_data_request_always_assigns_FTP_server_TLS_role()
    {
        int port = ReserveAvailablePort(IPAddress.Loopback);
        int passivePort = ReserveDifferentPort(IPAddress.Loopback, port);
        FtpServerOptions options = CreateOptions(
            IPAddress.Loopback, port, passivePort, 5000);
        FtpServerLimitManager limits = new FtpServerLimitManager(options);
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        FtpServerSessionContext connection =
            new FtpServerSessionContext(socket, options.Listeners[0], limits);
        FtpServerSession session = new FtpServerSession(
            connection,
            options,
            new LocalFtpFileSystem(RootPath),
            new ScriptedServerAuthenticator(),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink(),
            context => FtpServerLoginCommandHandler.HandleCommand(context));
        session.State.OpenConnection();
        FtpCommand command = FtpCommandParser.ParseDecodedCommand(
            "RETR content.bin");
        FtpServerCommandContext context =
            new FtpServerCommandContext(session, command);
        FtpClearDataStreamRequest clear = new FtpClearDataStreamRequest();

        FtpProtectedDataStreamRequest protectedRequest =
            FtpServerFileTransferCommandHandler.CreateProtectedRequest(
                context, clear);

        Assert.Multiple(() =>
        {
            Assert.That(protectedRequest.Role,
                Is.EqualTo(FtpDataChannelRole.Server));
            Assert.That(protectedRequest.Transfer, Is.SameAs(clear));
            Assert.That(protectedRequest.TlsOptions!.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(protectedRequest.CertificateProvider,
                Is.SameAs(session.ActiveCertificateProvider));
        });
    }

    private FtpServer CreateServer()
    {
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = true;
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new ScriptedServerAuthenticator(),
            authorizer,
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink());
    }

    private static FtpServerOptions CreateOptions(
        IPAddress address,
        int controlPort,
        int passivePort,
        int dataTimeout)
    {
        bool ipv6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = address.ToString();
        listener.Endpoint.Port = controlPort;
        listener.Endpoint.AddressFamily = ipv6 ?
            FtpAddressFamily.IPv6 : FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
        options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
        options.DataConnection.PassivePortStart = passivePort;
        options.DataConnection.PassivePortEnd = passivePort;
        options.Timeouts.DataConnectionTimeoutMilliseconds = dataTimeout;
        options.MaximumSessionsPerAddress = 16;
        options.MaximumSessionsPerUser = 16;
        return options;
    }

    private static string CreateActiveSetup(
        string setupKind,
        IPAddress address,
        int port)
    {
        if (setupKind == "PORT")
        {
            return "PORT 127,0,0,1," + (port / 256).ToString() + "," +
                (port % 256).ToString();
        }
        string protocol = address.AddressFamily ==
            AddressFamily.InterNetworkV6 ? "2" : "1";
        return "EPRT |" + protocol + "|" + address.ToString() + "|" +
            port.ToString() + "|";
    }

    private static byte[] ReceiveAfterWriteHalfClose(Socket socket)
    {
        using NetworkStream stream = new NetworkStream(socket, false);
        socket.Shutdown(SocketShutdown.Send);
        using MemoryStream received = new MemoryStream();
        stream.CopyTo(received);
        return received.ToArray();
    }

    private static FtpPassiveReply ParsePassiveReply(string? reply)
    {
        Assert.That(reply, Is.Not.Null);
        FtpReply parsed = new FtpReplyParser(
            FtpDefaults.MaximumReplyBytes,
            FtpDefaults.MaximumReplyLines,
            false).ParseReplyText(reply + "\r\n");
        return FtpEndpointCodec.ParsePassiveReply(parsed);
    }

    private static int ParsePassivePort(string? reply)
    {
        return ParsePassiveReply(reply).Endpoint.Port;
    }

    private static int ParseExtendedPassivePort(string? reply)
    {
        Assert.That(reply, Is.Not.Null);
        string[] fields = reply!.Split('|');
        Assert.That(fields, Has.Length.GreaterThan(3));
        return int.Parse(
            fields[3], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Login(Stream stream, StreamReader reader)
    {
        Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
        WriteText(stream, "USER alice\r\nPASS secret\r\nACCT billing\r\n");
        Assert.That(reader.ReadLine(),
            Is.EqualTo("331 User name accepted; password required."));
        Assert.That(reader.ReadLine(),
            Is.EqualTo("332 Account information required."));
        Assert.That(reader.ReadLine(),
            Is.EqualTo("230 User logged in; proceed."));
    }

    private static TcpClient Connect(IPAddress address, int port)
    {
        TcpClient client = new TcpClient(address.AddressFamily);
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        client.Connect(address, port);
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

    private static int ReserveDifferentPort(
        IPAddress address,
        params int[] otherPorts)
    {
        int port = ReserveAvailablePort(address);
        while (otherPorts.Contains(port))
        {
            port = ReserveAvailablePort(address);
        }
        return port;
    }

    private static int ReserveAvailablePort(IPAddress address)
    {
        using TcpListener reservation = new TcpListener(address, 0);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            reservation.Server.DualMode = false;
        }
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerLoginProviderProxy>();
    }
}
