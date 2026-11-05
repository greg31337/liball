using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpClientReconnectTests
{
    [Test]
    public void Reconnect_restores_login_encoding_directory_transfer_parameters_and_options()
    {
        using TcpListener listener = StartListener();
        List<string> firstCommands = new List<string>();
        List<string> secondCommands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient first = listener.AcceptTcpClient();
            using NetworkStream firstStream = first.GetStream();
            RunFeatureLogin(firstStream, firstCommands);
            Receive(firstStream, firstCommands, "200 UTF8 OFF\r\n");
            Receive(firstStream, firstCommands, "200 MLST OPTS size;\r\n");
            Receive(firstStream, firstCommands, "200 XMODE FAST\r\n");
            Receive(firstStream, firstCommands, "200 TYPE accepted\r\n");
            Receive(firstStream, firstCommands, "200 STRU accepted\r\n");
            Receive(firstStream, firstCommands, "200 MODE accepted\r\n");
            Receive(firstStream, firstCommands, "250 CWD accepted\r\n");
            Receive(firstStream, firstCommands, "221 Goodbye\r\n");

            using TcpClient second = listener.AcceptTcpClient();
            using NetworkStream secondStream = second.GetStream();
            RunFeatureLogin(secondStream, secondCommands);
            Receive(secondStream, secondCommands, "200 UTF8 OFF\r\n");
            Receive(secondStream, secondCommands, "200 MLST OPTS size;\r\n");
            Receive(secondStream, secondCommands, "200 XMODE FAST\r\n");
            Receive(secondStream, secondCommands, "200 TYPE accepted\r\n");
            Receive(secondStream, secondCommands, "200 STRU accepted\r\n");
            Receive(secondStream, secondCommands, "200 MODE accepted\r\n");
            Receive(secondStream, secondCommands, "250 CWD accepted\r\n");
            Receive(secondStream, secondCommands, "200 NOOP okay\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        client.AuthenticateUser(credentials);
        ApplyOption(client, FtpFeatureNames.Utf8, "OFF");
        ApplyOption(client, FtpFeatureNames.Mlst, "size;");
        ApplyOption(client, "XMODE", "FAST");
        FtpTransferOptions representation = new FtpTransferOptions();
        representation.Type = FtpTransferType.Ebcdic;
        representation.TextFormat = FtpTextFormat.CarriageControl;
        client.SetTransferRepresentation(representation);
        client.SetFileStructure(FtpFileStructure.Record);
        client.SetTransferMode(FtpTransferMode.Block);
        client.ChangeWorkingDirectory("/work");
        client.Disconnect();

        client.Reconnect();
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstCommands, Is.EqualTo(new[]
            {
                "FEAT", "OPTS UTF8 ON", "OPTS MLST type;size;modify;",
                "HOST tenant.example", "USER alice", "PASS secret",
                "OPTS UTF8 OFF", "OPTS MLST size;", "OPTS XMODE FAST",
                "TYPE E C", "STRU R", "MODE B", "CWD /work", "QUIT",
            }));
            Assert.That(secondCommands, Is.EqualTo(new[]
            {
                "FEAT", "OPTS UTF8 ON", "OPTS MLST type;size;modify;",
                "HOST tenant.example", "USER alice", "PASS secret",
                "OPTS UTF8 OFF", "OPTS MLST size;", "OPTS XMODE FAST",
                "TYPE E C", "STRU R", "MODE B", "CWD /work", "NOOP",
            }));
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(client.State.Session.State.SelectedHost,
                Is.EqualTo("tenant.example"));
            Assert.That(client.State.Connection!.ControlProtected, Is.False);
            Assert.That(client.State.Connection.Utf8Enabled, Is.False);
            Assert.That(client.State.Session.State.CurrentDirectory, Is.EqualTo("/work"));
            Assert.That(client.State.Session.State.TransferType,
                Is.EqualTo(FtpTransferType.Ebcdic));
            Assert.That(client.State.Session.State.TextFormat,
                Is.EqualTo(FtpTextFormat.CarriageControl));
            Assert.That(client.State.Session.State.FileStructure,
                Is.EqualTo(FtpFileStructure.Record));
            Assert.That(client.State.Session.State.TransferMode,
                Is.EqualTo(FtpTransferMode.Block));
            Assert.That(client.State.FeatureState.MlstFacts,
                Is.EqualTo(new[] { "size" }));
            Assert.That(client.State.FeatureState.OptionProcessor!.State.ExtensionOptions,
                Has.Length.EqualTo(1));
        });

        client.AbortConnection();
    }

    [Test]
    public void Reconnect_reestablishes_explicit_TLS_and_private_data_protection()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using TcpListener listener = StartListener();
        List<string> firstCommands = new List<string>();
        List<string> secondCommands = new List<string>();
        Task server = Task.Run(() =>
        {
            RunTlsConnection(listener, certificate, firstCommands, true);
            RunTlsConnection(listener, certificate, secondCommands, false);
        });
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());
        client.Connect(CreateTlsOptions(listener));
        FtpCommandResult initialProtection =
            client.SetDataProtection(FtpDataProtectionLevel.Private);
        client.Disconnect();

        client.Reconnect();
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstCommands, Is.EqualTo(new[]
            {
                "AUTH TLS", "FEAT", "PBSZ 0", "PROT P", "QUIT",
            }));
            Assert.That(secondCommands, Is.EqualTo(new[]
            {
                "AUTH TLS", "FEAT", "PBSZ 0", "PROT P", "NOOP",
            }));
            Assert.That(initialProtection.Succeeded, Is.True);
            Assert.That(noop.Succeeded, Is.True);
            Assert.That(client.State.Connection!.ControlProtected, Is.True);
            Assert.That(client.State.Session.State.SecurityExchangeIsTls, Is.True);
            Assert.That(client.State.Session.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
        });

        client.AbortConnection();
    }

    private static void RunFeatureLogin(Stream stream, List<string> commands)
    {
        WriteText(stream, "220 Ready\r\n");
        Receive(stream, commands,
            "211-Features:\r\n" +
            " UTF8\r\n" +
            " MLST Type*;Size;Modify*;\r\n" +
            " MODE Z\r\n" +
            "211 End\r\n");
        Receive(stream, commands, "200 UTF8 ON\r\n");
        Receive(stream, commands, "200 MLST OPTS type;size;modify;\r\n");
        Receive(stream, commands, "220 HOST selected\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void RunTlsConnection(
        TcpListener listener,
        X509Certificate2 certificate,
        List<string> commands,
        bool quit)
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        using NetworkStream transport = accepted.GetStream();
        WriteText(transport, "220 Clear greeting\r\n");
        commands.Add(ReadCommand(transport));
        WriteText(transport, "234 Begin TLS\r\n");
        FtpTlsServerStream server = CreateTlsServer(transport, certificate);
        FtpReply authorization = CreateReply(234, "Begin TLS");
        FtpTlsStreamResult result = server.UpgradeExplicitServer(authorization);
        Stream stream = result.Stream!;
        Receive(stream, commands, "502 FEAT unavailable\r\n");
        Receive(stream, commands, "200 PBSZ accepted\r\n");
        Receive(stream, commands, "200 PROT accepted\r\n");
        if (quit)
        {
            Receive(stream, commands, "221 Goodbye\r\n");
        }
        else
        {
            Receive(stream, commands, "200 NOOP okay\r\n");
        }
        server.CloseServerTls();
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

    private static void ApplyOption(FtpClient client, string name, string value)
    {
        FtpOptionRequest option = new FtpOptionRequest();
        option.CommandName = name;
        option.Value = value;
        FtpCommandResult result = client.ApplyOption(option);
        Assert.That(result.Succeeded, Is.True);
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClientOptions CreatePlainOptions(TcpListener listener)
    {
        FtpClientOptions options = CreateBaseOptions(listener);
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = true;
        options.VirtualHost = "tenant.example";
        return options;
    }

    private static FtpClientOptions CreateTlsOptions(TcpListener listener)
    {
        FtpClientOptions options = CreateBaseOptions(listener);
        options.SecurityMode = FtpSecurityMode.ExplicitTlsRequired;
        options.NegotiateFeatures = false;
        options.Tls.Protocols = SslProtocols.Tls12;
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.TargetHost = "localhost";
        return options;
    }

    private static FtpClientOptions CreateBaseOptions(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        options.Timeouts.TlsHandshakeTimeoutMilliseconds = 5000;
        return options;
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
                throw new EndOfStreamException();
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException();
                }
                return Encoding.UTF8.GetString(bytes.ToArray());
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

    private sealed class AcceptingCertificateValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            FtpCertificateValidationResult result =
                new FtpCertificateValidationResult();
            result.Handled = true;
            result.Accepted = request.Certificate != null;
            return result;
        }
    }
}
