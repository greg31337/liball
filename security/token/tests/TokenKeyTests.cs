using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using token;

namespace token.Tests;

public class TokenKeyTests
{
    [Test]
    public void KeyContainerDefaultsToEmptyUnrestrictedState()
    {
        TokenKey key = new TokenKey();

        Assert.Multiple(() =>
        {
            Assert.That(key.KeyId, Is.Null);
            Assert.That(key.KeyIdBytes, Is.Null);
            Assert.That(key.KeyType, Is.Null);
            Assert.That(key.CoseKeyType, Is.Zero);
            Assert.That(key.Algorithm, Is.Null);
            Assert.That(key.CoseAlgorithm, Is.Zero);
            Assert.That(key.HasCoseAlgorithm, Is.False);
            Assert.That(key.Use, Is.Null);
            Assert.That(key.KeyOperations, Is.Empty);
            Assert.That(key.CoseKeyOperations, Is.Empty);
            Assert.That(key.BaseInitializationVector, Is.Null);
            Assert.That(key.Curve, Is.Null);
            Assert.That(key.CoseCurve, Is.Zero);
            Assert.That(key.SymmetricKey, Is.Null);
            Assert.That(key.Rsa, Is.Null);
            Assert.That(key.Ecdsa, Is.Null);
            Assert.That(key.OkpPublicKey, Is.Null);
            Assert.That(key.OkpPrivateKey, Is.Null);
            Assert.That(key.Certificate, Is.Null);
            Assert.That(key.CertificateChain, Is.Empty);
            Assert.That(key.ContainsPrivateMaterial(), Is.False);
            Assert.That(key.ContainsPublicMaterial(), Is.False);
        });
    }

    [Test]
    public void KeyContainerRecognizesSymmetricAndOkpMaterial()
    {
        TokenKey symmetric = new TokenKey();
        symmetric.SymmetricKey = new byte[] { 1, 2, 3 };
        TokenKey okpPublic = new TokenKey();
        okpPublic.OkpPublicKey = new byte[] { 4, 5 };
        TokenKey okpPrivate = new TokenKey();
        okpPrivate.OkpPrivateKey = new byte[] { 6, 7 };

        Assert.Multiple(() =>
        {
            Assert.That(symmetric.ContainsPrivateMaterial(), Is.True);
            Assert.That(symmetric.ContainsPublicMaterial(), Is.False);
            Assert.That(okpPublic.ContainsPublicMaterial(), Is.True);
            Assert.That(okpPublic.ContainsPrivateMaterial(), Is.False);
            Assert.That(okpPrivate.ContainsPrivateMaterial(), Is.True);
            Assert.That(okpPrivate.ContainsPublicMaterial(), Is.False);
        });
    }

    [Test]
    public void KeyContainerRecognizesNativeAndCertificateMaterial()
    {
        using RSA rsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using X509Certificate2 certificate = CreateCertificate();
        TokenKey rsaKey = new TokenKey();
        rsaKey.Rsa = rsa;
        rsaKey.NativePrivateMaterial = true;
        TokenKey ecKey = new TokenKey();
        ecKey.Ecdsa = ecdsa;
        TokenKey certificateKey = new TokenKey();
        certificateKey.Certificate = certificate;

        Assert.Multiple(() =>
        {
            Assert.That(rsaKey.ContainsPublicMaterial(), Is.True);
            Assert.That(rsaKey.ContainsPrivateMaterial(), Is.True);
            Assert.That(ecKey.ContainsPublicMaterial(), Is.True);
            Assert.That(ecKey.ContainsPrivateMaterial(), Is.False);
            Assert.That(certificateKey.ContainsPublicMaterial(), Is.True);
            Assert.That(certificateKey.ContainsPrivateMaterial(), Is.False);
        });
    }

    [Test]
    public void EmptyQueryMatchesAnyNonNullKey()
    {
        TokenKeyQuery query = new TokenKeyQuery();

        Assert.Multiple(() =>
        {
            Assert.That(query.Matches(new TokenKey()), Is.True);
            Assert.That(query.Matches(null), Is.False);
        });
    }

    [Test]
    public void QueryCombinesTextCriteriaAndMaterialRequirements()
    {
        TokenKey key = new TokenKey();
        key.KeyId = "signing-key";
        key.KeyType = TokenKeyTypes.EllipticCurve;
        key.Algorithm = JoseAlgorithms.EcdsaSha256;
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[]
        {
            TokenKeyOperations.Sign, TokenKeyOperations.Verify
        };
        key.Curve = "P-256";
        key.OkpPrivateKey = new byte[] { 1 };
        key.OkpPublicKey = new byte[] { 2 };

        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = "signing-key";
        query.KeyType = TokenKeyTypes.EllipticCurve;
        query.Algorithm = JoseAlgorithms.EcdsaSha256;
        query.Use = TokenKeyUses.Signature;
        query.KeyOperation = TokenKeyOperations.Verify;
        query.Curve = "P-256";
        query.RequirePrivateMaterial = true;
        query.RequirePublicMaterial = true;

        Assert.That(query.Matches(key), Is.True);

        key.Use = TokenKeyUses.Encryption;
        Assert.That(query.Matches(key), Is.False);
    }

