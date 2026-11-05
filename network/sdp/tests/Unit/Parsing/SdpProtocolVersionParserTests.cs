using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpProtocolVersionParserTests
{
    [Test]
    public void ProtocolVersionZero_IsParsedFromEveryInputPath()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions();
        byte[] utf8 = Encoding.UTF8.GetBytes("v=00\r\n");

        SdpParseResult text = parser.ParseText("v=0\r\n", options);
        SdpParseResult characters = parser.ParseCharacters("v=0\r\n".AsSpan(), options);
        SdpParseResult bytes = parser.ParseUtf8Bytes(utf8.AsSpan(), options);

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(text.Diagnostics, Is.Empty);
            Assert.That(text.Description?.HasProtocolVersion, Is.True);
            Assert.That(text.Description?.ProtocolVersion, Is.Zero);
            Assert.That(characters.Success, Is.True);
            Assert.That(characters.Description?.HasProtocolVersion, Is.True);
            Assert.That(characters.Description?.ProtocolVersion, Is.Zero);
            Assert.That(bytes.Success, Is.True);
            Assert.That(bytes.Description?.HasProtocolVersion, Is.True);
            Assert.That(bytes.Description?.ProtocolVersion, Is.Zero);
        });
    }

    [Test]
    public void NonzeroVersion_IsRetainedAndDiagnosedAtItsValue()
    {
        ISdpParser parser = new SdpFragmentParser();
        byte[] input = Encoding.UTF8.GetBytes("\uFEFFv=1\r\n");

        SdpParseResult result = parser.ParseUtf8Bytes(input.AsSpan(), new SdpParseOptions { AllowByteOrderMark = true });
        SdpDiagnostic diagnostic = result.Diagnostics.Count == 1 ? result.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasProtocolVersion, Is.True);
            Assert.That(result.Description?.ProtocolVersion, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ValidationProtocolVersion));
            Assert.That(diagnostic.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(diagnostic.Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(diagnostic.FieldName, Is.EqualTo('v'));
            Assert.That(diagnostic.MediaIndex, Is.EqualTo(-1));
            Assert.That(diagnostic.Path.FieldName, Is.EqualTo('v'));
            Assert.That(diagnostic.Path.OccurrenceIndex, Is.Zero);
            Assert.That(diagnostic.Location.CharacterOffset, Is.EqualTo(2));
            Assert.That(diagnostic.Location.ByteOffset, Is.EqualTo(5));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostic.Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [TestCase("v=")]
    [TestCase("v=-1")]
    [TestCase("v=0x")]
    public void InvalidVersionNumber_IsPresentButNotTreatedAsMissing(string input)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasProtocolVersion, Is.True);
            Assert.That(result.Description?.ProtocolVersion, Is.Zero);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [TestCase("v=18446744073709551616")]
    [TestCase("v=2147483648")]
    public void UnrepresentableVersion_IsReportedAsNumericOverflow(string input)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasProtocolVersion, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('v'));
        });
    }

    [Test]
    public void DuplicateVersion_IsDiagnosedWithoutReplacingTheFirstValue()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nv=1", new SdpParseOptions());
        SdpDiagnostic diagnostic = result.Diagnostics.Count == 1 ? result.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.ProtocolVersion, Is.Zero);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(diagnostic.Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(2));
            Assert.That(diagnostic.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void VersionAfterAnotherSessionField_IsParsedAndReportedOutOfOrder()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("s=example\r\nv=0", new SdpParseOptions());
        SdpDiagnostic diagnostic = result.Diagnostics.Count == 1 ? result.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasProtocolVersion, Is.True);
            Assert.That(result.Description?.ProtocolVersion, Is.Zero);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(2));
            Assert.That(diagnostic.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingOrInvalidVersionDiscriminator_IsReported()
    {
        ISdpParser parser = new SdpParser();
        string missingInput = "o=- 0 0 IN IP4 host\r\ns=example\r\nt=0 0";
        string uppercaseInput = "V=0\r\no=- 0 0 IN IP4 host\r\ns=example\r\nt=0 0";
        string utf8Text = "o=- 0 0 IN IP4 host\r\ns=example\r\nt=0 0\r\n";

        SdpParseResult missing = parser.ParseText(missingInput, new SdpParseOptions());
        SdpParseResult uppercase = parser.ParseText(uppercaseInput, new SdpParseOptions());
        byte[] utf8Input = Encoding.UTF8.GetBytes(utf8Text);
        SdpParseResult utf8Missing = parser.ParseUtf8Bytes(utf8Input.AsSpan(), new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Description?.HasProtocolVersion, Is.False);
            Assert.That(missing.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(missing.Diagnostics[0].FieldName, Is.EqualTo('v'));
            Assert.That(missing.Diagnostics[0].Path.Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(missing.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(missing.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(missingInput.Length));
            Assert.That(missing.Diagnostics[0].Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(missing.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
            Assert.That(missing.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(6));
            Assert.That(uppercase.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(uppercase.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(uppercase.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(utf8Missing.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(utf8Missing.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(utf8Text.Length));
            Assert.That(utf8Missing.Diagnostics[0].Location.ByteOffset, Is.EqualTo(utf8Input.Length));
            Assert.That(utf8Missing.Diagnostics[0].Location.LineNumber, Is.EqualTo(4));
            Assert.That(utf8Missing.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void VersionInMediaScope_IsRejectedAndDoesNotSatisfyTheRequiredSessionField()
    {
        ISdpParser parser = new SdpParser();

        string input = "o=- 0 0 IN IP4 host\r\ns=example\r\nt=0 0\r\nm=x 9 X 0\r\nv=0";
        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasProtocolVersion, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(result.Diagnostics[2].Scope, Is.EqualTo(SdpScope.Session));
        });
    }
}
