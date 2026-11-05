using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpEncryptionKeyLineParserTests
{
    [TestCase("prompt", "prompt", false, "")]
    [TestCase("clear:secret phrase:part", "clear", true, "secret phrase:part")]
    [TestCase("base64:YQ==", "base64", true, "YQ==")]
    [TestCase("base64:YWJj", "base64", true, "YWJj")]
    [TestCase("base64:+/==", "base64", true, "+/==")]
    [TestCase("base64:", "base64", true, "")]
    [TestCase("uri:https://keys.example.test/key?id=1", "uri", true, "https://keys.example.test/key?id=1")]
    [TestCase("uri:", "uri", true, "")]
    [TestCase("x-vendor:opaque value", "x-vendor", true, "opaque value")]
    [TestCase("extension", "extension", false, "")]
    public void EncryptionKey_PreservesRegisteredAndExtensionForms(string rawText, string expectedMethod, bool expectedHasValue, string expectedValue)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };

        SdpParseResult result = parser.ParseText("v=0\r\nk=" + rawText, options);
        SdpEncryptionKey key = result.Description?.EncryptionKey ?? new SdpEncryptionKey();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(key.Method, Is.EqualTo(expectedMethod));
            Assert.That(key.HasValue, Is.EqualTo(expectedHasValue));
            Assert.That(key.Value, Is.EqualTo(expectedValue));
            Assert.That(key.RawText, Is.EqualTo(rawText));
            Assert.That(key.Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(key.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(key.Location.LineNumber, Is.EqualTo(2));
            Assert.That(key.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void StrictMode_RetainsKeyAndReportsAnObsoleteSyntaxWarning()
    {
        ISdpParser parser = new SdpFragmentParser();
        string sensitiveValue = "do-not-copy-this-secret";

        SdpParseResult result = parser.ParseText("v=0\r\nk=clear:" + sensitiveValue, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.EncryptionKey?.Value, Is.EqualTo(sensitiveValue));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationObsoleteSyntax));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('k'));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.EqualTo(-1));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain(sensitiveValue));
        });
    }

    [TestCase(SdpParseMode.Interoperable)]
    [TestCase(SdpParseMode.Lossless)]
    public void CompatibilityModes_DoNotWarnForRetainedObsoleteKeys(SdpParseMode mode)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nk=prompt", new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void StrictMediaKeyWarning_CarriesMediaScopeAndIndex()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=audio 9 RTP/AVP 0\r\nk=prompt", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.MediaDescriptions[0].EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationObsoleteSyntax));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void SessionAndMediaKeys_AreStoredIndependently()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };
        string input =
            "v=0\r\n" +
            "k=prompt\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "k=base64:YQ==\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "k=uri:https://keys.example.test/video";

        SdpParseResult result = parser.ParseText(input, options);
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].EncryptionKey?.Method, Is.EqualTo("base64"));
            Assert.That(description.MediaDescriptions[0].EncryptionKey?.Value, Is.EqualTo("YQ=="));
            Assert.That(description.MediaDescriptions[0].EncryptionKey?.Location.LineNumber, Is.EqualTo(4));
            Assert.That(description.MediaDescriptions[1].EncryptionKey?.Method, Is.EqualTo("uri"));
            Assert.That(description.MediaDescriptions[1].EncryptionKey?.Value, Is.EqualTo("https://keys.example.test/video"));
        });
    }

    [Test]
    public void DuplicateKeys_PreserveTheFirstValueAndResetCardinalityForEachMedia()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };
        string input =
            "v=0\r\n" +
            "k=prompt\r\n" +
            "k=clear:session duplicate\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "k=clear:audio first\r\n" +
            "k=clear:audio duplicate\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "k=clear:video first";

        SdpParseResult result = parser.ParseText(input, options);
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(description.EncryptionKey?.RawText, Is.EqualTo("prompt"));
            Assert.That(description.MediaDescriptions[0].EncryptionKey?.Value, Is.EqualTo("audio first"));
            Assert.That(description.MediaDescriptions[1].EncryptionKey?.Value, Is.EqualTo("video first"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [TestCase("", "", false, "", 3)]
    [TestCase("bad method:value", "bad method", true, "value", 6)]
    [TestCase("prompt:value", "prompt", true, "value", 9)]
    [TestCase("clear", "clear", false, "", 8)]
    [TestCase("clear:", "clear", true, "", 9)]
    [TestCase("base64", "base64", false, "", 9)]
    [TestCase("uri", "uri", false, "", 6)]
    public void InvalidKeyShapes_AreRetainedAndDiagnosed(string rawText, string expectedMethod, bool expectedHasValue, string expectedValue, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };

        SdpParseResult result = parser.ParseText("v=0\r\nk=" + rawText, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.EncryptionKey?.Method, Is.EqualTo(expectedMethod));
            Assert.That(result.Description?.EncryptionKey?.HasValue, Is.EqualTo(expectedHasValue));
            Assert.That(result.Description?.EncryptionKey?.Value, Is.EqualTo(expectedValue));
            Assert.That(result.Description?.EncryptionKey?.RawText, Is.EqualTo(rawText));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [TestCase("base64:YQ=")]
    [TestCase("base64:Y===")]
    [TestCase("base64:YW J")]
    [TestCase("uri:path with-space")]
    [TestCase("uri:https://example.test/%ZZ")]
    public void InvalidRegisteredKeyValues_AreRetainedAndDiagnosedWithoutEchoingValues(string rawText)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };

        SdpParseResult result = parser.ParseText("v=0\r\nk=" + rawText, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.EncryptionKey?.RawText, Is.EqualTo(rawText));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain(result.Description?.EncryptionKey?.Value));
        });
    }

    [Test]
    public void ClearKeyValue_IsTextAndIsNotSubjectToTheTokenLengthLimit()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Interoperable,
            MaximumTokenLength = 5
        };
        string value = "a long clear key value";

        SdpParseResult result = parser.ParseText("v=0\r\nk=clear:" + value, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.EncryptionKey?.Value, Is.EqualTo(value));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [TestCase("k=prompt", 4, 3)]
    [TestCase("k=base64:YWJjYQ==", 6, 10)]
    public void EncryptionKeyTokenLimit_StopsBeforeFollowingLines(string keyLine, int maximumTokenLength, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Interoperable,
            MaximumTokenLength = maximumTokenLength
        };

        SdpParseResult result = parser.ParseText("v=0\r\n" + keyLine + "\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.EncryptionKey, Is.Null);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void Utf8EncryptionKeyMethodLimit_IsMeasuredInBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions
        {
            Mode = SdpParseMode.Interoperable,
            MaximumTokenLength = 4
        };

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes("v=0\r\nk=ééé:value"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.EncryptionKey, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }
}
