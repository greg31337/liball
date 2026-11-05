using System.Text;
using email;

namespace email.Tests;

public class EmailEncodedWordTests
{
    [Test]
    public void ParsesBAndQWordsWhileRetainingCharsetLanguageAndRawSource()
    {
        byte[] input = Encoding.ASCII.GetBytes("=?UTF-8*en?B?SGVsbG8=?= / =?iso-8859-1?Q?caf=E9?=");

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(result.GetPartCount(), Is.EqualTo(3));
        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(2));
        Assert.That(result.GetDecodedByteCount(), Is.EqualTo(9));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(0).GetBytes()), Is.EqualTo("Hello"));
        Assert.That(result.GetPart(0).GetCharset(), Is.EqualTo("UTF-8"));
        Assert.That(result.GetPart(0).HasLanguage(), Is.True);
        Assert.That(result.GetPart(0).GetLanguage(), Is.EqualTo("en"));
        Assert.That(result.GetPart(0).GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.Base64));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(0).GetRawBytes()), Is.EqualTo("=?UTF-8*en?B?SGVsbG8=?="));
        Assert.That(result.GetPart(1).GetKind(), Is.EqualTo(EmailEncodedWordPartKind.Literal));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(1).GetBytes()), Is.EqualTo(" / "));
        Assert.That(result.GetPart(1).HasCharset(), Is.False);
        Assert.That(result.GetPart(2).GetBytes(), Is.EqualTo(new byte[] { 99, 97, 102, 233 }));
        Assert.That(result.GetPart(2).GetCharset(), Is.EqualTo("iso-8859-1"));
        Assert.That(result.GetPart(2).HasLanguage(), Is.False);
        Assert.That(result.GetPart(2).GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.QuotedPrintable));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void SuppressesLinearWhitespaceOnlyBetweenAdjacentEncodedWords()
    {
        byte[] input = Encoding.ASCII.GetBytes("=?utf-8?Q?Hello?= \r\n\t=?utf-8?Q?_world?=");

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(result.GetPartCount(), Is.EqualTo(2));
        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("Hello world"));
        Assert.That(result.GetPart(0).GetSourcePosition().GetByteOffset(), Is.Zero);
        Assert.That(result.GetPart(1).GetSourcePosition().GetLineNumber(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RetainsWhitespaceNextToLiteralText()
    {
        byte[] input = Encoding.ASCII.GetBytes("prefix =?us-ascii?Q?middle?= suffix");

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(result.GetPartCount(), Is.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("prefix middle suffix"));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(0).GetBytes()), Is.EqualTo("prefix "));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(2).GetBytes()), Is.EqualTo(" suffix"));
    }

    [Test]
    public void QDecodingMapsUnderscoreAndHexAndReportsRecoverableDefects()
    {
        byte[] input = Encoding.ASCII.GetBytes("=?utf-8?Q?one_two=21_bad=GZ_lower=2f?=");

        EmailEncodedWordParseResult tolerant = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText);
        EmailEncodedWordParseResult strict = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(tolerant.GetCombinedBytes()), Is.EqualTo("one two! bad=GZ lower/"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.EncodedWordInvalidPayload), Is.True);
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetCombinedBytes(), Is.EqualTo(tolerant.GetCombinedBytes()));
        Assert.That(strict.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void Base64RecoveryReturnsUsableBytesAndReportsNonCanonicalPadding()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes("=?utf-8?b?QQ?="),
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("A"));
        Assert.That(result.GetPart(0).GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.Base64));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordInvalidPayload), Is.True);
    }

    [Test]
    public void UnsupportedAndIncompleteWordsRemainLiteralWhileLaterWordsRecover()
    {
        byte[] input = Encoding.ASCII.GetBytes("start =?utf-8?X?abc?= =?broken next =?utf-8?Q?unfinished then =?utf-8?Q?ok?=");

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("start =?utf-8?X?abc?= =?broken next =?utf-8?Q?unfinished then ok"));
        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordUnsupportedEncoding), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordMalformed), Is.True);
        Assert.That(result.GetPart(result.GetPartCount() - 1).IsEncodedWord(), Is.True);
    }

    [Test]
    public void EmptyLanguageTagIsRetainedAsPresent()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes("=?utf-8*?Q?value?="),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(result.GetPart(0).GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.GetPart(0).HasLanguage(), Is.True);
        Assert.That(result.GetPart(0).GetLanguage(), Is.Empty);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ForbiddenContextRetainsARecognizedWordAsLiteral()
    {
        byte[] input = Encoding.ASCII.GetBytes("=?utf-8?Q?value?=");

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            input,
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.Forbidden);

        Assert.That(result.GetEncodedWordCount(), Is.Zero);
        Assert.That(result.GetPartCount(), Is.EqualTo(1));
        Assert.That(result.GetPart(0).IsEncodedWord(), Is.False);
        Assert.That(result.GetCombinedBytes(), Is.EqualTo(input));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.EncodedWordContextViolation));
    }

    [TestCase(EmailEncodedWordContext.Phrase, "=?utf-8?Q?last.name?=")]
    [TestCase(EmailEncodedWordContext.Comment, "=?utf-8?Q?left(right?=")]
    public void QContextRestrictionsAreReportedWithoutDiscardingDecodedBytes(EmailEncodedWordContext context, string input)
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes(input),
            EmailEncodedWordParseMode.Tolerant,
            context);

        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordContextViolation), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void MissingLinearWhitespaceIsDiagnosedButBothWordsAreDecoded()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes("x=?us-ascii?Q?A?==?us-ascii?Q?B?=y"),
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("xABy"));
        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordSeparationViolation), Is.True);
    }

    [Test]
    public void WordLengthViolationIsRecoverable()
    {
        string input = "=?utf-8?Q?" + new string('a', 64) + "?=";

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes(input),
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(input.Length, Is.EqualTo(76));
        Assert.That(result.GetCombinedBytes().Length, Is.EqualTo(64));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordTooLong), Is.True);
    }

    [Test]
    public void EncodedWordCountLimitStopsAndPreservesTheRemainingSource()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(
            Encoding.ASCII.GetBytes("=?us-ascii?Q?A?= =?us-ascii?Q?B?="),
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText,
            1,
            100);

        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(1));
        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("A =?us-ascii?Q?B?="));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordLimitExceeded), Is.True);
    }

    [Test]
    public void DecodedByteLimitReturnsOnlyTheAllowedPrefix()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(
            Encoding.ASCII.GetBytes("=?us-ascii?Q?abcd?="),
            EmailEncodedWordParseMode.Tolerant,
            EmailEncodedWordContext.UnstructuredText,
            10,
            2);

        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("ab"));
        Assert.That(result.GetDecodedByteCount(), Is.EqualTo(2));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.EncodedWordDecodedByteLimitExceeded), Is.True);
    }

    [Test]
    public void EmptyInputAndArgumentValidationAreDeterministic()
    {
        EmailEncodedWordParseResult empty = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Array.Empty<byte>(),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(empty.GetPartCount(), Is.Zero);
        Assert.That(empty.GetCombinedBytes(), Is.Empty);
        Assert.That(empty.IsSuccess(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWord().ParseWithInputAndModeAndContext(Array.Empty<byte>(), (EmailEncodedWordParseMode)99, EmailEncodedWordContext.UnstructuredText));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWord().ParseWithInputAndModeAndContext(Array.Empty<byte>(), EmailEncodedWordParseMode.Strict, (EmailEncodedWordContext)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWord().ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(Array.Empty<byte>(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.UnstructuredText, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWord().ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(Array.Empty<byte>(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.UnstructuredText, 1, -1));
    }

    [Test]
    public void ReturnedCollectionsAndByteArraysAreSnapshots()
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes("=?us-ascii?Q?A?="),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        byte[] bytes = result.GetPart(0).GetBytes();
        byte[] raw = result.GetPart(0).GetRawBytes();
        bytes[0] = 90;
        raw[0] = 90;
        result.GetParts().Clear();
        result.GetIssues().Clear();

        Assert.That(Encoding.ASCII.GetString(result.GetPart(0).GetBytes()), Is.EqualTo("A"));
        Assert.That(Encoding.ASCII.GetString(result.GetPart(0).GetRawBytes()), Is.EqualTo("=?us-ascii?Q?A?="));
        Assert.That(result.GetPartCount(), Is.EqualTo(1));
    }
}
