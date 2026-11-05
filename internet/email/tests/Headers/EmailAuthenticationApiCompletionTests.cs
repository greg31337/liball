using System.Text;
using email;

namespace email.Tests;

public class EmailAuthenticationApiCompletionTests
{
    [Test]
    public void AuthenticationResultsAndReceivedSpfExposeCompleteParseAndFormatProjections()
    {
        byte[] authenticationInput = Ascii("mx.example; spf=pass smtp.mailfrom=user@example.com; dkim=pass header.d=example.com");
        EmailAuthenticationResultsParseResult authenticationParse = new EmailAuthenticationResultsParser().ParseWithInputAndMode(authenticationInput, EmailStructuredValueParseMode.Strict);

        Assert.That(authenticationParse.HasValue(), Is.True);
        Assert.That(authenticationParse.GetConsumedByteCount(), Is.EqualTo(authenticationInput.Length));
        Assert.That(authenticationParse.GetSourceByteCount(), Is.EqualTo(authenticationInput.Length));
        Assert.That(authenticationParse.IsComplete(), Is.True);
        Assert.That(authenticationParse.IsSuccess(), Is.True);
        Assert.That(authenticationParse.HasFatalError(), Is.False);

        EmailAuthenticationResults authentication = authenticationParse.GetValue()!;
        Assert.That(authentication.GetResults().Select(result => result.GetMethod()), Is.EqualTo(new[] { "spf", "dkim" }));
        EmailAuthenticationFormatResult authenticationFormat = authentication.Format();
        Assert.That(authenticationFormat.IsSuccess(), Is.True);
        Assert.That(new EmailAuthenticationResultsParser().ParseWithInputAndMode(authenticationFormat.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);

        byte[] spfInput = Ascii("PASS (sender permitted) Client-IP=192.0.2.1; envelope-from=\"user@example.com\";");
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        EmailSourcePosition position = new EmailSourcePosition(40, 3, 7);
        EmailReceivedSpfParseResult spfParse = new EmailReceivedSpfParser().ParseWithInputAndOptionsAndInitialPosition(spfInput, options, position);

        Assert.That(spfParse.HasValue(), Is.True);
        Assert.That(spfParse.GetConsumedByteCount(), Is.EqualTo(spfInput.Length));
        Assert.That(spfParse.GetSourceByteCount(), Is.EqualTo(spfInput.Length));
        Assert.That(spfParse.IsComplete(), Is.True);
        Assert.That(spfParse.IsSuccess(), Is.True);
        Assert.That(spfParse.HasFatalError(), Is.False);

        EmailReceivedSpf spf = spfParse.GetValue()!;
        Assert.That(spf.GetNormalizedResult(), Is.EqualTo("pass"));
        Assert.That(spf.GetParameters().Count, Is.EqualTo(2));
        EmailReceivedSpfParameter parameter = spf.GetParameter(0);
        Assert.That(parameter.GetNormalizedName(), Is.EqualTo("client-ip"));
        Assert.That(Encoding.ASCII.GetString(parameter.GetRawBytes()), Is.EqualTo("Client-IP=192.0.2.1"));
        Assert.That(Encoding.ASCII.GetString(parameter.GetRawValueBytes()), Is.EqualTo("192.0.2.1"));
        Assert.That(parameter.GetSourcePosition().GetByteOffset(), Is.EqualTo(64));
        EmailAuthenticationFormatResult spfFormat = spf.Format();
        Assert.That(spfFormat.IsSuccess(), Is.True);
        Assert.That(new EmailReceivedSpfParser().ParseWithInputAndMode(spfFormat.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
    }

    [Test]
    public void DkimParsersKeysAndResettableOptionsRetainTheirDocumentedState()
    {
        byte[] signatureInput = Ascii("v=1; a=rsa-sha256; d=example.com; s=selector; c=relaxed/simple; h=from:subject; bh=YWJjZA==; b=ZWZnaA==");
        EmailSignatureParserOptions signatureOptions = new EmailSignatureParserOptions();
        EmailDkimSignatureParseResult signatureParse = new EmailDkimSignatureParser().ParseWithInputAndOptionsAndInitialPosition(signatureInput, signatureOptions, new EmailSourcePosition(11, 2, 4));

        Assert.That(signatureParse.GetConsumedByteCount(), Is.EqualTo(signatureInput.Length));
        Assert.That(signatureParse.GetSourceByteCount(), Is.EqualTo(signatureInput.Length));
        Assert.That(signatureParse.IsComplete(), Is.True);
        Assert.That(signatureParse.HasFatalError(), Is.False);
        Assert.That(signatureParse.GetValue()!.GetHeaderKind(), Is.EqualTo(EmailSignatureHeaderKind.DkimSignature));
        Assert.That(signatureParse.GetValue()!.GetSignedHeader(1), Is.EqualTo("subject"));

        EmailDkimSigningOptions signing = new EmailDkimSigningOptions();
        signing.SetTimestamp(100);
        signing.SetExpiration(200);
        signing.SetBodyLengthLimit(12);
        Assert.That(signing.GetTimestamp(), Is.EqualTo(100));
        Assert.That(signing.GetExpiration(), Is.EqualTo(200));
        Assert.That(signing.GetBodyLengthLimit(), Is.EqualTo(12));
        signing.ClearTimestamp();
        signing.ClearExpiration();
        signing.ClearBodyLengthLimit();
        Assert.That(signing.HasTimestamp(), Is.False);
        Assert.That(signing.HasExpiration(), Is.False);
        Assert.That(signing.HasBodyLengthLimit(), Is.False);

        EmailDkimVerificationOptions verification = new EmailDkimVerificationOptions();
        verification.SetVerificationTime(300);
        Assert.That(verification.GetVerificationTime(), Is.EqualTo(300));
        verification.ClearVerificationTime();
        Assert.That(verification.HasVerificationTime(), Is.False);

        EmailDkimKeyParser parser = new EmailDkimKeyParser();
        EmailDkimKeyParserOptions keyOptions = new EmailDkimKeyParserOptions();
        Assert.That(parser.ImportSubjectPublicKeyInfoWithInputAndOptions(new byte[] { 1, 2, 3 }, keyOptions).IsSuccess(), Is.False);
        Assert.That(parser.ImportRsaPublicKeyWithInputAndOptions(new byte[] { 1, 2, 3 }, keyOptions).IsSuccess(), Is.False);
        Assert.That(parser.ImportPemWithInputAndOptions("not a public key", keyOptions).IsSuccess(), Is.False);

        byte[] keyInput = Ascii("v=DKIM1; k=ed25519; p=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        EmailDkimKeyParseResult keyParse = parser.ParseWithInputAndOptions(keyInput, keyOptions);
        Assert.That(keyParse.GetConsumedByteCount(), Is.EqualTo(keyInput.Length));
        Assert.That(keyParse.GetSourceByteCount(), Is.EqualTo(keyInput.Length));
        Assert.That(keyParse.IsComplete(), Is.True);
        EmailDkimKey key = keyParse.GetValue()!;
        Assert.That(key.HasEd25519PublicKey(), Is.True);
        EmailValueFormatOptions formatOptions = new EmailValueFormatOptions();
        Assert.That(key.FormatPreservingWithOptions(formatOptions).GetBytes(), Is.EqualTo(keyInput));
    }

    [Test]
    public void DisposingAnIncrementalBodyCanonicalizerClosesIt()
    {
        EmailDkimBodyCanonicalizationOptions options = new EmailDkimBodyCanonicalizationOptions();
        EmailDkimBodyCanonicalizer canonicalizer = new EmailDkimBodyCanonicalizer(options);
        canonicalizer.Feed(Ascii("body"));

        canonicalizer.Dispose();

        Assert.Throws<InvalidOperationException>(() => canonicalizer.Feed(Ascii("more")));
    }

    [Test]
    public void ArcParsersCollectionAndResettableOptionsExposeEveryHeaderKind()
    {
        byte[] authenticationInput = Ascii("i=1; mx.example; dkim=pass header.d=example.com");
        EmailArcAuthenticationResultsParseResult authenticationParse = new EmailArcAuthenticationResultsParser().ParseWithInputAndMode(authenticationInput, EmailStructuredValueParseMode.Strict);
        AssertParseResult(authenticationParse.HasValue(), authenticationParse.GetConsumedByteCount(), authenticationParse.GetSourceByteCount(), authenticationParse.IsComplete(), authenticationParse.HasFatalError(), authenticationInput.Length);
        EmailArcAuthenticationResults authentication = authenticationParse.GetValue()!;
        Assert.That(authentication.GetInstanceTag().GetName(), Is.EqualTo("i"));
        Assert.That(authentication.Format().IsSuccess(), Is.True);

        byte[] messageSignatureInput = Ascii("i=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=arc");
        EmailArcMessageSignatureParseResult messageSignatureParse = new EmailArcMessageSignatureParser().ParseWithInputAndMode(messageSignatureInput, EmailStructuredValueParseMode.Strict);
        AssertParseResult(messageSignatureParse.HasValue(), messageSignatureParse.GetConsumedByteCount(), messageSignatureParse.GetSourceByteCount(), messageSignatureParse.IsComplete(), messageSignatureParse.HasFatalError(), messageSignatureInput.Length);

        byte[] sealInput = Ascii("i=1; a=rsa-sha256; b=YWJjZA==; cv=none; d=example.com; s=arc; t=1");
        EmailArcSealParseResult sealParse = new EmailArcSealParser().ParseWithInputAndMode(sealInput, EmailStructuredValueParseMode.Strict);
        AssertParseResult(sealParse.HasValue(), sealParse.GetConsumedByteCount(), sealParse.GetSourceByteCount(), sealParse.IsComplete(), sealParse.HasFatalError(), sealInput.Length);
        EmailArcSeal seal = sealParse.GetValue()!;
        Assert.That(seal.GetRawBytes(), Is.EqualTo(sealInput));
        Assert.That(seal.GetSourcePosition().GetByteOffset(), Is.Zero);
        Assert.That(seal.Format().IsSuccess(), Is.True);

        string headersText = "ARC-Authentication-Results: " + Encoding.ASCII.GetString(authenticationInput) + "\r\n"
            + "ARC-Message-Signature: " + Encoding.ASCII.GetString(messageSignatureInput) + "\r\n"
            + "ARC-Seal: " + Encoding.ASCII.GetString(sealInput) + "\r\n"
            + "From: sender@example.com\r\n\r\n";
        EmailHeaders headers = new EmailHeaderBlockParser().ParseWithInput(Ascii(headersText)).GetHeaders();
        EmailArcSetCollectionResult collection = new EmailArcSetCollector().CollectWithHeaders(headers);
        Assert.That(collection.GetSets().Count, Is.EqualTo(1));
        Assert.That(collection.GetFields().Count, Is.EqualTo(3));
        EmailArcSet set = collection.GetSets()[0];
        Assert.That(set.GetAuthenticationResultsFields().Count, Is.EqualTo(1));
        Assert.That(set.GetMessageSignatureFields().Count, Is.EqualTo(1));
        Assert.That(set.GetSealFields().Count, Is.EqualTo(1));
        Assert.That(set.GetDeclaredChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(set.GetMessageSignatureAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.RsaSha256));
        Assert.That(set.GetSealAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.RsaSha256));
        Assert.That(set.HasMatchingAlgorithms(), Is.True);
        EmailArcHeaderField field = set.GetAuthenticationResultsField(0);
        Assert.That(field.HasArcAuthenticationResults(), Is.True);
        Assert.That(field.GetArcAuthenticationResults()!.GetInstance(), Is.EqualTo(1));

        EmailArcSealingOptions sealing = new EmailArcSealingOptions();
        sealing.SetTimestamp(400);
        sealing.SetBodyLengthLimit(20);
        Assert.That(sealing.GetTimestamp(), Is.EqualTo(400));
        Assert.That(sealing.GetBodyLengthLimit(), Is.EqualTo(20));
        sealing.ClearTimestamp();
        sealing.ClearBodyLengthLimit();
        Assert.That(sealing.HasTimestamp(), Is.False);
        Assert.That(sealing.HasBodyLengthLimit(), Is.False);

        EmailArcVerificationOptions verification = new EmailArcVerificationOptions();
        verification.SetVerificationTime(500);
        Assert.That(verification.GetVerificationTime(), Is.EqualTo(500));
        verification.ClearVerificationTime();
        Assert.That(verification.HasVerificationTime(), Is.False);
    }

    static void AssertParseResult(bool hasValue, int consumed, int source, bool complete, bool fatal, int expectedLength)
    {
        Assert.That(hasValue, Is.True);
        Assert.That(consumed, Is.EqualTo(expectedLength));
        Assert.That(source, Is.EqualTo(expectedLength));
        Assert.That(complete, Is.True);
        Assert.That(fatal, Is.False);
    }

    static byte[] Ascii(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
