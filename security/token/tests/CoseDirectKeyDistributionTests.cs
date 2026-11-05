using token;

namespace token.Tests;

public class CoseDirectKeyDistributionTests
{
    [TestCase(
        CoseAlgorithms.DirectHkdfSha256,
        "849B57219DAE48DE646D07DBB533566E976686457C1491BE3A76DCEA6C427188",
        "A10129",
        "32547753D1E24F41579D770BA852D4C9")]
    [TestCase(
        CoseAlgorithms.DirectHkdfSha512,
        "849B57219DAE48DE646D07DBB533566E976686457C1491BE3A76DCEA6C427188",
        "A1012A",
        "C734655EAD210D8354018308986BD01D")]
    [TestCase(
        CoseAlgorithms.DirectHkdfAes128,
        "849B57219DAE48DE646D07DBB533566E",
        "A1012B",
        "F0CCBAF836D73DA63ED8508EF966EEC9")]
    [TestCase(
        CoseAlgorithms.DirectHkdfAes256,
        "0F1E2D3C4B5A69788796A5B4C3D2E1F01F2E3D4C5B6A798897A6B5C4D3E2F100",
        "A1012C",
        "17B0BCA769867BF795D2AA0D77C6984D")]
    public void DirectHkdfMatchesCoseWorkingGroupVectors(
        int algorithm,
        string secretHex,
        string protectedHex,
        string expectedHex)
    {
        TokenKey key = Key(
            Convert.FromHexString(secretHex),
            algorithm);
        key.CoseAlgorithm = 0;
        key.HasCoseAlgorithm = false;
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.AesCcm16_64_128,
            128,
            protectedHex);
        byte[] salt = Convert.FromHexString(
            "61616262636364646565666667676868");

