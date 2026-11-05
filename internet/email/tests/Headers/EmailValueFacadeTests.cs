using System.Text;
using email;

namespace email.Tests;

public class EmailValueFacadeTests
{
    const string Dkim = "v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key";
    const string ArcMessage = "i=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=arc";
    const string ArcSeal = "i=1; a=rsa-sha256; b=YWJjZA==; cv=none; d=example.com; s=arc; t=123";

    [Test]
    public void ParserFacadeExposesEveryStructuredValueRoute()
    {
        Assert.That(new EmailValueParser().ParseMailbox(Bytes("Alice <alice@example.com>"), EmailAddressParseMode.Strict).HasMailbox(), Is.True);
        Assert.That(new EmailValueParser().ParseAddressGroup(Bytes("Friends: alice@example.com;"), EmailAddressParseMode.Strict).HasGroup(), Is.True);
        Assert.That(new EmailValueParser().ParseAddressListWithInputAndMode(Bytes("alice@example.com, bob@example.com"), EmailAddressParseMode.Strict).GetAddressList().GetCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseDateWithInputAndMode(Bytes("Fri, 21 Nov 1997 09:55:06 -0600"), EmailDateParseMode.Strict).GetValue()!.GetYear(), Is.EqualTo(1997));
        Assert.That(new EmailValueParser().ParseMessageIdWithInputAndMode(Bytes("<one@example.com>"), EmailMessageIdParseMode.Strict).HasSingleIdentifier(), Is.True);
        Assert.That(new EmailValueParser().ParseContentIdWithInputAndMode(Bytes("<part@example.com>"), EmailMessageIdParseMode.Strict).HasSingleIdentifier(), Is.True);
        Assert.That(new EmailValueParser().ParseReferencesWithInputAndMode(Bytes("<one@example.com> <two@example.com>"), EmailMessageIdParseMode.Strict).GetIdentifiers().GetCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseInReplyToWithInputAndMode(Bytes("<one@example.com> <two@example.com>"), EmailMessageIdParseMode.Strict).GetIdentifiers().GetCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseParametersWithInputAndMode(Bytes("charset=utf-8; format=flowed"), EmailStructuredValueParseMode.Strict).GetValue().GetCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("text/plain; charset=utf-8"), EmailStructuredValueParseMode.Strict).GetValue().IsType("text", "plain"), Is.True);
        Assert.That(new EmailValueParser().ParseDispositionWithInputAndMode(Bytes("attachment; filename=report.pdf"), EmailStructuredValueParseMode.Strict).GetValue()!.IsAttachment(), Is.True);
        Assert.That(new EmailValueParser().ParseContentLanguageWithInputAndMode(Bytes("en, fr"), EmailStructuredValueParseMode.Strict).GetValue().GetCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseReceivedWithInputAndMode(Bytes("from origin.example by mx.example; 1 Jan 2024 00:00 +0000"), EmailReceivedParseMode.Strict).GetValue()!.GetClauseCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseAuthenticationResultsWithInputAndMode(Bytes("mx.example; dkim=pass header.d=example.com"), EmailStructuredValueParseMode.Strict).GetValue()!.GetResultCount(), Is.EqualTo(1));
        Assert.That(new EmailValueParser().ParseReceivedSpfWithInputAndMode(Bytes("pass client-ip=192.0.2.1; envelope-from=sender@example.com"), EmailStructuredValueParseMode.Strict).GetValue()!.GetParameterCount(), Is.EqualTo(2));
        Assert.That(new EmailValueParser().ParseDkimSignatureWithInputAndMode(Bytes(Dkim), EmailStructuredValueParseMode.Strict).GetValue()!.GetSelector(), Is.EqualTo("key"));
        Assert.That(new EmailValueParser().ParseArcMessageSignatureWithInputAndMode(Bytes(ArcMessage), EmailStructuredValueParseMode.Strict).GetValue()!.GetInstance(), Is.EqualTo(1));
        Assert.That(new EmailValueParser().ParseArcSealWithInputAndMode(Bytes(ArcSeal), EmailStructuredValueParseMode.Strict).GetValue()!.GetChainStatus(), Is.EqualTo(EmailArcChainStatus.None));
        Assert.That(new EmailValueParser().ParseArcAuthenticationResultsWithInputAndMode(Bytes("i=1; mx.example; dkim=pass header.d=example.com"), EmailStructuredValueParseMode.Strict).GetValue()!.GetInstance(), Is.EqualTo(1));
    }

    [Test]
    public void FacadeRoutesStrictAndTolerantModesForEveryParserFamily()
    {
        AssertModes(
            new EmailValueParser().ParseAddressListWithInputAndMode(Bytes("alice@example.com,, bob@example.com"), EmailAddressParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseAddressListWithInputAndMode(Bytes("alice@example.com,, bob@example.com"), EmailAddressParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseDateWithInputAndMode(Bytes("21 Nov 97 09:55 GMT"), EmailDateParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseDateWithInputAndMode(Bytes("21 Nov 97 09:55 GMT"), EmailDateParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseMessageIdWithInputAndMode(Bytes("one@example.com"), EmailMessageIdParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseMessageIdWithInputAndMode(Bytes("one@example.com"), EmailMessageIdParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseContentIdWithInputAndMode(Bytes("part@example.com"), EmailMessageIdParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseContentIdWithInputAndMode(Bytes("part@example.com"), EmailMessageIdParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseReferencesWithInputAndMode(Bytes("one@example.com <two@example.com>"), EmailMessageIdParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseReferencesWithInputAndMode(Bytes("one@example.com <two@example.com>"), EmailMessageIdParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseInReplyToWithInputAndMode(Bytes("one@example.com <two@example.com>"), EmailMessageIdParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseInReplyToWithInputAndMode(Bytes("one@example.com <two@example.com>"), EmailMessageIdParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseParametersWithInputAndMode(Bytes("a=one; A=two"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseParametersWithInputAndMode(Bytes("a=one; A=two"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("text/plain; a=one; A=two"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("text/plain; a=one; A=two"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseDispositionWithInputAndMode(Bytes("; filename=x"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseDispositionWithInputAndMode(Bytes("; filename=x"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseContentLanguageWithInputAndMode(Bytes(", en,, fr,"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseContentLanguageWithInputAndMode(Bytes(", en,, fr,"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseReceivedWithInputAndMode(Bytes("by relay from origin; 1 Jan 2024 00:00 +0000"), EmailReceivedParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseReceivedWithInputAndMode(Bytes("by relay from origin; 1 Jan 2024 00:00 +0000"), EmailReceivedParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseAuthenticationResultsWithInputAndMode(Bytes("mx.example; spf pass; dkim=pass"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseAuthenticationResultsWithInputAndMode(Bytes("mx.example; spf pass; dkim=pass"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseReceivedSpfWithInputAndMode(Bytes("pass client-ip"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseReceivedSpfWithInputAndMode(Bytes("pass client-ip"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseDkimSignatureWithInputAndMode(Bytes("a=rsa-sha256; b=bad!"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseDkimSignatureWithInputAndMode(Bytes("a=rsa-sha256; b=bad!"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseArcMessageSignatureWithInputAndMode(Bytes("i=51; a=rsa-sha256"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseArcMessageSignatureWithInputAndMode(Bytes("i=51; a=rsa-sha256"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseArcSealWithInputAndMode(Bytes("i=1; a=rsa-sha256; h=from"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseArcSealWithInputAndMode(Bytes("i=1; a=rsa-sha256; h=from"), EmailStructuredValueParseMode.Strict).IsSuccess());
        AssertModes(
            new EmailValueParser().ParseArcAuthenticationResultsWithInputAndMode(Bytes("i=51; mx.example; dkim=pass"), EmailStructuredValueParseMode.Tolerant).IsSuccess(),
            new EmailValueParser().ParseArcAuthenticationResultsWithInputAndMode(Bytes("i=51; mx.example; dkim=pass"), EmailStructuredValueParseMode.Strict).IsSuccess());
    }

    [Test]
    public void WriterFacadeRoundTripsEverySupportedValueFamily()
    {
        EmailAddressParseResult mailbox = new EmailValueParser().ParseMailbox(Bytes("Alice <alice@example.com>"), EmailAddressParseMode.Strict);
        EmailAddressParseResult group = new EmailValueParser().ParseAddressGroup(Bytes("Friends: alice@example.com;"), EmailAddressParseMode.Strict);
        EmailAddressParseResult addresses = new EmailValueParser().ParseAddressListWithInputAndMode(Bytes("alice@example.com, bob@example.com"), EmailAddressParseMode.Strict);
        EmailDateParseResult date = new EmailValueParser().ParseDateWithInputAndMode(Bytes("Fri, 21 Nov 1997 09:55:06 -0600"), EmailDateParseMode.Strict);
        EmailMessageIdParseResult messageId = new EmailValueParser().ParseMessageIdWithInputAndMode(Bytes("<one@example.com>"), EmailMessageIdParseMode.Strict);
        EmailMessageIdParseResult identifiers = new EmailValueParser().ParseReferencesWithInputAndMode(Bytes("<one@example.com> <two@example.com>"), EmailMessageIdParseMode.Strict);
        EmailParameterParseResult parameters = new EmailValueParser().ParseParametersWithInputAndMode(Bytes("charset=utf-8; name=\"two words\""), EmailStructuredValueParseMode.Strict);
        EmailMediaTypeParseResult mediaType = new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("Text/Plain; Charset=utf-8; format=flowed"), EmailStructuredValueParseMode.Strict);
        EmailDispositionParseResult disposition = new EmailValueParser().ParseDispositionWithInputAndMode(Bytes("Attachment; filename=\"report final.pdf\""), EmailStructuredValueParseMode.Strict);
        EmailContentLanguageParseResult languages = new EmailValueParser().ParseContentLanguageWithInputAndMode(Bytes("EN, fr"), EmailStructuredValueParseMode.Strict);
        EmailReceivedParseResult received = new EmailValueParser().ParseReceivedWithInputAndMode(Bytes("from origin.example by mx.example; 1 Jan 2024 00:00 +0000"), EmailReceivedParseMode.Strict);
        EmailAuthenticationResultsParseResult authentication = new EmailValueParser().ParseAuthenticationResultsWithInputAndMode(Bytes("mx.example; dkim=pass header.d=example.com"), EmailStructuredValueParseMode.Strict);
        EmailReceivedSpfParseResult spf = new EmailValueParser().ParseReceivedSpfWithInputAndMode(Bytes("pass client-ip=192.0.2.1"), EmailStructuredValueParseMode.Strict);
        EmailDkimSignatureParseResult dkim = new EmailValueParser().ParseDkimSignatureWithInputAndMode(Bytes(Dkim), EmailStructuredValueParseMode.Strict);
        EmailArcMessageSignatureParseResult arcMessage = new EmailValueParser().ParseArcMessageSignatureWithInputAndMode(Bytes(ArcMessage), EmailStructuredValueParseMode.Strict);
        EmailArcSealParseResult arcSeal = new EmailValueParser().ParseArcSealWithInputAndMode(Bytes(ArcSeal), EmailStructuredValueParseMode.Strict);
        EmailArcAuthenticationResultsParseResult arcAuthentication = new EmailValueParser().ParseArcAuthenticationResultsWithInputAndMode(Bytes("i=1; mx.example; dkim=pass header.d=example.com"), EmailStructuredValueParseMode.Strict);

        Assert.That(new EmailValueParser().ParseMailbox(new EmailValueWriter().WriteAddressWithValue(mailbox.GetMailbox()!).GetBytes(), EmailAddressParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseAddressGroup(new EmailValueWriter().WriteAddressGroupWithValue(group.GetGroup()!).GetBytes(), EmailAddressParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseAddressListWithInputAndMode(new EmailValueWriter().WriteAddressListWithValue(addresses.GetAddressList()).GetBytes(), EmailAddressParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseDateWithInputAndMode(new EmailValueWriter().WriteDateWithValue(date.GetValue()!).GetBytes(), EmailDateParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseMessageIdWithInputAndMode(new EmailValueWriter().WriteMessageIdWithValue(messageId.GetIdentifier()!).GetBytes(), EmailMessageIdParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseContentIdWithInputAndMode(new EmailValueWriter().WriteContentIdWithValue(messageId.GetIdentifier()!).GetBytes(), EmailMessageIdParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseReferencesWithInputAndMode(new EmailValueWriter().WriteReferencesWithValue(identifiers.GetIdentifiers()).GetBytes(), EmailMessageIdParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseInReplyToWithInputAndMode(new EmailValueWriter().WriteInReplyToWithValue(identifiers.GetIdentifiers()).GetBytes(), EmailMessageIdParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseParametersWithInputAndMode(new EmailValueWriter().WriteParametersWithValue(parameters.GetValue()).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseMediaTypeWithInputAndMode(new EmailValueWriter().WriteMediaTypeWithValue(mediaType.GetValue()).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseDispositionWithInputAndMode(new EmailValueWriter().WriteDispositionWithValue(disposition.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseContentLanguageWithInputAndMode(new EmailValueWriter().WriteContentLanguageWithValue(languages.GetValue()).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseReceivedWithInputAndMode(new EmailValueWriter().WriteReceivedWithValue(received.GetValue()!).GetBytes(), EmailReceivedParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseAuthenticationResultsWithInputAndMode(new EmailValueWriter().WriteAuthenticationResultsWithValue(authentication.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseReceivedSpfWithInputAndMode(new EmailValueWriter().WriteReceivedSpfWithValue(spf.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseDkimSignatureWithInputAndMode(new EmailValueWriter().WriteDkimSignatureWithValue(dkim.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseArcMessageSignatureWithInputAndMode(new EmailValueWriter().WriteArcMessageSignatureWithValue(arcMessage.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseArcSealWithInputAndMode(new EmailValueWriter().WriteArcSealWithValue(arcSeal.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseArcAuthenticationResultsWithInputAndMode(new EmailValueWriter().WriteArcAuthenticationResultsWithValue(arcAuthentication.GetValue()!).GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
    }

    [Test]
    public void CanonicalMimeTraceAndLanguageWritingIsDeterministicAndFoldable()
    {
        EmailMediaType mediaType = new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("Text/Plain; Charset = \"utf-8\"; Name=\"a long file name.txt\""), EmailStructuredValueParseMode.Strict).GetValue();
        EmailDisposition disposition = new EmailValueParser().ParseDispositionWithInputAndMode(Bytes("Attachment; Filename=\"a long file name.txt\""), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailContentLanguage languages = new EmailValueParser().ParseContentLanguageWithInputAndMode(Bytes("EN-gb, FR"), EmailStructuredValueParseMode.Strict).GetValue();
        EmailReceived received = new EmailValueParser().ParseReceivedWithInputAndMode(Bytes("FROM origin.example BY relay.example WITH ESMTP; 1 Jan 2024 00:00 +0000"), EmailReceivedParseMode.Strict).GetValue()!;
        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetMaximumLineLength(30);

        EmailValueFormatResult media = new EmailValueWriter().WriteMediaTypeWithValueAndOptions(mediaType, options);
        EmailValueFormatResult dispositionText = new EmailValueWriter().WriteDispositionWithValueAndOptions(disposition!, options);
        EmailValueFormatResult languageText = new EmailValueWriter().WriteContentLanguageWithValueAndOptions(languages, options);
        EmailValueFormatResult receivedText = new EmailValueWriter().WriteReceivedWithValueAndOptions(received!, options);

        Assert.That(Ascii(media.GetBytes()), Is.EqualTo("text/plain; charset=utf-8;\r\n\tname=\"a long file name.txt\""));
        Assert.That(media.GetFoldCount(), Is.EqualTo(1));
        Assert.That(Ascii(dispositionText.GetBytes()), Is.EqualTo("attachment;\r\n\tfilename=\"a long file name.txt\""));
        Assert.That(Ascii(languageText.GetBytes()), Is.EqualTo("en-gb, fr"));
        Assert.That(Ascii(receivedText.GetBytes()), Does.StartWith("from origin.example"));
        Assert.That(Ascii(receivedText.GetBytes()), Does.Contain("\r\n\t"));
        Assert.That(new EmailValueParser().ParseMediaTypeWithInputAndMode(media.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseDispositionWithInputAndMode(dispositionText.GetBytes(), EmailStructuredValueParseMode.Strict).IsSuccess(), Is.True);
        Assert.That(new EmailValueParser().ParseReceivedWithInputAndMode(receivedText.GetBytes(), EmailReceivedParseMode.Strict).IsSuccess(), Is.True);
    }

    [Test]
    public void FacadeOptionOverloadsPreserveSourcePositionsAndLimits()
    {
        EmailAddressParserOptions addressOptions = new EmailAddressParserOptions();
        EmailDateParserOptions dateOptions = new EmailDateParserOptions();
        EmailStructuredValueParserOptions structuredOptions = new EmailStructuredValueParserOptions();
        EmailReceivedParserOptions receivedOptions = new EmailReceivedParserOptions();
        EmailAuthenticationParserOptions authenticationOptions = new EmailAuthenticationParserOptions();
        EmailSignatureParserOptions signatureOptions = new EmailSignatureParserOptions();
        EmailSourcePosition position = new EmailSourcePosition(100, 4, 7);

        Assert.That(new EmailValueParser().ParseAddressListWithInputAndOptionsAndInitialPosition(Bytes("a@example.com"), addressOptions, position).GetMailbox()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(new EmailValueParser().ParseDateWithInputAndOptionsAndInitialPosition(Bytes("1 Jan 2024 00:00 +0000"), dateOptions, position).GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(new EmailValueParser().ParseMediaTypeWithInputAndOptionsAndInitialPosition(Bytes("text/plain"), structuredOptions, position).GetValue().GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(new EmailValueParser().ParseReceivedWithInputAndOptionsAndInitialPosition(Bytes("from a by b; 1 Jan 2024 00:00 +0000"), receivedOptions, position).GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(new EmailValueParser().ParseAuthenticationResultsWithInputAndOptionsAndInitialPosition(Bytes("mx.example; none"), authenticationOptions, position).GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(new EmailValueParser().ParseDkimSignatureWithInputAndOptionsAndInitialPosition(Bytes(Dkim), signatureOptions, position).GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));

        structuredOptions.SetMaximumInputBytes(3);
        EmailMediaTypeParseResult limited = new EmailValueParser().ParseMediaTypeWithInputAndOptionsAndInitialPosition(Bytes("text/plain"), structuredOptions, position);
        Assert.That(limited.HasFatalError(), Is.True);
        Assert.That(limited.GetConsumedByteCount(), Is.Zero);
    }

    [Test]
    public void WriterRejectsInjectionAndOutputOverflowWithoutPartialBytes()
    {
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        EmailParameter unsafeParameter = new EmailParameter("filename", Bytes("bad\r\nInjected"), Bytes("bad\r\nInjected"), false, Bytes("filename=bad"), position);
        EmailParameterList unsafeList = new EmailParameterList(new List<EmailParameter> { unsafeParameter }, new List<EmailRfc2231DecodedParameter>());
        EmailValueFormatResult unsafeResult = new EmailValueWriter().WriteParametersWithValue(unsafeList);
        EmailMediaType mediaType = new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("text/plain; charset=utf-8"), EmailStructuredValueParseMode.Strict).GetValue();
        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetMaximumOutputBytes(10);
        EmailValueFormatResult limited = new EmailValueWriter().WriteMediaTypeWithValueAndOptions(mediaType, options);

        Assert.That(unsafeResult.HasFatalError(), Is.True);
        Assert.That(unsafeResult.GetIssues().ContainsCode(EmailIssueCode.ValueFormattingInvalid), Is.True);
        Assert.That(unsafeResult.GetByteCount(), Is.Zero);
        Assert.That(limited.HasFatalError(), Is.True);
        Assert.That(limited.GetIssues().ContainsCode(EmailIssueCode.ValueOutputLimitExceeded), Is.True);
        Assert.That(limited.GetByteCount(), Is.Zero);
    }

    [Test]
    public void UnifiedFormatResultsReturnDefensiveByteCopies()
    {
        EmailMediaType mediaType = new EmailValueParser().ParseMediaTypeWithInputAndMode(Bytes("text/plain"), EmailStructuredValueParseMode.Strict).GetValue();
        EmailValueFormatResult result = new EmailValueWriter().WriteMediaTypeWithValue(mediaType);
        byte[] first = result.GetBytes();
        first[0] = (byte)'X';

        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("text/plain"));
    }

    [Test]
    public void GeneralWriterOptionOverloadsMatchTheirTypedValues()
    {
        EmailValueParser parser = new EmailValueParser();
        EmailValueWriter writer = new EmailValueWriter();
        EmailAddressFormatOptions addressOptions = new EmailAddressFormatOptions();
        EmailMessageIdFormatOptions identifierOptions = new EmailMessageIdFormatOptions();
        EmailValueFormatOptions valueOptions = new EmailValueFormatOptions();

        EmailAddress mailbox = parser.ParseMailbox(Bytes("Alice <alice@example.com>"), EmailAddressParseMode.Strict).GetMailbox()!;
        EmailAddressGroup group = parser.ParseGroup(Bytes("Friends: alice@example.com;"), EmailAddressParseMode.Strict).GetGroup()!;
        EmailMessageIdList identifiers = parser.ParseReferencesWithInputAndMode(Bytes("<one@example.com> <two@example.com>"), EmailMessageIdParseMode.Strict).GetIdentifiers();
        EmailParameterList parameters = parser.ParseParametersWithInputAndMode(Bytes("charset=utf-8"), EmailStructuredValueParseMode.Strict).GetValue();

        Assert.That(writer.WriteAddressWithValueAndOptions(mailbox, addressOptions).IsSuccess(), Is.True);
        Assert.That(writer.WriteAddressGroupWithValueAndOptions(group, addressOptions).IsSuccess(), Is.True);
        Assert.That(writer.WriteMessageIdListWithValue(identifiers).IsSuccess(), Is.True);
        Assert.That(writer.WriteMessageIdListWithValueAndOptions(identifiers, identifierOptions).IsSuccess(), Is.True);
        Assert.That(writer.WriteReferencesWithValueAndOptions(identifiers, identifierOptions).IsSuccess(), Is.True);
        Assert.That(writer.WriteInReplyToWithValueAndOptions(identifiers, identifierOptions).IsSuccess(), Is.True);
        Assert.That(writer.WriteParametersWithValueAndOptions(parameters, valueOptions).IsSuccess(), Is.True);
    }

    static void AssertModes(bool tolerantSuccess, bool strictSuccess)
    {
        Assert.That(tolerantSuccess, Is.True);
        Assert.That(strictSuccess, Is.False);
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
