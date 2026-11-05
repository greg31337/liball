using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class CoseEncrypt0OperationsTests
{
    [Test]
    public void DetachedCiphertextAndExternalAuthenticatedDataAreBound()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        byte[] plaintext = Encoding.UTF8.GetBytes("detached ciphertext");
        byte[] externalData = Encoding.UTF8.GetBytes("encrypt0 aad");
        TokenKey key = Symmetric(Sequence(16, 2), algorithm);
        TokenPolicy policy = Policy("A128GCM");

        CoseDetachedEncryptionResult created =
            CoseEncrypt0Operations.CreateDetached(
                plaintext,
                externalData,
                Header(algorithm),
                key,
                true,
                policy);
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(created.Bytes);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.DecryptDetached(
            created.Bytes,
            created.Ciphertext,
            externalData,
            key,
            policy);
        byte[] changedCiphertext = created.Ciphertext.ToArray();
        changedCiphertext[^1] ^= 1;
        CoseEncrypt0Result wrongCiphertext =
            CoseEncrypt0Operations.DecryptDetached(
                created.Bytes,
                changedCiphertext,
                externalData,
                key,
                policy);
        CoseEncrypt0Result wrongData = CoseEncrypt0Operations.DecryptDetached(
            created.Bytes,
            created.Ciphertext,
            new byte[] { 8 },
            key,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Bytes, Is.Not.Null);
            Assert.That(created.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            Assert.That(parsed.Message.DetachedCiphertext, Is.True);
            Assert.That(parsed.Message.Ciphertext, Is.Null);
            AssertVerified(decrypted, plaintext);
            Assert.That(decrypted.Message.Ciphertext,
                Is.EqualTo(created.Ciphertext));
            AssertFailure(wrongCiphertext, "decryption_failed");
            AssertFailure(wrongData, "decryption_failed");
        });
    }

    [TestCase(CoseAlgorithms.Aes128Gcm, 16)]
    [TestCase(CoseAlgorithms.Aes192Gcm, 24)]
    [TestCase(CoseAlgorithms.Aes256Gcm, 32)]
    public void AesGcmAlgorithmsRoundTrip(int algorithm, int keySize)
    {
        byte[] plaintext = Sequence(257, 9);
        TokenKey key = Symmetric(Sequence(keySize, 1), algorithm);
        TokenPolicy policy = Policy(CoseAlgorithmsOperations.GetName(algorithm));

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            plaintext,
            Header(algorithm),
            key,
            policy);
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(created.Bytes);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            created.Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Message.Tagged, Is.True);
            Assert.That(parsed.Message.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            Assert.That(parsed.Message.Header.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(parsed.Message.PlaintextAvailable, Is.False);
            Assert.That(parsed.Message.AuthenticationTagVerified,
                Is.False);
            AssertVerified(decrypted, plaintext);
        });
    }

    [Test]
    public void ExactEncrypt0AuthenticatedDataInteroperatesBothWays()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "the context string is cryptographically significant");
        byte[] nonce = Sequence(12, 40);
        TokenKey key = Symmetric(Sequence(16, 2), algorithm);
        TokenPolicy policy = Policy("A128GCM");
        CoseHeader createdHeader = Header(algorithm);
        createdHeader.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(nonce));

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            plaintext,
            createdHeader,
            key,
            false,
            policy);
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(created.Bytes);
        TokenBytesResult expectedData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            parsed.Message.Header.ProtectedBytes);
        TokenBytesResult wrongData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt,
            parsed.Message.Header.ProtectedBytes);
        byte[] ciphertext = parsed.Message.Ciphertext[..^16];
        byte[] tag = parsed.Message.Ciphertext[^16..];
        TokenBytesResult independentlyDecrypted = CoseAesGcm.Decrypt(
            ciphertext,
            nonce,
            tag,
            expectedData.Bytes,
            key,
            algorithm);
        TokenBytesResult wrongContext = CoseAesGcm.Decrypt(
            ciphertext,
            nonce,
            tag,
            wrongData.Bytes,
            key,
            algorithm);

        CoseHeader independentHeader = Header(algorithm);
        independentHeader.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(nonce));
        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            independentHeader,
            policy);
        TokenBytesResult independentData = CoseEncStructure.Create(
            CoseEncStructureContext.Encrypt0,
            protectedBytes.Bytes);
        CoseAesGcmResult encrypted = CoseAesGcm.Encrypt(
            plaintext,
            nonce,
            independentData.Bytes,
            key,
            algorithm);
        CoseEncrypt0 independentMessage = new CoseEncrypt0();
        independentMessage.Header = independentHeader;
        independentMessage.Ciphertext = Join(
            encrypted.Ciphertext,
            encrypted.AuthenticationTag);
        byte[] independentCbor = independentMessage.WriteWithTag(false).Bytes;
        CoseEncrypt0Result libraryDecrypted =
            CoseEncrypt0Operations.Decrypt(
                independentCbor,
                key,
                policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(created.Bytes[0], Is.EqualTo(0x83));
            Assert.That(createdHeader.InitializationVector,
                Is.EqualTo(nonce));
            Assert.That(independentlyDecrypted.Success, Is.True);
            Assert.That(independentlyDecrypted.Bytes,
                Is.EqualTo(plaintext));
            Assert.That(wrongContext.Success, Is.False);
            Assert.That(wrongContext.Error.Code,
                Is.EqualTo("decryption_failed"));
            AssertVerified(libraryDecrypted, plaintext);
        });
    }

    [Test]
    public void GeneratedNoncesAreFreshAndExplicitTagIsHonored()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenKey key = Symmetric(Sequence(16, 4), algorithm);
        TokenPolicy policy = Policy("A128GCM");

        TokenBytesResult first = CoseEncrypt0Operations.Create(
            System.Array.Empty<byte>(),
            Header(algorithm),
            key,
            false,
            policy);
        TokenBytesResult second = CoseEncrypt0Operations.Create(
            System.Array.Empty<byte>(),
            Header(algorithm),
            key,
            policy);
        CoseEncrypt0Result firstParsed = CoseEncrypt0.Read(first.Bytes);
        CoseEncrypt0Result secondParsed = CoseEncrypt0.Read(second.Bytes);

        Assert.Multiple(() =>
        {
            AssertSuccess(first);
            AssertSuccess(second);
            Assert.That(firstParsed.Message.Tagged, Is.False);
            Assert.That(secondParsed.Message.Tagged, Is.True);
            Assert.That(firstParsed.Message.Ciphertext, Has.Length.EqualTo(16));
            Assert.That(firstParsed.Message.Header.InitializationVector,
                Is.Not.EqualTo(secondParsed.Message.Header.InitializationVector));
            AssertVerified(CoseEncrypt0Operations.Decrypt(
                first.Bytes,
                key,
                policy), System.Array.Empty<byte>());
        });
    }

    [Test]
    public void CiphertextProtectedHeaderAndNonceTamperingFailClosed()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenKey key = Symmetric(Sequence(16, 7), algorithm);
        TokenPolicy policy = Policy("A128GCM");
        TokenBytesResult created = CoseEncrypt0Operations.Create(
            Sequence(32, 1),
            Header(algorithm),
            key,
            policy);

        CoseEncrypt0 ciphertext = CoseEncrypt0.Read(created.Bytes).Message;
        ciphertext.Ciphertext[^1] ^= 1;
        CoseEncrypt0 protectedHeader =
            CoseEncrypt0.Read(created.Bytes).Message;
        protectedHeader.Header.SetProtected(
            CoseHeaderLabels.ContentType,
            Text("application/cbor"));
        CoseEncrypt0 nonce = CoseEncrypt0.Read(created.Bytes).Message;
        byte[] changedNonce = nonce.Header.InitializationVector.ToArray();
        changedNonce[0] ^= 1;
        nonce.Header.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(changedNonce));

        CoseEncrypt0Result ciphertextResult =
            CoseEncrypt0Operations.Decrypt(
                ciphertext.Write().Bytes,
                key,
                policy);
        CoseEncrypt0Result protectedResult =
            CoseEncrypt0Operations.Decrypt(
                protectedHeader.Write().Bytes,
                key,
                policy);
        CoseEncrypt0Result nonceResult = CoseEncrypt0Operations.Decrypt(
            nonce.Write().Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(ciphertextResult, "decryption_failed");
            AssertFailure(protectedResult, "decryption_failed");
            AssertFailure(nonceResult, "decryption_failed");
        });
    }

    [Test]
    public void HeaderAlgorithmNonceAndPolicyRulesAreEnforced()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenKey key = Symmetric(Sequence(16, 10), algorithm);
        TokenPolicy policy = Policy("A128GCM");
        CoseHeader missing = new CoseHeader();
        CoseHeader unprotected = new CoseHeader();
        unprotected.SetUnprotected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        CoseHeader nonInteger = new CoseHeader();
        nonInteger.SetProtected(
            CoseHeaderLabels.Algorithm,
            Text("A128GCM"));
        CoseHeader invalid = Header(CoseAlgorithms.HmacSha256);
        CoseHeader conflicting = Header(algorithm);
        conflicting.SetUnprotected(
            CoseHeaderLabels.InitializationVector,
            Bytes(Sequence(12, 1)));
        conflicting.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(new byte[] { 1 }));
        CoseHeader partial = Header(algorithm);
        partial.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(new byte[] { 1 }));
        CoseHeader critical = Header(algorithm);
        critical.SetProtectedByTextLabel("private", Int(1));
        critical.SetProtected(
            CoseHeaderLabels.Critical,
            Array(Text("private")));
        TokenPolicy invalidPolicy = Policy("A128GCM");
        invalidPolicy.AllowedAlgorithms = null;

        Assert.Multiple(() =>
        {
            AssertCreateFailure(missing, key, policy,
                "missing_cose_algorithm");
            AssertCreateFailure(unprotected, key, policy,
                "cose_algorithm_not_protected");
            AssertCreateFailure(nonInteger, key, policy,
                "invalid_cose_algorithm");
            AssertCreateFailure(invalid, key,
                Policy(CoseAlgorithmsOperations.GetName(CoseAlgorithms.HmacSha256)),
                "invalid_algorithm");
            AssertCreateFailure(Header(algorithm), key,
                Policy("A256GCM"),
                "algorithm_not_allowed");
            AssertCreateFailure(conflicting, key, policy,
                "conflicting_initialization_vectors");
            AssertCreateFailure(partial, key, policy,
                "missing_base_initialization_vector");
            AssertCreateFailure(critical, key, policy,
                "unknown_cose_critical_header");
            AssertCreateFailure(Header(algorithm), key, invalidPolicy,
                "invalid_policy");
        });
    }

    [Test]
    public void PartialInitializationVectorRequiresCompatibleBaseVector()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenPolicy policy = Policy("A128GCM");
        CoseHeader header = Header(algorithm);
        header.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(new byte[] { 1 }));
        TokenKey shortBase = Symmetric(Sequence(16, 10), algorithm);
        shortBase.BaseInitializationVector = new byte[11];
        TokenKey longPartial = Symmetric(Sequence(16, 10), algorithm);
        longPartial.BaseInitializationVector = new byte[12];
        CoseHeader longHeader = Header(algorithm);
        longHeader.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(new byte[13]));

        Assert.Multiple(() =>
        {
            AssertCreateFailure(
                header,
                shortBase,
                policy,
                "invalid_base_initialization_vector");
            AssertCreateFailure(
                longHeader,
                longPartial,
                policy,
                "invalid_partial_initialization_vector");
        });
    }

    [Test]
    public void PartialInitializationVectorRoundTripsWithoutSendingFullVector()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenPolicy policy = Policy("A128GCM");
        TokenKey key = Symmetric(Sequence(16, 10), algorithm);
        key.BaseInitializationVector = Convert.FromHexString(
            "000102030405060708090A0B");
        CoseHeader header = Header(algorithm);
        header.SetUnprotected(
            CoseHeaderLabels.PartialInitializationVector,
            Bytes(Convert.FromHexString("010203")));
        byte[] plaintext = Encoding.UTF8.GetBytes("partial IV");

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            plaintext,
            header,
            key,
            policy);
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(created.Bytes);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            created.Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.Header.InitializationVector, Is.Null);
            Assert.That(parsed.Message.Header.PartialInitializationVector,
                Is.EqualTo(Convert.FromHexString("010203")));
            AssertVerified(decrypted, plaintext);
        });
    }

    [Test]
    public void WrongKeysAndKeyMetadataRestrictionsAreEnforced()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenPolicy policy = Policy("A128GCM");
        TokenKey key = Symmetric(Sequence(16, 12), algorithm);
        TokenBytesResult created = CoseEncrypt0Operations.Create(
            Sequence(20, 3),
            Header(algorithm),
            key,
            policy);
        TokenKey wrong = Symmetric(Sequence(16, 80), algorithm);
        TokenKey decryptOnly = Symmetric(Sequence(16, 12), algorithm);
        decryptOnly.KeyOperations =
            new string[] { TokenKeyOperations.Decrypt };
        TokenKey encryptOnly = Symmetric(Sequence(16, 12), algorithm);
        encryptOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationEncrypt };
        TokenKey signatureUse = Symmetric(Sequence(16, 12), algorithm);
        signatureUse.Use = TokenKeyUses.Signature;
        TokenKey conflicting = Symmetric(
            Sequence(16, 12),
            CoseAlgorithms.Aes128KeyWrap);

        TokenBytesResult deniedCreate = CoseEncrypt0Operations.Create(
            Sequence(20, 3),
            Header(algorithm),
            decryptOnly,
            policy);
        CoseEncrypt0Result wrongResult = CoseEncrypt0Operations.Decrypt(
            created.Bytes,
            wrong,
            policy);
        CoseEncrypt0Result deniedDecrypt =
            CoseEncrypt0Operations.Decrypt(
                created.Bytes,
                encryptOnly,
                policy);
        TokenBytesResult deniedUse = CoseEncrypt0Operations.Create(
            Sequence(20, 3),
            Header(algorithm),
            signatureUse,
            policy);
        TokenBytesResult deniedAlgorithm = CoseEncrypt0Operations.Create(
            Sequence(20, 3),
            Header(algorithm),
            conflicting,
            policy);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(deniedCreate,
                "key_operation_not_permitted");
            AssertFailure(wrongResult, "decryption_failed");
            AssertFailure(deniedDecrypt,
                "key_operation_not_permitted");
            AssertBytesFailure(deniedUse,
                "key_use_algorithm_mismatch");
            AssertBytesFailure(deniedAlgorithm,
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void DetachedAndShortCiphertextAreRejected()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenKey key = Symmetric(Sequence(16, 20), algorithm);
        TokenPolicy policy = Policy("A128GCM");
        TokenBytesResult created = CoseEncrypt0Operations.Create(
            new byte[] { 1 },
            Header(algorithm),
            key,
            policy);
        CoseEncrypt0 detached = CoseEncrypt0.Read(created.Bytes).Message;
        detached.DetachedCiphertext = true;
        detached.Ciphertext = null;
        CoseEncrypt0 shortMessage =
            CoseEncrypt0.Read(created.Bytes).Message;
        shortMessage.Ciphertext = new byte[15];

        CoseEncrypt0Result detachedResult =
            CoseEncrypt0Operations.Decrypt(
                detached.Write().Bytes,
                key,
                policy);
        CoseEncrypt0Result shortResult = CoseEncrypt0Operations.Decrypt(
            shortMessage.Write().Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(detachedResult, "missing_ciphertext");
            AssertFailure(shortResult, "decryption_failed");
        });
    }

    [Test]
    public void AesCcmRoundTripsWhenPlatformSupportIsAvailable()
    {
        if (!AesCcm.IsSupported)
        {
            Assert.Ignore("AES-CCM is unavailable on this platform.");
        }

        int algorithm = CoseAlgorithms.AesCcm16_128_128;
        TokenKey key = Symmetric(Sequence(16, 30), algorithm);
        TokenPolicy policy = Policy(CoseAlgorithmsOperations.GetName(algorithm));
        byte[] plaintext = Sequence(200, 5);

        TokenBytesResult created = CoseEncrypt0Operations.Create(
            plaintext,
            Header(algorithm),
            key,
            policy);
        CoseEncrypt0Result parsed = CoseEncrypt0.Read(created.Bytes);
        CoseEncrypt0Result decrypted = CoseEncrypt0Operations.Decrypt(
            created.Bytes,
            key,
            policy);

        Assert.Multiple(() =>
        {
            AssertSuccess(created);
            Assert.That(parsed.Message.Header.InitializationVector,
                Has.Length.EqualTo(13));
            Assert.That(parsed.Message.Ciphertext,
                Has.Length.EqualTo(plaintext.Length + 16));
            AssertVerified(decrypted, plaintext);
        });
    }

    [Test]
    public void SizeLimitsAndNullArgumentsAreEnforced()
    {
        int algorithm = CoseAlgorithms.Aes128Gcm;
        TokenKey key = Symmetric(Sequence(16, 60), algorithm);
        TokenPolicy policy = Policy("A128GCM");
        TokenBytesResult created = CoseEncrypt0Operations.Create(
            Sequence(30, 1),
            Header(algorithm),
            key,
            policy);
        TokenPolicy small = Policy("A128GCM");
        small.MaximumTokenSizeInBytes = created.Bytes.Length - 1;

        TokenBytesResult smallCreate = CoseEncrypt0Operations.Create(
            Sequence(30, 1),
            Header(algorithm),
            key,
            small);
        CoseEncrypt0Result smallDecrypt =
            CoseEncrypt0Operations.Decrypt(
                created.Bytes,
                key,
                small);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(smallCreate, "buffer_limit_exceeded");
            AssertFailure(smallDecrypt, "cbor_size_exceeded");
            Assert.That(() => CoseEncrypt0Operations.Create(
                null, Header(algorithm), key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Create(
                System.Array.Empty<byte>(), null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Create(
                System.Array.Empty<byte>(), Header(algorithm), null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Create(
                System.Array.Empty<byte>(), Header(algorithm), key, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Decrypt(
                null, key, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Decrypt(
                System.Array.Empty<byte>(), null, policy),
                Throws.ArgumentNullException);
            Assert.That(() => CoseEncrypt0Operations.Decrypt(
                System.Array.Empty<byte>(), key, null),
                Throws.ArgumentNullException);
        });
    }

    static CoseHeader Header(int algorithm)
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(algorithm));
        return header;
    }

    static TokenKey Symmetric(byte[] material, int algorithm)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            material,
            algorithm).Key;
        key.Use = TokenKeyUses.Encryption;
        return key;
    }

    static TokenPolicy Policy(params string[] algorithms)
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowedAlgorithms = algorithms;
        return policy;
    }

    static CborItem Int(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index += 1;
        }

        return result;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void AssertCreateFailure(
        CoseHeader header,
        TokenKey key,
        TokenPolicy policy,
        string code)
    {
        AssertBytesFailure(CoseEncrypt0Operations.Create(
            new byte[] { 1 },
            header,
            key,
            policy), code);
    }

    static void AssertVerified(
        CoseEncrypt0Result result,
        byte[] plaintext)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Message.PlaintextAvailable, Is.True);
        Assert.That(result.Message.AuthenticationTagVerified, Is.True);
        Assert.That(result.Message.Plaintext, Is.EqualTo(plaintext));
    }

    static void AssertSuccess(TokenBytesResult result)
    {
        Assert.That(result.Success, Is.True,
            result.Error == null ? "no error" : result.Error.Code);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Bytes, Is.Not.Null);
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }

    static void AssertFailure(CoseEncrypt0Result result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Message, Is.Null);
    }
}
