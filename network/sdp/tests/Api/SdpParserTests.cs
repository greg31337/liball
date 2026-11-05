using System.Text;

namespace sdp.Tests.Api;

public class SdpParserTests
{
    [Test]
    public void Parser_ExposesDistinctResultBasedInputPaths()
    {
        ISdpParser parser = new SdpParser();
        SdpParseOptions options = new SdpParseOptions();
        string text = "v=0\r\no=- 0 0 IN IP4 host\r\ns=-\r\nt=0 0\r\n";
        byte[] utf8 = Encoding.UTF8.GetBytes(text);

        SdpParseResult textResult = parser.ParseText(text, options);
        SdpParseResult characterResult = parser.ParseCharacters(text.AsSpan(), options);
        SdpParseResult utf8Result = parser.ParseUtf8Bytes(utf8.AsSpan(), options);

        Assert.Multiple(() =>
        {
            Assert.That(textResult, Is.Not.SameAs(characterResult));
            Assert.That(characterResult, Is.Not.SameAs(utf8Result));
            Assert.That(textResult.Success, Is.True);
            Assert.That(characterResult.Success, Is.True);
            Assert.That(utf8Result.Success, Is.True);
            Assert.That(textResult.Description, Is.Not.Null);
            Assert.That(characterResult.Description, Is.Not.Null);
            Assert.That(utf8Result.Description, Is.Not.Null);
            Assert.That(textResult.Diagnostics, Is.Empty);
            Assert.That(characterResult.Diagnostics, Is.Empty);
            Assert.That(utf8Result.Diagnostics, Is.Empty);
            Assert.That(textResult.CharactersConsumed, Is.EqualTo(text.Length));
            Assert.That(characterResult.CharactersConsumed, Is.EqualTo(text.Length));
            Assert.That(utf8Result.CharactersConsumed, Is.EqualTo(text.Length));
            Assert.That(utf8Result.BytesConsumed, Is.EqualTo(utf8.Length));
            Assert.That(textResult.LinesConsumed, Is.EqualTo(4));
            Assert.That(characterResult.LinesConsumed, Is.EqualTo(4));
            Assert.That(utf8Result.LinesConsumed, Is.EqualTo(4));
        });
    }

    [Test]
    public void ParserShell_SnapshotsEveryAppliedOption()
    {
        ISdpParser parser = new SdpParser();
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowByteOrderMark = true,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true,
            PreserveRawLines = true,
            PreserveMalformedLines = true,
            MaximumInputCharacters = 101,
            MaximumInputBytes = 102,
            MaximumLineLength = 103,
            MaximumLineCount = 104,
            MaximumMediaCount = 105,
            MaximumAttributesPerScope = 106,
            MaximumTokenLength = 107,
            MaximumCandidateCount = 108,
            MaximumDiagnosticCount = 109
        };

        SdpParseResult result = parser.ParseText("v=0", options);
        options.Mode = SdpParseMode.Strict;
        options.MaximumLineLength = 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.AppliedOptions, Is.Not.SameAs(options));
            Assert.That(result.AppliedOptions.Mode, Is.EqualTo(SdpParseMode.Lossless));
            Assert.That(result.AppliedOptions.AllowByteOrderMark, Is.True);
            Assert.That(result.AppliedOptions.AllowLfLineEndings, Is.True);
            Assert.That(result.AppliedOptions.AllowCrLineEndings, Is.True);
            Assert.That(result.AppliedOptions.PreserveRawLines, Is.True);
            Assert.That(result.AppliedOptions.PreserveMalformedLines, Is.True);
            Assert.That(result.AppliedOptions.MaximumInputCharacters, Is.EqualTo(101));
            Assert.That(result.AppliedOptions.MaximumInputBytes, Is.EqualTo(102));
            Assert.That(result.AppliedOptions.MaximumLineLength, Is.EqualTo(103));
            Assert.That(result.AppliedOptions.MaximumLineCount, Is.EqualTo(104));
            Assert.That(result.AppliedOptions.MaximumMediaCount, Is.EqualTo(105));
            Assert.That(result.AppliedOptions.MaximumAttributesPerScope, Is.EqualTo(106));
            Assert.That(result.AppliedOptions.MaximumTokenLength, Is.EqualTo(107));
            Assert.That(result.AppliedOptions.MaximumCandidateCount, Is.EqualTo(108));
            Assert.That(result.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(109));
        });
    }

    [Test]
    public void ParserShell_ReturnsIndependentMutableResultPayloads()
    {
        ISdpParser parser = new SdpParser();
        SdpParseOptions options = new SdpParseOptions();

        string input = "v=0\r\no=- 0 0 IN IP4 host\r\ns=-\r\nt=0 0";
        SdpParseResult first = parser.ParseText(input, options);
        SdpParseResult second = parser.ParseText(input, options);
        first.Diagnostics.Add(new SdpDiagnostic { Code = SdpDiagnosticCodes.ParseMalformedLine });
        first.AppliedOptions.MaximumLineLength = 1;

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics, Is.Not.SameAs(second.Diagnostics));
            Assert.That(first.AppliedOptions, Is.Not.SameAs(second.AppliedOptions));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.AppliedOptions.MaximumLineLength, Is.EqualTo(SdpLimits.DefaultMaximumLineLength));
            Assert.That(options.MaximumLineLength, Is.EqualTo(SdpLimits.DefaultMaximumLineLength));
        });
    }

    [Test]
    public void ParserShell_RejectsNullReferenceArguments()
    {
        ISdpParser parser = new SdpParser();
        SdpParseOptions options = new SdpParseOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => parser.ParseText(null!, options));
            Assert.Throws<ArgumentNullException>(() => parser.ParseText(string.Empty, null!));
            Assert.Throws<ArgumentNullException>(() => parser.ParseCharacters("v=0".AsSpan(), null!));
            Assert.Throws<ArgumentNullException>(() => parser.ParseUtf8Bytes(new byte[0], null!));
        });
    }
}