        CoseDirectKeyDistributionResult encrypted =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                algorithm,
                salt,
                parameters);
        CoseDirectKeyDistributionResult decrypted =
            CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                key,
                algorithm,
                salt,
                parameters);

        byte[] expected = Convert.FromHexString(expectedHex);
        Assert.Multiple(() =>
        {
            AssertSuccess(encrypted, expected);
            AssertSuccess(decrypted, expected);
            Assert.That(decrypted.ContentKey,
                Is.Not.SameAs(encrypted.ContentKey));
        });
    }

    [Test]
    public void DirectReturnsIndependentCopiesOfExactContentKey()
    {
        byte[] secret = Sequence(32, 11);
        TokenKey key = Key(secret, CoseAlgorithms.Direct);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes256Gcm,
            256,
            "");

        CoseDirectKeyDistributionResult encrypted =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Direct,
                null,
                parameters);
        CoseDirectKeyDistributionResult decrypted =
            CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Direct,
                null,
                parameters);

        Assert.Multiple(() =>
        {
            AssertSuccess(encrypted, secret);
            AssertSuccess(decrypted, secret);
            Assert.That(encrypted.ContentKey,
                Is.Not.SameAs(key.SymmetricKey));
            Assert.That(decrypted.ContentKey,
                Is.Not.SameAs(encrypted.ContentKey));
        });

        encrypted.ContentKey[0] = 0;
        Assert.Multiple(() =>
        {
            Assert.That(key.SymmetricKey[0], Is.EqualTo(11));
            Assert.That(decrypted.ContentKey[0], Is.EqualTo(11));
            Assert.That(secret[0], Is.EqualTo(11));
        });
    }

    [Test]
    public void PartyUNonceCanProvideUniquenessWithoutSalt()
    {
        TokenKey key = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        CoseKdfContextParameters first = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        first.PartyUInfo.Nonce = Bytes(new byte[] { 1, 2, 3 });
        CoseKdfContextParameters second = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        second.PartyUInfo.Nonce = Bytes(new byte[] { 1, 2, 4 });

        CoseDirectKeyDistributionResult firstResult =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                null,
                first);
        CoseDirectKeyDistributionResult repeated =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                null,
                first);
        CoseDirectKeyDistributionResult secondResult =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                null,
                second);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(repeated.ContentKey,
                Is.EqualTo(firstResult.ContentKey));
            Assert.That(secondResult.ContentKey,
                Is.Not.EqualTo(firstResult.ContentKey));
        });
    }

    [TestCase(CoseAlgorithms.DirectHkdfSha256, 32)]
    [TestCase(CoseAlgorithms.DirectHkdfSha512, 64)]
    [TestCase(CoseAlgorithms.DirectHkdfAes128, 16)]
    [TestCase(CoseAlgorithms.DirectHkdfAes256, 32)]
    public void SaltAndContextChangesProduceDifferentDerivedKeys(
        int algorithm,
        int secretLength)
    {
        TokenKey key = Key(Sequence(secretLength, 4), algorithm);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes256Gcm,
            256,
            ProtectedFor(algorithm));
        CoseKdfContextParameters changedContext = Context(
            CoseAlgorithms.Aes256Gcm,
            256,
            ProtectedFor(algorithm));
        changedContext.PartyVInfo.Identity = new byte[] { 9 };

        CoseDirectKeyDistributionResult baseline =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                algorithm,
                new byte[] { 1 },
                parameters);
        CoseDirectKeyDistributionResult changedSalt =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                algorithm,
                new byte[] { 2 },
                parameters);
        CoseDirectKeyDistributionResult changedInformation =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                algorithm,
                new byte[] { 1 },
                changedContext);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.ContentKey, Has.Length.EqualTo(32));
            Assert.That(changedInformation.ContentKey,
                Is.Not.EqualTo(baseline.ContentKey));
            if (algorithm == CoseAlgorithms.DirectHkdfAes128
                || algorithm == CoseAlgorithms.DirectHkdfAes256)
            {
                Assert.That(changedSalt.ContentKey,
                    Is.EqualTo(baseline.ContentKey));
            }
            else
            {
                Assert.That(changedSalt.ContentKey,
                    Is.Not.EqualTo(baseline.ContentKey));
            }
        });
    }

    [Test]
    public void DecryptionRequiresEmptyRecipientCiphertext()
    {
        TokenKey key = Key(
            Sequence(16, 1),
            CoseAlgorithms.Direct);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        byte[] container = new byte[] { 9, 8, 7 };

        CoseDirectKeyDistributionResult arrayResult =
            CoseDirectKeyDistributionOperations.Decrypt(
                new byte[] { 1 },
                key,
                CoseAlgorithms.Direct,
                null,
                parameters);
        CoseDirectKeyDistributionResult spanResult =
            CoseDirectKeyDistributionOperations.Decrypt(
                container.AsSpan(1, 1),
                key,
                CoseAlgorithms.Direct,
                null,
                parameters);
        CoseDirectKeyDistributionResult emptySlice =
            CoseDirectKeyDistributionOperations.Decrypt(
                container.AsSpan(1, 0),
                key,
                CoseAlgorithms.Direct,
                null,
                parameters);

        Assert.Multiple(() =>
        {
            AssertFailure(arrayResult, "invalid_encrypted_key");
            AssertFailure(spanResult, "invalid_encrypted_key");
            Assert.That(emptySlice.Success, Is.True);
        });
    }

    [Test]
    public void DirectRequiresEmptyProtectedHeadersAndExactKeySize()
    {
        TokenKey key = Key(
            Sequence(16, 1),
            CoseAlgorithms.Direct);
        CoseKdfContextParameters protectedParameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10126");
        CoseKdfContextParameters wrongSize = Context(
            CoseAlgorithms.Aes256Gcm,
            256,
            "");
        CoseKdfContextParameters invalidSize = Context(
            CoseAlgorithms.Aes128Gcm,
            127,
            "");

        Assert.Multiple(() =>
        {
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Direct,
                null,
                protectedParameters),
                "invalid_protected_headers");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Direct,
                null,
                wrongSize),
                "invalid_key_size");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Direct,
                null,
                invalidSize),
                "invalid_key_size");
        });
    }

    [Test]
    public void DirectHkdfRequiresUniquenessAndValidOutputSize()
    {
        TokenKey key = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        CoseKdfContextParameters missing = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        CoseKdfContextParameters tooLarge = Context(
            CoseAlgorithms.Aes128Gcm,
            65288,
            "A10129");
        CoseKdfContextParameters maximum = Context(
            CoseAlgorithms.Aes128Gcm,
            65280,
            "A10129");

        CoseDirectKeyDistributionResult maximumResult =
            CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                maximum);

        Assert.Multiple(() =>
        {
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                null,
                missing),
                "missing_kdf_uniqueness");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                tooLarge),
                "invalid_key_size");
            Assert.That(maximumResult.Success, Is.True);
            Assert.That(maximumResult.ContentKey,
                Has.Length.EqualTo(8160));
        });
    }

    [Test]
    public void AesHkdfRequiresItsExactSharedSecretSize()
    {
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A1012B");
        TokenKey shortKey = Key(
            Sequence(15, 1),
            CoseAlgorithms.DirectHkdfAes128);
        TokenKey longKey = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfAes128);

        Assert.Multiple(() =>
        {
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                shortKey,
                CoseAlgorithms.DirectHkdfAes128,
                new byte[] { 1 },
                parameters),
                "weak_key");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                longKey,
                CoseAlgorithms.DirectHkdfAes128,
                new byte[] { 1 },
                parameters),
                "invalid_key_size");
        });
    }

    [Test]
    public void KeyAlgorithmUseAndTypeMetadataAreEnforced()
    {
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        TokenKey wrongCose = Key(
            Sequence(64, 1),
            CoseAlgorithms.DirectHkdfSha512);
        TokenKey wrongJose = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        wrongJose.Algorithm = JoseAlgorithms.Direct;
        TokenKey wrongUse = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        wrongUse.Use = TokenKeyUses.Signature;
        TokenKey wrongType = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                wrongCose,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                parameters),
                "conflicting_key_algorithm");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                wrongJose,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                parameters),
                "conflicting_key_algorithm");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                wrongUse,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                parameters),
                "key_use_algorithm_mismatch");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                wrongType,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                parameters),
                "invalid_key_type");
        });
    }

    [Test]
    public void OperationsAreEnforcedForDirectAndDerivation()
    {
        CoseKdfContextParameters directParameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");
        TokenKey decryptOnly = Key(
            Sequence(16, 1),
            CoseAlgorithms.Direct);
        decryptOnly.KeyOperations =
            new string[] { TokenKeyOperations.Decrypt };
        TokenKey encryptOnly = Key(
            Sequence(16, 1),
            CoseAlgorithms.Direct);
        encryptOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationEncrypt };
        CoseKdfContextParameters derivedParameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        TokenKey deriveBits = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        deriveBits.KeyOperations =
            new string[] { TokenKeyOperations.DeriveBits };
        TokenKey deriveKey = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        deriveKey.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationDeriveKey };
        TokenKey denied = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        denied.KeyOperations =
            new string[] { TokenKeyOperations.Encrypt };

        Assert.Multiple(() =>
        {
            Assert.That(CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                decryptOnly,
                CoseAlgorithms.Direct,
                null,
                directParameters).Success,
                Is.True);
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                decryptOnly,
                CoseAlgorithms.Direct,
                null,
                directParameters),
                "key_operation_not_permitted");
            AssertFailure(CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                encryptOnly,
                CoseAlgorithms.Direct,
                null,
                directParameters),
                "key_operation_not_permitted");
            Assert.That(CoseDirectKeyDistributionOperations.Encrypt(
                deriveBits,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                derivedParameters).Success,
                Is.True);
            Assert.That(CoseDirectKeyDistributionOperations.Encrypt(
                deriveKey,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                derivedParameters).Success,
                Is.True);
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                denied,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                derivedParameters),
                "key_operation_not_permitted");
        });
    }

    [Test]
    public void InvalidContextAndAlgorithmsReturnStableFailures()
    {
        TokenKey key = Key(
            Sequence(32, 1),
            CoseAlgorithms.DirectHkdfSha256);
        CoseKdfContextParameters invalidContext = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");
        invalidContext.PartyVInfo = null;
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "A10129");

        Assert.Multiple(() =>
        {
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.DirectHkdfSha256,
                new byte[] { 1 },
                invalidContext),
                "invalid_cose_kdf_context");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                999999,
                new byte[] { 1 },
                parameters),
                "unknown_algorithm");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Aes256Gcm,
                new byte[] { 1 },
                parameters),
                "invalid_algorithm");
            AssertFailure(CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.RsaSha1,
                new byte[] { 1 },
                parameters),
                "unsupported_algorithm");
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        TokenKey key = Key(
            Sequence(16, 1),
            CoseAlgorithms.Direct);
        CoseKdfContextParameters parameters = Context(
            CoseAlgorithms.Aes128Gcm,
            128,
            "");

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseDirectKeyDistributionOperations.Encrypt(
                null,
                CoseAlgorithms.Direct,
                null,
                parameters),
                Throws.ArgumentNullException);
            Assert.That(() => CoseDirectKeyDistributionOperations.Encrypt(
                key,
                CoseAlgorithms.Direct,
                null,
                null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseDirectKeyDistributionOperations.Decrypt(
                (byte[])null,
                key,
                CoseAlgorithms.Direct,
                null,
                parameters),
                Throws.ArgumentNullException);
            Assert.That(() => CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                null,
                CoseAlgorithms.Direct,
                null,
                parameters),
                Throws.ArgumentNullException);
            Assert.That(() => CoseDirectKeyDistributionOperations.Decrypt(
                Array.Empty<byte>(),
                key,
                CoseAlgorithms.Direct,
                null,
                null),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ResultDefaultsToNoKeyOutput()
    {
        CoseDirectKeyDistributionResult result =
            new CoseDirectKeyDistributionResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ContentKey, Is.Null);
            Assert.That(result.EncryptedKey, Is.Null);
        });
    }

    static TokenKey Key(byte[] secret, int algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        key.CoseAlgorithm = algorithm;
        key.HasCoseAlgorithm = true;
        key.Use = TokenKeyUses.Encryption;
        key.SymmetricKey = secret.ToArray();
        return key;
    }

    static CoseKdfContextParameters Context(
        int contentAlgorithm,
        ulong keySizeInBits,
        string protectedHex)
    {
        CoseKdfContextParameters parameters =
            new CoseKdfContextParameters(
                contentAlgorithm,
                keySizeInBits);
        parameters.ProtectedParameters = protectedHex.Length == 0
            ? Array.Empty<byte>()
            : Convert.FromHexString(protectedHex);
        return parameters;
    }

    static string ProtectedFor(int algorithm)
    {
        if (algorithm == CoseAlgorithms.DirectHkdfSha256)
        {
            return "A10129";
        }

        if (algorithm == CoseAlgorithms.DirectHkdfSha512)
        {
            return "A1012A";
        }

        if (algorithm == CoseAlgorithms.DirectHkdfAes128)
        {
            return "A1012B";
        }

        return "A1012C";
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = (byte)(start + index);
            index += 1;
        }

        return bytes;
    }

    static void AssertSuccess(
        CoseDirectKeyDistributionResult result,
        byte[] expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.ContentKey, Is.EqualTo(expected));
        Assert.That(result.EncryptedKey, Is.Not.Null);
        Assert.That(result.EncryptedKey, Is.Empty);
    }

    static void AssertFailure(
        CoseDirectKeyDistributionResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(-1));
        Assert.That(result.ContentKey, Is.Null);
        Assert.That(result.EncryptedKey, Is.Null);
    }
}
