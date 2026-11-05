using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using token;

namespace token.Tests;

public class TokenX509ServiceTests
{
    [Test]
    public void X509ServiceImportsCertificateKeysAndCalculatesThumbprints()
    {
        ITokenX509Service service = new TokenX509Service();
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(
            rsa,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        TokenKeyResult imported = service.ImportCertificate(certificate);
        TokenBytesResult sha1 = service.CalculateCertificateSha1Thumbprint(certificate);
        TokenBytesResult sha256 = service.CalculateCertificateSha256Thumbprint(certificate);
        TokenTextResult sha1Text = service.CalculateCertificateSha1ThumbprintBase64Url(certificate);
        TokenTextResult sha256Text = service.CalculateCertificateSha256ThumbprintBase64Url(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(imported.Key!.Certificate, Is.SameAs(certificate));
            Assert.That(imported.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(imported.Key.NativePublicMaterial, Is.True);
            Assert.That(sha1.Bytes, Is.EqualTo(SHA1.HashData(certificate.RawData)));
            Assert.That(sha256.Bytes, Is.EqualTo(SHA256.HashData(certificate.RawData)));
            Assert.That(sha1Text.Text, Has.Length.EqualTo(27));
            Assert.That(sha256Text.Text, Has.Length.EqualTo(43));
            Assert.That(sha1Text.Text, Does.Not.Contain("="));
            Assert.That(sha256Text.Text, Does.Not.Contain("="));
        });
    }

    [Test]
    public void X509ServiceValidatesCertificateLifetimeAtRequestedInstant()
    {
        ITokenX509Service service = new TokenX509Service();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-2);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddDays(-1);
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(rsa, start, end);

        TokenResult tooEarly = service.ValidateCertificate(certificate, start.AddMinutes(-1));
        TokenResult valid = service.ValidateCertificate(certificate, start.AddHours(1));
        TokenResult expired = service.ValidateCertificate(certificate, end.AddMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(tooEarly.Error!.Code, Is.EqualTo("certificate_not_yet_valid"));
            Assert.That(valid.Success, Is.True);
            Assert.That(expired.Error!.Code, Is.EqualTo("certificate_expired"));
        });
    }

    [Test]
    public void X509ServiceImportsEcdsaCertificatesAndReturnsExactThumbprintEncodings()
    {
        ITokenX509Service service = new TokenX509Service();
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest request = new CertificateRequest(
            "CN=token-public-ecdsa-certificate",
            ecdsa,
            HashAlgorithmName.SHA256);
        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        TokenKeyResult imported = service.ImportCertificate(certificate);
        TokenBytesResult sha1 = service.CalculateCertificateSha1Thumbprint(certificate);
        TokenBytesResult sha256 = service.CalculateCertificateSha256Thumbprint(certificate);
        TokenTextResult sha1Text = service.CalculateCertificateSha1ThumbprintBase64Url(certificate);
        TokenTextResult sha256Text = service.CalculateCertificateSha256ThumbprintBase64Url(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(imported.Key!.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(imported.Key.Curve, Is.EqualTo("P-256"));
            Assert.That(sha1Text.Text, Is.EqualTo(Base64Url.Encode(sha1.Bytes!).Text));
            Assert.That(sha256Text.Text, Is.EqualTo(Base64Url.Encode(sha256.Bytes!).Text));
        });
    }

    [Test]
    public void X509ServiceImportsAndValidatesExactOfflineCertificateChain()
    {
        ITokenX509Service service = new TokenX509Service();
        X509Certificate2[] certificates = CreateCertificateChain();
        using X509Certificate2 leaf = certificates[0];
        using X509Certificate2 root = certificates[1];
        X509Certificate2[] supplied = new X509Certificate2[] { leaf, root };

        TokenKeyResult imported = service.ImportCertificateChain(leaf, supplied);
        supplied[0] = root;
        TokenResult validated = service.ValidateCertificateChain(
            new X509Certificate2[] { leaf, root },
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(imported.Key!.CertificateChain, Is.Not.SameAs(supplied));
            Assert.That(imported.Key.CertificateChain[0], Is.SameAs(leaf));
            Assert.That(imported.Key.CertificateChain[1], Is.SameAs(root));
            Assert.That(validated.Success, Is.True);
        });
    }

    [Test]
    public void X509ServiceRejectsMismatchedAndInvalidChains()
    {
        ITokenX509Service service = new TokenX509Service();
        X509Certificate2[] first = CreateCertificateChain();
        X509Certificate2[] second = CreateCertificateChain();
        using X509Certificate2 leaf = first[0];
        using X509Certificate2 root = first[1];
        using X509Certificate2 otherLeaf = second[0];
        using X509Certificate2 otherRoot = second[1];

        TokenKeyResult mismatch = service.ImportCertificateChain(
            leaf,
            new X509Certificate2[] { root });
        TokenResult wrongAnchor = service.ValidateCertificateChain(
            new X509Certificate2[] { leaf, otherRoot },
            DateTimeOffset.UtcNow);
        TokenResult empty = service.ValidateCertificateChain(
            Array.Empty<X509Certificate2>(),
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Error!.Code, Is.EqualTo("certificate_chain_mismatch"));
            Assert.That(wrongAnchor.Error!.Code, Is.EqualTo("invalid_certificate_chain"));
            Assert.That(empty.Error!.Code, Is.EqualTo("missing_certificate_chain"));
        });
    }

    [Test]
    public void X509ServiceRejectsNullArguments()
    {
        ITokenX509Service service = new TokenX509Service();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ImportCertificate(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ImportCertificateChain(null!, Array.Empty<X509Certificate2>()), Throws.ArgumentNullException);
            Assert.That(() => service.ValidateCertificate(null!, DateTimeOffset.UtcNow), Throws.ArgumentNullException);
            Assert.That(() => service.ValidateCertificateChain(null!, DateTimeOffset.UtcNow), Throws.ArgumentNullException);
            Assert.That(() => service.CalculateCertificateSha1Thumbprint(null!), Throws.ArgumentNullException);
            Assert.That(() => service.CalculateCertificateSha256Thumbprint(null!), Throws.ArgumentNullException);
        });
    }

    static X509Certificate2 CreateRsaCertificate(
        RSA key,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        CertificateRequest request = new CertificateRequest(
            "CN=token-public-x509-service",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            key,
            RSASignaturePadding.Pkcs1);
        return request.Create(
            request.SubjectName,
            generator,
            start,
            end,
            new byte[] { 1 });
    }

    static X509Certificate2[] CreateCertificateChain()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddDays(30);
        using RSA rootKey = RSA.Create(2048);
        CertificateRequest rootRequest = new CertificateRequest(
            "CN=token-public-x509-root",
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

        X509SignatureGenerator rootGenerator = X509SignatureGenerator.CreateForRSA(
            rootKey,
            RSASignaturePadding.Pkcs1);
        X509Certificate2 root = rootRequest.Create(
            rootRequest.SubjectName,
            rootGenerator,
            start,
            end,
            new byte[] { 1 });

        using RSA leafKey = RSA.Create(2048);
        CertificateRequest leafRequest = new CertificateRequest(
            "CN=token-public-x509-leaf",
            leafKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        leafRequest.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));
        leafRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        leafRequest.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(leafRequest.PublicKey, false));
        X509Certificate2 leaf = leafRequest.Create(
            rootRequest.SubjectName,
            rootGenerator,
            start,
            end,
            new byte[] { 2 });

        return new X509Certificate2[] { leaf, root };
    }
}
