using System.Text;
using email;

namespace email.Tests;

public class EmailPublicLayerCoverageAuditTests
{
    [Test]
    public void AuthenticationCollectionsAndRawValuesAreIndependentSnapshots()
    {
        byte[] input = Bytes("mx.example; spf=pass smtp.mailfrom=\"user@example.com\"");
        EmailAuthenticationResultsParseResult parsed = new EmailAuthenticationResultsParser().ParseWithInputAndMode(input, EmailStructuredValueParseMode.Strict);
        EmailAuthenticationMethodResult method = parsed.GetValue()!.GetResult(0);
        List<EmailAuthenticationProperty> properties = method.GetProperties();
        EmailAuthenticationProperty property = properties[0];

        Assert.That(property.WasQuoted(), Is.True);
        Assert.That(Text(property.GetRawValueBytes()), Is.EqualTo("\"user@example.com\""));
        properties.Clear();
        Assert.That(method.GetPropertyCount(), Is.EqualTo(1));

        byte[] rawValue = property.GetRawValueBytes();
        rawValue[0] = (byte)'x';
        Assert.That(Text(property.GetRawValueBytes()), Is.EqualTo("\"user@example.com\""));
    }

    [Test]
    public void ValueFacadeOptionOverloadsParseAndFormatAuthenticationHeaders()
    {
        byte[] spfInput = Bytes("pass client-ip=192.0.2.1;");
        byte[] dkimInput = Bytes("v=1; a=rsa-sha256; d=example.com; s=selector; h=from; bh=YWJjZA==; b=ZWZnaA==");
        byte[] messageSignatureInput = Bytes("i=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=arc");
        byte[] sealInput = Bytes("i=1; a=rsa-sha256; b=YWJjZA==; cv=none; d=example.com; s=arc; t=1");
        byte[] authenticationInput = Bytes("i=1; mx.example; dkim=pass header.d=example.com");
        EmailValueParser parser = new EmailValueParser();
        EmailSourcePosition position = new EmailSourcePosition(12, 2, 3);

        EmailReceivedSpf receivedSpf = parser.ParseReceivedSpfWithInputAndOptionsAndInitialPosition(spfInput, new EmailAuthenticationParserOptions(), position).GetValue()!;
        EmailDkimSignature dkim = parser.ParseDkimSignatureWithInputAndOptionsAndInitialPosition(dkimInput, new EmailSignatureParserOptions(), position).GetValue()!;
        EmailDkimSignature messageSignature = parser.ParseArcMessageSignatureWithInputAndOptionsAndInitialPosition(messageSignatureInput, new EmailSignatureParserOptions(), position).GetValue()!;
        EmailArcSeal seal = parser.ParseArcSealWithInputAndOptionsAndInitialPosition(sealInput, new EmailSignatureParserOptions(), position).GetValue()!;
        EmailArcAuthenticationResults authentication = parser.ParseArcAuthenticationResultsWithInputAndOptionsAndInitialPosition(authenticationInput, new EmailSignatureParserOptions(), position).GetValue()!;

        Assert.That(receivedSpf.GetSourcePosition().GetByteOffset(), Is.EqualTo(12));
        Assert.That(messageSignature.GetTags().GetRawBytes(), Is.EqualTo(messageSignatureInput));
        Assert.That(seal.GetSourcePosition().GetLineNumber(), Is.EqualTo(2));
        Assert.That(authentication.GetSourcePosition().GetColumnNumber(), Is.EqualTo(3));

        EmailValueWriter writer = new EmailValueWriter();
        Assert.That(writer.WriteReceivedSpfWithValueAndOptions(receivedSpf, new EmailAuthenticationFormatOptions()).IsSuccess(), Is.True);
        Assert.That(writer.WriteDkimSignatureWithValueAndOptions(dkim, new EmailSignatureFormatOptions()).IsSuccess(), Is.True);
        Assert.That(writer.WriteArcMessageSignatureWithValueAndOptions(messageSignature, new EmailSignatureFormatOptions()).IsSuccess(), Is.True);
        Assert.That(writer.WriteArcSealWithValueAndOptions(seal, new EmailSignatureFormatOptions()).IsSuccess(), Is.True);
        Assert.That(writer.WriteArcAuthenticationResultsWithValueAndOptions(authentication, new EmailSignatureFormatOptions()).IsSuccess(), Is.True);
        Assert.Throws<ArgumentException>(() => writer.WriteArcMessageSignatureWithValueAndOptions(dkim, new EmailSignatureFormatOptions()));
    }

    [Test]
    public void HeaderWriterOptionOverloadsRespectSeparatorPolicyAndValidateArguments()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("X-Case: value\r\n\r\nbody"));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetIncludeSeparator(false);
        EmailHeaderWriter writer = new EmailHeaderWriter();

        EmailHeaderWriteResult headers = writer.WriteRawWithHeadersAndOptions(block.GetHeaders(), options);
        EmailHeaderWriteResult rawBlock = writer.WriteRawWithBlockAndOptions(block, options);
        EmailHeaderWriteResult preservedBlock = writer.WritePreservingWithBlockAndOptions(block, options);

        Assert.That(Text(headers.GetBytes()), Is.EqualTo("X-Case: value\r\n"));
        Assert.That(Text(rawBlock.GetBytes()), Is.EqualTo("X-Case: value\r\n"));
        Assert.That(Text(preservedBlock.GetBytes()), Is.EqualTo("X-Case: value\r\n"));
        Assert.That(rawBlock.GetMode(), Is.EqualTo(EmailHeaderWriteMode.Raw));
        Assert.That(preservedBlock.GetMode(), Is.EqualTo(EmailHeaderWriteMode.Preserving));
        Assert.Throws<ArgumentNullException>(() => writer.WriteRawWithHeadersAndOptions(block.GetHeaders(), null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteRawWithBlockAndOptions(null!, options));
        Assert.Throws<ArgumentNullException>(() => writer.WritePreservingWithBlockAndOptions(block, null!));
    }

    [Test]
    public void LegacyCompleteEntityFactoryPreservesSuppliedMetadata()
    {
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        EmailParameterList parameters = new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
        EmailMediaType mediaType = new EmailMediaType("text", "plain", parameters, Bytes("text/plain"), position, false);
        EmailDisposition disposition = new EmailDisposition("inline", parameters, Bytes("inline"), position);
        EmailMessageId contentId = new EmailMessageId("part", "example.com", false, Bytes("<part@example.com>"), position, false, false);
        EmailContentLanguage language = new EmailContentLanguage(new List<string> { "en" }, Bytes("en"), position);
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(Bytes("body"), false);

        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(
            MimeEntityKind.Leaf,
            new EmailHeadersOperations().CreateWith(),
            mediaType,
            disposition,
            "7bit",
            contentId,
            "https://example.test/part",
            language,
            "description",
            null!,
            null!,
            content,
            null!,
            new List<MimeEntity>(),
            null!,
            null!,
            null!,
            new EmailRange(0, 4),
            new EmailIssueList());

        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(entity.GetMediaType(), Is.SameAs(mediaType));
        Assert.That(entity.GetDisposition(), Is.SameAs(disposition));
        Assert.That(entity.GetContentId(), Is.SameAs(contentId));
        Assert.That(entity.GetContentLanguage(), Is.SameAs(language));
        Assert.That(entity.GetRawContent(), Is.SameAs(content));
        Assert.That(entity.GetContentDescription(), Is.EqualTo("description"));
        Assert.That(entity.GetContentLocation(), Is.EqualTo("https://example.test/part"));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
