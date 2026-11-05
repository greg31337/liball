using System.Text;
using email;

namespace email.Tests;

public class EmailHeaderBlockParserTests
{
    [Test]
    public void ParsesOrderedDuplicateHeadersAndStopsAtTheBody()
    {
        const string source = "Received: one\r\nSubject: hello\r\nReceived: two\r\n\r\nBODY\r\nContent-Type: ignored";
        int bodyOffset = source.IndexOf("BODY", StringComparison.Ordinal);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(bodyOffset));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(source.Length));
        Assert.That(result.GetBodyStartOffset(), Is.EqualTo(bodyOffset));
        Assert.That(result.GetSeparatorLineEnding(), Is.EqualTo(EmailLineEnding.CrLf));
        EmailHeaders headers = result.GetHeaders();
        Assert.That(headers.GetCount(), Is.EqualTo(3));
        Assert.That(headers.GetCountNamed("received"), Is.EqualTo(2));
        Assert.That(headers.GetAt(0).GetName(), Is.EqualTo("Received"));
        Assert.That(headers.GetAt(1).GetName(), Is.EqualTo("Subject"));
        Assert.That(headers.GetAt(2).GetName(), Is.EqualTo("Received"));
        Assert.That(headers.GetAt(0).GetValueKind(), Is.EqualTo(EmailHeaderValueKind.Received));
        Assert.That(headers.GetAt(1).GetValueKind(), Is.EqualTo(EmailHeaderValueKind.Unstructured));
        Assert.That(Ascii(headers.GetAt(2).GetOriginalRawFieldBytes()), Is.EqualTo("Received: two\r\n"));
    }

    [Test]
    public void PreservesContinuationBytesFoldsAndFinalLineEnding()
    {
        const string source = "Subject: first\r\n\tsecond\r\n third\r\n\r\n";

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));
        EmailHeader header = result.GetHeaders().GetAt(0);

        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" first\r\n\tsecond\r\n third"));
        Assert.That(Ascii(header.GetOriginalRawFieldBytes()), Is.EqualTo("Subject: first\r\n\tsecond\r\n third\r\n"));
        Assert.That(Ascii(header.GetUnfoldedValue()), Is.EqualTo(" first\tsecond third"));
        Assert.That(header.GetOriginalFoldCount(), Is.EqualTo(2));
        Assert.That(header.GetOriginalFold(0).IsCanonical(), Is.True);
        Assert.That(header.GetOriginalFold(1).IsCanonical(), Is.True);
        Assert.That(header.GetLineEnding(), Is.EqualTo(EmailLineEnding.CrLf));
        Assert.That(header.IsDirty(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DetectsAnEmptyHeaderBlock()
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes("\r\nbody"));

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetHeaders().GetCount(), Is.Zero);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(2));
        Assert.That(result.GetBodyStartOffset(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [TestCase("A: one\n\nbody", EmailLineEnding.Lf, 2)]
    [TestCase("A: one\r\rbody", EmailLineEnding.Cr, 2)]
    public void RetainsBareLineEndingsAndReportsThem(string source, EmailLineEnding ending, int expectedIssues)
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSeparatorLineEnding(), Is.EqualTo(ending));
        Assert.That(result.GetHeaders().GetAt(0).GetLineEnding(), Is.EqualTo(ending));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(expectedIssues));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNonCanonicalLineEnding), Is.True);
        Assert.That(result.GetHeaders().GetAt(0).GetIssues().ContainsCode(EmailIssueCode.HeaderNonCanonicalLineEnding), Is.True);
    }

    [Test]
    public void PreservesInvalidFieldNameBytesWithUnknownKind()
    {
        const string source = "Bad Name: value\r\n:empty-name\r\n\r\n";

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));

        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetHeaders().GetAt(0).GetName(), Is.EqualTo("Bad Name"));
        Assert.That(result.GetHeaders().GetAt(1).GetName(), Is.Empty);
        Assert.That(result.GetHeaders().GetAt(0).GetValueKind(), Is.EqualTo(EmailHeaderValueKind.Unknown));
        Assert.That(Ascii(result.GetHeaders().GetAt(0).GetOriginalRawFieldBytes()), Is.EqualTo("Bad Name: value\r\n"));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
        Assert.That(result.GetHeaders().GetAt(1).GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
    }

    [Test]
    public void RecoversAColonlessLineAsANoncanonicalContinuation()
    {
        const string source = "Subject: first\r\nbroken continuation\r\nX-Test: second\r\n\r\n";

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(source));
        EmailHeader subject = result.GetHeaders().GetAt(0);

        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(2));
        Assert.That(Ascii(subject.GetRawValue()), Is.EqualTo(" first\r\nbroken continuation"));
        Assert.That(Ascii(subject.GetOriginalRawFieldBytes()), Is.EqualTo("Subject: first\r\nbroken continuation\r\n"));
        Assert.That(subject.GetOriginalFoldCount(), Is.EqualTo(1));
        Assert.That(subject.GetOriginalFold(0).IsCanonical(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderMissingColon), Is.True);
        Assert.That(subject.GetIssues().ContainsCode(EmailIssueCode.HeaderMissingColon), Is.True);
    }

    [Test]
    public void RetainsAnOrphanContinuationAsAMalformedUnnamedHeader()
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes(" orphan\r\n\r\n"));
        EmailHeader header = result.GetHeaders().GetAt(0);

        Assert.That(header.GetName(), Is.Empty);
        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" orphan"));
        Assert.That(header.GetValueKind(), Is.EqualTo(EmailHeaderValueKind.Unknown));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderContinuationWithoutField), Is.True);
        Assert.That(header.GetIssues().ContainsCode(EmailIssueCode.HeaderContinuationWithoutField), Is.True);
    }

    [Test]
    public void RetainsNulAndReportsItsExactSourcePosition()
    {
        byte[] source = Bytes("X-Test: one\0two\r\n\r\n");
        EmailSourcePosition initial = new EmailSourcePosition(100, 4, 7);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(source, new EmailHeaderBlockParserOptions(), initial, true);
        EmailHeader header = result.GetHeaders().GetAt(0);
        EmailIssue issue = result.GetIssues().GetAll().Single(value => value.GetCode() == EmailIssueCode.HeaderNullByte);

        Assert.That(Ascii(header.GetRawValue()), Is.EqualTo(" one\0two"));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(111));
        Assert.That(issue.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(4));
        Assert.That(issue.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(18));
        Assert.Throws<ArgumentException>(() => new EmailHeadersOperations().CreateWith().Add(header));
    }

    [Test]
    public void AppliesInitialPositionToValuePositionAndExactSourceRange()
    {
        EmailSourcePosition initial = new EmailSourcePosition(500, 9, 12);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("X-Test: value\r\n\r\n"), new EmailHeaderBlockParserOptions(), initial, true);
        EmailHeader header = result.GetHeaders().GetAt(0);

        Assert.That(header.GetSourceRange()!.GetStartOffset(), Is.EqualTo(500));
        Assert.That(header.GetSourceRange()!.GetLength(), Is.EqualTo(15));
        Assert.That(header.GetValueSourcePosition()!.GetByteOffset(), Is.EqualTo(507));
        Assert.That(header.GetValueSourcePosition()!.GetLineNumber(), Is.EqualTo(9));
        Assert.That(header.GetValueSourcePosition()!.GetColumnNumber(), Is.EqualTo(19));
    }

    [Test]
    public void NonFinalInputReturnsNeedMoreDataWithoutInventingASeparatorIssue()
    {
        byte[] source = Bytes("Subject: partial\r\n");

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndEndOfInput(source, false);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailHeaderBlockParseStatus.NeedMoreData));
        Assert.That(result.NeedsMoreData(), Is.True);
        Assert.That(result.HasSeparator(), Is.False);
        Assert.That(result.HasBodyStartOffset(), Is.False);
        Assert.That(result.GetBodyStartOffset(), Is.EqualTo(-1));
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(source.Length));
        Assert.That(result.GetHeaders().GetCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.False);
    }

    [Test]
    public void NonFinalUnterminatedLineAndPendingCarriageReturnRemainUnconsumed()
    {
        EmailHeaderBlockParseResult line = new EmailHeaderBlockParser().ParseWithInputAndEndOfInput(Bytes("Subject: partial"), false);
        EmailHeaderBlockParseResult carriageReturn = new EmailHeaderBlockParser().ParseWithInputAndEndOfInput(Bytes("A: b\r"), false);

        Assert.That(line.NeedsMoreData(), Is.True);
        Assert.That(line.GetConsumedByteCount(), Is.Zero);
        Assert.That(carriageReturn.NeedsMoreData(), Is.True);
        Assert.That(carriageReturn.GetConsumedByteCount(), Is.Zero);
    }

    [Test]
    public void FinalInputWithoutSeparatorReturnsPartialHeadersAndAnError()
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes("Subject: complete\r\nX-End: unterminated"));

        Assert.That(result.GetStatus(), Is.EqualTo(EmailHeaderBlockParseStatus.EndOfInput));
        Assert.That(result.ReachedEndOfInput(), Is.True);
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetHeaders().GetAt(1).GetLineEnding(), Is.EqualTo(EmailLineEnding.None));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
    }

    [Test]
    public void StrictModePromotesRecoveriesToErrors()
    {
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();
        options.SetMode(EmailStructuredValueParseMode.Strict);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("Bad Name: value\n\n"), options, new EmailSourcePosition(0, 1, 1), true);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(3));
        Assert.That(result.GetHeaders().GetAt(0).GetParseMode(), Is.EqualTo(EmailStructuredValueParseMode.Strict));
    }

    [Test]
    public void EnforcesLineHeaderByteAndFieldCountLimits()
    {
        EmailParsingLimits lineLimits = new EmailParsingLimits();
        lineLimits.SetMaxLineBytes(3);
        EmailHeaderBlockParserOptions lineOptions = Options(lineLimits);
        EmailLimitException line = Assert.Throws<EmailLimitException>(() => new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("A:xy\r\n\r\n"), lineOptions, new EmailSourcePosition(0, 1, 1), true));

        EmailParsingLimits byteLimits = new EmailParsingLimits();
        byteLimits.SetMaxHeaderBytes(6);
        EmailHeaderBlockParserOptions byteOptions = Options(byteLimits);
        EmailLimitException bytes = Assert.Throws<EmailLimitException>(() => new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("A:b\r\n\r\n"), byteOptions, new EmailSourcePosition(0, 1, 1), true));

        EmailParsingLimits countLimits = new EmailParsingLimits();
        countLimits.SetMaxHeaderFieldCount(1);
        EmailHeaderBlockParserOptions countOptions = Options(countLimits);
        EmailLimitException count = Assert.Throws<EmailLimitException>(() => new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("A:1\r\nB:2\r\n\r\n"), countOptions, new EmailSourcePosition(0, 1, 1), true));

        Assert.That(line.GetLimitName(), Is.EqualTo("MaxLineBytes"));
        Assert.That(line.GetObserved(), Is.EqualTo(4));
        Assert.That(bytes.GetLimitName(), Is.EqualTo("MaxHeaderBytes"));
        Assert.That(bytes.GetObserved(), Is.EqualTo(7));
        Assert.That(count.GetLimitName(), Is.EqualTo("MaxHeaderFieldCount"));
        Assert.That(count.GetObserved(), Is.EqualTo(2));
    }

    [Test]
    public void AcceptsExactLimitsAndDoesNotCountBodyBytesAsHeaders()
    {
        EmailParsingLimits limits = new EmailParsingLimits();
        limits.SetMaxLineBytes(3);
        limits.SetMaxHeaderBytes(7);
        limits.SetMaxHeaderFieldCount(1);
        string source = "A:b\r\n\r\n" + new string('x', 100);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes(source), Options(limits), new EmailSourcePosition(0, 1, 1), true);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(7));
        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void OptionsOwnDefensiveLimitSnapshots()
    {
        EmailParsingLimits supplied = new EmailParsingLimits();
        supplied.SetMaxHeaderFieldCount(2);
        EmailHeaderBlockParserOptions options = Options(supplied);
        supplied.SetMaxHeaderFieldCount(1);
        EmailParsingLimits returned = options.GetLimits();
        returned.SetMaxHeaderFieldCount(1);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("A:1\r\nB:2\r\n\r\n"), options, new EmailSourcePosition(0, 1, 1), true);

        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(2));
        Assert.That(options.GetLimits().GetMaxHeaderFieldCount(), Is.EqualTo(2));
    }

    [Test]
    public void ResultIssueListsAreDefensiveSnapshots()
    {
        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInput(Bytes("Bad Name: value\r\n\r\n"));
        EmailIssueList issues = result.GetIssues();
        issues.Clear();

        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
        Assert.That(result.GetHeaders().GetAt(0).GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
    }

    [Test]
    public void InvalidOptionsAndPositionsAreRejected()
    {
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();

        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailStructuredValueParseMode)99));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("\r\n"), null!, new EmailSourcePosition(0, 1, 1), true));
        Assert.Throws<ArgumentNullException>(() => new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("\r\n"), options, null!, true));
    }

    static EmailHeaderBlockParserOptions Options(EmailParsingLimits limits)
    {
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();
        options.SetLimits(limits);
        return options;
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
