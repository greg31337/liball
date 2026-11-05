using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpParserContextTests
{
    [Test]
    public void LosslessMode_RetainsSessionAndMediaLinesInTheirScopes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Lossless };
        string input = "v=0\r\ns=Example\r\nm=audio 9 RTP/AVP 0\r\na=sendrecv";

        SdpParseResult result = parser.ParseText(input, options);
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpMediaDescription media = description.MediaDescriptions.Count == 1 ? description.MediaDescriptions[0] : new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.CharactersConsumed, Is.EqualTo(input.Length));
            Assert.That(result.BytesConsumed, Is.EqualTo(0));
            Assert.That(result.LinesConsumed, Is.EqualTo(4));
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(description.RawLines, Has.Count.EqualTo(2));
            Assert.That(description.RawLines[0].FieldName, Is.EqualTo('v'));
            Assert.That(description.RawLines[1].FieldName, Is.EqualTo('s'));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(media.RawLines, Has.Count.EqualTo(2));
            Assert.That(media.RawLines[0].FieldName, Is.EqualTo('m'));
            Assert.That(media.RawLines[0].Value, Is.EqualTo("audio 9 RTP/AVP 0"));
            Assert.That(media.RawLines[1].FieldName, Is.EqualTo('a'));
            Assert.That(media.Location.CharacterOffset, Is.EqualTo("v=0\r\ns=Example\r\n".Length));
            Assert.That(media.Location.LineNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void MalformedLine_DiagnosticCarriesCurrentMediaPathAndLocation()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nm=x 9 X 0\r\nBROKEN\r\n";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpDiagnostic diagnostic = result.Diagnostics.Count == 1 ? result.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description, Is.Not.Null);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.LinesConsumed, Is.EqualTo(3));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(diagnostic.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(diagnostic.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(diagnostic.MediaIndex, Is.EqualTo(0));
            Assert.That(diagnostic.Path.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(diagnostic.Path.MediaIndex, Is.EqualTo(0));
            Assert.That(diagnostic.Location.CharacterOffset, Is.EqualTo("v=0\r\nm=x 9 X 0\r\n".Length));
            Assert.That(diagnostic.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(diagnostic.Location.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostic.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void AlternateLineEndings_RequireExplicitOptionsAndAreRetainedInLosslessMode()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseResult rejectedLf = parser.ParseText("v=0\n", new SdpParseOptions());
        SdpParseResult acceptedLf = parser.ParseText("v=0\n", new SdpParseOptions { AllowLfLineEndings = true });
        SdpParseResult acceptedCr = parser.ParseText("v=0\r", new SdpParseOptions { AllowCrLineEndings = true });
        SdpParseResult rejectedMediaLf = parser.ParseText("v=0\r\nm=x 9 X 0\n", new SdpParseOptions());
        SdpParseResult losslessLf = parser.ParseText("v=0\n", new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription losslessDescription = losslessLf.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(rejectedLf.Success, Is.False);
            Assert.That(rejectedLf.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(rejectedLf.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidLineEnding));
            Assert.That(rejectedLf.Diagnostics[0].FieldName, Is.EqualTo('v'));
            Assert.That(acceptedLf.Success, Is.True);
            Assert.That(acceptedLf.Diagnostics, Is.Empty);
            Assert.That(acceptedCr.Success, Is.True);
            Assert.That(acceptedCr.Diagnostics, Is.Empty);
            Assert.That(rejectedMediaLf.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(rejectedMediaLf.Diagnostics[0].MediaIndex, Is.EqualTo(0));
            Assert.That(rejectedMediaLf.Diagnostics[0].FieldName, Is.EqualTo('m'));

            Assert.That(losslessLf.Success, Is.False);
            Assert.That(losslessDescription.RawLines, Has.Count.EqualTo(1));
            Assert.That(losslessDescription.RawLines[0].LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(losslessDescription.RawLines[0].IsMalformed, Is.True);
        });
    }

    [Test]
    public void Utf8Input_EnforcesBomPolicyAndReportsExactInvalidSequenceLocation()
    {
        ISdpParser parser = new SdpFragmentParser();
        byte[] bomInput = Encoding.UTF8.GetBytes("\uFEFFv=0\r\n");
        SdpParseResult rejectedBom = parser.ParseUtf8Bytes(bomInput.AsSpan(), new SdpParseOptions());
        SdpParseResult acceptedBom = parser.ParseUtf8Bytes(
            bomInput.AsSpan(),
            new SdpParseOptions { AllowByteOrderMark = true, PreserveRawLines = true });
        byte[] invalidInput = new byte[] { 118, 61, 48, 13, 10, 195, 169, 255 };
        SdpParseResult invalid = parser.ParseUtf8Bytes(invalidInput.AsSpan(), new SdpParseOptions());
        byte[] invalidAfterBomInput = new byte[] { 239, 187, 191, 255 };
        SdpParseResult invalidAfterBom = parser.ParseUtf8Bytes(invalidAfterBomInput.AsSpan(), new SdpParseOptions { AllowByteOrderMark = true });
        SdpSessionDescription acceptedDescription = acceptedBom.Description ?? new SdpSessionDescription();
        SdpDiagnostic invalidDiagnostic = invalid.Diagnostics.Count == 1 ? invalid.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(rejectedBom.Success, Is.False);
            Assert.That(rejectedBom.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseByteOrderMarkNotAllowed));
            Assert.That(rejectedBom.Diagnostics[0].Location.ByteOffset, Is.EqualTo(0));

            Assert.That(acceptedBom.Success, Is.True);
            Assert.That(acceptedBom.BytesConsumed, Is.EqualTo(bomInput.Length));
            Assert.That(acceptedBom.CharactersConsumed, Is.EqualTo("v=0\r\n".Length));
            Assert.That(acceptedBom.LinesConsumed, Is.EqualTo(1));
            Assert.That(acceptedDescription.RawLines, Has.Count.EqualTo(1));
            Assert.That(acceptedDescription.RawLines[0].Location.ByteOffset, Is.EqualTo(3));
            Assert.That(acceptedDescription.RawLines[0].Location.CharacterOffset, Is.EqualTo(0));

            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.BytesConsumed, Is.EqualTo(7));
            Assert.That(invalid.CharactersConsumed, Is.EqualTo(6));
            Assert.That(invalidDiagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidUtf8));
            Assert.That(invalidDiagnostic.Location.ByteOffset, Is.EqualTo(7));
            Assert.That(invalidDiagnostic.Location.CharacterOffset, Is.EqualTo(6));
            Assert.That(invalidDiagnostic.Location.LineNumber, Is.EqualTo(2));
            Assert.That(invalidDiagnostic.Location.ColumnNumber, Is.EqualTo(2));
            Assert.That(invalidAfterBom.Diagnostics[0].Location.ByteOffset, Is.EqualTo(3));
            Assert.That(invalidAfterBom.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(0));
            Assert.That(invalidAfterBom.Diagnostics[0].Location.LineNumber, Is.EqualTo(1));
            Assert.That(invalidAfterBom.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void InputAndLineLimits_StopAtDeterministicPositions()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseResult characterLimit = parser.ParseText("v=0", new SdpParseOptions { MaximumInputCharacters = 2 });
        SdpParseResult byteLimit = parser.ParseUtf8Bytes(Encoding.ASCII.GetBytes("v=0"), new SdpParseOptions { MaximumInputBytes = 2 });
        SdpParseResult lineLength = parser.ParseText("v=0", new SdpParseOptions { MaximumLineLength = 2 });
        SdpParseResult lineCount = parser.ParseText("v=0\r\ns=x", new SdpParseOptions { MaximumLineCount = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(characterLimit.Success, Is.False);
            Assert.That(characterLimit.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInputCharacterLimitExceeded));
            Assert.That(characterLimit.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(characterLimit.CharactersConsumed, Is.EqualTo(0));

            Assert.That(byteLimit.Success, Is.False);
            Assert.That(byteLimit.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInputByteLimitExceeded));
            Assert.That(byteLimit.BytesConsumed, Is.EqualTo(0));

            Assert.That(lineLength.Success, Is.False);
            Assert.That(lineLength.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseLineLengthLimitExceeded));
            Assert.That(lineLength.CharactersConsumed, Is.EqualTo(3));
            Assert.That(lineLength.LinesConsumed, Is.EqualTo(1));

            Assert.That(lineCount.Success, Is.False);
            Assert.That(lineCount.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseLineCountLimitExceeded));
            Assert.That(lineCount.CharactersConsumed, Is.EqualTo("v=0\r\n".Length));
            Assert.That(lineCount.LinesConsumed, Is.EqualTo(1));
            Assert.That(lineCount.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void MediaAndAttributeLimits_UseScopeSpecificDiagnosticPaths()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseResult mediaLimit = parser.ParseText("v=0\r\nm=x 9 X 0\r\nm=y 9 X 0", new SdpParseOptions { MaximumMediaCount = 1 });
        SdpParseResult attributeLimit = parser.ParseText("v=0\r\na=x\r\na=y", new SdpParseOptions { MaximumAttributesPerScope = 1 });
        SdpParseResult resetAtMedia = parser.ParseText("v=0\r\na=s\r\nm=x 9 X 0\r\na=m", new SdpParseOptions { MaximumAttributesPerScope = 1 });
        SdpDiagnostic mediaDiagnostic = mediaLimit.Diagnostics.Count == 1 ? mediaLimit.Diagnostics[0] : new SdpDiagnostic();
        SdpDiagnostic attributeDiagnostic = attributeLimit.Diagnostics.Count == 1 ? attributeLimit.Diagnostics[0] : new SdpDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(mediaLimit.Success, Is.False);
            Assert.That(mediaLimit.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(mediaDiagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseMediaCountLimitExceeded));
            Assert.That(mediaDiagnostic.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(mediaDiagnostic.MediaIndex, Is.EqualTo(1));
            Assert.That(mediaDiagnostic.Path.MediaIndex, Is.EqualTo(1));
            Assert.That(mediaDiagnostic.Path.OccurrenceIndex, Is.EqualTo(1));

            Assert.That(attributeLimit.Success, Is.False);
            Assert.That(attributeDiagnostic.Code, Is.EqualTo(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded));
            Assert.That(attributeDiagnostic.Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(attributeDiagnostic.FieldName, Is.EqualTo('a'));
            Assert.That(attributeDiagnostic.Path.OccurrenceIndex, Is.EqualTo(1));

            Assert.That(resetAtMedia.Success, Is.True);
            Assert.That(resetAtMedia.Diagnostics, Is.Empty);
            Assert.That(resetAtMedia.Description?.MediaDescriptions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CharacterValidation_ReportsNulAndControlLocations()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseResult nul = parser.ParseText("v=0\r\nx\0", new SdpParseOptions());
        SdpParseResult control = parser.ParseText("v=0\r\nx\t", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(nul.Success, Is.False);
            Assert.That(nul.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNulCharacterNotAllowed));
            Assert.That(nul.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(6));
            Assert.That(nul.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(nul.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(2));
            Assert.That(nul.CharactersConsumed, Is.EqualTo(6));

            Assert.That(control.Success, Is.False);
            Assert.That(control.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseControlCharacterNotAllowed));
            Assert.That(control.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(6));
            Assert.That(control.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(control.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void DiagnosticCapacity_ProducesOneTerminalMarker()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions
        {
            AllowLfLineEndings = true,
            MaximumDiagnosticCount = 1
        };

        SdpParseResult result = parser.ParseText("\n\n", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
        });
    }

    [Test]
    public void InvalidParserConfiguration_IsRejectedBeforeInputProcessing()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions unknownMode = new SdpParseOptions { Mode = SdpParseMode.Unknown };
        SdpParseOptions negativeLimit = new SdpParseOptions { MaximumLineCount = -1 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseText("v=0", unknownMode));
            Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseText("v=0", negativeLimit));
        });
    }
}
