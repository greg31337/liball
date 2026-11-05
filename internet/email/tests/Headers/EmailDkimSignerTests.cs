using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailDkimSignerTests
{
    [Test]
    public void ProducesDeterministicRsaSha256SignatureWithRequestedTags()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: Alice <alice@example.test>\r\nSubject:\t Alpha  beta \r\n");
        EmailDkimSigningOptions options = Options();
        options.SetExpiration(1700003600);
        options.SetBodyLengthLimit(5);

        EmailDkimSigningResult first = new EmailDkimSigner().Sign(headers, Bytes("Alpha  beta \r\n\r\n"), rsa, options);
        EmailDkimSigningResult second = new EmailDkimSigner().Sign(headers, Bytes("Alpha  beta \r\n\r\n"), rsa, options);
        EmailDkimSignature signature = first.GetSignature();

        Assert.Multiple(() =>
        {
            Assert.That(first.GetHeader().GetRawFieldBytes(), Is.EqualTo(second.GetHeader().GetRawFieldBytes()));
            Assert.That(first.GetSignatureBytes(), Is.EqualTo(second.GetSignatureBytes()));
            Assert.That(signature.GetAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.RsaSha256));
            Assert.That(signature.GetSigningDomain(), Is.EqualTo("example.test"));
            Assert.That(signature.GetSelector(), Is.EqualTo("test-key"));
            Assert.That(signature.GetHeaderCanonicalizationKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
            Assert.That(signature.GetBodyCanonicalizationKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
            Assert.That(signature.GetTimestamp(), Is.EqualTo(1700000000));
            Assert.That(signature.GetExpiration(), Is.EqualTo(1700003600));
            Assert.That(signature.GetBodyLength(), Is.EqualTo(5));
            Assert.That(first.GetBodyCanonicalization().GetHashedByteCount(), Is.EqualTo(5));
            Assert.That(first.GetBodyCanonicalization().HasBodyLengthLimit(), Is.True);
            Assert.That(first.GetBodyCanonicalization().GetBodyLengthLimit(), Is.EqualTo(5));
            Assert.That(first.GetBodyHashBase64(), Is.EqualTo(signature.GetBodyHash()));
            Assert.That(first.GetSignatureBase64(), Is.EqualTo(signature.GetSignatureData()));
            Assert.That(first.GetTimestamp(), Is.EqualTo(1700000000));
            Assert.That(rsa.VerifyHash(first.GetHeaderHashBytes(), first.GetSignatureBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), Is.True);
            Assert.That(first.GetHeaderCanonicalization().GetSelection(0).GetRequestedName(), Is.EqualTo("From"));
        });

        EmailHeader finalHeader = first.GetHeader();
        EmailDkimHeaderCanonicalizationResult canonical = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(headers, signature, finalHeader);
        Assert.That(SHA256.HashData(canonical.GetCanonicalizedHeaders()), Is.EqualTo(first.GetHeaderHashBytes()));
        foreach (string line in Ascii(finalHeader.GetRawFieldBytes()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.That(line.Length, Is.LessThanOrEqualTo(options.GetPreferredLineLength()), line);
        }
    }

    [Test]
    public void SupportsSimpleCanonicalizationAndFullBodySigning()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject:  Keep\tSpacing \r\n");
        EmailDkimSigningOptions options = Options();
        options.SetHeaderCanonicalizationKind(EmailDkimCanonicalizationKind.Simple);
        options.SetBodyCanonicalizationKind(EmailDkimCanonicalizationKind.Simple);

        EmailDkimSigningResult result = new EmailDkimSigner().Sign(headers, Bytes("Body \t\r\n\r\n"), rsa, options);

        Assert.That(result.GetSignature().HasBodyLength(), Is.False);
        Assert.That(result.GetHeaderCanonicalization().GetKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Simple));
        Assert.That(result.GetBodyCanonicalization().GetKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Simple));
        Assert.That(Ascii(result.GetHeader().GetRawFieldBytes()), Does.Contain("c=simple/simple;"));
        Assert.That(rsa.VerifyHash(result.GetHeaderHashBytes(), result.GetSignatureBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), Is.True);
    }

    [Test]
    public void InsertsNewSignatureBeforeExistingSignatureAndCanSignTheOlderOne()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders(
            "From: sender@example.test\r\n" +
            "DKIM-Signature: v=1; a=rsa-sha256; d=old.test; s=old; h=from; bh=AA==; b=AA==\r\n");
        EmailHeader older = headers.GetAt(1);
        EmailDkimSigningOptions options = Options();
        options.ClearSignedHeaders();
        options.AddSignedHeader("From");
        options.AddSignedHeader("DKIM-Signature");

        EmailDkimSigningResult result = new EmailDkimSigner().SignAndInsert(headers, Bytes("body\r\n"), rsa, options);

        Assert.Multiple(() =>
        {
            Assert.That(headers.GetCount(), Is.EqualTo(3));
            Assert.That(headers.GetAt(0).GetName(), Is.EqualTo("DKIM-Signature"));
            Assert.That(headers.GetAt(2), Is.SameAs(older));
            Assert.That(result.GetHeaderCanonicalization().GetSelectionCount(), Is.EqualTo(2));
            Assert.That(result.GetHeaderCanonicalization().GetSelection(1).GetHeader(), Is.SameAs(older));
            Assert.That(result.GetSignature().GetSignedHeaders(), Is.EqualTo(new[] { "From", "DKIM-Signature" }));
        });
    }

    [Test]
    public void RecordsTheActualHashedLengthWhenRequestedLengthExceedsBody()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\n");
        EmailDkimSigningOptions options = Options();
        options.SetBodyLengthLimit(1000);

        EmailDkimSigningResult result = new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, options);

        Assert.That(result.GetSignature().GetBodyLength(), Is.EqualTo(result.GetBodyCanonicalization().GetHashedByteCount()));
        Assert.That(result.GetSignature().GetBodyLength(), Is.EqualTo(result.GetBodyCanonicalization().GetCanonicalizedByteCount()));
        Assert.That(result.GetBodyCanonicalization().IsBodyLengthLimitSatisfied(), Is.False);
    }

    [Test]
    public void ValidatesRequiredInputsTimeKeyPolicyAndResourceLimits()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: test\r\n");
        EmailDkimSigningOptions missingDomain = new EmailDkimSigningOptions();
        EmailDkimSigningOptions missingFrom = Options();
        missingFrom.ClearSignedHeaders();
        missingFrom.AddSignedHeader("Subject");
        EmailDkimSigningOptions expired = Options();
        expired.SetExpiration(1700000000);
        EmailDkimSigningOptions keyPolicy = Options();
        keyPolicy.SetMinimumRsaKeyBits(2048);
        EmailDkimSigningOptions bodyLimit = Options();
        bodyLimit.SetMaximumBodyInputBytes(1);
        EmailDkimSigningOptions headerLimit = Options();
        headerLimit.SetMaximumCanonicalizedHeaderBytes(1);
        EmailDkimSigningOptions signatureLimit = Options();
        signatureLimit.SetMaximumSignatureHeaderBytes(20);
        EmailDkimSigningOptions countLimit = Options();
        countLimit.SetMaximumSignedHeaderCount(1);

        Assert.Throws<ArgumentException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, missingDomain));
        Assert.Throws<ArgumentException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, missingFrom));
        Assert.Throws<ArgumentException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, expired));
        Assert.Throws<ArgumentException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, keyPolicy));
        EmailLimitException body = Assert.Throws<EmailLimitException>(() => new EmailDkimSigner().Sign(headers, Bytes("xx"), rsa, bodyLimit));
        EmailLimitException header = Assert.Throws<EmailLimitException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, headerLimit));
        EmailLimitException signature = Assert.Throws<EmailLimitException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, signatureLimit));
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailDkimSigner().Sign(headers, Bytes("x"), rsa, countLimit));
        Assert.Throws<ArgumentException>(() => missingDomain.SetDomain("bad_domain.test"));
        Assert.Throws<ArgumentException>(() => missingDomain.SetSelector("-bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => missingDomain.SetTimestamp(-1));

        Assert.Multiple(() =>
        {
            Assert.That(body.GetLimitName(), Is.EqualTo("MaxDkimBodyInputBytes"));
            Assert.That(header.GetLimitName(), Is.EqualTo("MaxDkimHeaderInputBytes"));
            Assert.That(signature.GetLimitName(), Is.EqualTo("MaxDkimSignatureHeaderBytes"));
            Assert.That(count.GetLimitName(), Is.EqualTo("MaxDkimSelectedHeaderCount"));
        });
    }

    static EmailDkimSigningOptions Options()
    {
        EmailDkimSigningOptions options = new EmailDkimSigningOptions();
        options.SetDomain("example.test");
        options.SetSelector("test-key");
        options.SetTimestamp(1700000000);
        options.AddSignedHeader("Subject");
        return options;
    }

    static RSA Rsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        return rsa;
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n"));
        Assert.That(result.IsComplete(), Is.True);
        return result.GetHeaders();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
