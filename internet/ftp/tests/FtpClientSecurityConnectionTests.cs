using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace ftp.Tests;

public class FtpClientSecurityConnectionTests
{
    [Test]
    public void Explicit_required_connection_upgrades_only_after_AUTH_is_accepted()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        string? command = null;
        Task server = Task.Run(() =>
        {
            command = RunExplicitTlsServer(listener, certificate, 234, true);
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        client.Connect(CreateSecureOptions(port, FtpSecurityMode.ExplicitTlsRequired));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.EqualTo("AUTH TLS"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Connection!.ControlProtected, Is.True);
            Assert.That(client.State.Connection.PlaintextFallback, Is.False);
            Assert.That(client.State.Connection.TlsResult!.Authenticated, Is.True);
            Assert.That(client.State.Connection.TlsResult.ExplicitUpgrade, Is.True);
            Assert.That(client.State.Session.State.ControlProtected, Is.True);
            Assert.That(client.State.TlsCoordinator!.State.FeatureRefreshRequired, Is.False);
            Assert.That(client.State.TlsCoordinator.State.FeatureRefreshComplete, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Implicit_TLS_authenticates_before_reading_the_server_greeting()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        Task server = Task.Run(() =>
        {
            RunImplicitTlsServer(listener, certificate);
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        client.Connect(CreateSecureOptions(port, FtpSecurityMode.ImplicitTls));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Connection!.Greeting!.Text, Is.EqualTo("Implicit TLS ready"));
            Assert.That(client.State.Connection.ControlProtected, Is.True);
            Assert.That(client.State.Connection.TlsResult!.ImplicitTls, Is.True);
            Assert.That(client.State.Session.State.SecurityExchangeComplete, Is.True);
            Assert.That(client.State.Session.State.SecurityExchangeIsTls, Is.True);
            Assert.That(client.State.Session.State.ControlProtected, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Opportunistic_mode_falls_back_only_after_a_clean_negative_AUTH_reply()
    {
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        string? command = null;
        Task server = Task.Run(() =>
        {
            command = RunRejectedExplicitServer(listener, 502);
        });
        FtpClientOptions options = CreateSecureOptions(
            port, FtpSecurityMode.ExplicitTlsOpportunistic);
        options.AllowPlaintextCredentials = true;
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.EqualTo("AUTH TLS"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Connection!.ControlProtected, Is.False);
            Assert.That(client.State.Connection.PlaintextFallback, Is.True);
            Assert.That(client.State.TlsCoordinator!.State.PlaintextFallback, Is.True);
            Assert.That(client.State.Session.State.ControlProtected, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Required_mode_closes_instead_of_downgrading_after_AUTH_rejection()
    {
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        Task server = Task.Run(() =>
        {
            RunRejectedExplicitServer(listener, 534);
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        FtpTlsException failure = Assert.Throws<FtpTlsException>(() =>
            client.Connect(CreateSecureOptions(
                port, FtpSecurityMode.ExplicitTlsRequired)))!;
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("534"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Opportunistic_mode_never_falls_back_after_an_accepted_AUTH_handshake_failure()
    {
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Clear greeting\r\n");
            ReadCommand(stream);
            WriteText(stream, "234 Begin TLS\r\n");
        });
        FtpClientOptions options = CreateSecureOptions(
            port, FtpSecurityMode.ExplicitTlsOpportunistic);
        options.AllowPlaintextCredentials = true;
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        Assert.Throws<FtpTlsException>(() => client.Connect(options));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
            Assert.That(client.State.Session.State.ControlProtected, Is.False);
        });
    }

    [Test]
    public void Profile_probe_tests_implicit_explicit_and_plain_without_credentials()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        string? explicitCommand = null;
        Task server = Task.Run(() =>
        {
            RunImplicitTlsServer(listener, certificate);
            explicitCommand = RunExplicitTlsServer(listener, certificate, 234, false);
            RunPlainServer(listener);
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());
        FtpClientOptions options = CreateSecureOptions(
            port, FtpSecurityMode.ExplicitTlsRequired);
        options.Credentials.UserName = "must-not-be-sent";
        options.Credentials.Password = "secret-must-not-be-sent";

        FtpConnectionProbeResult result = client.ProbeConnectionProfiles(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.FailureMessage, Is.Null);
            Assert.That(result.Profiles, Has.Length.EqualTo(3));
            Assert.That(result.Profiles[0].SecurityMode, Is.EqualTo(FtpSecurityMode.ImplicitTls));
            Assert.That(result.Profiles[0].Viable, Is.True);
            Assert.That(result.Profiles[0].ControlProtected, Is.True);
            Assert.That(result.Profiles[1].SecurityMode, Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(result.Profiles[1].Viable, Is.True);
            Assert.That(result.Profiles[1].ControlProtected, Is.True);
            Assert.That(result.Profiles[2].SecurityMode, Is.EqualTo(FtpSecurityMode.Plain));
            Assert.That(result.Profiles[2].Viable, Is.True);
            Assert.That(result.Profiles[2].ControlProtected, Is.False);
            Assert.That(explicitCommand, Is.EqualTo("AUTH TLS"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
            Assert.That(client.State.Options, Is.Null);
        });
    }

    [Test]
    public void Public_AUTH_TLS_and_CCC_upgrade_refresh_and_restore_the_control_stream()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        List<string> commands = new List<string>();
        Task serverTask = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream transport = accepted.GetStream();
            WriteText(transport, "220 Clear FTP ready\r\n");
            commands.Add(ReadCommand(transport));
            WriteText(transport, "234 Begin TLS\r\n");
            FtpTlsServerStream tlsServer = CreateTlsServer(
                transport, certificate);
            FtpTlsStreamResult tls = tlsServer.UpgradeExplicitServer(
                CreateReply(234, "Begin TLS"));
            Stream secureStream = tls.Stream!;
            commands.Add(ReadCommand(secureStream));
            WriteText(secureStream,
                "211-Features:\r\n" +
                " PBSZ\r\n" +
                " PROT\r\n" +
                "211 End\r\n");
            commands.Add(ReadCommand(secureStream));
            WriteText(secureStream, "200 Continue clearly\r\n");
            tlsServer.ShutdownServerTlsForClearControl();
            commands.Add(ReadCommand(transport));
            WriteText(transport, "200 Clear NOOP accepted\r\n");
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());
        client.Connect(CreateSecureOptions(port, FtpSecurityMode.Plain));
        FtpTlsOptions tlsOptions = new FtpTlsOptions();
        tlsOptions.Protocols = SslProtocols.Tls12;
        tlsOptions.CheckCertificateRevocation = false;
        tlsOptions.TargetHost = "localhost";
        tlsOptions.AllowClearCommandChannel = true;

        FtpCommandResult authorization = client.UpgradeControlTls(tlsOptions);
        tlsOptions.AllowClearCommandChannel = false;
        FtpCommandResult clear = client.ClearControlTls();
        FtpCommandResult noop = client.SendNoOperation();
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "AUTH TLS", "FEAT", "CCC", "NOOP",
            }));
            Assert.That(authorization.Succeeded, Is.True);
            Assert.That(authorization.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.SecurityExchangeComplete));
            Assert.That(clear.Succeeded, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.State.Features.Negotiated, Is.True);
            Assert.That(client.State.Connection!.ControlProtected, Is.False);
            Assert.That(client.State.Connection.TlsClient, Is.Null);
            Assert.That(client.State.Connection.Stream,
                Is.SameAs(client.State.Connection.TransportStream));
            Assert.That(client.State.Session.State.SecurityExchangeIsTls, Is.True);
            Assert.That(client.State.Session.State.ClearCommandChannelCompleted,
                Is.True);
            Assert.That(client.State.Options!.SecurityMode,
                Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(client.State.Options.Tls.AllowClearCommandChannel,
                Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Connected));
        });

        client.AbortConnection();
    }

