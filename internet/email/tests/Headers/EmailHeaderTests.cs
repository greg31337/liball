using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderTests
{
    [Test]
    public void PreservesOriginalNameValueFoldingLineEndingsAndRange()
    {
        const string rawValue = " first\r\n\tsecond\n third\r fourth";
        EmailRange range = new EmailRange(120, 48);
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HeaderMalformed, "retained defect"));
        EmailHeader header = CreateFull("X-CuStOm", rawValue, EmailLineEnding.Lf, range, new EmailSourcePosition(129, 4, 10), EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Tolerant, issues);

        Assert.That(header.GetName(), Is.EqualTo("X-CuStOm"));
        Assert.That(header.GetNormalizedName(), Is.EqualTo("x-custom"));
        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(rawValue));
        Assert.That(Ascii(header.GetOriginalRawValue()), Is.EqualTo(rawValue));
        Assert.That(Ascii(header.GetUnfoldedValue()), Is.EqualTo(" first\tsecond third fourth"));
        Assert.That(header.GetOriginalFoldCount(), Is.EqualTo(3));
        Assert.That(header.GetFolds(), Has.Count.EqualTo(3));
        Assert.That(header.GetFold(0).GetRawValueOffset(), Is.EqualTo(6));
        Assert.That(header.GetFold(0).GetLineEnding(), Is.EqualTo(EmailLineEnding.CrLf));
        Assert.That(header.GetFold(0).GetWhitespaceByteCount(), Is.EqualTo(1));
        Assert.That(header.GetFold(0).IsFold(), Is.True);
        Assert.That(new EmailHeaderFold(0, EmailLineEnding.CrLf, 0).IsFold(), Is.False);
        Assert.That(header.GetFold(0).IsCanonical(), Is.True);
        Assert.That(header.GetFold(1).GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(header.GetFold(1).IsCanonical(), Is.False);
        Assert.That(header.GetFold(2).GetLineEnding(), Is.EqualTo(EmailLineEnding.Cr));
        Assert.That(header.GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(header.GetOriginalLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(header.HasSourceRange(), Is.True);
        Assert.That(header.GetSourceRange()!.EqualsRange(range), Is.True);
        Assert.That(header.GetValueSourcePosition()!.GetByteOffset(), Is.EqualTo(129));
        Assert.That(Ascii(header.GetOriginalRawFieldBytes()), Is.EqualTo("X-CuStOm:" + rawValue + "\n"));
        Assert.That(header.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(header.IsDirty(), Is.False);

        header.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.ValidationFailed, "additional"));
        Assert.That(header.GetIssues().GetCount(), Is.EqualTo(2));
    }

    [Test]
    public void LazilyDecodesEncodedWordsAndCachesUnicodeValueAndIssues()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" =?utf-8?Q?Hello_=E2=9C=93?=\r\n\t=?iso-8859-1?Q?_caf=E9?= "), EmailHeaderValueKind.Unstructured);

        Assert.That(header.HasDecodedValueCache(), Is.False);
        string first = header.GetDecodedValue()!;
        int issueCount = header.GetIssues().GetCount();
        string second = header.GetDecodedValue()!;

        Assert.That(first, Is.EqualTo("Hello ✓ café"));
        Assert.That(second, Is.SameAs(first));
        Assert.That(header.HasDecodedValueCache(), Is.True);
        Assert.That(header.GetIssues().GetCount(), Is.EqualTo(issueCount));
    }

    [Test]
    public void DecodingIssuesAreAnchoredAtTheHeaderValueSourcePosition()
    {
        EmailHeader header = CreateFull("Subject", " =?utf-8?Q?bad=GZ?=", EmailLineEnding.CrLf, new EmailRange(90, 34), new EmailSourcePosition(100, 3, 5), EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Strict, new EmailIssueList());

        Assert.That(header.GetDecodedValue(), Is.EqualTo("bad=GZ"));

        EmailIssue issue = header.GetIssues().GetAt(0);
        Assert.That(issue.GetCode(), Is.EqualTo(EmailIssueCode.EncodedWordInvalidPayload));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(101));
        Assert.That(issue.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(3));
        Assert.That(issue.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(6));
    }

    [Test]
    public void LazilyParsesAndCachesTypedMediaValue()
    {
        EmailHeader header = CreateFull("Content-Type", " Text/Plain;\r\n\tCharset=utf-8", EmailLineEnding.CrLf, new EmailRange(190, 48), new EmailSourcePosition(203, 5, 14), EmailHeaderValueKind.MediaType, EmailStructuredValueParseMode.Strict, new EmailIssueList());

        Assert.That(header.HasStructuredValueCache(), Is.False);
        object first = header.GetStructuredValue()!;
        object second = header.GetStructuredValue()!;

        Assert.That(first, Is.TypeOf<EmailMediaType>());
        Assert.That(second, Is.SameAs(first));
        Assert.That(((EmailMediaType)first).IsType("text", "plain"), Is.True);
        Assert.That(Ascii(((EmailMediaType)first).GetParameters().GetFirst("charset")!.GetValueBytes()), Is.EqualTo("utf-8"));
        Assert.That(header.HasStructuredValueCache(), Is.True);
        Assert.That(header.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void TypedCacheRoutesEveryStructuredParserFamily()
    {
        EmailHeader mailbox = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("From", Bytes(" Alice <alice@example.com>"), EmailHeaderValueKind.Mailbox);
        EmailHeader date = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Date", Bytes(" 1 Jan 2024 00:00 +0000"), EmailHeaderValueKind.Date);
        EmailHeader identifier = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Message-ID", Bytes(" <one@example.com>"), EmailHeaderValueKind.MessageId);
        EmailHeader parameters = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("X-Parameters", Bytes(" one=1; two=2"), EmailHeaderValueKind.Parameters);
        EmailHeader received = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Received", Bytes(" from a by b; 1 Jan 2024 00:00 +0000"), EmailHeaderValueKind.Received);
        EmailHeader authentication = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Authentication-Results", Bytes(" mx.example; dkim=pass header.d=example.com"), EmailHeaderValueKind.AuthenticationResults);
        EmailHeader signature = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("DKIM-Signature", Bytes(" v=1; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=key"), EmailHeaderValueKind.DkimSignature);
        EmailHeader keywords = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Keywords", Bytes(" project, release"), EmailHeaderValueKind.Keywords);
        EmailHeader listPost = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("List-Post", Bytes(" <mailto:list@example.com>"), EmailHeaderValueKind.ListPost);
        EmailHeader resent = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Resent-Message-ID", Bytes(" <resent@example.com>"), EmailHeaderValueKind.ResentField);

        Assert.That(mailbox.GetStructuredValue(), Is.TypeOf<EmailAddress>());
        Assert.That(date.GetStructuredValue(), Is.TypeOf<EmailDateTime>());
        Assert.That(identifier.GetStructuredValue(), Is.TypeOf<EmailMessageId>());
        Assert.That(parameters.GetStructuredValue(), Is.TypeOf<EmailParameterList>());
        Assert.That(received.GetStructuredValue(), Is.TypeOf<EmailReceived>());
        Assert.That(authentication.GetStructuredValue(), Is.TypeOf<EmailAuthenticationResults>());
        Assert.That(signature.GetStructuredValue(), Is.TypeOf<EmailDkimSignature>());
        Assert.That(keywords.GetStructuredValue(), Is.TypeOf<EmailKeywordList>());
        Assert.That(listPost.GetStructuredValue(), Is.TypeOf<EmailListManagementValue>());
        Assert.That(resent.GetStructuredValue(), Is.TypeOf<EmailResentField>());
    }

    [Test]
    public void StrictAndTolerantTypedIssuesAreCachedWithoutDuplication()
    {
        EmailHeader header = CreateFull("Content-Language", ", en,, fr,", EmailLineEnding.CrLf, null!, new EmailSourcePosition(50, 3, 8), EmailHeaderValueKind.ContentLanguage, EmailStructuredValueParseMode.Strict, new EmailIssueList());

        object strict = header.GetStructuredValue()!;
        int strictCount = header.GetIssues().GetCount();
        object repeated = header.GetStructuredValue()!;

        Assert.That(strict, Is.TypeOf<EmailContentLanguage>());
        Assert.That(repeated, Is.SameAs(strict));
        Assert.That(header.GetIssues().ContainsCode(EmailIssueCode.ContentLanguageMalformed), Is.True);
        Assert.That(header.GetIssues().HasSeverityAtLeast(EmailIssueSeverity.Error), Is.True);
        Assert.That(header.GetIssues().GetCount(), Is.EqualTo(strictCount));

        header.SetParseMode(EmailStructuredValueParseMode.Tolerant);
        Assert.That(header.HasStructuredValueCache(), Is.False);
        Assert.That(header.GetIssues().GetCount(), Is.Zero);
        Assert.That(header.GetStructuredValue(), Is.TypeOf<EmailContentLanguage>());
        Assert.That(header.GetIssues().HasSeverityAtLeast(EmailIssueSeverity.Error), Is.False);
    }

    [Test]
    public void ValueMutationInvalidatesLazyCachesAndTracksDirtyState()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", Bytes(" text/plain"), EmailHeaderValueKind.MediaType);
        EmailMediaType first = (EmailMediaType)header.GetStructuredValue()!;
        header.GetDecodedValue();

        header.SetValue(" application/json");

        Assert.That(header.GetDirtyState(), Is.EqualTo(EmailHeaderDirtyState.ValueChanged));
        Assert.That(header.IsValueDirty(), Is.True);
        Assert.That(header.HasDecodedValueCache(), Is.False);
        Assert.That(header.HasStructuredValueCache(), Is.False);
        EmailMediaType second = (EmailMediaType)header.GetStructuredValue()!;
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.IsType("application", "json"), Is.True);
        Assert.That(Ascii(header.GetOriginalRawValue()), Is.EqualTo(" text/plain"));
        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" application/json"));

        header.SetRawValue(Bytes(" text/plain"));
        Assert.That(header.GetDirtyState(), Is.EqualTo(EmailHeaderDirtyState.Clean));
    }

    [Test]
    public void NameAndLineEndingMutationsTrackIndependentlyAndResetExactly()
    {
        EmailHeader header = CreateFull("Subject", " original", EmailLineEnding.Lf, new EmailRange(20, 18), new EmailSourcePosition(28, 2, 9), EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());

        header.SetName("X-Subject");
        Assert.That(header.GetDirtyState(), Is.EqualTo(EmailHeaderDirtyState.NameChanged));
        header.SetLineEnding(EmailLineEnding.CrLf);
        Assert.That(header.GetDirtyState(), Is.EqualTo(EmailHeaderDirtyState.MultipleChanges));
        Assert.That(header.IsNameDirty(), Is.True);
        Assert.That(header.IsLineEndingDirty(), Is.True);
        Assert.That(Ascii(header.GetRawFieldBytes()), Is.EqualTo("X-Subject: original\r\n"));
        Assert.That(Ascii(header.GetOriginalRawFieldBytes()), Is.EqualTo("Subject: original\n"));

        header.ResetToOriginal();

        Assert.That(header.GetName(), Is.EqualTo("Subject"));
        Assert.That(header.GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(header.IsDirty(), Is.False);
        Assert.That(header.HasSourceRange(), Is.True);
    }

    [Test]
    public void AcceptChangesCreatesANewCleanBaselineAndDetachesSourceRange()
    {
        EmailHeader header = CreateFull("Subject", " old", EmailLineEnding.CrLf, new EmailRange(100, 14), new EmailSourcePosition(108, 1, 9), EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
        header.SetName("X-Subject");
        header.SetValue(" new");
        header.SetLineEnding(EmailLineEnding.Lf);

        header.AcceptChanges();

        Assert.That(header.IsDirty(), Is.False);
        Assert.That(header.HasSourceRange(), Is.False);
        Assert.That(header.GetOriginalName(), Is.EqualTo("X-Subject"));
        Assert.That(Ascii(header.GetOriginalRawValue()), Is.EqualTo(" new"));
        Assert.That(header.GetOriginalLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(Ascii(header.GetOriginalRawFieldBytes()), Is.EqualTo("X-Subject: new\n"));
    }

    [Test]
    public void MutableInputsRejectHeaderInjectionButAllowCanonicalFolding()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValue("Subject", Bytes(" value"));

        Assert.Throws<ArgumentException>(() => header.SetName("Bad:Name"));
        Assert.Throws<ArgumentException>(() => header.SetName("Bad\r\nName"));
        Assert.Throws<ArgumentException>(() => header.SetValue("safe\r\nInjected: yes"));
        Assert.Throws<ArgumentException>(() => header.SetRawValue(Bytes("safe\n injected")));
        Assert.Throws<ArgumentException>(() => header.SetRawValue(Bytes("safe\r\nInjected")));
        Assert.Throws<ArgumentException>(() => header.SetRawValue(new byte[] { 32, 0, 88 }));

        header.SetRawValue(Bytes(" first\r\n\tsecond"));
        Assert.That(header.GetFoldCount(), Is.EqualTo(1));
        Assert.That(header.GetFold(0).IsCanonical(), Is.True);
        Assert.That(Ascii(header.GetUnfoldedValue()), Is.EqualTo(" first\tsecond"));
    }

    [Test]
    public void RawArraysAndIssueCollectionsAreDefensiveCopies()
    {
        EmailIssueList initialIssues = new EmailIssueList();
        initialIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HeaderMalformed, "original issue"));
        EmailHeader header = CreateFull("X-Test", " value", EmailLineEnding.CrLf, null!, new EmailSourcePosition(7, 1, 8), EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, initialIssues);
        byte[] name = header.GetNameBytes();
        byte[] raw = header.GetRawValue();
        byte[] original = header.GetOriginalRawFieldBytes();
        EmailIssueList returnedIssues = header.GetIssues();
        name[0] = (byte)'Z';
        raw[1] = (byte)'X';
        original[0] = (byte)'Z';
        returnedIssues.Clear();

        Assert.That(header.GetName(), Is.EqualTo("X-Test"));
        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" value"));
        Assert.That(Ascii(header.GetOriginalRawFieldBytes()), Is.EqualTo("X-Test: value\r\n"));
        Assert.That(header.GetIssues().GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void UnknownKindRetainsRawAndDecodedAccessWithoutInventingTypedValue()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValue("X-Unknown", Bytes(" opaque\r\n\tvalue"));

        Assert.That(header.GetValueKind(), Is.EqualTo(EmailHeaderValueKind.Unknown));
        Assert.That(header.GetStructuredValue(), Is.Null);
        Assert.That(header.HasStructuredValueCache(), Is.True);
        Assert.That(header.GetDecodedValue(), Is.EqualTo("opaque\tvalue"));
        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" opaque\r\n\tvalue"));
    }

    static EmailHeader CreateFull(string name, string rawValue, EmailLineEnding ending, EmailRange range, EmailSourcePosition position, EmailHeaderValueKind kind, EmailStructuredValueParseMode mode, EmailIssueList issues)
    {
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(Bytes(name), Bytes(rawValue), ending, range, position, kind, mode, issues);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
