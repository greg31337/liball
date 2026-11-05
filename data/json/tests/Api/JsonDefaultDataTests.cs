namespace json.Tests;

public class JsonDefaultDataTests
{
    [Test]
    public void ResourceDefaultsHaveStableSafeValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonDefaults.MaximumDocumentBytes, Is.EqualTo(16_777_216));
            Assert.That(JsonDefaults.MaximumInputBufferBytes, Is.EqualTo(1_048_576));
            Assert.That(JsonDefaults.MaximumTokenBytes, Is.EqualTo(1_048_576));
            Assert.That(JsonDefaults.MaximumDecodedStringBytes, Is.EqualTo(1_048_576));
            Assert.That(JsonDefaults.MaximumDecodedStringCodeUnits, Is.EqualTo(1_048_576));
            Assert.That(JsonDefaults.MaximumNumberCharacters, Is.EqualTo(1_024));
            Assert.That(JsonDefaults.MaximumNestingDepth, Is.EqualTo(64));
            Assert.That(JsonDefaults.MaximumArrayItems, Is.EqualTo(1_000_000));
            Assert.That(JsonDefaults.MaximumObjectMembers, Is.EqualTo(1_000_000));
            Assert.That(JsonDefaults.MaximumDomNodes, Is.EqualTo(1_000_000));
            Assert.That(JsonDefaults.MaximumOutputBytes, Is.EqualTo(16_777_216));
            Assert.That(JsonDefaults.StreamBufferBytes, Is.EqualTo(16_384));
            Assert.That(JsonDefaults.MaximumDiagnosticExcerptBytes, Is.EqualTo(256));
        });
    }

    [Test]
    public void FormattingDefaultsProduceConventionalPrettyJsonWhitespace()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonDefaults.Indentation, Is.EqualTo("  "));
            Assert.That(JsonDefaults.NewLine, Is.EqualTo("\n"));
            Assert.That(JsonDefaults.SpaceAfterColon, Is.True);
        });
    }

    [Test]
    public void MediaTypesUseRegisteredJsonValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonMediaTypes.ApplicationJson, Is.EqualTo("application/json"));
            Assert.That(JsonMediaTypes.ApplicationJsonSequence, Is.EqualTo("application/json-seq"));
        });
    }

    [TestCase(JsonSyntaxBytes.BeginObject, '{')]
    [TestCase(JsonSyntaxBytes.EndObject, '}')]
    [TestCase(JsonSyntaxBytes.BeginArray, '[')]
    [TestCase(JsonSyntaxBytes.EndArray, ']')]
    [TestCase(JsonSyntaxBytes.NameSeparator, ':')]
    [TestCase(JsonSyntaxBytes.ValueSeparator, ',')]
    [TestCase(JsonSyntaxBytes.StringDelimiter, '"')]
    [TestCase(JsonSyntaxBytes.Escape, '\\')]
    [TestCase(JsonSyntaxBytes.Space, ' ')]
    [TestCase(JsonSyntaxBytes.HorizontalTab, '\t')]
    [TestCase(JsonSyntaxBytes.LineFeed, '\n')]
    [TestCase(JsonSyntaxBytes.CarriageReturn, '\r')]
    [TestCase(JsonSyntaxBytes.RecordSeparator, '\u001e')]
    public void SyntaxByteMatchesUtf8CodeUnit(byte actual, char expected)
    {
        Assert.That(actual, Is.EqualTo((byte)expected));
    }
}
