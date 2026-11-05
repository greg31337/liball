using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailArcSealerTests
{
    [Test]
    public void CreatesAndInsertsACompleteVerifiableArcSetWithCallerKeys()
    {
        using RSA messageKey = Rsa();
        using RSA sealKey = Rsa();
        byte[] body = Bytes("message body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: sealed\r\n");
        EmailArcSealingOptions options = Options();

        EmailArcSealingResult sealedResult = new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, Assessment("mx1.example"), EmailArcChainStatus.None, messageKey, sealKey, options);

        Assert.That(sealedResult.GetInstance(), Is.EqualTo(1));
        Assert.That(sealedResult.GetChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(sealedResult.GetPriorArcHeaderCount(), Is.Zero);
        Assert.That(sealedResult.GetTimestamp(), Is.EqualTo(1700000000));
        Assert.That(sealedResult.GetMessageSignature().GetSigningDomain(), Is.EqualTo("ams.example"));
        Assert.That(sealedResult.GetMessageSignature().GetSelector(), Is.EqualTo("ams-key"));
        Assert.That(sealedResult.GetSeal().GetSigningDomain(), Is.EqualTo("seal.example"));
        Assert.That(sealedResult.GetSeal().GetSelector(), Is.EqualTo("seal-key"));
        Assert.That(sealedResult.GetSeal().GetChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(headers.GetAt(0).GetName(), Is.EqualTo("ARC-Seal"));
        Assert.That(headers.GetAt(1).GetName(), Is.EqualTo("ARC-Message-Signature"));
        Assert.That(headers.GetAt(2).GetName(), Is.EqualTo("ARC-Authentication-Results"));
        Assert.That(sealedResult.GetAuthenticationResults().GetAuthenticationResults().GetAuthenticationServiceIdentifier(), Is.EqualTo("mx1.example"));
        Assert.That(sealedResult.GetBodyCanonicalization().GetKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
        Assert.That(sealedResult.GetHeadersInInsertionOrder().ConvertAll(value => value.GetName()), Is.EqualTo(new[]
        {
            "ARC-Seal",
            "ARC-Message-Signature",
            "ARC-Authentication-Results"
        }));
        Assert.That(messageKey.VerifyHash(sealedResult.GetMessageHeaderHashBytes(), sealedResult.GetMessageSignatureBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), Is.True);
        Assert.That(sealKey.VerifyHash(sealedResult.GetSealHeaderHashBytes(), sealedResult.GetSealSignatureBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), Is.True);

        EmailArcVerificationResult verification = Verify(headers, body, messageKey, sealKey);
        Assert.That(verification.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
        Assert.That(verification.GetSet(0).GetMessageSignatureResult().IsSuccess(), Is.True);
        Assert.That(verification.GetSet(0).GetSealResult().IsSuccess(), Is.True);
    }

    [Test]
    public void ExtendsAChainWithoutChangingAnyPriorHeaderObjectOrByte()
    {
        using RSA messageKey = Rsa();
        using RSA sealKey = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject:\t original spacing \r\n");
        EmailArcSealingOptions options = Options();
        new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, Assessment("mx1.example"), EmailArcChainStatus.None, messageKey, sealKey, options);
        List<EmailHeader> priorHeaders = headers.GetAll();
        List<byte[]> priorBytes = priorHeaders.ConvertAll(value => value.GetRawFieldBytes());
        int countBefore = headers.GetCount();

        EmailArcSealingResult preview = new EmailArcSealer().SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, Assessment("mx2.example"), EmailArcChainStatus.Pass, messageKey, sealKey, options);

        Assert.That(preview.GetInstance(), Is.EqualTo(2));
        Assert.That(headers.GetCount(), Is.EqualTo(countBefore));
        AssertPreserved(headers, priorHeaders, priorBytes);

        EmailArcSealingResult inserted = new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, Assessment("mx2.example"), EmailArcChainStatus.Pass, messageKey, sealKey, options);

        Assert.That(inserted.GetInstance(), Is.EqualTo(2));
        Assert.That(inserted.GetPriorArcHeaderCount(), Is.EqualTo(3));
        Assert.That(inserted.GetSealCanonicalization().GetSelectionCount(), Is.EqualTo(6));
        Assert.That(headers.GetCount(), Is.EqualTo(countBefore + 3));
        AssertPreserved(headers, priorHeaders, priorBytes);
        Assert.That(Verify(headers, body, messageKey, sealKey).GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
    }

    [Test]
    public void IncludesEveryExistingDkimSignatureInTheMessageSignatureByDefault()
    {
        using RSA key = Rsa();
        EmailHeaders headers = ParseHeaders(
            "DKIM-Signature: v=1; a=rsa-sha256; d=one.example; s=key; h=from; bh=YWJjZA==; b=YWJjZA==\r\n" +
            "DKIM-Signature: v=1; a=rsa-sha256; d=two.example; s=key; h=from; bh=YWJjZA==; b=YWJjZA==\r\n" +
            "From: sender@example.test\r\n");

        EmailArcSealingResult result = new EmailArcSealer().SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, Bytes("body\r\n"), Assessment("mx.example"), EmailArcChainStatus.None, key, SameKeyOptions());

        Assert.That(result.GetMessageSignature().GetSignedHeaders(), Is.EqualTo(new[] { "From", "DKIM-Signature", "DKIM-Signature" }));
        Assert.That(result.GetMessageHeaderCanonicalization().GetSelectionCount(), Is.EqualTo(3));
        Assert.That(result.GetMessageHeaderCanonicalization().GetSelection(1).GetSourceIndex(), Is.EqualTo(2));
        Assert.That(result.GetMessageHeaderCanonicalization().GetSelection(2).GetSourceIndex(), Is.EqualTo(1));
        Assert.That(headers.GetCount(), Is.EqualTo(3));
    }

    [Test]
    public void SealsAReportedFailureWithOnlyTheNewSetAndPreventsContinuation()
    {
        using RSA key = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\n");
        EmailArcSealingOptions options = SameKeyOptions();
        new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx1.example"), EmailArcChainStatus.None, key, options);

        EmailArcSealingResult failed = new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx2.example"), EmailArcChainStatus.Fail, key, options);

        Assert.That(failed.GetInstance(), Is.EqualTo(2));
        Assert.That(failed.GetSeal().GetChainStatus(), Is.EqualTo(EmailArcChainStatus.Fail));
        Assert.That(failed.GetSealCanonicalization().UsesFailedChainScope(), Is.True);
        Assert.That(failed.GetSealCanonicalization().GetSelectionCount(), Is.EqualTo(3));
        Assert.That(failed.GetSealCanonicalization().GetSelections().ConvertAll(value => value.GetInstance()), Is.EqualTo(new[] { 2, 2, 2 }));
        Assert.That(failed.GetCollection().GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.Invalid));
        Assert.Throws<InvalidOperationException>(() => new EmailArcSealer().SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx3.example"), EmailArcChainStatus.Pass, key, options));
    }

    [Test]
    public void RejectsInvalidPolicyAndResourceInputsWithoutMutatingHeaders()
    {
        using RSA key = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: atomic\r\n");
        int originalCount = headers.GetCount();
        EmailArcSealingOptions prohibited = SameKeyOptions();
        prohibited.AddSignedHeader("ARC-Seal");
        EmailArcSealingOptions headerLimit = SameKeyOptions();
        headerLimit.SetMaximumSignatureHeaderBytes(20);

        Assert.Throws<ArgumentException>(() => new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx.example"), EmailArcChainStatus.Pass, key, SameKeyOptions()));
        Assert.Throws<ArgumentException>(() => new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx.example"), EmailArcChainStatus.None, key, prohibited));
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailArcSealer().SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, Assessment("mx.example"), EmailArcChainStatus.None, key, headerLimit));

        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxArcSignatureHeaderBytes"));
        Assert.That(headers.GetCount(), Is.EqualTo(originalCount));
        Assert.That(headers.GetCountNamed("ARC-Seal"), Is.Zero);
        Assert.That(headers.GetCountNamed("ARC-Message-Signature"), Is.Zero);
        Assert.That(headers.GetCountNamed("ARC-Authentication-Results"), Is.Zero);
    }

    static void AssertPreserved(EmailHeaders headers, List<EmailHeader> priorHeaders, List<byte[]> priorBytes)
    {
        int index = 0;
        while (index < priorHeaders.Count)
        {
            Assert.That(headers.IndexOf(priorHeaders[index]), Is.GreaterThanOrEqualTo(0));
            Assert.That(priorHeaders[index].GetRawFieldBytes(), Is.EqualTo(priorBytes[index]));
            index++;
        }
    }

    static EmailArcVerificationResult Verify(EmailHeaders headers, byte[] body, RSA messageKey, RSA sealKey)
    {
        List<EmailDkimVerificationKey> keys =
        [
            Binding("ams.example", "ams-key", messageKey),
            Binding("seal.example", "seal-key", sealKey)
        ];
        EmailArcVerificationOptions options = new EmailArcVerificationOptions();
        options.SetVerificationTime(1700000100);
        options.SetAllowedClockSkewSeconds(0);
        return new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, options);
    }

    static EmailDkimVerificationKey Binding(string domain, string selector, RSA key)
    {
        EmailDkimKey parsed = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(key.ExportSubjectPublicKeyInfo()).GetValue()!;
        return new EmailDkimVerificationKey(domain, selector, parsed!);
    }

    static EmailArcSealingOptions Options()
    {
        EmailArcSealingOptions options = new EmailArcSealingOptions();
        options.SetMessageSignatureDomain("ams.example");
        options.SetMessageSignatureSelector("ams-key");
        options.SetSealDomain("seal.example");
        options.SetSealSelector("seal-key");
        options.SetTimestamp(1700000000);
        options.AddSignedHeader("Subject");
        return options;
    }

    static EmailArcSealingOptions SameKeyOptions()
    {
        EmailArcSealingOptions options = new EmailArcSealingOptions();
        options.SetMessageSignatureDomain("example.test");
        options.SetMessageSignatureSelector("key");
        options.SetSealDomain("example.test");
        options.SetSealSelector("key");
        options.SetTimestamp(1700000000);
        return options;
    }

    static EmailAuthenticationResults Assessment(string service)
    {
        EmailAuthenticationResultsParseResult parsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(Bytes(service + "; dkim=pass header.d=example.test"), EmailStructuredValueParseMode.Strict);
        Assert.That(parsed.IsSuccess(), Is.True);
        return parsed.GetValue()!;
    }

    static RSA Rsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        return rsa;
    }

    static EmailHeaders ParseHeaders(string fields)
    {
        EmailHeaderBlockParseResult parsed = new EmailHeaderBlockParser().ParseWithInput(Bytes(fields + "\r\n"));
        Assert.That(parsed.IsComplete(), Is.True);
        return parsed.GetHeaders();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
