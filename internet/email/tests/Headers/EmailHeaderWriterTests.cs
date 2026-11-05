using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderWriterTests
{
    [Test]
    public void RawBlockWritingRetainsCurrentBytesMixedEndingsFoldsAndSeparator()
    {
        const string source = "X-CuStOm: one\n\tfold\r\n\r\nbody";
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteRawWithBlock(block);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("X-CuStOm: one\n\tfold\r\n\r\n"));
        Assert.That(result.GetMode(), Is.EqualTo(EmailHeaderWriteMode.Raw));
        Assert.That(result.GetHeaderCount(), Is.EqualTo(1));
        Assert.That(result.GetCopiedHeaderCount(), Is.EqualTo(1));
        Assert.That(result.GetRegeneratedHeaderCount(), Is.Zero);
        Assert.That(result.GetFoldCount(), Is.EqualTo(1));
    }

    [Test]
    public void PreservingWritingCopiesUntouchedMalformedSourceExactly()
    {
        byte[] source = Bytes("Bad Name: one\0two\n\nbody");
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(source);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
        Assert.That(result.GetBytes(), Is.EqualTo(Bytes("Bad Name: one\0two\n\n")));
        Assert.That(result.GetCopiedHeaderCount(), Is.EqualTo(1));
        Assert.That(result.GetRegeneratedHeaderCount(), Is.Zero);
    }

    [Test]
    public void RawAndPreservingWritingRetainOrphanAndColonlessLinesExactly()
    {
        const string source = " orphan\r\nBroken line\r\n\r\n";
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        EmailHeaderWriteResult raw = new EmailHeaderWriter().WriteRawWithBlock(block);
        EmailHeaderWriteResult preserving = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(Ascii(block.GetHeaders().GetAt(0).GetOriginalRawFieldBytes()), Is.EqualTo(" orphan\r\nBroken line\r\n"));
        Assert.That(Ascii(raw.GetBytes()), Is.EqualTo(source));
        Assert.That(Ascii(preserving.GetBytes()), Is.EqualTo(source));
    }

    [Test]
    public void PreservingWritingRegeneratesOnlyDirtyOrNewHeaders()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("sUbJeCt: old\nX-Keep: exact\r\n\r\n"));
        block.GetHeaders().GetAt(0).SetValue(" changed");

        EmailHeaderWriteResult result = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("Subject: changed\r\nX-Keep: exact\r\n\r\n"));
        Assert.That(result.GetCopiedHeaderCount(), Is.EqualTo(1));
        Assert.That(result.GetRegeneratedHeaderCount(), Is.EqualTo(1));

        EmailHeaders created = new EmailHeadersOperations().CreateWith();
        created.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("subject", Bytes(" new"), EmailHeaderValueKind.Unstructured));
        EmailHeaderWriteResult createdResult = new EmailHeaderWriter().WritePreservingWithHeaders(created);
        Assert.That(Ascii(createdResult.GetBytes()), Is.EqualTo("Subject: new\r\n\r\n"));
        Assert.That(createdResult.GetRegeneratedHeaderCount(), Is.EqualTo(1));
    }

    [Test]
    public void CanonicalWritingNormalizesNamesValuesFoldsAndLineEndings()
    {
        const string source = "content-type: Text/Plain;\n Charset=UTF-8\nsubject:  hello   world\n\n";
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(block.GetHeaders());
        string text = Ascii(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(text, Does.StartWith("Content-Type: text/plain; charset=UTF-8\r\n"));
        Assert.That(text, Does.Contain("Subject: hello world\r\n"));
        Assert.That(text, Does.EndWith("\r\n\r\n"));
        Assert.That(text, Does.Not.Contain("\n "));
        Assert.That(result.GetCopiedHeaderCount(), Is.Zero);
        Assert.That(result.GetRegeneratedHeaderCount(), Is.EqualTo(2));
    }

    [Test]
    public void RegistryKindDrivesConstructedAndRenamedStandardHeaders()
    {
        EmailHeader mediaType = new EmailHeaderOperations().CreateWithNameAndRawValue("X-Temporary", Bytes(" Text/Plain; Charset=UTF-8"));
        mediaType.SetName("Content-Type");
        EmailHeader subject = new EmailHeaderOperations().CreateWithNameAndRawValue("Subject", Bytes(" =?utf-8?Q?caf=C3=A9?="));
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { mediaType, subject });

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(headers);
        string text = Ascii(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(text, Does.StartWith("Content-Type: text/plain; charset=UTF-8\r\n"));
        EmailHeaderBlockParseResult reparsed = new EmailHeaderBlockParser().ParseWithInput(result.GetBytes());
        Assert.That(reparsed.GetHeaders().GetFirst("subject")!.GetDecodedValue(), Is.EqualTo("café"));
    }

    [Test]
    public void CanonicalAsciiPolicyGeneratesEncodedWordsForUnicodeText()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" café ✓"), EmailHeaderValueKind.Unstructured));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(headers);
        string text = Ascii(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(text, Does.StartWith("Subject: =?utf-8?"));
        Assert.That(text, Does.EndWith("\r\n\r\n"));
        EmailHeaderBlockParseResult reparsed = new EmailHeaderBlockParser().ParseWithInput(result.GetBytes());
        Assert.That(reparsed.GetHeaders().GetAt(0).GetDecodedValue(), Is.EqualTo("café ✓"));
    }

    [Test]
    public void InternationalizedPolicyWritesUtf8Directly()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" café"), EmailHeaderValueKind.Unstructured));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetAllowInternationalized(true);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, options);

        Assert.That(result.GetBytes(), Is.EqualTo(Bytes("Subject: café\r\n\r\n")));
        Assert.That(Ascii(result.GetBytes()), Does.Not.Contain("=?"));
    }

    [Test]
    public void AddressFormattingUsesPhraseContextEncodedWords()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("From: José <jose@example.com>\r\n\r\n"));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(block.GetHeaders());
        string text = Ascii(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(text, Does.StartWith("From: =?utf-8?"));
        Assert.That(text, Does.Contain("<jose@example.com>"));
    }

    [Test]
    public void GenericFoldingUsesCrlfWhitespaceAndHonorsConfiguredLengths()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" alpha beta gamma delta epsilon zeta eta theta"), EmailHeaderValueKind.Unstructured));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetPreferredLineLength(30);
        options.SetMaximumLineLength(40);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, options);
        string text = Ascii(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(text, Does.Contain("\r\n\t"));
        Assert.That(result.GetFoldCount(), Is.GreaterThan(0));
        string[] lines = text.Split("\r\n", StringSplitOptions.None);
        for (int index = 0; index < lines.Length; index++)
        {
            Assert.That(Encoding.UTF8.GetByteCount(lines[index]), Is.LessThanOrEqualTo(40));
        }
    }

    [Test]
    public void UnbreakableTokenBeyondHardLengthProducesNoPartialOutput()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" " + new string('x', 50)), EmailHeaderValueKind.Unstructured));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetPreferredLineLength(20);
        options.SetMaximumLineLength(30);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, options);

        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteLineLengthExceeded), Is.True);
    }

    [Test]
    public void CanonicalWritingRejectsNameValueAndControlInjection()
    {
        EmailHeader unsafeName = Preserved(Bytes("Bad\rName"), Bytes(" value"));
        EmailHeader unsafeValue = Preserved(Bytes("X-Test"), Bytes(" safe\r\nInjected: yes"));
        EmailHeader unsafeControl = Preserved(Bytes("X-Control"), new byte[] { 32, 1, 88 });

        AssertRejected(unsafeName);
        AssertRejected(unsafeValue);
        AssertRejected(unsafeControl);

        EmailHeaderWriteResult raw = new EmailHeaderWriter().WriteRawWithHeaders(new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { unsafeValue }));
        Assert.That(raw.IsSuccess(), Is.True);
        Assert.That(Ascii(raw.GetBytes()), Does.Contain("Injected: yes"));
    }

    [Test]
    public void CanonicalWritingRejectsNulEvenWhenItCameFromPreservedSource()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("X-Test: one\0two\r\n\r\n"));

        EmailHeaderWriteResult canonical = new EmailHeaderWriter().WriteCanonicalWithHeaders(block.GetHeaders());
        EmailHeaderWriteResult preserving = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(canonical.HasFatalError(), Is.True);
        Assert.That(canonical.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteInjectionRejected), Is.True);
        Assert.That(canonical.GetByteCount(), Is.Zero);
        Assert.That(preserving.GetBytes(), Is.EqualTo(Bytes("X-Test: one\0two\r\n\r\n")));
    }

    [Test]
    public void CanonicalWritingRejectsControlsHiddenInsideEncodedWords()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Subject", Bytes(" =?utf-8?B?DQpJbmplY3RlZDogZXZpbA==?=")));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(headers);

        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteInjectionRejected), Is.True);
    }

    [Test]
    public void CardinalityViolationsBlockCanonicalButWarnWhenPreserving()
    {
        const string source = "Subject: one\r\nsubject: two\r\n\r\n";
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        EmailHeaderWriteResult canonical = new EmailHeaderWriter().WriteCanonicalWithHeaders(block.GetHeaders());
        EmailHeaderWriteResult preserving = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(canonical.HasFatalError(), Is.True);
        Assert.That(canonical.GetByteCount(), Is.Zero);
        Assert.That(canonical.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteCardinalityViolation), Is.True);
        Assert.That(preserving.IsSuccess(), Is.True);
        Assert.That(preserving.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteCardinalityViolation), Is.True);
        Assert.That(Ascii(preserving.GetBytes()), Is.EqualTo(source));
    }

    [Test]
    public void CardinalityCheckCanBeExplicitlyDisabled()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("Subject: one\r\nSubject: two\r\n\r\n"));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetCheckCardinality(false);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(block.GetHeaders(), options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("Subject: one\r\nSubject: two\r\n\r\n"));
    }

    [Test]
    public void OutputByteLimitReturnsFatalResultWithoutPartialBytes()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" hello"), EmailHeaderValueKind.Unstructured));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetMaximumOutputBytes(10);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, options);

        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteOutputLimitExceeded), Is.True);
    }

    [Test]
    public void RawModeWritesCurrentDirtyRepresentationWithoutCanonicalization()
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValue("X-Test", Bytes(" old"));
        header.SetName("X-Changed");
        header.SetRawValue(Bytes(" current\r\n\tfold"));
        header.SetLineEnding(EmailLineEnding.Lf);
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header });

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteRawWithHeaders(headers);

        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("X-Changed: current\r\n\tfold\n\r\n"));
        Assert.That(result.GetFoldCount(), Is.EqualTo(1));
    }

    [Test]
    public void IncompleteParsedBlockDoesNotGainASeparatorDuringPreservingWrite()
    {
        EmailHeaderBlockParseResult block = new EmailHeaderBlockParser().ParseWithInput(Bytes("X-Test: value"));

        EmailHeaderWriteResult result = new EmailHeaderWriter().WritePreservingWithBlock(block);

        Assert.That(block.HasSeparator(), Is.False);
        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("X-Test: value"));
    }

    [Test]
    public void OptionsCanOmitSeparatorAndPreserveUnknownFieldCasing()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("x-CUSTOM", Bytes(" value")));
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetIncludeSeparator(false);
        options.SetCanonicalizeFieldNames(false);

        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, options);

        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("x-CUSTOM: value\r\n"));
    }

    [Test]
    public void OptionsAndResultsUseDefensiveCopies()
    {
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetPreferredLineLength(30);
        options.SetMaximumLineLength(40);
        EmailHeaderWriteOptions clone = options.Clone();
        options.SetPreferredLineLength(25);
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" hello"), EmailHeaderValueKind.Unstructured) });
        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, clone);
        byte[] bytes = result.GetBytes();
        EmailIssueList issues = result.GetIssues();
        bytes[0] = 0;
        issues.Clear();

        Assert.That(clone.GetPreferredLineLength(), Is.EqualTo(30));
        Assert.That(Ascii(result.GetBytes()), Is.EqualTo("Subject: hello\r\n\r\n"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void InvalidArgumentsAndModesAreRejected()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();

        Assert.Throws<ArgumentNullException>(() => new EmailHeaderWriter().WriteCanonicalWithHeaders((EmailHeaders)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(headers, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderWriter().WriteRawWithBlock((EmailHeaderBlockParseResult)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHeaderWriter().Write(headers, (EmailHeaderWriteMode)99, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetPreferredLineLength(19));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEncodedWords(0));
    }

    static void AssertRejected(EmailHeader header)
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header });
        EmailHeaderWriteResult result = new EmailHeaderWriter().WriteCanonicalWithHeaders(headers);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderWriteInjectionRejected), Is.True);
    }

    static EmailHeader Preserved(byte[] name, byte[] value)
    {
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(name, value, EmailLineEnding.CrLf, null!, new EmailSourcePosition(0, 1, 1), EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
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
