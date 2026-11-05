using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using token;

namespace token.Tests;

public class X509KeyHelpersTests
{
    [Test]
    public void ImportCreatesNormalizedRsaCertificateKey()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(
            rsa,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        TokenKeyResult result = X509KeyHelpers.Import(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Key.Certificate, Is.SameAs(certificate));
            Assert.That(result.Key.CertificateChain, Is.Empty);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.Rsa));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeRsa));
            Assert.That(result.Key.Rsa, Is.Null);
            Assert.That(result.Key.NativePublicMaterial, Is.True);
            Assert.That(result.Key.NativePrivateMaterial, Is.False);
            Assert.That(result.Key.ContainsPublicMaterial(), Is.True);
            Assert.That(result.Key.ContainsPrivateMaterial(), Is.False);
            Assert.That(
                result.Key.CertificateSha1Thumbprint,
                Is.EqualTo(certificate.GetCertHash(HashAlgorithmName.SHA1)));
            Assert.That(
                result.Key.CertificateSha256Thumbprint,
                Is.EqualTo(certificate.GetCertHash(HashAlgorithmName.SHA256)));
        });
    }

    [Test]
    public void ImportCreatesNormalizedEcCertificateKey()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        CertificateRequest request = new CertificateRequest(
            "CN=token-ec-certificate-test",
            ecdsa,
            HashAlgorithmName.SHA384);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForECDsa(ecdsa);
        using X509Certificate2 certificate = request.Create(
            request.SubjectName,
            generator,
            start,
            start.AddDays(2),
            new byte[] { 1 });

        TokenKeyResult result = X509KeyHelpers.Import(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Key.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(result.Key.CoseKeyType, Is.EqualTo(CoseKeyLabels.KeyTypeEc2));
            Assert.That(result.Key.Curve, Is.EqualTo("P-384"));
            Assert.That(result.Key.CoseCurve, Is.EqualTo(CoseKeyLabels.CurveP384));
            Assert.That(result.Key.Ecdsa, Is.Null);
            Assert.That(result.Key.NativePrivateMaterial, Is.False);
        });
    }

    [Test]
    public void ImportRejectsWeakRsaCertificate()
    {
        using RSA rsa = RSA.Create(1024);
        using X509Certificate2 certificate = CreateRsaCertificate(
            rsa,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        TokenKeyResult result = X509KeyHelpers.Import(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Key, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("weak_key"));
        });
    }

    [Test]
    public void ImportCopiesChainArrayAndRequiresMatchingLeaf()
    {
        X509Certificate2[] chain = CreateCertificateChain();
        using X509Certificate2 leaf = chain[0];
        using X509Certificate2 root = chain[1];
        X509Certificate2[] supplied = new X509Certificate2[] { leaf, root };

        TokenKeyResult imported = X509KeyHelpers.Import(leaf, supplied);
        supplied[0] = root;
        TokenKeyResult mismatch = X509KeyHelpers.Import(
            leaf,
            new X509Certificate2[] { root });

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(imported.Key.CertificateChain, Is.Not.SameAs(supplied));
            Assert.That(imported.Key.CertificateChain[0], Is.SameAs(leaf));
            Assert.That(imported.Key.CertificateChain[1], Is.SameAs(root));
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.Error.Code, Is.EqualTo("certificate_chain_mismatch"));
        });
    }

    [Test]
    public void ValidateEnforcesCertificateLifetime()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-2);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddDays(-1);
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(rsa, start, end);

        TokenResult tooEarly = X509KeyHelpers.Validate(
            certificate,
            start.AddDays(-1));
        TokenResult valid = X509KeyHelpers.Validate(
            certificate,
            start.AddHours(1));
        TokenResult expired = X509KeyHelpers.Validate(
            certificate,
            end.AddDays(1));

        Assert.Multiple(() =>
        {
            Assert.That(tooEarly.Success, Is.False);
            Assert.That(tooEarly.Error.Code, Is.EqualTo("certificate_not_yet_valid"));
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Error, Is.Null);
            Assert.That(expired.Success, Is.False);
            Assert.That(expired.Error.Code, Is.EqualTo("certificate_expired"));
        });
    }

    [Test]
    public void ValidateChainAcceptsExactSignedPathAndRejectsWrongAnchor()
    {
        X509Certificate2[] validChain = CreateCertificateChain();
        X509Certificate2[] otherChain = CreateCertificateChain();
        using X509Certificate2 leaf = validChain[0];
        using X509Certificate2 root = validChain[1];
        using X509Certificate2 otherLeaf = otherChain[0];
        using X509Certificate2 otherRoot = otherChain[1];
        DateTimeOffset validationTime = DateTimeOffset.UtcNow;

        TokenResult valid = X509KeyHelpers.ValidateChain(
            new X509Certificate2[] { leaf, root },
            validationTime);
        TokenResult invalid = X509KeyHelpers.ValidateChain(
            new X509Certificate2[] { leaf, otherRoot },
            validationTime);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Error, Is.Null);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error.Code, Is.EqualTo("invalid_certificate_chain"));
        });
    }

    [Test]
    public void ValidateCertificateKeyDetectsMetadataAndThumbprintConflicts()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(
            rsa,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        TokenKeyResult imported = X509KeyHelpers.Import(certificate);
        imported.Key.KeyType = TokenKeyTypes.EllipticCurve;

        TokenResult wrongType = X509KeyHelpers.Validate(
            imported.Key,
            DateTimeOffset.UtcNow,
            false);
        imported.Key.KeyType = TokenKeyTypes.Rsa;
        imported.Key.CertificateSha256Thumbprint[0] ^= 0xff;
        TokenResult wrongThumbprint = X509KeyHelpers.Validate(
            imported.Key,
            DateTimeOffset.UtcNow,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(wrongType.Success, Is.False);
            Assert.That(wrongType.Error.Code, Is.EqualTo("conflicting_key_type"));
            Assert.That(wrongThumbprint.Success, Is.False);
            Assert.That(
                wrongThumbprint.Error.Code,
                Is.EqualTo("certificate_thumbprint_mismatch"));
        });
    }

    [Test]
    public void ValidateCertificateKeyRejectsWrongAlgorithmFamily()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateRsaCertificate(
            rsa,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        TokenKeyResult imported = X509KeyHelpers.Import(certificate);
        imported.Key.Algorithm = JoseAlgorithms.EcdsaSha256;

        TokenResult result = X509KeyHelpers.Validate(
            imported.Key,
            DateTimeOffset.UtcNow,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_key_algorithm"));
        });
    }

    [Test]
    public void ValidateImportedChainUsesExplicitTrustAnchor()
    {
        X509Certificate2[] chain = CreateCertificateChain();
        using X509Certificate2 leaf = chain[0];
        using X509Certificate2 root = chain[1];
        TokenKeyResult imported = X509KeyHelpers.Import(
            leaf,
            new X509Certificate2[] { leaf, root });

        TokenResult result = X509KeyHelpers.Validate(
            imported.Key,
            DateTimeOffset.UtcNow,
            true);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void CertificateImportAndValidationRejectNullArguments()
    {
        using X509Certificate2 certificate = CreateCertificate();

        Assert.Multiple(() =>
        {
            Assert.That(() => X509KeyHelpers.Import(null), Throws.ArgumentNullException);
            Assert.That(
                () => X509KeyHelpers.Import(certificate, null),
                Throws.ArgumentNullException);
            Assert.That(() => X509KeyHelpers.Validate((TokenKey)null), Throws.ArgumentNullException);
            Assert.That(
                () => X509KeyHelpers.Validate((X509Certificate2)null, DateTimeOffset.UtcNow),
                Throws.ArgumentNullException);
            Assert.That(
                () => X509KeyHelpers.ValidateChain(null, DateTimeOffset.UtcNow),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void CalculateSha1ThumbprintHashesCertificateDerBytes()
    {
        using X509Certificate2 certificate = CreateCertificate();
        byte[] expected = SHA1.HashData(certificate.RawData);

        TokenBytesResult result = X509KeyHelpers.CalculateSha1Thumbprint(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Has.Length.EqualTo(20));
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void CalculateSha256ThumbprintHashesCertificateDerBytes()
    {
        using X509Certificate2 certificate = CreateCertificate();
        byte[] expected = SHA256.HashData(certificate.RawData);

        TokenBytesResult result = X509KeyHelpers.CalculateSha256Thumbprint(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Has.Length.EqualTo(32));
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void Base64UrlThumbprintsMatchJoseEncodingOfDigest()
    {
        using X509Certificate2 certificate = CreateCertificate();
        string expectedSha1 = Convert.ToBase64String(SHA1.HashData(certificate.RawData))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string expectedSha256 = Convert.ToBase64String(SHA256.HashData(certificate.RawData))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        TokenTextResult sha1 = X509KeyHelpers.CalculateSha1ThumbprintBase64Url(certificate);
        TokenTextResult sha256 = X509KeyHelpers.CalculateSha256ThumbprintBase64Url(certificate);

        Assert.Multiple(() =>
        {
            Assert.That(sha1.Success, Is.True);
            Assert.That(sha1.Error, Is.Null);
            Assert.That(sha1.Text, Is.EqualTo(expectedSha1));
            Assert.That(sha1.Text, Has.Length.EqualTo(27));
            Assert.That(sha1.Text, Does.Not.Contain("="));
            Assert.That(sha256.Success, Is.True);
            Assert.That(sha256.Error, Is.Null);
            Assert.That(sha256.Text, Is.EqualTo(expectedSha256));
            Assert.That(sha256.Text, Has.Length.EqualTo(43));
            Assert.That(sha256.Text, Does.Not.Contain("="));
        });
    }

    [Test]
    public void ThumbprintsAreStableAcrossCertificateInstances()
    {
        using X509Certificate2 original = CreateCertificate();
        using X509Certificate2 imported = X509CertificateLoader.LoadCertificate(original.RawData);

        TokenBytesResult originalSha256 = X509KeyHelpers.CalculateSha256Thumbprint(original);
        TokenBytesResult importedSha256 = X509KeyHelpers.CalculateSha256Thumbprint(imported);

        Assert.That(importedSha256.Bytes, Is.EqualTo(originalSha256.Bytes));
    }

    [Test]
    public void ThumbprintHelpersRejectNullCertificate()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => X509KeyHelpers.CalculateSha1Thumbprint(null), Throws.ArgumentNullException);
            Assert.That(() => X509KeyHelpers.CalculateSha256Thumbprint(null), Throws.ArgumentNullException);
            Assert.That(() => X509KeyHelpers.CalculateSha1ThumbprintBase64Url(null), Throws.ArgumentNullException);
            Assert.That(() => X509KeyHelpers.CalculateSha256ThumbprintBase64Url(null), Throws.ArgumentNullException);
        });
    }

    static X509Certificate2 CreateCertificate()
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=token-thumbprint-test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddMinutes(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(key, RSASignaturePadding.Pkcs1);
        return request.Create(request.SubjectName, generator, start, start.AddDays(1), new byte[] { 1 });
    }

    static X509Certificate2 CreateRsaCertificate(
        RSA key,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        CertificateRequest request = new CertificateRequest(
            "CN=token-rsa-certificate-test",
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
            "CN=token-test-root",
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
            "CN=token-test-leaf",
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
