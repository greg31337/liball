using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class EcdhKeyWrapTests
{
    [Test]
    public void DecryptMatchesRfc7520Section54Vector()
    {
        JwkResult recipient = JsonWebKey.Read(
            "{\"kty\":\"EC\",\"crv\":\"P-384\","
            + "\"x\":\"YU4rRUzdmVqmRtWOs2OpDE_T5fsNIodcG8G5FWPrTPMyxpzsSOGaQLpe2FpxBmu2\","
            + "\"y\":\"A8-yxCHxkfBz3hKZfI1jUYMjUhsEveZ9THuwFjH2sCNdtksRJU7D5-SkgaFL1ETP\","
            + "\"d\":\"iTx2pk7wW-GqJkHcEkFQb2EFyYcO7RugmaW3mRrQVAOUiPommT0IdnYK2xDlZh-j\"}");
        JwkResult ephemeral = JsonWebKey.Read(
            "{\"kty\":\"EC\",\"crv\":\"P-384\","
            + "\"x\":\"uBo4kHPw6kbjx5l0xowrd_oYzBmaz-GKFZu4xAFFkbYiWgutEK6iuEDsQ6wNdNg3\","
            + "\"y\":\"sp3p5SGhZVC2faXumI-e9JU2Mo8KpoYrFDr5yPNVtW4PgEwZOyQTA-JdaY8tb7E0\"}");

        EcdhKeyManagementResult result = EcdhAlgorithms.Decrypt(
            Decode("0DJjBXri_kBcC46IkU5_Jk9BqaQeHdv2"),
            recipient.Key,
            ephemeral.Key,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertWrappedSuccess(result, 16);
            Assert.That(result.ContentEncryptionKey,
                Is.EqualTo(Decode("Nou2ueKlP70ZXDbq9UrRwg")));
            Assert.That(result.EncryptedKey,
                Is.EqualTo(Decode("0DJjBXri_kBcC46IkU5_Jk9BqaQeHdv2")));
            Assert.That(result.EphemeralPublicKey, Is.Null);
        });

        recipient.Key.ReleaseOwnedMaterial();
        ephemeral.Key.ReleaseOwnedMaterial();
    }

    [TestCase("P-256", JoseAlgorithms.EcdhEsAes128KeyWrap,
        JoseAlgorithms.Aes128Gcm, 16)]
    [TestCase("P-384", JoseAlgorithms.EcdhEsAes192KeyWrap,
        JoseAlgorithms.Aes192Gcm, 24)]
    [TestCase("P-521", JoseAlgorithms.EcdhEsAes256KeyWrap,
        JoseAlgorithms.Aes256CbcHmacSha512, 64)]
    public void GeneratedContentKeysRoundTripForEveryWrapSize(
        string curveName,
        string algorithm,
        string contentEncryptionAlgorithm,
        int contentEncryptionKeySize)
    {
        using ECDsa recipient = CreateCurve(curveName);
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            recipient,
            algorithm,
            true).Key;
        ECParameters publicParameters = recipient.ExportParameters(false);
        using ECDsa publicRecipient = ECDsa.Create(publicParameters);
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            publicRecipient,
            algorithm,
            false).Key;
        byte[] partyU = Encoding.UTF8.GetBytes("sender");
        byte[] partyV = Encoding.UTF8.GetBytes("recipient");

        EcdhKeyManagementResult encrypted = EcdhAlgorithms.Encrypt(
            publicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyU,
            partyV);
        EcdhKeyManagementResult decrypted = EcdhAlgorithms.Decrypt(
            encrypted.EncryptedKey,
            privateKey,
            encrypted.EphemeralPublicKey,
            algorithm,
            contentEncryptionAlgorithm,
            partyU,
            partyV);

        Assert.Multiple(() =>
        {
            AssertWrappedSuccess(encrypted, contentEncryptionKeySize);
            AssertWrappedSuccess(decrypted, contentEncryptionKeySize);
            Assert.That(encrypted.EncryptedKey,
                Has.Length.EqualTo(contentEncryptionKeySize + 8));
            Assert.That(decrypted.ContentEncryptionKey,
                Is.EqualTo(encrypted.ContentEncryptionKey));
            Assert.That(encrypted.EphemeralPublicKey, Is.Not.Null);
            Assert.That(encrypted.EphemeralPublicKey.NativePublicMaterial, Is.True);
            Assert.That(encrypted.EphemeralPublicKey.NativePrivateMaterial, Is.False);
            Assert.That(encrypted.EphemeralPublicKey.Curve, Is.EqualTo(curveName));
            Assert.That(encrypted.EphemeralPublicKey.Algorithm, Is.Null);
        });

        encrypted.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void CallerSuppliedKeyAndPartyInfoSpansRoundTrip()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            recipient,
            JoseAlgorithms.EcdhEsAes256KeyWrap,
            true).Key;
        byte[] contentEncryptionKey = Sequence(16);
        byte[] expected = contentEncryptionKey.ToArray();
        byte[] partyU = Container(Encoding.UTF8.GetBytes("Alice"));
        byte[] partyV = Container(Encoding.UTF8.GetBytes("Bob"));

        EcdhKeyManagementResult wrapped = EcdhAlgorithms.Wrap(
            contentEncryptionKey.AsSpan(),
            key,
            JoseAlgorithms.EcdhEsAes256KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            partyU.AsSpan(1, 5),
            partyV.AsSpan(1, 3));
        contentEncryptionKey[0] ^= 255;
        EcdhKeyManagementResult unwrapped = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey.AsSpan(),
            key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes256KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Alice"),
            Encoding.UTF8.GetBytes("Bob"));

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.ContentEncryptionKey, Is.EqualTo(expected));
            Assert.That(unwrapped.ContentEncryptionKey, Is.EqualTo(expected));
            Assert.That(wrapped.ContentEncryptionKey,
                Is.Not.SameAs(contentEncryptionKey));
            Assert.That(partyU[0], Is.Zero);
            Assert.That(partyU[6], Is.Zero);
            Assert.That(partyV[0], Is.Zero);
            Assert.That(partyV[4], Is.Zero);
        });

        wrapped.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void TamperingWrongAgreementContextAndMalformedInputFailUniformly()
    {
        using ECDsa recipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa otherRecipient = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(
            recipient,
            true).Key;
        TokenKey otherKey = EcdsaKeyImporter.Import(
            otherRecipient,
            true).Key;
        byte[] partyU = Encoding.UTF8.GetBytes("Alice");
        byte[] partyV = Encoding.UTF8.GetBytes("Bob");
        EcdhKeyManagementResult wrapped = EcdhAlgorithms.Wrap(
            Sequence(16),
            key,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            partyU,
            partyV);
        byte[] tampered = wrapped.EncryptedKey.ToArray();
        tampered[8] ^= 1;

        EcdhKeyManagementResult authenticationFailure = EcdhAlgorithms.Decrypt(
            tampered,
            key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            partyU,
            partyV);
        EcdhKeyManagementResult contextFailure = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey,
            key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Mallory"),
            partyV);
        EcdhKeyManagementResult recipientFailure = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey,
            otherKey,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            partyU,
            partyV);
        EcdhKeyManagementResult lengthFailure = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey.AsSpan(1),
            key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm,
            partyU,
            partyV);

        Assert.Multiple(() =>
        {
            AssertFailure(authenticationFailure, "unwrap_failed");
            AssertFailure(contextFailure, "unwrap_failed");
            AssertFailure(recipientFailure, "unwrap_failed");
            AssertFailure(lengthFailure, "unwrap_failed");
        });

        wrapped.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void JoseAndEquivalentCoseAlgorithmBindingsAreEnforced()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKeyResult jose = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            true);
        TokenKeyResult cose = EcdsaKeyImporter.ImportCose(
            source,
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            true);
        TokenKeyResult conflicting = EcdsaKeyImporter.ImportCose(
            source,
            CoseAlgorithms.EcdhEsAes256KeyWrap,
            true);

        EcdhKeyManagementResult wrapped = EcdhAlgorithms.Wrap(
            Sequence(16),
            jose.Key,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        EcdhKeyManagementResult unwrapped = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey,
            cose.Key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        EcdhKeyManagementResult rejected = EcdhAlgorithms.Wrap(
            Sequence(16),
            conflicting.Key,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(jose.Success, Is.True);
            Assert.That(cose.Success, Is.True);
            Assert.That(conflicting.Success, Is.True);
            Assert.That(unwrapped.ContentEncryptionKey,
                Is.EqualTo(wrapped.ContentEncryptionKey));
            AssertFailure(rejected, "conflicting_key_algorithm");
        });

        wrapped.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void KeyOperationsAndEphemeralAlgorithmMetadataAreEnforced()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            true).Key;
        TokenKey denied = EcdsaKeyImporter.ImportJose(
            source,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            false).Key;
        denied.KeyOperations = new string[] { TokenKeyOperations.WrapKey };
        EcdhKeyManagementResult wrapped = EcdhAlgorithms.Wrap(
            Sequence(16),
            key,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        wrapped.EphemeralPublicKey.HasCoseAlgorithm = true;
        wrapped.EphemeralPublicKey.CoseAlgorithm =
            CoseAlgorithms.EcdhEsAes256KeyWrap;

        EcdhKeyManagementResult operationFailure = EcdhAlgorithms.Wrap(
            Sequence(16),
            denied,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);
        EcdhKeyManagementResult metadataFailure = EcdhAlgorithms.Decrypt(
            wrapped.EncryptedKey,
            key,
            wrapped.EphemeralPublicKey,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertFailure(operationFailure, "key_operation_not_permitted");
            AssertFailure(metadataFailure, "conflicting_key_algorithm");
        });

        wrapped.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void WrongKeySizeAndIncompatibleOverloadsReturnStableFailures()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(source, true).Key;
        EcdhKeyManagementResult direct = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            AssertFailure(EcdhAlgorithms.Wrap(
                Sequence(24),
                key,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), "invalid_key_size");
            AssertFailure(EcdhAlgorithms.Wrap(
                Sequence(16),
                key,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "invalid_algorithm");
            AssertFailure(EcdhAlgorithms.Decrypt(
                key,
                direct.EphemeralPublicKey,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), "invalid_algorithm");
            AssertFailure(EcdhAlgorithms.Decrypt(
                Array.Empty<byte>(),
                key,
                direct.EphemeralPublicKey,
                JoseAlgorithms.EcdhEs,
                JoseAlgorithms.Aes128Gcm), "invalid_algorithm");
        });

        direct.EphemeralPublicKey.ReleaseOwnedMaterial();
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(source, true).Key;
        byte[] contentEncryptionKey = Sequence(16);
        byte[] info = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(() => EcdhAlgorithms.Wrap(
                null,
                key,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Wrap(
                contentEncryptionKey,
                null,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Wrap(
                contentEncryptionKey,
                key,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm,
                null,
                info), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                null,
                key,
                key,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                contentEncryptionKey,
                key,
                null,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm), Throws.ArgumentNullException);
            Assert.That(() => EcdhAlgorithms.Decrypt(
                contentEncryptionKey,
                key,
                key,
                JoseAlgorithms.EcdhEsAes128KeyWrap,
                JoseAlgorithms.Aes128Gcm,
                info,
                null), Throws.ArgumentNullException);
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

    static byte[] Decode(string text)
    {
        TokenBytesResult result = Base64Url.Decode(text);
        Assert.That(result.Success, Is.True);
        return result.Bytes;
    }

    static byte[] Sequence(int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)index;
            index += 1;
        }

        return result;
    }

    static byte[] Container(byte[] value)
    {
        byte[] container = new byte[value.Length + 2];
        value.CopyTo(container, 1);
        return container;
    }

    static void AssertWrappedSuccess(
        EcdhKeyManagementResult result,
        int keySize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentEncryptionKey, Has.Length.EqualTo(keySize));
            Assert.That(result.EncryptedKey, Is.Not.Empty);
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
