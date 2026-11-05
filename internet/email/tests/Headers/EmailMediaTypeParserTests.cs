using System.Text;
using email;

namespace email.Tests;

public class EmailMediaTypeParserTests
{
    [Test]
    public void ParsesMediaTypeAndOrderedRawParameters()
    {
        const string input = "Text/X-Custom; Charset = \"utf-8\"; format=flowed";

        EmailMediaTypeParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailMediaType value = result.GetValue();
        Assert.That(value.GetTopLevelType(), Is.EqualTo("Text"));
        Assert.That(value.GetSubtype(), Is.EqualTo("X-Custom"));
        Assert.That(value.GetNormalizedTopLevelType(), Is.EqualTo("text"));
        Assert.That(value.GetNormalizedSubtype(), Is.EqualTo("x-custom"));
        Assert.That(value.IsType("TEXT", "x-custom"), Is.True);
        Assert.That(value.IsPrivateSubtype(), Is.True);
        Assert.That(value.WasDefaulted(), Is.False);
        EmailParameterList parameters = value.GetParameters();
        Assert.That(parameters.GetCount(), Is.EqualTo(2));
        Assert.That(parameters.GetAt(0).GetName(), Is.EqualTo("Charset"));
        Assert.That(Text(parameters.GetAt(0).GetRawValueBytes()), Is.EqualTo("\"utf-8\""));
        Assert.That(Text(parameters.GetAt(0).GetValueBytes()), Is.EqualTo("utf-8"));
        Assert.That(parameters.GetAt(0).WasQuoted(), Is.True);
        Assert.That(Text(parameters.GetAt(0).GetRawBytes()), Is.EqualTo("Charset = \"utf-8\""));
        Assert.That(parameters.GetAt(1).GetName(), Is.EqualTo("format"));
        Assert.That(Text(value.GetRawBytes()), Is.EqualTo(input));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RecognizesPrivateAndUnknownTopLevelExtensionTokens()
    {
        EmailMediaType privateType = Parse("X-Vendor/example", EmailStructuredValueParseMode.Strict).GetValue();
        EmailMediaType registeredExtension = Parse("font/woff2", EmailStructuredValueParseMode.Strict).GetValue();

        Assert.That(privateType.IsExtensionType(), Is.True);
        Assert.That(privateType.IsPrivateType(), Is.True);
        Assert.That(registeredExtension.IsExtensionType(), Is.True);
        Assert.That(registeredExtension.IsPrivateType(), Is.False);
        Assert.That(registeredExtension.GetSubtype(), Is.EqualTo("woff2"));
    }

    [Test]
    public void PreservesDuplicateParametersAndUsesDeterministicDecodedPrecedence()
    {
        const string input = "text/plain; charset=first; CHARSET=second; format=flowed";

        EmailMediaTypeParseResult tolerant = Parse(input, EmailStructuredValueParseMode.Tolerant);
        EmailMediaTypeParseResult strict = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailParameterList parameters = tolerant.GetValue().GetParameters();
        Assert.That(parameters.GetCount(), Is.EqualTo(3));
        Assert.That(parameters.GetAllNamed("charset").Count, Is.EqualTo(2));
        Assert.That(parameters.HasDuplicate("CHARSET"), Is.True);
        Assert.That(Text(parameters.GetDecoded("charset")!.GetDecodedBytes()), Is.EqualTo("first"));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.Rfc2231Duplicate), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetValue().GetParameters().GetCount(), Is.EqualTo(3));
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ResolvesExtendedAndContinuedParametersWithoutLosingPhysicalOrder()
    {
        const string input = "application/octet-stream; filename=fallback.txt; filename*1*=part%202.txt; filename*0*=UTF-8''caf%C3%A9%20";

        EmailMediaTypeParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailParameterList parameters = result.GetValue().GetParameters();
        Assert.That(parameters.GetCount(), Is.EqualTo(3));
        Assert.That(parameters.GetAt(0).GetName(), Is.EqualTo("filename"));
        Assert.That(parameters.GetAt(1).GetName(), Is.EqualTo("filename*1*"));
        Assert.That(parameters.GetAt(2).GetName(), Is.EqualTo("filename*0*"));
        EmailRfc2231DecodedParameter decoded = parameters.GetDecoded("filename")!;
        Assert.That(Encoding.UTF8.GetString(decoded!.GetDecodedBytes()), Is.EqualTo("café part 2.txt"));
        Assert.That(decoded.GetCharset(), Is.EqualTo("UTF-8"));
        Assert.That(decoded.IsExtended(), Is.True);
        Assert.That(decoded.IsContinued(), Is.True);
        Assert.That(decoded.GetSegmentCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void AppliesMimeDefaultForMissingOrInvalidMediaType()
    {
        EmailMediaType explicitDefault = new EmailMediaTypeOperations().CreateDefaultWith();
        EmailMediaTypeParseResult invalid = Parse("text", EmailStructuredValueParseMode.Tolerant);

        Assert.That(explicitDefault.IsType("text", "plain"), Is.True);
        Assert.That(explicitDefault.WasDefaulted(), Is.True);
        Assert.That(Text(explicitDefault.GetParameters().GetDecoded("charset")!.GetDecodedBytes()), Is.EqualTo("us-ascii"));
        Assert.That(invalid.GetValue().IsType("text", "plain"), Is.True);
        Assert.That(invalid.GetValue().WasDefaulted(), Is.True);
        Assert.That(Text(invalid.GetValue().GetRawBytes()), Is.EqualTo("text"));
        Assert.That(invalid.GetIssues().ContainsCode(EmailIssueCode.MediaTypeMalformed), Is.True);
        Assert.That(invalid.GetIssues().ContainsCode(EmailIssueCode.MediaTypeDefaultApplied), Is.True);
        Assert.That(invalid.IsSuccess(), Is.True);
    }

    [Test]
    public void StrictInvalidMediaTypeStillReturnsSafeDefaultWithErrors()
    {
        EmailMediaTypeParseResult result = Parse("text/", EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetValue().WasDefaulted(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
    }

    [Test]
    public void RecoversMalformedParameterAndRetainsLaterParameter()
    {
        EmailMediaTypeParseResult result = Parse("text/plain; broken; charset=utf-8", EmailStructuredValueParseMode.Tolerant);

        Assert.That(result.GetValue().GetParameters().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetValue().GetParameters().GetAt(0).GetName(), Is.EqualTo("charset"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.StructuredParameterMalformed), Is.True);
    }

    [Test]
    public void ParsesCommentsAndFoldedWhitespaceAroundStructure()
    {
        EmailMediaTypeParseResult result = Parse("text (kind) / plain;\r\n charset = (label) us-ascii", EmailStructuredValueParseMode.Strict);

        Assert.That(result.GetValue().IsType("text", "plain"), Is.True);
        Assert.That(Text(result.GetValue().GetParameters().GetDecoded("charset")!.GetDecodedBytes()), Is.EqualTo("us-ascii"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void UnfoldsQuotedWhitespaceWithoutDroppingItsEdges()
    {
        EmailMediaTypeParseResult result = Parse("text/plain; note=\" leading\r\n  middle \"", EmailStructuredValueParseMode.Strict);

        Assert.That(Text(result.GetValue().GetParameters().GetAt(0).GetValueBytes()), Is.EqualTo(" leading middle "));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void TracksSourcePositionsAndDefensivelyCopiesRawValues()
    {
        byte[] input = Bytes("text/plain; charset=us-ascii");
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        EmailMediaTypeParseResult result = new EmailMediaTypeParser().ParseWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(100, 8, 5));
        input[0] = (byte)'X';
        byte[] raw = result.GetValue().GetRawBytes();
        raw[0] = (byte)'Y';

        Assert.That(result.GetValue().GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(result.GetValue().GetParameters().GetAt(0).GetSourcePosition().GetByteOffset(), Is.EqualTo(112));
        Assert.That(Text(result.GetValue().GetRawBytes()), Is.EqualTo("text/plain; charset=us-ascii"));
    }

    [Test]
    public void AppliesInputParameterAndDecodedByteLimits()
    {
        EmailStructuredValueParserOptions input = new EmailStructuredValueParserOptions();
        input.SetMaximumInputBytes(5);
        EmailStructuredValueParserOptions count = new EmailStructuredValueParserOptions();
        count.SetMaximumParameters(1);
        EmailStructuredValueParserOptions decoded = new EmailStructuredValueParserOptions();
        decoded.SetMaximumDecodedParameterBytes(3);

        EmailMediaTypeParseResult inputResult = new EmailMediaTypeParser().ParseWithInputAndOptions(Bytes("text/plain"), input);
        EmailMediaTypeParseResult countResult = new EmailMediaTypeParser().ParseWithInputAndOptions(Bytes("text/plain; a=1; b=2"), count);
        EmailMediaTypeParseResult decodedResult = new EmailMediaTypeParser().ParseWithInputAndOptions(Bytes("text/plain; name=long"), decoded);

        Assert.That(inputResult.HasFatalError(), Is.True);
        Assert.That(inputResult.GetConsumedByteCount(), Is.Zero);
        Assert.That(countResult.HasFatalError(), Is.True);
        Assert.That(countResult.GetValue().GetParameters().GetCount(), Is.EqualTo(1));
        Assert.That(decodedResult.HasFatalError(), Is.True);
        Assert.That(decodedResult.GetIssues().ContainsCode(EmailIssueCode.Rfc2231DecodedByteLimitExceeded), Is.True);
    }

    [Test]
    public void ParserOptionsValidateAndCloneIndependently()
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMode(EmailStructuredValueParseMode.Strict);
        options.SetMaximumParameters(5);
        options.SetMaximumInputBytes(100);
        options.SetMaximumDecodedParameterBytes(200);
        EmailStructuredValueParserOptions clone = options.Clone();
        clone.SetMaximumParameters(7);

        Assert.That(options.GetMaximumParameters(), Is.EqualTo(5));
        Assert.That(clone.GetMaximumParameters(), Is.EqualTo(7));
        Assert.That(clone.GetMode(), Is.EqualTo(EmailStructuredValueParseMode.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailStructuredValueParseMode)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumParameters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumInputBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDecodedParameterBytes(-1));
    }

    static EmailMediaTypeParseResult Parse(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailMediaTypeParser().ParseWithInputAndMode(Bytes(value), mode);
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
