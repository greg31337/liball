using email;

namespace email.Tests;

public class EmailSecurityPolicyCompletionTests
{
    [Test]
    public void SmimeSignaturePolicyReportsEveryConfiguredChoice()
    {
        EmailSmimeAlgorithmPolicy policy = new EmailSmimeAlgorithmPolicy();
        policy.SetAllowSha1Verification(true);
        policy.SetAllowSha256(false);
        policy.SetAllowSha384(false);
        policy.SetAllowSha512(true);
        policy.SetMinimumRsaKeyBits(1024);
        policy.SetMaximumRsaKeyBits(4096);

        Assert.That(policy.GetAllowSha1Verification(), Is.True);
        Assert.That(policy.GetAllowSha256(), Is.False);
        Assert.That(policy.GetAllowSha384(), Is.False);
        Assert.That(policy.GetAllowSha512(), Is.True);
        Assert.That(policy.GetMinimumRsaKeyBits(), Is.EqualTo(1024));
        Assert.That(policy.GetMaximumRsaKeyBits(), Is.EqualTo(4096));
        Assert.That(policy.IsDigestAllowed(EmailSmimeDigestAlgorithm.Sha1, false), Is.True);
        Assert.That(policy.IsDigestAllowed(EmailSmimeDigestAlgorithm.Sha1, true), Is.False);
        Assert.That(policy.IsRsaKeySizeAllowed(2048), Is.True);
    }

    [Test]
    public void SmimeEncryptionPolicyReportsEveryConfiguredChoice()
    {
        EmailSmimeEncryptionPolicy policy = new EmailSmimeEncryptionPolicy();
        policy.SetAllowAes128Cbc(false);
        policy.SetAllowAes256Cbc(false);
        policy.SetAllowAes128Gcm(true);
        policy.SetAllowAes256Gcm(true);
        policy.SetAllowRsaOaepSha256(false);
        policy.SetAllowRsaPkcs1Encryption(true);
        policy.SetAllowRsaPkcs1Decryption(false);
        policy.SetMinimumRsaKeyBits(1024);
        policy.SetMaximumRsaKeyBits(4096);

        Assert.That(policy.GetAllowAes128Cbc(), Is.False);
        Assert.That(policy.GetAllowAes256Cbc(), Is.False);
        Assert.That(policy.GetAllowAes128Gcm(), Is.True);
        Assert.That(policy.GetAllowAes256Gcm(), Is.True);
        Assert.That(policy.GetAllowRsaOaepSha256(), Is.False);
        Assert.That(policy.GetAllowRsaPkcs1Encryption(), Is.True);
        Assert.That(policy.GetAllowRsaPkcs1Decryption(), Is.False);
        Assert.That(policy.GetMinimumRsaKeyBits(), Is.EqualTo(1024));
        Assert.That(policy.GetMaximumRsaKeyBits(), Is.EqualTo(4096));
        Assert.That(policy.IsContentAlgorithmAllowed(EmailSmimeContentEncryptionAlgorithm.Aes256Gcm), Is.True);
        Assert.That(policy.IsKeyTransportAllowed(EmailSmimeKeyTransportAlgorithm.RsaPkcs1, true), Is.True);
        Assert.That(policy.IsKeyTransportAllowed(EmailSmimeKeyTransportAlgorithm.RsaPkcs1, false), Is.False);
        Assert.That(policy.IsRsaKeySizeAllowed(2048), Is.True);
    }

    [Test]
    public void SensitiveSecurityResultsBecomeUnreadableAfterDisposal()
    {
        EmailOpenPgpOperationResult operation = new EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus.Success, [1, 2], [3, 4], false, new EmailIssueList());
        EmailOpenPgpDecryptionResult openPgp = new EmailOpenPgpDecryptionResult(EmailOpenPgpDecryptionStatus.Success, [5, 6], [7], true, new EmailIssueList());
        EmailSmimeDecryptionResult smime = new EmailSmimeDecryptionResult(EmailSmimeDecryptionStatus.Success, [8, 9], new EmailIssueList(), EmailSmimeContentEncryptionAlgorithm.Aes256Gcm, EmailSmimeKeyTransportAlgorithm.RsaOaepSha256, null, true);

        operation.Dispose();
        openPgp.Dispose();
        smime.Dispose();

        Assert.That(operation.IsDisposed(), Is.True);
        Assert.That(operation.HasOutput(), Is.False);
        Assert.Throws<ObjectDisposedException>(() => operation.GetOutput());
        Assert.That(openPgp.IsDisposed(), Is.True);
        Assert.That(openPgp.HasContent(), Is.False);
        Assert.Throws<ObjectDisposedException>(() => openPgp.GetContent());
        Assert.That(smime.IsDisposed(), Is.True);
        Assert.That(smime.HasContent(), Is.False);
        Assert.Throws<ObjectDisposedException>(() => smime.GetContent());
    }
}