    [Test]
    public void TextFiltersAreOrdinalAndCaseSensitive()
    {
        TokenKey key = new TokenKey();
        key.KeyId = "Key-A";
        key.KeyType = TokenKeyTypes.Rsa;
        key.Algorithm = JoseAlgorithms.RsaSha256;
        key.Use = TokenKeyUses.Signature;
        key.KeyOperations = new string[] { TokenKeyOperations.Verify };
        key.Curve = "P-256";

        Assert.Multiple(() =>
        {
            Assert.That(QueryWithKeyId("key-a").Matches(key), Is.False);
            Assert.That(QueryWithKeyType("rsa").Matches(key), Is.False);
            Assert.That(QueryWithAlgorithm("rs256").Matches(key), Is.False);
            Assert.That(QueryWithUse("SIG").Matches(key), Is.False);
            Assert.That(QueryWithOperation("Verify").Matches(key), Is.False);
            Assert.That(QueryWithCurve("p-256").Matches(key), Is.False);
        });
    }

    [Test]
    public void JoseQueryMatchesEquivalentCoseOnlyKey()
    {
        TokenKey key = new TokenKey();
        key.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        key.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        key.HasCoseAlgorithm = true;
        key.CoseKeyOperations = new int[] { CoseKeyLabels.OperationVerify };
        key.CoseCurve = CoseKeyLabels.CurveP256;

        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyType = TokenKeyTypes.EllipticCurve;
        query.Algorithm = JoseAlgorithms.EcdsaSha256;
        query.KeyOperation = TokenKeyOperations.Verify;
        query.Curve = "P-256";

        Assert.That(query.Matches(key), Is.True);
    }

    [Test]
    public void JoseAlgorithmQueryRecognizesLegacyCoseEcdsaLabel()
    {
        TokenKey key = new TokenKey();
        key.CoseAlgorithm = CoseAlgorithms.EcdsaSha256;
        key.HasCoseAlgorithm = true;
        TokenKeyQuery query = QueryWithAlgorithm(JoseAlgorithms.EcdsaSha256);

        Assert.That(query.Matches(key), Is.True);
    }

    [Test]
    public void CoseQueryMatchesEquivalentJoseOnlyKey()
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.OctetKeyPair;
        key.Algorithm = JoseAlgorithms.Ed25519;
        key.KeyOperations = new string[] { TokenKeyOperations.Sign };
        key.Curve = "Ed25519";

        TokenKeyQuery query = new TokenKeyQuery();
        query.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        query.CoseAlgorithm = CoseAlgorithms.Ed25519;
        query.HasCoseAlgorithm = true;
        query.CoseKeyOperation = CoseKeyLabels.OperationSign;
        query.CoseCurve = CoseKeyLabels.CurveEd25519;

