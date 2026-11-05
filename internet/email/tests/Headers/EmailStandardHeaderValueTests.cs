using System.Text;
using email;

namespace email.Tests;

public class EmailStandardHeaderValueTests
{
    [Test]
    public void ParsesAndCanonicallyWritesEveryNewStandardFamily()
    {
        EmailValueParser parser = new EmailValueParser();
        EmailValueWriter writer = new EmailValueWriter();

        EmailStandardHeaderValue returnPath = parser.ParseReturnPathWithInputAndMode(Bytes(" <bounce@example.test> "), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue mime = parser.ParseMimeVersionWithInputAndMode(Bytes(" 1.0 "), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue transfer = parser.ParseContentTransferEncodingWithInputAndMode(Bytes(" BaSe64 "), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue location = parser.ParseContentLocationWithInputAndMode(Bytes(" https://example.test/a%20b "), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue groups = parser.ParseNewsgroupsWithInputAndMode(Bytes("comp.mail.mime, comp.std.misc"), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue followup = parser.ParseFollowupToWithInputAndMode(Bytes("poster"), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue listId = parser.ParseListIdWithInputAndMode(Bytes("Example List <News.Example.COM>"), EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue trace = parser.ParseTraceWithFieldNameAndInputAndMode("Delivered-To", Bytes(" user@example.test "), EmailStructuredValueParseMode.Strict).GetValue()!;

        Assert.Multiple(() =>
        {
            Assert.That(returnPath.GetAddress()!.GetLocalPart(), Is.EqualTo("bounce"));
            Assert.That(Ascii(writer.WriteReturnPathWithValue(returnPath).GetBytes()), Is.EqualTo("<bounce@example.test>"));
            Assert.That(mime.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(mime.GetMinorVersion(), Is.Zero);
            Assert.That(Ascii(writer.WriteMimeVersionWithValue(mime).GetBytes()), Is.EqualTo("1.0"));
            Assert.That(transfer.HasKnownTransferEncoding(), Is.True);
            Assert.That(transfer.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.Base64));
            Assert.That(Ascii(writer.WriteContentTransferEncodingWithValue(transfer).GetBytes()), Is.EqualTo("base64"));
            Assert.That(Ascii(writer.WriteContentLocationWithValue(location).GetBytes()), Is.EqualTo("https://example.test/a%20b"));
            Assert.That(groups.GetAll(), Is.EqualTo(new[] { "comp.mail.mime", "comp.std.misc" }));
            Assert.That(Ascii(writer.WriteNewsgroupsWithValue(groups).GetBytes()), Is.EqualTo("comp.mail.mime, comp.std.misc"));
            Assert.That(Ascii(writer.WriteFollowupToWithValue(followup).GetBytes()), Is.EqualTo("poster"));
            Assert.That(listId.GetPrimaryValue(), Is.EqualTo("Example List"));
            Assert.That(listId.GetSecondaryValue(), Is.EqualTo("News.Example.COM"));
            Assert.That(Ascii(writer.WriteListIdWithValue(listId).GetBytes()), Is.EqualTo("Example List <news.example.com>"));
            Assert.That(trace.GetOriginalFieldName(), Is.EqualTo("Delivered-To"));
            Assert.That(Ascii(writer.WriteTraceWithValue(trace).GetBytes()), Is.EqualTo("user@example.test"));
        });
    }

    [Test]
    public void PreservesRawValuesAndSupportsNullAndExtensionForms()
    {
        EmailValueParser parser = new EmailValueParser();
        byte[] nullRaw = Bytes(" \t<> ");
        byte[] extensionRaw = Bytes(" X-Rot13 ");

        EmailStandardHeaderValue nullPath = parser.ParseReturnPathWithInputAndMode(nullRaw, EmailStructuredValueParseMode.Strict).GetValue()!;
        EmailStandardHeaderValue extension = parser.ParseContentTransferEncodingWithInputAndMode(extensionRaw, EmailStructuredValueParseMode.Strict).GetValue()!;

        nullRaw[0] = (byte)'X';
        extensionRaw[1] = (byte)'Y';
        Assert.That(nullPath.IsNullPath(), Is.True);
        Assert.That(nullPath.HasAddress(), Is.False);
        Assert.That(Ascii(nullPath.GetRawBytes()), Is.EqualTo(" \t<> "));
        Assert.That(extension.HasKnownTransferEncoding(), Is.False);
        Assert.That(extension.GetPrimaryValue(), Is.EqualTo("x-rot13"));
        Assert.That(Ascii(new EmailValueWriter().WriteContentTransferEncodingWithValue(extension).GetBytes()), Is.EqualTo("x-rot13"));
    }

    [TestCase("Return-Path", "bounce@example.test")]
    [TestCase("MIME-Version", "1")]
    [TestCase("Content-Transfer-Encoding", "base64; broken")]
    [TestCase("Content-Location", "https://example.test/a b")]
    [TestCase("Newsgroups", "one,,two")]
    [TestCase("Followup-To", ",")]
    [TestCase("List-ID", "list.example.test")]
    [TestCase("Delivered-To", "")]
    public void StrictRejectsButTolerantRetainsMalformedValues(string fieldName, string text)
    {
        EmailStandardHeaderParseResult strict = new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode(fieldName, Bytes(text), EmailStructuredValueParseMode.Strict);
        EmailStandardHeaderParseResult tolerant = new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode(fieldName, Bytes(text), EmailStructuredValueParseMode.Tolerant);

        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.GetValue(), Is.Not.Null);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.MetadataValueMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.GetValue(), Is.Not.Null);
        Assert.That(tolerant.GetValue()!.IsMalformed(), Is.True);
    }

    [Test]
    public void OptionsEnforceLimitsAndPreserveSourcePosition()
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        EmailSourcePosition position = new EmailSourcePosition(100, 5, 9);
        EmailStandardHeaderParseResult positioned = new EmailValueParser().ParseListIdWithInputAndOptionsAndInitialPosition(Bytes("<list.example.test>"), options, position);

        options.SetMaximumInputBytes(3);
        EmailStandardHeaderParseResult limited = new EmailValueParser().ParseMimeVersionWithInputAndOptionsAndInitialPosition(Bytes("1.0 extra"), options, position);

        Assert.That(positioned.GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(limited.HasFatalError(), Is.True);
        Assert.That(limited.GetConsumedByteCount(), Is.Zero);
        Assert.That(limited.GetSourceByteCount(), Is.EqualTo(9));
    }

    [Test]
    public void RegistryKindsDriveLazyParsingForAllNewFamilies()
    {
        foreach ((string name, string value, Type type) in new[]
        {
            ("Return-Path", "<a@example.test>", typeof(EmailStandardHeaderValue)),
            ("MIME-Version", "1.0", typeof(EmailStandardHeaderValue)),
            ("Content-Transfer-Encoding", "quoted-printable", typeof(EmailStandardHeaderValue)),
            ("Content-Location", "cid:part@example.test", typeof(EmailStandardHeaderValue)),
            ("Newsgroups", "comp.mail.mime", typeof(EmailStandardHeaderValue)),
            ("Followup-To", "poster", typeof(EmailStandardHeaderValue)),
            ("List-ID", "<list.example.test>", typeof(EmailStandardHeaderValue)),
            ("X-Originating-IP", "[192.0.2.1]", typeof(EmailStandardHeaderValue))
        })
        {
            EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Bytes(value), new EmailHeaderRegistry().GetValueKind(name));
            Assert.That(header.GetStructuredValue(), Is.TypeOf(type), name);
        }
    }

    [Test]
    public void MetadataParserAndWriterFacadeRoundTripsExistingFamilies()
    {
        EmailValueParser parser = new EmailValueParser();
        EmailValueWriter writer = new EmailValueWriter();
        EmailKeywordList keywords = parser.ParseKeywordsWithInputAndMode(Bytes("alpha, beta"), EmailStructuredValueParseMode.Strict).GetKeywords()!;
        EmailPriority priority = parser.ParsePriorityWithInputAndMode(Bytes("Urgent"), EmailStructuredValueParseMode.Strict).GetPriority()!;
        EmailPriority xPriority = parser.ParseXPriorityWithInputAndMode(Bytes("2"), EmailStructuredValueParseMode.Strict).GetPriority()!;
        EmailImportance importance = parser.ParseImportanceWithInputAndMode(Bytes("High"), EmailStructuredValueParseMode.Strict).GetImportance()!;
        EmailSensitivity sensitivity = parser.ParseSensitivityWithInputAndMode(Bytes("Company-Confidential"), EmailStructuredValueParseMode.Strict).GetSensitivity()!;
        EmailAutoSubmitted autoSubmitted = parser.ParseAutoSubmittedWithInputAndMode(Bytes("auto-replied; owner=bot"), EmailStructuredValueParseMode.Strict).GetAutoSubmitted()!;
        EmailListManagementValue list = parser.ParseListManagementWithInputAndKindAndMode(Bytes("<https://example.test/help>, <mailto:help@example.test>"), EmailListManagementKind.Help, EmailStructuredValueParseMode.Strict).GetListManagement()!;
        EmailListUnsubscribePost oneClick = parser.ParseListUnsubscribePostWithInputAndMode(Bytes("List-Unsubscribe=One-Click"), EmailStructuredValueParseMode.Strict).GetListUnsubscribePost()!;
        EmailResentField resent = parser.ParseResentFieldWithFieldNameAndInputAndMode("Resent-Message-ID", Bytes("<resent@example.test>"), EmailStructuredValueParseMode.Strict).GetResentField()!;

        Assert.Multiple(() =>
        {
            Assert.That(Ascii(writer.WriteKeywordsWithValue(keywords).GetBytes()), Is.EqualTo("alpha, beta"));
            Assert.That(Ascii(writer.WritePriorityWithValue(priority).GetBytes()), Is.EqualTo("urgent"));
            Assert.That(Ascii(writer.WritePriorityWithValue(xPriority).GetBytes()), Is.EqualTo("2"));
            Assert.That(Ascii(writer.WriteImportanceWithValue(importance).GetBytes()), Is.EqualTo("high"));
            Assert.That(Ascii(writer.WriteSensitivityWithValue(sensitivity).GetBytes()), Is.EqualTo("company-confidential"));
            Assert.That(Ascii(writer.WriteAutoSubmittedWithValue(autoSubmitted).GetBytes()), Is.EqualTo("auto-replied; owner=bot"));
            Assert.That(Ascii(writer.WriteListManagementWithValue(list).GetBytes()), Is.EqualTo("<https://example.test/help>, <mailto:help@example.test>"));
            Assert.That(Ascii(writer.WriteListUnsubscribePostWithValue(oneClick).GetBytes()), Is.EqualTo("List-Unsubscribe=One-Click"));
            Assert.That(Ascii(writer.WriteResentFieldWithValue(resent).GetBytes()), Is.EqualTo("<resent@example.test>"));
        });
    }

    [Test]
    public void WritersRejectWrongFamiliesInjectionAndOutputOverflow()
    {
        EmailStandardHeaderValue location = new EmailValueParser().ParseContentLocationWithInputAndMode(Bytes("cid:part"), EmailStructuredValueParseMode.Strict).GetValue()!;
        Assert.Throws<ArgumentException>(() => new EmailValueWriter().WriteReturnPathWithValue(location));

        EmailStandardHeaderValue unsafeTrace = new EmailStandardHeaderValue(EmailStandardHeaderValueKind.Trace, "Delivered-To", "safe\r\nInjected", null, new List<string>(), null, false, 0, 0, false, EmailTransferEncodingKind.SevenBit, Bytes("safe"), new EmailSourcePosition(0, 1, 1), true);
        Assert.That(new EmailValueWriter().WriteTraceWithValue(unsafeTrace).HasFatalError(), Is.True);

        EmailValueFormatOptions options = new EmailValueFormatOptions();
        options.SetMaximumOutputBytes(3);
        Assert.That(new EmailValueWriter().WriteContentLocationWithValueAndOptions(location, options).HasFatalError(), Is.True);
    }

    [Test]
    public void InvalidParserArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode(null!, Bytes("x"), EmailStructuredValueParseMode.Strict));
        Assert.Throws<ArgumentException>(() => new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("X-Unknown", Bytes("x"), EmailStructuredValueParseMode.Strict));
        EmailStandardHeaderValue groups = new EmailValueParser().ParseNewsgroupsWithInputAndMode(Bytes("one"), EmailStructuredValueParseMode.Strict).GetValue()!;
        Assert.Throws<ArgumentOutOfRangeException>(() => groups.GetAt(1));
    }

    [Test]
    public void FacadeOptionOverloadsCoverEveryStandardAndMetadataFamily()
    {
        EmailValueParser parser = new EmailValueParser();
        EmailValueWriter writer = new EmailValueWriter();
        EmailStructuredValueParserOptions structured = new EmailStructuredValueParserOptions();
        EmailMetadataParserOptions metadata = new EmailMetadataParserOptions();
        EmailValueFormatOptions format = new EmailValueFormatOptions();
        EmailSourcePosition position = new EmailSourcePosition(40, 3, 5);

        EmailStandardHeaderValue returnPath = parser.ParseReturnPathWithInputAndOptionsAndInitialPosition(Bytes("<a@example.test>"), structured, position).GetValue()!;
        EmailStandardHeaderValue mimeVersion = parser.ParseMimeVersionWithInputAndOptionsAndInitialPosition(Bytes("1.0"), structured, position).GetValue()!;
        EmailStandardHeaderValue transfer = parser.ParseContentTransferEncodingWithInputAndOptionsAndInitialPosition(Bytes("base64"), structured, position).GetValue()!;
        EmailStandardHeaderValue location = parser.ParseContentLocationWithInputAndOptionsAndInitialPosition(Bytes("cid:part"), structured, position).GetValue()!;
        EmailStandardHeaderValue groups = parser.ParseNewsgroupsWithInputAndOptionsAndInitialPosition(Bytes("comp.mail.mime"), structured, position).GetValue()!;
        EmailStandardHeaderValue followup = parser.ParseFollowupToWithInputAndOptionsAndInitialPosition(Bytes("poster"), structured, position).GetValue()!;
        EmailStandardHeaderValue listId = parser.ParseListIdWithInputAndOptionsAndInitialPosition(Bytes("<list.example.test>"), structured, position).GetValue()!;
        EmailStandardHeaderValue trace = parser.ParseTraceWithFieldNameAndInputAndOptionsAndInitialPosition("X-Originating-IP", Bytes("[192.0.2.1]"), structured, position).GetValue()!;

        EmailKeywordList keywords = parser.ParseKeywordsWithInputAndOptionsAndInitialPosition(Bytes("alpha, beta"), metadata, position).GetKeywords()!;
        EmailPriority priority = parser.ParsePriorityWithInputAndOptionsAndInitialPosition(Bytes("urgent"), metadata, position).GetPriority()!;
        EmailPriority xPriority = parser.ParseXPriorityWithInputAndOptionsAndInitialPosition(Bytes("2"), metadata, position).GetPriority()!;
        EmailImportance importance = parser.ParseImportanceWithInputAndOptionsAndInitialPosition(Bytes("high"), metadata, position).GetImportance()!;
        EmailSensitivity sensitivity = parser.ParseSensitivityWithInputAndOptionsAndInitialPosition(Bytes("private"), metadata, position).GetSensitivity()!;
        EmailAutoSubmitted submitted = parser.ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(Bytes("auto-generated"), metadata, position).GetAutoSubmitted()!;
        EmailListManagementValue management = parser.ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(Bytes("<mailto:list@example.test>"), EmailListManagementKind.Post, metadata, position).GetListManagement()!;
        EmailListUnsubscribePost oneClick = parser.ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(Bytes("List-Unsubscribe=One-Click"), metadata, position).GetListUnsubscribePost()!;
        EmailResentField resent = parser.ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition("Resent-Message-ID", Bytes("<one@example.test>"), metadata, position).GetResentField()!;

        Assert.Multiple(() =>
        {
            Assert.That(returnPath.GetSourcePosition().GetByteOffset(), Is.EqualTo(40));
            Assert.That(writer.WriteStandardHeaderWithValue(returnPath).IsSuccess(), Is.True);
            Assert.That(writer.WriteStandardHeaderWithValueAndOptions(returnPath, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteReturnPathWithValueAndOptions(returnPath, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteMimeVersionWithValueAndOptions(mimeVersion, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteContentTransferEncodingWithValueAndOptions(transfer, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteContentLocationWithValueAndOptions(location, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteNewsgroupsWithValueAndOptions(groups, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteFollowupToWithValueAndOptions(followup, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteListIdWithValueAndOptions(listId, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteTraceWithValueAndOptions(trace, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteKeywordsWithValueAndOptions(keywords, format).IsSuccess(), Is.True);
            Assert.That(writer.WritePriorityWithValueAndOptions(priority, format).IsSuccess(), Is.True);
            Assert.That(writer.WritePriorityWithValueAndOptions(xPriority, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteImportanceWithValueAndOptions(importance, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteSensitivityWithValueAndOptions(sensitivity, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteAutoSubmittedWithValueAndOptions(submitted, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteListManagementWithValueAndOptions(management, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteListUnsubscribePostWithValueAndOptions(oneClick, format).IsSuccess(), Is.True);
            Assert.That(writer.WriteResentFieldWithValueAndOptions(resent, format).IsSuccess(), Is.True);
        });
    }

    static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
    static string Ascii(byte[] value) => Encoding.UTF8.GetString(value);
}
