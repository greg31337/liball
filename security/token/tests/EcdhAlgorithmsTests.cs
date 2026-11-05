using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class EcdhAlgorithmsTests
{
    [Test]
    public void DecryptMatchesRfc7518AppendixCVector()
    {
        JwkResult alice = JsonWebKey.Read(
            "{\"kty\":\"EC\",\"crv\":\"P-256\","
            + "\"x\":\"gI0GAILBdu7T53akrFmMyGcsF3n5dO7MmwNBHKW5SV0\","
            + "\"y\":\"SLW_xSffzlPWrHEVI30DHM_4egVwt3NQqeUD7nMFpps\"}");
        JwkResult bob = JsonWebKey.Read(
            "{\"kty\":\"EC\",\"crv\":\"P-256\","
            + "\"x\":\"weNJy2HscCSM6AEDTDg04biOvhFhyyWvOHQfeF_PxMQ\","
            + "\"y\":\"e8lnCO-AlStT-NJVX-crhB7QRYhiix03illJOVAOyck\","
            + "\"d\":\"VEmDZpDXXK8p8N0Cndsxs924q6nS1RXFASRl6BfUqdw\"}");
        byte[] expected = new byte[]
        {
            86, 170, 141, 234, 248, 35, 109, 32,
            92, 34, 40, 205, 113, 167, 16, 26
        };

        EcdhKeyManagementResult result = EcdhAlgorithms.Decrypt(
            bob.Key,
            alice.Key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Alice"),
            Encoding.UTF8.GetBytes("Bob"));

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 16);
            Assert.That(result.ContentEncryptionKey, Is.EqualTo(expected));
            Assert.That(result.EphemeralPublicKey, Is.Null);
        });

        alice.Key.ReleaseOwnedMaterial();
        bob.Key.ReleaseOwnedMaterial();
    }

    [TestCase("P-256", JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase("P-384", JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase("P-521", JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void GeneratedAgreementRoundTripsForSupportedCurvesAndKeySizes(
        string curveName,
        string contentEncryptionAlgorithm,
        int keySize)
    {
        using ECDsa recipient = CreateCurve(curveName);
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        ECParameters publicParameters = recipient.ExportParameters(false);
        using ECDsa recipientPublic = ECDsa.Create(publicParameters);
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            recipientPublic,
            JoseAlgorithms.EcdhEs,
            false).Key;

        EcdhKeyManagementResult encrypted = EcdhAlgorithms.Encrypt(
            publicKey,
            JoseAlgorithms.EcdhEs,
            contentEncryptionAlgorithm,
            Encoding.UTF8.GetBytes("sender"),
            Encoding.UTF8.GetBytes("recipient"));
        EcdhKeyManagementResult decrypted = EcdhAlgorithms.Decrypt(
            privateKey,
            encrypted.EphemeralPublicKey,
            JoseAlgorithms.EcdhEs,
            contentEncryptionAlgorithm,
            Encoding.UTF8.GetBytes("sender"),
            Encoding.UTF8.GetBytes("recipient"));

        Assert.Multiple(() =>
        {
            AssertSuccess(encrypted, keySize);
            AssertSuccess(decrypted, keySize);
            Assert.That(decrypted.ContentEncryptionKey,
                Is.EqualTo(encrypted.ContentEncryptionKey));
            Assert.That(encrypted.EphemeralPublicKey, Is.Not.Null);
            Assert.That(encrypted.EphemeralPublicKey.NativePublicMaterial, Is.True);
            Assert.That(encrypted.EphemeralPublicKey.NativePrivateMaterial, Is.False);
            Assert.That(encrypted.EphemeralPublicKey.OwnsNativeKey, Is.True);
            Assert.That(encrypted.EphemeralPublicKey.Curve, Is.EqualTo(curveName));
            Assert.That(encrypted.EphemeralPublicKey.Algorithm, Is.Null);
        });

        encrypted.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void EncryptGeneratesFreshEphemeralKeys()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            false).Key;

        EcdhKeyManagementResult first = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes256Gcm);
        EcdhKeyManagementResult second = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes256Gcm);
        ECParameters firstParameters = first.EphemeralPublicKey.Ecdsa.ExportParameters(false);
        ECParameters secondParameters = second.EphemeralPublicKey.Ecdsa.ExportParameters(false);

        Assert.Multiple(() =>
        {
            Assert.That(firstParameters.Q.X, Is.Not.EqualTo(secondParameters.Q.X));
            Assert.That(firstParameters.Q.Y, Is.Not.EqualTo(secondParameters.Q.Y));
            Assert.That(first.ContentEncryptionKey,
                Is.Not.EqualTo(second.ContentEncryptionKey));
        });

        first.EphemeralPublicKey.ReleaseOwnedMaterial();
        second.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void PartyInfoSpansUseOnlySelectedSlices()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        byte[] partyU = Container(Encoding.UTF8.GetBytes("Alice"));
        byte[] partyV = Container(Encoding.UTF8.GetBytes("Bob"));

        EcdhKeyManagementResult encrypted = EcdhAlgorithms.Encrypt(
            privateKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm,
            partyU.AsSpan(1, 5),
            partyV.AsSpan(1, 3));
        EcdhKeyManagementResult decrypted = EcdhAlgorithms.Decrypt(
            privateKey,
            encrypted.EphemeralPublicKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Alice"),
            Encoding.UTF8.GetBytes("Bob"));

        Assert.Multiple(() =>
        {
            Assert.That(decrypted.ContentEncryptionKey,
                Is.EqualTo(encrypted.ContentEncryptionKey));
            Assert.That(partyU[0], Is.Zero);
            Assert.That(partyU[6], Is.Zero);
            Assert.That(partyV[0], Is.Zero);
            Assert.That(partyV[4], Is.Zero);
        });

        encrypted.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void ChangedPartyInfoProducesDifferentDerivedKey()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        EcdhKeyManagementResult encrypted = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Alice"),
            Encoding.UTF8.GetBytes("Bob"));

        EcdhKeyManagementResult changed = EcdhAlgorithms.Decrypt(
            key,
            encrypted.EphemeralPublicKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Mallory"),
            Encoding.UTF8.GetBytes("Bob"));

        Assert.That(changed.ContentEncryptionKey,
            Is.Not.EqualTo(encrypted.ContentEncryptionKey));
        encrypted.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void EcdhTaggedEcJwkCanBeReadAndWritten()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters parameters = source.ExportParameters(false);
        string json = "{\"kty\":\"EC\",\"crv\":\"P-256\",\"x\":\""
            + B64(parameters.Q.X) + "\",\"y\":\"" + B64(parameters.Q.Y)
            + "\",\"alg\":\"ECDH-ES\"}";

        JwkResult read = JsonWebKey.Read(json);
        TokenTextResult written = JsonWebKey.WritePublic(read.Key);

        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True);
            Assert.That(read.Key.Algorithm, Is.EqualTo(JoseAlgorithms.EcdhEs));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Does.Contain("\"alg\":\"ECDH-ES\""));
        });

        read.Key.ReleaseOwnedMaterial();
    }

    [Test]
    public void DecryptRequiresPrivateRecipientAndPublicEphemeralKey()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters recipientPublicParameters = recipient.ExportParameters(false);
        using ECDsa recipientPublic = ECDsa.Create(recipientPublicParameters);
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            recipientPublic,
            JoseAlgorithms.EcdhEs,
            false).Key;
        EcdhKeyManagementResult encrypted = EcdhAlgorithms.Encrypt(
            publicKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);
        TokenKey privateEphemeral = EcdsaKeyImporter.ImportJose(
            ECDsa.Create(ECCurve.NamedCurves.nistP256),
            JoseAlgorithms.EcdhEs,
            true).Key;
        privateEphemeral.OwnsNativeKey = true;

        EcdhKeyManagementResult missingPrivate = EcdhAlgorithms.Decrypt(
            publicKey,
            encrypted.EphemeralPublicKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);
        EcdhKeyManagementResult privateHeader = EcdhAlgorithms.Decrypt(
            privateKey,
            privateEphemeral,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertFailure(missingPrivate, "missing_private_key");
            AssertFailure(privateHeader, "invalid_ephemeral_key");
        });

        encrypted.EphemeralPublicKey.ReleaseOwnedMaterial();
        privateEphemeral.ReleaseOwnedMaterial();
    }

    [Test]
    public void DecryptRejectsCurveMismatch()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa ephemeral = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey recipientKey = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEs,
            true).Key;
        TokenKey ephemeralKey = EcdsaKeyImporter.ImportJose(
            ephemeral,
            JoseAlgorithms.EcdhEs,
            false).Key;

        EcdhKeyManagementResult result = EcdhAlgorithms.Decrypt(
            recipientKey,
            ephemeralKey,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);

        AssertFailure(result, "curve_mismatch");
    }

    [Test]
    public void KeyMetadataAndOperationsAreEnforced()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey conflictingAlgorithm = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdsaSha256,
            false).Key;
        TokenKey conflictingCose = EcdsaKeyImporter.ImportCose(
            source,
            CoseAlgorithms.EcdsaP256Sha256,
            false).Key;
        TokenKey signatureUse = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEs,
            false).Key;
        signatureUse.Use = TokenKeyUses.Signature;
        TokenKey deniedOperation = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEs,
            false).Key;
        deniedOperation.KeyOperations = new string[] { TokenKeyOperations.DeriveBits };
        TokenKey allowedOperation = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEs,
            false).Key;
        allowedOperation.CoseKeyOperations = new int[] { CoseKeyLabels.OperationDeriveKey };

        Assert.Multiple(() =>
        {
            AssertFailure(EcdhAlgorithms.Encrypt(
                conflictingAlgorithm,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "conflicting_key_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                conflictingCose,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "conflicting_key_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                signatureUse,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "key_use_algorithm_mismatch");
            AssertFailure(EcdhAlgorithms.Encrypt(
                deniedOperation,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "key_operation_not_permitted");
            EcdhKeyManagementResult allowed = EcdhAlgorithms.Encrypt(
                allowedOperation,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm);
            Assert.That(allowed.Success, Is.True);
            allowed.EphemeralPublicKey.ReleaseOwnedMaterial();
        });
    }

    [Test]
    public void WrongKeyTypeAndMissingMaterialAreRejected()
    {
        TokenKey wrongType = new TokenKey();
        wrongType.KeyType = TokenKeyTypes.Rsa;
        TokenKey missing = new TokenKey();
        missing.KeyType = TokenKeyTypes.EllipticCurve;

        Assert.Multiple(() =>
        {
            AssertFailure(EcdhAlgorithms.Encrypt(
                wrongType,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "invalid_key_type");
            AssertFailure(EcdhAlgorithms.Encrypt(
                missing,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "missing_key_material");
        });
    }

    [Test]
    public void UnsupportedAlgorithmsReturnStableFailures()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(source, false).Key;

        Assert.Multiple(() =>
        {
            AssertFailure(EcdhAlgorithms.Encrypt(
                key, "ecdh-es", JoseAlgorithms.Aes128Gcm), "unknown_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                key,
                JoseAlgorithms.Aes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), "invalid_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                key,
                JoseAlgorithms.EcdhEs,
                "a128gcm"), "unknown_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                key,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128KeyWrap), "invalid_algorithm");
            AssertFailure(EcdhAlgorithms.Encrypt(
                key,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Cbc), "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEs,
            true).Key;
        byte[] info = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(() => EcdhAlgorithms.Encrypt(
                null, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Encrypt(
                key, null, JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Encrypt(
                key, JoseAlgorithms.EcdhEs, null), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Encrypt(
                key, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm, null, info),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Encrypt(
                key, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm, info, null),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                null, key, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                key, null, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                key, key, null, JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                key, key, JoseAlgorithms.EcdhEs, null),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                key, key, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm, null, info),
                Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                key, key, JoseAlgorithms.EcdhEs, JoseAlgorithms.Aes128Gcm, info, null),
                Throws.ArgumentNullException);
        });
    }

    static ECDsa CreateCurve(string name)
    {
        if (name == "P-256")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        if (name == "P-384")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP521);
    }

    static string B64(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    static byte[] Container(byte[] value)
    {
        byte[] container = new byte[value.Length + 2];
        value.CopyTo(container, 1);
        return container;
    }

    static void AssertSuccess(EcdhKeyManagementResult result, int keySize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentEncryptionKey, Has.Length.EqualTo(keySize));
            Assert.That(result.EncryptedKey, Is.Empty);
        });
    }

    static void AssertFailure(EcdhKeyManagementResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ContentEncryptionKey, Is.Null);
            Assert.That(result.EncryptedKey, Is.Null);
            Assert.That(result.EphemeralPublicKey, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
