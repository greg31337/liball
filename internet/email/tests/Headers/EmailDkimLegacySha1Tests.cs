using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailDkimLegacySha1Tests
{
    [Test]
    public void RequiresExplicitOptInAndReportsLegacyVerificationWarning()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("legacy body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: archive\r\n");
        headers.Insert(0, CreateSha1Signature(headers, body, rsa));
        List<EmailDkimVerificationKey> keys = [Binding(Imported(rsa))];
        EmailDkimVerificationOptions defaults = VerificationOptions();
        EmailDkimVerificationOptions enabled = VerificationOptions();
        enabled.SetAllowLegacyRsaSha1(true);
        EmailDkimVerificationOptions clone = enabled.Clone();
        enabled.SetAllowLegacyRsaSha1(false);

        EmailDkimSignatureVerificationResult disabledResult = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, defaults).GetSignature(0);
        EmailDkimSignatureVerificationResult enabledResult = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, clone).GetSignature(0);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetAllowLegacyRsaSha1(), Is.False);
            Assert.That(disabledResult.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.UnsupportedAlgorithm));
            Assert.That(disabledResult.GetReason(), Does.Contain("explicitly enabled"));
            Assert.That(disabledResult.HasBodyCanonicalization(), Is.False);
            Assert.That(clone.GetAllowLegacyRsaSha1(), Is.True);
            Assert.That(enabled.GetAllowLegacyRsaSha1(), Is.False);
            Assert.That(enabledResult.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.Pass));
            Assert.That(enabledResult.IsLegacyAlgorithm(), Is.True);
            Assert.That(enabledResult.IsBodyHashValid(), Is.True);
            Assert.That(enabledResult.IsSignatureValid(), Is.True);
            Assert.That(enabledResult.GetPolicyStatus(), Is.EqualTo(EmailDkimVerificationPolicyStatus.AcceptedWithWarnings));
            Assert.That(enabledResult.GetBodyCanonicalization()!.GetHash().GetAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha1));
            Assert.That(enabledResult.GetBodyCanonicalization()!.GetHash().GetDigestByteCount(), Is.EqualTo(20));
            Assert.That(enabledResult.GetReason(), Does.Contain("explicit opt-in policy"));
        });
    }

    [Test]
    public void AppliesSha1BodyIntegrityAndKeyHashRestrictionsAfterOptIn()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("legacy body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: archive\r\n");
        headers.Insert(0, CreateSha1Signature(headers, body, rsa));
        EmailDkimVerificationOptions options = VerificationOptions();
        options.SetAllowLegacyRsaSha1(true);

        EmailDkimSignatureVerificationResult changed = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, Bytes("changed body\r\n"), [Binding(Imported(rsa))], options).GetSignature(0);
        EmailDkimSignatureVerificationResult disallowed = new EmailDkimVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding(DnsKey(rsa, "h=sha256"))], options).GetSignature(0);

        Assert.Multiple(() =>
        {
            Assert.That(changed.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.BodyHashMismatch));
            Assert.That(changed.GetBodyCanonicalization()!.GetHash().GetAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha1));
            Assert.That(disallowed.GetStatus(), Is.EqualTo(EmailDkimVerificationStatus.KeyHashNotAllowed));
            Assert.That(disallowed.IsLegacyAlgorithm(), Is.True);
        });
    }

    [Test]
    public void ExplicitlyProhibitsNewRsaSha1SignaturesWithoutMutation()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        byte[] originalBody = (byte[])body.Clone();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: no sha1\r\n");
        byte[] originalHeaders = JoinHeaderBytes(headers);
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();
        options.SetDomain("example.test");
        options.SetSelector("key");
        EmailDkimSigningAttemptResult support = new EmailDkimSigner().GetRsaSha1SigningSupport();

        EmailDkimSigningAttemptResult result = new EmailDkimSigner().SignRsaSha1(headers, body, rsa, options);

        Assert.Multiple(() =>
        {
            Assert.That(new EmailDkimSigner().IsRsaSha1SigningSupported(), Is.False);
            Assert.That(support.GetStatus(), Is.EqualTo(EmailDkimSigningAttemptStatus.ProhibitedAlgorithm));
            Assert.That(result.GetStatus(), Is.EqualTo(EmailDkimSigningAttemptStatus.ProhibitedAlgorithm));
            Assert.That(result.GetAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.RsaSha1));
            Assert.That(result.IsProhibitedAlgorithm(), Is.True);
            Assert.That(result.IsUnsupportedAlgorithm(), Is.False);
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.HasSigningResult(), Is.False);
            Assert.That(result.GetReason(), Does.Contain("prohibited"));
            Assert.That(result.GetReason(), Does.Contain("historic"));
            Assert.That(JoinHeaderBytes(headers), Is.EqualTo(originalHeaders));
            Assert.That(body, Is.EqualTo(originalBody));
            Assert.That(headers.GetCountNamed("DKIM-Signature"), Is.Zero);
        });
    }

    [Test]
    public void ProhibitedSigningAttemptStillValidatesReferenceArguments()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\n");
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();

        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignRsaSha1(null!, new byte[0], rsa, options));
        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignRsaSha1(headers, new byte[0], null!, options));
        Assert.Throws<ArgumentNullException>(() => new EmailDkimSigner().SignRsaSha1(headers, new byte[0], rsa, null!));
    }

    static EmailHeader CreateSha1Signature(EmailHeaders headers, byte[] body, RSA rsa)
    {
        EmailDkimBodyCanonicalizationOptions bodyOptions = new EmailDkimBodyCanonicalizationOptions();
        bodyOptions.SetKind(EmailDkimCanonicalizationKind.Relaxed);
        bodyOptions.SetHashAlgorithm(EmailHashAlgorithm.Sha1);
        EmailDkimBodyCanonicalizationResult bodyResult = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(body, bodyOptions).GetResult();
        string value = " v=1; a=rsa-sha1; c=relaxed/relaxed; d=example.test; s=key; t=1700000000; h=From:Subject; bh=" + Convert.ToBase64String(bodyResult.GetHash().GetDigestBytes()) + "; b=";
        EmailHeader unsigned = new EmailHeaderOperations().CreateWithNameAndRawValue("DKIM-Signature", Bytes(value));
        EmailDkimSignature parsed = new EmailDkimSignatureParser().ParseWithInputAndMode(unsigned.GetRawValue(), EmailStructuredValueParseMode.Strict).GetValue()!;
        byte[] hash = SHA1.HashData(new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(headers, parsed!, unsigned).GetCanonicalizedHeaders());
        string signature = Convert.ToBase64String(rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
        return new EmailHeaderOperations().CreateWithNameAndRawValue("DKIM-Signature", Bytes(value + signature));
    }

    static EmailDkimVerificationOptions VerificationOptions()
    {
        EmailDkimVerificationOptions options = new EmailDkimVerificationOptions();
        options.SetVerificationTime(1700000100);
        options.SetAllowedClockSkewSeconds(0);
        return options;
    }

    static EmailDkimVerificationKey Binding(EmailDkimKey key)
    {
        return new EmailDkimVerificationKey("example.test", "key", key);
    }

    static EmailDkimKey Imported(RSA rsa)
    {
        return new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(rsa.ExportSubjectPublicKeyInfo()).GetValue()!;
    }

    static EmailDkimKey DnsKey(RSA rsa, string tags)
    {
        string record = "v=DKIM1; p=" + Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()) + "; " + tags;
        return new EmailDkimKeyParser().ParseWithInputAndMode(Bytes(record), EmailStructuredValueParseMode.Strict).GetValue()!;
    }

    static RSA Rsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        return rsa;
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        return new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n")).GetHeaders();
    }

    static byte[] JoinHeaderBytes(EmailHeaders headers)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < headers.GetCount())
        {
            bytes.AddRange(headers.GetAt(index).GetRawFieldBytes());
            index++;
        }

        return bytes.ToArray();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
