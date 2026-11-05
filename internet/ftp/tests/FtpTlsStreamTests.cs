using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpTlsStreamTests
{
    [Test]
    public void Implicit_TLS_authenticates_before_the_server_greeting()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpTlsServerStream server = CreateServer(
            connection.ServerStream, certificate, false, false);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, null, false, false);

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.AuthenticateImplicitServer());
        FtpTlsStreamResult clientResult = client.AuthenticateImplicitClient();
        FtpTlsStreamResult serverResult = serverTask.GetAwaiter().GetResult();
        byte[] greeting = Encoding.ASCII.GetBytes("220 Service ready\r\n");
        serverResult.Stream!.Write(greeting, 0, greeting.Length);
        serverResult.Stream.Flush();
        byte[] received = ReadExact(clientResult.Stream!, greeting.Length);
        string certificateFingerprint =
            certificate.GetCertHashString(HashAlgorithmName.SHA256);
        FtpEvent diagnosticEvent = FtpTlsDiagnosticsFactory.CreateEvent(
            clientResult,
            FtpSecurityMode.ImplicitTls,
            FtpDataProtectionLevel.Private,
            FtpSessionState.Connected);

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.EqualTo(greeting));
            Assert.That(clientResult.Authenticated, Is.True);
            Assert.That(clientResult.Server, Is.False);
            Assert.That(clientResult.ImplicitTls, Is.True);
            Assert.That(clientResult.ExplicitUpgrade, Is.False);
            Assert.That(clientResult.Encrypted, Is.True);
            Assert.That(clientResult.Signed, Is.True);
            Assert.That(clientResult.TlsResumeAllowed, Is.True);
            Assert.That(clientResult.Protocol, Is.EqualTo(SslProtocols.Tls12));
            Assert.That(serverResult.Authenticated, Is.True);
            Assert.That(serverResult.Server, Is.True);
            Assert.That(serverResult.ImplicitTls, Is.True);
            Assert.That(serverResult.TlsResumeAllowed, Is.True);
            Assert.That(serverResult.Protocol, Is.EqualTo(SslProtocols.Tls12));
            Assert.That(clientResult.Diagnostics.Authenticated, Is.True);
            Assert.That(clientResult.Diagnostics.ServerRole, Is.False);
            Assert.That(clientResult.Diagnostics.ImplicitTls, Is.True);
            Assert.That(clientResult.Diagnostics.ExplicitUpgrade, Is.False);
            Assert.That(clientResult.Diagnostics.DataChannel, Is.False);
            Assert.That(clientResult.Diagnostics.Protocol, Is.EqualTo("Tls12"));
            Assert.That(clientResult.Diagnostics.CipherSuite, Is.Not.Empty);
            Assert.That(clientResult.Diagnostics.RemoteCertificateAvailable, Is.True);
            Assert.That(clientResult.Diagnostics.RemoteCertificateSha256Fingerprint,
                Is.EqualTo(certificateFingerprint));
            Assert.That(clientResult.Diagnostics.LocalCertificateAvailable, Is.False);
            Assert.That(serverResult.Diagnostics.ServerRole, Is.True);
            Assert.That(serverResult.Diagnostics.LocalCertificateAvailable, Is.True);
            Assert.That(serverResult.Diagnostics.RemoteCertificateAvailable, Is.False);
            Assert.That(diagnosticEvent.Kind, Is.EqualTo(FtpEventKind.Tls));
            Assert.That(diagnosticEvent.TlsDiagnostics,
                Is.Not.SameAs(clientResult.Diagnostics));
            Assert.That(diagnosticEvent.TlsDiagnostics!.Protocol, Is.EqualTo("Tls12"));
            Assert.That(diagnosticEvent.Message, Does.Not.Contain(certificateFingerprint));
            Assert.That(diagnosticEvent.Message,
                Does.Not.Contain(Convert.ToBase64String(certificate.RawData)));
            Assert.That(diagnosticEvent.Redacted, Is.True);
        });

        client.CloseClientTls();
        server.CloseServerTls();
    }

    [Test]
    public void Explicit_TLS_preserves_the_clear_234_exchange_then_upgrades_the_same_stream()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpTlsServerStream server = CreateServer(
            connection.ServerStream, certificate, false, false);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, null, false, false);
        byte[] clearReply = Encoding.ASCII.GetBytes("234 Begin TLS\r\n");
        connection.ServerStream.Write(clearReply, 0, clearReply.Length);
        connection.ServerStream.Flush();
        byte[] clearReceived = ReadExact(connection.ClientStream, clearReply.Length);
        FtpReply authorization = CreateReply(234, "Begin TLS");

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.UpgradeExplicitServer(authorization));
        FtpTlsStreamResult clientResult = client.UpgradeExplicitClient(authorization);
        FtpTlsStreamResult serverResult = serverTask.GetAwaiter().GetResult();
        byte[] command = Encoding.ASCII.GetBytes("FEAT\r\n");
        clientResult.Stream!.Write(command, 0, command.Length);
        clientResult.Stream.Flush();
        byte[] securedCommand = ReadExact(serverResult.Stream!, command.Length);

        Assert.Multiple(() =>
        {
            Assert.That(clearReceived, Is.EqualTo(clearReply));
            Assert.That(securedCommand, Is.EqualTo(command));
            Assert.That(clientResult.ImplicitTls, Is.False);
            Assert.That(clientResult.ExplicitUpgrade, Is.True);
            Assert.That(serverResult.ImplicitTls, Is.False);
            Assert.That(serverResult.ExplicitUpgrade, Is.True);
        });

        client.CloseClientTls();
        server.CloseServerTls();
    }

    [Test]
    public void Explicit_upgrade_rejects_any_reply_other_than_234_before_TLS_bytes_are_sent()
    {
        using TestConnection connection = CreateConnection();
        FtpTlsServerStreamRequest serverRequest = new FtpTlsServerStreamRequest();
        serverRequest.TransportStream = connection.ServerStream;
        serverRequest.LeaveTransportOpen = true;
        serverRequest.CertificateSelection = (sender, name) =>
            throw new InvalidOperationException("TLS must not start after a rejected AUTH reply.");
        serverRequest.Options!.Protocols = SslProtocols.Tls12;
        FtpTlsServerStream server = new FtpTlsServerStream(serverRequest);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, null, false, true);
        FtpReply rejection = CreateReply(534, "TLS unavailable");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                client.UpgradeExplicitClient(rejection));
            Assert.Throws<FtpProtocolException>(() =>
                server.UpgradeExplicitServer(rejection));
            Assert.That(client.Started, Is.False);
            Assert.That(server.Started, Is.False);
        });

        byte[] clearCommand = Encoding.ASCII.GetBytes("QUIT\r\n");
        connection.ClientStream.Write(clearCommand, 0, clearCommand.Length);
        connection.ClientStream.Flush();
        Assert.That(ReadExact(connection.ServerStream, clearCommand.Length),
            Is.EqualTo(clearCommand));
    }

    [Test]
    public void Server_can_require_and_authenticate_a_client_certificate()
    {
        using X509Certificate2 serverCertificate = CreateCertificate();
        using X509Certificate2 clientCertificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpTlsServerStream server = CreateServer(
            connection.ServerStream, serverCertificate, true, false);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, clientCertificate, false, false);

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.AuthenticateImplicitServer());
        FtpTlsStreamResult clientResult = client.AuthenticateImplicitClient();
        FtpTlsStreamResult serverResult = serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(clientResult.MutuallyAuthenticated, Is.True);
            Assert.That(serverResult.MutuallyAuthenticated, Is.True);
            Assert.That(serverResult.Stream!.RemoteCertificate, Is.Not.Null);
        });

        client.CloseClientTls();
        server.CloseServerTls();
    }

    [Test]
    public void Data_TLS_diagnostics_distinguish_the_channel_without_claiming_AUTH_upgrade()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpTlsServerStream server = CreateServer(
            connection.ServerStream, certificate, false, false);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, null, false, false);

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.AuthenticateDataServer());
        FtpTlsStreamResult clientResult = client.AuthenticateDataClient();
        FtpTlsStreamResult serverResult = serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(clientResult.DataChannel, Is.True);
            Assert.That(clientResult.ExplicitUpgrade, Is.False);
            Assert.That(clientResult.Diagnostics.DataChannel, Is.True);
            Assert.That(clientResult.Diagnostics.ExplicitUpgrade, Is.False);
            Assert.That(clientResult.Diagnostics.ServerRole, Is.False);
            Assert.That(serverResult.DataChannel, Is.True);
            Assert.That(serverResult.ExplicitUpgrade, Is.False);
            Assert.That(serverResult.Diagnostics.DataChannel, Is.True);
            Assert.That(serverResult.Diagnostics.ExplicitUpgrade, Is.False);
            Assert.That(serverResult.Diagnostics.ServerRole, Is.True);
        });

        client.CloseClientTls();
        server.CloseServerTls();
    }

    [Test]
    public void Implicit_server_provider_selects_the_certificate_from_client_SNI()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpCertificateResult selected = new FtpCertificateResult();
        selected.Handled = true;
        selected.Succeeded = true;
        selected.Certificate = certificate;
        RecordingCertificateProvider provider =
            new RecordingCertificateProvider(selected);
        FtpTlsServerStreamRequest serverRequest = new FtpTlsServerStreamRequest();
        serverRequest.TransportStream = connection.ServerStream;
        serverRequest.CertificateProvider = provider;
        serverRequest.HostName = "default.example.test";
        serverRequest.Options!.Protocols = SslProtocols.Tls12;
        serverRequest.Options.CheckCertificateRevocation = false;
        FtpTlsServerStream server = new FtpTlsServerStream(serverRequest);
        FtpTlsClientStreamRequest clientRequest = CreateClientRequest(
            connection.ClientStream, null, false, false);
        clientRequest.TargetHost = "tenant.example.test";
        FtpTlsClientStream client = new FtpTlsClientStream(clientRequest);

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.AuthenticateImplicitServer());
        FtpTlsStreamResult clientResult = client.AuthenticateImplicitClient();
        FtpTlsStreamResult serverResult = serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(clientResult.Authenticated, Is.True);
            Assert.That(serverResult.Authenticated, Is.True);
            Assert.That(provider.Request, Is.Not.Null);
            Assert.That(provider.Request!.HostName,
                Is.EqualTo("default.example.test"));
            Assert.That(provider.Request.ServerName,
                Is.EqualTo("tenant.example.test"));
            Assert.That(provider.Request.ImplicitTls, Is.True);
            Assert.That(server.CertificatePolicy!.SelectionSucceeded, Is.True);
            Assert.That(server.CertificatePolicy.SelectionCalls, Is.EqualTo(1));
        });

        client.CloseClientTls();
        server.CloseServerTls();
    }

    [Test]
    public void Handshake_timeout_and_caller_cancellation_close_the_failed_secure_stream()
    {
        using TestConnection timeoutConnection = CreateConnection();
        FtpTlsClientStreamRequest timeoutRequest = CreateClientRequest(
            timeoutConnection.ClientStream, null, false, false);
        timeoutRequest.HandshakeTimeoutMilliseconds = 250;
        FtpTlsClientStream timedOut = new FtpTlsClientStream(timeoutRequest);

        Assert.Throws<TimeoutException>(() =>
            timedOut.AuthenticateImplicitClient());

        using TestConnection canceledConnection = CreateConnection();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FtpTlsClientStreamRequest canceledRequest = CreateClientRequest(
            canceledConnection.ClientStream, null, false, false);
        canceledRequest.CancellationToken = cancellation.Token;
        FtpTlsClientStream canceled = new FtpTlsClientStream(canceledRequest);

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                canceled.AuthenticateImplicitClient());
            Assert.That(timedOut.Failed, Is.True);
            Assert.That(timedOut.Closed, Is.True);
            Assert.That(timedOut.SecureStream, Is.Null);
            Assert.That(canceled.Failed, Is.True);
            Assert.That(canceled.Closed, Is.True);
            Assert.That(canceled.SecureStream, Is.Null);
        });
    }

    [Test]
    public void Closing_TLS_is_idempotent_and_honors_leave_transport_open()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        TrackingStream clientTransport = new TrackingStream(connection.ClientStream);
        TrackingStream serverTransport = new TrackingStream(connection.ServerStream);
        FtpTlsServerStream server = CreateServer(
            serverTransport, certificate, false, true);
        FtpTlsClientStream client = CreateClient(
            clientTransport, null, false, true);

        Task<FtpTlsStreamResult> serverTask = Task.Run(() =>
            server.AuthenticateImplicitServer());
        client.AuthenticateImplicitClient();
        serverTask.GetAwaiter().GetResult();

        client.CloseClientTls();
        client.CloseClientTls();
        server.CloseServerTls();
        server.CloseServerTls();

        Assert.Multiple(() =>
        {
            Assert.That(clientTransport.Disposed, Is.False);
            Assert.That(serverTransport.Disposed, Is.False);
            Assert.That(client.Closed, Is.True);
            Assert.That(server.Closed, Is.True);
            Assert.Throws<FtpProtocolException>(() =>
                client.AuthenticateImplicitClient());
            Assert.Throws<FtpProtocolException>(() =>
                server.AuthenticateImplicitServer());
        });
    }

    [Test]
    public void CCC_shutdown_exchanges_TLS_close_notifications_then_restores_clear_transport()
    {
        using X509Certificate2 certificate = CreateCertificate();
        using TestConnection connection = CreateConnection();
        FtpTlsServerStream server = CreateServer(
            connection.ServerStream, certificate, false, true);
        FtpTlsClientStream client = CreateClient(
            connection.ClientStream, null, false, true);

        Task<FtpTlsStreamResult> serverHandshake = Task.Run(() =>
            server.AuthenticateImplicitServer());
        FtpTlsStreamResult clientResult = client.AuthenticateImplicitClient();
        FtpTlsStreamResult serverResult = serverHandshake.GetAwaiter().GetResult();
        byte[] protectedReply = Encoding.ASCII.GetBytes("200 CCC accepted\r\n");
        serverResult.Stream!.Write(protectedReply, 0, protectedReply.Length);
        serverResult.Stream.Flush();
        byte[] receivedReply = ReadExact(clientResult.Stream!, protectedReply.Length);

        Task serverShutdown = Task.Run(() =>
            server.ShutdownServerTlsForClearControl());
        client.ShutdownClientTlsForClearControl();
        serverShutdown.GetAwaiter().GetResult();

        byte[] clearCommand = Encoding.ASCII.GetBytes("NOOP\r\n");
        connection.ClientStream.Write(clearCommand, 0, clearCommand.Length);
        connection.ClientStream.Flush();
        byte[] receivedCommand = ReadExact(connection.ServerStream, clearCommand.Length);

        Assert.Multiple(() =>
        {
            Assert.That(receivedReply, Is.EqualTo(protectedReply));
            Assert.That(receivedCommand, Is.EqualTo(clearCommand));
            Assert.That(client.Closed, Is.True);
            Assert.That(client.Failed, Is.False);
            Assert.That(server.Closed, Is.True);
            Assert.That(server.Failed, Is.False);
        });
    }

    [Test]
    public void Invalid_client_and_server_transport_requests_are_rejected_before_handshake()
    {
        using MemoryStream stream = new MemoryStream();
        FtpTlsClientStreamRequest missingHost = CreateClientRequest(
            stream, null, false, true);
        missingHost.TargetHost = "";
        FtpTlsClientStreamRequest zeroTimeout = CreateClientRequest(
            stream, null, false, true);
        zeroTimeout.HandshakeTimeoutMilliseconds = 0;
        FtpTlsServerStreamRequest missingCertificate = new FtpTlsServerStreamRequest();
        missingCertificate.TransportStream = stream;
        missingCertificate.Options!.Protocols = SslProtocols.Tls12;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpTlsClientStream(null));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpTlsClientStream(missingHost));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpTlsClientStream(zeroTimeout));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpTlsServerStream(missingCertificate));
        });
    }

    private static FtpTlsClientStream CreateClient(
        Stream stream,
        X509Certificate2? clientCertificate,
        bool requireClientCertificate,
        bool leaveOpen)
    {
        FtpTlsClientStreamRequest request = CreateClientRequest(
            stream, clientCertificate, requireClientCertificate, leaveOpen);
        return new FtpTlsClientStream(request);
    }

    private static FtpTlsClientStreamRequest CreateClientRequest(
        Stream stream,
        X509Certificate2? clientCertificate,
        bool requireClientCertificate,
        bool leaveOpen)
    {
        FtpTlsClientStreamRequest request = new FtpTlsClientStreamRequest();
        request.TransportStream = stream;
        request.TargetHost = "localhost";
        request.LeaveTransportOpen = leaveOpen;
        request.HandshakeTimeoutMilliseconds = 5000;
        request.CertificateValidation = (sender, remote, chain, errors) => true;
        request.Options!.Protocols = SslProtocols.Tls12;
        request.Options.CheckCertificateRevocation = false;
        request.Options.RequireClientCertificate = requireClientCertificate;
        request.Options.ClientCertificate = clientCertificate;
        return request;
    }

    private static FtpTlsServerStream CreateServer(
        Stream stream,
        X509Certificate2 certificate,
        bool requireClientCertificate,
        bool leaveOpen)
    {
        FtpTlsServerStreamRequest request = new FtpTlsServerStreamRequest();
        request.TransportStream = stream;
        request.LeaveTransportOpen = leaveOpen;
        request.HandshakeTimeoutMilliseconds = 5000;
        request.CertificateValidation = (sender, remote, chain, errors) => true;
        request.Options!.Protocols = SslProtocols.Tls12;
        request.Options.CheckCertificateRevocation = false;
        request.Options.RequireClientCertificate = requireClientCertificate;
        request.Options.ServerCertificate = certificate;
        return new FtpTlsServerStream(request);
    }

    internal static X509Certificate2 CreateCertificate()
    {
        byte[] pfx = Convert.FromBase64String(TestCertificatePfx);
        try
        {
            return X509CertificateLoader.LoadPkcs12(
                pfx,
                "ftp-test",
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable,
                null);
        }
        catch (PlatformNotSupportedException)
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12(
                    pfx,
                    "ftp-test",
                    X509KeyStorageFlags.Exportable,
                    null);
            }
            catch (CryptographicException)
            {
                Assert.Ignore("The platform TLS backend cannot import a server private key in this runner.");
                throw;
            }
        }
    }

    private const string TestCertificatePfx =
        "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";

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }

    private static byte[] ReadExact(Stream stream, int count)
    {
        byte[] bytes = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(bytes, offset, count - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("The test stream ended before the expected bytes arrived.");
            }
            offset += read;
        }
        return bytes;
    }

    private static TestConnection CreateConnection()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Socket client = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(IPAddress.Loopback, port);
        Socket server = listener.AcceptSocket();
        listener.Stop();
        return new TestConnection(client, server);
    }

    private sealed class TestConnection : IDisposable
    {
        internal Socket Client;
        internal Socket Server;
        internal NetworkStream ClientStream;
        internal NetworkStream ServerStream;

        internal TestConnection(Socket client, Socket server)
        {
            Client = client;
            Server = server;
            ClientStream = new NetworkStream(client, false);
            ServerStream = new NetworkStream(server, false);
        }

        public void Dispose()
        {
            ClientStream.Dispose();
            ServerStream.Dispose();
            Client.Dispose();
            Server.Dispose();
        }
    }

    private sealed class TrackingStream : Stream
    {
        internal Stream Inner;
        internal bool Disposed;

        internal TrackingStream(Stream inner)
        {
            Inner = inner;
            Disposed = false;
        }

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => Inner.CanWrite;
        public override long Length => Inner.Length;
        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        public override void Flush()
        {
            Inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Inner.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
        }
    }

    private sealed class RecordingCertificateProvider : IFtpCertificateProvider
    {
        internal FtpCertificateResult Result;
        internal FtpCertificateRequest? Request;

        internal RecordingCertificateProvider(FtpCertificateResult result)
        {
            Result = result;
            Request = null;
        }

        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            Request = request;
            return Result;
        }
    }
}
