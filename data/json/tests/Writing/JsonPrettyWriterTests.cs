namespace json.Tests;

public class JsonPrettyWriterTests
{
    [Test]
    public void DefaultPrettyOptionsFormatNestedAndEmptyContainers()
    {
        WriterFixture fixture = new WriterFixture(CreatePrettyOptions());

        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("emptyObject"));
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.WritePropertyName("emptyArray"));
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WritePropertyName("nested"));
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("value"));
        AssertSuccess(fixture.Writer.WriteBoolean(true));
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "{\n" +
                "  \"emptyObject\": {},\n" +
                "  \"emptyArray\": [],\n" +
                "  \"nested\": [\n" +
                "    {\n" +
                "      \"value\": true\n" +
                "    },\n" +
                "    []\n" +
                "  ]\n" +
                "}"));
    }

    [Test]
    public void CustomIndentationCrLfAndOmittedColonSpaceApplyAtEveryDepth()
    {
        JsonWriterOptions options = CreatePrettyOptions();
        options.Indentation = "\t ";
        options.NewLine = "\r\n";
        options.OmitSpaceAfterColon = true;
        WriterFixture fixture = new WriterFixture(options);

        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("a"));
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("b"));
        AssertSuccess(fixture.Writer.WriteBoolean(true));
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "{\r\n" +
                "\t \"a\":[\r\n" +
                "\t \t {\r\n" +
                "\t \t \t \"b\":true\r\n" +
                "\t \t },\r\n" +
                "\t \t []\r\n" +
                "\t ]\r\n" +
                "}"));
    }

    [Test]
    public void ColonSpacingOptionChangesOnlyPrettyObjectNames()
    {
        JsonWriterOptions spacedOptions = CreatePrettyOptions();
        WriterFixture spaced = new WriterFixture(spacedOptions);
        WriteSingleProperty(spaced);

        JsonWriterOptions tightOptions = CreatePrettyOptions();
        tightOptions.OmitSpaceAfterColon = true;
        WriterFixture tight = new WriterFixture(tightOptions);
        WriteSingleProperty(tight);

        JsonWriterOptions compactOptions = new JsonWriterOptions();
        compactOptions.OmitSpaceAfterColon = false;
        WriterFixture compact = new WriterFixture(compactOptions);
        WriteSingleProperty(compact);

        Assert.Multiple(() =>
        {
            Assert.That(spaced.Text(), Is.EqualTo("{\n  \"a\": 1\n}"));
            Assert.That(tight.Text(), Is.EqualTo("{\n  \"a\":1\n}"));
            Assert.That(compact.Text(), Is.EqualTo("{\"a\":1}"));
        });
    }

    [TestCase("", "[\n0\n]")]
    [TestCase(" ", "[\n 0\n]")]
    [TestCase("\t", "[\n\t0\n]")]
    [TestCase(" \t", "[\n \t0\n]")]
    [TestCase("\t ", "[\n\t 0\n]")]
    public void EveryValidIndentationFormIsRepeatedPerDepth(
        string indentation,
        string expected)
    {
        JsonWriterOptions options = CreatePrettyOptions();
        options.Indentation = indentation;
        WriterFixture fixture = new WriterFixture(options);

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteInt64(0));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(fixture.Text(), Is.EqualTo(expected));
    }

    [Test]
    public void LfAndCrLfAreTheOnlyEmittedNewLineForms()
    {
        JsonWriterOptions lfOptions = CreatePrettyOptions();
        lfOptions.NewLine = "\n";
        WriterFixture lf = new WriterFixture(lfOptions);
        WriteTwoItems(lf);

        JsonWriterOptions crlfOptions = CreatePrettyOptions();
        crlfOptions.NewLine = "\r\n";
        WriterFixture crlf = new WriterFixture(crlfOptions);
        WriteTwoItems(crlf);

        Assert.Multiple(() =>
        {
            Assert.That(lf.Text(), Is.EqualTo("[\n  1,\n  2\n]"));
            Assert.That(crlf.Text(),
                Is.EqualTo("[\r\n  1,\r\n  2\r\n]"));
        });
    }

    [Test]
    public void CompactModeEmitsNoLayoutForValidPrettySettings()
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Compact;
        options.Indentation = "\t ";
        options.NewLine = "\r\n";
        options.OmitSpaceAfterColon = false;
        WriterFixture fixture = new WriterFixture(options);

        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("a"));
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteInt64(1));
        AssertSuccess(fixture.Writer.WriteInt64(2));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(fixture.Text(), Is.EqualTo("{\"a\":[1,2]}"));
    }

    [Test]
    public void NonJsonIndentationCharactersAreRejectedByPublicWriter()
    {
        string[] invalidIndentations = new string[]
        {
            "x",
            "\0",
            "\n",
            "\r",
            "\v",
            "\f",
            "\u0085",
            "\u00a0",
            "\u1680",
            "\u2003",
            "\u2028",
            "\u2029",
            " \tx"
        };

        foreach (string indentation in invalidIndentations)
        {
            JsonWriterOptions options = CreatePrettyOptions();
            options.Indentation = indentation;
            AssertInvalidOptions(options, indentation);
        }
    }

    [Test]
    public void NonJsonLineEndingsAreRejectedByPublicWriter()
    {
        string[] invalidNewLines = new string[]
        {
            "",
            "\0",
            " ",
            "\t",
            "\r",
            "\n\r",
            "\n\n",
            "\r\r\n",
            " \n",
            "\u0085",
            "\u2028",
            "\u2029"
        };

        foreach (string newLine in invalidNewLines)
        {
            JsonWriterOptions options = CreatePrettyOptions();
            options.NewLine = newLine;
            AssertInvalidOptions(options, newLine);
        }
    }

    static void WriteSingleProperty(WriterFixture fixture)
    {
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("a"));
        AssertSuccess(fixture.Writer.WriteInt64(1));
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());
    }

    static void WriteTwoItems(WriterFixture fixture)
    {
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteInt64(1));
        AssertSuccess(fixture.Writer.WriteInt64(2));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());
    }

    static void AssertInvalidOptions(
        JsonWriterOptions options,
        string invalidValue)
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
            new JsonUtf8Writer(options, target))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(
                failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions),
                Convert.ToHexString(
                    System.Text.Encoding.UTF8.GetBytes(invalidValue)));
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    static JsonWriterOptions CreatePrettyOptions()
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        return options;
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    sealed class WriterFixture
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterFixture(JsonWriterOptions options)
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }

        internal string Text()
        {
            return System.Text.Encoding.UTF8.GetString(
                Destination.WrittenSpan);
        }
    }
}
