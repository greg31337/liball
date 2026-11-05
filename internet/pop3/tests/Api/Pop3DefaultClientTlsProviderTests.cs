using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace pop3.Tests;

public class Pop3DefaultClientTlsProviderTests
{
    [Test]
    public void AuthenticationOptionsMapModernIdentityAndCertificatePolicy()
    {
        using (X509Certificate2 certificate = CreateCertificate("client.example"))
        {
            Pop3TlsClientOptions options = CreateOptions();
            options.TargetHost = "pop.example.test";
            options.EnabledProtocols = SslProtocols.Tls12;
            options.CheckCertificateRevocation = true;
            options.ClientCertificates.Add(certificate);
            Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();

            SslClientAuthenticationOptions authentication = provider.CreateAuthenticationOptions(options);
            options.ClientCertificates.Clear();

            Assert.That(authentication.TargetHost, Is.EqualTo("pop.example.test"));
            Assert.That(authentication.EnabledSslProtocols, Is.EqualTo(SslProtocols.Tls12));
            Assert.That(authentication.CertificateRevocationCheckMode, Is.EqualTo(X509RevocationMode.Online));
            Assert.That(authentication.EncryptionPolicy, Is.EqualTo(EncryptionPolicy.RequireEncryption));
            Assert.That(authentication.ClientCertificates, Is.Not.Null);
            Assert.That(authentication.ClientCertificates!, Has.Count.EqualTo(1));
            Assert.That(authentication.ClientCertificates[0], Is.SameAs(certificate));
        }
    }

    [Test]
    public void RevocationChecksCanBeDisabledWithoutChangingTrustValidation()
    {
        Pop3TlsClientOptions options = CreateOptions();
        options.CheckCertificateRevocation = false;

        SslClientAuthenticationOptions authentication =
            new Pop3DefaultClientTlsProvider().CreateAuthenticationOptions(options);

        Assert.That(authentication.CertificateRevocationCheckMode, Is.EqualTo(X509RevocationMode.NoCheck));
        Assert.That(authentication.RemoteCertificateValidationCallback, Is.Null);
    }

    [Test]
    public void ProviderRejectsMissingTransportIdentityAndWeakProtocolPolicy()
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        TrackingHandshakeStream stream = new TrackingHandshakeStream(false);
        Pop3TlsClientOptions options = CreateOptions();

        Assert.Throws<ArgumentNullException>(() => provider.SecureClientStream(null!, options, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => provider.SecureClientStream(stream, null!, CancellationToken.None));

        options.TargetHost = null!;
        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.TargetHost = "";
        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.TargetHost = "   ";
        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.TargetHost = "pop.example.test";
        options.EnabledProtocols = SslProtocols.None;
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.EnabledProtocols = (SslProtocols)1;
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.EnabledProtocols = SslProtocols.Tls12 | (SslProtocols)1;
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));
        options.EnabledProtocols = SslProtocols.Tls12;
        options.ClientCertificates = null!;
        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(stream, options, CancellationToken.None));

        Assert.That(stream.WriteCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void ProviderRequiresReadableAndWritableTransport()
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        Pop3TlsClientOptions options = CreateOptions();
        CapabilityStream readOnly = new CapabilityStream(true, false);
        CapabilityStream writeOnly = new CapabilityStream(false, true);

        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(readOnly, options, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => provider.SecureClientStream(writeOnly, options, CancellationToken.None));

        Assert.That(readOnly.IsDisposed, Is.False);
        Assert.That(writeOnly.IsDisposed, Is.False);
        readOnly.Dispose();
        writeOnly.Dispose();
    }

    [Test]
    public void PreCanceledHandshakeDoesNotTouchOrTakeOwnershipOfTransport()
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        Pop3TlsClientOptions options = CreateOptions();
        TrackingHandshakeStream stream = new TrackingHandshakeStream(false);
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                provider.SecureClientStream(stream, options, cancellation.Token));

            Assert.That(stream.WriteCallCount, Is.Zero);
            Assert.That(stream.IsDisposed, Is.False);
        }
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void FailedHandshakeHonorsInnerStreamOwnership(bool leaveOpen, bool expectedDisposed)
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        Pop3TlsClientOptions options = CreateOptions();
        options.LeaveInnerStreamOpen = leaveOpen;
        TrackingHandshakeStream stream = new TrackingHandshakeStream(false);

        AuthenticationException exception = Assert.Throws<AuthenticationException>(() =>
            provider.SecureClientStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Message, Is.EqualTo("The POP3 client TLS handshake failed."));
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(stream.WriteCallCount, Is.GreaterThan(0));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void CanceledHandshakeHonorsInnerStreamOwnership(bool leaveOpen, bool expectedDisposed)
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        Pop3TlsClientOptions options = CreateOptions();
        options.LeaveInnerStreamOpen = leaveOpen;
        TrackingHandshakeStream stream = new TrackingHandshakeStream(true);
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() =>
                provider.SecureClientStream(stream, options, cancellation.Token));

            Assert.That(stream.WriteCallCount, Is.GreaterThan(0));
            Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        }
        stream.Dispose();
    }

    [Test]
    public void ClientHelloCarriesTheConfiguredServiceIdentity()
    {
        Pop3DefaultClientTlsProvider provider = new Pop3DefaultClientTlsProvider();
        Pop3TlsClientOptions options = CreateOptions();
        options.TargetHost = "unique-pop.example.test";
        options.LeaveInnerStreamOpen = true;
        TrackingHandshakeStream stream = new TrackingHandshakeStream(false);

        Assert.Throws<AuthenticationException>(() =>
            provider.SecureClientStream(stream, options, CancellationToken.None));

        Assert.That(ContainsBytes(
            stream.WrittenBytes(),
            Encoding.ASCII.GetBytes("unique-pop.example.test")), Is.True);
        stream.Dispose();
    }

    private static Pop3TlsClientOptions CreateOptions()
    {
        Pop3TlsClientOptions options = new Pop3TlsClientOptions();
        options.TargetHost = "pop.example.test";
        options.CheckCertificateRevocation = false;
        return options;
    }

    private static X509Certificate2 CreateCertificate(string commonName)
    {
        using (RSA key = RSA.Create(2048))
        {
            CertificateRequest request = new CertificateRequest(
                "CN=" + commonName,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
                key,
                RSASignaturePadding.Pkcs1);
            return request.Create(
                request.SubjectName,
                generator,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1),
                new byte[] { 1, 2, 3, 4 });
        }
    }

    private static bool ContainsBytes(byte[] source, byte[] value)
    {
        int start = 0;
        while (start <= source.Length - value.Length)
        {
            int index = 0;
            while (index < value.Length && source[start + index] == value[index])
            {
                index++;
            }
            if (index == value.Length)
            {
                return true;
            }
            start++;
        }
        return false;
    }

    private class CapabilityStream : Stream
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

    private class TrackingHandshakeStream : Stream
    {
        bool BlockReads;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;
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
            return 0;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
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
            Written.Write(buffer, offset, count);
        }

        public byte[] WrittenBytes()
        {
            return Written.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
