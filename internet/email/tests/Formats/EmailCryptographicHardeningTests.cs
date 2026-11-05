using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using email;

namespace email.Tests;

[NonParallelizable]
public class EmailCryptographicHardeningTests
{
    [Test]
    public void SensitiveResultsClearOwnedBuffersOnDispose()
    {
        byte[] source = Bytes("sensitive plaintext");
        EmailOpenPgpOperationResult operation = new EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus.Success, source, Bytes("preserved"), false, new EmailIssueList());
        EmailSmimeDecryptionResult smime = new EmailSmimeDecryptionResult(EmailSmimeDecryptionStatus.Success, source, new EmailIssueList(), EmailSmimeContentEncryptionAlgorithm.Aes256Gcm, EmailSmimeKeyTransportAlgorithm.RsaOaepSha256, null, true);
        EmailOpenPgpDecryptionResult pgp = new EmailOpenPgpDecryptionResult(EmailOpenPgpDecryptionStatus.Success, source, Bytes("recipient"), true, new EmailIssueList());

        operation.Dispose();
        operation.Dispose();
        smime.Dispose();
        pgp.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(source, Is.EqualTo(Bytes("sensitive plaintext")), "Results must not clear caller-owned input arrays.");
            Assert.That(operation.IsDisposed(), Is.True);
            Assert.That(operation.HasOutput(), Is.False);
            Assert.Throws<ObjectDisposedException>(() => operation.GetOutput());
            Assert.That(smime.IsDisposed(), Is.True);
            Assert.That(smime.HasContent(), Is.False);
            Assert.Throws<ObjectDisposedException>(() => smime.GetContent());
            Assert.That(pgp.IsDisposed(), Is.True);
            Assert.That(pgp.HasContent(), Is.False);
            Assert.Throws<ObjectDisposedException>(() => pgp.GetContent());
        });
    }

    [Test]
    public void ProviderExceptionTextIsNotCopiedIntoDiagnostics()
    {
        const string secretMarker = "PRIVATE_PROVIDER_SECRET_42";
        using RSA issuerKey = RSA.Create(2048);
        CertificateRequest request = new CertificateRequest("CN=Diagnostic Test", issuerKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509SignatureGenerator signer = X509SignatureGenerator.CreateForRSA(issuerKey, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.Create(request.SubjectName, signer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), new byte[] { 2 });
        using ThrowingRsa failingKey = new ThrowingRsa(secretMarker);

        EmailSmimeSigningResult result = new EmailSmimeSigner().SignDetachedWithContentAndSigners(Bytes("content"),
        [
            new EmailSmimeSigningCredentialOperations().CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(certificate, failingKey, EmailSmimeDigestAlgorithm.Sha256)
        ]);
        string diagnostics = string.Join("\n", result.GetIssues().GetAll().Select(issue => issue.GetDescription() + " " + (issue.HasRecoveryDescription() ? issue.GetRecoveryDescription() : "")));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.SmimeSigningFailed), Is.True);
            Assert.That(diagnostics, Does.Not.Contain(secretMarker));
            Assert.That(diagnostics, Does.Contain("without exposing provider diagnostics"));
        });
    }

    [Test]
    public void EnforcesSignatureAndCumulativeDecryptionWorkBudgets()
    {
        byte[] keyId = Convert.FromHexString("0123456789ABCDEF");
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        EmailOpenPgpMimeOptions oneOperation = new EmailOpenPgpMimeOptions();
        EmailCryptographicLimits signingLimits = oneOperation.GetLimits();
        signingLimits.SetMaxOperationCount(1);
        oneOperation.SetLimits(signingLimits);
        EmailOpenPgpSigningCredential signingCredential = new EmailOpenPgpSigningCredential(keyId, 19, 8, signingKey);

        EmailLimitException signingLimit = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpMimeSigner().SignWithCanonicalMimeEntityAndCredentialAndOptions(Bytes("Content-Type: text/plain\r\n\r\nbody"), signingCredential, oneOperation));
        EmailOpenPgpSigningResult signed = new EmailOpenPgpMimeSigner().SignWithCanonicalMimeEntityAndCredential(Bytes("Content-Type: text/plain\r\n\r\nbody"), signingCredential);
        EmailLimitException verificationLimit = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpMimeVerifier().VerifyWithRootAndKeysAndOptions(
            ParseEntity(signed.GetMimeEntityBytes()),
            [new EmailOpenPgpVerificationKey(keyId, 19, signingKey)],
            oneOperation));

        using RSA recipientKey = RSA.Create(2048);
        byte[] fingerprint = Enumerable.Range(0, 20).Select(value => (byte)value).ToArray();
        EmailOpenPgpAlgorithmOptions algorithms = new EmailOpenPgpAlgorithmOptions();
        algorithms.SetAllowDeprecatedRsaGeneration(true);
        EmailOpenPgpMimeOptions encryptionOptions = new EmailOpenPgpMimeOptions();
        encryptionOptions.SetAlgorithmOptions(algorithms);
        encryptionOptions.SetAeadChunkSizeOctet(0);
        byte[] canonical = Bytes("Content-Type: text/plain\r\n\r\n" + new string('x', 500) + "\r\n");
        EmailOpenPgpEncryptionResult encrypted = new EmailOpenPgpMimeEncryptor().EncryptWithCanonicalMimeEntityAndRecipientsAndOptions(canonical,
        [
            new EmailOpenPgpEncryptionRecipient(4, fingerprint, recipientKey)
        ], encryptionOptions)!;
        EmailOpenPgpMimeOptions decryptionOptions = encryptionOptions.Clone();
        EmailCryptographicLimits decryptionLimits = decryptionOptions.GetLimits();
        decryptionLimits.SetMaxOperationCount(2);
        decryptionOptions.SetLimits(decryptionLimits);

        EmailLimitException decryptionLimit = Assert.Throws<EmailLimitException>(() => new EmailOpenPgpMimeDecryptor().DecryptInlineWithInputAndCredentialsAndOptions(
            encrypted!.GetOpenPgpMessageBytes(),
            [new EmailOpenPgpDecryptionCredential(4, fingerprint, recipientKey)],
            decryptionOptions));

        Assert.Multiple(() =>
        {
            Assert.That(signingLimit.GetLimitName(), Is.EqualTo("MaxCryptographicOperationCount"));
            Assert.That(signed.IsSuccess(), Is.True);
            Assert.That(verificationLimit.GetLimitName(), Is.EqualTo("MaxCryptographicOperationCount"));
            Assert.That(encrypted!.IsSuccess(), Is.True);
            Assert.That(decryptionLimit.GetLimitName(), Is.EqualTo("MaxCryptographicOperationCount"));
            Assert.That(decryptionLimit.GetObserved(), Is.GreaterThan(2));
        });
    }

    static byte[] Bytes(string value) { return Encoding.ASCII.GetBytes(value); }

    static MimeEntity ParseEntity(byte[] value)
    {
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(value);
        Assert.That(parsed.IsComplete(), Is.True);
        return parsed.GetEntity()!;
    }

    sealed class ThrowingRsa : RSA
    {
        string marker;

        public ThrowingRsa(string marker)
        {
            this.marker = marker;
            KeySizeValue = 2048;
            LegalKeySizesValue = [new KeySizes(2048, 2048, 0)];
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) { throw new CryptographicException(marker); }
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) { throw new CryptographicException(marker); }
        public override RSAParameters ExportParameters(bool includePrivateParameters) { return new RSAParameters(); }
        public override void ImportParameters(RSAParameters parameters) { }
        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw new CryptographicException(marker); }
        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw new CryptographicException(marker); }
    }
}
