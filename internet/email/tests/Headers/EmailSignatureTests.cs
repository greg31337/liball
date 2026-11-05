using System.Text;
using email;

namespace email.Tests;

public class EmailSignatureTests
{
    [Test]
    public void ParsesDkimTagsIntoTypedValuesAndPreservesRawData()
    {
        const string input = "v=1; a=ed25519-sha256; c=relaxed/relaxed; d=Example.COM; s=selector; h=From : Subject; bh=YWJjZA==; b= ZGVm\r\n\tZw== ; l=42; q=dns/txt; t=123; x=456; z=From:sender@example.com; x_vendor=opaque";

        EmailDkimSignatureParseResult result = ParseDkim(input, EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailDkimSignature value = result.GetValue()!;
        Assert.That(value!.GetVersion(), Is.EqualTo(1));
        Assert.That(value.GetAlgorithmKind(), Is.EqualTo(EmailDkimAlgorithmKind.Ed25519Sha256));
        Assert.That(value.GetHeaderCanonicalizationKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
        Assert.That(value.GetBodyCanonicalizationKind(), Is.EqualTo(EmailDkimCanonicalizationKind.Relaxed));
        Assert.That(value.GetSigningDomain(), Is.EqualTo("Example.COM"));
        Assert.That(value.GetSelector(), Is.EqualTo("selector"));
        Assert.That(value.GetSignedHeaders(), Is.EqualTo(new[] { "From", "Subject" }));
        Assert.That(value.GetBodyHash(), Is.EqualTo("YWJjZA=="));
        Assert.That(value.GetSignatureData(), Is.EqualTo("ZGVmZw=="));
        Assert.That(value.GetBodyLength(), Is.EqualTo(42));
        Assert.That(value.GetTimestamp(), Is.EqualTo(123));
        Assert.That(value.GetExpiration(), Is.EqualTo(456));
        Assert.That(value.GetQueryMethods(), Is.EqualTo(new[] { "dns/txt" }));
        Assert.That(value.GetCopiedHeaders(), Is.EqualTo("From:sender@example.com"));
        Assert.That(value.GetTags().GetAt(value.GetTags().GetCount() - 1).GetName(), Is.EqualTo("x_vendor"));
        Assert.That(value.GetTags().GetAt(value.GetTags().GetCount() - 1).GetValue(), Is.EqualTo("opaque"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
        Assert.That(Ascii(value.GetTags().GetFirst("b")!.GetRawValueBytes()), Is.EqualTo(" ZGVm\r\n\tZw== "));
    }

    [Test]
    public void PreservesUnknownTagsAndTheirOriginalOrder()
    {
        const string input = "v=1; x_first=one; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key; x_last=two";

        EmailDkimSignature value = ParseDkim(input, EmailStructuredValueParseMode.Strict).GetValue()!;

        Assert.That(value!.GetTags().GetCount(), Is.EqualTo(9));
        Assert.That(value.GetTags().GetAt(1).GetNormalizedName(), Is.EqualTo("x_first"));
        Assert.That(value.GetTags().GetAt(8).GetNormalizedName(), Is.EqualTo("x_last"));
        Assert.That(Ascii(value.GetTags().GetAt(1).GetRawBytes()), Is.EqualTo(" x_first=one"));
    }

    [Test]
    public void DuplicateTagsAreRetainedWithModeSpecificSeverity()
    {
        const string input = "v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=one; S=two";

        EmailDkimSignatureParseResult tolerant = ParseDkim(input, EmailStructuredValueParseMode.Tolerant);
        EmailDkimSignatureParseResult strict = ParseDkim(input, EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.SignatureTagDuplicate), Is.True);
        Assert.That(tolerant.GetValue()!.GetTags().GetAllNamed("s").Count, Is.EqualTo(2));
        Assert.That(tolerant.GetValue()!.GetSelector(), Is.EqualTo("one"));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ReportsMissingAndMalformedDkimTagsWithoutCryptoVerification()
    {
        const string malformed = "a=rsa-sha256; b=not_base64!; bh=YWJjZA==; d=example.com; h=from; s=key; t=not-a-time";

        EmailDkimSignatureParseResult tolerant = ParseDkim(malformed, EmailStructuredValueParseMode.Tolerant);
        EmailDkimSignatureParseResult strict = ParseDkim(malformed, EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.HasValue(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.DkimSignatureMalformed), Is.True);
        Assert.That(tolerant.GetValue()!.GetSignatureData(), Is.EqualTo("not_base64!"));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ParsesArcMessageSignatureInstanceAndIgnoresVersionAsAnExtension()
    {
        const string input = "(edge) i (set) = 2; v=99; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from:subject; s=arc";

        EmailArcMessageSignatureParseResult result = new EmailArcMessageSignatureParser().ParseWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.IsArcMessageSignature(), Is.True);
        Assert.That(result.GetValue()!.GetInstance(), Is.EqualTo(2));
        Assert.That(result.GetValue()!.GetVersion(), Is.EqualTo(99));
        Assert.That(result.GetValue()!.GetTags().GetFirst("v")!.GetValue(), Is.EqualTo("99"));
    }

    [Test]
    public void ValidatesArcInstanceBounds()
    {
        const string input = "i=51; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=arc";

        EmailArcMessageSignatureParseResult tolerant = new EmailArcMessageSignatureParser().ParseWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Tolerant);
        EmailArcMessageSignatureParseResult strict = new EmailArcMessageSignatureParser().ParseWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.GetValue()!.GetInstance(), Is.EqualTo(51));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ArcHeaderMalformed), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ParsesArcSealAndRejectsDkimOnlyTags()
    {
        const string valid = "i=1; a=rsa-sha256; b=YWJjZA==; cv=none; d=example.com; s=arc; t=123";
        const string invalid = "i=2; a=rsa-sha256; b=YWJjZA==; cv=pass; d=example.com; s=arc; t=124; h=from";

        EmailArcSealParseResult parsed = new EmailArcSealParser().ParseWithInputAndMode(Bytes(valid), EmailStructuredValueParseMode.Strict);
        EmailArcSealParseResult prohibited = new EmailArcSealParser().ParseWithInputAndMode(Bytes(invalid), EmailStructuredValueParseMode.Strict);
        EmailSignatureFormatOptions options = new EmailSignatureFormatOptions();
        options.SetMaximumLineLength(998);
        EmailSignatureFormatResult formatted = parsed.GetValue()!.FormatWithOptions(options);

        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(parsed.GetValue()!.GetInstance(), Is.EqualTo(1));
        Assert.That(parsed.GetValue()!.GetChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(parsed.GetValue()!.GetTimestamp(), Is.EqualTo(123));
        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(Ascii(formatted.GetBytes()), Is.EqualTo(valid));
        Assert.That(new EmailArcSealParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(prohibited.IsSuccess(), Is.False);
        Assert.That(prohibited.GetValue()!.GetTags().GetFirst("h")!.GetValue(), Is.EqualTo("from"));
    }

    [Test]
    public void ParsesAndFormatsArcAuthenticationResultsPayload()
    {
        const string input = "i=3; mx.example; dkim=pass header.d=example.com header.s=selector; spf=pass smtp.mailfrom=example.com";

        EmailArcAuthenticationResultsParseResult result = new EmailArcAuthenticationResultsParser().ParseWithInputAndMode(Bytes(input), EmailStructuredValueParseMode.Strict);
        EmailSignatureFormatOptions options = new EmailSignatureFormatOptions();
        options.SetMaximumLineLength(998);
        EmailSignatureFormatResult formatted = result.GetValue()!.FormatWithOptions(options);
        EmailArcAuthenticationResultsParseResult reparsed = new EmailArcAuthenticationResultsParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.GetInstance(), Is.EqualTo(3));
        Assert.That(result.GetValue()!.GetAuthenticationResults().GetResultCount(), Is.EqualTo(2));
        Assert.That(result.GetValue()!.GetAuthenticationResults().GetResult(0).GetMethod(), Is.EqualTo("dkim"));
        Assert.That(Ascii(result.GetValue()!.GetRawBytes()), Is.EqualTo(input));
        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(Ascii(formatted.GetBytes()), Is.EqualTo("i=3; mx.example; dkim=pass header.d=example.com header.s=selector; spf=pass smtp.mailfrom=example.com"));
        Assert.That(reparsed.IsSuccess(), Is.True);
    }

    [Test]
    public void CanonicalFormattingNormalizesTagsFoldsBase64AndRoundTrips()
    {
        const string input = "V = 1 ; A = RSA-SHA256 ; X_vendor = opaque ; B = YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXo= ; BH = YWJjZA== ; D = Example.COM ; H = From : Subject ; S = selector";
        EmailDkimSignature value = ParseDkim(input, EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailSignatureFormatOptions options = new EmailSignatureFormatOptions();
        options.SetMaximumLineLength(35);

        EmailSignatureFormatResult formatted = value!.FormatWithOptions(options);
        EmailDkimSignatureParseResult reparsed = new EmailDkimSignatureParser().ParseWithInputAndMode(formatted.GetBytes(), EmailStructuredValueParseMode.Strict);

        Assert.That(formatted.IsSuccess(), Is.True);
        Assert.That(formatted.GetFoldCount(), Is.GreaterThan(0));
        Assert.That(Ascii(formatted.GetBytes()), Does.Contain("x_vendor=opaque"));
        Assert.That(Ascii(formatted.GetBytes()), Does.Contain("\r\n\t"));
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetValue()!.GetSignatureData(), Is.EqualTo(value.GetSignatureData()));
        Assert.That(reparsed.GetValue()!.GetTags().GetAt(2).GetNormalizedName(), Is.EqualTo("x_vendor"));
    }

    [Test]
    public void FormatterRejectsDuplicatesAndOutputOverflowWithoutPartialBytes()
    {
        EmailDkimSignature duplicate = ParseDkim("v=1; a=rsa-sha256; a=ed25519-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key", EmailStructuredValueParseMode.Tolerant).GetValue()!;
        EmailDkimSignature valid = ParseDkim("v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key", EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailSignatureFormatOptions options = new EmailSignatureFormatOptions();
        options.SetMaximumOutputBytes(10);

        EmailSignatureFormatResult duplicateResult = duplicate!.Format();
        EmailSignatureFormatResult limitResult = valid!.FormatWithOptions(options);

        Assert.That(duplicateResult.HasFatalError(), Is.True);
        Assert.That(duplicateResult.GetIssues().ContainsCode(EmailIssueCode.SignatureFormattingInvalid), Is.True);
        Assert.That(duplicateResult.GetByteCount(), Is.Zero);
        Assert.That(limitResult.HasFatalError(), Is.True);
        Assert.That(limitResult.GetIssues().ContainsCode(EmailIssueCode.SignatureOutputLimitExceeded), Is.True);
        Assert.That(limitResult.GetByteCount(), Is.Zero);
    }

    [Test]
    public void HonorsSourcePositionsAndParserLimits()
    {
        EmailSignatureParserOptions positionOptions = new EmailSignatureParserOptions();
        positionOptions.SetMode(EmailStructuredValueParseMode.Strict);
        EmailDkimSignatureParseResult positioned = new EmailDkimSignatureParser().ParseWithInputAndOptionsAndInitialPosition(Bytes("v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key"), positionOptions, new EmailSourcePosition(50, 4, 7));
        EmailSignatureParserOptions tagOptions = new EmailSignatureParserOptions();
        tagOptions.SetMaximumTags(2);
        EmailDkimSignatureParseResult tagLimited = new EmailDkimSignatureParser().ParseWithInputAndOptions(Bytes("v=1; a=rsa-sha256; b=YWJjZA=="), tagOptions);
        EmailSignatureParserOptions inputOptions = new EmailSignatureParserOptions();
        inputOptions.SetMaximumInputBytes(3);
        EmailDkimSignatureParseResult inputLimited = new EmailDkimSignatureParser().ParseWithInputAndOptions(Bytes("v=1; a=rsa-sha256"), inputOptions);

        Assert.That(positioned.GetValue()!.GetTags().GetFirst("v")!.GetSourcePosition().GetByteOffset(), Is.EqualTo(50));
        Assert.That(positioned.GetValue()!.GetTags().GetFirst("a")!.GetSourcePosition().GetByteOffset(), Is.EqualTo(55));
        Assert.That(tagLimited.HasFatalError(), Is.True);
        Assert.That(tagLimited.GetValue()!.GetTags().GetCount(), Is.EqualTo(2));
        Assert.That(tagLimited.IsComplete(), Is.False);
        Assert.That(inputLimited.HasValue(), Is.False);
        Assert.That(inputLimited.GetIssues().ContainsCode(EmailIssueCode.SignatureValueLimitExceeded), Is.True);
    }

    [Test]
    public void ReturnedRawArraysAreDefensiveCopies()
    {
        EmailDkimSignature value = ParseDkim("v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key", EmailStructuredValueParseMode.Strict).GetValue()!;
        byte[] valueRaw = value!.GetRawBytes();
        byte[] tagRaw = value.GetTags().GetFirst("a")!.GetRawBytes();
        byte[] rawValue = value.GetTags().GetFirst("a")!.GetRawValueBytes();
        valueRaw[0] = (byte)'X';
        tagRaw[0] = (byte)'X';
        rawValue[0] = (byte)'X';

        Assert.That(Ascii(value.GetRawBytes()), Does.StartWith("v=1"));
        Assert.That(Ascii(value.GetTags().GetFirst("a")!.GetRawBytes()), Does.Contain("a=rsa"));
        Assert.That(Ascii(value.GetTags().GetFirst("a")!.GetRawValueBytes()), Is.EqualTo("rsa-sha256"));
    }

    static EmailDkimSignatureParseResult ParseDkim(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailDkimSignatureParser().ParseWithInputAndMode(Bytes(value), mode);
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
