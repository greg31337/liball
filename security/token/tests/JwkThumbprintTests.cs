using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using token;

namespace token.Tests;

public class JwkThumbprintTests
{
    [Test]
    public void Rfc7638RsaExampleMatchesPublishedSha256Thumbprint()
    {
        string modulus = "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAt"
            + "VT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn6"
            + "4tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FD"
            + "W2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n9"
            + "1CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINH"
            + "aQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw";
        string json = "{\"kty\":\"RSA\",\"n\":\"" + modulus
            + "\",\"e\":\"AQAB\",\"alg\":\"RS256\",\"kid\":\"2011-04-29\"}";
        JwkResult key = JsonWebKey.Read(json);

        TokenBytesResult result = JsonWebKey.Thumbprint(key.Key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Has.Length.EqualTo(32));
            Assert.That(
                B64(result.Bytes),
                Is.EqualTo("NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs"));
        });

        key.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void RsaPublicPrivateAndOptionalMetadataHaveSameThumbprint()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey privateKey = RsaKeyImporterOperations.Import(rsa, true).Key;
        TokenKey publicKey = RsaKeyImporterOperations.Import(rsa).Key;
        privateKey.KeyId = "private-id";
        privateKey.Algorithm = "not-used-for-thumbprints";
        privateKey.Use = "also-ignored";
        privateKey.KeyOperations = new string[] { "unknown" };
        privateKey.CertificateSha1Thumbprint = new byte[3];

        TokenBytesResult privateResult = JsonWebKey.Thumbprint(privateKey);
        TokenBytesResult publicResult = JsonWebKey.Thumbprint(publicKey);

        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Success, Is.True);
            Assert.That(publicResult.Success, Is.True);
            Assert.That(privateResult.Bytes, Is.EqualTo(publicResult.Bytes));
        });
    }

    [Test]
    public void EcThumbprintHashesOnlyCanonicalPublicMembers()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters parameters = ecdsa.ExportParameters(true);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, true).Key;
        key.KeyId = "ignored";
        string canonical = "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\""
            + B64(parameters.Q.X) + "\",\"y\":\"" + B64(parameters.Q.Y) + "\"}";

        TokenBytesResult result = JsonWebKey.Thumbprint(key);

        Assert.That(result.Bytes, Is.EqualTo(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))));
    }

    [Test]
    public void OkpThumbprintIgnoresPrivateMaterial()
    {
        byte[] publicBytes = Enumerable.Repeat((byte)11, 32).ToArray();
        TokenKey privateKey = new TokenKey();
        privateKey.KeyType = TokenKeyTypes.OctetKeyPair;
        privateKey.Curve = "Ed25519";
        privateKey.OkpPublicKey = publicBytes;
        privateKey.OkpPrivateKey = Enumerable.Repeat((byte)12, 32).ToArray();
        TokenKey publicKey = new TokenKey();
        publicKey.KeyType = TokenKeyTypes.OctetKeyPair;
        publicKey.Curve = "Ed25519";
        publicKey.OkpPublicKey = publicBytes;
        string canonical = "{\"crv\":\"Ed25519\",\"kty\":\"OKP\",\"x\":\""
            + B64(publicBytes) + "\"}";

        TokenBytesResult privateResult = JsonWebKey.Thumbprint(privateKey);
        TokenBytesResult publicResult = JsonWebKey.Thumbprint(publicKey);

        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Bytes, Is.EqualTo(publicResult.Bytes));
            Assert.That(
                privateResult.Bytes,
                Is.EqualTo(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))));
        });
    }

    [Test]
    public void SymmetricThumbprintHashesCanonicalSecretMembers()
    {
        byte[] secret = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        TokenKey key = SymmetricKeyImporterOperations.Import(secret).Key;
        key.Algorithm = "ignored";
        key.Use = "ignored";
        string canonical = "{\"k\":\"" + B64(secret) + "\",\"kty\":\"oct\"}";

        TokenBytesResult result = JsonWebKey.Thumbprint(key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(
                result.Bytes,
                Is.EqualTo(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))));
        });

        SymmetricKeyImporterOperations.Clear(key);
    }

    [Test]
    public void DifferentSymmetricSecretsHaveDifferentThumbprints()
    {
        TokenKey first = SymmetricKeyImporterOperations.Import(new byte[32]).Key;
        TokenKey second = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)1, 32).ToArray()).Key;

        TokenBytesResult firstResult = JsonWebKey.Thumbprint(first);
        TokenBytesResult secondResult = JsonWebKey.Thumbprint(second);

        Assert.That(firstResult.Bytes, Is.Not.EqualTo(secondResult.Bytes));
        SymmetricKeyImporterOperations.Clear(first);
        SymmetricKeyImporterOperations.Clear(second);
    }

    [Test]
    public void CertificateBackedAndNativeRsaKeysHaveSameThumbprint()
    {
        using RSA rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(rsa);
        TokenKey certificateKey = X509KeyHelpers.Import(certificate).Key;
        TokenKey nativeKey = RsaKeyImporterOperations.Import(rsa).Key;

        TokenBytesResult certificateResult = JsonWebKey.Thumbprint(certificateKey);
        TokenBytesResult nativeResult = JsonWebKey.Thumbprint(nativeKey);

        Assert.That(certificateResult.Bytes, Is.EqualTo(nativeResult.Bytes));
    }

    [Test]
    public void ThumbprintReturnsWriterFailuresAndRejectsNull()
    {
        TokenKey missing = new TokenKey();
        TokenKey weak = new TokenKey();
        weak.KeyType = TokenKeyTypes.Symmetric;
        weak.SymmetricKey = new byte[8];

        TokenBytesResult missingResult = JsonWebKey.Thumbprint(missing);
        TokenBytesResult weakResult = JsonWebKey.Thumbprint(weak);

        Assert.Multiple(() =>
        {
            AssertFailure(missingResult, "missing_key_material");
            AssertFailure(weakResult, "weak_key");
            Assert.That(() => JsonWebKey.Thumbprint(null), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void Rfc9278ExampleMatchesPublishedThumbprintUri()
    {
        string modulus = "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAt"
            + "VT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn6"
            + "4tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FD"
            + "W2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n9"
            + "1CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINH"
            + "aQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw";
        JwkResult key = JsonWebKey.Read(
            "{\"kty\":\"RSA\",\"n\":\"" + modulus + "\",\"e\":\"AQAB\"}");

        TokenTextResult result = JsonWebKey.ThumbprintUri(key.Key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(
                result.Text,
                Is.EqualTo(
                    "urn:ietf:params:oauth:jwk-thumbprint:sha-256:"
                    + "NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs"));
        });

        key.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void ThumbprintUriContainsBase64UrlEncodingOfThumbprint()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, true).Key;

        TokenBytesResult thumbprint = JsonWebKey.Thumbprint(key);
        TokenTextResult uri = JsonWebKey.ThumbprintUri(key);

        Assert.Multiple(() =>
        {
            Assert.That(uri.Success, Is.True);
            Assert.That(
                uri.Text,
                Is.EqualTo(
                    "urn:ietf:params:oauth:jwk-thumbprint:sha-256:"
                    + B64(thumbprint.Bytes)));
            Assert.That(uri.Text, Does.Not.Contain("="));
        });
    }

    [Test]
    public void ThumbprintUriSupportsOkpAndSymmetricKeys()
    {
        TokenKey okp = new TokenKey();
        okp.KeyType = TokenKeyTypes.OctetKeyPair;
        okp.Curve = "Ed25519";
        okp.OkpPublicKey = Enumerable.Repeat((byte)3, 32).ToArray();
        TokenKey symmetric = SymmetricKeyImporterOperations.Import(
            Enumerable.Repeat((byte)4, 32).ToArray()).Key;

        TokenTextResult okpUri = JsonWebKey.ThumbprintUri(okp);
        TokenTextResult symmetricUri = JsonWebKey.ThumbprintUri(symmetric);

        Assert.Multiple(() =>
        {
            Assert.That(okpUri.Success, Is.True);
            Assert.That(symmetricUri.Success, Is.True);
            Assert.That(okpUri.Text, Does.StartWith(
                "urn:ietf:params:oauth:jwk-thumbprint:sha-256:"));
            Assert.That(symmetricUri.Text, Does.StartWith(
                "urn:ietf:params:oauth:jwk-thumbprint:sha-256:"));
            Assert.That(okpUri.Text, Is.Not.EqualTo(symmetricUri.Text));
        });

        SymmetricKeyImporterOperations.Clear(symmetric);
    }

    [Test]
    public void ThumbprintUriIgnoresPrivateAndOptionalMetadata()
    {
        using RSA rsa = RSA.Create(2048);
        TokenKey privateKey = RsaKeyImporterOperations.Import(rsa, true).Key;
        TokenKey publicKey = RsaKeyImporterOperations.Import(rsa).Key;
        privateKey.KeyId = "ignored";
        privateKey.Algorithm = "ignored";
        privateKey.Use = "ignored";

        TokenTextResult privateUri = JsonWebKey.ThumbprintUri(privateKey);
        TokenTextResult publicUri = JsonWebKey.ThumbprintUri(publicKey);

        Assert.That(privateUri.Text, Is.EqualTo(publicUri.Text));
    }

    [Test]
    public void ThumbprintUriPropagatesFailuresAndRejectsNull()
    {
        TokenKey missing = new TokenKey();

        TokenTextResult result = JsonWebKey.ThumbprintUri(missing);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("missing_key_material"));
            Assert.That(() => JsonWebKey.ThumbprintUri(null), Throws.ArgumentNullException);
        });
    }

    static X509Certificate2 CreateCertificate(RSA rsa)
    {
        CertificateRequest request = new CertificateRequest(
            "CN=jwk-thumbprint-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            rsa,
            RSASignaturePadding.Pkcs1);
        return request.Create(
            request.SubjectName,
            generator,
            start,
            start.AddDays(2),
            new byte[] { 1 });
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    static void AssertFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }
}
