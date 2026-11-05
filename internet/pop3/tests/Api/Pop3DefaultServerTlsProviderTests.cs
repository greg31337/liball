using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace pop3.Tests;

[NonParallelizable]
public class Pop3DefaultServerTlsProviderTests
{
    [Test]
    public void AuthenticationOptionsMapCertificateProtocolAndClientPolicy()
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        {
            Pop3TlsServerOptions options = CreateOptions(certificate);
            options.EnabledProtocols = SslProtocols.Tls12;
            options.RequireClientCertificate = true;
            options.CheckCertificateRevocation = true;
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();

            SslServerAuthenticationOptions authentication =
                provider.CreateAuthenticationOptions(options);

            Assert.That(authentication.ServerCertificate, Is.SameAs(certificate));
            Assert.That(authentication.EnabledSslProtocols,
                Is.EqualTo(SslProtocols.Tls12));
            Assert.That(authentication.ClientCertificateRequired, Is.True);
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.Online));
            Assert.That(authentication.EncryptionPolicy,
                Is.EqualTo(EncryptionPolicy.RequireEncryption));
            Assert.That(authentication.RemoteCertificateValidationCallback, Is.Null);
        }
    }

    [Test]
    public void OptionalClientCertificatesAndDisabledRevocationMapDirectly()
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        {
            Pop3TlsServerOptions options = CreateOptions(certificate);
            options.RequireClientCertificate = false;
            options.CheckCertificateRevocation = false;

            SslServerAuthenticationOptions authentication =
                new Pop3DefaultServerTlsProvider().CreateAuthenticationOptions(options);

            Assert.That(authentication.ClientCertificateRequired, Is.False);
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.NoCheck));
        }
    }

    [Test]
    public void ProviderRejectsMissingCertificatePrivateKeyAndWeakProtocols()
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        using (X509Certificate2 publicCertificate =
            X509CertificateLoader.LoadCertificate(certificate.RawData))
        {
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();
            TrackingHandshakeStream stream = new TrackingHandshakeStream(false);
            Pop3TlsServerOptions options = CreateOptions(certificate);

            Assert.Throws<ArgumentNullException>(() =>
                provider.SecureServerStream(null!, options, CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                provider.SecureServerStream(stream, null!, CancellationToken.None));

            options.ServerCertificate = null;
            Assert.Throws<ArgumentException>(() =>
                provider.SecureServerStream(stream, options, CancellationToken.None));
            options.ServerCertificate = publicCertificate;
            Assert.Throws<ArgumentException>(() =>
                provider.SecureServerStream(stream, options, CancellationToken.None));
            options.ServerCertificate = certificate;
            options.EnabledProtocols = SslProtocols.None;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.SecureServerStream(stream, options, CancellationToken.None));
            options.EnabledProtocols = (SslProtocols)1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.SecureServerStream(stream, options, CancellationToken.None));
            options.EnabledProtocols = SslProtocols.Tls12 | (SslProtocols)1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.SecureServerStream(stream, options, CancellationToken.None));

            Assert.That(stream.ReadCallCount, Is.Zero);
            Assert.That(stream.WriteCallCount, Is.Zero);
            Assert.That(stream.IsDisposed, Is.False);
            stream.Dispose();
        }
    }

    [Test]
    public void ProviderRequiresReadableAndWritableTransport()
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        {
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();
            Pop3TlsServerOptions options = CreateOptions(certificate);
            CapabilityStream readOnly = new CapabilityStream(true, false);
            CapabilityStream writeOnly = new CapabilityStream(false, true);

            Assert.Throws<ArgumentException>(() =>
                provider.SecureServerStream(readOnly, options, CancellationToken.None));
            Assert.Throws<ArgumentException>(() =>
                provider.SecureServerStream(writeOnly, options, CancellationToken.None));

            Assert.That(readOnly.IsDisposed, Is.False);
            Assert.That(writeOnly.IsDisposed, Is.False);
            readOnly.Dispose();
            writeOnly.Dispose();
        }
    }

    [Test]
    public void PreCanceledHandshakeDoesNotTouchOrTakeOwnershipOfTransport()
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();
            Pop3TlsServerOptions options = CreateOptions(certificate);
            TrackingHandshakeStream stream = new TrackingHandshakeStream(false);
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                provider.SecureServerStream(stream, options, cancellation.Token));

            Assert.That(stream.ReadCallCount, Is.Zero);
            Assert.That(stream.WriteCallCount, Is.Zero);
            Assert.That(stream.IsDisposed, Is.False);
            stream.Dispose();
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void FailedHandshakeHonorsInnerStreamOwnership(
        bool leaveOpen,
        bool expectedDisposed)
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        {
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();
            Pop3TlsServerOptions options = CreateOptions(certificate);
            options.LeaveInnerStreamOpen = leaveOpen;
            TrackingHandshakeStream stream = new TrackingHandshakeStream(false);

            AuthenticationException exception = Assert.Throws<AuthenticationException>(() =>
                provider.SecureServerStream(
                    stream,
                    options,
                    CancellationToken.None))!;

            Assert.That(exception.Message,
                Is.EqualTo("The POP3 server TLS handshake failed."));
            Assert.That(exception.InnerException, Is.Not.Null);
            Assert.That(stream.ReadCallCount, Is.GreaterThan(0));
            Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
            stream.Dispose();
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void CanceledHandshakeHonorsInnerStreamOwnership(
        bool leaveOpen,
        bool expectedDisposed)
    {
        using (X509Certificate2 certificate = CreateCertificate("server.example"))
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            Pop3DefaultServerTlsProvider provider =
                new Pop3DefaultServerTlsProvider();
            Pop3TlsServerOptions options = CreateOptions(certificate);
            options.LeaveInnerStreamOpen = leaveOpen;
            TrackingHandshakeStream stream = new TrackingHandshakeStream(true);
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() =>
                provider.SecureServerStream(stream, options, cancellation.Token));

            Assert.That(stream.ReadCallCount, Is.GreaterThan(0));
            Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
            stream.Dispose();
        }
    }

    [Test]
    public async Task OptionalClientCertificatePolicyCompletesATls12Handshake()
    {
        using (X509Certificate2 certificate = CreateCertificate("localhost"))
        using (CancellationTokenSource cancellation = new CancellationTokenSource(5000))
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, port, cancellation.Token);
                using (TcpClient accepted = await acceptTask)
                {
                    Pop3DefaultServerTlsProvider provider =
                        new Pop3DefaultServerTlsProvider();
                    Pop3TlsServerOptions options = CreateOptions(certificate);
                    options.EnabledProtocols = SslProtocols.Tls12;
                    Task<Stream> serverTask = Task.Run(() =>
                        provider.SecureServerStream(
                            accepted.GetStream(),
                            options,
                            cancellation.Token));
                    using (SslStream clientStream = new SslStream(
                        client.GetStream(),
                        false,
                        (sender, peerCertificate, chain, errors) => true))
                    {
                        SslClientAuthenticationOptions clientOptions =
                            new SslClientAuthenticationOptions();
                        clientOptions.TargetHost = "localhost";
                        clientOptions.EnabledSslProtocols = SslProtocols.Tls12;
                        clientOptions.CertificateRevocationCheckMode =
                            X509RevocationMode.NoCheck;
                        await clientStream.AuthenticateAsClientAsync(
                            clientOptions,
                            cancellation.Token);
                        using (Stream securedServerStream = await serverTask)
                        {
                            SslStream serverStream = (SslStream)securedServerStream;
                            Assert.That(serverStream.IsAuthenticated, Is.True);
                            Assert.That(serverStream.IsEncrypted, Is.True);
                            Assert.That(serverStream.SslProtocol,
                                Is.EqualTo(SslProtocols.Tls12));
                            Assert.That(serverStream.RemoteCertificate, Is.Null);
                        }
                    }
                }
            }
            listener.Stop();
        }
    }

    static Pop3TlsServerOptions CreateOptions(X509Certificate2 certificate)
    {
        Pop3TlsServerOptions options = new Pop3TlsServerOptions();
        options.ServerCertificate = certificate;
        options.CheckCertificateRevocation = false;
        return options;
    }

    static X509Certificate2 CreateCertificate(string commonName)
    {
        using (RSA key = RSA.Create(2048))
        {
            CertificateRequest request = new CertificateRequest(
                "CN=" + commonName,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature,
                    false));
            return MacOsKeychainTestGuard.Create(() => request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1)));
        }
    }

    class CapabilityStream : Stream
    {
        bool Readable;
        bool Writable;
        public bool IsDisposed;

        public CapabilityStream(bool readable, bool writable)
        {
            Readable = readable;
            Writable = writable;
        }

        public override bool CanRead
        {
            get { return Readable && !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return Writable && !IsDisposed; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    class TrackingHandshakeStream : Stream
    {
        bool BlockReads;
        public bool IsDisposed;
        public int ReadCallCount;
        public int WriteCallCount;

        public TrackingHandshakeStream(bool blockReads)
        {
            BlockReads = blockReads;
        }

        public override bool CanRead
        {
            get { return !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            return 0;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            ReadCallCount++;
            if (BlockReads)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCallCount++;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
