using System.Text;
using email;

namespace email.Tests;

public class EmailRfc2231Tests
{
    [Test]
    public void PercentEncoderUsesAttributeCharactersAndCanonicalUppercaseEscapes()
    {
        byte[] input = Encoding.ASCII.GetBytes("This is ***fun***");

        byte[] encoded = new EmailPercentEncoding().Encode(input);
        EmailPercentDecodeResult decoded = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(encoded, EmailRfc2231DecodeMode.Strict, 100);

        Assert.That(Encoding.ASCII.GetString(encoded), Is.EqualTo("This%20is%20%2A%2A%2Afun%2A%2A%2A"));
        Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(decoded.GetDecodedByteCount(), Is.EqualTo(input.Length));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void PercentEncodingRoundTripsEveryByteValue()
    {
        byte[] input = new byte[256];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)index;
            index++;
        }

        byte[] encoded = new EmailPercentEncoding().Encode(input);
        EmailPercentDecodeResult decoded = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(encoded, EmailRfc2231DecodeMode.Strict, input.Length);

        Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void PercentDecoderDecodesCanonicalEscapesAndRetainsOrdinaryBytes()
    {
        EmailPercentDecodeResult result = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(
            Encoding.ASCII.GetBytes("caf%C3%A9%20menu"),
            EmailRfc2231DecodeMode.Strict,
            100);

        Assert.That(Encoding.UTF8.GetString(result.GetDecodedBytes()), Is.EqualTo("café menu"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void PercentDecoderRecoversInvalidAndLowercaseEscapesByMode()
    {
        byte[] input = Encoding.ASCII.GetBytes("a%2fb%GZ%4");

        EmailPercentDecodeResult tolerant = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(input, EmailRfc2231DecodeMode.Tolerant, 100);
        EmailPercentDecodeResult strict = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(input, EmailRfc2231DecodeMode.Strict, 100);

        Assert.That(Encoding.ASCII.GetString(tolerant.GetDecodedBytes()), Is.EqualTo("a/b%GZ%4"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.PercentEncodingLowercaseHex), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.PercentEncodingInvalidEscape), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetDecodedBytes(), Is.EqualTo(tolerant.GetDecodedBytes()));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void PercentDecoderReportsExactSourcePositionAndReturnsLimitPrefix()
    {
        EmailSourcePosition initial = new EmailSourcePosition(20, 3, 5);

        EmailPercentDecodeResult positioned = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(
            Encoding.ASCII.GetBytes("ab%GZ"),
            EmailRfc2231DecodeMode.Tolerant,
            100,
            initial);
        EmailPercentDecodeResult limited = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(
            Encoding.ASCII.GetBytes("%41%42%43"),
            EmailRfc2231DecodeMode.Tolerant,
            2);

        Assert.That(positioned.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(22));
        Assert.That(positioned.GetIssues().GetAt(0).GetSourcePosition()!.GetLineNumber(), Is.EqualTo(3));
        Assert.That(positioned.GetIssues().GetAt(0).GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(7));
        Assert.That(Encoding.ASCII.GetString(limited.GetDecodedBytes()), Is.EqualTo("AB"));
        Assert.That(limited.HasFatalError(), Is.True);
        Assert.That(limited.GetIssues().ContainsCode(EmailIssueCode.Rfc2231DecodedByteLimitExceeded), Is.True);
    }

    [Test]
    public void SingleExtendedValueRetainsCharsetAndEmptyLanguage()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("filename*", "UTF-8''caf%C3%A9.txt", false)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);
        EmailRfc2231DecodedParameter value = result.GetParameter(0);

        Assert.That(value.GetName(), Is.EqualTo("filename"));
        Assert.That(Encoding.UTF8.GetString(value.GetDecodedBytes()), Is.EqualTo("café.txt"));
        Assert.That(value.HasCharset(), Is.True);
        Assert.That(value.GetCharset(), Is.EqualTo("UTF-8"));
        Assert.That(value.HasLanguage(), Is.True);
        Assert.That(value.GetLanguage(), Is.Empty);
        Assert.That(value.IsExtended(), Is.True);
        Assert.That(value.IsContinued(), Is.False);
        Assert.That(value.GetSegmentCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RfcExampleCombinesMixedEncodedUnencodedQuotedAndUnquotedSegments()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("title*0*", "us-ascii'en'This%20is%20even%20more%20", false),
            Parameter("title*1*", "%2A%2A%2Afun%2A%2A%2A%20", true),
            Parameter("title*2", "isn't it!", true)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);
        EmailRfc2231DecodedParameter value = result.GetParameter(0);

        Assert.That(Encoding.ASCII.GetString(value.GetDecodedBytes()), Is.EqualTo("This is even more ***fun*** isn't it!"));
        Assert.That(value.GetCharset(), Is.EqualTo("us-ascii"));
        Assert.That(value.GetLanguage(), Is.EqualTo("en"));
        Assert.That(value.IsExtended(), Is.True);
        Assert.That(value.IsContinued(), Is.True);
        Assert.That(value.GetSegmentCount(), Is.EqualTo(3));
        Assert.That(value.GetSourceParameter(0).WasQuoted(), Is.False);
        Assert.That(value.GetSourceParameter(1).WasQuoted(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ContinuationsAreCaseInsensitiveAndOrderedBySectionNotInputOrder()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("FileName*2", "C", true),
            Parameter("filename*0", "A", false),
            Parameter("FILENAME*1", "B", true)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);

        Assert.That(result.GetParameterCount(), Is.EqualTo(1));
        Assert.That(result.GetParameter(0).GetName(), Is.EqualTo("FileName"));
        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("ABC"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RegularFallbackCoexistsWithAndLosesToExtendedValueWithoutADiagnostic()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("filename", "fallback.txt", true),
            Parameter("filename*", "utf-8''preferred.txt", false)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("preferred.txt"));
        Assert.That(result.GetParameter(0).GetSourceParameterCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DuplicateAndConflictingFormsUseDeterministicFirstOrContinuationPrecedence()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("filename*", "utf-8''single", false),
            Parameter("FILENAME*", "utf-8''duplicate", false),
            Parameter("filename*0*", "utf-8''continued-", false),
            Parameter("filename*0*", "utf-8''ignored-", false),
            Parameter("filename*1", "value", true)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("continued-value"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231Duplicate), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231ConflictingForms), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void GapReturnsContiguousPrefixAndIgnoresLaterSections()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("name*0", "first", false),
            Parameter("name*2", "third", false)
        };

        EmailRfc2231DecodeResult tolerant = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);
        EmailRfc2231DecodeResult strict = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(tolerant.GetParameter(0).GetDecodedBytes()), Is.EqualTo("first"));
        Assert.That(tolerant.GetParameter(0).GetSegmentCount(), Is.EqualTo(1));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.Rfc2231ContinuationGap), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void MissingSectionZeroPrefersRegularFallback()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("name*1", "orphan", false),
            Parameter("name", "fallback", true)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("fallback"));
        Assert.That(result.GetParameter(0).IsContinued(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231ContinuationGap), Is.True);
    }

    [Test]
    public void MissingSectionZeroWithoutFallbackUsesLowestContiguousRun()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("name*2*", "%42", false),
            Parameter("name*1*", "%41", false)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("AB"));
        Assert.That(result.GetParameter(0).IsContinued(), Is.True);
        Assert.That(result.GetParameter(0).HasCharset(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231ContinuationGap), Is.True);
    }

    [Test]
    public void LeadingZeroSectionIsRecoveredAndReported()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("name*00", "A", false),
            Parameter("name*1", "B", false)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("AB"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231MalformedName), Is.True);
    }

    [Test]
    public void MalformedExtendedMetadataAndPayloadRemainUsable()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("one*", "missing%20delimiters", false),
            Parameter("two*", "utf-8'en_US'has space", true)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("missing delimiters"));
        Assert.That(result.GetParameter(0).HasCharset(), Is.False);
        Assert.That(Encoding.ASCII.GetString(result.GetParameter(1).GetDecodedBytes()), Is.EqualTo("has space"));
        Assert.That(result.GetParameter(1).GetLanguage(), Is.EqualTo("en_US"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231MalformedExtendedValue), Is.True);
    }

    [Test]
    public void SegmentAndDecodedByteLimitsReturnDeterministicPrefixes()
    {
        List<EmailRfc2231Parameter> segments = new List<EmailRfc2231Parameter>
        {
            Parameter("name*0", "ab", false),
            Parameter("name*1", "cd", false)
        };

        EmailRfc2231DecodeResult segmentLimited = new EmailRfc2231().DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(segments, EmailRfc2231DecodeMode.Tolerant, 1, 100);
        EmailRfc2231DecodeResult byteLimited = new EmailRfc2231().DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(segments, EmailRfc2231DecodeMode.Tolerant, 10, 3);

        Assert.That(Encoding.ASCII.GetString(segmentLimited.GetParameter(0).GetDecodedBytes()), Is.EqualTo("ab"));
        Assert.That(segmentLimited.HasFatalError(), Is.True);
        Assert.That(segmentLimited.GetIssues().ContainsCode(EmailIssueCode.Rfc2231SegmentLimitExceeded), Is.True);
        Assert.That(Encoding.ASCII.GetString(byteLimited.GetParameter(0).GetDecodedBytes()), Is.EqualTo("abc"));
        Assert.That(byteLimited.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(byteLimited.HasFatalError(), Is.True);
        Assert.That(byteLimited.GetIssues().ContainsCode(EmailIssueCode.Rfc2231DecodedByteLimitExceeded), Is.True);
    }

    [Test]
    public void MalformedNamesArePreservedAsRegularValues()
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            Parameter("bad*section*x", "raw", false)
        };

        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Tolerant);

        Assert.That(result.GetParameter(0).GetName(), Is.EqualTo("bad*section*x"));
        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("raw"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.Rfc2231MalformedName), Is.True);
    }

    [Test]
    public void InputAndResultOwnershipAndArgumentsAreValidated()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("value");
        EmailRfc2231Parameter parameter = new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted("name", bytes, true);
        bytes[0] = 0;
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter> { parameter };
        EmailRfc2231DecodeResult result = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);
        byte[] returned = result.GetParameter(0).GetDecodedBytes();
        returned[0] = 0;
        result.GetParameters().Clear();
        result.GetParameter(0).GetSourceParameters().Clear();
        result.GetIssues().Clear();

        Assert.That(Encoding.ASCII.GetString(parameter.GetValueBytes()), Is.EqualTo("value"));
        Assert.That(Encoding.ASCII.GetString(result.GetParameter(0).GetDecodedBytes()), Is.EqualTo("value"));
        Assert.That(result.GetParameterCount(), Is.EqualTo(1));
        Assert.That(result.GetSourceParameterCount(), Is.EqualTo(1));
        Assert.That(result.GetSourceParameter(0), Is.SameAs(parameter));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted(null!, Array.Empty<byte>(), false));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted("", Array.Empty<byte>(), false));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted("x", null!, false));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231().DecodeWithParametersAndMode(null!, EmailRfc2231DecodeMode.Strict));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231().DecodeWithParametersAndMode(new List<EmailRfc2231Parameter> { null! }, EmailRfc2231DecodeMode.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRfc2231().DecodeWithParametersAndMode(new List<EmailRfc2231Parameter>(), (EmailRfc2231DecodeMode)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRfc2231().DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(new List<EmailRfc2231Parameter>(), EmailRfc2231DecodeMode.Strict, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRfc2231().DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(new List<EmailRfc2231Parameter>(), EmailRfc2231DecodeMode.Strict, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(Array.Empty<byte>(), EmailRfc2231DecodeMode.Strict, -1));
    }

    static EmailRfc2231Parameter Parameter(string name, string value, bool quoted)
    {
        return new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted(name, Encoding.ASCII.GetBytes(value), quoted);
    }
}
