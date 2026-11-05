using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailOpenPgpAlgorithmTests
{
    [Test]
    public void ReportsImplementedDeprecatedAndUnsupportedCapabilitiesExplicitly()
    {
        EmailOpenPgpAlgorithmSupport sha256 = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Hash, 8);
        EmailOpenPgpAlgorithmSupport rsa = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.PublicKey, 1);
        EmailOpenPgpAlgorithmSupport ed25519 = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.PublicKey, 27);
        EmailOpenPgpAlgorithmSupport bzip2 = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Compression, 3);
        EmailOpenPgpAlgorithmSupport ocb = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Aead, 2);
        EmailOpenPgpAlgorithmSupport literalPacket = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Packet, 11);
        EmailOpenPgpAlgorithmSupport encryptedPacket = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.Packet, 9);

        Assert.Multiple(() =>
        {
            Assert.That(sha256.GetCategory(), Is.EqualTo(EmailOpenPgpAlgorithmCategory.Hash));
            Assert.That(sha256.GetIdentifier(), Is.EqualTo(8));
            Assert.That(sha256.GetName(), Is.EqualTo("SHA2-256"));
            Assert.That(sha256.IsSupported(), Is.True);
            Assert.That(sha256.IsDeprecated(), Is.False);
            Assert.That(rsa.IsSupported(), Is.True);
            Assert.That(rsa.IsDeprecated(), Is.True);
            Assert.That(ed25519.IsSupported(), Is.False);
            Assert.That(ed25519.GetReason(), Is.Not.Empty);
            Assert.That(bzip2.IsSupported(), Is.False);
            Assert.That(ocb.IsSupported(), Is.False);
            Assert.That(literalPacket.IsSupported(), Is.True);
            Assert.That(encryptedPacket.IsSupported(), Is.False);
        });
    }

    [Test]
    public void DoesNotAdvertiseUnimplementedAlgorithmOrPacketIdentifiers()
    {
        HashSet<int> hashes = [2, 8, 9, 10];
        if (SHA3_256.IsSupported) hashes.Add(12);
        if (SHA3_512.IsSupported) hashes.Add(14);

        AssertSupportSet(EmailOpenPgpAlgorithmCategory.Packet, [1, 2, 3, 4, 5, 6, 7, 8, 11, 14, 18, 21]);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.PublicKey, [1, 2, 3, 19]);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.Symmetric, [7, 8, 9]);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.Compression, [0, 1, 2]);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.Hash, hashes);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.Aead, AesGcm.IsSupported ? [3] : []);
        AssertSupportSet(EmailOpenPgpAlgorithmCategory.StringToKey, [0, 1, 3]);
    }

    [Test]
    public void ComputesModernHashesAndRequiresOptInForSha1Verification()
    {
        byte[] input = Bytes("abc");
        EmailOpenPgpOperationResult sha256 = new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithm(input, 8);
        EmailOpenPgpOperationResult sha3 = new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithm(input, 12);
        EmailOpenPgpOperationResult sha1Rejected = new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithm(input, 2);
        EmailOpenPgpAlgorithmOptions legacy = new EmailOpenPgpAlgorithmOptions();
        legacy.SetAllowSha1Verification(true);
        EmailOpenPgpOperationResult sha1 = new EmailOpenPgpAlgorithms().ComputeHashForVerification(input, 2, legacy);
        EmailOpenPgpOperationResult md5 = new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithm(input, 1);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(sha256.GetOutput()), Is.EqualTo("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"));
            if (SHA3_256.IsSupported)
                Assert.That(Convert.ToHexString(sha3.GetOutput()), Is.EqualTo("3A985DA74FE225B2045C172D6BD390BD855F086E3E9D525B46BFE24511431532"));
            else
                Assert.That(sha3.IsUnsupported(), Is.True);
            Assert.That(sha1Rejected.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.PolicyRejected));
            Assert.That(Convert.ToHexString(sha1.GetOutput()), Is.EqualTo("A9993E364706816ABA3E25717850C26C9CD0D89D"));
            Assert.That(md5.IsUnsupported(), Is.True);
            Assert.That(md5.GetPreservedInput(), Is.EqualTo(input));
            Assert.That(md5.GetIssues().ContainsCode(EmailIssueCode.OpenPgpAlgorithmUnsupported), Is.True);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void RoundTripsImplementedCompressionAlgorithms(int algorithm)
    {
        byte[] content = Bytes(string.Concat(Enumerable.Repeat("OpenPGP compression data\r\n", 100)));

        EmailOpenPgpOperationResult compressed = new EmailOpenPgpAlgorithms().CompressWithDataAndCompressionAlgorithm(content, algorithm);
        EmailOpenPgpOperationResult compressedWithOptions = new EmailOpenPgpAlgorithms().CompressWithDataAndCompressionAlgorithmAndOptions(content, algorithm, new EmailOpenPgpAlgorithmOptions());
        EmailOpenPgpOperationResult decompressed = new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithm(compressed.GetOutput(), algorithm)!;

        Assert.Multiple(() =>
        {
            Assert.That(compressed.IsSuccess(), Is.True);
            Assert.That(compressedWithOptions.GetOutput(), Is.EqualTo(compressed.GetOutput()));
            Assert.That(decompressed!.IsSuccess(), Is.True);
            Assert.That(decompressed.GetOutput(), Is.EqualTo(content));
        });
    }

    [Test]
    public void PreservesUnsupportedOrInvalidCompressedInputWithoutOutput()
    {
        byte[] input = [1, 2, 3, 4];

        EmailOpenPgpOperationResult unsupported = new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithm(input, 3)!;
        EmailOpenPgpOperationResult invalid = new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithm(input, 2)!;

        Assert.Multiple(() =>
        {
            Assert.That(unsupported!.IsUnsupported(), Is.True);
            Assert.That(unsupported.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.UnsupportedAlgorithm));
            Assert.That(unsupported.HasOutput(), Is.False);
            Assert.That(unsupported.GetPreservedInput(), Is.EqualTo(input));
            Assert.That(invalid!.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.InvalidInput));
            Assert.That(invalid.HasOutput(), Is.False);
            Assert.That(invalid.GetPreservedInput(), Is.EqualTo(input));
        });
    }

    [Test]
    public void DerivesBoundedIteratedAndSaltedStringToKeyData()
    {
        byte[] specifier = [3, 8, 1, 2, 3, 4, 5, 6, 7, 8, 96];

        EmailOpenPgpOperationResult derived = new EmailOpenPgpAlgorithms().DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytes(Bytes("password"), specifier, 48)!;
        EmailOpenPgpOperationResult simpleRejected = new EmailOpenPgpAlgorithms().DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytes(Bytes("password"), [0, 8], 32)!;
        EmailOpenPgpAlgorithmOptions legacy = new EmailOpenPgpAlgorithmOptions();
        legacy.SetAllowDeprecatedStringToKey(true);
        EmailOpenPgpOperationResult simple = new EmailOpenPgpAlgorithms().DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytesAndOptions(Bytes("password"), [0, 8], 32, legacy)!;
        EmailOpenPgpOperationResult argon2 = new EmailOpenPgpAlgorithms().DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytes(Bytes("password"), new byte[20] { 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 3 }, 32)!;

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(derived!.GetOutput()), Is.EqualTo("2A07AB96719B61E9E5F02C563FF8F8AD9B1E85D832B51A709904DDF536220F49BED7B9DD08CB018BC2F208802CCB3474"));
            Assert.That(simpleRejected!.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.PolicyRejected));
            Assert.That(Convert.ToHexString(simple!.GetOutput()), Is.EqualTo("5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8"));
            Assert.That(argon2!.IsUnsupported(), Is.True);
            Assert.That(argon2.GetPreservedInput()[0], Is.EqualTo(4));
        });
    }

    [Test]
    public void SignsAndVerifiesRsaAndEcdsaMpiSignatures()
    {
        byte[] hash = SHA256.HashData(Bytes("signed OpenPGP data"));
        EmailOpenPgpAlgorithmOptions legacyRsa = new EmailOpenPgpAlgorithmOptions();
        legacyRsa.SetAllowDeprecatedRsaGeneration(true);
        using RSA rsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        EmailOpenPgpOperationResult rsaRejected = new EmailOpenPgpAlgorithms().SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, 8, 1, rsa)!;
        EmailOpenPgpOperationResult rsaSignature = new EmailOpenPgpAlgorithms().SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, 8, 1, rsa, legacyRsa)!;
        EmailOpenPgpOperationResult rsaVerified = new EmailOpenPgpAlgorithms().VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, rsaSignature!.GetOutput(), 8, 1, rsa)!;
        byte[] differentHash = SHA256.HashData(Bytes("different"));
        EmailOpenPgpOperationResult rsaMismatch = new EmailOpenPgpAlgorithms().VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(differentHash, rsaSignature.GetOutput(), 8, 1, rsa)!;
        EmailOpenPgpOperationResult ecdsaSignature = new EmailOpenPgpAlgorithms().SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, 8, 19, ecdsa)!;
        EmailOpenPgpOperationResult ecdsaVerified = new EmailOpenPgpAlgorithms().VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, ecdsaSignature!.GetOutput(), 8, 19, ecdsa)!;
        EmailOpenPgpOperationResult unsupported = new EmailOpenPgpAlgorithms().SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKey(hash, 8, 27, ecdsa)!;
        EmailOpenPgpOperationResult providerFailure = new EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus.Failed, new byte[0], hash, false, new EmailIssueList());

        Assert.Multiple(() =>
        {
            Assert.That(rsaRejected!.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.PolicyRejected));
            Assert.That(rsaSignature.IsSuccess(), Is.True);
            Assert.That(rsaVerified!.IsVerified(), Is.True);
            Assert.That(rsaMismatch!.IsSuccess(), Is.True);
            Assert.That(rsaMismatch.IsVerified(), Is.False);
            Assert.That(ecdsaSignature.IsSuccess(), Is.True);
            Assert.That(ecdsaVerified!.IsVerified(), Is.True);
            Assert.That(unsupported!.IsUnsupported(), Is.True);
            Assert.That(providerFailure.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.Failed));
        });
    }

    [Test]
    public void EncryptsAndDecryptsLegacyRsaSessionKeyOnlyWithOptIn()
    {
        using RSA rsa = RSA.Create(2048);
        byte[] sessionKey = Enumerable.Range(0, 35).Select(value => (byte)value).ToArray();
        EmailOpenPgpAlgorithmOptions legacy = new EmailOpenPgpAlgorithmOptions();
        legacy.SetAllowDeprecatedRsaGeneration(true);

        EmailOpenPgpOperationResult rejected = new EmailOpenPgpAlgorithms().EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKey(sessionKey, 1, rsa)!;
        EmailOpenPgpOperationResult encrypted = new EmailOpenPgpAlgorithms().EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(sessionKey, 1, rsa, legacy)!;
        EmailOpenPgpOperationResult decrypted = new EmailOpenPgpAlgorithms().DecryptSessionKeyWithEncryptedSessionKeyAndPublicKeyAlgorithmAndKey(encrypted!.GetOutput(), 1, rsa);
        EmailOpenPgpOperationResult x25519 = new EmailOpenPgpAlgorithms().EncryptSessionKeyWithSessionKeyDataAndPublicKeyAlgorithmAndKeyAndOptions(sessionKey, 25, rsa, legacy)!;

        Assert.Multiple(() =>
        {
            Assert.That(rejected!.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.PolicyRejected));
            Assert.That(encrypted.IsSuccess(), Is.True);
            Assert.That(decrypted.IsSuccess(), Is.True);
            Assert.That(decrypted.GetOutput(), Is.EqualTo(sessionKey));
            Assert.That(x25519!.IsUnsupported(), Is.True);
        });
    }

    [Test]
    public void AuthenticatesAesGcmAndWithholdsTamperedPlaintext()
    {
        byte[] plaintext = Bytes("authenticated OpenPGP content");
        byte[] key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        byte[] nonce = Enumerable.Range(1, 12).Select(value => (byte)value).ToArray();
        byte[] associatedData = [0xD4, 2, 9, 3, 0];

        EmailOpenPgpOperationResult encrypted = new EmailOpenPgpAlgorithms().EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(plaintext, key, 9, 3, nonce, associatedData);
        EmailOpenPgpOperationResult decrypted = new EmailOpenPgpAlgorithms().DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(encrypted.GetOutput(), key, 9, 3, nonce, associatedData);
        byte[] tampered = encrypted.GetOutput();
        tampered[tampered.Length - 1] ^= 1;
        EmailOpenPgpOperationResult failed = new EmailOpenPgpAlgorithms().DecryptAeadWithEncryptedAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(tampered, key, 9, 3, nonce, associatedData);
        EmailOpenPgpOperationResult ocb = new EmailOpenPgpAlgorithms().EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(plaintext, key, 9, 2, new byte[15], associatedData);
        EmailOpenPgpOperationResult twofish = new EmailOpenPgpAlgorithms().EncryptAeadWithPlaintextAndKeyAndSymmetricAlgorithmAndAeadAlgorithmAndNonceAndAssociatedData(plaintext, key, 10, 3, nonce, associatedData);

        Assert.Multiple(() =>
        {
            Assert.That(encrypted.IsSuccess(), Is.True);
            Assert.That(decrypted.GetOutput(), Is.EqualTo(plaintext));
            Assert.That(failed.GetStatus(), Is.EqualTo(EmailOpenPgpOperationStatus.AuthenticationFailed));
            Assert.That(failed.HasOutput(), Is.False);
            Assert.That(failed.GetIssues().ContainsCode(EmailIssueCode.OpenPgpAuthenticationFailed), Is.True);
            Assert.That(ocb.IsUnsupported(), Is.True);
            Assert.That(twofish.IsUnsupported(), Is.True);
        });
    }

    [Test]
    public void EnforcesAlgorithmInputExpansionAndStringToKeyLimits()
    {
        EmailCryptographicLimits inputLimits = new EmailCryptographicLimits();
        inputLimits.SetMaxInputBytes(3);
        EmailOpenPgpAlgorithmOptions inputOptions = new EmailOpenPgpAlgorithmOptions();
        inputOptions.SetLimits(inputLimits);

        byte[] content = new byte[10000];
        EmailOpenPgpOperationResult compressed = new EmailOpenPgpAlgorithms().CompressWithDataAndCompressionAlgorithm(content, 2);
        EmailCryptographicLimits outputLimits = new EmailCryptographicLimits();
        outputLimits.SetMaxDecompressedBytes(100);
        EmailOpenPgpAlgorithmOptions outputOptions = new EmailOpenPgpAlgorithmOptions();
        outputOptions.SetLimits(outputLimits);
        EmailOpenPgpAlgorithmOptions s2kOptions = new EmailOpenPgpAlgorithmOptions();
        s2kOptions.SetMaximumStringToKeyBytes(1000);

        EmailLimitException input = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithmAndOptions(Bytes("four"), 8, inputOptions));
        EmailLimitException output = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithmAndOptions(compressed.GetOutput(), 2, outputOptions));
        EmailLimitException s2k = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpAlgorithms().DeriveStringToKeyWithPassphraseAndSpecifierAndKeyBytesAndOptions(Bytes("password"), [3, 8, 1, 2, 3, 4, 5, 6, 7, 8, 96], 32, s2kOptions));

        Assert.Multiple(() =>
        {
            Assert.That(input.GetLimitName(), Is.EqualTo("MaxOpenPgpAlgorithmInputBytes"));
            Assert.That(output.GetLimitName(), Is.EqualTo("MaxDecompressedBytes"));
            Assert.That(s2k.GetLimitName(), Is.EqualTo("MaxOpenPgpStringToKeyBytes"));
        });
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }

    static void AssertSupportSet(EmailOpenPgpAlgorithmCategory category, IEnumerable<int> expectedIdentifiers)
    {
        HashSet<int> expected = new HashSet<int>(expectedIdentifiers);
        for (int identifier = 0; identifier <= 255; identifier++)
            Assert.That(new EmailOpenPgpAlgorithms().GetSupport(category, identifier).IsSupported(), Is.EqualTo(expected.Contains(identifier)), category + " identifier " + identifier);
    }
}
