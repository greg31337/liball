using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace pop3.Tests;

public class Pop3ClientTlsCertificateTests
{
    [Test]
    public async Task ImplicitTlsCompletesWithATrustedTestCertificate()
    {
        using (TestCertificateAuthority certificates = new TestCertificateAuthority())
        using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            TcpListener listener = StartListener();
            TrustedRootTlsProvider provider = new TrustedRootTlsProvider(certificates.Root, false);
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Task<string[]> serverTask = ServeImplicitTls(
                listener,
                certificates.Server,
                timeout.Token);

            try
            {
                engine.ConnectHost(
                    CreateOptions(
                        listener,
                        provider,
                        Pop3SecurityMode.ImplicitTls,
                        "localhost",
                        false),
                    timeout.Token);
                string[] commands = await serverTask;

                Pop3ClientSnapshot snapshot = engine.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
                Assert.That(snapshot.IsConnected, Is.True);
                Assert.That(snapshot.IsSecure, Is.True);
                Assert.That(snapshot.Greeting, Is.EqualTo("implicit TLS ready"));
                Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("CAPA unavailable"));
                Assert.That(commands, Is.EqualTo(new[] { Pop3Commands.Capa }));
                Assert.That(provider.ChainTrustedAtValidation, Is.True);
                Assert.That(
                    provider.ObservedPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch,
                    Is.EqualTo(SslPolicyErrors.None));
            }
            finally
            {
                engine.CloseConnection();
                listener.Stop();
            }
        }
    }

    [Test]
    public async Task RequiredStlsCompletesWithATrustedTestCertificateAndRefreshesCapabilities()
    {
        using (TestCertificateAuthority certificates = new TestCertificateAuthority())
        using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            TcpListener listener = StartListener();
            TrustedRootTlsProvider provider = new TrustedRootTlsProvider(certificates.Root, false);
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Task<string[]> serverTask = ServeStls(
                listener,
                certificates.Server,
                timeout.Token);

            try
            {
                Pop3ClientOptions options = CreateOptions(
                    listener,
                    provider,
                    Pop3SecurityMode.StlsRequired,
                    "localhost",
                    false);
                options.EnablePipelining = true;
                engine.ConnectHost(options, timeout.Token);
                string[] commands = await serverTask;

                Pop3ClientSnapshot snapshot = engine.GetSnapshot();
                Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
                Assert.That(snapshot.IsSecure, Is.True);
                Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
                Assert.That(snapshot.Capabilities.SupportsPipelining, Is.False);
                Assert.That(snapshot.Capabilities.SupportsUser, Is.True);
                Assert.That(snapshot.Capabilities.Implementation, Is.EqualTo("encrypted-test-server"));
                Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("protected capabilities"));
                Assert.That(commands, Is.EqualTo(new[]
                {
                    Pop3Commands.Capa,
                    Pop3Commands.Stls,
                    Pop3Commands.Capa
                }));
                Assert.That(provider.ChainTrustedAtValidation, Is.True);
            }
            finally
            {
                engine.CloseConnection();
                listener.Stop();
            }
        }
    }

    [Test]
    public async Task TrustedCertificateWithWrongServiceIdentityFailsImplicitTls()
    {
        using (TestCertificateAuthority certificates = new TestCertificateAuthority())
        using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            TcpListener listener = StartListener();
            TrustedRootTlsProvider provider = new TrustedRootTlsProvider(certificates.Root, false);
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Task serverTask = ServeRejectedHandshake(
                listener,
                certificates.Server,
                timeout.Token);

            try
            {
                Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
                    engine.ConnectHost(
                        CreateOptions(
                            listener,
                            provider,
                            Pop3SecurityMode.ImplicitTls,
                            "wrong.example.test",
                            false),
                        timeout.Token))!;
                await serverTask;

                Assert.That(exception.Operation, Is.EqualTo("SecureClientStream"));
                Assert.That(exception.InnerException, Is.TypeOf<AuthenticationException>());
                Assert.That(provider.ChainTrustedAtValidation, Is.True);
                Assert.That(
                    provider.ObservedPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch,
                    Is.EqualTo(SslPolicyErrors.RemoteCertificateNameMismatch));
                Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            }
            finally
            {
                engine.CloseConnection();
                listener.Stop();
            }
        }
    }

    [Test]
    public async Task RequiredRevocationPolicyFailureClosesImplicitTlsConnection()
    {
        using (TestCertificateAuthority certificates = new TestCertificateAuthority())
        using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            TcpListener listener = StartListener();
            TrustedRootTlsProvider provider = new TrustedRootTlsProvider(certificates.Root, true);
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Task serverTask = ServeRejectedHandshake(
                listener,
                certificates.Server,
                timeout.Token);

            try
            {
                Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
                    engine.ConnectHost(
                        CreateOptions(
                            listener,
                            provider,
                            Pop3SecurityMode.ImplicitTls,
                            "localhost",
                            true),
                        timeout.Token))!;
                await serverTask;

                Assert.That(exception.Operation, Is.EqualTo("SecureClientStream"));
                Assert.That(exception.InnerException, Is.TypeOf<AuthenticationException>());
                Assert.That(provider.ChainTrustedAtValidation, Is.True);
                Assert.That(provider.RevocationCheckRequested, Is.True);
                Assert.That(provider.RejectedForRevocationPolicy, Is.True);
                Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            }
            finally
            {
                engine.CloseConnection();
                listener.Stop();
            }
        }
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static Pop3ClientOptions CreateOptions(
        TcpListener listener,
        IPop3ClientTlsProvider provider,
        Pop3SecurityMode securityMode,
        string targetHost,
        bool checkRevocation)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "127.0.0.1";
        options.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.SecurityMode = securityMode;
        options.ConnectTimeoutMilliseconds = 5000;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        options.BufferSize = 32;
        options.Tls.TargetHost = targetHost;
        options.Tls.EnabledProtocols = SslProtocols.Tls12;
        options.Tls.CheckCertificateRevocation = checkRevocation;
        options.TlsProvider = provider;
        return options;
    }

    private static async Task<string[]> ServeImplicitTls(
        TcpListener listener,
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        using (TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken))
        using (SslStream stream = new SslStream(client.GetStream(), false))
        {
            await AuthenticateServer(stream, certificate, cancellationToken);
            await WriteText(stream, "+OK implicit TLS ready\r\n", cancellationToken);
            string capa = await ReadLine(stream, cancellationToken);
            await WriteText(stream, "-ERR CAPA unavailable\r\n", cancellationToken);
            return new[] { capa };
        }
    }

    private static async Task<string[]> ServeStls(
        TcpListener listener,
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        using (TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken))
        {
            NetworkStream plaintext = client.GetStream();
            await WriteText(plaintext, "+OK STLS ready\r\n", cancellationToken);
            string initialCapa = await ReadLine(plaintext, cancellationToken);
            await WriteText(
                plaintext,
                "+OK initial capabilities\r\nSTLS\r\nPIPELINING\r\nX-OLD value\r\n.\r\n",
                cancellationToken);
            string stls = await ReadLine(plaintext, cancellationToken);
            await WriteText(plaintext, "+OK begin TLS\r\n", cancellationToken);

            using (SslStream secure = new SslStream(plaintext, false))
            {
                await AuthenticateServer(secure, certificate, cancellationToken);
                string protectedCapa = await ReadLine(secure, cancellationToken);
                await WriteText(
                    secure,
                    "+OK protected capabilities\r\nUSER\r\nIMPLEMENTATION encrypted-test-server\r\n.\r\n",
                    cancellationToken);
                return new[] { initialCapa, stls, protectedCapa };
            }
        }
    }

    private static async Task ServeRejectedHandshake(
        TcpListener listener,
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        using (TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken))
        using (SslStream stream = new SslStream(client.GetStream(), false))
        {
            try
            {
                await AuthenticateServer(stream, certificate, cancellationToken);
            }
            catch (AuthenticationException)
            {
            }
            catch (IOException)
            {
            }
        }
    }

    private static Task AuthenticateServer(
        SslStream stream,
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();
        options.ServerCertificate = certificate;
        options.EnabledSslProtocols = SslProtocols.Tls12;
        options.ClientCertificateRequired = false;
        options.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
        return stream.AuthenticateAsServerAsync(options, cancellationToken);
    }

    private static async Task<string> ReadLine(Stream stream, CancellationToken cancellationToken)
    {
        List<byte> bytes = new List<byte>();
        byte[] current = new byte[1];
        while (true)
        {
            int count = await stream.ReadAsync(current, 0, 1, cancellationToken);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }
            if (current[0] == (byte)'\n')
            {
                if (bytes.Count == 0 || bytes[bytes.Count - 1] != (byte)'\r')
                {
                    throw new InvalidDataException("A test-server command did not end in CRLF.");
                }
                bytes.RemoveAt(bytes.Count - 1);
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
            bytes.Add(current[0]);
        }
    }

    private static async Task WriteText(
        Stream stream,
        string text,
        CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private sealed class TrustedRootTlsProvider : IPop3ClientTlsProvider
    {
        X509Certificate2 Root;
        bool RejectRevocation;
        public bool ChainTrustedAtValidation;
        public bool RevocationCheckRequested;
        public bool RejectedForRevocationPolicy;
        public SslPolicyErrors ObservedPolicyErrors;

        public TrustedRootTlsProvider(X509Certificate2 root, bool rejectRevocation)
        {
            Root = root;
            RejectRevocation = rejectRevocation;
        }

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            RevocationCheckRequested = options.CheckCertificateRevocation;
            SslStream secure = new SslStream(
                stream,
                options.LeaveInnerStreamOpen,
                ValidateCertificate);
            SslClientAuthenticationOptions authentication =
                new SslClientAuthenticationOptions();
            authentication.TargetHost = options.TargetHost;
            authentication.EnabledSslProtocols = options.EnabledProtocols;
            authentication.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
            authentication.EncryptionPolicy = EncryptionPolicy.RequireEncryption;
            authentication.ClientCertificates = options.ClientCertificates;
            try
            {
                secure.AuthenticateAsClientAsync(authentication, cancellationToken)
                    .GetAwaiter().GetResult();
                return secure;
            }
            catch
            {
                secure.Dispose();
                throw;
            }
        }

        bool ValidateCertificate(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            ObservedPolicyErrors = sslPolicyErrors;
            if (certificate == null)
            {
                return false;
            }

            using (X509Certificate2 peer =
                X509CertificateLoader.LoadCertificate(certificate.GetRawCertData()))
            using (X509Chain trustedChain = new X509Chain())
            {
                trustedChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                trustedChain.ChainPolicy.CustomTrustStore.Add(Root);
                trustedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                trustedChain.ChainPolicy.DisableCertificateDownloads = true;
                ChainTrustedAtValidation = trustedChain.Build(peer);
            }

            if (!ChainTrustedAtValidation)
            {
                return false;
            }
            if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) !=
                SslPolicyErrors.None)
            {
                return false;
            }
            if (RejectRevocation && RevocationCheckRequested)
            {
                RejectedForRevocationPolicy = true;
                return false;
            }
            return true;
        }
    }

    private sealed class TestCertificateAuthority : IDisposable
    {
        public X509Certificate2 Root;
        public X509Certificate2 Server;

        public TestCertificateAuthority()
        {
            using (RSA rootKey = RSA.Create(2048))
            {
                CertificateRequest rootRequest = new CertificateRequest(
                    "CN=pop3 test root",
                    rootKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                rootRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));
                rootRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        true));
                rootRequest.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));
                Root = MacOsKeychainTestGuard.Create(() =>
                    rootRequest.CreateSelfSigned(
                        DateTimeOffset.UtcNow.AddDays(-1),
                        DateTimeOffset.UtcNow.AddDays(2)));
            }

            using (RSA serverKey = RSA.Create(2048))
            {
                CertificateRequest serverRequest = new CertificateRequest(
                    "CN=localhost",
                    serverKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                serverRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, true));
                serverRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                            X509KeyUsageFlags.KeyEncipherment,
                        true));
                OidCollection usages = new OidCollection();
                usages.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                serverRequest.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(usages, true));
                SubjectAlternativeNameBuilder names = new SubjectAlternativeNameBuilder();
                names.AddDnsName("localhost");
                serverRequest.CertificateExtensions.Add(names.Build());

                byte[] serialNumber = RandomNumberGenerator.GetBytes(16);
                using (X509Certificate2 issued = serverRequest.Create(
                    Root,
                    DateTimeOffset.UtcNow.AddHours(-1),
                    DateTimeOffset.UtcNow.AddDays(1),
                    serialNumber))
                {
                    Server = MacOsKeychainTestGuard.Create(() =>
                        issued.CopyWithPrivateKey(serverKey));
                }
            }
        }

        public void Dispose()
        {
            Server.Dispose();
            Root.Dispose();
        }
    }
}
