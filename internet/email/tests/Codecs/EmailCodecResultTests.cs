using System.Text;
using email;

namespace email.Tests;

public class EmailCodecResultTests
{
    [Test]
    public void CharsetLookupResultsRepresentFoundAndMissingStates()
    {
        EmailCharsetLookupResult found = new EmailCharsetLookupResult(true, "UTF8", "utf-8", Encoding.UTF8);
        EmailCharsetLookupResult missing = new EmailCharsetLookupResult(false, "x-missing", null, null);

        Assert.That(found.IsFound(), Is.True);
        Assert.That(found.GetRequestedName(), Is.EqualTo("UTF8"));
        Assert.That(found.GetCanonicalName(), Is.EqualTo("utf-8"));
        Assert.That(found.GetCodePage(), Is.EqualTo(Encoding.UTF8.CodePage));
        Assert.That(found.GetEncoding(), Is.Not.Null);
        Assert.That(missing.IsFound(), Is.False);
        Assert.That(missing.GetRequestedName(), Is.EqualTo("x-missing"));
        Assert.That(missing.GetCanonicalName(), Is.Null);
        Assert.That(missing.GetCodePage(), Is.EqualTo(-1));
        Assert.That(missing.GetEncoding(), Is.Null);

        Assert.Throws<ArgumentNullException>(() => new EmailCharsetLookupResult(false, null!, null, null));
        Assert.Throws<ArgumentNullException>(() => new EmailCharsetLookupResult(true, "x", null, Encoding.UTF8));
        Assert.Throws<ArgumentNullException>(() => new EmailCharsetLookupResult(true, "x", "x", null));
        Assert.Throws<ArgumentException>(() => new EmailCharsetLookupResult(false, "x", "x", null));
        Assert.Throws<ArgumentException>(() => new EmailCharsetLookupResult(false, "x", null, Encoding.UTF8));
    }

    [Test]
    public void CharsetDecodeResultOwnsInputsAndValidatesRequiredState()
    {
        byte[] undecodable = new byte[] { 255 };
        EmailIssueList issues = new EmailIssueList();
        EmailCharsetDecodeResult result = new EmailCharsetDecodeResult("text", "declared", "utf-8", "utf-8", 3, undecodable, true, issues);
        undecodable[0] = 0;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Fatal, EmailIssueCode.ValidationFailed, "later"));

        Assert.That(result.GetText(), Is.EqualTo("text"));
        Assert.That(result.HasRequestedCharset(), Is.True);
        Assert.That(result.GetRequestedCharset(), Is.EqualTo("declared"));
        Assert.That(result.HasResolvedCharset(), Is.True);
        Assert.That(result.GetResolvedCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.HasBom(), Is.True);
        Assert.That(result.GetBomCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.GetBomByteCount(), Is.EqualTo(3));
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 255 }));
        Assert.That(result.UsedReplacement(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);

        Assert.Throws<ArgumentNullException>(() => new EmailCharsetDecodeResult(null!, null, null, null, 0, Array.Empty<byte>(), false, new EmailIssueList()));
        Assert.Throws<ArgumentNullException>(() => new EmailCharsetDecodeResult("", null, null, null, 0, null!, false, new EmailIssueList()));
        Assert.Throws<ArgumentNullException>(() => new EmailCharsetDecodeResult("", null, null, null, 0, Array.Empty<byte>(), false, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailCharsetDecodeResult("", null, null, null, -1, Array.Empty<byte>(), false, new EmailIssueList()));
    }

    [Test]
    public void PercentResultOwnsBytesIssuesAndReportsEverySeverityState()
    {
        byte[] value = new byte[] { 1, 2 };
        EmailIssueList warningIssues = new EmailIssueList();
        warningIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.PercentEncodingLowercaseHex, "warning"));
        EmailPercentDecodeResult warning = new EmailPercentDecodeResult(value, warningIssues);
        value[0] = 9;
        warningIssues.Clear();
        EmailIssueList fatalIssues = new EmailIssueList();
        fatalIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Fatal, EmailIssueCode.Rfc2231DecodedByteLimitExceeded, "fatal"));
        EmailPercentDecodeResult fatal = new EmailPercentDecodeResult(Array.Empty<byte>(), fatalIssues);

        Assert.That(warning.GetDecodedBytes(), Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(warning.GetDecodedByteCount(), Is.EqualTo(2));
        Assert.That(warning.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(warning.IsSuccess(), Is.True);
        Assert.That(warning.HasFatalError(), Is.False);
        Assert.That(fatal.IsSuccess(), Is.False);
        Assert.That(fatal.HasFatalError(), Is.True);
        Assert.Throws<ArgumentNullException>(() => new EmailPercentDecodeResult(null!, new EmailIssueList()));
        Assert.Throws<ArgumentNullException>(() => new EmailPercentDecodeResult(Array.Empty<byte>(), null!));
    }

    [Test]
    public void EncodedWordAndRfc2231ResultConstructorsValidateCountsAndIndexes()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWordEncodeResult(null!, EmailEncodedWordEncoding.Base64, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordEncodeResult(Array.Empty<byte>(), EmailEncodedWordEncoding.None, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordEncodeResult(Array.Empty<byte>(), EmailEncodedWordEncoding.Base64, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordEncodeResult(Array.Empty<byte>(), EmailEncodedWordEncoding.Base64, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordEncodeResult(Array.Empty<byte>(), EmailEncodedWordEncoding.Base64, 0, 0, -1));

        EmailEncodedWordParseResult parsed = new EmailEncodedWordParseResult(new List<EmailEncodedWordPart>(), new EmailIssueList(), 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => parsed.GetPart(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => parsed.GetPart(0));
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWordParseResult(null!, new EmailIssueList(), 0, 0));
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWordParseResult(new List<EmailEncodedWordPart>(), null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordParseResult(new List<EmailEncodedWordPart>(), new EmailIssueList(), -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailEncodedWordParseResult(new List<EmailEncodedWordPart>(), new EmailIssueList(), 0, -1));
        Assert.Throws<ArgumentException>(() => new EmailEncodedWordParseResult(new List<EmailEncodedWordPart> { null! }, new EmailIssueList(), 0, 0));

        EmailRfc2231Parameter parameter = new EmailRfc2231Parameter("name", Array.Empty<byte>(), false);
        List<EmailRfc2231Parameter> source = new List<EmailRfc2231Parameter> { parameter };
        EmailRfc2231DecodedParameter decoded = new EmailRfc2231DecodedParameter("name", Array.Empty<byte>(), null, false, null, false, false, false, 1, source);
        EmailRfc2231DecodeResult decodeResult = new EmailRfc2231DecodeResult(new List<EmailRfc2231DecodedParameter> { decoded }, source, new EmailIssueList(), 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => decodeResult.GetParameter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => decodeResult.GetParameter(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => decodeResult.GetSourceParameter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => decodeResult.GetSourceParameter(1));

        EmailRfc2231EncodeResult encodeResult = new EmailRfc2231EncodeResult(new byte[] { 1 }, source, 0, 1);
        Assert.That(encodeResult.GetByteCount(), Is.EqualTo(1));
        Assert.That(encodeResult.GetFinalLineColumn(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => encodeResult.GetParameter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => encodeResult.GetParameter(1));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231EncodeResult(null!, source, 0, 0));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231EncodeResult(Array.Empty<byte>(), null!, 0, 0));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231EncodeResult(Array.Empty<byte>(), new List<EmailRfc2231Parameter>(), 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRfc2231EncodeResult(Array.Empty<byte>(), source, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRfc2231EncodeResult(Array.Empty<byte>(), source, 0, -1));
    }
}
