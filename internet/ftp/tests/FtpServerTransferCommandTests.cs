using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerTransferCommandTests
{
    [Test]
    public void PRET_is_advertised_and_enforces_exact_pre_setup_intent()
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            controlPort, passivePort, FtpDataConnectionMode.Passive);
        options.EnablePret = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(controlPort);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));

            WriteText(stream, "USER alice\r\nPASS secret\r\nACCT billing\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();

            WriteText(stream, "FEAT\r\n");
            List<string> features = new List<string>();
            string? featureLine = reader.ReadLine();
            while (featureLine != null)
            {
                features.Add(featureLine);
                if (featureLine == "211 End")
                {
                    break;
                }
                featureLine = reader.ReadLine();
            }

            WriteText(stream,
                "EPSV\r\n" +
                "PRET RETR /folder/file name.bin\r\n" +
                "EPSV\r\n" +
                "RETR /folder/other.bin\r\n" +
                "PRET LIST /folder name\r\n" +
                "EPSV\r\n" +
                "PRET LIST /folder name\r\n" +
                "QUIT\r\n");
            string? missing = reader.ReadLine();
            string? preparedRetrieval = reader.ReadLine();
            string? passiveRetrieval = reader.ReadLine();
            string? mismatch = reader.ReadLine();
            string? preparedListing = reader.ReadLine();
            string? passiveListing = reader.ReadLine();
            string? late = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features, Does.Contain(" PRET"));
                Assert.That(missing, Does.StartWith("503 "));
                Assert.That(preparedRetrieval, Does.StartWith("200 "));
                Assert.That(passiveRetrieval, Does.StartWith("229 "));
                Assert.That(mismatch, Does.StartWith("550 "));
                Assert.That(preparedListing, Does.StartWith("200 "));
                Assert.That(passiveListing, Does.StartWith("229 "));
                Assert.That(late, Does.StartWith("503 "));
                Assert.That(quit, Does.StartWith("221 "));
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
    public void PRET_is_rejected_when_the_server_option_is_disabled()
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            controlPort, passivePort, FtpDataConnectionMode.Passive);

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(controlPort);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "PRET RETR /file.bin\r\nQUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? pret = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(pret, Does.StartWith("502 "));
                Assert.That(quit, Does.StartWith("221 "));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Built_in_transfer_setup_commands_apply_state_and_exact_replies()
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(controlPort);
        int activePort = ReserveDifferentPort(controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            controlPort, passivePort, FtpDataConnectionMode.Automatic);

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(controlPort);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));

            string portArgument = "127,0,0,1," +
                (activePort / 256).ToString() + "," +
                (activePort % 256).ToString();
            WriteText(stream,
                "USER alice\r\n" +
                "PASS secret\r\n" +
                "ACCT billing\r\n" +
                "TYPE I\r\n" +
                "STRU R\r\n" +
                "TYPE A\r\n" +
                "STRU R\r\n" +
                "MODE B\r\n" +
                "ALLO 4096 R 128\r\n" +
                "REST checkpoint\r\n" +
                "PORT " + portArgument + "\r\n" +
                "PASV\r\n" +
                "EPRT |3|127.0.0.1|" + activePort.ToString() + "|\r\n" +
                "EPSV 3\r\n" +
                "EPSV\r\n" +
                "EPSV ALL\r\n" +
                "PORT " + portArgument + "\r\n" +
                "ABOR\r\n" +
                "ALLO 1\r\n" +
                "QUIT\r\n");

            string? user = reader.ReadLine();
            string? password = reader.ReadLine();
            string? account = reader.ReadLine();
            string? typeImage = reader.ReadLine();
            string? invalidStructure = reader.ReadLine();
            string? typeAscii = reader.ReadLine();
            string? structure = reader.ReadLine();
            string? mode = reader.ReadLine();
            string? allocation = reader.ReadLine();
            string? restart = reader.ReadLine();
            string? active = reader.ReadLine();
            string? passive = reader.ReadLine();
            string? unsupportedActiveProtocol = reader.ReadLine();
            string? unsupportedPassiveProtocol = reader.ReadLine();
            string? extendedPassive = reader.ReadLine();
            string? extendedPassiveOnly = reader.ReadLine();
            string? blockedActive = reader.ReadLine();
            string? abort = reader.ReadLine();
            string? postAbortAllocation = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(user,
                    Is.EqualTo("331 User name accepted; password required."));
                Assert.That(password,
                    Is.EqualTo("332 Account information required."));
                Assert.That(account,
                    Is.EqualTo("230 User logged in; proceed."));
                Assert.That(typeImage,
                    Is.EqualTo("200 Transfer representation accepted."));
                Assert.That(invalidStructure,
                    Is.EqualTo(
                        "504 The requested transfer parameter combination is not supported."));
                Assert.That(typeAscii,
                    Is.EqualTo("200 Transfer representation accepted."));
                Assert.That(structure,
                    Is.EqualTo("200 File structure accepted."));
                Assert.That(mode,
                    Is.EqualTo("200 Transfer mode accepted."));
                Assert.That(allocation,
                    Is.EqualTo("200 Allocation request accepted."));
                Assert.That(restart,
                    Is.EqualTo("350 Restart marker accepted."));
                Assert.That(active,
                    Is.EqualTo("200 PORT command successful"));
                Assert.That(passive, Does.StartWith("227 Entering Passive Mode ("));
                Assert.That(unsupportedActiveProtocol, Does.StartWith("522 "));
                Assert.That(unsupportedPassiveProtocol, Does.StartWith("522 "));
                Assert.That(extendedPassive,
                    Does.StartWith("229 Entering Extended Passive Mode ("));
                Assert.That(extendedPassiveOnly,
                    Is.EqualTo("200 EPSV ALL accepted."));
                Assert.That(blockedActive, Does.StartWith("503 "));
                Assert.That(abort,
                    Is.EqualTo("225 No transfer is in progress."));
                Assert.That(postAbortAllocation,
                    Is.EqualTo("200 Allocation request accepted."));
                Assert.That(quit,
                    Is.EqualTo("221 FTP service closing control connection."));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [TestCase(FtpDataConnectionMode.Passive, "PORT 127,0,0,1,200,1")]
    [TestCase(FtpDataConnectionMode.Active, "PASV")]
    public void Data_connection_mode_returns_not_implemented_for_disabled_setup(
        FtpDataConnectionMode mode,
        string setupCommand)
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(controlPort);
        FtpServer server = CreateServer();
        FtpServerOptions options = CreateOptions(
            controlPort, passivePort, mode);

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(controlPort);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                setupCommand + "\r\nQUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? setup = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(setup, Does.StartWith("502 "));
                Assert.That(quit, Does.StartWith("221 "));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Abort_during_transfer_returns_426_then_226_and_restores_authentication()
    {
        int port = ReserveAvailablePort();
        using TcpListener listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        using TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.Connect(IPAddress.Loopback, port);
        using Socket socket = listener.AcceptSocket();
        FtpServerOptions options = CreateOptions(
            port, ReserveDifferentPort(port), FtpDataConnectionMode.Automatic);
        FtpServerSessionContext connection = new FtpServerSessionContext(
            socket,
            options.Listeners[0],
            new FtpServerLimitManager(options));
        FtpServerSession session = new FtpServerSession(
            connection,
            options,
            CreateProxy<IFtpFileSystem>(),
            new ScriptedServerAuthenticator(),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink(),
            commandContext =>
                FtpServerLoginCommandHandler.HandleCommand(commandContext));
        session.State.OpenConnection();
        session.State.State.SessionState = FtpSessionState.Transferring;
        session.State.State.TransferInProgress = true;
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("ABOR");
        FtpCommandResult result = FtpServerTransferCommandHandler.HandleCommand(
            new FtpServerCommandContext(session, command));

        Assert.That(result.Replies, Has.Length.EqualTo(2));
        session.State.ApplyCommandReply(command, result.Replies[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Replies[0].Code,
                Is.EqualTo(FtpReplyCodes.ConnectionClosedTransferAborted));
            Assert.That(result.Replies[1].Code,
                Is.EqualTo(FtpReplyCodes.ClosingDataConnection));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.State.State.TransferInProgress, Is.False);
            Assert.That(session.State.State.SessionState,
                Is.EqualTo(FtpSessionState.Authenticated));
        });
    }

    [Test]
    public void Abort_without_transfer_preserves_unauthenticated_state()
    {
        FtpSessionStateMachine state = new FtpSessionStateMachine();
        state.OpenConnection();
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("ABOR");
        state.ApplyCommandReply(
            command,
            FtpServerSession.CreateReply(
                FtpReplyCodes.DataConnectionOpenNoTransfer,
                "No transfer is in progress."));

        Assert.Multiple(() =>
        {
            Assert.That(state.State.SessionState,
                Is.EqualTo(FtpSessionState.Connected));
            Assert.That(state.State.TransferInProgress, Is.False);
            Assert.That(state.State.DataConnectionConfigured, Is.False);
        });
    }

    private static FtpServer CreateServer()
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            new ScriptedServerAuthenticator(),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink());
    }

    private static FtpServerOptions CreateOptions(
        int controlPort,
        int passivePort,
        FtpDataConnectionMode mode)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = controlPort;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new FtpListenerOptions[] { listener };
        options.DataConnection.Mode = mode;
        options.DataConnection.PassivePortStart = passivePort;
        options.DataConnection.PassivePortEnd = passivePort;
        return options;
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
            stream, Encoding.ASCII, false, 1024, true);
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static int ReserveDifferentPort(int otherPort)
    {
        int port = ReserveAvailablePort();
        while (port == otherPort)
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
