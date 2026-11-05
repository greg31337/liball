using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpAttributeLineParserTests
{
    [Test]
    public void SessionAttributes_PreserveKindSpellingValuesOrderAndDuplicates()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "a=SendRecv\r\n" +
            "a=fmtp:96 profile-level-id=42e01f\r\n" +
            "a=x-empty:\r\n" +
            "a=X-Dupe:first:value\r\n" +
            "a=X-Dupe:second";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        System.Collections.Generic.List<SdpAttribute> attributes = result.Description?.Attributes ?? new System.Collections.Generic.List<SdpAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(attributes, Has.Count.EqualTo(5));
            Assert.That(attributes[0].Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(attributes[0].Name, Is.EqualTo("SendRecv"));
            Assert.That(attributes[0].Value, Is.Empty);
            Assert.That(attributes[0].RawText, Is.EqualTo("SendRecv"));
            Assert.That(attributes[1].Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(attributes[1].Name, Is.EqualTo("fmtp"));
            Assert.That(attributes[1].Value, Is.EqualTo("96 profile-level-id=42e01f"));
            Assert.That(attributes[2].Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(attributes[2].Name, Is.EqualTo("x-empty"));
            Assert.That(attributes[2].Value, Is.Empty);
            Assert.That(attributes[2].RawText, Is.EqualTo("x-empty:"));
            Assert.That(attributes[3].Name, Is.EqualTo("X-Dupe"));
            Assert.That(attributes[3].Value, Is.EqualTo("first:value"));
            Assert.That(attributes[4].Name, Is.EqualTo("X-Dupe"));
            Assert.That(attributes[4].Value, Is.EqualTo("second"));
            Assert.That(attributes[0].Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(attributes[0].Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(attributes[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(attributes[0].Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void SessionAndMediaAttributes_AreStoredInIndependentOrderedCollections()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "a=session:first\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=mid:audio\r\n" +
            "a=sendonly\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "a=mid:video";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.Attributes, Has.Count.EqualTo(1));
            Assert.That(description.Attributes[0].Name, Is.EqualTo("session"));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Attributes, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Attributes[0].RawText, Is.EqualTo("mid:audio"));
            Assert.That(description.MediaDescriptions[0].Attributes[1].RawText, Is.EqualTo("sendonly"));
            Assert.That(description.MediaDescriptions[1].Attributes, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[1].Attributes[0].RawText, Is.EqualTo("mid:video"));
            Assert.That(description.MediaDescriptions[1].Attributes[0].Location.LineNumber, Is.EqualTo(7));
        });
    }

    [TestCase("", SdpAttributeKind.Property, "", "", 3)]
    [TestCase(":value", SdpAttributeKind.Value, "", "value", 3)]
    [TestCase("bad/name:value", SdpAttributeKind.Value, "bad/name", "value", 6)]
    [TestCase("bad name:value", SdpAttributeKind.Value, "bad name", "value", 6)]
    [TestCase("naïve:value", SdpAttributeKind.Value, "naïve", "value", 5)]
    public void InvalidAttributeNames_AreRetainedAndDiagnosed(string rawText, SdpAttributeKind expectedKind, string expectedName, string expectedValue, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\na=" + rawText, new SdpParseOptions());
        SdpAttribute attribute = result.Description?.Attributes[0] ?? new SdpAttribute();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Has.Count.EqualTo(1));
            Assert.That(attribute.Kind, Is.EqualTo(expectedKind));
            Assert.That(attribute.Name, Is.EqualTo(expectedName));
            Assert.That(attribute.Value, Is.EqualTo(expectedValue));
            Assert.That(attribute.RawText, Is.EqualTo(rawText));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('a'));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo(expectedName));
            Assert.That(result.Diagnostics[0].Path.AttributeName, Is.EqualTo(expectedName));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void InvalidAttributeDiagnostic_ReportsScopeMediaAndSourceOccurrence()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\na=valid\r\na=bad/name\r\nm=audio 9 RTP/AVP 0\r\na=also-valid\r\na=other/name:value";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Has.Count.EqualTo(2));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.EqualTo(-1));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Location.LineNumber, Is.EqualTo(6));
        });
    }

    [Test]
    public void Utf8Attribute_PreservesInternationalValueAndExactLocation()
    {
        ISdpParser parser = new SdpFragmentParser();
        string prefix = "v=0\r\ns=Résumé\r\n";
        byte[] input = Encoding.UTF8.GetBytes(prefix + "a=X-Custom:välué text:part");

        SdpParseResult result = parser.ParseUtf8Bytes(input, new SdpParseOptions());
        SdpAttribute attribute = result.Description?.Attributes[0] ?? new SdpAttribute();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(attribute.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(attribute.Name, Is.EqualTo("X-Custom"));
            Assert.That(attribute.Value, Is.EqualTo("välué text:part"));
            Assert.That(attribute.RawText, Is.EqualTo("X-Custom:välué text:part"));
            Assert.That(attribute.Location.CharacterOffset, Is.EqualTo(prefix.Length));
            Assert.That(attribute.Location.ByteOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(prefix)));
            Assert.That(attribute.Location.LineNumber, Is.EqualTo(3));
            Assert.That(attribute.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void AttributeValue_IsNotSubjectToTheNameTokenLengthLimit()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 1 };
        string value = "a long value with spaces:and:colons";

        SdpParseResult result = parser.ParseText("v=0\r\na=x:" + value, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.Attributes[0].Name, Is.EqualTo("x"));
            Assert.That(result.Description?.Attributes[0].Value, Is.EqualTo(value));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AttributeNameTokenLimit_StopsBeforeFollowingLines()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseText("v=0\r\na=abcde:value\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Is.Empty);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("abcde"));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void Utf8AttributeNameLimit_IsMeasuredInBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes("v=0\r\na=ééé:value"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void AttributeCountLimit_StopsBeforeProjectingTheExcessAttribute()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumAttributesPerScope = 2 };
        string input = "v=0\r\na=one\r\na=two:value\r\na=three\r\ns=unread";

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Has.Count.EqualTo(2));
            Assert.That(result.Description?.Attributes[0].Name, Is.EqualTo("one"));
            Assert.That(result.Description?.Attributes[1].Name, Is.EqualTo("two"));
            Assert.That(result.LinesConsumed, Is.EqualTo(4));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(2));
        });
    }

    [Test]
    public void AttributeCountLimit_ResetsForEveryMediaScope()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumAttributesPerScope = 1 };
        string input =
            "v=0\r\n" +
            "a=session\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=audio\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "a=video\r\n" +
            "a=excess";

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Attributes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[1].Attributes, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void PreservedRawLineAndProjectedAttributeShareTheOriginalSourcePosition()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { PreserveRawLines = true };

        SdpParseResult result = parser.ParseText("v=0\r\na=control:trackID=1", options);
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.Attributes, Has.Count.EqualTo(1));
            Assert.That(description.RawLines, Has.Count.EqualTo(2));
            Assert.That(description.RawLines[1].FieldName, Is.EqualTo('a'));
            Assert.That(description.RawLines[1].Value, Is.EqualTo("control:trackID=1"));
            Assert.That(description.RawLines[1].Location.CharacterOffset, Is.EqualTo(description.Attributes[0].Location.CharacterOffset));
            Assert.That(description.RawLines[1].Location.LineNumber, Is.EqualTo(description.Attributes[0].Location.LineNumber));
        });
    }
}
