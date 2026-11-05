using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailArcVerifierTests
{
    [Test]
    public void ReportsNoneWhenTheMessageHasNoArcChain()
    {
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: none\r\n");

        EmailArcVerificationResult result = new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeys(headers, Bytes("body\r\n"), []);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.None));
        Assert.That(result.GetChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(result.GetFailureKind(), Is.EqualTo(EmailArcVerificationFailureKind.None));
        Assert.That(result.GetAuthenticationResultKind(), Is.EqualTo(EmailAuthenticationResultKind.None));
        Assert.That(result.GetSetCount(), Is.Zero);
    }

    [Test]
    public void VerifiesMessageSignatureSealAndAuthenticationResultsLink()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: signed\r\n");
        AddSet(headers, body, rsa, 1, "none", "mx1.example");

        EmailArcVerificationResult result = Verify(headers, body, rsa);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
        Assert.That(result.GetChainStatus(), Is.EqualTo(EmailArcChainStatus.Pass));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetFailureKind(), Is.EqualTo(EmailArcVerificationFailureKind.None));
        Assert.That(result.GetAuthenticationResultKind(), Is.EqualTo(EmailAuthenticationResultKind.Pass));
        Assert.That(result.GetReason(), Is.EqualTo("The ARC chain verified successfully."));
        Assert.That(result.HasOldestPass(), Is.True);
        Assert.That(result.GetOldestPass(), Is.Zero);
        Assert.That(result.GetSetCount(), Is.EqualTo(1));
        Assert.That(result.GetSets().Count, Is.EqualTo(1));

        EmailArcSetVerificationResult set = result.GetSet(0);
        Assert.That(set.GetSet(), Is.SameAs(result.GetCollection().GetSet(0)));
        Assert.That(set.GetInstance(), Is.EqualTo(1));
        Assert.That(set.HasMessageSignatureResult(), Is.True);
        EmailArcSignatureVerificationResult message = set.GetMessageSignatureResult();
        Assert.That(message.GetInstance(), Is.EqualTo(1));
        Assert.That(message.GetKind(), Is.EqualTo(EmailSignatureHeaderKind.ArcMessageSignature));
        Assert.That(message.GetField(), Is.SameAs(set.GetSet().GetMessageSignatureField(0)));
        Assert.That(message.GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
        Assert.That(message.GetReason(), Is.EqualTo("The ARC-Message-Signature verified successfully."));
        Assert.That(message.WasEvaluated(), Is.True);
        Assert.That(message.HasVerificationKey(), Is.True);
        Assert.That(message.GetVerificationKey(), Is.Not.Null);
        Assert.That(message.GetBodyCanonicalization(), Is.Not.Null);
        Assert.That(message.GetMessageHeaderCanonicalization(), Is.Not.Null);
        Assert.That(message.IsBodyHashValid(), Is.True);
        Assert.That(message.IsSignatureValid(), Is.True);
        Assert.That(message.HasBodyCanonicalization(), Is.True);
        Assert.That(message.HasMessageHeaderCanonicalization(), Is.True);
        EmailArcSignatureVerificationResult seal = set.GetSealResult();
        Assert.That(seal.GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
        Assert.That(seal.IsSignatureValid(), Is.True);
        Assert.That(seal.HasSealCanonicalization(), Is.True);
        Assert.That(seal.GetSealCanonicalization(), Is.Not.Null);
        Assert.That(set.HasAuthenticationResults(), Is.True);
        Assert.That(set.GetAuthenticationResults()!.GetAuthenticationServiceIdentifier(), Is.EqualTo("mx1.example"));
    }

    [Test]
    public void UsesTheSharedKeyResolverForMessageSignaturesAndSeals()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: resolver\r\n");
        AddSet(headers, body, rsa, 1, "none", "mx1.example");
        RecordingDkimKeyResolver resolver = new RecordingDkimKeyResolver(Binding(rsa));

        EmailArcVerificationResult result = new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, resolver, Options());

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
        Assert.That(resolver.GetResolutionCount(), Is.EqualTo(2));
        Assert.That(resolver.GetLastDomain(), Is.EqualTo("example.test"));
        Assert.That(resolver.GetLastSelector(), Is.EqualTo("key"));

        RecordingDkimKeyResolver defaultResolver = new RecordingDkimKeyResolver(Binding(rsa));
        EmailArcVerificationResult defaultResult = new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeyResolver(headers, body, defaultResolver);
        Assert.That(defaultResult.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
    }

    [Test]
    public void CanonicalizesSealFieldsByInstanceAndArcFieldOrder()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: order\r\n");
        AddSet(headers, body, rsa, 1, "none", "mx1.example");
        AddSet(headers, body, rsa, 2, "pass", "mx2.example");
        EmailArcSetCollectionResult collection = new EmailArcSetCollector().CollectWithHeaders(headers);

        EmailArcSealCanonicalizationResult canonical = new EmailArcCanonicalizer().CanonicalizeSealWithCollectionAndSealInstance(collection, 2);

        Assert.That(canonical.GetSelectionCount(), Is.EqualTo(6));
        Assert.That(canonical.GetSelections().ConvertAll(value => value.GetInstance()), Is.EqualTo(new[] { 1, 1, 1, 2, 2, 2 }));
        Assert.That(canonical.GetSelections().ConvertAll(value => value.GetKind()), Is.EqualTo(new[]
        {
            EmailSignatureHeaderKind.ArcAuthenticationResults,
            EmailSignatureHeaderKind.ArcMessageSignature,
            EmailSignatureHeaderKind.ArcSeal,
            EmailSignatureHeaderKind.ArcAuthenticationResults,
            EmailSignatureHeaderKind.ArcMessageSignature,
            EmailSignatureHeaderKind.ArcSeal
        }));
        Assert.That(canonical.GetSelection(5).WasSignatureValueBlanked(), Is.True);
        Assert.That(canonical.GetSelection(2).WasSignatureValueBlanked(), Is.False);
        Assert.That(canonical.GetSealInstance(), Is.EqualTo(2));
        Assert.That(canonical.GetCanonicalizedByteCount(), Is.EqualTo(canonical.GetCanonicalizedHeaders().Length));
        Assert.That(canonical.GetSourceByteCount(), Is.GreaterThan(canonical.GetCanonicalizedByteCount()));
        Assert.That(canonical.GetSelection(0).GetSourceIndex(), Is.GreaterThanOrEqualTo(0));
        Assert.That(canonical.GetSelection(0).GetHeader().GetName(), Is.EqualTo("ARC-Authentication-Results"));
        Assert.That(canonical.UsesFailedChainScope(), Is.False);
        Assert.That(Ascii(canonical.GetCanonicalizedHeaders()), Does.StartWith("arc-authentication-results:i=1; mx1.example;"));
        Assert.That(Ascii(canonical.GetCanonicalizedHeaders()), Does.EndWith("b="));
    }

    [Test]
    public void ReportsOldestPassWithoutFailingChainWhenOnlyAnOlderBodySignatureFails()
    {
        using RSA rsa = Rsa();
        byte[] firstBody = Bytes("first body\r\n");
        byte[] secondBody = Bytes("second body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: changed body\r\n");
        AddSet(headers, firstBody, rsa, 1, "none", "mx1.example");
        AddSet(headers, secondBody, rsa, 2, "pass", "mx2.example");

        EmailArcVerificationResult result = Verify(headers, secondBody, rsa);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Pass));
        Assert.That(result.HasOldestPass(), Is.True);
        Assert.That(result.GetOldestPass(), Is.EqualTo(2));
        Assert.That(result.GetSet(0).GetMessageSignatureResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.BodyHashMismatch));
        Assert.That(result.GetSet(1).GetMessageSignatureResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
        Assert.That(result.GetSet(0).GetSealResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
        Assert.That(result.GetSet(1).GetSealResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
    }

    [Test]
    public void StopsBeforeSealChecksWhenNewestMessageSignatureFails()
    {
        using RSA rsa = Rsa();
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: newest\r\n");
        AddSet(headers, Bytes("original\r\n"), rsa, 1, "none", "mx1.example");

        EmailArcVerificationResult result = Verify(headers, Bytes("changed\r\n"), rsa);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Fail));
        Assert.That(result.GetFailureKind(), Is.EqualTo(EmailArcVerificationFailureKind.MessageSignature));
        Assert.That(result.GetFailureInstance(), Is.EqualTo(1));
        Assert.That(result.GetSet(0).GetMessageSignatureResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.BodyHashMismatch));
        Assert.That(result.GetSet(0).GetSealResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.NotEvaluated));
    }

    [Test]
    public void ReportsSealFailureWhenAnAuthenticationAssessmentIsChanged()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: seal\r\n");
        AddSet(headers, body, rsa, 1, "none", "mx1.example");
        headers.GetFirst("ARC-Authentication-Results")!.SetValue("i=1; changed.example; dkim=fail header.d=example.test");

        EmailArcVerificationResult result = Verify(headers, body, rsa);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Fail));
        Assert.That(result.GetFailureKind(), Is.EqualTo(EmailArcVerificationFailureKind.Seal));
        Assert.That(result.GetFailureInstance(), Is.EqualTo(1));
        Assert.That(result.GetSet(0).GetMessageSignatureResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.Pass));
        Assert.That(result.GetSet(0).GetSealResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.SignatureMismatch));
        Assert.That(result.GetSet(0).GetSealResult().HasSealCanonicalization(), Is.True);
    }

    [Test]
    public void RejectsStructurallyInvalidChainsBeforeCryptographicEvaluation()
    {
        EmailHeaders headers = ParseHeaders(
            "ARC-Authentication-Results: i=1; mx.example; dkim=pass header.d=example.test\r\n" +
            "ARC-Message-Signature: i=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.test; h=from; s=key\r\n" +
            "From: sender@example.test\r\n");

        EmailArcVerificationResult result = new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeys(headers, Bytes("body\r\n"), []);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcVerificationStatus.Fail));
        Assert.That(result.GetFailureKind(), Is.EqualTo(EmailArcVerificationFailureKind.Structure));
        Assert.That(result.GetFailureInstance(), Is.Zero);
        Assert.That(result.GetCollection().GetIssues().ContainsCode(EmailIssueCode.ArcSetIncomplete), Is.True);
        Assert.That(result.GetSet(0).GetMessageSignatureResult().GetStatus(), Is.EqualTo(EmailArcSignatureVerificationStatus.NotEvaluated));
        Assert.That(result.GetSet(0).HasSealResult(), Is.False);
    }

    [Test]
    public void EnforcesVerificationHeaderLimits()
    {
        using RSA rsa = Rsa();
        byte[] body = Bytes("body\r\n");
        EmailHeaders headers = ParseHeaders("From: sender@example.test\r\nSubject: limits\r\n");
        AddSet(headers, body, rsa, 1, "none", "mx1.example");
        EmailArcVerificationOptions options = Options();
        options.SetMaximumSignatureHeaderBytes(20);

        EmailLimitException error = Assert.Throws<EmailLimitException>(() => new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding(rsa)], options));

        Assert.That(error.GetLimitName(), Is.EqualTo("MaxArcSignatureHeaderBytes"));
    }

    static EmailArcVerificationResult Verify(EmailHeaders headers, byte[] body, RSA rsa)
    {
        return new EmailArcVerifier().VerifyWithHeadersAndBodyAndKeysAndOptions(headers, body, [Binding(rsa)], Options());
    }

    static EmailArcVerificationOptions Options()
    {
        EmailArcVerificationOptions options = new EmailArcVerificationOptions();
        options.SetVerificationTime(1700001000);
        options.SetAllowedClockSkewSeconds(0);
        return options;
    }

    static void AddSet(EmailHeaders headers, byte[] body, RSA rsa, int instance, string chainStatus, string service)
    {
        EmailHeader authenticationResults = Header("ARC-Authentication-Results", " i=" + instance + "; " + service + "; dkim=pass header.d=example.test");
        headers.Insert(0, authenticationResults);

        EmailDkimBodyCanonicalizationResult bodyResult = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndKind(body, EmailDkimCanonicalizationKind.Relaxed).GetResult();
        string messageValue = " i=" + instance + "; a=rsa-sha256; c=relaxed/relaxed; d=example.test; s=key; t=" + (1700000000 + instance) + "; h=From:Subject; bh=" + Convert.ToBase64String(bodyResult.GetHash().GetDigestBytes()) + "; b=";
        EmailHeader unsignedMessage = Header("ARC-Message-Signature", messageValue);
        EmailDkimSignature messageSignature = new EmailArcMessageSignatureParser().ParseWithInputAndMode(unsignedMessage.GetRawValue(), EmailStructuredValueParseMode.Strict).GetValue()!;
        byte[] messageHash = SHA256.HashData(new EmailArcCanonicalizer().CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeader(headers, messageSignature!, unsignedMessage).GetCanonicalizedHeaders());
        string messageSignatureValue = Convert.ToBase64String(rsa.SignHash(messageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        headers.Insert(0, Header("ARC-Message-Signature", messageValue + messageSignatureValue));

        string sealValue = " i=" + instance + "; a=rsa-sha256; cv=" + chainStatus + "; d=example.test; s=key; t=" + (1700000000 + instance) + "; b=";
        headers.Insert(0, Header("ARC-Seal", sealValue));
        EmailArcSetCollectionResult unsignedCollection = new EmailArcSetCollector().CollectWithHeaders(headers);
        byte[] sealHash = SHA256.HashData(new EmailArcCanonicalizer().CanonicalizeSealWithCollectionAndSealInstance(unsignedCollection, instance).GetCanonicalizedHeaders());
        string sealSignatureValue = Convert.ToBase64String(rsa.SignHash(sealHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        headers.ReplaceAt(0, Header("ARC-Seal", sealValue + sealSignatureValue));
    }

    static EmailHeader Header(string name, string value)
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValue(name, Bytes(value));
        header.SetLineEnding(EmailLineEnding.CrLf);
        return header;
    }

    static EmailDkimVerificationKey Binding(RSA rsa)
    {
        EmailDkimKey key = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(rsa.ExportSubjectPublicKeyInfo()).GetValue()!;
        return new EmailDkimVerificationKey("example.test", "key", key!);
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

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
