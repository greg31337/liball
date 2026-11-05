using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpAdversarialLimitTests
{
    [Test]
    public void TotalInputLimits_AcceptExactBoundaryAndRejectTheNextUnit()
    {
        string text =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=Résumé\r\n" +
            "t=0 0";
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        ISdpParser parser = new SdpParser();

        SdpParseResult exactCharacters = parser.ParseText(text, new SdpParseOptions { MaximumInputCharacters = text.Length });
        SdpParseResult excessCharacters = parser.ParseText(text, new SdpParseOptions { MaximumInputCharacters = text.Length - 1 });
        SdpParseResult exactBytes = parser.ParseUtf8Bytes(utf8, new SdpParseOptions { MaximumInputBytes = utf8.Length });
        SdpParseResult excessBytes = parser.ParseUtf8Bytes(utf8, new SdpParseOptions { MaximumInputBytes = utf8.Length - 1 });

        Assert.Multiple(() =>
        {
            Assert.That(exactCharacters.Success, Is.True);
            Assert.That(exactCharacters.Diagnostics, Is.Empty);
            Assert.That(exactBytes.Success, Is.True);
            Assert.That(exactBytes.Diagnostics, Is.Empty);
            Assert.That(excessCharacters.Success, Is.False);
            Assert.That(excessCharacters.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInputCharacterLimitExceeded));
            Assert.That(excessCharacters.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(excessCharacters.CharactersConsumed, Is.Zero);
            Assert.That(excessBytes.Success, Is.False);
            Assert.That(excessBytes.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInputByteLimitExceeded));
            Assert.That(excessBytes.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(excessBytes.BytesConsumed, Is.Zero);
        });
    }

    [Test]
    public void LineAndFieldStormLimits_ReturnOnlyTheBoundedPartialDescription()
    {
        ISdpParser parser = new SdpFragmentParser();
        string overlongField = "a=12345678\r\na=ok";
        string fieldStorm = "v=0\r\na=one\r\na=two\r\na=three\r\na=four";

        SdpParseResult lineLength = parser.ParseText(overlongField, new SdpParseOptions { MaximumLineLength = 9 });
        SdpParseResult lineCount = parser.ParseText(fieldStorm, new SdpParseOptions { MaximumLineCount = 3 });

        Assert.Multiple(() =>
        {
            Assert.That(lineLength.Success, Is.False);
            Assert.That(lineLength.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(lineLength.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseLineLengthLimitExceeded));
            Assert.That(lineLength.LinesConsumed, Is.EqualTo(2));
            Assert.That(lineLength.Description?.Attributes, Has.Count.EqualTo(1));
            Assert.That(lineLength.Description?.Attributes[0].Name, Is.EqualTo("ok"));

            Assert.That(lineCount.Success, Is.False);
            Assert.That(lineCount.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(lineCount.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseLineCountLimitExceeded));
            Assert.That(lineCount.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(lineCount.LinesConsumed, Is.EqualTo(3));
            Assert.That(lineCount.Description?.Attributes, Has.Count.EqualTo(2));
            Assert.That(lineCount.Description?.Attributes[1].Name, Is.EqualTo("two"));
        });
    }

    [Test]
    public void MediaAndAttributeStormLimits_DoNotProjectTheOverflowingField()
    {
        ISdpParser parser = new SdpFragmentParser();
        string mediaStorm =
            "m=audio 1000 RTP/AVP 0\r\n" +
            "m=audio 1002 RTP/AVP 0\r\n" +
            "m=audio 1004 RTP/AVP 0\r\n" +
            "a=unread";
        string attributeStorm =
            "a=one\r\n" +
            "a=two\r\n" +
            "a=three\r\n" +
            "m=unread 9 X 0";

        SdpParseResult media = parser.ParseText(mediaStorm, new SdpParseOptions { MaximumMediaCount = 2 });
        SdpParseResult attributes = parser.ParseText(attributeStorm, new SdpParseOptions { MaximumAttributesPerScope = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(media.Success, Is.False);
            Assert.That(media.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMediaCountLimitExceeded));
            Assert.That(media.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(media.Description?.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(media.Description?.MediaDescriptions[1].Port, Is.EqualTo(1002));
            Assert.That(media.LinesConsumed, Is.EqualTo(3));

            Assert.That(attributes.Success, Is.False);
            Assert.That(attributes.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded));
            Assert.That(attributes.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(attributes.Description?.Attributes, Has.Count.EqualTo(2));
            Assert.That(attributes.Description?.MediaDescriptions, Is.Empty);
            Assert.That(attributes.LinesConsumed, Is.EqualTo(3));
        });
    }

    [Test]
    public void Utf8OriginTokenLimit_IsMeasuredInBytesWithoutChangingTextSemantics()
    {
        string input =
            "v=0\r\n" +
            "o=éé 1 1 IN IP4 h\r\n" +
            "s=x\r\n" +
            "t=0 0";
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 3 };
        ISdpParser parser = new SdpParser();

        SdpParseResult text = parser.ParseText(input, options);
        SdpParseResult utf8 = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes(input), options);

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(text.Description?.Origin?.Username, Is.EqualTo("éé"));
            Assert.That(utf8.Success, Is.False);
            Assert.That(utf8.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(utf8.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(utf8.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(utf8.Diagnostics[0].FieldName, Is.EqualTo('o'));
            Assert.That(utf8.Description?.Origin, Is.Null);
            Assert.That(utf8.LinesConsumed, Is.EqualTo(2));
        });
    }

    [Test]
    public void WarningStorm_EndsWithAFatalDiagnosticCapacityMarker()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=x\r\n" +
            "t=0 0\r\n" +
            "x=one\r\n" +
            "y=two\r\n" +
            "m=unread 9 X 0";
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Interoperable,
            MaximumDiagnosticCount = 1
        };
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.LinesConsumed, Is.EqualTo(6));
            Assert.That(result.Description?.MediaDescriptions, Is.Empty);
        });
    }

    [Test]
    public void FatalFieldCondition_IsNeverHiddenByTheDiagnosticLimitMarker()
    {
        string input = "o=toolong 1 1 IN IP4 h\n";
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Interoperable,
            MaximumTokenLength = 4,
            MaximumDiagnosticCount = 1
        };
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('o'));
            Assert.That(result.Description?.Origin, Is.Null);
            Assert.That(result.LinesConsumed, Is.EqualTo(1));
        });
    }
}