    [Test]
    public void Public_AUTH_TLS_rejection_returns_the_reply_and_closes_the_session()
    {
        using TcpListener listener = StartListener();
        int port = GetPort(listener);
        string? command = null;
        Task serverTask = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            command = ReadCommand(stream);
            WriteText(stream, "534 TLS unavailable\r\n");
            Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());
        client.Connect(CreateSecureOptions(port, FtpSecurityMode.Plain));
        FtpTlsOptions tlsOptions = new FtpTlsOptions();
        tlsOptions.Protocols = SslProtocols.Tls12;
        tlsOptions.CheckCertificateRevocation = false;
        tlsOptions.TargetHost = "localhost";

        FtpCommandResult result = client.UpgradeControlTls(tlsOptions);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.EqualTo("AUTH TLS"));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ConnectionClosed, Is.True);
            Assert.That(result.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.RequestDeniedByPolicy));
            Assert.That(client.State.Connection, Is.Null);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Closed));
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

    private static FtpClientOptions CreateSecureOptions(
        int port,
        FtpSecurityMode securityMode)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = securityMode;
        options.NegotiateFeatures = false;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        options.Timeouts.TlsHandshakeTimeoutMilliseconds = 5000;
        options.Tls.Protocols = SslProtocols.Tls12;
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.TargetHost = "localhost";
        return options;
    }

    private static string RunExplicitTlsServer(
        TcpListener listener,
        X509Certificate2 certificate,
        int replyCode,
        bool readFeature)
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        using NetworkStream stream = accepted.GetStream();
        WriteText(stream, "220 Clear greeting\r\n");
        string command = ReadCommand(stream);
        WriteText(stream, replyCode + " Begin TLS\r\n");
        FtpReply reply = CreateReply(replyCode, "Begin TLS");
        FtpTlsServerStream server = CreateTlsServer(stream, certificate);
        FtpTlsStreamResult result = server.UpgradeExplicitServer(reply);
        if (readFeature)
        {
            string featureCommand = ReadCommand(result.Stream!);
            if (featureCommand != "FEAT")
            {
                throw new InvalidDataException(
                    "The explicit TLS client did not refresh FEAT after its handshake.");
            }
            WriteText(result.Stream!, "502 FEAT unavailable\r\n");
        }
        server.CloseServerTls();
        return command;
    }

    private static void RunImplicitTlsServer(
        TcpListener listener,
        X509Certificate2 certificate)
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        using NetworkStream stream = accepted.GetStream();
        FtpTlsServerStream server = CreateTlsServer(stream, certificate);
        FtpTlsStreamResult result = server.AuthenticateImplicitServer();
        WriteText(result.Stream!, "220 Implicit TLS ready\r\n");
        server.CloseServerTls();
    }

    private static string RunRejectedExplicitServer(
        TcpListener listener,
        int replyCode)
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        using NetworkStream stream = accepted.GetStream();
        WriteText(stream, "220 Clear greeting\r\n");
        string command = ReadCommand(stream);
        WriteText(stream, replyCode + " TLS unavailable\r\n");
        return command;
    }

    private static void RunPlainServer(TcpListener listener)
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        using NetworkStream stream = accepted.GetStream();
        WriteText(stream, "220 Plain ready\r\n");
    }

    private static FtpTlsServerStream CreateTlsServer(
        Stream stream,
        X509Certificate2 certificate)
    {
        FtpTlsServerStreamRequest request = new FtpTlsServerStreamRequest();
        request.TransportStream = stream;
        request.LeaveTransportOpen = true;
        request.HandshakeTimeoutMilliseconds = 5000;
        request.Options!.Protocols = SslProtocols.Tls12;
        request.Options.CheckCertificateRevocation = false;
        request.Options.ServerCertificate = certificate;
        return new FtpTlsServerStream(request);
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
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
                throw new EndOfStreamException("The client closed before sending AUTH TLS.");
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException("The client command did not use CRLF.");
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

    private sealed class AcceptingCertificateValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            FtpCertificateValidationResult result = new FtpCertificateValidationResult();
            result.Handled = true;
            result.Accepted = request.Certificate != null;
            return result;
        }
    }
}