        Assert.That(query.Matches(key), Is.True);
    }

    [Test]
    public void PopulatedNativeIdentifierTakesPrecedenceOverFallbackIdentifier()
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Rsa;
        key.CoseKeyType = CoseKeyLabels.KeyTypeEc2;
        key.Algorithm = JoseAlgorithms.RsaSha256;
        key.CoseAlgorithm = CoseAlgorithms.EcdsaP256Sha256;
        key.HasCoseAlgorithm = true;
        key.KeyOperations = new string[] { TokenKeyOperations.Sign };
        key.CoseKeyOperations = new int[] { CoseKeyLabels.OperationVerify };
        key.Curve = "P-384";
        key.CoseCurve = CoseKeyLabels.CurveP256;

        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyType = TokenKeyTypes.EllipticCurve;
        query.Algorithm = JoseAlgorithms.EcdsaSha256;
        query.KeyOperation = TokenKeyOperations.Verify;
        query.Curve = "P-256";

        Assert.That(query.Matches(key), Is.False);
    }

    [TestCase(TokenKeyOperations.Sign, CoseKeyLabels.OperationSign)]
    [TestCase(TokenKeyOperations.Verify, CoseKeyLabels.OperationVerify)]
    [TestCase(TokenKeyOperations.Encrypt, CoseKeyLabels.OperationEncrypt)]
    [TestCase(TokenKeyOperations.Decrypt, CoseKeyLabels.OperationDecrypt)]
    [TestCase(TokenKeyOperations.WrapKey, CoseKeyLabels.OperationWrapKey)]
    [TestCase(TokenKeyOperations.UnwrapKey, CoseKeyLabels.OperationUnwrapKey)]
    [TestCase(TokenKeyOperations.DeriveKey, CoseKeyLabels.OperationDeriveKey)]
    [TestCase(TokenKeyOperations.DeriveBits, CoseKeyLabels.OperationDeriveBits)]
    [TestCase(TokenKeyOperations.MacCreate, CoseKeyLabels.OperationMacCreate)]
    [TestCase(TokenKeyOperations.MacVerify, CoseKeyLabels.OperationMacVerify)]
    public void EveryNormalizedOperationMapsAcrossFormats(string operation, int coseOperation)
    {
        TokenKey coseKey = new TokenKey();
        coseKey.CoseKeyOperations = new int[] { coseOperation };
        TokenKey joseKey = new TokenKey();
        joseKey.KeyOperations = new string[] { operation };
        TokenKeyQuery joseQuery = QueryWithOperation(operation);
        TokenKeyQuery coseQuery = new TokenKeyQuery();
        coseQuery.CoseKeyOperation = coseOperation;

        Assert.Multiple(() =>
        {
            Assert.That(joseQuery.Matches(coseKey), Is.True);
            Assert.That(coseQuery.Matches(joseKey), Is.True);
        });
    }

    [Test]
    public void BinaryKeyIdentifierMustMatchExactly()
    {
        TokenKey key = new TokenKey();
        key.KeyIdBytes = new byte[] { 0, 1, 2, 255 };
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyIdBytes = new byte[] { 0, 1, 2, 255 };

        Assert.That(query.Matches(key), Is.True);

        query.KeyIdBytes = new byte[] { 0, 1, 3, 255 };
        Assert.That(query.Matches(key), Is.False);
    }

    [Test]
    public void BinaryMetadataQueriesTreatMissingCandidateValuesAsMismatches()
    {
        TokenKey key = new TokenKey();
        TokenKeyQuery identifier = new TokenKeyQuery();
        identifier.KeyIdBytes = new byte[] { 1 };
        TokenKeyQuery sha1 = new TokenKeyQuery();
        sha1.CertificateSha1Thumbprint = new byte[20];
        TokenKeyQuery sha256 = new TokenKeyQuery();
        sha256.CertificateSha256Thumbprint = new byte[32];

        Assert.Multiple(() =>
        {
            Assert.That(identifier.Matches(key), Is.False);
            Assert.That(sha1.Matches(key), Is.False);
            Assert.That(sha256.Matches(key), Is.False);
        });
    }

    [Test]
    public void CertificateQueriesUseStoredOrCalculatedThumbprints()
    {
        using X509Certificate2 certificate = CreateCertificate();
        byte[] sha1 = certificate.GetCertHash(HashAlgorithmName.SHA1);
        byte[] sha256 = certificate.GetCertHash(HashAlgorithmName.SHA256);
        TokenKey certificateKey = new TokenKey();
        certificateKey.Certificate = certificate;
        TokenKey storedKey = new TokenKey();
        storedKey.CertificateSha1Thumbprint = sha1;
        storedKey.CertificateSha256Thumbprint = sha256;
        TokenKeyQuery query = new TokenKeyQuery();
        query.CertificateSha1Thumbprint = sha1;
        query.CertificateSha256Thumbprint = sha256;

        Assert.Multiple(() =>
        {
            Assert.That(query.Matches(certificateKey), Is.True);
            Assert.That(query.Matches(storedKey), Is.True);
        });

        query.CertificateSha256Thumbprint = new byte[32];
        Assert.Multiple(() =>
        {
            Assert.That(query.Matches(certificateKey), Is.False);
            Assert.That(query.Matches(storedKey), Is.False);
        });
    }

    [Test]
    public void CertificateValueTakesPrecedenceOverStoredThumbprintMetadata()
    {
        using X509Certificate2 certificate = CreateCertificate();
        byte[] incorrect = new byte[32];
        TokenKey key = new TokenKey();
        key.Certificate = certificate;
        key.CertificateSha256Thumbprint = incorrect;
        TokenKeyQuery query = new TokenKeyQuery();
        query.CertificateSha256Thumbprint = incorrect;

        Assert.That(query.Matches(key), Is.False);
    }

    [Test]
    public void QueryToleratesUnsetOperationArrays()
    {
        TokenKey key = new TokenKey();
        key.KeyOperations = null;
        key.CoseKeyOperations = null;

        Assert.Multiple(() =>
        {
            Assert.That(QueryWithOperation(TokenKeyOperations.Sign).Matches(key), Is.False);
            TokenKeyQuery query = new TokenKeyQuery();
            query.CoseKeyOperation = CoseKeyLabels.OperationSign;
            Assert.That(query.Matches(key), Is.False);
        });
    }

    static TokenKeyQuery QueryWithKeyId(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = value;
        return query;
    }

    static TokenKeyQuery QueryWithKeyType(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyType = value;
        return query;
    }

    static TokenKeyQuery QueryWithAlgorithm(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.Algorithm = value;
        return query;
    }

    static TokenKeyQuery QueryWithUse(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.Use = value;
        return query;
    }

    static TokenKeyQuery QueryWithOperation(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyOperation = value;
        return query;
    }

    static TokenKeyQuery QueryWithCurve(string value)
    {
        TokenKeyQuery query = new TokenKeyQuery();
        query.Curve = value;
        return query;
    }

    static X509Certificate2 CreateCertificate()
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest(
            "CN=token-key-query-test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DateTimeOffset start = DateTimeOffset.UtcNow.AddMinutes(-1);
        X509SignatureGenerator generator = X509SignatureGenerator.CreateForRSA(
            key,
            RSASignaturePadding.Pkcs1);
        return request.Create(
            request.SubjectName,
            generator,
            start,
            start.AddDays(1),
            new byte[] { 2 });
    }
}
